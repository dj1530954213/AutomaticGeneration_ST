using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinFormsApp1.Excel;
using WinFormsApp1.Models;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// TCP数据处理服务实现
    /// </summary>
    public class TcpDataService : ITcpDataService
    {
        private readonly IExcelWorkbookParser _excelParser;
        private readonly IWorksheetLocatorService _worksheetLocator;

        public TcpDataService(
            IExcelWorkbookParser excelParser,
            IWorksheetLocatorService worksheetLocator)
        {
            _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));
            _worksheetLocator = worksheetLocator ?? throw new ArgumentNullException(nameof(worksheetLocator));
        }

        public List<TcpCommunicationPoint> ProcessTcpCommunicationTable(string excelFilePath)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath))
                throw new ArgumentException("Excel文件路径不能为空", nameof(excelFilePath));

            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException($"Excel文件不存在: {excelFilePath}");

            Console.WriteLine("[INFO] 开始处理TCP通讯表...");

            // 查找TCP通讯表工作表
            var tcpWorksheetNames = new[] { "TCP通讯表" };
            List<Dictionary<string, object>> tcpData = null;
            string actualWorksheetName = "";

            foreach (var worksheetName in tcpWorksheetNames)
            {
                var validation = _worksheetLocator.ValidateWorksheet(excelFilePath, worksheetName);
                if (validation.IsFound)
                {
                    actualWorksheetName = validation.ActualName;
                    Console.WriteLine($"[INFO] 找到TCP通讯表: '{actualWorksheetName}' (匹配类型: {validation.MatchType})");
                    tcpData = _excelParser.ParseWorksheetSmart(excelFilePath, worksheetName, _worksheetLocator);
                    break;
                }
            }

            if (tcpData == null || !tcpData.Any())
            {
                Console.WriteLine("[WARNING] 未找到TCP通讯表或表为空，返回空列表");
                return new List<TcpCommunicationPoint>();
            }

            Console.WriteLine($"[INFO] TCP通讯表包含 {tcpData.Count} 行数据");

            // 解析TCP点位数据
            var tcpPoints = new List<TcpCommunicationPoint>();
            int successCount = 0;
            int errorCount = 0;

            foreach (var row in tcpData)
            {
                try
                {
                    var tcpPoint = CreateTcpPointFromRow(row);
                    if (tcpPoint != null && tcpPoint.IsValid())
                    {
                        tcpPoints.Add(tcpPoint);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"[WARNING] 处理TCP点位数据时出错: {ex.Message}");
                }
            }

            // 逐行到其他工作表查找并补全字段（以 TCP 通讯表为主表）
            EnrichTcpPointsFromOtherSheets(excelFilePath, tcpPoints);

            Console.WriteLine($"[INFO] TCP通讯表处理完成: 成功 {successCount} 个，错误 {errorCount} 个");
            return tcpPoints;
        }

        public async Task<List<TcpCommunicationPoint>> ProcessTcpCommunicationTableAsync(string excelFilePath)
        {
            return await Task.Run(() => ProcessTcpCommunicationTable(excelFilePath));
        }

        public ValidationResult ValidateTcpPoints(List<TcpCommunicationPoint> tcpPoints)
        {
            var result = new ValidationResult();

            if (tcpPoints == null || !tcpPoints.Any())
            {
                result.AddWarning("TCP点位列表为空");
                return result;
            }

            var duplicateNames = tcpPoints
                .GroupBy(p => p.HmiTagName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateName in duplicateNames)
            {
                result.AddError($"发现重复的HMI标签名称: {duplicateName}");
            }

            foreach (var point in tcpPoints)
            {
                if (!point.IsValid())
                {
                    result.AddError($"TCP点位 '{point.HmiTagName}' 数据不完整或无效");
                }

                if (point is TcpAnalogPoint analogPoint)
                {
                    ValidateAnalogPoint(analogPoint, result);
                }
                else if (point is TcpDigitalPoint digitalPoint)
                {
                    ValidateDigitalPoint(digitalPoint, result);
                }
            }

            return result;
        }

        public List<TcpAnalogPoint> GetAnalogPoints(List<TcpCommunicationPoint> tcpPoints)
        {
            return tcpPoints?.OfType<TcpAnalogPoint>().ToList() ?? new List<TcpAnalogPoint>();
        }

        public List<TcpDigitalPoint> GetDigitalPoints(List<TcpCommunicationPoint> tcpPoints)
        {
            return tcpPoints?.OfType<TcpDigitalPoint>().ToList() ?? new List<TcpDigitalPoint>();
        }

        private TcpCommunicationPoint CreateTcpPointFromRow(object rowData)
        {
            var row = rowData as Dictionary<string, object> ?? new Dictionary<string, object>();

            // 获取基础数据
            var hmiTagName = GetValue<string>(row, "变量名称（HMI）");
            var dataType = GetValue<string>(row, "数据类型");
            var description = GetValue<string>(row, "变量描述") ?? GetValue<string>(row, "描述");
            var channel = GetValue<string>(row, "起始TCP通道名称") ?? GetValue<string>(row, "CHANNEL");

            if (string.IsNullOrWhiteSpace(hmiTagName) || string.IsNullOrWhiteSpace(dataType))
            {
                return null;
            }

            // 根据数据类型创建不同的TCP点位
            TcpCommunicationPoint tcpPoint;

            if (dataType.ToUpper() == "BOOL")
            {
                tcpPoint = CreateTcpDigitalPoint(row, hmiTagName, dataType, description, channel);
            }
            else if (dataType.ToUpper() is "REAL" or "INT" or "DINT")
            {
                tcpPoint = CreateTcpAnalogPoint(row, hmiTagName, dataType, description, channel);
            }
            else
            {
                Console.WriteLine($"[WARNING] 不支持的TCP数据类型: {dataType} (点位: {hmiTagName})");
                return null;
            }

            // 设置通用属性
            tcpPoint.TcpAddress = GetValue<string>(row, "TCP地址") ?? GetValue<string>(row, "地址");
            tcpPoint.ByteOrder = GetValue<int?>(row, "BYTE_ORDER");
            tcpPoint.TypeNumber = GetValue<int?>(row, "TYPE_NUMBER");

            return tcpPoint;
        }

        private TcpDigitalPoint CreateTcpDigitalPoint(Dictionary<string, object> row, string hmiTagName, 
            string dataType, string description, string channel)
        {
            return new TcpDigitalPoint
            {
                HmiTagName = hmiTagName,
                DataType = dataType,
                Description = description ?? "",
                Channel = channel ?? "",
                InitialState = GetValue<bool>(row, "初始状态") || GetValue<bool>(row, "默认值"),
                BitAddress = GetValue<int>(row, "位地址"),
                RegisterAddress = GetValue<string>(row, "寄存器地址") ?? "",
                StateInvert = GetValue<bool>(row, "状态反转"),
                TrueStateDescription = GetValue<string>(row, "开状态描述") ?? "ON",
                FalseStateDescription = GetValue<string>(row, "关状态描述") ?? "OFF"
            };
        }

        #region 跨表字段补全辅助

