using AutomaticGeneration_ST.Services.Interfaces; // 引入 DataContext 所在的命名空间
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Generation.Interfaces
{
    /// <summary>
    /// 定义了所有通讯相关代码生成器的通用接口。
    /// </summary>
    public interface ICommunicationGenerator
    {
        /// <summary>
        /// 根据数据上下文生成通讯相关的ST代码。
        /// </summary>
        /// <param name="context">包含所有设备和点位信息的全局数据上下文。</param>
        /// <returns>一个GenerationResult对象的列表，每个对象代表一个通讯相关的代码文件。</returns>
        List<GenerationResult> Generate(DataContext context);
    }
}