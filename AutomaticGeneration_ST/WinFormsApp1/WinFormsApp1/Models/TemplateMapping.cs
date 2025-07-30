using System.Collections.Generic;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 强类型表示 template-mapping.json 文件的内容。
    /// </summary>
    public class TemplateMapping
    {
        /// <summary>
        /// 存储从逻辑模板名到Scriban文件名的映射关系。
        /// Key: 来自Excel的模板名 (例如 "MOV_CTRL")
        /// Value: 物理模板文件名 (例如 "StandardValve.scriban")
        /// </summary>
        public Dictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>();
    }
}