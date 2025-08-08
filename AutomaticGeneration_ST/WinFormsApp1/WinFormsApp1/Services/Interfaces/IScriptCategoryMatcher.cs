using AutomaticGeneration_ST.Models;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 脚本分类匹配器接口
    /// </summary>
    public interface IScriptCategoryMatcher
    {
        /// <summary>
        /// 支持的分类类型
        /// </summary>
        ScriptCategory SupportedCategory { get; }
        
        /// <summary>
        /// 匹配优先级（数字越大优先级越高）
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// 判断脚本内容是否匹配对应的分类
        /// </summary>
        /// <param name="scriptContent">脚本内容</param>
        /// <returns>匹配结果</returns>
        MatchResult IsMatch(string scriptContent);
    }
    
    /// <summary>
    /// 匹配结果
    /// </summary>
    public class MatchResult
    {
        /// <summary>
        /// 是否匹配
        /// </summary>
        public bool IsMatch { get; set; } = false;
        
        /// <summary>
        /// 置信度分数（0-100）
        /// </summary>
        public int ConfidenceScore { get; set; } = 0;
        
        /// <summary>
        /// 匹配到的关键字列表
        /// </summary>
        public List<string> MatchedKeywords { get; set; } = new List<string>();
        
        /// <summary>
        /// 匹配原因说明
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// 创建成功匹配结果
        /// </summary>
        /// <param name="confidenceScore">置信度分数</param>
        /// <param name="matchedKeywords">匹配关键字</param>
        /// <param name="reason">匹配原因</param>
        /// <returns>匹配结果</returns>
        public static MatchResult Success(int confidenceScore, List<string> matchedKeywords, string reason = "")
        {
            return new MatchResult
            {
                IsMatch = true,
                ConfidenceScore = confidenceScore,
                MatchedKeywords = matchedKeywords ?? new List<string>(),
                Reason = reason
            };
        }
        
        /// <summary>
        /// 创建失败匹配结果
        /// </summary>
        /// <param name="reason">失败原因</param>
        /// <returns>匹配结果</returns>
        public static MatchResult Failure(string reason = "")
        {
            return new MatchResult
            {
                IsMatch = false,
                ConfidenceScore = 0,
                MatchedKeywords = new List<string>(),
                Reason = reason
            };
        }
    }
}
