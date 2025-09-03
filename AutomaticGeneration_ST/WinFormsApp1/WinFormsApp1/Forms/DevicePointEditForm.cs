////NEED DELETE: 设备点编辑窗口（模板管理/标识/编辑相关），与核心流程无关
//using System;
//using System.ComponentModel;
//using System.Drawing;
//using System.Windows.Forms;
//using WinFormsApp1.Devices;
//using WinFormsApp1.Templates;

//namespace WinFormsApp1.Forms
//{
//    /// <summary>
//    /// 设备点位编辑窗体
//    /// </summary>
//    public partial class DevicePointEditForm : Form
//    {
//        private DevicePoint _devicePoint;
//        private bool _isEditing = false;

//        public DevicePointEditForm()
//        {
//            InitializeComponent();
//            InitializeForm();
//        }

//        public DevicePointEditForm(DevicePoint point) : this()
//        {
//            _devicePoint = new DevicePoint
//            {
//                Name = point.Name,
//                Type = point.Type,
//                Address = point.Address,
//                Description = point.Description,
//                Unit = point.Unit,
//                MinValue = point.MinValue,
//                MaxValue = point.MaxValue,
//                IsAlarmEnabled = point.IsAlarmEnabled,
//                AlarmHigh = point.AlarmHigh,
//                AlarmLow = point.AlarmLow
//            };
            
//            _isEditing = true;
//            LoadPointData();
//        }

//        private void InitializeComponent()
//        {
//            this.SuspendLayout();

//            // 窗体属性
//            this.Text = "点位编辑";
//            this.Size = new Size(500, 450);
//            this.StartPosition = FormStartPosition.CenterParent;
//            this.FormBorderStyle = FormBorderStyle.FixedDialog;
//            this.MaximizeBox = false;
//            this.MinimizeBox = false;

//            // 创建主面板
//            var mainPanel = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 1,
//                RowCount = 3,
//                Padding = new Padding(10)
//            };

//            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
//            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

//            // 基本信息组
//            var basicGroup = CreateBasicInfoGroup();
//            mainPanel.Controls.Add(basicGroup, 0, 0);

//            // 高级设置组
//            var advancedGroup = CreateAdvancedGroup();
//            mainPanel.Controls.Add(advancedGroup, 0, 1);

//            // 按钮面板
//            var buttonPanel = CreateButtonPanel();
//            mainPanel.Controls.Add(buttonPanel, 0, 2);

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
//                ColumnCount = 2,
//                RowCount = 5
//            };

//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

//            // 点位名称
//            layout.Controls.Add(new Label { Text = "点位名称:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
//            txtPointName = new TextBox { Dock = DockStyle.Fill };
//            layout.Controls.Add(txtPointName, 1, 0);

//            // 点位类型
//            layout.Controls.Add(new Label { Text = "点位类型:", TextAlign = ContentAlignment.MiddleRight }, 0, 1);
//            cmbPointType = new ComboBox
//            {
//                Dock = DockStyle.Fill,
//                DropDownStyle = ComboBoxStyle.DropDownList
//            };
//            cmbPointType.Items.AddRange(Enum.GetNames(typeof(PointType)));
//            cmbPointType.SelectedIndexChanged += CmbPointType_SelectedIndexChanged;
//            layout.Controls.Add(cmbPointType, 1, 1);

//            // 地址
//            layout.Controls.Add(new Label { Text = "地址:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
//            txtAddress = new TextBox { Dock = DockStyle.Fill };
//            layout.Controls.Add(txtAddress, 1, 2);

//            // 单位
//            layout.Controls.Add(new Label { Text = "单位:", TextAlign = ContentAlignment.MiddleRight }, 0, 3);
//            txtUnit = new TextBox { Dock = DockStyle.Fill };
//            layout.Controls.Add(txtUnit, 1, 3);

//            // 描述
//            layout.Controls.Add(new Label { Text = "描述:", TextAlign = ContentAlignment.MiddleRight }, 0, 4);
//            txtDescription = new TextBox 
//            { 
//                Dock = DockStyle.Fill,
//                Multiline = true,
//                Height = 60
//            };
//            layout.Controls.Add(txtDescription, 1, 4);

