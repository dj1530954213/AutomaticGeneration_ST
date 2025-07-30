using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Scriban;
using Scriban.Runtime;

namespace WinFormsApp1.Templates
{
    /// <summary>
    /// 模板变量提取器 - 自动分析模板所需字段和变量依赖关系
    /// </summary>
    public static class TemplateVariableExtractor
    {
        #region 数据结构定义

        /// <summary>
        /// 变量提取结果
        /// </summary>
        public class ExtractionResult
        {
            /// <summary>
            /// 所有提取的变量
            /// </summary>
            public List<VariableInfo> Variables { get; set; } = new List<VariableInfo>();

            /// <summary>
            /// 必需的标准字段
            /// </summary>
            public List<StandardField> RequiredStandardFields { get; set; } = new List<StandardField>();

            /// <summary>
            /// 自定义字段
            /// </summary>
            public List<string> CustomFields { get; set; } = new List<string>();

            /// <summary>
            /// 使用的函数调用
            /// </summary>
            public List<FunctionCall> FunctionCalls { get; set; } = new List<FunctionCall>();

            /// <summary>
            /// 循环变量
            /// </summary>
            public List<LoopVariable> LoopVariables { get; set; } = new List<LoopVariable>();

            /// <summary>
            /// 条件表达式
            /// </summary>
            public List<ConditionalExpression> ConditionalExpressions { get; set; } = new List<ConditionalExpression>();

            /// <summary>
            /// 变量依赖关系图
            /// </summary>
            public Dictionary<string, List<string>> Dependencies { get; set; } = new Dictionary<string, List<string>>();

            /// <summary>
            /// 提取统计信息
            /// </summary>
            public ExtractionStatistics Statistics { get; set; } = new ExtractionStatistics();

            /// <summary>
            /// 提取过程中的警告
            /// </summary>
            public List<string> Warnings { get; set; } = new List<string>();
        }

        /// <summary>
        /// 变量信息
        /// </summary>
        public class VariableInfo
        {
            /// <summary>
            /// 变量名称
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 变量类型
            /// </summary>
            public VariableType Type { get; set; } = VariableType.Unknown;

            /// <summary>
            /// 使用位置列表（行号）
            /// </summary>
            public List<int> UsageLines { get; set; } = new List<int>();

            /// <summary>
            /// 是否为必需字段
            /// </summary>
            public bool IsRequired { get; set; } = true;

            /// <summary>
            /// 默认值（如果有）
            /// </summary>
            public string? DefaultValue { get; set; }

            /// <summary>
            /// 变量描述
            /// </summary>
            public string? Description { get; set; }

            /// <summary>
            /// 使用次数
            /// </summary>
            public int UsageCount => UsageLines.Count;

            /// <summary>
            /// 是否在循环中使用
            /// </summary>
            public bool IsUsedInLoop { get; set; } = false;

            /// <summary>
            /// 是否在条件中使用
            /// </summary>
            public bool IsUsedInCondition { get; set; } = false;
        }

        /// <summary>
        /// 标准字段信息
        /// </summary>
        public class StandardField
        {
            /// <summary>
            /// 字段名称
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 字段类别
            /// </summary>
            public FieldCategory Category { get; set; } = FieldCategory.Basic;

            /// <summary>
            /// 适用的点位类型
            /// </summary>
            public List<PointType> ApplicablePointTypes { get; set; } = new List<PointType>();

            /// <summary>
            /// 字段描述
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// 是否为可选字段
            /// </summary>
            public bool IsOptional { get; set; } = false;

            /// <summary>
            /// 使用位置
            /// </summary>
            public List<int> UsageLines { get; set; } = new List<int>();
        }

        /// <summary>
        /// 函数调用信息
        /// </summary>
        public class FunctionCall
        {
            /// <summary>
            /// 函数名称
            /// </summary>
            public string FunctionName { get; set; } = string.Empty;

            /// <summary>
            /// 参数列表
            /// </summary>
            public List<string> Parameters { get; set; } = new List<string>();

