using System;
using System.Collections.Generic;
using System.IO;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 导出配置类
    /// </summary>
    public class ExportConfiguration
    {
        /// <summary>
        /// 输出目录路径
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件名前缀（可选）
        /// </summary>
        public string FileNamePrefix { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件名后缀（可选）
        /// </summary>
        public string FileNameSuffix { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件扩展名（默认为.txt）
        /// </summary>
        public string FileExtension { get; set; } = ".txt";
        
        /// <summary>
        /// 是否含有时间戳
        /// </summary>
        public bool IncludeTimestamp { get; set; } = true;
        
        /// <summary>
        /// 是否覆盖现有文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;
        
        /// <summary>
        /// 是否创建子目录（按分类）
        /// </summary>
        public bool CreateSubDirectories { get; set; } = false;
        
        /// <summary>
        /// 需要导出的分类列表（空表示导出所有分类）
        /// </summary>
        public HashSet<ScriptCategory> CategoriesToExport { get; set; } = new HashSet<ScriptCategory>();
        
        /// <summary>
        /// 文件编码（默认UTF-8）
        /// </summary>
        public System.Text.Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
        
        /// <summary>
        /// 自定义文件名映射
        /// </summary>
        public Dictionary<ScriptCategory, string> CustomFileNames { get; set; } = new Dictionary<ScriptCategory, string>();
        
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ExportConfiguration() { }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="outputDirectory">输出目录</param>
        public ExportConfiguration(string outputDirectory)
        {
            OutputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        }
        
        /// <summary>
        /// 获取指定分类的文件路径
        /// </summary>
        /// <param name="category">脚本分类</param>
        /// <returns>完整的文件路径</returns>
        public string GetFilePath(ScriptCategory category)
        {
            var fileName = GetFileName(category);
            var directory = CreateSubDirectories 
                ? Path.Combine(OutputDirectory, category.GetFileName())
                : OutputDirectory;
                
            return Path.Combine(directory, fileName);
        }
        
        /// <summary>
        /// 获取指定分类的文件名
        /// </summary>
        /// <param name="category">脚本分类</param>
        /// <returns>文件名</returns>
        public string GetFileName(ScriptCategory category)
        {
            // 优先使用自定义文件名
            if (CustomFileNames.ContainsKey(category))
            {
                return AddFileExtension(CustomFileNames[category]);
            }
            
            // 使用默认命名规则
            var baseName = category.GetFileName();
            var fullName = $"{FileNamePrefix}{baseName}{FileNameSuffix}";
            
            // 添加时间戳
            if (IncludeTimestamp)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                fullName = $"{fullName}_{timestamp}";
            }
            
            return AddFileExtension(fullName);
        }
        
        /// <summary>
        /// 为文件名添加扩展名
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>带扩展名的文件名</returns>
        private string AddFileExtension(string fileName)
        {
            if (string.IsNullOrEmpty(FileExtension))
                return fileName;
                
            var extension = FileExtension.StartsWith(".") ? FileExtension : "." + FileExtension;
            return fileName + extension;
        }
        
        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        /// <returns>验证结果</returns>
        public bool IsValid(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(OutputDirectory))
            {
                errorMessage = "输出目录不能为空";
                return false;
            }
            
            try
            {
                Path.GetFullPath(OutputDirectory);
            }
            catch (Exception ex)
            {
                errorMessage = $"输出目录路径无效: {ex.Message}";
                return false;
            }
            
            errorMessage = string.Empty;
            return true;
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>默认配置对象</returns>
        public static ExportConfiguration CreateDefault(string outputDirectory)
        {
            return new ExportConfiguration(outputDirectory)
            {
                FileExtension = ".txt",
                IncludeTimestamp = false, // 默认不含时间戳，保持文件名简洁
                OverwriteExisting = true,
                CreateSubDirectories = false,
                Encoding = System.Text.Encoding.UTF8
            };
        }
    }
}
