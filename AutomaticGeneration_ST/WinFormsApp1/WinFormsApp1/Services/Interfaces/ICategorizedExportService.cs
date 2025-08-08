using AutomaticGeneration_ST.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 分类导出服务接口
    /// </summary>
    public interface ICategorizedExportService
    {
        /// <summary>
        /// 按分类导出脚本到独立文件
        /// </summary>
        /// <param name="categorizedScripts">已分类的脚本列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>导出结果</returns>
        ExportResult ExportScriptsByCategory(List<CategorizedScript> categorizedScripts, ExportConfiguration configuration);
        
        /// <summary>
        /// 异步按分类导出脚本到独立文件
        /// </summary>
        /// <param name="categorizedScripts">已分类的脚本列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>导出结果</returns>
        Task<ExportResult> ExportScriptsByCategoryAsync(List<CategorizedScript> categorizedScripts, ExportConfiguration configuration);
        
        /// <summary>
        /// 从设备列表生成并导出分类脚本
        /// </summary>
        /// <param name="devices">设备列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>导出结果</returns>
        ExportResult GenerateAndExportFromDevices(List<Device> devices, ExportConfiguration configuration);
        
        /// <summary>
        /// 获取支持的分类列表
        /// </summary>
        /// <returns>支持的分类列表</returns>
        List<ScriptCategory> GetSupportedCategories();
        
        /// <summary>
        /// 验证导出配置
        /// </summary>
        /// <param name="configuration">导出配置</param>
        /// <returns>验证结果</returns>
        bool ValidateConfiguration(ExportConfiguration configuration, out string errorMessage);
    }
}
