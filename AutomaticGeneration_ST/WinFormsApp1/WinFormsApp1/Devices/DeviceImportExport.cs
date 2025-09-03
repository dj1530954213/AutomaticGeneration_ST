////NEED DELETE: 设备导入/导出（旧实现，存在属性缺失编译错误），与新流水线重复
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.Json;
//using System.Threading.Tasks;
//using WinFormsApp1.Templates;
//using WinFormsApp1.Devices.Interfaces;
//using WinFormsApp1.Forms;
//using AutomaticGeneration_ST.Models;

//namespace WinFormsApp1.Devices
//{
//    /// <summary>
//    /// 设备导入导出格式
//    /// </summary>
//    public enum DeviceExportFormat
//    {
//        Json,
//        Xml,
//        Excel,
//        Csv
//    }

//    /// <summary>
//    /// 导入导出选项
//    /// </summary>
//    public class ImportExportOptions
//    {
//        public bool IncludePoints { get; set; } = true;
//        public bool IncludeProperties { get; set; } = true;
//        public bool IncludeMetadata { get; set; } = true;
//        public bool ValidateOnImport { get; set; } = true;
//        public bool OverwriteExisting { get; set; } = false;
//        public DeviceExportFormat Format { get; set; } = DeviceExportFormat.Json;
//        public string? CustomTemplateId { get; set; }
//    }

//    /// <summary>
//    /// 导入导出结果
//    /// </summary>
//    public class ImportExportResult
//    {
//        public bool Success { get; set; }
//        public string Message { get; set; } = "";
//        public List<string> Warnings { get; set; } = new();
//        public List<string> Errors { get; set; } = new();
//        public int ProcessedCount { get; set; }
//        public int SuccessCount { get; set; }
//        public int FailureCount { get; set; }
//        public TimeSpan ElapsedTime { get; set; }
//        public List<CompositeDevice> ImportedDevices { get; set; } = new();
//    }

//    /// <summary>
//    /// 设备批量操作结果
//    /// </summary>
//    public class BatchOperationResult
//    {
//        public bool Success { get; set; }
//        public string Message { get; set; } = "";
//        public List<string> ProcessedItems { get; set; } = new();
//        public List<string> FailedItems { get; set; } = new();
//        public Dictionary<string, string> ErrorMessages { get; set; } = new();
//    }

//    /// <summary>
//    /// 设备导入导出服务
//    /// </summary>
//    public static class DeviceImportExport
//    {
//        /// <summary>
//        /// 导出单个设备
//        /// </summary>
//        public static async Task<ImportExportResult> ExportDeviceAsync(
//            CompositeDevice device, 
//            string filePath, 
//            ImportExportOptions? options = null)
//        {
//            var result = new ImportExportResult();
//            var startTime = DateTime.Now;

//            try
//            {
//                var opts = options ?? new ImportExportOptions();

//                switch (opts.Format)
//                {
//                    case DeviceExportFormat.Json:
//                        await ExportToJsonAsync(new[] { device }, filePath, opts);
//                        break;
//                    case DeviceExportFormat.Xml:
//                        await ExportToXmlAsync(new[] { device }, filePath, opts);
//                        break;
//                    case DeviceExportFormat.Excel:
//                        await ExportToExcelAsync(new[] { device }, filePath, opts);
//                        break;
//                    case DeviceExportFormat.Csv:
//                        await ExportToCsvAsync(new[] { device }, filePath, opts);
//                        break;
//                    default:
//                        throw new NotSupportedException($"不支持的导出格式: {opts.Format}");
//                }

//                result.Success = true;
//                result.Message = "设备导出成功";
//                result.SuccessCount = 1;
//                result.ProcessedCount = 1;
//            }
//            catch (Exception ex)
//            {
//                result.Success = false;
//                result.Message = $"导出设备失败: {ex.Message}";
//                result.Errors.Add(ex.Message);
//                result.FailureCount = 1;
//            }
//            finally
//            {
//                result.ElapsedTime = DateTime.Now - startTime;
//            }

//            return result;
//        }

