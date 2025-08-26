using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using AutomaticGeneration_ST.Models;
using WinFormsApp1.Utils;

namespace AutomaticGeneration_ST.Services.VariableBlocks
{
    /// <summary>
    /// 负责渲染 *VARIABLE.scriban 模板，生成包裹于 [ ... ] 的变量块字符串。
    /// </summary>
    public static class VariableBlockRenderer
    {
        /// <summary>
        /// 渲染变量块模板。
        /// </summary>
        /// <param name="templatePath">*_VARIABLE.scriban 文件路径</param>
        /// <param name="point">点位对象（可为 null）。模板中通过 {{ point.xxx }} 访问</param>
        /// <param name="globals">额外注入的全局上下文（例如 device_tag 等可直接 {{ device_tag }} 访问）</param>
        /// <returns>渲染后的字符串（包含外层 [ ]）</returns>
        public static string Render(string templatePath, object? point, IDictionary<string, object>? globals = null)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"变量模板不存在: {templatePath}");

            var templateText = File.ReadAllText(templatePath);
            if (string.IsNullOrWhiteSpace(templateText))
                throw new ArgumentException($"变量模板为空: {templatePath}");

            var template = Template.Parse(templateText);
            if (template.HasErrors)
                throw new InvalidOperationException($"变量模板解析错误: {string.Join(",", template.Messages)}");

            // 准备 Scriban 上下文
            var scriptObject = new ScriptObject();
            if (point != null)
            {
                scriptObject.Add("point", point);
            }
            if (globals != null)
            {
                foreach (var kv in globals)
                {
                    scriptObject[kv.Key] = kv.Value;
                }
            }

            var context = new TemplateContext
            {
                MemberRenamer = member => member.Name.ToSnakeCase()
            };
            context.PushGlobal(scriptObject);

            var result = template.Render(context);
            return result?.Trim();
        }

    }
}
