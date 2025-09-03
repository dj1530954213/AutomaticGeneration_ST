////NEED DELETE: 模板预览窗口（展示性质），非核心流程
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Windows.Forms;
//using WinFormsApp1.Templates;
//using WinFormsApp1.Config;

//namespace WinFormsApp1.Forms
//{
//    /// <summary>
//    /// 模板预览对话框 - 显示选中模板的预览内容
//    /// </summary>
//    public partial class TemplatePreviewForm : Form
//    {
//        private readonly Dictionary<PointType, TemplateVersion> _selectedTemplates;
//        private TabControl previewTabControl;
//        private Button closeButton;
//        private Panel statusPanel;
//        private Label statusLabel;
        
//        public TemplatePreviewForm(Dictionary<PointType, TemplateVersion> selectedTemplates)
//        {
//            _selectedTemplates = selectedTemplates ?? throw new ArgumentNullException(nameof(selectedTemplates));
            
//            InitializeComponent();
//            LoadTemplatePreview();
//            ApplyTheme();
//        }
        
//        private void InitializeComponent()
//        {
//            // 窗体基本设置
//            Text = "模板预览";
//            Size = new Size(900, 700);
//            StartPosition = FormStartPosition.CenterParent;
//            Font = new Font("微软雅黑", 10F);
            
//            // 创建主布局
//            var mainLayout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 3,
//                Padding = new Padding(10)
//            };
            
//            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
//            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
//            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            
//            // 创建预览标签页控件
//            previewTabControl = new TabControl
//            {
//                Dock = DockStyle.Fill,
//                Font = new Font("微软雅黑", 9F)
//            };
            
//            // 创建状态面板
//            statusPanel = new Panel
//            {
//                Dock = DockStyle.Fill,
//                BackColor = Color.FromArgb(240, 248, 255)
//            };
            
//            statusLabel = new Label
//            {
//                Dock = DockStyle.Fill,
//                Text = "正在加载模板预览...",
//                Font = new Font("微软雅黑", 9F),
//                ForeColor = Color.DarkBlue,
//                TextAlign = ContentAlignment.MiddleLeft,
//                Padding = new Padding(10, 0, 10, 0)
//            };
//            statusPanel.Controls.Add(statusLabel);
            
//            // 创建按钮面板
//            var buttonPanel = new Panel
//            {
//                Dock = DockStyle.Fill
//            };
            
//            closeButton = new Button
//            {
//                Text = "关闭",
//                Size = new Size(80, 30),
//                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
//                Font = new Font("微软雅黑", 9F),
//                UseVisualStyleBackColor = true
//            };
//            closeButton.Location = new Point(buttonPanel.Width - closeButton.Width - 10, 
//                (buttonPanel.Height - closeButton.Height) / 2);
//            closeButton.Click += (s, e) => Close();
            
//            buttonPanel.Controls.Add(closeButton);
            
//            // 添加到主布局
//            mainLayout.Controls.Add(previewTabControl, 0, 0);
//            mainLayout.Controls.Add(statusPanel, 0, 1);
//            mainLayout.Controls.Add(buttonPanel, 0, 2);
            
//            Controls.Add(mainLayout);
//        }
        
//        private void LoadTemplatePreview()
//        {
//            try
//            {
//                previewTabControl.TabPages.Clear();
//                int loadedCount = 0;
                
//                foreach (var selection in _selectedTemplates)
//                {
//                    var pointType = selection.Key;
//                    var templateVersion = selection.Value;
                    
//                    try
//                    {
//                        var templateInfo = TemplateManager.GetTemplateInfo(pointType, templateVersion);
//                        if (templateInfo != null)
//                        {
//                            var tabPage = CreatePreviewTabPage(pointType, templateVersion, templateInfo);
//                            previewTabControl.TabPages.Add(tabPage);
//                            loadedCount++;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        // 为无法加载的模板创建错误页面
//                        var errorTabPage = CreateErrorTabPage(pointType, templateVersion, ex.Message);
//                        previewTabControl.TabPages.Add(errorTabPage);
//                    }
//                }
                
//                statusLabel.Text = $"已加载 {loadedCount}/{_selectedTemplates.Count} 个模板预览";
//                statusLabel.ForeColor = loadedCount == _selectedTemplates.Count ? Color.DarkGreen : Color.DarkOrange;
//            }
//            catch (Exception ex)
//            {
//                statusLabel.Text = $"加载预览时出错: {ex.Message}";
//                statusLabel.ForeColor = Color.Red;
//            }
//        }
        
