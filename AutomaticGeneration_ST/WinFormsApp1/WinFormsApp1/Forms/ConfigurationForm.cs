using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1.Config;

namespace WinFormsApp1.Forms
{
    public partial class ConfigurationForm : Form
    {
        private TreeView _categoryTreeView;
        private PropertyGrid _propertyGrid;
        private Button _okButton;
        private Button _cancelButton;
        private Button _applyButton;
        private Button _resetButton;
        private Button _importButton;
        private Button _exportButton;
        private Label _descriptionLabel;
        private Label _statusLabel;
        private CheckBox _requiresRestartCheckBox;
        
        private WinFormsApp1.Config.ApplicationConfiguration _originalConfig;
        private WinFormsApp1.Config.ApplicationConfiguration _workingConfig;
        private bool _hasChanges = false;
        private readonly Dictionary<SettingsCategory, string> _categoryDescriptions;

        public ConfigurationForm()
        {
            _originalConfig = ConfigurationManager.Current.Clone();
            _workingConfig = ConfigurationManager.Current.Clone();
            
            _categoryDescriptions = new Dictionary<SettingsCategory, string>
            {
                [SettingsCategory.General] = "应用程序的通用设置，包括语言、自动保存、备份等选项。",
                [SettingsCategory.Template] = "模板系统相关设置，包括默认版本、编辑器配置、验证选项等。",
                [SettingsCategory.Performance] = "性能优化设置，包括缓存配置、并发处理、内存管理等。",
                [SettingsCategory.UI] = "用户界面设置，包括主题、字体、窗口布局、动画效果等。",
                [SettingsCategory.Export] = "文件导出设置，包括格式、编码、缩进、路径等选项。",
                [SettingsCategory.Advanced] = "高级设置，包括调试模式、日志配置、插件系统等。"
            };

            InitializeComponent();
            LoadConfiguration();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体设置
            this.Text = "系统配置";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            // 内容区域
            var contentPanel = CreateContentPanel();
            
            // 按钮区域
            var buttonPanel = CreateButtonPanel();
            
            // 状态区域
            var statusPanel = CreateStatusPanel();

            mainPanel.Controls.Add(contentPanel, 0, 0);
            mainPanel.Controls.Add(buttonPanel, 0, 1);
            mainPanel.Controls.Add(statusPanel, 0, 2);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private Panel CreateContentPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 400
            };

            // 上方：主要内容分割器
            var mainSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 200
            };

