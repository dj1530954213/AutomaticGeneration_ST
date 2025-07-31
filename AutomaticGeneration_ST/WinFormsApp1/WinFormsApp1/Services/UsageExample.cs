using AutomaticGeneration_ST.Services;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// 新架构使用示例
    /// </summary>
    public class UsageExample
    {
        /// <summary>
        /// 基本使用示例 - 兼容现有代码
        /// </summary>
        public static void BasicUsageExample()
        {
            try
            {
                // 配置路径
                var templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                var configPath = Path.Combine(templateDirectory, "template-mapping.json");
                var excelFilePath = @"C:\Path\To\Your\ExcelFile.xlsx";

                // 创建服务容器
                var serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);

                // 获取数据服务（兼容现有接口）
                var dataService = serviceContainer.GetService<IDataService>();

                // 加载数据
                Console.WriteLine("开始加载Excel数据...");
                var dataContext = dataService.LoadData(excelFilePath);

                // 输出结果
                Console.WriteLine($"加载完成!");
                Console.WriteLine($"  设备数量: {dataContext.Devices.Count}");
                Console.WriteLine($"  独立点位数量: {dataContext.StandalonePoints.Count}");
                Console.WriteLine($"  总点位数量: {dataContext.AllPointsMasterList.Count}");

                // 显示设备信息
                Console.WriteLine("\n设备列表:");
                foreach (var device in dataContext.Devices.Take(5)) // 只显示前5个
                {
                    Console.WriteLine($"  - {device.DeviceTag} (模板: {device.TemplateName}, 点位: {device.Points.Count})");
                }

                if (dataContext.Devices.Count > 5)
                {
                    Console.WriteLine($"  ... 还有 {dataContext.Devices.Count - 5} 个设备");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 高级使用示例 - 直接使用编排器
        /// </summary>
        public static void AdvancedUsageExample()
        {
            try
            {
                // 配置路径
                var templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                var configPath = Path.Combine(templateDirectory, "template-mapping.json");
                var excelFilePath = @"C:\Path\To\Your\ExcelFile.xlsx";

                // 创建服务容器
                var serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);

                // 获取编排器
                var orchestrator = serviceContainer.GetService<IDataProcessingOrchestrator>();

                // 处理数据
                Console.WriteLine("开始处理数据...");
                var result = orchestrator.ProcessData(excelFilePath);

                // 输出详细统计信息
                Console.WriteLine("\n=== 处理结果统计 ===");
                Console.WriteLine($"总点位数量: {result.Statistics.TotalPointsProcessed}");
                Console.WriteLine($"设备数量: {result.Statistics.DevicesCreated}");
                Console.WriteLine($"独立点位: {result.Statistics.StandalonePoints}");
                Console.WriteLine($"硬点数量: {result.Statistics.HardwarePoints}");
                Console.WriteLine($"软点数量: {result.Statistics.SoftwarePoints}");
                Console.WriteLine($"通讯点数量: {result.Statistics.CommunicationPoints}");
                Console.WriteLine($"错误数量: {result.Statistics.ErrorsEncountered}");
                Console.WriteLine($"警告数量: {result.Statistics.Warnings.Count}");

                // 显示警告信息
                if (result.Statistics.Warnings.Any())
                {
                    Console.WriteLine("\n=== 警告信息 ===");
                    foreach (var warning in result.Statistics.Warnings.Take(10))
                    {
                        Console.WriteLine($"  - {warning}");
                    }
                    if (result.Statistics.Warnings.Count > 10)
                    {
                        Console.WriteLine($"  ... 还有 {result.Statistics.Warnings.Count - 10} 个警告");
                    }
                }

                // 显示设备详细信息
                Console.WriteLine("\n=== 设备详细信息 ===");
                var deviceGroups = result.Devices.GroupBy(d => d.TemplateName);
                foreach (var group in deviceGroups)
                {
                    Console.WriteLine($"模板 '{group.Key}': {group.Count()} 个设备");
                    foreach (var device in group.Take(3))
                    {
                        var hardwarePoints = device.Points.Values.Count(p => p.PointType == "硬点");
                        var softwarePoints = device.Points.Values.Count(p => p.PointType == "软点");
                        Console.WriteLine($"  - {device.DeviceTag}: 总点位 {device.Points.Count} (硬点: {hardwarePoints}, 软点: {softwarePoints})");
                    }
                    if (group.Count() > 3)
                    {
                        Console.WriteLine($"    ... 还有 {group.Count() - 3} 个设备");
                    }
                }

                // 显示独立硬点信息（用于IO映射）
                Console.WriteLine($"\n=== 独立硬点分析 (用于IO映射) ===");
                var standaloneHardwarePoints = result.HardwarePoints
                    .Where(p => result.StandalonePoints.Contains(p))
                    .GroupBy(p => p.ModuleType)
                    .OrderBy(g => g.Key);

                foreach (var group in standaloneHardwarePoints)
                {
                    Console.WriteLine($"{group.Key}: {group.Count()} 个点位");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// 服务容器使用示例
        /// </summary>
        public static void ServiceContainerExample()
        {
            try
            {
                var templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                var configPath = Path.Combine(templateDirectory, "template-mapping.json");

                // 创建服务容器
                var serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);

                // 检查服务注册情况
                Console.WriteLine("=== 已注册的服务 ===");
                foreach (var serviceType in serviceContainer.GetRegisteredServiceTypes())
                {
                    Console.WriteLine($"  - {serviceType.Name}");
                }

                // 获取并测试各个服务
                Console.WriteLine("\n=== 服务测试 ===");

                // 测试模板注册表
                var templateRegistry = serviceContainer.GetService<ITemplateRegistry>();
                Console.WriteLine($"模板注册表中的模板: {string.Join(", ", templateRegistry.GetAllTemplateNames())}");

                // 测试Excel解析器
                var excelParser = serviceContainer.GetService<IExcelWorkbookParser>();
                Console.WriteLine("Excel解析器服务已就绪");

                // 测试点位工厂
                var pointFactory = serviceContainer.GetService<IPointFactory>();
                Console.WriteLine("点位工厂服务已就绪");

                // 测试设备分类服务
                var deviceClassificationService = serviceContainer.GetService<IDeviceClassificationService>();
                Console.WriteLine("设备分类服务已就绪");

                Console.WriteLine("\n所有服务测试通过!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务容器测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行所有示例
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("新架构使用示例");
            Console.WriteLine("=".PadRight(60, '='));

            Console.WriteLine("\n1. 服务容器示例");
            Console.WriteLine("-".PadRight(40, '-'));
            ServiceContainerExample();

            Console.WriteLine("\n\n2. 基本使用示例");
            Console.WriteLine("-".PadRight(40, '-'));
            BasicUsageExample();

            Console.WriteLine("\n\n3. 高级使用示例");
            Console.WriteLine("-".PadRight(40, '-'));
            AdvancedUsageExample();

            Console.WriteLine("\n示例运行完成!");
        }
    }
}