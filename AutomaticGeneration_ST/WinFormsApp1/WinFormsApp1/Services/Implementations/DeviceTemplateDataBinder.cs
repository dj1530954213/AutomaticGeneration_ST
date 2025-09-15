using AutomaticGeneration_ST.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 设备模板数据绑定服务
    /// 专门处理设备模板（如阀门模板）的特殊数据填充逻辑
    /// </summary>
    public class DeviceTemplateDataBinder
    {
        private readonly LogService _logger = LogService.Instance;
        
        // 缓存机制防止重复调用
        private readonly Dictionary<string, Dictionary<string, object>> _bindingCache = new();
        private readonly Dictionary<string, DateTime> _lastProcessTime = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5); // 5分钟缓存超时
        
        // 调用频率限制器
        private readonly Dictionary<string, int> _callCounter = new();
        private readonly Dictionary<string, DateTime> _callResetTime = new();
        private const int MAX_CALLS_PER_MINUTE = 10; // 每分钟最多10次调用

        /// <summary>
        /// 为设备模板绑定数据
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="templateContent">模板内容</param>
        /// <returns>绑定数据的字典</returns>
        public Dictionary<string, object> BindDeviceTemplateData(Device device, string templateContent)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            if (string.IsNullOrWhiteSpace(templateContent))
                throw new ArgumentException("模板内容不能为空", nameof(templateContent));

            // 缓存键加入别名签名，确保别名变化时不会命中旧缓存
            var aliasSig = (device.AliasIndex != null && device.AliasIndex.Count > 0)
                ? string.Join("|", device.AliasIndex.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}")).GetHashCode().ToString()
                : "noalias";
            var cacheKey = $"{device.DeviceTag}_{templateContent.GetHashCode()}_{aliasSig}";
            
            // 检查调用频率限制
            if (!CheckCallFrequencyLimit(cacheKey))
            {
                _logger.LogWarning($"⚡ 设备 [{device.DeviceTag}] 调用频率过高，已被限制");
                return GetCachedOrEmpty(cacheKey, device);
            }

            // 检查缓存
            if (_bindingCache.ContainsKey(cacheKey) && 
                _lastProcessTime.ContainsKey(cacheKey) &&
                DateTime.Now - _lastProcessTime[cacheKey] < _cacheTimeout)
            {
                _logger.LogInfo($"💾 从缓存获取设备 [{device.DeviceTag}] 的绑定数据");
                return _bindingCache[cacheKey];
            }

            var dataBinding = new Dictionary<string, object>();

            try
            {
                _logger.LogInfo($"🔗 开始为设备 [{device.DeviceTag}] 绑定模板数据...");

                // 1. 绑定设备位号
                dataBinding["device_tag"] = device.DeviceTag;
                _logger.LogInfo($"   ✓ 设备位号: {device.DeviceTag}");

                // 2. 提取模板中的占位符
                var placeholders = ExtractPlaceholders(templateContent);
                _logger.LogInfo($"   📝 发现 {placeholders.Count} 个占位符: {string.Join(", ", placeholders)}");

                // 3. 仅通过“别名”精确匹配，占位符必须在设备别名中存在；否则抛出异常
                var pointBindings = new Dictionary<string, int>();
                foreach (var placeholder in placeholders)
                {
                    if (placeholder.Equals("device_tag", StringComparison.OrdinalIgnoreCase))
                        continue; // device_tag已经处理过

                    if (device.TryGetHmiByAlias(placeholder, out var hmi))
                    {
                        dataBinding[placeholder] = hmi ?? string.Empty; // HMI 允许为空 → 空字符串
                        pointBindings[placeholder] = 1;
                        _logger.LogInfo($"   ✓ 别名匹配 {placeholder} -> {(string.IsNullOrEmpty(hmi) ? "<空字符串>" : hmi)}");
                    }
                    else
                    {
                        throw new KeyNotFoundException($"设备[{device.DeviceTag}] 模板占位符 '{placeholder}' 在“设备分类表”的“别名”列中未找到对应行");
                    }
                }

                _logger.LogSuccess($"🎯 设备 [{device.DeviceTag}] 数据绑定完成，成功绑定 {pointBindings.Count} 个点位");

                // 保存到缓存
                _bindingCache[cacheKey] = dataBinding;
                _lastProcessTime[cacheKey] = DateTime.Now;

                return dataBinding;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ 设备模板数据绑定失败: {ex.Message}");
                throw new Exception($"设备模板数据绑定失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从模板内容中提取占位符
        /// </summary>
        /// <param name="templateContent">模板内容</param>
        /// <returns>占位符列表</returns>
        private List<string> ExtractPlaceholders(string templateContent)
        {
            var placeholders = new HashSet<string>();

            // 使用正则表达式匹配 {{placeholder}} 格式的占位符
            var pattern = @"\{\{([^}]+)\}\}";
            var matches = Regex.Matches(templateContent, pattern);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var placeholder = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(placeholder) && 
                        !placeholder.StartsWith("#") && // 跳过注释
                        !placeholder.StartsWith("/") && // 跳过Scriban控制语句
                        !placeholder.Contains("for") && // 跳过循环语句
                        !placeholder.Contains("end"))   // 跳过结束语句
                    {
                        placeholders.Add(placeholder);
                    }
                }
            }

            return placeholders.ToList();
        }

        /// <summary>
        /// 生成设备模板的完整数据上下文
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="templateContent">模板内容</param>
        /// <returns>完整的数据上下文</returns>
        public Dictionary<string, object> GenerateDeviceTemplateContext(Device device, string templateContent)
        {
            var context = BindDeviceTemplateData(device, templateContent);
            
            // 添加设备的基本信息
            context["device"] = device;
            context["device_name"] = device.DeviceTag;
            context["template_name"] = device.TemplateName;
            context["io_point_count"] = device.IoPoints?.Count ?? 0;
            context["device_point_count"] = device.DevicePoints?.Count ?? 0;
            context["total_point_count"] = (device.IoPoints?.Count ?? 0) + (device.DevicePoints?.Count ?? 0);
            
            // 添加点位集合（用于模板中的循环） - 保持兼容性
            #pragma warning disable CS0618 // 忽略过时警告
            context["points"] = device.Points?.Values.ToList() ?? new List<Models.Point>();
            #pragma warning restore CS0618
            
            // 添加新的字典格式的点位数据
            context["io_points"] = device.IoPoints ?? new Dictionary<string, Dictionary<string, object>>();
            context["device_points"] = device.DevicePoints ?? new Dictionary<string, Dictionary<string, object>>();
            
            return context;
        }

        /// <summary>
        /// 检查调用频率限制
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <returns>是否允许调用</returns>
        private bool CheckCallFrequencyLimit(string cacheKey)
        {
            var now = DateTime.Now;
            
            // 重置计数器（每分钟重置一次）
            if (_callResetTime.ContainsKey(cacheKey))
            {
                if (now - _callResetTime[cacheKey] >= TimeSpan.FromMinutes(1))
                {
                    _callCounter[cacheKey] = 0;
                    _callResetTime[cacheKey] = now;
                }
            }
            else
            {
                _callCounter[cacheKey] = 0;
                _callResetTime[cacheKey] = now;
            }

            // 检查调用次数
            if (_callCounter.ContainsKey(cacheKey))
            {
                _callCounter[cacheKey]++;
                return _callCounter[cacheKey] <= MAX_CALLS_PER_MINUTE;
            }

            _callCounter[cacheKey] = 1;
            return true;
        }

        /// <summary>
        /// 获取缓存数据或返回空数据
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="device">设备对象</param>
        /// <returns>数据字典</returns>
        private Dictionary<string, object> GetCachedOrEmpty(string cacheKey, Device device)
        {
            // 如果有缓存，返回缓存
            if (_bindingCache.ContainsKey(cacheKey))
            {
                return _bindingCache[cacheKey];
            }

            // 返回最基本的数据
            return new Dictionary<string, object>
            {
                ["device_tag"] = device.DeviceTag
            };
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public void ClearExpiredCache()
        {
            var now = DateTime.Now;
            var expiredKeys = _lastProcessTime
                .Where(kvp => now - kvp.Value > _cacheTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _bindingCache.Remove(key);
                _lastProcessTime.Remove(key);
                _callCounter.Remove(key);
                _callResetTime.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogInfo($"🧹 清理了 {expiredKeys.Count} 个过期缓存项");
            }
        }
    }
}