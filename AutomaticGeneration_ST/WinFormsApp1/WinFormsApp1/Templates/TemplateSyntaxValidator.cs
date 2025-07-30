using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Scriban;
using Scriban.Parsing;

namespace WinFormsApp1.Templates
{
    /// <summary>
    /// 模板语法验证器 - 提供全面的Scriban模板语法验证功能
    /// </summary>
    public static class TemplateSyntaxValidator
    {
        #region 验证结果类

        /// <summary>
        /// 语法验证结果
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
            public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
            public List<string> RequiredFields { get; set; } = new List<string>();
            public TemplateComplexity Complexity { get; set; } = TemplateComplexity.Simple;
            public TimeSpan ValidationTime { get; set; }

            /// <summary>
            /// 是否有错误或警告
            /// </summary>
            public bool HasIssues => Errors.Any() || Warnings.Any();

            /// <summary>
            /// 获取问题总数
            /// </summary>
            public int TotalIssues => Errors.Count + Warnings.Count;

            /// <summary>
            /// 获取格式化的验证摘要
            /// </summary>
            public string GetSummary()
            {
                if (IsValid && !HasIssues)
                    return "✅ 语法正确，无问题";

                var parts = new List<string>();
                if (Errors.Any())
                    parts.Add($"❌ {Errors.Count} 个错误");
                if (Warnings.Any())
                    parts.Add($"⚠️ {Warnings.Count} 个警告");

                return string.Join(", ", parts);
            }
        }

        /// <summary>
        /// 验证错误
        /// </summary>
        public class ValidationError
        {
            public int Line { get; set; }
            public int Column { get; set; }
            public string Message { get; set; } = string.Empty;
            public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
            public string ErrorCode { get; set; } = string.Empty;
            public string ContextText { get; set; } = string.Empty;
            public string SuggestedFix { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"第{Line}行,第{Column}列: {Message}";
            }
        }

        /// <summary>
        /// 验证警告
        /// </summary>
        public class ValidationWarning
        {
            public int Line { get; set; }
            public int Column { get; set; }
            public string Message { get; set; } = string.Empty;
            public WarningType Type { get; set; } = WarningType.General;
            public string SuggestedImprovement { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"第{Line}行,第{Column}列: {Message}";
            }
        }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public enum ErrorSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// 警告类型
        /// </summary>
        public enum WarningType
        {
            General,
            Performance,
            BestPractice,
            Compatibility,
            Security
        }

        /// <summary>
        /// 模板复杂度
        /// </summary>
        public enum TemplateComplexity
        {
            Simple,      // 简单模板：只有基本变量替换
            Moderate,    // 中等复杂度：包含循环或条件
            Complex,     // 复杂模板：多层嵌套和复杂逻辑
            VeryComplex  // 非常复杂：深度嵌套和高级功能
        }

        #endregion

        #region 预定义的变量和字段

