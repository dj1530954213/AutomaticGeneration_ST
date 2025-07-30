using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WinFormsApp1.Templates;

namespace WinFormsApp1.Forms
{
    public partial class PerformanceMonitorForm : Form
    {
        private System.Windows.Forms.Timer _refreshTimer;
        private ListView _statisticsListView;
        private ListView _performanceListView;
        private Chart _performanceChart;
        private Chart _cacheChart;
        private TabControl _tabControl;
        private Button _refreshButton;
        private Button _clearCacheButton;
        private Button _exportButton;
        private Label _statusLabel;

        public PerformanceMonitorForm()
        {
            InitializeComponent();
            InitializeTimer();
            LoadInitialData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体设置
            this.Text = "模板性能监控";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Icon = SystemIcons.Information;

            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 工具栏
            var toolPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 40
            };

            _refreshButton = new Button
            {
                Text = "刷新数据",
                Size = new Size(80, 30),
                Location = new Point(10, 5),
                FlatStyle = FlatStyle.Flat
            };
            _refreshButton.Click += OnRefresh;

            _clearCacheButton = new Button
            {
                Text = "清空缓存",
                Size = new Size(80, 30),
                Location = new Point(100, 5),
                FlatStyle = FlatStyle.Flat
            };
            _clearCacheButton.Click += OnClearCache;

            _exportButton = new Button
            {
                Text = "导出报告",
                Size = new Size(80, 30),
                Location = new Point(190, 5),
                FlatStyle = FlatStyle.Flat
            };
            _exportButton.Click += OnExportReport;

            _statusLabel = new Label
            {
                Text = "准备就绪",
                AutoSize = true,
                Location = new Point(300, 10),
                ForeColor = Color.DarkGreen
            };

            toolPanel.Controls.AddRange(new Control[] { 
                _refreshButton, _clearCacheButton, _exportButton, _statusLabel 
            });

            // 标签页控件
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 缓存统计标签页
            var statsTab = new TabPage("缓存统计");
            statsTab.Controls.Add(CreateStatisticsPanel());
            _tabControl.TabPages.Add(statsTab);

            // 性能历史标签页
            var perfTab = new TabPage("性能历史");
            perfTab.Controls.Add(CreatePerformancePanel());
            _tabControl.TabPages.Add(perfTab);

            // 实时图表标签页
            var chartTab = new TabPage("实时图表");
            chartTab.Controls.Add(CreateChartsPanel());
            _tabControl.TabPages.Add(chartTab);

            mainPanel.Controls.Add(toolPanel, 0, 0);
            mainPanel.Controls.Add(_tabControl, 0, 1);
            this.Controls.Add(mainPanel);

            this.ResumeLayout(false);
        }

