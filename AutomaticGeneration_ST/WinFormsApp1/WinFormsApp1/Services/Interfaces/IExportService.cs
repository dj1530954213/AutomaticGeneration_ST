using AutomaticGeneration_ST.Services.Generation.Interfaces; // 引入 GenerationResult 所在的命名空间
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 定义了将生成结果导出到持久化存储（如文件系统）的服务的标准接口。
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// 将一系列生成结果导出到指定的根路径下。
        /// </summary>
        /// <param name="rootExportPath">导出的根目录路径 (例如 "C:\Generated_ST")。</param>
        /// <param name="resultsToExport">一个包含所有待导出内容的GenerationResult列表。</param>
        void Export(string rootExportPath, IEnumerable<GenerationResult> resultsToExport);
    }
}