//            group.Controls.Add(layout);
//            return group;
//        }

//        private GroupBox CreateAdvancedGroup()
//        {
//            var group = new GroupBox
//            {
//                Text = "高级设置",
//                Height = 150,
//                Dock = DockStyle.Fill
//            };

//            var layout = new TableLayoutPanel
//            {
//                Dock = DockStyle.Fill,
//                ColumnCount = 4,
//                RowCount = 3,
//                Padding = new Padding(10)
//            };

//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
//            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

//            // 量程设置
//            layout.Controls.Add(new Label { Text = "最小值:", TextAlign = ContentAlignment.MiddleRight }, 0, 0);
//            numMinValue = new NumericUpDown 
//            { 
//                Dock = DockStyle.Fill,
//                DecimalPlaces = 2,
//                Minimum = decimal.MinValue,
//                Maximum = decimal.MaxValue
//            };
//            layout.Controls.Add(numMinValue, 1, 0);

//            layout.Controls.Add(new Label { Text = "最大值:", TextAlign = ContentAlignment.MiddleRight }, 2, 0);
//            numMaxValue = new NumericUpDown 
//            { 
//                Dock = DockStyle.Fill,
//                DecimalPlaces = 2,
//                Minimum = decimal.MinValue,
//                Maximum = decimal.MaxValue,
//                Value = 100
//            };
//            layout.Controls.Add(numMaxValue, 3, 0);

//            // 报警设置
//            chkAlarmEnabled = new CheckBox
//            {
//                Text = "启用报警",
//                Dock = DockStyle.Fill
//            };
//            chkAlarmEnabled.CheckedChanged += ChkAlarmEnabled_CheckedChanged;
//            layout.Controls.Add(chkAlarmEnabled, 0, 1);
//            layout.SetColumnSpan(chkAlarmEnabled, 4);

//            layout.Controls.Add(new Label { Text = "高报警:", TextAlign = ContentAlignment.MiddleRight }, 0, 2);
//            numAlarmHigh = new NumericUpDown 
//            { 
//                Dock = DockStyle.Fill,
//                DecimalPlaces = 2,
//                Minimum = decimal.MinValue,
//                Maximum = decimal.MaxValue,
//                Value = 80,
//                Enabled = false
//            };
//            layout.Controls.Add(numAlarmHigh, 1, 2);

//            layout.Controls.Add(new Label { Text = "低报警:", TextAlign = ContentAlignment.MiddleRight }, 2, 2);
//            numAlarmLow = new NumericUpDown 
//            { 
//                Dock = DockStyle.Fill,
//                DecimalPlaces = 2,
//                Minimum = decimal.MinValue,
//                Maximum = decimal.MaxValue,
//                Value = 20,
//                Enabled = false
//            };
//            layout.Controls.Add(numAlarmLow, 3, 2);

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
//                DialogResult = DialogResult.OK
//            };
//            btnOk.Click += BtnOk_Click;

//            btnCancel = new Button
//            {
//                Text = "取消",
//                Size = new Size(80, 30),
//                UseVisualStyleBackColor = true,
//                DialogResult = DialogResult.Cancel
//            };

//            btnPreview = new Button
//            {
//                Text = "预览",
//                Size = new Size(80, 30),
//                UseVisualStyleBackColor = true
//            };
//            btnPreview.Click += BtnPreview_Click;

//            // 右对齐按钮
//            var buttonContainer = new FlowLayoutPanel
//            {
//                FlowDirection = FlowDirection.RightToLeft,
//                Dock = DockStyle.Right,
//                Width = 270,
//                WrapContents = false
//            };

//            buttonContainer.Controls.AddRange(new Control[] { btnCancel, btnOk, btnPreview });
//            panel.Controls.Add(buttonContainer);

//            return panel;
//        }

//        #endregion

//        #region 控件字段

