////--NEED DELETE: 设备模板选择窗口（模板管理UI），非核心主链路
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Windows.Forms;
//using WinFormsApp1.Devices;

//namespace WinFormsApp1.Forms
//{
//    /// <summary>
//    /// 设备模板选择窗体
//    /// </summary>
//    public partial class DeviceTemplateSelectForm : Form
//    {
//        private List<DeviceTemplate> _templates;
//        private DeviceTemplate? _selectedTemplate;

//        public DeviceTemplateSelectForm(List<DeviceTemplate> templates)
//        {
//            _templates = templates ?? new List<DeviceTemplate>();
//            InitializeComponent();
//            LoadTemplates();
//        }

//        private void InitializeComponent()
//        {
//            this.SuspendLayout();

//            // 窗体属性
//            this.Text = "选择设备模板";
//            this.Size = new Size(700, 500);
//            this.StartPosition = FormStartPosition.CenterParent;
//            this.FormBorderStyle = FormBorderStyle.FixedDialog;
//            this.MaximizeBox = false;
//            this.MinimizeBox = false;

//            // 创建主面板
//            var mainPanel = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 4,
//                Padding = new Padding(10)
//            };

//            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

//            // 说明标签
//            var lblDescription = new Label
//            {
//                Text = "请选择要应用的设备模板，模板将提供预定义的点位配置：",
//                Height = 25,
//                Dock = DockStyle.Fill,
//                TextAlign = ContentAlignment.MiddleLeft
//            };
//            mainPanel.Controls.Add(lblDescription, 0, 0);

//            // 模板列表
//            var templateListGroup = CreateTemplateListGroup();
//            mainPanel.Controls.Add(templateListGroup, 0, 1);

//            // 模板详情
//            var templateDetailsGroup = CreateTemplateDetailsGroup();
//            mainPanel.Controls.Add(templateDetailsGroup, 0, 2);

//            // 按钮面板
//            var buttonPanel = CreateButtonPanel();
//            mainPanel.Controls.Add(buttonPanel, 0, 3);

//            this.Controls.Add(mainPanel);
//            this.ResumeLayout(false);
//        }

//        #region 界面创建

//        private GroupBox CreateTemplateListGroup()
//        {
//            var group = new GroupBox
//            {
//                Text = "可用模板",
//                Dock = DockStyle.Fill
//            };

//            var layout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 2,
//                Padding = new Padding(5)
//            };

//            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

//            // 搜索框
//            var searchPanel = new Panel
//            {
//                Height = 30,
//                Dock = DockStyle.Fill
//            };

//            var lblSearch = new Label
//            {
//                Text = "搜索:",
//                Size = new Size(40, 23),
//                Location = new Point(5, 4),
//                TextAlign = ContentAlignment.MiddleLeft
//            };

//            txtSearch = new TextBox
//            {
//                Size = new Size(200, 23),
//                Location = new Point(50, 4)
//            };
//            txtSearch.TextChanged += TxtSearch_TextChanged;

//            searchPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch });
//            layout.Controls.Add(searchPanel, 0, 0);

//            // 模板列表
//            lvTemplates = new ListView
//            {
//                Dock = DockStyle.Fill,
//                View = View.Details,
//                FullRowSelect = true,
//                GridLines = true,
//                MultiSelect = false
//            };

//            lvTemplates.Columns.Add("名称", 150);
//            lvTemplates.Columns.Add("类型", 80);
//            lvTemplates.Columns.Add("描述", 300);
//            lvTemplates.Columns.Add("内置", 60);

//            lvTemplates.SelectedIndexChanged += LvTemplates_SelectedIndexChanged;
//            lvTemplates.DoubleClick += LvTemplates_DoubleClick;

//            layout.Controls.Add(lvTemplates, 0, 1);
//            group.Controls.Add(layout);

//            return group;
//        }

//        private GroupBox CreateTemplateDetailsGroup()
//        {
//            var group = new GroupBox
//            {
//                Text = "模板详情",
//                Dock = DockStyle.Fill
//            };

