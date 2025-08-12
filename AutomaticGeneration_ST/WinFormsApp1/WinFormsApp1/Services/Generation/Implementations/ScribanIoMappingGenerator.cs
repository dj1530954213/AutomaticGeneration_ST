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
                
                // 对于IO映射，优先使用ChannelNumber（通道位号）并进行转换
                if (!string.IsNullOrWhiteSpace(p.ChannelNumber))
                {
                    try 
                    {
                        // 使用ChannelConverter将通道位号转换为硬点通道号
                        // 例如: 1_1_AI_0 -> DPIO_2_1_2_1
                        var result = ChannelConverter.ConvertToHardChannel(p.ChannelNumber);
                        System.Diagnostics.Debug.WriteLine($"成功转换ChannelNumber: {p.ChannelNumber} -> {result} (点位: {p.HmiTagName})");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"错误: ChannelNumber转换失败 - {p.ChannelNumber}, 错误: {ex.Message}");
                        // 继续尝试备用方案
                    }
                }
                
                // 备用方案：如果ChannelNumber为空或转换失败，尝试使用PlcAbsoluteAddress
                if (!string.IsNullOrWhiteSpace(p.PlcAbsoluteAddress))
                {
                    try 
                    {
                        // 记录警告信息，表明使用了备用方案
                        System.Diagnostics.Debug.WriteLine($"警告: 点位 {p.HmiTagName} 使用PlcAbsoluteAddress作为备用: {p.PlcAbsoluteAddress}");
                        var result = ChannelConverter.ConvertToHardChannel(p.PlcAbsoluteAddress);
                        System.Diagnostics.Debug.WriteLine($"成功转换PlcAbsoluteAddress: {p.PlcAbsoluteAddress} -> {result}");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"错误: PlcAbsoluteAddress转换也失败 - {p.PlcAbsoluteAddress}, 错误: {ex.Message}");
                    }
                }
                
                // 最终fallback：所有转换都失败时的处理
                var fallbackMsg = $"(* 无法解析地址: {p.HmiTagName} *)";
                System.Diagnostics.Debug.WriteLine($"警告: 点位 {p.HmiTagName} 无法解析地址，ChannelNumber: '{p.ChannelNumber}', PlcAbsoluteAddress: '{p.PlcAbsoluteAddress}'");
                return fallbackMsg;
            }));

            // 添加调试信息函数，用于验证通道号解析
            scriptObject.Import("debug_channel_info", new Func<Models.Point, string>((p) =>
            {
                if (p == null) return "点位为空";
                
                var info = new System.Text.StringBuilder();
                info.AppendLine($"点位: {p.HmiTagName}");
                info.AppendLine($"ChannelNumber: '{p.ChannelNumber}'");
                info.AppendLine($"PlcAbsoluteAddress: '{p.PlcAbsoluteAddress}'");
                
                // 验证ChannelNumber格式
                if (!string.IsNullOrWhiteSpace(p.ChannelNumber))
                {
                    var isValid = WinFormsApp1.Utils.ChannelConverter.IsValidChannelPosition(p.ChannelNumber);
                    info.AppendLine($"ChannelNumber格式有效: {isValid}");
                    if (isValid)
                    {
                        var converted = ChannelConverter.ConvertToHardChannel(p.ChannelNumber);
                        info.AppendLine($"转换结果: {converted}");
                    }
                }
                
                return info.ToString();
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

            // 步骤 3.5: 专门处理AI点位的逗号问题
            if (moduleType.ToUpper() == "AI")
            {
                content = RemoveTrailingCommaFromAiPoints(content);
            }

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

            // 使用智能换行符处理
            var lines = SplitLines(content);
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 过滤掉程序名称、变量类型和变量名称标识行
                if (IsMetadataLine(trimmedLine))
                {
                    continue; // 跳过这些行
                }
                
                filteredLines.Add(line);
            }

            return NormalizeLineEndings(filteredLines);
        }
        
        /// <summary>
        /// 判断是否为需要过滤的元数据行
        /// </summary>
        /// <param name="line">待检查的文本行</param>
        /// <returns>如果是元数据行返回true</returns>
        private static bool IsMetadataLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            
            // 标准化处理：去除空格，统一冒号格式
            var normalizedLine = line.Replace(" ", "").Replace("：", ":");
            
            return normalizedLine.StartsWith("程序名称:", StringComparison.OrdinalIgnoreCase) ||
                   normalizedLine.StartsWith("变量类型:", StringComparison.OrdinalIgnoreCase) ||
                   normalizedLine.StartsWith("变量名称:", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 智能分割文本行，正确处理各种换行符组合
        /// </summary>
        /// <param name="content">原始文本内容</param>
        /// <returns>分割后的行数组</returns>
        private static string[] SplitLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new string[0];

            // 先统一换行符为\n，然后分割
            var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
            return normalizedContent.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        /// <summary>
        /// 专门处理AI点位函数块调用中倒数第二行的逗号问题
        /// 移除AI点位函数块调用中最后一个参数后的逗号
        /// </summary>
        /// <param name="content">原始生成内容</param>
        /// <returns>处理后的内容</returns>
        private static string RemoveTrailingCommaFromAiPoints(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var lines = SplitLines(content);
            var processedLines = new List<string>();
            
            bool inAiFunctionBlock = false;
            int functionBlockStartIndex = -1;
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmedLine = line.Trim();
                
                // 检测AI函数块调用的开始（如：AI_ALARM_xxx(）
                if (trimmedLine.StartsWith("AI_ALARM_", StringComparison.OrdinalIgnoreCase) && 
                    trimmedLine.Contains("(") && !trimmedLine.Contains(");"))
                {
                    inAiFunctionBlock = true;
                    functionBlockStartIndex = i;
                    processedLines.Add(line);
                    continue;
                }
                
                // 检测AI函数块调用的结束
                if (inAiFunctionBlock && trimmedLine == ");")
                {
                    // 处理倒数第二行的逗号
                    if (processedLines.Count > functionBlockStartIndex + 1)
                    {
                        var lastParamIndex = processedLines.Count - 1;
                        var lastParamLine = processedLines[lastParamIndex];
                        
                        // 如果最后一个参数行以逗号结尾，则移除逗号
                        if (lastParamLine.TrimEnd().EndsWith(","))
                        {
                            // 保持原有的缩进，只移除末尾的逗号
                            var trimmedLastParam = lastParamLine.TrimEnd();
                            var newLastParamLine = trimmedLastParam.Substring(0, trimmedLastParam.Length - 1);
                            
                            // 恢复原来的行末空白字符（如果有的话），但不包括逗号
                            var originalWhitespace = lastParamLine.Substring(trimmedLastParam.Length);
                            processedLines[lastParamIndex] = newLastParamLine + originalWhitespace;
                        }
                    }
                    
                    inAiFunctionBlock = false;
                    functionBlockStartIndex = -1;
                    processedLines.Add(line);
                    continue;
                }
                
                processedLines.Add(line);
            }
            
            return string.Join(Environment.NewLine, processedLines);
        }

        /// <summary>
        /// 标准化换行符并清理多余空行
        /// </summary>
        /// <param name="lines">文本行列表</param>
        /// <returns>标准化后的文本内容</returns>
        private static string NormalizeLineEndings(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            // 清理连续的空行，最多保留一个空行
            var cleanedLines = new List<string>();
            bool lastLineWasEmpty = false;

            foreach (var line in lines)
            {
                bool currentLineEmpty = string.IsNullOrWhiteSpace(line);
                
                if (currentLineEmpty && lastLineWasEmpty)
                {
                    // 跳过连续的空行
                    continue;
                }
                
                cleanedLines.Add(line);
                lastLineWasEmpty = currentLineEmpty;
            }

            // 移除开头和结尾的空行
            while (cleanedLines.Count > 0 && string.IsNullOrWhiteSpace(cleanedLines[0]))
            {
                cleanedLines.RemoveAt(0);
            }
            while (cleanedLines.Count > 0 && string.IsNullOrWhiteSpace(cleanedLines[cleanedLines.Count - 1]))
            {
                cleanedLines.RemoveAt(cleanedLines.Count - 1);
            }

            // 使用平台标准换行符重新组合
            return string.Join(Environment.NewLine, cleanedLines);
        }

        /// <summary>
        /// 验证修复效果的测试方法
        /// </summary>
        public static void ValidateChannelParsing()
        {
            var testPoint = new Models.Point("TestPoint")
            {
                ChannelNumber = "1_1_AI_0",
                PlcAbsoluteAddress = "%MD320"
            };
            
            var generator = new ScribanIoMappingGenerator();
            var template = Scriban.Template.Parse("{{ get_address points.0 }}");
            
            var result = generator.Generate("AI", new[] { testPoint }, template);
            
            System.Diagnostics.Debug.WriteLine($"测试结果: {result.Content}");
            System.Diagnostics.Debug.WriteLine("如果结果包含DPIO_2_1_2_1，说明修复成功地优先使用了ChannelNumber");
        }
    }
}