//        private TextBox txtPointName;
//        private ComboBox cmbPointType;
//        private TextBox txtAddress;
//        private TextBox txtUnit;
//        private TextBox txtDescription;
//        private NumericUpDown numMinValue;
//        private NumericUpDown numMaxValue;
//        private CheckBox chkAlarmEnabled;
//        private NumericUpDown numAlarmHigh;
//        private NumericUpDown numAlarmLow;
//        private Button btnOk;
//        private Button btnCancel;
//        private Button btnPreview;

//        #endregion

//        #region 窗体初始化

//        private void InitializeForm()
//        {
//            _devicePoint = new DevicePoint();
            
//            // 设置默认值
//            cmbPointType.SelectedIndex = 0;
//            UpdateFieldsVisibility();
//        }

//        private void LoadPointData()
//        {
//            if (_devicePoint == null) return;

//            txtPointName.Text = _devicePoint.Name;
//            cmbPointType.SelectedItem = _devicePoint.Type.ToString();
//            txtAddress.Text = _devicePoint.Address;
//            txtUnit.Text = _devicePoint.Unit;
//            txtDescription.Text = _devicePoint.Description;
//            numMinValue.Value = (decimal)_devicePoint.MinValue;
//            numMaxValue.Value = (decimal)_devicePoint.MaxValue;
//            chkAlarmEnabled.Checked = _devicePoint.IsAlarmEnabled;
//            numAlarmHigh.Value = (decimal)_devicePoint.AlarmHigh;
//            numAlarmLow.Value = (decimal)_devicePoint.AlarmLow;

//            UpdateFieldsVisibility();
//        }

//        #endregion

//        #region 事件处理

//        private void CmbPointType_SelectedIndexChanged(object sender, EventArgs e)
//        {
//            UpdateFieldsVisibility();
//        }

//        private void ChkAlarmEnabled_CheckedChanged(object sender, EventArgs e)
//        {
//            numAlarmHigh.Enabled = chkAlarmEnabled.Checked;
//            numAlarmLow.Enabled = chkAlarmEnabled.Checked;
//        }

//        private void BtnOk_Click(object sender, EventArgs e)
//        {
//            if (ValidateInput())
//            {
//                SavePointData();
//                this.DialogResult = DialogResult.OK;
//                this.Close();
//            }
//        }

//        private void BtnPreview_Click(object sender, EventArgs e)
//        {
//            if (ValidateInput())
//            {
//                SavePointData();
//                ShowPreview();
//            }
//        }

//        #endregion

//        #region 私有方法

//        private void UpdateFieldsVisibility()
//        {
//            if (cmbPointType.SelectedItem == null) return;

//            if (Enum.TryParse<PointType>(cmbPointType.SelectedItem.ToString(), out var pointType))
//            {
//                // 根据点位类型显示/隐藏相关字段
//                bool isAnalog = pointType == PointType.AI || pointType == PointType.AO;
                
//                txtUnit.Enabled = isAnalog;
//                numMinValue.Enabled = isAnalog;
//                numMaxValue.Enabled = isAnalog;
//                chkAlarmEnabled.Enabled = isAnalog;
                
//                if (!isAnalog)
//                {
//                    txtUnit.Text = "";
//                    chkAlarmEnabled.Checked = false;
//                }

//                // 设置默认单位
//                if (isAnalog && string.IsNullOrEmpty(txtUnit.Text))
//                {
//                    txtUnit.Text = pointType == PointType.AI ? "V" : "V";
//                }
//            }
//        }

//        private bool ValidateInput()
//        {
//            // 验证点位名称
//            if (string.IsNullOrWhiteSpace(txtPointName.Text))
//            {
//                MessageBox.Show("请输入点位名称。", "验证失败", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                txtPointName.Focus();
//                return false;
//            }

//            // 验证地址格式
//            if (string.IsNullOrWhiteSpace(txtAddress.Text))
//            {
//                MessageBox.Show("请输入点位地址。", "验证失败", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                txtAddress.Focus();
//                return false;
//            }

//            // 验证量程设置
//            if (numMinValue.Value >= numMaxValue.Value)
//            {
//                MessageBox.Show("最小值必须小于最大值。", "验证失败", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                numMinValue.Focus();
//                return false;
//            }