        private Panel CreateStatisticsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            _statisticsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            _statisticsListView.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader { Text = "指标", Width = 200 },
                new ColumnHeader { Text = "数值", Width = 150 },
                new ColumnHeader { Text = "单位", Width = 100 },
                new ColumnHeader { Text = "说明", Width = 300 }
            });

            panel.Controls.Add(_statisticsListView);
            return panel;
        }

        private Panel CreatePerformancePanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            _performanceListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            _performanceListView.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader { Text = "时间", Width = 120 },
                new ColumnHeader { Text = "模板", Width = 150 },
                new ColumnHeader { Text = "渲染时间", Width = 100 },
                new ColumnHeader { Text = "数据大小", Width = 100 },
                new ColumnHeader { Text = "变量数", Width = 80 },
                new ColumnHeader { Text = "缓存命中", Width = 80 },
                new ColumnHeader { Text = "状态", Width = 100 }
            });

            panel.Controls.Add(_performanceListView);
            return panel;
        }

        private Panel CreateChartsPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            // 性能趋势图表
            _performanceChart = new Chart
            {
                Dock = DockStyle.Fill
            };

            var perfChartArea = new ChartArea("PerformanceArea")
            {
                AxisX = { Title = "时间", LabelStyle = { Format = "HH:mm:ss" } },
                AxisY = { Title = "渲染时间 (ms)" }
            };
            _performanceChart.ChartAreas.Add(perfChartArea);

            var perfSeries = new Series("渲染时间")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Blue,
                BorderWidth = 2
            };
            _performanceChart.Series.Add(perfSeries);

            var cacheSeries = new Series("缓存命中")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.Green,
                BorderWidth = 2,
                YAxisType = AxisType.Secondary
            };
            _performanceChart.Series.Add(cacheSeries);

            perfChartArea.AxisY2.Enabled = AxisEnabled.True;
            perfChartArea.AxisY2.Title = "缓存命中率 (%)";

            _performanceChart.Legends.Add(new Legend("Legend1"));

            // 缓存使用图表
            _cacheChart = new Chart
            {
                Dock = DockStyle.Fill
            };

            var cacheChartArea = new ChartArea("CacheArea")
            {
                AxisX = { Title = "缓存类型" },
                AxisY = { Title = "条目数量" }
            };
            _cacheChart.ChartAreas.Add(cacheChartArea);

            var cacheBarSeries = new Series("缓存分布")
            {
                ChartType = SeriesChartType.Column,
                Color = Color.Orange
            };
            _cacheChart.Series.Add(cacheBarSeries);

            var memoryPieSeries = new Series("内存使用")
            {
                ChartType = SeriesChartType.Pie,
                ChartArea = "CacheArea"
            };
            _cacheChart.Series.Add(memoryPieSeries);

            panel.Controls.Add(_performanceChart, 0, 0);
            panel.Controls.Add(_cacheChart, 0, 1);
            return panel;
        }

        private void InitializeTimer()
        {
            _refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000, // 5秒刷新一次
                Enabled = true
            };
            _refreshTimer.Tick += (s, e) => RefreshData();
        }

        private async void LoadInitialData()
        {
            await RefreshData();
        }

        private async Task RefreshData()
        {
            try
            {
                UpdateStatus("正在刷新数据...", Color.Blue);

                await Task.Run(() =>
                {
                    this.Invoke(new Action(() =>
                    {
                        UpdateStatistics();
                        UpdatePerformanceHistory();
                        UpdateCharts();
                    }));
                });

                UpdateStatus("数据已更新", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                UpdateStatus($"更新失败: {ex.Message}", Color.Red);
            }
        }

        private void UpdateStatistics()
        {
            var stats = TemplateManager.GetCacheStatistics();
            
            _statisticsListView.Items.Clear();

            var items = new[]
            {
                new { Name = "总缓存条目", Value = stats.TotalEntries.ToString(), Unit = "个", Desc = "当前缓存中的总条目数" },
                new { Name = "命中次数", Value = stats.HitCount.ToString(), Unit = "次", Desc = "缓存命中的总次数" },
                new { Name = "未命中次数", Value = stats.MissCount.ToString(), Unit = "次", Desc = "缓存未命中的总次数" },
                new { Name = "命中率", Value = $"{stats.HitRatio:P2}", Unit = "%", Desc = "缓存命中的百分比" },
                new { Name = "总请求数", Value = stats.TotalRequests.ToString(), Unit = "次", Desc = "总的模板请求次数" },
                new { Name = "内存使用", Value = $"{stats.TotalMemoryUsage / 1024.0 / 1024.0:F2}", Unit = "MB", Desc = "缓存占用的内存大小" },
                new { Name = "平均渲染时间", Value = $"{stats.AverageRenderTime.TotalMilliseconds:F2}", Unit = "ms", Desc = "模板渲染的平均耗时" },
                new { Name = "上次清理时间", Value = stats.LastCleanupTime.ToString("HH:mm:ss"), Unit = "", Desc = "最后一次缓存清理的时间" }
            };

            foreach (var item in items)
            {
                var listItem = new ListViewItem(new[] { item.Name, item.Value, item.Unit, item.Desc });
                _statisticsListView.Items.Add(listItem);
            }

            // 添加按类型分组的统计
            if (stats.EntriesByType.Any())
            {
                foreach (var kvp in stats.EntriesByType)
                {
                    var listItem = new ListViewItem(new[] { 
                        $"{kvp.Key}缓存", 
                        kvp.Value.ToString(), 
                        "个", 
                        $"{kvp.Key}类型的缓存条目数" 
                    });
                    _statisticsListView.Items.Add(listItem);
                }
            }
        }

        private void UpdatePerformanceHistory()
        {
            var history = TemplateManager.GetPerformanceHistory(50);
            
            _performanceListView.Items.Clear();

            foreach (var perf in history.OrderByDescending(p => p.Timestamp))
            {
                var status = perf.CacheHit ? "命中" : "未命中";
                var statusColor = perf.CacheHit ? Color.Green : Color.Orange;

                var item = new ListViewItem(new[]
                {
                    perf.Timestamp.ToString("HH:mm:ss"),
                    perf.TemplateKey,
                    $"{perf.RenderTime.TotalMilliseconds:F2}",
                    $"{perf.DataSize / 1024.0:F1} KB",
                    perf.VariableCount.ToString(),
                    perf.CacheHit ? "是" : "否",
                    status
                });

                if (perf.CacheHit)
                    item.BackColor = Color.LightGreen;
                else if (perf.RenderTime.TotalMilliseconds > 100)
                    item.BackColor = Color.LightYellow;

                _performanceListView.Items.Add(item);
            }
        }

        private void UpdateCharts()
        {
            UpdatePerformanceChart();
            UpdateCacheChart();
        }

        private void UpdatePerformanceChart()
        {
            var history = TemplateManager.GetPerformanceHistory(20);
            
            _performanceChart.Series["渲染时间"].Points.Clear();
            _performanceChart.Series["缓存命中"].Points.Clear();

            foreach (var perf in history.OrderBy(p => p.Timestamp))
            {
                var timePoint = perf.Timestamp.ToOADate();
                
                _performanceChart.Series["渲染时间"].Points.AddXY(timePoint, perf.RenderTime.TotalMilliseconds);
                _performanceChart.Series["缓存命中"].Points.AddXY(timePoint, perf.CacheHit ? 100 : 0);
            }

            _performanceChart.Invalidate();
        }

        private void UpdateCacheChart()
        {
            var stats = TemplateManager.GetCacheStatistics();
            
            _cacheChart.Series["缓存分布"].Points.Clear();

            foreach (var kvp in stats.EntriesByType)
            {
                _cacheChart.Series["缓存分布"].Points.AddXY(kvp.Key.ToString(), kvp.Value);
            }

            _cacheChart.Invalidate();
        }

        private void UpdateStatus(string message, Color color)
        {
            if (_statusLabel.InvokeRequired)
            {
                _statusLabel.Invoke(new Action(() =>
                {
                    _statusLabel.Text = message;
                    _statusLabel.ForeColor = color;
                }));
            }
            else
            {
                _statusLabel.Text = message;
                _statusLabel.ForeColor = color;
            }
        }

        private async void OnRefresh(object? sender, EventArgs e)
        {
            await RefreshData();
        }

        private void OnClearCache(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "确定要清空所有模板缓存吗？这将导致下次使用模板时需要重新编译。",
                "确认清空缓存",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    TemplateManager.ClearTemplateCache();
                    UpdateStatus("缓存已清空", Color.DarkGreen);
                    _ = RefreshData();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"清空缓存失败: {ex.Message}", Color.Red);
                    MessageBox.Show($"清空缓存失败: {ex.Message}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnExportReport(object? sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "文本文件|*.txt|CSV文件|*.csv",
                Title = "导出性能报告",
                FileName = $"性能报告_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ExportPerformanceReport(saveDialog.FileName);
                    UpdateStatus("报告已导出", Color.DarkGreen);
                    MessageBox.Show("性能报告导出成功！", "导出完成", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"导出失败: {ex.Message}", Color.Red);
                    MessageBox.Show($"导出报告失败: {ex.Message}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportPerformanceReport(string filePath)
        {
            var stats = TemplateManager.GetCacheStatistics();
            var history = TemplateManager.GetPerformanceHistory(200);

            using var writer = new System.IO.StreamWriter(filePath, false, System.Text.Encoding.UTF8);
            
            writer.WriteLine("模板性能监控报告");
            writer.WriteLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine(new string('-', 50));
            writer.WriteLine();

            // 统计信息
            writer.WriteLine("缓存统计信息:");
            writer.WriteLine($"总缓存条目: {stats.TotalEntries}");
            writer.WriteLine($"命中率: {stats.HitRatio:P2}");
            writer.WriteLine($"总请求数: {stats.TotalRequests}");
            writer.WriteLine($"内存使用: {stats.TotalMemoryUsage / 1024.0 / 1024.0:F2} MB");
            writer.WriteLine($"平均渲染时间: {stats.AverageRenderTime.TotalMilliseconds:F2} ms");
            writer.WriteLine();

            // 性能历史
            writer.WriteLine("性能历史记录:");
            writer.WriteLine("时间\t模板\t渲染时间(ms)\t数据大小\t变量数\t缓存命中");
            
            foreach (var perf in history.OrderByDescending(p => p.Timestamp))
            {
                writer.WriteLine($"{perf.Timestamp:HH:mm:ss}\t{perf.TemplateKey}\t" +
                    $"{perf.RenderTime.TotalMilliseconds:F2}\t{perf.DataSize}\t" +
                    $"{perf.VariableCount}\t{(perf.CacheHit ? "是" : "否")}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}