////NEED DELETE: 配置向导（非核心流程），与导入-生成-导出主链路无关
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using WinFormsApp1.Config;

//namespace WinFormsApp1.Forms
//{
//    /// <summary>
//    /// 配置向导窗体 - 帮助用户进行初始配置
//    /// </summary>
//    public partial class ConfigurationWizardForm : Form
//    {
//        #region 私有字段

//        private Panel sidebarPanel;
//        private Panel contentPanel;
//        private Panel buttonPanel;
        
//        private ListBox stepListBox;
//        private Button prevButton;
//        private Button nextButton;
//        private Button finishButton;
//        private Button cancelButton;
        
//        private int currentStepIndex = 0;
//        private List<WizardStep> wizardSteps;
//        private Dictionary<string, object> wizardData = new();

//        #endregion

//        #region 构造函数

//        public ConfigurationWizardForm()
//        {
//            InitializeComponent();
//            InitializeWizardSteps();
//            ShowCurrentStep();
//        }

//        #endregion

//        #region 初始化方法

//        private void InitializeComponent()
//        {
//            Text = "ST自动生成器 - 初始配置向导";
//            Size = new Size(800, 600);
//            StartPosition = FormStartPosition.CenterScreen;
//            FormBorderStyle = FormBorderStyle.FixedDialog;
//            MaximizeBox = false;
//            MinimizeBox = false;
//            Font = new Font("微软雅黑", 9F);

//            CreateSidebarPanel();
//            CreateContentPanel();
//            CreateButtonPanel();
            
//            SetupLayout();
//        }

//        private void CreateSidebarPanel()
//        {
//            sidebarPanel = new Panel
//            {
//                Dock = DockStyle.Left,
//                Width = 200,
//                BackColor = Color.FromArgb(245, 245, 245),
//                Padding = new Padding(10)
//            };

//            var titleLabel = new Label
//            {
//                Text = "配置步骤",
//                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
//                ForeColor = Color.FromArgb(50, 50, 50),
//                AutoSize = true,
//                Location = new Point(10, 10)
//            };

//            stepListBox = new ListBox
//            {
//                Location = new Point(10, 40),
//                Size = new Size(180, 400),
//                Font = new Font("微软雅黑", 8F),
//                BorderStyle = BorderStyle.None,
//                BackColor = Color.FromArgb(245, 245, 245),
//                SelectionMode = SelectionMode.None
//            };

//            sidebarPanel.Controls.AddRange(new Control[] { titleLabel, stepListBox });
//        }

//        private void CreateContentPanel()
//        {
//            contentPanel = new Panel
//            {
//                Dock = DockStyle.Fill,
//                Padding = new Padding(20),
//                BackColor = Color.White
//            };
//        }

//        private void CreateButtonPanel()
//        {
//            buttonPanel = new Panel
//            {
//                Dock = DockStyle.Bottom,
//                Height = 60,
//                BackColor = Color.FromArgb(248, 249, 250),
//                Padding = new Padding(20, 15, 20, 15)
//            };

//            prevButton = new Button
//            {
//                Text = "< 上一步",
//                Size = new Size(80, 30),
//                Location = new Point(480, 15),
//                Font = new Font("微软雅黑", 8F),
//                Enabled = false
//            };
//            prevButton.Click += OnPrevious;

//            nextButton = new Button
//            {
//                Text = "下一步 >",
//                Size = new Size(80, 30),
//                Location = new Point(570, 15),
//                Font = new Font("微软雅黑", 8F)
//            };
//            nextButton.Click += OnNext;

//            finishButton = new Button
//            {
//                Text = "完成",
//                Size = new Size(80, 30),
//                Location = new Point(660, 15),
//                Font = new Font("微软雅黑", 8F),
//                BackColor = Color.FromArgb(40, 167, 69),
//                ForeColor = Color.White,
//                FlatStyle = FlatStyle.Flat,
//                Visible = false
//            };
//            finishButton.FlatAppearance.BorderSize = 0;
//            finishButton.Click += OnFinish;