            /// <summary>
            /// 调用位置
            /// </summary>
            public int Line { get; set; }

            /// <summary>
            /// 是否为内置函数
            /// </summary>
            public bool IsBuiltIn { get; set; } = false;
        }

        /// <summary>
        /// 循环变量信息
        /// </summary>
        public class LoopVariable
        {
            /// <summary>
            /// 迭代变量名
            /// </summary>
            public string IteratorName { get; set; } = string.Empty;

            /// <summary>
            /// 集合变量名
            /// </summary>
            public string CollectionName { get; set; } = string.Empty;

            /// <summary>
            /// 循环起始行
            /// </summary>
            public int StartLine { get; set; }

            /// <summary>
            /// 循环结束行
            /// </summary>
            public int EndLine { get; set; }

            /// <summary>
            /// 嵌套层级
            /// </summary>
            public int NestingLevel { get; set; } = 1;
        }

        /// <summary>
        /// 条件表达式信息
        /// </summary>
        public class ConditionalExpression
        {
            /// <summary>
            /// 条件表达式文本
            /// </summary>
            public string Expression { get; set; } = string.Empty;

            /// <summary>
            /// 条件类型
            /// </summary>
            public ConditionalType Type { get; set; } = ConditionalType.If;

            /// <summary>
            /// 使用的变量
            /// </summary>
            public List<string> UsedVariables { get; set; } = new List<string>();

            /// <summary>
            /// 条件位置
            /// </summary>
            public int Line { get; set; }
        }

        /// <summary>
        /// 提取统计信息
        /// </summary>
        public class ExtractionStatistics
        {
            /// <summary>
            /// 总变量数
            /// </summary>
            public int TotalVariables { get; set; }

            /// <summary>
            /// 标准字段数
            /// </summary>
            public int StandardFieldCount { get; set; }

            /// <summary>
            /// 自定义字段数
            /// </summary>
            public int CustomFieldCount { get; set; }

            /// <summary>
            /// 函数调用数
            /// </summary>
            public int FunctionCallCount { get; set; }

            /// <summary>
            /// 循环数量
            /// </summary>
            public int LoopCount { get; set; }

            /// <summary>
            /// 条件表达式数量
            /// </summary>
            public int ConditionalCount { get; set; }

            /// <summary>
            /// 最大嵌套深度
            /// </summary>
            public int MaxNestingDepth { get; set; }

            /// <summary>
            /// 模板复杂度评分
            /// </summary>
            public int ComplexityScore { get; set; }
        }

        /// <summary>
        /// 变量类型
        /// </summary>
        public enum VariableType
        {
            /// <summary>
            /// 未知类型
            /// </summary>
            Unknown,

            /// <summary>
            /// 标准点位字段
            /// </summary>
            StandardField,

            /// <summary>
            /// 自定义字段
            /// </summary>
            CustomField,

            /// <summary>
            /// 循环迭代变量
            /// </summary>
            LoopIterator,

            /// <summary>
            /// 函数调用结果
            /// </summary>
            FunctionResult,

            /// <summary>
            /// 复合表达式
            /// </summary>
            ComplexExpression
        }

        /// <summary>
        /// 字段类别
        /// </summary>
        public enum FieldCategory
        {
            /// <summary>
            /// 基本信息
            /// </summary>
            Basic,

            /// <summary>
            /// 硬件信息
            /// </summary>
            Hardware,

            /// <summary>
            /// 报警信息
            /// </summary>
            Alarm,

            /// <summary>
            /// 范围信息
            /// </summary>
            Range,

            /// <summary>
            /// 地址信息
            /// </summary>
            Address,

            /// <summary>
            /// 配置信息
            /// </summary>
            Configuration
        }

        /// <summary>
        /// 条件类型
        /// </summary>
        public enum ConditionalType
        {
            If,
            ElsIf,
            Case,
            When
        }

        #endregion

        #region 标准字段定义

