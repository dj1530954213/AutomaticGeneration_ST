using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using AutomaticGeneration_ST.Services;
using OfficeOpenXml; // 引入EPPlus的命名空间
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.Implementations
{
    public class ExcelDataService : IDataService
    {
        private readonly LogService _logger = LogService.Instance;
        private readonly IWorksheetLocatorService _worksheetLocator;

        // 支持向后兼容的无参构造函数
        public ExcelDataService() : this(null)
        {
        }

        // 支持新架构的有参构造函数
        public ExcelDataService(IWorksheetLocatorService worksheetLocator)
        {
            _worksheetLocator = worksheetLocator;
        }
        
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
                    var ioSheet = FindWorksheetSmart(package, "IO点表");
                    if (ioSheet == null) 
                    {
                        var availableSheets = string.Join(", ", package.Workbook.Worksheets.Select(ws => ws.Name));
                        throw new InvalidDataException(
                            $"在Excel文件中未找到名为'IO点表'的工作表。\n" +
                            $"可用的工作表: {availableSheets}\n" +
                            $"建议使用以下别名: IO, IO表, Points, 点位表, 点表");
                    }

                    var parsedPointsCount = ParseIoSheet(ioSheet, context.AllPointsMasterList);
                    _logger.LogSuccess($"✅ IO点表解析完成，共解析 {parsedPointsCount} 个点位");

                // --- 步骤 2: 解析 "设备分类表"，构建设备实例和点位字典 ---
                _logger.LogInfo("🏭 步骤2: 开始处理设备分类表...");
                var deviceSheet = FindWorksheetSmart(package, "设备分类表");
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
                    var devicePointSheet = FindWorksheetSmart(package, sheetName);
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

                // --- 步骤 5: 处理TCP通讯表（新增功能）---
                ProcessTcpCommunicationTableInLegacyService(excelFilePath, context);

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
                    //实际解析点表的地方
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
                        RangeLow = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "量程低限"),
                        RangeHigh = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "量程高限"),
                        Unit = GetSafeFieldValue<string>(sheet, row, headerIndexes, "单位"),
                        InstrumentType = GetSafeFieldValue<string>(sheet, row, headerIndexes, "仪表类型"),
                        PointType = GetSafeFieldValue<string>(sheet, row, headerIndexes, "点位类型"),
                        // 添加报警相关字段
                        SHH_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SHH设定值"),
                        SH_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SH设定值"),
                        SL_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SL设定值"),
                        SLL_Value = GetSafeFieldValue<double?>(sheet, row, headerIndexes, "SLL设定值")
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

        /// <summary>
        /// 智能查找工作表 - 支持模糊匹配和别名
        /// </summary>
        /// <param name="package">Excel包</param>
        /// <param name="expectedName">期望的工作表名称</param>
        /// <returns>找到的工作表，如果未找到则返回null</returns>
        private ExcelWorksheet FindWorksheetSmart(ExcelPackage package, string expectedName)
        {
            if (package?.Workbook?.Worksheets == null || string.IsNullOrWhiteSpace(expectedName))
                return null;

            var worksheets = package.Workbook.Worksheets;

            // 如果有新的工作表定位服务，优先使用
            if (_worksheetLocator != null)
            {
                try
                {
                    // 伴随临时文件路径，需要从包中获取所有工作表名称
                    var availableNames = worksheets.Select(w => w.Name).ToList();
                    var match = FindWorksheetByLogic(availableNames, expectedName);
                    if (!string.IsNullOrEmpty(match))
                    {
                        var found = worksheets[match];
                        if (found != null)
                        {
                            LogInfo($"智能匹配工作表: '{expectedName}' -> '{match}'");
                            return found;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"智能工作表定位失败，使用默认逻辑: {ex.Message}");
                }
            }

            // 使用内置的智能匹配逻辑
            var availableSheets = worksheets.Select(w => w.Name).ToList();
            var matchedName = FindWorksheetByLogic(availableSheets, expectedName);
            if (!string.IsNullOrEmpty(matchedName))
            {
                var result = worksheets[matchedName];
                if (result != null)
                {
                    LogInfo($"内置匹配工作表: '{expectedName}' -> '{matchedName}'");
                }
                return result;
            }

            return null;
        }

        /// <summary>
        /// 工作表查找逻辑 - 支持多种匹配策略
        /// </summary>
        private string FindWorksheetByLogic(List<string> availableNames, string expectedName)
        {
            if (availableNames == null || !availableNames.Any() || string.IsNullOrWhiteSpace(expectedName))
                return null;

            // 1. 精确匹配
            var exactMatch = availableNames.FirstOrDefault(n => n == expectedName);
            if (!string.IsNullOrEmpty(exactMatch))
                return exactMatch;

            // 2. 忽略大小写匹配
            var ignoreCaseMatch = availableNames.FirstOrDefault(n => 
                string.Equals(n, expectedName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(ignoreCaseMatch))
                return ignoreCaseMatch;

            // 3. 去除空格和特殊字符匹配
            var normalizedExpected = NormalizeWorksheetName(expectedName);
            var normalizedMatch = availableNames.FirstOrDefault(n => 
                NormalizeWorksheetName(n) == normalizedExpected);
            if (!string.IsNullOrEmpty(normalizedMatch))
                return normalizedMatch;

            // 4. 模糊匹配（包含关系）
            var fuzzyMatch = availableNames.FirstOrDefault(n => 
                n.Contains(expectedName, StringComparison.OrdinalIgnoreCase) ||
                expectedName.Contains(n, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(fuzzyMatch))
                return fuzzyMatch;

            // 5. 别名匹配
            var aliasMatch = FindByBuiltInAliases(availableNames, expectedName);
            if (!string.IsNullOrEmpty(aliasMatch))
                return aliasMatch;

            return null;
        }

        /// <summary>
        /// 根据内置别名查找工作表
        /// </summary>
        private string FindByBuiltInAliases(List<string> availableNames, string expectedName)
        {
            // 预定义的工作表别名映射
            var aliases = new Dictionary<string, string[]>()
            {
                ["IO点表"] = new[] { "IO", "IO表", "Points", "IO Points", "点位表", "点表" },
                ["设备分类表"] = new[] { "设备分类", "分类表", "Device", "Devices", "设备表", "设备" },
                ["阀门"] = new[] { "Valve", "Valves", "阀" },
                ["调节阀"] = new[] { "Control Valve", "CV", "调节", "控制阀" },
                ["可燃气体探测器"] = new[] { "气体探测器", "Gas Detector", "Gas", "探测器" },
                ["低压开关柜"] = new[] { "开关柜", "Switchgear", "LV Panel", "低压柜" },
                ["撇装机柜"] = new[] { "机柜", "Cabinet", "Skid", "撇装" },
                ["加臭"] = new[] { "Odorizer", "Odorant", "臭化" },
                ["恒电位仪"] = new[] { "Potentiostat", "电位仪" }
            };

            // 检查expectedName是否有预定义的别名
            if (aliases.ContainsKey(expectedName))
            {
                var candidateAliases = aliases[expectedName];
                foreach (var alias in candidateAliases)
                {
                    var match = availableNames.FirstOrDefault(n => 
                        string.Equals(n, alias, StringComparison.OrdinalIgnoreCase) ||
                        NormalizeWorksheetName(n) == NormalizeWorksheetName(alias));
                    if (!string.IsNullOrEmpty(match))
                        return match;
                }
            }

            // 反向查找：检查expectedName是否是某个主名称的别名
            foreach (var kvp in aliases)
            {
                if (kvp.Value.Any(alias => string.Equals(alias, expectedName, StringComparison.OrdinalIgnoreCase)))
                {
                    // 尝试找到主名称或其他别名
                    foreach (var candidate in new[] { kvp.Key }.Concat(kvp.Value))
                    {
                        var match = availableNames.FirstOrDefault(n => 
                            string.Equals(n, candidate, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(match))
                            return match;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 标准化工作表名称
        /// </summary>
        private string NormalizeWorksheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // 移除空格、制表符、换行符等
            var normalized = System.Text.RegularExpressions.Regex.Replace(name, @"\s+", "");
            
            // 移除常见的特殊字符
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[\(\)\[\]\-_、，（）【】]", "");
            
            return normalized.ToLowerInvariant();
        }

        /// <summary>
        /// 在传统ExcelDataService中处理TCP通讯表 - 安全集成方案
        /// </summary>
        private void ProcessTcpCommunicationTableInLegacyService(string excelFilePath, DataContext context)
        {
            try
            {
                _logger.LogInfo("🌐 步骤5: 开始处理TCP通讯表...");
                
                // 创建临时的服务容器来获取TCP服务
                var templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template-mapping.json");
                
                // 检查配置文件是否存在
                if (!File.Exists(configPath))
                {
                    _logger.LogWarning($"⚠️ TCP处理配置文件不存在: {configPath}，跳过TCP通讯处理");
                    return;
                }
                
                var serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);
                var tcpService = serviceContainer.GetService<ITcpDataService>();
                
                if (tcpService == null)
                {
                    _logger.LogWarning("⚠️ TCP数据服务未配置，跳过TCP通讯处理");
                    return;
                }

                // 处理TCP通讯表
                var tcpPoints = tcpService.ProcessTcpCommunicationTable(excelFilePath);
                if (tcpPoints?.Any() == true)
                {
                    // 将TCP点位数据存储到context的元数据中
                    // 这样不会破坏现有的DataContext结构
                    if (context.Metadata == null)
                    {
                        context.Metadata = new Dictionary<string, object>();
                    }

                    var analogPoints = tcpService.GetAnalogPoints(tcpPoints);
                    var digitalPoints = tcpService.GetDigitalPoints(tcpPoints);

                    context.Metadata["TcpPoints"] = tcpPoints;
                    context.Metadata["TcpAnalogPoints"] = analogPoints;
                    context.Metadata["TcpDigitalPoints"] = digitalPoints;
                    context.Metadata["TcpProcessingEnabled"] = true;

                    _logger.LogSuccess($"✅ TCP通讯处理完成: 总计 {tcpPoints.Count} 个TCP点位 " +
                                     $"(模拟量: {analogPoints.Count}, 数字量: {digitalPoints.Count})");

                    // 验证TCP点位
                    var validation = tcpService.ValidateTcpPoints(tcpPoints);
                    if (!validation.IsValid)
                    {
                        foreach (var error in validation.Errors)
                        {
                            _logger.LogWarning($"TCP验证错误: {error}");
                        }
                    }
                    foreach (var warning in validation.Warnings)
                    {
                        _logger.LogWarning($"TCP验证警告: {warning}");
                    }

                    // --- 步骤 5a: 生成TCP通讯ST代码 ---
                    GenerateTcpCode(serviceContainer, tcpPoints, analogPoints, digitalPoints, context);
                }
                else
                {
                    _logger.LogInfo("📋 未找到TCP通讯表或表为空");
                    if (context.Metadata == null)
                    {
                        context.Metadata = new Dictionary<string, object>();
                    }
                    context.Metadata["TcpProcessingEnabled"] = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ TCP通讯处理失败: {ex.Message}");
                // 确保即使TCP处理失败，也不会影响主流程
                if (context.Metadata == null)
                {
                    context.Metadata = new Dictionary<string, object>();
                }
                context.Metadata["TcpProcessingEnabled"] = false;
                context.Metadata["TcpProcessingError"] = ex.Message;
            }
        }

        /// <summary>
        /// 生成TCP通讯ST代码
        /// </summary>
        private void GenerateTcpCode(ServiceContainer serviceContainer, 
            List<WinFormsApp1.Models.TcpCommunicationPoint> tcpPoints,
            List<WinFormsApp1.Models.TcpAnalogPoint> analogPoints,
            List<WinFormsApp1.Models.TcpDigitalPoint> digitalPoints,
            DataContext context)
        {
            try
            {
                _logger.LogInfo("📝 开始生成TCP通讯ST代码...");

                // 获取TCP代码生成器
                var tcpGenerator = serviceContainer.GetService<WinFormsApp1.Generators.TcpCodeGenerator>();
                if (tcpGenerator == null)
                {
                    _logger.LogWarning("⚠️ TCP代码生成器未注册，跳过TCP代码生成");
                    return;
                }

                var generatedCode = new List<string>();

                // 生成模拟量代码
                if (analogPoints.Any())
                {
                    _logger.LogInfo($"📊 生成 {analogPoints.Count} 个TCP模拟量ST代码...");
                    try
                    {
                        var analogCode = tcpGenerator.GenerateCode(analogPoints);
                        if (!string.IsNullOrWhiteSpace(analogCode))
                        {
                            generatedCode.Add("// TCP模拟量代码");
                            generatedCode.Add(analogCode);
                            generatedCode.Add(""); // 空行分隔
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ TCP模拟量代码生成失败: {ex.Message}");
                    }
                }

                // 生成数字量代码
                if (digitalPoints.Any())
                {
                    _logger.LogInfo($"🔲 生成 {digitalPoints.Count} 个TCP数字量ST代码...");
                    try
                    {
                        var digitalCode = tcpGenerator.GenerateCode(digitalPoints);
                        if (!string.IsNullOrWhiteSpace(digitalCode))
                        {
                            generatedCode.Add("// TCP数字量代码");
                            generatedCode.Add(digitalCode);
                            generatedCode.Add(""); // 空行分隔
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"⚠️ TCP数字量代码生成失败: {ex.Message}");
                    }
                }

                // 如果生成了代码，保存到DataContext.Metadata
                if (generatedCode.Any())
                {
                    var finalCode = string.Join(Environment.NewLine, generatedCode);
                    _logger.LogInfo($"📄 TCP ST代码生成完成，共 {generatedCode.Count(s => !string.IsNullOrWhiteSpace(s))} 行代码");
                    
                    // 为了演示，先输出代码预览
                    var preview = finalCode.Length > 200 ? finalCode.Substring(0, 200) + "..." : finalCode;
                    _logger.LogInfo($"📋 TCP代码预览:\n{preview}");
                    
                    // 将TCP代码保存到DataContext.Metadata中
                    if (context.Metadata == null)
                    {
                        context.Metadata = new Dictionary<string, object>();
                    }
                    context.Metadata["TcpCommunicationPrograms"] = generatedCode.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    _logger.LogInfo($"✅ TCP代码已保存到DataContext.Metadata，共 {((List<string>)context.Metadata["TcpCommunicationPrograms"]).Count} 个程序段");
                }
                else
                {
                    _logger.LogWarning("⚠️ 未生成任何TCP ST代码");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ TCP代码生成失败: {ex.Message}");
            }
        }
    }
}