//        /// <summary>
//        /// 导出多个设备
//        /// </summary>
//        public static async Task<ImportExportResult> ExportDevicesAsync(
//            IEnumerable<CompositeDevice> devices, 
//            string filePath, 
//            ImportExportOptions? options = null)
//        {
//            var result = new ImportExportResult();
//            var startTime = DateTime.Now;
//            var deviceList = devices.ToList();

//            try
//            {
//                var opts = options ?? new ImportExportOptions();

//                switch (opts.Format)
//                {
//                    case DeviceExportFormat.Json:
//                        await ExportToJsonAsync(deviceList, filePath, opts);
//                        break;
//                    case DeviceExportFormat.Xml:
//                        await ExportToXmlAsync(deviceList, filePath, opts);
//                        break;
//                    case DeviceExportFormat.Excel:
//                        await ExportToExcelAsync(deviceList, filePath, opts);
//                        break;
//                    case DeviceExportFormat.Csv:
//                        await ExportToCsvAsync(deviceList, filePath, opts);
//                        break;
//                    default:
//                        throw new NotSupportedException($"不支持的导出格式: {opts.Format}");
//                }

//                result.Success = true;
//                result.Message = "设备导出成功";
//                result.SuccessCount = deviceList.Count;
//                result.ProcessedCount = deviceList.Count;
//            }
//            catch (Exception ex)
//            {
//                result.Success = false;
//                result.Message = $"导出设备失败: {ex.Message}";
//                result.Errors.Add(ex.Message);
//                result.FailureCount = deviceList.Count;
//            }
//            finally
//            {
//                result.ElapsedTime = DateTime.Now - startTime;
//            }

//            return result;
//        }

//        /// <summary>
//        /// 导入设备
//        /// </summary>
//        public static async Task<ImportExportResult> ImportDevicesAsync(
//            string filePath, 
//            ImportExportOptions? options = null)
//        {
//            var result = new ImportExportResult();
//            var startTime = DateTime.Now;

//            try
//            {
//                var opts = options ?? new ImportExportOptions();
//                var extension = Path.GetExtension(filePath).ToLower();

//                List<CompositeDevice> devices = extension switch
//                {
//                    ".json" => await ImportFromJsonAsync(filePath, opts),
//                    ".xml" => await ImportFromXmlAsync(filePath, opts),
//                    ".xlsx" or ".xls" => await ImportFromExcelAsync(filePath, opts),
//                    ".csv" => await ImportFromCsvAsync(filePath, opts),
//                    _ => throw new NotSupportedException($"不支持的导入格式: {extension}")
//                };

//                // 验证设备
//                var validDevices = new List<CompositeDevice>();
//                foreach (var device in devices)
//                {
//                    result.ProcessedCount++;

//                    if (opts.ValidateOnImport)
//                    {
//                        var validationErrors = DeviceManager.ValidateDevice(device);
//                        if (validationErrors.Any())
//                        {
//                            result.FailureCount++;
//                            result.Errors.AddRange(validationErrors.Select(e => $"设备 '{device.DeviceName}': {e}"));
//                            continue;
//                        }
//                    }

//                    // 检查重复设备
//                    var existingDevice = DeviceManager.GetAllDevices()
//                        .FirstOrDefault(d => d.Name.Equals(device.DeviceName, StringComparison.OrdinalIgnoreCase));

//                    if (existingDevice != null && !opts.OverwriteExisting)
//                    {
//                        result.Warnings.Add($"设备 '{device.DeviceName}' 已存在，跳过导入");
//                        continue;
//                    }

//                    validDevices.Add(device);
//                    result.SuccessCount++;
//                }

//                // 导入有效设备
//                foreach (var device in validDevices)
//                {
//                    if (opts.OverwriteExisting)
//                    {
//                        DeviceManager.UpdateDevice(device);
//                    }
//                    else
//                    {
//                        DeviceManager.CreateDevice(device.DeviceName, ConvertToDeviceType(device.Type));
//                    }
//                }

//                result.ImportedDevices = validDevices;
//                result.Success = result.SuccessCount > 0;
//                result.Message = $"成功导入 {result.SuccessCount} 个设备";

