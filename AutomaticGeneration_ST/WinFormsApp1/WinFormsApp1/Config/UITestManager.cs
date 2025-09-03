////--NEED DELETE: UI测试/演示辅助（非核心）
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;

//namespace WinFormsApp1.Config
//{
//    public static class UITestManager
//    {
//        public class UITestResult
//        {
//            public bool IsSuccessful { get; set; }
//            public string TestName { get; set; } = "";
//            public string Message { get; set; } = "";
//            public Exception? Exception { get; set; }
//        }

//        public class UITestSuite
//        {
//            public List<UITestResult> Results { get; set; } = new();
//            public DateTime ExecutionTime { get; set; }
//            public TimeSpan Duration { get; set; }
//            public int TotalTests => Results.Count;
//            public int PassedTests => Results.Count(r => r.IsSuccessful);
//            public int FailedTests => Results.Count(r => !r.IsSuccessful);
//            public bool AllTestsPassed => FailedTests == 0;
//        }

//        public static UITestSuite RunUIStabilityTests(Form mainForm)
//        {
//            var testSuite = new UITestSuite
//            {
//                ExecutionTime = DateTime.Now
//            };

//            var startTime = DateTime.Now;

//            try
//            {
//                // 基础控件存在性测试
//                testSuite.Results.Add(TestControlsExistence(mainForm));
                
//                // 主题切换测试
//                testSuite.Results.Add(TestThemeSwitching(mainForm));
                
//                // 响应式布局测试
//                testSuite.Results.Add(TestResponsiveLayout(mainForm));
                
//                // 快捷键系统测试
//                testSuite.Results.Add(TestKeyboardShortcuts(mainForm));
                
//                // 提示系统测试
//                testSuite.Results.Add(TestTooltipSystem(mainForm));
                
//                // 窗口状态管理测试
//                testSuite.Results.Add(TestWindowStateManagement(mainForm));
                
//                // 控件样式一致性测试
//                testSuite.Results.Add(TestControlStyleConsistency(mainForm));
                
//                // 菜单系统测试
//                testSuite.Results.Add(TestMenuSystem(mainForm));
                
//                // 分割容器功能测试
//                testSuite.Results.Add(TestSplitContainers(mainForm));
                
//                // 内存泄漏基本检测
//                testSuite.Results.Add(TestBasicMemoryUsage());
//            }
//            catch (Exception ex)
//            {
//                testSuite.Results.Add(new UITestResult
//                {
//                    IsSuccessful = false,
//                    TestName = "整体测试执行",
//                    Message = $"测试执行过程中发生异常: {ex.Message}",
//                    Exception = ex
//                });
//            }

//            testSuite.Duration = DateTime.Now - startTime;
//            return testSuite;
//        }

//        private static UITestResult TestControlsExistence(Form form)
//        {
//            var result = new UITestResult { TestName = "基础控件存在性检查" };

//            try
//            {
//                var requiredControls = new[]
//                {
//                    "mainMenuStrip", "mainToolStrip", "mainStatusStrip",
//                    "mainSplitContainer", "rightSplitContainer",
//                    "button_upload", "button_export",
//                    "richTextBox1", "previewTabControl"
//                };

//                var missingControls = new List<string>();
//                foreach (var controlName in requiredControls)
//                {
//                    var control = FindControlByName(form, controlName);
//                    if (control == null)
//                    {
//                        missingControls.Add(controlName);
//                    }
//                }

//                if (missingControls.Any())
//                {
//                    result.IsSuccessful = false;
//                    result.Message = $"缺少关键控件: {string.Join(", ", missingControls)}";
//                }
//                else
//                {
//                    result.IsSuccessful = true;
//                    result.Message = "所有关键控件都存在";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"控件检查失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestThemeSwitching(Form form)
//        {
//            var result = new UITestResult { TestName = "主题切换功能测试" };

//            try
//            {
//                var originalTheme = ThemeManager.CurrentTheme;

//                // 测试切换到深色主题
//                ThemeManager.SetTheme(ThemeType.Dark);
//                Application.DoEvents();

//                // 检查背景色是否改变
//                var isDarkApplied = form.BackColor != Color.White;

//                // 测试切换到浅色主题
//                ThemeManager.SetTheme(ThemeType.Light);
//                Application.DoEvents();

//                // 检查背景色是否恢复
//                var isLightApplied = form.BackColor == Color.White ||
//                                   form.BackColor == SystemColors.Control;

//                // 恢复原始主题
//                ThemeManager.SetTheme(originalTheme);

