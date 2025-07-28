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
                
                logger.LogDebug($"模板渲染完成，生成{result.Length}个字符");
                return result;
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
                
                logger.LogDebug($"内联模板渲染完成，生成{result.Length}个字符");
                return result;
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
    }
}