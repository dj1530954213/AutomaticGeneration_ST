using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1.Template;
using WinFormsApp1.Utils;

namespace WinFormsApp1.Generators
{
    //TODO: 重复代码(ID:DUP-001) - [点位生成器：Generate方法逻辑高度相似] 
    //TODO: 建议重构为抽象基类BasePointGenerator，提取公共生成流程 优先级:高
    /// <summary>
    /// DO点位代码生成器
    /// </summary>
    /// <remarks>
    /// 状态: @duplicate
    /// 优先级: P2 (中风险)
    /// 重复度: 85%
    /// 重复位置: AiGenerator.cs, AoGenerator.cs, DiGenerator.cs
    /// 建议: 重构为抽象基类BasePointGenerator，提取公共的Generate流程
    /// 风险级别: 中风险 - 需要分析调用关系后重构
    /// 分析时间: 2025-08-15
    /// 重复方法: Generate, ValidateRequiredFields, PreprocessData, GetTemplatePath
    /// </remarks>
    public class DoGenerator : IPointGenerator
    {
        public string PointType => "DO";
        
        public bool CanGenerate(Dictionary<string, object> row)
        {
            return row.TryGetValue("模块类型", out var type) && 
                   string.Equals(type?.ToString()?.Trim(), "DO", StringComparison.OrdinalIgnoreCase);
        }
        
        public string Generate(Dictionary<string, object> row)
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var templatePath = Path.Combine(basePath!, "Templates", "DO", "default.scriban");
            
            //TODO: 重复代码(ID:DUP-002) - [数据预处理：硬点通道号转换逻辑重复] 
            //TODO: 建议重构为共享工具类ChannelDataProcessor 优先级:高
            var processedData = new Dictionary<string, object>(row);
            
            // 处理硬点通道号转换
            var channelPosition = GetStringValue(processedData, "通道位号");
            if (!string.IsNullOrWhiteSpace(channelPosition))
            {
                var hardChannel = ChannelConverter.ConvertToHardChannel(channelPosition);
                processedData["硬点通道号"] = hardChannel;
            }
            else
            {
                // 如果没有通道位号，尝试直接使用输出通道
                var outputChannel = GetStringValue(processedData, "输出通道");
                if (!string.IsNullOrWhiteSpace(outputChannel))
                {
                    processedData["硬点通道号"] = outputChannel;
                }
                else
                {
                    processedData["硬点通道号"] = "DPIO_2_1_2_1"; // 默认值
                }
            }
            
            return TemplateRenderer.Render(templatePath, processedData);
        }
        
        private string GetStringValue(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value?.ToString()?.Trim() ?? string.Empty : string.Empty;
        }
    }
}