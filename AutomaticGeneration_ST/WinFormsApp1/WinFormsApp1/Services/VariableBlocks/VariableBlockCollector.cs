using AutomaticGeneration_ST.Models;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomaticGeneration_ST.Services.VariableBlocks
{
    /// <summary>
    /// 按照主模板中的声明(子程序变量:XXX) 收集并渲染对应的 *VARIABLE.scriban 文件。
    /// </summary>
    public static class VariableBlockCollector
    {
        private static readonly Regex DeclRegex = new(@"子程序变量\s*[:：]\s*(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled);

        /// <summary>
        /// 收集变量块
        /// </summary>
        /// <param name="mainTemplatePath">主模板文件路径</param>
        /// <param name="points">传入的点位列表</param>
        /// <returns>渲染后的变量块字符串集合</returns>
        public static List<string> Collect(string mainTemplatePath, IEnumerable<Models.Point> points)
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
            var results = new List<string>();

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
                    throw new FileNotFoundException($"变量模板 '{tplName}' 未找到 (路径: {string.Join(";", fileCandidates)})");

                foreach (var point in points)
                {
                    var block = VariableBlockRenderer.Render(varTemplatePath, point);
                    if (!string.IsNullOrWhiteSpace(block))
                        results.Add(block);
                }
            }
            return results;
        }
    }
}
