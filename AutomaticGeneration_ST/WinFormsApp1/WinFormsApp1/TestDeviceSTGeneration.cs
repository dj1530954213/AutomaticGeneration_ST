using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services;
using AutomaticGeneration_ST.Tests;
using System;
using System.Collections.Generic;
using WinFormsApp1;

namespace AutomaticGeneration_ST
{
    /// <summary>
    /// 测试设备ST程序生成功能的控制台程序
    /// </summary>
    public class TestDeviceSTGeneration
    {
        private static readonly LogService _logger = LogService.Instance;

        public static void Main(string[] args)
        {
            try
            {
                _logger.LogInfo("🧪 开始设备ST程序生成功能测试...");

                // 1. 运行设备模板数据绑定测试
                var deviceSTTest = new DeviceSTGenerationTest();
                var bindingTestResult = deviceSTTest.TestTemplateDataBinding();
                
                _logger.LogInfo($"📝 模板数据绑定测试结果: {(bindingTestResult.Success ? "通过" : "失败")}");
                _logger.LogInfo($"   详情: {bindingTestResult.Message}");
                
                if (bindingTestResult.Details != null)
                {
                    foreach (var detail in bindingTestResult.Details)
                    {
                        _logger.LogInfo($"   - {detail}");
                    }
                }

                // 2. 运行设备ST程序生成测试
                var stGenerationTestResult = deviceSTTest.TestDeviceSTGeneration();
                
                _logger.LogInfo($"🏭 设备ST程序生成测试结果: {(stGenerationTestResult.Success ? "通过" : "失败")}");
                _logger.LogInfo($"   详情: {stGenerationTestResult.Message}");
                
                if (stGenerationTestResult.Details != null)
                {
                    foreach (var detail in stGenerationTestResult.Details)
                    {
                        _logger.LogInfo($"   - {detail}");
                    }
                }

                // 3. 显示总体测试结果
                var allTestsPassed = bindingTestResult.Success && stGenerationTestResult.Success;
                
                if (allTestsPassed)
                {
                    _logger.LogSuccess("✅ 所有设备ST程序生成功能测试通过！");
                    _logger.LogInfo("Form1.cs中的设备ST程序窗口应该能够正常显示生成的ST代码。");
                }
                else
                {
                    _logger.LogWarning("⚠️ 部分测试未通过，可能需要进一步检查模板文件或数据绑定逻辑。");
                }

                // 4. 额外验证模板文件存在性
                _logger.LogInfo("🔍 验证模板文件...");
                VerifyTemplateFiles();

                Console.WriteLine("\n按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ 测试程序执行失败: {ex.Message}");
                _logger.LogError($"堆栈跟踪: {ex.StackTrace}");
                Console.WriteLine("\n按任意键退出...");
                Console.ReadKey();
            }
        }

        private static void VerifyTemplateFiles()
        {
            var templatesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            _logger.LogInfo($"模板目录: {templatesDir}");

            // 检查阀门模板文件夹
            var valveTemplateDir = System.IO.Path.Combine(templatesDir, "阀门");
            if (System.IO.Directory.Exists(valveTemplateDir))
            {
                _logger.LogInfo("✓ 发现阀门模板文件夹");
                
                var templateFiles = System.IO.Directory.GetFiles(valveTemplateDir, "*.scriban");
                foreach (var templateFile in templateFiles)
                {
                    var fileName = System.IO.Path.GetFileName(templateFile);
                    _logger.LogInfo($"   ✓ 模板文件: {fileName}");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到阀门模板文件夹");
            }

            // 检查配置文件
            var configFile = System.IO.Path.Combine(templatesDir, "template-mapping.json");
            if (System.IO.File.Exists(configFile))
            {
                _logger.LogInfo("✓ 发现模板映射配置文件");
            }
            else
            {
                _logger.LogWarning("⚠️ 未找到模板映射配置文件");
            }
        }
    }
}