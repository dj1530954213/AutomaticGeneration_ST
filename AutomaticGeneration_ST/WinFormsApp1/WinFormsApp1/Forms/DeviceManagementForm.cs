using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1.Devices;
using WinFormsApp1.Devices.Interfaces;
using AutomaticGeneration_ST.Models;
using WinFormsApp1.Forms;

namespace WinFormsApp1.Forms
{
    /// <summary>
    /// 设备管理主窗体
    /// </summary>
    public partial class DeviceManagementForm : Form
    {
        private BindingList<CompositeDevice> _devices;
        private DeviceType? _filterDeviceType;

        public DeviceManagementForm()
        {
            InitializeComponent();
            InitializeForm();
            LoadDevices();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体属性
            this.Text = "设备管理";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.WindowState = FormWindowState.Normal;

            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };

            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 工具栏
            var toolbar = CreateToolbar();
            mainPanel.Controls.Add(toolbar, 0, 0);

            // 设备列表
            var deviceListGroup = CreateDeviceListGroup();
            mainPanel.Controls.Add(deviceListGroup, 0, 1);

            // 状态栏
            var statusBar = CreateStatusBar();
            mainPanel.Controls.Add(statusBar, 0, 2);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        #region 界面创建

        private Panel CreateToolbar()
        {
            var toolbar = new Panel
            {
                Height = 40,
                Dock = DockStyle.Fill
            };

            // 操作按钮
            btnAddDevice = new Button
            {
                Text = "新建设备",
                Size = new Size(80, 30),
                Location = new System.Drawing.Point(5, 5),
                UseVisualStyleBackColor = true
            };
            btnAddDevice.Click += BtnAddDevice_Click;

            btnEditDevice = new Button
            {
                Text = "编辑设备",
                Size = new Size(80, 30),
                Location = new System.Drawing.Point(90, 5),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnEditDevice.Click += BtnEditDevice_Click;

            btnDeleteDevice = new Button
            {
                Text = "删除设备",
                Size = new Size(80, 30),
                Location = new System.Drawing.Point(175, 5),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnDeleteDevice.Click += BtnDeleteDevice_Click;

            btnCopyDevice = new Button
            {
                Text = "复制设备",
                Size = new Size(80, 30),
                Location = new System.Drawing.Point(260, 5),
                UseVisualStyleBackColor = true,
                Enabled = false
            };
            btnCopyDevice.Click += BtnCopyDevice_Click;

            // 分隔线
            var separator1 = new Panel
            {
                Width = 2,
                Height = 25,
                Location = new System.Drawing.Point(350, 8),
                BackColor = SystemColors.ControlDark
            };

            // 筛选控件
            var lblFilter = new Label
            {
                Text = "类型筛选:",
                Size = new Size(60, 23),
                Location = new System.Drawing.Point(360, 9),
                TextAlign = ContentAlignment.MiddleLeft
            };

            cmbFilterType = new ComboBox
            {
                Size = new Size(100, 23),
                Location = new System.Drawing.Point(425, 8),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilterType.Items.Add("全部");
            cmbFilterType.Items.AddRange(Enum.GetNames(typeof(DeviceType)));
            cmbFilterType.SelectedIndex = 0;
            cmbFilterType.SelectedIndexChanged += CmbFilterType_SelectedIndexChanged;

            // 搜索控件
            var lblSearch = new Label
            {
                Text = "搜索:",
                Size = new Size(40, 23),
                Location = new System.Drawing.Point(540, 9),
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtSearch = new TextBox
            {
                Size = new Size(150, 23),
                Location = new System.Drawing.Point(585, 8)
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;

            btnSearch = new Button
            {
                Text = "搜索",
                Size = new Size(50, 23),
                Location = new System.Drawing.Point(740, 8),
                UseVisualStyleBackColor = true
            };
            btnSearch.Click += BtnSearch_Click;

            // 刷新按钮
            btnRefresh = new Button
            {
                Text = "刷新",
                Size = new Size(60, 30),
                Location = new System.Drawing.Point(800, 5),
                UseVisualStyleBackColor = true
            };
            btnRefresh.Click += BtnRefresh_Click;

            toolbar.Controls.AddRange(new Control[] 
            {
                btnAddDevice, btnEditDevice, btnDeleteDevice, btnCopyDevice,
                separator1, lblFilter, cmbFilterType, lblSearch, txtSearch, btnSearch, btnRefresh
            });

            return toolbar;
        }

        private GroupBox CreateDeviceListGroup()
        {
            var group = new GroupBox
            {
                Text = "设备列表",
                Dock = DockStyle.Fill
            };

            dgvDevices = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            SetupDevicesDataGridView();
            dgvDevices.SelectionChanged += DgvDevices_SelectionChanged;
            dgvDevices.DoubleClick += DgvDevices_DoubleClick;

            // 右键菜单
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("编辑设备", null, (s, e) => EditSelectedDevice());
            contextMenu.Items.Add("复制设备", null, (s, e) => CopySelectedDevice());
            contextMenu.Items.Add("删除设备", null, (s, e) => DeleteSelectedDevice());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("生成ST代码", null, (s, e) => GenerateSelectedDeviceCode());
            contextMenu.Items.Add("导出设备配置", null, (s, e) => ExportSelectedDevice());

            dgvDevices.ContextMenuStrip = contextMenu;
            group.Controls.Add(dgvDevices);

            return group;
        }

        private Panel CreateStatusBar()
        {
            var statusBar = new Panel
            {
                Height = 25,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblStatus = new Label
            {
                Text = "就绪",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            lblDeviceCount = new Label
            {
                Text = "设备总数: 0",
                Width = 100,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 5, 0)
            };

            statusBar.Controls.AddRange(new Control[] { lblStatus, lblDeviceCount });
            return statusBar;
        }

        private void SetupDevicesDataGridView()
        {
            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "设备名称",
                DataPropertyName = "Name",
                Width = 150
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "设备类型",
                DataPropertyName = "Type",
                Width = 100
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Manufacturer",
                HeaderText = "制造商",
                DataPropertyName = "Manufacturer",
                Width = 120
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Model",
                HeaderText = "型号",
                DataPropertyName = "Model",
                Width = 120
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "描述",
                DataPropertyName = "Description",
                Width = 200
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PointCount",
                HeaderText = "点位数",
                Width = 80
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Author",
                HeaderText = "创建者",
                DataPropertyName = "Author",
                Width = 100
            });

            dgvDevices.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CreatedTime",
                HeaderText = "创建时间",
                DataPropertyName = "CreatedTime",
                Width = 130
            });

            // 设置单元格格式
            dgvDevices.CellFormatting += DgvDevices_CellFormatting;
        }

        #endregion

        #region 控件字段

        private Button btnAddDevice;
        private Button btnEditDevice;
        private Button btnDeleteDevice;
        private Button btnCopyDevice;
        private ComboBox cmbFilterType;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnRefresh;
        private DataGridView dgvDevices;
        private Label lblStatus;
        private Label lblDeviceCount;

        #endregion

        #region 窗体初始化

        private void InitializeForm()
        {
            _devices = new BindingList<CompositeDevice>();
            dgvDevices.DataSource = _devices;

            // 订阅设备管理器事件
            DeviceManager.DeviceAdded += DeviceManager_DeviceAdded;
            DeviceManager.DeviceUpdated += DeviceManager_DeviceUpdated;
            DeviceManager.DeviceRemoved += DeviceManager_DeviceRemoved;
        }

        private void LoadDevices()
        {
            try
            {
                var devices = DeviceManager.GetAllDevices();
                _devices.Clear();
                
                foreach (var device in devices)
                {
                    _devices.Add(device);
                }

                FilterDevices();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                ShowError($"加载设备列表失败: {ex.Message}");
            }
        }

        #endregion

        #region 事件处理

        private void BtnAddDevice_Click(object sender, EventArgs e)
        {
            using var form = new DeviceConfigForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadDevices();
                lblStatus.Text = "设备添加成功";
            }
        }

        private void BtnEditDevice_Click(object sender, EventArgs e)
        {
            EditSelectedDevice();
        }

        private void BtnDeleteDevice_Click(object sender, EventArgs e)
        {
            DeleteSelectedDevice();
        }

        private void BtnCopyDevice_Click(object sender, EventArgs e)
        {
            CopySelectedDevice();
        }

        private void CmbFilterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            FilterDevices();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            // 延迟搜索以提高性能
            if (_searchTimer != null)
            {
                _searchTimer.Stop();
                _searchTimer.Dispose();
            }

            _searchTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _searchTimer.Tick += (s, args) =>
            {
                _searchTimer.Stop();
                FilterDevices();
            };
            _searchTimer.Start();
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            FilterDevices();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadDevices();
            lblStatus.Text = "设备列表已刷新";
        }

