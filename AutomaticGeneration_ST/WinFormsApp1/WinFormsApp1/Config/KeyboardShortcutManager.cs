//NEED DELETE: 快捷键管理（UI交互增强），非核心
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WinFormsApp1.Config
{
    public static class KeyboardShortcutManager
    {
        // 快捷键定义
        public static readonly Dictionary<string, Keys> Shortcuts = new()
        {
            // 文件操作
            { "OpenFile", Keys.Control | Keys.O },
            { "ExportResults", Keys.Control | Keys.S },
            { "Exit", Keys.Alt | Keys.F4 },
            
            // 编辑操作
            { "Copy", Keys.Control | Keys.C },
            { "SelectAll", Keys.Control | Keys.A },
            { "Find", Keys.Control | Keys.F },
            
            // 视图操作
            { "Refresh", Keys.F5 },
            { "ClearLog", Keys.Control | Keys.L },
            { "ToggleTheme", Keys.Control | Keys.T },
            { "FullScreen", Keys.F11 },
            
            // 工具操作
            { "Settings", Keys.Control | Keys.Oemcomma },
            { "Help", Keys.F1 },
            { "About", Keys.Control | Keys.Shift | Keys.A },
            
            // 导航操作
            { "FocusFileList", Keys.Control | Keys.D1 },
            { "FocusPreview", Keys.Control | Keys.D2 },
            { "FocusLog", Keys.Control | Keys.D3 },
            
            // 调试操作
            { "ShowDebugInfo", Keys.Control | Keys.Shift | Keys.D },
            { "ReloadConfig", Keys.Control | Keys.R },
            { "RunUITests", Keys.Control | Keys.Shift | Keys.T }
        };
        
        // 快捷键描述
        public static readonly Dictionary<string, string> ShortcutDescriptions = new()
        {
            { "OpenFile", "打开点表文件" },
            { "ExportResults", "导出ST脚本" },
            { "Exit", "退出应用程序" },
            { "Copy", "复制选中内容" },
            { "SelectAll", "全选内容" },
            { "Find", "查找内容" },
            { "Refresh", "刷新/重新生成" },
            { "ClearLog", "清空日志" },
            { "ToggleTheme", "切换主题" },
            { "FullScreen", "全屏显示" },
            { "Settings", "打开设置" },
            { "Help", "显示帮助" },
            { "About", "关于软件" },
            { "FocusFileList", "聚焦到文件列表" },
            { "FocusPreview", "聚焦到预览区域" },
            { "FocusLog", "聚焦到日志区域" },
            { "ShowDebugInfo", "显示调试信息" },
            { "ReloadConfig", "重新加载配置" },
            { "RunUITests", "运行UI稳定性测试" }
        };
        
        // 快捷键事件委托
        public delegate void ShortcutEventHandler(string shortcutName);
        
        // 快捷键事件
        public static event ShortcutEventHandler? ShortcutPressed;
        
        // 注册快捷键到窗体
        public static void RegisterShortcuts(Form form)
        {
            if (form == null) return;
            
            // 设置窗体接收键盘事件
            form.KeyPreview = true;
            
            // 订阅键盘事件
            form.KeyDown += OnKeyDown;
            
            // 为菜单项设置快捷键
            SetupMenuShortcuts(form);
        }
        
        // 注销快捷键
        public static void UnregisterShortcuts(Form form)
        {
            if (form == null) return;
            
            form.KeyDown -= OnKeyDown;
        }
        
        // 键盘事件处理
        private static void OnKeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                var keyPressed = e.KeyData;
                
                // 查找匹配的快捷键
                foreach (var shortcut in Shortcuts)
                {
                    if (shortcut.Value == keyPressed)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        
                        // 触发快捷键事件
                        ShortcutPressed?.Invoke(shortcut.Key);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                System.Diagnostics.Debug.WriteLine($"快捷键处理错误: {ex.Message}");
            }
        }
        
        // 为菜单项设置快捷键显示
        private static void SetupMenuShortcuts(Form form)
        {
            try
            {
                if (form.MainMenuStrip != null)
                {
                    SetMenuItemShortcuts(form.MainMenuStrip.Items);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置菜单快捷键错误: {ex.Message}");
            }
        }
        
        // 递归设置菜单项快捷键
        private static void SetMenuItemShortcuts(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    // 根据菜单项名称设置快捷键
                    SetMenuItemShortcut(menuItem);
                    
                    // 递归处理子菜单
                    if (menuItem.HasDropDownItems)
                    {
                        SetMenuItemShortcuts(menuItem.DropDownItems);
                    }
                }
            }
        }
        
        // 为单个菜单项设置快捷键
        private static void SetMenuItemShortcut(ToolStripMenuItem menuItem)
        {
            var text = menuItem.Text.ToLower();
            
            // 根据菜单项文本匹配快捷键
            if (text.Contains("打开") || text.Contains("open"))
            {
                menuItem.ShortcutKeys = Shortcuts["OpenFile"];
                menuItem.ShowShortcutKeys = true;
            }
            else if (text.Contains("导出") || text.Contains("保存") || text.Contains("export") || text.Contains("save"))
            {
                menuItem.ShortcutKeys = Shortcuts["ExportResults"];
                menuItem.ShowShortcutKeys = true;
            }
            else if (text.Contains("退出") || text.Contains("exit"))
            {
                menuItem.ShortcutKeys = Shortcuts["Exit"];
                menuItem.ShowShortcutKeys = true;
            }
            else if (text.Contains("设置") || text.Contains("preferences"))
            {
                menuItem.ShortcutKeys = Shortcuts["Settings"];
                menuItem.ShowShortcutKeys = true;
            }
            else if (text.Contains("帮助") || text.Contains("help"))
            {
                menuItem.ShortcutKeys = Shortcuts["Help"];
                menuItem.ShowShortcutKeys = true;
            }
            else if (text.Contains("关于") || text.Contains("about"))
            {
                menuItem.ShortcutKeys = Shortcuts["About"];
                menuItem.ShowShortcutKeys = true;
            }
            else if (text.Contains("刷新") || text.Contains("refresh"))
            {
                menuItem.ShortcutKeys = Shortcuts["Refresh"];
                menuItem.ShowShortcutKeys = true;
            }
            else if (text.Contains("清空") && text.Contains("日志"))
            {
                menuItem.ShortcutKeys = Shortcuts["ClearLog"];
                menuItem.ShowShortcutKeys = true;
            }
        }
        
        // 获取快捷键的友好显示文本
        public static string GetShortcutDisplayText(string shortcutName)
        {
            if (!Shortcuts.ContainsKey(shortcutName))
                return "";
            
            var keys = Shortcuts[shortcutName];
            return GetKeysDisplayText(keys);
        }
        
        // 将Keys枚举转换为显示文本
        public static string GetKeysDisplayText(Keys keys)
        {
            var parts = new List<string>();
            
            if ((keys & Keys.Control) == Keys.Control)
                parts.Add("Ctrl");
            if ((keys & Keys.Alt) == Keys.Alt)
                parts.Add("Alt");
            if ((keys & Keys.Shift) == Keys.Shift)
                parts.Add("Shift");
            
            // 获取主键
            var mainKey = keys & ~(Keys.Control | Keys.Alt | Keys.Shift);
            if (mainKey != Keys.None)
            {
                parts.Add(GetKeyDisplayName(mainKey));
            }
            
            return string.Join("+", parts);
        }
        
        // 获取单个键的显示名称
        private static string GetKeyDisplayName(Keys key)
        {
            return key switch
            {
                Keys.Oemcomma => ",",
                Keys.F1 => "F1",
                Keys.F5 => "F5",
                Keys.F11 => "F11",
                Keys.Enter => "Enter",
                Keys.Space => "Space",
                Keys.Tab => "Tab",
                Keys.Escape => "Esc",
                Keys.Delete => "Del",
                Keys.Back => "Backspace",
                Keys.Home => "Home",
                Keys.End => "End",
                Keys.PageUp => "PgUp",
                Keys.PageDown => "PgDn",
                _ => key.ToString()
            };
        }
        
        // 检查快捷键是否冲突
        public static bool HasConflict(Keys newKeys, string excludeShortcut = "")
        {
            foreach (var shortcut in Shortcuts)
            {
                if (shortcut.Key != excludeShortcut && shortcut.Value == newKeys)
                {
                    return true;
                }
            }
            return false;
        }
        
        // 获取所有快捷键信息
        public static List<(string Name, string Description, string Keys)> GetAllShortcuts()
        {
            var result = new List<(string, string, string)>();
            
            foreach (var shortcut in Shortcuts)
            {
                var description = ShortcutDescriptions.ContainsKey(shortcut.Key) 
                    ? ShortcutDescriptions[shortcut.Key] 
                    : shortcut.Key;
                var keysText = GetKeysDisplayText(shortcut.Value);
                
                result.Add((shortcut.Key, description, keysText));
            }
            
            return result;
        }

        /// <summary>
        /// 刷新窗体的快捷键设置
        /// </summary>
        public static void RefreshShortcuts(Form form)
        {
            try
            {
                if (form?.MainMenuStrip != null)
                {
                    ApplyShortcutsToMenu(form.MainMenuStrip);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新快捷键时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 将快捷键应用到菜单条
        /// </summary>
        private static void ApplyShortcutsToMenu(MenuStrip menuStrip)
        {
            try
            {
                foreach (ToolStripMenuItem menuItem in menuStrip.Items)
                {
                    ApplyShortcutsToMenuItem(menuItem);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用菜单快捷键时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 递归应用快捷键到菜单项
        /// </summary>
        private static void ApplyShortcutsToMenuItem(ToolStripMenuItem menuItem)
        {
            try
            {
                // 根据菜单项的名称或标签查找对应的快捷键
                var shortcutKey = FindShortcutForMenuItem(menuItem);
                if (!string.IsNullOrEmpty(shortcutKey) && Shortcuts.ContainsKey(shortcutKey))
                {
                    menuItem.ShortcutKeys = Shortcuts[shortcutKey];
                }

                // 递归处理子菜单项
                foreach (ToolStripItem subItem in menuItem.DropDownItems)
                {
                    if (subItem is ToolStripMenuItem subMenuItem)
                    {
                        ApplyShortcutsToMenuItem(subMenuItem);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用菜单项快捷键时出错 ({menuItem.Name}): {ex.Message}");
            }
        }

        /// <summary>
        /// 根据菜单项查找对应的快捷键名称
        /// </summary>
        private static string FindShortcutForMenuItem(ToolStripMenuItem menuItem)
        {
            // 根据菜单项的Name或Text属性查找对应的快捷键
            var itemName = menuItem.Name?.ToLower() ?? "";
            var itemText = menuItem.Text?.ToLower() ?? "";

            // 映射菜单项到快捷键名称
            if (itemName.Contains("open") || itemText.Contains("打开"))
                return "OpenFile";
            if (itemName.Contains("export") || itemText.Contains("导出"))
                return "ExportResults";
            if (itemName.Contains("exit") || itemText.Contains("退出"))
                return "Exit";
            if (itemName.Contains("copy") || itemText.Contains("复制"))
                return "Copy";
            if (itemName.Contains("selectall") || itemText.Contains("全选"))
                return "SelectAll";
            if (itemName.Contains("find") || itemText.Contains("查找"))
                return "Find";
            if (itemName.Contains("refresh") || itemText.Contains("刷新"))
                return "Refresh";
            if (itemName.Contains("settings") || itemText.Contains("设置"))
                return "Settings";
            if (itemName.Contains("help") || itemText.Contains("帮助"))
                return "Help";
            if (itemName.Contains("about") || itemText.Contains("关于"))
                return "About";

            return "";
        }
    }
}