//            cancelButton = new Button
//            {
//                Text = "取消",
//                Size = new Size(80, 30),
//                Location = new Point(20, 15),
//                Font = new Font("微软雅黑", 8F),
//                DialogResult = DialogResult.Cancel
//            };

//            buttonPanel.Controls.AddRange(new Control[] 
//            { 
//                prevButton, nextButton, finishButton, cancelButton 
//            });
//        }

//        private void SetupLayout()
//        {
//            Controls.AddRange(new Control[] 
//            { 
//                contentPanel, sidebarPanel, buttonPanel 
//            });
//        }

//        private void InitializeWizardSteps()
//        {
//            wizardSteps = new List<WizardStep>
//            {
//                new WelcomeStep(),
//                new GeneralSettingsStep(),
//                new TemplateSettingsStep(),
//                new UISettingsStep(),
//                new ExportSettingsStep(),
//                new FieldMappingStep(),
//                new SummaryStep()
//            };

//            // 填充步骤列表
//            foreach (var step in wizardSteps)
//            {
//                stepListBox.Items.Add($"  {step.Title}");
//            }
//        }

//        #endregion

//        #region 导航方法

//        private void ShowCurrentStep()
//        {
//            if (currentStepIndex >= 0 && currentStepIndex < wizardSteps.Count)
//            {
//                var step = wizardSteps[currentStepIndex];
                
//                // 清空内容面板
//                contentPanel.Controls.Clear();
                
//                // 创建步骤内容
//                var stepControl = step.CreateStepControl(wizardData);
//                stepControl.Dock = DockStyle.Fill;
//                contentPanel.Controls.Add(stepControl);
                
//                // 更新步骤列表高亮
//                UpdateStepListHighlight();
                
//                // 更新按钮状态
//                UpdateButtonStates();
//            }
//        }

//        private void UpdateStepListHighlight()
//        {
//            stepListBox.ClearSelected();
            
//            // 重绘列表项以显示当前步骤
//            stepListBox.Invalidate();
//        }

//        private void UpdateButtonStates()
//        {
//            prevButton.Enabled = currentStepIndex > 0;
            
//            bool isLastStep = currentStepIndex == wizardSteps.Count - 1;
//            nextButton.Visible = !isLastStep;
//            finishButton.Visible = isLastStep;
//        }

//        #endregion

//        #region 事件处理

//        private void OnPrevious(object? sender, EventArgs e)
//        {
//            if (currentStepIndex > 0)
//            {
//                // 保存当前步骤数据
//                var currentStep = wizardSteps[currentStepIndex];
//                currentStep.SaveStepData(contentPanel, wizardData);
                
//                currentStepIndex--;
//                ShowCurrentStep();
//            }
//        }

//        private void OnNext(object? sender, EventArgs e)
//        {
//            var currentStep = wizardSteps[currentStepIndex];
            
//            // 验证当前步骤
//            var validationResult = currentStep.ValidateStep(contentPanel, wizardData);
//            if (!validationResult.IsValid)
//            {
//                MessageBox.Show(validationResult.ErrorMessage, "验证失败", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return;
//            }
            
//            // 保存当前步骤数据
//            currentStep.SaveStepData(contentPanel, wizardData);
            
//            if (currentStepIndex < wizardSteps.Count - 1)
//            {
//                currentStepIndex++;
//                ShowCurrentStep();
//            }
//        }

//        private async void OnFinish(object? sender, EventArgs e)
//        {
//            try
//            {
//                // 保存最后一步的数据
//                var currentStep = wizardSteps[currentStepIndex];
//                currentStep.SaveStepData(contentPanel, wizardData);
                
//                finishButton.Enabled = false;
//                finishButton.Text = "正在保存...";
                
//                // 应用所有配置
//                await ApplyWizardConfiguration();
                
