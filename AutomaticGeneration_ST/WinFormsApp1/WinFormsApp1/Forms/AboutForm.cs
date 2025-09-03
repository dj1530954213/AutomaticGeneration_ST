////--NEED DELETE: 帮助/关于窗口（帮助工具栏相关），非核心功能
//using System;
//using System.Drawing;
//using System.Reflection;
//using System.Windows.Forms;
//using WinFormsApp1.Config;

//namespace WinFormsApp1.Forms
//{
//    //TODO: 重复代码(ID:DUP-004) - [UI初始化：窗体样式设置和组件初始化模式重复] 
//    //TODO: 建议重构为FormStyleApplier基类或FormBuilder工厂模式 优先级:中等
//    public partial class AboutForm : Form
//    {
//        private PictureBox? logoPictureBox;
//        private Label? titleLabel;
//        private Label? versionLabel;
//        private Label? descriptionLabel;
//        private RichTextBox? detailsRichTextBox;
//        private Button? okButton;
//        private LinkLabel? websiteLinkLabel;
//        private Panel? topPanel;
//        private Panel? bottomPanel;

//        public AboutForm()
//        {
//            InitializeComponent();
//            LoadApplicationInfo();
//            ApplyTheme();
//        }

//        private void InitializeComponent()
//        {
//            this.SuspendLayout();

//            // 窗体设置
//            this.Text = "关于 ST脚本自动生成器";
//            this.Size = new Size(500, 400);
//            this.StartPosition = FormStartPosition.CenterParent;
//            this.FormBorderStyle = FormBorderStyle.FixedDialog;
//            this.MaximizeBox = false;
//            this.MinimizeBox = false;
//            this.ShowInTaskbar = false;
//            this.BackColor = Color.White;

//            // 顶部面板
//            topPanel = new Panel
//            {
//                Dock = DockStyle.Top,
//                Height = 120,
//                BackColor = Color.FromArgb(240, 248, 255)
//            };

//            // Logo图标 (使用简单的图形替代)
//            logoPictureBox = new PictureBox
//            {
//                Size = new Size(64, 64),
//                Location = new Point(20, 28),
//                BackColor = Color.FromArgb(0, 123, 255),
//                BorderStyle = BorderStyle.FixedSingle
//            };
            
//            // 绘制简单的图标
//            logoPictureBox.Paint += (s, e) => {
//                var g = e.Graphics;
//                g.Clear(Color.FromArgb(0, 123, 255));
                
//                // 绘制ST字样
//                using var font = new Font("微软雅黑", 16, FontStyle.Bold);
//                using var brush = new SolidBrush(Color.White);
//                var text = "ST";
//                var textSize = g.MeasureString(text, font);
//                var x = (64 - textSize.Width) / 2;
//                var y = (64 - textSize.Height) / 2;
//                g.DrawString(text, font, brush, x, y);
//            };

//            // 标题标签
//            titleLabel = new Label
//            {
//                Text = "ST脚本自动生成器",
//                Font = new Font("微软雅黑", 16, FontStyle.Bold),
//                ForeColor = Color.FromArgb(33, 37, 41),
//                Location = new Point(100, 30),
//                AutoSize = true
//            };

//            // 版本标签
//            versionLabel = new Label
//            {
//                Text = "版本 2.0.0",
//                Font = new Font("微软雅黑", 10),
//                ForeColor = Color.FromArgb(108, 117, 125),
//                Location = new Point(100, 60),
//                AutoSize = true
//            };

//            // 描述标签
//            descriptionLabel = new Label
//            {
//                Text = "专业的工业自动化代码生成工具",
//                Font = new Font("微软雅黑", 9),
//                ForeColor = Color.FromArgb(73, 80, 87),
//                Location = new Point(100, 85),
//                AutoSize = true
//            };

//            topPanel.Controls.AddRange(new Control[] { 
//                logoPictureBox, titleLabel, versionLabel, descriptionLabel 
//            });

//            // 详细信息文本框
//            detailsRichTextBox = new RichTextBox
//            {
//                Dock = DockStyle.Fill,
//                Margin = new Padding(20, 10, 20, 10),
//                ReadOnly = true,
//                BorderStyle = BorderStyle.None,
//                BackColor = Color.White,
//                Font = new Font("微软雅黑", 9),
//                ScrollBars = RichTextBoxScrollBars.Vertical
//            };

//            // 底部面板
//            bottomPanel = new Panel
//            {
//                Dock = DockStyle.Bottom,
//                Height = 60,
//                BackColor = Color.FromArgb(248, 249, 250)
//            };

//            // 网站链接
//            websiteLinkLabel = new LinkLabel
//            {
//                Text = "访问项目主页",
//                Font = new Font("微软雅黑", 9),
//                Location = new Point(20, 20),
//                AutoSize = true,
//                LinkBehavior = LinkBehavior.HoverUnderline
//            };
//            websiteLinkLabel.Click += WebsiteLinkLabel_Click;

//            // 确定按钮
//            okButton = new Button
//            {
//                Text = "确定",
//                Size = new Size(80, 30),
//                Location = new Point(400, 15),
//                DialogResult = DialogResult.OK,
//                BackColor = Color.FromArgb(0, 123, 255),
//                ForeColor = Color.White,
//                FlatStyle = FlatStyle.Flat
//            };
//            okButton.FlatAppearance.BorderSize = 0;

//            bottomPanel.Controls.AddRange(new Control[] { websiteLinkLabel, okButton });

//            // 添加控件到窗体
//            this.Controls.Add(detailsRichTextBox);
//            this.Controls.Add(topPanel);
//            this.Controls.Add(bottomPanel);

