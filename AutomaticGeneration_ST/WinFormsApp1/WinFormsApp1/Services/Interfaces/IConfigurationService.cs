using AutomaticGeneration_ST.Models;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    public interface IConfigurationService
    {
        /// <summary>
        /// 加载并提供模板映射配置。
        /// </summary>
        /// <param name="configFilePath">配置文件的路径。</param>
        /// <returns>一个包含所有映射关系的强类型对象。</returns>
        TemplateMapping LoadTemplateMappings(string configFilePath);
    }
}