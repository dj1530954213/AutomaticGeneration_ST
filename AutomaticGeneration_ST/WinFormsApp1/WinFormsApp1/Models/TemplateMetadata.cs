using System;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 模板元数据 - 存储从.scriban和.TXT文件中提取的模板信息
    /// </summary>
    public class TemplateMetadata
    {
        /// <summary>
        /// 程序名称（从scriban文件第1行"程序名称:"后提取）
        /// </summary>
        public string ProgramName { get; set; } = string.Empty;

        /// <summary>
        /// 变量类型（从scriban文件第2行"变量类型:"后提取）
        /// </summary>
        public string VariableType { get; set; } = string.Empty;

        /// <summary>
        /// 初始化值（从同名.TXT文件读取）
        /// </summary>
        public string InitializationValue { get; set; } = string.Empty;

        /// <summary>
        /// 是否存在同名.TXT文件
        /// </summary>
        public bool HasTxtFile { get; set; }

        /// <summary>
        /// 模板文件路径
        /// </summary>
        public string TemplatePath { get; set; } = string.Empty;

        /// <summary>
        /// TXT文件路径
        /// </summary>
        public string TxtFilePath { get; set; } = string.Empty;
    }
}