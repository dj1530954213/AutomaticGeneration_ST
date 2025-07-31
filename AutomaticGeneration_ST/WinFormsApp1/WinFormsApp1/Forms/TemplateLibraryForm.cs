using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WinFormsApp1.Config;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Forms
{
    /// <summary>
    /// 模板库管理窗体 - 用户自定义模板收藏和分类管理
    /// </summary>
    public partial class TemplateLibraryForm : Form
    {
        #region 私有字段

        private SplitContainer mainSplitContainer;
        private SplitContainer leftSplitContainer;
        private TreeView categoryTreeView;
        private ListView templateListView;
        private Panel detailPanel;
        private Panel toolbarPanel;
        private Panel searchPanel;
        private Panel statusPanel;
        
        // 工具栏控件
        private ToolStrip toolStrip;
        private ToolStripButton addFavoriteButton;
        private ToolStripButton removeFavoriteButton;
        private ToolStripButton editButton;
        private ToolStripButton importButton;
        private ToolStripButton exportButton;
        private ToolStripComboBox viewModeComboBox;
        private ToolStripComboBox sortOrderComboBox;
        
        // 搜索控件
        private TextBox searchTextBox;
        private ComboBox pointTypeFilterComboBox;
        private ComboBox ratingFilterComboBox;
        private Button clearSearchButton;
        
        // 详情面板控件
        private Label templateNameLabel;
        private Label templateDescriptionLabel;
        private Label templateInfoLabel;
        private RichTextBox templatePreviewTextBox;
        private Panel ratingPanel;
        private TextBox notesTextBox;
        private ListBox tagsListBox;
        
        // 状态栏控件
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel countLabel;
        private ToolStripProgressBar progressBar;
        
        // 数据相关
        private List<TemplateLibraryManager.TemplateFavorite> currentFavorites = new List<TemplateLibraryManager.TemplateFavorite>();
        private TemplateLibraryManager.TemplateFavorite? selectedFavorite;
        private string selectedCategoryId = string.Empty;
        private bool isLoading = false;
        
        // UI相关
        private ImageList treeImageList;
        private ImageList listImageList;
        private TemplateLibraryManager.ViewMode currentViewMode = TemplateLibraryManager.ViewMode.List;
        private TemplateLibraryManager.SortOrder currentSortOrder = TemplateLibraryManager.SortOrder.LastUsed;

        #endregion

        #region 构造函数和初始化

        public TemplateLibraryForm()
        {
            InitializeComponent();
            InitializeImageLists();
            CreateControls();
            SetupLayout();
            BindEvents();
            LoadLibraryData();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            Text = "模板库管理";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            MaximizeBox = true;
            MinimizeBox = true;
            MinimumSize = new Size(800, 600);
        }

        private void InitializeImageLists()
        {
            // 创建树视图图标列表
            treeImageList = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };
            
            // 创建列表视图图标列表
            listImageList = new ImageList
            {
                ImageSize = new Size(32, 32),
                ColorDepth = ColorDepth.Depth32Bit
            };
            
            // 这里可以添加具体的图标
            // 暂时使用系统图标或简单的颜色块
        }

        private void CreateControls()
        {
            CreateMainLayout();
            CreateToolbar();
            CreateSearchPanel();
            CreateCategoryTreeView();
            CreateTemplateListView();
            CreateDetailPanel();
            CreateStatusBar();
        }

        private void CreateMainLayout()
        {
            // 主分割容器（上下布局）
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel1MinSize = 50,
                Panel2MinSize = 100,
                IsSplitterFixed = true
            };

            // 左侧分割容器（左右布局）
            leftSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                Panel1MinSize = 150,
                Panel2MinSize = 200
            };
        }

        private void CreateToolbar()
        {
            toolbarPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 50,
                BackColor = SystemColors.Control
            };

            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                Font = new Font("微软雅黑", 9F),
                ImageScalingSize = new Size(24, 24)
            };

            // 创建工具栏按钮
            addFavoriteButton = new ToolStripButton
            {
                Text = "⭐ 添加收藏",
                ToolTipText = "添加模板到收藏夹",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            removeFavoriteButton = new ToolStripButton
            {
                Text = "🗑️ 移除",
                ToolTipText = "从收藏夹移除选中的模板",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false
            };

            editButton = new ToolStripButton
            {
                Text = "✏️ 编辑",
                ToolTipText = "编辑选中模板的信息",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false
            };

            importButton = new ToolStripButton
            {
                Text = "📥 导入",
                ToolTipText = "导入模板库",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            exportButton = new ToolStripButton
            {
                Text = "📤 导出",
                ToolTipText = "导出模板库",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
            };

            // 视图模式选择
            viewModeComboBox = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(80, 25),
                ToolTipText = "选择显示模式"
            };
            viewModeComboBox.Items.AddRange(new[] { "列表", "网格", "卡片" });
            viewModeComboBox.SelectedIndex = 0;

            // 排序方式选择
            sortOrderComboBox = new ToolStripComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(100, 25),
                ToolTipText = "选择排序方式"
            };
            sortOrderComboBox.Items.AddRange(new[] { "最近使用", "名称", "创建时间", "修改时间", "评分", "使用次数" });
            sortOrderComboBox.SelectedIndex = 0;

            toolStrip.Items.AddRange(new ToolStripItem[]
            {
                addFavoriteButton,
                removeFavoriteButton,
                editButton,
                new ToolStripSeparator(),
                importButton,
                exportButton,
                new ToolStripSeparator(),
                new ToolStripLabel("视图:"),
                viewModeComboBox,
                new ToolStripLabel("排序:"),
                sortOrderComboBox
            });

            toolbarPanel.Controls.Add(toolStrip);
        }

        private void CreateSearchPanel()
        {
            searchPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = SystemColors.Control
            };

            var searchLabel = new Label
            {
                Text = "搜索:",
                Location = new Point(10, 12),
                Size = new Size(40, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            searchTextBox = new TextBox
            {
                Location = new Point(55, 10),
                Size = new Size(200, 23),
                PlaceholderText = "输入关键词搜索模板..."
            };

            var pointTypeLabel = new Label
            {
                Text = "类型:",
                Location = new Point(270, 12),
                Size = new Size(40, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pointTypeFilterComboBox = new ComboBox
            {
                Location = new Point(315, 10),
                Size = new Size(80, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            pointTypeFilterComboBox.Items.AddRange(new[] { "全部", "AI", "AO", "DI", "DO" });
            pointTypeFilterComboBox.SelectedIndex = 0;

            var ratingLabel = new Label
            {
                Text = "评分:",
                Location = new Point(410, 12),
                Size = new Size(40, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            ratingFilterComboBox = new ComboBox
            {
                Location = new Point(455, 10),
                Size = new Size(80, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            ratingFilterComboBox.Items.AddRange(new[] { "全部", "⭐⭐⭐⭐⭐", "⭐⭐⭐⭐", "⭐⭐⭐", "⭐⭐", "⭐" });
            ratingFilterComboBox.SelectedIndex = 0;

            clearSearchButton = new Button
            {
                Text = "清空",
                Location = new Point(550, 9),
                Size = new Size(50, 25),
                UseVisualStyleBackColor = true
            };

            searchPanel.Controls.AddRange(new Control[]
            {
                searchLabel, searchTextBox, pointTypeLabel, pointTypeFilterComboBox,
                ratingLabel, ratingFilterComboBox, clearSearchButton
            });

            toolbarPanel.Controls.Add(searchPanel);
        }

        private void CreateCategoryTreeView()
        {
            categoryTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F),
                ShowLines = true,
                ShowPlusMinus = true,
                FullRowSelect = true,
                HideSelection = false,
                ImageList = treeImageList
            };
        }

        private void CreateTemplateListView()
        {
            templateListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("微软雅黑", 9F),
                LargeImageList = listImageList,
                SmallImageList = listImageList
            };

            // 添加列
            templateListView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "名称", Width = 200 },
                new ColumnHeader { Text = "类型", Width = 60 },
                new ColumnHeader { Text = "版本", Width = 80 },
                new ColumnHeader { Text = "评分", Width = 80 },
                new ColumnHeader { Text = "使用次数", Width = 80 },
                new ColumnHeader { Text = "最后使用", Width = 120 },
                new ColumnHeader { Text = "描述", Width = 300 }
            });
        }

        private void CreateDetailPanel()
        {
            detailPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 350,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var titleLabel = new Label
            {
                Text = "模板详情",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 10, 0, 0),
                BackColor = SystemColors.Control
            };

            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            // 模板名称
            templateNameLabel = new Label
            {
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 10),
                MaximumSize = new Size(320, 0),
                Text = "选择模板查看详情"
            };

            // 模板描述
            templateDescriptionLabel = new Label
            {
                AutoSize = true,
                Location = new Point(0, 40),
                MaximumSize = new Size(320, 0),
                ForeColor = Color.Gray,
                Text = ""
            };

            // 模板信息
            templateInfoLabel = new Label
            {
                AutoSize = true,
                Location = new Point(0, 70),
                MaximumSize = new Size(320, 0),
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.DarkBlue,
                Text = ""
            };

            // 评分面板
            ratingPanel = new Panel
            {
                Location = new Point(0, 100),
                Size = new Size(320, 30),
                BackColor = Color.Transparent
            };

            // 预览文本框
            templatePreviewTextBox = new RichTextBox
            {
                Location = new Point(0, 140),
                Size = new Size(320, 200),
                ReadOnly = true,
                Font = new Font("Consolas", 9F),
                BackColor = Color.FromArgb(248, 248, 248),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 备注文本框
            var notesLabel = new Label
            {
                Text = "备注:",
                Location = new Point(0, 350),
                Size = new Size(50, 20),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };

            notesTextBox = new TextBox
            {
                Location = new Point(0, 375),
                Size = new Size(320, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 标签列表
            var tagsLabel = new Label
            {
                Text = "标签:",
                Location = new Point(0, 445),
                Size = new Size(50, 20),
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };

            tagsListBox = new ListBox
            {
                Location = new Point(0, 470),
                Size = new Size(320, 80),
                BorderStyle = BorderStyle.FixedSingle
            };

            contentPanel.Controls.AddRange(new Control[]
            {
                templateNameLabel, templateDescriptionLabel, templateInfoLabel,
                ratingPanel, templatePreviewTextBox, notesLabel, notesTextBox,
                tagsLabel, tagsListBox
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
                Text = "模板: 0",
                AutoSize = true
            };

            progressBar = new ToolStripProgressBar
            {
                Size = new Size(100, 16),
                Visible = false
            };

            statusStrip.Items.AddRange(new ToolStripItem[]
            {
                statusLabel,
                new ToolStripStatusLabel("|"),
                countLabel,
                progressBar
            });

            statusPanel.Controls.Add(statusStrip);
        }

        private void SetupLayout()
        {
            // 设置控件层次结构
            mainSplitContainer.Panel1.Controls.Add(toolbarPanel);
            
            leftSplitContainer.Panel1.Controls.Add(categoryTreeView);
            leftSplitContainer.Panel2.Controls.Add(templateListView);
            leftSplitContainer.Panel2.Controls.Add(detailPanel);
            
            mainSplitContainer.Panel2.Controls.Add(leftSplitContainer);
            
            Controls.Add(mainSplitContainer);
            Controls.Add(statusPanel);
        }

        private void BindEvents()
        {
            // 工具栏事件
            addFavoriteButton.Click += OnAddFavorite;
            removeFavoriteButton.Click += OnRemoveFavorite;
            editButton.Click += OnEditTemplate;
            importButton.Click += OnImportLibrary;
            exportButton.Click += OnExportLibrary;
            viewModeComboBox.SelectedIndexChanged += OnViewModeChanged;
            sortOrderComboBox.SelectedIndexChanged += OnSortOrderChanged;

            // 搜索面板事件
            searchTextBox.TextChanged += OnSearchTextChanged;
            pointTypeFilterComboBox.SelectedIndexChanged += OnFilterChanged;
            ratingFilterComboBox.SelectedIndexChanged += OnFilterChanged;
            clearSearchButton.Click += OnClearSearch;

            // 列表和树视图事件
            categoryTreeView.AfterSelect += OnCategorySelected;
            templateListView.SelectedIndexChanged += OnTemplateSelected;
            templateListView.DoubleClick += OnTemplateDoubleClick;

            // 详情面板事件
            notesTextBox.TextChanged += OnNotesChanged;

            // 窗体事件
            FormClosing += OnFormClosing;
            Load += OnFormLoad;
        }

        #endregion

        #region 数据加载和显示

        private void LoadLibraryData()
        {
            try
            {
                isLoading = true;
                UpdateStatus("正在加载模板库...", true);

                // 初始化模板库
                TemplateLibraryManager.Initialize();

                // 加载分类树
                LoadCategoryTree();

                // 加载模板列表
                LoadTemplates();

                UpdateStatus("模板库加载完成", false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载模板库失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("加载失败", false);
            }
            finally
            {
                isLoading = false;
            }
        }

        private void LoadCategoryTree()
        {
            try
            {
                categoryTreeView.BeginUpdate();
                categoryTreeView.Nodes.Clear();

                var categories = TemplateLibraryManager.GetCategoryTree();

                foreach (var category in categories)
                {
                    var node = CreateCategoryNode(category);
                    categoryTreeView.Nodes.Add(node);
                }

                // 添加特殊节点
                var allNode = new TreeNode("📚 全部模板")
                {
                    Tag = "ALL",
                    NodeFont = new Font(categoryTreeView.Font, FontStyle.Bold)
                };
                categoryTreeView.Nodes.Insert(0, allNode);

                var recentNode = new TreeNode("🕒 最近使用")
                {
                    Tag = "RECENT",
                    NodeFont = new Font(categoryTreeView.Font, FontStyle.Bold)
                };
                categoryTreeView.Nodes.Insert(1, recentNode);

                var topRatedNode = new TreeNode("⭐ 最高评分")
                {
                    Tag = "TOP_RATED",
                    NodeFont = new Font(categoryTreeView.Font, FontStyle.Bold)
                };
                categoryTreeView.Nodes.Insert(2, topRatedNode);

                categoryTreeView.ExpandAll();

                // 默认选择"全部模板"
                if (categoryTreeView.Nodes.Count > 0)
                {
                    categoryTreeView.SelectedNode = categoryTreeView.Nodes[0];
                }
            }
            finally
            {
                categoryTreeView.EndUpdate();
            }
        }

        private TreeNode CreateCategoryNode(TemplateLibraryManager.TemplateCategory category)
        {
            var node = new TreeNode($"{category.Icon} {category.Name}")
            {
                Tag = category.Id,
                ToolTipText = category.Description
            };

            foreach (var child in category.Children)
            {
                node.Nodes.Add(CreateCategoryNode(child));
            }

            return node;
        }

        private void LoadTemplates(string categoryId = "")
        {
            try
            {
                templateListView.BeginUpdate();
                templateListView.Items.Clear();

                List<TemplateLibraryManager.TemplateFavorite> favorites;

                if (string.IsNullOrEmpty(categoryId) || categoryId == "ALL")
                {
                    favorites = TemplateLibraryManager.GetAllFavorites(currentSortOrder);
                }
                else if (categoryId == "RECENT")
                {
                    favorites = TemplateLibraryManager.GetRecentlyUsedFavorites(20);
                }
                else if (categoryId == "TOP_RATED")
                {
                    favorites = TemplateLibraryManager.GetTopRatedFavorites(20);
                }
                else
                {
                    favorites = TemplateLibraryManager.GetFavoritesByCategory(categoryId, currentSortOrder);
                }

                // 应用搜索过滤
                favorites = ApplySearchFilter(favorites);

                currentFavorites = favorites;

                foreach (var favorite in favorites)
                {
                    var item = CreateTemplateListItem(favorite);
                    templateListView.Items.Add(item);
                }

                UpdateCountLabel();
            }
            finally
            {
                templateListView.EndUpdate();
            }
        }

        private ListViewItem CreateTemplateListItem(TemplateLibraryManager.TemplateFavorite favorite)
        {
            var template = favorite.Template;
            var item = new ListViewItem(template.Name)
            {
                Tag = favorite,
                ImageIndex = GetTemplateImageIndex(template.PointType)
            };

            // 添加子项
            item.SubItems.AddRange(new[]
            {
                template.PointType.ToString(),
                template.Version.ToString(),
                GetRatingText(favorite.Rating),
                favorite.UsageCount.ToString(),
                favorite.LastUsedDate.ToString("yyyy-MM-dd HH:mm"),
                template.Description
            });

            return item;
        }

        private List<TemplateLibraryManager.TemplateFavorite> ApplySearchFilter(List<TemplateLibraryManager.TemplateFavorite> favorites)
        {
            var keyword = searchTextBox.Text.Trim();
            var pointTypeFilter = pointTypeFilterComboBox.SelectedIndex;
            var ratingFilter = ratingFilterComboBox.SelectedIndex;

            if (string.IsNullOrEmpty(keyword) && pointTypeFilter == 0 && ratingFilter == 0)
            {
                return favorites;
            }

            return favorites.Where(f =>
            {
                // 关键词过滤
                if (!string.IsNullOrEmpty(keyword))
                {
                    var lowerKeyword = keyword.ToLower();
                    if (!f.Template.Name.ToLower().Contains(lowerKeyword) &&
                        !f.Template.Description.ToLower().Contains(lowerKeyword) &&
                        !f.Notes.ToLower().Contains(lowerKeyword) &&
                        !f.Tags.Any(t => t.ToLower().Contains(lowerKeyword)))
                    {
                        return false;
                    }
                }

                // 点位类型过滤
                if (pointTypeFilter > 0)
                {
                    var expectedType = (PointType)(pointTypeFilter - 1);
                    if (f.Template.PointType != expectedType)
                    {
                        return false;
                    }
                }

                // 评分过滤
                if (ratingFilter > 0)
                {
                    var minRating = 6 - ratingFilter;
                    if (f.Rating < minRating)
                    {
                        return false;
                    }
                }

                return true;
            }).ToList();
        }

        private void UpdateTemplateDetails(TemplateLibraryManager.TemplateFavorite? favorite)
        {
            selectedFavorite = favorite;

            if (favorite == null)
            {
                templateNameLabel.Text = "选择模板查看详情";
                templateDescriptionLabel.Text = "";
                templateInfoLabel.Text = "";
                templatePreviewTextBox.Text = "";
                notesTextBox.Text = "";
                tagsListBox.Items.Clear();
                ClearRatingPanel();
                
                removeFavoriteButton.Enabled = false;
                editButton.Enabled = false;
                return;
            }

            var template = favorite.Template;

            templateNameLabel.Text = template.Name;
            templateDescriptionLabel.Text = template.Description;
            templateInfoLabel.Text = $"类型: {template.PointType} | 版本: {template.Version} | 作者: {template.Author}\n" +
                                   $"创建: {template.CreatedDate:yyyy-MM-dd} | 修改: {template.ModifiedDate:yyyy-MM-dd}\n" +
                                   $"使用次数: {favorite.UsageCount} | 最后使用: {favorite.LastUsedDate:yyyy-MM-dd HH:mm}";

            // 加载模板内容预览
            LoadTemplatePreview(template);

            notesTextBox.Text = favorite.Notes;

            // 更新标签列表
            tagsListBox.Items.Clear();
            foreach (var tag in favorite.Tags)
            {
                tagsListBox.Items.Add(tag);
            }

            // 更新评分显示
            UpdateRatingPanel(favorite.Rating);

            removeFavoriteButton.Enabled = true;
            editButton.Enabled = true;
        }

        private void LoadTemplatePreview(TemplateInfo template)
        {
            try
            {
                if (File.Exists(template.FilePath))
                {
                    var content = File.ReadAllText(template.FilePath);
                    templatePreviewTextBox.Text = content.Length > 1000 
                        ? content.Substring(0, 1000) + "\n\n... (内容过长，已截断显示)" 
                        : content;
                }
                else
                {
                    templatePreviewTextBox.Text = "模板文件不存在";
                }
            }
            catch (Exception ex)
            {
                templatePreviewTextBox.Text = $"加载模板预览失败: {ex.Message}";
            }
        }

        #endregion

        #region 事件处理

        private void OnFormLoad(object? sender, EventArgs e)
        {
            // 窗体加载完成后的初始化工作
            try
            {
                // 确保窗体已完全初始化后再设置分割器距离
                if (mainSplitContainer != null && mainSplitContainer.Height > 0)
                {
                    var maxDistance = mainSplitContainer.Height - mainSplitContainer.Panel2MinSize - mainSplitContainer.SplitterWidth;
                    var targetDistance = Math.Min(maxDistance, Math.Max(mainSplitContainer.Panel1MinSize, 80));
                    mainSplitContainer.SplitterDistance = targetDistance;
                }

                if (leftSplitContainer != null && leftSplitContainer.Width > 0)
                {
                    var maxDistance = leftSplitContainer.Width - leftSplitContainer.Panel2MinSize - leftSplitContainer.SplitterWidth;
                    var targetDistance = Math.Min(maxDistance, Math.Max(leftSplitContainer.Panel1MinSize, leftSplitContainer.Width / 4));
                    leftSplitContainer.SplitterDistance = targetDistance;
                }
            }
            catch (Exception ex)
            {
                // 静默处理分割器设置错误
                System.Diagnostics.Debug.WriteLine($"设置分割器距离时出错: {ex.Message}");
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                // 保存配置
                TemplateLibraryManager.SaveLibraryConfig();
            }
            catch (Exception ex)
            {
                var result = MessageBox.Show($"保存模板库配置失败: {ex.Message}\n\n是否强制关闭窗体？", 
                    "保存失败", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void OnCategorySelected(object? sender, TreeViewEventArgs e)
        {
            if (isLoading || e.Node?.Tag == null)
                return;

            selectedCategoryId = e.Node.Tag.ToString() ?? "";
            LoadTemplates(selectedCategoryId);
        }

        private void OnTemplateSelected(object? sender, EventArgs e)
        {
            if (templateListView.SelectedItems.Count > 0)
            {
                var selectedItem = templateListView.SelectedItems[0];
                var favorite = selectedItem.Tag as TemplateLibraryManager.TemplateFavorite;
                UpdateTemplateDetails(favorite);
            }
            else
            {
                UpdateTemplateDetails(null);
            }
        }

        private void OnTemplateDoubleClick(object? sender, EventArgs e)
        {
            if (selectedFavorite != null)
            {
                // 记录使用并关闭窗体
                TemplateLibraryManager.RecordTemplateUsage(selectedFavorite.Id);
                
                // 可以在这里添加打开模板编辑器的逻辑
                var editorForm = new TemplateEditorForm();
                editorForm.Show();
                
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void OnAddFavorite(object? sender, EventArgs e)
        {
            try
            {
                // 这里应该打开一个对话框让用户选择要添加的模板
                // 暂时显示提示信息
                MessageBox.Show("请从模板编辑器中添加模板到收藏夹", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加收藏失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnRemoveFavorite(object? sender, EventArgs e)
        {
            if (selectedFavorite == null)
                return;

            try
            {
                var result = MessageBox.Show($"确定要从收藏夹移除模板 '{selectedFavorite.Template.Name}' 吗？",
                    "确认移除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    TemplateLibraryManager.RemoveFromFavorites(selectedFavorite.Id);
                    LoadTemplates(selectedCategoryId);
                    UpdateTemplateDetails(null);
                    UpdateStatus("模板已从收藏夹移除", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移除收藏失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnEditTemplate(object? sender, EventArgs e)
        {
            if (selectedFavorite == null)
                return;

            try
            {
                // 打开编辑对话框
                using var editForm = new TemplateEditDialog(selectedFavorite);
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    LoadTemplates(selectedCategoryId);
                    UpdateStatus("模板信息已更新", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"编辑模板失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnImportLibrary(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "导入模板库",
                    Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    FilterIndex = 1
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var result = MessageBox.Show("导入模式:\n\n是 - 合并模式（保留现有数据）\n否 - 替换模式（完全替换）",
                        "选择导入模式", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (result != DialogResult.Cancel)
                    {
                        var mergeMode = result == DialogResult.Yes;
                        TemplateLibraryManager.ImportLibrary(dialog.FileName, mergeMode);
                        
                        LoadLibraryData();
                        UpdateStatus($"模板库导入完成 ({(mergeMode ? "合并" : "替换")}模式)", false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入模板库失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnExportLibrary(object? sender, EventArgs e)
        {
            try
            {
                using var dialog = new SaveFileDialog
                {
                    Title = "导出模板库",
                    Filter = "JSON文件 (*.json)|*.json",
                    FilterIndex = 1,
                    FileName = $"template_library_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var includeSystemCategories = MessageBox.Show("是否包含系统分类？", "导出选项",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

                    TemplateLibraryManager.ExportLibrary(dialog.FileName, includeSystemCategories);
                    UpdateStatus($"模板库已导出到: {dialog.FileName}", false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出模板库失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnViewModeChanged(object? sender, EventArgs e)
        {
            if (viewModeComboBox.SelectedIndex >= 0)
            {
                currentViewMode = (TemplateLibraryManager.ViewMode)viewModeComboBox.SelectedIndex;
                ApplyViewMode();
            }
        }

        private void OnSortOrderChanged(object? sender, EventArgs e)
        {
            if (sortOrderComboBox.SelectedIndex >= 0)
            {
                currentSortOrder = (TemplateLibraryManager.SortOrder)sortOrderComboBox.SelectedIndex;
                LoadTemplates(selectedCategoryId);
            }
        }

        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            // 延迟搜索，避免频繁刷新
            if (searchTextBox.Text.Length == 0 || searchTextBox.Text.Length > 2)
            {
                LoadTemplates(selectedCategoryId);
            }
        }

        private void OnFilterChanged(object? sender, EventArgs e)
        {
            LoadTemplates(selectedCategoryId);
        }

        private void OnClearSearch(object? sender, EventArgs e)
        {
            searchTextBox.Clear();
            pointTypeFilterComboBox.SelectedIndex = 0;
            ratingFilterComboBox.SelectedIndex = 0;
            LoadTemplates(selectedCategoryId);
        }

        private void OnNotesChanged(object? sender, EventArgs e)
        {
            if (selectedFavorite != null && !isLoading)
            {
                // 延迟保存，避免频繁写入
                System.Windows.Forms.Timer? saveTimer = null;
                saveTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                saveTimer.Tick += (s, args) =>
                {
                    try
                    {
                        TemplateLibraryManager.UpdateFavorite(selectedFavorite.Id, notes: notesTextBox.Text);
                        saveTimer?.Stop();
                        saveTimer?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"保存备注失败: {ex.Message}");
                    }
                };
                saveTimer.Start();
            }
        }

        #endregion

        #region UI辅助方法

        private void ApplyViewMode()
        {
            switch (currentViewMode)
            {
                case TemplateLibraryManager.ViewMode.List:
                    templateListView.View = View.Details;
                    break;
                case TemplateLibraryManager.ViewMode.Grid:
                    templateListView.View = View.LargeIcon;
                    break;
                case TemplateLibraryManager.ViewMode.Card:
                    templateListView.View = View.Tile;
                    break;
            }
        }

        private void UpdateRatingPanel(int rating)
        {
            ratingPanel.Controls.Clear();

            for (int i = 1; i <= 5; i++)
            {
                var star = new Label
                {
                    Text = i <= rating ? "⭐" : "☆",
                    Location = new Point((i - 1) * 25, 5),
                    Size = new Size(20, 20),
                    Font = new Font("Segoe UI Emoji", 12F),
                    Cursor = Cursors.Hand,
                    Tag = i
                };

                star.Click += OnStarClick;
                ratingPanel.Controls.Add(star);
            }
        }

        private void ClearRatingPanel()
        {
            ratingPanel.Controls.Clear();
        }

        private void OnStarClick(object? sender, EventArgs e)
        {
            if (selectedFavorite != null && sender is Label star && star.Tag is int rating)
            {
                try
                {
                    TemplateLibraryManager.UpdateFavorite(selectedFavorite.Id, rating: rating);
                    selectedFavorite.Rating = rating;
                    UpdateRatingPanel(rating);
                    
                    // 更新列表显示
                    if (templateListView.SelectedItems.Count > 0)
                    {
                        templateListView.SelectedItems[0].SubItems[3].Text = GetRatingText(rating);
                    }
                    
                    UpdateStatus($"评分已更新为 {rating} 星", false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"更新评分失败: {ex.Message}", "错误",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateStatus(string message, bool showProgress)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
            
            if (progressBar != null)
            {
                progressBar.Visible = showProgress;
            }
        }

        private void UpdateCountLabel()
        {
            if (countLabel != null)
            {
                countLabel.Text = $"模板: {currentFavorites.Count}";
            }
        }

        private string GetRatingText(int rating)
        {
            return rating > 0 ? new string('⭐', rating) : "未评分";
        }

        private int GetTemplateImageIndex(PointType pointType)
        {
            // 返回对应点位类型的图标索引
            return (int)pointType;
        }

        private void ApplyTheme()
        {
            try
            {
                // 应用主题色彩
                switch (ThemeManager.CurrentTheme)
                {
                    case ThemeType.Light:
                        BackColor = ThemeManager.LightTheme.BackgroundColor;
                        ForeColor = ThemeManager.LightTheme.TextColor;
                        break;
                    case ThemeType.Dark:
                        BackColor = ThemeManager.DarkTheme.BackgroundColor;
                        ForeColor = ThemeManager.DarkTheme.TextColor;
                        break;
                    
                    // 可以在这里添加更多主题相关的设置
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题失败: {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// 模板编辑对话框
    /// </summary>
    public partial class TemplateEditDialog : Form
    {
        private TemplateLibraryManager.TemplateFavorite favorite;
        private TextBox notesTextBox;
        private CheckedListBox tagsCheckedListBox;
        private Panel ratingPanel;
        private int selectedRating;

        public TemplateEditDialog(TemplateLibraryManager.TemplateFavorite favorite)
        {
            this.favorite = favorite;
            this.selectedRating = favorite.Rating;
            
            InitializeComponent();
            CreateControls();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = "编辑模板信息";
            Size = new Size(400, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
        }

        private void CreateControls()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10)
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 评分
            var ratingLabel = new Label
            {
                Text = "评分:",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill
            };

            ratingPanel = new Panel
            {
                Height = 40,
                Dock = DockStyle.Fill
            };
            CreateRatingControls();

            // 备注
            var notesLabel = new Label
            {
                Text = "备注:",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill
            };

            notesTextBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 9F)
            };

            // 标签
            var tagsLabel = new Label
            {
                Text = "标签:",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill
            };

            tagsCheckedListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Font = new Font("微软雅黑", 9F)
            };

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
                UseVisualStyleBackColor = true,
                DialogResult = DialogResult.Cancel
            };

            var okButton = new Button
            {
                Text = "确定",
                Size = new Size(75, 30),
                UseVisualStyleBackColor = true,
                DialogResult = DialogResult.OK
            };
            okButton.Click += OnOkClick;

            buttonPanel.Controls.AddRange(new Control[] { cancelButton, okButton });

            layout.Controls.Add(CreateLabeledControl(ratingLabel, ratingPanel), 0, 0);
            layout.Controls.Add(CreateLabeledControl(notesLabel, notesTextBox), 0, 1);
            layout.Controls.Add(CreateLabeledControl(tagsLabel, tagsCheckedListBox), 0, 2);
            layout.Controls.Add(buttonPanel, 0, 3);

            Controls.Add(layout);
        }

        private Panel CreateLabeledControl(Label label, Control control)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Height = 100 };
            label.Height = 25;
            label.Dock = DockStyle.Top;
            control.Dock = DockStyle.Fill;
            panel.Controls.Add(control);
            panel.Controls.Add(label);
            return panel;
        }

        private void CreateRatingControls()
        {
            for (int i = 1; i <= 5; i++)
            {
                var star = new Label
                {
                    Text = i <= selectedRating ? "⭐" : "☆",
                    Location = new Point((i - 1) * 30, 10),
                    Size = new Size(25, 25),
                    Font = new Font("Segoe UI Emoji", 14F),
                    Cursor = Cursors.Hand,
                    Tag = i
                };

                star.Click += OnStarClick;
                ratingPanel.Controls.Add(star);
            }
        }

        private void OnStarClick(object? sender, EventArgs e)
        {
            if (sender is Label star && star.Tag is int rating)
            {
                selectedRating = rating;
                UpdateRatingDisplay();
            }
        }

        private void UpdateRatingDisplay()
        {
            foreach (Control control in ratingPanel.Controls)
            {
                if (control is Label star && star.Tag is int starRating)
                {
                    star.Text = starRating <= selectedRating ? "⭐" : "☆";
                }
            }
        }

        private void LoadData()
        {
            notesTextBox.Text = favorite.Notes;

            // 加载所有可用标签
            var allTags = TemplateLibraryManager.GetAllTags();
            foreach (var tag in allTags)
            {
                var isChecked = favorite.Tags.Contains(tag);
                tagsCheckedListBox.Items.Add(tag, isChecked);
            }
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            try
            {
                var selectedTags = new List<string>();
                foreach (int index in tagsCheckedListBox.CheckedIndices)
                {
                    selectedTags.Add(tagsCheckedListBox.Items[index].ToString() ?? "");
                }

                TemplateLibraryManager.UpdateFavorite(
                    favorite.Id,
                    rating: selectedRating,
                    notes: notesTextBox.Text,
                    tags: selectedTags
                );

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}