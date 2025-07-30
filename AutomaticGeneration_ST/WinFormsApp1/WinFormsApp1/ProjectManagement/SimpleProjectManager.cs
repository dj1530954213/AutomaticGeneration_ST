using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WinFormsApp1.Templates;

namespace WinFormsApp1.ProjectManagement
{
    /// <summary>
    /// 简单项目数据
    /// </summary>
    public class SimpleProject
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime ModifiedTime { get; set; } = DateTime.Now;
        public List<Dictionary<string, object>> PointData { get; set; } = new();
        public string GeneratedCode { get; set; } = "";
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// 简化的项目管理器
    /// </summary>
    public static class SimpleProjectManager
    {
        private static SimpleProject? _currentProject;
        private static string _currentFilePath = "";

        public static event EventHandler? ProjectChanged;

        /// <summary>
        /// 当前项目
        /// </summary>
        public static SimpleProject? CurrentProject => _currentProject;

        /// <summary>
        /// 当前文件路径
        /// </summary>
        public static string CurrentFilePath => _currentFilePath;

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public static bool HasUnsavedChanges { get; private set; }

        /// <summary>
        /// 创建新项目
        /// </summary>
        public static void CreateNewProject(string name = "新建项目", string description = "")
        {
            _currentProject = new SimpleProject
            {
                Name = name,
                Description = description
            };
            _currentFilePath = "";
            HasUnsavedChanges = true;
            ProjectChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// 打开项目文件
        /// </summary>
        public static async Task<bool> OpenProjectAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var json = await File.ReadAllTextAsync(filePath);
                var project = JsonSerializer.Deserialize<SimpleProject>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (project != null)
                {
                    _currentProject = project;
                    _currentFilePath = filePath;
                    HasUnsavedChanges = false;
                    ProjectChanged?.Invoke(null, EventArgs.Empty);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开项目失败: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 保存项目
        /// </summary>
        public static async Task<bool> SaveProjectAsync(string? filePath = null)
        {
            if (_currentProject == null)
                return false;

            try
            {
                var saveFilePath = filePath ?? _currentFilePath;
                if (string.IsNullOrEmpty(saveFilePath))
                    return false;

                _currentProject.ModifiedTime = DateTime.Now;

                var json = JsonSerializer.Serialize(_currentProject, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                // 确保目录存在
                var directory = Path.GetDirectoryName(saveFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(saveFilePath, json);
                _currentFilePath = saveFilePath;
                HasUnsavedChanges = false;
                ProjectChanged?.Invoke(null, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存项目失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 另存为项目
        /// </summary>
        public static async Task<bool> SaveAsProjectAsync(string filePath)
        {
            _currentFilePath = filePath;
            return await SaveProjectAsync();
        }

        /// <summary>
        /// 关闭当前项目
        /// </summary>
        public static void CloseProject()
        {
            _currentProject = null;
            _currentFilePath = "";
            HasUnsavedChanges = false;
            ProjectChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// 设置点位数据
        /// </summary>
        public static void SetPointData(List<Dictionary<string, object>> pointData)
        {
            if (_currentProject != null)
            {
                _currentProject.PointData = pointData;
                HasUnsavedChanges = true;
                ProjectChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 设置生成的代码
        /// </summary>
        public static void SetGeneratedCode(string code)
        {
            if (_currentProject != null)
            {
                _currentProject.GeneratedCode = code;
                HasUnsavedChanges = true;
                ProjectChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 更新项目设置
        /// </summary>
        public static void UpdateSettings(string key, object value)
        {
            if (_currentProject != null)
            {
                _currentProject.Settings[key] = value;
                HasUnsavedChanges = true;
                ProjectChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 获取项目设置
        /// </summary>
        public static T? GetSetting<T>(string key, T? defaultValue = default)
        {
            if (_currentProject?.Settings.TryGetValue(key, out var value) == true)
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    }
                    if (value is T directValue)
                    {
                        return directValue;
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取支持的文件扩展名
        /// </summary>
        public static string GetFileFilter()
        {
            return "ST项目文件|*.stproj|所有文件|*.*";
        }

        /// <summary>
        /// 检查是否需要保存
        /// </summary>
        public static bool NeedsSave()
        {
            return _currentProject != null && HasUnsavedChanges;
        }
    }
}