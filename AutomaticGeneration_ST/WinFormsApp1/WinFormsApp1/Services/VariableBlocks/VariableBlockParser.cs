using AutomaticGeneration_ST.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomaticGeneration_ST.Services.VariableBlocks
{
    /// <summary>
    /// 将渲染后的变量块字符串解析为 <see cref="VariableTableEntry"/> 列表。
    /// 变量块格式示例：
    /// [
    /// 变量名称:DATA_CONVERT_BY_BYTE_XXX
    /// 变量类型:DATA_CONVERT_BY_BYTE
    /// 初始值:( ... )
    /// ]
    /// 一对 [] 表示一条记录，块内每行 "键:值"。
    /// </summary>
    public static class VariableBlockParser
    {
        private static readonly Regex BlockRegex = new(@"\[([^\]]+)\]", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex LineRegex = new(@"^\s*(?<key>[^:]+)\s*:\s*(?<value>.+)$", RegexOptions.Compiled);

        public static List<VariableTableEntry> Parse(IEnumerable<string> blockContents)
        {
            var entries = new List<VariableTableEntry>();
            foreach (var content in blockContents)
            {
                if (string.IsNullOrWhiteSpace(content)) continue;
                foreach (Match blockMatch in BlockRegex.Matches(content))
                {
                    var blockBody = blockMatch.Groups[1].Value;
                    var lines = blockBody
                        .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim());

                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var line in lines)
                    {
                        var m = LineRegex.Match(line);
                        if (m.Success)
                        {
                            var key = m.Groups["key"].Value.Trim();
                            var value = m.Groups["value"].Value.Trim();
                            dict[key] = value;
                        }
                    }

                    // 必须包含 变量名称 & 变量类型 & 初始值
                    if (!dict.TryGetValue("变量名称", out var varName)) continue;
                    dict.TryGetValue("变量类型", out var varType);
                    dict.TryGetValue("初始值", out var initVal);

                    var entry = new VariableTableEntry
                    {
                        ProgramName = string.Empty, // 稍后由调用方填充
                        VariableName = varName,
                        VariableType = varType ?? string.Empty,
                        InitialValue = initVal ?? string.Empty,
                        DirectAddress = string.Empty,
                        VariableDescription = string.Empty
                    };
                    entries.Add(entry);
                }
            }
            return entries;
        }
    }
}
