////NEED DELETE: 设备配置编辑窗口（模板/标识/编辑相关UI），非核心导入/导出所需
//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using WinFormsApp1.Devices;
//using WinFormsApp1.Devices.Interfaces;
//using WinFormsApp1.Templates;

//namespace WinFormsApp1.Forms
//{
//    /// <summary>
//    /// 设备配置管理窗体
//    /// </summary>
//    public partial class DeviceConfigForm : Form
//    {
//        private CompositeDevice _currentDevice;
//        private bool _isEditing = false;
//        private Dictionary<string, object> _originalProperties = new();

//        public DeviceConfigForm()
//        {
//            InitializeComponent();
//            InitializeForm();
//        }

//        public DeviceConfigForm(CompositeDevice device) : this()
//        {
//            _currentDevice = device;
//            _isEditing = true;
//            LoadDeviceData();
//        }

//        private void InitializeComponent()
//        {
//            this.SuspendLayout();

//            // 窗体属性
//            this.Text = "设备配置管理";
//            this.Size = new Size(900, 700);
//            this.StartPosition = FormStartPosition.CenterParent;
//            this.FormBorderStyle = FormBorderStyle.FixedDialog;
//            this.MaximizeBox = false;
//            this.MinimizeBox = false;

//            // 创建主面板
//            var mainPanel = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 2,
//                RowCount = 3,
//                Padding = new Padding(10)
//            };

//            // 设置列宽
//            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
//            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

//            // 设置行高
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

//            // 基本信息组
//            var basicInfoGroup = CreateBasicInfoGroup();
//            mainPanel.Controls.Add(basicInfoGroup, 0, 0);
//            mainPanel.SetColumnSpan(basicInfoGroup, 2);

//            // 点位配置组
//            var pointsGroup = CreatePointsGroup();
//            mainPanel.Controls.Add(pointsGroup, 0, 1);

//            // 预览面板
//            var previewGroup = CreatePreviewGroup();
//            mainPanel.Controls.Add(previewGroup, 1, 1);

//            // 按钮面板
//            var buttonPanel = CreateButtonPanel();
//            mainPanel.Controls.Add(buttonPanel, 0, 2);
//            mainPanel.SetColumnSpan(buttonPanel, 2);

//            this.Controls.Add(mainPanel);
//            this.ResumeLayout(false);
//        }

//        #region 界面创建

//        private GroupBox CreateBasicInfoGroup()
//        {
//            var group = new GroupBox
//            {
//                Text = "基本信息",
//                Dock = DockStyle.Fill,
//                Padding = new Padding(10)
//            };

//            var layout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 4,
//                RowCount = 3
//            };

//            // 设置列宽
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

//            // 设备名称
//            layout.Controls.Add(new Label { Text = "设备名称:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
//            txtDeviceName = new TextBox { Dock = DockStyle.Fill };
//            layout.Controls.Add(txtDeviceName, 1, 0);

//            // 设备类型
//            layout.Controls.Add(new Label { Text = "设备类型:", TextAlign = ContentAlignment.MiddleRight }, 2, 0);
//            cmbDeviceType = new ComboBox
//            {
//                Dock = DockStyle.Fill,
//                DropDownStyle = ComboBoxStyle.DropDownList
//            };
//            cmbDeviceType.Items.AddRange(Enum.GetNames(typeof(DeviceType)));
//            cmbDeviceType.SelectedIndexChanged += CmbDeviceType_SelectedIndexChanged;
//            layout.Controls.Add(cmbDeviceType, 3, 0);

//            // 制造商
//            layout.Controls.Add(new Label { Text = "制造商:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
//            txtManufacturer = new TextBox { Dock = DockStyle.Fill };
//            layout.Controls.Add(txtManufacturer, 1, 1);

//            // 型号
//            layout.Controls.Add(new Label { Text = "型号:", TextAlign = ContentAlignment.MiddleRight }, 2, 1);
//            txtModel = new TextBox { Dock = DockStyle.Fill };
//            layout.Controls.Add(txtModel, 3, 1);

