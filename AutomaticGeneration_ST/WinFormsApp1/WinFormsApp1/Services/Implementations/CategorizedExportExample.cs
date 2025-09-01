////NEED DELETE
//// REASON: This is an example class demonstrating the use of the classified export function and is not used in the main program logic.

//using AutomaticGeneration_ST.Models;
//using AutomaticGeneration_ST.Services.Interfaces;
//using AutomaticGeneration_ST.Services.Implementations;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using WinFormsApp1;

//namespace AutomaticGeneration_ST.Services.Implementations
//{
//    /// <summary>
//    /// 分类导出功能的使用示例
//    /// </summary>
//    public class CategorizedExportExample
//    {
//        /// <summary>
//        /// 演示如何使用分类导出功能
//        /// </summary>
//        /// <param name="outputDirectory">输出目录</param>
//        /// <returns>演示结果</returns>
//        public static string DemonstrateClassifiedExport(string outputDirectory)
//        {
//            try
//            {
//                // 初始化日志服务（简化版）
//                var logger = LogService.Instance;
                
//                // 创建分类器和导出服务
//                var classifier = new ScriptClassificationService();
//                var exportService = new CategorizedFileExportService(classifier);
                
//                // 准备测试数据：模拟不同类型的ST脚本
//                var testScripts = CreateTestScripts();
                
//                // 执行分类
//                var categorizedScripts = classifier.ClassifyScripts(testScripts);
                
//                // 配置导出参数
//                var config = ExportConfiguration.CreateDefault(outputDirectory);
//                config.IncludeTimestamp = false; // 为了演示，不包含时间戳
                
//                // 执行导出
//                var exportResult = exportService.ExportScriptsByCategory(categorizedScripts, config);
                
//                // 返回结果摘要
//                return GenerateResultSummary(exportResult, categorizedScripts);
//            }
//            catch (Exception ex)
//            {
//                return $"演示过程中出错: {ex.Message}";
//            }
//        }
        
//        /// <summary>
//        /// 创建测试用的ST脚本
//        /// </summary>
//        /// <returns>测试脚本列表</returns>
//        private static List<string> CreateTestScripts()
//        {
//            return new List<string>
//            {
//                // AI类型脚本
//                "程序名称:AI_CONVERT\n" +
//                "(* AI点位: TEMP_01 - 反应器温度 *)\n" +
//                "AI_ALARM_TEMP_01(\n" +
//                "    IN:=%IW100,\n" +
//                "    ENG_MAX:=200.0,\n" +
//                "    ENG_MIN:=0.0,\n" +
//                "    HH_LIMIT:=180.0,\n" +
//                "    H_LIMIT:=160.0,\n" +
//                "    L_LIMIT:=20.0,\n" +
//                "    LL_LIMIT:=10.0,\n" +
//                "    OUT=>TEMP_01,\n" +
//                "    HH_ALARM=>TEMP_01_HH,\n" +
//                "    H_ALARM=>TEMP_01_H,\n" +
//                "    L_ALARM=>TEMP_01_L,\n" +
//                "    LL_ALARM=>TEMP_01_LL\n" +
//                ");",
                
//                // AO类型脚本
//                "程序名称:AO_CTRL\n" +
//                "(* AO点位: FV_001 - 流量控制阀 *)\n" +
//                "ENGIN_HEX_FV_001(\n" +
//                "    AV:=FV_001_SP,\n" +
//                "    MU:=100.0,\n" +
//                "    MD:=0.0,\n" +
//                "    WU:=65535,\n" +
//                "    WD:=0,\n" +
//                "    WH=>%QW200\n" +
//                ");",
                
//                // DI类型脚本
//                "程序名称:DI_MAPPING\n" +
//                "(* DI点位: LS_001 - 液位开关 *)\n" +
//                "LS_001 := %IX0.0;\n" +
//                "(* DI点位: PS_001 - 压力开关 *)\n" +
//                "PS_001 := %IX0.1;",
                
//                // DO类型脚本
//                "程序名称:DO_MAPPING\n" +
//                "(* DO点位: XV_001 - 电磁阀 *)\n" +
//                "%QX0.0 := XV_001;\n" +
//                "(* DO点位: M_001 - 电机 *)\n" +
//                "%QX0.1 := M_001;",
                
//                // 混合类型脚本（包含多种特征）
//                "(* 混合类型程序 *)\n" +
//                "AI_ALARM_TEMP_02(IN:=%IW102, ENG_MAX:=150.0);\n" +
//                "XV_002 := %QX1.0; // DO输出",
                
