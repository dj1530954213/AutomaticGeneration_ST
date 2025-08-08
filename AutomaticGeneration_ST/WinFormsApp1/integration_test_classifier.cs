using System;
using System.Collections.Generic;
using System.IO;
using AutomaticGeneration_ST.Services;
using AutomaticGeneration_ST.Services.Interfaces;

namespace IntegrationTest
{
    /// <summary>
    /// 集成测试：验证分类导出功能的准确性
    /// </summary>
    public class ClassifierIntegrationTest
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== ST脚本分类器集成测试 ===");
            Console.WriteLine();
            
            try
            {
                // 读取测试数据
                var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "test_st_scripts.txt");
                if (!File.Exists(testFilePath))
                {
                    Console.WriteLine("❌ 测试文件不存在: " + testFilePath);
                    return;
                }
                
                var testContent = File.ReadAllText(testFilePath);
                var testScripts = SplitScripts(testContent);
                
                Console.WriteLine($"📂 读取测试脚本: {testScripts.Count}个");
                Console.WriteLine();
                
                // 创建服务容器
                var templateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Templates");
                var serviceContainer = ServiceContainer.CreateDefault(templateDirectory);
                
                // 获取分类器服务
                var classifier = serviceContainer.GetService<IScriptClassifier>();
                var exportService = serviceContainer.GetService<ICategorizedExportService>();
                
                Console.WriteLine("✅ 服务容器初始化成功");
                Console.WriteLine();
                
                // 测试分类准确性
                TestClassificationAccuracy(classifier, testScripts);
                
                // 测试导出功能
                TestExportFunctionality(exportService, testScripts);
                
                Console.WriteLine();
                Console.WriteLine("🎉 集成测试完成!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 集成测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
        }
        
        private static List<string> SplitScripts(string content)
        {
            var scripts = new List<string>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            var currentScript = new List<string>();
            
            foreach (var line in lines)
            {
                if (line.StartsWith("(*") && currentScript.Count > 0)
                {
                    // 新脚本开始，保存前一个脚本
                    scripts.Add(string.Join("\n", currentScript));
                    currentScript.Clear();
                }
                
                currentScript.Add(line);
            }
            
            // 添加最后一个脚本
            if (currentScript.Count > 0)
            {
                scripts.Add(string.Join("\n", currentScript));
            }
            
            return scripts;
        }
        
        private static void TestClassificationAccuracy(IScriptClassifier classifier, List<string> testScripts)
        {
            Console.WriteLine("🔍 测试分类准确性:");
            Console.WriteLine(new string('-', 50));
            
            int correctClassifications = 0;
            int totalScripts = testScripts.Count;
            
            for (int i = 0; i < testScripts.Count; i++)
            {
                var script = testScripts[i];
                var category = classifier.ClassifyScript(script);
                
                // 根据脚本内容判断预期分类
                var expectedCategory = DetermineExpectedCategory(script);
                
                var isCorrect = category == expectedCategory;
                if (isCorrect) correctClassifications++;
                
                var statusIcon = isCorrect ? "✅" : "❌";
                Console.WriteLine($"{statusIcon} 脚本 {i + 1}: {category} (预期: {expectedCategory})");
                
                if (!isCorrect)
                {
                    Console.WriteLine($"   脚本内容预览: {script.Substring(0, Math.Min(100, script.Length))}...");
                }
            }
            
            var accuracy = (double)correctClassifications / totalScripts * 100;
            Console.WriteLine();
            Console.WriteLine($"📊 分类准确率: {correctClassifications}/{totalScripts} = {accuracy:F1}%");
            Console.WriteLine();
            
            if (accuracy >= 90)
            {
                Console.WriteLine("🎉 分类准确率达到要求 (≥90%)");
            }
            else
            {
                Console.WriteLine("⚠️ 分类准确率未达到要求 (目标≥90%)");
            }
        }
        
        private static AutomaticGeneration_ST.Models.ScriptCategory DetermineExpectedCategory(string script)
        {
            if (script.Contains("AI_ALARM_") || script.Contains("(* AI点位:"))
                return AutomaticGeneration_ST.Models.ScriptCategory.AI;
            
            if (script.Contains("ENGIN_HEX_") || script.Contains("(* AO点位:"))
                return AutomaticGeneration_ST.Models.ScriptCategory.AO;
            
            if (script.Contains("DI_INPUT") || script.Contains("(* DI点位:"))
                return AutomaticGeneration_ST.Models.ScriptCategory.DI;
            
            if (script.Contains("DO_OUTPUT") || script.Contains("(* DO点位:"))
                return AutomaticGeneration_ST.Models.ScriptCategory.DO;
            
            return AutomaticGeneration_ST.Models.ScriptCategory.UNKNOWN;
        }
        
        private static void TestExportFunctionality(ICategorizedExportService exportService, List<string> testScripts)
        {
            Console.WriteLine("📤 测试导出功能:");
            Console.WriteLine(new string('-', 50));
            
            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "STClassifier_IntegrationTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(tempDir);
                
                Console.WriteLine($"📁 临时输出目录: {tempDir}");
                
                // 执行分类导出
                var result = exportService.ExportCategorizedFiles(testScripts, tempDir);
                
                Console.WriteLine($"✅ 导出完成: {result.OutputDirectory}");
                Console.WriteLine($"📊 总脚本数: {result.TotalScripts}");
                Console.WriteLine($"📄 AI脚本: {result.AiCount}个");
                Console.WriteLine($"📄 AO脚本: {result.AoCount}个");
                Console.WriteLine($"📄 DI脚本: {result.DiCount}个");
                Console.WriteLine($"📄 DO脚本: {result.DoCount}个");
                Console.WriteLine($"📄 其他脚本: {result.OtherCount}个");
                
                // 验证文件是否生成
                var expectedFiles = new[] { "AI_CONVERT.txt", "AO_CTRL.txt", "DI.txt", "DO.txt" };
                
                Console.WriteLine();
                Console.WriteLine("📋 验证输出文件:");
                
                foreach (var fileName in expectedFiles)
                {
                    var filePath = Path.Combine(result.OutputDirectory, fileName);
                    if (File.Exists(filePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        Console.WriteLine($"✅ {fileName}: {fileInfo.Length} bytes");
                        
                        // 显示文件内容预览
                        var content = File.ReadAllText(filePath);
                        var preview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
                        Console.WriteLine($"   内容预览: {preview.Replace('\n', ' ').Replace('\r', ' ')}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {fileName}: 文件不存在");
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine($"🗂️ 输出目录保留在: {result.OutputDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 导出测试失败: {ex.Message}");
            }
        }
    }
}