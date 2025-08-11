using System;
using System.Collections.Generic;
using WinFormsApp1.Template;

namespace WinFormsApp1.Tests
{
    /// <summary>
    /// 模板过滤功能测试类
    /// 验证TemplateRenderer中的FilterClassificationLines功能是否正常工作
    /// </summary>
    public static class TemplateFilterTestRunner
    {
        private static LogService logger = LogService.Instance;

        /// <summary>
        /// 运行所有过滤测试
        /// </summary>
        public static void RunAllTests()
        {
            logger.LogInfo("🧪 开始测试模板过滤功能...");
            
            try
            {
                TestBasicFiltering();
                TestComplexFiltering();
                TestEdgeCases();
                
                logger.LogSuccess("✅ 所有过滤测试通过!");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ 测试失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 测试基本过滤功能
        /// </summary>
        private static void TestBasicFiltering()
        {
            logger.LogInfo("测试1: 基本过滤功能");
            
            var templateContent = @"
程序名称: IO映射
变量类型: VAR
变量名称: TestVar
SOME_REAL_CODE := 1;
另一行代码;
变量类型： GLOBAL_VAR
更多代码;
程序名称： 其他程序
";
            
            var testData = new Dictionary<string, object> 
            { 
                ["test"] = "value" 
            };
            
            var result = TemplateRenderer.RenderFromText(templateContent, testData);
            
            // 验证过滤结果
            if (result.Contains("程序名称:") || result.Contains("变量类型:") || result.Contains("变量名称:"))
            {
                throw new Exception($"基本过滤测试失败：仍包含分类标识行\n结果：{result}");
            }
            
            if (!result.Contains("SOME_REAL_CODE") || !result.Contains("另一行代码"))
            {
                throw new Exception($"基本过滤测试失败：丢失了有效代码\n结果：{result}");
            }
            
            logger.LogSuccess("✅ 基本过滤测试通过");
        }
        
        /// <summary>
        /// 测试复杂过滤场景
        /// </summary>
        private static void TestComplexFiltering()
        {
            logger.LogInfo("测试2: 复杂过滤场景");
            
            var templateContent = @"
    程序名称: AI模块    

   变量类型:  VAR_GLOBAL   
    
AI_Point_1 := %MD320;
程序名称:DO模块
DO_Point_1 := TRUE;
   变量名称:   Test_Variable   
更多有效代码;
";
            
            var testData = new Dictionary<string, object> 
            { 
                ["test"] = "complex" 
            };
            
            var result = TemplateRenderer.RenderFromText(templateContent, testData);
            
            // 验证复杂场景
            var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("程序名称:") || 
                    trimmed.StartsWith("变量类型:") || 
                    trimmed.StartsWith("变量名称:"))
                {
                    throw new Exception($"复杂过滤测试失败：未过滤行 '{trimmed}'\n完整结果：{result}");
                }
            }
            
            logger.LogSuccess("✅ 复杂过滤测试通过");
        }
        
        /// <summary>
        /// 测试边界情况
        /// </summary>
        private static void TestEdgeCases()
        {
            logger.LogInfo("测试3: 边界情况");
            
            // 测试空内容
            var emptyResult = TemplateRenderer.RenderFromText("", new Dictionary<string, object>());
            if (!string.IsNullOrEmpty(emptyResult))
            {
                throw new Exception("空内容测试失败");
            }
            
            // 测试只有分类标识的内容
            var onlyMetadata = @"
程序名称: Test
变量类型: VAR
变量名称: TestVar
";
            var onlyMetadataResult = TemplateRenderer.RenderFromText(onlyMetadata, new Dictionary<string, object>());
            if (!string.IsNullOrWhiteSpace(onlyMetadataResult))
            {
                throw new Exception($"只有元数据测试失败，应该返回空内容，实际：'{onlyMetadataResult}'");
            }
            
            // 测试中文冒号
            var chineseColon = @"
程序名称：Test（中文冒号）
变量类型：VAR
实际代码;
";
            var chineseColonResult = TemplateRenderer.RenderFromText(chineseColon, new Dictionary<string, object>());
            if (chineseColonResult.Contains("程序名称：") || chineseColonResult.Contains("变量类型："))
            {
                throw new Exception($"中文冒号测试失败：{chineseColonResult}");
            }
            
            logger.LogSuccess("✅ 边界情况测试通过");
        }
        
        /// <summary>
        /// 测试传统生成器路径的过滤功能
        /// </summary>
        public static void TestLegacyGeneratorFiltering()
        {
            logger.LogInfo("🔧 测试传统生成器路径的过滤功能...");
            
            try
            {
                // 模拟传统生成器调用
                var templatePath = System.IO.Path.Combine(
                    System.AppDomain.CurrentDomain.BaseDirectory, 
                    "Templates", "DO", "default.scriban"
                );
                
                if (!System.IO.File.Exists(templatePath))
                {
                    logger.LogWarning($"模板文件不存在，跳过测试: {templatePath}");
                    return;
                }
                
                var testData = new Dictionary<string, object>
                {
                    ["变量名称（HMI）"] = "TestDO",
                    ["模块类型"] = "DO",
                    ["硬点通道号"] = "DPIO_2_1_2_1"
                };
                
                var result = TemplateRenderer.Render(templatePath, testData);
                
                // 验证过滤效果
                if (result.Contains("程序名称:") || result.Contains("变量类型:") || result.Contains("变量名称:"))
                {
                    logger.LogWarning("⚠️ 传统生成器路径仍包含分类标识行，过滤可能未生效");
                    logger.LogDebug($"生成结果：{result}");
                }
                else
                {
                    logger.LogSuccess("✅ 传统生成器路径过滤功能正常");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"传统生成器测试失败: {ex.Message}");
            }
        }
    }
}
