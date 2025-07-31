using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Forms
{
    /// <summary>
    /// 备份管理窗体 - 模板备份和恢复管理界面
    /// </summary>
    public partial class BackupManagerForm : Form
    {
        #region 私有字段

        private SplitContainer mainSplitContainer;
        private ListView backupListView;
        private Panel detailPanel;
        private Panel toolbarPanel;
        private Panel statusPanel;

        // 工具栏控件
        private ToolStrip toolStrip;
        private ToolStripButton createBackupButton;
        private ToolStripButton restoreBackupButton;
        private ToolStripButton deleteBackupButton;
        private ToolStripButton protectBackupButton;
        private ToolStripButton verifyBackupButton;
        private ToolStripButton settingsButton;
        private ToolStripComboBox backupTypeFilterComboBox;

        // 详情面板控件
        private Label backupNameLabel;
        private Label backupInfoLabel;
        private TextBox backupDescriptionTextBox;
        private ListBox backupTagsListBox;
        private ProgressBar verificationProgressBar;
        private Label verificationStatusLabel;

        // 状态栏控件
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel countLabel;
        private ToolStripStatusLabel sizeLabel;
        private ToolStripProgressBar operationProgressBar;

        // 数据相关
        private List<TemplateBackupManager.BackupInfo> currentBackups = new List<TemplateBackupManager.BackupInfo>();
        private TemplateBackupManager.BackupInfo? selectedBackup;
        private bool isLoading = false;

        // UI相关
        private ImageList listImageList;

        #endregion

        #region 构造函数和初始化

        public BackupManagerForm()
        {
            InitializeComponent();
            InitializeImageList();
            CreateControls();
            SetupLayout();
            BindEvents();
            LoadBackupData();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            Text = "备份管理";
            Size = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(800, 600);
        }

        private void InitializeImageList()
        {
            listImageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };
            
            // 可以在这里添加具体的备份类型图标
        }

        private void CreateControls()
        {
            CreateMainLayout();
            CreateToolbar();
            CreateBackupListView();
            CreateDetailPanel();
            CreateStatusBar();
        }

        private void CreateMainLayout()
        {
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 400,
                Panel2MinSize = 250
            };
        }

        private void CreateToolbar()
        {
            toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = SystemColors.Control
            };

            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                ImageScalingSize = new Size(24, 24)
            };

            createBackupButton = new ToolStripButton
            {
                Text = "💾 创建备份",
                ToolTipText = "创建新的模板备份",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            restoreBackupButton = new ToolStripButton
            {
                Text = "🔄 恢复备份",
                ToolTipText = "从选中的备份恢复",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false
            };

            deleteBackupButton = new ToolStripButton
            {
                Text = "🗑️ 删除",
                ToolTipText = "删除选中的备份",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false
            };

            protectBackupButton = new ToolStripButton
            {
                Text = "🔒 保护",
                ToolTipText = "保护/取消保护备份",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false
            };

            verifyBackupButton = new ToolStripButton
            {
                Text = "✅ 验证",
                ToolTipText = "验证所有备份的完整性",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            settingsButton = new ToolStripButton
            {
                Text = "⚙️ 设置",
                ToolTipText = "备份设置",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            backupTypeFilterComboBox = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(100, 25),
                ToolTipText = "按类型筛选备份"
            };
            backupTypeFilterComboBox.Items.AddRange(new[] { "全部", "手动", "自动", "系统", "导出", "快照" });
            backupTypeFilterComboBox.SelectedIndex = 0;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                createBackupButton,
                restoreBackupButton,
                deleteBackupButton,
                protectBackupButton,
                new ToolStripSeparator(),
                verifyBackupButton,
                settingsButton,
                new ToolStripSeparator(),
                new ToolStripLabel("类型:"),
                backupTypeFilterComboBox
            });

            toolbarPanel.Controls.Add(toolStrip);
        }

        private void CreateBackupListView()
        {
            backupListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("微软雅黑", 9F),
                SmallImageList = listImageList
            };

            backupListView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "名称", Width = 200 },
                new ColumnHeader { Text = "类型", Width = 80 },
                new ColumnHeader { Text = "创建时间", Width = 140 },
                new ColumnHeader { Text = "大小", Width = 80 },
                new ColumnHeader { Text = "模板数", Width = 80 },
                new ColumnHeader { Text = "收藏数", Width = 80 },
                new ColumnHeader { Text = "状态", Width = 80 },
                new ColumnHeader { Text = "描述", Width = 300 }
            });
        }

        private void CreateDetailPanel()
        {
            detailPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            var titleLabel = new Label
            {
                Text = "备份详情",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // 备份名称
            backupNameLabel = new Label
            {
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 10),
                MaximumSize = new Size(350, 0),
                Text = "选择备份查看详情"
            };

            // 备份信息
            backupInfoLabel = new Label
            {
                AutoSize = true,
                Location = new Point(0, 40),
                MaximumSize = new Size(350, 0),
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.DarkBlue,
                Text = ""
            };

            // 备份描述
            var descriptionLabel = new Label
            {
                Text = "描述:",
                Location = new Point(0, 120),
                Size = new Size(50, 20),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };

            backupDescriptionTextBox = new TextBox
            {
                Location = new Point(0, 145),
                Size = new Size(350, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 标签列表
            var tagsLabel = new Label
            {
                Text = "标签:",
                Location = new Point(0, 235),
                Size = new Size(50, 20),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };

            backupTagsListBox = new ListBox
            {
                Location = new Point(0, 260),
                Size = new Size(350, 80),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 验证状态
            var verificationLabel = new Label
            {
                Text = "完整性验证:",
                Location = new Point(0, 350),
                Size = new Size(100, 20),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };

            verificationProgressBar = new ProgressBar
            {
                Location = new Point(0, 375),
                Size = new Size(350, 20),
                Style = ProgressBarStyle.Continuous
            };

            verificationStatusLabel = new Label
            {
                Location = new Point(0, 400),
                Size = new Size(350, 20),
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.Gray,
                Text = "点击验证按钮检查备份完整性"
            };

            contentPanel.Controls.AddRange(new Control[]
            {
                backupNameLabel, backupInfoLabel, descriptionLabel, backupDescriptionTextBox,
                tagsLabel, backupTagsListBox, verificationLabel, verificationProgressBar,
                verificationStatusLabel
            });

            detailPanel.Controls.Add(contentPanel);
            detailPanel.Controls.Add(titleLabel);
        }

        private void CreateStatusBar()
        {
            statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                BackColor = SystemColors.Control
            };

            statusStrip = new StatusStrip
            {
                Dock = DockStyle.Fill
            };

            statusLabel = new ToolStripStatusLabel
            {
                Text = "就绪",
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            countLabel = new ToolStripStatusLabel
            {
                Text = "备份: 0",
                AutoSize = true
            };

            sizeLabel = new ToolStripStatusLabel
            {
                Text = "总大小: 0 B",
                AutoSize = true
            };

            operationProgressBar = new ToolStripProgressBar
            {
                Size = new Size(100, 16),
                Visible = false
            };

            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                statusLabel,
                new ToolStripStatusLabel("|"),
                countLabel,
                new ToolStripStatusLabel("|"),
                sizeLabel,
                operationProgressBar
            });

            statusPanel.Controls.Add(statusStrip);
        }

        private void SetupLayout()
        {
            mainSplitContainer.Panel1.Controls.Add(backupListView);
            mainSplitContainer.Panel2.Controls.Add(detailPanel);

            Controls.Add(mainSplitContainer);
            Controls.Add(toolbarPanel);
            Controls.Add(statusPanel);
        }

        private void BindEvents()
        {
            // 工具栏事件
            createBackupButton.Click += OnCreateBackup;
            restoreBackupButton.Click += OnRestoreBackup;
            deleteBackupButton.Click += OnDeleteBackup;
            protectBackupButton.Click += OnProtectBackup;
            verifyBackupButton.Click += OnVerifyBackups;
            settingsButton.Click += OnOpenSettings;
            backupTypeFilterComboBox.SelectedIndexChanged += OnBackupTypeFilterChanged;

            // 列表事件
            backupListView.SelectedIndexChanged += OnBackupSelected;
            backupListView.DoubleClick += OnBackupDoubleClick;

            // 窗体事件
            FormClosing += OnFormClosing;
            Load += OnFormLoad;
        }

        #endregion

        #region 数据加载和显示

        private void LoadBackupData()
        {
            try
            {
                isLoading = true;
                UpdateStatus("正在加载备份列表...", true);

                // 初始化备份管理器
                TemplateBackupManager.Initialize();

                // 加载备份列表
                LoadBackupList();

                UpdateStatus("备份列表加载完成", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载备份列表失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("加载失败", false);
            }
            finally
            {
                isLoading = false;
            }
        }

        private void LoadBackupList()
        {
            try
            {
                backupListView.BeginUpdate();
                backupListView.Items.Clear();

                // 获取备份列表
                var backupType = GetSelectedBackupType();
                var backups = TemplateBackupManager.GetAllBackups(backupType);

                currentBackups = backups;

                foreach (var backup in backups)
                {
                    var item = CreateBackupListItem(backup);
                    backupListView.Items.Add(item);
                }

                UpdateStatusLabels();
            }
            finally
            {
                backupListView.EndUpdate();
            }
        }

        private TemplateBackupManager.BackupType? GetSelectedBackupType()
        {
            return backupTypeFilterComboBox.SelectedIndex switch
            {
                1 => TemplateBackupManager.BackupType.Manual,
                2 => TemplateBackupManager.BackupType.Automatic,
                3 => TemplateBackupManager.BackupType.System,
                4 => TemplateBackupManager.BackupType.Export,
                5 => TemplateBackupManager.BackupType.Snapshot,
                _ => null
            };
        }

        private ListViewItem CreateBackupListItem(TemplateBackupManager.BackupInfo backup)
        {
            var item = new ListViewItem(backup.Name)
            {
                Tag = backup,
                ImageIndex = GetBackupTypeImageIndex(backup.Type)
            };

            var statusText = File.Exists(backup.FilePath) ? 
                (backup.IsProtected ? "受保护" : "正常") : "文件缺失";

            var statusColor = File.Exists(backup.FilePath) ? 
                (backup.IsProtected ? Color.Blue : Color.Green) : Color.Red;

            item.SubItems.AddRange(new[]
            {
                GetBackupTypeText(backup.Type),
                backup.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
                FormatFileSize(backup.FileSize),
                backup.TemplateCount.ToString(),
                backup.FavoriteCount.ToString(),
                statusText,
                backup.Description
            });

            // 设置状态颜色
            if (!File.Exists(backup.FilePath))
            {
                item.ForeColor = Color.Red;
            }
            else if (backup.IsProtected)
            {
                item.ForeColor = Color.Blue;
            }

            return item;
        }

        private void UpdateBackupDetails(TemplateBackupManager.BackupInfo? backup)
        {
            selectedBackup = backup;

            if (backup == null)
            {
                backupNameLabel.Text = "选择备份查看详情";
                backupInfoLabel.Text = "";
                backupDescriptionTextBox.Text = "";
                backupTagsListBox.Items.Clear();
                verificationStatusLabel.Text = "点击验证按钮检查备份完整性";
                verificationProgressBar.Value = 0;

                restoreBackupButton.Enabled = false;
                deleteBackupButton.Enabled = false;
                protectBackupButton.Enabled = false;
                return;
            }

            backupNameLabel.Text = backup.Name;
            
            var fileExists = File.Exists(backup.FilePath);
            var statusText = fileExists ? "✓ 文件存在" : "✗ 文件缺失";
            var protectionText = backup.IsProtected ? "🔒 受保护" : "🔓 未保护";

            backupInfoLabel.Text = $"ID: {backup.Id}\n" +
                                  $"类型: {GetBackupTypeText(backup.Type)}\n" +
                                  $"创建时间: {backup.CreatedDate:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"文件大小: {FormatFileSize(backup.FileSize)}\n" +
                                  $"模板数量: {backup.TemplateCount}\n" +
                                  $"收藏数量: {backup.FavoriteCount}\n" +
                                  $"分类数量: {backup.CategoryCount}\n" +
                                  $"文件状态: {statusText}\n" +
                                  $"保护状态: {protectionText}\n" +
                                  $"应用版本: {backup.AppVersion}\n" +
                                  $"备份版本: {backup.BackupVersion}";

            backupDescriptionTextBox.Text = backup.Description;

            backupTagsListBox.Items.Clear();
            foreach (var tag in backup.Tags)
            {
                backupTagsListBox.Items.Add(tag);
            }

            // 更新按钮状态
            restoreBackupButton.Enabled = fileExists;
            deleteBackupButton.Enabled = !backup.IsProtected;
            protectBackupButton.Enabled = true;
            protectBackupButton.Text = backup.IsProtected ? "🔓 取消保护" : "🔒 保护";
        }

        #endregion

        #region 事件处理

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // 窗体加载完成后的初始化工作
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // 窗体关闭时的清理工作
        }

        private void OnBackupSelected(object? sender, EventArgs e)
        {
            if (backupListView.SelectedItems.Count > 0)
            {
                var selectedItem = backupListView.SelectedItems[0];
                var backup = selectedItem.Tag as TemplateBackupManager.BackupInfo;
                UpdateBackupDetails(backup);
            }
            else
            {
                UpdateBackupDetails(null);
            }
        }

        private void OnBackupDoubleClick(object? sender, EventArgs e)
        {
            if (selectedBackup != null && File.Exists(selectedBackup.FilePath))
            {
                OnRestoreBackup(sender, e);
            }
        }

        private async void OnCreateBackup(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new CreateBackupDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    UpdateStatus("正在创建备份...", true);
                    
                    var backupId = await TemplateBackupManager.CreateBackupAsync(
                        dialog.BackupName, 
                        dialog.BackupDescription, 
                        dialog.BackupTags);
                    
                    LoadBackupList();
                    UpdateStatus("备份创建成功", false);
                    
                    // 选中新创建的备份
                    var newBackupItem = backupListView.Items.Cast<ListViewItem>()
                        .FirstOrDefault(item => ((TemplateBackupManager.BackupInfo)item.Tag).Id == backupId);
                    if (newBackupItem != null)
                    {
                        newBackupItem.Selected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建备份失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("创建备份失败", false);
            }
        }

        private async void OnRestoreBackup(object? sender, EventArgs e)
        {
            if (selectedBackup == null)
                return;

            try
            {
                using var dialog = new RestoreBackupDialog(selectedBackup);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    UpdateStatus("正在恢复备份...", true);
                    
                    var result = await TemplateBackupManager.RestoreBackupAsync(
                        selectedBackup.Id, 
                        dialog.RestoreOptions);
                    
                    UpdateStatus("备份恢复完成", false);
                    
                    // 显示恢复结果
                    ShowRestoreResult(result);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复备份失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("恢复备份失败", false);
            }
        }

        private async void OnDeleteBackup(object? sender, EventArgs e)
        {
            if (selectedBackup == null)
                return;

            try
            {
                var result = MessageBox.Show(
                    $"确定要删除备份 '{selectedBackup.Name}' 吗？\n\n此操作不可撤销！",
                    "确认删除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    UpdateStatus("正在删除备份...", true);
                    
                    await TemplateBackupManager.DeleteBackupAsync(selectedBackup.Id);
                    
                    LoadBackupList();
                    UpdateBackupDetails(null);
                    UpdateStatus("备份已删除", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除备份失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("删除备份失败", false);
            }
        }

        private void OnProtectBackup(object? sender, EventArgs e)
        {
            if (selectedBackup == null)
                return;

            try
            {
                var protect = !selectedBackup.IsProtected;
                var actionText = protect ? "保护" : "取消保护";
                
                var result = MessageBox.Show(
                    $"确定要{actionText}备份 '{selectedBackup.Name}' 吗？",
                    $"确认{actionText}",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    TemplateBackupManager.ProtectBackup(selectedBackup.Id, protect);
                    
                    LoadBackupList();
                    UpdateStatus($"备份已{actionText}", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnVerifyBackups(object? sender, EventArgs e)
        {
            try
            {
                UpdateStatus("正在验证备份完整性...", true);
                verificationProgressBar.Value = 0;
                verificationStatusLabel.Text = "正在验证...";

                var results = await TemplateBackupManager.VerifyBackupIntegrityAsync();
                
                var totalBackups = results.Count;
                var validBackups = results.Count(r => r.Value);
                var invalidBackups = totalBackups - validBackups;

                verificationProgressBar.Value = 100;
                verificationStatusLabel.Text = $"验证完成: {validBackups} 个有效, {invalidBackups} 个无效";

                if (invalidBackups > 0)
                {
                    var invalidBackupNames = results.Where(r => !r.Value)
                        .Select(r => currentBackups.FirstOrDefault(b => b.Id == r.Key)?.Name ?? r.Key)
                        .ToList();

                    MessageBox.Show(
                        $"发现 {invalidBackups} 个损坏的备份:\n\n{string.Join("\n", invalidBackupNames)}",
                        "备份验证结果",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "所有备份都通过了完整性验证！",
                        "备份验证结果",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                LoadBackupList(); // 刷新列表以显示验证结果
                UpdateStatus("备份验证完成", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"验证备份失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("验证失败", false);
            }
        }

        private void OnOpenSettings(object? sender, EventArgs e)
        {
            try
            {
                using var settingsForm = new BackupSettingsDialog();
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    UpdateStatus("备份设置已更新", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开设置失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnBackupTypeFilterChanged(object? sender, EventArgs e)
        {
            LoadBackupList();
        }

        #endregion

        #region 辅助方法

        private void UpdateStatus(string message, bool showProgress)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }

            if (operationProgressBar != null)
            {
                operationProgressBar.Visible = showProgress;
            }
        }

        private void UpdateStatusLabels()
        {
            if (countLabel != null)
            {
                countLabel.Text = $"备份: {currentBackups.Count}";
            }

            if (sizeLabel != null)
            {
                var totalSize = currentBackups.Sum(b => b.FileSize);
                sizeLabel.Text = $"总大小: {FormatFileSize(totalSize)}";
            }
        }

        private void ShowRestoreResult(TemplateBackupManager.RestoreResult result)
        {
            var message = result.Success 
                ? $"恢复成功！\n\n成功: {result.RestoredItemCount}\n跳过: {result.SkippedItemCount}\n失败: {result.FailedItemCount}\n耗时: {result.Duration.TotalSeconds:F1} 秒"
                : $"恢复失败: {result.ErrorMessage}";

            var icon = result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error;
            
            MessageBox.Show(message, "恢复结果", MessageBoxButtons.OK, icon);
        }

        private string GetBackupTypeText(TemplateBackupManager.BackupType type)
        {
            return type switch
            {
                TemplateBackupManager.BackupType.Manual => "手动",
                TemplateBackupManager.BackupType.Automatic => "自动",
                TemplateBackupManager.BackupType.System => "系统",
                TemplateBackupManager.BackupType.Export => "导出",
                TemplateBackupManager.BackupType.Snapshot => "快照",
                _ => "未知"
            };
        }

        private int GetBackupTypeImageIndex(TemplateBackupManager.BackupType type)
        {
            return (int)type;
        }

        private string FormatFileSize(long bytes)
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

        private void ApplyTheme()
        {
            try
            {
                // 应用主题色彩
                BackColor = WinFormsApp1.Config.ThemeManager.GetBackgroundColor();
                ForeColor = WinFormsApp1.Config.ThemeManager.GetTextColor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题失败: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 创建备份对话框
    /// </summary>
    public partial class CreateBackupDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string BackupName { get; set; } = string.Empty;
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string BackupDescription { get; set; } = string.Empty;
        
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> BackupTags { get; set; } = new List<string>();

        private TextBox nameTextBox;
        private TextBox descriptionTextBox;
        private TextBox tagsTextBox;

        public CreateBackupDialog()
        {
            InitializeComponent();
            CreateControls();
        }

        private void InitializeComponent()
        {
            Text = "创建备份";
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
        }

        private void CreateControls()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 名称
            var nameLabel = new Label { Text = "备份名称:", Dock = DockStyle.Fill };
            nameTextBox = new TextBox { Dock = DockStyle.Fill, Text = $"Manual_{DateTime.Now:yyyyMMdd_HHmmss}" };

            // 描述
            var descLabel = new Label { Text = "描述:", Dock = DockStyle.Fill };
            descriptionTextBox = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };

            // 标签
            var tagsLabel = new Label { Text = "标签 (用逗号分隔):", Dock = DockStyle.Fill };
            tagsTextBox = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "例如: 重要, 发布前, 测试" };

            // 按钮
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                Height = 40
            };

            var cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            var okButton = new Button
            {
                Text = "创建",
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OnOkClick;

            buttonPanel.Controls.AddRange(new Control[] { cancelButton, okButton });

            layout.Controls.Add(CreateLabeledControl(nameLabel, nameTextBox), 0, 0);
            layout.Controls.Add(CreateLabeledControl(descLabel, descriptionTextBox), 0, 1);
            layout.Controls.Add(CreateLabeledControl(tagsLabel, tagsTextBox), 0, 2);
            layout.Controls.Add(buttonPanel, 0, 3);

            Controls.Add(layout);
        }

        private Panel CreateLabeledControl(Label label, Control control)
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            label.Height = 25;
            label.Dock = DockStyle.Top;
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control);
            panel.Controls.Add(label);
            return panel;
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("请输入备份名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            BackupName = nameTextBox.Text.Trim();
            BackupDescription = descriptionTextBox.Text.Trim();
            
            if (!string.IsNullOrWhiteSpace(tagsTextBox.Text))
            {
                BackupTags = tagsTextBox.Text.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();
            }

            DialogResult = DialogResult.OK;
        }
    }

    /// <summary>
    /// 恢复备份对话框
    /// </summary>
    public partial class RestoreBackupDialog : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TemplateBackupManager.RestoreOptions RestoreOptions { get; set; } = new TemplateBackupManager.RestoreOptions();

        private TemplateBackupManager.BackupInfo backup;
        private CheckBox restoreLibraryCheckBox;
        private CheckBox restoreTemplatesCheckBox;
        private CheckBox restoreConfigCheckBox;
        private RadioButton replaceModeRadio;
        private RadioButton mergeModeRadio;
        private RadioButton addOnlyModeRadio;
        private CheckBox createPreBackupCheckBox;

        public RestoreBackupDialog(TemplateBackupManager.BackupInfo backup)
        {
            this.backup = backup;
            InitializeComponent();
            CreateControls();
        }

        private void InitializeComponent()
        {
            Text = "恢复备份";
            Size = new Size(450, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
        }

        private void CreateControls()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(20)
            };

            // 备份信息
            var infoLabel = new Label
            {
                Text = $"备份: {backup.Name}\n创建时间: {backup.CreatedDate:yyyy-MM-dd HH:mm:ss}\n包含: {backup.TemplateCount} 个模板, {backup.FavoriteCount} 个收藏",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.DarkBlue
            };

            // 恢复内容选择
            var contentPanel = new GroupBox { Text = "恢复内容", Dock = DockStyle.Fill };
            var contentLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill };

            restoreLibraryCheckBox = new CheckBox { Text = "模板库 (收藏和分类)", Checked = true, AutoSize = true };
            restoreTemplatesCheckBox = new CheckBox { Text = "模板文件", Checked = true, AutoSize = true };
            restoreConfigCheckBox = new CheckBox { Text = "配置文件", Checked = true, AutoSize = true };

            contentLayout.Controls.AddRange(new Control[] { restoreLibraryCheckBox, restoreTemplatesCheckBox, restoreConfigCheckBox });
            contentPanel.Controls.Add(contentLayout);

            // 恢复模式选择
            var modePanel = new GroupBox { Text = "恢复模式", Dock = DockStyle.Fill };
            var modeLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill };

            mergeModeRadio = new RadioButton { Text = "合并模式 (保留现有数据，添加新数据)", Checked = true, AutoSize = true };
            replaceModeRadio = new RadioButton { Text = "替换模式 (完全替换现有数据)", AutoSize = true };
            addOnlyModeRadio = new RadioButton { Text = "仅添加模式 (只添加不存在的项目)", AutoSize = true };

            modeLayout.Controls.AddRange(new Control[] { mergeModeRadio, replaceModeRadio, addOnlyModeRadio });
            modePanel.Controls.Add(modeLayout);

            // 其他选项
            var optionsPanel = new GroupBox { Text = "其他选项", Dock = DockStyle.Fill };
            createPreBackupCheckBox = new CheckBox 
            { 
                Text = "恢复前创建当前状态备份", 
                Checked = true, 
                Dock = DockStyle.Fill 
            };
            optionsPanel.Controls.Add(createPreBackupCheckBox);

            // 按钮
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                Height = 40
            };

            var cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            var okButton = new Button
            {
                Text = "恢复",
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OnOkClick;

            buttonPanel.Controls.AddRange(new Control[] { cancelButton, okButton });

            layout.Controls.Add(infoLabel, 0, 0);
            layout.Controls.Add(contentPanel, 0, 1);
            layout.Controls.Add(modePanel, 0, 2);
            layout.Controls.Add(optionsPanel, 0, 3);
            layout.Controls.Add(buttonPanel, 0, 4);

            Controls.Add(layout);
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            RestoreOptions.RestoreLibrary = restoreLibraryCheckBox.Checked;
            RestoreOptions.RestoreTemplates = restoreTemplatesCheckBox.Checked;
            RestoreOptions.RestoreConfig = restoreConfigCheckBox.Checked;
            RestoreOptions.CreatePreRestoreBackup = createPreBackupCheckBox.Checked;

            if (replaceModeRadio.Checked)
                RestoreOptions.Mode = TemplateBackupManager.RestoreMode.Replace;
            else if (addOnlyModeRadio.Checked)
                RestoreOptions.Mode = TemplateBackupManager.RestoreMode.AddOnly;
            else
                RestoreOptions.Mode = TemplateBackupManager.RestoreMode.Merge;

            DialogResult = DialogResult.OK;
        }
    }

    /// <summary>
    /// 备份设置对话框
    /// </summary>
    public partial class BackupSettingsDialog : Form
    {
        // 这里可以添加备份设置的具体实现
        public BackupSettingsDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "备份设置";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = "备份设置功能即将推出...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 12F)
            };

            var okButton = new Button
            {
                Text = "确定",
                Size = new Size(75, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(400, 330),
                DialogResult = DialogResult.OK
            };

            Controls.AddRange(new Control[] { label, okButton });
        }
    }
}