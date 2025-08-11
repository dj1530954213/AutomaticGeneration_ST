using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 工作表定位服务实现类 - 提供智能工作表查找和匹配功能
    /// </summary>
    public class WorksheetLocatorService : IWorksheetLocatorService
    {
        private readonly IExcelWorkbookParser _excelParser;

        // 预定义的工作表别名映射
        private static readonly Dictionary<string, string[]> WorksheetAliases = new()
        {
            ["IO点表"] = new[] { "IO点表", "IO", "IO表", "Points", "IO Points", "Input Output", "点位表", "点表" },
            ["设备分类表"] = new[] { "设备分类表", "设备分类", "分类表", "Device Classification", "Devices", "Device", "设备表", "设备" },
            ["阀门"] = new[] { "阀门", "Valve", "Valves", "阀" },
            ["调节阀"] = new[] { "调节阀", "Control Valve", "CV", "调节", "控制阀" },
            ["可燃气体探测器"] = new[] { "可燃气体探测器", "气体探测器", "Gas Detector", "Gas", "探测器", "可燃气体" },
            ["低压开关柜"] = new[] { "低压开关柜", "开关柜", "Switchgear", "LV Panel", "低压柜" },
            ["撇装机柜"] = new[] { "撇装机柜", "机柜", "Cabinet", "Skid", "撇装" },
            ["加臭"] = new[] { "加臭", "Odorizer", "Odorant", "臭化" },
            ["恒电位仪"] = new[] { "恒电位仪", "Potentiostat", "电位仪" }
        };

        public WorksheetLocatorService(IExcelWorkbookParser excelParser)
        {
            _excelParser = excelParser ?? throw new ArgumentNullException(nameof(excelParser));
        }

        public string LocateWorksheet(string filePath, string expectedName)
        {
            var validation = ValidateWorksheet(filePath, expectedName);
            return validation.IsFound ? validation.ActualName : null;
        }

        public string LocateWorksheetByAliases(string filePath, IEnumerable<string> aliases)
        {
            if (aliases == null || !aliases.Any())
                return null;

            foreach (var alias in aliases)
            {
                var result = LocateWorksheet(filePath, alias);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }

            return null;
        }

        public WorksheetValidationResult ValidateWorksheet(string filePath, string expectedName)
        {
            var result = new WorksheetValidationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(expectedName))
                {
                    result.ErrorMessage = "Excel文件路径或工作表名称不能为空";
                    return result;
                }

                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = $"Excel文件不存在: {filePath}";
                    return result;
                }

                var availableSheets = _excelParser.GetWorksheetNames(filePath);
                result.AvailableWorksheets = availableSheets;

                if (availableSheets == null || !availableSheets.Any())
                {
                    result.ErrorMessage = "Excel文件中没有找到任何工作表";
                    return result;
                }

                // 步骤1: 精确匹配
                var exactMatch = availableSheets.FirstOrDefault(s => s == expectedName);
                if (!string.IsNullOrEmpty(exactMatch))
                {
                    result.IsFound = true;
                    result.ActualName = exactMatch;
                    result.MatchType = WorksheetMatchType.Exact;
                    return result;
                }

                // 步骤2: 忽略大小写匹配
                var ignoreCaseMatch = availableSheets.FirstOrDefault(s => 
                    string.Equals(s, expectedName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(ignoreCaseMatch))
                {
                    result.IsFound = true;
                    result.ActualName = ignoreCaseMatch;
                    result.MatchType = WorksheetMatchType.IgnoreCase;
                    return result;
                }

                // 步骤3: 忽略空格和特殊字符匹配
                var normalizedExpected = NormalizeSheetName(expectedName);
                var whitespaceMatch = availableSheets.FirstOrDefault(s => 
                    NormalizeSheetName(s) == normalizedExpected);
                if (!string.IsNullOrEmpty(whitespaceMatch))
                {
                    result.IsFound = true;
                    result.ActualName = whitespaceMatch;
                    result.MatchType = WorksheetMatchType.IgnoreWhitespace;
                    return result;
                }

                // 步骤4: 模糊匹配（包含关系）
                var fuzzyMatch = availableSheets.FirstOrDefault(s => 
                    s.Contains(expectedName, StringComparison.OrdinalIgnoreCase) ||
                    expectedName.Contains(s, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(fuzzyMatch))
                {
                    result.IsFound = true;
                    result.ActualName = fuzzyMatch;
                    result.MatchType = WorksheetMatchType.Fuzzy;
                    return result;
                }

                // 步骤5: 别名匹配
                var aliasMatch = FindByAliases(availableSheets, expectedName);
                if (!string.IsNullOrEmpty(aliasMatch))
                {
                    result.IsFound = true;
                    result.ActualName = aliasMatch;
                    result.MatchType = WorksheetMatchType.Alias;
                    return result;
                }

                // 未找到匹配
                result.MatchType = WorksheetMatchType.NotFound;
                result.ErrorMessage = $"在Excel文件中未找到名为'{expectedName}'的工作表。\n" +
                                    $"可用的工作表: {string.Join(", ", availableSheets)}";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"验证工作表时发生错误: {ex.Message}";
                return result;
            }
        }

        public List<string> GetAvailableWorksheetNames(string filePath)
        {
            try
            {
                return _excelParser.GetWorksheetNames(filePath) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 根据别名查找工作表
        /// </summary>
        private string FindByAliases(List<string> availableSheets, string expectedName)
        {
            // 检查是否有预定义的别名
            if (WorksheetAliases.ContainsKey(expectedName))
            {
                var aliases = WorksheetAliases[expectedName];
                foreach (var alias in aliases)
                {
                    var match = availableSheets.FirstOrDefault(s => 
                        string.Equals(s, alias, StringComparison.OrdinalIgnoreCase) ||
                        NormalizeSheetName(s) == NormalizeSheetName(alias));
                    if (!string.IsNullOrEmpty(match))
                    {
                        return match;
                    }
                }
            }

            // 反向查找：检查expectedName是否是某个主名称的别名
            foreach (var kvp in WorksheetAliases)
            {
                if (kvp.Value.Any(alias => string.Equals(alias, expectedName, StringComparison.OrdinalIgnoreCase)))
                {
                    // 尝试找到主名称或其他别名
                    foreach (var candidate in kvp.Value)
                    {
                        var match = availableSheets.FirstOrDefault(s => 
                            string.Equals(s, candidate, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(match))
                        {
                            return match;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 标准化工作表名称（去除空格、特殊字符、转为小写）
        /// </summary>
        private string NormalizeSheetName(string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
                return string.Empty;

            // 移除空格、制表符、换行符等空白字符
            var normalized = Regex.Replace(sheetName, @"\s+", "");
            
            // 移除常见的特殊字符
            normalized = Regex.Replace(normalized, @"[\(\)\[\]\-_、，（）【】]", "");
            
            // 转为小写以便比较
            return normalized.ToLowerInvariant();
        }
    }
}