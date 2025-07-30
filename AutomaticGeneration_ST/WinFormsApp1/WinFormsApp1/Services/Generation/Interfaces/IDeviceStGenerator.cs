using AutomaticGeneration_ST.Models;
using Scriban;

namespace AutomaticGeneration_ST.Services.Generation.Interfaces
{
    /// <summary>
    /// 定义设备级ST代码生成器的接口
    /// </summary>
    public interface IDeviceStGenerator
    {
        /// <summary>
        /// 为指定设备生成ST代码
        /// </summary>
        /// <param name="device">要生成代码的设备</param>
        /// <param name="template">用于生成的Scriban模板</param>
        /// <returns>生成结果</returns>
        GenerationResult Generate(Device device, Template template);
    }
}