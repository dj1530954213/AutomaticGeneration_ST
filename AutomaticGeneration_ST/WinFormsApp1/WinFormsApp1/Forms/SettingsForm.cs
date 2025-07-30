using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1.Forms
{
    public partial class SettingsForm : Form
    {
        private TabControl settingsTabControl;
        private Button okButton;
        private Button cancelButton;
        private Button applyButton;
        
        public SettingsForm()
        {
            InitializeComponent();
            InitializeSettings();
        }
        
        private void InitializeComponent()
        {
            this.Text = "设置";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            
            // 创建主要控件
            settingsTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Location = new Point(10, 10),
                Size = new Size(560, 400)
            };
            
            // 创建按钮面板
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };
            
            okButton = new Button
            {
                Text = "确定",
                Size = new Size(75, 30),
                Location = new Point(350, 10),
                DialogResult = DialogResult.OK
            };
            okButton.Click += OkButton_Click;
            
            cancelButton = new Button
            {
                Text = "取消",
                Size = new Size(75, 30),
                Location = new Point(435, 10),
                DialogResult = DialogResult.Cancel
            };
            
            applyButton = new Button
            {
                Text = "应用",
                Size = new Size(75, 30),
                Location = new Point(520, 10)
            };
            applyButton.Click += ApplyButton_Click;
            
            buttonPanel.Controls.AddRange(new Control[] { okButton, cancelButton, applyButton });
            
            this.Controls.Add(settingsTabControl);
            this.Controls.Add(buttonPanel);
            
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void InitializeSettings()
        {
            // 创建常规设置标签页
            CreateGeneralTab();
            
            // 创建模板设置标签页
            CreateTemplateTab();
            
            // 创建外观设置标签页
            CreateAppearanceTab();
            
            // 创建高级设置标签页
            CreateAdvancedTab();
        }
        
        private void CreateGeneralTab()
        {
            var generalTab = new TabPage("常规");
            
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // 最近文件数量设置
            var recentFilesLabel = new Label
            {
                Text = "最近文件保存数量:",
                Location = new Point(10, 20),
                Size = new Size(150, 23)
            };
            
            var recentFilesNumeric = new NumericUpDown
            {
                Minimum = 5,
                Maximum = 20,
                Value = 10,
                Location = new Point(170, 20),
                Size = new Size(60, 23)
            };
            
            // 自动保存设置
            var autoSaveCheckBox = new CheckBox
            {
                Text = "启用自动保存配置",
                Location = new Point(10, 60),
                Size = new Size(200, 23),
                Checked = true
            };
            
            // 启动时加载最近文件
            var loadRecentCheckBox = new CheckBox
            {
                Text = "启动时显示最近文件",
                Location = new Point(10, 100),
                Size = new Size(200, 23),
                Checked = true
            };
            
            panel.Controls.AddRange(new Control[] 
            {
                recentFilesLabel, recentFilesNumeric,
                autoSaveCheckBox, loadRecentCheckBox
            });
            
            generalTab.Controls.Add(panel);
            settingsTabControl.TabPages.Add(generalTab);
        }
        
        private void CreateTemplateTab()
        {
            var templateTab = new TabPage("模板");
            
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // 模板路径设置
            var templatePathLabel = new Label
            {
                Text = "模板文件夹路径:",
                Location = new Point(10, 20),
                Size = new Size(150, 23)
            };
            
            var templatePathTextBox = new TextBox
            {
                Text = "Templates/",
                Location = new Point(10, 45),
                Size = new Size(300, 23),
                ReadOnly = true
            };
            
            var browseButton = new Button
            {
                Text = "浏览...",
                Location = new Point(320, 45),
                Size = new Size(75, 23)
            };
            
            // 默认模板版本
            var defaultVersionLabel = new Label
            {
                Text = "默认模板版本:",
                Location = new Point(10, 90),
                Size = new Size(150, 23)
            };
            
            var defaultVersionComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(170, 90),
                Size = new Size(100, 23)
            };
            defaultVersionComboBox.Items.AddRange(new[] { "default", "advanced", "custom" });
            defaultVersionComboBox.SelectedIndex = 0;
            
            panel.Controls.AddRange(new Control[] 
            {
                templatePathLabel, templatePathTextBox, browseButton,
                defaultVersionLabel, defaultVersionComboBox
            });
            
            templateTab.Controls.Add(panel);
            settingsTabControl.TabPages.Add(templateTab);
        }
        
        private void CreateAppearanceTab()
        {
            var appearanceTab = new TabPage("外观");
            
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // 主题设置
            var themeLabel = new Label
            {
                Text = "主题样式:",
                Location = new Point(10, 20),
                Size = new Size(150, 23)
            };
            
            var themeComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(170, 20),
                Size = new Size(120, 23)
            };
            themeComboBox.Items.AddRange(new[] { "浅色主题", "深色主题", "跟随系统" });
            themeComboBox.SelectedIndex = 0;
            
            // 字体设置
            var fontLabel = new Label
            {
                Text = "界面字体:",
                Location = new Point(10, 60),
                Size = new Size(150, 23)
            };
            
            var fontButton = new Button
            {
                Text = "微软雅黑, 11pt",
                Location = new Point(170, 60),
                Size = new Size(150, 23)
            };
            
            // 代码字体设置
            var codeFontLabel = new Label
            {
                Text = "代码字体:",
                Location = new Point(10, 100),
                Size = new Size(150, 23)
            };
            
            var codeFontButton = new Button
            {
                Text = "Consolas, 10pt",
                Location = new Point(170, 100),
                Size = new Size(150, 23)
            };
            
            panel.Controls.AddRange(new Control[] 
            {
                themeLabel, themeComboBox,
                fontLabel, fontButton,
                codeFontLabel, codeFontButton
            });
            
            appearanceTab.Controls.Add(panel);
            settingsTabControl.TabPages.Add(appearanceTab);
        }
        
        private void CreateAdvancedTab()
        {
            var advancedTab = new TabPage("高级");
            
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            // 日志级别设置
            var logLevelLabel = new Label
            {
                Text = "日志级别:",
                Location = new Point(10, 20),
                Size = new Size(150, 23)
            };
            
            var logLevelComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(170, 20),
                Size = new Size(100, 23)
            };
            logLevelComboBox.Items.AddRange(new[] { "全部", "信息", "警告", "错误" });
            logLevelComboBox.SelectedIndex = 0;
            
            // 性能设置
            var performanceGroupBox = new GroupBox
            {
                Text = "性能设置",
                Location = new Point(10, 60),
                Size = new Size(400, 120)
            };
            
            var enableCacheCheckBox = new CheckBox
            {
                Text = "启用模板缓存",
                Location = new Point(15, 25),
                Size = new Size(200, 23),
                Checked = true
            };
            
            var maxMemoryLabel = new Label
            {
                Text = "最大内存使用 (MB):",
                Location = new Point(15, 55),
                Size = new Size(150, 23)
            };
            
            var maxMemoryNumeric = new NumericUpDown
            {
                Minimum = 100,
                Maximum = 2048,
                Value = 512,
                Location = new Point(175, 55),
                Size = new Size(80, 23)
            };
            
            performanceGroupBox.Controls.AddRange(new Control[] 
            {
                enableCacheCheckBox, maxMemoryLabel, maxMemoryNumeric
            });
            
            panel.Controls.AddRange(new Control[] 
            {
                logLevelLabel, logLevelComboBox, performanceGroupBox
            });
            
            advancedTab.Controls.Add(panel);
            settingsTabControl.TabPages.Add(advancedTab);
        }
        
        private void OkButton_Click(object sender, EventArgs e)
        {
            ApplySettings();
            this.Close();
        }
        
        private void ApplyButton_Click(object sender, EventArgs e)
        {
            ApplySettings();
        }
        
        private void ApplySettings()
        {
            // 这里将实现设置的应用逻辑
            // 在后续步骤中会完善
            MessageBox.Show("设置已应用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}