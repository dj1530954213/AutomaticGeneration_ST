using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp1.Config;

namespace WinFormsApp1.Forms
{
    /// <summary>
    /// 配置重置选项对话框
    /// </summary>
    public partial class ConfigurationResetForm : Form
    {
        #region 私有字段

        private GroupBox resetOptionsGroupBox;
        private CheckBox resetGeneralCheckBox;
        private CheckBox resetTemplateCheckBox;
        private CheckBox resetPerformanceCheckBox;
        private CheckBox resetUICheckBox;
        private CheckBox resetExportCheckBox;
        private CheckBox resetAdvancedCheckBox;
        private CheckBox resetFieldMappingCheckBox;
        private CheckBox resetTemplateLibraryCheckBox;
        private CheckBox resetWindowSettingsCheckBox;
        
        private GroupBox backupOptionsGroupBox;
        private CheckBox createBackupCheckBox;
        private TextBox backupDescriptionTextBox;
        private Label backupDescriptionLabel;
        
        private Button okButton;
        private Button cancelButton;
        private Button selectAllButton;
        private Button selectNoneButton;
        private Button selectRecommendedButton;
        
        private Label warningLabel;

        #endregion

        #region 构造函数

        public ConfigurationResetForm()
        {
            InitializeComponent();
            SetupDefaultSelections();
        }

        #endregion

        #region 初始化方法

        private void InitializeComponent()
        {
            Text = "重置配置选项";
            Size = new Size(480, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("微软雅黑", 9F);

            CreateResetOptionsGroup();
            CreateBackupOptionsGroup();
            CreateButtonsPanel();
            CreateWarningLabel();

            SetupLayout();
        }

        private void CreateResetOptionsGroup()
        {
            resetOptionsGroupBox = new GroupBox
            {
                Text = "重置选项",
                Location = new Point(12, 12),
                Size = new Size(440, 240),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };

            // 第一列
            resetGeneralCheckBox = CreateCheckBox("通用配置", new Point(15, 25), 
                "应用程序标题、语言、自动保存等基本设置");
            resetTemplateCheckBox = CreateCheckBox("模板配置", new Point(15, 50), 
                "模板目录、验证、编辑器设置等");
            resetPerformanceCheckBox = CreateCheckBox("性能配置", new Point(15, 75), 
                "缓存、线程数、内存管理等性能设置");
            resetUICheckBox = CreateCheckBox("界面配置", new Point(15, 100), 
                "主题、字体、窗口布局等界面设置");
            resetExportCheckBox = CreateCheckBox("导出配置", new Point(15, 125), 
                "导出格式、编码、路径等导出设置");

            // 第二列
            resetAdvancedCheckBox = CreateCheckBox("高级配置", new Point(230, 25), 
                "调试模式、日志、插件等高级设置");
            resetFieldMappingCheckBox = CreateCheckBox("字段映射配置", new Point(230, 50), 
                "Excel列名映射和别名设置");
            resetTemplateLibraryCheckBox = CreateCheckBox("模板库配置", new Point(230, 75), 
                "模板收藏、分类、评分等设置");
            resetWindowSettingsCheckBox = CreateCheckBox("窗口设置", new Point(230, 100), 
                "窗口位置、大小、状态等设置");

            // 选择按钮
            selectAllButton = new Button
            {
                Text = "全选",
                Location = new Point(15, 160),
                Size = new Size(60, 28),
                Font = new Font("微软雅黑", 8F)
            };
            selectAllButton.Click += OnSelectAll;

            selectNoneButton = new Button
            {
                Text = "全不选",
                Location = new Point(85, 160),
                Size = new Size(60, 28),
                Font = new Font("微软雅黑", 8F)
            };
            selectNoneButton.Click += OnSelectNone;

            selectRecommendedButton = new Button
            {
                Text = "推荐选择",
                Location = new Point(155, 160),
                Size = new Size(80, 28),
                Font = new Font("微软雅黑", 8F)
            };
            selectRecommendedButton.Click += OnSelectRecommended;

            resetOptionsGroupBox.Controls.AddRange(new Control[]
            {
                resetGeneralCheckBox, resetTemplateCheckBox, resetPerformanceCheckBox,
                resetUICheckBox, resetExportCheckBox, resetAdvancedCheckBox,
                resetFieldMappingCheckBox, resetTemplateLibraryCheckBox, resetWindowSettingsCheckBox,
                selectAllButton, selectNoneButton, selectRecommendedButton
            });
        }

        private CheckBox CreateCheckBox(string text, Point location, string tooltip)
        {
            var checkBox = new CheckBox
            {
                Text = text,
                Location = location,
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 8F),
                AutoSize = false
            };

            // 添加工具提示
            var toolTip = new ToolTip();
            toolTip.SetToolTip(checkBox, tooltip);

            return checkBox;
        }

