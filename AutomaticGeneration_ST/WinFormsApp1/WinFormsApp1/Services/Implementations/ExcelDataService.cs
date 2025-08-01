using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

                // --- 步骤 2: 解析 "设备分类表"，构建设备实例和点位字典 ---
                _logger.LogInfo("🏭 步骤2: 开始处理设备分类表...");
                var deviceSheet = package.Workbook.Worksheets["设备分类表"];
                var deviceMap = new Dictionary<string, Device>(); // 临时字典用于高效构建设备
                
                if (deviceSheet != null)
                {
                    ParseDeviceClassificationSheet(deviceSheet, deviceMap, context.AllPointsMasterList, pointsAssignedToDevices);
                    _logger.LogSuccess($"✅ 设备分类表解析完成，创建了 {deviceMap.Count} 个设备");
                }
                else
                {
                    _logger.LogWarning("⚠️ 未找到设备分类表，将跳过设备构建步骤");
                }

                // --- 步骤 3: 解析设备专用表，填充软点位的详细信息 ---
                _logger.LogInfo("📋 步骤3: 处理设备专用表，填充软点位详细信息...");
                var deviceSheetNames = new List<string> { "阀门", "调节阀", "可燃气体探测器", "低压开关柜", "撬装机柜" };
                int processedSheetCount = 0;
                
                foreach (var sheetName in deviceSheetNames)
                {
                    var devicePointSheet = package.Workbook.Worksheets[sheetName];
                    if (devicePointSheet != null)
                    {
                        FillDevicePointDetails(devicePointSheet, deviceMap, sheetName);
                        processedSheetCount++;
                        _logger.LogInfo($"   ✓ 处理设备表: {sheetName}");
                    }
                }
                
                context.Devices = deviceMap.Values.ToList();
                _logger.LogInfo($"📊 设备点位加载完成，共处理 {processedSheetCount} 个设备表");
                
                // 输出设备统计信息
                if (context.Devices.Any())
                {
                    foreach (var device in context.Devices)
                    {
                        _logger.LogInfo($"   📋 设备 [{device.DeviceTag}] ({device.TemplateName}): IO点位={device.IoPoints.Count}, 设备点位={device.DevicePoints.Count}");
                    }
                }

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
                var hmiTagName = GetSafeFieldValue<string>(sheet, row, headerIndexes, "变量名称（HMI）");
                if (string.IsNullOrWhiteSpace(hmiTagName)) 
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    var point = new Models.Point(hmiTagName)
                    {
                        ModuleName = GetSafeFieldValue<string>(sheet, row, headerIndexes, "模块名称"),
                        ModuleType = GetSafeFieldValue<string>(sheet, row, headerIndexes, "模块类型"),
                        PowerSupplyType = GetSafeFieldValue<string>(sheet, row, headerIndexes, "供电类型（有源/无源）"),
                        WireSystem = GetSafeFieldValue<string>(sheet, row, headerIndexes, "线制"),
                        ChannelNumber = GetSafeFieldValue<string>(sheet, row, headerIndexes, "通道位号"),
                        StationName = GetSafeFieldValue<string>(sheet, row, headerIndexes, "场站名"),
                        StationId = GetSafeFieldValue<string>(sheet, row, headerIndexes, "场站编号"),
                        Description = GetSafeFieldValue<string>(sheet, row, headerIndexes, "变量描述"),
                        DataType = GetSafeFieldValue<string>(sheet, row, headerIndexes, "数据类型"),
                        PlcAbsoluteAddress = GetSafeFieldValue<string>(sheet, row, headerIndexes, "PLC绝对地址"),
                        ScadaCommAddress = GetSafeFieldValue<string>(sheet, row, headerIndexes, "上位机通讯地址"),
                        StoreHistory = GetSafeFieldValue<bool?>(sheet, row, headerIndexes, "是否历史存储"),
                        PowerDownProtection = GetSafeFieldValue<bool?>(sheet, row, headerIndexes, "是否掉电保护"),
                        RangeLow = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "量程低"),
                        RangeHigh = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "量程高"),
                        Unit = GetSafeFieldValue<string>(sheet, row, headerIndexes, "单位"),
                        InstrumentType = GetSafeFieldValue<string>(sheet, row, headerIndexes, "仪表类型"),
                        PointType = GetSafeFieldValue<string>(sheet, row, headerIndexes, "点位类型"),
                        // 添加报警相关字段
                        SHH_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SHH值"),
                        SH_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SH值"),
                        SL_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SL值"),
                        SLL_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SLL值")
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

        /// <summary>
        /// 解析设备分类表，创建设备实例和点位字典
        /// </summary>
        private void ParseDeviceClassificationSheet(ExcelWorksheet sheet, Dictionary<string, Device> deviceMap,
            Dictionary<string, Models.Point> masterList, HashSet<string> pointsAssignedToDevices)
        {
            if (sheet.Dimension == null) 
            {
                _logger.LogWarning("⚠️ 设备分类表为空或没有数据");
                return;
            }

            var totalRows = sheet.Dimension.End.Row - sheet.Dimension.Start.Row;
            _logger.LogInfo($"📊 设备分类表包含 {totalRows} 行数据（包含表头）");

            // 获取列索引
            var headerIndexes = GetColumnIndexes(sheet);
            
            if (headerIndexes.Count == 0)
            {
                _logger.LogError("❌ 设备分类表未找到有效的列标题");
                return;
            }

            // 输出找到的列标题，帮助调试
            _logger.LogInfo($"📋 设备分类表包含字段: {string.Join(", ", headerIndexes.Keys)}");

            // 检查关键字段是否存在，支持多种可能的字段名
            var deviceTagFields = new[] { "设备位号", "设备号", "设备标签", "Device Tag", "DeviceTag" };
            var templateFields = new[] { "模板名称", "模板", "Template", "TemplateName" };
            var hmiTagFields = new[] { "变量名称（HMI）", "变量名称", "HMI名", "HMI Tag", "TagName" };
            var categoryFields = new[] { "设备类别(硬点、软点、通讯点)", "设备类别", "类别", "Category", "Type" };

            var deviceTagField = deviceTagFields.FirstOrDefault(f => headerIndexes.ContainsKey(f));
            var templateField = templateFields.FirstOrDefault(f => headerIndexes.ContainsKey(f));
            var hmiTagField = hmiTagFields.FirstOrDefault(f => headerIndexes.ContainsKey(f));
            var categoryField = categoryFields.FirstOrDefault(f => headerIndexes.ContainsKey(f));

            if (string.IsNullOrEmpty(deviceTagField))
            {
                _logger.LogError($"❌ 未找到设备位号字段，尝试过的字段名: {string.Join(", ", deviceTagFields)}");
                return;
            }

            if (string.IsNullOrEmpty(hmiTagField))
            {
                _logger.LogError($"❌ 未找到HMI变量名字段，尝试过的字段名: {string.Join(", ", hmiTagFields)}");
                return;
            }

            _logger.LogInfo($"✓ 使用字段映射: 设备位号='{deviceTagField}', 模板='{templateField}', HMI变量='{hmiTagField}', 类别='{categoryField}'");

            int processedRows = 0;
            int createdDevices = 0;
            int skippedRows = 0;

            // 遍历数据行，为每个设备创建点位字典
            for (int row = sheet.Dimension.Start.Row + 1; row <= sheet.Dimension.End.Row; row++)
            {
                try
                {
                    var deviceTag = GetSafeFieldValue<string>(sheet, row, headerIndexes, deviceTagField);
                    var templateName = GetSafeFieldValue<string>(sheet, row, headerIndexes, templateField ?? "");
                    var hmiTagName = GetSafeFieldValue<string>(sheet, row, headerIndexes, hmiTagField);

                    processedRows++;

                    if (string.IsNullOrWhiteSpace(deviceTag) || string.IsNullOrWhiteSpace(hmiTagName)) 
                    {
                        skippedRows++;
                        continue;
                    }

                    // 检查设备是否已存在，如果不存在则创建
                    if (!deviceMap.ContainsKey(deviceTag))
                    {
                        deviceMap[deviceTag] = new Device(deviceTag, templateName ?? "");
                        createdDevices++;
                        _logger.LogInfo($"   ✓ 创建新设备: [{deviceTag}] 模板='{templateName}'");
                    }

                    // 先检查是否在IO表中存在（硬点）
                    if (masterList.TryGetValue(hmiTagName, out var ioPoint))
                    {
                        // 硬点：从IO表获取详细信息
                        var ioPointData = new Dictionary<string, object>
                        {
                            ["变量名称（HMI名）"] = ioPoint.HmiTagName ?? "",
                            ["模块名称"] = ioPoint.ModuleName ?? "",
                            ["模块类型"] = ioPoint.ModuleType ?? "",
                            ["供电类型（有源/无源）"] = ioPoint.PowerSupplyType ?? "",
                            ["线制"] = ioPoint.WireSystem ?? "",
                            ["通道位号"] = ioPoint.ChannelNumber ?? "",
                            ["场站名"] = ioPoint.StationName ?? "",
                            ["场站编号"] = ioPoint.StationId ?? "",
                            ["变量描述"] = ioPoint.Description ?? "",
                            ["数据类型"] = ioPoint.DataType ?? "",
                            ["PLC绝对地址"] = ioPoint.PlcAbsoluteAddress ?? "",
                            ["上位机通讯地址"] = ioPoint.ScadaCommAddress ?? "",
                            ["是否历史存储"] = ioPoint.StoreHistory,
                            ["是否掉电保护"] = ioPoint.PowerDownProtection,
                            ["量程低"] = ioPoint.RangeLow,
                            ["量程高"] = ioPoint.RangeHigh,
                            ["单位"] = ioPoint.Unit ?? "",
                            ["仪表类型"] = ioPoint.InstrumentType ?? "",
                            ["点位类型"] = ioPoint.PointType ?? "",
                            ["SHH值"] = ioPoint.SHH_Value,
                            ["SH值"] = ioPoint.SH_Value,
                            ["SL值"] = ioPoint.SL_Value,
                            ["SLL值"] = ioPoint.SLL_Value
                        };
                        
                        deviceMap[deviceTag].AddIoPoint(hmiTagName, ioPointData);
                        pointsAssignedToDevices.Add(hmiTagName);
                    }
                    else
                    {
                        // 软点：先创建基础信息，详细信息稍后从设备专用表获取
                        var softPointData = new Dictionary<string, object>
                        {
                            ["变量名称"] = hmiTagName,
                            ["变量描述"] = "", // 从设备专用表获取
                            ["数据类型"] = "", // 从设备专用表获取
                            ["PLC地址"] = "", // 从设备专用表获取
                            ["MODBUS地址"] = "" // 从设备专用表获取
                        };
                        
                        deviceMap[deviceTag].AddDevicePoint(hmiTagName, softPointData);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"   ❌ 解析设备分类表第{row}行时出错: {ex.Message}");
                }
            }

            _logger.LogInfo($"📊 设备分类表解析统计:");
            _logger.LogInfo($"   • 处理行数: {processedRows}");
            _logger.LogInfo($"   • 创建设备: {createdDevices}");
            _logger.LogInfo($"   • 硬点位: {pointsAssignedToDevices.Count}");
            _logger.LogInfo($"   • 跳过行数: {skippedRows}");
        }

        /// <summary>
        /// 填充设备专用表的软点位详细信息
        /// </summary>
        private void FillDevicePointDetails(ExcelWorksheet sheet, Dictionary<string, Device> deviceMap, string sheetName)
        {
            if (sheet.Dimension == null) return;

            // 获取列索引
            var headerIndexes = GetColumnIndexes(sheet);
            int updatedPoints = 0;

            // 遍历数据行
            for (int row = sheet.Dimension.Start.Row + 1; row <= sheet.Dimension.End.Row; row++)
            {
                try
                {
                    var variableName = GetSafeFieldValue<string>(sheet, row, headerIndexes, "变量名称");
                    if (string.IsNullOrWhiteSpace(variableName)) continue;

                    // 查找包含此软点位的设备
                    Device targetDevice = null;
                    foreach (var device in deviceMap.Values)
                    {
                        if (device.DevicePoints.ContainsKey(variableName))
                        {
                            targetDevice = device;
                            break;
                        }
                    }

                    if (targetDevice != null)
                    {
                        // 更新软点位的详细信息
                        var updatedPointData = new Dictionary<string, object>();
                        
                        // 遍历所有列，将数据存入字典
                        foreach (var header in headerIndexes)
                        {
                            try
                            {
                                var cellValue = GetSafeFieldValue<object>(sheet, row, headerIndexes, header.Key);
                                updatedPointData[header.Key] = cellValue ?? "";
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"   ⚠️ 获取{sheetName}表第{row}行'{header.Key}'字段时出错: {ex.Message}");
                                updatedPointData[header.Key] = "";
                            }
                        }

                        // 更新设备中的软点位数据
                        targetDevice.DevicePoints[variableName] = updatedPointData;
                        updatedPoints++;
                        _logger.LogInfo($"   ✓ 更新设备 [{targetDevice.DeviceTag}] 软点位: {variableName}");
                    }
                    else
                    {
                        _logger.LogWarning($"   ⚠️ 软点位 {variableName} 在设备分类表中未找到对应设备");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"   ❌ 处理{sheetName}表第{row}行时出错: {ex.Message}");
                }
            }

            _logger.LogInfo($"   📊 {sheetName}表处理完成，更新了 {updatedPoints} 个软点位");
        }


        // 旧的ParseOtherSheet方法已被ParseDevicePointSheet替代，该方法支持字典结构存储
        // [已废弃] 原方法只处理Point对象，现在需要分别处理IO点位和设备点位

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

        /// <summary>
        /// 安全地获取字段值，处理字段不存在或为空的情况
        /// </summary>
        private T GetSafeFieldValue<T>(ExcelWorksheet sheet, int row, Dictionary<string, int> headerIndexes, string fieldName)
        {
            try
            {
                // 检查字段是否存在于表头中
                if (!headerIndexes.ContainsKey(fieldName))
                {
                    return default(T);
                }

                int columnIndex = headerIndexes[fieldName];
                
                // 检查列索引是否有效
                if (columnIndex <= 0 || columnIndex > sheet.Dimension.End.Column)
                {
                    return default(T);
                }

                return GetCellValue<T>(sheet.Cells[row, columnIndex]);
            }
            catch (Exception ex)
            {
                // 记录警告但不抛出异常，返回默认值
                System.Diagnostics.Debug.WriteLine($"获取字段 '{fieldName}' 第{row}行时出错: {ex.Message}");
                return default(T);
            }
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