        private void DgvDevices_SelectionChanged(object sender, EventArgs e)
        {
            var hasSelection = dgvDevices.SelectedRows.Count > 0;
            btnEditDevice.Enabled = hasSelection;
            btnDeleteDevice.Enabled = hasSelection;
            btnCopyDevice.Enabled = hasSelection;
        }

        private void DgvDevices_DoubleClick(object sender, EventArgs e)
        {
            EditSelectedDevice();
        }

        private void DgvDevices_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _devices.Count)
            {
                var device = _devices[e.RowIndex];
                
                if (dgvDevices.Columns[e.ColumnIndex].Name == "PointCount")
                {
                    e.Value = device.Points.Count;
                    e.FormattingApplied = true;
                }
                else if (dgvDevices.Columns[e.ColumnIndex].Name == "CreatedTime")
                {
                    e.Value = device.CreatedTime.ToString("yyyy-MM-dd HH:mm");
                    e.FormattingApplied = true;
                }
                else if (dgvDevices.Columns[e.ColumnIndex].Name == "Type")
                {
                    e.Value = GetCompositeDeviceTypeDisplayName(device.Type);
                    e.FormattingApplied = true;
                }
            }
        }

        private void DeviceManager_DeviceAdded(object sender, CompositeDevice device)
        {
            BeginInvoke(() =>
            {
                if (!_devices.Contains(device))
                {
                    _devices.Add(device);
                    FilterDevices();
                    UpdateStatus();
                }
            });
        }

        private void DeviceManager_DeviceUpdated(object sender, CompositeDevice device)
        {
            BeginInvoke(() =>
            {
                var index = _devices.IndexOf(device);
                if (index >= 0)
                {
                    _devices[index] = device;
                    FilterDevices();
                }
            });
        }

        private void DeviceManager_DeviceRemoved(object sender, string deviceId)
        {
            BeginInvoke(() =>
            {
                var device = _devices.FirstOrDefault(d => d.Id == deviceId);
                if (device != null)
                {
                    _devices.Remove(device);
                    FilterDevices();
                    UpdateStatus();
                }
            });
        }

        #endregion

        #region 私有方法

        private System.Windows.Forms.Timer _searchTimer;

        private void FilterDevices()
        {
            try
            {
                var allDevices = DeviceManager.GetAllDevices();
                var filteredDevices = allDevices.AsEnumerable();

                // 类型筛选
                if (cmbFilterType.SelectedIndex > 0)
                {
                    if (Enum.TryParse<DeviceType>(cmbFilterType.SelectedItem.ToString(), out var filterType))
                    {
                        filteredDevices = filteredDevices.Where(d => d.Type.ToString() == filterType.ToString());
                    }
                }

                // 搜索筛选
                var searchText = txtSearch.Text.ToLower();
                if (!string.IsNullOrEmpty(searchText))
                {
                    filteredDevices = filteredDevices.Where(d =>
                        d.Name.ToLower().Contains(searchText) ||
                        d.Description.ToLower().Contains(searchText) ||
                        d.Manufacturer.ToLower().Contains(searchText) ||
                        d.Model.ToLower().Contains(searchText));
                }

                // 更新显示
                _devices.Clear();
                foreach (var device in filteredDevices)
                {
                    _devices.Add(device);
                }

                UpdateStatus();
            }
            catch (Exception ex)
            {
                ShowError($"筛选设备失败: {ex.Message}");
            }
        }

        private void UpdateStatus()
        {
            lblDeviceCount.Text = $"设备总数: {_devices.Count}";
        }

        private void EditSelectedDevice()
        {
            if (dgvDevices.SelectedRows.Count == 0) return;

            var device = (CompositeDevice)dgvDevices.SelectedRows[0].DataBoundItem;
            using var form = new DeviceConfigForm(device);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadDevices();
                lblStatus.Text = "设备编辑成功";
            }
        }

        private void CopySelectedDevice()
        {
            if (dgvDevices.SelectedRows.Count == 0) return;

            var originalDevice = (CompositeDevice)dgvDevices.SelectedRows[0].DataBoundItem;
            
            // 创建设备副本 - 使用具体实现类
            var copiedDevice = CreateBasicCompositeDevice(
                $"{originalDevice.Id}_copy",
                $"{originalDevice.DeviceName}_副本", 
                originalDevice.Type);
            
            copiedDevice.Description = originalDevice.Description;
            
            // 复制点位 - 使用可写访问方式
            foreach (var point in originalDevice.Points)
            {
                var newPoint = new AutomaticGeneration_ST.Models.Point(point.HmiTagName)
                {
                    ModuleName = point.ModuleName,
                    ModuleType = point.ModuleType,
                    ChannelNumber = point.ChannelNumber,
                    Description = point.Description,
                    Unit = point.Unit,
                    RangeLow = point.RangeLow,
                    RangeHigh = point.RangeHigh,
                    IsAlarmEnabled = point.IsAlarmEnabled,
                    AlarmHigh = point.AlarmHigh,
                    AlarmLow = point.AlarmLow
                };
                
                copiedDevice.AddPoint(newPoint);
            }

            // 复制属性 - 使用设置方法
            foreach (var prop in originalDevice.Properties)
            {
                copiedDevice.SetParameter(prop.Key, prop.Value);
            }

            using var form = new DeviceConfigForm(copiedDevice);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadDevices();
                lblStatus.Text = "设备复制成功";
            }
        }

        private void DeleteSelectedDevice()
        {
            if (dgvDevices.SelectedRows.Count == 0) return;

            var device = (CompositeDevice)dgvDevices.SelectedRows[0].DataBoundItem;
            
            var result = MessageBox.Show(
                $"确定要删除设备 '{device.DeviceName}' 吗？\n\n删除后将无法恢复。",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    if (DeviceManager.RemoveDevice(device.Id))
                    {
                        LoadDevices();
                        lblStatus.Text = "设备删除成功";
                    }
                    else
                    {
                        ShowError("删除设备失败");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"删除设备失败: {ex.Message}");
                }
            }
        }

        private void GenerateSelectedDeviceCode()
        {
            if (dgvDevices.SelectedRows.Count == 0) return;

            var device = (CompositeDevice)dgvDevices.SelectedRows[0].DataBoundItem;
            
            try
            {
                var code = DeviceManager.GenerateDeviceCode(device);
                
                using var form = new Form
                {
                    Text = $"设备ST代码 - {device.DeviceName}",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent
                };

                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 2,
                    Padding = new Padding(10)
                };

                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var textBox = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    Text = code,
                    ReadOnly = true,
                    Font = new Font("Consolas", 10),
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White
                };

                var buttonPanel = new Panel { Height = 35, Dock = DockStyle.Fill };
                var copyButton = new Button
                {
                    Text = "复制代码",
                    Size = new Size(80, 25),
                    Location = new System.Drawing.Point(5, 5)
                };
                copyButton.Click += (s, e) =>
                {
                    Clipboard.SetText(code);
                    MessageBox.Show("代码已复制到剪贴板", "提示");
                };

                buttonPanel.Controls.Add(copyButton);
                layout.Controls.Add(textBox, 0, 0);
                layout.Controls.Add(buttonPanel, 0, 1);
                form.Controls.Add(layout);

                form.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"生成ST代码失败: {ex.Message}");
            }
        }

        private void ExportSelectedDevice()
        {
            if (dgvDevices.SelectedRows.Count == 0) return;

            var device = (CompositeDevice)dgvDevices.SelectedRows[0].DataBoundItem;
            
            using var saveDialog = new SaveFileDialog
            {
                Filter = "JSON文件|*.json|所有文件|*.*",
                FileName = $"{device.DeviceName}_config.json"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(device, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });

                    System.IO.File.WriteAllText(saveDialog.FileName, json);
                    lblStatus.Text = "设备配置导出成功";
                    MessageBox.Show("设备配置导出成功", "提示");
                }
                catch (Exception ex)
                {
                    ShowError($"导出设备配置失败: {ex.Message}");
                }
            }
        }

        private string GetDeviceTypeDisplayName(DeviceType type)
        {
            return type switch
            {
                DeviceType.Motor => "电机",
                DeviceType.Valve => "阀门",
                DeviceType.Pump => "泵",
                DeviceType.Tank => "储罐",
                DeviceType.Sensor => "传感器",
                DeviceType.Controller => "控制器",
                DeviceType.Custom => "自定义",
                _ => type.ToString()
            };
        }
        
        private string GetCompositeDeviceTypeDisplayName(CompositeDeviceType type)
        {
            return type switch
            {
                CompositeDeviceType.ValveController => "阀门控制器",
                CompositeDeviceType.PumpController => "泵站控制器",
                CompositeDeviceType.VFDController => "变频器控制器",
                _ => type.ToString()
            };
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "发生错误";
        }

        #endregion

        #region 设备创建辅助方法

        private CompositeDevice CreateBasicCompositeDevice(string deviceId, string deviceName, CompositeDeviceType deviceType)
        {
            return new BasicCompositeDevice(deviceId, deviceName, deviceType);
        }

        #endregion

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 取消订阅事件
            DeviceManager.DeviceAdded -= DeviceManager_DeviceAdded;
            DeviceManager.DeviceUpdated -= DeviceManager_DeviceUpdated;
            DeviceManager.DeviceRemoved -= DeviceManager_DeviceRemoved;

            _searchTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
