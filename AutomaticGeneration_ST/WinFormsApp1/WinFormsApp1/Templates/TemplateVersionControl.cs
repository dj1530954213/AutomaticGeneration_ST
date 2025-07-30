using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace WinFormsApp1.Templates
{
    public class TemplateVersionInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "System";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class TemplateHistory
    {
        public string TemplateId { get; set; } = "";
        public PointType PointType { get; set; }
        public List<TemplateVersionInfo> Versions { get; set; } = new();
        public string CurrentVersionId { get; set; } = "";
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class TemplateVersionControlConfig
    {
        public int MaxVersionsPerTemplate { get; set; } = 10;
        public bool AutoBackup { get; set; } = true;
        public string BackupDirectory { get; set; } = "Templates/Backups";
        public Dictionary<string, TemplateHistory> Templates { get; set; } = new();
    }

    public static class TemplateVersionControl
    {
        private static TemplateVersionControlConfig? _config;
        private static readonly string ConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Templates", "version_control.json");

        public static TemplateVersionControlConfig Config => _config ??= LoadConfig();

        public static event Action<TemplateHistory>? VersionAdded;
        public static event Action<TemplateHistory>? VersionRestored;
        public static event Action<TemplateHistory>? VersionDeleted;

        public static string CreateVersion(PointType pointType, Templates.TemplateVersion templateType, 
            string content, string description = "", string author = "User")
        {
            try
            {
                var templateId = $"{pointType}_{templateType}";
                var config = Config;

                if (!config.Templates.ContainsKey(templateId))
                {
                    config.Templates[templateId] = new TemplateHistory
                    {
                        TemplateId = templateId,
                        PointType = pointType
                    };
                }

                var history = config.Templates[templateId];
                var version = new TemplateVersionInfo
                {
                    Name = $"v{history.Versions.Count + 1}_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Content = content,
                    Description = description,
                    Author = author,
                    CreatedDate = DateTime.Now
                };

                // 添加版本
                history.Versions.Add(version);
                history.CurrentVersionId = version.Id;
                history.LastModified = DateTime.Now;

                // 清理旧版本（保持最大版本数限制）
                CleanupOldVersions(history);

                // 自动备份
                if (config.AutoBackup)
                {
                    CreateBackupFile(templateId, version);
                }

                SaveConfig();
                VersionAdded?.Invoke(history);

                return version.Id;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"创建模板版本失败: {ex.Message}", ex);
            }
        }

        public static bool RestoreVersion(string templateId, string versionId)
        {
            try
            {
                var config = Config;
                if (!config.Templates.ContainsKey(templateId))
                    return false;

                var history = config.Templates[templateId];
                var version = history.Versions.FirstOrDefault(v => v.Id == versionId);
                
                if (version == null)
                    return false;

                // 创建当前版本的备份
                var currentVersion = GetCurrentVersion(templateId);
                if (currentVersion != null)
                {
                    CreateVersion(history.PointType, GetTemplateTypeFromId(templateId), 
                        currentVersion.Content, "自动备份 - 版本恢复前", "System");
                }

                // 恢复指定版本
                history.CurrentVersionId = versionId;
                history.LastModified = DateTime.Now;

                // 更新模板文件
                UpdateTemplateFile(templateId, version.Content);

                SaveConfig();
                VersionRestored?.Invoke(history);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static TemplateVersionInfo? GetCurrentVersion(string templateId)
        {
            var config = Config;
            if (!config.Templates.ContainsKey(templateId))
                return null;

            var history = config.Templates[templateId];
            return history.Versions.FirstOrDefault(v => v.Id == history.CurrentVersionId);
        }

        public static List<TemplateVersionInfo> GetVersionHistory(string templateId)
        {
            var config = Config;
            if (!config.Templates.ContainsKey(templateId))
                return new List<TemplateVersionInfo>();

            return config.Templates[templateId].Versions
                .OrderByDescending(v => v.CreatedDate)
                .ToList();
        }

        public static List<TemplateHistory> GetAllTemplateHistories()
        {
            return Config.Templates.Values.ToList();
        }

        public static bool DeleteVersion(string templateId, string versionId)
        {
            try
            {
                var config = Config;
                if (!config.Templates.ContainsKey(templateId))
                    return false;

                var history = config.Templates[templateId];
                var version = history.Versions.FirstOrDefault(v => v.Id == versionId);
                
                if (version == null || history.CurrentVersionId == versionId)
                    return false; // 不能删除当前版本

                history.Versions.Remove(version);
                history.LastModified = DateTime.Now;

                // 删除备份文件
                DeleteBackupFile(templateId, version);

                SaveConfig();
                VersionDeleted?.Invoke(history);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CompareVersions(string templateId, string versionId1, string versionId2, 
            out string diff)
        {
            diff = "";
            
            try
            {
                var config = Config;
                if (!config.Templates.ContainsKey(templateId))
                    return false;

                var history = config.Templates[templateId];
                var version1 = history.Versions.FirstOrDefault(v => v.Id == versionId1);
                var version2 = history.Versions.FirstOrDefault(v => v.Id == versionId2);

                if (version1 == null || version2 == null)
                    return false;

                diff = GenerateSimpleDiff(version1.Content, version2.Content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ExportVersionHistory(string templateId, string format = "json")
        {
            try
            {
                var config = Config;
                if (!config.Templates.ContainsKey(templateId))
                    return "";

                var history = config.Templates[templateId];

                return format.ToLower() switch
                {
                    "json" => JsonSerializer.Serialize(history, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        Converters = { new JsonStringEnumConverter() }
                    }),
                    "csv" => ExportToCsv(history),
                    _ => JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true })
                };
            }
            catch
            {
                return "";
            }
        }

        public static bool ImportVersionHistory(string templateId, string data, string format = "json")
        {
            try
            {
                TemplateHistory? history = null;

                switch (format.ToLower())
                {
                    case "json":
                        history = JsonSerializer.Deserialize<TemplateHistory>(data);
                        break;
                    default:
                        return false;
                }

                if (history == null)
                    return false;

                var config = Config;
                config.Templates[templateId] = history;
                SaveConfig();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Dictionary<string, object> GetVersionStatistics(string templateId)
        {
            var stats = new Dictionary<string, object>();
            
            try
            {
                var config = Config;
                if (!config.Templates.ContainsKey(templateId))
                    return stats;

                var history = config.Templates[templateId];
                var versions = history.Versions;

                stats["总版本数"] = versions.Count;
                stats["当前版本"] = GetCurrentVersion(templateId)?.Name ?? "未知";
                stats["最后修改时间"] = history.LastModified;
                stats["创建时间"] = versions.Count > 0 ? versions.Min(v => v.CreatedDate) : DateTime.MinValue;
                
                var authorStats = versions.GroupBy(v => v.Author)
                    .ToDictionary(g => g.Key, g => g.Count());
                stats["作者统计"] = authorStats;

                var monthlyStats = versions.GroupBy(v => new { v.CreatedDate.Year, v.CreatedDate.Month })
                    .ToDictionary(g => $"{g.Key.Year}-{g.Key.Month:D2}", g => g.Count());
                stats["月度版本统计"] = monthlyStats;
            }
            catch (Exception ex)
            {
                stats["错误"] = ex.Message;
            }

            return stats;
        }

        private static void CleanupOldVersions(TemplateHistory history)
        {
            var config = Config;
            if (history.Versions.Count <= config.MaxVersionsPerTemplate)
                return;

            // 保留最新的版本和当前版本
            var versionsToKeep = history.Versions
                .OrderByDescending(v => v.CreatedDate)
                .Take(config.MaxVersionsPerTemplate - 1)
                .ToList();

            var currentVersion = history.Versions.FirstOrDefault(v => v.Id == history.CurrentVersionId);
            if (currentVersion != null && !versionsToKeep.Contains(currentVersion))
            {
                versionsToKeep.Add(currentVersion);
            }

            // 删除旧版本的备份文件
            var versionsToDelete = history.Versions.Except(versionsToKeep).ToList();
            foreach (var version in versionsToDelete)
            {
                DeleteBackupFile(history.TemplateId, version);
            }

            history.Versions = versionsToKeep;
        }

        private static void CreateBackupFile(string templateId, TemplateVersionInfo version)
        {
            try
            {
                var config = Config;
                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.BackupDirectory);
                Directory.CreateDirectory(backupDir);

                var fileName = $"{templateId}_{version.Name}_{version.Id}.scriban";
                var filePath = Path.Combine(backupDir, fileName);

                File.WriteAllText(filePath, version.Content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建备份文件失败: {ex.Message}");
            }
        }

        private static void DeleteBackupFile(string templateId, TemplateVersionInfo version)
        {
            try
            {
                var config = Config;
                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, config.BackupDirectory);
                var fileName = $"{templateId}_{version.Name}_{version.Id}.scriban";
                var filePath = Path.Combine(backupDir, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备份文件失败: {ex.Message}");
            }
        }

        private static void UpdateTemplateFile(string templateId, string content)
        {
            try
            {
                var parts = templateId.Split('_');
                if (parts.Length >= 2 && 
                    Enum.TryParse<PointType>(parts[0], out var pointType) &&
                    Enum.TryParse<Templates.TemplateVersion>(parts[1], out var templateVersion))
                {
                    var templateInfo = TemplateManager.GetTemplateInfo(pointType, templateVersion);
                    if (templateInfo != null && File.Exists(templateInfo.FilePath))
                    {
                        File.WriteAllText(templateInfo.FilePath, content);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新模板文件失败: {ex.Message}");
            }
        }

        private static Templates.TemplateVersion GetTemplateTypeFromId(string templateId)
        {
            var parts = templateId.Split('_');
            if (parts.Length >= 2 && Enum.TryParse<Templates.TemplateVersion>(parts[1], out var templateType))
            {
                return templateType;
            }
            return Templates.TemplateVersion.Default;
        }

        private static string GenerateSimpleDiff(string content1, string content2)
        {
            var lines1 = content1.Split('\n');
            var lines2 = content2.Split('\n');
            
            var diff = new System.Text.StringBuilder();
            diff.AppendLine("=== 版本对比 ===");
            diff.AppendLine($"版本1行数: {lines1.Length}");
            diff.AppendLine($"版本2行数: {lines2.Length}");
            diff.AppendLine();

            var maxLines = Math.Max(lines1.Length, lines2.Length);
            var differences = 0;

            for (int i = 0; i < maxLines; i++)
            {
                var line1 = i < lines1.Length ? lines1[i] : "";
                var line2 = i < lines2.Length ? lines2[i] : "";

                if (line1 != line2)
                {
                    differences++;
                    diff.AppendLine($"行 {i + 1}:");
                    diff.AppendLine($"- {line1}");
                    diff.AppendLine($"+ {line2}");
                    diff.AppendLine();
                }
            }

            if (differences == 0)
            {
                diff.AppendLine("两个版本内容相同");
            }
            else
            {
                diff.Insert(0, $"发现 {differences} 处差异\n\n");
            }

            return diff.ToString();
        }

        private static string ExportToCsv(TemplateHistory history)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("版本ID,版本名称,作者,创建时间,描述,是否当前版本");

            foreach (var version in history.Versions.OrderByDescending(v => v.CreatedDate))
            {
                var isCurrent = version.Id == history.CurrentVersionId ? "是" : "否";
                csv.AppendLine($"{version.Id},{version.Name},{version.Author}," +
                              $"{version.CreatedDate:yyyy-MM-dd HH:mm:ss},{version.Description},{isCurrent}");
            }

            return csv.ToString();
        }

        private static TemplateVersionControlConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<TemplateVersionControlConfig>(json);
                    return config ?? new TemplateVersionControlConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载版本控制配置失败: {ex.Message}");
            }

            return new TemplateVersionControlConfig();
        }

        private static void SaveConfig()
        {
            try
            {
                var configDir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                var json = JsonSerializer.Serialize(Config, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存版本控制配置失败: {ex.Message}");
            }
        }
    }
}