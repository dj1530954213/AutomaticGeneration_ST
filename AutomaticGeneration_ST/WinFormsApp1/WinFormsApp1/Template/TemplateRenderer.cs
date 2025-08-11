using Scriban;
using System;
using System.Collections.Generic;
using System.IO;

namespace WinFormsApp1.Template
{
    public static class TemplateRenderer
    {
        private static LogService logger = LogService.Instance;
        
        public static string Render(string templatePath, Dictionary<string, object> data)
        {
            try
            {
                logger.LogDebug($"开始渲染模板: {Path.GetFileName(templatePath)}");
                
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"模板文件不存在: {templatePath}");
                }
                
                var templateText = File.ReadAllText(templatePath, System.Text.Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(templateText))
                {
                    throw new ArgumentException($"模板文件为空: {templatePath}");
                }
                
                // 预处理数据，处理中文括号字段名
                var processedData = PreprocessDataForTemplate(data);
                
                var template = Scriban.Template.Parse(templateText);
                
                if (template.HasErrors)
                {
                    var errors = string.Join(", ", template.Messages);
                    throw new InvalidOperationException($"模板解析错误: {errors}");
                }
                
                var result = template.Render(processedData);
                
                // 应用内容过滤功能，移除分类标识行
                var filteredResult = FilterClassificationLines(result);
                