//                if (result.FailureCount > 0)
//                {
//                    result.Message += $"，失败 {result.FailureCount} 个";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Success = false;
//                result.Message = $"导入设备失败: {ex.Message}";
//                result.Errors.Add(ex.Message);
//            }
//            finally
//            {
//                result.ElapsedTime = DateTime.Now - startTime;
//            }

//            return result;
//        }

//        #region 私有导出方法

//        private static async Task ExportToJsonAsync(
//            IEnumerable<CompositeDevice> devices, 
//            string filePath, 
//            ImportExportOptions options)
//        {
//            var exportData = PrepareExportData(devices, options);
            
//            var jsonOptions = new JsonSerializerOptions
//            {
//                WriteIndented = true,
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
//            };

//            var json = JsonSerializer.Serialize(exportData, jsonOptions);
//            await File.WriteAllTextAsync(filePath, json);
//        }

//        private static async Task ExportToXmlAsync(
//            IEnumerable<CompositeDevice> devices, 
//            string filePath, 
//            ImportExportOptions options)
//        {
//            var xml = new System.Text.StringBuilder();
//            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
//            xml.AppendLine("<Devices>");

//            foreach (var device in devices)
//            {
//                xml.AppendLine("  <Device>");
//                xml.AppendLine($"    <Name>{EscapeXml(device.DeviceName)}</Name>");
//                xml.AppendLine($"    <Type>{device.Type}</Type>");
//                xml.AppendLine($"    <Description>{EscapeXml(device.Description)}</Description>");
//                xml.AppendLine($"    <Manufacturer>{EscapeXml(device.Manufacturer)}</Manufacturer>");
//                xml.AppendLine($"    <Model>{EscapeXml(device.Model)}</Model>");
//                xml.AppendLine($"    <CreatedTime>{device.CreatedTime:yyyy-MM-dd HH:mm:ss}</CreatedTime>");

//                if (options.IncludePoints && device.Points.Any())
//                {
//                    xml.AppendLine("    <Points>");
//                    foreach (var point in device.Points)
//                    {
//                        xml.AppendLine("      <Point>");
//                        xml.AppendLine($"        <Name>{EscapeXml(point.Name)}</Name>");
//                        xml.AppendLine($"        <Type>{point.Type}</Type>");
//                        xml.AppendLine($"        <Address>{EscapeXml(point.Address)}</Address>");
//                        xml.AppendLine($"        <Description>{EscapeXml(point.Description)}</Description>");
//                        xml.AppendLine($"        <Unit>{EscapeXml(point.Unit)}</Unit>");
//                        xml.AppendLine($"        <MinValue>{point.MinValue}</MinValue>");
//                        xml.AppendLine($"        <MaxValue>{point.MaxValue}</MaxValue>");
//                        xml.AppendLine($"        <IsAlarmEnabled>{point.IsAlarmEnabled}</IsAlarmEnabled>");
//                        xml.AppendLine("      </Point>");
//                    }
//                    xml.AppendLine("    </Points>");
//                }

//                xml.AppendLine("  </Device>");
//            }

//            xml.AppendLine("</Devices>");
//            await File.WriteAllTextAsync(filePath, xml.ToString());
//        }

//        private static async Task ExportToExcelAsync(
//            IEnumerable<CompositeDevice> devices, 
//            string filePath, 
//            ImportExportOptions options)
//        {
//            // 简化的Excel导出实现
//            var csv = new System.Text.StringBuilder();
            
//            // 表头
//            csv.AppendLine("设备名称,设备类型,制造商,型号,描述,点位数量,创建时间");

//            foreach (var device in devices)
//            {
//                csv.AppendLine($"\"{device.DeviceName}\",\"{device.Type}\",\"{device.Manufacturer}\"," +
//                              $"\"{device.Model}\",\"{device.Description}\",{device.Points.Count}," +
//                              $"\"{device.CreatedTime:yyyy-MM-dd HH:mm:ss}\"");
//            }

//            await File.WriteAllTextAsync(filePath, csv.ToString());
//        }

