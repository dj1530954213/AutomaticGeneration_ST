using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 应用程序设置类别
    /// </summary>
    public enum SettingsCategory
    {
        General,      // 通用设置
        Template,     // 模板设置
        Performance,  // 性能设置
        UI,          // 界面设置
        Export,      // 导出设置
        Advanced     // 高级设置
    }

    /// <summary>
    /// 配置项类型
    /// </summary>
    public enum ConfigItemType
    {
        String,
        Integer,
        Double,
        Boolean,
        Enum,
        List,
        Object,
        FilePath,
        DirectoryPath,
        Color
    }

    /// <summary>
    /// 配置项定义
    /// </summary>
    public class ConfigItemDefinition
    {
        public string Key { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public ConfigItemType Type { get; set; }
        public SettingsCategory Category { get; set; }
        public object? DefaultValue { get; set; }
        public object? MinValue { get; set; }
        public object? MaxValue { get; set; }
        public List<object>? ValidValues { get; set; }
        public bool IsRequired { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;
        public bool RequiresRestart { get; set; } = false;
        public string Group { get; set; } = "";
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// 通用应用程序配置
    /// </summary>
    public class GeneralConfiguration
    {
        [DisplayName("应用程序标题")]
        [Description("显示在主窗口标题栏的应用程序名称")]
        public string ApplicationTitle { get; set; } = "ST脚本自动生成器 v0.1";

        [DisplayName("默认语言")]
        [Description("应用程序界面语言")]
        public string DefaultLanguage { get; set; } = "zh-CN";

        [DisplayName("自动保存间隔")]
        [Description("自动保存配置的间隔时间（分钟）")]
        public int AutoSaveInterval { get; set; } = 5;

        [DisplayName("启用自动备份")]
        [Description("是否自动创建配置和数据备份")]
        public bool EnableAutoBackup { get; set; } = true;

        [DisplayName("最大备份数量")]
        [Description("保留的最大备份文件数量")]
        public int MaxBackupCount { get; set; } = 10;

        [DisplayName("启用崩溃报告")]
        [Description("是否自动收集和发送崩溃报告")]
        public bool EnableCrashReporting { get; set; } = false;

        [DisplayName("启用使用统计")]
        [Description("是否收集匿名使用统计信息")]
        public bool EnableUsageStatistics { get; set; } = false;

        [DisplayName("检查更新")]
        [Description("启动时检查软件更新")]
        public bool CheckForUpdates { get; set; } = true;
    }

    /// <summary>
    /// 模板系统配置
    /// </summary>
    public class TemplateConfiguration
    {
        [DisplayName("默认模板版本")]
        [Description("新建点位时使用的默认模板版本")]
        public string DefaultTemplateVersion { get; set; } = "Default";

        [DisplayName("模板目录")]
        [Description("模板文件存储的根目录")]
        public string TemplateDirectory { get; set; } = "Templates";

        [DisplayName("启用模板验证")]
        [Description("保存模板时进行语法验证")]
        public bool EnableTemplateValidation { get; set; } = true;

        [DisplayName("自动格式化模板")]
        [Description("保存时自动格式化模板代码")]
        public bool AutoFormatTemplate { get; set; } = true;

        [DisplayName("模板备份数量")]
        [Description("每个模板保留的备份版本数量")]
        public int TemplateBackupCount { get; set; } = 5;

        [DisplayName("启用模板预览")]
        [Description("编辑时实时预览模板渲染结果")]
        public bool EnableTemplatePreview { get; set; } = true;

        [DisplayName("预览更新延迟")]
        [Description("预览更新的延迟时间（毫秒）")]
        public int PreviewUpdateDelay { get; set; } = 500;

        [DisplayName("模板编辑器字体")]
        [Description("模板编辑器使用的字体名称")]
        public string EditorFontName { get; set; } = "Consolas";

        [DisplayName("模板编辑器字体大小")]
        [Description("模板编辑器字体大小")]
        public int EditorFontSize { get; set; } = 12;

        [DisplayName("启用语法高亮")]
        [Description("在模板编辑器中启用语法高亮")]
        public bool EnableSyntaxHighlighting { get; set; } = true;

        [DisplayName("显示行号")]
        [Description("在模板编辑器中显示行号")]
        public bool ShowLineNumbers { get; set; } = true;

        [DisplayName("自动完成")]
        [Description("启用代码自动完成功能")]
        public bool EnableAutoComplete { get; set; } = true;
    }

    /// <summary>
    /// 性能配置
    /// </summary>
    public class PerformanceConfiguration
    {
        [DisplayName("启用缓存")]
        [Description("启用模板编译和渲染缓存")]
        public bool EnableCaching { get; set; } = true;

        [DisplayName("最大缓存大小")]
        [Description("最大缓存条目数量")]
        public int MaxCacheSize { get; set; } = 500;

        [DisplayName("最大内存使用")]
        [Description("缓存最大内存使用量（MB）")]
        public int MaxMemoryUsageMB { get; set; } = 50;

        [DisplayName("缓存过期时间")]
        [Description("缓存条目过期时间（小时）")]
        public double CacheExpirationHours { get; set; } = 2.0;

        [DisplayName("启用性能监控")]
        [Description("启用详细的性能监控和统计")]
        public bool EnablePerformanceMonitoring { get; set; } = true;

        [DisplayName("并发渲染线程数")]
        [Description("并发模板渲染的最大线程数")]
        public int MaxConcurrentRenderThreads { get; set; } = Environment.ProcessorCount;

        [DisplayName("批量处理大小")]
        [Description("批量处理点位的单批次大小")]
        public int BatchProcessingSize { get; set; } = 100;

        [DisplayName("启用预热")]
        [Description("启动时预热常用模板")]
        public bool EnablePrewarming { get; set; } = true;

        [DisplayName("内存清理间隔")]
        [Description("自动内存清理间隔（分钟）")]
        public int MemoryCleanupIntervalMinutes { get; set; } = 10;
    }

    /// <summary>
    /// 用户界面配置
    /// </summary>
    public class UIConfiguration
    {
        [DisplayName("主题")]
        [Description("应用程序主题")]
        public string Theme { get; set; } = "Light";

        [DisplayName("字体族")]
        [Description("应用程序默认字体族")]
        public string FontFamily { get; set; } = "微软雅黑";

        [DisplayName("字体大小")]
        [Description("应用程序默认字体大小")]
        public int FontSize { get; set; } = 10;

        [DisplayName("显示工具栏")]
        [Description("是否显示主工具栏")]
        public bool ShowToolbar { get; set; } = true;

        [DisplayName("显示状态栏")]
        [Description("是否显示状态栏")]
        public bool ShowStatusBar { get; set; } = true;

        [DisplayName("启用动画")]
        [Description("启用界面动画效果")]
        public bool EnableAnimations { get; set; } = true;

        [DisplayName("窗口状态")]
        [Description("启动时的窗口状态")]
        public string WindowState { get; set; } = "Maximized";

        [DisplayName("记住窗口位置")]
        [Description("记住窗口位置和大小")]
        public bool RememberWindowPosition { get; set; } = true;

        [DisplayName("左侧面板宽度")]
        [Description("左侧面板的默认宽度")]
        public int LeftPanelWidth { get; set; } = 250;

        [DisplayName("右侧面板宽度")]
        [Description("右侧面板的默认宽度")]
        public int RightPanelWidth { get; set; } = 300;

        [DisplayName("日志面板高度")]
        [Description("日志面板的默认高度")]
        public int LogPanelHeight { get; set; } = 200;

        [DisplayName("显示提示信息")]
        [Description("显示操作提示和帮助信息")]
        public bool ShowTooltips { get; set; } = true;

        [DisplayName("确认删除操作")]
        [Description("删除操作前显示确认对话框")]
        public bool ConfirmDeleteOperations { get; set; } = true;
    }

    /// <summary>
    /// 导出配置
    /// </summary>
    public class ExportConfiguration
    {
        [DisplayName("默认导出格式")]
        [Description("默认的文件导出格式")]
        public string DefaultExportFormat { get; set; } = "ST";

        [DisplayName("默认导出路径")]
        [Description("文件导出的默认路径")]
        public string DefaultExportPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [DisplayName("包含头注释")]
        [Description("导出文件时包含头部注释")]
        public bool IncludeHeaderComment { get; set; } = true;

        [DisplayName("包含时间戳")]
        [Description("导出文件时包含生成时间戳")]
        public bool IncludeTimestamp { get; set; } = true;

        [DisplayName("包含版本信息")]
        [Description("导出文件时包含软件版本信息")]
        public bool IncludeVersionInfo { get; set; } = true;

        [DisplayName("文件编码")]
        [Description("导出文件的字符编码")]
        public string FileEncoding { get; set; } = "UTF-8";

        [DisplayName("行结束符")]
        [Description("导出文件的行结束符类型")]
        public string LineEnding { get; set; } = "CRLF";

        [DisplayName("缩进类型")]
        [Description("代码缩进使用的字符类型")]
        public string IndentationType { get; set; } = "Spaces";

        [DisplayName("缩进大小")]
        [Description("代码缩进的字符数量")]
        public int IndentationSize { get; set; } = 4;

        [DisplayName("自动打开导出文件")]
        [Description("导出完成后自动打开文件")]
        public bool AutoOpenExportedFile { get; set; } = false;

        [DisplayName("压缩输出")]
        [Description("导出时移除多余的空白字符")]
        public bool CompressOutput { get; set; } = false;
    }

    /// <summary>
    /// 高级配置
    /// </summary>
    public class AdvancedConfiguration
    {
        [DisplayName("启用调试模式")]
        [Description("启用详细的调试日志记录")]
        public bool EnableDebugMode { get; set; } = false;

        [DisplayName("日志级别")]
        [Description("应用程序日志记录级别")]
        public string LogLevel { get; set; } = "Info";

        [DisplayName("最大日志文件大小")]
        [Description("单个日志文件的最大大小（MB）")]
        public int MaxLogFileSizeMB { get; set; } = 10;

        [DisplayName("日志文件保留天数")]
        [Description("日志文件保留的天数")]
        public int LogRetentionDays { get; set; } = 30;

        [DisplayName("启用插件系统")]
        [Description("启用第三方插件支持")]
        public bool EnablePluginSystem { get; set; } = false;

        [DisplayName("插件目录")]
        [Description("插件文件存储目录")]
        public string PluginDirectory { get; set; } = "Plugins";

        [DisplayName("网络超时")]
        [Description("网络操作超时时间（秒）")]
        public int NetworkTimeoutSeconds { get; set; } = 30;

        [DisplayName("启用实验功能")]
        [Description("启用实验性功能（可能不稳定）")]
        public bool EnableExperimentalFeatures { get; set; } = false;

        [DisplayName("自定义配置路径")]
        [Description("自定义配置文件存储路径")]
        public string CustomConfigPath { get; set; } = "";

        [DisplayName("启用远程配置")]
        [Description("从远程服务器同步配置")]
        public bool EnableRemoteConfig { get; set; } = false;

        [DisplayName("远程配置URL")]
        [Description("远程配置服务器地址")]
        public string RemoteConfigUrl { get; set; } = "";
    }

    /// <summary>
    /// 主配置模型
    /// </summary>
    public class ApplicationConfiguration
    {
        [JsonPropertyName("general")]
        public GeneralConfiguration General { get; set; } = new();

        [JsonPropertyName("template")]
        public TemplateConfiguration Template { get; set; } = new();

        [JsonPropertyName("performance")]
        public PerformanceConfiguration Performance { get; set; } = new();

        [JsonPropertyName("ui")]
        public UIConfiguration UI { get; set; } = new();

        [JsonPropertyName("export")]
        public ExportConfiguration Export { get; set; } = new();

        [JsonPropertyName("advanced")]
        public AdvancedConfiguration Advanced { get; set; } = new();

        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.0.0";

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;

        [JsonPropertyName("configurationId")]
        public string ConfigurationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 获取所有配置类别
        /// </summary>
        public Dictionary<SettingsCategory, object> GetAllCategories()
        {
            return new Dictionary<SettingsCategory, object>
            {
                [SettingsCategory.General] = General,
                [SettingsCategory.Template] = Template,
                [SettingsCategory.Performance] = Performance,
                [SettingsCategory.UI] = UI,
                [SettingsCategory.Export] = Export,
                [SettingsCategory.Advanced] = Advanced
            };
        }

        /// <summary>
        /// 验证配置有效性
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            // 验证通用配置
            if (General.AutoSaveInterval < 1 || General.AutoSaveInterval > 60)
                errors.Add("自动保存间隔必须在1-60分钟之间");

            if (General.MaxBackupCount < 1 || General.MaxBackupCount > 100)
                errors.Add("最大备份数量必须在1-100之间");

            // 验证性能配置
            if (Performance.MaxCacheSize < 10 || Performance.MaxCacheSize > 10000)
                errors.Add("最大缓存大小必须在10-10000之间");

            if (Performance.MaxMemoryUsageMB < 10 || Performance.MaxMemoryUsageMB > 1000)
                errors.Add("最大内存使用量必须在10-1000MB之间");

            if (Performance.MaxConcurrentRenderThreads < 1 || Performance.MaxConcurrentRenderThreads > 64)
                errors.Add("并发渲染线程数必须在1-64之间");

            // 验证UI配置
            if (UI.FontSize < 8 || UI.FontSize > 72)
                errors.Add("字体大小必须在8-72之间");

            if (UI.LeftPanelWidth < 100 || UI.LeftPanelWidth > 1000)
                errors.Add("左侧面板宽度必须在100-1000之间");

            // 验证导出配置
            if (Export.IndentationSize < 1 || Export.IndentationSize > 16)
                errors.Add("缩进大小必须在1-16之间");

            // 验证高级配置
            if (Advanced.MaxLogFileSizeMB < 1 || Advanced.MaxLogFileSizeMB > 100)
                errors.Add("最大日志文件大小必须在1-100MB之间");

            if (Advanced.LogRetentionDays < 1 || Advanced.LogRetentionDays > 365)
                errors.Add("日志保留天数必须在1-365之间");

            if (Advanced.NetworkTimeoutSeconds < 5 || Advanced.NetworkTimeoutSeconds > 300)
                errors.Add("网络超时时间必须在5-300秒之间");

            return errors;
        }

        /// <summary>
        /// 重置为默认值
        /// </summary>
        public void ResetToDefaults()
        {
            General = new GeneralConfiguration();
            Template = new TemplateConfiguration();
            Performance = new PerformanceConfiguration();
            UI = new UIConfiguration();
            Export = new ExportConfiguration();
            Advanced = new AdvancedConfiguration();
            LastModified = DateTime.Now;
        }

        /// <summary>
        /// 创建副本
        /// </summary>
        public ApplicationConfiguration Clone()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            return System.Text.Json.JsonSerializer.Deserialize<ApplicationConfiguration>(json) ?? new ApplicationConfiguration();
        }
    }
}