                logger.LogDebug($"模板渲染完成，生成{result.Length}个字符，过滤后{filteredResult.Length}个字符");
                return filteredResult;
            }
            catch (Exception ex)
            {
                logger.LogError($"模板渲染失败: {ex.Message}");
                throw;
            }
        }
        
        private static Dictionary<string, object> PreprocessDataForTemplate(Dictionary<string, object> data)
        {
            var processedData = new Dictionary<string, object>();
            
            foreach (var kvp in data)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                
                // 保持原始字段名
                processedData[key] = value;
                
                // 处理中文括号字段名，创建安全的变量名映射
                if (key.Contains("（") && key.Contains("）"))
                {
                    // 将中文括号替换为下划线，创建模板可用的变量名
                    var safeKey = key.Replace("（", "_").Replace("）", "_");
                    processedData[safeKey] = value;
                    
                    // 额外创建去掉括号的版本
                    var noBracketKey = key.Replace("（", "").Replace("）", "");
                    processedData[noBracketKey] = value;
                }
                
                // 为常用字段创建简化别名
                switch (key)
                {
                    case "变量名称（HMI）":
                        processedData["变量名称"] = value;
                        processedData["HMI变量名"] = value;
                        processedData["变量名称_HMI_"] = value; // 安全的模板变量名
                        processedData["变量名称HMI"] = value;   // 无括号版本
                        break;
                    case "模块类型":
                        processedData["类型"] = value;
                        break;
                }
            }
            
            logger.LogDebug($"预处理完成，原始字段 {data.Count} 个，处理后字段 {processedData.Count} 个");
            
            return processedData;
        }
        
        public static string RenderFromText(string templateText, Dictionary<string, object> data)
        {
            try
            {
                logger.LogDebug("开始渲染内联模板");
                
                if (string.IsNullOrWhiteSpace(templateText))
                {
                    throw new ArgumentException("模板文本不能为空");
                }
                
                var template = Scriban.Template.Parse(templateText);
                
                if (template.HasErrors)
                {
                    var errors = string.Join(", ", template.Messages);
                    throw new InvalidOperationException($"模板解析错误: {errors}");
                }
                
                var result = template.Render(data);
                
                // 应用内容过滤功能，移除分类标识行
                var filteredResult = FilterClassificationLines(result);
                
                logger.LogDebug($"内联模板渲染完成，生成{result.Length}个字符，过滤后{filteredResult.Length}个字符");
                return filteredResult;
            }
            catch (Exception ex)
            {
                logger.LogError($"内联模板渲染失败: {ex.Message}");
                throw;
            }
        }
        
        public static bool ValidateTemplate(string templatePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            try
            {
                if (!File.Exists(templatePath))
                {
                    errorMessage = $"模板文件不存在: {templatePath}";
                    return false;
                }
                
                var templateText = File.ReadAllText(templatePath, System.Text.Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(templateText))
                {
                    errorMessage = $"模板文件为空: {templatePath}";
                    return false;
                }
                
                var template = Scriban.Template.Parse(templateText);
                
                if (template.HasErrors)
                {
                    errorMessage = string.Join(", ", template.Messages);
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
        
        public static List<string> ExtractVariableNames(string templatePath)
        {
            var variables = new List<string>();
            
            try
            {
                if (!File.Exists(templatePath))
                {
                    return variables;
                }
                
                var templateText = File.ReadAllText(templatePath, System.Text.Encoding.UTF8);
                var template = Scriban.Template.Parse(templateText);
                
                // 简单的变量名提取（基于正则表达式）
                var regex = new System.Text.RegularExpressions.Regex(@"\{\{\s*(\w+)\s*\}\}");
                var matches = regex.Matches(templateText);
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var varName = match.Groups[1].Value;
                    if (!variables.Contains(varName))
                    {
                        variables.Add(varName);
                    }
                }
                
                return variables;
            }
            catch (Exception ex)
            {
                logger.LogError($"提取模板变量失败: {ex.Message}");
                return variables;
            }
        }
        
        /// <summary>
        /// 过滤掉模板生成内容中的分类标识行
        /// 复用ScribanIoMappingGenerator中已验证的过滤逻辑
        /// </summary>
        /// <param name="content">原始生成内容</param>
        /// <returns>过滤后的内容</returns>
        private static string FilterClassificationLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // 使用智能换行符处理
            var lines = SplitLines(content);
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 过滤掉程序名称、变量类型和变量名称标识行
                if (IsMetadataLine(trimmedLine))
                {
                    logger.LogDebug($"过滤掉分类标识行: {trimmedLine}");
                    continue; // 跳过这些行
                }
                
                filteredLines.Add(line);
            }

            return NormalizeLineEndings(filteredLines);
        }
        
        /// <summary>
        /// 判断是否为需要过滤的元数据行
        /// </summary>
        /// <param name="line">待检查的文本行</param>
        /// <returns>如果是元数据行返回true</returns>
        private static bool IsMetadataLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            
            // 标准化处理：去除空格，统一冒号格式
            var normalizedLine = line.Replace(" ", "").Replace("：", ":");
            
            return normalizedLine.StartsWith("程序名称:", StringComparison.OrdinalIgnoreCase) ||
                   normalizedLine.StartsWith("变量类型:", StringComparison.OrdinalIgnoreCase) ||
                   normalizedLine.StartsWith("变量名称:", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 智能分割文本行，正确处理各种换行符组合
        /// </summary>
        /// <param name="content">原始文本内容</param>
        /// <returns>分割后的行数组</returns>
        private static string[] SplitLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new string[0];

            // 先统一换行符为\n，然后分割
            var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
            return normalizedContent.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        /// <summary>
        /// 标准化换行符并清理多余空行
        /// </summary>
        /// <param name="lines">文本行列表</param>
        /// <returns>标准化后的文本内容</returns>
        private static string NormalizeLineEndings(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            // 清理连续的空行，最多保留一个空行
            var cleanedLines = new List<string>();
            bool lastLineWasEmpty = false;

            foreach (var line in lines)
            {
                bool currentLineEmpty = string.IsNullOrWhiteSpace(line);
                
                if (currentLineEmpty && lastLineWasEmpty)
                {
                    // 跳过连续的空行
                    continue;
                }
                
                cleanedLines.Add(line);
                lastLineWasEmpty = currentLineEmpty;
            }

            // 移除开头和结尾的空行
            while (cleanedLines.Count > 0 && string.IsNullOrWhiteSpace(cleanedLines[0]))
            {
                cleanedLines.RemoveAt(0);
            }
            while (cleanedLines.Count > 0 && string.IsNullOrWhiteSpace(cleanedLines[cleanedLines.Count - 1]))
            {
                cleanedLines.RemoveAt(cleanedLines.Count - 1);
            }

            // 使用平台标准换行符重新组合
            return string.Join(Environment.NewLine, cleanedLines);
        }
    }
}