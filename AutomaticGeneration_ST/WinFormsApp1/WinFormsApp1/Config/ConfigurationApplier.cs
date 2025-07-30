using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 配置应用器 - 负责将配置更改应用到应用程序
    /// </summary>
    public static class ConfigurationApplier
    {
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化配置应用器
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            // 监听配置变更事件
            ConfigurationManager.ConfigurationChanged += OnConfigurationChanged;
            ConfigurationManager.ConfigurationLoaded += OnConfigurationLoaded;

            _isInitialized = true;
        }

        /// <summary>
        /// 应用所有当前配置
        /// </summary>
        public static void ApplyAllConfiguration()
        {
            var config = ConfigurationManager.Current;
            
            ApplyGeneralConfiguration(config.General);
            ApplyTemplateConfiguration(config.Template);
            ApplyPerformanceConfiguration(config.Performance);
            ApplyUIConfiguration(config.UI);
            ApplyExportConfiguration(config.Export);
            ApplyAdvancedConfiguration(config.Advanced);
        }

        /// <summary>
        /// 配置变更事件处理
        /// </summary>
        private static void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
        {
            try
            {
                ApplyConfigurationChange(e.Key, e.NewValue, e.Category);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用配置变更失败 {e.Key}: {ex.Message}");
            }
        }

        /// <summary>
        /// 配置加载事件处理
        /// </summary>
        private static void OnConfigurationLoaded(object? sender, ApplicationConfiguration config)
        {
            ApplyAllConfiguration();
        }

        /// <summary>
        /// 应用单个配置变更
        /// </summary>
        private static void ApplyConfigurationChange(string key, object? value, SettingsCategory category)
        {
            switch (category)
            {
                case SettingsCategory.General:
                    ApplyGeneralConfigurationChange(key, value);
                    break;
                case SettingsCategory.Template:
                    ApplyTemplateConfigurationChange(key, value);
                    break;
                case SettingsCategory.Performance:
                    ApplyPerformanceConfigurationChange(key, value);
                    break;
                case SettingsCategory.UI:
                    ApplyUIConfigurationChange(key, value);
                    break;
                case SettingsCategory.Export:
                    ApplyExportConfigurationChange(key, value);
                    break;
                case SettingsCategory.Advanced:
                    ApplyAdvancedConfigurationChange(key, value);
                    break;
            }
        }

        #region 应用各类别配置

        private static void ApplyGeneralConfiguration(GeneralConfiguration config)
        {
            try
            {
                // 应用应用程序标题
                var mainForm = Application.OpenForms["Form1"];
                if (mainForm != null)
                {
                    mainForm.Text = config.ApplicationTitle;
                }

                // 应用语言设置
                if (!string.IsNullOrEmpty(config.DefaultLanguage))
                {
                    if (Enum.TryParse<SupportedLanguage>(config.DefaultLanguage, out var language))
                    {
                        LanguageManager.SetLanguage(language);
                    }
                }

                // 其他通用配置应用...
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用通用配置失败: {ex.Message}");
            }
        }

        private static void ApplyTemplateConfiguration(TemplateConfiguration config)
        {
            try
            {
                // 应用模板缓存配置 (暂时禁用)
                // TODO: 实现TemplateCacheManager
                // TemplateCacheManager.Configure(
                //     maxCacheSize: 500,
                //     defaultExpiration: TimeSpan.FromHours(2),
                //     enablePerformanceMonitoring: config.EnableTemplatePreview
                // );

                // 其他模板配置应用...
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用模板配置失败: {ex.Message}");
            }
        }

        private static void ApplyPerformanceConfiguration(PerformanceConfiguration config)
        {
            try
            {
                // 应用缓存配置
                if (config.EnableCaching)
                {
                    TemplateCacheManager.Configure(
                        maxCacheSize: config.MaxCacheSize,
                        maxMemoryUsage: config.MaxMemoryUsageMB * 1024 * 1024,
                        defaultExpiration: TimeSpan.FromHours(config.CacheExpirationHours)
                    );
                }
                else
                {
                    TemplateManager.ClearTemplateCache();
                }

                // 其他性能配置应用...
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用性能配置失败: {ex.Message}");
            }
        }

        private static void ApplyUIConfiguration(UIConfiguration config)
        {
            try
            {
                // 应用主题
                if (Enum.TryParse<ThemeType>(config.Theme, out var themeType))
                {
                    ThemeManager.SetTheme(themeType);
                }

                // 应用字体设置
                if (!string.IsNullOrEmpty(config.FontFamily) && config.FontSize > 0)
                {
                    var font = new Font(config.FontFamily, config.FontSize);
                    var allForms = Application.OpenForms.Cast<Form>();
                    foreach (var form in allForms)
                        ControlStyleManager.SetDefaultFont(form, font);
                }

                // 应用窗口状态
                var mainForm = Application.OpenForms["Form1"];
                if (mainForm != null)
                {
                    if (Enum.TryParse<FormWindowState>(config.WindowState, out var windowState))
                    {
                        mainForm.WindowState = windowState;
                    }
                }

                // 其他UI配置应用...
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用UI配置失败: {ex.Message}");
            }
        }

        private static void ApplyExportConfiguration(ExportConfiguration config)
        {
            try
            {
                // 导出配置主要在导出时应用，这里可以设置一些全局默认值
                // 例如设置OutputManager的默认配置
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用导出配置失败: {ex.Message}");
            }
        }

        private static void ApplyAdvancedConfiguration(AdvancedConfiguration config)
        {
            try
            {
                // 应用调试模式
                if (config.EnableDebugMode)
                {
                    // 启用详细日志
                    WinFormsApp1.LogService.Instance.SetLogLevel(WinFormsApp1.LogLevel.Debug);
                }
                else
                {
                    // 根据配置的日志级别设置
                    if (Enum.TryParse<WinFormsApp1.LogLevel>(config.LogLevel, out var logLevel))
                    {
                        WinFormsApp1.LogService.Instance.SetLogLevel(logLevel);
                    }
                }

                // 其他高级配置应用...
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用高级配置失败: {ex.Message}");
            }
        }

        #endregion

        #region 应用单个配置变更

        private static void ApplyGeneralConfigurationChange(string key, object? value)
        {
            var parts = key.Split('.');
            if (parts.Length != 2) return;

            var propertyName = parts[1];

            switch (propertyName)
            {
                case "ApplicationTitle":
                    if (value is string title)
                    {
                        var mainForm = Application.OpenForms["Form1"];
                        if (mainForm != null)
                        {
                            mainForm.Text = title;
                        }
                    }
                    break;

                case "DefaultLanguage":
                    if (value is string language)
                    {
                        if (Enum.TryParse<SupportedLanguage>(language, out var supportedLang))
                        {
                            LanguageManager.SetLanguage(supportedLang);
                        }
                    }
                    break;
            }
        }

        private static void ApplyTemplateConfigurationChange(string key, object? value)
        {
            // 模板配置变更处理
        }

        private static void ApplyPerformanceConfigurationChange(string key, object? value)
        {
            var parts = key.Split('.');
            if (parts.Length != 2) return;

            var propertyName = parts[1];

            switch (propertyName)
            {
                case "EnableCaching":
                    if (value is bool enableCaching)
                    {
                        if (!enableCaching)
                        {
                            TemplateManager.ClearTemplateCache();
                        }
                    }
                    break;

                case "MaxCacheSize":
                case "MaxMemoryUsageMB":
                case "CacheExpirationHours":
                    // 重新配置缓存管理器
                    var config = ConfigurationManager.Current.Performance;
                    TemplateCacheManager.Configure(
                        maxCacheSize: config.MaxCacheSize,
                        maxMemoryUsage: config.MaxMemoryUsageMB * 1024 * 1024,
                        defaultExpiration: TimeSpan.FromHours(config.CacheExpirationHours)
                    );
                    break;
            }
        }

        private static void ApplyUIConfigurationChange(string key, object? value)
        {
            var parts = key.Split('.');
            if (parts.Length != 2) return;

            var propertyName = parts[1];

            switch (propertyName)
            {
                case "Theme":
                    if (value is string theme && Enum.TryParse<ThemeType>(theme, out var themeType))
                    {
                        ThemeManager.SetTheme(themeType);
                    }
                    break;

                case "FontFamily":
                case "FontSize":
                    var uiConfig = ConfigurationManager.Current.UI;
                    if (!string.IsNullOrEmpty(uiConfig.FontFamily) && uiConfig.FontSize > 0)
                    {
                        var font = new Font(uiConfig.FontFamily, uiConfig.FontSize);
                        // TODO: 需要获取适当的Control对象来设置字体
                        // ControlStyleManager.SetDefaultFont(targetControl, font);
                    }
                    break;
            }
        }

        private static void ApplyExportConfigurationChange(string key, object? value)
        {
            // 导出配置变更处理
        }

        private static void ApplyAdvancedConfigurationChange(string key, object? value)
        {
            var parts = key.Split('.');
            if (parts.Length != 2) return;

            var propertyName = parts[1];

            switch (propertyName)
            {
                case "EnableDebugMode":
                    if (value is bool enableDebug)
                    {
                        WinFormsApp1.LogService.Instance.SetLogLevel(enableDebug ? WinFormsApp1.LogLevel.Debug : WinFormsApp1.LogLevel.Info);
                    }
                    break;

                case "LogLevel":
                    if (value is string logLevel && Enum.TryParse<WinFormsApp1.LogLevel>(logLevel, out var level))
                    {
                        WinFormsApp1.LogService.Instance.SetLogLevel(level);
                    }
                    break;
            }
        }

        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Dispose()
        {
            if (_isInitialized)
            {
                ConfigurationManager.ConfigurationChanged -= OnConfigurationChanged;
                ConfigurationManager.ConfigurationLoaded -= OnConfigurationLoaded;
                _isInitialized = false;
            }
        }
    }
}