//            // 描述
//            layout.Controls.Add(new Label { Text = "描述:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
//            txtDescription = new TextBox 
//            { 
//                Dock = DockStyle.Fill,
//                Multiline = true,
//                Height = 60
//            };
//            layout.Controls.Add(txtDescription, 1, 2);
//            layout.SetColumnSpan(txtDescription, 3);

//            group.Controls.Add(layout);
//            return group;
//        }

//        private GroupBox CreatePointsGroup()
//        {
//            var group = new GroupBox
//            {
//                Text = "点位配置",
//                Dock = DockStyle.Fill
//            };

//            var layout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 3,
//                Padding = new Padding(5)
//            };

//            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
//            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

//            // 工具栏
//            var toolbar = new Panel
//            {
//                Height = 35,
//                Dock = DockStyle.Fill
//            };

//            btnAddPoint = new Button
//            {
//                Text = "添加点位",
//                Size = new Size(80, 25),
//                Location = new Point(5, 5)
//            };
//            btnAddPoint.Click += BtnAddPoint_Click;

//            btnEditPoint = new Button
//            {
//                Text = "编辑点位",
//                Size = new Size(80, 25),
//                Location = new Point(90, 5)
//            };
//            btnEditPoint.Click += BtnEditPoint_Click;

//            btnDeletePoint = new Button
//            {
//                Text = "删除点位",
//                Size = new Size(80, 25),
//                Location = new Point(175, 5)
//            };
//            btnDeletePoint.Click += BtnDeletePoint_Click;

//            btnApplyTemplate = new Button
//            {
//                Text = "应用模板",
//                Size = new Size(80, 25),
//                Location = new Point(260, 5)
//            };
//            btnApplyTemplate.Click += BtnApplyTemplate_Click;

//            toolbar.Controls.AddRange(new Control[] { btnAddPoint, btnEditPoint, btnDeletePoint, btnApplyTemplate });
//            layout.Controls.Add(toolbar, 0, 0);

//            // 点位列表
//            dgvPoints = new DataGridView
//            {
//                Dock = DockStyle.Fill,
//                AutoGenerateColumns = false,
//                AllowUserToAddRows = false,
//                AllowUserToDeleteRows = false,
//                ReadOnly = true,
//                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
//                MultiSelect = false
//            };

//            SetupPointsDataGridView();
//            layout.Controls.Add(dgvPoints, 0, 1);

//            // 状态栏
//            lblPointsStatus = new Label
//            {
//                Text = "总计: 0 个点位",
//                Height = 20,
//                Dock = DockStyle.Fill,
//                TextAlign = ContentAlignment.MiddleLeft
//            };
//            layout.Controls.Add(lblPointsStatus, 0, 2);

//            group.Controls.Add(layout);
//            return group;
//        }

//        private GroupBox CreatePreviewGroup()
//        {
//            var group = new GroupBox
//            {
//                Text = "ST代码预览",
//                Dock = DockStyle.Fill
//            };

//            var layout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 2,
//                Padding = new Padding(5)
//            };

//            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
//            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

//            // 代码预览框
//            txtCodePreview = new RichTextBox
//            {
//                Dock = DockStyle.Fill,
//                ReadOnly = true,
//                Font = new Font("Consolas", 10),
//                BackColor = Color.FromArgb(30, 30, 30),
//                ForeColor = Color.White
//            };
//            layout.Controls.Add(txtCodePreview, 0, 0);

//            // 预览工具栏
//            var previewToolbar = new Panel
//            {
//                Height = 35,
//                Dock = DockStyle.Fill
//            };

//            btnRefreshPreview = new Button
//            {
//                Text = "刷新预览",
//                Size = new Size(80, 25),
//                Location = new Point(5, 5)
//            };
//            btnRefreshPreview.Click += BtnRefreshPreview_Click;

//            btnCopyCode = new Button
//            {
//                Text = "复制代码",
//                Size = new Size(80, 25),
//                Location = new Point(90, 5)
//            };
//            btnCopyCode.Click += BtnCopyCode_Click;

//            previewToolbar.Controls.AddRange(new Control[] { btnRefreshPreview, btnCopyCode });
//            layout.Controls.Add(previewToolbar, 0, 1);

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

//            btnSave = new Button
//            {
//                Text = "保存",
//                Size = new Size(80, 30),
//                UseVisualStyleBackColor = true
//            };
//            btnSave.Click += BtnSave_Click;