public static class StringExtensions
{
    private static readonly Dictionary<string, string> SpecialMappings = new Dictionary<string, string>
    {
        { "SHH_Value", "shh_value" },
        { "SH_Value", "sh_value" },
        { "SL_Value", "sl_value" },
        { "SLL_Value", "sll_value" },
        { "SHH_Point", "shh_point" },
        { "SH_Point", "sh_point" },
        { "SL_Point", "sl_point" },
        { "SLL_Point", "sll_point" },
        { "SHH_PlcAddress", "shh_plc_address" },
        { "SH_PlcAddress", "sh_plc_address" },
        { "SL_PlcAddress", "sl_plc_address" },
        { "SLL_PlcAddress", "sll_plc_address" }
    };

    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 优先检查特殊映射
        if (SpecialMappings.TryGetValue(input, out var specialMapping))
        {
            return specialMapping;
        }

        // 标准的snake_case转换
        var result = "";
        for (int i = 0; i < input.Length; i++)
        {
            var currentChar = input[i];
            
            // 如果是大写字母且不是第一个字符，则在前面添加下划线
            if (char.IsUpper(currentChar) && i > 0)
            {
                // 避免在现有下划线后再添加下划线
                if (result.Length > 0 && result[result.Length - 1] != '_')
                {
                    result += "_";
                }
            }
            
            // 将字符转换为小写并添加到结果中
            result += char.ToLower(currentChar);
        }
        
        return result;
    }
}