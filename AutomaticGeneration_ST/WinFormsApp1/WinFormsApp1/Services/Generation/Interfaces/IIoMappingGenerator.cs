using AutomaticGeneration_ST.Models;
using Scriban;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Generation.Interfaces
{
    /// <summary>
    /// 定义了IO映射渲染器的标准接口。其职责是使用给定的模板渲染给定的点位列表。
    /// </summary>
    public interface IIoMappingGenerator
    {
        /// <summary>
        /// 使用指定的Scriban模板，为指定的一组点位生成ST代码。
        /// </summary>
        /// <param name="moduleType">当前处理的模块类型 (例如 "AI", "DI")。</param>
        /// <param name="pointsInGroup">属于该模块类型的所有点位的列表。</param>
        /// <param name="template">已加载并解析的、用于当前模块类型的Scriban模板。</param>
        /// <returns>包含单个文件内容的GenerationResult对象。</returns>
        GenerationResult Generate(string moduleType, IEnumerable<Models.Point> pointsInGroup, Template template);
    }
}