//            // 验证报警设置
//            if (chkAlarmEnabled.Checked)
//            {
//                if (numAlarmLow.Value >= numAlarmHigh.Value)
//                {
//                    MessageBox.Show("低报警值必须小于高报警值。", "验证失败", 
//                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                    numAlarmLow.Focus();
//                    return false;
//                }

//                if (numAlarmLow.Value < numMinValue.Value || numAlarmHigh.Value > numMaxValue.Value)
//                {
//                    MessageBox.Show("报警值必须在量程范围内。", "验证失败", 
//                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                    return false;
//                }
//            }

//            return true;
//        }

//        private void SavePointData()
//        {
//            _devicePoint.Name = txtPointName.Text.Trim();
            
//            if (Enum.TryParse<PointType>(cmbPointType.SelectedItem?.ToString(), out var pointType))
//            {
//                _devicePoint.Type = pointType;
//            }

//            _devicePoint.Address = txtAddress.Text.Trim();
//            _devicePoint.Unit = txtUnit.Text.Trim();
//            _devicePoint.Description = txtDescription.Text.Trim();
//            _devicePoint.MinValue = (double)numMinValue.Value;
//            _devicePoint.MaxValue = (double)numMaxValue.Value;
//            _devicePoint.IsAlarmEnabled = chkAlarmEnabled.Checked;
//            _devicePoint.AlarmHigh = (double)numAlarmHigh.Value;
//            _devicePoint.AlarmLow = (double)numAlarmLow.Value;
//        }

//        private void ShowPreview()
//        {
//            try
//            {
//                // 创建临时设备用于预览（简化实现）
//                var deviceInfo = new
//                {
//                    Name = "PreviewDevice",
//                    Type = "Custom",
//                    Points = new List<DevicePoint> { _devicePoint }
//                };

//                // 简化的ST代码生成
//                var code = GenerateSimpleSTCode(deviceInfo.Points);
                
//                using var previewForm = new Form
//                {
//                    Text = "点位代码预览",
//                    Size = new Size(600, 400),
//                    StartPosition = FormStartPosition.CenterParent
//                };

//                var textBox = new RichTextBox
//                {
//                    Dock = DockStyle.Fill,
//                    Text = code,
//                    ReadOnly = true,
//                    Font = new Font("Consolas", 10),
//                    BackColor = Color.FromArgb(30, 30, 30),
//                    ForeColor = Color.White
//                };

//                previewForm.Controls.Add(textBox);
//                previewForm.ShowDialog();
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"预览生成失败: {ex.Message}", "错误", 
//                    MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }

//        #endregion

//        /// <summary>
//        /// 获取编辑后的设备点位
//        /// </summary>
//        public DevicePoint? GetDevicePoint()
//        {
//            return _devicePoint;
//        }

//        /// <summary>
//        /// 生成简单的ST代码预览
//        /// </summary>
//        private string GenerateSimpleSTCode(List<DevicePoint> points)
//        {
//            var code = new System.Text.StringBuilder();
            
//            code.AppendLine("// ST代码预览");
//            code.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
//            code.AppendLine();
            
//            code.AppendLine("PROGRAM PreviewDevice");
//            code.AppendLine("VAR");
            
//            foreach (var point in points)
//            {
//                var stType = GetSTDataType(point.Type.ToString() ?? "AI");
//                code.AppendLine($"    {point.Name} : {stType}; // {point.Description}");
//            }
            
//            code.AppendLine("END_VAR");
//            code.AppendLine();
//            code.AppendLine("// 设备控制逻辑");
//            code.AppendLine("BEGIN");
//            code.AppendLine("    // TODO: 添加设备控制逻辑");
//            code.AppendLine("END_PROGRAM");
            
//            return code.ToString();
//        }

//        /// <summary>
//        /// 获取ST数据类型
//        /// </summary>
//        private string GetSTDataType(string moduleType)
//        {
//            return moduleType switch
//            {
//                "AI" => "REAL",
//                "AO" => "REAL", 
//                "DI" => "BOOL",
//                "DO" => "BOOL",
//                _ => "REAL"
//            };
//        }
//    }
//}
