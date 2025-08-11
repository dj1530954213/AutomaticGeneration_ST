using AutomaticGeneration_ST.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// 模板元数据解析器 - 从.scriban和.TXT文件中提取模板信息
    /// </summary>
    public class TemplateMetadataParser
    {
        /// <summary>
        /// 解析模板元数据
        /// </summary>
        /// <param name="scribanFilePath">scriban模板文件路径</param>
        /// <returns>模板元数据，如果解析失败返回null</returns>
        public TemplateMetadata? ParseTemplate(string scribanFilePath)
        {
            Console.WriteLine($"[TemplateMetadataParser] 开始解析模板: {scribanFilePath}");
            
            if (string.IsNullOrWhiteSpace(scribanFilePath) || !File.Exists(scribanFilePath))
            {
                Console.WriteLine($"[TemplateMetadataParser] 模板文件不存在或路径无效: {scribanFilePath}");
                return null;
            }

            try
            {
                var metadata = new TemplateMetadata
                {
                    TemplatePath = scribanFilePath
                };

                // 读取scriban文件内容
                var lines = File.ReadAllLines(scribanFilePath);
                Console.WriteLine($"[TemplateMetadataParser] 读取到 {lines.Length} 行内容");
                
                if (lines.Length < 2)
                {
                    Console.WriteLine($"[TemplateMetadataParser] 文件行数不足(<2行)，跳过: {scribanFilePath}");
                    return null;
                }

                // 解析第1行：程序名称
                Console.WriteLine($"[TemplateMetadataParser] 第1行内容: {lines[0]}");
                var programNameMatch = Regex.Match(lines[0], @"程序名称:\s*(.+)", RegexOptions.IgnoreCase);
                if (programNameMatch.Success)
                {
                    metadata.ProgramName = programNameMatch.Groups[1].Value.Trim();
                    Console.WriteLine($"[TemplateMetadataParser] 提取到程序名称: {metadata.ProgramName}");
                }
                else
                {
                    Console.WriteLine($"[TemplateMetadataParser] 未能提取程序名称，第1行格式不匹配");
                }

                // 解析第2行：变量类型
                Console.WriteLine($"[TemplateMetadataParser] 第2行内容: {lines[1]}");
                var variableTypeMatch = Regex.Match(lines[1], @"变量类型:\s*(.+)", RegexOptions.IgnoreCase);
                if (variableTypeMatch.Success)
                {
                    metadata.VariableType = variableTypeMatch.Groups[1].Value.Trim();
                    Console.WriteLine($"[TemplateMetadataParser] 提取到变量类型: {metadata.VariableType}");
                }
                else
                {
                    Console.WriteLine($"[TemplateMetadataParser] 未能提取变量类型，第2行格式不匹配");
                }

                // 检查并读取与变量类型同名的.TXT文件
                string txtFilePath = null;
                if (!string.IsNullOrWhiteSpace(metadata.VariableType))
                {
                    var directory = Path.GetDirectoryName(scribanFilePath);
                    txtFilePath = Path.Combine(directory!, $"{metadata.VariableType}.TXT");
                    Console.WriteLine($"[TemplateMetadataParser] 根据变量类型查找TXT文件: {txtFilePath}");
                    
                    // 如果按变量类型找不到，回退到按文件名查找
                    if (!File.Exists(txtFilePath))
                    {
                        Console.WriteLine($"[TemplateMetadataParser] 按变量类型未找到TXT文件，尝试按文件名查找");
                        txtFilePath = GetCorrespondingTxtFile(scribanFilePath);
                        Console.WriteLine($"[TemplateMetadataParser] 按文件名查找TXT文件: {txtFilePath}");
                    }
                }
                else
                {
                    Console.WriteLine($"[TemplateMetadataParser] 变量类型为空，使用文件名查找TXT文件");
                    txtFilePath = GetCorrespondingTxtFile(scribanFilePath);
                    Console.WriteLine($"[TemplateMetadataParser] 查找对应TXT文件: {txtFilePath}");
                }
                
                if (File.Exists(txtFilePath))
                {
                    metadata.HasTxtFile = true;
                    metadata.TxtFilePath = txtFilePath;
                    metadata.InitializationValue = File.ReadAllText(txtFilePath).Trim();
                    Console.WriteLine($"[TemplateMetadataParser] 找到TXT文件，初始化值: {metadata.InitializationValue}");
                }
                else
                {
                    Console.WriteLine($"[TemplateMetadataParser] 未找到对应的TXT文件: {txtFilePath}");
                }

                // 只有同时满足以下条件的模板才需要生成点表：
                // 1. 有非空的程序名称
                // 2. 有非空的变量类型  
                // 3. 存在对应的TXT文件
                Console.WriteLine($"[TemplateMetadataParser] 检查条件 - 程序名称: '{metadata.ProgramName}', 变量类型: '{metadata.VariableType}', 有TXT文件: {metadata.HasTxtFile}");
                
                if (string.IsNullOrWhiteSpace(metadata.ProgramName) || 
                    string.IsNullOrWhiteSpace(metadata.VariableType) || 
                    !metadata.HasTxtFile)
                {
                    Console.WriteLine($"[TemplateMetadataParser] 模板不符合生成点表条件，跳过: {scribanFilePath}");
                    return null; // 不符合条件，不生成点表
                }

                Console.WriteLine($"[TemplateMetadataParser] 模板解析成功: {metadata.ProgramName}");
                return metadata;
            }
            catch (Exception ex)
            {
                // 记录异常但不抛出，返回null表示解析失败
                Console.WriteLine($"解析模板元数据失败: {scribanFilePath}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取对应的TXT文件路径
        /// </summary>
        /// <param name="scribanFilePath">scriban文件路径</param>
        /// <returns>对应的TXT文件路径</returns>
        private string GetCorrespondingTxtFile(string scribanFilePath)
        {
            var directory = Path.GetDirectoryName(scribanFilePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(scribanFilePath);
            
            // 对于default.scriban，使用文件夹名作为TXT文件名
            if (fileNameWithoutExtension.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                var folderName = Path.GetFileName(directory);
                return Path.Combine(directory!, $"{folderName}.TXT");
            }
            
            // 对于其他模板，使用模板名
            return Path.Combine(directory!, $"{fileNameWithoutExtension}.TXT");
        }

        /// <summary>
        /// 批量解析模板目录下的所有模板
        /// </summary>
        /// <param name="templatesDirectory">模板根目录</param>
        /// <returns>模板元数据字典，Key为模板名称</returns>
        public Dictionary<string, TemplateMetadata> ParseAllTemplates(string templatesDirectory)
        {
            Console.WriteLine($"[TemplateMetadataParser] 开始批量解析模板，目录: {templatesDirectory}");
            var results = new Dictionary<string, TemplateMetadata>();

            if (!Directory.Exists(templatesDirectory))
            {
                Console.WriteLine($"[TemplateMetadataParser] 模板目录不存在: {templatesDirectory}");
                return results;
            }

            try
            {
                // 递归搜索所有.scriban文件
                var scribanFiles = Directory.GetFiles(templatesDirectory, "*.scriban", SearchOption.AllDirectories);
                Console.WriteLine($"[TemplateMetadataParser] 找到 {scribanFiles.Length} 个.scriban文件");

                foreach (var scribanFile in scribanFiles)
                {
                    var metadata = ParseTemplate(scribanFile);
                    if (metadata != null) // ParseTemplate已经做了完整的过滤
                    {
                        // 使用程序名称作为Key，确保唯一性
                        var programNameKey = metadata.ProgramName;
                        if (!results.ContainsKey(programNameKey))
                        {
                            results[programNameKey] = metadata;
                            Console.WriteLine($"[TemplateMetadataParser] 添加有效模板(按程序名称): {programNameKey}");
                        }
                        else
                        {
                            Console.WriteLine($"[TemplateMetadataParser] 发现重复的模板键(程序名称): {programNameKey}，跳过");
                        }

                        // 新增：同时使用模板文件名作为Key，解决第三方设备匹配问题
                        var templateFileName = Path.GetFileNameWithoutExtension(scribanFile);
                        if (!results.ContainsKey(templateFileName) && templateFileName != programNameKey)
                        {
                            results[templateFileName] = metadata;
                            Console.WriteLine($"[TemplateMetadataParser] 添加有效模板(按文件名): {templateFileName}");
                        }
                        else if (templateFileName != programNameKey)
                        {
                            Console.WriteLine($"[TemplateMetadataParser] 发现重复的模板键(文件名): {templateFileName}，跳过");
                        }
                    }
                }
                
                Console.WriteLine($"[TemplateMetadataParser] 批量解析完成，有效模板数量: {results.Count}");
                Console.WriteLine($"[TemplateMetadataParser] 有效模板列表: {string.Join(", ", results.Keys)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TemplateMetadataParser] 批量解析模板失败: {ex.Message}");
            }

            return results;
        }
    }
}