//                MessageBox.Show("配置已成功保存！", "配置完成", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
//                DialogResult = DialogResult.OK;
//                Close();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"保存配置时出错：{ex.Message}", "错误", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
//                finishButton.Enabled = true;
//                finishButton.Text = "完成";
//            }
//        }

//        #endregion

//        #region 配置应用

//        private async Task ApplyWizardConfiguration()
//        {
//            var config = ConfigurationManager.Current;
            
//            // 应用通用设置
//            if (wizardData.ContainsKey("ApplicationTitle"))
//                config.General.ApplicationTitle = wizardData["ApplicationTitle"].ToString() ?? "";
//            if (wizardData.ContainsKey("DefaultLanguage"))
//                config.General.DefaultLanguage = wizardData["DefaultLanguage"].ToString() ?? "zh-CN";
//            if (wizardData.ContainsKey("AutoSaveInterval"))
//                config.General.AutoSaveInterval = (int)(wizardData["AutoSaveInterval"] ?? 5);
//            if (wizardData.ContainsKey("EnableAutoBackup"))
//                config.General.EnableAutoBackup = (bool)(wizardData["EnableAutoBackup"] ?? true);
            
//            // 应用模板设置
//            if (wizardData.ContainsKey("TemplateDirectory"))
//                config.Template.TemplateDirectory = wizardData["TemplateDirectory"].ToString() ?? "Templates";
//            if (wizardData.ContainsKey("EnableTemplateValidation"))
//                config.Template.EnableTemplateValidation = (bool)(wizardData["EnableTemplateValidation"] ?? true);
//            if (wizardData.ContainsKey("AutoFormatTemplate"))
//                config.Template.AutoFormatTemplate = (bool)(wizardData["AutoFormatTemplate"] ?? true);
            
//            // 应用UI设置
//            if (wizardData.ContainsKey("Theme"))
//                config.UI.Theme = wizardData["Theme"].ToString() ?? "Light";
//            if (wizardData.ContainsKey("FontFamily"))
//                config.UI.FontFamily = wizardData["FontFamily"].ToString() ?? "微软雅黑";
//            if (wizardData.ContainsKey("FontSize"))
//                config.UI.FontSize = (int)(wizardData["FontSize"] ?? 10);
            
//            // 应用导出设置
//            if (wizardData.ContainsKey("DefaultExportFormat"))
//                config.Export.DefaultExportFormat = wizardData["DefaultExportFormat"].ToString() ?? "ST";
//            if (wizardData.ContainsKey("DefaultExportPath"))
//                config.Export.DefaultExportPath = wizardData["DefaultExportPath"].ToString() ?? "";
//            if (wizardData.ContainsKey("FileEncoding"))
//                config.Export.FileEncoding = wizardData["FileEncoding"].ToString() ?? "UTF-8";
            
//            // 保存主配置
//            await ConfigurationManager.SaveConfigurationAsync();
            
//            // 保存字段映射配置
//            if (wizardData.ContainsKey("FieldMappings"))
//            {
//                var fieldMappings = (List<FieldMappingConfiguration.FieldMapping>)wizardData["FieldMappings"];
//                var mappingCollection = FieldMappingConfiguration.CurrentMapping;
//                mappingCollection.FieldMappings = fieldMappings;
//                FieldMappingConfiguration.SaveMapping(mappingCollection);
//            }
//        }

//        #endregion

//        #region 公共方法

//        /// <summary>
//        /// 检查是否需要显示配置向导
//        /// </summary>
//        public static bool ShouldShowWizard()
//        {
//            try
//            {
//                var configPath = ConfigurationManager.ConfigFilePath;
//                var wizardFlagPath = Path.Combine(
//                    Path.GetDirectoryName(configPath) ?? "",
//                    ".wizard-completed");
                
//                return !File.Exists(configPath) || !File.Exists(wizardFlagPath);
//            }
//            catch
//            {
//                return true;
//            }
//        }

