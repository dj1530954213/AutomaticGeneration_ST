using System;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 已分类的ST脚本对象
    /// </summary>
    public class CategorizedScript
    {
        /// <summary>
        /// 脚本内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 脚本分类
        /// </summary>
        public ScriptCategory Category { get; set; } = ScriptCategory.UNKNOWN;
        
        /// <summary>
        /// 相关的设备标签（可选）
        /// </summary>
        public string? DeviceTag { get; set; }
        
        /// <summary>
        /// 相关的点位名称列表
        /// </summary>
        public List<string> PointNames { get; set; } = new List<string>();
        
        /// <summary>
        /// 分类的置信度分数（0-100）
        /// </summary>
        public int ConfidenceScore { get; set; } = 0;
        
        /// <summary>
        /// 匹配到的关键字（用于调试）
        /// </summary>
        public List<string> MatchedKeywords { get; set; } = new List<string>();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="content">脚本内容</param>
        /// <param name="category">分类</param>
        public CategorizedScript(string content, ScriptCategory category)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Category = category;
        }
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CategorizedScript() { }
        
        /// <summary>
        /// 获取脚本的摘要信息
        /// </summary>
        /// <returns>摘要信息</returns>
        public string GetSummary()
        {
            var contentPreview = Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content;
            return $"[{Category.GetDescription()}] {contentPreview}";
        }
        
        /// <summary>
        /// 检查是否为空脚本
        /// </summary>
        /// <returns>是否为空</returns>
        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(Content);
        }
    }
}
