using System;
using WinFormsApp1.Utils;
using WinFormsApp1;

namespace WinFormsApp1.Tests
{
    /// <summary>
    /// ChannelConverter测试类 - 验证PLC地址转换功能
    /// </summary>
    public class ChannelConverterTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("开始ChannelConverter测试...\n");
            
            TestPlcAddressConversion();
            TestLegacyChannelConversion();
            TestValidation();
            TestChannelTypeInference();
            TestEdgeCases();
            TestConversionInfo();
            
            Console.WriteLine("所有测试完成!\n");
        }
        
        /// <summary>
        /// 测试PLC地址转换功能
        /// </summary>
        public static void TestPlcAddressConversion()
        {
            Console.WriteLine("=== 测试PLC地址转换 ===");
            
            // AI类型测试用例
            var aiTestCases = new[]
            {
                ("%MD320", "DPIO_2_1_1_1"),    // AI第1个通道
                ("%MD344", "DPIO_2_1_1_2"),    // AI第2个通道 (320+24=344)
                ("%MD368", "DPIO_2_1_1_3"),    // AI第3个通道 (320+48=368)
            };
            
            foreach (var (input, expected) in aiTestCases)
            {
                var result = ChannelConverter.ConvertToHardChannel(input);
                var status = result == expected ? "✓ 通过" : $"✗ 失败 (期望:{expected}, 实际:{result})";
                Console.WriteLine($"  {input} -> {result} [{status}]");
            }
            
            // DI类型测试用例
            var diTestCases = new[]
            {
                ("%MX25.0", "DPIO_2_3_0_1"),   // DI第1个通道
                ("%MX25.1", "DPIO_2_3_0_2"),   // DI第2个通道
                ("%MX25.2", "DPIO_2_3_0_3"),   // DI第3个通道
            };
            
            foreach (var (input, expected) in diTestCases)
            {
                var result = ChannelConverter.ConvertToHardChannel(input);
                var status = result == expected ? "✓ 通过" : $"✗ 失败 (期望:{expected}, 实际:{result})";
                Console.WriteLine($"  {input} -> {result} [{status}]");
            }
            
            // DO类型测试用例
            var doTestCases = new[]
            {
                ("%MX26.0", "DPIO_2_4_0_1"),   // DO第1个通道
                ("%MX26.1", "DPIO_2_4_0_2"),   // DO第2个通道
                ("%MX26.2", "DPIO_2_4_0_3"),   // DO第3个通道
            };
            
            foreach (var (input, expected) in doTestCases)
            {
                var result = ChannelConverter.ConvertToHardChannel(input);
                var status = result == expected ? "✓ 通过" : $"✗ 失败 (期望:{expected}, 实际:{result})";
                Console.WriteLine($"  {input} -> {result} [{status}]");
            }
            
            // AO类型测试用例
            var aoTestCases = new[]
            {
                ("%MD896", "DPIO_2_2_1_1"),    // AO第1个通道
                ("%MD900", "DPIO_2_2_1_2"),    // AO第2个通道 (896+4=900)
                ("%MD904", "DPIO_2_2_1_3"),    // AO第3个通道 (896+8=904)
            };
            
            foreach (var (input, expected) in aoTestCases)
            {
                var result = ChannelConverter.ConvertToHardChannel(input);
                var status = result == expected ? "✓ 通过" : $"✗ 失败 (期望:{expected}, 实际:{result})";
                Console.WriteLine($"  {input} -> {result} [{status}]");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// 测试传统通道转换功能 (向后兼容)
        /// </summary>
        public static void TestLegacyChannelConversion()
        {
            Console.WriteLine("=== 测试传统格式转换 (向后兼容) ===");
            
            var testCases = new[]
            {
                ("1_1_AI_0", "DPIO_2_1_2_1"),
                ("1_2_AO_1", "DPIO_2_1_3_2"),
                ("2_1_DI_0", "DPIO_3_1_2_1"),
                ("2_2_DO_2", "DPIO_3_1_3_3"),
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result = ChannelConverter.ConvertToHardChannel(input);
                var status = result == expected ? "✓ 通过" : $"✗ 失败 (期望:{expected}, 实际:{result})";
                Console.WriteLine($"  {input} -> {result} [{status}]");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// 测试地址验证功能
        /// </summary>
        public static void TestValidation()
        {
            Console.WriteLine("=== 测试地址验证功能 ===");
            
            var validAddresses = new[]
            {
                "%MD320", "%MD896", "%MX25.0", "%MX26.7",  // PLC地址
                "1_1_AI_0", "2_3_DO_15"                    // 传统格式
            };
            
            var invalidAddresses = new[]
            {
                "", "   ", "invalid", "%MD", "%MX25", "1_1_XX_0", "abc_def_ghi"
            };
            
            Console.WriteLine("  有效地址测试:");
            foreach (var addr in validAddresses)
            {
                var isValid = ChannelConverter.IsValidChannelPosition(addr);
                var status = isValid ? "✓ 通过" : "✗ 失败";
                Console.WriteLine($"    '{addr}' -> {isValid} [{status}]");
            }
            
            Console.WriteLine("  无效地址测试:");
            foreach (var addr in invalidAddresses)
            {
                var isValid = ChannelConverter.IsValidChannelPosition(addr);
                var status = !isValid ? "✓ 通过" : "✗ 失败";
                Console.WriteLine($"    '{addr}' -> {isValid} [{status}]");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// 测试通道类型推断功能
        /// </summary>
        public static void TestChannelTypeInference()
        {
            Console.WriteLine("=== 测试通道类型推断功能 ===");
            
            var testCases = new[]
            {
                ("%MD320", "AI"), ("%MD500", "AI"), ("%MD895", "AI"),  // AI范围
                ("%MD896", "AO"), ("%MD1000", "AO"),                   // AO范围
                ("%MX25.0", "DI"), ("%MX25.7", "DI"),                  // DI地址
                ("%MX26.0", "DO"), ("%MX26.7", "DO"),                  // DO地址
                ("1_1_AI_0", "AI"), ("2_2_DO_5", "DO"),                // 传统格式
                ("%MD100", ""), ("invalid", "")                        // 无效地址
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result = ChannelConverter.GetChannelType(input);
                var status = result == expected ? "✓ 通过" : $"✗ 失败 (期望:{expected}, 实际:{result})";
                Console.WriteLine($"  {input} -> '{result}' [{status}]");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// 测试边界情况
        /// </summary>
        public static void TestEdgeCases()
        {
            Console.WriteLine("=== 测试边界情况 ===");
            
            var edgeCases = new[]
            {
                ("", "DPIO_2_1_2_1"),           // 空字符串
                ("   ", "DPIO_2_1_2_1"),         // 空白字符串
                ("%md320", "DPIO_2_1_1_1"),      // 小写PLC地址
                ("%MD0", "DPIO_2_1_2_1"),        // 无效范围的PLC地址
                ("0_0_ai_0", "DPIO_1_1_1_1"),    // 小写传统格式
            };
            
            foreach (var (input, expected) in edgeCases)
            {
                var result = ChannelConverter.ConvertToHardChannel(input);
                var status = result == expected ? "✓ 通过" : $"✗ 失败 (期望:{expected}, 实际:{result})";
                Console.WriteLine($"  '{input}' -> {result} [{status}]");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// 测试详细转换信息功能
        /// </summary>
        public static void TestConversionInfo()
        {
            Console.WriteLine("=== 测试详细转换信息功能 ===");
            
            var testAddresses = new[] { "%MD320", "%MX25.0", "1_1_AI_0", "invalid" };
            
            foreach (var addr in testAddresses)
            {
                var info = ChannelConverter.GetConversionInfo(addr);
                Console.WriteLine($"  地址: '{addr}'");
                Console.WriteLine($"    有效性: {info.IsValid}");
                Console.WriteLine($"    检测格式: {info.DetectedFormat ?? "无"}");
                Console.WriteLine($"    点位类型: {info.PointType ?? "无"}");
                Console.WriteLine($"    硬通道: {info.HardChannel ?? "无"}");
                Console.WriteLine($"    错误信息: {info.ErrorMessage ?? "无"}");
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// 测试实际Excel数据样例
        /// </summary>
        public static void TestRealDataSamples()
        {
            Console.WriteLine("=== 测试实际Excel数据样例 ===");
            
            // 基于实际Excel数据的测试用例
            var realDataSamples = new[]
            {
                // AI点位样例
                "%MD320", "%MD344", "%MD368", "%MD392", "%MD416",
                // DI点位样例  
                "%MX25.0", "%MX25.1", "%MX25.2", "%MX25.3",
                // DO点位样例
                "%MX26.0", "%MX26.1", "%MX26.2", "%MX26.3",
                // AO点位样例
                "%MD896", "%MD900", "%MD904", "%MD908"
            };
            
            Console.WriteLine("转换结果统计:");
            int successCount = 0;
            int totalCount = realDataSamples.Length;
            
            foreach (var sample in realDataSamples)
            {
                var result = ChannelConverter.ConvertToHardChannel(sample);
                var isDefaultValue = result == "DPIO_2_1_2_1";
                
                if (!isDefaultValue)
                {
                    successCount++;
                }
                
                var pointType = ChannelConverter.GetChannelType(sample);
                Console.WriteLine($"  {sample} -> {result} [类型: {pointType}]");
            }
            
            var successRate = (double)successCount / totalCount * 100;
            Console.WriteLine($"\n转换成功率: {successCount}/{totalCount} ({successRate:F1}%)");
            Console.WriteLine();
        }
    }
}