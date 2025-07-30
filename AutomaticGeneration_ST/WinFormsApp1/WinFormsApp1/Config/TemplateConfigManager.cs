using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 模板配置管理器 - 负责template_config.json文件的读取、更新和管理
    /// </summary>
    public static class TemplateConfigManager
    {
        private static readonly string ConfigFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Templates", "template_config.json");
        
        private static TemplateConfig? _cachedConfig;
        private static DateTime _lastLoadTime = DateTime.MinValue;
        
        /// <summary>
        /// 配置文件变更事件
        /// </summary>
        public static event Action<TemplateConfig>? ConfigChanged;
        
        /// <summary>
        /// 获取当前模板配置，支持缓存和自动刷新
        /// </summary>
        public static TemplateConfig GetConfig(bool forceReload = false)
        {
            try
            {
                // 检查是否需要重新加载配置文件
                if (forceReload || _cachedConfig == null || ShouldReloadConfig())
                {
                    _cachedConfig = LoadConfigFromFile();
                    _lastLoadTime = DateTime.Now;
                }
                
                return _cachedConfig ?? CreateDefaultConfig();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取模板配置失败: {ex.Message}");
                return CreateDefaultConfig();
            }
        }
        
        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public static bool SaveConfig(TemplateConfig config)
        {
            try
            {
                if (config == null)
                    return false;
                
                // 确保目录存在
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 更新最后修改时间
                config.LastUpdated = DateTime.Now;
                
                // 序列化配置
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() },
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFilePath, json);
                
                // 更新缓存
                _cachedConfig = config;
                _lastLoadTime = DateTime.Now;
                
                // 触发配置变更事件
                ConfigChanged?.Invoke(config);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存模板配置失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 添加或更新模板信息
        /// </summary>
        public static bool UpdateTemplateInfo(PointType pointType, TemplateVersion version, TemplateInfo templateInfo)
        {
            try
            {
                var config = GetConfig();
                
                // 确保点类型字典存在
                if (!config.Templates.ContainsKey(pointType))
                {
                    config.Templates[pointType] = new Dictionary<TemplateVersion, TemplateInfo>();
                }
                
                // 更新模板信息
                config.Templates[pointType][version] = templateInfo;
                
                return SaveConfig(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新模板信息失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 移除模板信息
        /// </summary>
        public static bool RemoveTemplateInfo(PointType pointType, TemplateVersion version)
        {
            try
            {
                var config = GetConfig();
                
                if (config.Templates.ContainsKey(pointType) && 
                    config.Templates[pointType].ContainsKey(version))
                {
                    config.Templates[pointType].Remove(version);
                    return SaveConfig(config);
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"移除模板信息失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 获取所有活跃的模板信息
        /// </summary>
        public static List<TemplateInfo> GetActiveTemplates(PointType? filterPointType = null)
        {
            try
            {
                var config = GetConfig();
                var templates = new List<TemplateInfo>();
                
                foreach (var pointTypeKvp in config.Templates)
                {
                    if (filterPointType == null || pointTypeKvp.Key == filterPointType)
                    {
                        foreach (var versionKvp in pointTypeKvp.Value)
                        {
                            if (versionKvp.Value.IsActive)
                            {
                                templates.Add(versionKvp.Value);
                            }
                        }
                    }
                }
                
                return templates.OrderBy(t => t.PointType)
                              .ThenBy(t => t.Version)
                              .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取活跃模板失败: {ex.Message}");
                return new List<TemplateInfo>();
            }
        }
        
        /// <summary>
        /// 验证配置文件完整性
        /// </summary>
        public static (bool IsValid, List<string> Issues) ValidateConfig()
        {
            var issues = new List<string>();
            
            try
            {
                var config = GetConfig();
                
                // 检查基本属性
                if (string.IsNullOrEmpty(config.Version))
                    issues.Add("配置文件版本号缺失");
                
                if (string.IsNullOrEmpty(config.TemplateDirectory))
                    issues.Add("模板目录路径缺失");
                
                // 检查每个点类型是否都有默认模板
                foreach (PointType pointType in Enum.GetValues<PointType>())
                {
                    if (!config.Templates.ContainsKey(pointType))
                    {
                        issues.Add($"缺少 {pointType} 点类型的模板配置");
                        continue;
                    }
                    
                    if (!config.Templates[pointType].ContainsKey(TemplateVersion.Default))
                    {
                        issues.Add($"{pointType} 点类型缺少默认模板");
                    }
                    
                    // 检查模板文件是否存在
                    foreach (var template in config.Templates[pointType].Values)
                    {
                        if (!string.IsNullOrEmpty(template.FilePath))
                        {
                            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, template.FilePath);
                            if (!File.Exists(fullPath))
                            {
                                issues.Add($"模板文件不存在: {template.FilePath}");
                            }
                        }
                    }
                }
                
                return (issues.Count == 0, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"配置验证过程中出错: {ex.Message}");
                return (false, issues);
            }
        }
        
        /// <summary>
        /// 获取配置统计信息
        /// </summary>
        public static Dictionary<string, object> GetConfigStatistics()
        {
            var stats = new Dictionary<string, object>();
            
            try
            {
                var config = GetConfig();
                
                stats["配置版本"] = config.Version;
                stats["最后更新时间"] = config.LastUpdated;
                stats["默认模板版本"] = config.DefaultVersion;
                stats["模板目录"] = config.TemplateDirectory;
                
                var totalTemplates = 0;
                var activeTemplates = 0;
                var pointTypeStats = new Dictionary<string, int>();
                
                foreach (var pointTypeKvp in config.Templates)
                {
                    var pointTypeName = pointTypeKvp.Key.ToString();
                    var templateCount = pointTypeKvp.Value.Count;
                    var activeCount = pointTypeKvp.Value.Values.Count(t => t.IsActive);
                    
                    pointTypeStats[pointTypeName] = templateCount;
                    totalTemplates += templateCount;
                    activeTemplates += activeCount;
                }
                
                stats["总模板数"] = totalTemplates;
                stats["活跃模板数"] = activeTemplates;
                stats["各点类型模板数"] = pointTypeStats;
                
                // 作者统计
                var authors = config.Templates.Values
                    .SelectMany(dict => dict.Values)
                    .GroupBy(t => t.Author)
                    .ToDictionary(g => g.Key, g => g.Count());
                stats["作者统计"] = authors;
                
            }
            catch (Exception ex)
            {
                stats["错误"] = ex.Message;
            }
            
            return stats;
        }
        
        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public static bool ResetToDefault()
        {
            try
            {
                var defaultConfig = CreateDefaultConfig();
                return SaveConfig(defaultConfig);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重置默认配置失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 备份当前配置
        /// </summary>
        public static bool BackupConfig(string? backupPath = null)
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return false;
                
                backupPath ??= ConfigFilePath + $".backup_{DateTime.Now:yyyyMMddHHmmss}";
                
                File.Copy(ConfigFilePath, backupPath, true);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"备份配置失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从备份恢复配置
        /// </summary>
        public static bool RestoreFromBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return false;
                
                // 验证备份文件是否有效
                var backupContent = File.ReadAllText(backupPath);
                var testConfig = JsonSerializer.Deserialize<TemplateConfig>(backupContent);
                
                if (testConfig == null)
                    return false;
                
                // 备份当前配置
                BackupConfig();
                
                // 恢复配置
                File.Copy(backupPath, ConfigFilePath, true);
                
                // 清除缓存以强制重新加载
                _cachedConfig = null;
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"从备份恢复配置失败: {ex.Message}");
                return false;
            }
        }
        
        #region 私有方法
        
        /// <summary>
        /// 检查是否需要重新加载配置文件
        /// </summary>
        private static bool ShouldReloadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                    return false;
                
                var fileLastWriteTime = File.GetLastWriteTime(ConfigFilePath);
                return fileLastWriteTime > _lastLoadTime;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 从文件加载配置
        /// </summary>
        private static TemplateConfig LoadConfigFromFile()
        {
            if (!File.Exists(ConfigFilePath))
            {
                var defaultConfig = CreateDefaultConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }
            
            var json = File.ReadAllText(ConfigFilePath);
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var config = JsonSerializer.Deserialize<TemplateConfig>(json, options);
            return config ?? CreateDefaultConfig();
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        private static TemplateConfig CreateDefaultConfig()
        {
            var config = new TemplateConfig
            {
                Version = "1.0",
                LastUpdated = DateTime.Now,
                DefaultVersion = TemplateVersion.Default,
                TemplateDirectory = "Templates"
            };
            
            // 初始化空的模板字典
            foreach (PointType pointType in Enum.GetValues<PointType>())
            {
                config.Templates[pointType] = new Dictionary<TemplateVersion, TemplateInfo>();
            }
            
            return config;
        }
        
        #endregion
    }
}