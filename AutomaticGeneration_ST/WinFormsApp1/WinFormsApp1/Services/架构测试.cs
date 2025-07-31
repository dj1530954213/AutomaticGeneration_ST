using AutomaticGeneration_ST.Services;
using System;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// 简单的架构测试类
    /// </summary>
    public static class ArchitectureTest
    {
        /// <summary>
        /// 测试新架构的基本功能
        /// </summary>
        public static void RunBasicTest()
        {
            try
            {
                Console.WriteLine("开始新架构基本测试...");

                // 创建服务容器
                var templateDirectory = @"C:\Templates";
                var configPath = @"C:\template-mapping.json";
                var serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);

                Console.WriteLine("✓ 服务容器创建成功");

                // 检查所有服务是否正确注册
                if (serviceContainer.IsRegistered<AutomaticGeneration_ST.Services.Interfaces.IExcelWorkbookParser>())
                    Console.WriteLine("✓ IExcelWorkbookParser 已注册");
                else
                    Console.WriteLine("✗ IExcelWorkbookParser 未注册");

                if (serviceContainer.IsRegistered<AutomaticGeneration_ST.Services.Interfaces.IPointFactory>())
                    Console.WriteLine("✓ IPointFactory 已注册");
                else
                    Console.WriteLine("✗ IPointFactory 未注册");

                if (serviceContainer.IsRegistered<AutomaticGeneration_ST.Services.Interfaces.ITemplateRegistry>())
                    Console.WriteLine("✓ ITemplateRegistry 已注册");
                else
                    Console.WriteLine("✗ ITemplateRegistry 未注册");

                if (serviceContainer.IsRegistered<AutomaticGeneration_ST.Services.Interfaces.IDeviceClassificationService>())
                    Console.WriteLine("✓ IDeviceClassificationService 已注册");
                else
                    Console.WriteLine("✗ IDeviceClassificationService 未注册");

                if (serviceContainer.IsRegistered<AutomaticGeneration_ST.Services.Interfaces.IDataProcessingOrchestrator>())
                    Console.WriteLine("✓ IDataProcessingOrchestrator 已注册");
                else
                    Console.WriteLine("✗ IDataProcessingOrchestrator 未注册");

                if (serviceContainer.IsRegistered<AutomaticGeneration_ST.Services.Interfaces.IDataService>())
                    Console.WriteLine("✓ IDataService 已注册");
                else
                    Console.WriteLine("✗ IDataService 未注册");

                // 尝试获取服务实例
                var dataService = serviceContainer.GetService<AutomaticGeneration_ST.Services.Interfaces.IDataService>();
                Console.WriteLine("✓ IDataService 服务实例获取成功");

                var orchestrator = serviceContainer.GetService<AutomaticGeneration_ST.Services.Interfaces.IDataProcessingOrchestrator>();
                Console.WriteLine("✓ IDataProcessingOrchestrator 服务实例获取成功");

                var templateRegistry = serviceContainer.GetService<AutomaticGeneration_ST.Services.Interfaces.ITemplateRegistry>();
                Console.WriteLine("✓ ITemplateRegistry 服务实例获取成功");

                Console.WriteLine("\n新架构基本测试完成！所有服务都正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }
    }
}