//        private TabPage CreatePreviewTabPage(PointType pointType, TemplateVersion templateVersion, TemplateInfo templateInfo)
//        {
//            var tabPage = new TabPage($"{pointType} - {templateVersion}")
//            {
//                Padding = new Padding(10),
//                UseVisualStyleBackColor = true
//            };
            
//            var mainPanel = new Panel
//            {
//                Dock = DockStyle.Fill
//            };
            
//            var layout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 3
//            };
            
//            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // 信息区域
//            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // 内容区域
//            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));   // 字段区域
            
//            // 模板信息区域
//            var infoPanel = CreateTemplateInfoPanel(templateInfo);
//            layout.Controls.Add(infoPanel, 0, 0);
            
//            // 模板内容预览
//            var contentPanel = CreateTemplateContentPanel(templateInfo);
//            layout.Controls.Add(contentPanel, 0, 1);
            
//            // 必需字段信息
//            var fieldsPanel = CreateRequiredFieldsPanel(templateInfo);
//            layout.Controls.Add(fieldsPanel, 0, 2);
            
//            mainPanel.Controls.Add(layout);
//            tabPage.Controls.Add(mainPanel);
            
//            return tabPage;
//        }
        
//        private Panel CreateTemplateInfoPanel(TemplateInfo templateInfo)
//        {
//            var panel = new Panel
//            {
//                Dock = DockStyle.Fill,
//                BackColor = Color.FromArgb(250, 252, 255),
//                Margin = new Padding(0, 0, 0, 5)
//            };
            
//            var infoLayout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 2,
//                RowCount = 3,
//                Padding = new Padding(10)
//            };
            
//            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
//            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
//            // 添加信息行
//            AddInfoRow(infoLayout, 0, "名称:", templateInfo.Name);
//            AddInfoRow(infoLayout, 1, "描述:", templateInfo.Description);
//            AddInfoRow(infoLayout, 2, "作者:", $"{templateInfo.Author} (创建: {templateInfo.CreatedDate:yyyy-MM-dd})");
            
//            panel.Controls.Add(infoLayout);
//            return panel;
//        }
        
//        private void AddInfoRow(TableLayoutPanel layout, int row, string label, string value)
//        {
//            var labelControl = new Label
//            {
//                Text = label,
//                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
//                Dock = DockStyle.Fill,
//                TextAlign = ContentAlignment.TopLeft,
//                ForeColor = Color.DarkBlue
//            };
            
//            var valueControl = new Label
//            {
//                Text = value,
//                Font = new Font("微软雅黑", 9F),
//                Dock = DockStyle.Fill,
//                TextAlign = ContentAlignment.TopLeft,
//                AutoEllipsis = true
//            };
            
//            layout.Controls.Add(labelControl, 0, row);
//            layout.Controls.Add(valueControl, 1, row);
//        }
        
//        private Panel CreateTemplateContentPanel(TemplateInfo templateInfo)
//        {
//            var panel = new Panel
//            {
//                Dock = DockStyle.Fill
//            };
            
//            var groupBox = new GroupBox
//            {
//                Text = "模板内容预览",
//                Dock = DockStyle.Fill,
//                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
//                Margin = new Padding(0, 5, 0, 5),
//                Padding = new Padding(5)
//            };
            
//            var textBox = new RichTextBox
//            {
//                Dock = DockStyle.Fill,
//                Font = new Font("Consolas", 9F),
//                ReadOnly = true,
//                BackColor = Color.FromArgb(248, 248, 248),
//                BorderStyle = BorderStyle.None,
//                WordWrap = false
//            };
            
//            try
//            {
//                // 读取模板文件内容
//                if (System.IO.File.Exists(templateInfo.FilePath))
//                {
//                    var content = System.IO.File.ReadAllText(templateInfo.FilePath);
                    
//                    // 简单的语法高亮
//                    textBox.Text = content;
//                    ApplySyntaxHighlighting(textBox);
//                }
//                else
//                {
//                    textBox.Text = "模板文件未找到: " + templateInfo.FilePath;
//                    textBox.ForeColor = Color.Red;
//                }
//            }
//            catch (Exception ex)
//            {
//                textBox.Text = $"读取模板文件时出错: {ex.Message}";
//                textBox.ForeColor = Color.Red;
//            }
            