/// <summary>
/// 构建跨表索引：以其他工作表中的 "变量名称" 或 "变量名称（HMI）" 为键，不包含 IO点表 / 设备分类表 / TCP通讯表
/// </summary>
private Dictionary<string, Dictionary<string, object>> BuildHmiLookup(string excelFilePath)
{
    var lookup = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
    var sheetNames = _excelParser.GetWorksheetNames(excelFilePath);
    foreach (var sheet in sheetNames)
    {
        var lower = sheet.ToLower();
        if (lower.Contains("tcp通讯表") || lower.Contains("io点表") || lower.Contains("设备分类表"))
            continue;
        var rows = _excelParser.ParseWorksheet(excelFilePath, sheet);
        foreach (var row in rows)
        {
            var keysToTry = new[] { "变量名称", "变量名称（HMI）" };
            string key = keysToTry.Select(k => row.TryGetValue(k, out var o) ? o?.ToString() : null)
                                   .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (!string.IsNullOrWhiteSpace(key))
            {
                lookup[key] = row;
            }
        }
    }
    Console.WriteLine($"[INFO] 跨表索引构建完成，共 {lookup.Count} 条记录");
    return lookup;
}

/// <summary>
/// 使用 lookup 数据补全 TCP 点位的描述及报警限值等字段
/// </summary>
private void EnrichTcpPointsFromLookup(List<TcpCommunicationPoint> points, Dictionary<string, Dictionary<string, object>> lookup)
{
    foreach (var p in points)
    {
        if (!lookup.TryGetValue(p.HmiTagName, out var row)) continue;
        var description = GetValue<string>(row, "变量描述") ?? GetValue<string>(row, "描述");
        if (!string.IsNullOrWhiteSpace(description)) p.Description = description;

        if (p is TcpAnalogPoint analog)
        {
            analog.ShhValue ??= GetValue<double?>(row, "SHH设定值") ?? GetValue<double?>(row, "SHH值");
            analog.ShValue  ??= GetValue<double?>(row, "SH设定值")  ?? GetValue<double?>(row, "SH值");
            analog.SlValue  ??= GetValue<double?>(row, "SL设定值")  ?? GetValue<double?>(row, "SL值");
            analog.SllValue ??= GetValue<double?>(row, "SLL设定值") ?? GetValue<double?>(row, "SLL值");
        }
    }
}

