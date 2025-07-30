using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1.Config
{
    public static class TooltipManager
    {
        private static ToolTip? mainTooltip;
        private static readonly Dictionary<Control, string> tooltipTexts = new();
        
        // 预定义的提示文本
        private static readonly Dictionary<string, string> DefaultTooltips = new()
        {
            // 按钮提示
            { "button_upload", "上传和处理Excel点表文件\n支持.xlsx和.xls格式\n快捷键: Ctrl+O" },
            { "button_export", "导出生成的ST脚本代码\n按AI/AO/DI/DO分类保存\n快捷键: Ctrl+S" },
            { "settingsToolButton", "打开应用程序设置\n配置模板、主题和其他选项\n快捷键: Ctrl+," },
            { "helpToolButton", "显示帮助信息\n查看快捷键和使用说明\n快捷键: F1" },
            { "openToolButton", "打开点表文件\n支持拖拽文件到窗口\n快捷键: Ctrl+O" },
            { "exportToolButton", "导出ST脚本\n生成的代码按类型分类\n快捷键: Ctrl+S" },
            { "regenerateToolButton", "重新生成代码\n基于当前数据重新生成\n快捷键: F5" },
            { "clearLogToolButton", "清空日志\n清除所有日志信息\n快捷键: Ctrl+L" },
            
            // 搜索和过滤
            { "logSearchBox", "搜索日志内容\n输入关键词快速查找日志\n支持实时搜索" },
            { "logFilterComboBox", "过滤日志级别\n选择要显示的日志类型\n包含信息、警告、错误等" },
            { "clearLogButton", "清空日志\n清除当前所有日志记录\n快捷键: Ctrl+L" },
            
            // 面板区域
            { "leftPanel", "文件管理区域\n显示最近处理的文件\n支持拖拽上传文件" },
            { "fileListBox", "最近文件列表\n显示最近处理的文件\n双击可重新打开文件" },
            { "previewTabControl", "预览和信息区域\n查看生成的代码和统计信息\n支持多标签页显示" },
            { "richTextBox1", "日志输出区域\n显示处理过程和错误信息\n支持文本搜索和过滤" },
            
            // 标签页
            { "codePreviewTab", "代码预览\n显示生成的ST脚本代码\n可复制和保存内容" },
            { "statisticsTab", "统计信息\n显示点位数量和类型统计\n包含文件处理状态" },
            { "fileInfoTab", "文件信息\n显示当前文件的详细信息\n包含文件大小和修改时间" },
            { "templateInfoTab", "模板信息\n显示当前使用的模板版本\n包含模板特性说明" },
            
            // 分割容器
            { "mainSplitContainer", "主分割容器\n拖拽调整左右面板大小\n位置会自动保存" },
            { "rightSplitContainer", "右侧分割容器\n拖拽调整上下面板大小\n位置会自动保存" }
        };
        
        // 上下文相关的提示
        private static readonly Dictionary<string, Dictionary<string, string>> ContextualTooltips = new()
        {
            ["processing"] = new()
            {
                { "button_upload", "正在处理文件，请稍候..." },
                { "button_export", "请等待文件处理完成" }
            },
            ["empty"] = new()
            {
                { "button_export", "请先上传点表文件\n然后生成代码后导出" },
                { "previewTabControl", "暂无内容\n请上传Excel文件开始处理" }
            },
            ["hasData"] = new()
            {
                { "regenerateToolButton", "重新生成当前文件的ST代码\n将覆盖之前生成的结果" }
            }
        };
        
        public static void Initialize(Form parentForm)
        {
            if (mainTooltip != null)
            {
                mainTooltip.Dispose();
            }
            
            mainTooltip = new ToolTip
            {
                AutoPopDelay = 5000,  // 5秒后自动消失
                InitialDelay = 800,   // 0.8秒后显示
                ReshowDelay = 500,    // 0.5秒后重新显示
                ShowAlways = true,    // 即使控件不活跃也显示
                IsBalloon = false,    // 使用矩形提示框
                UseAnimation = true,  // 使用动画效果
                UseFading = true      // 使用淡入淡出效果
            };
            
            // 应用主题颜色
            ApplyThemeToTooltip();
            
            // 为窗体所有控件设置提示
            SetupTooltips(parentForm);
        }
        
        public static void ApplyThemeToTooltip()
        {
            if (mainTooltip == null) return;
            
            // 根据当前主题设置提示框颜色
            if (ThemeManager.CurrentTheme == ThemeType.Dark)
            {
                mainTooltip.BackColor = ThemeManager.GetSurfaceColor();
                mainTooltip.ForeColor = ThemeManager.GetTextColor();
            }
            else
            {
                mainTooltip.BackColor = Color.FromArgb(255, 255, 225); // 浅黄色
                mainTooltip.ForeColor = Color.Black;
            }
        }
        
        public static void SetupTooltips(Control parent)
        {
            if (mainTooltip == null) return;
            
            SetTooltipForControl(parent);
            
            // 递归设置子控件的提示
            foreach (Control child in parent.Controls)
            {
                SetupTooltips(child);
            }
        }
        
        private static void SetTooltipForControl(Control control)
        {
            if (mainTooltip == null || control == null) return;
            
            var tooltipText = GetTooltipText(control);
            if (!string.IsNullOrEmpty(tooltipText))
            {
                mainTooltip.SetToolTip(control, tooltipText);
                tooltipTexts[control] = tooltipText;
            }
        }
        
        private static string GetTooltipText(Control control)
        {
            // 优先使用控件名称匹配
            if (!string.IsNullOrEmpty(control.Name) && DefaultTooltips.ContainsKey(control.Name))
            {
                return DefaultTooltips[control.Name];
            }
            
            // 根据控件类型提供通用提示
            return control switch
            {
                Button btn => GetButtonTooltip(btn),
                TextBox txt => GetTextBoxTooltip(txt),
                ComboBox cmb => GetComboBoxTooltip(cmb),
                ListBox lst => GetListBoxTooltip(lst),
                TabControl tab => GetTabControlTooltip(tab),
                SplitContainer split => GetSplitContainerTooltip(split),
                MenuStrip menu => "应用程序主菜单\n包含文件、编辑、视图等操作",
                StatusStrip status => "状态栏\n显示当前操作状态和进度",
                ToolStrip tool => "工具栏\n提供常用功能的快速访问",
                _ => ""
            };
        }
        
        private static string GetButtonTooltip(Button button)
        {
            var text = button.Text.ToLower();
            
            if (text.Contains("上传") || text.Contains("打开"))
                return "上传Excel点表文件\n支持拖拽操作";
            if (text.Contains("导出") || text.Contains("保存"))
                return "导出生成的ST脚本\n按类型分类保存";
            if (text.Contains("清空") || text.Contains("清除"))
                return "清空当前内容";
            if (text.Contains("刷新") || text.Contains("重新"))
                return "刷新或重新处理数据";
            if (text.Contains("设置") || text.Contains("配置"))
                return "打开应用程序设置";
            if (text.Contains("帮助"))
                return "显示帮助信息";
                
            return $"执行{button.Text}操作";
        }
        
        private static string GetTextBoxTooltip(TextBox textBox)
        {
            if (textBox.Multiline)
                return "多行文本输入框\n支持复制、粘贴等操作";
            
            var name = textBox.Name?.ToLower() ?? "";
            if (name.Contains("search"))
                return "搜索框\n输入关键词进行搜索";
            if (name.Contains("filter"))
                return "过滤框\n输入条件进行过滤";
                
            return "文本输入框\n输入相关信息";
        }
        
        private static string GetComboBoxTooltip(ComboBox comboBox)
        {
            var name = comboBox.Name?.ToLower() ?? "";
            if (name.Contains("filter"))
                return "过滤选项\n选择要显示的内容类型";
            if (name.Contains("theme"))
                return "主题选择\n切换应用程序外观";
                
            return "下拉选择框\n点击选择选项";
        }
        
        private static string GetListBoxTooltip(ListBox listBox)
        {
            var name = listBox.Name?.ToLower() ?? "";
            if (name.Contains("file"))
                return "文件列表\n显示最近处理的文件\n双击可重新打开";
                
            return "列表框\n显示可选择的项目";
        }
        
        private static string GetTabControlTooltip(TabControl tabControl)
        {
            return "标签页控制器\n点击标签页查看不同内容\n支持键盘导航";
        }
        
        private static string GetSplitContainerTooltip(SplitContainer splitContainer)
        {
            return "分割面板\n拖拽分割线调整面板大小\n位置设置会自动保存";
        }
        
        public static void UpdateContextualTooltips(string context)
        {
            if (mainTooltip == null || !ContextualTooltips.ContainsKey(context))
                return;
            
            var contextTooltips = ContextualTooltips[context];
            
            foreach (var kvp in tooltipTexts.ToArray())
            {
                var control = kvp.Key;
                var controlName = control.Name;
                
                if (!string.IsNullOrEmpty(controlName) && contextTooltips.ContainsKey(controlName))
                {
                    // 使用上下文相关的提示
                    mainTooltip.SetToolTip(control, contextTooltips[controlName]);
                }
                else
                {
                    // 恢复默认提示
                    var defaultText = GetTooltipText(control);
                    if (!string.IsNullOrEmpty(defaultText))
                    {
                        mainTooltip.SetToolTip(control, defaultText);
                    }
                }
            }
        }
        
        public static void SetCustomTooltip(Control control, string text)
        {
            if (mainTooltip == null || control == null) return;
            
            mainTooltip.SetToolTip(control, text);
            tooltipTexts[control] = text;
        }
        
        public static void RemoveTooltip(Control control)
        {
            if (mainTooltip == null || control == null) return;
            
            mainTooltip.SetToolTip(control, "");
            tooltipTexts.Remove(control);
        }
        
        public static void ShowTooltip(string text, Control control, int duration = 3000)
        {
            if (mainTooltip == null || control == null) return;
            
            var point = new Point(control.Width / 2, control.Height + 5);
            mainTooltip.Show(text, control, point, duration);
        }
        
        public static void HideTooltip(Control control)
        {
            if (mainTooltip == null || control == null) return;
            
            mainTooltip.Hide(control);
        }
        
        public static void Dispose()
        {
            if (mainTooltip != null)
            {
                mainTooltip.Dispose();
                mainTooltip = null;
            }
            tooltipTexts.Clear();
        }
        
        // 为快捷键添加特殊提示
        public static void AddShortcutTooltips(Form form)
        {
            if (mainTooltip == null) return;
            
            var shortcuts = KeyboardShortcutManager.GetAllShortcuts();
            
            foreach (Control control in GetAllControls(form))
            {
                var currentTooltip = tooltipTexts.ContainsKey(control) ? tooltipTexts[control] : "";
                var shortcutText = FindRelatedShortcut(control, shortcuts);
                
                if (!string.IsNullOrEmpty(shortcutText) && !currentTooltip.Contains("快捷键:"))
                {
                    var newTooltip = string.IsNullOrEmpty(currentTooltip) 
                        ? $"快捷键: {shortcutText}" 
                        : $"{currentTooltip}\n快捷键: {shortcutText}";
                    
                    mainTooltip.SetToolTip(control, newTooltip);
                    tooltipTexts[control] = newTooltip;
                }
            }
        }
        
        private static string FindRelatedShortcut(Control control, List<(string Name, string Description, string Keys)> shortcuts)
        {
            var controlName = control.Name?.ToLower() ?? "";
            var controlText = control.Text?.ToLower() ?? "";
            
            foreach (var shortcut in shortcuts)
            {
                var desc = shortcut.Description.ToLower();
                
                if ((controlName.Contains("upload") || controlText.Contains("上传")) && desc.Contains("打开"))
                    return shortcut.Keys;
                if ((controlName.Contains("export") || controlText.Contains("导出")) && desc.Contains("导出"))
                    return shortcut.Keys;
                if ((controlName.Contains("setting") || controlText.Contains("设置")) && desc.Contains("设置"))
                    return shortcut.Keys;
                if ((controlName.Contains("help") || controlText.Contains("帮助")) && desc.Contains("帮助"))
                    return shortcut.Keys;
            }
            
            return "";
        }
        
        private static IEnumerable<Control> GetAllControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                yield return control;
                foreach (Control child in GetAllControls(control))
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// 刷新窗体的工具提示设置
        /// </summary>
        public static void RefreshTooltips(Form form)
        {
            try
            {
                if (form != null)
                {
                    AddShortcutTooltips(form);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新工具提示时出错: {ex.Message}");
            }
        }
    }
}