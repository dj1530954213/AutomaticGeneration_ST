using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Scriban;
using Scriban.Runtime;
using WinFormsApp1.Config;

namespace WinFormsApp1.Templates
{
    public enum TemplateVersion
    {
        Default,
        Advanced,
        Custom
    }

    public enum PointType
    {
        AI, AO, DI, DO
    }

    public class TemplateInfo
    {
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public TemplateVersion Version { get; set; }
        public PointType PointType { get; set; }
        public string Description { get; set; } = "";
        public string Author { get; set; } = "System";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public List<string> RequiredFields { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }

    public class TemplateConfig
    {
        public string Version { get; set; } = "1.0";
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public Dictionary<PointType, Dictionary<TemplateVersion, TemplateInfo>> Templates { get; set; } = new();
        public TemplateVersion DefaultVersion { get; set; } = TemplateVersion.Default;
        public string TemplateDirectory { get; set; } = "Templates";
    }

    public class TemplateManager
    {
        private static readonly Dictionary<string, Scriban.Template> _compiledTemplates = new();

        public static TemplateConfig Config => TemplateConfigManager.GetConfig();

        public static event Action<TemplateInfo>? TemplateAdded;
        public static event Action<TemplateInfo>? TemplateUpdated;
        public static event Action<TemplateInfo>? TemplateRemoved;

        static TemplateManager()
        {
            InitializeDefaultTemplates();
            
            // 配置缓存参数
            TemplateCacheManager.Configure(
                maxCacheSize: 500,
                maxMemoryUsage: 50 * 1024 * 1024, // 50MB
                defaultExpiration: TimeSpan.FromHours(2)
            );
        }

        public static void InitializeDefaultTemplates()
        {
            try
            {
                // 确保基础目录结构存在
                EnsureTemplateDirectories();
                
                // 创建默认模板（如果不存在）
                CreateDefaultTemplatesIfNotExists();
                
                // 扫描并注册所有模板
                ScanAndRegisterTemplates();
                
                // 保存配置通过配置管理器
                var config = Config;
                TemplateConfigManager.SaveConfig(config);
                
                // 异步预热模板缓存
                _ = Task.Run(PrewarmTemplateCache);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"初始化模板系统失败: {ex.Message}", ex);
            }
        }

        private static void EnsureTemplateDirectories()
        {
            var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            Directory.CreateDirectory(baseDir);
            
            foreach (PointType pointType in Enum.GetValues<PointType>())
            {
                Directory.CreateDirectory(Path.Combine(baseDir, pointType.ToString()));
            }
        }

        private static void CreateDefaultTemplatesIfNotExists()
        {
            var templates = new Dictionary<PointType, Dictionary<TemplateVersion, string>>
            {
                [PointType.AI] = new()
                {
                    [TemplateVersion.Default] = GetDefaultAITemplate(),
                    [TemplateVersion.Advanced] = GetAdvancedAITemplate()
                },
                [PointType.AO] = new()
                {
                    [TemplateVersion.Default] = GetDefaultAOTemplate(),
                    [TemplateVersion.Advanced] = GetAdvancedAOTemplate()
                },
                [PointType.DI] = new()
                {
                    [TemplateVersion.Default] = GetDefaultDITemplate(),
                    [TemplateVersion.Advanced] = GetAdvancedDITemplate()
                },
                [PointType.DO] = new()
                {
                    [TemplateVersion.Default] = GetDefaultDOTemplate(),
                    [TemplateVersion.Advanced] = GetAdvancedDOTemplate()
                }
            };

            foreach (var pointType in templates.Keys)
            {
                foreach (var version in templates[pointType].Keys)
                {
                    var fileName = $"{version.ToString().ToLower()}.scriban";
                    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                        "Templates", pointType.ToString(), fileName);
                    
                    if (!File.Exists(filePath))
                    {
                        File.WriteAllText(filePath, templates[pointType][version]);
                    }
                }
            }
        }