#endregion

        /// <summary>
        /// 逐行遍历其它工作表，为 TCP 点位补全描述、报警限值等辅助字段。
        /// </summary>
        private void EnrichTcpPointsFromOtherSheets(string excelFilePath, List<TcpCommunicationPoint> points)
        {
            if (points == null || points.Count == 0) return;

            var sheetNames = _excelParser.GetWorksheetNames(excelFilePath);
            // 预解析所有可能用到的工作表，避免每个点位都重新解析
            var sheetRowsCache = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in sheetNames)
            {
                var lower = sheet.ToLower();
                if (lower.Contains("tcp通讯表") || lower.Contains("io点表") || lower.Contains("设备分类表"))
                    continue;
                sheetRowsCache[sheet] = _excelParser.ParseWorksheet(excelFilePath, sheet);
            }

            foreach (var p in points)
            {
                bool found = false;
                foreach (var kvp in sheetRowsCache)
                {
                    var rows = kvp.Value;
                    var matchedRow = rows.FirstOrDefault(r =>
                        (r.TryGetValue("变量名称", out var v1) && string.Equals(v1?.ToString(), p.HmiTagName, StringComparison.OrdinalIgnoreCase)) ||
                        (r.TryGetValue("变量名称（HMI）", out var v2) && string.Equals(v2?.ToString(), p.HmiTagName, StringComparison.OrdinalIgnoreCase)));
                    if (matchedRow != null)
                    {
                        // 描述
                        var description = GetValue<string>(matchedRow, "变量描述") ?? GetValue<string>(matchedRow, "描述");
                        if (!string.IsNullOrWhiteSpace(description)) p.Description = description;

                        if (p is TcpAnalogPoint analog)
                        {
                            analog.ShhValue ??= GetValue<double?>(matchedRow, "SHH设定值") ?? GetValue<double?>(matchedRow, "SHH值");
                            analog.ShValue  ??= GetValue<double?>(matchedRow, "SH设定值")  ?? GetValue<double?>(matchedRow, "SH值");
                            analog.SlValue  ??= GetValue<double?>(matchedRow, "SL设定值")  ?? GetValue<double?>(matchedRow, "SL值");
                            analog.SllValue ??= GetValue<double?>(matchedRow, "SLL设定值") ?? GetValue<double?>(matchedRow, "SLL值");
                        }
                        found = true;
                        break; // 已找到匹配行，无需继续其它表
                    }
                }
                if (!found)
                {
                    Console.WriteLine($"[DEBUG] 未在其它表找到变量 {p.HmiTagName}");
                }
            }
        }


private TcpAnalogPoint CreateTcpAnalogPoint(Dictionary<string, object> row, string hmiTagName, 
            string dataType, string description, string channel)
        {
            return new TcpAnalogPoint
            {
                HmiTagName = hmiTagName,
                DataType = dataType,
                Description = description ?? "",
                Channel = channel ?? "",
                Scale = GetValue<double?>(row, "SCALE") ?? GetValue<double?>(row, "缩放因子"),
                // Unit = GetValue<string>(row, "单位") ?? "",
                // RangeMin = GetValue<double?>(row, "量程低") ?? GetValue<double?>(row, "最小值"),
                // RangeMax = GetValue<double?>(row, "量程高") ?? GetValue<double?>(row, "最大值"),
                ShhValue = GetValue<double?>(row, "SHH设定值") ?? GetValue<double?>(row, "shh_value"),
                ShValue = GetValue<double?>(row, "SH设定值") ?? GetValue<double?>(row, "sh_value"),
                SlValue = GetValue<double?>(row, "SL设定值") ?? GetValue<double?>(row, "sl_value"),
                SllValue = GetValue<double?>(row, "SLL设定值") ?? GetValue<double?>(row, "sll_value"),
                // ShhPoint = GetValue<string>(row, "SHH点"),
                // ShPoint = GetValue<string>(row, "SH点"),
                // SlPoint = GetValue<string>(row, "SL点"),
                // SllPoint = GetValue<string>(row, "SLL点"),
                // MaintenanceStatusTag = GetValue<string>(row, "维护状态变量") ?? $"{hmiTagName}_WHZZT",
                // MaintenanceValueTag = GetValue<string>(row, "维护值变量") ?? $"{hmiTagName}_WHZ"
            };
        }

        private void ValidateAnalogPoint(TcpAnalogPoint point, ValidationResult result)
        {
            if (point.RangeMin.HasValue && point.RangeMax.HasValue && point.RangeMin >= point.RangeMax)
            {
                result.AddError($"模拟量点位 '{point.HmiTagName}' 的量程配置错误：最小值不能大于等于最大值");
            }

            if (point.ShhValue.HasValue && point.ShValue.HasValue && point.ShhValue <= point.ShValue)
            {
                result.AddWarning($"模拟量点位 '{point.HmiTagName}' 的报警配置可能有问题：HH值应该大于H值");
            }

            if (point.SlValue.HasValue && point.SllValue.HasValue && point.SlValue <= point.SllValue)
            {
                result.AddWarning($"模拟量点位 '{point.HmiTagName}' 的报警配置可能有问题：L值应该大于LL值");
            }
        }

        private void ValidateDigitalPoint(TcpDigitalPoint point, ValidationResult result)
        {
            if (point.BitAddress < 0 || point.BitAddress > 15)
            {
                result.AddWarning($"数字量点位 '{point.HmiTagName}' 的位地址 {point.BitAddress} 可能超出有效范围 (0-15)");
            }
        }

        private T GetValue<T>(Dictionary<string, object> row, string columnName)
        {
            return DataExtractorHelper.GetValue<T>(row, columnName);
        }
    }
}