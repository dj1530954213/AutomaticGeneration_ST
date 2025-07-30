using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 持久化存储类型
    /// </summary>
    public enum PersistenceStorageType
    {
        LocalFile,
        Registry,
        Database,
        Cloud
    }

    /// <summary>
    /// 配置备份信息
    /// </summary>
    public class ConfigurationBackup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public long FileSize { get; set; }
        public string Checksum { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsAutoBackup { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 持久化选项
    /// </summary>
    public class PersistenceOptions
    {
        public bool EnableCompression { get; set; } = false;
        public bool EnableEncryption { get; set; } = false;
        public bool EnableChecksumValidation { get; set; } = true;
        public bool EnableAutoBackup { get; set; } = true;
        public int MaxBackupCount { get; set; } = 10;
        public TimeSpan BackupInterval { get; set; } = TimeSpan.FromDays(1);
        public string EncryptionKey { get; set; } = "";
        public PersistenceStorageType StorageType { get; set; } = PersistenceStorageType.LocalFile;
    }

    /// <summary>
    /// 持久化结果
    /// </summary>
    public class PersistenceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string FilePath { get; set; } = "";
        public Exception? Exception { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public long FileSize { get; set; }
        public string Checksum { get; set; } = "";
    }

    /// <summary>
    /// 配置持久化服务
    /// </summary>
    public static class ConfigurationPersistence
    {
        private static PersistenceOptions _options = new();
        private static readonly List<ConfigurationBackup> _backups = new();
        private static System.Threading.Timer? _autoBackupTimer;

        public static event EventHandler<ConfigurationBackup>? BackupCreated;
        public static event EventHandler<ConfigurationBackup>? BackupRestored;
        public static event EventHandler<string>? BackupDeleted;

        /// <summary>
        /// 初始化持久化服务
        /// </summary>
        public static void Initialize(PersistenceOptions options)
        {
            _options = options;
            LoadBackupHistory();
            
            if (_options.EnableAutoBackup)
            {
                SetupAutoBackup();
            }
        }

        /// <summary>
        /// 保存配置（高级版本）
        /// </summary>
        public static async Task<PersistenceResult> SaveConfigurationAsync(
            ApplicationConfiguration config, 
            string filePath, 
            PersistenceOptions? options = null)
        {
            var opts = options ?? _options;
            var startTime = DateTime.Now;
            var result = new PersistenceResult { FilePath = filePath };

            try
            {
                // 序列化配置
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(config, jsonOptions);
                var data = Encoding.UTF8.GetBytes(json);

                // 压缩
                if (opts.EnableCompression)
                {
                    data = await CompressDataAsync(data);
                }

                // 加密
                if (opts.EnableEncryption && !string.IsNullOrEmpty(opts.EncryptionKey))
                {
                    data = EncryptData(data, opts.EncryptionKey);
                }

                // 计算校验和
                if (opts.EnableChecksumValidation)
                {
                    result.Checksum = CalculateChecksum(data);
                }

                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 写入文件
                await File.WriteAllBytesAsync(filePath, data);

                result.Success = true;
                result.Message = "配置保存成功";
                result.FileSize = data.Length;
                result.ElapsedTime = DateTime.Now - startTime;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"保存配置失败: {ex.Message}";
                result.Exception = ex;
                result.ElapsedTime = DateTime.Now - startTime;
            }

            return result;
        }

        /// <summary>
        /// 加载配置（高级版本）
        /// </summary>
        public static async Task<(ApplicationConfiguration? config, PersistenceResult result)> LoadConfigurationAsync(
            string filePath, 
            PersistenceOptions? options = null)
        {
            var opts = options ?? _options;
            var startTime = DateTime.Now;
            var result = new PersistenceResult { FilePath = filePath };

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Success = false;
                    result.Message = "配置文件不存在";
                    return (null, result);
                }

                // 读取文件
                var data = await File.ReadAllBytesAsync(filePath);
                result.FileSize = data.Length;

                // 验证校验和
                if (opts.EnableChecksumValidation)
                {
                    result.Checksum = CalculateChecksum(data);
                }

                // 解密
                if (opts.EnableEncryption && !string.IsNullOrEmpty(opts.EncryptionKey))
                {
                    data = DecryptData(data, opts.EncryptionKey);
                }

                // 解压缩
                if (opts.EnableCompression)
                {
                    data = await DecompressDataAsync(data);
                }

                // 反序列化
                var json = Encoding.UTF8.GetString(data);
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                var config = JsonSerializer.Deserialize<ApplicationConfiguration>(json, jsonOptions);

                result.Success = true;
                result.Message = "配置加载成功";
                result.ElapsedTime = DateTime.Now - startTime;

                return (config, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"加载配置失败: {ex.Message}";
                result.Exception = ex;
                result.ElapsedTime = DateTime.Now - startTime;

                return (null, result);
            }
        }

        /// <summary>
        /// 创建配置备份
        /// </summary>
        public static async Task<ConfigurationBackup?> CreateBackupAsync(
            ApplicationConfiguration config, 
            string? name = null, 
            string? description = null,
            bool isAutoBackup = false)
        {
            try
            {
                var backupName = name ?? $"配置备份_{DateTime.Now:yyyyMMdd_HHmmss}";
                var backupDir = GetBackupDirectory();
                var backupFile = Path.Combine(backupDir, $"{backupName}_{Guid.NewGuid():N}.config");

                var saveResult = await SaveConfigurationAsync(config, backupFile);
                if (!saveResult.Success) return null;

                var backup = new ConfigurationBackup
                {
                    Name = backupName,
                    FilePath = backupFile,
                    FileSize = saveResult.FileSize,
                    Checksum = saveResult.Checksum,
                    Description = description ?? "",
                    Version = config.Version,
                    IsAutoBackup = isAutoBackup
                };

                _backups.Add(backup);
                SaveBackupHistory();
                CleanupOldBackups();

                BackupCreated?.Invoke(null, backup);
                return backup;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建备份失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 恢复配置备份
        /// </summary>
        public static async Task<ApplicationConfiguration?> RestoreBackupAsync(string backupId)
        {
            try
            {
                var backup = _backups.FirstOrDefault(b => b.Id == backupId);
                if (backup == null || !File.Exists(backup.FilePath)) return null;

                var (config, result) = await LoadConfigurationAsync(backup.FilePath);
                if (result.Success && config != null)
                {
                    // 验证校验和
                    if (!string.IsNullOrEmpty(backup.Checksum) && backup.Checksum != result.Checksum)
                    {
                        throw new InvalidOperationException("备份文件校验和不匹配，可能已损坏");
                    }

                    BackupRestored?.Invoke(null, backup);
                    return config;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"恢复备份失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除配置备份
        /// </summary>
        public static bool DeleteBackup(string backupId)
        {
            try
            {
                var backup = _backups.FirstOrDefault(b => b.Id == backupId);
                if (backup == null) return false;

                if (File.Exists(backup.FilePath))
                {
                    File.Delete(backup.FilePath);
                }

                _backups.Remove(backup);
                SaveBackupHistory();

                BackupDeleted?.Invoke(null, backupId);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备份失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有备份
        /// </summary>
        public static List<ConfigurationBackup> GetAllBackups()
        {
            return _backups.OrderByDescending(b => b.CreatedTime).ToList();
        }

        /// <summary>
        /// 验证配置文件完整性
        /// </summary>
        public static async Task<bool> ValidateFileIntegrityAsync(string filePath, string expectedChecksum)
        {
            try
            {
                if (!File.Exists(filePath)) return false;

                var data = await File.ReadAllBytesAsync(filePath);
                var actualChecksum = CalculateChecksum(data);
                
                return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 导出配置为可读格式
        /// </summary>
        public static async Task<bool> ExportConfigurationAsync(
            ApplicationConfiguration config, 
            string filePath, 
            ConfigurationExportFormat format = ConfigurationExportFormat.Json)
        {
            try
            {
                string content = format switch
                {
                    ConfigurationExportFormat.Json => JsonSerializer.Serialize(config, new JsonSerializerOptions 
                    { 
                        WriteIndented = true, 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    }),
                    ConfigurationExportFormat.Xml => ConvertToXml(config),
                    ConfigurationExportFormat.Ini => ConvertToIni(config),
                    _ => throw new NotSupportedException($"不支持的导出格式: {format}")
                };

                await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"导出配置失败: {ex.Message}");
                return false;
            }
        }

        #region 私有方法

        private static async Task<byte[]> CompressDataAsync(byte[] data)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
            {
                await gzip.WriteAsync(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static async Task<byte[]> DecompressDataAsync(byte[] compressedData)
        {
            using var input = new MemoryStream(compressedData);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output);
            return output.ToArray();
        }

        private static byte[] EncryptData(byte[] data, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16]; // 简化实现，实际应用中应使用随机IV

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        private static byte[] DecryptData(byte[] encryptedData, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = new byte[16];

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        private static string CalculateChecksum(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        private static string GetBackupDirectory()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var backupDir = Path.Combine(appDataPath, "STGenerator", "ConfigBackups");
            Directory.CreateDirectory(backupDir);
            return backupDir;
        }

        private static void LoadBackupHistory()
        {
            try
            {
                var historyFile = Path.Combine(GetBackupDirectory(), "backup_history.json");
                if (File.Exists(historyFile))
                {
                    var json = File.ReadAllText(historyFile);
                    var backups = JsonSerializer.Deserialize<List<ConfigurationBackup>>(json);
                    if (backups != null)
                    {
                        _backups.Clear();
                        _backups.AddRange(backups.Where(b => File.Exists(b.FilePath)));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备份历史失败: {ex.Message}");
            }
        }

        private static void SaveBackupHistory()
        {
            try
            {
                var historyFile = Path.Combine(GetBackupDirectory(), "backup_history.json");
                var json = JsonSerializer.Serialize(_backups, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(historyFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存备份历史失败: {ex.Message}");
            }
        }

        private static void CleanupOldBackups()
        {
            try
            {
                var autoBackups = _backups.Where(b => b.IsAutoBackup).OrderByDescending(b => b.CreatedTime).ToList();
                var backupsToDelete = autoBackups.Skip(_options.MaxBackupCount).ToList();

                foreach (var backup in backupsToDelete)
                {
                    DeleteBackup(backup.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理旧备份失败: {ex.Message}");
            }
        }

        private static void SetupAutoBackup()
        {
            _autoBackupTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    var config = ConfigurationManager.Current;
                    await CreateBackupAsync(config, null, "自动备份", true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"自动备份失败: {ex.Message}");
                }
            }, null, _options.BackupInterval, _options.BackupInterval);
        }

        private static string ConvertToXml(ApplicationConfiguration config)
        {
            // 简化的XML转换实现
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine("<Configuration>");
            xml.AppendLine($"  <Version>{config.Version}</Version>");
            xml.AppendLine($"  <LastModified>{config.LastModified:yyyy-MM-dd HH:mm:ss}</LastModified>");
            // 这里可以添加更详细的XML序列化逻辑
            xml.AppendLine("</Configuration>");
            return xml.ToString();
        }

        private static string ConvertToIni(ApplicationConfiguration config)
        {
            // 简化的INI转换实现
            var ini = new StringBuilder();
            ini.AppendLine("[General]");
            ini.AppendLine($"Version={config.Version}");
            ini.AppendLine($"LastModified={config.LastModified:yyyy-MM-dd HH:mm:ss}");
            // 这里可以添加更详细的INI序列化逻辑
            return ini.ToString();
        }

        #endregion

        /// <summary>
        /// 清理资源
        /// </summary>
        public static void Dispose()
        {
            _autoBackupTimer?.Dispose();
        }
    }

    /// <summary>
    /// 配置导出格式
    /// </summary>
    public enum ConfigurationExportFormat
    {
        Json,
        Xml,
        Ini
    }
}