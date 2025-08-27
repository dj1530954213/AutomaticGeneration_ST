using System;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 变量表条目 - Excel点表中单行变量的数据结构
    /// </summary>
    public class VariableTableEntry
    {
        /// <summary>
        /// 程序名称（用作工作簿标题行，如"MOV_CTRL(PRG)"）
        /// </summary>
        public string ProgramName { get; set; } = string.Empty;

        /// <summary>
        /// 变量名（块入口变量名，如"MOV101_CTRL"、"AI_ALARM_TT101"）
        /// </summary>
        public string VariableName { get; set; } = string.Empty;

        /// <summary>
        /// 直接地址（Excel第2列，留空）
        /// </summary>
        public string DirectAddress { get; set; } = string.Empty;

        /// <summary>
        /// 变量说明（Excel第3列，留空）
        /// </summary>
        public string VariableDescription { get; set; } = string.Empty;

        /// <summary>
        /// 变量类型（如"AI_ALARM"、"XV_CTRL"）
        /// </summary>
        public string VariableType { get; set; } = string.Empty;

        /// <summary>
        /// 初始值（从.TXT文件读取的复杂初始化结构）
        /// </summary>
        public string InitialValue { get; set; } = string.Empty;

        /// <summary>
        /// 掉电保护（固定为FALSE）
        /// </summary>
        public string PowerFailureProtection { get; set; } = "FALSE";

        /// <summary>
        /// 可强制（默认 TRUE）
        /// </summary>
        public string ForceEnable { get; set; } = "TRUE";

        /// <summary>
        /// SOE使能（固定为FALSE）
        /// </summary>
        public string SOEEnable { get; set; } = "FALSE";
    }
}