//        /// <summary>
//        /// 标记向导已完成
//        /// </summary>
//        public static void MarkWizardCompleted()
//        {
//            try
//            {
//                var configPath = ConfigurationManager.ConfigFilePath;
//                var wizardFlagPath = Path.Combine(  
//                    Path.GetDirectoryName(configPath) ?? "",
//                    ".wizard-completed");
                
//                var directory = Path.GetDirectoryName(wizardFlagPath);
//                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }
                
//                File.WriteAllText(wizardFlagPath, DateTime.Now.ToString());
//            }
//            catch
//            {
//                // 忽略错误
//            }
//        }

//        #endregion

//        #region 重写方法

//        protected override void OnFormClosed(FormClosedEventArgs e)
//        {
//            if (DialogResult == DialogResult.OK)
//            {
//                MarkWizardCompleted();
//            }
            
//            base.OnFormClosed(e);
//        }

//        #endregion
//    }

//    #region 向导步骤基类和实现

//    /// <summary>
//    /// 向导步骤基类
//    /// </summary>
//    public abstract class WizardStep
//    {
//        public abstract string Title { get; }
//        public abstract string Description { get; }
        
//        public abstract Control CreateStepControl(Dictionary<string, object> wizardData);
//        public abstract ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData);
//        public abstract void SaveStepData(Control stepControl, Dictionary<string, object> wizardData);
        
//        public class ValidationResult
//        {
//            public bool IsValid { get; set; } = true;
//            public string ErrorMessage { get; set; } = "";
//        }
//    }

//    /// <summary>
//    /// 欢迎步骤
//    /// </summary>
//    public class WelcomeStep : WizardStep
//    {
//        public override string Title => "欢迎";
//        public override string Description => "欢迎使用ST自动生成器配置向导";

//        public override Control CreateStepControl(Dictionary<string, object> wizardData)
//        {
//            var panel = new Panel();
            
//            var titleLabel = new Label
//            {
//                Text = "欢迎使用ST自动生成器！",
//                Font = new Font("微软雅黑", 16F, FontStyle.Bold),
//                ForeColor = Color.FromArgb(40, 167, 69),
//                Location = new Point(20, 20),
//                Size = new Size(500, 40)
//            };
            
//            var descLabel = new Label
//            {
//                Text = "此向导将帮助您完成初始配置，确保系统能够正常工作。\n\n" +
//                       "您将配置以下内容：\n" +
//                       "• 基本应用程序设置\n" +
//                       "• 模板系统配置\n" +
//                       "• 用户界面偏好\n" +
//                       "• 文件导出选项\n" +
//                       "• Excel字段映射规则\n\n" +
//                       "点击\"下一步\"开始配置。",
//                Font = new Font("微软雅黑", 10F),
//                Location = new Point(20, 80),
//                Size = new Size(500, 300),
//                ForeColor = Color.FromArgb(73, 80, 87)
//            };
            
//            panel.Controls.AddRange(new Control[] { titleLabel, descLabel });
//            return panel;
//        }

//        public override ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            return new ValidationResult { IsValid = true };
//        }

//        public override void SaveStepData(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            // 欢迎步骤无需保存数据
//        }
//    }

//    /// <summary>
//    /// 通用设置步骤
//    /// </summary>
//    public class GeneralSettingsStep : WizardStep
//    {
//        public override string Title => "通用设置";
//        public override string Description => "配置应用程序的基本设置";

//        public override Control CreateStepControl(Dictionary<string, object> wizardData)
//        {
//            var panel = new Panel();
            
//            // 应用程序标题
//            var titleLabel = new Label
//            {
//                Text = "应用程序标题：",
//                Location = new Point(20, 20),
//                Size = new Size(120, 23),
//                Font = new Font("微软雅黑", 9F)
//            };
            
