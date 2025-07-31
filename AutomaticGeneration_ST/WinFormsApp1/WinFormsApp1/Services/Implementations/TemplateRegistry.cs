using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 模板注册表实现类
    /// </summary>
    public class TemplateRegistry : ITemplateRegistry
    {
        private readonly Dictionary<string, string> _templateMappings;
        private readonly string _templateBaseDirectory;

        public TemplateRegistry(string templateBaseDirectory)
        {
            _templateMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _templateBaseDirectory = templateBaseDirectory ?? throw new ArgumentNullException(nameof(templateBaseDirectory));
        }

        public void RegisterTemplate(string templateName, string templateFile)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("模板名称不能为空", nameof(templateName));

            if (string.IsNullOrWhiteSpace(templateFile))
                throw new ArgumentException("模板文件路径不能为空", nameof(templateFile));

            _templateMappings[templateName] = templateFile;
        }

        public string GetTemplateFile(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                return null;

            if (_templateMappings.TryGetValue(templateName, out var templateFile))
            {
                // 如果是相对路径，与基础目录组合
                if (!Path.IsPathRooted(templateFile))
                {
                    return Path.Combine(_templateBaseDirectory, templateFile);
                }
                return templateFile;
            }

            return null;
        }

        public bool HasTemplate(string templateName)
        {
            return !string.IsNullOrWhiteSpace(templateName) && 
                   _templateMappings.ContainsKey(templateName);
        }

        public IEnumerable<string> GetAllTemplateNames()
        {
            return _templateMappings.Keys;
        }

        public void LoadFromConfig(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                throw new ArgumentException("配置文件路径不能为空", nameof(configPath));

            if (!File.Exists(configPath))
                throw new FileNotFoundException($"配置文件不存在: {configPath}");

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var configData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                if (configData != null && configData.TryGetValue("Mappings", out var mappingsObj))
                {
                    if (mappingsObj is JsonElement mappingsElement && mappingsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var property in mappingsElement.EnumerateObject())
                        {
                            var templateName = property.Name;
                            var templateFile = property.Value.GetString();
                            
                            if (!string.IsNullOrWhiteSpace(templateFile))
                            {
                                RegisterTemplate(templateName, templateFile);
                            }
                        }
                    }
                }

                Console.WriteLine($"[INFO] 成功加载 {_templateMappings.Count} 个模板映射配置");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载模板配置失败: {ex.Message}", ex);
            }
        }
    }
}