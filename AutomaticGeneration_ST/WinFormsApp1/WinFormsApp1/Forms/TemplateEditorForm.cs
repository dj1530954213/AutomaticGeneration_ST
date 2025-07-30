using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WinFormsApp1.Config;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Forms
{
    /// <summary>
    /// 模板编辑器 - 支持在线编辑、语法高亮和智能提示
    /// </summary>
    public partial class TemplateEditorForm : Form
    {
        #region 私有字段

        private RichTextBox editorRichTextBox;
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private Panel editorPanel;
        private Panel lineNumberPanel;
        private TreeView templateTreeView;
        private SplitContainer mainSplitContainer;
        private SplitContainer editorSplitContainer;
        private TabControl editorTabControl;
        
        // 预览相关控件
        private Panel previewPanel;
        private TabControl previewTabControl;
        private TextBox sampleDataTextBox;
        private RichTextBox previewResultTextBox;
        private Button refreshPreviewButton;
        private CheckBox autoRefreshCheckBox;
        private Label previewStatusLabel;
        private System.Windows.Forms.Timer? previewRefreshTimer;
        
        private ToolStripMenuItem fileMenuItem;
        private ToolStripMenuItem editMenuItem;
        private ToolStripMenuItem viewMenuItem;
        private ToolStripMenuItem helpMenuItem;
        
        private ToolStripButton newButton;
        private ToolStripButton openButton;
        private ToolStripButton saveButton;
        private ToolStripButton undoButton;
        private ToolStripButton redoButton;
        private ToolStripButton findButton;
        
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel lineColumnLabel;
        private ToolStripStatusLabel encodingLabel;
        
        private string currentFilePath = string.Empty;
        private bool isContentChanged = false;
        private PointType currentPointType = PointType.AI;
        private TemplateVersion currentVersion = TemplateVersion.Default;
        
        // 自动完成相关
        private ListBox? autoCompleteListBox;
        private System.Windows.Forms.Timer? autoCompleteTimer;
        private string lastInputText = string.Empty;
        private int autoCompleteStartPosition = 0;
        
        // 语法高亮相关
        private readonly Dictionary<string, Color> syntaxColors = new Dictionary<string, Color>
        {
            { "keyword", Color.Blue },
            { "string", Color.FromArgb(163, 21, 21) },
            { "comment", Color.Green },
            { "variable", Color.Purple },
            { "function", Color.DarkBlue },
            { "operator", Color.Red }
        };
        
        private readonly string[] scribanKeywords = {
            "for", "end", "if", "else", "elsif", "case", "when", "while", "break", "continue",
            "capture", "assign", "include", "render", "with", "in", "and", "or", "not"
        };

        #endregion

        #region 构造函数和初始化

        public TemplateEditorForm()
        {
            InitializeComponent();
            LoadTemplateTree();
            SetupEditor();
            ApplyTheme();
            RegisterEventHandlers();
        }

        private void InitializeComponent()
        {
            Text = "模板编辑器";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Font = new Font("微软雅黑", 10F);

            // 创建菜单栏
            CreateMenuStrip();
            
            // 创建工具栏
            CreateToolStrip();
            
            // 创建状态栏
            CreateStatusStrip();
            
            // 创建主分割容器
            CreateMainLayout();
            
            // 设置控件层次结构
            Controls.Add(mainSplitContainer);
            Controls.Add(toolStrip);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            
            MainMenuStrip = menuStrip;
        }

        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip
            {
                Font = new Font("微软雅黑", 9F)
            };

            // 文件菜单
            fileMenuItem = new ToolStripMenuItem("文件(&F)");
            fileMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("新建(&N)", null, OnNewTemplate, Keys.Control | Keys.N),
                new ToolStripMenuItem("打开(&O)", null, OnOpenTemplate, Keys.Control | Keys.O),
                new ToolStripSeparator(),
                new ToolStripMenuItem("保存(&S)", null, OnSaveTemplate, Keys.Control | Keys.S),
                new ToolStripMenuItem("另存为(&A)", null, OnSaveAsTemplate, Keys.Control | Keys.Shift | Keys.S),
                new ToolStripSeparator(),
                new ToolStripMenuItem("导入模板(&I)", null, OnImportTemplate),
                new ToolStripMenuItem("导出模板(&E)", null, OnExportTemplate),
                new ToolStripSeparator(),
                new ToolStripMenuItem("退出(&X)", null, OnExitEditor, Keys.Alt | Keys.F4)
            });

            // 编辑菜单
            editMenuItem = new ToolStripMenuItem("编辑(&E)");
            editMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("撤销(&U)", null, OnUndo, Keys.Control | Keys.Z),
                new ToolStripMenuItem("重做(&R)", null, OnRedo, Keys.Control | Keys.Y),
                new ToolStripSeparator(),
                new ToolStripMenuItem("剪切(&T)", null, OnCut, Keys.Control | Keys.X),
                new ToolStripMenuItem("复制(&C)", null, OnCopy, Keys.Control | Keys.C),
                new ToolStripMenuItem("粘贴(&P)", null, OnPaste, Keys.Control | Keys.V),
                new ToolStripSeparator(),
                new ToolStripMenuItem("查找(&F)", null, OnFind, Keys.Control | Keys.F),
                new ToolStripMenuItem("替换(&H)", null, OnReplace, Keys.Control | Keys.H),
                new ToolStripSeparator(),
                new ToolStripMenuItem("全选(&A)", null, OnSelectAll, Keys.Control | Keys.A)
            });

            // 视图菜单
            viewMenuItem = new ToolStripMenuItem("视图(&V)");
            viewMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("显示行号", null, OnToggleLineNumbers) { Checked = true },
                new ToolStripMenuItem("语法高亮", null, OnToggleSyntaxHighlight) { Checked = true },
                new ToolStripMenuItem("自动换行", null, OnToggleWordWrap),
                new ToolStripSeparator(),
                new ToolStripMenuItem("显示预览面板", null, OnTogglePreviewPanel) { Checked = true },
                new ToolStripSeparator(),
                new ToolStripMenuItem("放大字体", null, OnZoomIn, Keys.Control | Keys.Oemplus),
                new ToolStripMenuItem("缩小字体", null, OnZoomOut, Keys.Control | Keys.OemMinus),
                new ToolStripMenuItem("重置字体", null, OnResetZoom, Keys.Control | Keys.D0)
            });

            // 帮助菜单
            helpMenuItem = new ToolStripMenuItem("帮助(&H)");
            helpMenuItem.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Scriban语法帮助", null, OnSyntaxHelp, Keys.F1),
                new ToolStripMenuItem("模板变量参考", null, OnVariableReference),
                new ToolStripSeparator(),
                new ToolStripMenuItem("模板库管理", null, OnOpenTemplateLibrary, Keys.F2),
                new ToolStripSeparator(),
                new ToolStripMenuItem("关于模板编辑器", null, OnAboutEditor)
            });

            menuStrip.Items.AddRange(new ToolStripItem[]
            {
                fileMenuItem, editMenuItem, viewMenuItem, helpMenuItem
            });
        }

        private void CreateToolStrip()
        {
            toolStrip = new ToolStrip
            {
                Font = new Font("微软雅黑", 9F),
                ImageScalingSize = new Size(16, 16)
            };

            newButton = new ToolStripButton("新建", null, OnNewTemplate) { ToolTipText = "新建模板 (Ctrl+N)" };
            openButton = new ToolStripButton("打开", null, OnOpenTemplate) { ToolTipText = "打开模板 (Ctrl+O)" };
            saveButton = new ToolStripButton("保存", null, OnSaveTemplate) { ToolTipText = "保存模板 (Ctrl+S)" };
            
            undoButton = new ToolStripButton("撤销", null, OnUndo) { ToolTipText = "撤销 (Ctrl+Z)" };
            redoButton = new ToolStripButton("重做", null, OnRedo) { ToolTipText = "重做 (Ctrl+Y)" };
            
            findButton = new ToolStripButton("查找", null, OnFind) { ToolTipText = "查找 (Ctrl+F)" };

            var addToFavoritesButton = new ToolStripButton("⭐ 收藏", null, (s, e) => AddCurrentTemplateToFavorites()) 
            { 
                ToolTipText = "添加当前模板到收藏夹" 
            };

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                newButton,
                openButton,
                saveButton,
                new ToolStripSeparator(),
                undoButton,
                redoButton,
                new ToolStripSeparator(),
                findButton,
                new ToolStripSeparator(),
                addToFavoritesButton,
                new ToolStripSeparator(),
                new ToolStripLabel("点类型:"),
                CreatePointTypeComboBox(),
                new ToolStripLabel("版本:"),
                CreateVersionComboBox()
            });
        }

        private ToolStripComboBox CreatePointTypeComboBox()
        {
            var combo = new ToolStripComboBox("pointTypeCombo")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(80, 25),
                ToolTipText = "选择点位类型"
            };
            
            foreach (PointType pointType in Enum.GetValues<PointType>())
            {
                combo.Items.Add(pointType.ToString());
            }
            
            combo.SelectedIndex = 0;
            combo.SelectedIndexChanged += OnPointTypeChanged;
            
            return combo;
        }

        private ToolStripComboBox CreateVersionComboBox()
        {
            var combo = new ToolStripComboBox("versionCombo")
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(80, 25),
                ToolTipText = "选择模板版本"
            };
            
            foreach (TemplateVersion version in Enum.GetValues<TemplateVersion>())
            {
                combo.Items.Add(version.ToString());
            }
            
            combo.SelectedIndex = 0;
            combo.SelectedIndexChanged += OnVersionChanged;
            
            return combo;
        }

        private void CreateStatusStrip()
        {
            statusStrip = new StatusStrip();

            statusLabel = new ToolStripStatusLabel("就绪")
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lineColumnLabel = new ToolStripStatusLabel("行 1, 列 1")
            {
                AutoSize = true
            };

            encodingLabel = new ToolStripStatusLabel("UTF-8")
            {
                AutoSize = true
            };

            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                statusLabel,
                new ToolStripStatusLabel("|"),
                lineColumnLabel,
                new ToolStripStatusLabel("|"),
                encodingLabel
            });
        }

        private void CreateMainLayout()
        {
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 50,
                Panel2MinSize = 100
            };

            // 创建模板树视图
            CreateTemplateTreeView();
            mainSplitContainer.Panel1.Controls.Add(templateTreeView);

            // 创建编辑器和预览的分割容器
            CreateEditorPreviewLayout();
            mainSplitContainer.Panel2.Controls.Add(editorSplitContainer);
        }

        private void CreateEditorPreviewLayout()
        {
            editorSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };

            // 创建编辑器标签页
            CreateEditorTabs();
            editorSplitContainer.Panel1.Controls.Add(editorTabControl);

            // 创建预览面板
            CreatePreviewPanel();
            editorSplitContainer.Panel2.Controls.Add(previewPanel);
        }

        private void CreateTemplateTreeView()
        {
            templateTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                ShowLines = true,
                ShowPlusMinus = true,
                FullRowSelect = true,
                HideSelection = false
            };

            templateTreeView.NodeMouseDoubleClick += OnTemplateNodeDoubleClick;
        }

        private void CreateEditorTabs()
        {
            editorTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F)
            };

            // 创建默认编辑器标签页
            CreateDefaultEditorTab();
        }

        private void CreateDefaultEditorTab()
        {
            var tabPage = new TabPage("新模板")
            {
                UseVisualStyleBackColor = true
            };

            var editorSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 25,
                Panel2MinSize = 50,
                IsSplitterFixed = false
            };

            // 行号面板
            lineNumberPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.LightGray,
                Width = 50
            };
            lineNumberPanel.Paint += OnLineNumberPanelPaint;

            // 编辑器面板
            editorPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            editorRichTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11F),
                WordWrap = false,
                AcceptsTab = true,
                SelectionTabs = new int[] { 20, 40, 60, 80 },
                ShowSelectionMargin = true,
                DetectUrls = false
            };

            // 绑定编辑器事件
            editorRichTextBox.TextChanged += OnEditorTextChanged;
            editorRichTextBox.SelectionChanged += OnEditorSelectionChanged;

            editorPanel.Controls.Add(editorRichTextBox);
            
            editorSplitter.Panel1.Controls.Add(lineNumberPanel);
            editorSplitter.Panel2.Controls.Add(editorPanel);
            
            tabPage.Controls.Add(editorSplitter);
            editorTabControl.TabPages.Add(tabPage);
        }

        /// <summary>
        /// 创建预览面板
        /// </summary>
        private void CreatePreviewPanel()
        {
            previewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            // 创建预览标签页控件
            previewTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F)
            };

            // 创建样本数据标签页
            var sampleDataTab = new TabPage("样本数据");
            CreateSampleDataTab(sampleDataTab);
            previewTabControl.TabPages.Add(sampleDataTab);

            // 创建预览结果标签页
            var previewResultTab = new TabPage("预览结果");
            CreatePreviewResultTab(previewResultTab);
            previewTabControl.TabPages.Add(previewResultTab);

            // 创建预览控制面板
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = SystemColors.Control
            };

            refreshPreviewButton = new Button
            {
                Text = "🔄 刷新预览",
                Location = new Point(5, 5),
                Size = new Size(100, 25),
                UseVisualStyleBackColor = true
            };
            refreshPreviewButton.Click += OnRefreshPreview;

            autoRefreshCheckBox = new CheckBox
            {
                Text = "自动刷新",
                Location = new Point(115, 7),
                Size = new Size(80, 20),
                Checked = true
            };
            autoRefreshCheckBox.CheckedChanged += OnAutoRefreshChanged;

            previewStatusLabel = new Label
            {
                Text = "就绪",
                Location = new Point(205, 9),
                Size = new Size(200, 15),
                ForeColor = Color.DarkGreen
            };

            controlPanel.Controls.AddRange(new Control[] 
            { 
                refreshPreviewButton, 
                autoRefreshCheckBox, 
                previewStatusLabel 
            });

            previewPanel.Controls.Add(previewTabControl);
            previewPanel.Controls.Add(controlPanel);

            // 初始化预览刷新定时器
            InitializePreviewTimer();
        }

        /// <summary>
        /// 创建样本数据标签页
        /// </summary>
        private void CreateSampleDataTab(TabPage tabPage)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 标题标签
            var titleLabel = new Label
            {
                Text = "模板变量样本数据 (JSON格式):",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Padding = new Padding(5)
            };

            // 样本数据文本框
            sampleDataTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10F),
                Text = GenerateDefaultSampleData()
            };
            sampleDataTextBox.TextChanged += OnSampleDataChanged;

            // 帮助标签
            var helpLabel = new Label
            {
                Text = "提示: 修改上方的JSON数据来预览不同的模板渲染结果",
                Dock = DockStyle.Fill,
                ForeColor = Color.Gray,
                Font = new Font("微软雅黑", 8F),
                Padding = new Padding(5, 2, 5, 5)
            };

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(sampleDataTextBox, 0, 1);
            layout.Controls.Add(helpLabel, 0, 2);

            tabPage.Controls.Add(layout);
        }

        /// <summary>
        /// 创建预览结果标签页
        /// </summary>
        private void CreatePreviewResultTab(TabPage tabPage)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 标题标签
            var titleLabel = new Label
            {
                Text = "模板渲染结果:",
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Padding = new Padding(5)
            };

            // 预览结果文本框
            previewResultTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10F),
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 248, 248),
                Text = "请选择模板进行预览..."
            };

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(previewResultTextBox, 0, 1);

            tabPage.Controls.Add(layout);
        }

        /// <summary>
        /// 初始化预览定时器
        /// </summary>
        private void InitializePreviewTimer()
        {
            previewRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1秒延迟
            };
            previewRefreshTimer.Tick += OnPreviewTimerTick;
        }

        #endregion

        #region 模板树管理

        private void LoadTemplateTree()
        {
            try
            {
                if (templateTreeView == null) return;

                templateTreeView.Nodes.Clear();

                foreach (PointType pointType in Enum.GetValues<PointType>())
                {
                    var pointTypeNode = new TreeNode($"{pointType} 点位模板")
                    {
                        Tag = pointType,
                        ImageIndex = 0,
                        SelectedImageIndex = 0
                    };

                    // 获取该点类型的所有模板
                    var templates = TemplateManager.GetAllTemplates(pointType);
                    foreach (var template in templates.OrderBy(t => t.Version))
                    {
                        var templateNode = new TreeNode($"{template.Version} - {template.Name}")
                        {
                            Tag = template,
                            ImageIndex = 1,
                            SelectedImageIndex = 1,
                            ToolTipText = $"作者: {template.Author}\n创建: {template.CreatedDate:yyyy-MM-dd}\n修改: {template.ModifiedDate:yyyy-MM-dd}"
                        };
                        
                        pointTypeNode.Nodes.Add(templateNode);
                    }

                    templateTreeView.Nodes.Add(pointTypeNode);
                }

                templateTreeView.ExpandAll();
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载模板树时出错: {ex.Message}", true);
            }
        }

        private void OnTemplateNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is TemplateInfo templateInfo)
            {
                LoadTemplateForEditing(templateInfo);
            }
        }

        private void LoadTemplateForEditing(TemplateInfo templateInfo)
        {
            try
            {
                var templatePath = TemplateManager.GetTemplatePath(templateInfo.PointType.ToString(), templateInfo.Version.ToString());
                if (File.Exists(templatePath))
                {
                    var content = File.ReadAllText(templatePath);
                    
                    // 创建新的编辑器标签页或使用现有的
                    var tabName = $"{templateInfo.PointType}_{templateInfo.Version}";
                    var existingTab = FindEditorTab(tabName);
                    
                    if (existingTab != null)
                    {
                        editorTabControl.SelectedTab = existingTab;
                    }
                    else
                    {
                        CreateEditorTab(tabName, content, templateInfo);
                    }
                    
                    currentFilePath = templatePath;
                    currentPointType = templateInfo.PointType;
                    currentVersion = templateInfo.Version;
                    
                    UpdateStatus($"已加载模板: {templateInfo.Name}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载模板时出错: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private TabPage? FindEditorTab(string tabName)
        {
            return editorTabControl.TabPages.Cast<TabPage>()
                .FirstOrDefault(tab => tab.Text == tabName);
        }

        private void CreateEditorTab(string tabName, string content, TemplateInfo templateInfo)
        {
            var tabPage = new TabPage(tabName)
            {
                UseVisualStyleBackColor = true,
                Tag = templateInfo
            };

            var editorSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 25,
                Panel2MinSize = 50
            };

            // 行号面板
            var linePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.LightGray,
                Width = 50
            };
            linePanel.Paint += OnLineNumberPanelPaint;

            // 编辑器
            var richTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11F),
                WordWrap = false,
                AcceptsTab = true,
                SelectionTabs = new int[] { 20, 40, 60, 80 },
                ShowSelectionMargin = true,
                DetectUrls = false,
                Text = content
            };

            // 设置事件处理
            richTextBox.TextChanged += OnEditorTextChanged;
            richTextBox.SelectionChanged += OnEditorSelectionChanged;
            richTextBox.KeyDown += OnEditorKeyDown;

            var editorPanel = new Panel { Dock = DockStyle.Fill };
            editorPanel.Controls.Add(richTextBox);

            editorSplitter.Panel1.Controls.Add(linePanel);
            editorSplitter.Panel2.Controls.Add(editorPanel);

            tabPage.Controls.Add(editorSplitter);
            editorTabControl.TabPages.Add(tabPage);
            editorTabControl.SelectedTab = tabPage;

            // 应用语法高亮
            ApplySyntaxHighlighting(richTextBox);
        }

        #endregion

        #region 编辑器设置和功能

        private void SetupEditor()
        {
            if (editorRichTextBox == null) return;

            editorRichTextBox.TextChanged += OnEditorTextChanged;
            editorRichTextBox.SelectionChanged += OnEditorSelectionChanged;
            editorRichTextBox.KeyDown += OnEditorKeyDown;
            editorRichTextBox.VScroll += OnEditorScroll;
            
            // 初始化自动完成功能
            InitializeAutoComplete();
        }

        private void InitializeAutoComplete()
        {
            // 创建自动完成计时器
            autoCompleteTimer = new System.Windows.Forms.Timer
            {
                Interval = 500 // 500ms延迟
            };
            autoCompleteTimer.Tick += OnAutoCompleteTimer;
            
            // 创建自动完成列表框
            CreateAutoCompleteListBox();
        }

        private void CreateAutoCompleteListBox()
        {
            autoCompleteListBox = new ListBox
            {
                Font = new Font("微软雅黑", 9F),
                Size = new Size(200, 120),
                Visible = false,
                IntegralHeight = false,
                BackColor = ThemeManager.GetBackgroundColor(),
                ForeColor = ThemeManager.GetTextColor(),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // 添加事件处理
            autoCompleteListBox.DoubleClick += OnAutoCompleteDoubleClick;
            autoCompleteListBox.KeyDown += OnAutoCompleteKeyDown;
            
            // 添加到编辑器容器
            if (editorPanel != null)
            {
                editorPanel.Controls.Add(autoCompleteListBox);
                autoCompleteListBox.BringToFront();
            }
        }

        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (sender is RichTextBox editor)
            {
                isContentChanged = true;
                UpdateTabTitle();
                ApplySyntaxHighlighting(editor);
                UpdateLineNumbers();
                ValidateSyntax(editor.Text);
            }
        }

        private void OnEditorSelectionChanged(object? sender, EventArgs e)
        {
            if (sender is RichTextBox editor)
            {
                UpdateCursorPosition(editor);
            }
        }

        private void OnEditorKeyDown(object? sender, KeyEventArgs e)
        {
            if (sender is RichTextBox editor)
            {
                // 处理Tab键
                if (e.KeyCode == Keys.Tab)
                {
                    if (autoCompleteListBox?.Visible == true)
                    {
                        // 如果自动完成列表可见，插入选中项
                        InsertAutoCompleteSelection();
                        e.Handled = true;
                        return;
                    }

                    if (e.Shift)
                    {
                        // Shift+Tab: 减少缩进
                        DecreaseIndent(editor);
                    }
                    else
                    {
                        // Tab: 增加缩进
                        IncreaseIndent(editor);
                    }
                    e.Handled = true;
                }
                // 处理回车键自动缩进
                else if (e.KeyCode == Keys.Return)
                {
                    if (autoCompleteListBox?.Visible == true)
                    {
                        // 如果自动完成列表可见，插入选中项
                        InsertAutoCompleteSelection();
                        e.Handled = true;
                        return;
                    }
                    AutoIndent(editor);
                }
                // 处理Escape键
                else if (e.KeyCode == Keys.Escape)
                {
                    HideAutoComplete();
                    e.Handled = true;
                }
                // 处理上下箭头键
                else if (autoCompleteListBox?.Visible == true && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
                {
                    NavigateAutoComplete(e.KeyCode == Keys.Down);
                    e.Handled = true;
                }
                // 处理Ctrl+Space触发自动完成
                else if (e.Control && e.KeyCode == Keys.Space)
                {
                    ShowAutoComplete(editor);
                    e.Handled = true;
                }
                // 处理其他字符输入
                else if (!e.Control && !e.Alt && char.IsLetterOrDigit((char)e.KeyValue))
                {
                    // 延迟显示自动完成
                    autoCompleteTimer?.Stop();
                    autoCompleteTimer?.Start();
                }
            }
        }

        private void OnEditorScroll(object? sender, EventArgs e)
        {
            if (lineNumberPanel != null)
            {
                lineNumberPanel.Invalidate();
            }
        }

        private void UpdateTabTitle()
        {
            if (editorTabControl.SelectedTab != null)
            {
                var tabText = editorTabControl.SelectedTab.Text;
                if (isContentChanged && !tabText.EndsWith(" *"))
                {
                    editorTabControl.SelectedTab.Text = tabText + " *";
                }
            }
        }

        private void UpdateCursorPosition(RichTextBox editor)
        {
            var currentPos = editor.SelectionStart;
            var line = editor.GetLineFromCharIndex(currentPos) + 1;
            var column = currentPos - editor.GetFirstCharIndexFromLine(line - 1) + 1;
            
            lineColumnLabel.Text = $"行 {line}, 列 {column}";
        }

        #endregion

        #region 语法高亮

        private void ApplySyntaxHighlighting(RichTextBox editor)
        {
            if (editor == null) return;

            try
            {
                var currentSelection = editor.SelectionStart;
                var currentLength = editor.SelectionLength;

                // 重置所有文本格式
                editor.SelectAll();
                editor.SelectionColor = ThemeManager.GetTextColor();
                editor.SelectionFont = new Font("Consolas", 11F);

                var text = editor.Text;

                // 高亮Scriban关键字
                HighlightKeywords(editor, text);

                // 高亮字符串
                HighlightStrings(editor, text);

                // 高亮注释
                HighlightComments(editor, text);

                // 高亮变量
                HighlightVariables(editor, text);

                // 恢复选择
                editor.SelectionStart = currentSelection;
                editor.SelectionLength = currentLength;
            }
            catch (Exception ex)
            {
                UpdateStatus($"语法高亮时出错: {ex.Message}", true);
            }
        }

        private void HighlightKeywords(RichTextBox editor, string text)
        {
            foreach (var keyword in scribanKeywords)
            {
                var pattern = $@"\b{Regex.Escape(keyword)}\b";
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                
                foreach (Match match in matches)
                {
                    editor.SelectionStart = match.Index;
                    editor.SelectionLength = match.Length;
                    editor.SelectionColor = syntaxColors["keyword"];
                    editor.SelectionFont = new Font("Consolas", 11F, FontStyle.Bold);
                }
            }
        }

        private void HighlightStrings(RichTextBox editor, string text)
        {
            // 双引号字符串
            var stringPattern = @"""[^""]*""";
            var matches = Regex.Matches(text, stringPattern);
            
            foreach (Match match in matches)
            {
                editor.SelectionStart = match.Index;
                editor.SelectionLength = match.Length;
                editor.SelectionColor = syntaxColors["string"];
            }

            // 单引号字符串
            stringPattern = @"'[^']*'";
            matches = Regex.Matches(text, stringPattern);
            
            foreach (Match match in matches)
            {
                editor.SelectionStart = match.Index;
                editor.SelectionLength = match.Length;
                editor.SelectionColor = syntaxColors["string"];
            }
        }

        private void HighlightComments(RichTextBox editor, string text)
        {
            // Scriban注释 {{# ... #}}
            var commentPattern = @"\{\{#.*?#\}\}";
            var matches = Regex.Matches(text, commentPattern, RegexOptions.Singleline);
            
            foreach (Match match in matches)
            {
                editor.SelectionStart = match.Index;
                editor.SelectionLength = match.Length;
                editor.SelectionColor = syntaxColors["comment"];
                editor.SelectionFont = new Font("Consolas", 11F, FontStyle.Italic);
            }

            // ST注释 (* ... *)
            commentPattern = @"\(\*.*?\*\)";
            matches = Regex.Matches(text, commentPattern, RegexOptions.Singleline);
            
            foreach (Match match in matches)
            {
                editor.SelectionStart = match.Index;
                editor.SelectionLength = match.Length;
                editor.SelectionColor = syntaxColors["comment"];
                editor.SelectionFont = new Font("Consolas", 11F, FontStyle.Italic);
            }
        }

        private void HighlightVariables(RichTextBox editor, string text)
        {
            // Scriban变量 {{ variable }}
            var variablePattern = @"\{\{\s*[\w\.]+\s*\}\}";
            var matches = Regex.Matches(text, variablePattern);
            
            foreach (Match match in matches)
            {
                editor.SelectionStart = match.Index;
                editor.SelectionLength = match.Length;
                editor.SelectionColor = syntaxColors["variable"];
                editor.SelectionFont = new Font("Consolas", 11F, FontStyle.Bold);
            }
        }

        #endregion

        #region 行号显示

        private void OnLineNumberPanelPaint(object? sender, PaintEventArgs e)
        {
            if (sender is Panel panel && editorRichTextBox != null)
            {
                DrawLineNumbers(e.Graphics, panel, editorRichTextBox);
            }
        }

        private void DrawLineNumbers(Graphics g, Panel panel, RichTextBox editor)
        {
            var lineHeight = TextRenderer.MeasureText("0", editor.Font).Height;
            var startIndex = editor.GetCharIndexFromPosition(new Point(0, 0));
            var startLine = editor.GetLineFromCharIndex(startIndex);
            var endIndex = editor.GetCharIndexFromPosition(new Point(0, editor.Height));
            var endLine = editor.GetLineFromCharIndex(endIndex);

            g.Clear(panel.BackColor);

            using var brush = new SolidBrush(ThemeManager.GetSecondaryTextColor());
            using var font = new Font("Consolas", 9F);

            for (int line = startLine; line <= endLine + 1; line++)
            {
                var lineNumber = (line + 1).ToString();
                var y = (line - startLine) * lineHeight;
                var rect = new Rectangle(0, y, panel.Width - 5, lineHeight);
                
                TextRenderer.DrawText(g, lineNumber, font, rect, 
                    ThemeManager.GetSecondaryTextColor(), 
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            }
        }

        private void UpdateLineNumbers()
        {
            if (lineNumberPanel != null)
            {
                lineNumberPanel.Invalidate();
            }
        }

        #endregion

        #region 语法验证

        private ListView? validationListView;
        private Panel? validationPanel;
        private bool validationPanelVisible = false;

        private void ValidateSyntax(string content)
        {
            try
            {
                // 使用增强的模板语法验证器
                var result = TemplateSyntaxValidator.ValidateTemplate(content, currentPointType, true);
                
                // 更新状态栏
                UpdateStatus(result.GetSummary(), !result.IsValid);
                
                // 更新验证面板
                UpdateValidationPanel(result);
                
                // 如果有问题，显示验证面板
                if (result.HasIssues && !validationPanelVisible)
                {
                    ShowValidationPanel();
                }
                else if (!result.HasIssues && validationPanelVisible)
                {
                    HideValidationPanel();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"语法验证失败: {ex.Message}", true);
            }
        }

        private void CreateValidationPanel()
        {
            validationPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                Visible = false
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = ThemeManager.GetSurfaceColor()
            };

            var titleLabel = new Label
            {
                Text = "📋 语法验证结果",
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Location = new Point(10, 6),
                AutoSize = true,
                ForeColor = ThemeManager.GetTextColor()
            };

            var closeButton = new Button
            {
                Text = "✕",
                Size = new Size(25, 20),
                Location = new Point(validationPanel.Width - 35, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = ThemeManager.GetSecondaryTextColor()
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => HideValidationPanel();

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(closeButton);

            validationListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("微软雅黑", 9F),
                BackColor = ThemeManager.GetBackgroundColor(),
                ForeColor = ThemeManager.GetTextColor()
            };

            // 添加列
            validationListView.Columns.Add("类型", 60);
            validationListView.Columns.Add("位置", 80);
            validationListView.Columns.Add("消息", 300);
            validationListView.Columns.Add("建议", 200);

            // 双击跳转到错误位置
            validationListView.DoubleClick += OnValidationItemDoubleClick;

            validationPanel.Controls.Add(validationListView);
            validationPanel.Controls.Add(headerPanel);

            // 添加到主分割容器
            if (mainSplitContainer?.Panel2 != null)
            {
                mainSplitContainer.Panel2.Controls.Add(validationPanel);
            }
        }

        private void UpdateValidationPanel(TemplateSyntaxValidator.ValidationResult result)
        {
            if (validationListView == null)
            {
                CreateValidationPanel();
            }

            validationListView!.Items.Clear();

            // 添加错误
            foreach (var error in result.Errors)
            {
                var item = new ListViewItem(new[]
                {
                    "❌ 错误",
                    $"第{error.Line}行,第{error.Column}列",
                    error.Message,
                    error.SuggestedFix
                })
                {
                    ForeColor = Color.Red,
                    Tag = error,
                    ToolTipText = $"错误代码: {error.ErrorCode}\n上下文: {error.ContextText}"
                };
                validationListView.Items.Add(item);
            }

            // 添加警告
            foreach (var warning in result.Warnings)
            {
                var item = new ListViewItem(new[]
                {
                    "⚠️ 警告",
                    $"第{warning.Line}行,第{warning.Column}列",
                    warning.Message,
                    warning.SuggestedImprovement
                })
                {
                    ForeColor = Color.Orange,
                    Tag = warning,
                    ToolTipText = $"警告类型: {warning.Type}"
                };
                validationListView.Items.Add(item);
            }

            // 添加模板信息
            if (!result.HasIssues)
            {
                var infoItem = new ListViewItem(new[]
                {
                    "ℹ️ 信息",
                    "",
                    $"模板复杂度: {result.Complexity}, 必需字段: {result.RequiredFields.Count}个",
                    "模板语法正确"
                })
                {
                    ForeColor = ThemeManager.GetSecondaryTextColor()
                };
                validationListView.Items.Add(infoItem);
            }

            // 自动调整列宽
            foreach (ColumnHeader column in validationListView.Columns)
            {
                column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            }
        }

        private void OnValidationItemDoubleClick(object? sender, EventArgs e)
        {
            if (validationListView?.SelectedItems.Count > 0)
            {
                var selectedItem = validationListView.SelectedItems[0];
                
                if (selectedItem.Tag is TemplateSyntaxValidator.ValidationError error)
                {
                    JumpToLine(error.Line, error.Column);
                }
                else if (selectedItem.Tag is TemplateSyntaxValidator.ValidationWarning warning)
                {
                    JumpToLine(warning.Line, warning.Column);
                }
            }
        }

        private void JumpToLine(int line, int column)
        {
            var currentEditor = GetCurrentEditor();
            if (currentEditor == null) return;

            try
            {
                var targetLine = Math.Max(0, line - 1);
                var lineIndex = currentEditor.GetFirstCharIndexFromLine(targetLine);
                
                if (lineIndex >= 0)
                {
                    var targetColumn = Math.Min(column - 1, currentEditor.Lines[targetLine].Length);
                    var targetIndex = lineIndex + Math.Max(0, targetColumn);
                    
                    currentEditor.SelectionStart = targetIndex;
                    currentEditor.SelectionLength = 0;
                    currentEditor.ScrollToCaret();
                    currentEditor.Focus();
                    
                    UpdateStatus($"已跳转到第{line}行,第{column}列");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"跳转失败: {ex.Message}", true);
            }
        }

        private void ShowValidationPanel()
        {
            if (validationPanel != null && !validationPanelVisible)
            {
                validationPanel.Visible = true;
                validationPanelVisible = true;
                
                // 调整编辑器标签页控件高度
                if (editorTabControl != null)
                {
                    editorTabControl.Height -= validationPanel.Height;
                }
            }
        }

        private void HideValidationPanel()
        {
            if (validationPanel != null && validationPanelVisible)
            {
                validationPanel.Visible = false;
                validationPanelVisible = false;
                
                // 恢复编辑器标签页控件高度
                if (editorTabControl != null)
                {
                    editorTabControl.Height += validationPanel.Height;
                }
            }
        }

        #endregion

        #region 自动完成功能

        private void OnAutoCompleteTimer(object? sender, EventArgs e)
        {
            autoCompleteTimer?.Stop();
            
            var currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                ShowAutoComplete(currentEditor);
            }
        }

        private void ShowAutoComplete(RichTextBox editor)
        {
            try
            {
                var currentPos = editor.SelectionStart;
                var currentText = editor.Text;
                
                // 获取当前单词
                var wordStart = GetWordStart(currentText, currentPos);
                var wordEnd = GetWordEnd(currentText, currentPos);
                var currentWord = currentText.Substring(wordStart, wordEnd - wordStart);
                
                if (string.IsNullOrWhiteSpace(currentWord) || currentWord.Length < 2)
                {
                    HideAutoComplete();
                    return;
                }
                
                // 获取自动完成建议
                var suggestions = TemplateSyntaxValidator.GetAutoCompleteSuggestions(currentWord, currentPointType);
                
                if (!suggestions.Any())
                {
                    HideAutoComplete();
                    return;
                }
                
                // 填充自动完成列表
                autoCompleteListBox!.Items.Clear();
                foreach (var suggestion in suggestions.Take(10)) // 限制显示数量
                {
                    autoCompleteListBox.Items.Add(suggestion);
                }
                
                // 设置位置和显示
                PositionAutoCompleteListBox(editor, wordStart);
                autoCompleteListBox.Visible = true;
                autoCompleteListBox.SelectedIndex = 0;
                
                // 记录当前状态
                autoCompleteStartPosition = wordStart;
                lastInputText = currentWord;
            }
            catch (Exception ex)
            {
                UpdateStatus($"自动完成出错: {ex.Message}", true);
            }
        }

        private void PositionAutoCompleteListBox(RichTextBox editor, int wordStart)
        {
            if (autoCompleteListBox == null) return;
            
            try
            {
                // 获取字符位置
                var position = editor.GetPositionFromCharIndex(wordStart);
                
                // 计算相对于编辑器的位置
                var editorPos = editor.PointToClient(position);
                
                // 调整位置避免超出边界
                var x = Math.Max(0, Math.Min(editorPos.X, editor.Width - autoCompleteListBox.Width));
                var y = editorPos.Y + editor.Font.Height;
                
                // 如果下方空间不足，显示在上方
                if (y + autoCompleteListBox.Height > editor.Height)
                {
                    y = editorPos.Y - autoCompleteListBox.Height;
                }
                
                autoCompleteListBox.Location = new Point(x, y);
            }
            catch
            {
                // 如果位置计算失败，使用默认位置
                autoCompleteListBox.Location = new Point(10, 30);
            }
        }

        private void HideAutoComplete()
        {
            if (autoCompleteListBox != null)
            {
                autoCompleteListBox.Visible = false;
            }
        }

        private void NavigateAutoComplete(bool down)
        {
            if (autoCompleteListBox?.Visible != true || autoCompleteListBox.Items.Count == 0)
                return;
            
            var currentIndex = autoCompleteListBox.SelectedIndex;
            
            if (down)
            {
                currentIndex = (currentIndex + 1) % autoCompleteListBox.Items.Count;
            }
            else
            {
                currentIndex = currentIndex <= 0 ? autoCompleteListBox.Items.Count - 1 : currentIndex - 1;
            }
            
            autoCompleteListBox.SelectedIndex = currentIndex;
        }

        private void InsertAutoCompleteSelection()
        {
            if (autoCompleteListBox?.Visible != true || autoCompleteListBox.SelectedItem == null)
                return;
            
            var currentEditor = GetCurrentEditor();
            if (currentEditor == null) return;
            
            try
            {
                var selectedText = autoCompleteListBox.SelectedItem.ToString();
                if (string.IsNullOrEmpty(selectedText)) return;
                
                var currentPos = currentEditor.SelectionStart;
                var currentText = currentEditor.Text;
                
                // 获取当前单词的范围
                var wordStart = GetWordStart(currentText, currentPos);
                var wordEnd = GetWordEnd(currentText, currentPos);
                
                // 替换当前单词
                currentEditor.SelectionStart = wordStart;
                currentEditor.SelectionLength = wordEnd - wordStart;
                currentEditor.SelectedText = selectedText;
                
                // 设置光标位置
                currentEditor.SelectionStart = wordStart + selectedText.Length;
                currentEditor.SelectionLength = 0;
                
                // 隐藏自动完成
                HideAutoComplete();
                
                UpdateStatus($"已插入: {selectedText}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"插入自动完成项失败: {ex.Message}", true);
            }
        }

        private void OnAutoCompleteDoubleClick(object? sender, EventArgs e)
        {
            InsertAutoCompleteSelection();
        }

        private void OnAutoCompleteKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                InsertAutoCompleteSelection();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                HideAutoComplete();
                e.Handled = true;
            }
        }

        private int GetWordStart(string text, int position)
        {
            position = Math.Min(position, text.Length);
            
            while (position > 0 && (char.IsLetterOrDigit(text[position - 1]) || 
                   text[position - 1] == '_' || text[position - 1] == '.' ||
                   char.GetUnicodeCategory(text[position - 1]) == System.Globalization.UnicodeCategory.OtherLetter))
            {
                position--;
            }
            
            return position;
        }

        private int GetWordEnd(string text, int position)
        {
            while (position < text.Length && (char.IsLetterOrDigit(text[position]) || 
                   text[position] == '_' || text[position] == '.' ||
                   char.GetUnicodeCategory(text[position]) == System.Globalization.UnicodeCategory.OtherLetter))
            {
                position++;
            }
            
            return position;
        }

        #endregion

        #region 编辑功能实现

        private void IncreaseIndent(RichTextBox editor)
        {
            var selectionStart = editor.SelectionStart;
            var selectionLength = editor.SelectionLength;
            
            if (selectionLength == 0)
            {
                // 单行缩进
                editor.SelectedText = "    ";
            }
            else
            {
                // 多行缩进
                var selectedText = editor.SelectedText;
                var indentedText = string.Join("\n", 
                    selectedText.Split('\n').Select(line => "    " + line));
                editor.SelectedText = indentedText;
            }
        }

        private void DecreaseIndent(RichTextBox editor)
        {
            var selectionStart = editor.SelectionStart;
            var selectionLength = editor.SelectionLength;
            
            if (selectionLength == 0)
            {
                // 单行减少缩进
                var lineStart = editor.GetFirstCharIndexOfCurrentLine();
                var lineEnd = lineStart;
                while (lineEnd < editor.Text.Length && editor.Text[lineEnd] != '\n')
                    lineEnd++;
                
                var lineText = editor.Text.Substring(lineStart, lineEnd - lineStart);
                if (lineText.StartsWith("    "))
                {
                    editor.SelectionStart = lineStart;
                    editor.SelectionLength = 4;
                    editor.SelectedText = "";
                }
            }
            else
            {
                // 多行减少缩进
                var selectedText = editor.SelectedText;
                var dedentedText = string.Join("\n",
                    selectedText.Split('\n').Select(line => 
                        line.StartsWith("    ") ? line.Substring(4) : line));
                editor.SelectedText = dedentedText;
            }
        }

        private void AutoIndent(RichTextBox editor)
        {
            var currentLine = editor.GetLineFromCharIndex(editor.SelectionStart);
            if (currentLine > 0)
            {
                var previousLineIndex = editor.GetFirstCharIndexFromLine(currentLine - 1);
                var previousLineEnd = editor.GetFirstCharIndexFromLine(currentLine) - 1;
                var previousLineText = editor.Text.Substring(previousLineIndex, 
                    previousLineEnd - previousLineIndex);
                
                // 计算前一行的缩进
                var indent = "";
                foreach (char c in previousLineText)
                {
                    if (c == ' ' || c == '\t')
                        indent += c;
                    else
                        break;
                }
                
                // 如果前一行以特定字符结尾，增加缩进
                if (previousLineText.TrimEnd().EndsWith(":") || 
                    previousLineText.TrimEnd().EndsWith("{"))
                {
                    indent += "    ";
                }
                
                editor.SelectedText = "\n" + indent;
            }
        }

        #endregion

        #region 事件处理方法

        private void OnPointTypeChanged(object? sender, EventArgs e)
        {
            if (sender is ToolStripComboBox combo)
            {
                if (Enum.TryParse<PointType>(combo.SelectedItem?.ToString(), out var pointType))
                {
                    currentPointType = pointType;
                    UpdateStatus($"当前点类型: {pointType}");
                }
            }
        }

        private void OnVersionChanged(object? sender, EventArgs e)
        {
            if (sender is ToolStripComboBox combo)
            {
                if (Enum.TryParse<TemplateVersion>(combo.SelectedItem?.ToString(), out var version))
                {
                    currentVersion = version;
                    UpdateStatus($"当前版本: {version}");
                }
            }
        }

        private void OnNewTemplate(object? sender, EventArgs e)
        {
            CreateNewTemplate();
        }

        private void OnOpenTemplate(object? sender, EventArgs e)
        {
            OpenTemplateFile();
        }

        private void OnSaveTemplate(object? sender, EventArgs e)
        {
            SaveCurrentTemplate();
        }

        private void OnSaveAsTemplate(object? sender, EventArgs e)
        {
            SaveTemplateAs();
        }



        private void OnExitEditor(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnUndo(object? sender, EventArgs e)
        {
            if (GetCurrentEditor()?.CanUndo == true)
            {
                GetCurrentEditor().Undo();
            }
        }

        private void OnRedo(object? sender, EventArgs e)
        {
            if (GetCurrentEditor()?.CanRedo == true)
            {
                GetCurrentEditor().Redo();
            }
        }

        private void OnCut(object? sender, EventArgs e)
        {
            GetCurrentEditor()?.Cut();
        }

        private void OnCopy(object? sender, EventArgs e)
        {
            GetCurrentEditor()?.Copy();
        }

        private void OnPaste(object? sender, EventArgs e)
        {
            GetCurrentEditor()?.Paste();
        }

        private void OnFind(object? sender, EventArgs e)
        {
            ShowFindDialog();
        }

        private void OnReplace(object? sender, EventArgs e)
        {
            ShowReplaceDialog();
        }

        private void OnSelectAll(object? sender, EventArgs e)
        {
            GetCurrentEditor()?.SelectAll();
        }

        private void OnToggleLineNumbers(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = !menuItem.Checked;
                ToggleLineNumbers(menuItem.Checked);
            }
        }

        private void OnToggleSyntaxHighlight(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = !menuItem.Checked;
                ToggleSyntaxHighlighting(menuItem.Checked);
            }
        }

        private void OnToggleWordWrap(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                menuItem.Checked = !menuItem.Checked;
                ToggleWordWrap(menuItem.Checked);
            }
        }

        private void OnZoomIn(object? sender, EventArgs e)
        {
            ChangeFontSize(2);
        }

        private void OnZoomOut(object? sender, EventArgs e)
        {
            ChangeFontSize(-2);
        }

        private void OnResetZoom(object? sender, EventArgs e)
        {
            ResetFontSize();
        }

        private void OnSyntaxHelp(object? sender, EventArgs e)
        {
            ShowSyntaxHelp();
        }

        private void OnVariableReference(object? sender, EventArgs e)
        {
            ShowVariableReference();
        }

        private void OnAboutEditor(object? sender, EventArgs e)
        {
            ShowAbout();
        }

        #endregion

        #region 辅助方法

        private RichTextBox? GetCurrentEditor()
        {
            var currentTab = editorTabControl.SelectedTab;
            return currentTab?.Controls.OfType<SplitContainer>()
                .FirstOrDefault()?.Panel2.Controls.OfType<Panel>()
                .FirstOrDefault()?.Controls.OfType<RichTextBox>()
                .FirstOrDefault();
        }

        private void CreateNewTemplate()
        {
            var tabName = $"新模板{editorTabControl.TabCount + 1}";
            CreateEditorTab(tabName, GetDefaultTemplateContent(), new TemplateInfo 
            { 
                PointType = currentPointType, 
                Version = currentVersion,
                Name = tabName
            });
            
            isContentChanged = false;
            UpdateStatus("创建新模板");
        }

        private string GetDefaultTemplateContent()
        {
            return $@"(*
{currentPointType} 点位模板
版本: {currentVersion}
创建时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
作者: 
描述: 
*)

