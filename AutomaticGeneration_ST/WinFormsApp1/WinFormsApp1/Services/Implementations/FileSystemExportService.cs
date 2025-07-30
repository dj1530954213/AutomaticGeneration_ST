using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Interfaces;
using System.Collections.Generic;
using System.IO; // 引入 System.IO 命名空间，用于文件和目录操作
using System.Linq;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 一个将生成结果写入本地文件系统的标准导出服务。
    /// </summary>
    public class FileSystemExportService : IExportService
    {
        public void Export(string rootExportPath, IEnumerable<GenerationResult> resultsToExport)
        {
            if (string.IsNullOrWhiteSpace(rootExportPath))
            {
                throw new System.ArgumentException("导出的根路径不能为空。", nameof(rootExportPath));
            }

            // 步骤 1: 创建根导出目录
            // Directory.CreateDirectory 如果目录已存在，则不会执行任何操作，因此它是安全的。
            Directory.CreateDirectory(rootExportPath);

            // 步骤 2: 按类别分组，以便于创建子文件夹
            var resultsByCategory = resultsToExport.GroupBy(r => r.Category);

            // 步骤 3: 遍历每个类别，创建对应的子文件夹并写入文件
            foreach (var group in resultsByCategory)
            {
                var category = group.Key;
                string subFolderPath;

                // 步骤 3a: 根据类别确定子文件夹的名称
                switch (category)
                {
                    case "Device":
                        subFolderPath = Path.Combine(rootExportPath, "设备文件夹");
                        break;
                    case "IO":
                        subFolderPath = Path.Combine(rootExportPath, "IO映射文件夹");
                        break;
                    case "Communication":
                        subFolderPath = Path.Combine(rootExportPath, "通讯模块配置文件夹");
                        break;
                    default:
                        // 如果遇到未知的类别，可以将其放在根目录或一个"其他"文件夹中
                        subFolderPath = Path.Combine(rootExportPath, "其他");
                        break;
                }

                // 步骤 3b: 创建子文件夹
                Directory.CreateDirectory(subFolderPath);

                // 步骤 3c: 遍历该类别下的所有结果，并写入文件
                foreach (var result in group)
                {
                    // 使用 Path.Combine 来正确地拼接路径，这是跨平台和避免错误的最佳实践。
                    var fullFilePath = Path.Combine(subFolderPath, result.FileName);

                    // 使用 File.WriteAllText 来写入文件。它会自动处理文件的创建、覆盖和关闭，非常方便。
                    // 这里使用了 UTF-8 编码，这是PLC编程（尤其是支持多语言注释时）的良好选择。
                    File.WriteAllText(fullFilePath, result.Content, System.Text.Encoding.UTF8);
                }
            }
        }
    }
}