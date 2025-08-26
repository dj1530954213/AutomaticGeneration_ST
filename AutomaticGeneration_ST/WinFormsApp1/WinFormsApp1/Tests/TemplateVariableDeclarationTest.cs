using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomaticGeneration_ST.Tests
{
    /// <summary>
    /// 验证每个主模板都声明了变量模板且对应的变量模板文件存在。
    /// </summary>
    public static class TemplateVariableDeclarationTest
    {
        private static readonly Regex DeclRegex = new(@"子程序变量声明文件\s*[:：]\s*(?<name>[A-Za-z0-9_]+)", RegexOptions.Compiled);

        public static TestResult Run(string templatesRoot)
        {
            var result = new TestResult
            {
                TestName = "模板变量声明完整性测试",
                StartTime = DateTime.Now,
                Success = true
            };

            var failures = new List<string>();

            if (!Directory.Exists(templatesRoot))
            {
                result.Success = false;
                result.Message = $"模板目录不存在: {templatesRoot}";
                result.EndTime = DateTime.Now;
                return result;
            }

            var allTemplates = Directory.GetFiles(templatesRoot, "*.scriban", SearchOption.AllDirectories)
                                         .Where(p => !p.EndsWith("_VARIABLE.scriban", StringComparison.OrdinalIgnoreCase))
                                         .ToList();

            foreach (var tpl in allTemplates)
            {
                var text = File.ReadAllText(tpl);
                var match = DeclRegex.Match(text);
                if (!match.Success)
                {
                    failures.Add($"❌ 未找到声明: {Relative(templatesRoot, tpl)}");
                    continue;
                }

                var name = match.Groups["name"].Value;
                var dir = Path.GetDirectoryName(tpl)!;
                var candidates = new[]
                {
                    Path.Combine(dir, name + ".scriban"),
                    Path.Combine(dir, name + "_VARIABLE.scriban")
                };
                if (!candidates.Any(File.Exists))
                {
                    failures.Add($"❌ 变量模板不存在: {Relative(templatesRoot, tpl)} -> {string.Join(" | ", candidates.Select(c => Relative(templatesRoot, c)))}");
                }
            }

            if (failures.Any())
            {
                result.Success = false;
                result.Message = $"共有 {failures.Count} 个模板未通过校验";
                result.Details = failures;
            }
            else
            {
                result.Message = "所有模板均声明完整，变量模板文件存在";
            }

            result.EndTime = DateTime.Now;
            return result;
        }

        private static string Relative(string root, string path) => Path.GetRelativePath(root, path);
    }

}
