using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp1.Tests;

namespace WinFormsApp1.Forms
{
    public partial class TestRunnerForm : Form
    {
        private TreeView _testTreeView;
        private RichTextBox _resultsTextBox;
        private Button _runAllButton;
        private Button _runSelectedButton;
        private Button _exportReportButton;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private TabControl _tabControl;
        private ListView _summaryListView;
        private Label _overallResultLabel;

        private bool _isRunning = false;

        public TestRunnerForm()
        {
            InitializeComponent();
            InitializeTestData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体设置
            this.Text = "模板系统测试运行器";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Icon = SystemIcons.Application;

            // 创建主面板
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            // 工具栏
            var toolPanel = CreateToolPanel();
            
            // 主内容区域
            var contentPanel = CreateContentPanel();
            
            // 状态栏
            var statusPanel = CreateStatusPanel();

            mainPanel.Controls.Add(toolPanel, 0, 0);
            mainPanel.Controls.Add(contentPanel, 0, 1);
            mainPanel.Controls.Add(statusPanel, 0, 2);

            this.Controls.Add(mainPanel);
            this.ResumeLayout(false);
        }

        private Panel CreateToolPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 50
            };

            _runAllButton = new Button
            {
                Text = "🏃 运行所有测试",
                Size = new Size(120, 35),
                Location = new Point(10, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGreen
            };
            _runAllButton.Click += OnRunAllTests;

            _runSelectedButton = new Button
            {
                Text = "▶️ 运行选中测试",
                Size = new Size(120, 35),
                Location = new Point(140, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightBlue
            };
            _runSelectedButton.Click += OnRunSelectedTests;

            _exportReportButton = new Button
            {
                Text = "📄 导出报告",
                Size = new Size(100, 35),
                Location = new Point(270, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightYellow
            };
            _exportReportButton.Click += OnExportReport;

            _overallResultLabel = new Label
            {
                Text = "准备运行测试...",
                AutoSize = true,
                Location = new Point(400, 18),
                Font = new Font("微软雅黑", 12, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            panel.Controls.AddRange(new Control[] { 
                _runAllButton, _runSelectedButton, _exportReportButton, _overallResultLabel 
            });

            return panel;
        }

        private Panel CreateContentPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 250
            };

            // 上方: 测试树视图
            _testTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                Font = new Font("微软雅黑", 10)
            };
            _testTreeView.AfterCheck += OnTestTreeAfterCheck;

            splitter.Panel1.Controls.Add(_testTreeView);

            // 下方: 标签页控件
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 10)
            };

