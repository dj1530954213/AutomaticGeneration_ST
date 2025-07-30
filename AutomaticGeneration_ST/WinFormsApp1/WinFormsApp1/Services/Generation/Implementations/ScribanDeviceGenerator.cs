using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using Scriban;
using Scriban.Runtime;
using System;

namespace AutomaticGeneration_ST.Services.Generation.Implementations
{
    public class ScribanDeviceGenerator : IDeviceStGenerator
    {
        public GenerationResult Generate(Device device, Template template)
        {
            var scriptObject = new ScriptObject();
            scriptObject.Add("device", device);

            // 创建点位快速访问对象
            var pointsShortcuts = new ScriptObject();
            foreach (var point in device.Points.Values)
            {
                pointsShortcuts[point.HmiTagName] = point;
            }
            scriptObject.Add("points", pointsShortcuts);

            // 创建按类型分组的点位集合
            var pointsByType = new ScriptObject();
            var aiPoints = new System.Collections.Generic.List<Models.Point>();
            var aoPoints = new System.Collections.Generic.List<Models.Point>();
            var diPoints = new System.Collections.Generic.List<Models.Point>();
            var doPoints = new System.Collections.Generic.List<Models.Point>();

            foreach (var point in device.Points.Values)
            {
                switch (point.ModuleType?.ToUpper())
                {
                    case "AI":
                        aiPoints.Add(point);
                        break;
                    case "AO":
                        aoPoints.Add(point);
                        break;
                    case "DI":
                        diPoints.Add(point);
                        break;
                    case "DO":
                        doPoints.Add(point);
                        break;
                }
            }

            pointsByType.Add("ai", aiPoints);
            pointsByType.Add("ao", aoPoints);
            pointsByType.Add("di", diPoints);
            pointsByType.Add("do", doPoints);
            scriptObject.Add("points_by_type", pointsByType);

            var context = new TemplateContext()
            {
                // 允许C#属性名在Scriban中自动转换为小写下划线
                MemberRenamer = member => member.Name.ToSnakeCase()
            };

            // *** 增强的辅助函数集合 ***
            
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

            // 检查点位是否存在的函数
            Func<string, bool> hasPointFunc = (tagName) =>
            {
                return device.Points.ContainsKey(tagName);
            };
            scriptObject.Add("has_point", hasPointFunc);

            // 获取设备点位数量统计
            var deviceStats = new ScriptObject();
            deviceStats.Add("total_points", device.Points.Count);
            deviceStats.Add("ai_count", aiPoints.Count);
            deviceStats.Add("ao_count", aoPoints.Count);
            deviceStats.Add("di_count", diPoints.Count);
            deviceStats.Add("do_count", doPoints.Count);
            scriptObject.Add("device_stats", deviceStats);

            // 生成时间戳
            scriptObject.Add("generation_time", DateTime.Now);
            scriptObject.Add("generation_date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // 模板元信息
            var templateInfo = new ScriptObject();
            templateInfo.Add("template_name", device.TemplateName);
            templateInfo.Add("device_tag", device.DeviceTag);
            scriptObject.Add("template_info", templateInfo);

            context.PushGlobal(scriptObject);

            string generatedContent = template.Render(context);

            return new GenerationResult
            {
                FileName = $"DEV_{device.DeviceTag}.st",
                Content = generatedContent,
                Category = "Device"
            };
        }
    }
}