        /// <summary>
        /// 标准字段映射表
        /// </summary>
        private static readonly Dictionary<string, StandardField> StandardFieldMap = new Dictionary<string, StandardField>
        {
            ["变量名称HMI"] = new StandardField
            {
                Name = "变量名称HMI",
                Category = FieldCategory.Basic,
                Description = "HMI系统中的变量名称，用作点位的唯一标识符",
                ApplicablePointTypes = { PointType.AI, PointType.AO, PointType.DI, PointType.DO },
                IsOptional = false
            },
            ["变量描述"] = new StandardField
            {
                Name = "变量描述",
                Category = FieldCategory.Basic,
                Description = "点位的中文描述信息",
                ApplicablePointTypes = { PointType.AI, PointType.AO, PointType.DI, PointType.DO },
                IsOptional = true
            },
            ["硬点通道号"] = new StandardField
            {
                Name = "硬点通道号",
                Category = FieldCategory.Hardware,
                Description = "硬件点位的通道编号",
                ApplicablePointTypes = { PointType.AI, PointType.AO, PointType.DI, PointType.DO },
                IsOptional = false
            },
            ["量程高限"] = new StandardField
            {
                Name = "量程高限",
                Category = FieldCategory.Range,
                Description = "模拟量的测量上限值",
                ApplicablePointTypes = { PointType.AI, PointType.AO },
                IsOptional = false
            },
            ["量程低限"] = new StandardField
            {
                Name = "量程低限",
                Category = FieldCategory.Range,
                Description = "模拟量的测量下限值",
                ApplicablePointTypes = { PointType.AI, PointType.AO },
                IsOptional = false
            },
            ["PLC绝对地址"] = new StandardField
            {
                Name = "PLC绝对地址",
                Category = FieldCategory.Address,
                Description = "PLC中的绝对内存地址",
                ApplicablePointTypes = { PointType.AI, PointType.AO, PointType.DI, PointType.DO },
                IsOptional = true
            },
            ["SHH值"] = new StandardField
            {
                Name = "SHH值",
                Category = FieldCategory.Alarm,
                Description = "超高报警限值",
                ApplicablePointTypes = { PointType.AI },
                IsOptional = true
            },
            ["SH值"] = new StandardField
            {
                Name = "SH值",
                Category = FieldCategory.Alarm,
                Description = "高报警限值",
                ApplicablePointTypes = { PointType.AI },
                IsOptional = true
            },
            ["SL值"] = new StandardField
            {
                Name = "SL值",
                Category = FieldCategory.Alarm,
                Description = "低报警限值",
                ApplicablePointTypes = { PointType.AI },
                IsOptional = true
            },
            ["SLL值"] = new StandardField
            {
                Name = "SLL值",
                Category = FieldCategory.Alarm,
                Description = "超低报警限值",
                ApplicablePointTypes = { PointType.AI },
                IsOptional = true
            }
        };

        /// <summary>
        /// Scriban内置函数列表
        /// </summary>
        private static readonly HashSet<string> BuiltInFunctions = new HashSet<string>
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

        #region 主要提取方法

