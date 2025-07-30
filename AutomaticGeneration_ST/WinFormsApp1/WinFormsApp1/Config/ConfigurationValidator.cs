using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 验证规则类型
    /// </summary>
    public enum ValidationRuleType
    {
        Required,
        Range,
        StringLength,
        RegularExpression,
        Custom,
        FileExists,
        DirectoryExists,
        ValidEnum
    }

    /// <summary>
    /// 验证规则
    /// </summary>
    public class ValidationRule
    {
        public ValidationRuleType Type { get; set; }
        public string PropertyPath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public object? MinValue { get; set; }
        public object? MaxValue { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public string Pattern { get; set; } = "";
        public Func<object?, bool>? CustomValidator { get; set; }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, List<string>> PropertyErrors { get; set; } = new();

        public void AddError(string propertyPath, string message)
        {
            Errors.Add($"{propertyPath}: {message}");
            
            if (!PropertyErrors.ContainsKey(propertyPath))
                PropertyErrors[propertyPath] = new List<string>();
            
            PropertyErrors[propertyPath].Add(message);
        }

        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        public void Merge(ValidationResult other)
        {
            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
            
            foreach (var kvp in other.PropertyErrors)
            {
                if (!PropertyErrors.ContainsKey(kvp.Key))
                    PropertyErrors[kvp.Key] = new List<string>();
                
                PropertyErrors[kvp.Key].AddRange(kvp.Value);
            }
        }
    }

    /// <summary>
    /// 高级配置验证器
    /// </summary>
    public static class ConfigurationValidator
    {
        private static readonly List<ValidationRule> _validationRules = new();

        static ConfigurationValidator()
        {
            InitializeValidationRules();
        }

        /// <summary>
        /// 验证完整配置
        /// </summary>
        public static ValidationResult ValidateConfiguration(ApplicationConfiguration config)
        {
            var result = new ValidationResult();

            // 基础验证
            result.Merge(ValidateBasicConstraints(config));
            
            // 规则验证
            result.Merge(ValidateRules(config));
            
            // 业务逻辑验证
            result.Merge(ValidateBusinessLogic(config));
            
            // 依赖关系验证
            result.Merge(ValidateDependencies(config));

            return result;
        }

        /// <summary>
        /// 验证单个属性
        /// </summary>
        public static ValidationResult ValidateProperty(string propertyPath, object? value, ApplicationConfiguration config)
        {
            var result = new ValidationResult();
            
            var rules = _validationRules.Where(r => r.PropertyPath == propertyPath).ToList();
            foreach (var rule in rules)
            {
                if (!ValidateRule(rule, value, config))
                {
                    result.AddError(propertyPath, rule.ErrorMessage);
                }
            }

            return result;
        }

        /// <summary>
        /// 添加自定义验证规则
        /// </summary>
        public static void AddValidationRule(ValidationRule rule)
        {
            _validationRules.Add(rule);
        }

        #region 私有验证方法

        private static void InitializeValidationRules()
        {
            // 通用设置验证规则
            _validationRules.AddRange(new[]
            {
                new ValidationRule
                {
                    Type = ValidationRuleType.Required,
                    PropertyPath = "General.ApplicationTitle",
                    ErrorMessage = "应用程序标题不能为空"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.StringLength,
                    PropertyPath = "General.ApplicationTitle",
                    MinLength = 1,
                    MaxLength = 100,
                    ErrorMessage = "应用程序标题长度必须在1-100字符之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "General.AutoSaveInterval",
                    MinValue = 1,
                    MaxValue = 60,
                    ErrorMessage = "自动保存间隔必须在1-60分钟之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "General.MaxBackupCount",
                    MinValue = 1,
                    MaxValue = 100,
                    ErrorMessage = "最大备份数量必须在1-100之间"
                }
            });

            // 模板设置验证规则
            _validationRules.AddRange(new[]
            {
                new ValidationRule
                {
                    Type = ValidationRuleType.DirectoryExists,
                    PropertyPath = "Template.TemplateDirectory",
                    ErrorMessage = "模板目录不存在"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Template.TemplateBackupCount",
                    MinValue = 1,
                    MaxValue = 20,
                    ErrorMessage = "模板备份数量必须在1-20之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Template.PreviewUpdateDelay",
                    MinValue = 100,
                    MaxValue = 5000,
                    ErrorMessage = "预览更新延迟必须在100-5000毫秒之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Template.EditorFontSize",
                    MinValue = 8,
                    MaxValue = 72,
                    ErrorMessage = "编辑器字体大小必须在8-72之间"
                }
            });

            // 性能设置验证规则
            _validationRules.AddRange(new[]
            {
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Performance.MaxCacheSize",
                    MinValue = 10,
                    MaxValue = 10000,
                    ErrorMessage = "最大缓存大小必须在10-10000之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Performance.MaxMemoryUsageMB",
                    MinValue = 10,
                    MaxValue = 2048,
                    ErrorMessage = "最大内存使用量必须在10-2048MB之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Performance.CacheExpirationHours",
                    MinValue = 0.1,
                    MaxValue = 24.0,
                    ErrorMessage = "缓存过期时间必须在0.1-24小时之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Performance.MaxConcurrentRenderThreads",
                    MinValue = 1,
                    MaxValue = 64,
                    ErrorMessage = "最大并发渲染线程数必须在1-64之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Performance.BatchProcessingSize",
                    MinValue = 1,
                    MaxValue = 1000,
                    ErrorMessage = "批量处理大小必须在1-1000之间"
                }
            });

            // UI设置验证规则
            _validationRules.AddRange(new[]
            {
                new ValidationRule
                {
                    Type = ValidationRuleType.ValidEnum,
                    PropertyPath = "UI.Theme",
                    ErrorMessage = "无效的主题设置"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Required,
                    PropertyPath = "UI.FontFamily",
                    ErrorMessage = "字体族不能为空"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "UI.FontSize",
                    MinValue = 8,
                    MaxValue = 72,
                    ErrorMessage = "字体大小必须在8-72之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "UI.LeftPanelWidth",
                    MinValue = 100,
                    MaxValue = 1000,
                    ErrorMessage = "左侧面板宽度必须在100-1000之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "UI.RightPanelWidth",
                    MinValue = 100,
                    MaxValue = 1000,
                    ErrorMessage = "右侧面板宽度必须在100-1000之间"
                }
            });

            // 导出设置验证规则
            _validationRules.AddRange(new[]
            {
                new ValidationRule
                {
                    Type = ValidationRuleType.Required,
                    PropertyPath = "Export.DefaultExportPath",
                    ErrorMessage = "默认导出路径不能为空"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.DirectoryExists,
                    PropertyPath = "Export.DefaultExportPath",
                    ErrorMessage = "默认导出路径不存在"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Export.IndentationSize",
                    MinValue = 1,
                    MaxValue = 16,
                    ErrorMessage = "缩进大小必须在1-16之间"
                }
            });

            // 高级设置验证规则
            _validationRules.AddRange(new[]
            {
                new ValidationRule
                {
                    Type = ValidationRuleType.ValidEnum,
                    PropertyPath = "Advanced.LogLevel",
                    ErrorMessage = "无效的日志级别"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Advanced.MaxLogFileSizeMB",
                    MinValue = 1,
                    MaxValue = 100,
                    ErrorMessage = "最大日志文件大小必须在1-100MB之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Advanced.LogRetentionDays",
                    MinValue = 1,
                    MaxValue = 365,
                    ErrorMessage = "日志保留天数必须在1-365之间"
                },
                new ValidationRule
                {
                    Type = ValidationRuleType.Range,
                    PropertyPath = "Advanced.NetworkTimeoutSeconds",
                    MinValue = 5,
                    MaxValue = 300,
                    ErrorMessage = "网络超时时间必须在5-300秒之间"
                }
            });
        }

        private static ValidationResult ValidateBasicConstraints(ApplicationConfiguration config)
        {
            var result = new ValidationResult();

            // 使用反射进行基本的Data Annotation验证
            ValidateObjectRecursively(config, "", result);

            return result;
        }

        private static void ValidateObjectRecursively(object obj, string basePath, ValidationResult result)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var propertyPath = string.IsNullOrEmpty(basePath) ? property.Name : $"{basePath}.{property.Name}";
                var value = property.GetValue(obj);

                // 检查Required属性
                var requiredAttr = property.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttr != null)
                {
                    if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                    {
                        result.AddError(propertyPath, requiredAttr.ErrorMessage ?? $"{property.Name}是必需的");
                    }
                }

                // 检查Range属性
                var rangeAttr = property.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr != null && value != null)
                {
                    try
                    {
                        var numValue = Convert.ToDouble(value);
                        var min = Convert.ToDouble(rangeAttr.Minimum);
                        var max = Convert.ToDouble(rangeAttr.Maximum);
                        
                        if (numValue < min || numValue > max)
                        {
                            result.AddError(propertyPath, rangeAttr.ErrorMessage ?? $"{property.Name}必须在{min}-{max}之间");
                        }
                    }
                    catch
                    {
                        result.AddError(propertyPath, $"{property.Name}的值无法转换为数字");
                    }
                }

                // 检查StringLength属性
                var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
                if (stringLengthAttr != null && value is string stringValue)
                {
                    if (stringValue.Length < stringLengthAttr.MinimumLength || 
                        stringValue.Length > stringLengthAttr.MaximumLength)
                    {
                        result.AddError(propertyPath, stringLengthAttr.ErrorMessage ?? 
                            $"{property.Name}长度必须在{stringLengthAttr.MinimumLength}-{stringLengthAttr.MaximumLength}之间");
                    }
                }

                // 递归验证复杂对象
                if (value != null && !IsSimpleType(property.PropertyType))
                {
                    ValidateObjectRecursively(value, propertyPath, result);
                }
            }
        }

        private static ValidationResult ValidateRules(ApplicationConfiguration config)
        {
            var result = new ValidationResult();

            foreach (var rule in _validationRules)
            {
                var value = GetPropertyValue(config, rule.PropertyPath);
                if (!ValidateRule(rule, value, config))
                {
                    result.AddError(rule.PropertyPath, rule.ErrorMessage);
                }
            }

            return result;
        }

        private static bool ValidateRule(ValidationRule rule, object? value, ApplicationConfiguration config)
        {
            return rule.Type switch
            {
                ValidationRuleType.Required => value != null && !string.IsNullOrWhiteSpace(value.ToString()),
                ValidationRuleType.Range => ValidateRange(value, rule.MinValue, rule.MaxValue),
                ValidationRuleType.StringLength => ValidateStringLength(value, rule.MinLength, rule.MaxLength),
                ValidationRuleType.RegularExpression => ValidateRegex(value, rule.Pattern),
                ValidationRuleType.Custom => rule.CustomValidator?.Invoke(value) ?? true,
                ValidationRuleType.FileExists => ValidateFileExists(value),
                ValidationRuleType.DirectoryExists => ValidateDirectoryExists(value),
                ValidationRuleType.ValidEnum => ValidateEnum(value, rule.PropertyPath),
                _ => true
            };
        }

        private static bool ValidateRange(object? value, object? minValue, object? maxValue)
        {
            if (value == null || minValue == null || maxValue == null) return true;

            try
            {
                var numValue = Convert.ToDouble(value);
                var min = Convert.ToDouble(minValue);
                var max = Convert.ToDouble(maxValue);
                return numValue >= min && numValue <= max;
            }
            catch
            {
                return false;
            }
        }

        private static bool ValidateStringLength(object? value, int minLength, int maxLength)
        {
            if (value is not string str) return true;
            return str.Length >= minLength && str.Length <= maxLength;
        }

        private static bool ValidateRegex(object? value, string pattern)
        {
            if (value is not string str || string.IsNullOrEmpty(pattern)) return true;
            return Regex.IsMatch(str, pattern);
        }

        private static bool ValidateFileExists(object? value)
        {
            if (value is not string path || string.IsNullOrEmpty(path)) return true;
            return File.Exists(path);
        }

        private static bool ValidateDirectoryExists(object? value)
        {
            if (value is not string path || string.IsNullOrEmpty(path)) return true;
            
            try
            {
                // 如果是相对路径，则不验证存在性
                if (!Path.IsPathRooted(path)) return true;
                
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private static bool ValidateEnum(object? value, string propertyPath)
        {
            if (value is not string enumValue) return true;

            // 根据属性路径确定枚举类型
            var enumType = propertyPath switch
            {
                "UI.Theme" => typeof(ThemeType),
                "Advanced.LogLevel" => typeof(LogLevel),
                "UI.WindowState" => typeof(FormWindowState),
                _ => null
            };

            if (enumType == null) return true;

            return Enum.TryParse(enumType, enumValue, true, out _);
        }

        private static ValidationResult ValidateBusinessLogic(ApplicationConfiguration config)
        {
            var result = new ValidationResult();

            // 性能配置业务逻辑验证
            if (config.Performance.MaxConcurrentRenderThreads > Environment.ProcessorCount * 2)
            {
                result.AddWarning("并发渲染线程数超过处理器核心数的2倍，可能影响系统性能");
            }

            if (config.Performance.MaxMemoryUsageMB > 1024)
            {
                result.AddWarning("内存使用量设置较高，请确保系统有足够的可用内存");
            }

            // 模板配置业务逻辑验证
            if (config.Template.PreviewUpdateDelay < 200)
            {
                result.AddWarning("预览更新延迟设置过低，可能影响编辑器性能");
            }

            // UI配置业务逻辑验证
            if (config.UI.LeftPanelWidth + config.UI.RightPanelWidth > 800)
            {
                result.AddWarning("左右面板宽度总和过大，在小屏幕上可能显示不完整");
            }

            return result;
        }

        private static ValidationResult ValidateDependencies(ApplicationConfiguration config)
        {
            var result = new ValidationResult();

            // 缓存相关依赖验证
            if (!config.Performance.EnableCaching && config.Performance.EnablePerformanceMonitoring)
            {
                result.AddWarning("禁用缓存时，性能监控的部分功能可能受限");
            }

            // 模板相关依赖验证
            if (!config.Template.EnableTemplateValidation && config.Template.AutoFormatTemplate)
            {
                result.AddWarning("禁用模板验证时，自动格式化功能可能不稳定");
            }

            // 高级功能依赖验证
            if (config.Advanced.EnableExperimentalFeatures && !config.Advanced.EnableDebugMode)
            {
                result.AddWarning("启用实验功能时建议同时启用调试模式");
            }

            return result;
        }

        private static object? GetPropertyValue(object obj, string propertyPath)
        {
            var parts = propertyPath.Split('.');
            var current = obj;

            foreach (var part in parts)
            {
                if (current == null) return null;
                
                var property = current.GetType().GetProperty(part);
                if (property == null) return null;
                
                current = property.GetValue(current);
            }

            return current;
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive || 
                   type.IsEnum || 
                   type == typeof(string) || 
                   type == typeof(DateTime) || 
                   type == typeof(decimal) || 
                   type == typeof(Guid) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        #endregion
    }
}