//            var titleTextBox = new TextBox
//            {
//                Name = "ApplicationTitle",
//                Text = wizardData.GetValueOrDefault("ApplicationTitle", "ST脚本自动生成器 v2.0").ToString(),
//                Location = new Point(150, 20),
//                Size = new Size(300, 23),
//                Font = new Font("微软雅黑", 9F)
//            };
            
//            // 默认语言
//            var languageLabel = new Label
//            {
//                Text = "默认语言：",
//                Location = new Point(20, 60),
//                Size = new Size(120, 23),
//                Font = new Font("微软雅黑", 9F)
//            };
            
//            var languageComboBox = new ComboBox
//            {
//                Name = "DefaultLanguage",
//                DropDownStyle = ComboBoxStyle.DropDownList,
//                Location = new Point(150, 60),
//                Size = new Size(300, 23),
//                Font = new Font("微软雅黑", 9F)
//            };
//            languageComboBox.Items.AddRange(new[] { "zh-CN", "en-US" });
//            languageComboBox.SelectedItem = wizardData.GetValueOrDefault("DefaultLanguage", "zh-CN");
            
//            // 自动保存间隔
//            var autoSaveLabel = new Label
//            {
//                Text = "自动保存间隔（分钟）：",
//                Location = new Point(20, 100),
//                Size = new Size(150, 23),
//                Font = new Font("微软雅黑", 9F)
//            };
            
//            var autoSaveNumeric = new NumericUpDown
//            {
//                Name = "AutoSaveInterval",
//                Minimum = 1,
//                Maximum = 60,
//                Value = (decimal)wizardData.GetValueOrDefault("AutoSaveInterval", 5),
//                Location = new Point(180, 100),
//                Size = new Size(100, 23),
//                Font = new Font("微软雅黑", 9F)
//            };
            
//            // 启用自动备份
//            var autoBackupCheckBox = new CheckBox
//            {
//                Name = "EnableAutoBackup",
//                Text = "启用自动备份",
//                Checked = (bool)wizardData.GetValueOrDefault("EnableAutoBackup", true),
//                Location = new Point(20, 140),
//                Size = new Size(200, 23),
//                Font = new Font("微软雅黑", 9F)
//            };
            
//            panel.Controls.AddRange(new Control[] 
//            { 
//                titleLabel, titleTextBox, languageLabel, languageComboBox,
//                autoSaveLabel, autoSaveNumeric, autoBackupCheckBox
//            });
            
//            return panel;
//        }

//        public override ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            var titleTextBox = stepControl.Controls.OfType<TextBox>().FirstOrDefault(c => c.Name == "ApplicationTitle");
//            if (titleTextBox != null && string.IsNullOrWhiteSpace(titleTextBox.Text))
//            {
//                return new ValidationResult
//                {
//                    IsValid = false,
//                    ErrorMessage = "请输入应用程序标题"
//                };
//            }
            
//            return new ValidationResult { IsValid = true };
//        }

//        public override void SaveStepData(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            var titleTextBox = stepControl.Controls.OfType<TextBox>().FirstOrDefault(c => c.Name == "ApplicationTitle");
//            if (titleTextBox != null)
//                wizardData["ApplicationTitle"] = titleTextBox.Text;
                
//            var languageComboBox = stepControl.Controls.OfType<ComboBox>().FirstOrDefault(c => c.Name == "DefaultLanguage");
//            if (languageComboBox != null)
//                wizardData["DefaultLanguage"] = languageComboBox.SelectedItem?.ToString() ?? "zh-CN";
                
//            var autoSaveNumeric = stepControl.Controls.OfType<NumericUpDown>().FirstOrDefault(c => c.Name == "AutoSaveInterval");
//            if (autoSaveNumeric != null)
//                wizardData["AutoSaveInterval"] = (int)autoSaveNumeric.Value;
                
//            var autoBackupCheckBox = stepControl.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Name == "EnableAutoBackup");
//            if (autoBackupCheckBox != null)
//                wizardData["EnableAutoBackup"] = autoBackupCheckBox.Checked;
//        }
//    }

