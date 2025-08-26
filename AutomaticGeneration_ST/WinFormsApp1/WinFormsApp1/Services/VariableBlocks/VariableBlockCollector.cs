using AutomaticGeneration_ST.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.VariableBlocks
{
    /// <summary>
    /// 按照主模板中的声明(子程序变量:XXX) 收集并渲染对应的 *VARIABLE.scriban 文件。
    /// </summary>
    public static class VariableBlockCollector
    {
        private static readonly Regex DeclRegex = new(@"子程序变量(?:声明文件)?\s*[:：]\s*(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled);

        /// <summary>
        /// 收集变量块
        /// </summary>
        /// <param name="mainTemplatePath">主模板文件路径</param>
        /// <param name="points">传入的点位列表</param>
        /// <returns>渲染后的变量块字符串集合</returns>
        public static List<string> Collect(string mainTemplatePath, IEnumerable<object> points)
        {
            if (!File.Exists(mainTemplatePath))
                throw new FileNotFoundException($"主模板不存在: {mainTemplatePath}");

            var mainDir = Path.GetDirectoryName(mainTemplatePath)!;
            var templateText = File.ReadAllText(mainTemplatePath);
            var varTemplateNames = DeclRegex.Matches(templateText)
                                            .Cast<Match>()
                                            .Select(m => m.Groups["name"].Value)
                                            .Distinct()
                                            .ToList();
            var logger = LogService.Instance;
            logger.LogInfo($"[VariableBlockCollector] 开始扫描主模板: {mainTemplatePath}");
            logger.LogInfo($"[VariableBlockCollector] 检测到变量模板标识: {(varTemplateNames.Any() ? string.Join(", ", varTemplateNames) : "<空>")}");

            var results = new List<string>();

            if (!varTemplateNames.Any())
            {
                // 未声明子程序变量模板 => 不需要变量渲染，按设计静默跳过
                logger.LogInfo($"[VariableBlockCollector] 主模板 {Path.GetFileName(mainTemplatePath)} 未找到任何 子程序变量 声明行，跳过变量模板渲染");
                return results;
            }

            foreach (var tplName in varTemplateNames)
            {
                // 文件命名规则: <name>.scriban 或 <name>_VARIABLE.scriban?
                var fileCandidates = new[]
                {
                    Path.Combine(mainDir, tplName + ".scriban"),
                    Path.Combine(mainDir, tplName + "_VARIABLE.scriban")
                };
                var varTemplatePath = fileCandidates.FirstOrDefault(File.Exists);
                if (varTemplatePath == null)
                {
                    // 严格模式：当主模板声明了变量模板但找不到对应文件时，立即抛出错误
                    var candidatesMsg = string.Join(";", fileCandidates);
                    logger.LogError($"[VariableBlockCollector] 变量模板 '{tplName}' 未找到 (路径: {candidatesMsg})");
                    throw new FileNotFoundException($"变量模板 '{tplName}' 未找到 (候选: {candidatesMsg})");
                }

                foreach (var point in points)
                {
                    var block = VariableBlockRenderer.Render(varTemplatePath, point);
                    if (!string.IsNullOrWhiteSpace(block))
                    {
                        results.Add(block);
                        LogService.Instance.LogInfo($"[VariableBlockRendered] 模板 {tplName}:\n{block}\n----");
                    }
                }
            }
            if (results.Count == 0)
            {
                // 严格模式：声明了变量模板但渲染结果为空，直接报错，避免后续出现不一致的变量表
                var fileName = Path.GetFileName(mainTemplatePath);
                var msg = $"主模板 {fileName} 声明了变量模板，但渲染结果为空 (points.Count={points.Count()})";
                logger.LogError($"[VariableBlockCollector] {msg}");
                throw new InvalidOperationException(msg);
            }
            return results;
        }
    }
}