//                // 未知类型脚本
//                "// 这是一个普通的注释\n" +
//                "VAR\n" +
//                "  test_var : INT := 100;\n" +
//                "END_VAR"
//            };
//        }
        
//        /// <summary>
//        /// 生成结果摘要
//        /// </summary>
//        /// <param name="exportResult">导出结果</param>
//        /// <param name="categorizedScripts">已分类的脚本</param>
//        /// <returns>结果摘要</returns>
//        private static string GenerateResultSummary(ExportResult exportResult, List<CategorizedScript> categorizedScripts)
//        {
//            var summary = new System.Text.StringBuilder();
            
//            summary.AppendLine("✨ ST脚本分类导出演示结果 ✨");
//            summary.AppendLine();
            
//            // 总体结果
//            summary.AppendLine($"🔍 处理结果: {(exportResult.IsSuccess ? "✅ 成功" : "❌ 失败")}");
//            summary.AppendLine($"⏱️  耗时: {exportResult.Duration.TotalMilliseconds:F0} 毫秒");
//            summary.AppendLine();
            
//            // 分类统计
//            summary.AppendLine("📈 分类统计:");
//            var categoryStats = new Dictionary<ScriptCategory, int>();
//            foreach (var script in categorizedScripts)
//            {
//                categoryStats[script.Category] = categoryStats.GetValueOrDefault(script.Category, 0) + 1;
//            }
            
//            foreach (var stat in categoryStats)
//            {
//                var icon = GetCategoryIcon(stat.Key);
//                summary.AppendLine($"  {icon} {stat.Key.GetDescription()}: {stat.Value} 个");
//            }
//            summary.AppendLine();
            
//            // 导出文件结果
//            if (exportResult.IsSuccess)
//            {
//                summary.AppendLine("💾 导出文件:");
//                foreach (var fileResult in exportResult.FileResults)
//                {
//                    if (fileResult.IsSuccess)
//                    {
//                        var icon = GetCategoryIcon(fileResult.Category);
//                        summary.AppendLine($"  {icon} {fileResult.Category.GetFileName()}.txt - {fileResult.ScriptCount} 个脚本 ({fileResult.FileSizeFormatted})");
//                    }
//                }
                
//                summary.AppendLine();
//                summary.AppendLine($"📁 输出目录: {exportResult.FileResults.FirstOrDefault()?.FilePath.Replace(Path.GetFileName(exportResult.FileResults.FirstOrDefault()?.FilePath ?? ""), "")}");
//            }
//            else
//            {
//                summary.AppendLine($"❌ 导出失败: {exportResult.ErrorMessage}");
//            }
            
//            // 置信度分析
//            summary.AppendLine();
//            summary.AppendLine("🎯 置信度分析:");
//            var highConfidence = categorizedScripts.Count(s => s.ConfidenceScore >= 80);
//            var mediumConfidence = categorizedScripts.Count(s => s.ConfidenceScore >= 60 && s.ConfidenceScore < 80);
//            var lowConfidence = categorizedScripts.Count(s => s.ConfidenceScore > 0 && s.ConfidenceScore < 60);
//            var unknown = categorizedScripts.Count(s => s.ConfidenceScore == 0);
            
//            summary.AppendLine($"  🔥 高置信度 (>=80%): {highConfidence} 个");
//            summary.AppendLine($"  🔶 中置信度 (60-79%): {mediumConfidence} 个");
//            summary.AppendLine($"  🔵 低置信度 (1-59%): {lowConfidence} 个");
//            summary.AppendLine($"  ❓ 未知类型 (0%): {unknown} 个");
            
//            if (exportResult.IsSuccess)
//            {
//                summary.AppendLine();
//                summary.AppendLine("✅ 演示完成！现在您可以在输出目录中查看生成的分类文件。");
//            }
            
//            return summary.ToString();
//        }
        
//        /// <summary>
//        /// 获取分类对应的图标
//        /// </summary>
//        /// <param name="category">分类</param>
//        /// <returns>图标</returns>
//        private static string GetCategoryIcon(ScriptCategory category)
//        {
//            return category switch
//            {
//                ScriptCategory.AI_CONVERT => "🌡️", // 温度计（代表模拟量输入）
//                ScriptCategory.AO_CTRL => "🟠",     // 黄色圆圈（代表模拟量输出）
//                ScriptCategory.DI => "🟢",           // 绿色圆圈（代表数字量输入）
//                ScriptCategory.DO => "🔴",           // 红色圆圈（代表数字量输出）
//                _ => "❓"                      // 问号（未知类型）
//            };
//        }
//    }
//}
