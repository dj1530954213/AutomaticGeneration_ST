using AutomaticGeneration_ST.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// ST代码分析器 - 从生成的ST代码中提取函数调用和变量信息
    /// </summary>
    public class STCodeAnalyzer
    {
        /// <summary>
        /// 从ST代码中提取变量表条目
        /// </summary>
        /// <param name="stCode">生成的ST代码</param>
        /// <param name="templateMetadata">对应的模板元数据</param>
        /// <returns>变量表条目列表</returns>
        public List<VariableTableEntry> ExtractVariableEntries(string stCode, TemplateMetadata templateMetadata)
        {
            Console.WriteLine($"[STCodeAnalyzer] 开始提取变量条目，模板: {templateMetadata?.ProgramName}");
            var entries = new List<VariableTableEntry>();

            if (string.IsNullOrWhiteSpace(stCode) || templateMetadata == null)
            {
                Console.WriteLine($"[STCodeAnalyzer] ST代码为空或模板元数据为空，跳过处理");
                return entries;
            }

            try
            {
                Console.WriteLine($"[STCodeAnalyzer] ST代码长度: {stCode.Length} 字符");
                
                // === 1. 优先尝试解析变量块 ([ ... ]) ===
                var blockMatches = Regex.Matches(stCode, "\\[[^\\]]+\\]", RegexOptions.Singleline);
                if (blockMatches.Count > 0)
                {
                    Console.WriteLine($"[STCodeAnalyzer] 检测到 {blockMatches.Count} 个变量块, 使用 VariableBlockParser 解析");
                    var blockContents = blockMatches.Cast<Match>().Select(m => m.Value).ToList();
                    try
                    {
                        var blockEntries = VariableBlocks.VariableBlockParser.Parse(blockContents);
                        foreach (var be in blockEntries)
                        {
                            be.ProgramName = templateMetadata?.ProgramName ?? string.Empty;
                            entries.Add(be);
                        }
                        Console.WriteLine($"[STCodeAnalyzer] 变量块解析得到 {blockEntries.Count} 条变量, 直接返回");
                        return entries;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[STCodeAnalyzer] 解析变量块失败: {ex.Message}");
                    }
                }

                // 只有具有变量类型的模板才需要继续正则匹配函数调用
                if (string.IsNullOrWhiteSpace(templateMetadata.VariableType))
                {
                    Console.WriteLine($"[STCodeAnalyzer] 模板 {templateMetadata.ProgramName} 没有变量类型，且未检测到变量块，跳过处理");
                    return entries;
                }

                Console.WriteLine($"[STCodeAnalyzer] 未检测到变量块，改用正则提取函数调用，模板: {templateMetadata.ProgramName}");
                
                // === 2. 正则匹配函数调用和 *_MID 变量 ===
                var functionCallPattern = @"^[\s]*([A-Za-z][A-Za-z0-9_]*)\s*\(";
                var midPattern = @"\b([A-Za-z][A-Za-z0-9_]*?_MID)\b";

                var funcMatches = Regex.Matches(stCode, functionCallPattern, RegexOptions.Multiline);
                var midMatches = Regex.Matches(stCode, midPattern, RegexOptions.Multiline);

                Console.WriteLine($"[STCodeAnalyzer] 函数调用匹配到 {funcMatches.Count} 个, _MID 匹配到 {midMatches.Count} 个");

                var allNames = new HashSet<string>();
                foreach (Match m in funcMatches) allNames.Add(m.Groups[1].Value.Trim());
                foreach (Match m in midMatches) allNames.Add(m.Groups[1].Value.Trim());

                int validCount = 0;
                int invalidCount = 0;

                foreach (var functionName in allNames)
                {
                    Console.WriteLine($"[STCodeAnalyzer] 检查函数调用: {functionName}");

                    // 过滤掉不需要的函数调用（如注释中的内容）
                    if (IsValidFunctionCall(functionName, stCode))
                    {
                        Console.WriteLine($"[STCodeAnalyzer] 找到有效函数调用: {functionName}");
                        validCount++;
                        
                        var entry = new VariableTableEntry
                        {
                            ProgramName = $"{templateMetadata.ProgramName}(PRG)",
                            VariableName = functionName,
                            DirectAddress = string.Empty,
                            VariableDescription = string.Empty,
                            VariableType = GetVariableType(templateMetadata, functionName),
                            InitialValue = GetInitialValue(templateMetadata, functionName),
                            PowerFailureProtection = "FALSE",
                            SOEEnable = "FALSE"
                        };

                        entries.Add(entry);
                        Console.WriteLine($"[STCodeAnalyzer] 创建变量条目: {entry.VariableName} (类型: {entry.VariableType})");
                    }
                    else
                    {
                        Console.WriteLine($"[STCodeAnalyzer] 跳过无效函数调用: {functionName}");
                        invalidCount++;
                    }
                }
                
                Console.WriteLine($"[STCodeAnalyzer] 函数调用统计 - 有效: {validCount}, 无效: {invalidCount}");

                // 去重
                var beforeDeduplication = entries.Count;
                entries = entries
                    .GroupBy(e => e.VariableName)
                    .Select(g => g.First())
                    .ToList();

                Console.WriteLine($"[STCodeAnalyzer] 去重前: {beforeDeduplication} 个条目，去重后: {entries.Count} 个条目");
                Console.WriteLine($"[STCodeAnalyzer] 最终提取到 {entries.Count} 个有效的变量条目");
                
                if (entries.Any())
                {
                    Console.WriteLine($"[STCodeAnalyzer] 变量名称列表: {string.Join(", ", entries.Select(e => e.VariableName))}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STCodeAnalyzer] 分析ST代码失败: {ex.Message}");
                Console.WriteLine($"[STCodeAnalyzer] 异常堆栈: {ex.StackTrace}");
            }

            return entries;
        }

        /// <summary>
        /// 验证是否为有效的函数调用
        /// </summary>
        /// <param name="functionName">函数名</param>
        /// <param name="stCode">完整的ST代码</param>
        /// <param name="matchIndex">匹配位置</param>
        /// <returns>是否为有效函数调用</returns>
        private bool IsValidFunctionCall(string functionName, string stCode, int matchIndex)
        {
            return IsValidFunctionCallInternal(functionName, stCode, matchIndex);
        }

        private bool IsValidFunctionCall(string functionName, string stCode)
        {
            int idx = stCode.IndexOf(functionName, StringComparison.Ordinal);
            return IsValidFunctionCallInternal(functionName, stCode, idx);
        }

        private string GetVariableType(TemplateMetadata metadata, string variableName)
        {
            foreach (var kv in metadata.VariableMetaMap)
            {
                if (variableName.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value.VariableType;
            }
            return metadata.VariableType;
        }

        private string GetInitialValue(TemplateMetadata metadata, string variableName)
        {
            foreach (var kv in metadata.VariableMetaMap)
            {
                if (variableName.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value.InitialValue;
            }
            return metadata.InitializationValue;
        }

        private bool IsValidFunctionCallInternal(string functionName, string stCode, int matchIndex)
        {
            // 排除常见的非函数调用情况
            if (string.IsNullOrWhiteSpace(functionName))
                return false;

            // 排除过短的名称（可能是语法元素）
            if (functionName.Length < 3)
                return false;

            // 排除常见的ST语言关键字和操作符
            var excludeKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "IF", "THEN", "ELSE", "END_IF", "FOR", "TO", "DO", "END_FOR",
                "WHILE", "END_WHILE", "CASE", "OF", "END_CASE", "VAR", "END_VAR",
                "FUNCTION", "END_FUNCTION", "PROGRAM", "END_PROGRAM",
                "T", "PT", "IN", "OUT", "SET", "RESET", "CLK", "Q", "ET", "M"
            };

            if (excludeKeywords.Contains(functionName))
                return false;

            // 检查是否在注释中
            if (IsInComment(stCode, matchIndex))
                return false;

            // 检查函数名是否符合预期的命名模式
            // 有效的函数调用通常包含字母、数字、下划线，且以字母开头
            if (!Regex.IsMatch(functionName, @"^[A-Za-z][A-Za-z0-9_]*$"))
                return false;

            return true;
        }

        /// <summary>
        /// 检查指定位置是否在注释中
        /// </summary>
        /// <param name="stCode">ST代码</param>
        /// <param name="position">检查位置</param>
        /// <returns>是否在注释中</returns>
        private bool IsInComment(string stCode, int position)
        {
            // 检查是否在 (* ... *) 注释中
            var beforePosition = stCode.Substring(0, Math.Min(position, stCode.Length));
            var lastCommentStart = beforePosition.LastIndexOf("(*");
            var lastCommentEnd = beforePosition.LastIndexOf("*)");

            if (lastCommentStart >= 0 && (lastCommentEnd < 0 || lastCommentStart > lastCommentEnd))
            {
                // 在未闭合的注释中
                return true;
            }

            // 检查是否在 // 单行注释中
            var lines = stCode.Split('\n');
            int currentPos = 0;
            foreach (var line in lines)
            {
                if (position >= currentPos && position < currentPos + line.Length)
                {
                    var lineContent = line.Substring(0, position - currentPos);
                    if (lineContent.Contains("//"))
                    {
                        return true;
                    }
                    break;
                }
                currentPos += line.Length + 1; // +1 for newline
            }

            return false;
        }

        /// <summary>
        /// 批量分析多个ST代码文件
        /// </summary>
        /// <param name="stCodesByTemplate">按模板分组的ST代码字典</param>
        /// <param name="templateMetadataDict">模板元数据字典</param>
        /// <returns>按模板分组的变量表条目</returns>
        public Dictionary<string, List<VariableTableEntry>> AnalyzeMultipleSTCodes(
            Dictionary<string, List<string>> stCodesByTemplate,
            Dictionary<string, TemplateMetadata> templateMetadataDict)
        {
            Console.WriteLine($"[STCodeAnalyzer] 开始批量分析ST代码，模板组数: {stCodesByTemplate.Count}");
            Console.WriteLine($"[STCodeAnalyzer] 可用模板元数据: {string.Join(", ", templateMetadataDict.Keys)}");
            
            var results = new Dictionary<string, List<VariableTableEntry>>();

            foreach (var templateGroup in stCodesByTemplate)
            {
                var templateName = templateGroup.Key;
                var stCodes = templateGroup.Value;

                Console.WriteLine($"[STCodeAnalyzer] 正在处理模板组: {templateName}，包含 {stCodes.Count} 个ST代码");

                // 查找对应的模板元数据 - 尝试多种匹配方式
                var templateMetadata = templateMetadataDict.Values
                    .FirstOrDefault(tm => tm.ProgramName.Equals(templateName, StringComparison.OrdinalIgnoreCase));

                // 如果按程序名称没找到，尝试按模板字典的Key匹配
                if (templateMetadata == null)
                {
                    templateMetadataDict.TryGetValue(templateName, out templateMetadata);
                }

                // 如果还没找到，尝试部分匹配
                if (templateMetadata == null)
                {
                    templateMetadata = templateMetadataDict.Values
                        .FirstOrDefault(tm => templateName.Contains(tm.ProgramName, StringComparison.OrdinalIgnoreCase) ||
                                             tm.ProgramName.Contains(templateName, StringComparison.OrdinalIgnoreCase));
                }

                if (templateMetadata == null)
                {
                    Console.WriteLine($"[STCodeAnalyzer] 未找到模板 {templateName} 的元数据，跳过处理");
                    Console.WriteLine($"[STCodeAnalyzer] 可用的模板元数据: {string.Join(", ", templateMetadataDict.Keys)}");
                    continue;
                }

                Console.WriteLine($"[STCodeAnalyzer] 找到匹配的模板元数据: {templateMetadata.ProgramName}");

                var allEntries = new List<VariableTableEntry>();

                // 分析该模板下的所有ST代码
                Console.WriteLine($"[STCodeAnalyzer] 开始分析 {stCodes.Count} 个ST代码文件");
                for (int i = 0; i < stCodes.Count; i++)
                {
                    var stCode = stCodes[i];
                    Console.WriteLine($"[STCodeAnalyzer] 分析第 {i + 1} 个ST代码 (长度: {stCode.Length})");
                    var entries = ExtractVariableEntries(stCode, templateMetadata);
                    Console.WriteLine($"[STCodeAnalyzer] 第 {i + 1} 个ST代码提取到 {entries.Count} 个条目");
                    allEntries.AddRange(entries);
                }

                // 去重并排序
                Console.WriteLine($"[STCodeAnalyzer] 总计提取到 {allEntries.Count} 个条目，开始去重");
                var uniqueEntries = allEntries
                    .GroupBy(e => e.VariableName)
                    .Select(g => g.First())
                    .OrderBy(e => e.VariableName)
                    .ToList();

                Console.WriteLine($"[STCodeAnalyzer] 去重后得到 {uniqueEntries.Count} 个唯一条目");
                
                if (uniqueEntries.Any())
                {
                    results[templateName] = uniqueEntries;
                    Console.WriteLine($"[STCodeAnalyzer] 模板 {templateName} 分析完成，最终条目数: {uniqueEntries.Count}");
                }
                else
                {
                    Console.WriteLine($"[STCodeAnalyzer] 模板 {templateName} 没有变量条目");
                }
            }

            Console.WriteLine($"[STCodeAnalyzer] 批量分析完成，有效模板数: {results.Count}");
            Console.WriteLine($"[STCodeAnalyzer] 有效模板列表: {string.Join(", ", results.Keys)}");
            
            return results;
        }
    }
}