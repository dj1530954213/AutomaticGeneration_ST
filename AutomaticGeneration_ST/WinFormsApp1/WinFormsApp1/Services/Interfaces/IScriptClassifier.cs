using AutomaticGeneration_ST.Models;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// ST脚本分类器接口
    /// </summary>
    public interface IScriptClassifier
    {
        /// <summary>
        /// 对单个ST脚本进行分类
        /// </summary>
        /// <param name="scriptContent">脚本内容</param>
        /// <returns>分类结果</returns>
        CategorizedScript ClassifyScript(string scriptContent);
        
        /// <summary>
        /// 批量对ST脚本进行分类
        /// </summary>
        /// <param name="scripts">脚本内容列表</param>
        /// <returns>分类结果列表</returns>
        List<CategorizedScript> ClassifyScripts(List<string> scripts);
        
        /// <summary>
        /// 批量对设备的ST脚本进行分类
        /// </summary>
        /// <param name="devices">设备列表</param>
        /// <returns>分类结果列表</returns>
        List<CategorizedScript> ClassifyDeviceScripts(List<Device> devices);
        
        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        /// <param name="categorizedScripts">已分类的脚本列表</param>
        /// <returns>统计信息</returns>
        Dictionary<ScriptCategory, int> GetClassificationStatistics(List<CategorizedScript> categorizedScripts);
    }
}