//                if (isDarkApplied && isLightApplied)
//                {
//                    result.IsSuccessful = true;
//                    result.Message = "主题切换功能正常";
//                }
//                else
//                {
//                    result.IsSuccessful = false;
//                    result.Message = $"主题切换异常 - 深色主题: {isDarkApplied}, 浅色主题: {isLightApplied}";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"主题切换测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestResponsiveLayout(Form form)
//        {
//            var result = new UITestResult { TestName = "响应式布局测试" };

//            try
//            {
//                var originalSize = form.Size;
//                var originalLocation = form.Location;

//                // 测试窗体大小调整
//                form.Size = new Size(800, 600);
//                Application.DoEvents();
                
//                form.Size = new Size(1400, 900);
//                Application.DoEvents();
                
//                // 恢复原始大小
//                form.Size = originalSize;
//                form.Location = originalLocation;
//                Application.DoEvents();

//                // 检查分割容器是否正常工作
//                var mainSplitter = FindControlByName(form, "mainSplitContainer") as SplitContainer;
//                var rightSplitter = FindControlByName(form, "rightSplitContainer") as SplitContainer;

//                bool splittersWork = true;
//                if (mainSplitter != null)
//                {
//                    var originalDistance = mainSplitter.SplitterDistance;
//                    mainSplitter.SplitterDistance = 200;
//                    mainSplitter.SplitterDistance = originalDistance;
//                }
//                else
//                {
//                    splittersWork = false;
//                }

//                result.IsSuccessful = splittersWork;
//                result.Message = splittersWork ? "响应式布局正常" : "分割容器功能异常";
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"响应式布局测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestKeyboardShortcuts(Form form)
//        {
//            var result = new UITestResult { TestName = "快捷键系统测试" };

//            try
//            {
//                // 检查快捷键是否已注册
//                var isKeyPreviewEnabled = form.KeyPreview;
//                var shortcutsCount = KeyboardShortcutManager.Shortcuts.Count;

//                if (isKeyPreviewEnabled && shortcutsCount > 0)
//                {
//                    result.IsSuccessful = true;
//                    result.Message = $"快捷键系统正常，已注册 {shortcutsCount} 个快捷键";
//                }
//                else
//                {
//                    result.IsSuccessful = false;
//                    result.Message = $"快捷键系统异常 - KeyPreview: {isKeyPreviewEnabled}, 快捷键数量: {shortcutsCount}";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"快捷键系统测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestTooltipSystem(Form form)
//        {
//            var result = new UITestResult { TestName = "提示系统测试" };

//            try
//            {
//                // 测试是否能为控件设置提示
//                var testButton = FindControlByName(form, "button_upload");
//                if (testButton != null)
//                {
//                    TooltipManager.SetCustomTooltip(testButton, "测试提示");
//                    TooltipManager.RemoveTooltip(testButton);
                    
//                    result.IsSuccessful = true;
//                    result.Message = "提示系统功能正常";
//                }
//                else
//                {
//                    result.IsSuccessful = false;
//                    result.Message = "找不到测试控件";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"提示系统测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestWindowStateManagement(Form form)
//        {
//            var result = new UITestResult { TestName = "窗口状态管理测试" };

//            try
//            {
//                // 测试窗口设置的保存和加载
//                var settings = WindowSettings.Load();
//                var testSettings = new WindowSettings();
//                testSettings.Save();

//                result.IsSuccessful = true;
//                result.Message = "窗口状态管理正常";
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"窗口状态管理测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestControlStyleConsistency(Form form)
//        {
//            var result = new UITestResult { TestName = "控件样式一致性测试" };

//            try
//            {
//                var buttons = GetAllControlsOfType<Button>(form);
//                var inconsistentButtons = 0;

//                foreach (var button in buttons)
//                {
//                    // 检查基本样式属性
//                    if (button.Font == null || button.Font.Name != ControlStyleManager.DefaultFont.Name)
//                    {
//                        inconsistentButtons++;
//                    }
//                }

//                if (inconsistentButtons == 0)
//                {
//                    result.IsSuccessful = true;
//                    result.Message = $"控件样式一致，检查了 {buttons.Count} 个按钮";
//                }
//                else
//                {
//                    result.IsSuccessful = false;
//                    result.Message = $"发现 {inconsistentButtons} 个样式不一致的按钮";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"控件样式一致性测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestMenuSystem(Form form)
//        {
//            var result = new UITestResult { TestName = "菜单系统测试" };

//            try
//            {
//                var menuStrip = FindControlByName(form, "mainMenuStrip") as MenuStrip;
//                if (menuStrip != null && menuStrip.Items.Count > 0)
//                {
//                    var menuItemsCount = CountMenuItems(menuStrip.Items);
//                    result.IsSuccessful = true;
//                    result.Message = $"菜单系统正常，包含 {menuItemsCount} 个菜单项";
//                }
//                else
//                {
//                    result.IsSuccessful = false;
//                    result.Message = "菜单系统异常或菜单项为空";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"菜单系统测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestSplitContainers(Form form)
//        {
//            var result = new UITestResult { TestName = "分割容器功能测试" };

