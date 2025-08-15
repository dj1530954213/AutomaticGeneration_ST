using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace WinFormsApp1.Tests
{
    /// <summary>
    /// 测试执行器 - 提供简单的测试执行入口
    /// </summary>
    /// <remarks>
    /// 状态: @test-code-mixed
    /// 优先级: P1 (低风险)
    /// 建议: 应移至独立的测试项目
    /// 风险级别: 低风险
    /// 分析时间: 2025-08-15
    /// 影响范围: 仅开发阶段，不影响生产功能
    /// 说明: 提供测试执行的统一入口和结果管理
    /// </remarks>
    public static class TestExecutor
    {
        #region 私有字段

        private static readonly Dictionary<string, string> _testResults = new();

        #endregion

        #region 主要执行方法

        /// <summary>
        /// 执行完整的系统功能验证
        /// </summary>
        public static async Task<bool> ExecuteFullSystemValidationAsync()
        {
            Console.WriteLine("====================================================");
            Console.WriteLine("ST自动生成器系统 - 完整功能验证");
            Console.WriteLine("====================================================");
            Console.WriteLine($"开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            var overallStopwatch = Stopwatch.StartNew();
            bool overallSuccess = true;

            try
            {
                // 1. 环境检查
                Console.WriteLine("📋 步骤 1/6: 执行环境检查...");
                var envCheckResult = await ExecuteEnvironmentCheckAsync();
                LogResult("环境检查", envCheckResult);
                if (!envCheckResult) overallSuccess = false;

                // 2. 快速验证测试
                Console.WriteLine("\n🔍 步骤 2/6: 执行快速验证测试...");
                var quickTestResult = await ExecuteQuickValidationAsync();
                LogResult("快速验证", quickTestResult);
                if (!quickTestResult) overallSuccess = false;

                // 3. 核心功能测试
                Console.WriteLine("\n🔧 步骤 3/6: 执行核心功能测试...");
                var coreTestResult = await ExecuteCoreFunctionalTestsAsync();
                LogResult("核心功能测试", coreTestResult);
                if (!coreTestResult) overallSuccess = false;

                // 4. 组合设备系统测试
                Console.WriteLine("\n🏗️ 步骤 4/6: 执行组合设备系统测试...");
                var deviceTestResult = await ExecuteDeviceSystemTestsAsync();
                LogResult("组合设备系统测试", deviceTestResult);
                if (!deviceTestResult) overallSuccess = false;

                // 5. 集成测试
                Console.WriteLine("\n🔗 步骤 5/6: 执行系统集成测试...");
                var integrationTestResult = await ExecuteIntegrationTestsAsync();
                LogResult("系统集成测试", integrationTestResult);
                if (!integrationTestResult) overallSuccess = false;

                // 6. 生成测试报告
                Console.WriteLine("\n📊 步骤 6/6: 生成测试报告...");
                var reportResult = await GenerateTestReportsAsync();
                LogResult("测试报告生成", reportResult);

                overallStopwatch.Stop();

                // 输出最终结果
                Console.WriteLine("\n====================================================");
                Console.WriteLine("系统功能验证完成");
                Console.WriteLine("====================================================");
                Console.WriteLine($"总执行时间: {overallStopwatch.Elapsed.TotalSeconds:F2} 秒");
                Console.WriteLine($"整体结果: {(overallSuccess ? "✅ 通过" : "❌ 失败")}");
                Console.WriteLine();

                // 输出详细结果
                Console.WriteLine("详细测试结果:");
                Console.WriteLine("----------------------------------------------------");
                foreach (var result in _testResults)
                {
                    var status = result.Value == "通过" ? "✅" : "❌";
                    Console.WriteLine($"{status} {result.Key}: {result.Value}");
                }

                Console.WriteLine();
                if (overallSuccess)
                {
                    Console.WriteLine("🎉 恭喜！ST自动生成器系统所有功能验证通过！");
                    Console.WriteLine("系统已准备就绪，可以投入使用。");
                }
                else
                {
                    Console.WriteLine("⚠️ 发现一些功能问题，请检查上述失败的测试项。");
                    Console.WriteLine("建议修复失败项后重新运行验证。");
                }

                return overallSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 测试执行过程中发生严重错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 执行快速健康检查
        /// </summary>
        public static async Task<bool> ExecuteQuickHealthCheckAsync()
        {
            Console.WriteLine("ST自动生成器 - 快速健康检查");
            Console.WriteLine("==========================================");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var testRunner = new TestRunner();
                var quickResult = await testRunner.RunQuickValidationTestsAsync();

                stopwatch.Stop();

                Console.WriteLine($"快速检查完成 - 耗时: {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine($"检查结果: {quickResult.PassedTests}/{quickResult.TotalTests} 通过");
                Console.WriteLine($"成功率: {quickResult.SuccessRate:F1}%");
                Console.WriteLine();

                foreach (var result in quickResult)
                {
                    var status = result.Success ? "✅" : "❌";
                    Console.WriteLine($"{status} {result.TestName}: {result.Message}");
                }

                Console.WriteLine();
                if (quickResult.OverallSuccess())
                {
                    Console.WriteLine("✅ 系统健康状态良好！");
                }
                else
                {
                    Console.WriteLine("⚠️ 发现一些问题，建议运行完整的功能验证。");
                }

                return quickResult.OverallSuccess();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 健康检查失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 具体测试执行方法

        /// <summary>
        /// 执行环境检查
        /// </summary>
        private static async Task<bool> ExecuteEnvironmentCheckAsync()
        {
            try
            {
                var checks = new List<(string name, Func<Task<bool>> check)>
                {
                    ("检查.NET运行时", async () =>
                    {
                        var version = Environment.Version;
                        Console.WriteLine($"  .NET版本: {version}");
                        return version.Major >= 6; // 要求.NET 6或更高版本
                    }),

                    ("检查程序集完整性", async () =>
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var location = assembly.Location;
                        Console.WriteLine($"  程序集位置: {location}");
                        return !string.IsNullOrEmpty(location) && File.Exists(location);
                    }),

                    ("检查工作目录", async () =>
                    {
                        var workingDir = Directory.GetCurrentDirectory();
                        Console.WriteLine($"  工作目录: {workingDir}");
                        return Directory.Exists(workingDir);
                    }),

                    ("检查临时目录权限", async () =>
                    {
                        var tempDir = Path.GetTempPath();
                        var testFile = Path.Combine(tempDir, $"st_test_{Guid.NewGuid()}.tmp");
                        try
                        {
                            await File.WriteAllTextAsync(testFile, "test");
                            var canRead = File.Exists(testFile);
                            if (File.Exists(testFile)) File.Delete(testFile);
                            Console.WriteLine($"  临时目录可写: {canRead}");
                            return canRead;
                        }
                        catch
                        {
                            Console.WriteLine("  临时目录不可写");
                            return false;
                        }
                    })
                };

                var results = new List<bool>();
                foreach (var check in checks)
                {
                    try
                    {
                        var result = await check.check();
                        results.Add(result);
                        var status = result ? "✅" : "❌";
                        Console.WriteLine($"  {status} {check.name}");
                    }
                    catch (Exception ex)
                    {
                        results.Add(false);
                        Console.WriteLine($"  ❌ {check.name}: {ex.Message}");
                    }
                }

                var success = results.TrueForAll(r => r);
                Console.WriteLine($"环境检查结果: {results.Count(r => r)}/{results.Count} 通过");
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"环境检查失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行快速验证
        /// </summary>
        private static async Task<bool> ExecuteQuickValidationAsync()
        {
            try
            {
                var testRunner = new TestRunner();
                var result = await testRunner.RunQuickValidationTestsAsync();

                Console.WriteLine($"快速验证完成: {result.PassedTests()}/{result.TotalTests()} 通过 ({result.SuccessRate():F1}%)");
                Console.WriteLine($"执行时间: {result.Duration().TotalMilliseconds:F0} ms");

                return result.OverallSuccess();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"快速验证失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行核心功能测试
        /// </summary>
        private static async Task<bool> ExecuteCoreFunctionalTestsAsync()
        {
            try
            {
                var testRunner = new TestRunner();
                var result = await testRunner.RunTestSuiteAsync("系统功能测试");

                Console.WriteLine($"核心功能测试完成: {result.PassedTests()}/{result.TotalTests()} 通过 ({result.SuccessRate():F1}%)");
                Console.WriteLine($"执行时间: {result.TotalDuration().TotalSeconds:F2} 秒");

                // 显示失败的关键测试
                var criticalFailures = result.Where(r => 
                    !r.Success && r.Severity == TestSeverity.Critical).ToList();

                if (criticalFailures.Any())
                {
                    Console.WriteLine("关键功能失败:");
                    foreach (var failure in criticalFailures.Take(3))
                    {
                        Console.WriteLine($"  ❌ {failure.TestName}: {failure.Message}");
                    }
                }

                return result.SuccessRate() >= 80; // 要求80%以上通过率
            }
            catch (Exception ex)
            {
                Console.WriteLine($"核心功能测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行设备系统测试
        /// </summary>
        private static async Task<bool> ExecuteDeviceSystemTestsAsync()
        {
            try
            {
                var testRunner = new TestRunner();
                var result = await testRunner.RunTestSuiteAsync("组合设备系统测试");

                Console.WriteLine($"设备系统测试完成: {result.PassedTests()}/{result.TotalTests()} 通过 ({result.SuccessRate():F1}%)");
                Console.WriteLine($"执行时间: {result.TotalDuration().TotalSeconds:F2} 秒");

                return result.SuccessRate() >= 75; // 要求75%以上通过率
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设备系统测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行集成测试
        /// </summary>
        private static async Task<bool> ExecuteIntegrationTestsAsync()
        {
            try
            {
                // 执行端到端集成测试
                var integrationTests = new SystemFunctionalTests();
                
                // 设置集成测试进度回调
                integrationTests.TestProgress += (s, message) =>
                {
                    Console.WriteLine($"    {message}");
                };

                var result = await integrationTests.RunComprehensiveFunctionalTestsAsync();

                // 只关注集成相关的测试结果
                var integrationResults = result.Results.Where(r => 
                    r.TestSuite == "系统集成" || r.TestSuite == "代码生成").ToList();

                var passedIntegration = integrationResults.Count(r => r.Success);
                var totalIntegration = integrationResults.Count;
                var integrationRate = totalIntegration > 0 ? (double)passedIntegration / totalIntegration * 100 : 0;

                Console.WriteLine($"集成测试完成: {passedIntegration}/{totalIntegration} 通过 ({integrationRate:F1}%)");

                return integrationRate >= 85; // 要求85%以上通过率
            }
            catch (Exception ex)
            {
                Console.WriteLine($"集成测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 生成测试报告
        /// </summary>
        private static async Task<bool> GenerateTestReportsAsync()
        {
            try
            {
                var testRunner = new TestRunner();
                var allResults = await testRunner.RunAllTestsAsync();

                Console.WriteLine($"报告生成完成，结果保存在: {testRunner.ReportOutputDirectory}");
                Console.WriteLine($"综合测试结果: {allResults.TotalPassed}/{allResults.TotalTests} 通过 ({allResults.OverallSuccessRate:F1}%)");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"报告生成失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 记录测试结果
        /// </summary>
        private static void LogResult(string testName, bool success)
        {
            _testResults[testName] = success ? "通过" : "失败";
        }

        /// <summary>
        /// 显示使用帮助
        /// </summary>
        public static void ShowUsage()
        {
            Console.WriteLine("ST自动生成器测试执行器");
            Console.WriteLine("========================");
            Console.WriteLine();
            Console.WriteLine("用法:");
            Console.WriteLine("  TestExecutor.ExecuteFullSystemValidationAsync()  - 执行完整系统验证");
            Console.WriteLine("  TestExecutor.ExecuteQuickHealthCheckAsync()      - 执行快速健康检查");
            Console.WriteLine();
            Console.WriteLine("示例代码:");
            Console.WriteLine("  var success = await TestExecutor.ExecuteFullSystemValidationAsync();");
            Console.WriteLine("  if (success) Console.WriteLine(\"系统验证通过\");");
            Console.WriteLine();
        }

        /// <summary>
        /// 获取系统信息
        /// </summary>
        public static Dictionary<string, object> GetSystemInfo()
        {
            return new Dictionary<string, object>
            {
                ["操作系统"] = Environment.OSVersion.ToString(),
                [".NET版本"] = Environment.Version.ToString(),
                ["处理器数量"] = Environment.ProcessorCount,
                ["工作目录"] = Directory.GetCurrentDirectory(),
                ["执行程序集"] = Assembly.GetExecutingAssembly().Location,
                ["当前时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["系统启动时间"] = Environment.TickCount64 / 1000 / 60 // 分钟
            };
        }

        #endregion
    }

    /// <summary>
    /// 测试执行入口程序
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 程序入口点 - 可用于独立测试执行
        /// </summary>
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("ST自动生成器系统测试执行器");
                Console.WriteLine("========================================");

                // 显示系统信息
                var systemInfo = TestExecutor.GetSystemInfo();
                Console.WriteLine("系统信息:");
                foreach (var info in systemInfo)
                {
                    Console.WriteLine($"  {info.Key}: {info.Value}");
                }
                Console.WriteLine();

                // 检查命令行参数
                if (args.Length > 0)
                {
                    switch (args[0].ToLower())
                    {
                        case "quick":
                        case "q":
                            Console.WriteLine("执行快速健康检查...\n");
                            var quickSuccess = await TestExecutor.ExecuteQuickHealthCheckAsync();
                            return quickSuccess ? 0 : 1;

                        case "full":
                        case "f":
                        default:
                            Console.WriteLine("执行完整系统验证...\n");
                            var fullSuccess = await TestExecutor.ExecuteFullSystemValidationAsync();
                            return fullSuccess ? 0 : 1;

                        case "help":
                        case "h":
                        case "?":
                            TestExecutor.ShowUsage();
                            return 0;
                    }
                }
                else
                {
                    // 默认执行完整验证
                    Console.WriteLine("执行完整系统验证...\n");
                    var success = await TestExecutor.ExecuteFullSystemValidationAsync();
                    return success ? 0 : 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序执行失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return -1;
            }
        }
    }
}