//            this.AcceptButton = okButton;
//            this.CancelButton = okButton;

//            this.ResumeLayout(false);
//            this.PerformLayout();
//        }

//        private void LoadApplicationInfo()
//        {
//            try
//            {
//                var assembly = Assembly.GetExecutingAssembly();
//                var version = assembly.GetName().Version;
//                var buildDate = GetBuildDate(assembly);

//                if (versionLabel != null)
//                {
//                    versionLabel.Text = $"版本 {version?.ToString(3) ?? "2.0.0"}";
//                }

//                if (detailsRichTextBox != null)
//                {
//                    var detailsText = $@"产品信息:
//• 软件名称: ST脚本自动生成器
//• 版本号: {version?.ToString() ?? "2.0.0.0"}
//• 构建日期: {buildDate:yyyy年M月d日}
//• 开发者: Claude AI Assistant

//技术信息:
//• 框架: .NET 9.0
//• UI技术: Windows Forms
//• 模板引擎: Scriban v6.2.1
//• Excel处理: NPOI v2.7.4
//• 文件格式: .xlsx, .xls

//功能特性:
//• 支持AI/AO/DI/DO四种点位类型
//• 智能识别Excel点表格式
//• 自动生成符合IEC 61131-3标准的ST代码
//• 可自定义代码生成模板
//• 支持浅色/深色主题切换
//• 完整的快捷键操作系统

//系统要求:
//• 操作系统: Windows 10/11 (x64)
//• 运行时: .NET 9.0 Runtime
//• 内存: 最低 512MB RAM
//• 磁盘空间: 50MB 可用空间

//许可信息:
//本软件基于MIT许可证发布，允许商业和非商业使用。

//技术支持:
//如需技术支持或报告问题，请联系开发团队。

//© 2025 版权所有";

//                    detailsRichTextBox.Text = detailsText;
                    
//                    // 设置标题样式
//                    SetRichTextStyle();
//                }
//            }
//            catch (Exception ex)
//            {
//                if (detailsRichTextBox != null)
//                {
//                    detailsRichTextBox.Text = $"加载应用程序信息时出错: {ex.Message}";
//                }
//            }
//        }

//        private void SetRichTextStyle()
//        {
//            if (detailsRichTextBox == null) return;

//            try
//            {
//                var text = detailsRichTextBox.Text;
//                var lines = text.Split('\n');
                
//                detailsRichTextBox.SelectAll();
//                detailsRichTextBox.SelectionFont = new Font("微软雅黑", 9);
//                detailsRichTextBox.SelectionColor = Color.FromArgb(73, 80, 87);

//                // 设置标题样式
//                var titleFont = new Font("微软雅黑", 10, FontStyle.Bold);
//                var titleColor = Color.FromArgb(33, 37, 41);

//                foreach (var line in lines)
//                {
//                    if (line.EndsWith(":") && !line.StartsWith("•"))
//                    {
//                        var index = text.IndexOf(line);
//                        if (index >= 0)
//                        {
//                            detailsRichTextBox.Select(index, line.Length);
//                            detailsRichTextBox.SelectionFont = titleFont;
//                            detailsRichTextBox.SelectionColor = titleColor;
//                        }
//                    }
//                }

//                detailsRichTextBox.Select(0, 0);
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"设置富文本样式时出错: {ex.Message}");
//            }
//        }

//        private DateTime GetBuildDate(Assembly assembly)
//        {
//            try
//            {
//                var attribute = assembly.GetCustomAttribute<System.Reflection.AssemblyMetadataAttribute>();
//                if (attribute?.Key == "BuildDate" && DateTime.TryParse(attribute.Value, out var date))
//                {
//                    return date;
//                }
//            }
//            catch
//            {
//                // 忽略错误
//            }

//            // 默认返回当前日期
//            return DateTime.Now.Date;
//        }

//        private void ApplyTheme()
//        {
//            try
//            {
//                var isDarkTheme = ThemeManager.CurrentTheme == ThemeType.Dark;

//                if (isDarkTheme)
//                {
//                    this.BackColor = ThemeManager.GetBackgroundColor();
                    
//                    if (topPanel != null)
//                        topPanel.BackColor = ThemeManager.GetSurfaceColor();
                    
//                    if (bottomPanel != null)
//                        bottomPanel.BackColor = ThemeManager.GetSurfaceColor();
                    
//                    if (detailsRichTextBox != null)
//                    {
//                        detailsRichTextBox.BackColor = ThemeManager.GetBackgroundColor();
//                        detailsRichTextBox.ForeColor = ThemeManager.GetTextColor();
//                    }
                    
//                    if (titleLabel != null)
//                        titleLabel.ForeColor = ThemeManager.GetTextColor();
                    
//                    if (versionLabel != null)
//                        versionLabel.ForeColor = ThemeManager.GetSecondaryTextColor();
                    
//                    if (descriptionLabel != null)
//                        descriptionLabel.ForeColor = ThemeManager.GetSecondaryTextColor();
//                }
//            }
//            catch (Exception ex)
//            {
//                System.Diagnostics.Debug.WriteLine($"应用主题时出错: {ex.Message}");
//            }
//        }

//        private void WebsiteLinkLabel_Click(object? sender, EventArgs e)
//        {
//            try
//            {
//                // 这里可以打开项目网站
//                MessageBox.Show("项目主页功能暂未实现", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"打开网站失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        protected override void OnKeyDown(KeyEventArgs e)
//        {
//            if (e.KeyCode == Keys.Escape)
//            {
//                this.DialogResult = DialogResult.Cancel;
//                this.Close();
//            }
//            base.OnKeyDown(e);
//        }
//    }
//}