//            var layout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 2,
//                RowCount = 1,
//                Padding = new Padding(5)
//            };

//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

//            // 模板信息
//            var infoGroup = new GroupBox
//            {
//                Text = "基本信息",
//                Dock = DockStyle.Fill
//            };

//            var infoLayout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 2,
//                RowCount = 4,
//                Padding = new Padding(5)
//            };

//            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

//            infoLayout.Controls.Add(new Label { Text = "名称:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
//            lblTemplateName = new Label { Dock = DockStyle.Fill, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
//            infoLayout.Controls.Add(lblTemplateName, 1, 0);

//            infoLayout.Controls.Add(new Label { Text = "类型:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
//            lblTemplateType = new Label { Dock = DockStyle.Fill };
//            infoLayout.Controls.Add(lblTemplateType, 1, 1);

//            infoLayout.Controls.Add(new Label { Text = "内置:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
//            lblTemplateBuiltIn = new Label { Dock = DockStyle.Fill };
//            infoLayout.Controls.Add(lblTemplateBuiltIn, 1, 2);

//            infoLayout.Controls.Add(new Label { Text = "描述:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
//            txtTemplateDescription = new TextBox 
//            { 
//                Dock = DockStyle.Fill, 
//                Multiline = true, 
//                ReadOnly = true,
//                Height = 80
//            };
//            infoLayout.Controls.Add(txtTemplateDescription, 1, 3);

//            infoGroup.Controls.Add(infoLayout);
//            layout.Controls.Add(infoGroup, 0, 0);

//            // 默认点位列表
//            var pointsGroup = new GroupBox
//            {
//                Text = "默认点位",
//                Dock = DockStyle.Fill
//            };

//            dgvPoints = new DataGridView
//            {
//                Dock = DockStyle.Fill,
//                AutoGenerateColumns = false,
//                AllowUserToAddRows = false,
//                AllowUserToDeleteRows = false,
//                ReadOnly = true,
//                SelectionMode = DataGridViewSelectionMode.FullRowSelect
//            };

//            SetupPointsDataGridView();
//            pointsGroup.Controls.Add(dgvPoints);
//            layout.Controls.Add(pointsGroup, 1, 0);

//            group.Controls.Add(layout);
//            return group;
//        }

//        private Panel CreateButtonPanel()
//        {
//            var panel = new Panel
//            {
//                Height = 40,
//                Dock = DockStyle.Fill
//            };

//            btnOk = new Button
//            {
//                Text = "确定",
//                Size = new Size(80, 30),
//                UseVisualStyleBackColor = true,
//                DialogResult = DialogResult.OK,
//                Enabled = false
//            };
//            btnOk.Click += BtnOk_Click;

//            btnCancel = new Button
//            {
//                Text = "取消",
//                Size = new Size(80, 30),
//                UseVisualStyleBackColor = true,
//                DialogResult = DialogResult.Cancel
//            };

//            // 右对齐按钮
//            var buttonContainer = new FlowLayoutPanel
//            {
//                FlowDirection = FlowDirection.RightToLeft,
//                Dock = DockStyle.Right,
//                Width = 170,
//                WrapContents = false
//            };

//            buttonContainer.Controls.AddRange(new Control[] { btnCancel, btnOk });
//            panel.Controls.Add(buttonContainer);

//            return panel;
//        }

//        private void SetupPointsDataGridView()
//        {
//            dgvPoints.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "Name",
//                HeaderText = "点位名称",
//                DataPropertyName = "Name",
//                Width = 120
//            });

//            dgvPoints.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "Type",
//                HeaderText = "类型",
//                DataPropertyName = "Type",
//                Width = 60
//            });

//            dgvPoints.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "Description",
//                HeaderText = "描述",
//                DataPropertyName = "Description",
//                Width = 150
//            });

//            dgvPoints.Columns.Add(new DataGridViewTextBoxColumn
//            {
//                Name = "Unit",
//                HeaderText = "单位",
//                DataPropertyName = "Unit",
//                Width = 60
//            });
//        }

//        #endregion

//        #region 控件字段

