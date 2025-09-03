//NEED DELETE: 界面样式管理（视图相关），与核心导入/生成/导出无关
using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp1.Config
{
    public static class ControlStyleManager
    {
        // 标准间距常量
        public const int SMALL_PADDING = 5;
        public const int MEDIUM_PADDING = 10;
        public const int LARGE_PADDING = 15;
        public const int EXTRA_LARGE_PADDING = 20;
        
        // 标准字体
        public static readonly Font HeaderFont = new Font("微软雅黑", 12F, FontStyle.Bold);
        public static readonly Font DefaultFont = new Font("微软雅黑", 10F);
        public static readonly Font SmallFont = new Font("微软雅黑", 9F);
        public static readonly Font CodeFont = new Font("Consolas", 9F);
        
        // 标准控件尺寸
        public static readonly Size StandardButtonSize = new Size(120, 35);
        public static readonly Size SmallButtonSize = new Size(80, 30);
        public static readonly Size LargeButtonSize = new Size(160, 40);
        
        // 圆角设置
        public const int CORNER_RADIUS = 4;
        
        public static void ApplyStandardSpacing(Control container)
        {
            if (container == null) return;
            
            // 为容器设置标准内边距
            container.Padding = new Padding(MEDIUM_PADDING);
            
            // 递归处理子控件
            ApplySpacingToChildren(container);
        }
        
        private static void ApplySpacingToChildren(Control parent)
        {
            var controls = new Control[parent.Controls.Count];
            parent.Controls.CopyTo(controls, 0);
            
            for (int i = 0; i < controls.Length; i++)
            {
                var control = controls[i];
                
                // 设置控件间距
                SetControlMargin(control);
                
                // 设置控件对齐
                SetControlAlignment(control);
                
                // 递归处理子控件
                if (control.HasChildren)
                {
                    ApplySpacingToChildren(control);
                }
            }
        }
        
        private static void SetControlMargin(Control control)
        {
            switch (control)
            {
                case Button button:
                    control.Margin = new Padding(SMALL_PADDING);
                    if (control.Size == Size.Empty)
                        control.Size = StandardButtonSize;
                    break;
                    
                case Label label:
                    control.Margin = new Padding(SMALL_PADDING, SMALL_PADDING, SMALL_PADDING, 2);
                    break;
                    
                case TextBox textBox:
                    control.Margin = new Padding(SMALL_PADDING);
                    break;
                    
                case Panel panel:
                    control.Margin = new Padding(MEDIUM_PADDING);
                    control.Padding = new Padding(MEDIUM_PADDING);
                    break;
                    
                case GroupBox groupBox:
                    control.Margin = new Padding(MEDIUM_PADDING);
                    control.Padding = new Padding(MEDIUM_PADDING, EXTRA_LARGE_PADDING, MEDIUM_PADDING, MEDIUM_PADDING);
                    break;
                    
                case TabControl tabControl:
                    control.Margin = new Padding(SMALL_PADDING);
                    control.Padding = new Padding(SMALL_PADDING);
                    break;
                    
                default:
                    control.Margin = new Padding(SMALL_PADDING);
                    break;
            }
        }
        
        private static void SetControlAlignment(Control control)
        {
            // 根据控件类型设置合适的锚点
            if (control is MenuStrip || control is ToolStrip || control is StatusStrip)
            {
                // 这些控件通常应该停靠，不需要设置锚点
                return;
            }
            else if (control is SplitContainer)
            {
                control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            }
            else if (control is Panel panel && panel.Dock == DockStyle.None)
            {
                control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            }
            else if (control is Button)
            {
                control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }
            else if (control is TextBox textBox)
            {
                if (textBox.Multiline)
                {
                    control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                }
                else
                {
                    control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
            }
            else if (control is Label)
            {
                control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }
        }
        
        /// <summary>
        /// 设置默认字体
        /// </summary>
        public static void SetDefaultFont(Control control, Font font = null)
        {
            if (control == null) return;
            
            font ??= DefaultFont;
            control.Font = font;
            
            // 递归设置子控件字体
            foreach (Control child in control.Controls)
            {
                SetDefaultFont(child, font);
            }
        }
        
        /// <summary>
        /// 设置默认背景色
        /// </summary>
        public static void SetDefaultBackColor(Control control, Color color = default)
        {
            if (control == null) return;
            
            if (color == default)
            {
                color = ThemeManager.CurrentTheme == ThemeType.Dark 
                    ? ThemeManager.DarkTheme.BackgroundColor 
                    : ThemeManager.LightTheme.BackgroundColor;
            }
            
            control.BackColor = color;
            
            // 递归设置子控件背景色
            foreach (Control child in control.Controls)
            {
                if (child is not Button && child is not TextBox) // 保持某些控件的默认样式
                {
                    SetDefaultBackColor(child, color);
                }
            }
        }

        public static void ApplyButtonStyle(Button button, ButtonStyle style = ButtonStyle.Default)
        {
            if (button == null) return;
            
            switch (style)
            {
                case ButtonStyle.Primary:
                    button.BackColor = ThemeManager.GetAccentColor();
                    button.ForeColor = Color.White;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 0;
                    button.Size = StandardButtonSize;
                    break;
                    
                case ButtonStyle.Secondary:
                    button.BackColor = ThemeManager.GetSurfaceColor();
                    button.ForeColor = ThemeManager.GetTextColor();
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = ThemeManager.GetBorderColor();
                    button.FlatAppearance.BorderSize = 1;
                    button.Size = StandardButtonSize;
                    break;
                    
                case ButtonStyle.Small:
                    button.BackColor = ThemeManager.GetSurfaceColor();
                    button.ForeColor = ThemeManager.GetTextColor();
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = ThemeManager.GetBorderColor();
                    button.FlatAppearance.BorderSize = 1;
                    button.Size = SmallButtonSize;
                    button.Font = SmallFont;
                    break;
                    
                case ButtonStyle.Large:
                    button.BackColor = ThemeManager.GetAccentColor();
                    button.ForeColor = Color.White;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 0;
                    button.Size = LargeButtonSize;
                    button.Font = HeaderFont;
                    break;
                    
                default:
                    button.BackColor = ThemeManager.GetSurfaceColor();
                    button.ForeColor = ThemeManager.GetTextColor();
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = ThemeManager.GetBorderColor();
                    button.FlatAppearance.BorderSize = 1;
                    button.Size = StandardButtonSize;
                    break;
            }
            
            // 添加鼠标悬停效果
            button.MouseEnter += (s, e) => {
                button.BackColor = style == ButtonStyle.Primary || style == ButtonStyle.Large 
                    ? Color.FromArgb(Math.Max(0, button.BackColor.R - 20), 
                                   Math.Max(0, button.BackColor.G - 20), 
                                   Math.Max(0, button.BackColor.B - 20))
                    : ThemeManager.GetHoverColor();
            };
            
            button.MouseLeave += (s, e) => {
                button.BackColor = style == ButtonStyle.Primary || style == ButtonStyle.Large 
                    ? ThemeManager.GetAccentColor()
                    : ThemeManager.GetSurfaceColor();
            };
        }
        
        public static void ApplyTextBoxStyle(TextBox textBox)
        {
            if (textBox == null) return;
            
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = ThemeManager.GetBackgroundColor();
            textBox.ForeColor = ThemeManager.GetTextColor();
            textBox.Font = DefaultFont;
            
            // 添加焦点效果
            textBox.Enter += (s, e) => {
                textBox.BackColor = Color.FromArgb(245, 248, 255);
            };
            
            textBox.Leave += (s, e) => {
                textBox.BackColor = ThemeManager.GetBackgroundColor();
            };
        }
        
        public static void ApplyLabelStyle(Label label, LabelStyle style = LabelStyle.Default)
        {
            if (label == null) return;
            
            switch (style)
            {
                case LabelStyle.Header:
                    label.Font = HeaderFont;
                    label.ForeColor = ThemeManager.GetTextColor();
                    break;
                    
                case LabelStyle.Secondary:
                    label.Font = DefaultFont;
                    label.ForeColor = ThemeManager.GetSecondaryTextColor();
                    break;
                    
                case LabelStyle.Small:
                    label.Font = SmallFont;
                    label.ForeColor = ThemeManager.GetSecondaryTextColor();
                    break;
                    
                default:
                    label.Font = DefaultFont;
                    label.ForeColor = ThemeManager.GetTextColor();
                    break;
            }
        }
        
        public static void ApplyPanelStyle(Panel panel, bool withBorder = false)
        {
            if (panel == null) return;
            
            panel.BackColor = ThemeManager.GetSurfaceColor();
            
            if (withBorder)
            {
                panel.BorderStyle = BorderStyle.FixedSingle;
            }
            
            panel.Padding = new Padding(MEDIUM_PADDING);
        }
    }
    
    public enum ButtonStyle
    {
        Default,
        Primary,
        Secondary,
        Small,
        Large
    }
    
    public enum LabelStyle
    {
        Default,
        Header,
        Secondary,
        Small
    }
}
