using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Implementations;
using AutomaticGeneration_ST.Services.Interfaces;

namespace AutomaticGeneration_ST.Tests
{
    /// <summary>
    /// 测试从设备分类表创建完整设备对象的功能
    /// </summary>
    public class DeviceObjectCreationTest
    {
        private readonly ExcelDataService _dataService;
        private readonly string _testDataPath;

        public DeviceObjectCreationTest()
        {
            _dataService = new ExcelDataService();
            _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "参考资料");
        }

        /// <summary>
        /// 测试从真实Excel文件创建设备对象
        /// </summary>
        public TestResult TestDeviceObjectCreationFromExcel()
        {
            var result = new TestResult 
            { 
                TestName = "设备对象创建测试",
                StartTime = DateTime.Now 
            };

            try
            {
                Console.WriteLine("=== 开始设备对象创建测试 ===");

                // 查找测试数据文件
                var testFiles = FindTestExcelFiles();
                if (!testFiles.Any())
                {
                    result.Success = false;
                    result.Message = "未找到测试用的Excel文件";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // 使用第一个找到的Excel文件进行测试
                var testFile = testFiles.First();
                Console.WriteLine($"使用测试文件: {Path.GetFileName(testFile)}");

                // 调用数据服务加载数据
                var dataContext = _dataService.LoadData(testFile);

                // 验证结果
                var validationResult = ValidateDataContext(dataContext);
                
                result.Success = validationResult.IsValid ?? false;
                result.Message = validationResult.Message;
                result.Details = validationResult.Details;
                result.EndTime = DateTime.Now;

                // 输出详细统计信息
                PrintDetailedStatistics(dataContext);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"测试执行失败: {ex.Message}";
                result.EndTime = DateTime.Now;
                Console.WriteLine($"测试异常: {ex}");
                return result;
            }
        }

        /// <summary>
        /// 查找可用的测试Excel文件
        /// </summary>
        private List<string> FindTestExcelFiles()
        {
            var testFiles = new List<string>();
            
            if (!Directory.Exists(_testDataPath))
            {
                Console.WriteLine($"测试数据目录不存在: {_testDataPath}");
                return testFiles;
            }

            // 查找Excel文件
            var excelFiles = Directory.GetFiles(_testDataPath, "*.xlsx", SearchOption.TopDirectoryOnly)
                                    .Concat(Directory.GetFiles(_testDataPath, "*.xls", SearchOption.TopDirectoryOnly))
                                    .ToList();

            Console.WriteLine($"在 {_testDataPath} 中找到 {excelFiles.Count} 个Excel文件:");
            foreach (var file in excelFiles)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)}");
                testFiles.Add(file);
            }

            return testFiles;
        }

        /// <summary>
        /// 验证数据上下文的有效性
        /// </summary>
        private ValidationResult ValidateDataContext(DataContext dataContext)
        {
            var result = new ValidationResult();
            var details = new List<string>();

            try
            {
                // 验证基本数据结构
                if (dataContext == null)
                {
                    result.IsValid = false;
                    result.Message = "DataContext 为 null";
                    return result;
                }

                // 验证点位数据
                if (dataContext.AllPointsMasterList == null)
                {
                    details.Add("❌ AllPointsMasterList 为 null");
                    result.IsValid = false;
                }
                else
                {
                    var pointCount = dataContext.AllPointsMasterList.Count;
                    details.Add($"✅ 点位总数: {pointCount}");
                    
                    if (pointCount > 0)
                    {
                        // 统计各种点位类型
                        var pointsByType = dataContext.AllPointsMasterList.Values
                            .GroupBy(p => p.GetType().Name)
                            .ToDictionary(g => g.Key, g => g.Count());

                        foreach (var kvp in pointsByType)
                        {
                            details.Add($"  - {kvp.Key}: {kvp.Value}个");
                        }
                    }
                }

                // 验证设备数据
                if (dataContext.Devices == null)
                {
                    details.Add("❌ Devices 为 null");
                    result.IsValid = false;
                }
                else
                {
                    var deviceCount = dataContext.Devices.Count;
                    details.Add($"✅ 设备总数: {deviceCount}");

                    if (deviceCount > 0)
                    {
                        // 验证设备详细信息
                        int devicesWithPoints = 0;
                        int totalDevicePoints = 0;
                        var templateUsage = new Dictionary<string, int>();

                        foreach (var device in dataContext.Devices)
                        {
                            if (device.Points?.Any() == true)
                            {
                                devicesWithPoints++;
                                totalDevicePoints += device.Points.Count;
                            }

                            if (!string.IsNullOrEmpty(device.TemplateName))
                            {
                                templateUsage[device.TemplateName] = templateUsage.GetValueOrDefault(device.TemplateName, 0) + 1;
                            }
                        }

                        details.Add($"  - 包含点位的设备: {devicesWithPoints}个");
                        details.Add($"  - 设备关联的点位总数: {totalDevicePoints}个");

                        if (templateUsage.Any())
                        {
                            details.Add("  - 模板使用情况:");
                            foreach (var kvp in templateUsage.OrderByDescending(x => x.Value))
                            {
                                details.Add($"    • {kvp.Key}: {kvp.Value}个设备");
                            }
                        }
                    }
                }

                // 验证独立点位数据
                if (dataContext.StandalonePoints != null)
                {
                    var standaloneCount = dataContext.StandalonePoints.Count;
                    details.Add($"✅ 独立点位总数: {standaloneCount}");
                }

                // 如果没有明确的错误，则认为验证成功
                if (result.IsValid == null) // 未设置为false
                {
                    result.IsValid = true;
                    result.Message = "数据上下文验证通过";
                }

                result.Details = details;
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"验证过程中出现异常: {ex.Message}";
                result.Details = details;
                return result;
            }
        }

        /// <summary>
        /// 输出详细的统计信息
        /// </summary>
        private void PrintDetailedStatistics(DataContext dataContext)
        {
            Console.WriteLine("\n=== 详细统计信息 ===");
            
            if (dataContext.AllPointsMasterList?.Any() == true)
            {
                Console.WriteLine("\n📊 点位统计:");
                var samplePoints = dataContext.AllPointsMasterList.Values.Take(3);
                foreach (var point in samplePoints)
                {
                    Console.WriteLine($"  示例点位: {point.Name} ({point.GetType().Name})");
                    // 根据实际的点位类型显示相关信息
                    Console.WriteLine($"    - 点位类型: {point.PointType ?? "未指定"}");
                }
            }

            if (dataContext.Devices?.Any() == true)
            {
                Console.WriteLine("\n🏭 设备统计:");
                var sampleDevices = dataContext.Devices.Take(3);
                foreach (var device in sampleDevices)
                {
                    Console.WriteLine($"  示例设备: {device.DeviceTag}");
                    Console.WriteLine($"    - 模板: {device.TemplateName ?? "未指定"}");
                    Console.WriteLine($"    - 点位数量: {device.Points?.Count ?? 0}");
                    
                    if (device.Points?.Any() == true)
                    {
                        var pointTypes = device.Points.Values.GroupBy(p => p.GetType().Name)
                                                   .Select(g => $"{g.Key}({g.Count()})")
                                                   .ToList();
                        Console.WriteLine($"    - 点位类型分布: {string.Join(", ", pointTypes)}");
                    }
                }
            }
        }

        /// <summary>
        /// 验证结果类
        /// </summary>
        private class ValidationResult
        {
            public bool? IsValid { get; set; }
            public string Message { get; set; } = "";
            public List<string> Details { get; set; } = new List<string>();
        }
    }

    /// <summary>
    /// 测试结果类
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<string> Details { get; set; } = new List<string>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }
}