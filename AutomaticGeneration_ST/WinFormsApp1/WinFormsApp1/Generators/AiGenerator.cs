using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1.Template;
using WinFormsApp1.Utils;

namespace WinFormsApp1.Generators
{
    //TODO: 重复代码(ID:DUP-001) - [点位生成器：Generate方法逻辑高度相似] 
    //TODO: 建议重构为抽象基类BasePointGenerator，提取公共生成流程 优先级:高
    public class AiGenerator : IPointGenerator
    {
        private static readonly LogService logger = LogService.Instance;
        
        public string PointType => "AI";
        
        public bool CanGenerate(Dictionary<string, object> row)
        {
            return row.TryGetValue("模块类型", out var type) && 
                   string.Equals(type?.ToString()?.Trim(), "AI", StringComparison.OrdinalIgnoreCase);
        }
        
        public string Generate(Dictionary<string, object> row)
        {
            try
            {
                logger.LogDebug($"开始生成AI点位代码: {GetVariableName(row)}");
                
                // 验证必要字段
                ValidateRequiredFields(row);
                
                // 获取模板路径
                var templatePath = GetTemplatePath();
                
                // 预处理数据
                var processedData = PreprocessData(row);
                
                // 渲染模板
                var result = TemplateRenderer.Render(templatePath, processedData);
                
                logger.LogDebug($"AI点位代码生成完成: {GetVariableName(row)}");
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"生成AI点位代码失败: {ex.Message}");
                throw;
            }
        }
        
        private void ValidateRequiredFields(Dictionary<string, object> row)
        {
            // 检查变量名称，更宽松的验证
            var variableName = GetStringValue(row, "变量名称（HMI）");
            if (string.IsNullOrWhiteSpace(variableName))
            {
                // 如果变量名称为空，尝试使用其他标识符
                var alternatives = new[] { "变量名称", "变量名", "标识符", "名称" };
                bool found = false;
                
                foreach (var alt in alternatives)
                {
                    var altValue = GetStringValue(row, alt);
                    if (!string.IsNullOrWhiteSpace(altValue))
                    {
                        row["变量名称（HMI）"] = altValue; // 设置为找到的值
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    // 如果还是找不到，跳过这一行而不抛出异常
                    logger.LogWarning("AI点位缺少变量名称，跳过该行");
                    throw new ArgumentException("AI点位缺少变量名称");
                }
            }
        }
        
        private string GetTemplatePath()
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var templatePath = Path.Combine(basePath!, "Templates", "AI", "default.scriban");
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"AI模板文件不存在: {templatePath}");
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
            
            // 设置量程默认值
            SetDefaultIfEmpty(processedData, "量程低限", 0.0);
            SetDefaultIfEmpty(processedData, "量程高限", 100.0);
            
            // 保持原有的报警限值处理
            SetDefaultIfEmpty(processedData, "高高限", 100.0);
            SetDefaultIfEmpty(processedData, "高限", 90.0);
            SetDefaultIfEmpty(processedData, "低限", 10.0);
            SetDefaultIfEmpty(processedData, "低低限", 0.0);
            
            return processedData;
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
        
        //TODO: 重复代码(ID:DUP-007) - [数据提取：GetStringValue方法在多个生成器中重复实现] 
        //TODO: 建议重构为通用的DataValueExtractor工具类 优先级:中等
        private string GetStringValue(Dictionary<string, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value?.ToString()?.Trim() ?? string.Empty : string.Empty;
        }
        
        private string GetVariableName(Dictionary<string, object> row)
        {
            return GetStringValue(row, "变量名称（HMI）");
        }
    }
}