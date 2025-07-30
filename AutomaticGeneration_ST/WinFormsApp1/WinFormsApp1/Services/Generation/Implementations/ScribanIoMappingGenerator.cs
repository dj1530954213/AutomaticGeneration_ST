using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using Scriban;
using Scriban.Runtime;
using System.Collections.Generic;
using System.Linq;

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

            // 获取点位地址的智能函数
            Func<Models.Point, string> getAddressFunc = (p) => 
            {
                if (p == null) return "(* 点位未找到 *)";
                return p.PointType == "硬点" ? (p.PlcAbsoluteAddress ?? p.HmiTagName) : p.HmiTagName;
            };
            scriptObject.Add("get_address", getAddressFunc);

            // 获取安全的默认值
            Func<object, object, object> safeValueFunc = (value, defaultValue) =>
            {
                return value ?? defaultValue;
            };
            scriptObject.Add("safe_value", safeValueFunc);

            // 格式化数值的函数
            Func<double?, string> formatNumberFunc = (value) =>
            {
                return value?.ToString("F2") ?? "0.0";
            };
            scriptObject.Add("format_number", formatNumberFunc);

            // 生成标准报警点名的函数
            Func<string, string, string> alarmPointFunc = (tagName, alarmType) =>
            {
                return $"{tagName}_{alarmType?.ToUpper()}";
            };
            scriptObject.Add("alarm_point", alarmPointFunc);

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
                Func<Models.Point, bool> hasAlarmConfigFunc = (p) =>
                {
                    return p != null && (p.SHH_Value.HasValue || p.SH_Value.HasValue || 
                                       p.SL_Value.HasValue || p.SLL_Value.HasValue);
                };
                scriptObject.Add("has_alarm_config", hasAlarmConfigFunc);

                // 获取报警限值
                Func<Models.Point, string> getAlarmLimitsFunc = (p) =>
                {
                    if (p == null) return "";
                    var limits = new List<string>();
                    if (p.SHH_Value.HasValue) limits.Add($"HH:{p.SHH_Value.Value:F2}");
                    if (p.SH_Value.HasValue) limits.Add($"H:{p.SH_Value.Value:F2}");
                    if (p.SL_Value.HasValue) limits.Add($"L:{p.SL_Value.Value:F2}");
                    if (p.SLL_Value.HasValue) limits.Add($"LL:{p.SLL_Value.Value:F2}");
                    return string.Join(", ", limits);
                };
                scriptObject.Add("get_alarm_limits", getAlarmLimitsFunc);
            }

            context.PushGlobal(scriptObject);

            // 步骤 2: 渲染模板
            var content = template.Render(context);

            // 步骤 3: 封装结果
            return new GenerationResult
            {
                FileName = $"{moduleType}_Mapping.st",
                Content = content,
                Category = "IO"
            };
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