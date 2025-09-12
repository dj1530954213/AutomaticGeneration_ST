// NEED DELETE: Optional categorized export service not used by the current main UI flow.
// Reason: Form1's categorized export logic is commented out; the standard export path
// uses project cache grouping and direct file writes. This service is registered but unused.
// 说明：该服务用于“分类导出”功能，当前UI中相关调用已注释，主流程未使用，可安全删除。
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 分类文件导出服务实现
    /// </summary>
    public class CategorizedFileExportService : ICategorizedExportService
    {
        /// <summary>
        /// 脚本分类器
        /// </summary>
        private readonly IScriptClassifier _scriptClassifier;
        
        /// <summary>
        /// 日志服务
        /// </summary>
        private readonly LogService _logger;
        
        /// <summary>
        /// 支持的分类列表
        /// </summary>
        private readonly List<ScriptCategory> _supportedCategories = new List<ScriptCategory>
        {
            ScriptCategory.AI_CONVERT,
            ScriptCategory.AO_CTRL,
            ScriptCategory.DI,
            ScriptCategory.DO
        };
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="scriptClassifier">脚本分类器</param>
        public CategorizedFileExportService(IScriptClassifier scriptClassifier = null)
        {
            _scriptClassifier = scriptClassifier ?? new ScriptClassificationService();
            _logger = LogService.Instance;
            _logger.LogInfo("初始化分类导出服务");
        }
        
        /// <summary>
        /// 按分类导出脚本到独立文件
        /// </summary>
        /// <param name="categorizedScripts">已分类的脚本列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>导出结果</returns>
        public ExportResult ExportScriptsByCategory(List<CategorizedScript> categorizedScripts, ExportConfiguration configuration)
        {
            var result = new ExportResult
            {
                StartTime = DateTime.Now
            };
            
            try
            {
                // 验证配置
                if (!ValidateConfiguration(configuration, out string validationError))
                {
                    result.SetFailure($"配置验证失败: {validationError}");
                    return result;
                }
                
                // 验证输入
                if (categorizedScripts == null || categorizedScripts.Count == 0)
                {
                    result.SetFailure("没有可导出的脚本");
                    return result;
                }
                
                _logger.LogInfo($"开始导出 {categorizedScripts.Count} 个脚本到目录: {configuration.OutputDirectory}");
                
                // 创建输出目录
                EnsureDirectoryExists(configuration.OutputDirectory);
                
                // 按分类分组
                var groupedScripts = GroupScriptsByCategory(categorizedScripts, configuration);
                
                // 导出每个分类
                foreach (var group in groupedScripts)
                {
                    var fileResult = ExportCategoryToFile(group.Key, group.Value, configuration);
                    result.AddFileResult(fileResult);
                }
                
                // 记录统计信息
                foreach (var group in groupedScripts)
                {
                    result.Statistics.AddCategoryCount(group.Key, group.Value.Count);
                }
                
                result.SetSuccess();
                _logger.LogInfo($"导出完成，生成 {result.SuccessfulFilesCount} 个文件");
            }
            catch (Exception ex)
            {
                _logger.LogError($"导出过程中出错: {ex.Message}");
                result.SetFailure($"导出过程中出错: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 异步按分类导出脚本到独立文件
        /// </summary>
        /// <param name="categorizedScripts">已分类的脚本列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>导出结果</returns>
        public async Task<ExportResult> ExportScriptsByCategoryAsync(List<CategorizedScript> categorizedScripts, ExportConfiguration configuration)
        {
            return await Task.Run(() => ExportScriptsByCategory(categorizedScripts, configuration));
        }
        
        /// <summary>
        /// 从设备列表生成并导出分类脚本
        /// </summary>
        /// <param name="devices">设备列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>导出结果</returns>
        public ExportResult GenerateAndExportFromDevices(List<Device> devices, ExportConfiguration configuration)
        {
            var result = new ExportResult
            {
                StartTime = DateTime.Now
            };
            
            try
            {
                _logger.LogInfo($"从 {devices?.Count ?? 0} 个设备生成并导出脚本");
                
                // 先分类设备脚本
                var categorizedScripts = _scriptClassifier.ClassifyDeviceScripts(devices);
                
                if (categorizedScripts.Count == 0)
                {
                    result.AddWarning("没有从设备中生成到可用的脚本");
                }
                
                // 然后导出
                return ExportScriptsByCategory(categorizedScripts, configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError($"从设备生成脚本时出错: {ex.Message}");
                result.SetFailure($"从设备生成脚本时出错: {ex.Message}");
                return result;
            }
        }
        
        /// <summary>
        /// 获取支持的分类列表
        /// </summary>
        /// <returns>支持的分类列表</returns>
        public List<ScriptCategory> GetSupportedCategories()
        {
            return new List<ScriptCategory>(_supportedCategories);
        }
        
        /// <summary>
        /// 验证导出配置
        /// </summary>
        /// <param name="configuration">导出配置</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>验证结果</returns>
        public bool ValidateConfiguration(ExportConfiguration configuration, out string errorMessage)
        {
            if (configuration == null)
            {
                errorMessage = "导出配置不能为null";
                return false;
            }
            
            return configuration.IsValid(out errorMessage);
        }
        
        /// <summary>
        /// 按分类分组脚本
        /// </summary>
        /// <param name="categorizedScripts">已分类的脚本列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>分组结果</returns>
        private Dictionary<ScriptCategory, List<CategorizedScript>> GroupScriptsByCategory(
            List<CategorizedScript> categorizedScripts, 
            ExportConfiguration configuration)
        {
            var groups = new Dictionary<ScriptCategory, List<CategorizedScript>>();
            
            foreach (var script in categorizedScripts)
            {
                // 检查是否在导出的分类列表中
                if (configuration.CategoriesToExport.Count > 0 && 
                    !configuration.CategoriesToExport.Contains(script.Category))
                {
                    continue;
                }
                
                // 过滤空脚本
                if (script.IsEmpty())
                {
                    _logger.LogWarning($"跳过空脚本: {script.Category.GetDescription()}");
                    continue;
                }
                
                if (!groups.ContainsKey(script.Category))
                {
                    groups[script.Category] = new List<CategorizedScript>();
                }
                
                groups[script.Category].Add(script);
            }
            
            _logger.LogInfo($"共分为 {groups.Count} 个分类组");
            
            return groups;
        }
        
        /// <summary>
        /// 将指定分类的脚本导出到文件
        /// </summary>
        /// <param name="category">分类</param>
        /// <param name="scripts">脚本列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>文件导出结果</returns>
        private ExportFileResult ExportCategoryToFile(
            ScriptCategory category, 
            List<CategorizedScript> scripts, 
            ExportConfiguration configuration)
        {
            var filePath = configuration.GetFilePath(category);
            var fileResult = new ExportFileResult(category, filePath);
            
            try
            {
                _logger.LogInfo($"开始导出 {category.GetDescription()} 类型的 {scripts.Count} 个脚本到: {filePath}");
                
                // 创建目录（如果需要）
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    EnsureDirectoryExists(directory);
                }
                
                // 检查文件是否存在
                if (File.Exists(filePath) && !configuration.OverwriteExisting)
                {
                    fileResult.SetFailure($"文件已存在且不允许覆盖: {filePath}");
                    return fileResult;
                }
                
                // 生成文件内容
                var content = GenerateFileContent(category, scripts, configuration);
                
                // 写入文件
                File.WriteAllText(filePath, content, configuration.Encoding);
                
                // 获取文件信息
                var fileInfo = new FileInfo(filePath);
                fileResult.SetSuccess(scripts.Count, fileInfo.Length);
                
                _logger.LogInfo($"成功导出 {category.GetDescription()}: {scripts.Count} 个脚本，文件大小: {fileResult.FileSizeFormatted}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"导出 {category.GetDescription()} 失败: {ex.Message}");
                fileResult.SetFailure($"导出失败: {ex.Message}");
            }
            
            return fileResult;
        }
        
        /// <summary>
        /// 生成文件内容
        /// </summary>
        /// <param name="category">分类</param>
        /// <param name="scripts">脚本列表</param>
        /// <param name="configuration">导出配置</param>
        /// <returns>文件内容</returns>
        private string GenerateFileContent(
            ScriptCategory category, 
            List<CategorizedScript> scripts, 
            ExportConfiguration configuration)
        {
            var contentBuilder = new StringBuilder();
            
            // 文件头部信息
            contentBuilder.AppendLine($"// ST代码分类导出: {category.GetDescription()}");
            contentBuilder.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            contentBuilder.AppendLine($"// 脚本数量: {scripts.Count}");
            contentBuilder.AppendLine();
            
            // 添加分类统计信息
            if (scripts.Count > 0)
            {
                var avgConfidence = scripts.Average(s => s.ConfidenceScore);
                var highConfidenceCount = scripts.Count(s => s.ConfidenceScore >= 80);
                
                contentBuilder.AppendLine($"// 分类统计: 平均置信度 {avgConfidence:F1}%, 高置信度 {highConfidenceCount}/{scripts.Count} 个");
                contentBuilder.AppendLine();
            }
            
            // 按置信度排序脚本（高置信度在前）
            var sortedScripts = scripts.OrderByDescending(s => s.ConfidenceScore).ThenBy(s => s.DeviceTag).ToList();
            
            for (int i = 0; i < sortedScripts.Count; i++)
            {
                var script = sortedScripts[i];
                
                // 脚本分隔符和注释
                if (i > 0)
                {
                    contentBuilder.AppendLine();
                    contentBuilder.AppendLine("// " + new string('-', 50));
                }
                
                // 脚本元信息
                contentBuilder.AppendLine($"// 脚本 #{i + 1}: {(!string.IsNullOrEmpty(script.DeviceTag) ? $"设备 {script.DeviceTag}" : "未知设备")}");
                contentBuilder.AppendLine($"// 置信度: {script.ConfidenceScore}%");
                
                if (script.MatchedKeywords.Count > 0)
                {
                    contentBuilder.AppendLine($"// 匹配关键字: {string.Join(", ", script.MatchedKeywords.Take(5))}"); // 只显示前5个
                }
                
                if (script.PointNames.Count > 0)
                {
                    contentBuilder.AppendLine($"// 相关点位: {string.Join(", ", script.PointNames.Take(10))}"); // 只显示前10个
                }
                
                contentBuilder.AppendLine();
                
                // 脚本内容（逐行过滤元数据标记，如“子程序变量声明文件:...”）
                contentBuilder.AppendLine(FilterContent(script.Content));
            }
            
            // 文件尾部信息
            contentBuilder.AppendLine();
            contentBuilder.AppendLine($"// ===== 文件结束 - {category.GetDescription()} =====");
            contentBuilder.AppendLine($"// 总计 {scripts.Count} 个脚本");
            
            return contentBuilder.ToString();
        }

        /// <summary>
        /// 过滤内容中的元数据标记行（如“子程序变量声明文件:...”等）
        /// </summary>
        private static string FilterContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return content ?? string.Empty;

            var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            var kept = new List<string>(lines.Length);
            foreach (var line in lines)
            {
                if (ShouldFilterLine(line)) continue;
                kept.Add(line);
            }
            return string.Join(Environment.NewLine, kept);
        }

        /// <summary>
        /// 判断是否为应过滤的元数据/标记行
        /// </summary>
        private static bool ShouldFilterLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;

            var trimmed = line.Trim();
            var normalized = trimmed
                .Replace('：', ':')
                .TrimStart('/', '*', '(', ')', ';', '-', ' ')
                .Trim();

            string[] prefixes = new[]
            {
                "子程序变量声明文件:",
                "变量声明文件:",
                "子程序 变量声明文件:",
            };

            foreach (var p in prefixes)
            {
                if (normalized.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (normalized.IndexOf("变量声明文件", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (normalized.IndexOf("子程序变量声明", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }
        
        /// <summary>
        /// 确保目录存在
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogInfo($"创建目录: {directoryPath}");
            }
        }
    }
}