//    // 其他步骤类的实现类似，这里简化处理
//    public class TemplateSettingsStep : WizardStep
//    {
//        public override string Title => "模板设置";
//        public override string Description => "配置模板系统相关设置";
        
//        public override Control CreateStepControl(Dictionary<string, object> wizardData)
//        {
//            var panel = new Panel();
//            var label = new Label
//            {
//                Text = "模板设置将在此配置...",
//                Location = new Point(20, 20),
//                Size = new Size(400, 200),
//                Font = new Font("微软雅黑", 10F)
//            };
//            panel.Controls.Add(label);
//            return panel;
//        }
        
//        public override ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            return new ValidationResult { IsValid = true };
//        }
        
//        public override void SaveStepData(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            // 实现模板设置数据保存
//        }
//    }

//    public class UISettingsStep : WizardStep
//    {
//        public override string Title => "界面设置";
//        public override string Description => "配置用户界面相关设置";
        
//        public override Control CreateStepControl(Dictionary<string, object> wizardData)
//        {
//            var panel = new Panel();
//            var label = new Label
//            {
//                Text = "界面设置将在此配置...",
//                Location = new Point(20, 20),
//                Size = new Size(400, 200),
//                Font = new Font("微软雅黑", 10F)
//            };
//            panel.Controls.Add(label);
//            return panel;
//        }
        
//        public override ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            return new ValidationResult { IsValid = true };
//        }
        
//        public override void SaveStepData(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            // 实现UI设置数据保存
//        }
//    }

//    public class ExportSettingsStep : WizardStep
//    {
//        public override string Title => "导出设置";
//        public override string Description => "配置文件导出相关设置";
        
//        public override Control CreateStepControl(Dictionary<string, object> wizardData)
//        {
//            var panel = new Panel();
//            var label = new Label
//            {
//                Text = "导出设置将在此配置...",
//                Location = new Point(20, 20),
//                Size = new Size(400, 200),
//                Font = new Font("微软雅黑", 10F)
//            };
//            panel.Controls.Add(label);
//            return panel;
//        }
        
//        public override ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            return new ValidationResult { IsValid = true };
//        }
        
//        public override void SaveStepData(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            // 实现导出设置数据保存
//        }
//    }

//    public class FieldMappingStep : WizardStep
//    {
//        public override string Title => "字段映射";
//        public override string Description => "配置Excel字段映射规则";
        
//        public override Control CreateStepControl(Dictionary<string, object> wizardData)
//        {
//            var panel = new Panel();
//            var label = new Label
//            {
//                Text = "字段映射配置将在此设置...",
//                Location = new Point(20, 20),
//                Size = new Size(400, 200),
//                Font = new Font("微软雅黑", 10F)
//            };
//            panel.Controls.Add(label);
//            return panel;
//        }
        
//        public override ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            return new ValidationResult { IsValid = true };
//        }
        
//        public override void SaveStepData(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            // 实现字段映射数据保存
//        }
//    }

//    public class SummaryStep : WizardStep
//    {
//        public override string Title => "完成";
//        public override string Description => "配置摘要和完成";
        
//        public override Control CreateStepControl(Dictionary<string, object> wizardData)
//        {
//            var panel = new Panel();
//            var label = new Label
//            {
//                Text = "配置摘要：\n\n所有配置已完成，点击\"完成\"按钮保存配置。",
//                Location = new Point(20, 20),
//                Size = new Size(400, 300),
//                Font = new Font("微软雅黑", 10F)
//            };
//            panel.Controls.Add(label);
//            return panel;
//        }
        
//        public override ValidationResult ValidateStep(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            return new ValidationResult { IsValid = true };
//        }
        
//        public override void SaveStepData(Control stepControl, Dictionary<string, object> wizardData)
//        {
//            // 摘要步骤无需保存额外数据
//        }
//    }

//    #endregion
//}
