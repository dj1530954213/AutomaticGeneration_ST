using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// DO类型脚本匹配器
    /// </summary>
    public class DoScriptMatcher : IScriptCategoryMatcher
    {
        /// <summary>
        /// 支持的分类类型
        /// </summary>
        public ScriptCategory SupportedCategory => ScriptCategory.DO;
        
        /// <summary>
        /// 匹配优先级
        /// </summary>
        public int Priority => 8; // 优先级略低于AI/AO
        
        /// <summary>
        /// DO类型关键字模式
        /// </summary>
        private readonly List<KeywordPattern> _doKeywordPatterns = new List<KeywordPattern>
        {
            // 程序名称匹配
            new KeywordPattern("程序名称:DO_MAPPING", 95, KeywordType.ProgramName),
            new KeywordPattern(@"程序名称\s*:\s*DO_MAPPING", 95, KeywordType.ProgramName, true),
            
            // 注释匹配
            new KeywordPattern("(* DO点位:", 85, KeywordType.Comment),
            new KeywordPattern(@"\(\*\s*DO点位\s*:", 85, KeywordType.Comment, true),
            
            // DO特有的赋值模式：地址 := 变量（与DI相反）
            new KeywordPattern(@"%[QM]X\d+\s*:=\s*\w+", 80, KeywordType.Parameter, true), // 数字量输出赋值
            new KeywordPattern(@"%[QM]\d+\.\d+\s*:=\s*\w+", 80, KeywordType.Parameter, true), // 位地址赋值
            
            // 一般的DO赋值模式
            new KeywordPattern("%QX", 70, KeywordType.Parameter),
            new KeywordPattern("%MX", 70, KeywordType.Parameter),
            new KeywordPattern(@"%[QM]X\d+\s*:=", 65, KeywordType.Parameter, true),
            
            // 数字量输出相关关键字
            new KeywordPattern("数字量输出", 60, KeywordType.Comment),
            new KeywordPattern("digital output", 55, KeywordType.Comment),
            new KeywordPattern("DO", 45, KeywordType.Comment), // 低分，因为太普通
            
            // BOOL类型相关（DO通常也是BOOL类型）
            new KeywordPattern(": BOOL", 40, KeywordType.Parameter),
            new KeywordPattern(@"\w+\s*:\s*BOOL", 40, KeywordType.Parameter, true),
            
            // 控制输出相关
            new KeywordPattern("控制", 35, KeywordType.Comment),
            new KeywordPattern("输出", 35, KeywordType.Comment),
            new KeywordPattern("control", 30, KeywordType.Comment),
            new KeywordPattern("output", 30, KeywordType.Comment),
            
            // 执行机构相关（对DO常见）
            new KeywordPattern("阀门", 30, KeywordType.Comment),
            new KeywordPattern("泵", 30, KeywordType.Comment),
            new KeywordPattern("valve", 25, KeywordType.Comment),
            new KeywordPattern("pump", 25, KeywordType.Comment)
        };
        
        /// <summary>
        /// 判断脚本内容是否匹配DO分类
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
            foreach (var pattern in _doKeywordPatterns)
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
                        var bonusScore = Math.Min(matches.Count - 1, 2) * 3; // DO的额外加分较少
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
            if (matchedKeywords.Any(k => k.Contains("DO_MAPPING", StringComparison.OrdinalIgnoreCase)))
            {
                return MatchResult.Success(Math.Max(totalScore, 95), matchedKeywords, 
                    $"匹配到DO核心关键字：{string.Join(", ", matchReasons)}");
            }
            
            // 检查是否有明确的DO赋值模式
            if (matchedKeywords.Any(k => k.Contains("%QX", StringComparison.OrdinalIgnoreCase) || 
                                       k.Contains("%MX", StringComparison.OrdinalIgnoreCase)))
            {
                return MatchResult.Success(Math.Max(totalScore, 80), matchedKeywords, 
                    $"匹配到DO赋值模式：{string.Join(", ", matchReasons)}");
            }
            
            // 计算置信度分数（最高100分）
            int confidenceScore = Math.Min(totalScore, 100);
            
            // 判断是否超过阈值
            if (confidenceScore >= 50) // DO类型的阈值较低，因为相对简单
            {
                return MatchResult.Success(confidenceScore, matchedKeywords, 
                    $"匹配到{matchReasons.Count}个DO特征：{string.Join(", ", matchReasons)}");
            }
            
            return MatchResult.Failure($"不满足DO类型的匹配条件，置信度分数: {confidenceScore}");
        }
    }
}
