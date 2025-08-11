using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 工作表定位服务接口 - 提供灵活的工作表名称查找和匹配功能
    /// </summary>
    public interface IWorksheetLocatorService
    {
        /// <summary>
        /// 根据预期的工作表名称查找实际存在的工作表名称
        /// 支持模糊匹配、忽略大小写、空格等
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="expectedName">预期的工作表名称</param>
        /// <returns>匹配的工作表名称，如果未找到则返回null</returns>
        string LocateWorksheet(string filePath, string expectedName);

        /// <summary>
        /// 根据工作表别名列表查找实际存在的工作表名称
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="aliases">工作表别名列表，按优先级排序</param>
        /// <returns>匹配的工作表名称，如果未找到则返回null</returns>
        string LocateWorksheetByAliases(string filePath, IEnumerable<string> aliases);

        /// <summary>
        /// 验证指定工作表是否存在（支持模糊匹配）
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="expectedName">预期的工作表名称</param>
        /// <returns>验证结果</returns>
        WorksheetValidationResult ValidateWorksheet(string filePath, string expectedName);

        /// <summary>
        /// 获取所有可用的工作表名称
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>工作表名称列表</returns>
        List<string> GetAvailableWorksheetNames(string filePath);
    }

    /// <summary>
    /// 工作表验证结果
    /// </summary>
    public class WorksheetValidationResult
    {
        /// <summary>
        /// 是否找到匹配的工作表
        /// </summary>
        public bool IsFound { get; set; }

        /// <summary>
        /// 实际找到的工作表名称
        /// </summary>
        public string ActualName { get; set; }

        /// <summary>
        /// 匹配类型（精确匹配、模糊匹配等）
        /// </summary>
        public WorksheetMatchType MatchType { get; set; }

        /// <summary>
        /// 错误信息（如果未找到）
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 可用的工作表名称列表（用于错误提示）
        /// </summary>
        public List<string> AvailableWorksheets { get; set; } = new List<string>();
    }

    /// <summary>
    /// 工作表匹配类型
    /// </summary>
    public enum WorksheetMatchType
    {
        /// <summary>
        /// 精确匹配
        /// </summary>
        Exact,

        /// <summary>
        /// 忽略大小写匹配
        /// </summary>
        IgnoreCase,

        /// <summary>
        /// 忽略空格和特殊字符匹配
        /// </summary>
        IgnoreWhitespace,

        /// <summary>
        /// 模糊匹配（包含关系）
        /// </summary>
        Fuzzy,

        /// <summary>
        /// 别名匹配
        /// </summary>
        Alias,

        /// <summary>
        /// 未找到
        /// </summary>
        NotFound
    }
}