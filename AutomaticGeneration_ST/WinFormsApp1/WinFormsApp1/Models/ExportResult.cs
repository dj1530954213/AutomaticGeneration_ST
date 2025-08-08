using System;
using System.Collections.Generic;
using System.Linq;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 导出结果类
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; } = false;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();
        
        /// <summary>
        /// 每个分类的导出结果
        /// </summary>
        public List<ExportFileResult> FileResults { get; set; } = new List<ExportFileResult>();
        
        /// <summary>
        /// 导出统计信息
        /// </summary>
        public ExportStatistics Statistics { get; set; } = new ExportStatistics();
        
        /// <summary>
        /// 导出开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 导出结束时间
        /// </summary>
        public DateTime EndTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 导出耗时
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
        
        /// <summary>
        /// 成功导出的文件数量
        /// </summary>
        public int SuccessfulFilesCount => FileResults.Count(f => f.IsSuccess);
        
        /// <summary>
        /// 失败导出的文件数量
        /// </summary>
        public int FailedFilesCount => FileResults.Count(f => !f.IsSuccess);
        
        /// <summary>
        /// 成功导出的文件路径列表
        /// </summary>
        public List<string> SuccessfulFilePaths => FileResults.Where(f => f.IsSuccess).Select(f => f.FilePath).ToList();
        
        /// <summary>
        /// 添加文件结果
        /// </summary>
        /// <param name="fileResult">文件结果</param>
        public void AddFileResult(ExportFileResult fileResult)
        {
            if (fileResult != null)
            {
                FileResults.Add(fileResult);
                
                // 更新统计信息
                if (fileResult.IsSuccess)
                {
                    Statistics.TotalScriptsExported += fileResult.ScriptCount;
                    Statistics.TotalFilesGenerated++;
                }
                else
                {
                    Statistics.TotalErrors++;
                }
            }
        }
        
        /// <summary>
        /// 添加警告
        /// </summary>
        /// <param name="warning">警告信息</param>
        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
            {
                Warnings.Add(warning);
                Statistics.TotalWarnings++;
            }
        }
        
        /// <summary>
        /// 设置成功状态
        /// </summary>
        public void SetSuccess()
        {
            IsSuccess = true;
            ErrorMessage = string.Empty;
            EndTime = DateTime.Now;
        }
        
        /// <summary>
        /// 设置失败状态
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public void SetFailure(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage ?? string.Empty;
            EndTime = DateTime.Now;
            Statistics.TotalErrors++;
        }
        
        /// <summary>
        /// 获取结果摘要
        /// </summary>
        /// <returns>结果摘要</returns>
        public string GetSummary()
        {
            if (IsSuccess)
            {
                return $"导出成功：生成 {SuccessfulFilesCount} 个文件，" +
                       $"导出 {Statistics.TotalScriptsExported} 个脚本，" +
                       $"耗时 {Duration.TotalSeconds:F2} 秒";
            }
            else
            {
                return $"导出失败：{ErrorMessage}";
            }
        }
    }
    
    /// <summary>
    /// 单个文件的导出结果
    /// </summary>
    public class ExportFileResult
    {
        /// <summary>
        /// 脚本分类
        /// </summary>
        public ScriptCategory Category { get; set; } = ScriptCategory.UNKNOWN;
        
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; } = false;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// 导出的脚本数量
        /// </summary>
        public int ScriptCount { get; set; } = 0;
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSizeBytes { get; set; } = 0;
        
        /// <summary>
        /// 文件大小（易读格式）
        /// </summary>
        public string FileSizeFormatted
        {
            get
            {
                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} B";
                else if (FileSizeBytes < 1024 * 1024)
                    return $"{FileSizeBytes / 1024.0:F1} KB";
                else
                    return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
            }
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="category">脚本分类</param>
        /// <param name="filePath">文件路径</param>
        public ExportFileResult(ScriptCategory category, string filePath)
        {
            Category = category;
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }
        
        /// <summary>
        /// 设置成功状态
        /// </summary>
        /// <param name="scriptCount">脚本数量</param>
        /// <param name="fileSizeBytes">文件大小</param>
        public void SetSuccess(int scriptCount, long fileSizeBytes = 0)
        {
            IsSuccess = true;
            ErrorMessage = string.Empty;
            ScriptCount = scriptCount;
            FileSizeBytes = fileSizeBytes;
        }
        
        /// <summary>
        /// 设置失败状态
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public void SetFailure(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage ?? string.Empty;
            ScriptCount = 0;
            FileSizeBytes = 0;
        }
    }
    
    /// <summary>
    /// 导出统计信息
    /// </summary>
    public class ExportStatistics
    {
        /// <summary>
        /// 导出的总脚本数
        /// </summary>
        public int TotalScriptsExported { get; set; } = 0;
        
        /// <summary>
        /// 生成的总文件数
        /// </summary>
        public int TotalFilesGenerated { get; set; } = 0;
        
        /// <summary>
        /// 总错误数
        /// </summary>
        public int TotalErrors { get; set; } = 0;
        
        /// <summary>
        /// 总警告数
        /// </summary>
        public int TotalWarnings { get; set; } = 0;
        
        /// <summary>
        /// 每个分类的脚本数统计
        /// </summary>
        public Dictionary<ScriptCategory, int> ScriptCountByCategory { get; set; } = new Dictionary<ScriptCategory, int>();
        
        /// <summary>
        /// 添加分类统计
        /// </summary>
        /// <param name="category">分类</param>
        /// <param name="count">数量</param>
        public void AddCategoryCount(ScriptCategory category, int count)
        {
            if (ScriptCountByCategory.ContainsKey(category))
            {
                ScriptCountByCategory[category] += count;
            }
            else
            {
                ScriptCountByCategory[category] = count;
            }
        }
    }
}