//        private static async Task ExportToCsvAsync(
//            IEnumerable<CompositeDevice> devices, 
//            string filePath, 
//            ImportExportOptions options)
//        {
//            var csv = new System.Text.StringBuilder();
            
//            if (options.IncludePoints)
//            {
//                // 包含点位信息的详细导出
//                csv.AppendLine("设备名称,设备类型,制造商,型号,点位名称,点位类型,点位地址,点位描述,单位,最小值,最大值,报警启用");

//                foreach (var device in devices)
//                {
//                    if (device.Points.Any())
//                    {
//                        foreach (var point in device.Points)
//                        {
//                            csv.AppendLine($"\"{device.DeviceName}\",\"{device.Type}\",\"{device.Manufacturer}\"," +
//                                          $"\"{device.Model}\",\"{point.Name}\",\"{point.Type}\"," +
//                                          $"\"{point.Address}\",\"{point.Description}\",\"{point.Unit}\"," +
//                                          $"{point.MinValue},{point.MaxValue},{point.IsAlarmEnabled}");
//                        }
//                    }
//                    else
//                    {
//                        csv.AppendLine($"\"{device.DeviceName}\",\"{device.Type}\",\"{device.Manufacturer}\"," +
//                                      $"\"{device.Model}\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\"");
//                    }
//                }
//            }
//            else
//            {
//                // 仅设备基本信息
//                csv.AppendLine("设备名称,设备类型,制造商,型号,描述,点位数量,创建时间");

//                foreach (var device in devices)
//                {
//                    csv.AppendLine($"\"{device.DeviceName}\",\"{device.Type}\",\"{device.Manufacturer}\"," +
//                                  $"\"{device.Model}\",\"{device.Description}\",{device.Points.Count}," +
//                                  $"\"{device.CreatedTime:yyyy-MM-dd HH:mm:ss}\"");
//                }
//            }

//            await File.WriteAllTextAsync(filePath, csv.ToString());
//        }

//        #endregion

//        #region 私有导入方法

//        private static async Task<List<CompositeDevice>> ImportFromJsonAsync(
//            string filePath, 
//            ImportExportOptions options)
//        {
//            var json = await File.ReadAllTextAsync(filePath);
//            var jsonOptions = new JsonSerializerOptions
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                PropertyNameCaseInsensitive = true
//            };

//            // 尝试解析为设备数组或单个设备
//            try
//            {
//                var devices = JsonSerializer.Deserialize<List<CompositeDevice>>(json, jsonOptions);
//                return devices ?? new List<CompositeDevice>();
//            }
//            catch
//            {
//                // 尝试解析为单个设备
//                var device = JsonSerializer.Deserialize<CompositeDevice>(json, jsonOptions);
//                return device != null ? new List<CompositeDevice> { device } : new List<CompositeDevice>();
//            }
//        }

//        private static async Task<List<CompositeDevice>> ImportFromXmlAsync(
//            string filePath, 
//            ImportExportOptions options)
//        {
//            // 简化的XML导入实现
//            var devices = new List<CompositeDevice>();
            
//            // 这里应该实现真正的XML解析逻辑
//            // 为了示例，返回空列表
//            await Task.Delay(1);
            
//            return devices;
//        }

//        private static async Task<List<CompositeDevice>> ImportFromExcelAsync(
//            string filePath, 
//            ImportExportOptions options)
//        {
//            // 简化的Excel导入实现
//            var devices = new List<CompositeDevice>();
            
//            // 这里应该实现真正的Excel解析逻辑
//            // 为了示例，返回空列表
//            await Task.Delay(1);
            
//            return devices;
//        }

//        private static async Task<List<CompositeDevice>> ImportFromCsvAsync(
//            string filePath, 
//            ImportExportOptions options)
//        {
//            var devices = new List<CompositeDevice>();
//            var lines = await File.ReadAllLinesAsync(filePath);

//            if (lines.Length < 2) return devices; // 至少需要表头和一行数据

//            var header = lines[0].Split(',');
//            var deviceMap = new Dictionary<string, CompositeDevice>();

