using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Interfaces;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Generation.Implementations
{
    /// <summary>
    /// 一个临时的、占位符性质的通讯生成器实现。
    /// 在正式功能开发完成前，它确保程序可以正常编译和运行。
    /// </summary>
    public class PlaceholderCommunicationGenerator : IModbusRtuConfigGenerator, IModbusTcpConfigGenerator
    {
        /// <summary>
        /// 生成通讯代码的占位符方法。
        /// </summary>
        /// <param name="context">全局数据上下文。</param>
        /// <returns>一个空的GenerationResult列表，因为功能尚未实现。</returns>
        public List<GenerationResult> Generate(DataContext context)
        {
            // 功能尚未实现，直接返回一个空的结果列表。
            // 未来，这里将包含读取通讯点表、构建配置帧、生成轮询逻辑等复杂操作。
            // 也可以在这里生成一个带注释的占位文件，以提醒用户此功能正在开发中。
            /*
            return new List<GenerationResult>
            {
                new GenerationResult
                {
                    FileName = "Communication_NotImplemented.txt",
                    Content = "(* 通讯功能正在开发中，此文件为自动生成的占位符。 *)",
                    Category = "Communication"
                }
            };
            */
            return new List<GenerationResult>();
        }
    }
}