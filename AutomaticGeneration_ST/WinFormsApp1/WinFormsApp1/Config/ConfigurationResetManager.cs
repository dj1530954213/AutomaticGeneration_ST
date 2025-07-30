using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 配置重置管理器 - 提供灵活的配置重置功能
    /// </summary>
    public static class ConfigurationResetManager
    {
        #region 重置选项定义

        /// <summary>
        /// 重置选项
        /// </summary>
        public class ResetOptions
        {
            /// <summary>
            /// 重置通用配置
            /// </summary>
            public bool ResetGeneral { get; set; } = true;

            /// <summary>
            /// 重置模板配置
            /// </summary>
            public bool ResetTemplate { get; set; } = true;

            /// <summary>
            /// 重置性能配置
            /// </summary>
            public bool ResetPerformance { get; set; } = true;

            /// <summary>
            /// 重置界面配置
            /// </summary>
            public bool ResetUI { get; set; } = true;

            /// <summary>
            /// 重置导出配置
            /// </summary>
            public bool ResetExport { get; set; } = true;

            /// <summary>
            /// 重置高级配置
            /// </summary>
            public bool ResetAdvanced { get; set; } = false;

            /// <summary>
            /// 重置字段映射配置
            /// </summary>
            public bool ResetFieldMapping { get; set; } = false;

            /// <summary>
            /// 重置设备配置
            /// </summary>
            public bool ResetDevice { get; set; } = true;

            /// <summary>
            /// 重置应用程序设置
            /// </summary>
            public bool ResetApplicationSettings { get; set; } = true;

            /// <summary>
            /// 重置模板库配置
            /// </summary>
            public bool ResetTemplateLibrary { get; set; } = false;

            /// <summary>
            /// 重置窗口位置和大小
            /// </summary>
            public bool ResetWindowSettings { get; set; } = false;

            /// <summary>
            /// 是否创建备份
            /// </summary>
            public bool CreateBackup { get; set; } = true;

            /// <summary>
            /// 备份描述
            /// </summary>
            public string BackupDescription { get; set; } = "重置前自动备份";
        }

        /// <summary>
        /// 重置结果
        /// </summary>
        public class ResetResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public List<string> Details { get; set; } = new();
            public string? BackupPath { get; set; }
            public Exception? Exception { get; set; }
        }

        #endregion

        #region 主要重置方法

        /// <summary>
        /// 执行配置重置
        /// </summary>
        public static async Task<ResetResult> ResetConfigurationAsync(ResetOptions? options = null)
        {
            var result = new ResetResult();
            var details = new List<string>();

            try
            {
                options ??= new ResetOptions();

                // 创建备份
                if (options.CreateBackup)
                {
                    var backupResult = await CreateResetBackupAsync(options.BackupDescription);
                    if (backupResult.Success)
                    {
                        result.BackupPath = backupResult.BackupPath;
                        details.Add($"已创建配置备份：{backupResult.BackupPath}");
                    }
                    else
                    {
                        details.Add($"备份创建失败：{backupResult.Message}");
                    }
                }

                // 重置主配置
                if (await ResetMainConfigurationAsync(options))
                {
                    details.Add("主配置已重置为默认值");
                }

                // 重置字段映射配置
                if (options.ResetFieldMapping)
                {
                    if (ResetFieldMappingConfiguration())
                    {
                        details.Add("字段映射配置已重置");
                    }
                }

                // 重置模板库配置
                if (options.ResetTemplateLibrary)
                {
                    if (await ResetTemplateLibraryConfigurationAsync())
                    {
                        details.Add("模板库配置已重置");
                    }
                }

                // 重置窗口设置
                if (options.ResetWindowSettings)
                {
                    if (ResetWindowSettingsConfiguration())
                    {
                        details.Add("窗口设置已重置");
                    }
                }

                // 重置缓存配置
                if (options.ResetPerformance)
                {
                    if (ResetCacheConfiguration())
                    {
                        details.Add("缓存配置已重置");
                    }
                }

                result.Success = true;
                result.Message = "配置重置成功";
                result.Details = details;

                // 触发配置重新加载
                await ConfigurationManager.LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"配置重置失败：{ex.Message}";
                result.Exception = ex;
                result.Details = details;
            }

            return result;
        }

        /// <summary>
        /// 显示重置选项对话框
        /// </summary>
        public static async Task<ResetResult?> ShowResetOptionsDialogAsync()
        {
            var form = new ConfigurationResetForm();
            
            if (form.ShowDialog() == DialogResult.OK)
            {
                return await ResetConfigurationAsync(form.SelectedOptions);
            }

            return null;
        }

        #endregion

        #region 具体重置方法

        /// <summary>
        /// 重置主配置
        /// </summary>
        private static async Task<bool> ResetMainConfigurationAsync(ResetOptions options)
        {
            try
            {
                var currentConfig = ConfigurationManager.Current;
                var newConfig = new ApplicationConfiguration();

                // 选择性重置
                if (options.ResetGeneral)
                {
                    currentConfig.General = newConfig.General;
                }

                if (options.ResetTemplate)
                {
                    currentConfig.Template = newConfig.Template;
                }

                if (options.ResetPerformance)
                {
                    currentConfig.Performance = newConfig.Performance;
                }

                if (options.ResetUI)
                {
                    currentConfig.UI = newConfig.UI;
                }

                if (options.ResetExport)
                {
                    currentConfig.Export = newConfig.Export;
                }

                if (options.ResetAdvanced)
                {
                    currentConfig.Advanced = newConfig.Advanced;
                }

                var saveResult = await ConfigurationManager.SaveConfigurationAsync();
                return saveResult.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重置字段映射配置
        /// </summary>
        private static bool ResetFieldMappingConfiguration()
        {
            try
            {
                var defaultMapping = FieldMappingConfiguration.CreateDefaultMapping();
                return FieldMappingConfiguration.SaveMapping(defaultMapping);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重置模板库配置
        /// </summary>
        private static async Task<bool> ResetTemplateLibraryConfigurationAsync()
        {
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "template-library.json");

                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                // 重新初始化模板库
                await Task.Run(() => WinFormsApp1.Templates.TemplateManager.InitializeDefaultTemplates());
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重置窗口设置配置
        /// </summary>
        private static bool ResetWindowSettingsConfiguration()
        {
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "window-settings.json");

                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重置缓存配置
        /// </summary>
        private static bool ResetCacheConfiguration()
        {
            try
            {
                var configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "cache_config.json");

                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }

                // 清空模板缓存
                WinFormsApp1.Templates.TemplateManager.ClearTemplateCache();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 备份相关方法

        /// <summary>
        /// 创建重置前备份
        /// </summary>
        private static async Task<(bool Success, string Message, string? BackupPath)> CreateResetBackupAsync(string description)
        {
            try
            {
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "Backups", "ResetBackups");

                Directory.CreateDirectory(backupDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDir, $"reset_backup_{timestamp}");
                Directory.CreateDirectory(backupPath);

                // 备份主配置
                var configFile = ConfigurationManager.ConfigFilePath;
                if (File.Exists(configFile))
                {
                    File.Copy(configFile, Path.Combine(backupPath, "config.json"));
                }

                // 备份字段映射配置
                var fieldMappingFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "field-mapping.json");

                if (File.Exists(fieldMappingFile))
                {
                    File.Copy(fieldMappingFile, Path.Combine(backupPath, "field-mapping.json"));
                }

                // 备份模板库配置
                var templateLibraryFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "template-library.json");

                if (File.Exists(templateLibraryFile))
                {
                    File.Copy(templateLibraryFile, Path.Combine(backupPath, "template-library.json"));
                }

                // 创建备份信息文件
                var backupInfo = new
                {
                    CreatedDate = DateTime.Now,
                    Description = description,
                    Version = "2.0.0",
                    BackupType = "ConfigurationReset",
                    Files = Directory.GetFiles(backupPath).Select(Path.GetFileName).ToList()
                };

                var infoJson = System.Text.Json.JsonSerializer.Serialize(backupInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.WriteAllTextAsync(Path.Combine(backupPath, "backup-info.json"), infoJson);

                return (true, "备份创建成功", backupPath);
            }
            catch (Exception ex)
            {
                return (false, $"备份创建失败：{ex.Message}", null);
            }
        }

        /// <summary>
        /// 获取所有重置备份
        /// </summary>
        public static List<BackupInfo> GetResetBackups()
        {
            var backups = new List<BackupInfo>();

            try
            {
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "Backups", "ResetBackups");

                if (!Directory.Exists(backupDir))
                    return backups;

                foreach (var dir in Directory.GetDirectories(backupDir))
                {
                    var infoFile = Path.Combine(dir, "backup-info.json");
                    if (File.Exists(infoFile))
                    {
                        try
                        {
                            var json = File.ReadAllText(infoFile);
                            var info = System.Text.Json.JsonSerializer.Deserialize<BackupInfo>(json);
                            if (info != null)
                            {
                                info.BackupPath = dir;
                                backups.Add(info);
                            }
                        }
                        catch
                        {
                            // 忽略无效的备份信息文件
                        }
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return backups.OrderByDescending(b => b.CreatedDate).ToList();
        }

        /// <summary>
        /// 从备份恢复配置
        /// </summary>
        public static async Task<bool> RestoreFromBackupAsync(string backupPath)
        {
            try
            {
                // 恢复主配置
                var configBackup = Path.Combine(backupPath, "config.json");
                if (File.Exists(configBackup))
                {
                    File.Copy(configBackup, ConfigurationManager.ConfigFilePath, true);
                }

                // 恢复字段映射配置
                var fieldMappingBackup = Path.Combine(backupPath, "field-mapping.json");
                var fieldMappingTarget = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "field-mapping.json");

                if (File.Exists(fieldMappingBackup))
                {
                    File.Copy(fieldMappingBackup, fieldMappingTarget, true);
                }

                // 恢复模板库配置
                var templateLibraryBackup = Path.Combine(backupPath, "template-library.json");
                var templateLibraryTarget = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "STGenerator", "template-library.json");

                if (File.Exists(templateLibraryBackup))
                {
                    File.Copy(templateLibraryBackup, templateLibraryTarget, true);
                }

                // 重新加载配置
                await ConfigurationManager.LoadConfigurationAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region 辅助类

        /// <summary>
        /// 备份信息
        /// </summary>
        public class BackupInfo
        {
            public DateTime CreatedDate { get; set; }
            public string Description { get; set; } = "";
            public string Version { get; set; } = "";
            public string BackupType { get; set; } = "";
            public List<string> Files { get; set; } = new();
            public string BackupPath { get; set; } = "";
        }

        #endregion
    }

    /// <summary>
    /// 配置重置表单
    /// </summary>
    public partial class ConfigurationResetForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ConfigurationResetManager.ResetOptions SelectedOptions { get; private set; } = new ConfigurationResetManager.ResetOptions();

        private CheckBox resetTemplatesCheckBox;
        private CheckBox resetDevicesCheckBox;
        private CheckBox resetSettingsCheckBox;
        private CheckBox createBackupCheckBox;
        private Button okButton;
        private Button cancelButton;

        public ConfigurationResetForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "重置配置选项";
            Size = new Size(400, 250);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            resetTemplatesCheckBox = new CheckBox
            {
                Text = "重置模板配置",
                Location = new Point(20, 20),
                Size = new Size(300, 20),
                Checked = true
            };

            resetDevicesCheckBox = new CheckBox
            {
                Text = "重置设备配置",
                Location = new Point(20, 50),
                Size = new Size(300, 20),
                Checked = true
            };

            resetSettingsCheckBox = new CheckBox
            {
                Text = "重置应用设置",
                Location = new Point(20, 80),
                Size = new Size(300, 20),
                Checked = true
            };

            createBackupCheckBox = new CheckBox
            {
                Text = "重置前创建备份",
                Location = new Point(20, 110),
                Size = new Size(300, 20),
                Checked = true
            };

            okButton = new Button
            {
                Text = "确认重置",
                Location = new Point(220, 160),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;

            cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(310, 160),
                Size = new Size(60, 30),
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[] {
                resetTemplatesCheckBox,
                resetDevicesCheckBox,
                resetSettingsCheckBox,
                createBackupCheckBox,
                okButton,
                cancelButton
            });
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            SelectedOptions = new ConfigurationResetManager.ResetOptions
            {
                ResetTemplate = resetTemplatesCheckBox.Checked,
                ResetDevice = resetDevicesCheckBox.Checked,
                ResetApplicationSettings = resetSettingsCheckBox.Checked,
                CreateBackup = createBackupCheckBox.Checked
            };
        }
    }
}