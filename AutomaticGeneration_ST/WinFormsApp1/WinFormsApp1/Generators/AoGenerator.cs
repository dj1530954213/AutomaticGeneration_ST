using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1.Template;
using WinFormsApp1.Utils;

namespace WinFormsApp1.Generators
{
    //TODO: 重复代码(ID:DUP-001) - [点位生成器：Generate方法逻辑高度相似] 
    //TODO: 建议重构为抽象基类BasePointGenerator，提取公共生成流程 优先级:高
    public class AoGenerator : IPointGenerator
    {
        private static readonly LogService logger = LogService.Instance;
        
        public string PointType => "AO";
        
        public bool CanGenerate(Dictionary<string, object> row)
        {
            return row.TryGetValue("模块类型", out var type) && 
                   string.Equals(type?.ToString()?.Trim(), "AO", StringComparison.OrdinalIgnoreCase);
        }
        
        public string Generate(Dictionary<string, object> row)
        {
            try
            {
                logger.LogDebug($"开始生成AO点位代码: {GetVariableName(row)}");
                
                ValidateRequiredFields(row);
                var templatePath = GetTemplatePath();
                var processedData = PreprocessData(row);
                var result = TemplateRenderer.Render(templatePath, processedData);
                
                logger.LogDebug($"AO点位代码生成完成: {GetVariableName(row)}");
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"生成AO点位代码失败: {ex.Message}");
                throw;
            }
        }
        
        private void ValidateRequiredFields(Dictionary<string, object> row)
        {
            var requiredFields = new[] { "变量名称（HMI）" };
            
            foreach (var field in requiredFields)
            {
                if (!row.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    throw new ArgumentException($"AO点位缺少必要字段: {field}");
                }
            }
        }
        
        private string GetTemplatePath()
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var templatePath = Path.Combine(basePath!, "Templates", "AO", "default.scriban");
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"AO模板文件不存在: {templatePath}");
            }
            
            return templatePath;
        }
        
        //TODO: 重复代码(ID:DUP-002) - [数据预处理：硬点通道号转换逻辑重复] 
        //TODO: 建议重构为共享工具类ChannelDataProcessor 优先级:高
        private Dictionary<string, object> PreprocessData(Dictionary<string, object> row)
        {
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
            
            // 设置量程默认值
            SetDefaultIfEmpty(processedData, "量程低限", 0.0);
            SetDefaultIfEmpty(processedData, "量程高限", 100.0);
            
            return processedData;
        }
        
        private string GetStringValue(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value?.ToString()?.Trim() ?? string.Empty : string.Empty;
        }
        
        private void SetDefaultIfEmpty(Dictionary<string, object> data, string key, object defaultValue)
        {
            if (!data.TryGetValue(key, out var value) || 
                string.IsNullOrWhiteSpace(value?.ToString()) ||
                (double.TryParse(value?.ToString(), out var numValue) && numValue == 0))
            {
                data[key] = defaultValue;
            }
        }
        
        private string GetVariableName(Dictionary<string, object> row)
        {
            return GetStringValue(row, "变量名称（HMI）");
        }
    }
}