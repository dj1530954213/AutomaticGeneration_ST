using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using WinFormsApp1.Excel;
using WinFormsApp1.Generators;
using WinFormsApp1.Output;
using WinFormsApp1.Config;
using WinFormsApp1.ProjectManagement;
using WinFormsApp1.Tests;
using System.Windows.Forms;
using AutomaticGeneration_ST.Services;
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private LogService logger = null!;
        private string uploadedFilePath = "";
        private List<string> generatedScripts = new List<string>();
        private List<Dictionary<string, object>> pointData = new List<Dictionary<string, object>>();
        private List<string> recentFiles = new List<string>();
        private const int MaxRecentFiles = 10;
        private System.Windows.Forms.Timer statusTimer = new System.Windows.Forms.Timer();

        // 设备 ST 代码缓存，Key = 文件路径 + 最后修改时间
        private bool isUpdatingPreview = false;
        private STGenerationService stGenerationService = new STGenerationService();
        
        // 新架构：ProjectCache机制 - 上传一次，处理一次，后续只从缓存读取
        private ProjectCache? currentProjectCache = null;
        private readonly ImportPipeline importPipeline = new ImportPipeline();
        private bool deviceListNeedsRefresh = true;
        private readonly object projectCacheLock = new object(); // 线程同步锁
        
        // 服务容器和分类导出服务
        private ServiceContainer? serviceContainer = null;
        private ICategorizedExportService? categorizedExportService = null;

        public Form1()
        {
            InitializeComponent();
            InitializeLogger();
            InitializeUI();
            InitializeTheme();
            InitializeKeyboardShortcuts();
            InitializeTooltips();
            InitializeProjectManagement();
        }

        private async void InitializeLogger()
        {
            // 初始化配置管理器
            try
            {
                await Config.ConfigurationManager.InitializeAsync();
                Config.ConfigurationApplier.Initialize();
                
                logger = LogService.Instance;
                logger.Initialize(richTextBox1);
                logger.LogInfo("ST脚本自动生成器 v2.0 已启动");
                logger.LogInfo("支持的点位类型: AI, AO, DI, DO");
                logger.LogInfo("配置系统初始化完成");
                
                // 应用当前配置
                Config.ConfigurationApplier.ApplyAllConfiguration();
            }
            catch (Exception ex)
            {
                logger = LogService.Instance;
                logger.Initialize(richTextBox1);
                logger.LogError($"配置系统初始化失败: {ex.Message}");
                logger.LogInfo("ST脚本自动生成器 v2.0 已启动（使用默认配置）");
                logger.LogInfo("支持的点位类型: AI, AO, DI, DO");
            }
        }

        private void InitializeUI()
        {
            // 设置状态栏初始状态
            statusLabel.Text = "就绪";
            progressBar.Visible = false;
            
            // 应用标准样式
            ApplyStandardStyles();
            
            // 设置文件列表标题
            var fileLabel = new Label
            {
                Text = "📁 文件列表",
                Font = ControlStyleManager.HeaderFont,
                Location = new System.Drawing.Point(ControlStyleManager.MEDIUM_PADDING, 65),
                Size = new Size(100, 25),
                ForeColor = ThemeManager.GetTextColor()
            };
            ControlStyleManager.ApplyLabelStyle(fileLabel, LabelStyle.Header);
            leftPanel.Controls.Add(fileLabel);
            
            // 启用拖拽功能
            EnableDragDrop();
            
            // 加载最近文件列表
            LoadRecentFiles();
            
            // 初始化预览选项卡
            InitializePreviewTabs();
            
            // 添加右键菜单
            InitializeContextMenus();
            
            // 设置分割器样式
            mainSplitContainer.SplitterWidth = 3;
            rightSplitContainer.SplitterWidth = 3;
            
            // 初始化日志过滤功能
            InitializeLogFilters();
            
            // 初始化状态栏定时器
            InitializeStatusTimer();
            
            // 初始化菜单事件
            InitializeMenuEvents();
            
            logger.LogInfo("现代化UI界面初始化完成");
        }

        private void EnableDragDrop()
        {
            // 为左侧面板启用拖拽
            leftPanel.AllowDrop = true;
            leftPanel.DragEnter += LeftPanel_DragEnter;
            leftPanel.DragDrop += LeftPanel_DragDrop;
            
            // 为文件列表启用拖拽
            fileListBox.AllowDrop = true;
            fileListBox.DragEnter += LeftPanel_DragEnter;
            fileListBox.DragDrop += LeftPanel_DragDrop;
        }

        private void LeftPanel_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var file = files[0];
                    var ext = Path.GetExtension(file).ToLower();
                    if (ext == ".xlsx" || ext == ".csv")
                    {
                        e.Effect = DragDropEffects.Copy;
                        return;
                    }
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void LeftPanel_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var file = files[0];
                    var ext = Path.GetExtension(file).ToLower();
                    if (ext == ".xlsx" || ext == ".csv")
                    {
                        uploadedFilePath = file;
                        logger.LogInfo($"拖拽导入文件: {Path.GetFileName(file)}");
                        
                        // 添加到最近文件
                        AddToRecentFiles(file);
                        
                        // 更新文件列表
                        UpdateFileList(file);
                        
                        // 处理文件
                        ProcessExcelFile(file);
                    }
                }
            }
        }

        private void LoadRecentFiles()
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "STGenerator", "recent_files.txt");
                
                if (File.Exists(configPath))
                {
                    recentFiles = File.ReadAllLines(configPath).Where(File.Exists).Take(MaxRecentFiles).ToList();
                    UpdateRecentFilesMenu();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"加载最近文件列表失败: {ex.Message}");
            }
        }

        private void AddToRecentFiles(string filePath)
        {
            if (recentFiles.Contains(filePath))
            {
                recentFiles.Remove(filePath);
            }
            
            recentFiles.Insert(0, filePath);
            
            if (recentFiles.Count > MaxRecentFiles)
            {
                recentFiles = recentFiles.Take(MaxRecentFiles).ToList();
            }
            
            SaveRecentFiles();
            UpdateRecentFilesMenu();
        }

        private void SaveRecentFiles()
        {
            try
            {
                var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "STGenerator");
                Directory.CreateDirectory(configDir);
                
                var configPath = Path.Combine(configDir, "recent_files.txt");
                File.WriteAllLines(configPath, recentFiles);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"保存最近文件列表失败: {ex.Message}");
            }
        }

        private void UpdateRecentFilesMenu()
        {
            // 这个方法将在添加菜单项时实现
            // 目前先留空，等步骤7实现工具栏时会完善
        }

        private void InitializeLogFilters()
        {
            // 搜索框事件
            logSearchBox.TextChanged += LogSearchBox_TextChanged;
            
            // 过滤下拉框事件
            logFilterComboBox.SelectedIndexChanged += LogFilterComboBox_SelectedIndexChanged;
            
            // 清空按钮事件
            clearLogButton.Click += ClearLogButton_Click;
            
            logger.LogInfo("日志过滤功能初始化完成");
        }

        private void LogSearchBox_TextChanged(object? sender, EventArgs e)
        {
            // 实现日志搜索功能
            ApplyLogFilters();
        }

        private void LogFilterComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // 实现日志级别过滤
            ApplyLogFilters();
        }

        private void ClearLogButton_Click(object? sender, EventArgs e)
        {
            richTextBox1.Clear();
            logger.LogInfo("日志已清空");
        }

        private void ApplyLogFilters()
        {
            // 这里会实现日志过滤逻辑
            // 由于当前日志系统比较简单，这个功能先预留
            // 在后续优化中会实现完整的过滤功能
        }

        private void InitializeStatusTimer()
        {
            statusTimer.Interval = 1000; // 每秒更新一次
            statusTimer.Tick += StatusTimer_Tick;
            statusTimer.Start();
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            // 更新时间显示
            var timeLabel = mainStatusStrip.Items["timeLabel"] as ToolStripStatusLabel;
            if (timeLabel != null)
            {
                timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        private void UpdateStatusBarStats()
        {
            var totalPointsLabel = mainStatusStrip.Items["totalPointsLabel"] as ToolStripStatusLabel;
            if (totalPointsLabel != null)
            {
                totalPointsLabel.Text = $"总点位: {pointData.Count}";
            }
        }

        private void InitializeMenuEvents()
        {
            // 菜单事件已在Designer.cs中直接绑定
            // 这里可以添加其他菜单相关的初始化逻辑
            logger.LogInfo("菜单事件初始化完成");
        }

        // 文件菜单事件处理器
        private void OpenFileMenuItem_Click(object? sender, EventArgs e)
        {
            button_upload_Click(sender, e);
        }

        private void ExportFileMenuItem_Click(object? sender, EventArgs e)
        {
            button_export_Click(sender, e);
        }

        private void RegenerateMenuItem_Click(object? sender, EventArgs e)
        {
            RegenerateCode();
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        // 编辑菜单事件处理器
        private void ClearLogMenuItem_Click(object? sender, EventArgs e)
        {
            ClearLogButton_Click(sender, e);
        }

        // 帮助菜单事件处理器
        private void AboutMenuItem_Click(object? sender, EventArgs e)
        {
            ShowHelp();
        }

        private async void RegenerateCode()
        {
            if (!string.IsNullOrEmpty(uploadedFilePath) && File.Exists(uploadedFilePath))
            {
                logger.LogInfo("重新生成ST代码...");
                ProcessExcelFile(uploadedFilePath);
            }
            else if (pointData.Any())
            {
                logger.LogInfo("基于现有数据重新生成ST代码...");
                try
                {
                    await GenerateSTScriptsAsync();
                    logger.LogSuccess("ST代码重新生成完成");
                }
                catch (Exception ex)
                {
                    logger.LogError($"重新生成ST代码失败: {ex.Message}");
                    MessageBox.Show($"重新生成ST代码失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                logger.LogWarning("没有可重新生成的数据，请先上传点表文件");
                MessageBox.Show("请先上传点表文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowSettings()
        {
            logger.LogInfo("设置功能已移除");
            MessageBox.Show("设置功能暂时不可用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowHelp()
        {
            var helpText = @"ST脚本自动生成器 v2.0 - 使用帮助

🔸 支持的文件格式：Excel (.xlsx) 和 CSV (.csv)
🔸 支持的点位类型：AI、AO、DI、DO
🔸 拖拽支持：可直接拖拽文件到左侧面板
🔸 快捷键：
   • Ctrl+O: 打开文件
   • Ctrl+S: 导出结果  
   • F5: 重新生成代码
   • Ctrl+L: 清空日志
   • F1: 显示帮助

📧 技术支持：ST脚本生成器开发团队";

            MessageBox.Show(helpText, "帮助", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateProgressBar(string statusText, int percentage, bool isIndeterminate)
        {
            statusLabel.Text = statusText;
            
            if (isIndeterminate)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.Visible = true;
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = Math.Min(100, Math.Max(0, percentage));
                progressBar.Visible = percentage > 0 && percentage < 100;
            }
        }

        private void InitializePreviewTabs()
        {
            // 添加代码预览选项卡
            var previewTab = new TabPage("📋 IO点位映射ST预览");
            var previewTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 20F),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke,
                Name = "previewTextBox"
            };
            previewTab.Controls.Add(previewTextBox);
            previewTabControl.TabPages.Add(previewTab);
            
            // 添加设备ST程序选项卡
            var deviceSTTab = new TabPage("🏭 设备ST程序");
            
            // 创建设备选择面板
            var deviceSelectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.LightBlue
            };
            
            var deviceLabel = new Label
            {
                Text = "选择设备:",
                Location = new System.Drawing.Point(10, 8),
                Size = new Size(80, 20),
                BackColor = Color.Transparent
            };
            
            var deviceComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(90, 5),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "deviceComboBox"
            };
            deviceComboBox.Items.Add("全部设备");
            deviceComboBox.SelectedIndex = 0;
            deviceComboBox.SelectedIndexChanged += DeviceComboBox_SelectedIndexChanged;
            
            var refreshButton = new Button
            {
                Text = "🔄",
                Location = new System.Drawing.Point(300, 5),
                Size = new Size(30, 25),
                FlatStyle = FlatStyle.Flat
            };
            refreshButton.Click += (s, e) => RefreshDeviceList();
            
            deviceSelectionPanel.Controls.Add(deviceLabel);
            deviceSelectionPanel.Controls.Add(deviceComboBox);
            deviceSelectionPanel.Controls.Add(refreshButton);
            
            var deviceSTTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10F),
                ReadOnly = true,
                BackColor = Color.LightCyan,
                Name = "deviceSTTextBox"
            };
            
            deviceSTTab.Controls.Add(deviceSelectionPanel);
            deviceSTTab.Controls.Add(deviceSTTextBox);
            previewTabControl.TabPages.Add(deviceSTTab);
            
            // 添加统计信息选项卡
            var statisticsTab = new TabPage("📊 统计信息");
            var statsTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 11F),
                ReadOnly = true,
                BackColor = Color.AliceBlue,
                Name = "statsTextBox"
            };
            statisticsTab.Controls.Add(statsTextBox);
            previewTabControl.TabPages.Add(statisticsTab);
            
            // 添加文件信息选项卡
            var fileInfoTab = new TabPage("📁 文件信息");
            var fileInfoTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 11F),
                ReadOnly = true,
                BackColor = Color.Honeydew,
                Name = "fileInfoTextBox"
            };
            fileInfoTab.Controls.Add(fileInfoTextBox);
            previewTabControl.TabPages.Add(fileInfoTab);
            
            // 添加模板信息选项卡
            var templateTab = new TabPage("🎨 模板信息");
            var templateTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 11F),
                ReadOnly = true,
                BackColor = Color.Lavender,
                Name = "templateTextBox"
            };
            templateTab.Controls.Add(templateTextBox);
            previewTabControl.TabPages.Add(templateTab);
            
            // 添加TCP通讯ST程序选项卡
            var tcpCommTab = new TabPage("🌐 TCP通讯ST程序");
            var tcpCommTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10F),
                ReadOnly = true,
                BackColor = ThemeManager.GetSurfaceColor(),
                Name = "tcpCommTextBox"
            };
            tcpCommTab.Controls.Add(tcpCommTextBox);
            previewTabControl.TabPages.Add(tcpCommTab);
        }

        private void InitializeContextMenus()
        {
            // 为文件列表添加右键菜单
            var fileContextMenu = new ContextMenuStrip();
            fileContextMenu.Items.Add("📁 打开文件", null, (s, e) => button_upload_Click(s, e));
            fileContextMenu.Items.Add("🔄 重新处理", null, (s, e) => ReprocessSelectedFile());
            fileContextMenu.Items.Add("❌ 移除文件", null, (s, e) => RemoveSelectedFile());
            fileContextMenu.Items.Add(new ToolStripSeparator());
            fileContextMenu.Items.Add("📋 复制路径", null, (s, e) => CopyFilePath());
            fileContextMenu.Items.Add("📂 打开文件夹", null, (s, e) => OpenFileFolder());
            fileListBox.ContextMenuStrip = fileContextMenu;
            
            // 为预览区域添加右键菜单
            var previewContextMenu = new ContextMenuStrip();
            previewContextMenu.Items.Add("📋 复制全部", null, (s, e) => CopyAllPreviewContent());
            previewContextMenu.Items.Add("📋 复制选中", null, (s, e) => CopySelectedPreviewContent());
            previewContextMenu.Items.Add(new ToolStripSeparator());
            previewContextMenu.Items.Add("💾 保存预览", null, (s, e) => SavePreviewContent());
            previewContextMenu.Items.Add("🔍 查找", null, (s, e) => ShowFindDialog());
            
            // 为每个预览标签页的文本框添加右键菜单
            foreach (TabPage tab in previewTabControl.TabPages)
            {
                if (tab.Controls[0] is RichTextBox textBox)
                {
                    textBox.ContextMenuStrip = previewContextMenu;
                }
            }
            
            // 为日志区域添加右键菜单
            var logContextMenu = new ContextMenuStrip();
            logContextMenu.Items.Add("📋 复制全部", null, (s, e) => CopyAllLogContent());
            logContextMenu.Items.Add("📋 复制选中", null, (s, e) => CopySelectedLogContent());
            logContextMenu.Items.Add(new ToolStripSeparator());
            logContextMenu.Items.Add("🗑️ 清空日志", null, (s, e) => ClearLogButton_Click(s, e));
            logContextMenu.Items.Add("💾 保存日志", null, (s, e) => SaveLogContent());
            richTextBox1.ContextMenuStrip = logContextMenu;
        }

        private void ReprocessSelectedFile()
        {
            if (fileListBox.SelectedItem != null && !string.IsNullOrEmpty(uploadedFilePath))
            {
                logger.LogInfo("重新处理文件: " + Path.GetFileName(uploadedFilePath));
                ProcessExcelFile(uploadedFilePath);
            }
        }

        private void RemoveSelectedFile()
        {
            if (fileListBox.SelectedItem != null)
            {
                fileListBox.Items.Remove(fileListBox.SelectedItem);
                logger.LogInfo("已移除选中的文件");
            }
        }

        private void CopyFilePath()
        {
            if (!string.IsNullOrEmpty(uploadedFilePath))
            {
                Clipboard.SetText(uploadedFilePath);
                logger.LogInfo("文件路径已复制到剪贴板");
            }
        }

        private void OpenFileFolder()
        {
            if (!string.IsNullOrEmpty(uploadedFilePath) && File.Exists(uploadedFilePath))
            {
                var argument = "/select, \"" + uploadedFilePath + "\"";
                System.Diagnostics.Process.Start("explorer.exe", argument);
                logger.LogInfo("已打开文件所在文件夹");
            }
        }

        private void CopyAllPreviewContent()
        {
            var currentTab = previewTabControl.SelectedTab;
            if (currentTab?.Controls[0] is RichTextBox textBox)
            {
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    Clipboard.SetText(textBox.Text);
                    logger.LogInfo("预览内容已复制到剪贴板");
                }
            }
        }

        private void CopySelectedPreviewContent()
        {
            var currentTab = previewTabControl.SelectedTab;
            if (currentTab?.Controls[0] is RichTextBox textBox)
            {
                if (!string.IsNullOrEmpty(textBox.SelectedText))
                {
                    Clipboard.SetText(textBox.SelectedText);
                    logger.LogInfo("选中的预览内容已复制到剪贴板");
                }
                else
                {
                    logger.LogWarning("没有选中的内容可复制");
                }
            }
        }

        private void SavePreviewContent()
        {
            var currentTab = previewTabControl.SelectedTab;
            if (currentTab?.Controls[0] is RichTextBox textBox)
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "文本文件|*.txt|所有文件|*.*";
                    saveDialog.FileName = $"{currentTab.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveDialog.FileName, textBox.Text, System.Text.Encoding.UTF8);
                        logger.LogInfo($"预览内容已保存到: {Path.GetFileName(saveDialog.FileName)}");
                    }
                }
            }
        }

        private void ShowFindDialog()
        {
            // 简单的查找对话框
            var findText = Microsoft.VisualBasic.Interaction.InputBox("请输入要查找的文本:", "查找", "");
            if (!string.IsNullOrEmpty(findText))
            {
                var currentTab = previewTabControl.SelectedTab;
                if (currentTab?.Controls[0] is RichTextBox textBox)
                {
                    var index = textBox.Find(findText, RichTextBoxFinds.None);
                    if (index >= 0)
                    {
                        textBox.Focus();
                        logger.LogInfo($"找到文本: {findText}");
                    }
                    else
                    {
                        logger.LogWarning($"未找到文本: {findText}");
                    }
                }
            }
        }

        private void CopyAllLogContent()
        {
            if (!string.IsNullOrEmpty(richTextBox1.Text))
            {
                Clipboard.SetText(richTextBox1.Text);
                logger.LogInfo("所有日志内容已复制到剪贴板");
            }
        }

        private void CopySelectedLogContent()
        {
            if (!string.IsNullOrEmpty(richTextBox1.SelectedText))
            {
                Clipboard.SetText(richTextBox1.SelectedText);
                logger.LogInfo("选中的日志内容已复制到剪贴板");
            }
            else
            {
                logger.LogWarning("没有选中的日志内容可复制");
            }
        }

        private void SaveLogContent()
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "日志文件|*.log|文本文件|*.txt|所有文件|*.*";
                saveDialog.FileName = $"STGenerator_Log_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, richTextBox1.Text, System.Text.Encoding.UTF8);
                    logger.LogInfo($"日志已保存到: {Path.GetFileName(saveDialog.FileName)}");
                }
            }
        }

        private void InitializeProjectManagement()
        {
            try
            {
                // 订阅项目变更事件
                SimpleProjectManager.ProjectChanged += OnProjectChanged;
                
                // 创建新项目或加载现有项目
                if (SimpleProjectManager.CurrentProject == null)
                {
                    SimpleProjectManager.CreateNewProject();
                }
                
                // 初始化服务容器和分类导出服务
                InitializeServices();
                
                logger.LogInfo("项目管理系统已初始化");
            }
            catch (Exception ex)
            {
                logger?.LogError($"初始化项目管理系统时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 初始化服务容器和相关服务
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                // 获取模板目录路径
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var templateDirectory = Path.Combine(appDirectory, "Templates");
                var configPath = Path.Combine(templateDirectory, "template-mapping.json");
                
                // 创建服务容器
                serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);
                
                // 获取分类导出服务
                categorizedExportService = serviceContainer.GetService<ICategorizedExportService>();
                
                logger?.LogInfo("服务容器和分类导出服务初始化完成");
            }
            catch (Exception ex)
            {
                logger?.LogError($"初始化服务时出错: {ex.Message}");
                // 即使初始化失败，也不影响主要功能
            }
        }

        private void OnProjectChanged(object? sender, EventArgs e)
        {
            try
            {
                // 更新窗口标题
                UpdateWindowTitle();
                
                // 同步项目数据
                SyncProjectData();
                
                logger.LogInfo("项目状态已更新");
            }
            catch (Exception ex)
            {
                logger?.LogError($"更新项目状态时出错: {ex.Message}");
            }
        }

        private void UpdateWindowTitle()
        {
            var projectName = SimpleProjectManager.CurrentProject?.Name ?? "新建项目";
            var hasChanges = SimpleProjectManager.HasUnsavedChanges ? " *" : "";
            var filePath = !string.IsNullOrEmpty(SimpleProjectManager.CurrentFilePath) 
                ? $" - {Path.GetFileName(SimpleProjectManager.CurrentFilePath)}" 
                : "";
            
            this.Text = $"{projectName}{hasChanges}{filePath} - ST脚本自动生成器";
        }

        private void SyncProjectData()
        {
            try
            {
                var project = SimpleProjectManager.CurrentProject;
                if (project != null)
                {
                    // 同步点位数据
                    if (project.PointData.Any())
                    {
                        pointData = project.PointData;
                        UpdateStatusBarStats();
                    }
                    
                    // 同步生成的代码
                    if (!string.IsNullOrEmpty(project.GeneratedCode))
                    {
                        var codeScripts = project.GeneratedCode.Split(new[] { "\n\n" + new string('-', 50) + "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                        generatedScripts = codeScripts.ToList();
                        UpdatePreviewArea();
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"同步项目数据时出错: {ex.Message}");
            }
        }

        private async void NewProjectMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                if (SimpleProjectManager.NeedsSave())
                {
                    var result = MessageBox.Show(
                        "当前项目有未保存的更改，是否保存？",
                        "确认", 
                        MessageBoxButtons.YesNoCancel, 
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        await SaveProject();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                
                SimpleProjectManager.CreateNewProject();
                ClearCurrentData();
                
                logger.LogInfo("已创建新项目");
            }
            catch (Exception ex)
            {
                logger.LogError($"创建新项目失败: {ex.Message}");
                MessageBox.Show($"创建新项目失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OpenProjectMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                if (SimpleProjectManager.NeedsSave())
                {
                    var result = MessageBox.Show(
                        "当前项目有未保存的更改，是否保存？",
                        "确认", 
                        MessageBoxButtons.YesNoCancel, 
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        await SaveProject();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                
                using var openDialog = new OpenFileDialog
                {
                    Title = "打开项目文件",
                    Filter = SimpleProjectManager.GetFileFilter(),
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    var success = await SimpleProjectManager.OpenProjectAsync(openDialog.FileName);
                    if (success)
                    {
                        logger.LogInfo($"已打开项目: {Path.GetFileName(openDialog.FileName)}");
                    }
                    else
                    {
                        logger.LogError("打开项目文件失败");
                        MessageBox.Show("打开项目文件失败，请检查文件格式是否正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"打开项目失败: {ex.Message}");
                MessageBox.Show($"打开项目失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SaveProjectMenuItem_Click(object? sender, EventArgs e)
        {
            await SaveProject();
        }

        private async void SaveAsProjectMenuItem_Click(object? sender, EventArgs e)
        {
            await SaveProjectAs();
        }

        private async Task<bool> SaveProject()
        {
            try
            {
                if (string.IsNullOrEmpty(SimpleProjectManager.CurrentFilePath))
                {
                    return await SaveProjectAs();
                }
                
                // 更新项目数据
                UpdateProjectData();
                
                var success = await SimpleProjectManager.SaveProjectAsync();
                if (success)
                {
                    logger.LogInfo("项目已保存");
                }
                else
                {
                    logger.LogError("保存项目失败");
                    MessageBox.Show("保存项目失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                logger.LogError($"保存项目失败: {ex.Message}");
                MessageBox.Show($"保存项目失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<bool> SaveProjectAs()
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Title = "另存为项目文件",
                    Filter = SimpleProjectManager.GetFileFilter(),
                    DefaultExt = "stproj",
                    FileName = SimpleProjectManager.CurrentProject?.Name ?? "新建项目",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    // 更新项目数据
                    UpdateProjectData();
                    
                    var success = await SimpleProjectManager.SaveAsProjectAsync(saveDialog.FileName);
                    if (success)
                    {
                        logger.LogInfo($"项目已另存为: {Path.GetFileName(saveDialog.FileName)}");
                    }
                    else
                    {
                        logger.LogError("另存为项目失败");
                        MessageBox.Show("另存为项目失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    
                    return success;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError($"另存为项目失败: {ex.Message}");
                MessageBox.Show($"另存为项目失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void UpdateProjectData()
        {
            try
            {
                // 更新点位数据
                SimpleProjectManager.SetPointData(pointData);
                
                // 更新生成的代码
                if (generatedScripts.Any())
                {
                    var combinedCode = string.Join("\n\n" + new string('-', 50) + "\n\n", generatedScripts);
                    SimpleProjectManager.SetGeneratedCode(combinedCode);
                }
                
                // 更新其他设置
                SimpleProjectManager.UpdateSettings("lastProcessedFile", uploadedFilePath);
                SimpleProjectManager.UpdateSettings("lastUpdateTime", DateTime.Now);
            }
            catch (Exception ex)
            {
                logger?.LogError($"更新项目数据时出错: {ex.Message}");
            }
        }

        private void ClearCurrentData()
        {
            try
            {
                // 清空当前数据
                pointData.Clear();
                generatedScripts.Clear();
                uploadedFilePath = "";
                
                // 清空文件列表
                fileListBox.Items.Clear();
                
                // 清空项目缓存
                ClearProjectCache();
                
                // 清空预览区域
                UpdatePreviewArea();
                
                // 更新状态
                UpdateStatusBarStats();
                
                logger.LogInfo("当前数据已清空");
            }
            catch (Exception ex)
            {
                logger?.LogError($"清空当前数据时出错: {ex.Message}");
            }
        }

        private async void CloseProjectMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                if (SimpleProjectManager.NeedsSave())
                {
                    var result = MessageBox.Show(
                        "当前项目有未保存的更改，是否保存？",
                        "确认", 
                        MessageBoxButtons.YesNoCancel, 
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        await SaveProject();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                
                SimpleProjectManager.CloseProject();
                ClearCurrentData();
                
                logger.LogInfo("项目已关闭");
            }
            catch (Exception ex)
            {
                logger.LogError($"关闭项目失败: {ex.Message}");
                MessageBox.Show($"关闭项目失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_upload_Click(object sender, EventArgs e)
        {
            try
            {
                logger.LogInfo("开始上传点表文件...");
                
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "选择点表文件";
                    openFileDialog.Filter = "Excel文件|*.xlsx|CSV文件|*.csv|所有文件|*.*";
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        uploadedFilePath = openFileDialog.FileName;
                        logger.LogInfo($"选择文件: {Path.GetFileName(uploadedFilePath)}");
                        
                        // 更新文件列表
                        UpdateFileList(uploadedFilePath);
                        
                        // 更新状态栏
                        UpdateProgressBar("正在处理文件...", 0, true);
                        
                        ProcessExcelFile(uploadedFilePath);
                        
                        // 处理完成后更新状态
                        UpdateProgressBar("文件处理完成", 100, false);
                        
                        // 标记项目有变更
                        UpdateProjectData();
                    }
                    else
                    {
                        logger.LogWarning("用户取消了文件选择");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"上传文件时出错: {ex.Message}");
                MessageBox.Show($"上传文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateFileList(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var fileSize = new FileInfo(filePath).Length;
            var displayText = $"{fileName} ({FormatFileSize(fileSize)})";
            
            // 避免重复添加
            if (!fileListBox.Items.Contains(displayText))
            {
                fileListBox.Items.Add(displayText);
                fileListBox.SelectedItem = displayText;
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            return $"{bytes / (1024 * 1024):F1} MB";
        }

        /// <summary>
        /// 新架构：Excel文件处理的单一入口点
        /// 使用ImportPipeline执行完整的"Excel解析 → 设备分类 → 代码生成"管道
        /// 实现"上传一次，处理一次，后续只读取缓存"的核心原则
        /// </summary>
        private async void ProcessExcelFile(string filePath)
        {
            try
            {
                logger.LogInfo("🚀 启动新架构Excel处理管道...");
                
                // 首先验证文件路径
                var pathValidation = BasicValidator.ValidateFilePath(filePath, true);
                if (!pathValidation.IsValid)
                {
                    logger.LogError($"文件路径验证失败: {string.Join(", ", pathValidation.Errors)}");
                    MessageBox.Show($"文件路径验证失败:\n{string.Join("\n", pathValidation.Errors)}", 
                        "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // ============ 关键架构改进：使用ImportPipeline作为单一处理入口 ============
                // 清除旧缓存以确保全新处理
                ClearProjectCache();
                
                // 通过ImportPipeline执行完整的数据处理管道
                var projectCache = await importPipeline.ImportAsync(filePath);
                if (projectCache == null)
                {
                    logger.LogError("ImportPipeline处理失败");
                    MessageBox.Show("Excel文件处理失败，请检查文件格式", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // 更新当前项目缓存
                lock (projectCacheLock)
                {
                    currentProjectCache = projectCache;
                }
                
                // 更新UI显示数据（从缓存读取，不再触发处理）
                UpdateUIFromProjectCache(projectCache);
                
                // 更新项目管理数据
                UpdateProjectData();
                
                logger.LogSuccess($"✅ Excel文件处理完成 - 设备数:{projectCache.Statistics.TotalDevices}, 点位数:{projectCache.Statistics.TotalPoints}, ST文件数:{projectCache.Statistics.GeneratedSTFiles}");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ 处理Excel文件时出错: {ex.Message}");
                MessageBox.Show($"处理Excel文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 处理失败时清除缓存
                ClearProjectCache();
            }
        }
        
        /// <summary>
        /// 从ProjectCache更新UI显示（只读模式）
        /// </summary>
        private void UpdateUIFromProjectCache(ProjectCache projectCache)
        {
            try
            {
                // 更新旧版pointData结构以保持向后兼容性
                pointData.Clear();
                
                // 从设备分类数据重建pointData格式
                if (projectCache.DataContext.Devices?.Any() == true)
                {
                    foreach (var device in projectCache.DataContext.Devices)
                    {
                        // 添加IO点位数据
                        foreach (var ioPoint in device.IoPoints)
                        {
                            pointData.Add(ioPoint.Value);
                        }
                        
                        // 添加设备点位数据
                        foreach (var devicePoint in device.DevicePoints)
                        {
                            pointData.Add(devicePoint.Value);
                        }
                    }
                }
                
                // 更新生成的代码列表
                generatedScripts.Clear();
                generatedScripts.AddRange(projectCache.IOMappingScripts);
                
                // 添加设备ST代码
                foreach (var devicePrograms in projectCache.DeviceSTPrograms.Values)
                {
                    generatedScripts.AddRange(devicePrograms);
                }
                
                // 添加TCP通讯程序
                if (projectCache.TcpCommunicationPrograms?.Any() == true)
                {
                    generatedScripts.AddRange(projectCache.TcpCommunicationPrograms);
                    logger.LogInfo($"📡 已添加 {projectCache.TcpCommunicationPrograms.Count} 个TCP通讯程序到显示列表");
                }
                
                // 刷新预览区域（从缓存读取）
                UpdatePreviewArea();
                
                // 更新状态栏统计
                UpdateStatusBarStats();
                
                // 更新提示上下文
                UpdateTooltipContext();
                
                logger.LogInfo($"📊 UI更新完成 - 显示数据来自ProjectCache");
            }
            catch (Exception ex)
            {
                logger.LogError($"❌ 从ProjectCache更新UI时出错: {ex.Message}");
            }
        }

        private async Task GenerateSTScriptsAsync()
        {
            try
            {
                logger.LogInfo("开始生成ST脚本...");
                generatedScripts.Clear();
                
                int successCount = 0;
                int errorCount = 0;
                int totalCount = pointData.Count;
                int processedCount = 0;
                var allGeneratedCode = new List<string>();
                
                UpdateProgressBar("正在生成ST脚本...", 0, false);
                
                foreach (var row in pointData)
                {
                    try
                    {
                        // 获取点位类型
                        var pointType = row.TryGetValue("模块类型", out var type) ? type?.ToString()?.Trim().ToUpper() : null;
                        
                        if (string.IsNullOrWhiteSpace(pointType))
                        {
                            logger.LogWarning($"跳过没有类型的行: {GetVariableName(row)}");
                            continue;
                        }
                        
                        // 检查是否支持该类型
                        if (!GeneratorFactory.IsSupported(pointType))
                        {
                            logger.LogWarning($"不支持的点位类型: {pointType}，变量名: {GetVariableName(row)}");
                            continue;
                        }
                        
                        // 获取生成器并生成代码
                        var generator = GeneratorFactory.GetGenerator(pointType);
                        var script = generator.Generate(row);
                        
                        if (!string.IsNullOrWhiteSpace(script))
                        {
                            generatedScripts.Add(script);
                            allGeneratedCode.Add(script);
                            successCount++;
                            logger.LogDebug($"成功生成{pointType}点位: {GetVariableName(row)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        logger.LogError($"生成点位脚本失败: {GetVariableName(row)} - {ex.Message}");
                    }
                    
                    // 更新进度
                    processedCount++;
                    int progress = (int)((double)processedCount / totalCount * 100);
                    UpdateProgressBar($"正在生成ST脚本... ({processedCount}/{totalCount})", progress, false);
                    
                    // 异步更新UI
                    await Task.Delay(1);
                }
                
                logger.LogSuccess($"ST脚本生成完成: 成功{successCount}个，失败{errorCount}个");
                
                // 验证生成的代码
                if (allGeneratedCode.Any())
                {
                    var combinedCode = string.Join("\n\n", allGeneratedCode);
                    var codeValidation = BasicValidator.ValidateGeneratedCode(combinedCode);
                    
                    if (!codeValidation.IsValid)
                    {
                        logger.LogWarning($"生成的代码验证失败: {string.Join(", ", codeValidation.Errors)}");
                        
                        var result = MessageBox.Show(
                            $"生成的ST代码验证发现问题:\n\n{string.Join("\n", codeValidation.Errors.Take(3))}\n\n是否继续使用这些代码？",
                            "代码验证", 
                            MessageBoxButtons.YesNo, 
                            MessageBoxIcon.Warning);
                        
                        if (result == DialogResult.No)
                        {
                            generatedScripts.Clear();
                            return;
                        }
                    }
                    else if (codeValidation.Warnings.Any())
                    {
                        logger.LogWarning($"代码验证警告: {string.Join(", ", codeValidation.Warnings)}");
                    }
                }
                
                // 更新状态栏统计
                UpdateStatusBarStats();
                
                if (generatedScripts.Any())
                {
                    // 更新预览区域
                    UpdatePreviewArea();
                    
                    // 标记项目有变更
                    UpdateProjectData();
                    
                    // 显示预览
                    logger.LogInfo("生成预览:");
                    logger.LogInfo("=" + new string('=', 50));
                    
                    // 显示所有脚本的完整内容
                    for (int i = 0; i < generatedScripts.Count; i++)
                    {
                        var script = generatedScripts[i];
                        var lines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines) // 显示完整内容
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                logger.LogInfo(line.Trim());
                        }
                        logger.LogInfo("");
                    }
                    
                    logger.LogInfo("=" + new string('=', 50));
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"生成ST脚本时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 使用新的标准化服务架构生成ST脚本
        /// </summary>
        private async Task GenerateSTScriptsWithNewServiceAsync()
        {
            try
            {
                logger.LogInfo("开始使用新服务架构生成ST脚本...");
                generatedScripts.Clear();
                
                // 设置路径
                var templateDirectory = Path.Combine(Application.StartupPath, "Templates");
                var configFilePath = Path.Combine(Application.StartupPath, "template-mapping.json");
                var tempExportPath = Path.Combine(Path.GetTempPath(), "STGeneration_" + DateTime.Now.ToString("yyyyMMddHHmmss"));
                
                // 确保配置文件存在
                if (!File.Exists(configFilePath))
                {
                    logger.LogWarning("配置文件不存在，使用默认配置");
                    // 创建默认配置
                    CreateDefaultTemplateMapping(configFilePath);
                }
                
                UpdateProgressBar("正在使用新架构生成ST脚本...", 0, true);
                
                // 使用新的STGenerationService和缓存机制
                var fileCount = await Task.Run(() => 
                {
                    // 使用缓存机制获取数据上下文，避免重复解析Excel
                    var dataContext = GetCachedDataContext(uploadedFilePath);
                    if (dataContext == null)
                    {
                        throw new Exception("无法加载Excel数据");
                    }
                    
                    // 使用新的重载方法，接受DataContext参数
                    return stGenerationService.GenerateSTCode(
                        dataContext,
                        templateDirectory,
                        configFilePath,
                        tempExportPath
                    );
                });
                
                logger.LogSuccess($"新架构ST脚本生成完成: 共生成{fileCount}个文件");
                
                // 读取生成的文件内容到generatedScripts中，以保持与现有UI的兼容性
                await LoadGeneratedFilesFromTempDirectory(tempExportPath);
                
                // 更新状态栏统计
                UpdateStatusBarStats();
                
                if (generatedScripts.Any())
                {
                    // 更新预览区域
                    UpdatePreviewArea();
                    
                    // 标记项目有变更
                    UpdateProjectData();
                    
                    logger.LogInfo($"新架构生成预览 - 共{generatedScripts.Count}个脚本文件");
                }
                
                UpdateProgressBar("ST脚本生成完成", 100, false);
            }
            catch (Exception ex)
            {
                logger.LogError($"使用新架构生成ST脚本时出错: {ex.Message}");
                UpdateProgressBar("生成失败", 0, false);
                throw;
            }
        }
        
        /// <summary>
        /// 创建默认的模板映射配置文件
        /// </summary>
        private void CreateDefaultTemplateMapping(string configFilePath)
        {
            try
            {
                // 确保配置文件目录存在
                var configDir = Path.GetDirectoryName(configFilePath);
                if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                var defaultMapping = new Dictionary<string, string>
                {
                    // 设备模板映射（示例）
                    ["MOV_CTRL"] = "StandardValve.scriban",
                    ["ESDV_CTRL"] = "EmergencyShutdownValve.scriban", 
                    ["CONTROL_VALVE"] = "AnalogControlValve_PID.scriban",
                    ["PUMP_CTRL"] = "CentrifugalPump.scriban",
                    
                    // IO映射模板
                    ["AI_MAPPING"] = "AI/default.scriban",
                    ["AO_MAPPING"] = "AO/default.scriban", 
                    ["DI_MAPPING"] = "DI/default.scriban",
                    ["DO_MAPPING"] = "DO/default.scriban",
                    
                    // 注释字段
                    ["_comment"] = "--- 模板映射配置文件 ---",
                    ["_description"] = "此文件定义了设备模板名称与实际Scriban模板文件的映射关系",
                    ["_version"] = "1.0",
                    ["_created"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(defaultMapping, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // 支持中文字符
                });
                
                File.WriteAllText(configFilePath, json, System.Text.Encoding.UTF8);
                logger.LogInfo($"已创建默认模板映射配置文件: {Path.GetFileName(configFilePath)}");
                logger.LogInfo($"配置文件包含 {defaultMapping.Count - 4} 个有效映射");
            }
            catch (Exception ex)
            {
                logger.LogError($"创建默认模板映射配置文件失败: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 从临时目录加载生成的文件内容
        /// </summary>
        private async Task LoadGeneratedFilesFromTempDirectory(string tempPath)
        {
            try
            {
                if (!Directory.Exists(tempPath))
                {
                    logger.LogWarning("临时生成目录不存在");
                    return;
                }
                
                var stFiles = Directory.GetFiles(tempPath, "*.st", SearchOption.AllDirectories);
                foreach (var file in stFiles)
                {
                    var content = await File.ReadAllTextAsync(file, System.Text.Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        generatedScripts.Add(content);
                    }
                }
                
                logger.LogInfo($"从临时目录加载了{stFiles.Length}个ST文件");
                
                // 清理临时目录
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"加载生成文件时出错: {ex.Message}");
            }
        }

        private string GetVariableName(Dictionary<string, object> row)
        {
            // 尝试多个可能的字段名
            var possibleNames = new[] { "变量名称（HMI）", "变量名称", "变量名", "标识符", "名称" };
            
            foreach (var name in possibleNames)
            {
                if (row.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    return value.ToString()!.Trim();
                }
            }
            
            return "未知";
        }

        private void UpdatePreviewArea()
        {
            if (isUpdatingPreview) return; // 防抖，避免递归或短时间多次刷新
            isUpdatingPreview = true;
            try
            {
                // 更新IO映射ST程序预览标签页
                var previewTextBox = previewTabControl.TabPages[0].Controls["previewTextBox"] as RichTextBox;
                if (previewTextBox != null)
                {
                    var ioMappingContent = GenerateIOMappingPreview();
                    previewTextBox.Text = ioMappingContent;
                }
                
                // 更新设备ST程序标签页
                var deviceSTTextBox = previewTabControl.TabPages[1].Controls["deviceSTTextBox"] as RichTextBox;
                if (deviceSTTextBox != null)
                {
                    var selectedDevice = GetSelectedDevice();
                    var deviceSTContent = GenerateDeviceSTPreview(selectedDevice);
                    deviceSTTextBox.Text = deviceSTContent;
                }
                
                // 刷新设备列表（仅在数据变化时）
                RefreshDeviceListIfNeeded();
                
                // 更新统计信息标签页
                var statsTextBox = previewTabControl.TabPages[2].Controls["statsTextBox"] as RichTextBox;
                if (statsTextBox != null)
                {
                    var stats = GenerateStatistics();
                    statsTextBox.Text = stats;
                }
                
                // 更新文件信息标签页
                var fileInfoTextBox = previewTabControl.TabPages[3].Controls["fileInfoTextBox"] as RichTextBox;
                if (fileInfoTextBox != null)
                {
                    var fileInfo = GenerateFileInfo();
                    fileInfoTextBox.Text = fileInfo;
                }
                
                // 更新模板信息标签页
                var templateTextBox = previewTabControl.TabPages[4].Controls["templateTextBox"] as RichTextBox;
                if (templateTextBox != null)
                {
                    var templateInfo = GenerateTemplateInfo();
                    templateTextBox.Text = templateInfo;
                }
                
                // 更新TCP通讯ST程序标签页
                var tcpCommTextBox = previewTabControl.TabPages[5].Controls["tcpCommTextBox"] as RichTextBox;
                if (tcpCommTextBox != null)
                {
                    var tcpCommContent = GenerateTcpCommPreview();
                    
                    // 检查内容长度和完整性
                    logger.LogInfo($"[DEBUG] TCP预览内容长度: {tcpCommContent.Length} 字符");
                    logger.LogInfo($"[DEBUG] 内容结尾: {tcpCommContent.Substring(Math.Max(0, tcpCommContent.Length - 100))}");
                    
                    // 设置RichTextBox最大长度以避免截断
                    tcpCommTextBox.MaxLength = int.MaxValue;
                    tcpCommTextBox.Text = tcpCommContent;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"更新预览区域失败: {ex.Message}");
            }
            finally
            {
                isUpdatingPreview = false;
            }
        }


        private string GenerateStatistics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("📊 点位统计信息");
            sb.AppendLine("=" + new string('=', 30));
            sb.AppendLine();
            
            sb.AppendLine($"📈 总点位数量: {pointData.Count}");
            sb.AppendLine($"📝 生成脚本数: {generatedScripts.Count}");
            sb.AppendLine();
            
            // 按类型统计
            var typeStats = pointData.GroupBy(p => 
                p.TryGetValue("模块类型", out var type) ? type?.ToString() : "未知")
                .ToDictionary(g => g.Key ?? "未知", g => g.Count());
            
            sb.AppendLine("🔢 按类型统计:");
            foreach (var kvp in typeStats)
            {
                sb.AppendLine($"  • {kvp.Key}: {kvp.Value} 个");
            }
            
            sb.AppendLine();
            sb.AppendLine($"⏰ 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            if (!string.IsNullOrEmpty(uploadedFilePath))
            {
                sb.AppendLine($"📁 源文件: {Path.GetFileName(uploadedFilePath)}");
                sb.AppendLine($"📐 文件大小: {FormatFileSize(new FileInfo(uploadedFilePath).Length)}");
            }
            
            return sb.ToString();
        }

        private string GenerateFileInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("📁 文件信息详情");
            sb.AppendLine("=" + new string('=', 30));
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(uploadedFilePath))
            {
                var fileInfo = new FileInfo(uploadedFilePath);
                sb.AppendLine($"📂 文件名: {fileInfo.Name}");
                sb.AppendLine($"📁 路径: {fileInfo.DirectoryName}");
                sb.AppendLine($"📐 大小: {FormatFileSize(fileInfo.Length)}");
                sb.AppendLine($"📅 修改时间: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"📝 类型: {Path.GetExtension(uploadedFilePath).ToUpper()} 文件");
                sb.AppendLine();
                
                // 最近处理的文件列表
                if (recentFiles.Any())
                {
                    sb.AppendLine("📋 最近处理的文件:");
                    for (int i = 0; i < Math.Min(5, recentFiles.Count); i++)
                    {
                        var fileName = Path.GetFileName(recentFiles[i]);
                        sb.AppendLine($"  {i + 1}. {fileName}");
                    }
                }
            }
            else
            {
                sb.AppendLine("📭 暂无文件信息");
                sb.AppendLine("请先上传点表文件以查看详细信息。");
            }
            
            return sb.ToString();
        }

        private string GenerateTemplateInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("🎨 模板信息");
            sb.AppendLine("=" + new string('=', 30));
            sb.AppendLine();
            
            sb.AppendLine("📝 当前使用的模板:");
            sb.AppendLine("  • AI点位: Templates/AI/default.scriban");
            sb.AppendLine("  • AO点位: Templates/AO/default.scriban");
            sb.AppendLine("  • DI点位: Templates/DI/default.scriban");
            sb.AppendLine("  • DO点位: Templates/DO/default.scriban");
            sb.AppendLine();
            
            sb.AppendLine("🔧 模板引擎: Scriban v6.2.1");
            sb.AppendLine("📊 模板特性:");
            sb.AppendLine("  ✅ 支持条件判断");
            sb.AppendLine("  ✅ 支持循环语句");
            sb.AppendLine("  ✅ 支持变量替换");
            sb.AppendLine("  ✅ 支持中文字段名");
            sb.AppendLine();
            
            sb.AppendLine("🎯 模板版本: 默认版本 (v1.0)");
            sb.AppendLine("📅 更新时间: 2025-01-28");
            
            return sb.ToString();
        }
        
        private string GenerateTcpCommPreview()
        {
            var sb = new StringBuilder();
            sb.AppendLine("🌐 TCP通讯ST程序");
            sb.AppendLine("=" + new string('=', 50));
            sb.AppendLine();
            
            try
            {
                // 添加调试信息
                logger.LogInfo($"[DEBUG] 检查TCP通讯数据: currentProjectCache={currentProjectCache != null}");
                if (currentProjectCache != null)
                {
                    logger.LogInfo($"[DEBUG] TcpCommunicationPrograms={currentProjectCache.TcpCommunicationPrograms != null}, Count={currentProjectCache.TcpCommunicationPrograms?.Count ?? -1}");
                }
                
                // 检查是否有项目缓存数据
                if (currentProjectCache?.TcpCommunicationPrograms == null || !currentProjectCache.TcpCommunicationPrograms.Any())
                {
                    sb.AppendLine("⚠️ 当前项目中未检测到TCP通讯点位数据");
                    sb.AppendLine();
                    sb.AppendLine("支持的TCP通讯点位类型:");
                    sb.AppendLine("  • TCP模拟量点位 (REAL, INT, DINT)");
                    sb.AppendLine("  • TCP数字量点位 (BOOL)");
                    sb.AppendLine();
                    sb.AppendLine("请确保Excel点表文件中包含以下字段:");
                    sb.AppendLine("  • 数据类型: REAL/INT/DINT/BOOL");
                    sb.AppendLine("  • 起始TCP通道名称: 如 MW_100, DB1_100等");
                    sb.AppendLine("  • 变量名称（HMI）: 如 TEMP_001_PV");
                    sb.AppendLine("  • 变量描述: 如 温度传感器1");
                    sb.AppendLine();
                    sb.AppendLine("模拟量点位额外支持:");
                    sb.AppendLine("  • 缩放倍数: 数值缩放");
                    sb.AppendLine("  • 报警限值: SHH值, SH值, SL值, SLL值");
                    sb.AppendLine("  • 字节序: BYTE_ORDER");
                    sb.AppendLine("  • 数据类型编号: TYPE_NUMBER");
                    
                    return sb.ToString();
                }
                
                // 统计TCP通讯点位
                var tcpPrograms = currentProjectCache.TcpCommunicationPrograms;
                sb.AppendLine($"📊 TCP通讯程序统计: 共 {tcpPrograms.Count} 个程序段");
                sb.AppendLine();
                
                // 重新设计TCP程序分类逻辑 - 精确分类
                var analogPrograms = new List<string>();
                var digitalPrograms = new List<string>();
                
                logger.LogInfo($"[DEBUG] 开始分类TCP程序, 总数: {tcpPrograms.Count}");
                
                for (int i = 0; i < tcpPrograms.Count; i++)
                {
                    var program = tcpPrograms[i];
                    if (string.IsNullOrWhiteSpace(program))
                    {
                        logger.LogInfo($"[DEBUG] 跳过空程序 #{i + 1}");
                        continue;
                    }
                    
                    // 跳过无意义的注释程序 - 改进过滤条件
                    var trimmedProgram = program.Trim();
                    if ((trimmedProgram.StartsWith("// TCP") || trimmedProgram.StartsWith("//TCP")) && program.Length < 50)
                    {
                        logger.LogInfo($"[DEBUG] 跳过注释程序 #{i + 1}: {trimmedProgram}");
                        continue;
                    }
                    
                    // 额外过滤：跳过长度很短且只包含注释的程序
                    if (program.Length < 30 && (trimmedProgram.StartsWith("//") || trimmedProgram.StartsWith("(*")))
                    {
                        logger.LogInfo($"[DEBUG] 跳过短注释程序 #{i + 1}: {trimmedProgram}");
                        continue;
                    }
                    
                    logger.LogInfo($"[DEBUG] 分析程序 #{i + 1}: 长度={program.Length}, 预览={program.Substring(0, Math.Min(80, program.Length)).Replace('\n', ' ')}...");
                    
                    // 第一步：检查明确的模拟量标识
                    bool hasAnalogMarkers = program.Contains("DATA_CONVERT_BY_BYTE") || 
                                           program.Contains("AI_ALARM_COMMUNICATION") ||
                                           program.Contains("TCP模拟量数据采集") ||
                                           program.Contains("TCP模拟量数据缩放") ||
                                           program.Contains("RESULT_REAL") ||
                                           program.Contains("RESULT_INT") ||
                                           program.Contains("RESULT_DINT");
                    
                    // 第二步：检查明确的数字量标识
                    bool hasDigitalMarkers = program.Contains("TCP状态量数据采集") ||
                                            (program.Contains(":=") && !hasAnalogMarkers && 
                                             program.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length <= 8);
                    
                    if (hasAnalogMarkers)
                    {
                        analogPrograms.Add(program);
                        logger.LogInfo($"[DEBUG] 分类为模拟量: 包含模拟量标识");
                    }
                    else if (hasDigitalMarkers)
                    {
                        digitalPrograms.Add(program);
                        logger.LogInfo($"[DEBUG] 分类为数字量: 包含数字量标识");
                    }
                    else
                    {
                        // 如果都不明确，根据程序复杂度判断
                        var lines = program.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length <= 5 && program.Contains(":=") && !program.Contains("("))
                        {
                            digitalPrograms.Add(program);
                            logger.LogInfo($"[DEBUG] 分类为数字量: 简单赋值语句");
                        }
                        else
                        {
                            analogPrograms.Add(program);
                            logger.LogInfo($"[DEBUG] 默认分类为模拟量: 复杂程序");
                        }
                    }
                }
                
                logger.LogInfo($"[DEBUG] 分类结果: 模拟量={analogPrograms.Count}, 数字量={digitalPrograms.Count}");
                
                if (analogPrograms.Any())
                {
                    sb.AppendLine($"🔄 TCP模拟量程序段 ({analogPrograms.Count} 个):");
                    sb.AppendLine("─" + new string('─', 45));
                    
                    foreach (var program in analogPrograms) // 显示所有模拟量程序
                    {
                        var lines = program.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        
                        // 改进的程序名称提取
                        var programName = ExtractTcpProgramName(lines, "模拟量");
                        sb.AppendLine($"  • {programName}");
                        sb.AppendLine("    " + new string('-', 40));
                        
                        // 显示完整ST程序内容
                        var cleanLines = program.Split('\n');
                        int lineCount = 0;
                        foreach (var line in cleanLines)
                        {
                            var trimmedLine = line.Trim();
                            if (!string.IsNullOrEmpty(trimmedLine) && 
                                !trimmedLine.StartsWith("程序名称:") && 
                                !trimmedLine.StartsWith("变量类型:"))
                            {
                                sb.AppendLine($"    {trimmedLine}");
                                lineCount++;
                            }
                        }
                        logger.LogInfo($"[DEBUG] 模拟量程序 {programName}: 显示了 {lineCount} 行代码, 原始程序长度={program.Length}");
                        sb.AppendLine();
                    }
                }
                
                if (digitalPrograms.Any())
                {
                    sb.AppendLine($"💡 TCP数字量程序段 ({digitalPrograms.Count} 个):");
                    sb.AppendLine("─" + new string('─', 45));
                    
                    foreach (var program in digitalPrograms) // 显示所有数字量程序
                    {
                        var lines = program.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        
                        // 改进的程序名称提取
                        var programName = ExtractTcpProgramName(lines, "数字量");
                        sb.AppendLine($"  • {programName}");
                        sb.AppendLine("    " + new string('-', 40));
                        
                        // 显示完整ST程序内容
                        var cleanLines = program.Split('\n');
                        int lineCount = 0;
                        foreach (var line in cleanLines)
                        {
                            var trimmedLine = line.Trim();
                            if (!string.IsNullOrEmpty(trimmedLine) && 
                                !trimmedLine.StartsWith("程序名称:") && 
                                !trimmedLine.StartsWith("变量类型:"))
                            {
                                sb.AppendLine($"    {trimmedLine}");
                                lineCount++;
                            }
                        }
                        logger.LogInfo($"[DEBUG] 数字量程序 {programName}: 显示了 {lineCount} 行代码, 原始程序长度={program.Length}");
                        sb.AppendLine();
                    }
                }
                
                sb.AppendLine("📝 使用的模板:");
                sb.AppendLine("  • TCP模拟量: Templates/TCP通讯/ANALOG.scriban");
                sb.AppendLine("  • TCP数字量: Templates/TCP通讯/DIGITAL.scriban");
                
                // 最终内容检查
                var finalContent = sb.ToString();
                logger.LogInfo($"[DEBUG] TCP预览生成完成: 总长度={finalContent.Length} 字符");
                logger.LogInfo($"[DEBUG] 内容最后100字符: {finalContent.Substring(Math.Max(0, finalContent.Length - 100))}");
                return finalContent;
            }
            catch (Exception ex)
            {
                sb.AppendLine($"❌ 生成TCP通讯预览时出错: {ex.Message}");
                logger.LogError($"生成TCP通讯预览失败: {ex.Message}");
                logger.LogError($"异常详情: {ex}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// 提取TCP程序名称
        /// </summary>
        private string ExtractTcpProgramName(string[] lines, string programType)
        {
            if (lines == null || !lines.Any()) return $"未知{programType}程序";
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 尝试从注释中提取程序名称
                if (trimmedLine.StartsWith("(*") && trimmedLine.EndsWith("*)"))
                {
                    // 移除注释标记
                    var comment = trimmedLine.Substring(2, trimmedLine.Length - 4).Trim();
                    
                    // 如果包含TCP数据采集关键词，提取变量名
                    if (comment.Contains("TCP模拟量数据采集:") || comment.Contains("TCP状态量数据采集:"))
                    {
                        var parts = comment.Split(new[] { ":", "->" }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var variableName = parts[1].Trim();
                            var description = parts.Length > 2 ? parts[2].Trim() : "";
                            return string.IsNullOrEmpty(description) ? variableName : $"{variableName} ({description})";
                        }
                    }
                    
                    // 返回完整注释作为程序名
                    if (!string.IsNullOrEmpty(comment))
                    {
                        return comment.Length > 50 ? comment.Substring(0, 47) + "..." : comment;
                    }
                }
                
                // 尝试从函数调用中提取变量名（如DATA_CONVERT_BY_BYTE_XXX）
                if (trimmedLine.Contains("DATA_CONVERT_BY_BYTE_"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"DATA_CONVERT_BY_BYTE_(\w+)");
                    if (match.Success)
                    {
                        return $"TCP模拟量转换: {match.Groups[1].Value}";
                    }
                }
                
                // 尝试从简单赋值中提取变量名（数字量程序）
                if (trimmedLine.Contains(":=") && programType == "数字量")
                {
                    var parts = trimmedLine.Split(":=");
                    if (parts.Length >= 2)
                    {
                        var variableName = parts[0].Trim();
                        var value = parts[1].Trim().TrimEnd(';');
                        return $"TCP数字量: {variableName} := {value}";
                    }
                }
            }
            
            return $"TCP{programType}程序";
        }

        /// <summary>
        /// 获取TCP程序的关键代码预览
        /// </summary>
        private List<string> GetTcpProgramPreview(string[] lines, int maxLines)
        {
            var previewLines = new List<string>();
            int addedLines = 0;
            
            foreach (var line in lines)
            {
                if (addedLines >= maxLines) break;
                
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;
                
                // 跳过空行和某些不重要的行
                if (trimmedLine.StartsWith("程序名称:") || 
                    trimmedLine.StartsWith("变量类型:"))
                {
                    continue;
                }
                
                // 添加重要的代码行
                previewLines.Add(trimmedLine);
                addedLines++;
            }
            
            return previewLines;
        }

        private async void button_export_Click(object sender, EventArgs e)
        {
            try
            {
                logger.LogInfo("开始导出ST脚本...");
                
                // 检查新架构的ProjectCache数据
                if (currentProjectCache == null || 
                    (!currentProjectCache.IOMappingScripts.Any() && !currentProjectCache.DeviceSTPrograms.Any()))
                {
                    logger.LogWarning("没有可导出的ST脚本，请先上传并处理点表文件");
                    MessageBox.Show("没有可导出的ST脚本，请先上传并处理点表文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 执行完整验证
                logger.LogInfo("正在执行导出前验证...");
                
                // 从ProjectCache获取所有代码进行验证
                var allCodes = new List<string>();
                allCodes.AddRange(currentProjectCache.IOMappingScripts);
                foreach (var deviceGroup in currentProjectCache.DeviceSTPrograms.Values)
                {
                    allCodes.AddRange(deviceGroup);
                }
                
                var combinedCode = string.Join("\n\n", allCodes);
                // 使用原始pointData进行验证，因为BasicValidator期望Dictionary格式
                var allPointData = pointData; // 使用现有的pointData列表
                var fullValidation = await BasicValidator.RunFullValidationAsync(allPointData, "", combinedCode);
                
                if (!fullValidation.IsValid)
                {
                    logger.LogError($"导出前验证失败: {fullValidation.Summary}");
                    var result = MessageBox.Show(
                        $"导出前验证发现问题:\n\n{fullValidation.Summary}\n\n错误详情:\n{string.Join("\n", fullValidation.Errors.Take(5))}\n\n是否仍要继续导出？",
                        "验证失败", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.No)
                    {
                        return;
                    }
                }
                else if (fullValidation.Warnings.Any())
                {
                    logger.LogWarning($"导出验证警告: {string.Join(", ", fullValidation.Warnings)}");
                    MessageBox.Show(
                        $"验证通过但有警告:\n\n{string.Join("\n", fullValidation.Warnings.Take(3))}\n\n将继续导出...",
                        "验证警告", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }

                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "选择输出文件夹";
                    folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportSTScriptsFromProjectCache(folderDialog.SelectedPath);
                    }
                    else
                    {
                        logger.LogWarning("用户取消了文件夹选择");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"导出ST脚本时出错: {ex.Message}");
                MessageBox.Show($"导出ST脚本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ExportSTScripts(string selectedPath)
        {
            try
            {
                logger.LogInfo($"正在分类保存ST脚本到: {selectedPath}");
                
                var outputDirectory = OutputWriter.WriteCategorizedFiles(generatedScripts, pointData, selectedPath);
                
                logger.LogSuccess($"ST脚本分类导出成功");
                logger.LogInfo($"共导出{generatedScripts.Count}个点位的ST代码");
                
                // 导出成功后询问是否保存项目
                var saveProjectResult = MessageBox.Show(
                    $"ST脚本导出成功!\n\n输出文件夹: {Path.GetFileName(outputDirectory)}\n位置: {outputDirectory}\n点位数量: {generatedScripts.Count}\n\n是否保存当前项目？",
                    "导出成功", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Information);
                
                if (saveProjectResult == DialogResult.Yes)
                {
                    // 更新项目数据
                    UpdateProjectData();
                    SimpleProjectManager.UpdateSettings("lastExportPath", outputDirectory);
                    SimpleProjectManager.UpdateSettings("lastExportTime", DateTime.Now);
                    
                    // 保存项目
                    var projectSaved = await SaveProject();
                    if (projectSaved)
                    {
                        logger.LogInfo("项目已保存");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"保存ST脚本文件时出错: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 新架构：从ProjectCache导出ST脚本，支持所有模板类型
        /// </summary>
        private async void ExportSTScriptsFromProjectCache(string selectedPath)
        {
            try
            {
                logger.LogInfo($"正在从ProjectCache导出ST脚本到: {selectedPath}");
                
                // 创建输出文件夹
                var folderName = $"ST_Scripts_{DateTime.Now:yyyyMMdd_HHmmss}";
                var outputDirectory = Path.Combine(selectedPath, folderName);
                Directory.CreateDirectory(outputDirectory);
                
                logger.LogInfo($"创建输出文件夹: {folderName}");
                
                int totalFiles = 0;
                var exportedFiles = new List<string>();
                
                // 1. 导出IO映射脚本 - 按通道类型分类导出
                if (currentProjectCache.IOMappingScripts.Any())
                {
                    logger.LogInfo($"开始分类导出IO映射脚本，共{currentProjectCache.IOMappingScripts.Count}个脚本");
                    
                    // 使用现有的分类方法将IO映射脚本按通道类型分类
                    var ioMappingByType = ConvertIOMappingScriptsToTemplateGroups(currentProjectCache.IOMappingScripts);
                    
                    // 定义文件名映射（将模板类型映射为用户要求的文件名）
                    var fileNameMapping = new Dictionary<string, string>
                    {
                        { "AI_CONVERT", "AI_CONVERT.txt" },
                        { "AO_CONVERT", "AO_CONVERT.txt" },
                        { "DI_CONVERT", "DI_MAPPING.txt" },
                        { "DO_CONVERT", "DO_MAPPING.txt" }
                    };
                    
                    // 为每个通道类型创建独立的txt文件
                    foreach (var typeGroup in ioMappingByType)
                    {
                        var templateType = typeGroup.Key;
                        var scripts = typeGroup.Value;
                        
                        if (scripts.Any() && fileNameMapping.ContainsKey(templateType))
                        {
                            var fileName = fileNameMapping[templateType];
                            var filePath = Path.Combine(outputDirectory, fileName);
                            var content = GenerateFileHeader($"IO映射脚本 - {templateType}通道") + string.Join("\n\n", scripts);
                            // 应用过滤功能，移除程序名称和变量类型标识行
                            var filteredContent = FilterMetadataLines(content);
                            File.WriteAllText(filePath, filteredContent, Encoding.UTF8);
                            
                            totalFiles++;
                            exportedFiles.Add($"{templateType}通道: {fileName} ({scripts.Count}个脚本)");
                            logger.LogInfo($"导出{templateType}通道IO映射脚本: {fileName} ({scripts.Count}个脚本)");
                        }
                    }
                    
                    // 如果有无法分类的脚本，单独导出
                    var totalClassifiedScripts = ioMappingByType.Values.Sum(scripts => scripts.Count);
                    if (totalClassifiedScripts < currentProjectCache.IOMappingScripts.Count)
                    {
                        var unclassifiedCount = currentProjectCache.IOMappingScripts.Count - totalClassifiedScripts;
                        logger.LogWarning($"有{unclassifiedCount}个IO映射脚本无法分类，将包含在所有分类文件中");
                    }
                    
                    logger.LogSuccess($"IO映射脚本分类导出完成，共生成{ioMappingByType.Count}个文件");
                }
                
                // 2. 导出设备ST程序（动态处理所有模板类型）
                foreach (var templateGroup in currentProjectCache.DeviceSTPrograms)
                {
                    var templateName = templateGroup.Key;
                    var deviceCodes = templateGroup.Value;
                    
                    if (deviceCodes.Any())
                    {
                        var fileName = $"Device_{templateName}.txt";
                        var filePath = Path.Combine(outputDirectory, fileName);
                        var content = GenerateFileHeader($"设备ST程序 - {templateName}模板") + string.Join("\n\n", deviceCodes);
                        // 应用过滤功能，移除程序名称和变量类型标识行
                        var filteredContent = FilterMetadataLines(content);
                        File.WriteAllText(filePath, filteredContent, Encoding.UTF8);
                        
                        totalFiles++;
                        exportedFiles.Add($"{templateName}设备: {fileName} ({deviceCodes.Count}个设备)");
                        logger.LogInfo($"导出{templateName}设备ST程序: {fileName} ({deviceCodes.Count}个设备)");
                    }
                }
                
                // 3. 导出TCP通讯脚本
                if (currentProjectCache.TcpCommunicationPrograms?.Any() == true)
                {
                    var analogPrograms = new List<string>();
                    var digitalPrograms = new List<string>();

                    foreach (var program in currentProjectCache.TcpCommunicationPrograms)
                    {
                        var hasAnalogMarkers = program.Contains("DATA_CONVERT_BY_BYTE") ||
                                                program.Contains("AI_ALARM_COMMUNICATION") ||
                                                program.Contains("TCP模拟量数据采集") ||
                                                program.Contains("TCP模拟量数据缩放") ||
                                                program.Contains("RESULT_REAL") ||
                                                program.Contains("RESULT_INT") ||
                                                program.Contains("RESULT_DINT");

                        var hasDigitalMarkers = !hasAnalogMarkers && (
                            program.Contains("TCP状态量数据采集") ||
                            program.Contains("TCP数字量数据采集") ||
                            program.Contains("DI_DEBOUNCE_EDGE") ||
                            (program.Contains(":=") && program.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).Length <= 12));

                        if (hasAnalogMarkers)
                            analogPrograms.Add(program);
                        else if (hasDigitalMarkers)
                            digitalPrograms.Add(program);
                    }

                    if (analogPrograms.Any())
                    {
                        var fileName = "TCP_ANALOG.txt";
                        var filePath = Path.Combine(outputDirectory, fileName);
                        var content = GenerateFileHeader("TCP模拟量脚本") + string.Join("\n\n", analogPrograms);
                        var filteredContent = FilterMetadataLines(content);
                        File.WriteAllText(filePath, filteredContent, Encoding.UTF8);
                        totalFiles++;
                        exportedFiles.Add($"TCP模拟量: {fileName} ({analogPrograms.Count}个脚本)");
                        logger.LogInfo($"导出TCP模拟量脚本: {fileName} ({analogPrograms.Count}个脚本)");
                    }

                    if (digitalPrograms.Any())
                    {
                        var fileName = "TCP_DIGITAL.txt";
                        var filePath = Path.Combine(outputDirectory, fileName);
                        var content = GenerateFileHeader("TCP状态量脚本") + string.Join("\n\n", digitalPrograms);
                        var filteredContent = FilterMetadataLines(content);
                        File.WriteAllText(filePath, filteredContent, Encoding.UTF8);
                        totalFiles++;
                        exportedFiles.Add($"TCP状态量: {fileName} ({digitalPrograms.Count}个脚本)");
                        logger.LogInfo($"导出TCP状态量脚本: {fileName} ({digitalPrograms.Count}个脚本)");
                    }
                }

                // 4. 生成变量表Excel文件
                logger.LogInfo("=== 开始尝试生成变量表Excel文件 ===");
                try
                {
                    logger.LogInfo("调用GenerateVariableTable方法...");
                    var variableTableGenerated = await GenerateVariableTable(outputDirectory);
                    logger.LogInfo($"GenerateVariableTable方法返回结果: {variableTableGenerated}");
                    
                    if (variableTableGenerated)
                    {
                        totalFiles++;
                        exportedFiles.Add("变量表: Variables_Table.xls");
                        logger.LogSuccess("变量表生成成功并添加到导出文件列表");
                    }
                    else
                    {
                        logger.LogWarning("GenerateVariableTable返回false，未生成变量表");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"生成变量表时发生异常: {ex.Message}");
                    logger.LogError($"异常堆栈: {ex.StackTrace}");
                }
                logger.LogInfo("=== 变量表生成流程结束 ===");
                
                // 4. 导出统计信息
                var statsFileName = "Export_Statistics.txt";
                var statsFilePath = Path.Combine(outputDirectory, statsFileName);
                var statsContent = GenerateExportStatistics();
                File.WriteAllText(statsFilePath, statsContent, Encoding.UTF8);
                totalFiles++;
                
                logger.LogSuccess($"ProjectCache导出完成，共生成{totalFiles}个文件");
                
                // 显示导出成功信息
                var exportSummary = string.Join("\n", exportedFiles);
                var saveProjectResult = MessageBox.Show(
                    $"ST脚本导出成功!\n\n输出文件夹: {Path.GetFileName(outputDirectory)}\n位置: {outputDirectory}\n\n导出文件:\n{exportSummary}\n\n是否保存当前项目？",
                    "导出成功", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Information);
                
                if (saveProjectResult == DialogResult.Yes)
                {
                    // 更新项目数据
                    UpdateProjectData();
                    SimpleProjectManager.UpdateSettings("lastExportPath", outputDirectory);
                    SimpleProjectManager.UpdateSettings("lastExportTime", DateTime.Now);
                    
                    // 保存项目
                    var projectSaved = await SaveProject();
                    if (projectSaved)
                    {
                        logger.LogInfo("项目已保存");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"从ProjectCache导出ST脚本时出错: {ex.Message}");
                MessageBox.Show($"导出ST脚本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
        
        /// <summary>
        /// 分类导出ST脚本按钮事件处理方法
        /// </summary>
        private async void button_categorized_export_Click(object sender, EventArgs e)
        {
            try
            {
                logger.LogInfo("开始执行分类导出ST脚本...");
                
                // 检查是否已初始化服务
                if (categorizedExportService == null)
                {
                    logger.LogError("分类导出服务未初始化，请重启程序或联系技术支持");
                    MessageBox.Show("分类导出服务未初始化，请重启程序后重试", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // 检查是否有ProjectCache数据
                if (currentProjectCache == null || 
                    currentProjectCache.IOMappingScripts == null || 
                    !currentProjectCache.IOMappingScripts.Any())
                {
                    logger.LogWarning("没有可分类导出的ST脚本，请先上传并处理点表文件");
                    MessageBox.Show("没有可分类导出的ST脚本，请先上传并处理点表文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // 选择导出目录
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "选择分类导出目录";
                    folderDialog.ShowNewFolderButton = true;
                    
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        var selectedPath = folderDialog.SelectedPath;
                        
                        // 显示进度条
                        UpdateProgressBar("正在执行分类导出...", 0, true);
                        
                        // 先将IO映射脚本转换为分类脚本
                        var categorizedScripts = new List<CategorizedScript>();
                        for (int i = 0; i < currentProjectCache.IOMappingScripts.Count; i++)
                        {
                            var scriptContent = currentProjectCache.IOMappingScripts[i];
                            categorizedScripts.Add(new CategorizedScript
                            {
                                Content = scriptContent,
                                Category = ScriptCategory.UNKNOWN, // 需要分类器来判断
                                DeviceTag = $"Script_{i + 1}" // 临时标识
                            });
                        }
                        
                        // 创建导出配置
                        var config = AutomaticGeneration_ST.Models.ExportConfiguration.CreateDefault(selectedPath);
                        config.OverwriteExisting = true;
                        config.IncludeTimestamp = false;
                        
                        // 执行分类导出
                        var exportResult = await Task.Run(() => 
                            categorizedExportService.ExportScriptsByCategory(
                                categorizedScripts, 
                                config));
                        
                        UpdateProgressBar("分类导出完成", 100, false);
                        
                        if (exportResult.IsSuccess)
                        {
                            // 生成成功统计信息
                            var statsMessage = GenerateCategorizedExportStats(exportResult, selectedPath);
                            
                            logger.LogSuccess($"分类导出成功! 共导出{exportResult.Statistics.TotalScriptsExported}个脚本到{exportResult.SuccessfulFilesCount}个分类文件中");
                            
                            // 显示详细结果
                            var result = MessageBox.Show(
                                statsMessage,
                                "分类导出成功", 
                                MessageBoxButtons.YesNo, 
                                MessageBoxIcon.Information,
                                MessageBoxDefaultButton.Button2);
                                
                            // 询问是否打开输出目录
                            if (result == DialogResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = selectedPath,
                                    UseShellExecute = true,
                                    Verb = "open"
                                });
                            }
                            
                            // 询问是否保存项目
                            var saveResult = MessageBox.Show(
                                "是否保存当前项目？",
                                "保存项目",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);
                                
                            if (saveResult == DialogResult.Yes)
                            {
                                SaveProjectAs();
                            }
                        }
                        else
                        {
                            logger.LogError($"分类导出失败: {exportResult.ErrorMessage}");
                            MessageBox.Show($"分类导出失败:\n\n{exportResult.ErrorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        logger.LogInfo("用户取消了分类导出操作");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateProgressBar("分类导出失败", 0, false);
                logger.LogError($"执行分类导出时出错: {ex.Message}");
                MessageBox.Show($"分类导出失败:\n\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 生成分类导出统计信息
        /// </summary>
        private string GenerateCategorizedExportStats(ExportResult exportResult, string outputPath)
        {
            var stats = new StringBuilder();
            stats.AppendLine("🎉 ST脚本分类导出完成!");
            stats.AppendLine();
            stats.AppendLine($"📂 输出目录: {Path.GetFileName(outputPath)}");
            stats.AppendLine($"📍 完整路径: {outputPath}");
            stats.AppendLine();
            stats.AppendLine("📊 分类统计:");
            
            foreach (var fileResult in exportResult.FileResults.OrderBy(f => f.Category.GetFileName()))
            {
                var icon = GetCategoryIcon(fileResult.Category.GetFileName());
                stats.AppendLine($"  {icon} {fileResult.Category.GetDescription()}: {fileResult.ScriptCount}个脚本");
                stats.AppendLine($"     📄 文件: {Path.GetFileName(fileResult.FilePath)} ({fileResult.FileSizeFormatted})");
            }
            
            stats.AppendLine();
            stats.AppendLine($"📈 总计: {exportResult.Statistics.TotalScriptsExported}个脚本已分类导出到{exportResult.SuccessfulFilesCount}个文件中");
            stats.AppendLine($"⏱️ 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            stats.AppendLine();
            stats.AppendLine("❓ 是否打开输出目录？");
            
            return stats.ToString();
        }
        
        /// <summary>
        /// 根据分类名称获取对应图标
        /// </summary>
        private string GetCategoryIcon(string categoryName)
        {
            return categoryName.ToUpper() switch
            {
                "AI_CONVERT" => "🔄",
                "AO_CTRL" => "📤",
                "DI_READ" => "📥",
                "DO_CTRL" => "⚡",
                _ => "📄"
            };
        }

        /// <summary>
        /// 生成文件头部信息
        /// </summary>
        private string GenerateFileHeader(string fileType)
        {
            var header = new StringBuilder();
            header.AppendLine("(*");
            header.AppendLine($" * {fileType}");
            header.AppendLine($" * 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            header.AppendLine($" * 项目: {currentProjectCache?.SourceFilePath ?? "未知项目"}");
            header.AppendLine($" * 生成器: ST自动生成工具 v2.0");
            header.AppendLine(" *)");
            header.AppendLine();
            return header.ToString();
        }

        /// <summary>
        /// 生成导出统计信息
        /// </summary>
        private string GenerateExportStatistics()
        {
            var stats = new StringBuilder();
            stats.AppendLine("ST脚本导出统计报告");
            stats.AppendLine("=" + new string('=', 50));
            stats.AppendLine();
            stats.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            stats.AppendLine($"源文件: {currentProjectCache?.SourceFilePath ?? "未知"}");
            stats.AppendLine();
            
            // 统计信息
            stats.AppendLine("📊 统计数据:");
            stats.AppendLine($"总设备数: {currentProjectCache?.Statistics.TotalDevices ?? 0}");
            stats.AppendLine($"总点位数: {currentProjectCache?.Statistics.TotalPoints ?? 0}");
            stats.AppendLine($"IO映射脚本数: {currentProjectCache?.IOMappingScripts.Count ?? 0}");
            stats.AppendLine($"设备模板类型数: {currentProjectCache?.DeviceSTPrograms.Count ?? 0}");
            stats.AppendLine();
            
            // 设备模板详情
            if (currentProjectCache?.DeviceSTPrograms.Any() == true)
            {
                stats.AppendLine("📋 设备模板详情:");
                foreach (var template in currentProjectCache.DeviceSTPrograms)
                {
                    stats.AppendLine($"• {template.Key}: {template.Value.Count} 个设备");
                }
            }
            
            return stats.ToString();
        }

        /// <summary>
        /// 生成变量表Excel文件
        /// </summary>
        /// <param name="outputDirectory">输出目录</param>
        /// <returns>是否生成成功</returns>
        private async Task<bool> GenerateVariableTable(string outputDirectory)
        {
            logger.LogInfo(">>> 进入GenerateVariableTable方法");
            logger.LogInfo($">>> 输出目录: {outputDirectory}");
            
            try
            {
                // 检查前置条件
                logger.LogInfo(">>> 检查前置条件...");
                logger.LogInfo($">>> currentProjectCache 是否为null: {currentProjectCache == null}");
                
                if (currentProjectCache == null)
                {
                    logger.LogError(">>> currentProjectCache 为 null，无法生成变量表");
                    return false;
                }
                
                logger.LogInfo($">>> DeviceSTPrograms 是否为null: {currentProjectCache.DeviceSTPrograms == null}");
                if (currentProjectCache.DeviceSTPrograms == null)
                {
                    logger.LogError(">>> DeviceSTPrograms 为 null，无法生成变量表");
                    return false;
                }
                
                logger.LogInfo($">>> DeviceSTPrograms 数量: {currentProjectCache.DeviceSTPrograms.Count}");
                if (!currentProjectCache.DeviceSTPrograms.Any())
                {
                    logger.LogInfo(">>> 没有设备ST程序数据，跳过变量表生成");
                    return false;
                }

                // 打印DeviceSTPrograms的详细信息
                logger.LogInfo(">>> DeviceSTPrograms 详细信息:");
                foreach (var deviceProgram in currentProjectCache.DeviceSTPrograms)
                {
                    logger.LogInfo($">>>   模板: {deviceProgram.Key}, ST代码数量: {deviceProgram.Value.Count}");
                }

                logger.LogInfo(">>> 开始生成变量表...");

                // 1. 解析模板元数据
                logger.LogInfo(">>> 步骤1: 解析模板元数据");
                var templatesDirectory = Path.Combine(Application.StartupPath, "Templates");
                logger.LogInfo($">>> 模板目录路径: {templatesDirectory}");
                logger.LogInfo($">>> 模板目录是否存在: {Directory.Exists(templatesDirectory)}");
                
                var templateParser = new AutomaticGeneration_ST.Services.TemplateMetadataParser();
                logger.LogInfo(">>> 创建TemplateMetadataParser完成");
                
                logger.LogInfo(">>> 调用ParseAllTemplates...");
                var templateMetadataDict = templateParser.ParseAllTemplates(templatesDirectory);
                logger.LogInfo($">>> ParseAllTemplates返回结果数量: {templateMetadataDict.Count}");

                if (!templateMetadataDict.Any())
                {
                    logger.LogWarning(">>> 未找到有效的模板元数据");
                    return false;
                }

                logger.LogInfo($">>> 解析到 {templateMetadataDict.Count} 个模板的元数据:");
                foreach (var template in templateMetadataDict)
                {
                    logger.LogInfo($">>>   模板Key: {template.Key}");
                    logger.LogInfo($">>>     程序名称: {template.Value.ProgramName}");
                    logger.LogInfo($">>>     变量类型: {template.Value.VariableType}");
                    logger.LogInfo($">>>     有TXT文件: {template.Value.HasTxtFile}");
                    logger.LogInfo($">>>     TXT文件路径: {template.Value.TxtFilePath}");
                }

                // 2. 分析ST代码，提取变量信息
                logger.LogInfo("开始分析ST代码...");
                logger.LogInfo($"当前缓存中的设备ST程序类型: {string.Join(", ", currentProjectCache.DeviceSTPrograms.Keys)}");
                logger.LogInfo($"当前缓存中的IO映射脚本数量: {currentProjectCache.IOMappingScripts.Count}");
                
                // 首先分析设备ST程序
                var stCodeAnalyzer = new AutomaticGeneration_ST.Services.STCodeAnalyzer();
                var variableEntriesByTemplate = stCodeAnalyzer.AnalyzeMultipleSTCodes(
                    currentProjectCache.DeviceSTPrograms, 
                    templateMetadataDict);
                
                // 然后分析IO映射脚本
                logger.LogInfo(">>> 步骤2.1: 分析IO映射脚本");
                var ioMappingSTCodes = ConvertIOMappingScriptsToTemplateGroups(currentProjectCache.IOMappingScripts);
                logger.LogInfo($">>> 从IO映射脚本中识别出 {ioMappingSTCodes.Count} 个模板类型");
                
                var ioVariableEntriesByTemplate = stCodeAnalyzer.AnalyzeMultipleSTCodes(
                    ioMappingSTCodes, 
                    templateMetadataDict);
                
                // 合并设备ST程序和IO映射脚本的分析结果
                logger.LogInfo(">>> 步骤2.2: 合并分析结果");
                foreach (var ioTemplate in ioVariableEntriesByTemplate)
                {
                    if (variableEntriesByTemplate.ContainsKey(ioTemplate.Key))
                    {
                        variableEntriesByTemplate[ioTemplate.Key].AddRange(ioTemplate.Value);
                        logger.LogInfo($">>> 合并模板 {ioTemplate.Key}: 添加了 {ioTemplate.Value.Count} 个IO变量");
                    }
                    else
                    {
                        variableEntriesByTemplate[ioTemplate.Key] = ioTemplate.Value;
                        logger.LogInfo($">>> 新增模板 {ioTemplate.Key}: {ioTemplate.Value.Count} 个IO变量");
                    }
                }

                if (!variableEntriesByTemplate.Any())
                {
                    logger.LogWarning("未提取到变量信息");
                    return false;
                }

                logger.LogInfo($"提取到 {variableEntriesByTemplate.Count} 个模板的变量信息");
                foreach (var template in variableEntriesByTemplate)
                {
                    logger.LogInfo($"模板 {template.Key}: {template.Value.Count} 个变量");
                }

                // 3. 生成Excel文件
                var excelFilePath = Path.Combine(outputDirectory, "Variables_Table.xls");
                var variableTableGenerator = new AutomaticGeneration_ST.Services.VariableTableGenerator();
                
                var generateResult = variableTableGenerator.GenerateVariableTable(variableEntriesByTemplate, excelFilePath);
                
                if (generateResult)
                {
                    logger.LogSuccess($"变量表生成成功: {Path.GetFileName(excelFilePath)}");
                    
                    // 生成统计信息
                    var stats = variableTableGenerator.GenerateStatistics(variableEntriesByTemplate);
                    logger.LogInfo($"变量表统计:\n{stats}");
                    
                    return true;
                }
                else
                {
                    logger.LogError("变量表生成失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"生成变量表时出错: {ex.Message}");
                return false;
            }
        }

        private void InitializeTheme()
        {
            // 订阅主题变更事件
            ThemeManager.ThemeChanged += OnThemeChanged;
            
            // 设置默认主题
            ThemeManager.SetTheme(ThemeType.Light);
            
            // 应用主题到当前窗体
            ApplyCurrentTheme();
        }

        private void OnThemeChanged(ThemeType theme)
        {
            ApplyCurrentTheme();
        }

        private void ApplyCurrentTheme()
        {
            // 应用主题到整个窗体
            ThemeManager.ApplyTheme(this);
            
            // 更新特定控件的颜色
            UpdateControlColors();
        }

        private void UpdateControlColors()
        {
            try
            {
                // 更新分割容器的背景色
                mainSplitContainer.BackColor = ThemeManager.GetBorderColor();
                rightSplitContainer.BackColor = ThemeManager.GetBorderColor();
                
                // 更新面板颜色
                leftPanel.BackColor = ThemeManager.GetSurfaceColor();
                configPanel.BackColor = ThemeManager.GetSurfaceColor();
                logFilterPanel.BackColor = ThemeManager.GetSurfaceColor();
                
                // 更新日志区域
                richTextBox1.BackColor = ThemeManager.GetBackgroundColor();
                richTextBox1.ForeColor = ThemeManager.GetTextColor();
                
                // 更新预览区域的所有标签页
                UpdatePreviewTabColors();
                
                // 强制重绘
                this.Invalidate(true);
            }
            catch (Exception ex)
            {
                logger?.LogError($"应用主题时出错: {ex.Message}");
            }
        }

        private void UpdatePreviewTabColors()
        {
            foreach (TabPage tabPage in previewTabControl.TabPages)
            {
                tabPage.BackColor = ThemeManager.GetSurfaceColor();
                tabPage.ForeColor = ThemeManager.GetTextColor();
                
                foreach (Control control in tabPage.Controls)
                {
                    if (control is RichTextBox rtb)
                    {
                        rtb.BackColor = ThemeManager.GetBackgroundColor();
                        rtb.ForeColor = ThemeManager.GetTextColor();
                    }
                }
            }
        }

        private void LightThemeMenuItem_Click(object sender, EventArgs e)
        {
            SetThemeMenuChecked(lightThemeMenuItem);
            ThemeManager.SetTheme(ThemeType.Light);
            logger.LogInfo("已切换到浅色主题");
        }

        private void DarkThemeMenuItem_Click(object sender, EventArgs e)
        {
            SetThemeMenuChecked(darkThemeMenuItem);
            ThemeManager.SetTheme(ThemeType.Dark);
            logger.LogInfo("已切换到深色主题");
        }

        private void SystemThemeMenuItem_Click(object sender, EventArgs e)
        {
            SetThemeMenuChecked(systemThemeMenuItem);
            ThemeManager.SetTheme(ThemeType.System);
            logger.LogInfo("已切换到跟随系统主题");
        }

        private void SetThemeMenuChecked(ToolStripMenuItem selectedItem)
        {
            lightThemeMenuItem.Checked = false;
            darkThemeMenuItem.Checked = false;
            systemThemeMenuItem.Checked = false;
            selectedItem.Checked = true;
        }



        private void ReloadApplicationSettings()
        {
            try
            {
                // 重新加载窗口设置
                var windowSettings = WindowSettings.Load();
                windowSettings.ApplyToForm(this);
                
                // 重新初始化快捷键（如果设置有变化）
                KeyboardShortcutManager.RefreshShortcuts(this);
                
                // 重新初始化提示系统（如果设置有变化）
                TooltipManager.RefreshTooltips(this);
                
                logger.LogInfo("应用程序设置已重新加载");
            }
            catch (Exception ex)
            {
                logger.LogError($"重新加载应用程序设置失败: {ex.Message}");
            }
        }

        private void ApplyStandardStyles()
        {
            try
            {
                // 应用标准间距到主要容器
                // ControlStyleManager.ApplyStandardSpacing(this);
                
                // 为按钮应用样式
                // ControlStyleManager.ApplyButtonStyle(button_upload, ButtonStyle.Primary);
                // ControlStyleManager.ApplyButtonStyle(button_export, ButtonStyle.Secondary);
                
                // 为面板应用样式
                // ControlStyleManager.ApplyPanelStyle(leftPanel, true);
                // ControlStyleManager.ApplyPanelStyle(configPanel, false);
                // ControlStyleManager.ApplyPanelStyle(logFilterPanel, true);
                
                // 设置控件字体
                this.Font = ControlStyleManager.DefaultFont;
                
                // 优化分割容器样式
                mainSplitContainer.SplitterWidth = 8;
                rightSplitContainer.SplitterWidth = 8;
                
                // 设置窗体最小尺寸
                this.MinimumSize = new Size(1000, 700);
                
                // 优化标签页控件样式
                ApplyTabControlStyle();
                
                // 实现响应式布局
                InitializeResponsiveLayout();
                
                logger.LogInfo("界面样式已应用");
            }
            catch (Exception ex)
            {
                logger?.LogError($"应用界面样式时出错: {ex.Message}");
            }
        }

        private void ApplyTabControlStyle()
        {
            if (previewTabControl != null)
            {
                previewTabControl.Font = ControlStyleManager.DefaultFont;
                previewTabControl.Padding = new System.Drawing.Point(ControlStyleManager.MEDIUM_PADDING, ControlStyleManager.SMALL_PADDING);
                
                // 为每个标签页设置样式
                foreach (TabPage tabPage in previewTabControl.TabPages)
                {
                    tabPage.Font = ControlStyleManager.DefaultFont;
                    tabPage.Padding = new Padding(ControlStyleManager.MEDIUM_PADDING);
                    
                    // 为标签页内的控件应用样式
                    foreach (Control control in tabPage.Controls)
                    {
                        if (control is RichTextBox rtb)
                        {
                            rtb.Font = ControlStyleManager.CodeFont;
                            rtb.BorderStyle = BorderStyle.None;
                            rtb.Margin = new Padding(0);
                            rtb.Dock = DockStyle.Fill;
                        }
                    }
                }
            }
        }

        private void InitializeResponsiveLayout()
        {
            try
            {
                // 设置主分割容器的响应式行为
                SetupResponsiveSplitContainer();
                
                // 设置按钮的响应式布局
                SetupResponsiveButtons();
                
                // 设置控件的锚点和停靠属性
                SetupControlAnchors();
                
                // 订阅窗体大小变化事件
                this.Resize += OnFormResize;
                
                logger.LogInfo("响应式布局已初始化");
            }
            catch (Exception ex)
            {
                logger?.LogError($"初始化响应式布局时出错: {ex.Message}");
            }
        }

        private void SetupResponsiveSplitContainer()
        {
            // 主分割容器响应式设置
            if (mainSplitContainer != null)
            {
                mainSplitContainer.SplitterMoved += (s, e) => {
                    // 保存分割器位置到设置
                    SaveSplitterPosition(1, mainSplitContainer.SplitterDistance);
                };
                
                // 设置最小面板大小
                mainSplitContainer.Panel1MinSize = 200;
                mainSplitContainer.Panel2MinSize = 400;
            }
            
            // 右侧分割容器响应式设置
            if (rightSplitContainer != null)
            {
                rightSplitContainer.SplitterMoved += (s, e) => {
                    // 保存分割器位置到设置
                    SaveSplitterPosition(2, rightSplitContainer.SplitterDistance);
                };
                
                // 设置最小面板大小
                rightSplitContainer.Panel1MinSize = 300;
                rightSplitContainer.Panel2MinSize = 150;
            }
        }

        private void SetupResponsiveButtons()
        {
            // 上传按钮响应式设置
            if (button_upload != null)
            {
                button_upload.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                button_upload.Location = new System.Drawing.Point(ControlStyleManager.MEDIUM_PADDING, ControlStyleManager.MEDIUM_PADDING);
            }
            
            // 导出按钮响应式设置
            if (button_export != null)
            {
                button_export.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                button_export.Location = new System.Drawing.Point(
                    button_upload.Right + ControlStyleManager.MEDIUM_PADDING, 
                    ControlStyleManager.MEDIUM_PADDING
                );
            }
            
            // 分类导出按钮响应式设置
            if (button_categorized_export != null)
            {
                button_categorized_export.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                button_categorized_export.Location = new System.Drawing.Point(
                    button_export.Right + ControlStyleManager.MEDIUM_PADDING, 
                    ControlStyleManager.MEDIUM_PADDING
                );
            }
        }

        private void SetupControlAnchors()
        {
            // 左侧面板控件锚点设置
            if (leftPanel != null)
            {
                leftPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
                
                // 文件列表框锚点
                if (fileListBox != null)
                {
                    fileListBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                }
            }
            
            // 预览区域控件锚点设置
            if (previewTabControl != null)
            {
                previewTabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }
            
            // 日志区域控件锚点设置
            if (richTextBox1 != null)
            {
                richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }
            
            // 日志过滤面板锚点设置
            if (logFilterPanel != null)
            {
                logFilterPanel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                
                // 搜索框响应式
                if (logSearchBox != null)
                {
                    logSearchBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                }
                
                // 过滤下拉框响应式
                if (logFilterComboBox != null)
                {
                    logFilterComboBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                }
                
                // 清空按钮响应式
                if (clearLogButton != null)
                {
                    clearLogButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                }
            }
        }

        private void OnFormResize(object? sender, EventArgs e)
        {
            try
            {
                // 响应式调整分割器位置
                AdjustSplitterPositions();
                
                // 响应式调整按钮布局
                AdjustButtonLayout();
                
                // 响应式调整字体大小（可选）
                AdjustFontSizes();
            }
            catch (Exception ex)
            {
                logger?.LogError($"窗体大小调整时出错: {ex.Message}");
            }
        }

        private void AdjustSplitterPositions()
        {
            try
            {
                if (mainSplitContainer != null)
                {
                    // 根据窗体宽度调整左侧面板比例
                    var targetWidth = Math.Max(200, Math.Min(350, this.Width / 4));
                    var minDistance = mainSplitContainer.Panel1MinSize;
                    var maxDistance = mainSplitContainer.Width - mainSplitContainer.Panel2MinSize;
                    
                    if (maxDistance > minDistance)
                    {
                        targetWidth = Math.Max(minDistance, Math.Min(maxDistance, targetWidth));
                        if (Math.Abs(mainSplitContainer.SplitterDistance - targetWidth) > 50)
                        {
                            mainSplitContainer.SplitterDistance = (int)targetWidth;
                        }
                    }
                }
                
                if (rightSplitContainer != null)
                {
                    // 根据窗体高度调整预览区域比例
                    var targetHeight = Math.Max(300, this.Height * 2 / 3);
                    var minDistance = rightSplitContainer.Panel1MinSize;
                    var maxDistance = rightSplitContainer.Height - rightSplitContainer.Panel2MinSize;
                    
                    if (maxDistance > minDistance)
                    {
                        targetHeight = Math.Max(minDistance, Math.Min(maxDistance, targetHeight));
                        if (Math.Abs(rightSplitContainer.SplitterDistance - targetHeight) > 50)
                        {
                            rightSplitContainer.SplitterDistance = (int)targetHeight;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"调整分割器位置时出错: {ex.Message}");
            }
        }

        private void AdjustButtonLayout()
        {
            // 在小尺寸时调整按钮布局
            if (this.Width < 1400) // 调整阈值以适应第三个按钮
            {
                // 紧凑布局
                if (button_upload != null && button_export != null && button_categorized_export != null)
                {
                    var smallSize = new Size(160, 35); // 较小的按钮尺寸
                    button_upload.Size = smallSize;
                    button_export.Size = smallSize;
                    button_categorized_export.Size = smallSize;
                    
                    button_export.Location = new System.Drawing.Point(
                        button_upload.Right + ControlStyleManager.SMALL_PADDING,
                        button_upload.Top
                    );
                    
                    button_categorized_export.Location = new System.Drawing.Point(
                        button_export.Right + ControlStyleManager.SMALL_PADDING,
                        button_upload.Top
                    );
                }
            }
            else
            {
                // 标准布局
                if (button_upload != null && button_export != null && button_categorized_export != null)
                {
                    button_upload.Size = ControlStyleManager.StandardButtonSize;
                    button_export.Size = ControlStyleManager.StandardButtonSize;
                    button_categorized_export.Size = new Size(200, 45); // 略大一些以适应文字
                    
                    button_export.Location = new System.Drawing.Point(
                        button_upload.Right + ControlStyleManager.MEDIUM_PADDING,
                        button_upload.Top
                    );
                    
                    button_categorized_export.Location = new System.Drawing.Point(
                        button_export.Right + ControlStyleManager.MEDIUM_PADDING,
                        button_upload.Top
                    );
                }
            }
        }

        private void AdjustFontSizes()
        {
            // 根据窗体大小调整字体
            if (this.Width < 1000 || this.Height < 700)
            {
                // 小窗体使用较小字体
                this.Font = ControlStyleManager.SmallFont;
            }
            else
            {
                // 标准窗体使用默认字体
                this.Font = ControlStyleManager.DefaultFont;
            }
        }

        private void SaveSplitterPosition(int splitterIndex, int distance)
        {
            try
            {
                // 这里可以保存分割器位置到WindowSettings
                // 为了避免频繁保存，可以使用定时器延迟保存
                if (statusTimer != null)
                {
                    statusTimer.Stop();
                    statusTimer.Interval = 1000; // 1秒后保存
                    statusTimer.Tick += (s, e) => {
                        var settings = WindowSettings.Load();
                        if (splitterIndex == 1)
                            settings.SplitterDistance1 = distance;
                        else
                            settings.SplitterDistance2 = distance;
                        settings.Save();
                        statusTimer.Stop();
                    };
                    statusTimer.Start();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"保存分割器位置时出错: {ex.Message}");
            }
        }

        private void InitializeKeyboardShortcuts()
        {
            try
            {
                // 注册快捷键到当前窗体
                KeyboardShortcutManager.RegisterShortcuts(this);
                
                // 订阅快捷键事件
                KeyboardShortcutManager.ShortcutPressed += OnShortcutPressed;
                
                logger.LogInfo("快捷键系统已初始化");
            }
            catch (Exception ex)
            {
                logger?.LogError($"初始化快捷键系统时出错: {ex.Message}");
            }
        }

        private void OnShortcutPressed(string shortcutName)
        {
            try
            {
                switch (shortcutName)
                {
                    case "OpenFile":
                        button_upload_Click(this, EventArgs.Empty);
                        logger.LogInfo("快捷键: 打开文件");
                        break;
                        
                    case "ExportResults":
                        button_export_Click(this, EventArgs.Empty);
                        logger.LogInfo("快捷键: 导出结果");
                        break;
                        
                    case "Exit":
                        this.Close();
                        break;
                        
                    case "Copy":
                        HandleCopyShortcut();
                        break;
                        
                    case "SelectAll":
                        HandleSelectAllShortcut();
                        break;
                        
                    case "Find":
                        HandleFindShortcut();
                        break;
                        
                    case "Refresh":
                        HandleRefreshShortcut();
                        break;
                        
                    case "ClearLog":
                        HandleClearLogShortcut();
                        break;
                        
                    case "ToggleTheme":
                        HandleToggleThemeShortcut();
                        break;
                        
                    case "Settings":
                        HandleSettingsShortcut();
                        break;
                        
                    case "Help":
                        HandleHelpShortcut();
                        break;
                        
                    case "About":
                        HandleAboutShortcut();
                        break;
                        
                    case "FocusFileList":
                        HandleFocusFileListShortcut();
                        break;
                        
                    case "FocusPreview":
                        HandleFocusPreviewShortcut();
                        break;
                        
                    case "FocusLog":
                        HandleFocusLogShortcut();
                        break;
                        
                    case "ShowDebugInfo":
                        HandleShowDebugInfoShortcut();
                        break;
                        
                    case "ReloadConfig":
                        HandleReloadConfigShortcut();
                        break;
                        
                    case "RunUITests":
                        HandleRunUITestsShortcut();
                        break;
                        
                    default:
                        logger.LogInfo($"未处理的快捷键: {shortcutName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"处理快捷键 {shortcutName} 时出错: {ex.Message}");
            }
        }

        private void HandleCopyShortcut()
        {
            // 根据当前焦点复制内容
            var focusedControl = this.ActiveControl;
            
            if (focusedControl == richTextBox1)
            {
                if (!string.IsNullOrEmpty(richTextBox1.SelectedText))
                {
                    Clipboard.SetText(richTextBox1.SelectedText);
                    logger.LogInfo("已复制日志内容到剪贴板");
                }
                else
                {
                    Clipboard.SetText(richTextBox1.Text);
                    logger.LogInfo("已复制全部日志到剪贴板");
                }
            }
            else if (focusedControl is RichTextBox previewRtb)
            {
                if (!string.IsNullOrEmpty(previewRtb.SelectedText))
                {
                    Clipboard.SetText(previewRtb.SelectedText);
                    logger.LogInfo("已复制预览内容到剪贴板");
                }
                else
                {
                    Clipboard.SetText(previewRtb.Text);
                    logger.LogInfo("已复制全部预览内容到剪贴板");
                }
            }
        }

        private void HandleSelectAllShortcut()
        {
            var focusedControl = this.ActiveControl;
            
            if (focusedControl == richTextBox1)
            {
                richTextBox1.SelectAll();
                logger.LogInfo("已全选日志内容");
            }
            else if (focusedControl is RichTextBox previewRtb)
            {
                previewRtb.SelectAll();
                logger.LogInfo("已全选预览内容");
            }
            else if (focusedControl is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void HandleFindShortcut()
        {
            // 聚焦到日志搜索框
            if (logSearchBox != null)
            {
                logSearchBox.Focus();
                logger.LogInfo("已聚焦到搜索框");
            }
        }

        private async void HandleRefreshShortcut()
        {
            // 重新生成代码
            if (!string.IsNullOrEmpty(uploadedFilePath) && File.Exists(uploadedFilePath))
            {
                logger.LogInfo("快捷键: 重新处理文件并生成代码");
                ProcessExcelFile(uploadedFilePath);
            }
            else if (pointData.Any())
            {
                logger.LogInfo("快捷键: 基于现有数据重新生成代码");
                try
                {
                    await GenerateSTScriptsAsync();
                    logger.LogSuccess("代码重新生成完成");
                }
                catch (Exception ex)
                {
                    logger.LogError($"重新生成代码失败: {ex.Message}");
                }
            }
            else
            {
                logger.LogWarning("没有可刷新的数据，请先上传点表文件");
            }
        }

        private void HandleClearLogShortcut()
        {
            richTextBox1.Clear();
            logger.LogInfo("快捷键: 日志已清空");
        }

        private void HandleToggleThemeShortcut()
        {
            // 切换主题
            var currentTheme = ThemeManager.CurrentTheme;
            var newTheme = currentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light;
            
            ThemeManager.SetTheme(newTheme);
            
            // 更新菜单选中状态
            if (newTheme == ThemeType.Light)
            {
                SetThemeMenuChecked(lightThemeMenuItem);
            }
            else
            {
                SetThemeMenuChecked(darkThemeMenuItem);
            }
            
            logger.LogInfo($"快捷键: 已切换到{(newTheme == ThemeType.Light ? "浅色" : "深色")}主题");
        }

        private void HandleSettingsShortcut()
        {
            // 打开设置对话框
            try
            {
                // 设置功能已移除
                MessageBox.Show("设置功能暂时不可用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                logger.LogInfo("设置功能已移除");
            }
            catch (Exception ex)
            {
                logger.LogError($"打开设置对话框失败: {ex.Message}");
            }
        }

        private void HandleHelpShortcut()
        {
            // 显示帮助信息
            var helpText = @"ST脚本自动生成器 - 快捷键帮助

文件操作:
  Ctrl+O        打开点表文件
  Ctrl+S        导出ST脚本
  Alt+F4        退出程序

编辑操作:
  Ctrl+C        复制选中内容
  Ctrl+A        全选内容
  Ctrl+F        聚焦搜索框

视图操作:
  F5            刷新/重新生成
  Ctrl+L        清空日志
  Ctrl+T        切换主题
  F11           全屏显示

工具操作:
  Ctrl+,        打开设置
  F1            显示帮助
  Ctrl+Shift+A  关于软件

导航操作:
  Ctrl+1        聚焦文件列表
  Ctrl+2        聚焦预览区域
  Ctrl+3        聚焦日志区域

更多功能请查看菜单栏。";

            MessageBox.Show(helpText, "快捷键帮助", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void HandleAboutShortcut()
        {
            // 显示关于对话框
            try
            {
                using var aboutForm = new Forms.AboutForm();
                aboutForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                logger.LogError($"显示关于对话框失败: {ex.Message}");
                
                // 备用简单消息框
                var aboutText = @"ST脚本自动生成器 v2.0

一个专业的工业自动化代码生成工具
支持AI/AO/DI/DO点位的ST脚本自动生成

开发者: Claude
技术栈: .NET 9.0, WinForms, NPOI, Scriban
发布时间: 2025年1月

© 2025 版权所有";

                MessageBox.Show(aboutText, "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void HandleFocusFileListShortcut()
        {
            if (fileListBox != null)
            {
                fileListBox.Focus();
                logger.LogInfo("已聚焦到文件列表");
            }
        }

        private void HandleFocusPreviewShortcut()
        {
            if (previewTabControl != null)
            {
                previewTabControl.Focus();
                logger.LogInfo("已聚焦到预览区域");
            }
        }

        private void HandleFocusLogShortcut()
        {
            if (richTextBox1 != null)
            {
                richTextBox1.Focus();
                logger.LogInfo("已聚焦到日志区域");
            }
        }

        private void HandleShowDebugInfoShortcut()
        {
            // 显示调试信息
            var debugInfo = $@"调试信息

内存使用: {GC.GetTotalMemory(false) / 1024 / 1024:F2} MB
点位数量: {pointData.Count}
生成脚本: {generatedScripts.Count}
最近文件: {recentFiles.Count}
当前主题: {ThemeManager.CurrentTheme}
窗体大小: {this.Size}
分割器位置: {mainSplitContainer?.SplitterDistance}, {rightSplitContainer?.SplitterDistance}
当前时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            MessageBox.Show(debugInfo, "调试信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void HandleReloadConfigShortcut()
        {
            try
            {
                // 重新加载窗口设置
                var settings = WindowSettings.Load();
                settings.ApplyToForm(this);
                
                // 重新应用主题
                ApplyCurrentTheme();
                
                logger.LogInfo("配置已重新加载");
            }
            catch (Exception ex)
            {
                logger.LogError($"重新加载配置失败: {ex.Message}");
            }
        }

        private void HandleRunUITestsShortcut()
        {
            try
            {
                logger.LogInfo("开始执行UI稳定性测试...");
                
                // 显示处理状态
                statusLabel.Text = "执行UI测试中...";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                
                Application.DoEvents();
                
                // 执行测试
                var testSuite = UITestManager.RunUIStabilityTests(this);
                
                // 生成报告
                var report = UITestManager.GenerateTestReport(testSuite);
                
                // 隐藏进度条
                progressBar.Visible = false;
                statusLabel.Text = "就绪";
                
                // 显示测试结果
                ShowTestResults(testSuite, report);
                
                logger.LogInfo($"UI测试完成 - 通过: {testSuite.PassedTests}/{testSuite.TotalTests}");
            }
            catch (Exception ex)
            {
                progressBar.Visible = false;
                statusLabel.Text = "就绪";
                logger.LogError($"执行UI测试失败: {ex.Message}");
            }
        }

        private void ShowTestResults(UITestManager.UITestSuite testSuite, string report)
        {
            var form = new Form
            {
                Text = "UI稳定性测试结果",
                Size = new Size(600, 500),
                StartPosition = FormStartPosition.CenterParent,
                ShowInTaskbar = false
            };

            var textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                Text = report,
                BackColor = ThemeManager.GetBackgroundColor(),
                ForeColor = ThemeManager.GetTextColor()
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            var okButton = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new System.Drawing.Point(500, 10),
                DialogResult = DialogResult.OK
            };

            var saveButton = new Button
            {
                Text = "保存报告",
                Size = new Size(80, 30),
                Location = new System.Drawing.Point(410, 10)
            };

            saveButton.Click += (s, e) => SaveTestReport(report);

            buttonPanel.Controls.AddRange(new Control[] { okButton, saveButton });
            form.Controls.AddRange(new Control[] { textBox, buttonPanel });

            form.AcceptButton = okButton;
            form.ShowDialog(this);
        }

        private void SaveTestReport(string report)
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "文本文件|*.txt|所有文件|*.*",
                    DefaultExt = "txt",
                    FileName = $"UI测试报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, report, Encoding.UTF8);
                    logger.LogInfo($"测试报告已保存到: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"保存测试报告失败: {ex.Message}");
            }
        }

        private void InitializeTooltips()
        {
            try
            {
                // 初始化提示系统
                TooltipManager.Initialize(this);
                
                // 添加快捷键相关的提示
                TooltipManager.AddShortcutTooltips(this);
                
                // 订阅主题变更事件，更新提示框颜色
                ThemeManager.ThemeChanged += (theme) => TooltipManager.ApplyThemeToTooltip();
                
                logger.LogInfo("提示系统已初始化");
            }
            catch (Exception ex)
            {
                logger?.LogError($"初始化提示系统时出错: {ex.Message}");
            }
        }

        private void UpdateTooltipContext()
        {
            try
            {
                // 根据当前状态更新提示内容
                if (pointData.Any())
                {
                    TooltipManager.UpdateContextualTooltips("hasData");
                }
                else
                {
                    TooltipManager.UpdateContextualTooltips("empty");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"更新提示上下文时出错: {ex.Message}");
            }
        }

        #region ProjectCache管理

        /// <summary>
        /// 安全地获取项目缓存，实现"上传一次，处理一次，后续只读取缓存"机制
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>项目缓存，如果处理失败则返回null</returns>
        private async Task<ProjectCache?> GetProjectCacheAsync(string filePath)
        {
            try
            {
                // 检查文件是否存在
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    logger?.LogWarning("文件路径无效或文件不存在");
                    return null;
                }

                var fileInfo = new FileInfo(filePath);
                
                // 使用锁检查缓存状态
                bool needsReprocess;
                lock (projectCacheLock)
                {
                    needsReprocess = currentProjectCache == null || 
                                   currentProjectCache.SourceFilePath != filePath || 
                                   currentProjectCache.IsSourceFileUpdated();
                }

                if (needsReprocess)
                {
                    logger?.LogInfo($"🚀 启动导入管道处理: {Path.GetFileName(filePath)}");
                    
                    // 异步处理数据（不在锁内）
                    var newCache = await importPipeline.ImportAsync(filePath);
                    
                    // 更新缓存（在锁内）
                    lock (projectCacheLock)
                    {
                        currentProjectCache = newCache;
                        deviceListNeedsRefresh = true;
                    }
                    
                    logger?.LogSuccess($"✅ 项目缓存创建完成 - 设备数: {newCache?.Statistics.TotalDevices}, 点位数: {newCache?.Statistics.TotalPoints}");
                    return newCache;
                }
                else
                {
                    logger?.LogInfo($"📋 使用现有项目缓存: {Path.GetFileName(filePath)}");
                    lock (projectCacheLock)
                    {
                        return currentProjectCache;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"❌ 获取项目缓存时出错: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 同步版本的获取项目缓存方法（兼容现有调用）
        /// </summary>
        private ProjectCache? GetProjectCache(string filePath)
        {
            try
            {
                return GetProjectCacheAsync(filePath).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger?.LogError($"❌ 同步获取项目缓存失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取数据上下文（从ProjectCache中提取，保持向后兼容）
        /// </summary>
        private AutomaticGeneration_ST.Services.Interfaces.DataContext? GetCachedDataContext(string filePath)
        {
            var projectCache = GetProjectCache(filePath);
            return projectCache?.DataContext;
        }

        /// <summary>
        /// 清除项目缓存（当用户选择新文件或重置应用时调用）
        /// </summary>
        private void ClearProjectCache()
        {
            lock (projectCacheLock)
            {
                currentProjectCache = null;
                logger?.LogInfo("🗑️ 已清除项目缓存");
            }
        }

        /// <summary>
        /// 检查当前是否有有效的项目缓存
        /// </summary>
        /// <returns>如果有有效项目缓存返回true</returns>
        private bool HasValidProjectCache()
        {
            return currentProjectCache != null && currentProjectCache.IsValid();
        }
        
        /// <summary>
        /// 将IO映射脚本转换为按模板分组的格式，以便进行变量分析
        /// </summary>
        /// <param name="ioMappingScripts">IO映射脚本列表</param>
        /// <returns>按模板名称分组的ST代码字典</returns>
        private Dictionary<string, List<string>> ConvertIOMappingScriptsToTemplateGroups(List<string> ioMappingScripts)
        {
            var templateGroups = new Dictionary<string, List<string>>();
            
            logger.LogInfo($"[ConvertIOMappingScriptsToTemplateGroups] 开始分析 {ioMappingScripts.Count} 个IO映射脚本");
            
            foreach (var script in ioMappingScripts)
            {
                // 根据脚本内容判断模板类型
                string templateType = DetermineTemplateTypeFromScript(script);
                
                if (!string.IsNullOrEmpty(templateType))
                {
                    if (!templateGroups.ContainsKey(templateType))
                    {
                        templateGroups[templateType] = new List<string>();
                    }
                    templateGroups[templateType].Add(script);
                    logger.LogInfo($"[ConvertIOMappingScriptsToTemplateGroups] 识别脚本为 {templateType} 类型");
                }
                else
                {
                    logger.LogInfo($"[ConvertIOMappingScriptsToTemplateGroups] 无法识别脚本类型，跳过");
                }
            }
            
            logger.LogInfo($"[ConvertIOMappingScriptsToTemplateGroups] 完成分析，识别出模板类型: {string.Join(", ", templateGroups.Keys)}");
            return templateGroups;
        }
        
        /// <summary>
        /// 根据脚本内容判断模板类型
        /// </summary>
        /// <param name="script">ST脚本内容</param>
        /// <returns>模板类型名称，如AI_CONVERT、AO_CONVERT等</returns>
        private string DetermineTemplateTypeFromScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                return "";
                
            // 根据脚本中的特征函数调用来判断模板类型
            if (script.Contains("AI_ALARM_") || script.Contains("(* AI点位:"))
            {
                return "AI_CONVERT";
            }
            else if (script.Contains("ENGIN_HEX_") || script.Contains("(* AO点位:"))
            {
                return "AO_CONVERT";
            }
            else if (script.Contains("(* DI点位:") || script.Contains("DI_"))
            {
                return "DI_CONVERT";
            }
            else if (script.Contains("(* DO点位:") || script.Contains("DO_"))
            {
                return "DO_CONVERT";
            }
            
            return "";
        }

        #endregion

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 检查是否有未保存的项目更改
                if (SimpleProjectManager.NeedsSave())
                {
                    var result = MessageBox.Show(
                        "当前项目有未保存的更改，是否保存？",
                        "确认退出", 
                        MessageBoxButtons.YesNoCancel, 
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        var saved = await SaveProject();
                        if (!saved)
                        {
                            e.Cancel = true; // 取消关闭
                            return;
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        e.Cancel = true; // 取消关闭
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"关闭窗体时检查项目保存状态出错: {ex.Message}");
            }
            
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                // 注销快捷键
                KeyboardShortcutManager.UnregisterShortcuts(this);
                
                // 清理提示系统
                TooltipManager.Dispose();
                
                // 清理配置系统
                Config.ConfigurationApplier.Dispose();
                Config.ConfigurationManager.Dispose();
                
                // 关闭项目
                SimpleProjectManager.CloseProject();
                
                // 保存窗口设置
                var settings = WindowSettings.Load();
                settings.UpdateFromForm(this);
                if (mainSplitContainer != null && rightSplitContainer != null)
                {
                    settings.UpdateSplitterDistances(
                        mainSplitContainer.SplitterDistance,
                        rightSplitContainer.SplitterDistance
                    );
                }
                settings.Save();
            }
            catch (Exception ex)
            {
                logger?.LogError($"关闭窗体时出错: {ex.Message}");
            }
            
            base.OnFormClosed(e);
        }

        // 菜单事件处理方法保留在原有位置

        /// <summary>
        /// 使用新的标准化架构进行ST代码生成的测试方法
        /// </summary>
        private async void TestNewArchitecture()
        {
            try
            {
                logger.LogInfo("开始使用新的标准化架构进行ST代码生成...");

                // 检查是否有上传的文件
                if (string.IsNullOrEmpty(uploadedFilePath) || !File.Exists(uploadedFilePath))
                {
                    logger.LogWarning("请先上传Excel点表文件");
                    MessageBox.Show("请先上传Excel点表文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 定义路径
                var templateDir = Path.Combine(Application.StartupPath, "Templates");
                var configFile = Path.Combine(Application.StartupPath, "template-mapping.json");
                
                // 选择导出目录
                using var folderDialog = new FolderBrowserDialog
                {
                    Description = "选择ST代码导出目录",
                    ShowNewFolderButton = true,
                    SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (folderDialog.ShowDialog() != DialogResult.OK)
                {
                    logger.LogInfo("用户取消了导出操作");
                    return;
                }

                var exportPath = Path.Combine(folderDialog.SelectedPath, $"ST_Generated_{DateTime.Now:yyyyMMdd_HHmmss}");

                // 使用新的服务架构
                var stGenerationService = new AutomaticGeneration_ST.Services.STGenerationService();

                // 显示进度
                UpdateProgressBar("正在使用新架构生成ST代码...", 0, true);

                // 使用缓存机制获取数据上下文，避免重复解析Excel
                var dataContext = GetCachedDataContext(uploadedFilePath);
                if (dataContext == null)
                {
                    throw new Exception("无法加载Excel数据");
                }

                // 从已有数据上下文获取统计信息，避免重复加载
                var statistics = stGenerationService.GetStatistics(dataContext);
                logger.LogInfo($"数据统计: 总点位 {statistics.TotalPoints}个, 设备 {statistics.DeviceCount}个, 独立点位 {statistics.StandalonePointsCount}个");
                
                // 生成ST代码（使用新的重载方法）
                var generatedFileCount = stGenerationService.GenerateSTCode(
                    dataContext, 
                    templateDir, 
                    configFile, 
                    exportPath);

                UpdateProgressBar("ST代码生成完成", 100, false);

                logger.LogSuccess($"新架构ST代码生成成功! 共生成 {generatedFileCount} 个文件");
                logger.LogInfo($"输出目录: {exportPath}");

                // 显示统计信息
                var statsMessage = $"ST代码生成完成!\n\n" +
                                 $"生成文件数: {generatedFileCount}\n" +
                                 $"总点位数: {statistics.TotalPoints}\n" +
                                 $"设备数量: {statistics.DeviceCount}\n" +
                                 $"独立点位: {statistics.StandalonePointsCount}\n" +
                                 $"输出目录: {exportPath}\n\n" +
                                 $"点位类型分布:\n";

                foreach (var kvp in statistics.PointTypeBreakdown)
                {
                    statsMessage += $"  {kvp.Key}: {kvp.Value}个\n";
                }

                MessageBox.Show(statsMessage, "生成完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 询问是否打开输出目录
                var openResult = MessageBox.Show("是否打开输出目录?", "操作完成", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (openResult == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", exportPath);
                }

                // 更新项目数据
                UpdateProjectData();
            }
            catch (Exception ex)
            {
                UpdateProgressBar("生成失败", 0, false);
                logger.LogError($"使用新架构生成ST代码时出错: {ex.Message}");
                MessageBox.Show($"生成失败:\n\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 重写导出按钮事件，增加新架构选项
        /// </summary>
        private async void button_export_Click_Enhanced(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "选择使用的生成架构:\n\n" +
                "是(Y) - 使用新的标准化架构\n" +
                "否(N) - 使用原有架构\n" +
                "取消 - 取消操作",
                "选择架构",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            switch (result)
            {
                case DialogResult.Yes:
                    TestNewArchitecture();
                    break;
                case DialogResult.No:
                    button_export_Click(sender, e);
                    break;
                case DialogResult.Cancel:
                    logger.LogInfo("用户取消了导出操作");
                    break;
            }
        }



        #region 新增UI功能方法

        /// <summary>
        /// 生成IO映射ST程序预览内容
        /// </summary>
        /// <summary>
        /// 新架构：从ProjectCache生成IO映射预览（只读模式）
        /// </summary>
        private string GenerateIOMappingPreview()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("📋 IO映射ST程序");
                sb.AppendLine("=" + new string('=', 40));
                sb.AppendLine();

                // 从ProjectCache获取IO映射数据（只读模式）
                if (currentProjectCache?.IOMappingScripts?.Any() == true)
                {
                    sb.AppendLine($"🎯 共生成 {currentProjectCache.IOMappingScripts.Count} 个IO映射文件");
                    sb.AppendLine();

                    foreach (var script in currentProjectCache.IOMappingScripts) // 显示所有IO映射文件
                    {
                        var lines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines) // 显示完整内容
                        {
                            sb.AppendLine(line);
                        }
                        sb.AppendLine();
                        sb.AppendLine(new string('-', 50));
                        sb.AppendLine();
                    }
                }
                else
                {
                    // 回退到兼容模式：从generatedScripts获取
                    if (generatedScripts != null && generatedScripts.Any())
                    {
                        // 过滤出IO映射相关的脚本（根据实际生成的内容）
                        var ioMappingScripts = generatedScripts.Where(script => 
                            script.Contains("(* AI点位:") || 
                            script.Contains("(* AO点位:") || 
                            script.Contains("(* DI点位:") || 
                            script.Contains("(* DO点位:") ||
                            script.Contains("AI_ALARM_") ||
                            script.Contains("AO_CTRL_") ||
                            script.Contains("DI_") ||
                            script.Contains("DO_")
                        ).ToList();

                        if (ioMappingScripts.Any())
                        {
                            sb.AppendLine($"🎯 共生成 {ioMappingScripts.Count} 个IO映射文件（兼容模式）");
                            sb.AppendLine();

                            foreach (var script in ioMappingScripts) // 显示所有IO映射文件
                            {
                                var lines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines) // 显示完整内容
                                {
                                    sb.AppendLine(line);
                                }
                                sb.AppendLine();
                                sb.AppendLine(new string('-', 50));
                                sb.AppendLine();
                            }
                        }
                        else
                        {
                            sb.AppendLine("⚠️ 未找到IO映射相关的ST程序");
                            sb.AppendLine("请检查模板配置和生成逻辑");
                        }
                    }
                    else
                    {
                        sb.AppendLine("暂无生成的IO映射ST程序，请先上传并处理点表文件。");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                logger?.LogError($"生成IO映射预览失败: {ex.Message}");
                return $"❌ 生成IO映射预览时出错: {ex.Message}";
            }
        }

        /// <summary>
        /// 获取当前选中的设备
        /// </summary>
        private string GetSelectedDevice()
        {
            try
            {
                var deviceComboBox = previewTabControl.TabPages[1].Controls.Find("deviceComboBox", true).FirstOrDefault() as ComboBox;
                if (deviceComboBox != null && deviceComboBox.SelectedItem != null)
                {
                    var selectedText = deviceComboBox.SelectedItem.ToString();
                    return selectedText == "全部设备" ? null : selectedText;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 刷新设备列表
        /// </summary>
        private void RefreshDeviceList()
        {
            try
            {
                var deviceComboBox = previewTabControl.TabPages[1].Controls.Find("deviceComboBox", true).FirstOrDefault() as ComboBox;
                if (deviceComboBox == null) return;

                var currentSelection = deviceComboBox.SelectedItem?.ToString();
                deviceComboBox.Items.Clear();
                deviceComboBox.Items.Add("全部设备");

                // 从缓存的数据上下文中获取设备列表
                if (!string.IsNullOrEmpty(uploadedFilePath))
                {
                    var dataContext = GetCachedDataContext(uploadedFilePath);
                    if (dataContext?.Devices != null)
                    {
                        foreach (var device in dataContext.Devices.OrderBy(d => d.DeviceTag))
                        {
                            deviceComboBox.Items.Add($"{device.DeviceTag} ({device.TemplateName})");
                        }
                    }
                }

                // 恢复之前的选择或默认选择第一项
                if (!string.IsNullOrEmpty(currentSelection) && deviceComboBox.Items.Contains(currentSelection))
                {
                    deviceComboBox.SelectedItem = currentSelection;
                }
                else
                {
                    deviceComboBox.SelectedIndex = 0;
                }
                
                deviceListNeedsRefresh = false; // 标记已刷新
            }
            catch (Exception ex)
            {
                logger?.LogError($"刷新设备列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 仅在需要时刷新设备列表（性能优化）
        /// </summary>
        private void RefreshDeviceListIfNeeded()
        {
            if (deviceListNeedsRefresh)
            {
                RefreshDeviceList();
            }
        }

        /// <summary>
        /// 设备选择框变化事件
        /// </summary>
        private void DeviceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var deviceSTTextBox = previewTabControl.TabPages[1].Controls["deviceSTTextBox"] as RichTextBox;
                if (deviceSTTextBox != null)
                {
                    var selectedDevice = GetSelectedDevice();
                    var deviceSTContent = GenerateDeviceSTPreview(selectedDevice);
                    deviceSTTextBox.Text = deviceSTContent;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"更新设备ST程序预览失败: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 生成设备ST程序预览内容（支持单个设备选择）
        /// </summary>
        // 获取（并缓存）设备 ST 程序集合，避免重复生成

        /// <summary>
        /// 新架构：从ProjectCache生成设备ST程序预览（只读模式）
        /// </summary>
        private string GenerateDeviceSTPreview(string? selectedDeviceTag = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("🏭 设备ST程序");
                sb.AppendLine("=" + new string('=', 40));
                sb.AppendLine();

                // 从ProjectCache获取设备ST程序数据（只读模式）
                if (currentProjectCache?.DeviceSTPrograms?.Any() == true)
                {
                    var totalDevices = currentProjectCache.Statistics.TotalDevices;
                    sb.AppendLine($"📋 发现 {totalDevices} 个设备");
                    
                    if (!string.IsNullOrEmpty(selectedDeviceTag))
                    {
                        sb.AppendLine($"🎯 当前显示: {selectedDeviceTag}");
                    }
                    else
                    {
                        sb.AppendLine("🎯 当前显示: 全部设备");
                    }
                    sb.AppendLine();

                    // 如果选择了特定设备，显示该设备的所有ST程序
                    if (!string.IsNullOrEmpty(selectedDeviceTag))
                    {
                        var targetDevice = currentProjectCache.DataContext.Devices?.FirstOrDefault(d => 
                            selectedDeviceTag.StartsWith(d.DeviceTag));
                        
                        if (targetDevice != null)
                        {
                            bool foundDevice = false;
                            
                            // 遍历所有模板，查找包含目标设备的ST程序
                            foreach (var templateGroup in currentProjectCache.DeviceSTPrograms)
                            {
                                var deviceCodes = templateGroup.Value.Where(code => 
                                    code.Contains(targetDevice.DeviceTag)).ToList();
                                
                                if (deviceCodes.Any())
                                {
                                    sb.AppendLine($"🎨 模板: {templateGroup.Key}");
                                    sb.AppendLine(new string('-', 30));
                                    
                                    foreach (var deviceCode in deviceCodes)
                                    {
                                        sb.AppendLine(deviceCode);
                                        sb.AppendLine();
                                    }
                                    foundDevice = true;
                                }
                            }
                            
                            if (!foundDevice)
                            {
                                sb.AppendLine("❌ 未找到该设备的ST程序");
                            }
                        }
                        else
                        {
                            sb.AppendLine("❌ 未找到指定的设备");
                        }
                    }
                    else
                    {
                        // 显示所有设备的ST程序预览
                        foreach (var templateGroup in currentProjectCache.DeviceSTPrograms)
                        {
                            sb.AppendLine($"🎨 模板: {templateGroup.Key} ({templateGroup.Value.Count} 个设备)");
                            sb.AppendLine(new string('-', 30));
                            
                            foreach (var code in templateGroup.Value) // 显示所有设备
                            {
                                var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines) // 显示完整代码
                                {
                                    sb.AppendLine(line);
                                }
                                sb.AppendLine();
                            }
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    // 回退到兼容模式或显示提示信息
                    if (!string.IsNullOrEmpty(uploadedFilePath))
                    {
                        sb.AppendLine("⚠️ ProjectCache数据不可用，尝试使用兼容模式...");
                        // 这里可以添加兼容模式的逻辑
                    }
                    else
                    {
                        sb.AppendLine("请先上传Excel文件以查看设备ST程序。");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                logger?.LogError($"GenerateDeviceSTPreview失败: {ex.Message}");
                return $"❌ 生成设备ST程序预览时出错: {ex.Message}";
            }
        }

        /// <summary>
        /// 过滤元数据行（程序名称、变量类型等标识行）
        /// 复用TemplateRenderer中已验证的过滤逻辑
        /// </summary>
        /// <param name="content">原始内容</param>
        /// <returns>过滤后的内容</returns>
        private string FilterMetadataLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var lines = SplitLines(content);
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 过滤掉程序名称和变量类型标识行
                if (IsMetadataLine(trimmedLine))
                {
                    logger.LogDebug($"导出时过滤掉元数据行: {trimmedLine}");
                    continue; // 跳过这些行
                }
                
                filteredLines.Add(line);
            }

            return NormalizeLineEndings(filteredLines);
        }

        /// <summary>
        /// 判断是否为需要过滤的元数据行
        /// </summary>
        /// <param name="line">待检查的文本行</param>
        /// <returns>如果是元数据行返回true</returns>
        private bool IsMetadataLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            
            // 标准化处理：去除空格，统一冒号格式
            var normalizedLine = line.Replace(" ", "").Replace("：", ":");
            
            return normalizedLine.StartsWith("程序名称:", StringComparison.OrdinalIgnoreCase) ||
                   normalizedLine.StartsWith("变量类型:", StringComparison.OrdinalIgnoreCase) ||
                   normalizedLine.StartsWith("变量名称:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 智能分割文本行，正确处理各种换行符组合
        /// </summary>
        /// <param name="content">原始文本内容</param>
        /// <returns>分割后的行数组</returns>
        private string[] SplitLines(string content)
        {
            if (string.IsNullOrEmpty(content))
                return new string[0];

            // 先统一换行符为\n，然后分割
            var normalizedContent = content.Replace("\r\n", "\n").Replace("\r", "\n");
            return normalizedContent.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        /// <summary>
        /// 标准化换行符并清理多余空行
        /// </summary>
        /// <param name="lines">文本行列表</param>
        /// <returns>标准化后的文本内容</returns>
        private string NormalizeLineEndings(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            // 清理连续的空行，最多保留一个空行
            var cleanedLines = new List<string>();
            bool lastLineWasEmpty = false;

            foreach (var line in lines)
            {
                bool currentLineEmpty = string.IsNullOrWhiteSpace(line);
                
                if (currentLineEmpty && lastLineWasEmpty)
                {
                    continue; // 跳过连续的空行
                }
                
                cleanedLines.Add(line);
                lastLineWasEmpty = currentLineEmpty;
            }

            // 移除开头和结尾的空行
            while (cleanedLines.Count > 0 && string.IsNullOrWhiteSpace(cleanedLines[0]))
            {
                cleanedLines.RemoveAt(0);
            }
            while (cleanedLines.Count > 0 && string.IsNullOrWhiteSpace(cleanedLines[cleanedLines.Count - 1]))
            {
                cleanedLines.RemoveAt(cleanedLines.Count - 1);
            }

            // 使用平台标准换行符重新组合
            return string.Join(Environment.NewLine, cleanedLines);
        }

    }
}
