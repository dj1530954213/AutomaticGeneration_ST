using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using WinFormsApp1.Utils;

namespace AutomaticGeneration_ST.Services.Generation.Implementations
{
    public class ScribanIoMappingGenerator : IIoMappingGenerator
    {
        public GenerationResult Generate(string moduleType, IEnumerable<Models.Point> pointsInGroup, Template template)
        {
            // 步骤 1: 为模板准备上下文 (Context)
            var scriptObject = new ScriptObject();
            var pointsList = pointsInGroup.ToList();
            
            // 关键：将点位列表赋值给名为 "points" 的变量，以匹配模板中的 `for point in points`
            scriptObject.Add("points", pointsList);

            var context = new TemplateContext()
            {
                // 允许C#属性名（如HmiTagName）在Scriban中自动转换为小写下划线（hmi_tag_name）
                MemberRenamer = member => member.Name.ToSnakeCase()
            };

            // *** 增强的IO映射上下文 ***

            // 使用ScriptObject.Import来正确注册函数
            scriptObject.Import("get_address", new Func<Models.Point, string>((p) => 
            {
                if (p == null) return "(* 点位未找到 *)";
                
                // 对于IO映射，优先使用PlcAbsoluteAddress并进行转换
                if (!string.IsNullOrWhiteSpace(p.PlcAbsoluteAddress))
                {
                    // 使用ChannelConverter将通道位号转换为硬点通道号
                    // 例如: 1_1_AI_0 -> DPIO_2_1_2_1
                    return ChannelConverter.ConvertToHardChannel(p.PlcAbsoluteAddress);
                }
                
                return p.HmiTagName ?? "(* 地址未找到 *)";
            }));

            // 获取安全的默认值
            scriptObject.Import("safe_value", new Func<object, object, object>((value, defaultValue) =>
            {
                return value ?? defaultValue;
            }));

            // 格式化数值的函数
            scriptObject.Import("format_number", new Func<double?, string>((value) =>
            {
                return value?.ToString("F2") ?? "0.0";
            }));

            // 生成标准报警点名的函数
            scriptObject.Import("alarm_point", new Func<string, string, string>((tagName, alarmType) =>
            {
                return $"{tagName}_{alarmType?.ToUpper()}";
            }));

            // IO映射统计信息
            var ioStats = new ScriptObject();
            ioStats.Add("module_type", moduleType);
            ioStats.Add("point_count", pointsList.Count);
            ioStats.Add("generation_time", DateTime.Now);
            ioStats.Add("generation_date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            scriptObject.Add("io_stats", ioStats);

            // 按场站分组的点位（如果有场站信息）
            var pointsByStation = new ScriptObject();
            var stationGroups = pointsList.GroupBy(p => p.StationName ?? "默认场站");
            foreach (var stationGroup in stationGroups)
            {
                pointsByStation.Add(stationGroup.Key, stationGroup.ToList());
            }
            scriptObject.Add("points_by_station", pointsByStation);

            // 为AI类型提供专门的报警相关函数
            if (moduleType.ToUpper() == "AI")
            {
                // 检查点位是否有报警配置
                scriptObject.Import("has_alarm_config", new Func<Models.Point, bool>((p) =>
                {
                    return p != null && (p.SHH_Value.HasValue || p.SH_Value.HasValue || 
                                       p.SL_Value.HasValue || p.SLL_Value.HasValue);
                }));

                // 获取报警限值
                scriptObject.Import("get_alarm_limits", new Func<Models.Point, string>((p) =>
                {
                    if (p == null) return "";
                    var limits = new List<string>();
                    if (p.SHH_Value.HasValue) limits.Add($"HH:{p.SHH_Value.Value:F2}");
                    if (p.SH_Value.HasValue) limits.Add($"H:{p.SH_Value.Value:F2}");
                    if (p.SL_Value.HasValue) limits.Add($"L:{p.SL_Value.Value:F2}");
                    if (p.SLL_Value.HasValue) limits.Add($"LL:{p.SLL_Value.Value:F2}");
                    return string.Join(", ", limits);
                }));
            }

            context.PushGlobal(scriptObject);

            // 步骤 2: 渲染模板
            var rawContent = template.Render(context);
            
            // 步骤 3: 过滤掉模板中的分类标识行
            var content = FilterClassificationLines(rawContent);

            // 步骤 4: 封装结果
            return new GenerationResult
            {
                FileName = $"{moduleType}_Mapping.st",
                Content = content,
                Category = "IO"
            };
        }
        
        /// <summary>
        /// 过滤掉模板生成内容中的分类标识行
        /// </summary>
        /// <param name="content">原始生成内容</param>
        /// <returns>过滤后的内容</returns>
        private static string FilterClassificationLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 过滤掉程序名称和变量类型标识行
                if (trimmedLine.StartsWith("程序名称:", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("变量类型:", StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 跳过这些行
                }
                
                filteredLines.Add(line);
            }

            return string.Join(Environment.NewLine, filteredLines);
        }
    }
}

public static class StringExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = "";
        for (int i = 0; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]) && i > 0)
            {
                result += "_";
            }
            result += char.ToLower(input[i]);
        }
        return result;
    }
}