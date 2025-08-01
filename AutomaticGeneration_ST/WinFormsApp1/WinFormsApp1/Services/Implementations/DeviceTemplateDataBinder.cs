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

            var cacheKey = $"{device.DeviceTag}_{templateContent.GetHashCode()}";
            
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

                // 3. 为每个占位符查找对应的点位变量名（使用新的字典结构）
                var pointBindings = new Dictionary<string, int>();
                foreach (var placeholder in placeholders)
                {
                    if (placeholder.Equals("device_tag", StringComparison.OrdinalIgnoreCase))
                        continue; // device_tag已经处理过

                    // 在设备的点位中查找包含该占位符的变量名
                    var matchedVariableName = FindMatchingPointVariable(device, placeholder);
                    if (!string.IsNullOrWhiteSpace(matchedVariableName))
                    {
                        dataBinding[placeholder] = matchedVariableName;
                        pointBindings[placeholder] = 1;
                        _logger.LogInfo($"   ✓ {placeholder} -> {matchedVariableName}");
                    }
                    else
                    {
                        // 如果找不到匹配的点位，使用占位符本身并记录警告
                        dataBinding[placeholder] = $"<{placeholder}>";
                        _logger.LogWarning($"   ⚠️ 未找到匹配的点位: {placeholder}");
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
        /// 在设备的点位中查找匹配指定占位符的点位（新版本支持字典结构）
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="placeholder">占位符</param>
        /// <returns>匹配的变量名，如果未找到则返回null</returns>
        private string FindMatchingPointVariable(Device device, string placeholder)
        {
            // 合并所有点位变量名进行搜索
            var allVariableNames = device.GetAllVariableNames();
            
            if (allVariableNames.Count == 0)
                return null;

            _logger.LogInfo($"   🔍 在 {allVariableNames.Count} 个点位中搜索占位符: {placeholder}");

            // 1. 精确匹配：查找变量名中包含占位符的点位
            var exactMatches = allVariableNames
                .Where(name => name.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (exactMatches.Count == 1)
            {
                return exactMatches.First();
            }

            // 2. 如果有多个匹配，优先选择最接近的匹配
            if (exactMatches.Count > 1)
            {
                var bestMatch = exactMatches
                    .OrderBy(name => Math.Abs(name.Length - placeholder.Length))
                    .ThenBy(name => name)
                    .First();

                _logger.LogInfo($"   🔍 占位符 [{placeholder}] 有多个匹配，选择最佳匹配: {bestMatch}");
                return bestMatch;
            }

            // 3. 模糊匹配：查找描述或其他字段中包含占位符的点位
            foreach (var variableName in allVariableNames)
            {
                var pointData = device.FindPointData(variableName);
                if (pointData != null)
                {
                    // 检查描述字段
                    var description = pointData.GetValueOrDefault("描述信息")?.ToString() ?? 
                                    pointData.GetValueOrDefault("变量描述")?.ToString() ?? "";
                    
                    if (description.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInfo($"   🔍 占位符 [{placeholder}] 通过描述匹配到: {variableName}");
                        return variableName;
                    }
                }
            }

            // 4. 特殊匹配规则：根据占位符的含义进行智能匹配
            return FindPointBySemanticMatchingNew(device, placeholder);
        }

        /// <summary>
        /// 兼容旧版本的FindMatchingPoint方法
        /// </summary>
        private Models.Point FindMatchingPoint(Device device, string placeholder)
        {
            var variableName = FindMatchingPointVariable(device, placeholder);
            if (string.IsNullOrWhiteSpace(variableName))
                return null;

            // 使用兼容的Points属性获取Point对象
            #pragma warning disable CS0618 // 忽略过时警告
            return device.Points.GetValueOrDefault(variableName);
            #pragma warning restore CS0618
        }

        /// <summary>
        /// 根据语义进行智能匹配（新版本支持字典结构）
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="placeholder">占位符</param>
        /// <returns>匹配的变量名</returns>
        private string FindPointBySemanticMatchingNew(Device device, string placeholder)
        {
            var lowerPlaceholder = placeholder.ToLower();

            // 常见的阀门控制信号映射
            var semanticMappings = new Dictionary<string, string[]>
            {
                // 位置反馈信号
                ["xs"] = new[] { "开到位", "开限位", "open", "opened", "XS_" },
                ["ua"] = new[] { "开到位反馈", "开位", "open_fb" },
                ["zsh"] = new[] { "开到位", "开限", "zsh", "ZSH_" },
                ["zsl"] = new[] { "关到位", "关限", "zsl", "ZSL_" },
                ["uia"] = new[] { "状态", "位置", "position" },
                
                // 控制命令信号
                ["c_am"] = new[] { "自动", "auto", "C_AM_" },
                ["s_am"] = new[] { "自动反馈", "auto_fb", "S_AM_", "AM_" },
                ["c_open"] = new[] { "开命令", "open_cmd", "C_OPEN_" },
                ["c_close"] = new[] { "关命令", "close_cmd", "C_CLOSE_" },
                ["s_open"] = new[] { "开反馈", "open_fb", "S_OPEN_" },
                ["s_close"] = new[] { "关反馈", "close_fb", "S_CLOSE_" },
                
                // 输出信号
                ["am"] = new[] { "自动模式", "auto_mode", "AM_" },
                ["x0"] = new[] { "开输出", "open_out", "X0_" },
                ["xc"] = new[] { "关输出", "close_out", "XC_" },
                ["da"] = new[] { "故障", "alarm", "DA_" },
                
                // PID控制器相关信号
                ["pvxs"] = new[] { "PV", "过程变量", "process_value" },
                ["pvua"] = new[] { "PV", "过程变量", "process_value" },
                ["c_am_pv"] = new[] { "C_AM_PV_", "自动" },
                ["s_am_pv"] = new[] { "S_AM_PV_", "AM_PV_", "自动反馈" },
                ["c_fp"] = new[] { "C_FP", "强制" },
                ["s_fp"] = new[] { "S_FP", "强制反馈" },
                ["fp"] = new[] { "FP", "强制" },
                ["out"] = new[] { "OUT", "输出" }
            };

            if (semanticMappings.ContainsKey(lowerPlaceholder))
            {
                var keywords = semanticMappings[lowerPlaceholder];
                var allVariableNames = device.GetAllVariableNames();
                
                foreach (var keyword in keywords)
                {
                    // 先在变量名中查找
                    var matchedByName = allVariableNames
                        .FirstOrDefault(name => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                    
                    if (!string.IsNullOrWhiteSpace(matchedByName))
                    {
                        _logger.LogInfo($"   🧠 占位符 [{placeholder}] 语义匹配到变量名: {matchedByName} (关键词: {keyword})");
                        return matchedByName;
                    }
                    
                    // 再在描述中查找
                    foreach (var variableName in allVariableNames)
                    {
                        var pointData = device.FindPointData(variableName);
                        if (pointData != null)
                        {
                            var description = pointData.GetValueOrDefault("描述信息")?.ToString() ?? 
                                            pointData.GetValueOrDefault("变量描述")?.ToString() ?? "";
                            
                            if (description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInfo($"   🧠 占位符 [{placeholder}] 语义匹配到描述: {variableName} (关键词: {keyword})");
                                return variableName;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 根据语义进行智能匹配（兼容旧版本）
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <param name="placeholder">占位符</param>
        /// <returns>匹配的点位</returns>
        private Models.Point FindPointBySemanticMatching(Device device, string placeholder)
        {
            var variableName = FindPointBySemanticMatchingNew(device, placeholder);
            if (string.IsNullOrWhiteSpace(variableName))
                return null;

            // 使用兼容的Points属性获取Point对象
            #pragma warning disable CS0618 // 忽略过时警告
            return device.Points.GetValueOrDefault(variableName);
            #pragma warning restore CS0618
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