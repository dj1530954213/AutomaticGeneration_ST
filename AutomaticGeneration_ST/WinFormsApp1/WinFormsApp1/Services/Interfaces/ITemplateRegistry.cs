using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 模板注册表接口 - 管理模板名称到模板文件的映射关系
    /// </summary>
    public interface ITemplateRegistry
    {
        /// <summary>
        /// 注册一个模板映射
        /// </summary>
        /// <param name="templateName">模板名称（如ESDV_CTRL）</param>
        /// <param name="templateFile">模板文件路径</param>
        void RegisterTemplate(string templateName, string templateFile);

        /// <summary>
        /// 获取模板文件路径
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <returns>模板文件路径，如果不存在返回null</returns>
        string GetTemplateFile(string templateName);

        /// <summary>
        /// 检查模板是否存在
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <returns>是否存在</returns>
        bool HasTemplate(string templateName);

        /// <summary>
        /// 获取所有已注册的模板名称
        /// </summary>
        /// <returns>模板名称列表</returns>
        IEnumerable<string> GetAllTemplateNames();

        /// <summary>
        /// 从配置文件加载模板映射
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        void LoadFromConfig(string configPath);
    }
}