//            btnCancel = new Button
//            {
//                Text = "取消",
//                Size = new Size(80, 30),
//                UseVisualStyleBackColor = true,
//                DialogResult = DialogResult.Cancel
//            };
//            btnCancel.Click += BtnCancel_Click;

//            btnValidate = new Button
//            {
//                Text = "验证配置",
//                Size = new Size(80, 30),
//                UseVisualStyleBackColor = true
//            };
//            btnValidate.Click += BtnValidate_Click;

//            // 右对齐按钮
//            var buttonContainer = new FlowLayoutPanel
//            {
//                FlowDirection = FlowDirection.RightToLeft,
//                Dock = DockStyle.Right,
//                Width = 270,
//                WrapContents = false
//            };

//            buttonContainer.Controls.AddRange(new Control[] { btnCancel, btnSave, btnValidate });
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
//                Name = "Address",
//                HeaderText = "地址",
//                DataPropertyName = "Address",
//                Width = 100
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

//            dgvPoints.Columns.Add(new DataGridViewCheckBoxColumn
//            {
//                Name = "IsAlarmEnabled",
//                HeaderText = "报警",
//                DataPropertyName = "IsAlarmEnabled",
//                Width = 50
//            });
//        }

//        #endregion

//        #region 控件字段

//        private TextBox txtDeviceName;
//        private ComboBox cmbDeviceType;
//        private TextBox txtManufacturer;
//        private TextBox txtModel;
//        private TextBox txtDescription;
//        private DataGridView dgvPoints;
//        private Button btnAddPoint;
//        private Button btnEditPoint;
//        private Button btnDeletePoint;
//        private Button btnApplyTemplate;
//        private Label lblPointsStatus;
//        private RichTextBox txtCodePreview;
//        private Button btnRefreshPreview;
//        private Button btnCopyCode;
//        private Button btnSave;
//        private Button btnCancel;
//        private Button btnValidate;

//        #endregion

//        #region 窗体初始化

//        private void InitializeForm()
//        {
//            _currentDevice = new BasicCompositeDevice("temp_device", "临时设备", CompositeDeviceType.ValveController);
//            RefreshPointsList();
//            RefreshPreview();
//        }

//        private void LoadDeviceData()
//        {
//            if (_currentDevice == null) return;

//            txtDeviceName.Text = _currentDevice.Name;
//            cmbDeviceType.SelectedItem = _currentDevice.Type.ToString();
//            txtManufacturer.Text = _currentDevice.Manufacturer;
//            txtModel.Text = _currentDevice.Model;
//            txtDescription.Text = _currentDevice.Description;

//            // 备份原始属性
//            _originalProperties = new Dictionary<string, object>(_currentDevice.Properties);

//            RefreshPointsList();
//            RefreshPreview();
//        }

//        #endregion

//        #region 事件处理

//        private void CmbDeviceType_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            if (cmbDeviceType.SelectedItem != null)
//            {
//                // 设备类型在创建后不能更改，只刷新预览
//                RefreshPreview();
//            }
//        }

//        private void BtnAddPoint_Click(object sender, EventArgs e)
//        {
//            using var form = new DevicePointEditForm();
//            if (form.ShowDialog() == DialogResult.OK)
//            {
//                var devicePoint = form.GetDevicePoint();
//                if (devicePoint != null)
//                {
//                    // 将DevicePoint转换为Point
//                    var point = new AutomaticGeneration_ST.Models.Point(devicePoint.Name)
//                    {
//                        Description = devicePoint.Description,
//                        Unit = devicePoint.Unit,
//                        IsAlarmEnabled = devicePoint.IsAlarmEnabled,
//                        AlarmHigh = devicePoint.AlarmHigh,
//                        AlarmLow = devicePoint.AlarmLow
//                    };
//                    _currentDevice.AddPoint(point);
//                    RefreshPointsList();
//                    RefreshPreview();
//                }
//            }
//        }

//        private void BtnEditPoint_Click(object sender, EventArgs e)
//        {
//            if (dgvPoints.SelectedRows.Count == 0) return;

