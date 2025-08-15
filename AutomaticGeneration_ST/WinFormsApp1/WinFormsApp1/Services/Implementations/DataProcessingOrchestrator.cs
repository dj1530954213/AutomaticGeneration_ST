using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinFormsApp1.Excel;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 数据处理编排器实现类 - 协调整个数据处理流程
    /// </summary>
    public class DataProcessingOrchestrator : IDataProcessingOrchestrator
    {
        private readonly IExcelWorkbookParser _excelParser;
        private readonly IPointFactory _pointFactory;
        private readonly IDeviceClassificationService _deviceClassificationService;
        private readonly ITemplateRegistry _templateRegistry;
        private readonly IWorksheetLocatorService _worksheetLocator;

        public DataProcessingOrchestrator(
            IExcelWorkbookParser excelParser,
            IPointFactory pointFactory,
            IDeviceClassificationService deviceClassificationService,
            ITemplateRegistry templateRegistry,
            IWorksheetLocatorService worksheetLocator)
        {
            _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));
            _pointFactory = pointFactory ?? throw new ArgumentNullException(nameof(pointFactory));
            _deviceClassificationService = deviceClassificationService ?? throw new ArgumentNullException(nameof(deviceClassificationService));
            _templateRegistry = templateRegistry ?? throw new ArgumentNullException(nameof(templateRegistry));
            _worksheetLocator = worksheetLocator ?? throw new ArgumentNullException(nameof(worksheetLocator));
        }

        public ProcessingResult ProcessData(string excelFilePath)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath))
                throw new ArgumentException("Excel文件路径不能为空", nameof(excelFilePath));

            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException($"Excel文件不存在: {excelFilePath}");

            var result = new ProcessingResult();
            var startTime = DateTime.Now;

            try
            {
                Console.WriteLine($"[INFO] 开始处理Excel文件: {Path.GetFileName(excelFilePath)}");

                // 步骤1: 解析IO点表，构建点位数据的基础
                var allPoints = ProcessIoPointsTable(excelFilePath, result.Statistics);
                result.AllPointsMaster = allPoints;

                // 步骤2: 解析设备分类表，构建设备对象并关联点位
                var (devices, assignedPointNames) = ProcessDeviceClassification(excelFilePath, allPoints, result.Statistics);
                result.Devices = devices;

                // 步骤3: 处理其他设备表，补充遗漏的点位
                ProcessOtherDeviceTables(excelFilePath, allPoints, result.Statistics);

                // 步骤4: 分类点位
                ClassifyPoints(allPoints, assignedPointNames, result);

                // 步骤5: 验证模板配置
                ValidateTemplateConfiguration(devices, result.Statistics);

                // 更新最终统计信息
                UpdateFinalStatistics(result);

                var duration = DateTime.Now - startTime;
                Console.WriteLine($"[INFO] 数据处理完成，耗时: {duration.TotalSeconds:F2}秒");

                return result;
            }
            catch (Exception ex)
            {
                result.Statistics.ErrorsEncountered++;
                Console.WriteLine($"[ERROR] 数据处理失败: {ex.Message}");
                throw new InvalidOperationException($"数据处理失败: {ex.Message}", ex);
            }
        }

        private Dictionary<string, Models.Point> ProcessIoPointsTable(string excelFilePath, ProcessingStatistics statistics)
        {
            Console.WriteLine("[INFO] 步骤1: 处理IO点表...");

            // 使用智能工作表定位服务
            var validation = _worksheetLocator.ValidateWorksheet(excelFilePath, "IO点表");
            if (!validation.IsFound)
            {
                throw new InvalidOperationException(
                    $"在Excel文件中未找到'IO点表'工作表。\n" +
                    $"错误信息: {validation.ErrorMessage}\n" +
                    $"建议: 请检查工作表名称是否正确或尝试使用以下别名: IO, IO表, Points, 点位表, 点表");
            }

            Console.WriteLine($"[INFO] 找到IO点表: '{validation.ActualName}' (匹配类型: {validation.MatchType})");
            
            var ioPointsData = _excelParser.ParseWorksheetSmart(excelFilePath, "IO点表", _worksheetLocator);
            Console.WriteLine($"[INFO] IO点表包含 {ioPointsData.Count} 行数据");

            var points = _pointFactory.CreatePointsBatch(ioPointsData);
            statistics.TotalPointsProcessed = points.Count;

            Console.WriteLine($"[INFO] 成功创建 {points.Count} 个点位对象");
            return points;
        }

        private (List<Device>, HashSet<string>) ProcessDeviceClassification(
            string excelFilePath, 
            Dictionary<string, Models.Point> allPoints, 
            ProcessingStatistics statistics)
        {
            Console.WriteLine("[INFO] 步骤2: 处理设备分类表...");

            // 使用智能工作表定位服务
            var validation = _worksheetLocator.ValidateWorksheet(excelFilePath, "设备分类表");
            if (!validation.IsFound)
            {
                Console.WriteLine($"[WARNING] 未找到设备分类表，所有点位将作为独立点位处理");
                Console.WriteLine($"[INFO] 错误信息: {validation.ErrorMessage}");
                Console.WriteLine($"[INFO] 建议使用以下别名: 设备分类, 分类表, Device Classification, Devices, Device, 设备表, 设备");
                return (new List<Device>(), new HashSet<string>());
            }

            Console.WriteLine($"[INFO] 找到设备分类表: '{validation.ActualName}' (匹配类型: {validation.MatchType})");
            
            var classificationData = _excelParser.ParseWorksheetSmart(excelFilePath, "设备分类表", _worksheetLocator);
            Console.WriteLine($"[INFO] 设备分类表包含 {classificationData.Count} 行数据");

            var (devices, assignedPointNames) = _deviceClassificationService.BuildDevicesFromClassification(
                classificationData, allPoints);

            statistics.DevicesCreated = devices.Count;
            Console.WriteLine($"[INFO] 成功创建 {devices.Count} 个设备对象");

            return (devices, assignedPointNames);
        }

        private void ProcessOtherDeviceTables(string excelFilePath, Dictionary<string, Models.Point> allPoints, ProcessingStatistics statistics)
        {
            Console.WriteLine("[INFO] 步骤3: 处理其他设备表...");

            var deviceTableNames = new List<string> 
            { 
                "阀门", "调节阀", "可燃气体探测器", "低压开关柜", 
                "撬装机柜", "加臭", "恒电位仪" 
            };

            int tablesProcessed = 0;
            int pointsEnhanced = 0;

            foreach (var tableName in deviceTableNames)
            {
                // 使用智能工作表查找
                var validation = _worksheetLocator.ValidateWorksheet(excelFilePath, tableName);
                if (validation.IsFound)
                {
                    try
                    {
                        Console.WriteLine($"[INFO] 找到设备表: '{validation.ActualName}' (匹配类型: {validation.MatchType})");
                        var tableData = _excelParser.ParseWorksheetSmart(excelFilePath, tableName, _worksheetLocator);
                        tablesProcessed++;

                        foreach (var row in tableData)
                        {
                            var hmiTagName = DataExtractorHelper.GetValue<string>(row, "变量名称（HMI）");
                            if (!string.IsNullOrWhiteSpace(hmiTagName))
                            {
                                if (allPoints.TryGetValue(hmiTagName, out var existingPoint))
                                {
                                    // 增强现有点位
                                    _pointFactory.CreateFromDevicePoint(row, existingPoint);
                                    pointsEnhanced++;
                                }
                                else
                                {
                                    // 创建新点位
                                    try
                                    {
                                        var newPoint = _pointFactory.CreateFromDevicePoint(row);
                                        allPoints.Add(hmiTagName, newPoint);
                                        statistics.TotalPointsProcessed++;
                                    }
                                    catch (Exception ex)
                                    {
                                        statistics.Warnings.Add($"从表'{tableName}'创建点位'{hmiTagName}'失败: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        statistics.Warnings.Add($"处理设备表'{tableName}'时出错: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"[INFO] 处理了 {tablesProcessed} 个设备表，增强了 {pointsEnhanced} 个点位");
        }

        private void ClassifyPoints(Dictionary<string, Models.Point> allPoints, HashSet<string> assignedPointNames, ProcessingResult result)
        {
            Console.WriteLine("[INFO] 步骤4: 分类点位...");

            foreach (var point in allPoints.Values)
            {
                if (assignedPointNames.Contains(point.HmiTagName))
                {
                    // 已分配给设备的点位不计入独立点位
                    continue;
                }

                result.StandalonePoints.Add(point);

                // 根据点位类型进行分类
                switch (point.PointType?.ToLower())
                {
                    case "硬点":
                        result.HardwarePoints.Add(point);
                        break;
                    case "软点":
                        result.SoftwarePoints.Add(point);
                        break;
                    case "通讯点":
                        result.CommunicationPoints.Add(point);
                        break;
                    default:
                        // 如果没有明确的点位类型，根据模块类型推断
                        if (!string.IsNullOrWhiteSpace(point.ModuleType))
                        {
                            var moduleType = point.ModuleType.ToUpper();
                            if (moduleType == "AI" || moduleType == "AO" || moduleType == "DI" || moduleType == "DO")
                            {
                                result.HardwarePoints.Add(point);
                                point.PointType = "硬点"; // 更新点位类型
                            }
                        }
                        break;
                }
            }

            Console.WriteLine($"[INFO] 点位分类完成:");
            Console.WriteLine($"  - 独立点位: {result.StandalonePoints.Count}");
            Console.WriteLine($"  - 硬点: {result.HardwarePoints.Count}");
            Console.WriteLine($"  - 软点: {result.SoftwarePoints.Count}");
            Console.WriteLine($"  - 通讯点: {result.CommunicationPoints.Count}");
        }

        private void ValidateTemplateConfiguration(List<Device> devices, ProcessingStatistics statistics)
        {
            Console.WriteLine("[INFO] 步骤5: 验证模板配置...");

            var missingTemplates = new HashSet<string>();

            foreach (var device in devices)
            {
                if (string.IsNullOrWhiteSpace(device.TemplateName))
                {
                    statistics.Warnings.Add($"设备 '{device.DeviceTag}' 没有指定模板名称");
                    continue;
                }

                if (!_templateRegistry.HasTemplate(device.TemplateName))
                {
                    missingTemplates.Add(device.TemplateName);
                }
            }

            if (missingTemplates.Count > 0)
            {
                var missingList = string.Join(", ", missingTemplates);
                statistics.Warnings.Add($"以下模板未在注册表中找到: {missingList}");
                Console.WriteLine($"[WARNING] 发现 {missingTemplates.Count} 个未注册的模板");
            }
            else
            {
                Console.WriteLine("[INFO] 所有设备模板配置验证通过");
            }
        }

        private void UpdateFinalStatistics(ProcessingResult result)
        {
            result.Statistics.StandalonePoints = result.StandalonePoints.Count;
            result.Statistics.HardwarePoints = result.HardwarePoints.Count;
            result.Statistics.SoftwarePoints = result.SoftwarePoints.Count;
            result.Statistics.CommunicationPoints = result.CommunicationPoints.Count;
        }

        // 已重构：GetValue<T>方法已移至DataExtractorHelper工具类，消除DUP-007重复代码
    }
}