using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 配置热更新管理器 - 支持无需重启即可应用新配置
    /// </summary>
    public static class ConfigurationHotReloadManager
    {
        #region 事件定义

        /// <summary>
        /// 配置热更新事件参数
        /// </summary>
        public class ConfigurationHotReloadEventArgs : EventArgs
        {
            public SettingsCategory Category { get; set; }
            public string PropertyName { get; set; } = "";
            public object? OldValue { get; set; }
            public object? NewValue { get; set; }
            public bool RequiresUIRefresh { get; set; }
            public bool RequiresServiceRestart { get; set; }
        }

        /// <summary>
        /// 热更新结果
        /// </summary>
        public class HotReloadResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public List<string> AppliedChanges { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
            public List<string> Errors { get; set; } = new();
            public bool RequiresRestart { get; set; }
        }

        /// <summary>
        /// 配置变更处理器委托
        /// </summary>
        public delegate Task<bool> ConfigurationChangeHandler(ConfigurationHotReloadEventArgs args);

        #endregion

        #region 事件和处理器

        /// <summary>
        /// 配置热更新事件
        /// </summary>
        public static event EventHandler<ConfigurationHotReloadEventArgs>? ConfigurationHotReloaded;

        /// <summary>
        /// 配置变更处理器映射
        /// </summary>
        private static readonly Dictionary<string, ConfigurationChangeHandler> _changeHandlers = new();

        /// <summary>
        /// 需要重启的配置项
        /// </summary>
        private static readonly HashSet<string> _restartRequiredSettings = new()
        {
            "General.DefaultLanguage",
            "Performance.EnableCaching",
            "Advanced.EnableDebugMode",
            "Advanced.EnablePluginSystem",
            "UI.Theme"
        };

        /// <summary>
        /// 需要UI刷新的配置项
        /// </summary>
        private static readonly HashSet<string> _uiRefreshRequiredSettings = new()
        {
            "UI.FontFamily",
            "UI.FontSize",
            "UI.ShowToolbar",
            "UI.ShowStatusBar",
            "UI.LeftPanelWidth",
            "UI.RightPanelWidth",
            "UI.LogPanelHeight"
        };

        #endregion

        #region 初始化和注册

        /// <summary>
        /// 初始化热更新管理器
        /// </summary>
        public static void Initialize()
        {
            // 注册默认处理器
            RegisterDefaultHandlers();

            // 监听配置变更事件
            ConfigurationManager.ConfigurationChanged += OnConfigurationChanged;
            ConfigurationManager.ConfigurationLoaded += OnConfigurationLoaded;
        }

        /// <summary>
        /// 注册默认的配置变更处理器
        /// </summary>
        private static void RegisterDefaultHandlers()
        {
            // UI配置处理器
            RegisterChangeHandler("UI", HandleUIConfigurationChange);

            // 模板配置处理器
            RegisterChangeHandler("Template", HandleTemplateConfigurationChange);

            // 性能配置处理器
            RegisterChangeHandler("Performance", HandlePerformanceConfigurationChange);

            // 导出配置处理器
            RegisterChangeHandler("Export", HandleExportConfigurationChange);

            // 高级配置处理器
            RegisterChangeHandler("Advanced", HandleAdvancedConfigurationChange);
        }

        /// <summary>
        /// 注册配置变更处理器
        /// </summary>
        public static void RegisterChangeHandler(string categoryOrProperty, ConfigurationChangeHandler handler)
        {
            _changeHandlers[categoryOrProperty] = handler;
        }

        #endregion

        #region 主要方法

        /// <summary>
        /// 应用配置热更新
        /// </summary>
        public static async Task<HotReloadResult> ApplyHotReloadAsync(ApplicationConfiguration newConfig)
        {
            var result = new HotReloadResult();
            var appliedChanges = new List<string>();
            var warnings = new List<string>();
            var errors = new List<string>();

            try
            {
                var currentConfig = ConfigurationManager.Current;
                var changes = DetectConfigurationChanges(currentConfig, newConfig);

                foreach (var change in changes)
                {
                    try
                    {
                        var applied = await ApplyConfigurationChangeAsync(change);
                        if (applied)
                        {
                            appliedChanges.Add($"{change.Category}.{change.PropertyName}");
                            
                            if (change.RequiresServiceRestart)
                            {
                                result.RequiresRestart = true;
                            }
                        }
                        else
                        {
                            warnings.Add($"配置项 {change.Category}.{change.PropertyName} 应用失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"应用配置项 {change.Category}.{change.PropertyName} 时出错: {ex.Message}");
                    }
                }

                result.Success = errors.Count == 0;
                result.Message = result.Success ? "配置热更新成功" : "配置热更新部分失败";
                result.AppliedChanges = appliedChanges;
                result.Warnings = warnings;
                result.Errors = errors;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"配置热更新失败: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// 检测配置变更
        /// </summary>
        private static List<ConfigurationHotReloadEventArgs> DetectConfigurationChanges(
            ApplicationConfiguration currentConfig, ApplicationConfiguration newConfig)
        {
            var changes = new List<ConfigurationHotReloadEventArgs>();

            // 检测各个类别的配置变更
            DetectCategoryChanges(SettingsCategory.General, currentConfig.General, newConfig.General, changes);
            DetectCategoryChanges(SettingsCategory.Template, currentConfig.Template, newConfig.Template, changes);
            DetectCategoryChanges(SettingsCategory.Performance, currentConfig.Performance, newConfig.Performance, changes);
            DetectCategoryChanges(SettingsCategory.UI, currentConfig.UI, newConfig.UI, changes);
            DetectCategoryChanges(SettingsCategory.Export, currentConfig.Export, newConfig.Export, changes);
            DetectCategoryChanges(SettingsCategory.Advanced, currentConfig.Advanced, newConfig.Advanced, changes);

            return changes;
        }

        /// <summary>
        /// 检测类别配置变更
        /// </summary>
        private static void DetectCategoryChanges(SettingsCategory category, object currentObj, object newObj, 
            List<ConfigurationHotReloadEventArgs> changes)
        {
            var properties = currentObj.GetType().GetProperties();
            
            foreach (var property in properties)
            {
                var currentValue = property.GetValue(currentObj);
                var newValue = property.GetValue(newObj);

                if (!Equals(currentValue, newValue))
                {
                    var propertyKey = $"{category}.{property.Name}";
                    
                    changes.Add(new ConfigurationHotReloadEventArgs
                    {
                        Category = category,
                        PropertyName = property.Name,
                        OldValue = currentValue,
                        NewValue = newValue,
                        RequiresUIRefresh = _uiRefreshRequiredSettings.Contains(propertyKey),
                        RequiresServiceRestart = _restartRequiredSettings.Contains(propertyKey)
                    });
                }
            }
        }

        /// <summary>
        /// 应用单个配置变更
        /// </summary>
        private static async Task<bool> ApplyConfigurationChangeAsync(ConfigurationHotReloadEventArgs change)
        {
            try
            {
                // 查找对应的处理器
                var categoryHandler = _changeHandlers.GetValueOrDefault(change.Category.ToString());
                var propertyHandler = _changeHandlers.GetValueOrDefault($"{change.Category}.{change.PropertyName}");

                // 优先使用属性特定的处理器
                var handler = propertyHandler ?? categoryHandler;

                if (handler != null)
                {
                    var success = await handler(change);
                    if (success)
                    {
                        // 触发热更新事件
                        ConfigurationHotReloaded?.Invoke(null, change);
                        return true;
                    }
                }
                else
                {
                    // 没有特定处理器，执行默认处理
                    return await ApplyDefaultConfigurationChangeAsync(change);
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 默认配置变更处理
        /// </summary>
        private static async Task<bool> ApplyDefaultConfigurationChangeAsync(ConfigurationHotReloadEventArgs change)
        {
            try
            {
                // 更新配置值
                var success = ConfigurationManager.SetValue($"{change.Category}.{change.PropertyName}", change.NewValue);
                
                if (success && change.RequiresUIRefresh)
                {
                    // 刷新UI
                    await RefreshUIAsync();
                }

                return success;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 配置变更处理器

        /// <summary>
        /// UI配置变更处理器
        /// </summary>
        private static async Task<bool> HandleUIConfigurationChange(ConfigurationHotReloadEventArgs args)
        {
            try
            {
                // 更新配置值
                ConfigurationManager.SetValue($"{args.Category}.{args.PropertyName}", args.NewValue);

                // 应用UI变更
                switch (args.PropertyName)
                {
                    case "FontFamily":
                    case "FontSize":
                        await ApplyFontChangesAsync();
                        break;
                        
                    case "ShowToolbar":
                        await ApplyToolbarVisibilityAsync((bool)(args.NewValue ?? false));
                        break;
                        
                    case "ShowStatusBar":
                        await ApplyStatusBarVisibilityAsync((bool)(args.NewValue ?? false));
                        break;
                        
                    case "LeftPanelWidth":
                    case "RightPanelWidth":
                    case "LogPanelHeight":
                        await ApplyLayoutChangesAsync();
                        break;
                        
                    default:
                        await RefreshUIAsync();
                        break;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 模板配置变更处理器
        /// </summary>
        private static async Task<bool> HandleTemplateConfigurationChange(ConfigurationHotReloadEventArgs args)
        {
            try
            {
                ConfigurationManager.SetValue($"{args.Category}.{args.PropertyName}", args.NewValue);

                // 根据具体的模板配置变更执行相应操作
                switch (args.PropertyName)
                {
                    case "EnableTemplateValidation":
                        // 更新模板验证设置
                        break;
                        
                    case "AutoFormatTemplate":
                        // 更新自动格式化设置
                        break;
                        
                    case "TemplateDirectory":
                        // 重新扫描模板目录
                        await Task.Run(() => TemplateManager.InitializeDefaultTemplates());
                        break;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 性能配置变更处理器
        /// </summary>
        private static async Task<bool> HandlePerformanceConfigurationChange(ConfigurationHotReloadEventArgs args)
        {
            try
            {
                ConfigurationManager.SetValue($"{args.Category}.{args.PropertyName}", args.NewValue);

                switch (args.PropertyName)
                {
                    case "EnableCaching":
                        if (!(bool)(args.NewValue ?? false))
                        {
                            TemplateManager.ClearTemplateCache();
                        }
                        break;
                        
                    case "MaxCacheSize":
                    case "MaxMemoryUsageMB":
                        // 需要重新配置缓存，通常需要重启
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 导出配置变更处理器
        /// </summary>
        private static async Task<bool> HandleExportConfigurationChange(ConfigurationHotReloadEventArgs args)
        {
            try
            {
                ConfigurationManager.SetValue($"{args.Category}.{args.PropertyName}", args.NewValue);
                // 导出配置通常立即生效，无需特殊处理
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 高级配置变更处理器
        /// </summary>
        private static async Task<bool> HandleAdvancedConfigurationChange(ConfigurationHotReloadEventArgs args)
        {
            try
            {
                ConfigurationManager.SetValue($"{args.Category}.{args.PropertyName}", args.NewValue);

                switch (args.PropertyName)
                {
                    case "EnableDebugMode":
                    case "LogLevel":
                        // 日志配置变更，需要重启日志系统
                        return false;
                        
                    case "EnablePluginSystem":
                        // 插件系统变更，需要重启
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region UI刷新方法

        /// <summary>
        /// 刷新UI
        /// </summary>
        private static async Task RefreshUIAsync()
        {
            await Task.Run(() =>
            {
                if (Application.OpenForms.Count > 0)
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        form.Invoke(new Action(() =>
                        {
                            try
                            {
                                // 应用主题
                                ThemeManager.ApplyTheme(form);
                                
                                // 刷新控件
                                form.Refresh();
                            }
                            catch
                            {
                                // 忽略刷新错误
                            }
                        }));
                    }
                }
            });
        }

        /// <summary>
        /// 应用字体变更
        /// </summary>
        private static async Task ApplyFontChangesAsync()
        {
            await Task.Run(() =>
            {
                var fontFamily = ConfigurationManager.GetValue<string>("UI.FontFamily", "微软雅黑");
                var fontSize = ConfigurationManager.GetValue<int>("UI.FontSize", 10);

                foreach (Form form in Application.OpenForms)
                {
                    form.Invoke(new Action(() =>
                    {
                        try
                        {
                            form.Font = new Font(fontFamily, fontSize);
                        }
                        catch
                        {
                            // 忽略字体应用错误
                        }
                    }));
                }
            });
        }

        /// <summary>
        /// 应用工具栏可见性变更
        /// </summary>
        private static async Task ApplyToolbarVisibilityAsync(bool visible)
        {
            await Task.Run(() =>
            {
                foreach (Form form in Application.OpenForms)
                {
                    form.Invoke(new Action(() =>
                    {
                        try
                        {
                            var toolStrips = form.Controls.OfType<ToolStrip>();
                            foreach (var toolStrip in toolStrips)
                            {
                                toolStrip.Visible = visible;
                            }
                        }
                        catch
                        {
                            // 忽略错误
                        }
                    }));
                }
            });
        }

        /// <summary>
        /// 应用状态栏可见性变更
        /// </summary>
        private static async Task ApplyStatusBarVisibilityAsync(bool visible)
        {
            await Task.Run(() =>
            {
                foreach (Form form in Application.OpenForms)
                {
                    form.Invoke(new Action(() =>
                    {
                        try
                        {
                            var statusStrips = form.Controls.OfType<StatusStrip>();
                            foreach (var statusStrip in statusStrips)
                            {
                                statusStrip.Visible = visible;
                            }
                        }
                        catch
                        {
                            // 忽略错误
                        }
                    }));
                }
            });
        }

        /// <summary>
        /// 应用布局变更
        /// </summary>
        private static async Task ApplyLayoutChangesAsync()
        {
            await Task.Run(() =>
            {
                foreach (Form form in Application.OpenForms)
                {
                    form.Invoke(new Action(() =>
                    {
                        try
                        {
                            // 查找SplitContainer并应用新的面板大小
                            var splitContainers = form.Controls.OfType<SplitContainer>();
                            foreach (var splitContainer in splitContainers)
                            {
                                // 根据配置更新分割距离
                                // 这里需要根据具体的窗体结构来调整
                            }
                        }
                        catch
                        {
                            // 忽略错误
                        }
                    }));
                }
            });
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 配置变更事件处理
        /// </summary>
        private static async void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
        {
            var args = new ConfigurationHotReloadEventArgs
            {
                Category = e.Category,
                PropertyName = e.Key.Split('.').LastOrDefault() ?? "",
                OldValue = e.OldValue,
                NewValue = e.NewValue,
                RequiresUIRefresh = _uiRefreshRequiredSettings.Contains(e.Key),
                RequiresServiceRestart = _restartRequiredSettings.Contains(e.Key)
            };

            await ApplyConfigurationChangeAsync(args);
        }

        /// <summary>
        /// 配置加载完成事件处理
        /// </summary>
        private static async void OnConfigurationLoaded(object? sender, ApplicationConfiguration e)
        {
            await RefreshUIAsync();
        }

        #endregion
    }
}