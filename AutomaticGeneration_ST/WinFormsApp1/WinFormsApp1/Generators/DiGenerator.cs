using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1.Template;
using WinFormsApp1.Utils;

namespace WinFormsApp1.Generators
{
    public class DiGenerator : IPointGenerator
    {
        public string PointType => "DI";
        
        public bool CanGenerate(Dictionary<string, object> row)
        {
            return row.TryGetValue("模块类型", out var type) && 
                   string.Equals(type?.ToString()?.Trim(), "DI", StringComparison.OrdinalIgnoreCase);
        }
        
        public string Generate(Dictionary<string, object> row)
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var templatePath = Path.Combine(basePath!, "Templates", "DI", "default.scriban");
            
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
                // 如果没有通道位号，尝试直接使用输入通道
                var inputChannel = GetStringValue(processedData, "输入通道");
                if (!string.IsNullOrWhiteSpace(inputChannel))
                {
                    processedData["硬点通道号"] = inputChannel;
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