//        private TextBox txtSearch;
//        private ListView lvTemplates;
//        private Label lblTemplateName;
//        private Label lblTemplateType;
//        private Label lblTemplateBuiltIn;
//        private TextBox txtTemplateDescription;
//        private DataGridView dgvPoints;
//        private Button btnOk;
//        private Button btnCancel;

//        #endregion

//        #region 数据加载

//        private void LoadTemplates()
//        {
//            FilterTemplates();
//        }

//        private void FilterTemplates()
//        {
//            lvTemplates.Items.Clear();

//            var searchText = txtSearch.Text.ToLower();
//            var filteredTemplates = _templates.Where(t => 
//                string.IsNullOrEmpty(searchText) ||
//                t.Name.ToLower().Contains(searchText) ||
//                t.Description.ToLower().Contains(searchText) ||
//                t.Type.ToString().ToLower().Contains(searchText)
//            ).ToList();

//            foreach (var template in filteredTemplates)
//            {
//                var item = new ListViewItem(template.Name)
//                {
//                    Tag = template
//                };

//                item.SubItems.Add(GetDeviceTypeDisplayName(template.Type));
//                item.SubItems.Add(template.Description);
//                item.SubItems.Add(template.IsBuiltIn ? "是" : "否");

//                lvTemplates.Items.Add(item);
//            }

//            if (lvTemplates.Items.Count > 0)
//            {
//                lvTemplates.Items[0].Selected = true;
//            }
//        }

//        private string GetDeviceTypeDisplayName(DeviceType type)
//        {
//            return type switch
//            {
//                DeviceType.Motor => "电机",
//                DeviceType.Valve => "阀门",
//                DeviceType.Pump => "泵",
//                DeviceType.Tank => "储罐",
//                DeviceType.Sensor => "传感器",
//                DeviceType.Controller => "控制器",
//                DeviceType.Custom => "自定义",
//                _ => type.ToString()
//            };
//        }

//        #endregion

//        #region 事件处理

//        private void TxtSearch_TextChanged(object sender, EventArgs e)
//        {
//            FilterTemplates();
//        }

//        private void LvTemplates_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            if (lvTemplates.SelectedItems.Count > 0)
//            {
//                var template = (DeviceTemplate)lvTemplates.SelectedItems[0].Tag;
//                ShowTemplateDetails(template);
//                _selectedTemplate = template;
//                btnOk.Enabled = true;
//            }
//            else
//            {
//                ClearTemplateDetails();
//                _selectedTemplate = null;
//                btnOk.Enabled = false;
//            }
//        }

//        private void LvTemplates_DoubleClick(object sender, EventArgs e)
//        {
//            if (_selectedTemplate != null)
//            {
//                this.DialogResult = DialogResult.OK;
//                this.Close();
//            }
//        }

//        private void BtnOk_Click(object sender, EventArgs e)
//        {
//            if (_selectedTemplate == null)
//            {
//                MessageBox.Show("请选择一个模板。", "提示", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Information);
//                return;
//            }

//            this.DialogResult = DialogResult.OK;
//            this.Close();
//        }

//        #endregion

//        #region 私有方法

//        private void ShowTemplateDetails(DeviceTemplate template)
//        {
//            lblTemplateName.Text = template.Name;
//            lblTemplateType.Text = GetDeviceTypeDisplayName(template.Type);
//            lblTemplateBuiltIn.Text = template.IsBuiltIn ? "是" : "否";
//            txtTemplateDescription.Text = template.Description;

//            // 显示默认点位
//            dgvPoints.DataSource = null;
//            dgvPoints.DataSource = template.DefaultPoints;
//        }

//        private void ClearTemplateDetails()
//        {
//            lblTemplateName.Text = "";
//            lblTemplateType.Text = "";
//            lblTemplateBuiltIn.Text = "";
//            txtTemplateDescription.Text = "";
//            dgvPoints.DataSource = null;
//        }

//        #endregion

//        /// <summary>
//        /// 获取选中的模板
//        /// </summary>
//        public DeviceTemplate? SelectedTemplate => _selectedTemplate;
//    }
//}