        private static void ScanAndRegisterTemplates()
        {
            var config = Config;
            config.Templates.Clear();

            var templatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            
            foreach (PointType pointType in Enum.GetValues<PointType>())
            {
                config.Templates[pointType] = new Dictionary<TemplateVersion, TemplateInfo>();
                var pointTypeDir = Path.Combine(templatesDir, pointType.ToString());
                
                if (Directory.Exists(pointTypeDir))
                {
                    var templateFiles = Directory.GetFiles(pointTypeDir, "*.scriban");
                    
                    foreach (var filePath in templateFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        if (Enum.TryParse<TemplateVersion>(fileName, true, out var version))
                        {
                            var templateInfo = new TemplateInfo
                            {
                                Name = $"{pointType} {version} Template",
                                FilePath = filePath,
                                Version = version,
                                PointType = pointType,
                                Description = GetTemplateDescription(pointType, version),
                                RequiredFields = GetRequiredFields(pointType),
                                ModifiedDate = File.GetLastWriteTime(filePath)
                            };
                            
                            config.Templates[pointType][version] = templateInfo;
                        }
                    }
                }
            }
            
            // 扫描自定义命名的模板文件（如阀门模板）
            ScanCustomNamedTemplates();
        }

        /// <summary>
        /// 扫描自定义命名的模板文件
        /// </summary>
        private static void ScanCustomNamedTemplates()
        {
            var templatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            
            // 扫描所有子文件夹
            var subDirectories = Directory.GetDirectories(templatesDir);
            
            foreach (var subDir in subDirectories)
            {
                var folderName = Path.GetFileName(subDir);
                
                // 跳过已知的点位类型文件夹，这些已经在上面处理过
                if (Enum.TryParse<PointType>(folderName, true, out var _))
                    continue;
                
                // 处理自定义文件夹（如"阀门"）
                var templateFiles = Directory.GetFiles(subDir, "*.scriban");
                
                foreach (var filePath in templateFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    
                    // 创建自定义模板信息
                    var templateInfo = new TemplateInfo
                    {
                        Name = fileName.Replace("_", " "), // ESDV_CTRL -> ESDV CTRL
                        FilePath = filePath,
                        Version = TemplateVersion.Custom,
                        PointType = InferPointTypeFromTemplate(filePath, folderName), // 推断点位类型
                        Description = $"{folderName}模板 - {fileName}",
                        Author = "System",
                        RequiredFields = ExtractRequiredFields(File.ReadAllText(filePath)),
                        ModifiedDate = File.GetLastWriteTime(filePath),
                        Metadata = new Dictionary<string, object>
                        {
                            ["Category"] = folderName,
                            ["CustomTemplate"] = true,
                            ["DeviceTemplate"] = true // 标记为设备模板
                        }
                    };
                    
                    // 确保该点位类型的字典存在
                    if (!Config.Templates.ContainsKey(templateInfo.PointType))
                    {
                        Config.Templates[templateInfo.PointType] = new Dictionary<TemplateVersion, TemplateInfo>();
                    }
                    
                    // 添加到模板配置中，使用文件名作为唯一标识
                    var customKey = $"Custom_{folderName}_{fileName}";
                    
                    // 由于字典只能用TemplateVersion作为key，我们需要扩展存储机制
                    // 暂时将其添加到Custom版本下，但在Metadata中保存详细信息
                    if (!Config.Templates[templateInfo.PointType].ContainsKey(TemplateVersion.Custom))
                    {
                        Config.Templates[templateInfo.PointType][TemplateVersion.Custom] = templateInfo;
                    }
                    
                    // 将自定义模板自动添加到模板库收藏夹
                    AutoAddToTemplateLibrary(templateInfo, folderName);
                }
            }
        }

        /// <summary>
        /// 从模板内容推断点位类型
        /// </summary>
        private static PointType InferPointTypeFromTemplate(string filePath, string folderName)
        {
            try
            {
                var content = File.ReadAllText(filePath).ToLower();
                
                // 根据文件夹名称和模板内容推断类型
                if (folderName.Contains("阀门") || folderName.Contains("valve"))
                {
                    // 阀门通常是数字输出类型
                    return PointType.DO;
                }
                
                // 根据模板内容关键词推断
                if (content.Contains("ai_") || content.Contains("模拟量输入"))
                    return PointType.AI;
                if (content.Contains("ao_") || content.Contains("模拟量输出"))
                    return PointType.AO;
                if (content.Contains("di_") || content.Contains("数字量输入"))
                    return PointType.DI;
                if (content.Contains("do_") || content.Contains("数字量输出") || content.Contains("mov") || content.Contains("valve"))
                    return PointType.DO;
                
                // 默认返回DO（因为大多数设备模板是控制类的）
                return PointType.DO;
            }
            catch
            {
                return PointType.DO;
            }
        }

