using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WinFormsApp1.Config
{
    //TODO: 重复代码(ID:DUP-005) - [配置管理：序列化、验证和持久化逻辑在多个配置管理器中冗余] 
    //TODO: 建议重构为通用ConfigurationService基础设施 优先级:中等
    /// <summary>
    /// 配置变更事件参数
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; set; } = "";
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public SettingsCategory Category { get; set; }
        public bool RequiresRestart { get; set; }
    }

    /// <summary>
    /// 配置保存结果
    /// </summary>
    public class ConfigurationSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public List<string> Warnings { get; set; } = new();
        public Exception? Exception { get; set; }
    }

    /// <summary>
    /// 配置管理器
    /// </summary>
    public static class ConfigurationManager
    {
        private static ApplicationConfiguration _currentConfig = new();
        private static readonly Dictionary<string, ConfigItemDefinition> _configDefinitions = new();
        private static readonly object _lockObject = new();
        private static string _configFilePath = "";
        private static FileSystemWatcher? _configFileWatcher;

        public static event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
        public static event EventHandler<ApplicationConfiguration>? ConfigurationLoaded;
        public static event EventHandler<ConfigurationSaveResult>? ConfigurationSaved;

        static ConfigurationManager()
        {
            InitializeConfigDefinitions();
            SetDefaultConfigPath();
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public static ApplicationConfiguration Current => _currentConfig;

        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        public static string ConfigFilePath => _configFilePath;

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        public static async Task InitializeAsync(string? customConfigPath = null)
        {
            if (!string.IsNullOrEmpty(customConfigPath))
            {
                _configFilePath = customConfigPath;
            }

            // 初始化持久化服务
            var persistenceOptions = new PersistenceOptions
            {
                EnableCompression = false,
                EnableEncryption = false,
                EnableChecksumValidation = true,
                EnableAutoBackup = true,
                MaxBackupCount = 10,
                BackupInterval = TimeSpan.FromDays(1)
            };
            ConfigurationPersistence.Initialize(persistenceOptions);

            await LoadConfigurationAsync();
            SetupFileWatcher();
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public static async Task<bool> LoadConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    _currentConfig = new ApplicationConfiguration();
                    _ = Task.Run(SaveConfigurationAsync); // 异步保存默认配置
                    ConfigurationLoaded?.Invoke(null, _currentConfig);
                    return true;
                }

                // 使用高级持久化服务加载
                var persistenceOptions = new PersistenceOptions
                {
                    EnableCompression = false,
                    EnableEncryption = false,
                    EnableChecksumValidation = true
                };

                var (config, loadResult) = await ConfigurationPersistence.LoadConfigurationAsync(_configFilePath, persistenceOptions);
                
                if (loadResult.Success && config != null)
                {
                    // 使用高级验证器验证配置
                    var validationResult = ConfigurationValidator.ValidateConfiguration(config);
                    if (validationResult.IsValid)
                    {
                        lock (_lockObject)
                        {
                            _currentConfig = config;
                        }
                        
                        ConfigurationLoaded?.Invoke(null, _currentConfig);
                        
                        // 记录验证警告
                        foreach (var warning in validationResult.Warnings)
                        {
                            System.Diagnostics.Debug.WriteLine($"配置验证警告: {warning}");
                        }
                        
                        return true;
                    }
                    else
                    {
                        // 配置验证失败，使用默认配置
                        System.Diagnostics.Debug.WriteLine("配置验证失败，使用默认配置");
                        foreach (var error in validationResult.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"配置验证错误: {error}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"加载配置失败: {loadResult.Message}");
                }

                // 加载失败，使用默认配置
                lock (_lockObject)
                {
                    _currentConfig = new ApplicationConfiguration();
                }
                
                ConfigurationLoaded?.Invoke(null, _currentConfig);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载配置时发生异常: {ex.Message}");
                
                lock (_lockObject)
                {
                    _currentConfig = new ApplicationConfiguration();
                }
                
                ConfigurationLoaded?.Invoke(null, _currentConfig);
                return false;
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public static async Task<ConfigurationSaveResult> SaveConfigurationAsync()
        {
            var result = new ConfigurationSaveResult();

            try
            {
                // 使用高级验证器验证配置
                var validationResult = ConfigurationValidator.ValidateConfiguration(_currentConfig);
                if (!validationResult.IsValid)
                {
                    result.Success = false;
                    result.Message = "配置验证失败";
                    result.Warnings.AddRange(validationResult.Errors);
                    return result;
                }

                if (validationResult.Warnings.Any())
                {
                    result.Warnings.AddRange(validationResult.Warnings);
                }

                // 更新时间戳
                _currentConfig.LastModified = DateTime.Now;

                // 创建备份
                await ConfigurationPersistence.CreateBackupAsync(_currentConfig, null, "自动备份", true);

                // 使用高级持久化服务保存
                var persistenceOptions = new PersistenceOptions
                {
                    EnableCompression = false,
                    EnableEncryption = false,
                    EnableChecksumValidation = true,
                    EnableAutoBackup = true
                };

                var saveResult = await ConfigurationPersistence.SaveConfigurationAsync(_currentConfig, _configFilePath, persistenceOptions);
                
                result.Success = saveResult.Success;
                result.Message = saveResult.Message;
                
                if (saveResult.Exception != null)
                {
                    result.Exception = saveResult.Exception;
                }

                ConfigurationSaved?.Invoke(null, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"保存配置失败: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        public static T GetValue<T>(string key, T defaultValue = default!)
        {
            try
            {
                var value = GetValueByPath(key);
                if (value != null)
                {
                    if (value is T directValue)
                        return directValue;

                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                    }

                    // 尝试类型转换
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取配置值失败 {key}: {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public static bool SetValue<T>(string key, T value)
        {
            try
            {
                var oldValue = GetValueByPath(key);
                if (SetValueByPath(key, value))
                {
                    // 获取配置定义信息
                    var definition = GetConfigDefinition(key);
                    var category = definition?.Category ?? SettingsCategory.General;
                    var requiresRestart = definition?.RequiresRestart ?? false;

                    // 触发变更事件
                    ConfigurationChanged?.Invoke(null, new ConfigurationChangedEventArgs
                    {
                        Key = key,
                        OldValue = oldValue,
                        NewValue = value,
                        Category = category,
                        RequiresRestart = requiresRestart
                    });

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置配置值失败 {key}: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 设置配置值 (非泛型版本)
        /// </summary>
        public static bool SetValue(string key, object? value)
        {
            if (value == null)
                return SetValue<object?>(key, null);
            
            // 根据值的类型调用对应的泛型方法
            var valueType = value.GetType();
            if (valueType == typeof(string))
                return SetValue(key, (string)value);
            else if (valueType == typeof(int))
                return SetValue(key, (int)value);
            else if (valueType == typeof(double))
                return SetValue(key, (double)value);
            else if (valueType == typeof(bool))
                return SetValue(key, (bool)value);
            else
                return SetValue<object>(key, value);
        }

        /// <summary>
        /// 重置配置到默认值
        /// </summary>
        public static async Task<bool> ResetToDefaultsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    _currentConfig.ResetToDefaults();
                }

                var result = await SaveConfigurationAsync();
                return result.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 导入配置
        /// </summary>
        public static async Task<bool> ImportConfigurationAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var importedConfig = JsonSerializer.Deserialize<ApplicationConfiguration>(json, options);
                if (importedConfig != null)
                {
                    // 验证导入的配置
                    var validationErrors = importedConfig.Validate();
                    if (validationErrors.Any())
                    {
                        // 可以选择是否继续导入
                        System.Diagnostics.Debug.WriteLine($"导入配置有验证错误: {string.Join(", ", validationErrors)}");
                    }

                    lock (_lockObject)
                    {
                        _currentConfig = importedConfig;
                        _currentConfig.LastModified = DateTime.Now;
                    }

                    var result = await SaveConfigurationAsync();
                    return result.Success;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导入配置失败: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 导出配置
        /// </summary>
        public static async Task<bool> ExportConfigurationAsync(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var json = JsonSerializer.Serialize(_currentConfig, options);
                await File.WriteAllTextAsync(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取配置定义
        /// </summary>
        public static List<ConfigItemDefinition> GetConfigDefinitions(SettingsCategory? category = null)
        {
            var definitions = _configDefinitions.Values.ToList();
            if (category.HasValue)
            {
                definitions = definitions.Where(d => d.Category == category.Value).ToList();
            }
            return definitions.OrderBy(d => d.DisplayOrder).ThenBy(d => d.DisplayName).ToList();
        }

        /// <summary>
        /// 获取指定键的配置定义
        /// </summary>
        public static ConfigItemDefinition? GetConfigDefinition(string key)
        {
            _configDefinitions.TryGetValue(key, out var definition);
            return definition;
        }

        /// <summary>
        /// 检查是否需要重启
        /// </summary>
        public static bool HasPendingRestartRequiredChanges()
        {
            // 这里可以维护一个需要重启的变更列表
            // 简化实现，可以通过检查特定配置项是否变更来判断
            return false;
        }

        #region 私有方法

        private static void InitializeConfigDefinitions()
        {
            // 通过反射自动生成配置定义
            var configType = typeof(ApplicationConfiguration);
            var categories = configType.GetProperties()
                .Where(p => p.PropertyType.Namespace == configType.Namespace)
                .ToList();

            foreach (var categoryProperty in categories)
            {
                var categoryName = categoryProperty.Name;
                var categoryType = categoryProperty.PropertyType;
                
                if (Enum.TryParse<SettingsCategory>(categoryName, true, out var category))
                {
                    var properties = categoryType.GetProperties()
                        .Where(p => p.CanRead && p.CanWrite)
                        .ToList();

                    foreach (var property in properties)
                    {
                        var key = $"{categoryName}.{property.Name}";
                        var displayNameAttr = property.GetCustomAttribute<DisplayNameAttribute>();
                        var descriptionAttr = property.GetCustomAttribute<DescriptionAttribute>();

                        var definition = new ConfigItemDefinition
                        {
                            Key = key,
                            DisplayName = displayNameAttr?.DisplayName ?? property.Name,
                            Description = descriptionAttr?.Description ?? "",
                            Type = GetConfigItemType(property.PropertyType),
                            Category = category,
                            DefaultValue = GetDefaultValue(property.PropertyType)
                        };

                        _configDefinitions[key] = definition;
                    }
                }
            }
        }

        private static ConfigItemType GetConfigItemType(Type type)
        {
            if (type == typeof(string))
                return ConfigItemType.String;
            if (type == typeof(int))
                return ConfigItemType.Integer;
            if (type == typeof(double) || type == typeof(float))
                return ConfigItemType.Double;
            if (type == typeof(bool))
                return ConfigItemType.Boolean;
            if (type.IsEnum)
                return ConfigItemType.Enum;

            return ConfigItemType.Object;
        }

        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        private static void SetDefaultConfigPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "STGenerator");
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "config.json");
        }

        private static object? GetValueByPath(string path)
        {
            var parts = path.Split('.');
            if (parts.Length != 2) return null;

            var categoryName = parts[0];
            var propertyName = parts[1];

            var categoryProperty = typeof(ApplicationConfiguration).GetProperty(categoryName);
            if (categoryProperty == null) return null;

            var categoryInstance = categoryProperty.GetValue(_currentConfig);
            if (categoryInstance == null) return null;

            var property = categoryProperty.PropertyType.GetProperty(propertyName);
            return property?.GetValue(categoryInstance);
        }

        private static bool SetValueByPath(string path, object? value)
        {
            var parts = path.Split('.');
            if (parts.Length != 2) return false;

            var categoryName = parts[0];
            var propertyName = parts[1];

            var categoryProperty = typeof(ApplicationConfiguration).GetProperty(categoryName);
            if (categoryProperty == null) return false;

            var categoryInstance = categoryProperty.GetValue(_currentConfig);
            if (categoryInstance == null) return false;

            var property = categoryProperty.PropertyType.GetProperty(propertyName);
            if (property == null || !property.CanWrite) return false;

            try
            {
                property.SetValue(categoryInstance, value);
                return true;
            }
            catch
            {
                return false;
            }
        }


        private static void SetupFileWatcher()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                var fileName = Path.GetFileName(_configFilePath);

                if (!string.IsNullOrEmpty(directory) && !string.IsNullOrEmpty(fileName))
                {
                    _configFileWatcher = new FileSystemWatcher(directory, fileName)
                    {
                        NotifyFilter = NotifyFilters.LastWrite,
                        EnableRaisingEvents = true
                    };

                    _configFileWatcher.Changed += async (sender, e) =>
                    {
                        // 延迟一段时间以避免重复触发
                        await Task.Delay(1000);
                        await LoadConfigurationAsync();
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置文件监控失败: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 获取配置备份列表
        /// </summary>
        public static List<ConfigurationBackup> GetConfigurationBackups()
        {
            return ConfigurationPersistence.GetAllBackups();
        }

        /// <summary>
        /// 创建手动备份
        /// </summary>
        public static async Task<ConfigurationBackup?> CreateManualBackupAsync(string name, string description = "")
        {
            return await ConfigurationPersistence.CreateBackupAsync(_currentConfig, name, description, false);
        }

        /// <summary>
        /// 从备份恢复配置
        /// </summary>
        public static async Task<bool> RestoreFromBackupAsync(string backupId)
        {
            var restoredConfig = await ConfigurationPersistence.RestoreBackupAsync(backupId);
            if (restoredConfig != null)
            {
                lock (_lockObject)
                {
                    _currentConfig = restoredConfig;
                }
                
                // 保存恢复的配置
                var saveResult = await SaveConfigurationAsync();
                if (saveResult.Success)
                {
                    ConfigurationLoaded?.Invoke(null, _currentConfig);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 删除配置备份
        /// </summary>
        public static bool DeleteBackup(string backupId)
        {
            return ConfigurationPersistence.DeleteBackup(backupId);
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        public static ValidationResult ValidateCurrentConfiguration()
        {
            return ConfigurationValidator.ValidateConfiguration(_currentConfig);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Dispose()
        {
            _configFileWatcher?.Dispose();
            ConfigurationPersistence.Dispose();
        }
    }
}