{{{{- for point in points -}}}}
(* {{{{ point.变量描述 }}}} *)
{{{{ point.变量名称HMI }}}} := {{{{ point.硬点通道号 }}}};
{{{{- end -}}}}";
        }

        private void OpenTemplateFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "打开模板文件",
                Filter = "Scriban模板文件 (*.scriban)|*.scriban|所有文件 (*.*)|*.*",
                DefaultExt = "scriban"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var content = File.ReadAllText(dialog.FileName);
                    var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    
                    CreateEditorTab(fileName, content, new TemplateInfo
                    {
                        PointType = currentPointType,
                        Version = currentVersion,
                        Name = fileName
                    });
                    
                    currentFilePath = dialog.FileName;
                    isContentChanged = false;
                    UpdateStatus($"已打开: {fileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开文件时出错: {ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveCurrentTemplate()
        {
            var currentEditor = GetCurrentEditor();
            if (currentEditor == null) return;

            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveTemplateAs();
                return;
            }

            try
            {
                File.WriteAllText(currentFilePath, currentEditor.Text);
                isContentChanged = false;
                
                // 更新标签页标题
                if (editorTabControl.SelectedTab != null)
                {
                    var tabText = editorTabControl.SelectedTab.Text;
                    if (tabText.EndsWith(" *"))
                    {
                        editorTabControl.SelectedTab.Text = tabText.Substring(0, tabText.Length - 2);
                    }
                }
                
                UpdateStatus($"已保存: {Path.GetFileName(currentFilePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件时出错: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveTemplateAs()
        {
            var currentEditor = GetCurrentEditor();
            if (currentEditor == null) return;

            using var dialog = new SaveFileDialog
            {
                Title = "另存为模板文件",
                Filter = "Scriban模板文件 (*.scriban)|*.scriban|所有文件 (*.*)|*.*",
                DefaultExt = "scriban",
                FileName = $"{currentPointType}_{currentVersion}.scriban"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(dialog.FileName, currentEditor.Text);
                    currentFilePath = dialog.FileName;
                    isContentChanged = false;
                    
                    // 更新标签页标题
                    if (editorTabControl.SelectedTab != null)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                        editorTabControl.SelectedTab.Text = fileName;
                    }
                    
                    UpdateStatus($"已另存为: {Path.GetFileName(dialog.FileName)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件时出错: {ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ImportTemplate()
        {
            MessageBox.Show("导入模板功能开发中...", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportTemplate()
        {
            MessageBox.Show("导出模板功能开发中...", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowFindDialog()
        {
            MessageBox.Show("查找功能开发中...", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowReplaceDialog()
        {
            MessageBox.Show("替换功能开发中...", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToggleLineNumbers(bool show)
        {
            if (lineNumberPanel != null)
            {
                lineNumberPanel.Visible = show;
            }
        }

        private void ToggleSyntaxHighlighting(bool enable)
        {
            if (enable)
            {
                var currentEditor = GetCurrentEditor();
                if (currentEditor != null)
                {
                    ApplySyntaxHighlighting(currentEditor);
                }
            }
        }

        private void ToggleWordWrap(bool enable)
        {
            var currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                currentEditor.WordWrap = enable;
            }
        }

        private void ChangeFontSize(float delta)
        {
            var currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                var newSize = Math.Max(8, Math.Min(72, currentEditor.Font.Size + delta));
                currentEditor.Font = new Font(currentEditor.Font.FontFamily, newSize);
                UpdateLineNumbers();
            }
        }

        private void ResetFontSize()
        {
            var currentEditor = GetCurrentEditor();
            if (currentEditor != null)
            {
                currentEditor.Font = new Font("Consolas", 11F);
                UpdateLineNumbers();
            }
        }

        private void ShowSyntaxHelp()
        {
            var helpText = @"Scriban 语法参考:

基本变量: {{ variable_name }}
循环: {{ for item in items }} ... {{ end }}
条件: {{ if condition }} ... {{ else }} ... {{ end }}
注释: {{# 这是注释 #}}

常用点位变量:
- {{ 变量名称HMI }}
- {{ 变量描述 }}  
- {{ 硬点通道号 }}
- {{ 量程高限 }}
- {{ 量程低限 }}

更多信息请参考Scriban官方文档。";

            MessageBox.Show(helpText, "Scriban语法帮助", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowVariableReference()
        {
            MessageBox.Show("变量参考功能开发中...", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            MessageBox.Show("ST脚本自动生成器 - 模板编辑器\n版本 1.0\n\n提供专业的Scriban模板编辑功能。", 
                "关于模板编辑器", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateStatus(string message, bool isError = false)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
                statusLabel.ForeColor = isError ? Color.Red : ThemeManager.GetTextColor();
            }
        }

        private void ApplyTheme()
        {
            try
            {
                BackColor = ThemeManager.GetBackgroundColor();
                ForeColor = ThemeManager.GetTextColor();
                
                if (menuStrip != null)
                {
                    menuStrip.BackColor = ThemeManager.GetMenuBarColor();
                    menuStrip.ForeColor = ThemeManager.GetTextColor();
                }
                
                if (toolStrip != null)
                {
                    toolStrip.BackColor = ThemeManager.GetToolBarColor();
                    toolStrip.ForeColor = ThemeManager.GetTextColor();
                }
                
                if (statusStrip != null)
                {
                    statusStrip.BackColor = ThemeManager.GetStatusBarColor();
                    statusStrip.ForeColor = ThemeManager.GetTextColor();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"应用主题时出错: {ex.Message}", true);
            }
        }

        private void RegisterEventHandlers()
        {
            FormClosing += OnFormClosing;
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (isContentChanged)
            {
                var result = MessageBox.Show("有未保存的更改，是否保存？", "确认",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    SaveCurrentTemplate();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        #endregion

        #region 导入/导出功能

        /// <summary>
        /// 导入模板
        /// </summary>
        private void OnImportTemplate(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "导入模板",
                    Filter = "模板文件 (*.template)|*.template|JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    FilterIndex = 1,
                    Multiselect = true
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (dialog.FileNames.Length == 1)
                    {
                        ImportSingleTemplate(dialog.FileName);
                    }
                    else
                    {
                        ImportMultipleTemplates(dialog.FileNames);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入模板时出错: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"导入失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 导出模板
        /// </summary>
        private void OnExportTemplate(object? sender, EventArgs e)
        {
            try
            {
                var currentTab = GetCurrentEditorTab();
                if (currentTab == null)
                {
                    MessageBox.Show("没有打开的模板可以导出", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dialog = new SaveFileDialog
                {
                    Title = "导出模板",
                    Filter = "模板文件 (*.template)|*.template|JSON文件 (*.json)|*.json",
                    FilterIndex = 1,
                    FileName = GetSuggestedFileName()
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ExportSingleTemplate(dialog.FileName, currentTab);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出模板时出错: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"导出失败: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 导入单个模板
        /// </summary>
        private void ImportSingleTemplate(string filePath)
        {
            UpdateStatus("正在导入模板...", false);

            try
            {
                var templateData = ImportTemplateFromFile(filePath);
                if (templateData != null)
                {
                    // 创建新的编辑器标签页
                    var tabPage = CreateNewEditorTab(templateData.Name);
                    var editor = GetEditorFromTab(tabPage);
                    
                    if (editor != null)
                    {
                        editor.Text = templateData.Content;
                        currentPointType = templateData.PointType;
                        currentVersion = templateData.Version;
                        
                        // 更新UI状态
                        UpdatePointTypeComboBox();
                        UpdateVersionComboBox();
                        
                        // 应用语法高亮
                        ApplySyntaxHighlighting(editor);
                        
                        // 标记为已修改
                        isContentChanged = true;
                        UpdateTitle();
                        
                        UpdateStatus($"成功导入模板: {templateData.Name}", false);
                        
                        // 显示导入详细信息
                        ShowImportDetails(templateData);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"导入文件 '{Path.GetFileName(filePath)}' 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导入多个模板
        /// </summary>
        private void ImportMultipleTemplates(string[] filePaths)
        {
            var progressForm = new ProgressForm($"正在导入 {filePaths.Length} 个模板文件...");
            var importResults = new List<ImportResult>();

            try
            {
                progressForm.Show(this);
                progressForm.SetMaximum(filePaths.Length);

                for (int i = 0; i < filePaths.Length; i++)
                {
                    var filePath = filePaths[i];
                    progressForm.SetProgress(i, $"导入: {Path.GetFileName(filePath)}");

                    try
                    {
                        ImportSingleTemplate(filePath);
                        importResults.Add(new ImportResult 
                        { 
                            FileName = Path.GetFileName(filePath), 
                            Success = true 
                        });
                    }
                    catch (Exception ex)
                    {
                        importResults.Add(new ImportResult 
                        { 
                            FileName = Path.GetFileName(filePath), 
                            Success = false, 
                            ErrorMessage = ex.Message 
                        });
                    }

                    if (progressForm.IsCancelled)
                        break;
                }

                progressForm.Close();
                ShowImportSummary(importResults);
            }
            finally
            {
                progressForm?.Close();
            }
        }

        /// <summary>
        /// 导出单个模板
        /// </summary>
        private void ExportSingleTemplate(string filePath, TabPage tabPage)
        {
            UpdateStatus("正在导出模板...", false);

            try
            {
                var editor = GetEditorFromTab(tabPage);
                if (editor == null)
                    throw new Exception("无法获取编辑器内容");

                var templateData = new TemplateData
                {
                    Name = tabPage.Text.Replace(" *", ""), // 移除修改标记
                    Content = editor.Text,
                    PointType = currentPointType,
                    Version = currentVersion,
                    Description = GenerateTemplateDescription(editor.Text),
                    CreatedDate = DateTime.Now,
                    Variables = ExtractTemplateVariables(editor.Text)
                };

                ExportTemplateToFile(templateData, filePath);
                UpdateStatus($"成功导出模板: {Path.GetFileName(filePath)}", false);
                
                // 显示导出详细信息
                ShowExportDetails(templateData, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"导出到文件 '{Path.GetFileName(filePath)}' 失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从文件导入模板数据
        /// </summary>
        private TemplateData? ImportTemplateFromFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".template":
                    return ImportFromTemplateFile(filePath);
                case ".json":
                    return ImportFromJsonFile(filePath);
                default:
                    // 尝试作为纯文本导入
                    return ImportFromTextFile(filePath);
            }
        }

        /// <summary>
        /// 从Template格式文件导入
        /// </summary>
        private TemplateData ImportFromTemplateFile(string filePath)
        {
            var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            var lines = content.Split('\n');
            
            var templateData = new TemplateData
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Content = content,
                PointType = PointType.AI,
                Version = TemplateVersion.Default
            };

            // 尝试从文件头部注释解析元数据
            ParseTemplateMetadata(lines, templateData);
            
            return templateData;
        }

        /// <summary>
        /// 从JSON格式文件导入
        /// </summary>
        private TemplateData ImportFromJsonFile(string filePath)
        {
            var jsonContent = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            
            try
            {
                var templateData = System.Text.Json.JsonSerializer.Deserialize<TemplateData>(jsonContent, 
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    });
                
                if (templateData == null)
                    throw new Exception("JSON文件格式无效");
                    
                return templateData;
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new Exception($"JSON格式错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 从纯文本文件导入
        /// </summary>
        private TemplateData ImportFromTextFile(string filePath)
        {
            var content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            
            return new TemplateData
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                Content = content,
                PointType = PointType.AI,
                Version = TemplateVersion.Default,
                Description = "从文本文件导入的模板"
            };
        }

        /// <summary>
        /// 导出模板数据到文件
        /// </summary>
        private void ExportTemplateToFile(TemplateData templateData, string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".template":
                    ExportToTemplateFile(templateData, filePath);
                    break;
                case ".json":
                    ExportToJsonFile(templateData, filePath);
                    break;
                default:
                    throw new Exception($"不支持的文件格式: {extension}");
            }
        }

        /// <summary>
        /// 导出为Template格式文件
        /// </summary>
        private void ExportToTemplateFile(TemplateData templateData, string filePath)
        {
            var content = new System.Text.StringBuilder();
            
            // 添加文件头部元数据注释
            content.AppendLine("{{# 模板元数据");
            content.AppendLine($"{{# 名称: {templateData.Name}");
            content.AppendLine($"{{# 点位类型: {templateData.PointType}");
            content.AppendLine($"{{# 模板版本: {templateData.Version}");
            content.AppendLine($"{{# 描述: {templateData.Description}");
            content.AppendLine($"{{# 创建时间: {templateData.CreatedDate:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"{{# 变量数量: {templateData.Variables?.Count ?? 0}");
            content.AppendLine("{{# }}");
            content.AppendLine();
            
            // 添加模板内容
            content.Append(templateData.Content);
            
            File.WriteAllText(filePath, content.ToString(), System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 导出为JSON格式文件
        /// </summary>
        private void ExportToJsonFile(TemplateData templateData, string filePath)
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(templateData, jsonOptions);
            File.WriteAllText(filePath, jsonContent, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 解析模板元数据
        /// </summary>
        private void ParseTemplateMetadata(string[] lines, TemplateData templateData)
        {
            foreach (var line in lines.Take(20)) // 只检查前20行
            {
                var trimmedLine = line.Trim();
                if (!trimmedLine.StartsWith("{{#"))
                    break;
                    
                if (trimmedLine.Contains("点位类型:") && Enum.TryParse<PointType>(
                    ExtractMetadataValue(trimmedLine), out var pointType))
                {
                    templateData.PointType = pointType;
                }
                else if (trimmedLine.Contains("模板版本:") && Enum.TryParse<TemplateVersion>(
                    ExtractMetadataValue(trimmedLine), out var version))
                {
                    templateData.Version = version;
                }
                else if (trimmedLine.Contains("描述:"))
                {
                    templateData.Description = ExtractMetadataValue(trimmedLine);
                }
            }
        }

        /// <summary>
        /// 提取元数据值
        /// </summary>
        private string ExtractMetadataValue(string line)
        {
            var colonIndex = line.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < line.Length - 1)
            {
                return line.Substring(colonIndex + 1).Trim().TrimEnd('}');
            }
            return string.Empty;
        }

        /// <summary>
        /// 生成模板描述
        /// </summary>
        private string GenerateTemplateDescription(string templateContent)
        {
            if (string.IsNullOrWhiteSpace(templateContent))
                return "空模板";
                
            var lines = templateContent.Split('\n');
            var variableCount = TemplateVariableExtractor.ExtractVariables(templateContent).Variables.Count;
            var loopCount = templateContent.Count(c => c == '{' && templateContent.Contains("for"));
            var conditionCount = templateContent.Count(c => c == '{' && templateContent.Contains("if"));
            
            return $"包含 {variableCount} 个变量，{loopCount} 个循环，{conditionCount} 个条件的模板 (共 {lines.Length} 行)";
        }

        /// <summary>
        /// 提取模板变量
        /// </summary>
        private List<string> ExtractTemplateVariables(string templateContent)
        {
            try
            {
                var result = TemplateVariableExtractor.ExtractVariables(templateContent, currentPointType);
                return result.Variables.Select(v => v.Name).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取建议的文件名
        /// </summary>
        private string GetSuggestedFileName()
        {
            var currentTab = GetCurrentEditorTab();
            if (currentTab != null)
            {
                var baseName = currentTab.Text.Replace(" *", "");
                return $"{baseName}_{currentPointType}_{currentVersion}";
            }
            return $"Template_{currentPointType}_{currentVersion}_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        /// <summary>
        /// 显示导入详细信息
        /// </summary>
        private void ShowImportDetails(TemplateData templateData)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"模板名称: {templateData.Name}");
            details.AppendLine($"点位类型: {templateData.PointType}");
            details.AppendLine($"模板版本: {templateData.Version}");
            details.AppendLine($"描述: {templateData.Description}");
            details.AppendLine($"内容长度: {templateData.Content?.Length ?? 0} 字符");
            details.AppendLine($"变量数量: {templateData.Variables?.Count ?? 0}");
            
            MessageBox.Show(details.ToString(), "导入成功", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 显示导出详细信息
        /// </summary>
        private void ShowExportDetails(TemplateData templateData, string filePath)
        {
            var details = new System.Text.StringBuilder();
            details.AppendLine($"导出路径: {filePath}");
            details.AppendLine($"文件大小: {GetFileSize(filePath)}");
            details.AppendLine($"模板名称: {templateData.Name}");
            details.AppendLine($"点位类型: {templateData.PointType}");
            details.AppendLine($"模板版本: {templateData.Version}");
            details.AppendLine($"变量数量: {templateData.Variables?.Count ?? 0}");
            
            MessageBox.Show(details.ToString(), "导出成功", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 显示导入汇总
        /// </summary>
        private void ShowImportSummary(List<ImportResult> results)
        {
            var successful = results.Count(r => r.Success);
            var failed = results.Count(r => !r.Success);
            
            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"导入完成:");
            summary.AppendLine($"  成功: {successful} 个");
            summary.AppendLine($"  失败: {failed} 个");
            summary.AppendLine();
            
            if (failed > 0)
            {
                summary.AppendLine("失败的文件:");
                foreach (var result in results.Where(r => !r.Success))
                {
                    summary.AppendLine($"  • {result.FileName}: {result.ErrorMessage}");
                }
            }
            
            MessageBox.Show(summary.ToString(), "批量导入结果", 
                MessageBoxButtons.OK, failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        /// <summary>
        /// 获取文件大小描述
        /// </summary>
        private string GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var bytes = fileInfo.Length;
                
                if (bytes < 1024)
                    return $"{bytes} B";
                else if (bytes < 1024 * 1024)
                    return $"{bytes / 1024.0:F1} KB";
                else
                    return $"{bytes / (1024.0 * 1024.0):F1} MB";
            }
            catch
            {
                return "未知";
            }
        }

        /// <summary>
        /// 更新点位类型下拉框
        /// </summary>
        private void UpdatePointTypeComboBox()
        {
            var combo = toolStrip?.Items["pointTypeCombo"] as ToolStripComboBox;
            if (combo != null)
            {
                combo.SelectedItem = currentPointType.ToString();
            }
        }

        /// <summary>
        /// 更新版本下拉框
        /// </summary>
        private void UpdateVersionComboBox()
        {
            var combo = toolStrip?.Items["versionCombo"] as ToolStripComboBox;
            if (combo != null)
            {
                combo.SelectedItem = currentVersion.ToString();
            }
        }

        #endregion

        #region 预览功能实现

        /// <summary>
        /// 生成默认样本数据
        /// </summary>
        private string GenerateDefaultSampleData()
        {
            var sampleData = new
            {
                变量名称HMI = "AI_Temperature_01",
                变量描述 = "反应器温度传感器",
                硬点通道号 = "AI001",
                量程高限 = 150.0,
                量程低限 = 0.0,
                PLC绝对地址 = "IW100",
                SHH值 = 140.0,
                SH值 = 120.0,
                SL值 = 10.0,
                SLL值 = 5.0,
                单位 = "°C",
                位号 = "TI-001",
                工艺描述 = "反应器1号温度监测点",
                报警组 = "温度报警",
                操作员站 = "操作站1"
            };

            return System.Text.Json.JsonSerializer.Serialize(sampleData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        /// <summary>
        /// 刷新预览按钮点击事件
        /// </summary>
        private void OnRefreshPreview(object? sender, EventArgs e)
        {
            RefreshPreview();
        }

        /// <summary>
        /// 自动刷新选项改变事件
        /// </summary>
        private void OnAutoRefreshChanged(object? sender, EventArgs e)
        {
            if (autoRefreshCheckBox.Checked)
            {
                // 启动自动刷新
                if (previewRefreshTimer != null)
                {
                    previewRefreshTimer.Start();
                }
                UpdatePreviewStatus("自动刷新已启用", false);
            }
            else
            {
                // 停止自动刷新
                if (previewRefreshTimer != null)
                {
                    previewRefreshTimer.Stop();
                }
                UpdatePreviewStatus("自动刷新已禁用", false);
            }
        }

        /// <summary>
        /// 样本数据改变事件
        /// </summary>
        private void OnSampleDataChanged(object? sender, EventArgs e)
        {
            if (autoRefreshCheckBox.Checked && previewRefreshTimer != null)
            {
                // 重置定时器，延迟刷新预览
                previewRefreshTimer.Stop();
                previewRefreshTimer.Start();
            }
        }



        /// <summary>
        /// 预览定时器触发事件
        /// </summary>
        private void OnPreviewTimerTick(object? sender, EventArgs e)
        {
            if (previewRefreshTimer != null)
            {
                previewRefreshTimer.Stop();
                RefreshPreview();
            }
        }

        /// <summary>
        /// 刷新预览
        /// </summary>
        private void RefreshPreview()
        {
            try
            {
                UpdatePreviewStatus("正在渲染模板...", false);

                var currentEditor = GetCurrentEditor();
                if (currentEditor == null || string.IsNullOrWhiteSpace(currentEditor.Text))
                {
                    previewResultTextBox.Text = "没有可预览的模板内容";
                    UpdatePreviewStatus("无模板内容", true);
                    return;
                }

                var templateContent = currentEditor.Text;
                var sampleDataJson = sampleDataTextBox.Text;

                // 渲染模板
                var result = RenderTemplate(templateContent, sampleDataJson);
                
                previewResultTextBox.Text = result.RenderedContent;
                
                if (result.HasErrors)
                {
                    previewResultTextBox.SelectionStart = 0;
                    previewResultTextBox.SelectionLength = 0;
                    previewResultTextBox.SelectionColor = Color.Red;
                    previewResultTextBox.AppendText($"\n\n=== 渲染错误 ===\n{result.ErrorMessage}");
                    UpdatePreviewStatus($"渲染失败: {result.ErrorMessage}", true);
                }
                else
                {
                    UpdatePreviewStatus($"渲染成功 (耗时: {result.RenderTime}ms)", false);
                }

                // 如果启用了自动刷新，显示变量分析
                if (autoRefreshCheckBox.Checked)
                {
                    ShowVariableAnalysis(templateContent);
                }
            }
            catch (Exception ex)
            {
                previewResultTextBox.Text = $"预览出错: {ex.Message}";
                UpdatePreviewStatus($"预览出错: {ex.Message}", true);
            }
        }

        /// <summary>
        /// 渲染模板
        /// </summary>
        private TemplateRenderResult RenderTemplate(string templateContent, string sampleDataJson)
        {
            var result = new TemplateRenderResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 解析样本数据JSON
                object? sampleData = null;
                if (!string.IsNullOrWhiteSpace(sampleDataJson))
                {
                    try
                    {
                        sampleData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(sampleDataJson);
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        result.HasErrors = true;
                        result.ErrorMessage = $"样本数据JSON格式错误: {ex.Message}";
                        return result;
                    }
                }

                // 使用Scriban模板引擎渲染
                var template = Scriban.Template.Parse(templateContent);
                if (template.HasErrors)
                {
                    result.HasErrors = true;
                    result.ErrorMessage = string.Join("\n", template.Messages.Select(m => $"第{m.Span.Start.Line}行: {m.Message}"));
                    return result;
                }

                var scriptObject = new Scriban.Runtime.ScriptObject();
                if (sampleData is Dictionary<string, object> dataDict)
                {
                    foreach (var kvp in dataDict)
                    {
                        // 处理JSON数字类型转换
                        var value = kvp.Value;
                        if (value is System.Text.Json.JsonElement jsonElement)
                        {
                            value = ConvertJsonElement(jsonElement);
                        }
                        scriptObject[kvp.Key] = value;
                    }
                }

                var context = new Scriban.TemplateContext();
                context.PushGlobal(scriptObject);

                result.RenderedContent = template.Render(context);
                result.HasErrors = false;
            }
            catch (Exception ex)
            {
                result.HasErrors = true;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.RenderTime = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 转换JsonElement到对应的.NET类型
        /// </summary>
        private object? ConvertJsonElement(System.Text.Json.JsonElement element)
        {
            return element.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => element.GetString(),
                System.Text.Json.JsonValueKind.Number => element.TryGetDouble(out var d) ? d : element.GetDecimal(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Null => null,
                System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
                System.Text.Json.JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                _ => element.ToString()
            };
        }

        /// <summary>
        /// 显示变量分析
        /// </summary>
        private void ShowVariableAnalysis(string templateContent)
        {
            try
            {
                var extractionResult = TemplateVariableExtractor.ExtractVariables(templateContent, currentPointType);
                
                // 更新预览状态，显示变量统计
                var statsText = $"变量: {extractionResult.Variables.Count}, 函数: {extractionResult.FunctionCalls.Count}, 复杂度: {extractionResult.Statistics.ComplexityScore}";
                UpdatePreviewStatus($"渲染成功 - {statsText}", false);
                
                // 如果有警告，在结果中显示
                if (extractionResult.Warnings.Any())
                {
                    previewResultTextBox.SelectionStart = previewResultTextBox.Text.Length;
                    previewResultTextBox.SelectionLength = 0;
                    previewResultTextBox.SelectionColor = Color.Orange;
                    previewResultTextBox.AppendText($"\n\n=== 模板分析警告 ===\n{string.Join("\n", extractionResult.Warnings)}");
                }
            }
            catch (Exception ex)
            {
                // 变量分析失败不影响预览，只记录日志
                System.Diagnostics.Debug.WriteLine($"变量分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新预览状态
        /// </summary>
        private void UpdatePreviewStatus(string message, bool isError)
        {
            if (previewStatusLabel != null)
            {
                previewStatusLabel.Text = message;
                previewStatusLabel.ForeColor = isError ? Color.Red : Color.DarkGreen;
            }
        }

        /// <summary>
        /// 切换预览面板显示状态
        /// </summary>
        private void OnTogglePreviewPanel(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && editorSplitContainer != null)
            {
                if (menuItem.Checked)
                {
                    // 隐藏预览面板
                    editorSplitContainer.Panel2Collapsed = true;
                    menuItem.Checked = false;
                    UpdateStatus("预览面板已隐藏", false);
                }
                else
                {
                    // 显示预览面板
                    editorSplitContainer.Panel2Collapsed = false;
                    menuItem.Checked = true;
                    UpdateStatus("预览面板已显示", false);
                    
                    // 立即刷新预览
                    RefreshPreview();
                }
            }
        }


        /// <summary>
        /// 打开模板库管理
        /// </summary>
        private void OnOpenTemplateLibrary(object? sender, EventArgs e)
        {
            try
            {
                var libraryForm = new TemplateLibraryForm();
                libraryForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开模板库失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 添加当前模板到收藏夹
        /// </summary>
        public void AddCurrentTemplateToFavorites()
        {
            try
            {
                var currentEditor = GetCurrentEditor();
                var currentTab = GetCurrentEditorTab();
                
                if (currentEditor == null || currentTab == null || string.IsNullOrWhiteSpace(currentEditor.Text))
                {
                    MessageBox.Show("没有可收藏的模板内容", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 创建模板信息
                var templateInfo = new TemplateInfo
                {
                    Name = currentTab.Text.Replace(" *", ""),
                    FilePath = currentFilePath,
                    Version = currentVersion,
                    PointType = currentPointType,
                    Description = GenerateTemplateDescription(currentEditor.Text),
                    Author = Environment.UserName,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                // 提取模板变量作为必需字段
                try
                {
                    var extractionResult = TemplateVariableExtractor.ExtractVariables(currentEditor.Text, currentPointType);
                    templateInfo.RequiredFields = extractionResult.Variables.Select(v => v.Name).ToList();
                }
                catch
                {
                    // 变量提取失败不影响收藏功能
                }

                var favoriteId = TemplateLibraryManager.AddToFavorites(templateInfo);
                
                MessageBox.Show($"模板 '{templateInfo.Name}' 已添加到收藏夹", "收藏成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                UpdateStatus($"模板已添加到收藏夹 (ID: {favoriteId})", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加收藏失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// 模板渲染结果
        /// </summary>
        private class TemplateRenderResult
        {
            public string RenderedContent { get; set; } = string.Empty;
            public bool HasErrors { get; set; } = false;
            public string ErrorMessage { get; set; } = string.Empty;
            public long RenderTime { get; set; } = 0;
        }

        #endregion

        #region 数据模型

        /// <summary>
        /// 模板数据
        /// </summary>
        public class TemplateData
        {
            public string Name { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public PointType PointType { get; set; } = PointType.AI;
            public TemplateVersion Version { get; set; } = TemplateVersion.Default;
            public string Description { get; set; } = string.Empty;
            public DateTime CreatedDate { get; set; } = DateTime.Now;
            public List<string>? Variables { get; set; }
        }

        /// <summary>
        /// 获取当前编辑器标签页
        /// </summary>
        private TabPage? GetCurrentEditorTab()
        {
            if (editorTabControl?.SelectedTab != null)
            {
                return editorTabControl.SelectedTab;
            }
            return null;
        }

        /// <summary>
        /// 创建新的编辑器标签页
        /// </summary>
        private TabPage CreateNewEditorTab(string templateName)
        {
            var tabPage = new TabPage($"模板: {templateName}")
            {
                Tag = templateName
            };

            // 创建编辑器控件
            var editor = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(253, 253, 253),
                ForeColor = Color.Black
            };

            tabPage.Controls.Add(editor);

            if (editorTabControl != null)
            {
                editorTabControl.TabPages.Add(tabPage);
                editorTabControl.SelectedTab = tabPage;
            }

            return tabPage;
        }

        /// <summary>
        /// 从标签页获取编辑器控件
        /// </summary>
        private RichTextBox? GetEditorFromTab(TabPage? tabPage)
        {
            if (tabPage?.Controls.Count > 0 && tabPage.Controls[0] is RichTextBox editor)
            {
                return editor;
            }
            return null;
        }

        /// <summary>
        /// 更新窗体标题
        /// </summary>
        private void UpdateTitle()
        {
            var baseTitle = "模板编辑器";
            var currentTab = GetCurrentEditorTab();
            
            if (currentTab != null)
            {
                var templateName = currentTab.Tag?.ToString() ?? "未命名";
                var modifiedMark = isContentChanged ? "*" : "";
                this.Text = $"{baseTitle} - {templateName}{modifiedMark}";
            }
            else
            {
                this.Text = baseTitle;
            }
        }

        /// <summary>
        /// 导入结果
        /// </summary>
        public class ImportResult
        {
            public string FileName { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }

        /// <summary>
        /// 进度窗体
        /// </summary>
        public class ProgressForm : Form
        {
            private ProgressBar progressBar;
            private Label statusLabel;
            private Button cancelButton;
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool IsCancelled { get; set; }

            public ProgressForm(string title)
            {
                Text = title;
                Size = new Size(400, 120);
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                progressBar = new ProgressBar
                {
                    Location = new Point(20, 20),
                    Size = new Size(340, 23),
                    Style = ProgressBarStyle.Continuous
                };

                statusLabel = new Label
                {
                    Location = new Point(20, 50),
                    Size = new Size(240, 20),
                    Text = "准备中..."
                };

                cancelButton = new Button
                {
                    Location = new Point(285, 47),
                    Size = new Size(75, 25),
                    Text = "取消",
                    UseVisualStyleBackColor = true
                };
                cancelButton.Click += (s, e) => IsCancelled = true;

                Controls.AddRange(new Control[] { progressBar, statusLabel, cancelButton });
            }

            public void SetMaximum(int maximum)
            {
                progressBar.Maximum = maximum;
            }

            public void SetProgress(int value, string status)
            {
                progressBar.Value = Math.Min(value, progressBar.Maximum);
                statusLabel.Text = status;
                Application.DoEvents();
            }
        }

        #endregion
    }
}