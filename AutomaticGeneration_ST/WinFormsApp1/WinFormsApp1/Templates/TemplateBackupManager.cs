using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WinFormsApp1.Templates
{
    /// <summary>
    /// 模板备份和恢复管理器 - 防止模板丢失，支持版本历史
    /// </summary>
    public static class TemplateBackupManager
    {
        #region 数据结构定义

        /// <summary>
        /// 备份信息
        /// </summary>
        public class BackupInfo
        {
            /// <summary>
            /// 备份ID
            /// </summary>
            public string Id { get; set; } = Guid.NewGuid().ToString();

            /// <summary>
            /// 备份名称
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 备份描述
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// 备份类型
            /// </summary>
            public BackupType Type { get; set; } = BackupType.Manual;

            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime CreatedDate { get; set; } = DateTime.Now;

            /// <summary>
            /// 备份文件路径
            /// </summary>
            public string FilePath { get; set; } = string.Empty;

            /// <summary>
            /// 文件大小（字节）
            /// </summary>
            public long FileSize { get; set; } = 0;

            /// <summary>
            /// 包含的模板数量
            /// </summary>
            public int TemplateCount { get; set; } = 0;

            /// <summary>
            /// 包含的收藏数量
            /// </summary>
            public int FavoriteCount { get; set; } = 0;

            /// <summary>
            /// 包含的分类数量
            /// </summary>
            public int CategoryCount { get; set; } = 0;

            /// <summary>
            /// 备份完整性校验和
            /// </summary>
            public string Checksum { get; set; } = string.Empty;

            /// <summary>
            /// 应用程序版本
            /// </summary>
            public string AppVersion { get; set; } = "2.0";

            /// <summary>
            /// 备份版本
            /// </summary>
            public string BackupVersion { get; set; } = "1.0";

            /// <summary>
            /// 是否被保护（不允许自动删除）
            /// </summary>
            public bool IsProtected { get; set; } = false;

            /// <summary>
            /// 标签列表
            /// </summary>
            public List<string> Tags { get; set; } = new List<string>();

            /// <summary>
            /// 元数据
            /// </summary>
            public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        }

        /// <summary>
        /// 备份内容
        /// </summary>
        public class BackupContent
        {
            /// <summary>
            /// 备份信息
            /// </summary>
            public BackupInfo Info { get; set; } = new BackupInfo();

            /// <summary>
            /// 模板库配置
            /// </summary>
            public TemplateLibraryManager.TemplateLibraryConfig? LibraryConfig { get; set; }

            /// <summary>
            /// 模板管理配置
            /// </summary>
            public TemplateConfig? TemplateConfig { get; set; }

            /// <summary>
            /// 模板文件内容映射
            /// </summary>
            public Dictionary<string, string> TemplateFiles { get; set; } = new Dictionary<string, string>();

            /// <summary>
            /// 应用程序配置
            /// </summary>
            public Dictionary<string, object>? AppConfig { get; set; }

            /// <summary>
            /// 创建时间戳
            /// </summary>
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// 恢复选项
        /// </summary>
        public class RestoreOptions
        {
            /// <summary>
            /// 是否恢复模板库
            /// </summary>
            public bool RestoreLibrary { get; set; } = true;

            /// <summary>
            /// 是否恢复模板文件
            /// </summary>
            public bool RestoreTemplates { get; set; } = true;

            /// <summary>
            /// 是否恢复配置
            /// </summary>
            public bool RestoreConfig { get; set; } = true;

            /// <summary>
            /// 恢复模式
            /// </summary>
            public RestoreMode Mode { get; set; } = RestoreMode.Merge;

            /// <summary>
            /// 是否创建恢复前备份
            /// </summary>
            public bool CreatePreRestoreBackup { get; set; } = true;

            /// <summary>
            /// 要恢复的特定项目ID列表（空表示全部）
            /// </summary>
            public List<string> SpecificItems { get; set; } = new List<string>();
        }

        /// <summary>
        /// 备份历史记录
        /// </summary>
        public class BackupHistory
        {
            /// <summary>
            /// 配置版本
            /// </summary>
            public string Version { get; set; } = "1.0";

            /// <summary>
            /// 备份信息列表
            /// </summary>
            public List<BackupInfo> Backups { get; set; } = new List<BackupInfo>();

            /// <summary>
            /// 最后更新时间
            /// </summary>
            public DateTime LastUpdated { get; set; } = DateTime.Now;

            /// <summary>
            /// 备份配置
            /// </summary>
            public BackupSettings Settings { get; set; } = new BackupSettings();
        }

        /// <summary>
        /// 备份配置
        /// </summary>
        public class BackupSettings
        {
            /// <summary>
            /// 自动备份间隔（小时）
            /// </summary>
            public int AutoBackupInterval { get; set; } = 24;

            /// <summary>
            /// 最大备份数量
            /// </summary>
            public int MaxBackupCount { get; set; } = 30;

            /// <summary>
            /// 是否启用自动备份
            /// </summary>
            public bool EnableAutoBackup { get; set; } = true;

            /// <summary>
            /// 是否压缩备份文件
            /// </summary>
            public bool CompressBackups { get; set; } = true;

            /// <summary>
            /// 备份保留天数
            /// </summary>
            public int RetentionDays { get; set; } = 90;

            /// <summary>
            /// 备份存储目录
            /// </summary>
            public string BackupDirectory { get; set; } = "Backups";

            /// <summary>
            /// 是否验证备份完整性
            /// </summary>
            public bool VerifyBackupIntegrity { get; set; } = true;

            /// <summary>
            /// 是否在启动时检查备份
            /// </summary>
            public bool CheckBackupOnStartup { get; set; } = true;
        }

        /// <summary>
        /// 备份类型
        /// </summary>
        public enum BackupType
        {
            /// <summary>
            /// 手动备份
            /// </summary>
            Manual,

            /// <summary>
            /// 自动备份
            /// </summary>
            Automatic,

            /// <summary>
            /// 系统备份
            /// </summary>
            System,

            /// <summary>
            /// 导出备份
            /// </summary>
            Export,

            /// <summary>
            /// 快照备份
            /// </summary>
            Snapshot
        }

        /// <summary>
        /// 恢复模式
        /// </summary>
        public enum RestoreMode
        {
            /// <summary>
            /// 完全替换
            /// </summary>
            Replace,

            /// <summary>
            /// 合并模式
            /// </summary>
            Merge,

            /// <summary>
            /// 仅添加新项
            /// </summary>
            AddOnly
        }

        /// <summary>
        /// 恢复结果
        /// </summary>
        public class RestoreResult
        {
            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Success { get; set; } = false;

            /// <summary>
            /// 错误信息
            /// </summary>
            public string ErrorMessage { get; set; } = string.Empty;

            /// <summary>
            /// 恢复的项目数量
            /// </summary>
            public int RestoredItemCount { get; set; } = 0;

            /// <summary>
            /// 跳过的项目数量
            /// </summary>
            public int SkippedItemCount { get; set; } = 0;

            /// <summary>
            /// 失败的项目数量
            /// </summary>
            public int FailedItemCount { get; set; } = 0;

            /// <summary>
            /// 详细信息
            /// </summary>
            public List<string> Details { get; set; } = new List<string>();

            /// <summary>
            /// 恢复耗时
            /// </summary>
            public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        }

        #endregion

        #region 私有字段

        private static BackupHistory? _backupHistory;
        private static readonly string _backupHistoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup_history.json");
        private static readonly string _defaultBackupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
        private static System.Timers.Timer? _autoBackupTimer;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取备份历史
        /// </summary>
        public static BackupHistory History
        {
            get
            {
                if (_backupHistory == null)
                {
                    LoadBackupHistory();
                }
                return _backupHistory ?? new BackupHistory();
            }
        }

        /// <summary>
        /// 获取备份目录
        /// </summary>
        public static string BackupDirectory
        {
            get
            {
                var directory = Path.Combine(_defaultBackupDirectory, History.Settings.BackupDirectory);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                return directory;
            }
        }

        #endregion

        #region 初始化和配置

        /// <summary>
        /// 初始化备份管理器
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadBackupHistory();
                SetupAutoBackup();
                
                if (History.Settings.CheckBackupOnStartup)
                {
                    Task.Run(PerformStartupCheck);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"初始化备份管理器失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载备份历史
        /// </summary>
        private static void LoadBackupHistory()
        {
            try
            {
                if (File.Exists(_backupHistoryPath))
                {
                    var jsonContent = File.ReadAllText(_backupHistoryPath);
                    _backupHistory = JsonSerializer.Deserialize<BackupHistory>(jsonContent, GetJsonOptions());
                }

                if (_backupHistory == null)
                {
                    _backupHistory = new BackupHistory();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载备份历史失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存备份历史
        /// </summary>
        private static void SaveBackupHistory()
        {
            try
            {
                if (_backupHistory != null)
                {
                    _backupHistory.LastUpdated = DateTime.Now;
                    
                    var directory = Path.GetDirectoryName(_backupHistoryPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var jsonContent = JsonSerializer.Serialize(_backupHistory, GetJsonOptions());
                    File.WriteAllText(_backupHistoryPath, jsonContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"保存备份历史失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置自动备份
        /// </summary>
        private static void SetupAutoBackup()
        {
            if (_autoBackupTimer != null)
            {
                _autoBackupTimer.Stop();
                _autoBackupTimer.Dispose();
            }

            if (History.Settings.EnableAutoBackup)
            {
                _autoBackupTimer = new System.Timers.Timer(TimeSpan.FromHours(History.Settings.AutoBackupInterval).TotalMilliseconds);
                _autoBackupTimer.Elapsed += async (sender, e) => await PerformAutoBackup();
                _autoBackupTimer.AutoReset = true;
                _autoBackupTimer.Start();
            }
        }

        /// <summary>
        /// 获取JSON序列化选项
        /// </summary>
        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        #endregion

        #region 备份操作

        /// <summary>
        /// 创建手动备份
        /// </summary>
        public static async Task<string> CreateBackupAsync(string name, string description = "", List<string>? tags = null)
        {
            return await CreateBackupInternalAsync(name, description, BackupType.Manual, tags);
        }

        /// <summary>
        /// 创建系统备份
        /// </summary>
        public static async Task<string> CreateSystemBackupAsync(string description = "")
        {
            var name = $"System_{DateTime.Now:yyyyMMdd_HHmmss}";
            return await CreateBackupInternalAsync(name, description, BackupType.System);
        }

        /// <summary>
        /// 执行自动备份
        /// </summary>
        private static async Task PerformAutoBackup()
        {
            try
            {
                var name = $"Auto_{DateTime.Now:yyyyMMdd_HHmmss}";
                var description = "自动备份";
                await CreateBackupInternalAsync(name, description, BackupType.Automatic);
                
                // 清理过期的自动备份
                await CleanupOldBackupsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"自动备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 内部备份创建方法
        /// </summary>
        private static async Task<string> CreateBackupInternalAsync(string name, string description, BackupType type, List<string>? tags = null)
        {
            try
            {
                var backupInfo = new BackupInfo
                {
                    Name = name,
                    Description = description,
                    Type = type,
                    Tags = tags ?? new List<string>()
                };

                // 创建备份内容
                var backupContent = await CreateBackupContentAsync(backupInfo);

                // 生成备份文件路径
                var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.backup";
                var filePath = Path.Combine(BackupDirectory, fileName);
                backupInfo.FilePath = filePath;

                // 保存备份文件
                await SaveBackupFileAsync(backupContent, filePath);

                // 更新备份信息
                var fileInfo = new FileInfo(filePath);
                backupInfo.FileSize = fileInfo.Length;
                backupInfo.Checksum = await CalculateChecksumAsync(filePath);

                // 添加到历史记录
                History.Backups.Add(backupInfo);
                SaveBackupHistory();

                return backupInfo.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"创建备份失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建备份内容
        /// </summary>
        private static async Task<BackupContent> CreateBackupContentAsync(BackupInfo backupInfo)
        {
            var content = new BackupContent { Info = backupInfo };

            try
            {
                // 备份模板库配置
                content.LibraryConfig = TemplateLibraryManager.LibraryConfig;
                backupInfo.FavoriteCount = content.LibraryConfig.Favorites.Count;
                backupInfo.CategoryCount = content.LibraryConfig.Categories.Count;

                // 备份模板管理配置
                content.TemplateConfig = await LoadTemplateConfigAsync();

                // 备份模板文件内容
                content.TemplateFiles = await LoadTemplateFilesAsync();
                backupInfo.TemplateCount = content.TemplateFiles.Count;

                // 备份应用程序配置
                content.AppConfig = await LoadAppConfigAsync();

                return content;
            }
            catch (Exception ex)
            {
                throw new Exception($"创建备份内容失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载模板配置
        /// </summary>
        private static async Task<TemplateConfig?> LoadTemplateConfigAsync()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "template_config.json");
                if (File.Exists(configPath))
                {
                    var content = await File.ReadAllTextAsync(configPath);
                    return JsonSerializer.Deserialize<TemplateConfig>(content, GetJsonOptions());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载模板配置失败: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// 加载模板文件内容
        /// </summary>
        private static async Task<Dictionary<string, string>> LoadTemplateFilesAsync()
        {
            var templateFiles = new Dictionary<string, string>();

            try
            {
                var templatesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                if (Directory.Exists(templatesDirectory))
                {
                    var templateFilePaths = Directory.GetFiles(templatesDirectory, "*.template", SearchOption.AllDirectories);
                    
                    foreach (var filePath in templateFilePaths)
                    {
                        try
                        {
                            var relativePath = Path.GetRelativePath(templatesDirectory, filePath);
                            var content = await File.ReadAllTextAsync(filePath);
                            templateFiles[relativePath] = content;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"读取模板文件失败 {filePath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载模板文件失败: {ex.Message}");
            }

            return templateFiles;
        }

        /// <summary>
        /// 加载应用程序配置
        /// </summary>
        private static async Task<Dictionary<string, object>?> LoadAppConfigAsync()
        {
            try
            {
                var config = new Dictionary<string, object>();
                
                // 这里可以添加应用程序特定的配置加载逻辑
                config["Version"] = "2.0";
                config["BackupCreated"] = DateTime.Now;
                
                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载应用配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存备份文件
        /// </summary>
        private static async Task SaveBackupFileAsync(BackupContent content, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonSerializer.Serialize(content, GetJsonOptions());

                if (History.Settings.CompressBackups)
                {
                    // 使用压缩保存
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
                    using var writer = new StreamWriter(gzipStream);
                    await writer.WriteAsync(jsonContent);
                }
                else
                {
                    // 直接保存
                    await File.WriteAllTextAsync(filePath, jsonContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"保存备份文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 计算文件校验和
        /// </summary>
        private static async Task<string> CalculateChecksumAsync(string filePath)
        {
            try
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = await md5.ComputeHashAsync(stream);
                return Convert.ToHexString(hashBytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"计算校验和失败: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region 恢复操作

        /// <summary>
        /// 恢复备份
        /// </summary>
        public static async Task<RestoreResult> RestoreBackupAsync(string backupId, RestoreOptions? options = null)
        {
            var result = new RestoreResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                options ??= new RestoreOptions();
                
                var backup = History.Backups.FirstOrDefault(b => b.Id == backupId);
                if (backup == null)
                {
                    result.ErrorMessage = "找不到指定的备份";
                    return result;
                }

                if (!File.Exists(backup.FilePath))
                {
                    result.ErrorMessage = "备份文件不存在";
                    return result;
                }

                // 验证备份完整性
                if (History.Settings.VerifyBackupIntegrity && !string.IsNullOrEmpty(backup.Checksum))
                {
                    var currentChecksum = await CalculateChecksumAsync(backup.FilePath);
                    if (currentChecksum != backup.Checksum)
                    {
                        result.ErrorMessage = "备份文件已损坏";
                        return result;
                    }
                }

                // 创建恢复前备份
                if (options.CreatePreRestoreBackup)
                {
                    try
                    {
                        await CreateSystemBackupAsync("恢复前自动备份");
                        result.Details.Add("已创建恢复前备份");
                    }
                    catch (Exception ex)
                    {
                        result.Details.Add($"创建恢复前备份失败: {ex.Message}");
                    }
                }

                // 加载备份内容
                var backupContent = await LoadBackupFileAsync(backup.FilePath);
                if (backupContent == null)
                {
                    result.ErrorMessage = "无法读取备份内容";
                    return result;
                }

                // 执行恢复操作
                await PerformRestoreAsync(backupContent, options, result);

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"恢复失败: {ex.Message}";
                result.Details.Add($"异常信息: {ex}");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }

            return result;
        }

        /// <summary>
        /// 加载备份文件
        /// </summary>
        private static async Task<BackupContent?> LoadBackupFileAsync(string filePath)
        {
            try
            {
                string jsonContent;

                if (History.Settings.CompressBackups)
                {
                    // 解压缩读取
                    using var fileStream = new FileStream(filePath, FileMode.Open);
                    using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                    using var reader = new StreamReader(gzipStream);
                    jsonContent = await reader.ReadToEndAsync();
                }
                else
                {
                    // 直接读取
                    jsonContent = await File.ReadAllTextAsync(filePath);
                }

                return JsonSerializer.Deserialize<BackupContent>(jsonContent, GetJsonOptions());
            }
            catch (Exception ex)
            {
                throw new Exception($"加载备份文件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行恢复操作
        /// </summary>
        private static async Task PerformRestoreAsync(BackupContent backupContent, RestoreOptions options, RestoreResult result)
        {
            try
            {
                // 恢复模板库
                if (options.RestoreLibrary && backupContent.LibraryConfig != null)
                {
                    await RestoreLibraryConfigAsync(backupContent.LibraryConfig, options, result);
                }

                // 恢复模板文件
                if (options.RestoreTemplates && backupContent.TemplateFiles.Any())
                {
                    await RestoreTemplateFilesAsync(backupContent.TemplateFiles, options, result);
                }

                // 恢复配置
                if (options.RestoreConfig && backupContent.TemplateConfig != null)
                {
                    await RestoreTemplateConfigAsync(backupContent.TemplateConfig, options, result);
                }

                result.Details.Add($"恢复完成: 成功 {result.RestoredItemCount}, 跳过 {result.SkippedItemCount}, 失败 {result.FailedItemCount}");
            }
            catch (Exception ex)
            {
                throw new Exception($"执行恢复操作失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 恢复模板库配置
        /// </summary>
        private static async Task RestoreLibraryConfigAsync(TemplateLibraryManager.TemplateLibraryConfig libraryConfig, RestoreOptions options, RestoreResult result)
        {
            try
            {
                var currentConfig = TemplateLibraryManager.LibraryConfig;

                switch (options.Mode)
                {
                    case RestoreMode.Replace:
                        // 完全替换
                        TemplateLibraryManager.LibraryConfig.Favorites.Clear();
                        TemplateLibraryManager.LibraryConfig.Favorites.AddRange(libraryConfig.Favorites);
                        
                        TemplateLibraryManager.LibraryConfig.Categories.Clear();
                        TemplateLibraryManager.LibraryConfig.Categories.AddRange(libraryConfig.Categories);
                        
                        TemplateLibraryManager.LibraryConfig.TagsUsageCount.Clear();
                        foreach (var tag in libraryConfig.TagsUsageCount)
                        {
                            TemplateLibraryManager.LibraryConfig.TagsUsageCount[tag.Key] = tag.Value;
                        }
                        
                        result.RestoredItemCount += libraryConfig.Favorites.Count + libraryConfig.Categories.Count;
                        break;

                    case RestoreMode.Merge:
                        // 合并模式
                        foreach (var favorite in libraryConfig.Favorites)
                        {
                            if (!currentConfig.Favorites.Any(f => f.Id == favorite.Id))
                            {
                                currentConfig.Favorites.Add(favorite);
                                result.RestoredItemCount++;
                            }
                            else
                            {
                                result.SkippedItemCount++;
                            }
                        }

                        foreach (var category in libraryConfig.Categories)
                        {
                            if (!currentConfig.Categories.Any(c => c.Id == category.Id))
                            {
                                currentConfig.Categories.Add(category);
                                result.RestoredItemCount++;
                            }
                            else
                            {
                                result.SkippedItemCount++;
                            }
                        }
                        break;

                    case RestoreMode.AddOnly:
                        // 仅添加新项
                        foreach (var favorite in libraryConfig.Favorites)
                        {
                            if (!currentConfig.Favorites.Any(f => f.Template.Name == favorite.Template.Name))
                            {
                                favorite.Id = Guid.NewGuid().ToString(); // 生成新ID
                                currentConfig.Favorites.Add(favorite);
                                result.RestoredItemCount++;
                            }
                            else
                            {
                                result.SkippedItemCount++;
                            }
                        }
                        break;
                }

                TemplateLibraryManager.SaveLibraryConfig();
                result.Details.Add("模板库配置已恢复");
            }
            catch (Exception ex)
            {
                result.FailedItemCount++;
                result.Details.Add($"恢复模板库配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复模板文件
        /// </summary>
        private static async Task RestoreTemplateFilesAsync(Dictionary<string, string> templateFiles, RestoreOptions options, RestoreResult result)
        {
            var templatesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            
            foreach (var (relativePath, content) in templateFiles)
            {
                try
                {
                    var fullPath = Path.Combine(templatesDirectory, relativePath);
                    var directory = Path.GetDirectoryName(fullPath);
                    
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (options.Mode == RestoreMode.Replace || !File.Exists(fullPath))
                    {
                        await File.WriteAllTextAsync(fullPath, content);
                        result.RestoredItemCount++;
                    }
                    else
                    {
                        result.SkippedItemCount++;
                    }
                }
                catch (Exception ex)
                {
                    result.FailedItemCount++;
                    result.Details.Add($"恢复模板文件 {relativePath} 失败: {ex.Message}");
                }
            }

            result.Details.Add($"已恢复 {result.RestoredItemCount} 个模板文件");
        }

        /// <summary>
        /// 恢复模板配置
        /// </summary>
        private static async Task RestoreTemplateConfigAsync(TemplateConfig templateConfig, RestoreOptions options, RestoreResult result)
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "template_config.json");
                
                if (options.Mode == RestoreMode.Replace || !File.Exists(configPath))
                {
                    var directory = Path.GetDirectoryName(configPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var jsonContent = JsonSerializer.Serialize(templateConfig, GetJsonOptions());
                    await File.WriteAllTextAsync(configPath, jsonContent);
                    
                    result.RestoredItemCount++;
                    result.Details.Add("模板配置已恢复");
                }
                else
                {
                    result.SkippedItemCount++;
                    result.Details.Add("模板配置已存在，跳过恢复");
                }
            }
            catch (Exception ex)
            {
                result.FailedItemCount++;
                result.Details.Add($"恢复模板配置失败: {ex.Message}");
            }
        }

        #endregion

        #region 备份管理

        /// <summary>
        /// 获取所有备份
        /// </summary>
        public static List<BackupInfo> GetAllBackups(BackupType? type = null)
        {
            var backups = History.Backups.OrderByDescending(b => b.CreatedDate).ToList();
            
            if (type.HasValue)
            {
                backups = backups.Where(b => b.Type == type.Value).ToList();
            }
            
            return backups;
        }

        /// <summary>
        /// 删除备份
        /// </summary>
        public static async Task<bool> DeleteBackupAsync(string backupId)
        {
            try
            {
                var backup = History.Backups.FirstOrDefault(b => b.Id == backupId);
                if (backup == null)
                    return false;

                if (backup.IsProtected)
                {
                    throw new InvalidOperationException("无法删除受保护的备份");
                }

                // 删除备份文件
                if (File.Exists(backup.FilePath))
                {
                    File.Delete(backup.FilePath);
                }

                // 从历史记录中移除
                History.Backups.Remove(backup);
                SaveBackupHistory();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"删除备份失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保护/取消保护备份
        /// </summary>
        public static bool ProtectBackup(string backupId, bool protect = true)
        {
            try
            {
                var backup = History.Backups.FirstOrDefault(b => b.Id == backupId);
                if (backup != null)
                {
                    backup.IsProtected = protect;
                    SaveBackupHistory();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"保护备份失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 清理过期备份
        /// </summary>
        public static async Task<int> CleanupOldBackupsAsync()
        {
            try
            {
                var cleanupCount = 0;
                var cutoffDate = DateTime.Now.AddDays(-History.Settings.RetentionDays);
                
                var oldBackups = History.Backups
                    .Where(b => !b.IsProtected && b.CreatedDate < cutoffDate && b.Type == BackupType.Automatic)
                    .OrderBy(b => b.CreatedDate)
                    .ToList();

                // 保留最近的几个自动备份
                var autoBackups = History.Backups.Where(b => b.Type == BackupType.Automatic).OrderByDescending(b => b.CreatedDate).ToList();
                var backupsToKeep = autoBackups.Take(Math.Max(5, History.Settings.MaxBackupCount / 2)).Select(b => b.Id).ToHashSet();

                foreach (var backup in oldBackups)
                {
                    if (!backupsToKeep.Contains(backup.Id))
                    {
                        await DeleteBackupAsync(backup.Id);
                        cleanupCount++;
                    }
                }

                return cleanupCount;
            }
            catch (Exception ex)
            {
                throw new Exception($"清理过期备份失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 验证备份完整性
        /// </summary>
        public static async Task<Dictionary<string, bool>> VerifyBackupIntegrityAsync()
        {
            var results = new Dictionary<string, bool>();

            foreach (var backup in History.Backups)
            {
                try
                {
                    if (!File.Exists(backup.FilePath))
                    {
                        results[backup.Id] = false;
                        continue;
                    }

                    if (!string.IsNullOrEmpty(backup.Checksum))
                    {
                        var currentChecksum = await CalculateChecksumAsync(backup.FilePath);
                        results[backup.Id] = currentChecksum == backup.Checksum;
                    }
                    else
                    {
                        // 尝试读取文件内容来验证
                        var content = await LoadBackupFileAsync(backup.FilePath);
                        results[backup.Id] = content != null;
                    }
                }
                catch
                {
                    results[backup.Id] = false;
                }
            }

            return results;
        }

        /// <summary>
        /// 更新备份设置
        /// </summary>
        public static void UpdateSettings(BackupSettings settings)
        {
            try
            {
                History.Settings = settings;
                SaveBackupHistory();
                SetupAutoBackup();
            }
            catch (Exception ex)
            {
                throw new Exception($"更新备份设置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行启动检查
        /// </summary>
        private static async Task PerformStartupCheck()
        {
            try
            {
                // 检查是否需要自动备份
                var lastAutoBackup = History.Backups
                    .Where(b => b.Type == BackupType.Automatic)
                    .OrderByDescending(b => b.CreatedDate)
                    .FirstOrDefault();

                if (lastAutoBackup == null || 
                    DateTime.Now - lastAutoBackup.CreatedDate > TimeSpan.FromHours(History.Settings.AutoBackupInterval))
                {
                    await PerformAutoBackup();
                }

                // 清理过期备份
                await CleanupOldBackupsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动检查失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取备份统计信息
        /// </summary>
        public static Dictionary<string, object> GetBackupStatistics()
        {
            var stats = new Dictionary<string, object>();
            
            stats["TotalBackups"] = History.Backups.Count;
            stats["ManualBackups"] = History.Backups.Count(b => b.Type == BackupType.Manual);
            stats["AutoBackups"] = History.Backups.Count(b => b.Type == BackupType.Automatic);
            stats["SystemBackups"] = History.Backups.Count(b => b.Type == BackupType.System);
            stats["ProtectedBackups"] = History.Backups.Count(b => b.IsProtected);
            
            var totalSize = History.Backups.Sum(b => b.FileSize);
            stats["TotalSize"] = FormatFileSize(totalSize);
            stats["TotalSizeBytes"] = totalSize;
            
            var oldestBackup = History.Backups.OrderBy(b => b.CreatedDate).FirstOrDefault();
            var newestBackup = History.Backups.OrderByDescending(b => b.CreatedDate).FirstOrDefault();
            
            stats["OldestBackup"] = oldestBackup?.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss") ?? "无";
            stats["NewestBackup"] = newestBackup?.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss") ?? "无";
            
            return stats;
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            else
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        #endregion
    }
}