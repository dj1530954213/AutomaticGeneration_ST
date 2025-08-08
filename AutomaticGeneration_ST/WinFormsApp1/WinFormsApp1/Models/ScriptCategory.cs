using System;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// ST脚本分类枚举
    /// </summary>
    public enum ScriptCategory
    {
        /// <summary>
        /// 模拟量输入转换程序
        /// </summary>
        AI_CONVERT = 1,
        
        /// <summary>
        /// 模拟量输出控制程序
        /// </summary>
        AO_CTRL = 2,
        
        /// <summary>
        /// 数字量输入映射程序
        /// </summary>
        DI = 3,
        
        /// <summary>
        /// 数字量输出映射程序
        /// </summary>
        DO = 4,
        
        /// <summary>
        /// 未知类型
        /// </summary>
        UNKNOWN = 0
    }
    
    /// <summary>
    /// 脚本分类扩展方法
    /// </summary>
    public static class ScriptCategoryExtensions
    {
        /// <summary>
        /// 获取分类对应的文件名
        /// </summary>
        /// <param name="category">脚本分类</param>
        /// <returns>文件名（不含扩展名）</returns>
        public static string GetFileName(this ScriptCategory category)
        {
            return category switch
            {
                ScriptCategory.AI_CONVERT => "AI_CONVERT",
                ScriptCategory.AO_CTRL => "AO_CTRL",
                ScriptCategory.DI => "DI",
                ScriptCategory.DO => "DO",
                _ => "UNKNOWN"
            };
        }
        
        /// <summary>
        /// 获取分类的中文描述
        /// </summary>
        /// <param name="category">脚本分类</param>
        /// <returns>中文描述</returns>
        public static string GetDescription(this ScriptCategory category)
        {
            return category switch
            {
                ScriptCategory.AI_CONVERT => "模拟量输入转换程序",
                ScriptCategory.AO_CTRL => "模拟量输出控制程序",
                ScriptCategory.DI => "数字量输入映射程序",
                ScriptCategory.DO => "数字量输出映射程序",
                _ => "未知类型"
            };
        }
    }
}