//            for (int i = 1; i < lines.Length; i++)
//            {
//                var values = ParseCsvLine(lines[i]);
//                if (values.Length < header.Length) continue;

//                var deviceName = values[0].Trim('"');
//                if (string.IsNullOrEmpty(deviceName)) continue;

//                // 获取或创建设备
//                if (!deviceMap.TryGetValue(deviceName, out var device))
//                {
//                    var deviceType = Enum.TryParse<DeviceType>(values[1].Trim('"'), out var parsedType) ? parsedType : DeviceType.Custom;
//                    var compositeType = ConvertToCompositeDeviceType(deviceType);
                    
//                    device = new BasicCompositeDevice(Guid.NewGuid().ToString(), deviceName, compositeType)
//                    {
//                        Manufacturer = values[2].Trim('"'),
//                        Model = values[3].Trim('\"')
//                    };

//                    deviceMap[deviceName] = device;
//                    devices.Add(device);
//                }

//                // 添加点位信息（如果存在）
//                if (values.Length > 4 && !string.IsNullOrEmpty(values[4].Trim('"')))
//                {
//                    var pointName = values[4].Trim('"');
//                    var point = new AutomaticGeneration_ST.Models.Point(pointName)
//                    {
//                        Description = values.Length > 7 ? values[7].Trim('"') : "",
//                        Unit = values.Length > 8 ? values[8].Trim('"') : "",
//                        PlcAbsoluteAddress = values.Length > 6 ? values[6].Trim('"') : "",
//                        PointType = Enum.TryParse<PointType>(values[5].Trim('"'), out var pointType) ? pointType.ToString() : "DI"
//                    };

//                    if (values.Length > 9 && double.TryParse(values[9], out var minValue))
//                        point.RangeLow = minValue;

//                    if (values.Length > 10 && double.TryParse(values[10], out var maxValue))
//                        point.RangeHigh = maxValue;

//                    if (values.Length > 11 && bool.TryParse(values[11], out var isAlarmEnabled))
//                        point.IsAlarmEnabled = isAlarmEnabled;

//                    device.AddPoint(point);
//                }
//            }

//            return devices;
//        }

//        #endregion

//        #region 辅助方法

//        private static object PrepareExportData(IEnumerable<CompositeDevice> devices, ImportExportOptions options)
//        {
//            var exportDevices = devices.Select(device => new
//            {
//                device.Id,
//                device.DeviceName,
//                device.Type,
//                device.Description,
//                device.Manufacturer,
//                device.Model,
//                device.CreatedTime,
//                device.ModifiedTime,
//                device.Author,
//                device.Version,
//                device.IsActive,
//                Points = options.IncludePoints ? device.Points.ToList() : new List<AutomaticGeneration_ST.Models.Point>(),
//                Properties = options.IncludeProperties ? device.Properties : new Dictionary<string, object>()
//            });

//            return new
//            {
//                ExportTime = DateTime.Now,
//                ExportedBy = Environment.UserName,
//                DeviceCount = devices.Count(),
//                Devices = exportDevices
//            };
//        }

//        private static string EscapeXml(string text)
//        {
//            if (string.IsNullOrEmpty(text)) return "";
            
//            return text.Replace("&", "&amp;")
//                      .Replace("<", "&lt;")
//                      .Replace(">", "&gt;")
//                      .Replace("\"", "&quot;")
//                      .Replace("'", "&apos;");
//        }

//        private static string[] ParseCsvLine(string line)
//        {
//            var result = new List<string>();
//            var current = new System.Text.StringBuilder();
//            bool inQuotes = false;

//            for (int i = 0; i < line.Length; i++)
//            {
//                char c = line[i];

//                if (c == '"')
//                {
//                    inQuotes = !inQuotes;
//                }
//                else if (c == ',' && !inQuotes)
//                {
//                    result.Add(current.ToString());
//                    current.Clear();
//                }
//                else
//                {
//                    current.Append(c);
//                }
//            }

//            result.Add(current.ToString());
//            return result.ToArray();
//        }

//        #endregion

