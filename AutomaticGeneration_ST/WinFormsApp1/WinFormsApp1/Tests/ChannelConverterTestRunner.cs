using System;
using WinFormsApp1.Utils;
using WinFormsApp1;

namespace WinFormsApp1.Tests
{
    /// <summary>
    /// ChannelConverter测试运行器 - 独立运行测试
    /// </summary>
    public class ChannelConverterTestRunner
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("ChannelConverter修复验证测试\n");
            Console.WriteLine("=".PadLeft(50, '='));
            
            // 初始化日志服务（简单的控制台实现）
            InitializeConsoleLogging();
            
            try
            {
                // 运行基本转换测试
                RunBasicConversionTests();
                
                // 运行实际数据样例测试
                RunRealDataTests();
                
                // 运行详细信息测试
                RunConversionInfoTests();
                
                Console.WriteLine("\n" + "=".PadLeft(50, '='));
                Console.WriteLine("所有测试完成!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试运行出错: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
        
        private static void InitializeConsoleLogging()
        {
            // 为了简化测试，我们直接在控制台输出日志信息
            Console.WriteLine("初始化控制台日志...");
        }
        
        private static void RunBasicConversionTests()
        {
            Console.WriteLine("\n=== 基本转换测试 ===");
            
            var testCases = new[]
            {
                // PLC地址格式测试用例
                ("%MD320", "AI", "DPIO_2_1_1_1"),   // AI第1个通道
                ("%MD344", "AI", "DPIO_2_1_1_2"),   // AI第2个通道
                ("%MD368", "AI", "DPIO_2_1_1_3"),   // AI第3个通道
                ("%MX25.0", "DI", "DPIO_2_3_0_1"), // DI第1个通道
                ("%MX25.1", "DI", "DPIO_2_3_0_2"), // DI第2个通道  
                ("%MX26.0", "DO", "DPIO_2_4_0_1"), // DO第1个通道
                ("%MD896", "AO", "DPIO_2_2_1_1"),  // AO第1个通道
                
                // 传统格式测试用例 (向后兼容)
                ("1_1_AI_0", "AI", "DPIO_2_1_2_1"),
                ("1_2_AO_1", "AO", "DPIO_2_1_3_2"),
            };
            
            int passedCount = 0;
            int totalCount = testCases.Length;
            
            foreach (var (input, expectedType, expectedOutput) in testCases)
            {
                try
                {
                    var result = ChannelConverter.ConvertToHardChannel(input);
                    var type = ChannelConverter.GetChannelType(input);
                    var isValid = ChannelConverter.IsValidChannelPosition(input);
                    
                    bool testPassed = result == expectedOutput && type == expectedType && isValid;
                    
                    if (testPassed)
                    {
                        passedCount++;
                        Console.WriteLine($"✓ {input} -> {result} [类型: {type}]");
                    }
                    else
                    {
                        Console.WriteLine($"✗ {input} -> {result} [类型: {type}]");
                        Console.WriteLine($"  期望: {expectedOutput} [类型: {expectedType}]");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ {input} -> 异常: {ex.Message}");
                }
            }
            
            Console.WriteLine($"测试结果: {passedCount}/{totalCount} 通过 ({(double)passedCount/totalCount*100:F1}%)");
        }
        
        private static void RunRealDataTests()
        {
            Console.WriteLine("\n=== 实际数据测试 ===");
            
            // 基于问题描述的实际Excel数据
            var realDataSamples = new[]
            {
                // AI 点位样例
                "%MD320", "%MD344", "%MD368", "%MD392", "%MD416",
                // DI 点位样例  
                "%MX25.0", "%MX25.1", "%MX25.2", "%MX25.3",
                // DO 点位样例
                "%MX26.0", "%MX26.1", "%MX26.2", "%MX26.3",
                // AO 点位样例
                "%MD896", "%MD900", "%MD904", "%MD908"
            };
            
            int successCount = 0;
            int totalCount = realDataSamples.Length;
            
            Console.WriteLine("测试144个点位样例中的代表性数据:");
            foreach (var sample in realDataSamples)
            {
                try
                {
                    var result = ChannelConverter.ConvertToHardChannel(sample);
                    var pointType = ChannelConverter.GetChannelType(sample);
                    var isValid = ChannelConverter.IsValidChannelPosition(sample);
                    var isDefaultValue = result == "DPIO_2_1_2_1";\n                    \n                    if (!isDefaultValue && isValid && !string.IsNullOrEmpty(pointType))\n                    {\n                        successCount++;\n                        Console.WriteLine($\"✓ {sample} -> {result} [类型: {pointType}]\");\n                    }\n                    else\n                    {\n                        Console.WriteLine($\"✗ {sample} -> {result} [类型: {pointType}, 有效: {isValid}]\");\n                    }\n                }\n                catch (Exception ex)\n                {\n                    Console.WriteLine($\"✗ {sample} -> 异常: {ex.Message}\");\n                }\n            }\n            \n            var successRate = (double)successCount / totalCount * 100;\n            Console.WriteLine($\"\\n转换成功率: {successCount}/{totalCount} ({successRate:F1}%)\");\n            \n            // 显示修复前后的对比\n            Console.WriteLine($\"\\n修复前: 0% 转换成功 (全部使用默认值 DPIO_2_1_2_1)\");\n            Console.WriteLine($\"修复后: {successRate:F1}% 转换成功 (正确识别PLC地址格式)\");\n        }\n        \n        private static void RunConversionInfoTests()\n        {\n            Console.WriteLine(\"\\n=== 详细转换信息测试 ===\");\n            \n            var testAddresses = new[] { \"%MD320\", \"%MX25.0\", \"1_1_AI_0\", \"invalid_format\" };\n            \n            foreach (var addr in testAddresses)\n            {\n                try\n                {\n                    var info = ChannelConverter.GetConversionInfo(addr);\n                    Console.WriteLine($\"\\n地址: '{addr}'\");\n                    Console.WriteLine($\"  有效性: {info.IsValid}\");\n                    Console.WriteLine($\"  检测格式: {info.DetectedFormat ?? \"无\"}\");\n                    Console.WriteLine($\"  点位类型: {info.PointType ?? \"无\"}\");\n                    Console.WriteLine($\"  内存类型: {info.MemoryType ?? \"无\"}\");\n                    Console.WriteLine($\"  地址: {info.Address?.ToString() ?? \"无\"}\");\n                    Console.WriteLine($\"  位位置: {info.BitPosition?.ToString() ?? \"无\"}\");\n                    Console.WriteLine($\"  硬通道: {info.HardChannel ?? \"无\"}\");\n                    Console.WriteLine($\"  错误信息: {info.ErrorMessage ?? \"无\"}\");\n                }\n                catch (Exception ex)\n                {\n                    Console.WriteLine($\"\\n地址: '{addr}' -> 异常: {ex.Message}\");\n                }\n            }\n        }\n    }\n}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ {sample} -> 异常: {ex.Message}");
                }
            }
            
            var successRate = (double)successCount / totalCount * 100;
            Console.WriteLine($"\n转换成功率: {successCount}/{totalCount} ({successRate:F1}%)");
            
            // 显示修复前后的对比
            Console.WriteLine($"\n修复前: 0% 转换成功 (全部使用默认值 DPIO_2_1_2_1)");
            Console.WriteLine($"修复后: {successRate:F1}% 转换成功 (正确识别PLC地址格式)");
        }
        
        private static void RunConversionInfoTests()
        {
            Console.WriteLine("\n=== 详细转换信息测试 ===");
            
            var testAddresses = new[] { "%MD320", "%MX25.0", "1_1_AI_0", "invalid_format" };
            
            foreach (var addr in testAddresses)
            {
                try
                {
                    var info = ChannelConverter.GetConversionInfo(addr);
                    Console.WriteLine($"\n地址: '{addr}'");
                    Console.WriteLine($"  有效性: {info.IsValid}");
                    Console.WriteLine($"  检测格式: {info.DetectedFormat ?? "无"}");
                    Console.WriteLine($"  点位类型: {info.PointType ?? "无"}");
                    Console.WriteLine($"  内存类型: {info.MemoryType ?? "无"}");
                    Console.WriteLine($"  地址: {info.Address?.ToString() ?? "无"}");
                    Console.WriteLine($"  位位置: {info.BitPosition?.ToString() ?? "无"}");
                    Console.WriteLine($"  硬通道: {info.HardChannel ?? "无"}");
                    Console.WriteLine($"  错误信息: {info.ErrorMessage ?? "无"}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n地址: '{addr}' -> 异常: {ex.Message}");
                }
            }
        }
    }
}