//            groupBox.Controls.Add(textBox);
//            panel.Controls.Add(groupBox);
            
//            return panel;
//        }
        
//        private Panel CreateRequiredFieldsPanel(TemplateInfo templateInfo)
//        {
//            var panel = new Panel
//            {
//                Dock = DockStyle.Fill
//            };
            
//            var groupBox = new GroupBox
//            {
//                Text = "必需字段",
//                Dock = DockStyle.Fill,
//                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
//                Padding = new Padding(5)
//            };
            
//            var fieldsTextBox = new TextBox
//            {
//                Dock = DockStyle.Fill,
//                Font = new Font("微软雅黑", 9F),
//                ReadOnly = true,
//                Multiline = true,
//                BackColor = Color.FromArgb(250, 255, 250),
//                BorderStyle = BorderStyle.None
//            };
            
//            if (templateInfo.RequiredFields.Any())
//            {
//                fieldsTextBox.Text = string.Join(", ", templateInfo.RequiredFields);
//            }
//            else
//            {
//                fieldsTextBox.Text = "此模板不需要特定字段";
//                fieldsTextBox.ForeColor = Color.Gray;
//            }
            
//            groupBox.Controls.Add(fieldsTextBox);
//            panel.Controls.Add(groupBox);
            
//            return panel;
//        }
        
//        private TabPage CreateErrorTabPage(PointType pointType, TemplateVersion templateVersion, string errorMessage)
//        {
//            var tabPage = new TabPage($"{pointType} - {templateVersion} (错误)")
//            {
//                Padding = new Padding(10),
//                BackColor = Color.FromArgb(255, 248, 248)
//            };
            
//            var errorLabel = new Label
//            {
//                Text = $"无法加载模板预览:\n\n{errorMessage}",
//                Dock = DockStyle.Fill,
//                Font = new Font("微软雅黑", 10F),
//                ForeColor = Color.DarkRed,
//                TextAlign = ContentAlignment.MiddleCenter
//            };
            
//            tabPage.Controls.Add(errorLabel);
//            return tabPage;
//        }
        
//        private void ApplySyntaxHighlighting(RichTextBox textBox)
//        {
//            try
//            {
//                // 简单的Scriban语法高亮
//                var content = textBox.Text;
//                textBox.SelectAll();
//                textBox.SelectionColor = Color.Black;
                
//                // 高亮注释
//                HighlightPattern(textBox, @"//.*", Color.Green);
                
//                // 高亮Scriban标签
//                HighlightPattern(textBox, @"\{\{.*?\}\}", Color.Blue);
                
//                // 高亮关键字
//                var keywords = new[] { "VAR", "END_VAR", "FUNCTION_BLOCK", "IF", "THEN", "ELSE", "END_IF", "FOR", "TO", "DO", "END_FOR" };
//                foreach (var keyword in keywords)
//                {
//                    HighlightPattern(textBox, $@"\b{keyword}\b", Color.Purple);
//                }
                
//                textBox.SelectionStart = 0;
//                textBox.SelectionLength = 0;
//            }
//            catch
//            {
//                // 语法高亮失败时忽略错误
//            }
//        }
        
//        private void HighlightPattern(RichTextBox textBox, string pattern, Color color)
//        {
//            try
//            {
//                var regex = new System.Text.RegularExpressions.Regex(pattern, 
//                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
//                foreach (System.Text.RegularExpressions.Match match in regex.Matches(textBox.Text))
//                {
//                    textBox.Select(match.Index, match.Length);
//                    textBox.SelectionColor = color;
//                }
//            }
//            catch
//            {
//                // 正则表达式错误时忽略
//            }
//        }
        
//        private void ApplyTheme()
//        {
//            try
//            {
//                var isDarkTheme = ThemeManager.CurrentTheme == ThemeType.Dark;
                
//                if (isDarkTheme)
//                {
//                    BackColor = Color.FromArgb(45, 45, 48);
//                    ForeColor = Color.White;
//                    statusPanel.BackColor = Color.FromArgb(37, 37, 38);
//                    previewTabControl.BackColor = Color.FromArgb(45, 45, 48);
//                }
//                else
//                {
//                    BackColor = SystemColors.Control;
//                    ForeColor = SystemColors.ControlText;
//                    statusPanel.BackColor = Color.FromArgb(240, 248, 255);
//                }
//            }
//            catch
//            {
//                // 主题应用失败时使用默认样式
//            }
//        }
//    }
//}