        private void CreateBackupOptionsGroup()
        {
            backupOptionsGroupBox = new GroupBox
            {
                Text = "备份选项",
                Location = new Point(12, 260),
                Size = new Size(440, 100),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };

            createBackupCheckBox = new CheckBox
            {
                Text = "重置前创建备份",
                Location = new Point(15, 25),
                Size = new Size(150, 20),
                Font = new Font("微软雅黑", 8F),
                Checked = true
            };
            createBackupCheckBox.CheckedChanged += OnCreateBackupChanged;

            backupDescriptionLabel = new Label
            {
                Text = "备份描述：",
                Location = new Point(15, 55),
                Size = new Size(80, 20),
                Font = new Font("微软雅黑", 8F),
                TextAlign = ContentAlignment.MiddleLeft
            };

            backupDescriptionTextBox = new TextBox
            {
                Location = new Point(100, 52),
                Size = new Size(320, 23),
                Font = new Font("微软雅黑", 8F),
                Text = $"配置重置备份 - {DateTime.Now:yyyy-MM-dd HH:mm}"
            };

            backupOptionsGroupBox.Controls.AddRange(new Control[]
            {
                createBackupCheckBox, backupDescriptionLabel, backupDescriptionTextBox
            });
        }

        private void CreateButtonsPanel()
        {
            okButton = new Button
            {
                Text = "确定重置",
                Location = new Point(295, 430),
                Size = new Size(80, 32),
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            okButton.FlatAppearance.BorderSize = 0;

            cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(385, 430),
                Size = new Size(70, 32),
                Font = new Font("微软雅黑", 9F),
                DialogResult = DialogResult.Cancel
            };
        }

        private void CreateWarningLabel()
        {
            warningLabel = new Label
            {
                Text = "⚠️ 警告：重置操作将覆盖当前配置，请确保已创建备份！",
                Location = new Point(12, 375),
                Size = new Size(440, 40),
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.FromArgb(220, 53, 69),
                TextAlign = ContentAlignment.MiddleLeft,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(8)
            };
        }

        private void SetupLayout()
        {
            Controls.AddRange(new Control[]
            {
                resetOptionsGroupBox, backupOptionsGroupBox, 
                warningLabel, okButton, cancelButton
            });
        }

        #endregion

        #region 事件处理

        private void OnSelectAll(object? sender, EventArgs e)
        {
            SetAllCheckBoxes(true);
        }

        private void OnSelectNone(object? sender, EventArgs e)
        {
            SetAllCheckBoxes(false);
        }

        private void OnSelectRecommended(object? sender, EventArgs e)
        {
            resetGeneralCheckBox.Checked = true;
            resetTemplateCheckBox.Checked = true;
            resetPerformanceCheckBox.Checked = true;
            resetUICheckBox.Checked = true;
            resetExportCheckBox.Checked = true;
            resetAdvancedCheckBox.Checked = false;
            resetFieldMappingCheckBox.Checked = false;
            resetTemplateLibraryCheckBox.Checked = false;
            resetWindowSettingsCheckBox.Checked = false;
        }

        private void OnCreateBackupChanged(object? sender, EventArgs e)
        {
            backupDescriptionLabel.Enabled = createBackupCheckBox.Checked;
            backupDescriptionTextBox.Enabled = createBackupCheckBox.Checked;
        }

        private void SetAllCheckBoxes(bool checked_)
        {
            resetGeneralCheckBox.Checked = checked_;
            resetTemplateCheckBox.Checked = checked_;
            resetPerformanceCheckBox.Checked = checked_;
            resetUICheckBox.Checked = checked_;
            resetExportCheckBox.Checked = checked_;
            resetAdvancedCheckBox.Checked = checked_;
            resetFieldMappingCheckBox.Checked = checked_;
            resetTemplateLibraryCheckBox.Checked = checked_;
            resetWindowSettingsCheckBox.Checked = checked_;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取重置选项
        /// </summary>
        public ConfigurationResetManager.ResetOptions GetResetOptions()
        {
            return new ConfigurationResetManager.ResetOptions
            {
                ResetGeneral = resetGeneralCheckBox.Checked,
                ResetTemplate = resetTemplateCheckBox.Checked,
                ResetPerformance = resetPerformanceCheckBox.Checked,
                ResetUI = resetUICheckBox.Checked,
                ResetExport = resetExportCheckBox.Checked,
                ResetAdvanced = resetAdvancedCheckBox.Checked,
                ResetFieldMapping = resetFieldMappingCheckBox.Checked,
                ResetTemplateLibrary = resetTemplateLibraryCheckBox.Checked,
                ResetWindowSettings = resetWindowSettingsCheckBox.Checked,
                CreateBackup = createBackupCheckBox.Checked,
                BackupDescription = backupDescriptionTextBox.Text.Trim()
            };
        }

        /// <summary>
        /// 设置默认选择
        /// </summary>
        private void SetupDefaultSelections()
        {
            // 默认选择推荐项
            OnSelectRecommended(null, EventArgs.Empty);
        }

        #endregion
    }
}