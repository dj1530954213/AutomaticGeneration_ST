using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Tests
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string Summary { get; set; } = "";
    }

    /// <summary>
    /// 基础验证器
    /// </summary>
    public static class BasicValidator
    {
        /// <summary>
        /// 验证Excel数据
        /// </summary>
        public static ValidationResult ValidateExcelData(List<Dictionary<string, object>> pointData)
        {
            var result = new ValidationResult { IsValid = true };

            if (pointData == null || pointData.Count == 0)
            {
                result.IsValid = false;
                result.Errors.Add("点位数据为空");
                result.Summary = "验证失败：没有有效的点位数据";
                return result;
            }

            var requiredFields = new[] { "变量名称", "点位类型" };
            var validPointTypes = Enum.GetNames(typeof(PointType));

            for (int i = 0; i < pointData.Count; i++)
            {
                var point = pointData[i];
                var rowPrefix = $"第{i + 1}行：";

                // 检查必填字段
                foreach (var field in requiredFields)
                {
                    if (!point.ContainsKey(field) || string.IsNullOrWhiteSpace(point[field]?.ToString()))
                    {
                        result.Errors.Add($"{rowPrefix}{field}不能为空");
                        result.IsValid = false;
                    }
                }

                // 检查点位类型有效性
                if (point.ContainsKey("点位类型"))
                {
                    var pointType = point["点位类型"]?.ToString();
                    if (!string.IsNullOrEmpty(pointType) && !validPointTypes.Contains(pointType, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Errors.Add($"{rowPrefix}无效的点位类型：{pointType}");
                        result.IsValid = false;
                    }
                }

                // 检查变量名称重复
                var variableName = point.GetValueOrDefault("变量名称", "")?.ToString();
                if (!string.IsNullOrEmpty(variableName))
                {
                    var duplicates = pointData.Where((p, idx) => 
                        idx != i && 
                        string.Equals(p.GetValueOrDefault("变量名称", "")?.ToString(), variableName, StringComparison.OrdinalIgnoreCase))
                        .Count();

                    if (duplicates > 0)
                    {
                        result.Warnings.Add($"{rowPrefix}变量名称重复：{variableName}");
                    }
                }

                // 检查地址重复
                var address = point.GetValueOrDefault("地址", "")?.ToString();
                if (!string.IsNullOrEmpty(address))
                {
                    var duplicates = pointData.Where((p, idx) => 
                        idx != i && 
                        string.Equals(p.GetValueOrDefault("地址", "")?.ToString(), address, StringComparison.OrdinalIgnoreCase))
                        .Count();

                    if (duplicates > 0)
                    {
                        result.Warnings.Add($"{rowPrefix}地址重复：{address}");
                    }
                }
            }

            // 生成摘要
            if (result.IsValid)
            {
                result.Summary = $"验证通过：共{pointData.Count}个点位";
                if (result.Warnings.Any())
                {
                    result.Summary += $"，{result.Warnings.Count}个警告";
                }
            }
            else
            {
                result.Summary = $"验证失败：{result.Errors.Count}个错误";
            }

            return result;
        }

        /// <summary>
        /// 验证模板语法
        /// </summary>
        public static ValidationResult ValidateTemplate(string templateContent)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(templateContent))
            {
                result.IsValid = false;
                result.Errors.Add("模板内容为空");
                result.Summary = "验证失败：模板内容为空";
                return result;
            }

            try
            {
                // 基础语法检查
                CheckBasicSyntax(templateContent, result);

                // 检查变量占位符
                CheckVariablePlaceholders(templateContent, result);

                // 检查ST语法基础结构
                CheckSTStructure(templateContent, result);

                // 生成摘要
                if (result.IsValid)
                {
                    result.Summary = "模板验证通过";
                    if (result.Warnings.Any())
                    {
                        result.Summary += $"，{result.Warnings.Count}个警告";
                    }
                }
                else
                {
                    result.Summary = $"模板验证失败：{result.Errors.Count}个错误";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"验证过程中发生错误：{ex.Message}");
                result.Summary = "模板验证失败";
            }

            return result;
        }

        /// <summary>
        /// 验证生成的ST代码
        /// </summary>
        public static ValidationResult ValidateGeneratedCode(string stCode)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(stCode))
            {
                result.IsValid = false;
                result.Errors.Add("生成的代码为空");
                result.Summary = "验证失败：生成的代码为空";
                return result;
            }

            try
            {
                // 检查基本ST结构
                CheckSTCodeStructure(stCode, result);

                // 检查语法正确性
                CheckSTSyntax(stCode, result);

                // 生成摘要
                if (result.IsValid)
                {
                    var lines = stCode.Split('\n').Length;
                    result.Summary = $"代码验证通过：共{lines}行";
                    if (result.Warnings.Any())
                    {
                        result.Summary += $"，{result.Warnings.Count}个警告";
                    }
                }
                else
                {
                    result.Summary = $"代码验证失败：{result.Errors.Count}个错误";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"验证过程中发生错误：{ex.Message}");
                result.Summary = "代码验证失败";
            }

            return result;
        }

        /// <summary>
        /// 验证文件路径
        /// </summary>
        public static ValidationResult ValidateFilePath(string filePath, bool checkExists = true)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.IsValid = false;
                result.Errors.Add("文件路径为空");
                result.Summary = "验证失败：文件路径为空";
                return result;
            }

            try
            {
                // 检查路径格式
                var invalidChars = Path.GetInvalidPathChars();
                if (filePath.Any(c => invalidChars.Contains(c)))
                {
                    result.IsValid = false;
                    result.Errors.Add("文件路径包含无效字符");
                }

                // 检查文件是否存在
                if (checkExists && !File.Exists(filePath))
                {
                    result.IsValid = false;
                    result.Errors.Add("文件不存在");
                }

                // 检查扩展名
                var extension = Path.GetExtension(filePath).ToLower();
                var supportedExtensions = new[] { ".xlsx", ".xls", ".csv" };
                if (checkExists && !supportedExtensions.Contains(extension))
                {
                    result.Warnings.Add($"文件扩展名 {extension} 可能不受支持");
                }

                result.Summary = result.IsValid ? "文件路径验证通过" : "文件路径验证失败";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"路径验证错误：{ex.Message}");
                result.Summary = "文件路径验证失败";
            }

            return result;
        }

        #region 私有方法

        private static void CheckBasicSyntax(string content, ValidationResult result)
        {
            var lines = content.Split('\n');
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                // 检查括号匹配
                var openBraces = line.Count(c => c == '{');
                var closeBraces = line.Count(c => c == '}');
                if (openBraces != closeBraces)
                {
                    result.Warnings.Add($"第{i + 1}行：括号可能不匹配");
                }
            }
        }

        private static void CheckVariablePlaceholders(string content, ValidationResult result)
        {
            var placeholders = new List<string>();
            int start = 0;

            while ((start = content.IndexOf("{{", start)) != -1)
            {
                var end = content.IndexOf("}}", start + 2);
                if (end == -1)
                {
                    result.Errors.Add("发现未闭合的变量占位符");
                    result.IsValid = false;
                    break;
                }

                var placeholder = content.Substring(start + 2, end - start - 2).Trim();
                if (string.IsNullOrEmpty(placeholder))
                {
                    result.Warnings.Add("发现空的变量占位符");
                }
                else
                {
                    placeholders.Add(placeholder);
                }

                start = end + 2;
            }

            // 检查常用变量
            var commonVariables = new[] { "变量名称", "地址", "描述", "点位类型" };
            var missingVariables = commonVariables.Where(v => !placeholders.Contains(v)).ToList();
            
            if (missingVariables.Any())
            {
                result.Warnings.Add($"可能缺少常用变量：{string.Join(", ", missingVariables)}");
            }
        }

        private static void CheckSTStructure(string content, ValidationResult result)
        {
            var upperContent = content.ToUpper();

            // 检查基本ST结构
            if (!upperContent.Contains("VAR") && !upperContent.Contains("TYPE"))
            {
                result.Warnings.Add("模板可能缺少VAR或TYPE声明");
            }

            if (upperContent.Contains("VAR") && !upperContent.Contains("END_VAR"))
            {
                result.Errors.Add("VAR声明缺少对应的END_VAR");
                result.IsValid = false;
            }

            if (upperContent.Contains("TYPE") && !upperContent.Contains("END_TYPE"))
            {
                result.Errors.Add("TYPE声明缺少对应的END_TYPE");
                result.IsValid = false;
            }
        }

        private static void CheckSTCodeStructure(string code, ValidationResult result)
        {
            var lines = code.Split('\n');
            var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//")).ToList();

            if (nonEmptyLines.Count == 0)
            {
                result.Errors.Add("生成的代码没有有效内容");
                result.IsValid = false;
                return;
            }

            // 检查基本结构
            var upperCode = code.ToUpper();
            if (!upperCode.Contains("VAR") && !upperCode.Contains(":=") && !upperCode.Contains(";"))
            {
                result.Warnings.Add("代码可能缺少基本的ST结构");
            }
        }

        private static void CheckSTSyntax(string code, ValidationResult result)
        {
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                // 检查分号结尾
                if (line.Contains(":=") && !line.EndsWith(";"))
                {
                    result.Warnings.Add($"第{i + 1}行：赋值语句可能缺少分号");
                }

                // 检查变量声明
                if (line.Contains(":") && !line.Contains(":=") && !line.EndsWith(";"))
                {
                    result.Warnings.Add($"第{i + 1}行：变量声明可能缺少分号");
                }
            }
        }

        #endregion

        /// <summary>
        /// 运行完整验证
        /// </summary>
        public static async Task<ValidationResult> RunFullValidationAsync(
            List<Dictionary<string, object>> pointData, 
            string templateContent, 
            string generatedCode)
        {
            var result = new ValidationResult { IsValid = true };
            var allErrors = new List<string>();
            var allWarnings = new List<string>();

            // 验证Excel数据
            var excelResult = ValidateExcelData(pointData);
            if (!excelResult.IsValid)
            {
                result.IsValid = false;
                allErrors.AddRange(excelResult.Errors.Select(e => $"Excel数据：{e}"));
            }
            allWarnings.AddRange(excelResult.Warnings.Select(w => $"Excel数据：{w}"));

            // 验证模板
            var templateResult = ValidateTemplate(templateContent);
            if (!templateResult.IsValid)
            {
                result.IsValid = false;
                allErrors.AddRange(templateResult.Errors.Select(e => $"模板：{e}"));
            }
            allWarnings.AddRange(templateResult.Warnings.Select(w => $"模板：{w}"));

            // 验证生成的代码
            var codeResult = ValidateGeneratedCode(generatedCode);
            if (!codeResult.IsValid)
            {
                result.IsValid = false;
                allErrors.AddRange(codeResult.Errors.Select(e => $"生成代码：{e}"));
            }
            allWarnings.AddRange(codeResult.Warnings.Select(w => $"生成代码：{w}"));

            result.Errors = allErrors;
            result.Warnings = allWarnings;
            result.Summary = result.IsValid ? 
                $"完整验证通过，{allWarnings.Count}个警告" : 
                $"验证失败，{allErrors.Count}个错误，{allWarnings.Count}个警告";

            return result;
        }
    }
}