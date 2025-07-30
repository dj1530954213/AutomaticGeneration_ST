using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinFormsApp1.Templates
{
    /// <summary>
    /// 模板库管理器 - 管理用户自定义模板收藏和分类
    /// </summary>
    public static class TemplateLibraryManager
    {
        #region 数据结构定义

        /// <summary>
        /// 模板收藏夹
        /// </summary>
        public class TemplateFavorite
        {
            /// <summary>
            /// 收藏ID
            /// </summary>
            public string Id { get; set; } = Guid.NewGuid().ToString();

            /// <summary>
            /// 模板信息
            /// </summary>
            public TemplateInfo Template { get; set; } = new TemplateInfo();

            /// <summary>
            /// 收藏时间
            /// </summary>
            public DateTime FavoriteDate { get; set; } = DateTime.Now;

            /// <summary>
            /// 用户评分 (1-5星)
            /// </summary>
            public int Rating { get; set; } = 0;

            /// <summary>
            /// 用户备注
            /// </summary>
            public string Notes { get; set; } = string.Empty;

            /// <summary>
            /// 使用次数
            /// </summary>
            public int UsageCount { get; set; } = 0;

            /// <summary>
            /// 最后使用时间
            /// </summary>
            public DateTime LastUsedDate { get; set; } = DateTime.Now;

            /// <summary>
            /// 是否为私有收藏
            /// </summary>
            public bool IsPrivate { get; set; } = false;

            /// <summary>
            /// 标签列表
            /// </summary>
            public List<string> Tags { get; set; } = new List<string>();
        }

        /// <summary>
        /// 模板分类
        /// </summary>
        public class TemplateCategory
        {
            /// <summary>
            /// 分类ID
            /// </summary>
            public string Id { get; set; } = Guid.NewGuid().ToString();

            /// <summary>
            /// 分类名称
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// 分类描述
            /// </summary>
            public string Description { get; set; } = string.Empty;

            /// <summary>
            /// 分类图标
            /// </summary>
            public string Icon { get; set; } = "📁";

            /// <summary>
            /// 分类颜色
            /// </summary>
            public string Color { get; set; } = "#3498db";

            /// <summary>
            /// 父分类ID
            /// </summary>
            public string? ParentId { get; set; }

            /// <summary>
            /// 子分类列表
            /// </summary>
            public List<TemplateCategory> Children { get; set; } = new List<TemplateCategory>();

            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime CreatedDate { get; set; } = DateTime.Now;

            /// <summary>
            /// 排序优先级
            /// </summary>
            public int SortOrder { get; set; } = 0;

            /// <summary>
            /// 是否为系统分类
            /// </summary>
            public bool IsSystemCategory { get; set; } = false;
        }

        /// <summary>
        /// 模板收藏库配置
        /// </summary>
        public class TemplateLibraryConfig
        {
            /// <summary>
            /// 配置版本
            /// </summary>
            public string Version { get; set; } = "1.0";

            /// <summary>
            /// 收藏列表
            /// </summary>
            public List<TemplateFavorite> Favorites { get; set; } = new List<TemplateFavorite>();

            /// <summary>
            /// 分类列表
            /// </summary>
            public List<TemplateCategory> Categories { get; set; } = new List<TemplateCategory>();

            /// <summary>
            /// 标签字典
            /// </summary>
            public Dictionary<string, int> TagsUsageCount { get; set; } = new Dictionary<string, int>();

            /// <summary>
            /// 最后更新时间
            /// </summary>
            public DateTime LastUpdated { get; set; } = DateTime.Now;

            /// <summary>
            /// 用户偏好设置
            /// </summary>
            public LibraryPreferences Preferences { get; set; } = new LibraryPreferences();
        }

        /// <summary>
        /// 库偏好设置
        /// </summary>
        public class LibraryPreferences
        {
            /// <summary>
            /// 默认排序方式
            /// </summary>
            public SortOrder DefaultSortOrder { get; set; } = SortOrder.LastUsed;

            /// <summary>
            /// 显示方式
            /// </summary>
            public ViewMode DefaultViewMode { get; set; } = ViewMode.List;

            /// <summary>
            /// 每页显示数量
            /// </summary>
            public int ItemsPerPage { get; set; } = 20;

            /// <summary>
            /// 是否自动备份
            /// </summary>
            public bool EnableAutoBackup { get; set; } = true;

            /// <summary>
            /// 备份保留天数
            /// </summary>
            public int BackupRetentionDays { get; set; } = 30;

            /// <summary>
            /// 是否启用同步
            /// </summary>
            public bool EnableSync { get; set; } = false;

            /// <summary>
            /// 同步服务器地址
            /// </summary>
            public string SyncServerUrl { get; set; } = string.Empty;
        }

        /// <summary>
        /// 排序方式
        /// </summary>
        public enum SortOrder
        {
            Name,       // 按名称
            Created,    // 按创建时间
            Modified,   // 按修改时间
            LastUsed,   // 按最后使用时间
            Rating,     // 按评分
            Usage       // 按使用次数
        }

        /// <summary>
        /// 显示方式
        /// </summary>
        public enum ViewMode
        {
            List,       // 列表视图
            Grid,       // 网格视图
            Card        // 卡片视图
        }

        /// <summary>
        /// 搜索结果
        /// </summary>
        public class SearchResult
        {
            /// <summary>
            /// 匹配的收藏项
            /// </summary>
            public List<TemplateFavorite> Favorites { get; set; } = new List<TemplateFavorite>();

            /// <summary>
            /// 搜索关键词
            /// </summary>
            public string SearchKeyword { get; set; } = string.Empty;

            /// <summary>
            /// 搜索耗时（毫秒）
            /// </summary>
            public long SearchTime { get; set; } = 0;

            /// <summary>
            /// 总结果数
            /// </summary>
            public int TotalResults { get; set; } = 0;

            /// <summary>
            /// 推荐标签
            /// </summary>
            public List<string> SuggestedTags { get; set; } = new List<string>();
        }

        #endregion

        #region 私有字段

        private static TemplateLibraryConfig? _libraryConfig;
        private static readonly string _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template_library.json");
        private static readonly string _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups", "Library");

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取当前库配置
        /// </summary>
        public static TemplateLibraryConfig LibraryConfig
        {
            get
            {
                if (_libraryConfig == null)
                {
                    LoadLibraryConfig();
                }
                return _libraryConfig ?? new TemplateLibraryConfig();
            }
        }

        /// <summary>
        /// 获取所有收藏项数量
        /// </summary>
        public static int TotalFavorites => LibraryConfig.Favorites.Count;

        /// <summary>
        /// 获取所有分类数量
        /// </summary>
        public static int TotalCategories => LibraryConfig.Categories.Count;

        /// <summary>
        /// 获取所有标签数量
        /// </summary>
        public static int TotalTags => LibraryConfig.TagsUsageCount.Count;

        #endregion

        #region 初始化和配置管理

        /// <summary>
        /// 初始化模板库
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadLibraryConfig();
                CreateDefaultCategories();
                
                // 如果启用了自动备份，执行备份
                if (LibraryConfig.Preferences.EnableAutoBackup)
                {
                    PerformAutoBackup();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"初始化模板库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载库配置
        /// </summary>
        private static void LoadLibraryConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var jsonContent = File.ReadAllText(_configFilePath);
                    _libraryConfig = JsonSerializer.Deserialize<TemplateLibraryConfig>(jsonContent, GetJsonOptions());
                }

                if (_libraryConfig == null)
                {
                    _libraryConfig = new TemplateLibraryConfig();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"加载模板库配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存库配置
        /// </summary>
        public static void SaveLibraryConfig()
        {
            try
            {
                if (_libraryConfig != null)
                {
                    _libraryConfig.LastUpdated = DateTime.Now;
                    
                    // 确保目录存在
                    var directory = Path.GetDirectoryName(_configFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var jsonContent = JsonSerializer.Serialize(_libraryConfig, GetJsonOptions());
                    File.WriteAllText(_configFilePath, jsonContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"保存模板库配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取JSON序列化选项
        /// </summary>
        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// 创建默认分类
        /// </summary>
        private static void CreateDefaultCategories()
        {
            var config = LibraryConfig;
            
            // 如果没有系统分类，创建默认分类
            if (!config.Categories.Any(c => c.IsSystemCategory))
            {
                var defaultCategories = new[]
                {
                    new TemplateCategory { Name = "常用模板", Description = "经常使用的模板", Icon = "⭐", Color = "#f39c12", IsSystemCategory = true, SortOrder = 1 },
                    new TemplateCategory { Name = "AI模板", Description = "模拟量输入点模板", Icon = "📊", Color = "#3498db", IsSystemCategory = true, SortOrder = 2 },
                    new TemplateCategory { Name = "AO模板", Description = "模拟量输出点模板", Icon = "📈", Color = "#e74c3c", IsSystemCategory = true, SortOrder = 3 },
                    new TemplateCategory { Name = "DI模板", Description = "数字量输入点模板", Icon = "🔘", Color = "#2ecc71", IsSystemCategory = true, SortOrder = 4 },
                    new TemplateCategory { Name = "DO模板", Description = "数字量输出点模板", Icon = "🔲", Color = "#9b59b6", IsSystemCategory = true, SortOrder = 5 },
                    new TemplateCategory { Name = "自定义", Description = "用户自定义模板", Icon = "🎨", Color = "#1abc9c", IsSystemCategory = true, SortOrder = 6 },
                    new TemplateCategory { Name = "回收站", Description = "已删除的模板", Icon = "🗑️", Color = "#95a5a6", IsSystemCategory = true, SortOrder = 99 }
                };

                config.Categories.AddRange(defaultCategories);
                SaveLibraryConfig();
            }
        }

        #endregion

        #region 收藏管理

        /// <summary>
        /// 添加模板到收藏
        /// </summary>
        public static string AddToFavorites(TemplateInfo template, string? categoryId = null, List<string>? tags = null, string notes = "")
        {
            try
            {
                var config = LibraryConfig;
                
                // 检查是否已经收藏
                var existingFavorite = config.Favorites.FirstOrDefault(f => 
                    f.Template.FilePath == template.FilePath && 
                    f.Template.PointType == template.PointType && 
                    f.Template.Version == template.Version);
                
                if (existingFavorite != null)
                {
                    throw new InvalidOperationException("该模板已在收藏列表中");
                }

                var favorite = new TemplateFavorite
                {
                    Template = template,
                    Notes = notes,
                    Tags = tags ?? new List<string>()
                };

                config.Favorites.Add(favorite);
                
                // 更新标签使用次数
                UpdateTagsUsage(favorite.Tags, 1);
                
                SaveLibraryConfig();
                
                return favorite.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"添加收藏失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从收藏中移除模板
        /// </summary>
        public static bool RemoveFromFavorites(string favoriteId)
        {
            try
            {
                var config = LibraryConfig;
                var favorite = config.Favorites.FirstOrDefault(f => f.Id == favoriteId);
                
                if (favorite != null)
                {
                    // 更新标签使用次数
                    UpdateTagsUsage(favorite.Tags, -1);
                    
                    config.Favorites.Remove(favorite);
                    SaveLibraryConfig();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"移除收藏失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新收藏信息
        /// </summary>
        public static bool UpdateFavorite(string favoriteId, int? rating = null, string? notes = null, List<string>? tags = null)
        {
            try
            {
                var config = LibraryConfig;
                var favorite = config.Favorites.FirstOrDefault(f => f.Id == favoriteId);
                
                if (favorite != null)
                {
                    // 更新标签使用次数（移除旧标签，添加新标签）
                    if (tags != null)
                    {
                        UpdateTagsUsage(favorite.Tags, -1);
                        UpdateTagsUsage(tags, 1);
                        favorite.Tags = tags;
                    }
                    
                    if (rating.HasValue)
                        favorite.Rating = Math.Max(0, Math.Min(5, rating.Value));
                    
                    if (notes != null)
                        favorite.Notes = notes;
                    
                    SaveLibraryConfig();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"更新收藏失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 记录模板使用
        /// </summary>
        public static void RecordTemplateUsage(string favoriteId)
        {
            try
            {
                var config = LibraryConfig;
                var favorite = config.Favorites.FirstOrDefault(f => f.Id == favoriteId);
                
                if (favorite != null)
                {
                    favorite.UsageCount++;
                    favorite.LastUsedDate = DateTime.Now;
                    SaveLibraryConfig();
                }
            }
            catch (Exception ex)
            {
                // 使用记录失败不应该影响主要功能，只记录日志
                System.Diagnostics.Debug.WriteLine($"记录模板使用失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有收藏项
        /// </summary>
        public static List<TemplateFavorite> GetAllFavorites(SortOrder sortOrder = SortOrder.LastUsed)
        {
            var favorites = LibraryConfig.Favorites.ToList();
            return SortFavorites(favorites, sortOrder);
        }

        /// <summary>
        /// 根据分类获取收藏项
        /// </summary>
        public static List<TemplateFavorite> GetFavoritesByCategory(string categoryId, SortOrder sortOrder = SortOrder.LastUsed)
        {
            var favorites = LibraryConfig.Favorites.Where(f => f.Tags.Contains(categoryId)).ToList();
            return SortFavorites(favorites, sortOrder);
        }

        /// <summary>
        /// 根据点位类型获取收藏项
        /// </summary>
        public static List<TemplateFavorite> GetFavoritesByPointType(PointType pointType, SortOrder sortOrder = SortOrder.LastUsed)
        {
            var favorites = LibraryConfig.Favorites.Where(f => f.Template.PointType == pointType).ToList();
            return SortFavorites(favorites, sortOrder);
        }

        /// <summary>
        /// 获取最近使用的收藏项
        /// </summary>
        public static List<TemplateFavorite> GetRecentlyUsedFavorites(int count = 10)
        {
            return LibraryConfig.Favorites
                .Where(f => f.UsageCount > 0)
                .OrderByDescending(f => f.LastUsedDate)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 获取最高评分的收藏项
        /// </summary>
        public static List<TemplateFavorite> GetTopRatedFavorites(int count = 10)
        {
            return LibraryConfig.Favorites
                .Where(f => f.Rating > 0)
                .OrderByDescending(f => f.Rating)
                .ThenByDescending(f => f.UsageCount)
                .Take(count)
                .ToList();
        }

        #endregion

        #region 分类管理

        /// <summary>
        /// 创建新分类
        /// </summary>
        public static string CreateCategory(string name, string description = "", string icon = "📁", string color = "#3498db", string? parentId = null)
        {
            try
            {
                var config = LibraryConfig;
                
                // 检查分类名称是否已存在
                if (config.Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && c.ParentId == parentId))
                {
                    throw new InvalidOperationException("同级分类中已存在相同名称的分类");
                }

                var category = new TemplateCategory
                {
                    Name = name,
                    Description = description,
                    Icon = icon,
                    Color = color,
                    ParentId = parentId,
                    SortOrder = config.Categories.Count(c => c.ParentId == parentId)
                };

                config.Categories.Add(category);
                SaveLibraryConfig();
                
                return category.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"创建分类失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除分类
        /// </summary>
        public static bool DeleteCategory(string categoryId)
        {
            try
            {
                var config = LibraryConfig;
                var category = config.Categories.FirstOrDefault(c => c.Id == categoryId);
                
                if (category != null)
                {
                    // 不允许删除系统分类
                    if (category.IsSystemCategory)
                    {
                        throw new InvalidOperationException("无法删除系统分类");
                    }
                    
                    // 递归删除子分类
                    var childCategories = config.Categories.Where(c => c.ParentId == categoryId).ToList();
                    foreach (var child in childCategories)
                    {
                        DeleteCategory(child.Id);
                    }
                    
                    // 从收藏项的标签中移除该分类
                    foreach (var favorite in config.Favorites)
                    {
                        favorite.Tags.Remove(categoryId);
                    }
                    
                    config.Categories.Remove(category);
                    SaveLibraryConfig();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"删除分类失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新分类信息
        /// </summary>
        public static bool UpdateCategory(string categoryId, string? name = null, string? description = null, string? icon = null, string? color = null)
        {
            try
            {
                var config = LibraryConfig;
                var category = config.Categories.FirstOrDefault(c => c.Id == categoryId);
                
                if (category != null)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                        category.Name = name;
                    
                    if (description != null)
                        category.Description = description;
                    
                    if (!string.IsNullOrWhiteSpace(icon))
                        category.Icon = icon;
                    
                    if (!string.IsNullOrWhiteSpace(color))
                        category.Color = color;
                    
                    SaveLibraryConfig();
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"更新分类失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public static List<TemplateCategory> GetAllCategories(bool includeEmpty = true)
        {
            var categories = LibraryConfig.Categories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToList();
            
            if (!includeEmpty)
            {
                // 过滤掉没有模板的分类
                categories = categories.Where(c => 
                    c.IsSystemCategory || 
                    LibraryConfig.Favorites.Any(f => f.Tags.Contains(c.Id))
                ).ToList();
            }
            
            return categories;
        }

        /// <summary>
        /// 获取分类树结构
        /// </summary>
        public static List<TemplateCategory> GetCategoryTree()
        {
            var allCategories = LibraryConfig.Categories.ToList();
            var rootCategories = allCategories.Where(c => string.IsNullOrEmpty(c.ParentId)).OrderBy(c => c.SortOrder).ToList();
            
            foreach (var rootCategory in rootCategories)
            {
                BuildCategoryTree(rootCategory, allCategories);
            }
            
            return rootCategories;
        }

        /// <summary>
        /// 构建分类树
        /// </summary>
        private static void BuildCategoryTree(TemplateCategory parent, List<TemplateCategory> allCategories)
        {
            parent.Children = allCategories
                .Where(c => c.ParentId == parent.Id)
                .OrderBy(c => c.SortOrder)
                .ToList();
            
            foreach (var child in parent.Children)
            {
                BuildCategoryTree(child, allCategories);
            }
        }

        #endregion

        #region 搜索功能

        /// <summary>
        /// 搜索模板
        /// </summary>
        public static SearchResult SearchTemplates(string keyword, PointType? pointType = null, List<string>? tags = null, int? minRating = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new SearchResult { SearchKeyword = keyword };
            
            try
            {
                var favorites = LibraryConfig.Favorites.AsQueryable();
                
                // 关键词搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.ToLower();
                    favorites = favorites.Where(f => 
                        f.Template.Name.ToLower().Contains(keyword) ||
                        f.Template.Description.ToLower().Contains(keyword) ||
                        f.Notes.ToLower().Contains(keyword) ||
                        f.Tags.Any(t => t.ToLower().Contains(keyword))
                    );
                }
                
                // 点位类型过滤
                if (pointType.HasValue)
                {
                    favorites = favorites.Where(f => f.Template.PointType == pointType.Value);
                }
                
                // 标签过滤
                if (tags != null && tags.Any())
                {
                    favorites = favorites.Where(f => tags.All(tag => f.Tags.Contains(tag)));
                }
                
                // 评分过滤
                if (minRating.HasValue)
                {
                    favorites = favorites.Where(f => f.Rating >= minRating.Value);
                }
                
                result.Favorites = favorites.ToList();
                result.TotalResults = result.Favorites.Count;
                
                // 生成推荐标签
                result.SuggestedTags = GenerateSuggestedTags(keyword, result.Favorites);
            }
            catch (Exception ex)
            {
                throw new Exception($"搜索模板失败: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                result.SearchTime = stopwatch.ElapsedMilliseconds;
            }
            
            return result;
        }

        /// <summary>
        /// 生成推荐标签
        /// </summary>
        private static List<string> GenerateSuggestedTags(string keyword, List<TemplateFavorite> searchResults)
        {
            var allTags = searchResults.SelectMany(f => f.Tags).Distinct().ToList();
            var suggestedTags = new List<string>();
            
            // 如果有关键词，优先推荐包含关键词的标签
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                suggestedTags.AddRange(allTags.Where(t => t.ToLower().Contains(keyword.ToLower())));
            }
            
            // 添加最常用的标签
            var popularTags = LibraryConfig.TagsUsageCount
                .Where(kvp => !suggestedTags.Contains(kvp.Key))
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key);
            
            suggestedTags.AddRange(popularTags);
            
            return suggestedTags.Take(8).ToList();
        }

        #endregion

        #region 标签管理

        /// <summary>
        /// 获取所有标签
        /// </summary>
        public static List<string> GetAllTags(bool sortByUsage = true)
        {
            if (sortByUsage)
            {
                return LibraryConfig.TagsUsageCount
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
            
            return LibraryConfig.TagsUsageCount.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// 获取标签使用次数
        /// </summary>
        public static int GetTagUsageCount(string tag)
        {
            return LibraryConfig.TagsUsageCount.TryGetValue(tag, out var count) ? count : 0;
        }

        /// <summary>
        /// 更新标签使用次数
        /// </summary>
        private static void UpdateTagsUsage(List<string> tags, int delta)
        {
            var config = LibraryConfig;
            
            foreach (var tag in tags)
            {
                if (config.TagsUsageCount.ContainsKey(tag))
                {
                    config.TagsUsageCount[tag] = Math.Max(0, config.TagsUsageCount[tag] + delta);
                    
                    // 如果使用次数为0，移除该标签
                    if (config.TagsUsageCount[tag] == 0)
                    {
                        config.TagsUsageCount.Remove(tag);
                    }
                }
                else if (delta > 0)
                {
                    config.TagsUsageCount[tag] = delta;
                }
            }
        }

        /// <summary>
        /// 清理未使用的标签
        /// </summary>
        public static int CleanupUnusedTags()
        {
            var config = LibraryConfig;
            var usedTags = config.Favorites.SelectMany(f => f.Tags).Distinct().ToHashSet();
            var tagsToRemove = config.TagsUsageCount.Keys.Where(tag => !usedTags.Contains(tag)).ToList();
            
            foreach (var tag in tagsToRemove)
            {
                config.TagsUsageCount.Remove(tag);
            }
            
            if (tagsToRemove.Any())
            {
                SaveLibraryConfig();
            }
            
            return tagsToRemove.Count;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 排序收藏项
        /// </summary>
        private static List<TemplateFavorite> SortFavorites(List<TemplateFavorite> favorites, SortOrder sortOrder)
        {
            return sortOrder switch
            {
                SortOrder.Name => favorites.OrderBy(f => f.Template.Name).ToList(),
                SortOrder.Created => favorites.OrderByDescending(f => f.FavoriteDate).ToList(),
                SortOrder.Modified => favorites.OrderByDescending(f => f.Template.ModifiedDate).ToList(),
                SortOrder.LastUsed => favorites.OrderByDescending(f => f.LastUsedDate).ToList(),
                SortOrder.Rating => favorites.OrderByDescending(f => f.Rating).ThenByDescending(f => f.UsageCount).ToList(),
                SortOrder.Usage => favorites.OrderByDescending(f => f.UsageCount).ThenByDescending(f => f.LastUsedDate).ToList(),
                _ => favorites
            };
        }

        /// <summary>
        /// 导出收藏库
        /// </summary>
        public static void ExportLibrary(string filePath, bool includeSystemCategories = false)
        {
            try
            {
                var exportConfig = new TemplateLibraryConfig
                {
                    Version = LibraryConfig.Version,
                    Favorites = LibraryConfig.Favorites.ToList(),
                    Categories = includeSystemCategories 
                        ? LibraryConfig.Categories.ToList()
                        : LibraryConfig.Categories.Where(c => !c.IsSystemCategory).ToList(),
                    TagsUsageCount = new Dictionary<string, int>(LibraryConfig.TagsUsageCount),
                    LastUpdated = DateTime.Now,
                    Preferences = LibraryConfig.Preferences
                };

                var jsonContent = JsonSerializer.Serialize(exportConfig, GetJsonOptions());
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"导出收藏库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 导入收藏库
        /// </summary>
        public static void ImportLibrary(string filePath, bool mergeMode = true)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"导入文件不存在: {filePath}");
                }

                var jsonContent = File.ReadAllText(filePath);
                var importConfig = JsonSerializer.Deserialize<TemplateLibraryConfig>(jsonContent, GetJsonOptions());
                
                if (importConfig == null)
                {
                    throw new InvalidOperationException("导入文件格式无效");
                }

                var config = LibraryConfig;
                
                if (mergeMode)
                {
                    // 合并模式：添加不存在的项目
                    foreach (var favorite in importConfig.Favorites)
                    {
                        if (!config.Favorites.Any(f => f.Id == favorite.Id))
                        {
                            config.Favorites.Add(favorite);
                        }
                    }
                    
                    foreach (var category in importConfig.Categories.Where(c => !c.IsSystemCategory))
                    {
                        if (!config.Categories.Any(c => c.Id == category.Id))
                        {
                            config.Categories.Add(category);
                        }
                    }
                    
                    foreach (var tag in importConfig.TagsUsageCount)
                    {
                        if (config.TagsUsageCount.ContainsKey(tag.Key))
                        {
                            config.TagsUsageCount[tag.Key] += tag.Value;
                        }
                        else
                        {
                            config.TagsUsageCount[tag.Key] = tag.Value;
                        }
                    }
                }
                else
                {
                    // 替换模式：完全替换（保留系统分类）
                    var systemCategories = config.Categories.Where(c => c.IsSystemCategory).ToList();
                    
                    config.Favorites = importConfig.Favorites;
                    config.Categories = systemCategories.Concat(importConfig.Categories.Where(c => !c.IsSystemCategory)).ToList();
                    config.TagsUsageCount = new Dictionary<string, int>(importConfig.TagsUsageCount);
                }
                
                SaveLibraryConfig();
            }
            catch (Exception ex)
            {
                throw new Exception($"导入收藏库失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 执行自动备份
        /// </summary>
        private static void PerformAutoBackup()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                }
                
                var backupFileName = $"library_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var backupFilePath = Path.Combine(_backupDirectory, backupFileName);
                
                ExportLibrary(backupFilePath, true);
                
                // 清理过期备份
                CleanupOldBackups();
            }
            catch (Exception ex)
            {
                // 备份失败不应该影响主要功能
                System.Diagnostics.Debug.WriteLine($"自动备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理过期备份
        /// </summary>
        private static void CleanupOldBackups()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                    return;
                
                var retentionDays = LibraryConfig.Preferences.BackupRetentionDays;
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                
                var backupFiles = Directory.GetFiles(_backupDirectory, "library_backup_*.json");
                
                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理备份文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取库统计信息
        /// </summary>
        public static Dictionary<string, object> GetLibraryStatistics()
        {
            var config = LibraryConfig;
            var stats = new Dictionary<string, object>();
            
            // 基本统计
            stats["TotalFavorites"] = config.Favorites.Count;
            stats["TotalCategories"] = config.Categories.Count(c => !c.IsSystemCategory);
            stats["TotalTags"] = config.TagsUsageCount.Count;
            
            // 点位类型分布
            var pointTypeDistribution = config.Favorites
                .GroupBy(f => f.Template.PointType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
            stats["PointTypeDistribution"] = pointTypeDistribution;
            
            // 评分分布
            var ratingDistribution = config.Favorites
                .Where(f => f.Rating > 0)
                .GroupBy(f => f.Rating)
                .ToDictionary(g => g.Key, g => g.Count());
            stats["RatingDistribution"] = ratingDistribution;
            
            // 使用情况
            stats["TotalUsages"] = config.Favorites.Sum(f => f.UsageCount);
            stats["AverageRating"] = config.Favorites.Where(f => f.Rating > 0).DefaultIfEmpty().Average(f => f?.Rating ?? 0);
            stats["MostUsedTemplate"] = config.Favorites.OrderByDescending(f => f.UsageCount).FirstOrDefault()?.Template.Name ?? "无";
            stats["RecentlyAdded"] = config.Favorites.Count(f => f.FavoriteDate > DateTime.Now.AddDays(-7));
            
            return stats;
        }

        #endregion
    }
}