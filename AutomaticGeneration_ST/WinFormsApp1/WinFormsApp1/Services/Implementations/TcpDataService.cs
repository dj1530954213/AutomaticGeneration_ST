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
                Unit = GetValue<string>(row, "单位") ?? "",
                RangeMin = GetValue<double?>(row, "量程低") ?? GetValue<double?>(row, "最小值"),
                RangeMax = GetValue<double?>(row, "量程高") ?? GetValue<double?>(row, "最大值"),
                ShhValue = GetValue<double?>(row, "SHH值") ?? GetValue<double?>(row, "shh_value"),
                ShValue = GetValue<double?>(row, "SH值") ?? GetValue<double?>(row, "sh_value"),
                SlValue = GetValue<double?>(row, "SL值") ?? GetValue<double?>(row, "sl_value"),
                SllValue = GetValue<double?>(row, "SLL值") ?? GetValue<double?>(row, "sll_value"),
                ShhPoint = GetValue<string>(row, "SHH点"),
                ShPoint = GetValue<string>(row, "SH点"),
                SlPoint = GetValue<string>(row, "SL点"),
                SllPoint = GetValue<string>(row, "SLL点"),
                MaintenanceStatusTag = GetValue<string>(row, "维护状态变量") ?? $"{hmiTagName}_WHZZT",
                MaintenanceValueTag = GetValue<string>(row, "维护值变量") ?? $"{hmiTagName}_WHZ"
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