        /// <summary>
        /// 提取模板中的所有变量和依赖信息
        /// </summary>
        /// <param name="templateContent">模板内容</param>
        /// <param name="pointType">点位类型，用于字段适用性验证</param>
        /// <returns>提取结果</returns>
        public static ExtractionResult ExtractVariables(string templateContent, PointType? pointType = null)
        {
            var result = new ExtractionResult();

            try
            {
                if (string.IsNullOrWhiteSpace(templateContent))
                {
                    result.Warnings.Add("模板内容为空");
                    return result;
                }

                // 1. 提取基础变量
                ExtractBasicVariables(templateContent, result);

                // 2. 分析函数调用
                ExtractFunctionCalls(templateContent, result);

                // 3. 提取循环结构和变量
                ExtractLoopVariables(templateContent, result);

                // 4. 提取条件表达式
                ExtractConditionalExpressions(templateContent, result);

                // 5. 分类变量（标准字段 vs 自定义字段）
                ClassifyVariables(result, pointType);

                // 6. 构建依赖关系图
                BuildDependencyGraph(templateContent, result);

                // 7. 计算统计信息
                CalculateStatistics(result);

                // 8. 验证字段适用性
                if (pointType.HasValue)
                {
                    ValidateFieldApplicability(result, pointType.Value);
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"提取过程中发生异常: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region 具体提取方法

        /// <summary>
        /// 提取基础变量
        /// </summary>
        private static void ExtractBasicVariables(string templateContent, ExtractionResult result)
        {
            var lines = templateContent.Split('\n');
            var variableMap = new Dictionary<string, VariableInfo>();

            // 匹配Scriban变量 {{ variable }} 或 {{ variable.property }}
            var variablePattern = @"\{\{\s*([a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*(?:\.[a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*)*)\s*(?:\|[^}]*)?\}\}";

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;
                var matches = Regex.Matches(line, variablePattern);

                foreach (Match match in matches)
                {
                    var variableName = match.Groups[1].Value.Trim();
                    
                    // 跳过内置函数和关键字
                    if (IsScribanKeyword(variableName) || IsBuiltInFunction(variableName))
                        continue;

                    if (!variableMap.ContainsKey(variableName))
                    {
                        variableMap[variableName] = new VariableInfo
                        {
                            Name = variableName,
                            Type = DetermineVariableType(variableName)
                        };
                    }

                    variableMap[variableName].UsageLines.Add(lineNumber);
                }
            }

            result.Variables = variableMap.Values.ToList();
        }

        /// <summary>
        /// 提取函数调用
        /// </summary>
        private static void ExtractFunctionCalls(string templateContent, ExtractionResult result)
        {
            var lines = templateContent.Split('\n');

            // 匹配函数调用模式 function_name(params) 或 object.function_name(params)
            var functionPattern = @"([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)\s*\(([^)]*)\)";

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;
                var matches = Regex.Matches(line, functionPattern);

                foreach (Match match in matches)
                {
                    var functionName = match.Groups[1].Value;
                    var paramString = match.Groups[2].Value;
                    
                    var parameters = string.IsNullOrWhiteSpace(paramString) 
                        ? new List<string>() 
                        : paramString.Split(',').Select(p => p.Trim()).ToList();

                    result.FunctionCalls.Add(new FunctionCall
                    {
                        FunctionName = functionName,
                        Parameters = parameters,
                        Line = lineNumber,
                        IsBuiltIn = IsBuiltInFunction(functionName)
                    });
                }
            }
        }

        /// <summary>
        /// 提取循环变量
        /// </summary>
        private static void ExtractLoopVariables(string templateContent, ExtractionResult result)
        {
            var lines = templateContent.Split('\n');
            var loopStack = new Stack<(LoopVariable loop, int depth)>();
            var currentDepth = 0;

            // 匹配for循环：{{ for item in collection }}
            var forPattern = @"\{\{\s*for\s+(\w+)\s+in\s+(\w+)\s*\}\}";
            var endPattern = @"\{\{\s*end\s*\}\}";

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                // 检查for循环开始
                var forMatch = Regex.Match(line, forPattern);
                if (forMatch.Success)
                {
                    currentDepth++;
                    var loopVar = new LoopVariable
                    {
                        IteratorName = forMatch.Groups[1].Value,
                        CollectionName = forMatch.Groups[2].Value,
                        StartLine = lineNumber,
                        NestingLevel = currentDepth
                    };
                    
                    loopStack.Push((loopVar, currentDepth));
                }

                // 检查end标记
                var endMatch = Regex.Match(line, endPattern);
                if (endMatch.Success && loopStack.Count > 0)
                {
                    var (loopVar, depth) = loopStack.Pop();
                    loopVar.EndLine = lineNumber;
                    result.LoopVariables.Add(loopVar);
                    currentDepth = Math.Max(0, currentDepth - 1);

                    // 标记在此循环中使用的变量
                    MarkVariablesInLoop(result.Variables, loopVar);
                }
            }
        }

