using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using OfficeOpenXml; // 引入EPPlus的命名空间
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.Implementations
{
    public class ExcelDataService : IDataService
    {
        private readonly LogService _logger = LogService.Instance;
        
        // 在类级别设置EPPlus的许可证上下文。这是EPPlus 5.x及以上版本所必需的。
        static ExcelDataService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // 或 Commercial，根据你的项目许可证选择
        }

        public DataContext LoadData(string excelFilePath)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath))
                throw new ArgumentException("Excel文件路径不能为空", nameof(excelFilePath));

            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException($"Excel文件不存在: {excelFilePath}");

            _logger.LogInfo($"📂 开始加载Excel文件: {Path.GetFileName(excelFilePath)}");

            try
            {
                // 初始化核心数据结构
                var context = new DataContext();
                var pointsAssignedToDevices = new HashSet<string>();

                using (var package = new ExcelPackage(new FileInfo(excelFilePath)))
                {
                    if (package.Workbook?.Worksheets == null || package.Workbook.Worksheets.Count == 0)
                        throw new InvalidDataException("Excel文件无效或没有工作表");

                    _logger.LogInfo($"📊 Excel文件包含 {package.Workbook.Worksheets.Count} 个工作表: {string.Join(", ", package.Workbook.Worksheets.Select(ws => ws.Name))}");

                    // --- 步骤 1: 解析 "IO点表"，构建点位数据的基础和权威 ---
                    _logger.LogInfo("🔍 步骤1: 开始解析IO点表...");
                    var ioSheet = package.Workbook.Worksheets["IO点表"];
                    if (ioSheet == null) 
                    {
                        var availableSheets = string.Join(", ", package.Workbook.Worksheets.Select(ws => ws.Name));
                        throw new InvalidDataException($"在Excel文件中未找到名为'IO点表'的工作簿。可用的工作表: {availableSheets}");
                    }

                    var parsedPointsCount = ParseIoSheet(ioSheet, context.AllPointsMasterList);
                    _logger.LogSuccess($"✅ IO点表解析完成，共解析 {parsedPointsCount} 个点位");

                // --- 步骤 2: 解析 "设备分类表"，构建设备实例并关联点位 ---
                _logger.LogInfo("🏭 步骤2: 开始处理设备分类表...");
                var deviceSheet = package.Workbook.Worksheets["设备分类表"];
                if (deviceSheet != null)
                {
                    var deviceMap = new Dictionary<string, Device>(); // 临时字典用于高效构建设备
                    ParseDeviceSheet(deviceSheet, deviceMap, context.AllPointsMasterList, pointsAssignedToDevices);
                    context.Devices = deviceMap.Values.ToList();
                    _logger.LogSuccess($"✅ 设备分类表解析完成，共构建 {context.Devices.Count} 个设备，关联 {pointsAssignedToDevices.Count} 个点位");
                    
                    // 输出设备统计信息
                    if (context.Devices.Any())
                    {
                        var deviceStats = context.Devices.GroupBy(d => d.TemplateName ?? "未指定模板")
                                                        .ToDictionary(g => g.Key, g => g.Count());
                        foreach (var stat in deviceStats.OrderByDescending(x => x.Value))
                        {
                            _logger.LogInfo($"   📋 模板 [{stat.Key}]: {stat.Value} 个设备");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ 未找到设备分类表，将跳过设备构建步骤");
                    context.Devices = new List<Device>();
                }

                // --- 步骤 3: （可选但强烈建议）解析其他点表，以捕获可能遗漏的点位 ---
                _logger.LogInfo("📋 步骤3: 处理其他设备专用表...");
                var otherSheetNames = new List<string> { "阀门", "调节阀", "可燃气体探测器", "低压开关柜", "撬装机柜" };
                int processedSheetCount = 0;
                foreach (var sheetName in otherSheetNames)
                {
                    var otherSheet = package.Workbook.Worksheets[sheetName];
                    if (otherSheet != null)
                    {
                        ParseOtherSheet(otherSheet, context.AllPointsMasterList);
                        processedSheetCount++;
                        _logger.LogInfo($"   ✓ 处理表格: {sheetName}");
                    }
                }
                _logger.LogInfo($"📊 其他设备表处理完成，共处理 {processedSheetCount} 个表格");

                    // --- 步骤 4: 最终识别并分离独立点位 ---
                    _logger.LogInfo("🔍 步骤4: 识别独立点位...");
                    // 此步骤必须在所有点位和设备都处理完毕后执行。
                    context.StandalonePoints = context.AllPointsMasterList.Values
                        .Where(p => !pointsAssignedToDevices.Contains(p.HmiTagName))
                        .ToList();
                    
                    _logger.LogSuccess($"✅ 识别出 {context.StandalonePoints.Count} 个独立点位");
                    
                    // 输出点位类型统计
                    if (context.StandalonePoints.Any())
                    {
                        var pointTypeStats = context.StandalonePoints.GroupBy(p => p.GetType().Name)
                                                                   .ToDictionary(g => g.Key, g => g.Count());
                        foreach (var stat in pointTypeStats.OrderByDescending(x => x.Value))
                        {
                            _logger.LogInfo($"   📊 独立点位 [{stat.Key}]: {stat.Value} 个");
                        }
                    }
                }

                _logger.LogSuccess($"🎉 Excel数据加载完成！");
                _logger.LogInfo($"📈 数据统计汇总:");
                _logger.LogInfo($"   • 设备总数: {context.Devices.Count}");
                _logger.LogInfo($"   • 点位总数: {context.AllPointsMasterList.Count}");
                _logger.LogInfo($"   • 设备关联点位: {pointsAssignedToDevices.Count}");
                _logger.LogInfo($"   • 独立点位: {context.StandalonePoints.Count}");
                
                return context;
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is FileNotFoundException || ex is InvalidDataException))
            {
                _logger.LogError($"❌ Excel数据加载时发生未预期错误: {ex.Message}");
                throw new Exception($"Excel文件解析失败: {ex.Message}", ex);
            }
        }

        private int ParseIoSheet(ExcelWorksheet sheet, Dictionary<string, Models.Point> masterList)
        {
            if (sheet.Dimension == null) 
            {
                _logger.LogWarning("⚠️ IO点表工作簿为空或没有数据");
                return 0;
            }

            var totalRows = sheet.Dimension.End.Row - sheet.Dimension.Start.Row;
            _logger.LogInfo($"📊 IO点表包含 {totalRows} 行数据（包含表头）");

            // 获取列索引
            var headerIndexes = GetColumnIndexes(sheet);
            if (headerIndexes.Count == 0)
            {
                throw new InvalidDataException("IO点表未找到有效的列标题");
            }
            
            LogInfo($"成功识别 {headerIndexes.Count} 个列标题");

            int parsedCount = 0;
            int skippedCount = 0;
            int errorCount = 0;

            // 遍历数据行（从第2行开始，跳过表头）
            for (int row = sheet.Dimension.Start.Row + 1; row <= sheet.Dimension.End.Row; row++)
            {
                var hmiTagName = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("变量名称（HMI）", 0)]);
                if (string.IsNullOrWhiteSpace(hmiTagName)) 
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    var point = new Models.Point(hmiTagName)
                    {
                        ModuleName = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("模块名称", 0)]),
                        ModuleType = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("模块类型", 0)]),
                        PowerSupplyType = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("供电类型", 0)]),
                        WireSystem = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("线制", 0)]),
                        ChannelNumber = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("通道位号", 0)]),
                        StationName = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("场站名", 0)]),
                        StationId = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("场站编号", 0)]),
                        Description = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("变量描述", 0)]),
                        DataType = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("数据类型", 0)]),
                        PlcAbsoluteAddress = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("PLC绝对地址", 0)]),
                        ScadaCommAddress = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("上位机通讯地址", 0)]),
                        StoreHistory = GetCellValue<bool?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("是否历史存储", 0)]),
                        PowerDownProtection = GetCellValue<bool?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("是否掉电保护", 0)]),
                        RangeLow = GetCellValue<double?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("量程低", 0)]),
                        RangeHigh = GetCellValue<double?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("量程高", 0)]),
                        Unit = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("单位", 0)]),
                        InstrumentType = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("仪表类型", 0)]),
                        PointType = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("点位类型", 0)]),
                        // 添加报警相关字段
                        SHH_Value = GetCellValue<double?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("SHH值", 0)]),
                        SH_Value = GetCellValue<double?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("SH值", 0)]),
                        SL_Value = GetCellValue<double?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("SL值", 0)]),
                        SLL_Value = GetCellValue<double?>(sheet.Cells[row, headerIndexes.GetValueOrDefault("SLL值", 0)])
                    };

                    if (!masterList.ContainsKey(hmiTagName))
                    {
                        masterList.Add(hmiTagName, point);
                        parsedCount++;
                    }
                    else
                    {
                        LogWarning($"重复的点位名称 '{hmiTagName}' 在第 {row} 行，已跳过");
                        skippedCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    errorCount++;
                    LogError($"解析第 {row} 行时出错 (点位: {hmiTagName}): {ex.Message}");
                    
                    // 如果错误太多，停止解析
                    if (errorCount > 50)
                    {
                        throw new Exception($"IO点表解析错误过多 ({errorCount} 个)，请检查Excel文件格式");
                    }
                }
            }

            LogInfo($"IO点表解析完成 - 成功: {parsedCount}, 跳过: {skippedCount}, 错误: {errorCount}");
            
            if (parsedCount == 0)
            {
                throw new InvalidDataException("IO点表没有成功解析任何点位，请检查文件格式");
            }

            return parsedCount;
        }

        private void ParseDeviceSheet(ExcelWorksheet sheet, Dictionary<string, Device> deviceMap,
            Dictionary<string, Models.Point> masterList, HashSet<string> pointsAssignedToDevices)
        {
            if (sheet.Dimension == null) return;

            // 获取列索引
            var headerIndexes = GetColumnIndexes(sheet);

            // 遍历数据行
            for (int row = sheet.Dimension.Start.Row + 1; row <= sheet.Dimension.End.Row; row++)
            {
                var deviceTag = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("设备位号", 0)]);
                var templateName = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("模板名称", 0)]);
                var hmiTagName = GetCellValue<string>(sheet.Cells[row, headerIndexes.GetValueOrDefault("变量名称（HMI）", 0)]);

                if (string.IsNullOrWhiteSpace(deviceTag) || string.IsNullOrWhiteSpace(hmiTagName)) continue;

                // 检查设备是否已存在
                if (!deviceMap.ContainsKey(deviceTag))
                {
                    deviceMap[deviceTag] = new Device(deviceTag, templateName ?? "");
                }

                // 查找并添加点位
                if (masterList.TryGetValue(hmiTagName, out var point))
                {
                    deviceMap[deviceTag].AddPoint(point);
                    pointsAssignedToDevices.Add(hmiTagName);
                }
            }
        }

        private void ParseOtherSheet(ExcelWorksheet sheet, Dictionary<string, Models.Point> masterList)
        {
            if (sheet.Dimension == null) return;

            // 获取列索引
            var headerIndexes = GetColumnIndexes(sheet);
            var hmiTagColumnIndex = headerIndexes.GetValueOrDefault("变量名称（HMI）", 0);
            
            if (hmiTagColumnIndex == 0) return; // 如果没有找到HMI变量名称列，跳过

            // 遍历数据行
            for (int row = sheet.Dimension.Start.Row + 1; row <= sheet.Dimension.End.Row; row++)
            {
                var hmiTagName = GetCellValue<string>(sheet.Cells[row, hmiTagColumnIndex]);
                if (string.IsNullOrWhiteSpace(hmiTagName)) continue;

                // 如果这个点位不在主列表中，创建一个基本的点位对象
                if (!masterList.ContainsKey(hmiTagName))
                {
                    try
                    {
                        var point = new Models.Point(hmiTagName);
                        // 尝试填充一些基本信息
                        if (headerIndexes.ContainsKey("变量描述"))
                        {
                            point.Description = GetCellValue<string>(sheet.Cells[row, headerIndexes["变量描述"]]);
                        }
                        masterList.Add(hmiTagName, point);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"解析其他工作表行 {row} 时出错: {ex.Message}");
                    }
                }
            }
        }

        private Dictionary<string, int> GetColumnIndexes(ExcelWorksheet sheet)
        {
            var indexes = new Dictionary<string, int>();
            
            if (sheet.Dimension == null) return indexes;

            // 读取第一行（表头）
            for (int col = sheet.Dimension.Start.Column; col <= sheet.Dimension.End.Column; col++)
            {
                var headerValue = GetCellValue<string>(sheet.Cells[1, col]);
                if (!string.IsNullOrWhiteSpace(headerValue))
                {
                    indexes[headerValue.Trim()] = col;
                }
            }

            return indexes;
        }

        private T GetCellValue<T>(ExcelRange cell)
        {
            if (cell?.Value == null)
                return default(T);

            try
            {
                var value = cell.Value;
                
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value.ToString();
                }
                else if (typeof(T) == typeof(double?) || typeof(T) == typeof(double))
                {
                    if (double.TryParse(value.ToString(), out double doubleValue))
                        return (T)(object)doubleValue;
                    return default(T);
                }
                else if (typeof(T) == typeof(bool?) || typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(value.ToString(), out bool boolValue))
                        return (T)(object)boolValue;
                    
                    var stringValue = value.ToString()?.ToLower();
                    if (stringValue == "是" || stringValue == "y" || stringValue == "yes")
                        return (T)(object)true;
                    if (stringValue == "否" || stringValue == "n" || stringValue == "no")
                        return (T)(object)false;
                    
                    return default(T);
                }
                else
                {
                    return (T)value;
                }
            }
            catch
            {
                return default(T);
            }
        }

        // 简单的日志记录方法
        private void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} ExcelDataService: {message}");
            System.Diagnostics.Debug.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} ExcelDataService: {message}");
        }

        private void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} ExcelDataService: {message}");
            System.Diagnostics.Debug.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} ExcelDataService: {message}");
        }

        private void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} ExcelDataService: {message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} ExcelDataService: {message}");
        }
    }
}