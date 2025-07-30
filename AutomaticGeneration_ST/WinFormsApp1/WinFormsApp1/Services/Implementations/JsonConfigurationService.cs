using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System.IO;
using System.Text.Json; // 引入命名空间

namespace AutomaticGeneration_ST.Services.Implementations
{
    public class JsonConfigurationService : IConfigurationService
    {
        public TemplateMapping LoadTemplateMappings(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"关键配置文件未找到: {configFilePath}");
            }

            var jsonString = File.ReadAllText(configFilePath, System.Text.Encoding.UTF8);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true, // 允许末尾逗号
                ReadCommentHandling = JsonCommentHandling.Skip // 跳过注释（虽然JSON标准不支持，但允许_comment字段）
            };
            
            // 直接反序列化为Dictionary，因为我们的JSON文件就是简单的键值对
            var mappingDict = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString, options);

            if (mappingDict == null)
            {
                throw new InvalidDataException($"配置文件 '{configFilePath}' 格式错误或为空。");
            }

            // 过滤掉注释字段（以_开头的字段）
            var filteredMappings = new Dictionary<string, string>();
            foreach (var kvp in mappingDict)
            {
                if (!kvp.Key.StartsWith("_") && !string.IsNullOrWhiteSpace(kvp.Value))
                {
                    filteredMappings[kvp.Key] = kvp.Value;
                }
            }

            return new TemplateMapping
            {
                Mappings = filteredMappings
            };
        }
    }
}