//NEED DELETE: 窗口位置/大小持久化（UI增强），非核心
using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace WinFormsApp1.Config
{
    public class WindowSettings
    {
        public Point Location { get; set; }
        public Size Size { get; set; }
        public FormWindowState WindowState { get; set; }
        public int SplitterDistance1 { get; set; } = 250;
        public int SplitterDistance2 { get; set; } = 300;
        public bool IsMaximized { get; set; }
        
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "STGenerator", "window_settings.json");
        
        public static WindowSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                    return settings ?? new WindowSettings();
                }
            }
            catch (Exception)
            {
                // 如果加载失败，返回默认设置
            }
            
            return new WindowSettings
            {
                Location = new Point(100, 100),
                Size = new Size(1200, 800),
                WindowState = FormWindowState.Maximized,
                SplitterDistance1 = 250,
                SplitterDistance2 = 300,
                IsMaximized = true
            };
        }
        
        public void Save()
        {
            try
            {
                var configDir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception)
            {
                // 保存失败时静默处理
            }
        }
        
        public void ApplyToForm(Form form)
        {
            try
            {
                // 检查位置是否在可见屏幕范围内
                var location = Location;
                var size = Size;
                
                // 确保窗口不会完全超出屏幕范围
                var workingArea = Screen.GetWorkingArea(location);
                if (location.X < workingArea.Left - size.Width + 100)
                    location.X = workingArea.Left;
                if (location.Y < workingArea.Top - size.Height + 100)
                    location.Y = workingArea.Top;
                if (location.X > workingArea.Right - 100)
                    location.X = workingArea.Right - size.Width;
                if (location.Y > workingArea.Bottom - 100)
                    location.Y = workingArea.Bottom - size.Height;
                
                form.Location = location;
                form.Size = size;
                
                if (IsMaximized)
                {
                    form.WindowState = FormWindowState.Maximized;
                }
                else
                {
                    form.WindowState = FormWindowState.Normal;
                }
            }
            catch (Exception)
            {
                // 应用失败时使用默认位置
                form.StartPosition = FormStartPosition.CenterScreen;
            }
        }
        
        public void UpdateFromForm(Form form)
        {
            try
            {
                IsMaximized = form.WindowState == FormWindowState.Maximized;
                
                if (form.WindowState == FormWindowState.Normal)
                {
                    Location = form.Location;
                    Size = form.Size;
                }
                
                WindowState = form.WindowState;
            }
            catch (Exception)
            {
                // 更新失败时保持当前设置
            }
        }
        
        public void UpdateSplitterDistances(int distance1, int distance2)
        {
            SplitterDistance1 = distance1;
            SplitterDistance2 = distance2;
        }
    }
}