            // 左侧：类别树
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var categoryLabel = new Label
            {
                Text = "配置类别",
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            _categoryTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9),
                FullRowSelect = true,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = false,
                ShowRootLines = false
            };

            leftPanel.Controls.Add(_categoryTreeView);
            leftPanel.Controls.Add(categoryLabel);

            // 右侧：属性网格
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            var propertyLabel = new Label
            {
                Text = "配置项",
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            _propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9),
                PropertySort = PropertySort.Categorized,
                ToolbarVisible = false
            };

            rightPanel.Controls.Add(_propertyGrid);
            rightPanel.Controls.Add(propertyLabel);

            mainSplitter.Panel1.Controls.Add(leftPanel);
            mainSplitter.Panel2.Controls.Add(rightPanel);

            // 下方：描述区域
            var descPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };

            var descLabel = new Label
            {
                Text = "说明",
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("微软雅黑", 9, FontStyle.Bold)
            };

            _descriptionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.DarkBlue,
                Text = "请选择左侧的配置类别查看相关设置。"
            };

            descPanel.Controls.Add(_descriptionLabel);
            descPanel.Controls.Add(descLabel);

            splitter.Panel1.Controls.Add(mainSplitter);
            splitter.Panel2.Controls.Add(descPanel);

            panel.Controls.Add(splitter);
            return panel;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                Padding = new Padding(10)
            };

            _okButton = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point(490, 15),
                DialogResult = DialogResult.OK,
                Font = new Font("微软雅黑", 9)
            };

            _cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(80, 30),
                Location = new Point(580, 15),
                DialogResult = DialogResult.Cancel,
                Font = new Font("微软雅黑", 9)
            };

            _applyButton = new Button
            {
                Text = "应用",
                Size = new Size(80, 30),
                Location = new Point(670, 15),
                Enabled = false,
                Font = new Font("微软雅黑", 9)
            };

            _resetButton = new Button
            {
                Text = "重置",
                Size = new Size(80, 30),
                Location = new Point(10, 15),
                Font = new Font("微软雅黑", 9)
            };

            _importButton = new Button
            {
                Text = "导入...",
                Size = new Size(80, 30),
                Location = new Point(100, 15),
                Font = new Font("微软雅黑", 9)
            };

            _exportButton = new Button
            {
                Text = "导出...",
                Size = new Size(80, 30),
                Location = new Point(190, 15),
                Font = new Font("微软雅黑", 9)
            };

            _requiresRestartCheckBox = new CheckBox
            {
                Text = "需要重启应用程序",
                AutoSize = true,
                Location = new Point(300, 20),
                Font = new Font("微软雅黑", 9),
                ForeColor = Color.Red,
                Visible = false
            };

            panel.Controls.AddRange(new Control[] {
                _okButton, _cancelButton, _applyButton, _resetButton,
                _importButton, _exportButton, _requiresRestartCheckBox
            });

            return panel;
        }

        private Panel CreateStatusPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 30
            };

            _statusLabel = new Label
            {
                Text = "就绪",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("微软雅黑", 8),
                ForeColor = Color.DarkGreen
            };

            panel.Controls.Add(_statusLabel);
            return panel;
        }

        private void LoadConfiguration()
        {
            // 加载类别树
            _categoryTreeView.Nodes.Clear();

            foreach (SettingsCategory category in Enum.GetValues<SettingsCategory>())
            {
                var node = new TreeNode
                {
                    Text = GetCategoryDisplayName(category),
                    Tag = category,
                    ImageIndex = GetCategoryIconIndex(category),
                    SelectedImageIndex = GetCategoryIconIndex(category)
                };

                _categoryTreeView.Nodes.Add(node);
            }

            // 默认选择第一个节点
            if (_categoryTreeView.Nodes.Count > 0)
            {
                _categoryTreeView.SelectedNode = _categoryTreeView.Nodes[0];
            }
        }

        private void SetupEventHandlers()
        {
            // 树节点选择事件
            _categoryTreeView.AfterSelect += OnCategorySelected;

            // 属性网格事件
            _propertyGrid.PropertyValueChanged += OnPropertyValueChanged;

            // 按钮事件
            _okButton.Click += OnOkClicked;
            _cancelButton.Click += OnCancelClicked;
            _applyButton.Click += OnApplyClicked;
            _resetButton.Click += OnResetClicked;
            _importButton.Click += OnImportClicked;
            _exportButton.Click += OnExportClicked;

            // 窗体关闭事件
            this.FormClosing += OnFormClosing;
        }

        private void OnCategorySelected(object? sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is SettingsCategory category)
            {
                LoadCategorySettings(category);
                UpdateDescription(category);
            }
        }

        private void LoadCategorySettings(SettingsCategory category)
        {
            try
            {
                var categoryConfigs = _workingConfig.GetAllCategories();
                if (categoryConfigs.TryGetValue(category, out var config))
                {
                    _propertyGrid.SelectedObject = config;
                }

                UpdateStatus($"已加载 {GetCategoryDisplayName(category)} 设置", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载设置失败: {ex.Message}", Color.Red);
            }
        }

        private void UpdateDescription(SettingsCategory category)
        {
            if (_categoryDescriptions.TryGetValue(category, out var description))
            {
                _descriptionLabel.Text = description;
            }
            else
            {
                _descriptionLabel.Text = "暂无说明。";
            }
        }

        private void OnPropertyValueChanged(object? s, PropertyValueChangedEventArgs e)
        {
            _hasChanges = true;
            _applyButton.Enabled = true;
            UpdateStatus("配置已修改", Color.Blue);

            // 检查是否需要重启
            CheckRestartRequired();
        }

        private void CheckRestartRequired()
        {
            // 简化实现：某些关键配置需要重启
            var restartRequiredSettings = new[]
            {
                "General.DefaultLanguage",
                "Performance.EnableCaching",
                "Advanced.EnableDebugMode"
            };

            // 这里可以实现更复杂的逻辑来检查具体哪些设置发生了变化
            _requiresRestartCheckBox.Visible = false; // 暂时隐藏
        }

        private async void OnOkClicked(object? sender, EventArgs e)
        {
            if (_hasChanges)
            {
                await ApplyChanges();
            }
            this.Close();
        }

        private void OnCancelClicked(object? sender, EventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show(
                    "您有未保存的配置更改。确定要放弃这些更改吗？",
                    "确认取消",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;
            }

            this.Close();
        }

        private async void OnApplyClicked(object? sender, EventArgs e)
        {
            await ApplyChanges();
        }

        private async Task ApplyChanges()
        {
            try
            {
                _applyButton.Enabled = false;
                UpdateStatus("正在保存配置...", Color.Blue);

                // 验证配置
                var errors = _workingConfig.Validate();
                if (errors.Any())
                {
                    var errorMessage = "配置验证失败：\n" + string.Join("\n", errors);
                    MessageBox.Show(errorMessage, "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _applyButton.Enabled = true;
                    return;
                }

                // 更新配置管理器
                var categories = _workingConfig.GetAllCategories();
                foreach (var kvp in categories)
                {
                    var categoryName = kvp.Key.ToString();
                    var properties = kvp.Value.GetType().GetProperties();
                    
                    foreach (var property in properties)
                    {
                        var key = $"{categoryName}.{property.Name}";
                        var value = property.GetValue(kvp.Value);
                        ConfigurationManager.SetValue(key, value);
                    }
                }

                // 保存到文件
                var result = await ConfigurationManager.SaveConfigurationAsync();
                if (result.Success)
                {
                    _originalConfig = _workingConfig.Clone();
                    _hasChanges = false;
                    UpdateStatus("配置保存成功", Color.DarkGreen);

                    if (result.Warnings.Any())
                    {
                        var warningMessage = "配置已保存，但有以下警告：\n" + string.Join("\n", result.Warnings);
                        MessageBox.Show(warningMessage, "保存警告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    UpdateStatus($"保存失败: {result.Message}", Color.Red);
                    MessageBox.Show($"保存配置失败：{result.Message}", "保存错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _applyButton.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"保存配置时出错: {ex.Message}", Color.Red);
                MessageBox.Show($"保存配置时出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _applyButton.Enabled = true;
            }
        }

        private async void OnResetClicked(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "确定要将所有配置重置为默认值吗？此操作不可撤销。",
                "确认重置",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    UpdateStatus("正在重置配置...", Color.Blue);
                    
                    _workingConfig.ResetToDefaults();
                    
                    // 重新加载界面
                    if (_categoryTreeView.SelectedNode?.Tag is SettingsCategory category)
                    {
                        LoadCategorySettings(category);
                    }

                    _hasChanges = true;
                    _applyButton.Enabled = true;
                    UpdateStatus("配置已重置为默认值", Color.DarkGreen);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"重置配置失败: {ex.Message}", Color.Red);
                    MessageBox.Show($"重置配置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void OnImportClicked(object? sender, EventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Filter = "配置文件|*.json|所有文件|*.*",
                Title = "导入配置文件"
            };

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    UpdateStatus("正在导入配置...", Color.Blue);
                    
                    var success = await ConfigurationManager.ImportConfigurationAsync(openDialog.FileName);
                    if (success)
                    {
                        _workingConfig = ConfigurationManager.Current.Clone();
                        _originalConfig = _workingConfig.Clone();
                        
                        // 重新加载界面
                        if (_categoryTreeView.SelectedNode?.Tag is SettingsCategory category)
                        {
                            LoadCategorySettings(category);
                        }

                        _hasChanges = false;
                        _applyButton.Enabled = false;
                        UpdateStatus("配置导入成功", Color.DarkGreen);
                    }
                    else
                    {
                        UpdateStatus("配置导入失败", Color.Red);
                        MessageBox.Show("导入配置文件失败，请检查文件格式是否正确。", "导入失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"导入配置时出错: {ex.Message}", Color.Red);
                    MessageBox.Show($"导入配置时出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void OnExportClicked(object? sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "配置文件|*.json|所有文件|*.*",
                Title = "导出配置文件",
                FileName = $"config_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    UpdateStatus("正在导出配置...", Color.Blue);
                    
                    var success = await ConfigurationManager.ExportConfigurationAsync(saveDialog.FileName);
                    if (success)
                    {
                        UpdateStatus("配置导出成功", Color.DarkGreen);
                        MessageBox.Show("配置文件导出成功！", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        UpdateStatus("配置导出失败", Color.Red);
                        MessageBox.Show("导出配置文件失败。", "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"导出配置时出错: {ex.Message}", Color.Red);
                    MessageBox.Show($"导出配置时出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_hasChanges && this.DialogResult != DialogResult.OK)
            {
                var result = MessageBox.Show(
                    "您有未保存的配置更改。确定要放弃这些更改吗？",
                    "确认关闭",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;
        }

        private string GetCategoryDisplayName(SettingsCategory category)
        {
            return category switch
            {
                SettingsCategory.General => "通用设置",
                SettingsCategory.Template => "模板设置",
                SettingsCategory.Performance => "性能设置",
                SettingsCategory.UI => "界面设置",
                SettingsCategory.Export => "导出设置",
                SettingsCategory.Advanced => "高级设置",
                _ => category.ToString()
            };
        }

        private int GetCategoryIconIndex(SettingsCategory category)
        {
            // 可以根据需要添加图标
            return 0;
        }
    }
}