            // 结果详情标签页
            var resultsTab = new TabPage("测试结果详情");
            _resultsTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen
            };
            resultsTab.Controls.Add(_resultsTextBox);
            _tabControl.TabPages.Add(resultsTab);

            // 汇总信息标签页
            var summaryTab = new TabPage("测试汇总");
            _summaryListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _summaryListView.Columns.AddRange(new ColumnHeader[]
            {
                new ColumnHeader { Text = "测试套件", Width = 200 },
                new ColumnHeader { Text = "总数", Width = 80 },
                new ColumnHeader { Text = "通过", Width = 80 },
                new ColumnHeader { Text = "失败", Width = 80 },
                new ColumnHeader { Text = "跳过", Width = 80 },
                new ColumnHeader { Text = "成功率", Width = 100 },
                new ColumnHeader { Text = "状态", Width = 100 }
            });
            summaryTab.Controls.Add(_summaryListView);
            _tabControl.TabPages.Add(summaryTab);

            splitter.Panel2.Controls.Add(_tabControl);
            panel.Controls.Add(splitter);

            return panel;
        }

        private Panel CreateStatusPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 30
            };

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Style = ProgressBarStyle.Continuous
            };

            _statusLabel = new Label
            {
                Text = "就绪",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            panel.Controls.Add(_statusLabel);
            panel.Controls.Add(_progressBar);

            return panel;
        }

        private void InitializeTestData()
        {
            try
            {
                // TemplateSystemTests.InitializeTests(); // 此方法不存在，已移除
                LoadTestTree();
                UpdateStatus("测试数据加载完成", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                UpdateStatus($"加载测试数据失败: {ex.Message}", Color.Red);
                MessageBox.Show($"初始化测试失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTestTree()
        {
            _testTreeView.Nodes.Clear();

            var testSuiteNames = TemplateSystemTests.GetTestSuites();
            foreach (var suiteName in testSuiteNames)
            {
                var suiteNode = new TreeNode(suiteName)
                {
                    Tag = suiteName,
                    Checked = true
                };

                // 为每个测试套件创建默认测试用例
                var testNode = new TreeNode($"{suiteName} - 默认测试")
                {
                    Tag = suiteName + "_test",
                    Checked = true
                };
                suiteNode.Nodes.Add(testNode);

                _testTreeView.Nodes.Add(suiteNode);
            }

            _testTreeView.ExpandAll();
        }

        private void OnTestTreeAfterCheck(object? sender, TreeViewEventArgs e)
        {
            // 防止递归调用
            _testTreeView.AfterCheck -= OnTestTreeAfterCheck;

            try
            {
                if (e.Node?.Tag is TestSuite)
                {
                    // 套件节点: 更新所有子测试的选中状态
                    foreach (TreeNode childNode in e.Node.Nodes)
                    {
                        childNode.Checked = e.Node.Checked;
                    }
                }
                else if (e.Node?.Tag is TestCase)
                {
                    // 测试节点: 检查是否需要更新父节点
                    var parentNode = e.Node.Parent;
                    if (parentNode != null)
                    {
                        var checkedCount = parentNode.Nodes.Cast<TreeNode>().Count(n => n.Checked);
                        parentNode.Checked = checkedCount > 0;
                    }
                }
            }
            finally
            {
                _testTreeView.AfterCheck += OnTestTreeAfterCheck;
            }
        }

        private async void OnRunAllTests(object? sender, EventArgs e)
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;
                UpdateRunningState(true);
                
                // 选中所有测试
                foreach (TreeNode suiteNode in _testTreeView.Nodes)
                {
                    suiteNode.Checked = true;
                    foreach (TreeNode testNode in suiteNode.Nodes)
                    {
                        testNode.Checked = true;
                    }
                }

                await RunSelectedTests();
            }
            finally
            {
                _isRunning = false;
                UpdateRunningState(false);
            }
        }

        private async void OnRunSelectedTests(object? sender, EventArgs e)
        {
            if (_isRunning) return;

            try
            {
                _isRunning = true;
                UpdateRunningState(true);
                await RunSelectedTests();
            }
            finally
            {
                _isRunning = false;
                UpdateRunningState(false);
            }
        }

        private async Task RunSelectedTests()
        {
            var selectedSuites = _testTreeView.Nodes.Cast<TreeNode>()
                .Where(n => n.Checked && n.Tag is string)
                .Select(n => n.Tag as string)
                .ToList();

            if (!selectedSuites.Any())
            {
                MessageBox.Show("请至少选择一个测试套件", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _resultsTextBox.Clear();
            _summaryListView.Items.Clear();
            
            var totalTests = selectedSuites.Count;
            var completedTests = 0;

            _progressBar.Maximum = totalTests;
            _progressBar.Value = 0;

            AppendToResults("开始运行测试...\n", Color.Yellow);

            foreach (var suiteName in selectedSuites)
            {
                if (suiteName == null) continue;

                AppendToResults($"\n=== 运行测试套件: {suiteName} ===\n", Color.Cyan);

                try
                {
                    var templateTests = new TemplateSystemTests();
                    var results = await templateTests.RunTestSuiteAsync(suiteName);

                    // 更新UI
                    foreach (var result in results)
                    {
                        var color = result.Success ? Color.LightGreen : Color.LightCoral;
                        var status = result.Success ? "✅ 通过" : "❌ 失败";

                        AppendToResults($"  {result.TestName}: {status} ({result.Duration.TotalMilliseconds:F2}ms)\n", color);
                        
                        if (!result.Success && !string.IsNullOrEmpty(result.Message))
                        {
                            AppendToResults($"    错误信息: {result.Message}\n", Color.Red);
                        }

                        completedTests++;
                        _progressBar.Value = completedTests;
                        UpdateStatus($"运行测试 {completedTests}/{totalTests}: {result.TestName}", Color.Blue);
                        
                        // 强制刷新UI
                        Application.DoEvents();
                    }
                }
                catch (Exception ex)
                {
                    AppendToResults($"  测试套件执行失败: {ex.Message}\n", Color.Red);
                }

                UpdateSummaryView();
            }

            AppendToResults("\n测试运行完成!\n", Color.Yellow);
            UpdateOverallResult();
            UpdateStatus("测试运行完成", Color.DarkGreen);
        }

        private void UpdateSummaryView()
        {
            _summaryListView.Items.Clear();

            var testSuites = TemplateSystemTests.GetTestSuites();
            foreach (var suiteName in testSuites)
            {
                var item = new ListViewItem(suiteName);
                item.SubItems.Add("0"); // TestCases count
                item.SubItems.Add("0"); // PassedCount
                item.SubItems.Add("0"); // FailedCount
                item.SubItems.Add("0"); // SkippedCount
                item.SubItems.Add("0%"); // SuccessRate

                var status = "⏭️ 未运行";
                item.SubItems.Add(status);

                _summaryListView.Items.Add(item);
            }
        }

        private void UpdateOverallResult()
        {
            var testSuites = TemplateSystemTests.GetTestSuites();
            var totalTests = testSuites.Count;
            var totalPassed = 0;
            var totalFailed = 0;
            var overallSuccessRate = 0.0;

            var resultText = $"总计: {totalPassed}/{totalTests} 通过 ({overallSuccessRate:P1})";
            var resultColor = totalFailed == 0 ? Color.DarkGreen : Color.DarkRed;

            _overallResultLabel.Text = resultText;
            _overallResultLabel.ForeColor = resultColor;
        }

        private void OnExportReport(object? sender, EventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Markdown文件|*.md|文本文件|*.txt",
                    Title = "导出测试报告",
                    FileName = $"测试报告_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var report = TemplateSystemTests.GenerateTestReport(new List<TestResult>());
                    File.WriteAllText(saveDialog.FileName, report, System.Text.Encoding.UTF8);
                    
                    UpdateStatus("测试报告导出成功", Color.DarkGreen);
                    MessageBox.Show("测试报告导出成功！", "导出完成", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"导出失败: {ex.Message}", Color.Red);
                MessageBox.Show($"导出报告失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateRunningState(bool isRunning)
        {
            _runAllButton.Enabled = !isRunning;
            _runSelectedButton.Enabled = !isRunning;
            _exportReportButton.Enabled = !isRunning;
            _testTreeView.Enabled = !isRunning;

            if (!isRunning)
            {
                _progressBar.Value = 0;
            }
        }

        private void AppendToResults(string text, Color color)
        {
            if (_resultsTextBox.InvokeRequired)
            {
                _resultsTextBox.Invoke(new Action(() => AppendToResults(text, color)));
                return;
            }

            _resultsTextBox.SelectionStart = _resultsTextBox.TextLength;
            _resultsTextBox.SelectionLength = 0;
            _resultsTextBox.SelectionColor = color;
            _resultsTextBox.AppendText(text);
            _resultsTextBox.ScrollToCaret();
        }

        private void UpdateStatus(string message, Color color)
        {
            if (_statusLabel.InvokeRequired)
            {
                _statusLabel.Invoke(new Action(() => UpdateStatus(message, color)));
                return;
            }

            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;
        }
    }
}