//            var index = dgvPoints.SelectedRows[0].Index;
//            var point = _currentDevice.Points[index];

//            // 先移除旧点位，再添加新点位来模拟更新
//            _currentDevice.RemovePoint(point.HmiTagName);
            
//            using var form = new DevicePointEditForm();
//            if (form.ShowDialog() == DialogResult.OK)
//            {
//                var editedPoint = form.GetDevicePoint();
//                if (editedPoint != null)
//                {
//                    // 将DevicePoint转换为Point并添加
//                    var newPoint = new AutomaticGeneration_ST.Models.Point(editedPoint.Name)
//                    {
//                        Description = editedPoint.Description,
//                        Unit = editedPoint.Unit
//                    };
//                    _currentDevice.AddPoint(newPoint);
//                    RefreshPointsList();
//                    RefreshPreview();
//                }
//            }
//        }

//        private void BtnDeletePoint_Click(object sender, EventArgs e)
//        {
//            if (dgvPoints.SelectedRows.Count == 0) return;

//            var result = MessageBox.Show("确定要删除选中的点位吗？", "确认删除", 
//                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

//            if (result == DialogResult.Yes)
//            {
//                var index = dgvPoints.SelectedRows[0].Index;
//                var point = _currentDevice.Points[index];
//                _currentDevice.RemovePoint(point.HmiTagName);
//                RefreshPointsList();
//                RefreshPreview();
//            }
//        }

//        private void BtnApplyTemplate_Click(object sender, EventArgs e)
//        {
//            var templates = DeviceManager.GetDeviceTemplates(ConvertToDeviceType(_currentDevice.Type));
//            if (!templates.Any())
//            {
//                MessageBox.Show("当前设备类型没有可用的模板。", "提示", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Information);
//                return;
//            }

//            using var form = new DeviceTemplateSelectForm(templates);
//            if (form.ShowDialog() == DialogResult.OK)
//            {
//                var selectedTemplate = form.SelectedTemplate;
//                if (selectedTemplate != null)
//                {
//                    // 清空现有点位
//                    _currentDevice.ClearPoints();

//                    // 添加模板点位
//                    foreach (var templatePoint in selectedTemplate.DefaultPoints)
//                    {
//                        var point = new AutomaticGeneration_ST.Models.Point(templatePoint.Name)
//                        {
//                            Description = templatePoint.Description,
//                            Unit = templatePoint.Unit,
//                            IsAlarmEnabled = templatePoint.IsAlarmEnabled,
//                            AlarmHigh = templatePoint.AlarmHigh,
//                            AlarmLow = templatePoint.AlarmLow
//                        };
//                        _currentDevice.AddPoint(point);
//                    }

//                    // 应用模板属性
//                    foreach (var prop in selectedTemplate.DefaultProperties)
//                    {
//                        _currentDevice.SetParameter(prop.Key, prop.Value);
//                    }

//                    RefreshPointsList();
//                    RefreshPreview();

//                    MessageBox.Show("模板应用成功！", "提示", 
//                        MessageBoxButtons.OK, MessageBoxIcon.Information);
//                }
//            }
//        }

//        private void BtnRefreshPreview_Click(object sender, EventArgs e)
//        {
//            RefreshPreview();
//        }

//        private void BtnCopyCode_Click(object sender, EventArgs e)
//        {
//            if (!string.IsNullOrEmpty(txtCodePreview.Text))
//            {
//                Clipboard.SetText(txtCodePreview.Text);
//                MessageBox.Show("代码已复制到剪贴板。", "提示", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Information);
//            }
//        }

//        private void BtnValidate_Click(object sender, EventArgs e)
//        {
//            ValidateDevice();
//        }

//        private void BtnSave_Click(object sender, EventArgs e)
//        {
//            if (SaveDevice())
//            {
//                this.DialogResult = DialogResult.OK;
//                this.Close();
//            }
//        }

//        private void BtnCancel_Click(object sender, EventArgs e)
//        {
//            this.DialogResult = DialogResult.Cancel;
//            this.Close();
//        }

//        #endregion

//        #region 私有方法

//        private void RefreshPointsList()
//        {
//            dgvPoints.DataSource = null;
//            dgvPoints.DataSource = _currentDevice.Points;
//            lblPointsStatus.Text = $"总计: {_currentDevice.Points.Count} 个点位";
//        }

