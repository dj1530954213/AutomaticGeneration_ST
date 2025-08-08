using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// AI类型脚本匹配器
    /// </summary>
    public class AiScriptMatcher : IScriptCategoryMatcher
    {
        /// <summary>
        /// 支持的分类类型
        /// </summary>
        public ScriptCategory SupportedCategory => ScriptCategory.AI_CONVERT;
        
        /// <summary>
        /// 匹配优先级
        /// </summary>
        public int Priority => 10;
        
        /// <summary>
        /// AI类型关键字模式
        /// </summary>
        private readonly List<KeywordPattern> _aiKeywordPatterns = new List<KeywordPattern>
        {
            // 程序名称匹配
            new KeywordPattern("程序名称:AI_CONVERT", 95, KeywordType.ProgramName),
            new KeywordPattern(@"程序名称\s*:\s*AI_CONVERT", 95, KeywordType.ProgramName, true),
            
            // 函数关键字匹配
            new KeywordPattern("AI_ALARM_", 90, KeywordType.Function),
            new KeywordPattern(@"AI_ALARM_\w+\(", 90, KeywordType.Function, true),
            
            // 注释匹配
            new KeywordPattern("(* AI点位:", 85, KeywordType.Comment),
            new KeywordPattern(@"\(\*\s*AI点位\s*:", 85, KeywordType.Comment, true),
            
            // 变量类型匹配
            new KeywordPattern("变量类型:AI_ALARM", 80, KeywordType.VariableType),
            new KeywordPattern(@"变量类型\s*:\s*AI_ALARM", 80, KeywordType.VariableType, true),
            
            // AI特定参数
            new KeywordPattern("ENG_MAX", 70, KeywordType.Parameter),
            new KeywordPattern("ENG_MIN", 70, KeywordType.Parameter),
            new KeywordPattern("HH_LIMIT", 60, KeywordType.Parameter),
            new KeywordPattern("H_LIMIT", 60, KeywordType.Parameter),
            new KeywordPattern("L_LIMIT", 60, KeywordType.Parameter),
            new KeywordPattern("LL_LIMIT", 60, KeywordType.Parameter),
            
            // AI特定输出
            new KeywordPattern("HH_ALARM", 50, KeywordType.Output),
            new KeywordPattern("H_ALARM", 50, KeywordType.Output),
            new KeywordPattern("L_ALARM", 50, KeywordType.Output),
            new KeywordPattern("LL_ALARM", 50, KeywordType.Output)
        };
        
        /// <summary>
        /// 判断脚本内容是否匹配AI分类
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
            foreach (var pattern in _aiKeywordPatterns)
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
            if (matchedKeywords.Any(k => k.Contains("AI_CONVERT", StringComparison.OrdinalIgnoreCase) ||
                                       k.Contains("AI_ALARM_", StringComparison.OrdinalIgnoreCase)))
            {
                return MatchResult.Success(Math.Max(totalScore, 90), matchedKeywords, 
                    $"匹配到AI核心关键字：{string.Join(", ", matchReasons)}");
            }
            
            // 计算置信度分数（最高100分）
            int confidenceScore = Math.Min(totalScore, 100);
            
            // 判断是否超过阈值
            if (confidenceScore >= 60) // AI类型的阈值设置为60
            {
                return MatchResult.Success(confidenceScore, matchedKeywords, 
                    $"匹配到{matchReasons.Count}个AI特征：{string.Join(", ", matchReasons)}");
            }
            
            return MatchResult.Failure($"不满足AI类型的匹配条件，置信度分数: {confidenceScore}");
        }
    }
    
    /// <summary>
    /// 关键字模式定义
    /// </summary>
    public class KeywordPattern
    {
        /// <summary>
        /// 匹配模式
        /// </summary>
        public string Pattern { get; set; }
        
        /// <summary>
        /// 分数
        /// </summary>
        public int Score { get; set; }
        
        /// <summary>
        /// 关键字类型
        /// </summary>
        public KeywordType Type { get; set; }
        
        /// <summary>
        /// 是否为正则表达式
        /// </summary>
        public bool IsRegex { get; set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pattern">匹配模式</param>
        /// <param name="score">分数</param>
        /// <param name="type">关键字类型</param>
        /// <param name="isRegex">是否为正则表达式</param>
        public KeywordPattern(string pattern, int score, KeywordType type, bool isRegex = false)
        {
            Pattern = pattern;
            Score = score;
            Type = type;
            IsRegex = isRegex;
        }
    }
    
    /// <summary>
    /// 关键字类型
    /// </summary>
    public enum KeywordType
    {
        /// <summary>
        /// 程序名称
        /// </summary>
        ProgramName,
        
        /// <summary>
        /// 函数名
        /// </summary>
        Function,
        
        /// <summary>
        /// 注释
        /// </summary>
        Comment,
        
        /// <summary>
        /// 变量类型
        /// </summary>
        VariableType,
        
        /// <summary>
        /// 参数
        /// </summary>
        Parameter,
        
        /// <summary>
        /// 输出
        /// </summary>
        Output
    }
    
    /// <summary>
    /// 关键字类型扩展方法
    /// </summary>
    public static class KeywordTypeExtensions
    {
        /// <summary>
        /// 获取关键字类型的描述
        /// </summary>
        /// <param name="type">关键字类型</param>
        /// <returns>描述</returns>
        public static string GetDescription(this KeywordType type)
        {
            return type switch
            {
                KeywordType.ProgramName => "程序名称",
                KeywordType.Function => "函数名",
                KeywordType.Comment => "注释",
                KeywordType.VariableType => "变量类型",
                KeywordType.Parameter => "参数",
                KeywordType.Output => "输出",
                _ => "未知"
            };
        }
    }
}
