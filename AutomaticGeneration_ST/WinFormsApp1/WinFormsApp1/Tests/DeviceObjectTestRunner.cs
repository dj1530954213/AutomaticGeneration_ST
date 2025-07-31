using System;
using System.IO;

namespace AutomaticGeneration_ST.Tests
{
    /// <summary>
    /// 设备对象创建测试的运行器
    /// </summary>
    public class DeviceObjectTestRunner
    {
        /// <summary>
        /// 运行设备对象创建测试
        /// </summary>
        public static void RunDeviceObjectTest()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("   设备对象创建功能测试");
            Console.WriteLine("========================================");
            Console.WriteLine($"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"工作目录: {Environment.CurrentDirectory}");
            Console.WriteLine();

            try
            {
                var tester = new DeviceObjectCreationTest();
                var result = tester.TestDeviceObjectCreationFromExcel();

                // 输出测试结果
                Console.WriteLine("========================================");
                Console.WriteLine("   测试结果");
                Console.WriteLine("========================================");
                Console.WriteLine($"测试名称: {result.TestName}");
                Console.WriteLine($"测试状态: {(result.Success ? "✅ 成功" : "❌ 失败")}");
                Console.WriteLine($"执行时间: {result.Duration.TotalSeconds:F2}秒");
                Console.WriteLine($"结果消息: {result.Message}");

                if (result.Details?.Count > 0)
                {
                    Console.WriteLine("\n详细信息:");
                    foreach (var detail in result.Details)
                    {
                        Console.WriteLine($"  {detail}");
                    }
                }

                Console.WriteLine("\n========================================");
                if (result.Success)
                {
                    Console.WriteLine("🎉 测试通过！软件能够成功从设备分类表创建完整的设备对象。");
                }
                else
                {
                    Console.WriteLine("⚠️  测试失败！需要检查设备对象创建功能。");
                }
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("   测试执行异常");
                Console.WriteLine("========================================");
                Console.WriteLine($"异常类型: {ex.GetType().Name}");
                Console.WriteLine($"异常消息: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                Console.WriteLine("========================================");
            }

            Console.WriteLine("\n按任意键继续...");
            Console.ReadKey();
        }
    }
}