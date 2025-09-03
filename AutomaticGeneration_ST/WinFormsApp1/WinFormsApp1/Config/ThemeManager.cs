//NEED DELETE: 主题/配色管理（视图相关），与核心链路无关
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1.Config
{
    public enum ThemeType
    {
        Light,
        Dark,
        System
    }

    public static class ThemeManager
    {
        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Light;
        
        public static event Action<ThemeType>? ThemeChanged;
        
        // 浅色主题颜色
        public static class LightTheme
        {
            public static readonly Color BackgroundColor = Color.White;
            public static readonly Color SurfaceColor = Color.FromArgb(248, 249, 250);
            public static readonly Color TextColor = Color.FromArgb(33, 37, 41);
            public static readonly Color SecondaryTextColor = Color.FromArgb(108, 117, 125);
            public static readonly Color BorderColor = Color.FromArgb(222, 226, 230);
            public static readonly Color AccentColor = Color.FromArgb(0, 123, 255);
            public static readonly Color HoverColor = Color.FromArgb(233, 236, 239);
            public static readonly Color SelectedColor = Color.FromArgb(0, 123, 255, 25);
            public static readonly Color MenuBarColor = Color.FromArgb(248, 249, 250);
            public static readonly Color StatusBarColor = Color.FromArgb(248, 249, 250);
            public static readonly Color ToolBarColor = Color.FromArgb(248, 249, 250);
        }
        
        // 深色主题颜色
        public static class DarkTheme
        {
            public static readonly Color BackgroundColor = Color.FromArgb(33, 37, 41);
            public static readonly Color SurfaceColor = Color.FromArgb(52, 58, 64);
            public static readonly Color TextColor = Color.FromArgb(248, 249, 250);
            public static readonly Color SecondaryTextColor = Color.FromArgb(173, 181, 189);
            public static readonly Color BorderColor = Color.FromArgb(73, 80, 87);
            public static readonly Color AccentColor = Color.FromArgb(13, 202, 240);
            public static readonly Color HoverColor = Color.FromArgb(73, 80, 87);
            public static readonly Color SelectedColor = Color.FromArgb(13, 202, 240, 25);
            public static readonly Color MenuBarColor = Color.FromArgb(52, 58, 64);
            public static readonly Color StatusBarColor = Color.FromArgb(52, 58, 64);
            public static readonly Color ToolBarColor = Color.FromArgb(52, 58, 64);
        }
        
        public static void SetTheme(ThemeType theme)
        {
            if (theme == ThemeType.System)
            {
                // 检测系统主题（简化版本，实际可能需要更复杂的系统检测）
                theme = IsSystemDarkMode() ? ThemeType.Dark : ThemeType.Light;
            }
            
            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }
        
        public static Color GetBackgroundColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.BackgroundColor : LightTheme.BackgroundColor;
        }
        
        public static Color GetSurfaceColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.SurfaceColor : LightTheme.SurfaceColor;
        }
        
        public static Color GetTextColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.TextColor : LightTheme.TextColor;
        }
        
        public static Color GetSecondaryTextColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.SecondaryTextColor : LightTheme.SecondaryTextColor;
        }
        
        public static Color GetBorderColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.BorderColor : LightTheme.BorderColor;
        }
        
        public static Color GetAccentColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.AccentColor : LightTheme.AccentColor;
        }
        
        public static Color GetHoverColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.HoverColor : LightTheme.HoverColor;
        }
        
        public static Color GetSelectedColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.SelectedColor : LightTheme.SelectedColor;
        }
        
        public static Color GetMenuBarColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.MenuBarColor : LightTheme.MenuBarColor;
        }
        
        public static Color GetStatusBarColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.StatusBarColor : LightTheme.StatusBarColor;
        }
        
        public static Color GetToolBarColor()
        {
            return CurrentTheme == ThemeType.Dark ? DarkTheme.ToolBarColor : LightTheme.ToolBarColor;
        }
        
        public static void ApplyTheme(Control control)
        {
            if (control == null) return;
            
            // 递归应用主题到所有子控件
            ApplyThemeToControl(control);
            
            foreach (Control child in control.Controls)
            {
                ApplyTheme(child);
            }
        }
        
        private static void ApplyThemeToControl(Control control)
        {
            // 基本控件颜色设置
            control.BackColor = GetBackgroundColor();
            control.ForeColor = GetTextColor();
            
            // 特殊控件处理
            if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = GetMenuBarColor();
                menuStrip.ForeColor = GetTextColor();
                ApplyMenuTheme(menuStrip);
            }
            else if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = GetToolBarColor();
                toolStrip.ForeColor = GetTextColor();
            }
            else if (control is StatusStrip statusStrip)
            {
                statusStrip.BackColor = GetStatusBarColor();
                statusStrip.ForeColor = GetTextColor();
            }
            else if (control is TabControl tabControl)
            {
                tabControl.BackColor = GetSurfaceColor();
            }
            else if (control is ListView listView)
            {
                listView.BackColor = GetBackgroundColor();
                listView.ForeColor = GetTextColor();
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = GetBackgroundColor();
                textBox.ForeColor = GetTextColor();
                textBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is RichTextBox richTextBox)
            {
                richTextBox.BackColor = GetBackgroundColor();
                richTextBox.ForeColor = GetTextColor();
            }
            else if (control is SplitContainer splitContainer)
            {
                splitContainer.BackColor = GetBorderColor();
            }
        }
        
        private static void ApplyMenuTheme(MenuStrip menuStrip)
        {
            foreach (ToolStripItem item in menuStrip.Items)
            {
                ApplyMenuItemTheme(item);
            }
        }
        
        private static void ApplyMenuItemTheme(ToolStripItem item)
        {
            item.BackColor = GetMenuBarColor();
            item.ForeColor = GetTextColor();
            
            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem subItem in menuItem.DropDownItems)
                {
                    ApplyMenuItemTheme(subItem);
                }
            }
        }
        
        private static bool IsSystemDarkMode()
        {
            try
            {
                // 简化的系统主题检测，实际项目中可能需要更复杂的实现
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value is int intValue && intValue == 0;
            }
            catch
            {
                return false; // 默认为浅色主题
            }
        }
    }
}