//        /// <summary>
//        /// 批量应用模板到设备
//        /// </summary>
//        public static async Task<BatchOperationResult> ApplyTemplateToDevicesAsync(
//            IEnumerable<CompositeDevice> devices, 
//            DeviceTemplate template)
//        {
//            var result = new BatchOperationResult();

//            foreach (var device in devices)
//            {
//                try
//                {
//                    if (device.Type.ToString() != template.Type.ToString())
//                    {
//                        result.FailedItems.Add(device.DeviceName);
//                        result.ErrorMessages[device.DeviceName] = $"设备类型不匹配：设备类型为 {device.Type}，模板类型为 {template.Type}";
//                        continue;
//                    }

//                    // 清空现有点位
//                    device.ClearPoints();

//                    // 应用模板点位
//                    foreach (var templatePoint in template.DefaultPoints)
//                    {
//                        var newPoint = new AutomaticGeneration_ST.Models.Point(templatePoint.Name)
//                        {
//                            Description = templatePoint.Description,
//                            Unit = templatePoint.Unit,
//                            RangeLow = templatePoint.MinValue,
//                            RangeHigh = templatePoint.MaxValue,
//                            IsAlarmEnabled = templatePoint.IsAlarmEnabled,
//                            AlarmHigh = templatePoint.AlarmHigh,
//                            AlarmLow = templatePoint.AlarmLow
//                        };
                        
//                        device.AddPoint(newPoint);
//                    }

//                    // 应用模板属性
//                    foreach (var prop in template.DefaultProperties)
//                    {
//                        device.SetParameter(prop.Key, prop.Value);
//                    }

//                    // 更新设备
//                    DeviceManager.UpdateDevice(device);
//                    result.ProcessedItems.Add(device.DeviceName);
//                }
//                catch (Exception ex)
//                {
//                    result.FailedItems.Add(device.DeviceName);
//                    result.ErrorMessages[device.DeviceName] = ex.Message;
//                }
//            }

//            result.Success = result.FailedItems.Count == 0;
//            result.Message = $"处理完成：成功 {result.ProcessedItems.Count} 个，失败 {result.FailedItems.Count} 个";

//            return result;
//        }

//        /// <summary>
//        /// 批量验证设备配置
//        /// </summary>
//        public static BatchOperationResult ValidateDevices(IEnumerable<CompositeDevice> devices)
//        {
//            var result = new BatchOperationResult();

//            foreach (var device in devices)
//            {
//                var validationErrors = DeviceManager.ValidateDevice(device);
//                if (validationErrors.Any())
//                {
//                    result.FailedItems.Add(device.DeviceName);
//                    result.ErrorMessages[device.DeviceName] = string.Join("; ", validationErrors);
//                }
//                else
//                {
//                    result.ProcessedItems.Add(device.DeviceName);
//                }
//            }

//            result.Success = result.FailedItems.Count == 0;
//            result.Message = $"验证完成：通过 {result.ProcessedItems.Count} 个，失败 {result.FailedItems.Count} 个";

//            return result;
//        }
        
//        /// <summary>
//        /// 将DeviceType转换为CompositeDeviceType
//        /// </summary>
//        private static CompositeDeviceType ConvertToCompositeDeviceType(DeviceType deviceType)
//        {
//            return deviceType switch
//            {
//                DeviceType.Valve => CompositeDeviceType.ValveController,
//                DeviceType.Pump => CompositeDeviceType.PumpController,
//                DeviceType.Controller => CompositeDeviceType.VFDController,
//                _ => CompositeDeviceType.ValveController // 默认值
//            };
//        }

//        /// <summary>
//        /// 将CompositeDeviceType转换为DeviceType
//        /// </summary>
//        private static DeviceType ConvertToDeviceType(CompositeDeviceType compositeType)
//        {
//            return compositeType switch
//            {
//                CompositeDeviceType.ValveController => DeviceType.Valve,
//                CompositeDeviceType.PumpController => DeviceType.Pump,
//                CompositeDeviceType.VFDController => DeviceType.Controller,
//                CompositeDeviceType.TankController => DeviceType.Tank,
//                _ => DeviceType.Custom
//            };
//        }
//    }
//}