//        private void RefreshPreview()
//        {
//            try
//            {
//                // 更新设备基本信息
//                _currentDevice.DeviceName = txtDeviceName.Text;
//                _currentDevice.Manufacturer = txtManufacturer.Text;
//                _currentDevice.Model = txtModel.Text;
//                _currentDevice.Description = txtDescription.Text;

//                // 生成ST代码
//                var code = DeviceManager.GenerateDeviceCode(_currentDevice);
                
//                // 应用语法高亮
//                ApplySyntaxHighlighting(code);
//            }
//            catch (Exception ex)
//            {
//                txtCodePreview.Text = $"代码生成失败: {ex.Message}";
//            }
//        }

//        private void ApplySyntaxHighlighting(string code)
//        {
//            txtCodePreview.Clear();
//            txtCodePreview.Text = code;

//            // 简单的语法高亮
//            var keywords = new[] { "VAR", "END_VAR", "TYPE", "END_TYPE", "STRUCT", "END_STRUCT", 
//                                   "BOOL", "INT", "REAL", "WORD", "DWORD", "IF", "THEN", "ELSE", "END_IF" };

//            foreach (var keyword in keywords)
//            {
//                HighlightKeyword(keyword, Color.LightBlue);
//            }

//            // 高亮注释
//            HighlightComments(Color.Green);
//        }

//        private void HighlightKeyword(string keyword, Color color)
//        {
//            var text = txtCodePreview.Text;
//            var index = 0;

//            while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) != -1)
//            {
//                txtCodePreview.Select(index, keyword.Length);
//                txtCodePreview.SelectionColor = color;
//                index += keyword.Length;
//            }

//            txtCodePreview.Select(0, 0);
//        }

//        private void HighlightComments(Color color)
//        {
//            var lines = txtCodePreview.Text.Split('\n');
//            var currentIndex = 0;

//            foreach (var line in lines)
//            {
//                var commentIndex = line.IndexOf("//");
//                if (commentIndex >= 0)
//                {
//                    var start = currentIndex + commentIndex;
//                    var length = line.Length - commentIndex;
                    
//                    txtCodePreview.Select(start, length);
//                    txtCodePreview.SelectionColor = color;
//                }
//                currentIndex += line.Length + 1; // +1 for newline
//            }

//            txtCodePreview.Select(0, 0);
//        }

//        private bool ValidateDevice()
//        {
//            var errors = DeviceManager.ValidateDevice(_currentDevice);
            
//            if (errors.Any())
//            {
//                var message = "设备配置验证失败:\n\n" + string.Join("\n", errors);
//                MessageBox.Show(message, "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                return false;
//            }
//            else
//            {
//                MessageBox.Show("设备配置验证通过！", "验证成功", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Information);
//                return true;
//            }
//        }

//        private bool SaveDevice()
//        {
//            // 验证必填字段
//            if (string.IsNullOrWhiteSpace(txtDeviceName.Text))
//            {
//                MessageBox.Show("请输入设备名称。", "验证失败", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                txtDeviceName.Focus();
//                return false;
//            }

//            if (cmbDeviceType.SelectedItem == null)
//            {
//                MessageBox.Show("请选择设备类型。", "验证失败", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                cmbDeviceType.Focus();
//                return false;
//            }

//            // 验证设备配置
//            if (!ValidateDevice())
//            {
//                return false;
//            }

//            try
//            {
//                // 更新设备信息
//                _currentDevice.DeviceName = txtDeviceName.Text;
//                _currentDevice.Manufacturer = txtManufacturer.Text;
//                _currentDevice.Model = txtModel.Text;
//                _currentDevice.Description = txtDescription.Text;

//                // 设备类型在创建后不能更改
//                // if (Enum.TryParse<DeviceType>(cmbDeviceType.SelectedItem.ToString(), out var deviceType))
//                // {
//                //     _currentDevice.Type = deviceType;
//                // }