        /// <summary>
        /// 自动将模板添加到模板库收藏夹
        /// </summary>
        private static void AutoAddToTemplateLibrary(TemplateInfo templateInfo, string category)
        {
            try
            {
                // 模板库管理器已移除，跳过收藏夹功能
                /*
                if (!WinFormsApp1.Templates.TemplateLibraryManager.LibraryConfig.Favorites.Any(f => 
                    f.Template.FilePath == templateInfo.FilePath))
                {
                    // 自动添加到收藏夹
                    var tags = new List<string> { category, "设备模板", "自动发现" };
                    
                    WinFormsApp1.Templates.TemplateLibraryManager.AddToFavorites(
                        templateInfo, 
                        tags: tags, 
                        notes: $"从{category}文件夹自动发现的模板"
                    );
                }
                */
            }
            catch (Exception ex)
            {
                // 添加到模板库失败不应该影响模板扫描
                System.Diagnostics.Debug.WriteLine($"自动添加模板到收藏夹失败: {ex.Message}");
            }
        }

        public static async Task<Scriban.Template> GetTemplateAsync(PointType pointType, TemplateVersion version = TemplateVersion.Default)
        {
            var templateInfo = GetTemplateInfo(pointType, version);
            if (templateInfo == null || !File.Exists(templateInfo.FilePath))
            {
                throw new FileNotFoundException($"模板文件不存在: {pointType} {version}");
            }

            var templateContent = await File.ReadAllTextAsync(templateInfo.FilePath);
            return await TemplateCacheManager.GetOrCompileTemplateAsync(templateInfo.FilePath, templateContent);
        }

        public static Scriban.Template GetTemplate(PointType pointType, TemplateVersion version = TemplateVersion.Default)
        {
            return GetTemplateAsync(pointType, version).GetAwaiter().GetResult();
        }

        public static TemplateInfo? GetTemplateInfo(PointType pointType, TemplateVersion version)
        {
            if (Config.Templates.ContainsKey(pointType) && 
                Config.Templates[pointType].ContainsKey(version))
            {
                return Config.Templates[pointType][version];
            }
            return null;
        }

