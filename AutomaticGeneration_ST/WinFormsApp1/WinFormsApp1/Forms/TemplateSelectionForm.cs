//NEED DELETE: 模板选择窗口（模板管理相关UI），与核心导入/生成/导出无关
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WinFormsApp1.Templates;
using WinFormsApp1.Config;

namespace WinFormsApp1.Forms
{
    /// <summary>
    /// 模板选择对话框 - 允许用户选择不同版本的模板进行代码生成
    /// </summary>
    public partial class TemplateSelectionForm : Form
    {
        private readonly Dictionary<PointType, TemplateVersion> _selectedTemplates;
        private TableLayoutPanel mainTableLayout;
        private GroupBox aiGroupBox, aoGroupBox, diGroupBox, doGroupBox;
        private ComboBox aiComboBox, aoComboBox, diComboBox, doComboBox;
        private Label aiDescLabel, aoDescLabel, diDescLabel, doDescLabel;
        private Button okButton, cancelButton, resetButton, previewButton;
        private CheckBox enableCustomTemplatesCheckBox;
        private Panel statusPanel;
        private Label statusLabel;
        
        /// <summary>
        /// 获取用户选择的模板配置
        /// </summary>
        public Dictionary<PointType, TemplateVersion> SelectedTemplates => _selectedTemplates;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="currentSelections">当前的模板选择（用于初始化对话框）</param>
        public TemplateSelectionForm(Dictionary<PointType, TemplateVersion>? currentSelections = null)
        {
            _selectedTemplates = currentSelections ?? new Dictionary<PointType, TemplateVersion>();
            
            InitializeComponent();
            InitializeTemplateOptions();
            ApplyTheme();
        }
        
        private void InitializeComponent()
        {
            // 窗体基本设置
            Text = "模板选择器";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("微软雅黑", 10F);
            
            // 创建主布局
            mainTableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(15)
            };
            
            mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 80F));
            mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            
            // 创建模板选择区域
            var templatePanel = CreateTemplateSelectionPanel();
            mainTableLayout.Controls.Add(templatePanel, 0, 0);
            
            // 创建状态面板
            statusPanel = CreateStatusPanel();
            mainTableLayout.Controls.Add(statusPanel, 0, 1);
            
            // 创建按钮面板
            var buttonPanel = CreateButtonPanel();
            mainTableLayout.Controls.Add(buttonPanel, 0, 2);
            
            Controls.Add(mainTableLayout);
        }
        
        private Panel CreateTemplateSelectionPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            
            var selectionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3
            };
            
            selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            selectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            selectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            selectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            selectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
            
            // 创建各点类型的选择控件
            aiGroupBox = CreatePointTypeGroupBox(PointType.AI, out aiComboBox, out aiDescLabel);
            aoGroupBox = CreatePointTypeGroupBox(PointType.AO, out aoComboBox, out aoDescLabel);
            diGroupBox = CreatePointTypeGroupBox(PointType.DI, out diComboBox, out diDescLabel);
            doGroupBox = CreatePointTypeGroupBox(PointType.DO, out doComboBox, out doDescLabel);
            
            selectionLayout.Controls.Add(aiGroupBox, 0, 0);
            selectionLayout.Controls.Add(aoGroupBox, 1, 0);
            selectionLayout.Controls.Add(diGroupBox, 0, 1);
            selectionLayout.Controls.Add(doGroupBox, 1, 1);
            
            // 自定义模板选项
            var customPanel = CreateCustomTemplatePanel();
            selectionLayout.Controls.Add(customPanel, 0, 2);
            selectionLayout.SetColumnSpan(customPanel, 2);
            
            panel.Controls.Add(selectionLayout);
            return panel;
        }
        
        private GroupBox CreatePointTypeGroupBox(PointType pointType, out ComboBox comboBox, out Label descLabel)
        {
            var groupBox = new GroupBox
            {
                Text = $"{pointType} 点位模板",
                Dock = DockStyle.Fill,
                Margin = new Padding(5),
                Padding = new Padding(10),
                Font = new Font("微软雅黑", 10F, FontStyle.Bold)
            };
            
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            
            // 模板版本选择下拉框
            comboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("微软雅黑", 9F),
                Margin = new Padding(0, 0, 0, 5)
            };
            
            var localComboBox = comboBox;
            comboBox.SelectedIndexChanged += (s, e) => OnTemplateSelectionChanged(pointType, localComboBox);
            
            // 模板描述标签
            descLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "选择模板版本以查看描述",
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = true
            };
            
            // 统计信息标签
            var statsLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 7F),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.BottomLeft
            };
            
            layout.Controls.Add(comboBox, 0, 0);
            layout.Controls.Add(descLabel, 0, 1);
            layout.Controls.Add(statsLabel, 0, 2);
            
            groupBox.Controls.Add(layout);
            
            // 存储统计标签引用
            groupBox.Tag = statsLabel;
            
            return groupBox;
        }
        
        private Panel CreateCustomTemplatePanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            
            enableCustomTemplatesCheckBox = new CheckBox
            {
                Text = "启用自定义模板支持",
                Dock = DockStyle.Left,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.DarkGreen,
                AutoSize = true
            };
            
            enableCustomTemplatesCheckBox.CheckedChanged += OnCustomTemplatesToggled;
            
            var infoLabel = new Label
            {
                Text = "自定义模板允许您创建和使用个性化的代码生成模板",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            
            panel.Controls.Add(enableCustomTemplatesCheckBox);
            panel.Controls.Add(infoLabel);
            
            return panel;
        }
        
        private Panel CreateStatusPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 248, 255),
                Margin = new Padding(0, 5, 0, 5)
            };
            
            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "请选择各点类型的模板版本",
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };
            
            panel.Controls.Add(statusLabel);
            return panel;
        }
        
        private Panel CreateButtonPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 45
            };
            
            var buttonLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };
            
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // 弹性空间
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            
            // 预览按钮
            previewButton = new Button
            {
                Text = "预览",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                UseVisualStyleBackColor = true,
                Margin = new Padding(2)
            };
            previewButton.Click += OnPreviewButtonClick;
            
            // 重置按钮
            resetButton = new Button
            {
                Text = "重置",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                UseVisualStyleBackColor = true,
                Margin = new Padding(2)
            };
            resetButton.Click += OnResetButtonClick;
            
            // 确定按钮
            okButton = new Button
            {
                Text = "确定",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                UseVisualStyleBackColor = true,
                Margin = new Padding(2)
            };
            okButton.Click += OnOkButtonClick;
            AcceptButton = okButton;
            
            // 取消按钮
            cancelButton = new Button
            {
                Text = "取消",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                UseVisualStyleBackColor = true,
                Margin = new Padding(2)
            };
            cancelButton.Click += OnCancelButtonClick;
            CancelButton = cancelButton;
            
            buttonLayout.Controls.Add(new Panel(), 0, 0); // 空白填充
            buttonLayout.Controls.Add(previewButton, 1, 0);
            buttonLayout.Controls.Add(resetButton, 2, 0);
            buttonLayout.Controls.Add(okButton, 3, 0);
            buttonLayout.Controls.Add(cancelButton, 4, 0);
            
            panel.Controls.Add(buttonLayout);
            return panel;
        }
        
        private void InitializeTemplateOptions()
        {
            try
            {
                // 获取所有可用的模板
                var templates = TemplateConfigManager.GetActiveTemplates();
                
                // 为每个点类型填充选项
                PopulateComboBox(aiComboBox, PointType.AI, templates);
                PopulateComboBox(aoComboBox, PointType.AO, templates);
                PopulateComboBox(diComboBox, PointType.DI, templates);
                PopulateComboBox(doComboBox, PointType.DO, templates);
                
                // 设置默认选择
                SetDefaultSelections();
                
                // 更新统计信息
                UpdateTemplateStatistics();
                
                statusLabel.Text = $"找到 {templates.Count} 个可用模板";
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"加载模板时出错: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }
        
        private void PopulateComboBox(ComboBox comboBox, PointType pointType, List<TemplateInfo> templates)
        {
            var pointTemplates = templates.Where(t => t.PointType == pointType).ToList();
            
            comboBox.Items.Clear();
            comboBox.DisplayMember = "Display";
            comboBox.ValueMember = "Version";
            
            foreach (var template in pointTemplates.OrderBy(t => t.Version))
            {
                var item = new
                {
                    Display = $"{template.Version} - {template.Name}",
                    Version = template.Version,
                    Template = template
                };
                comboBox.Items.Add(item);
            }
            
            // 设置默认选择
            if (_selectedTemplates.ContainsKey(pointType))
            {
                var selectedVersion = _selectedTemplates[pointType];
                for (int i = 0; i < comboBox.Items.Count; i++)
                {
                    var item = comboBox.Items[i];
                    if (item.GetType().GetProperty("Version")?.GetValue(item) is TemplateVersion version && 
                        version == selectedVersion)
                    {
                        comboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            else if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0; // 默认选择第一个
            }
        }
        
        private void SetDefaultSelections()
        {
            // 如果没有预设选择，使用默认模板
            foreach (PointType pointType in Enum.GetValues<PointType>())
            {
                if (!_selectedTemplates.ContainsKey(pointType))
                {
                    _selectedTemplates[pointType] = TemplateVersion.Default;
                }
            }
        }
        
        private void OnTemplateSelectionChanged(PointType pointType, ComboBox comboBox)
        {
            try
            {
                if (comboBox.SelectedItem != null)
                {
                    var selectedVersion = (TemplateVersion)comboBox.SelectedItem
                        .GetType().GetProperty("Version")!.GetValue(comboBox.SelectedItem)!;
                    var templateInfo = (TemplateInfo)comboBox.SelectedItem
                        .GetType().GetProperty("Template")!.GetValue(comboBox.SelectedItem)!;
                    
                    // 更新选择
                    _selectedTemplates[pointType] = selectedVersion;
                    
                    // 更新描述
                    UpdateTemplateDescription(pointType, templateInfo);
                    
                    // 更新状态
                    UpdateSelectionStatus();
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"选择模板时出错: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }
        
        private void UpdateTemplateDescription(PointType pointType, TemplateInfo templateInfo)
        {
            Label? descLabel = pointType switch
            {
                PointType.AI => aiDescLabel,
                PointType.AO => aoDescLabel,
                PointType.DI => diDescLabel,
                PointType.DO => doDescLabel,
                _ => null
            };
            
            if (descLabel != null)
            {
                var description = $"{templateInfo.Description}\n\n";
                description += $"作者: {templateInfo.Author}\n";
                description += $"创建时间: {templateInfo.CreatedDate:yyyy-MM-dd}\n";
                description += $"修改时间: {templateInfo.ModifiedDate:yyyy-MM-dd}\n";
                
                if (templateInfo.RequiredFields.Any())
                {
                    description += $"必需字段: {string.Join(", ", templateInfo.RequiredFields)}";
                }
                
                descLabel.Text = description;
                descLabel.ForeColor = Color.DarkGreen;
            }
        }
        
        private void UpdateTemplateStatistics()
        {
            var templates = TemplateConfigManager.GetActiveTemplates();
            
            foreach (PointType pointType in Enum.GetValues<PointType>())
            {
                var pointTemplates = templates.Where(t => t.PointType == pointType).ToList();
                var statsText = $"可用: {pointTemplates.Count} 个模板";
                
                GroupBox? groupBox = pointType switch
                {
                    PointType.AI => aiGroupBox,
                    PointType.AO => aoGroupBox,
                    PointType.DI => diGroupBox,
                    PointType.DO => doGroupBox,
                    _ => null
                };
                
                if (groupBox?.Tag is Label statsLabel)
                {
                    statsLabel.Text = statsText;
                }
            }
        }
        
        private void UpdateSelectionStatus()
        {
            var selectedCount = _selectedTemplates.Count;
            var totalCount = Enum.GetValues<PointType>().Length;
            
            if (selectedCount == totalCount)
            {
                statusLabel.Text = "所有点类型已选择模板，可以继续";
                statusLabel.ForeColor = Color.DarkGreen;
                okButton.Enabled = true;
            }
            else
            {
                statusLabel.Text = $"已选择 {selectedCount}/{totalCount} 个点类型的模板";
                statusLabel.ForeColor = Color.DarkBlue;
                okButton.Enabled = selectedCount > 0;
            }
        }
        
        private void OnCustomTemplatesToggled(object? sender, EventArgs e)
        {
            // 重新加载模板选项（包括或排除自定义模板）
            InitializeTemplateOptions();
            
            var message = enableCustomTemplatesCheckBox.Checked 
                ? "自定义模板已启用" 
                : "自定义模板已禁用";
            statusLabel.Text = message;
        }
        
        private void OnPreviewButtonClick(object? sender, EventArgs e)
        {
            try
            {
                // 创建模板预览对话框
                var previewForm = new TemplatePreviewForm(_selectedTemplates);
                previewForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"预览模板时出错: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void OnResetButtonClick(object? sender, EventArgs e)
        {
            // 重置为默认选择
            _selectedTemplates.Clear();
            foreach (PointType pointType in Enum.GetValues<PointType>())
            {
                _selectedTemplates[pointType] = TemplateVersion.Default;
            }
            
            // 重新初始化选项
            InitializeTemplateOptions();
            
            statusLabel.Text = "已重置为默认模板选择";
            statusLabel.ForeColor = Color.DarkBlue;
        }
        
        private void OnOkButtonClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        
        private void OnCancelButtonClick(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        
        private void ApplyTheme()
        {
            try
            {
                var isDarkTheme = ThemeManager.CurrentTheme == ThemeType.Dark;
                
                if (isDarkTheme)
                {
                    BackColor = Color.FromArgb(45, 45, 48);
                    ForeColor = Color.White;
                    statusPanel.BackColor = Color.FromArgb(37, 37, 38);
                }
                else
                {
                    BackColor = SystemColors.Control;
                    ForeColor = SystemColors.ControlText;
                    statusPanel.BackColor = Color.FromArgb(240, 248, 255);
                }
            }
            catch
            {
                // 主题应用失败时使用默认样式
            }
        }
        
        /// <summary>
        /// 获取选择摘要信息
        /// </summary>
        public string GetSelectionSummary()
        {
            if (!_selectedTemplates.Any())
                return "未选择任何模板";
            
            var summary = new List<string>();
            foreach (var kvp in _selectedTemplates)
            {
                summary.Add($"{kvp.Key}: {kvp.Value}");
            }
            
            return string.Join(", ", summary);
        }
    }
}