        /// <summary>
        /// 标准点位字段
        /// </summary>
        private static readonly HashSet<string> StandardPointFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "变量名称HMI", "变量描述", "硬点通道号", "量程高限", "量程低限", "单位",
            "模块名称", "模块类型", "供电类型", "线制", "通道位号", "场站名", "场站编号",
            "数据类型", "PLC绝对地址", "上位机通讯地址", "是否历史存储", "是否掉电保护",
            "仪表类型", "SLL值", "SLL点", "SLL点PLC地址", "SL值", "SL点", "SL点PLC地址",
            "SH值", "SH点", "SH点PLC地址", "SHH值", "SHH点", "SHH点PLC地址",
            "硬件报警对应的HMI变量", "硬件报警对应的PLC地址", "点位类型"
        };

        /// <summary>
        /// Scriban内置函数
        /// </summary>
        private static readonly HashSet<string> ScribanBuiltinFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "string.empty", "string.size", "string.upcase", "string.downcase", "string.capitalize",
            "string.strip", "string.lstrip", "string.rstrip", "string.split", "string.join",
            "string.replace", "string.slice", "string.truncate", "string.append", "string.prepend",
            "array.size", "array.first", "array.last", "array.empty", "array.join", "array.reverse",
            "array.sort", "array.uniq", "array.compact", "array.map", "array.where",
            "math.abs", "math.ceil", "math.floor", "math.round", "math.max", "math.min",
            "math.plus", "math.minus", "math.times", "math.divided_by", "math.modulo",
            "date.now", "date.format", "date.parse", "date.add_days", "date.add_months"
        };

        #endregion

        #region 主要验证方法

        /// <summary>
        /// 验证模板语法
        /// </summary>
        /// <param name="templateContent">模板内容</param>
        /// <param name="pointType">点位类型</param>
        /// <param name="validateFields">是否验证字段名</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateTemplate(string templateContent, PointType? pointType = null, bool validateFields = true)
        {
            var startTime = DateTime.Now;
            var result = new ValidationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(templateContent))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Line = 1,
                        Column = 1,
                        Message = "模板内容不能为空",
                        ErrorCode = "EMPTY_TEMPLATE",
                        Severity = ErrorSeverity.Error
                    });
                    result.IsValid = false;
                    return result;
                }

                // 1. 基础Scriban语法验证
                ValidateScribanSyntax(templateContent, result);

                // 2. 自定义语法规则验证
                ValidateCustomRules(templateContent, result);

                // 3. 字段名验证（如果启用）
                if (validateFields)
                {
                    ValidateFieldNames(templateContent, result);
                }

                // 4. 性能和最佳实践检查
                ValidateBestPractices(templateContent, result);

                // 5. 点位类型特定验证
                if (pointType.HasValue)
                {
                    ValidatePointTypeSpecific(templateContent, pointType.Value, result);
                }

                // 6. 计算模板复杂度
                result.Complexity = CalculateComplexity(templateContent);

                // 7. 提取必需字段
                result.RequiredFields = ExtractRequiredFields(templateContent);

                // 设置最终验证状态
                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError
                {
                    Line = 1,
                    Column = 1,
                    Message = $"验证过程中出现异常: {ex.Message}",
                    ErrorCode = "VALIDATION_EXCEPTION",
                    Severity = ErrorSeverity.Critical
                });
                result.IsValid = false;
            }
            finally
            {
                result.ValidationTime = DateTime.Now - startTime;
            }

            return result;
        }

        #endregion

        #region 具体验证方法

        /// <summary>
        /// 验证Scriban语法
        /// </summary>
        private static void ValidateScribanSyntax(string templateContent, ValidationResult result)
        {
            try
            {
                var template = Scriban.Template.Parse(templateContent);
                
                if (template.HasErrors)
                {
                    foreach (var message in template.Messages)
                    {
                        var line = message.Span.Start.Line + 1;
                        var column = message.Span.Start.Column + 1;
                        
                        result.Errors.Add(new ValidationError
                        {
                            Line = line,
                            Column = column,
                            Message = message.Message,
                            ErrorCode = "SCRIBAN_SYNTAX_ERROR",
                            Severity = GetSeverityFromMessage(message.Type.ToString()),
                            ContextText = GetContextText(templateContent, line, column),
                            SuggestedFix = GetSuggestedFix(message.Message)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ValidationError
                {
                    Line = 1,
                    Column = 1,
                    Message = $"Scriban语法解析失败: {ex.Message}",
                    ErrorCode = "SCRIBAN_PARSE_ERROR",
                    Severity = ErrorSeverity.Error
                });
            }
        }

        /// <summary>
        /// 验证自定义规则
        /// </summary>
        private static void ValidateCustomRules(string templateContent, ValidationResult result)
        {
            var lines = templateContent.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                // 检查未闭合的代码块
                ValidateCodeBlocks(line, lineNumber, result);

                // 检查变量命名规范
                ValidateVariableNaming(line, lineNumber, result);

                // 检查注释格式
                ValidateCommentFormat(line, lineNumber, result);

                // 检查空白行使用
                ValidateWhitespace(line, lineNumber, result);
            }

            // 全局检查
            ValidateGlobalStructure(templateContent, result);
        }

        /// <summary>
        /// 验证代码块
        /// </summary>
        private static void ValidateCodeBlocks(string line, int lineNumber, ValidationResult result)
        {
            // 检查未匹配的大括号
            var openBraces = Regex.Matches(line, @"\{\{").Count;
            var closeBraces = Regex.Matches(line, @"\}\}").Count;
            
            if (openBraces != closeBraces && (openBraces > 0 || closeBraces > 0))
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Line = lineNumber,
                    Column = 1,
                    Message = "该行可能存在未匹配的大括号",
                    Type = WarningType.General,
                    SuggestedImprovement = "确保每个{{都有对应的}}"
                });
            }

            // 检查嵌套深度
            var nestingLevel = GetNestingLevel(line);
            if (nestingLevel > 5)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Line = lineNumber,
                    Column = 1,
                    Message = "嵌套层级过深，可能影响可读性",
                    Type = WarningType.BestPractice,
                    SuggestedImprovement = "考虑将复杂逻辑拆分为多个模板"
                });
            }
        }

        /// <summary>
        /// 验证变量命名
        /// </summary>
        private static void ValidateVariableNaming(string line, int lineNumber, ValidationResult result)
        {
            // 查找变量引用 {{ variable }}
            var variablePattern = @"\{\{\s*([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)\s*\}\}";
            var matches = Regex.Matches(line, variablePattern);

            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value;
                
                // 检查命名规范
                if (variableName.Contains(".."))
                {
                    result.Errors.Add(new ValidationError
                    {
                        Line = lineNumber,
                        Column = match.Index + 1,
                        Message = $"变量名 '{variableName}' 包含连续的点号",
                        ErrorCode = "INVALID_VARIABLE_NAME",
                        Severity = ErrorSeverity.Error,
                        SuggestedFix = "移除多余的点号"
                    });
                }

                // 检查是否使用了保留字
                if (IsReservedKeyword(variableName))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = lineNumber,
                        Column = match.Index + 1,
                        Message = $"变量名 '{variableName}' 是保留关键字",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = "使用其他变量名以避免冲突"
                    });
                }
            }
        }

        /// <summary>
        /// 验证注释格式
        /// </summary>
        private static void ValidateCommentFormat(string line, int lineNumber, ValidationResult result)
        {
            // ST注释格式 (* ... *)
            var stCommentPattern = @"\(\*.*?\*\)";
            var stMatches = Regex.Matches(line, stCommentPattern);
            
            foreach (Match match in stMatches)
            {
                var comment = match.Value;
                if (comment.Length < 5) // (* *)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = lineNumber,
                        Column = match.Index + 1,
                        Message = "空注释或注释内容过短",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = "添加有意义的注释内容"
                    });
                }
            }

            // Scriban注释格式 {{# ... #}}
            var scribanCommentPattern = @"\{\{#.*?#\}\}";
            var scribanMatches = Regex.Matches(line, scribanCommentPattern);
            
            foreach (Match match in scribanMatches)
            {
                var comment = match.Value;
                if (comment.Length < 7) // {{# #}}
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = lineNumber,
                        Column = match.Index + 1,
                        Message = "空的Scriban注释",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = "添加注释内容或删除空注释"
                    });
                }
            }
        }

        /// <summary>
        /// 验证空白字符使用
        /// </summary>
        private static void ValidateWhitespace(string line, int lineNumber, ValidationResult result)
        {
            // 检查行尾空白
            if (line.TrimEnd() != line && line.Trim().Length > 0)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Line = lineNumber,
                    Column = line.Length,
                    Message = "行尾包含多余的空白字符",
                    Type = WarningType.BestPractice,
                    SuggestedImprovement = "删除行尾空白字符"
                });
            }

            // 检查Tab和空格混用
            if (line.Contains('\t') && line.Contains(' '))
            {
                var leadingChars = line.TakeWhile(c => c == ' ' || c == '\t').ToArray();
                if (leadingChars.Contains(' ') && leadingChars.Contains('\t'))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = lineNumber,
                        Column = 1,
                        Message = "混合使用Tab和空格进行缩进",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = "统一使用空格或Tab进行缩进"
                    });
                }
            }
        }

        /// <summary>
        /// 验证全局结构
        /// </summary>
        private static void ValidateGlobalStructure(string templateContent, ValidationResult result)
        {
            // 检查模板是否包含实际内容（不只是注释）
            var contentWithoutComments = Regex.Replace(templateContent, @"\(\*.*?\*\)", "", RegexOptions.Singleline);
            contentWithoutComments = Regex.Replace(contentWithoutComments, @"\{\{#.*?#\}\}", "", RegexOptions.Singleline);
            
            if (string.IsNullOrWhiteSpace(contentWithoutComments))
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Line = 1,
                    Column = 1,
                    Message = "模板只包含注释，没有实际内容",
                    Type = WarningType.General,
                    SuggestedImprovement = "添加实际的模板内容"
                });
            }

            // 检查循环结构是否完整
            ValidateLoopStructure(templateContent, result);

            // 检查条件结构是否完整
            ValidateConditionalStructure(templateContent, result);
        }

        /// <summary>
        /// 验证循环结构
        /// </summary>
        private static void ValidateLoopStructure(string templateContent, ValidationResult result)
        {
            var forPattern = @"\{\{\s*for\s+\w+\s+in\s+\w+\s*\}\}";
            var endPattern = @"\{\{\s*end\s*\}\}";
            
            var forMatches = Regex.Matches(templateContent, forPattern);
            var endMatches = Regex.Matches(templateContent, endPattern);
            
            if (forMatches.Count != endMatches.Count)
            {
                result.Errors.Add(new ValidationError
                {
                    Line = 1,
                    Column = 1,
                    Message = $"for循环结构不完整：找到{forMatches.Count}个for，{endMatches.Count}个end",
                    ErrorCode = "INCOMPLETE_LOOP",
                    Severity = ErrorSeverity.Error,
                    SuggestedFix = "确保每个for都有对应的end"
                });
            }
        }

        /// <summary>
        /// 验证条件结构
        /// </summary>
        private static void ValidateConditionalStructure(string templateContent, ValidationResult result)
        {
            var ifPattern = @"\{\{\s*if\s+.+?\s*\}\}";
            var endPattern = @"\{\{\s*end\s*\}\}";
            var elsePattern = @"\{\{\s*else\s*\}\}";
            
            var ifMatches = Regex.Matches(templateContent, ifPattern);
            var endMatches = Regex.Matches(templateContent, endPattern);
            
            // 注意：end标签被for和if共享，这里只是简单检查
            if (ifMatches.Count > endMatches.Count)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Line = 1,
                    Column = 1,
                    Message = "可能存在未闭合的if语句",
                    Type = WarningType.General,
                    SuggestedImprovement = "检查所有if语句是否都有对应的end"
                });
            }
        }

        /// <summary>
        /// 验证字段名
        /// </summary>
        private static void ValidateFieldNames(string templateContent, ValidationResult result)
        {
            var fieldPattern = @"\{\{\s*([a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*(?:\.[a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*)*)\s*\}\}";
            var matches = Regex.Matches(templateContent, fieldPattern);

            foreach (Match match in matches)
            {
                var fieldName = match.Groups[1].Value;
                var line = GetLineNumber(templateContent, match.Index);
                var column = GetColumnNumber(templateContent, match.Index);

                // 跳过内置函数和关键字
                if (IsScribanBuiltinFunction(fieldName) || IsReservedKeyword(fieldName))
                    continue;

                // 检查是否是标准字段
                if (!IsStandardPointField(fieldName))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = line,
                        Column = column,
                        Message = $"未识别的字段名: '{fieldName}'",
                        Type = WarningType.General,
                        SuggestedImprovement = "确认字段名拼写正确，或检查是否为自定义字段"
                    });
                }
            }
        }

        /// <summary>
        /// 验证最佳实践
        /// </summary>
        private static void ValidateBestPractices(string templateContent, ValidationResult result)
        {
            var lines = templateContent.Split('\n');

            // 检查模板长度
            if (lines.Length > 200)
            {
                result.Warnings.Add(new ValidationWarning
                {
                    Line = 1,
                    Column = 1,
                    Message = "模板过长，建议拆分为多个较小的模板",
                    Type = WarningType.Performance,
                    SuggestedImprovement = "考虑使用include语句引入子模板"
                });
            }

            // 检查复杂表达式
            var complexExpressionPattern = @"\{\{\s*[^}]{50,}\s*\}\}";
            var complexMatches = Regex.Matches(templateContent, complexExpressionPattern);
            
            foreach (Match match in complexMatches)
            {
                var line = GetLineNumber(templateContent, match.Index);
                var column = GetColumnNumber(templateContent, match.Index);
                
                result.Warnings.Add(new ValidationWarning
                {
                    Line = line,
                    Column = column,
                    Message = "表达式过于复杂，可能影响可读性",
                    Type = WarningType.BestPractice,
                    SuggestedImprovement = "考虑将复杂表达式拆分为多行或使用变量"
                });
            }

            // 检查硬编码值
            ValidateHardcodedValues(templateContent, result);
        }

        /// <summary>
        /// 验证硬编码值
        /// </summary>
        private static void ValidateHardcodedValues(string templateContent, ValidationResult result)
        {
            // 查找可能的硬编码数值
            var numberPattern = @"(?<!\w)\d+(?:\.\d+)?(?!\w)";
            var matches = Regex.Matches(templateContent, numberPattern);

            var suspiciousNumbers = new[] { "100", "1000", "4095", "32767", "65535" };

            foreach (Match match in matches)
            {
                var number = match.Value;
                if (suspiciousNumbers.Contains(number))
                {
                    var line = GetLineNumber(templateContent, match.Index);
                    var column = GetColumnNumber(templateContent, match.Index);
                    
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = line,
                        Column = column,
                        Message = $"检测到可能的硬编码值: {number}",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = "考虑使用变量或配置参数替换硬编码值"
                    });
                }
            }
        }

        /// <summary>
        /// 点位类型特定验证
        /// </summary>
        private static void ValidatePointTypeSpecific(string templateContent, PointType pointType, ValidationResult result)
        {
            switch (pointType)
            {
                case PointType.AI:
                    ValidateAITemplate(templateContent, result);
                    break;
                case PointType.AO:
                    ValidateAOTemplate(templateContent, result);
                    break;
                case PointType.DI:
                    ValidateDITemplate(templateContent, result);
                    break;
                case PointType.DO:
                    ValidateDOTemplate(templateContent, result);
                    break;
            }
        }

        /// <summary>
        /// 验证AI模板
        /// </summary>
        private static void ValidateAITemplate(string templateContent, ValidationResult result)
        {
            var requiredFields = new[] { "变量名称HMI", "硬点通道号", "量程高限", "量程低限" };
            
            foreach (var field in requiredFields)
            {
                if (!templateContent.Contains(field))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = 1,
                        Column = 1,
                        Message = $"AI模板缺少常用字段: {field}",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = $"考虑添加{field}字段以完善AI点位信息"
                    });
                }
            }
        }

        /// <summary>
        /// 验证AO模板
        /// </summary>
        private static void ValidateAOTemplate(string templateContent, ValidationResult result)
        {
            var requiredFields = new[] { "变量名称HMI", "硬点通道号", "量程高限", "量程低限" };
            
            foreach (var field in requiredFields)
            {
                if (!templateContent.Contains(field))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = 1,
                        Column = 1,
                        Message = $"AO模板缺少常用字段: {field}",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = $"考虑添加{field}字段以完善AO点位信息"
                    });
                }
            }
        }

        /// <summary>
        /// 验证DI模板
        /// </summary>
        private static void ValidateDITemplate(string templateContent, ValidationResult result)
        {
            var requiredFields = new[] { "变量名称HMI", "硬点通道号" };
            
            foreach (var field in requiredFields)
            {
                if (!templateContent.Contains(field))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = 1,
                        Column = 1,
                        Message = $"DI模板缺少常用字段: {field}",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = $"考虑添加{field}字段以完善DI点位信息"
                    });
                }
            }
        }

        /// <summary>
        /// 验证DO模板
        /// </summary>
        private static void ValidateDOTemplate(string templateContent, ValidationResult result)
        {
            var requiredFields = new[] { "变量名称HMI", "硬点通道号" };
            
            foreach (var field in requiredFields)
            {
                if (!templateContent.Contains(field))
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Line = 1,
                        Column = 1,
                        Message = $"DO模板缺少常用字段: {field}",
                        Type = WarningType.BestPractice,
                        SuggestedImprovement = $"考虑添加{field}字段以完善DO点位信息"
                    });
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算模板复杂度
        /// </summary>
        private static TemplateComplexity CalculateComplexity(string templateContent)
        {
            var complexity = 0;

            // 基础分数
            complexity += templateContent.Length / 100; // 长度因子

            // 循环和条件语句
            complexity += Regex.Matches(templateContent, @"\{\{\s*for\s+").Count * 2;
            complexity += Regex.Matches(templateContent, @"\{\{\s*if\s+").Count * 2;
            complexity += Regex.Matches(templateContent, @"\{\{\s*elsif\s+").Count * 1;

            // 函数调用
            complexity += Regex.Matches(templateContent, @"[a-zA-Z_]\w*\(").Count * 1;

            // 复杂表达式
            complexity += Regex.Matches(templateContent, @"\|\s*\w+").Count * 1; // 管道操作

            // 嵌套层级
            var maxNesting = GetMaxNestingLevel(templateContent);
            complexity += maxNesting * 2;

            return complexity switch
            {
                < 5 => TemplateComplexity.Simple,
                < 15 => TemplateComplexity.Moderate,
                < 30 => TemplateComplexity.Complex,
                _ => TemplateComplexity.VeryComplex
            };
        }

        /// <summary>
        /// 提取必需字段
        /// </summary>
        private static List<string> ExtractRequiredFields(string templateContent)
        {
            var fields = new HashSet<string>();
            var fieldPattern = @"\{\{\s*([a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*(?:\.[a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*)*)\s*\}\}";
            var matches = Regex.Matches(templateContent, fieldPattern);

            foreach (Match match in matches)
            {
                var fieldName = match.Groups[1].Value;
                
                // 跳过内置函数和关键字
                if (!IsScribanBuiltinFunction(fieldName) && !IsReservedKeyword(fieldName))
                {
                    fields.Add(fieldName);
                }
            }

            return fields.ToList();
        }

        /// <summary>
        /// 获取错误严重程度
        /// </summary>
        private static ErrorSeverity GetSeverityFromMessage(string messageType)
        {
            return messageType.ToLower() switch
            {
                "error" => ErrorSeverity.Error,
                "warning" => ErrorSeverity.Warning,
                "info" => ErrorSeverity.Info,
                _ => ErrorSeverity.Error
            };
        }

        /// <summary>
        /// 获取上下文文本
        /// </summary>
        private static string GetContextText(string content, int line, int column)
        {
            var lines = content.Split('\n');
            if (line <= 0 || line > lines.Length)
                return "";

            var targetLine = lines[line - 1];
            var start = Math.Max(0, column - 10);
            var length = Math.Min(20, targetLine.Length - start);
            
            if (length <= 0)
                return targetLine;

            return targetLine.Substring(start, length).Trim();
        }

        /// <summary>
        /// 获取修复建议
        /// </summary>
        private static string GetSuggestedFix(string errorMessage)
        {
            return errorMessage.ToLower() switch
            {
                var msg when msg.Contains("unexpected") => "检查语法是否正确",
                var msg when msg.Contains("expected") => "检查是否缺少必要的符号或关键字",
                var msg when msg.Contains("undefined") => "检查变量或函数名是否正确",
                _ => "请检查语法格式"
            };
        }

        /// <summary>
        /// 获取嵌套层级
        /// </summary>
        private static int GetNestingLevel(string line)
        {
            var level = 0;
            var inCode = false;
            
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (line[i] == '{' && line[i + 1] == '{')
                {
                    if (!inCode)
                    {
                        level++;
                        inCode = true;
                    }
                    i++; // 跳过下一个字符
                }
                else if (line[i] == '}' && line[i + 1] == '}')
                {
                    if (inCode)
                    {
                        inCode = false;
                    }
                    i++; // 跳过下一个字符
                }
            }
            
            return level;
        }

        /// <summary>
        /// 获取最大嵌套层级
        /// </summary>
        private static int GetMaxNestingLevel(string templateContent)
        {
            var maxLevel = 0;
            var currentLevel = 0;
            var inCode = false;

            for (int i = 0; i < templateContent.Length - 1; i++)
            {
                if (templateContent[i] == '{' && templateContent[i + 1] == '{')
                {
                    if (!inCode)
                    {
                        currentLevel++;
                        maxLevel = Math.Max(maxLevel, currentLevel);
                        inCode = true;
                    }
                    i++; // 跳过下一个字符
                }
                else if (templateContent[i] == '}' && templateContent[i + 1] == '}')
                {
                    if (inCode)
                    {
                        currentLevel--;
                        inCode = false;
                    }
                    i++; // 跳过下一个字符
                }
            }

            return maxLevel;
        }

        /// <summary>
        /// 是否是保留关键字
        /// </summary>
        private static bool IsReservedKeyword(string name)
        {
            var keywords = new[] { "for", "if", "else", "elsif", "end", "case", "when", "while", "break", "continue", "capture", "assign", "include", "render", "with", "in", "and", "or", "not" };
            return keywords.Contains(name.ToLower());
        }

        /// <summary>
        /// 是否是Scriban内置函数
        /// </summary>
        private static bool IsScribanBuiltinFunction(string name)
        {
            return ScribanBuiltinFunctions.Contains(name);
        }

        /// <summary>
        /// 是否是标准点位字段
        /// </summary>
        private static bool IsStandardPointField(string name)
        {
            return StandardPointFields.Contains(name);
        }

        /// <summary>
        /// 获取行号
        /// </summary>
        private static int GetLineNumber(string content, int index)
        {
            return content.Take(index).Count(c => c == '\n') + 1;
        }

        /// <summary>
        /// 获取列号
        /// </summary>
        private static int GetColumnNumber(string content, int index)
        {
            var lastNewLine = content.LastIndexOf('\n', index - 1);
            return index - lastNewLine;
        }

        #endregion

        #region 公共辅助方法

        /// <summary>
        /// 获取语法高亮建议
        /// </summary>
        public static Dictionary<string, List<int>> GetSyntaxHighlightSuggestions(string templateContent)
        {
            var suggestions = new Dictionary<string, List<int>>();

            // 关键字位置
            var keywordPositions = new List<int>();
            foreach (var keyword in new[] { "for", "if", "else", "elsif", "end", "case", "when" })
            {
                var pattern = $@"\{{\{{\s*{keyword}\s+";
                var matches = Regex.Matches(templateContent, pattern);
                keywordPositions.AddRange(matches.Cast<Match>().Select(m => m.Index));
            }
            suggestions["keywords"] = keywordPositions;

            // 字符串位置
            var stringPositions = new List<int>();
            var stringMatches = Regex.Matches(templateContent, @"""[^""]*""|'[^']*'");
            stringPositions.AddRange(stringMatches.Cast<Match>().Select(m => m.Index));
            suggestions["strings"] = stringPositions;

            // 注释位置
            var commentPositions = new List<int>();
            var commentMatches = Regex.Matches(templateContent, @"\(\*.*?\*\)|\{\{#.*?#\}\}", RegexOptions.Singleline);
            commentPositions.AddRange(commentMatches.Cast<Match>().Select(m => m.Index));
            suggestions["comments"] = commentPositions;

            return suggestions;
        }

        /// <summary>
        /// 获取自动完成建议
        /// </summary>
        public static List<string> GetAutoCompleteSuggestions(string currentInput, PointType? pointType = null)
        {
            var suggestions = new List<string>();

            // 添加标准字段
            suggestions.AddRange(StandardPointFields.Where(f => 
                f.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase)));

            // 添加Scriban关键字
            var keywords = new[] { "for", "if", "else", "elsif", "end", "case", "when", "while", "break", "continue" };
            suggestions.AddRange(keywords.Where(k => 
                k.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase)));

            // 添加内置函数
            suggestions.AddRange(ScribanBuiltinFunctions.Where(f => 
                f.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase)));

            return suggestions.Distinct().OrderBy(s => s).ToList();
        }

        #endregion
    }
}