        public static List<TemplateInfo> GetAllTemplates(PointType? pointType = null)
        {
            var templates = new List<TemplateInfo>();
            
            foreach (var pointTypeKvp in Config.Templates)
            {
                if (pointType == null || pointTypeKvp.Key == pointType)
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
            
            return templates;
        }

        public static async Task<string> RenderTemplateAsync(PointType pointType, Dictionary<string, object> data, 
            TemplateVersion version = TemplateVersion.Default)
        {
            var template = await GetTemplateAsync(pointType, version);
            var cacheKey = $"{pointType}_{version}";
            
            return await TemplateCacheManager.RenderTemplateWithCacheAsync(template, data, cacheKey);
        }

        public static string RenderTemplate(PointType pointType, Dictionary<string, object> data, 
            TemplateVersion version = TemplateVersion.Default)
        {
            return RenderTemplateAsync(pointType, data, version).GetAwaiter().GetResult();
        }

        public static bool AddCustomTemplate(PointType pointType, string name, string content, 
            string description = "", string author = "User")
        {
            try
            {
                var customDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Templates", pointType.ToString(), "Custom");
                Directory.CreateDirectory(customDir);
                
                var fileName = $"{name.Replace(" ", "_")}.scriban";
                var filePath = Path.Combine(customDir, fileName);
                
                // 验证模板语法
                var template = Scriban.Template.Parse(content);
                if (template.HasErrors)
                {
                    return false;
                }
                
                File.WriteAllText(filePath, content);
                
                var templateInfo = new TemplateInfo
                {
                    Name = name,
                    FilePath = filePath,
                    Version = TemplateVersion.Custom,
                    PointType = pointType,
                    Description = description,
                    Author = author,
                    RequiredFields = ExtractRequiredFields(content),
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };
                
                if (!Config.Templates.ContainsKey(pointType))
                {
                    Config.Templates[pointType] = new Dictionary<TemplateVersion, TemplateInfo>();
                }
                
                // 通过配置管理器更新模板信息
                if (TemplateConfigManager.UpdateTemplateInfo(pointType, TemplateVersion.Custom, templateInfo))
                {
                    // 清除旧版本缓存
                    TemplateCacheManager.ClearAllCache();
                    
                    TemplateAdded?.Invoke(templateInfo);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool UpdateTemplate(PointType pointType, TemplateVersion version, string content)
        {
            try
            {
                var templateInfo = GetTemplateInfo(pointType, version);
                if (templateInfo == null) return false;
                
                // 验证模板语法
                var template = Scriban.Template.Parse(content);
                if (template.HasErrors) return false;
                
                // 备份原文件
                var backupPath = templateInfo.FilePath + $".backup_{DateTime.Now:yyyyMMddHHmmss}";
                File.Copy(templateInfo.FilePath, backupPath);
                
                File.WriteAllText(templateInfo.FilePath, content);
                templateInfo.ModifiedDate = DateTime.Now;
                templateInfo.RequiredFields = ExtractRequiredFields(content);
                
                // 通过配置管理器保存更新
                if (TemplateConfigManager.UpdateTemplateInfo(pointType, version, templateInfo))
                {
                    // 清除相关缓存
                    TemplateCacheManager.ClearAllCache();
                    
                    TemplateUpdated?.Invoke(templateInfo);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool RemoveTemplate(PointType pointType, TemplateVersion version)
        {
            if (version == TemplateVersion.Default) return false; // 不能删除默认模板
            
            try
            {
                var templateInfo = GetTemplateInfo(pointType, version);
                if (templateInfo == null) return false;
                
                File.Delete(templateInfo.FilePath);
                
                // 通过配置管理器移除模板信息
                if (TemplateConfigManager.RemoveTemplateInfo(pointType, version))
                {
                    // 清除相关缓存
                    TemplateCacheManager.ClearAllCache();
                    
                    TemplateRemoved?.Invoke(templateInfo);
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static List<string> ExtractRequiredFields(string templateContent)
        {
            var fields = new List<string>();
            var template = Scriban.Template.Parse(templateContent);
            
            // 简单的字段提取逻辑，可以根据需要扩展
            var lines = templateContent.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("{{") && line.Contains("}}"))
                {
                    var start = line.IndexOf("{{") + 2;
                    var end = line.IndexOf("}}", start);
                    if (end > start)
                    {
                        var field = line.Substring(start, end - start).Trim();
                        if (!field.StartsWith("#") && !field.StartsWith("/") && !fields.Contains(field))
                        {
                            fields.Add(field);
                        }
                    }
                }
            }
            
            return fields;
        }


        private static string GetTemplateDescription(PointType pointType, TemplateVersion version)
        {
            return $"{pointType}点位的{version}版本模板，用于生成标准的ST代码";
        }

        private static List<string> GetRequiredFields(PointType pointType)
        {
            return pointType switch
            {
                PointType.AI => new List<string> { "变量名称", "模块类型", "通道", "工程单位", "量程下限", "量程上限" },
                PointType.AO => new List<string> { "变量名称", "模块类型", "通道", "工程单位", "输出下限", "输出上限" },
                PointType.DI => new List<string> { "变量名称", "模块类型", "通道", "信号类型" },
                PointType.DO => new List<string> { "变量名称", "模块类型", "通道", "输出类型" },
                _ => new List<string>()
            };
        }

        /// <summary>
        /// 预热模板缓存
        /// </summary>
        private static async Task PrewarmTemplateCache()
        {
            try
            {
                var templatestoPrewarm = new List<(string path, string content)>();
                
                // 收集需要预热的模板
                foreach (PointType pointType in Enum.GetValues<PointType>())
                {
                    var defaultTemplate = GetTemplateInfo(pointType, TemplateVersion.Default);
                    var advancedTemplate = GetTemplateInfo(pointType, TemplateVersion.Advanced);
                    
                    if (defaultTemplate != null && File.Exists(defaultTemplate.FilePath))
                    {
                        var content = await File.ReadAllTextAsync(defaultTemplate.FilePath);
                        templatestoPrewarm.Add((defaultTemplate.FilePath, content));
                    }
                    
                    if (advancedTemplate != null && File.Exists(advancedTemplate.FilePath))
                    {
                        var content = await File.ReadAllTextAsync(advancedTemplate.FilePath);
                        templatestoPrewarm.Add((advancedTemplate.FilePath, content));
                    }
                }
                
                await TemplateCacheManager.PrewarmCacheAsync(templatestoPrewarm);
            }
            catch (Exception ex)
            {
                // 预热失败不影响主要功能
                System.Diagnostics.Debug.WriteLine($"模板预热失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static CacheStatistics GetCacheStatistics()
        {
            return TemplateCacheManager.GetStatistics();
        }

        /// <summary>
        /// 获取渲染性能历史
        /// </summary>
        public static List<RenderPerformance> GetPerformanceHistory(int maxRecords = 100)
        {
            return TemplateCacheManager.GetPerformanceHistory(maxRecords);
        }

        /// <summary>
        /// 清理模板缓存
        /// </summary>
        public static void ClearTemplateCache()
        {
            TemplateCacheManager.ClearAllCache();
        }
        
        /// <summary>
        /// 获取指定点类型的可用模板
        /// </summary>
        public static List<TemplateInfo> GetAvailableTemplates(string pointType)
        {
            if (Enum.TryParse<PointType>(pointType, true, out var pointTypeEnum))
            {
                return GetAllTemplates(pointTypeEnum);
            }
            return new List<TemplateInfo>();
        }

        /// <summary>
        /// 获取模板路径
        /// </summary>
        public static string GetTemplatePath(string pointType, string version = "default")
        {
            var templates = GetAvailableTemplates(pointType);
            var template = templates.FirstOrDefault(t => t.Version.ToString().ToLower() == version.ToLower()) 
                         ?? templates.FirstOrDefault();
            
            return template?.FilePath ?? "";
        }

        // 默认模板定义
        private static string GetDefaultAITemplate() => @"
// AI点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}
VAR
    {{变量名称}}_Raw : INT;     // 原始值
    {{变量名称}}_EU : REAL;     // 工程值
    {{变量名称}}_Status : WORD; // 状态字
END_VAR

// 模拟量输入处理
{{变量名称}}_Raw := AI_{{模块类型}}_{{通道}};
{{变量名称}}_EU := SCALE_AI({{变量名称}}_Raw, {{量程下限}}, {{量程上限}});
{{变量名称}}_Status := AI_{{模块类型}}_{{通道}}_Status;
";

        private static string GetAdvancedAITemplate() => @"
// 高级AI点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}
// 工程单位: {{工程单位}}, 量程: {{量程下限}} - {{量程上限}}
TYPE AI_{{变量名称}}_Type :
STRUCT
    Raw : INT;           // 原始值 (0-32767)
    EU : REAL;          // 工程值
    Status : WORD;      // 状态字
    Quality : BOOL;     // 数据质量
    Alarm_HH : BOOL;    // 高高报警
    Alarm_H : BOOL;     // 高报警
    Alarm_L : BOOL;     // 低报警
    Alarm_LL : BOOL;    // 低低报警
    Filter : REAL;      // 滤波值
END_STRUCT
END_TYPE

VAR
    {{变量名称}} : AI_{{变量名称}}_Type;
    {{变量名称}}_Config : STRUCT
        HH_Limit : REAL := {{量程上限}} * 0.95;
        H_Limit : REAL := {{量程上限}} * 0.9;
        L_Limit : REAL := {{量程下限}} * 1.1;
        LL_Limit : REAL := {{量程下限}} * 1.05;
        Filter_Time : TIME := T#1s;
    END_STRUCT;
END_VAR

// AI处理程序块
FUNCTION_BLOCK FB_AI_Process
VAR_INPUT
    Raw_Value : INT;
    Config : AI_Config_Type;
END_VAR
VAR_OUTPUT
    AI_Data : AI_{{变量名称}}_Type;
END_VAR
VAR
    Filter_FB : TON;
END_VAR

// 原始值转工程值
AI_Data.Raw := Raw_Value;
AI_Data.EU := SCALE_AI(Raw_Value, {{量程下限}}, {{量程上限}});

// 数据质量检查
AI_Data.Quality := (Raw_Value >= 0) AND (Raw_Value <= 32767);

// 报警处理
AI_Data.Alarm_HH := AI_Data.EU > Config.HH_Limit;
AI_Data.Alarm_H := AI_Data.EU > Config.H_Limit;
AI_Data.Alarm_L := AI_Data.EU < Config.L_Limit;
AI_Data.Alarm_LL := AI_Data.EU < Config.LL_Limit;

// 一阶滤波
Filter_FB(IN := TRUE, PT := Config.Filter_Time);
IF Filter_FB.Q THEN
    AI_Data.Filter := AI_Data.Filter * 0.9 + AI_Data.EU * 0.1;
    Filter_FB(IN := FALSE);
END_IF;
";

        private static string GetDefaultAOTemplate() => @"
// AO点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}
VAR
    {{变量名称}}_Set : REAL;    // 设定值
    {{变量名称}}_Out : INT;     // 输出值
    {{变量名称}}_Status : WORD; // 状态字
END_VAR

// 模拟量输出处理
{{变量名称}}_Out := SCALE_AO({{变量名称}}_Set, {{输出下限}}, {{输出上限}});
AO_{{模块类型}}_{{通道}} := {{变量名称}}_Out;
{{变量名称}}_Status := AO_{{模块类型}}_{{通道}}_Status;
";

        private static string GetAdvancedAOTemplate() => @"
// 高级AO点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}
// 输出范围: {{输出下限}} - {{输出上限}} {{工程单位}}
TYPE AO_{{变量名称}}_Type :
STRUCT
    Setpoint : REAL;     // 设定值
    Output : INT;        // 输出原始值
    Status : WORD;       // 状态字
    Enable : BOOL := TRUE; // 输出使能
    Manual : BOOL;       // 手动模式
    Manual_Value : REAL; // 手动设定值
    Limit_High : REAL := {{输出上限}};  // 上限
    Limit_Low : REAL := {{输出下限}};   // 下限
    Rate_Limit : REAL := 100.0; // 变化率限制 %/s
END_STRUCT
END_TYPE

VAR
    {{变量名称}} : AO_{{变量名称}}_Type;
    {{变量名称}}_FB : FB_AO_Control;
END_VAR

// AO控制功能块
{{变量名称}}_FB(
    Setpoint := {{变量名称}}.Setpoint,
    Manual := {{变量名称}}.Manual,
    Manual_Value := {{变量名称}}.Manual_Value,
    Enable := {{变量名称}}.Enable,
    Limit_High := {{变量名称}}.Limit_High,
    Limit_Low := {{变量名称}}.Limit_Low,
    Rate_Limit := {{变量名称}}.Rate_Limit,
    Output => {{变量名称}}.Output,
    Status => {{变量名称}}.Status
);

// 输出到硬件
AO_{{模块类型}}_{{通道}} := {{变量名称}}.Output;
";

        private static string GetDefaultDITemplate() => @"
// DI点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}
VAR
    {{变量名称}} : BOOL;        // 数字输入状态
    {{变量名称}}_Status : WORD; // 状态字
END_VAR

// 数字量输入处理
{{变量名称}} := DI_{{模块类型}}_{{通道}};
{{变量名称}}_Status := DI_{{模块类型}}_{{通道}}_Status;
";

        private static string GetAdvancedDITemplate() => @"
// 高级DI点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}, 信号类型: {{信号类型}}
TYPE DI_{{变量名称}}_Type :
STRUCT
    Raw : BOOL;          // 原始输入
    Filtered : BOOL;     // 滤波后状态
    Status : WORD;       // 状态字
    Rising_Edge : BOOL;  // 上升沿
    Falling_Edge : BOOL; // 下降沿
    Debounce_Time : TIME := T#100ms; // 消抖时间
    Invert : BOOL;       // 信号取反
    Quality : BOOL := TRUE; // 信号质量
END_STRUCT
END_TYPE

VAR
    {{变量名称}} : DI_{{变量名称}}_Type;
    {{变量名称}}_Debounce : TON;
    {{变量名称}}_Edge : R_TRIG;
    {{变量名称}}_Fall_Edge : F_TRIG;
END_VAR

// DI处理
{{变量名称}}.Raw := DI_{{模块类型}}_{{通道}};
{{变量名称}}.Status := DI_{{模块类型}}_{{通道}}_Status;

// 信号取反处理
IF {{变量名称}}.Invert THEN
    {{变量名称}}.Raw := NOT {{变量名称}}.Raw;
END_IF;

// 消抖处理
{{变量名称}}_Debounce(IN := {{变量名称}}.Raw, PT := {{变量名称}}.Debounce_Time);
{{变量名称}}.Filtered := {{变量名称}}_Debounce.Q;

// 边沿检测
{{变量名称}}_Edge(CLK := {{变量名称}}.Filtered);
{{变量名称}}.Rising_Edge := {{变量名称}}_Edge.Q;

{{变量名称}}_Fall_Edge(CLK := {{变量名称}}.Filtered);
{{变量名称}}.Falling_Edge := {{变量名称}}_Fall_Edge.Q;
";

        private static string GetDefaultDOTemplate() => @"
// DO点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}
VAR
    {{变量名称}}_Cmd : BOOL;    // 输出命令
    {{变量名称}}_FB : BOOL;     // 反馈状态
    {{变量名称}}_Status : WORD; // 状态字
END_VAR

// 数字量输出处理
DO_{{模块类型}}_{{通道}} := {{变量名称}}_Cmd;
{{变量名称}}_FB := DO_{{模块类型}}_{{通道}}_FB;
{{变量名称}}_Status := DO_{{模块类型}}_{{通道}}_Status;
";

        private static string GetAdvancedDOTemplate() => @"
// 高级DO点位: {{变量名称}}
// 模块: {{模块类型}}, 通道: {{通道}}, 输出类型: {{输出类型}}
TYPE DO_{{变量名称}}_Type :
STRUCT
    Command : BOOL;      // 输出命令
    Feedback : BOOL;     // 反馈状态
    Status : WORD;       // 状态字
    Enable : BOOL := TRUE; // 输出使能
    Pulse_Mode : BOOL;   // 脉冲模式
    Pulse_Time : TIME := T#1s; // 脉冲时间
    Interlock : BOOL;    // 联锁信号
    Manual : BOOL;       // 手动模式
    Force_Off : BOOL;    // 强制关闭
    Alarm : BOOL;        // 故障报警
END_STRUCT
END_TYPE

VAR
    {{变量名称}} : DO_{{变量名称}}_Type;
    {{变量名称}}_Pulse : TON;
    {{变量名称}}_Output : BOOL;
END_VAR

// DO控制逻辑
// 联锁检查
{{变量名称}}.Alarm := {{变量名称}}.Command AND NOT {{变量名称}}.Interlock;

// 脉冲模式处理
IF {{变量名称}}.Pulse_Mode THEN
    {{变量名称}}_Pulse(IN := {{变量名称}}.Command, PT := {{变量名称}}.Pulse_Time);
    {{变量名称}}_Output := {{变量名称}}_Pulse.Q;
ELSE
    {{变量名称}}_Output := {{变量名称}}.Command;
END_IF;

// 最终输出处理
{{变量名称}}_Output := {{变量名称}}_Output 
    AND {{变量名称}}.Enable 
    AND {{变量名称}}.Interlock 
    AND NOT {{变量名称}}.Force_Off;

// 输出到硬件
DO_{{模块类型}}_{{通道}} := {{变量名称}}_Output;
{{变量名称}}.Feedback := DO_{{模块类型}}_{{通道}}_FB;
{{变量名称}}.Status := DO_{{模块类型}}_{{通道}}_Status;
";

        #region 性能监控相关

        private static readonly List<RenderPerformance> _performanceHistory = new();
        private static readonly object _performanceLock = new();


        /// <summary>
        /// 记录渲染性能
        /// </summary>
        private static void RecordPerformance(string templateKey, TimeSpan renderTime, int dataSize, int variableCount, bool cacheHit)
        {
            lock (_performanceLock)
            {
                var performance = new RenderPerformance
                {
                    Timestamp = DateTime.Now,
                    TemplateKey = templateKey,
                    RenderTime = renderTime,
                    DataSize = dataSize,
                    VariableCount = variableCount,
                    CacheHit = cacheHit
                };

                _performanceHistory.Insert(0, performance);

                // 限制历史记录数量
                if (_performanceHistory.Count > 1000)
                {
                    _performanceHistory.RemoveRange(1000, _performanceHistory.Count - 1000);
                }
            }
        }

        #endregion
    }

}