        /// <summary>
        /// 提取条件表达式
        /// </summary>
        private static void ExtractConditionalExpressions(string templateContent, ExtractionResult result)
        {
            var lines = templateContent.Split('\n');

            // 匹配各种条件表达式
            var conditionalPatterns = new Dictionary<ConditionalType, string>
            {
                { ConditionalType.If, @"\{\{\s*if\s+(.+?)\s*\}\}" },
                { ConditionalType.ElsIf, @"\{\{\s*elsif\s+(.+?)\s*\}\}" },
                { ConditionalType.Case, @"\{\{\s*case\s+(.+?)\s*\}\}" },
                { ConditionalType.When, @"\{\{\s*when\s+(.+?)\s*\}\}" }
            };

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                foreach (var (type, pattern) in conditionalPatterns)
                {
                    var matches = Regex.Matches(line, pattern);
                    foreach (Match match in matches)
                    {
                        var expression = match.Groups[1].Value;
                        var usedVars = ExtractVariablesFromExpression(expression);

                        result.ConditionalExpressions.Add(new ConditionalExpression
                        {
                            Expression = expression,
                            Type = type,
                            UsedVariables = usedVars,
                            Line = lineNumber
                        });

                        // 标记在条件中使用的变量
                        MarkVariablesInCondition(result.Variables, usedVars);
                    }
                }
            }
        }

        /// <summary>
        /// 分类变量
        /// </summary>
        private static void ClassifyVariables(ExtractionResult result, PointType? pointType)
        {
            foreach (var variable in result.Variables)
            {
                if (StandardFieldMap.ContainsKey(variable.Name))
                {
                    variable.Type = VariableType.StandardField;
                    var standardField = StandardFieldMap[variable.Name];
                    standardField.UsageLines = variable.UsageLines;
                    
                    // 检查是否适用于当前点位类型
                    if (pointType.HasValue && !standardField.ApplicablePointTypes.Contains(pointType.Value))
                    {
                        result.Warnings.Add($"字段 '{variable.Name}' 不适用于 {pointType.Value} 类型的点位");
                    }
                    
                    result.RequiredStandardFields.Add(standardField);
                }
                else
                {
                    variable.Type = VariableType.CustomField;
                    result.CustomFields.Add(variable.Name);
                }

                // 设置描述信息
                if (StandardFieldMap.ContainsKey(variable.Name))
                {
                    variable.Description = StandardFieldMap[variable.Name].Description;
                }
            }
        }

        /// <summary>
        /// 构建依赖关系图
        /// </summary>
        private static void BuildDependencyGraph(string templateContent, ExtractionResult result)
        {
            // 分析变量之间的依赖关系
            var lines = templateContent.Split('\n');
            
            foreach (var variable in result.Variables)
            {
                var dependencies = new List<string>();
                
                // 查找使用该变量的上下文，分析其依赖的其他变量
                foreach (var lineNumber in variable.UsageLines)
                {
                    if (lineNumber <= lines.Length)
                    {
                        var line = lines[lineNumber - 1];
                        var contextVars = ExtractVariablesFromExpression(line);
                        
                        foreach (var contextVar in contextVars)
                        {
                            if (contextVar != variable.Name && !dependencies.Contains(contextVar))
                            {
                                dependencies.Add(contextVar);
                            }
                        }
                    }
                }
                
                result.Dependencies[variable.Name] = dependencies;
            }
        }

        /// <summary>
        /// 计算统计信息
        /// </summary>
        private static void CalculateStatistics(ExtractionResult result)
        {
            result.Statistics.TotalVariables = result.Variables.Count;
            result.Statistics.StandardFieldCount = result.RequiredStandardFields.Count;
            result.Statistics.CustomFieldCount = result.CustomFields.Count;
            result.Statistics.FunctionCallCount = result.FunctionCalls.Count;
            result.Statistics.LoopCount = result.LoopVariables.Count;
            result.Statistics.ConditionalCount = result.ConditionalExpressions.Count;
            result.Statistics.MaxNestingDepth = result.LoopVariables.Any() 
                ? result.LoopVariables.Max(l => l.NestingLevel) 
                : 0;

            // 计算复杂度评分
            var score = 0;
            score += result.Variables.Count * 1;           // 变量数量
            score += result.FunctionCalls.Count * 2;      // 函数调用
            score += result.LoopVariables.Count * 5;      // 循环结构
            score += result.ConditionalExpressions.Count * 3; // 条件表达式
            score += result.Statistics.MaxNestingDepth * 3;   // 嵌套深度

            result.Statistics.ComplexityScore = score;
        }

