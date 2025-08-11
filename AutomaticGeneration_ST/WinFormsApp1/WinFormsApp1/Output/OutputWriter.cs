using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WinFormsApp1.Output
{
    public static class OutputWriter
    {
        private static readonly LogService logger = LogService.Instance;
        
        public static void WriteToFile(IEnumerable<string> codeSegments, string filePath)
        {
            try
            {
                logger.LogInfo($"开始写入ST脚本到文件: {Path.GetFileName(filePath)}");
                
                var content = GenerateFileContent(codeSegments);
                
                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(filePath, content, Encoding.UTF8);
                
                logger.LogSuccess($"ST脚本文件写入成功: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                logger.LogError($"写入ST脚本文件失败: {ex.Message}");
                throw;
            }
        }
        
        public static string GenerateFileContent(IEnumerable<string> codeSegments)
        {
            var validSegments = codeSegments.Where(segment => !string.IsNullOrWhiteSpace(segment)).ToList();
            
            if (!validSegments.Any())
            {
                throw new ArgumentException("没有有效的代码段可以输出");
            }
            
            var content = new StringBuilder();
            
            // 添加文件头
            content.AppendLine("(*");
            content.AppendLine($" * ST脚本自动生成器输出文件");
            content.AppendLine($" * 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($" * 点位数量: {validSegments.Count}");
            content.AppendLine(" *)");
            content.AppendLine();
            
            // 直接添加代码段，不添加程序声明和变量定义
            foreach (var segment in validSegments)
            {
                content.AppendLine(segment.Trim());
                content.AppendLine();
            }
            
            return content.ToString();
        }
        
        public static string GenerateSimpleFileContent(IEnumerable<string> codeSegments, string pointType)
        {
            var validSegments = codeSegments.Where(segment => !string.IsNullOrWhiteSpace(segment)).ToList();
            
            if (!validSegments.Any())
            {
                return string.Empty;
            }
            
            var content = new StringBuilder();
            
            // 添加文件头
            content.AppendLine("(*");
            content.AppendLine($" * {pointType}点位ST脚本");
            content.AppendLine($" * 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($" * 点位数量: {validSegments.Count}");
            content.AppendLine(" *)");
            content.AppendLine();
            
            // 直接添加代码段
            foreach (var segment in validSegments)
            {
                content.AppendLine(segment.Trim());
                content.AppendLine();
            }
            
            return content.ToString();
        }
        
        public static string WriteCategorizedFiles(List<string> scripts, List<Dictionary<string, object>> pointData, string selectedPath)
        {
            try
            {
                // 创建输出文件夹
                var folderName = $"ST_Scripts_{DateTime.Now:yyyyMMdd_HHmmss}";
                var outputDirectory = Path.Combine(selectedPath, folderName);
                Directory.CreateDirectory(outputDirectory);
                
                logger.LogInfo($"创建输出文件夹: {folderName}");
                
                // 按点位类型分类
                var categorizedScripts = new Dictionary<string, List<string>>
                {
                    ["AI"] = new List<string>(),
                    ["AO"] = new List<string>(),
                    ["DI"] = new List<string>(),
                    ["DO"] = new List<string>()
                };
                
                for (int i = 0; i < scripts.Count && i < pointData.Count; i++)
                {
                    var script = scripts[i];
                    var row = pointData[i];
                    
                    if (row.TryGetValue("模块类型", out var typeObj))
                    {
                        var pointType = typeObj?.ToString()?.Trim().ToUpper();
                        
                        if (!string.IsNullOrWhiteSpace(pointType) && categorizedScripts.ContainsKey(pointType))
                        {
                            categorizedScripts[pointType].Add(script);
                        }
                    }
                }
                
                // 写入分类文件
                int totalFiles = 0;
                foreach (var category in categorizedScripts)
                {
                    if (category.Value.Any())
                    {
                        var fileName = $"{category.Key}.txt";
                        var filePath = Path.Combine(outputDirectory, fileName);
                        
                        var content = GenerateSimpleFileContent(category.Value, category.Key);
                        File.WriteAllText(filePath, content, Encoding.UTF8);
                        
                        totalFiles++;
                        logger.LogInfo($"生成{category.Key}点位文件: {fileName} ({category.Value.Count}个点位)");
                    }
                }
                
                logger.LogSuccess($"分类文件输出完成，共生成{totalFiles}个文件");
                return outputDirectory;
            }
            catch (Exception ex)
            {
                logger.LogError($"分类文件输出失败: {ex.Message}");
                throw;
            }
        }
        
        public static string PreviewContent(IEnumerable<string> codeSegments, int maxLines = 50)
        {
            try
            {
                var content = GenerateFileContent(codeSegments);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length <= maxLines)
                {
                    return content;
                }
                
                var preview = new StringBuilder();
                for (int i = 0; i < maxLines - 3; i++)
                {
                    preview.AppendLine(lines[i]);
                }
                
                preview.AppendLine("...");
                preview.AppendLine($"(省略 {lines.Length - maxLines + 3} 行)");
                preview.AppendLine("...");
                
                return preview.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError($"生成预览内容失败: {ex.Message}");
                return $"预览生成失败: {ex.Message}";
            }
        }
    }
}