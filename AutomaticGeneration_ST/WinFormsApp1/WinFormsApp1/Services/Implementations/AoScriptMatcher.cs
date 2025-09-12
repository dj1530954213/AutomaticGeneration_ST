// NEED DELETE: Legacy script category matcher (AO). Not used by active flow.
// Reason: `ScriptClassificationService` initializes without registering matchers, and
// Form1's categorized export entry points are commented out. No runtime references.
// 说明：脚本分类匹配器（AO）为遗留实现，当前未注册使用，UI分类导出未启用，可安全删除。
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// AO类型脚本匹配器
    /// </summary>
    public class AoScriptMatcher : IScriptCategoryMatcher
    {
        /// <summary>
        /// 支持的分类类型
        /// </summary>
        public ScriptCategory SupportedCategory => ScriptCategory.AO_CTRL;
        
        /// <summary>
        /// 匹配优先级
        /// </summary>
        public int Priority => 10;
        
        /// <summary>
        /// AO类型关键字模式
        /// </summary>
        private readonly List<KeywordPattern> _aoKeywordPatterns = new List<KeywordPattern>
        {
            // 程序名称匹配
            new KeywordPattern("程序名称:AO_CONVERT", 95, KeywordType.ProgramName),
            new KeywordPattern("程序名称:AO_CTRL", 95, KeywordType.ProgramName),
            new KeywordPattern(@"程序名称\s*:\s*AO_CONVERT", 95, KeywordType.ProgramName, true),
            new KeywordPattern(@"程序名称\s*:\s*AO_CTRL", 95, KeywordType.ProgramName, true),
            
            // 函数关键字匹配
            new KeywordPattern("ENGIN_HEX_", 90, KeywordType.Function),
            new KeywordPattern(@"ENGIN_HEX_\w+\(", 90, KeywordType.Function, true),
            
            // 注释匹配
            new KeywordPattern("(* AO点位:", 85, KeywordType.Comment),
            new KeywordPattern(@"\(\*\s*AO点位\s*:", 85, KeywordType.Comment, true),
            
            // 变量类型匹配
            new KeywordPattern("变量类型:AO_ALARM", 80, KeywordType.VariableType),
            new KeywordPattern(@"变量类型\s*:\s*AO_ALARM", 80, KeywordType.VariableType, true),
            
            // AO特定参数（模拟量输出相关）
            new KeywordPattern("AV:=", 70, KeywordType.Parameter), // 模拟值输入
            new KeywordPattern("MU:=", 70, KeywordType.Parameter), // 上限
            new KeywordPattern("MD:=", 70, KeywordType.Parameter), // 下限
            new KeywordPattern("WU:=", 65, KeywordType.Parameter), // 原始值上限
            new KeywordPattern("WD:=", 65, KeywordType.Parameter), // 原始值下限
            new KeywordPattern("WH=>=", 60, KeywordType.Output), // 输出地址
            
            // AO特定模式（针对ENGIN_HEX函数）
            new KeywordPattern(@"\bAV\s*:=", 60, KeywordType.Parameter, true),
            new KeywordPattern(@"\bMU\s*:=", 60, KeywordType.Parameter, true),
            new KeywordPattern(@"\bMD\s*:=", 60, KeywordType.Parameter, true),
            new KeywordPattern(@"\bWH\s*=>\s*%[QM]W\d+", 70, KeywordType.Output, true), // 输出地址模式
            
            // 通用AO相关关键字
            new KeywordPattern("模拟量输出", 50, KeywordType.Comment),
            new KeywordPattern("analog output", 45, KeywordType.Comment)
        };
        
        /// <summary>
        /// 判断脚本内容是否匹配AO分类
        /// </summary>
        /// <param name="scriptContent">脚本内容</param>
        /// <returns>匹配结果</returns>
        public MatchResult IsMatch(string scriptContent)
        {
            if (string.IsNullOrWhiteSpace(scriptContent))
            {
                return MatchResult.Failure("脚本内容为空");
            }
            
            var matchedKeywords = new List<string>();
            int totalScore = 0;
            var matchReasons = new List<string>();
            
            // 检查每个关键字模式
            foreach (var pattern in _aoKeywordPatterns)
            {
                bool isMatched = false;
                
                if (pattern.IsRegex)
                {
                    var regex = new Regex(pattern.Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    var matches = regex.Matches(scriptContent);
                    if (matches.Count > 0)
                    {
                        isMatched = true;
                        // 多次匹配可以适当提高分数
                        var bonusScore = Math.Min(matches.Count - 1, 3) * 5; // 最多额外增加15分
                        totalScore += pattern.Score + bonusScore;
                        
                        foreach (Match match in matches)
                        {
                            matchedKeywords.Add(match.Value);
                        }
                    }
                }
                else
                {
                    if (scriptContent.Contains(pattern.Pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        isMatched = true;
                        totalScore += pattern.Score;
                        matchedKeywords.Add(pattern.Pattern);
                    }
                }
                
                if (isMatched)
                {
                    matchReasons.Add($"匹配{pattern.Type.GetDescription()}: {pattern.Pattern}");
                }
            }
            
            // 特殊规则：如果匹配到高优先级关键字，直接返回高分
            if (matchedKeywords.Any(k => k.Contains("AO_CONVERT", StringComparison.OrdinalIgnoreCase) ||
                                       k.Contains("AO_CTRL", StringComparison.OrdinalIgnoreCase) ||
                                       k.Contains("ENGIN_HEX_", StringComparison.OrdinalIgnoreCase)))
            {
                return MatchResult.Success(Math.Max(totalScore, 90), matchedKeywords, 
                    $"匹配到AO核心关键字：{string.Join(", ", matchReasons)}");
            }
            
            // 计算置信度分数（最高100分）
            int confidenceScore = Math.Min(totalScore, 100);
            
            // 判断是否超过阈值
            if (confidenceScore >= 60) // AO类型的阈值设置为60
            {
                return MatchResult.Success(confidenceScore, matchedKeywords, 
                    $"匹配到{matchReasons.Count}个AO特征：{string.Join(", ", matchReasons)}");
            }
            
            return MatchResult.Failure($"不满足AO类型的匹配条件，置信度分数: {confidenceScore}");
        }
    }
}
