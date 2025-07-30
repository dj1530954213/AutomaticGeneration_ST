using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scriban;

namespace WinFormsApp1.Templates
{
    /// <summary>
    /// 模板缓存管理器 - 负责模板的缓存和性能优化
    /// </summary>
    public static class TemplateCacheManager
    {
        private static readonly ConcurrentDictionary<string, Scriban.Template> _templateCache = new();
        private static readonly ConcurrentDictionary<string, string> _renderCache = new();
        private static readonly List<RenderPerformance> _performanceHistory = new();
        private static readonly object _performanceLock = new object();
        
        private static int _maxCacheSize = 500;
        private static long _maxMemoryUsage = 50 * 1024 * 1024; // 50MB
        private static TimeSpan _defaultExpiration = TimeSpan.FromHours(2);
        
        private static CacheStatistics _statistics = new CacheStatistics();

        /// <summary>
        /// 配置缓存参数
        /// </summary>
        public static void Configure(int maxCacheSize, long maxMemoryUsage, TimeSpan? defaultExpiration = null)
        {
            _maxCacheSize = maxCacheSize;
            _maxMemoryUsage = maxMemoryUsage;
            if (defaultExpiration.HasValue)
                _defaultExpiration = defaultExpiration.Value;
        }

        /// <summary>
        /// 获取或编译模板
        /// </summary>
        public static async Task<Scriban.Template> GetOrCompileTemplateAsync(string templatePath, string templateContent)
        {
            await Task.Yield(); // 模拟异步操作
            
            var cacheKey = $"template_{templatePath}_{templateContent.GetHashCode()}";
            
            if (_templateCache.TryGetValue(cacheKey, out var cachedTemplate))
            {
                _statistics.CacheHits++;
                return cachedTemplate;
            }

            _statistics.CacheMisses++;
            
            var template = Scriban.Template.Parse(templateContent);
            
            if (template.HasErrors)
            {
                var errors = string.Join(", ", template.Messages);
                throw new InvalidOperationException($"模板解析错误: {errors}");
            }

            // 检查缓存大小限制
            if (_templateCache.Count >= _maxCacheSize)
            {
                ClearOldEntries();
            }

            _templateCache.TryAdd(cacheKey, template);
            return template;
        }

        /// <summary>
        /// 带缓存的模板渲染
        /// </summary>
        public static async Task<string> RenderTemplateWithCacheAsync(Scriban.Template template, Dictionary<string, object> data, string cacheKey)
        {
            await Task.Yield(); // 模拟异步操作
            
            var dataHash = data.GetHashCode().ToString();
            var fullCacheKey = $"{cacheKey}_{dataHash}";
            
            var startTime = DateTime.UtcNow;
            
            if (_renderCache.TryGetValue(fullCacheKey, out var cachedResult))
            {
                _statistics.RenderCacheHits++;
                RecordPerformance(fullCacheKey, DateTime.UtcNow - startTime, true);
                return cachedResult;
            }

            _statistics.RenderCacheMisses++;
            
            var result = template.Render(data);
            var elapsed = DateTime.UtcNow - startTime;
            
            // 缓存渲染结果
            if (_renderCache.Count < _maxCacheSize)
            {
                _renderCache.TryAdd(fullCacheKey, result);
            }
            
            RecordPerformance(fullCacheKey, elapsed, false);
            return result;
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public static void ClearAllCache()
        {
            _templateCache.Clear();
            _renderCache.Clear();
            _statistics.CacheClearCount++;
        }

        /// <summary>
        /// 预热缓存
        /// </summary>
        public static async Task PrewarmCacheAsync(IEnumerable<(string path, string content)> templates)
        {
            foreach (var (path, content) in templates)
            {
                try
                {
                    await GetOrCompileTemplateAsync(path, content);
                }
                catch (Exception)
                {
                    // 忽略预热过程中的错误
                }
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public static CacheStatistics GetStatistics()
        {
            _statistics.TemplateCacheSize = _templateCache.Count;
            _statistics.RenderCacheSize = _renderCache.Count;
            _statistics.EstimatedMemoryUsage = EstimateMemoryUsage();
            return _statistics;
        }

        /// <summary>
        /// 获取性能历史记录
        /// </summary>
        public static List<RenderPerformance> GetPerformanceHistory(int maxRecords = 100)
        {
            lock (_performanceLock)
            {
                return _performanceHistory.TakeLast(maxRecords).ToList();
            }
        }

        private static void ClearOldEntries()
        {
            // 简单策略：清除前10%的条目
            var entriesToRemove = _templateCache.Count / 10;
            var keysToRemove = _templateCache.Keys.Take(entriesToRemove).ToList();
            
            foreach (var key in keysToRemove)
            {
                _templateCache.TryRemove(key, out _);
            }
        }

        private static void RecordPerformance(string cacheKey, TimeSpan elapsed, bool cacheHit)
        {
            var performance = new RenderPerformance
            {
                CacheKey = cacheKey,
                Timestamp = DateTime.UtcNow,
                ElapsedTime = elapsed,
                CacheHit = cacheHit
            };

            lock (_performanceLock)
            {
                _performanceHistory.Add(performance);
                
                // 保持历史记录在合理大小
                if (_performanceHistory.Count > 1000)
                {
                    _performanceHistory.RemoveRange(0, 100);
                }
            }
        }

        private static long EstimateMemoryUsage()
        {
            // 简单的内存使用估算
            return (_templateCache.Count + _renderCache.Count) * 1024; // 假设每个条目1KB
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public int CacheHits { get; set; }
        public int CacheMisses { get; set; }
        public int RenderCacheHits { get; set; }
        public int RenderCacheMisses { get; set; }
        public int TemplateCacheSize { get; set; }
        public int RenderCacheSize { get; set; }
        public long EstimatedMemoryUsage { get; set; }
        public int CacheClearCount { get; set; }
        
        // 新增属性
        public DateTime LastCleanupTime { get; set; } = DateTime.Now;
        public Dictionary<string, int> EntriesByType { get; set; } = new Dictionary<string, int>();
        
        // 兼容性属性
        public int HitCount => CacheHits;
        public int MissCount => CacheMisses;
        
        // 兼容性属性
        public int TotalEntries => TemplateCacheSize + RenderCacheSize;
        public double HitRatio => CacheHits + CacheMisses > 0 
            ? (double)CacheHits / (CacheHits + CacheMisses) 
            : 0;
        public int TotalRequests => CacheHits + CacheMisses;
        public long TotalMemoryUsage => EstimatedMemoryUsage;
        public TimeSpan AverageRenderTime { get; set; } = TimeSpan.Zero;
            
        public double HitRate => CacheHits + CacheMisses > 0 
            ? (double)CacheHits / (CacheHits + CacheMisses) 
            : 0;
            
        public double RenderHitRate => RenderCacheHits + RenderCacheMisses > 0 
            ? (double)RenderCacheHits / (RenderCacheHits + RenderCacheMisses) 
            : 0;
    }

    /// <summary>
    /// 渲染性能记录
    /// </summary>
    public class RenderPerformance
    {
        public string CacheKey { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public bool CacheHit { get; set; }
        
        // 兼容性属性 - 改为可写属性
        public string TemplateKey { get; set; } = "";
        public TimeSpan RenderTime { get; set; }
        
        // 初始化时设置默认值
        public RenderPerformance()
        {
            TemplateKey = CacheKey;
            RenderTime = ElapsedTime;
        }
        
        public int DataSize { get; set; } = 0;
        public int VariableCount { get; set; } = 0;
    }
}