//            try
//            {
//                var splitContainers = GetAllControlsOfType<SplitContainer>(form);
//                var workingSplitters = 0;

//                foreach (var splitter in splitContainers)
//                {
//                    if (splitter.Panel1 != null && splitter.Panel2 != null)
//                    {
//                        workingSplitters++;
//                    }
//                }

//                if (workingSplitters == splitContainers.Count && splitContainers.Count > 0)
//                {
//                    result.IsSuccessful = true;
//                    result.Message = $"所有 {splitContainers.Count} 个分割容器功能正常";
//                }
//                else
//                {
//                    result.IsSuccessful = false;
//                    result.Message = $"分割容器异常 - 总数: {splitContainers.Count}, 正常: {workingSplitters}";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"分割容器测试失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static UITestResult TestBasicMemoryUsage()
//        {
//            var result = new UITestResult { TestName = "基础内存使用检测" };

//            try
//            {
//                GC.Collect();
//                GC.WaitForPendingFinalizers();
//                GC.Collect();

//                var memoryUsage = GC.GetTotalMemory(false);
//                var memoryMB = memoryUsage / (1024.0 * 1024.0);

//                // 简单的内存使用检查（阈值可根据实际情况调整）
//                if (memoryMB < 100) // 100MB
//                {
//                    result.IsSuccessful = true;
//                    result.Message = $"内存使用正常: {memoryMB:F2} MB";
//                }
//                else
//                {
//                    result.IsSuccessful = false;
//                    result.Message = $"内存使用偏高: {memoryMB:F2} MB";
//                }
//            }
//            catch (Exception ex)
//            {
//                result.IsSuccessful = false;
//                result.Message = $"内存检测失败: {ex.Message}";
//                result.Exception = ex;
//            }

//            return result;
//        }

//        private static Control? FindControlByName(Control parent, string name)
//        {
//            if (parent.Name == name)
//                return parent;

//            foreach (Control child in parent.Controls)
//            {
//                var found = FindControlByName(child, name);
//                if (found != null)
//                    return found;
//            }

//            return null;
//        }

//        private static List<T> GetAllControlsOfType<T>(Control parent) where T : Control
//        {
//            var controls = new List<T>();
            
//            if (parent is T targetControl)
//            {
//                controls.Add(targetControl);
//            }

//            foreach (Control child in parent.Controls)
//            {
//                controls.AddRange(GetAllControlsOfType<T>(child));
//            }

//            return controls;
//        }

//        private static int CountMenuItems(ToolStripItemCollection items)
//        {
//            int count = 0;
//            foreach (ToolStripItem item in items)
//            {
//                count++;
//                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
//                {
//                    count += CountMenuItems(menuItem.DropDownItems);
//                }
//            }
//            return count;
//        }

//        public static string GenerateTestReport(UITestSuite testSuite)
//        {
//            var report = new StringBuilder();
            
//            report.AppendLine("=== UI稳定性测试报告 ===");
//            report.AppendLine($"执行时间: {testSuite.ExecutionTime:yyyy-MM-dd HH:mm:ss}");
//            report.AppendLine($"测试耗时: {testSuite.Duration.TotalMilliseconds:F0} ms");
//            report.AppendLine($"总测试数: {testSuite.TotalTests}");
//            report.AppendLine($"通过测试: {testSuite.PassedTests}");
//            report.AppendLine($"失败测试: {testSuite.FailedTests}");
//            report.AppendLine($"成功率: {(testSuite.PassedTests * 100.0 / testSuite.TotalTests):F1}%");
//            report.AppendLine();

//            foreach (var result in testSuite.Results)
//            {
//                var status = result.IsSuccessful ? "✅ 通过" : "❌ 失败";
//                report.AppendLine($"{status} - {result.TestName}");
//                report.AppendLine($"   {result.Message}");
                
//                if (result.Exception != null)
//                {
//                    report.AppendLine($"   异常: {result.Exception.GetType().Name}: {result.Exception.Message}");
//                }
                
//                report.AppendLine();
//            }

//            if (testSuite.AllTestsPassed)
//            {
//                report.AppendLine("🎉 所有测试通过！UI界面稳定性良好。");
//            }
//            else
//            {
//                report.AppendLine("⚠️ 部分测试失败，建议检查相关功能。");
//            }

//            return report.ToString();
//        }
//    }
//}