        /// <summary>
        /// 验证字段适用性
        /// </summary>
        private static void ValidateFieldApplicability(ExtractionResult result, PointType pointType)
        {
            foreach (var standardField in result.RequiredStandardFields)
            {
                if (!standardField.ApplicablePointTypes.Contains(pointType))
                {
                    result.Warnings.Add($"标准字段 '{standardField.Name}' 通常不用于 {pointType} 类型的点位");
                }
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 确定变量类型
        /// </summary>
        private static VariableType DetermineVariableType(string variableName)
        {
            if (StandardFieldMap.ContainsKey(variableName))
                return VariableType.StandardField;

            if (variableName.Contains('.'))
                return VariableType.ComplexExpression;

            return VariableType.CustomField;
        }

        /// <summary>
        /// 标记在循环中使用的变量
        /// </summary>
        private static void MarkVariablesInLoop(List<VariableInfo> variables, LoopVariable loop)
        {
            foreach (var variable in variables)
            {
                foreach (var usageLine in variable.UsageLines)
                {
                    if (usageLine >= loop.StartLine && usageLine <= loop.EndLine)
                    {
                        variable.IsUsedInLoop = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 标记在条件中使用的变量
        /// </summary>
        private static void MarkVariablesInCondition(List<VariableInfo> variables, List<string> conditionVars)
        {
            foreach (var variable in variables)
            {
                if (conditionVars.Contains(variable.Name))
                {
                    variable.IsUsedInCondition = true;
                }
            }
        }

        /// <summary>
        /// 从表达式中提取变量
        /// </summary>
        private static List<string> ExtractVariablesFromExpression(string expression)
        {
            var variables = new List<string>();
            var variablePattern = @"\b([a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*(?:\.[a-zA-Z_\u4e00-\u9fa5][a-zA-Z0-9_\u4e00-\u9fa5]*)*)\b";
            var matches = Regex.Matches(expression, variablePattern);

            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value;
                if (!IsScribanKeyword(variableName) && !IsBuiltInFunction(variableName))
                {
                    variables.Add(variableName);
                }
            }

            return variables.Distinct().ToList();
        }

        /// <summary>
        /// 是否为Scriban关键字
        /// </summary>
        private static bool IsScribanKeyword(string name)
        {
            var keywords = new[] { "for", "in", "if", "else", "elsif", "end", "case", "when", "while", "break", "continue", "capture", "assign", "include", "render", "with", "and", "or", "not" };
            return keywords.Contains(name.ToLower());
        }

        /// <summary>
        /// 是否为内置函数
        /// </summary>
        private static bool IsBuiltInFunction(string name)
        {
            return BuiltInFunctions.Contains(name.ToLower());
        }

        #endregion

        #region 公共辅助方法

        /// <summary>
        /// 生成变量使用报告
        /// </summary>
        public static string GenerateVariableReport(ExtractionResult result)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("# 模板变量分析报告");
            report.AppendLine();

            // 统计信息
            report.AppendLine("## 📊 统计信息");
            report.AppendLine($"- 总变量数: **{result.Statistics.TotalVariables}**");
            report.AppendLine($"- 标准字段: **{result.Statistics.StandardFieldCount}**");
            report.AppendLine($"- 自定义字段: **{result.Statistics.CustomFieldCount}**");
            report.AppendLine($"- 函数调用: **{result.Statistics.FunctionCallCount}**");
            report.AppendLine($"- 循环结构: **{result.Statistics.LoopCount}**");
            report.AppendLine($"- 条件表达式: **{result.Statistics.ConditionalCount}**");
            report.AppendLine($"- 复杂度评分: **{result.Statistics.ComplexityScore}**");
            report.AppendLine();

            // 标准字段
            if (result.RequiredStandardFields.Any())
            {
                report.AppendLine("## 📋 标准字段");
                foreach (var field in result.RequiredStandardFields.OrderBy(f => f.Category))
                {
                    report.AppendLine($"- **{field.Name}** ({field.Category})");
                    report.AppendLine($"  - 描述: {field.Description}");
                    report.AppendLine($"  - 使用次数: {field.UsageLines.Count}");
                    report.AppendLine($"  - 使用位置: 第{string.Join(",", field.UsageLines)}行");
                    report.AppendLine();
                }
            }

            // 自定义字段
            if (result.CustomFields.Any())
            {
                report.AppendLine("## 🔧 自定义字段");
                foreach (var field in result.CustomFields)
                {
                    var variable = result.Variables.FirstOrDefault(v => v.Name == field);
                    if (variable != null)
                    {
                        report.AppendLine($"- **{field}**");
                        report.AppendLine($"  - 使用次数: {variable.UsageCount}");
                        report.AppendLine($"  - 使用位置: 第{string.Join(",", variable.UsageLines)}行");
                        if (variable.IsUsedInLoop) report.AppendLine("  - 在循环中使用");
                        if (variable.IsUsedInCondition) report.AppendLine("  - 在条件中使用");
                        report.AppendLine();
                    }
                }
            }

            // 函数调用
            if (result.FunctionCalls.Any())
            {
                report.AppendLine("## ⚙️ 函数调用");
                foreach (var func in result.FunctionCalls.OrderBy(f => f.Line))
                {
                    report.AppendLine($"- **{func.FunctionName}** (第{func.Line}行)");
                    if (func.Parameters.Any())
                    {
                        report.AppendLine($"  - 参数: {string.Join(", ", func.Parameters)}");
                    }
                    if (func.IsBuiltIn)
                    {
                        report.AppendLine("  - 内置函数");
                    }
                    report.AppendLine();
                }
            }

            // 警告信息
            if (result.Warnings.Any())
            {
                report.AppendLine("## ⚠️ 警告信息");
                foreach (var warning in result.Warnings)
                {
                    report.AppendLine($"- {warning}");
                }
                report.AppendLine();
            }

            return report.ToString();
        }

        /// <summary>
        /// 获取缺失的必需字段
        /// </summary>
        public static List<string> GetMissingRequiredFields(ExtractionResult result, PointType pointType)
        {
            var requiredFields = StandardFieldMap.Values
                .Where(f => !f.IsOptional && f.ApplicablePointTypes.Contains(pointType))
                .Select(f => f.Name)
                .ToList();

            var existingFields = result.RequiredStandardFields.Select(f => f.Name).ToList();

            return requiredFields.Except(existingFields).ToList();
        }

        /// <summary>
        /// 获取变量使用建议
        /// </summary>
        public static List<string> GetUsageSuggestions(ExtractionResult result, PointType pointType)
        {
            var suggestions = new List<string>();

            // 检查缺失的常用字段
            var missingFields = GetMissingRequiredFields(result, pointType);
            if (missingFields.Any())
            {
                suggestions.Add($"建议添加以下常用字段: {string.Join(", ", missingFields)}");
            }

            // 检查未使用的变量
            var unusedVars = result.Variables.Where(v => v.UsageCount == 0).ToList();
            if (unusedVars.Any())
            {
                suggestions.Add($"发现未使用的变量: {string.Join(", ", unusedVars.Select(v => v.Name))}");
            }

            // 检查复杂度
            if (result.Statistics.ComplexityScore > 50)
            {
                suggestions.Add("模板复杂度较高，建议考虑拆分为多个简单模板");
            }

            return suggestions;
        }

        #endregion
    }
}