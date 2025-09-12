// NEED DELETE: Legacy script category matcher (DI). Not used by active flow.
// Reason: `ScriptClassificationService` does not register matchers currently, and categorized
// export paths in `Form1` are commented out. No active references.
// 说明：脚本分类匹配器（DI）属遗留实现，当前未被注册或调用，UI分类导出未启用，可安全删除。
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// DI类型脚本匹配器
    /// </summary>
    public class DiScriptMatcher : IScriptCategoryMatcher
    {
        /// <summary>
        /// 支持的分类类型
        /// </summary>
        public ScriptCategory SupportedCategory => ScriptCategory.DI;
        
        /// <summary>
        /// 匹配优先级
        /// </summary>
        public int Priority => 8; // 优先级略低于AI/AO
        
        /// <summary>
        /// DI类型关键字模式
        /// </summary>
        private readonly List<KeywordPattern> _diKeywordPatterns = new List<KeywordPattern>
        {
            // 程序名称匹配
            new KeywordPattern("程序名称:DI_MAPPING", 95, KeywordType.ProgramName),
            new KeywordPattern(@"程序名称\s*:\s*DI_MAPPING", 95, KeywordType.ProgramName, true),
            
            // 注释匹配
            new KeywordPattern("(* DI点位:", 85, KeywordType.Comment),
            new KeywordPattern(@"\(\*\s*DI点位\s*:", 85, KeywordType.Comment, true),
            
            // DI特有的赋值模式：变量 := 地址
            new KeywordPattern(@"\w+\s*:=\s*%[IM]X\d+", 80, KeywordType.Parameter, true), // 数字量输入赋值
            new KeywordPattern(@"\w+\s*:=\s*%[IM]\d+\.\d+", 80, KeywordType.Parameter, true), // 位地址赋值
            
            // 一般的DI赋值模式
            new KeywordPattern(":= %IX", 70, KeywordType.Parameter),
            new KeywordPattern(":= %MX", 70, KeywordType.Parameter),
            new KeywordPattern(@"\w+\s*:=\s*%[IM]X", 65, KeywordType.Parameter, true),
            
            // 数字量输入相关关键字
            new KeywordPattern("数字量输入", 60, KeywordType.Comment),
            new KeywordPattern("digital input", 55, KeywordType.Comment),
            new KeywordPattern("DI", 45, KeywordType.Comment), // 低分，因为太普通
            
            // BOOL类型相关（DI通常是BOOL类型）
            new KeywordPattern(": BOOL", 40, KeywordType.Parameter),
            new KeywordPattern(@"\w+\s*:\s*BOOL", 40, KeywordType.Parameter, true),
            
            // 开关状态相关
            new KeywordPattern("开关", 35, KeywordType.Comment),
            new KeywordPattern("状态", 30, KeywordType.Comment),
            new KeywordPattern("switch", 30, KeywordType.Comment),
            new KeywordPattern("status", 30, KeywordType.Comment)
        };
        
        /// <summary>
        /// 判断脚本内容是否匹配DI分类
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
            foreach (var pattern in _diKeywordPatterns)
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
                        var bonusScore = Math.Min(matches.Count - 1, 2) * 3; // DI的额外加分较少
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
            if (matchedKeywords.Any(k => k.Contains("DI_MAPPING", StringComparison.OrdinalIgnoreCase)))
            {
                return MatchResult.Success(Math.Max(totalScore, 95), matchedKeywords, 
                    $"匹配到DI核心关键字：{string.Join(", ", matchReasons)}");
            }
            
            // 检查是否有明确的DI赋值模式
            if (matchedKeywords.Any(k => k.Contains("%IX", StringComparison.OrdinalIgnoreCase) || 
                                       k.Contains("%MX", StringComparison.OrdinalIgnoreCase)))
            {
                return MatchResult.Success(Math.Max(totalScore, 80), matchedKeywords, 
                    $"匹配到DI赋值模式：{string.Join(", ", matchReasons)}");
            }
            
            // 计算置信度分数（最高100分）
            int confidenceScore = Math.Min(totalScore, 100);
            
            // 判断是否超过阈值
            if (confidenceScore >= 50) // DI类型的阈值较低，因为相对简单
            {
                return MatchResult.Success(confidenceScore, matchedKeywords, 
                    $"匹配到{matchReasons.Count}个DI特征：{string.Join(", ", matchReasons)}");
            }
            
            return MatchResult.Failure($"不满足DI类型的匹配条件，置信度分数: {confidenceScore}");
        }
    }
    
}