//                // 保存或更新设备
//                if (_isEditing)
//                {
//                    DeviceManager.UpdateDevice(_currentDevice);
//                }
//                else
//                {
//                    // 转换CompositeDeviceType为DeviceType
//                    var deviceType = ConvertToDeviceType(_currentDevice.Type);
//                    DeviceManager.CreateDevice(_currentDevice.Name, deviceType);
//                }

//                return true;
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"保存设备失败: {ex.Message}", "错误", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Error);
//                return false;
//            }
//        }

//        #endregion

//        /// <summary>
//        /// 获取当前配置的设备
//        /// </summary>
//        public CompositeDevice GetDevice()
//        {
//            return _currentDevice;
//        }
        
//        /// <summary>
//        /// 将CompositeDeviceType转换为DeviceType
//        /// </summary>
//        private DeviceType ConvertToDeviceType(CompositeDeviceType compositeType)
//        {
//            return compositeType switch
//            {
//                CompositeDeviceType.ValveController => DeviceType.Valve,
//                CompositeDeviceType.PumpController => DeviceType.Pump,
//                CompositeDeviceType.VFDController => DeviceType.Controller,
//                _ => DeviceType.Custom
//            };
//        }
//    }

//    /// <summary>
//    /// 基本的组合设备实现
//    /// </summary>
//    internal class BasicCompositeDevice : CompositeDevice
//    {
//        public BasicCompositeDevice(string deviceId, string deviceName, CompositeDeviceType deviceType)
//            : base(deviceId, deviceName, deviceType)
//        {
//        }

//        protected override async Task<DeviceOperationResult> OnStartAsync()
//        {
//            await Task.Delay(100); // 模拟启动过程
//            return new DeviceOperationResult { Success = true, Message = "设备启动成功" };
//        }

//        protected override async Task<DeviceOperationResult> OnStopAsync()
//        {
//            await Task.Delay(100); // 模拟停止过程
//            return new DeviceOperationResult { Success = true, Message = "设备停止成功" };
//        }

//        protected override async Task<DeviceOperationResult> OnResetAsync()
//        {
//            await Task.Delay(50); // 模拟重置过程
//            return new DeviceOperationResult { Success = true, Message = "设备重置成功" };
//        }

//        protected override async Task<DeviceOperationResult> OnInitializeAsync()
//        {
//            await Task.Delay(50);
//            return new DeviceOperationResult { Success = true, Message = "设备初始化成功" };
//        }

//        protected override async Task<DeviceOperationResult> OnDiagnoseAsync()
//        {
//            await Task.Delay(50);
//            return new DeviceOperationResult { Success = true, Message = "设备诊断完成" };
//        }

//        protected override bool OnValidateParameter(string parameterName, object value)
//        {
//            return true;
//        }

//        protected override string OnGenerateDeviceSpecificVariables()
//        {
//            return "// 设备特定变量";
//        }

//        protected override string OnGenerateDeviceControlLogic()
//        {
//            return "// 设备控制逻辑";
//        }

//        protected override string OnGenerateDeviceAlarmHandling()
//        {
//            return "// 设备报警处理";
//        }

//        protected override async Task<DeviceOperationResult> OnApplyConfigurationChangesAsync()
//        {
//            await Task.Delay(50);
//            return new DeviceOperationResult { Success = true, Message = "配置更改已应用" };
//        }

//        protected override async Task<DeviceOperationResult> OnCancelConfigurationChangesAsync()
//        {
//            await Task.Delay(50);
//            return new DeviceOperationResult { Success = true, Message = "配置更改已取消" };
//        }

//        protected override async Task<Dictionary<string, object>> OnGetDeviceSpecificMonitoringDataAsync()
//        {
//            await Task.Delay(50);
//            return new Dictionary<string, object>();
//        }

//        protected override async Task<List<Dictionary<string, object>>> OnGetHistoricalDataAsync(DateTime startTime, DateTime endTime)
//        {
//            await Task.Delay(50);
//            return new List<Dictionary<string, object>>();
//        }

//        protected override List<string> OnValidateDeviceSpecificConfiguration()
//        {
//            return new List<string>();
//        }

//        public override CompositeDevice Clone()
//        {
//            var cloned = new BasicCompositeDevice(Id + "_clone", DeviceName + "_克隆", DeviceType);
//            cloned.Description = Description;
//            return cloned;
//        }

//    }
//}
