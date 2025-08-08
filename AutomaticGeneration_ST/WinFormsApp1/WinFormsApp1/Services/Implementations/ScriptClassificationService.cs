using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// ST脚本分类服务实现
    /// </summary>
    public class ScriptClassificationService : IScriptClassifier
    {
        /// <summary>
        /// 所有注册的匹配器
        /// </summary>
        private readonly List<IScriptCategoryMatcher> _matchers;
        
        /// <summary>
        /// 日志服务
        /// </summary>
        private readonly LogService _logger;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public ScriptClassificationService()
        {
            _logger = LogService.Instance;
            _matchers = new List<IScriptCategoryMatcher>
            {
                new AiScriptMatcher(),
                new AoScriptMatcher(),
                new DiScriptMatcher(),
                new DoScriptMatcher()
            };
            
            // 按优先级排序
            _matchers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            _logger.LogInfo($"初始化脚本分类服务，加载 {_matchers.Count} 个匹配器");
        }
        
        /// <summary>
        /// 对单个ST脚本进行分类
        /// </summary>
        /// <param name="scriptContent">脚本内容</param>
        /// <returns>分类结果</returns>
        public CategorizedScript ClassifyScript(string scriptContent)
        {
            if (string.IsNullOrWhiteSpace(scriptContent))
            {
                _logger.LogWarning("脚本内容为空，返回未知类型");
                return new CategorizedScript(scriptContent, ScriptCategory.UNKNOWN)
                {
                    ConfidenceScore = 0,
                    MatchedKeywords = new List<string> { "空内容" }
                };
            }
            
            var bestMatch = FindBestMatch(scriptContent);
            
            return new CategorizedScript(scriptContent, bestMatch.Category)
            {
                ConfidenceScore = bestMatch.ConfidenceScore,
                MatchedKeywords = bestMatch.MatchedKeywords
            };
        }
        
        /// <summary>
        /// 批量对ST脚本进行分类
        /// </summary>
        /// <param name="scripts">脚本内容列表</param>
        /// <returns>分类结果列表</returns>
        public List<CategorizedScript> ClassifyScripts(List<string> scripts)
        {
            if (scripts == null || scripts.Count == 0)
            {
                _logger.LogWarning("脚本列表为空，返回空结果");
                return new List<CategorizedScript>();
            }
            
            var results = new List<CategorizedScript>();
            var startTime = DateTime.Now;
            
            _logger.LogInfo($"开始分类 {scripts.Count} 个脚本...");
            
            for (int i = 0; i < scripts.Count; i++)
            {
                try
                {
                    var categorizedScript = ClassifyScript(scripts[i]);
                    results.Add(categorizedScript);
                    
                    // 每处理50个记录一次日志
                    if ((i + 1) % 50 == 0 || i == scripts.Count - 1)
                    {
                        _logger.LogInfo($"已处理 {i + 1}/{scripts.Count} 个脚本");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"分类脚本 {i} 时出错: {ex.Message}");
                    // 添加失败的结果
                    results.Add(new CategorizedScript(scripts[i], ScriptCategory.UNKNOWN)
                    {
                        ConfidenceScore = 0,
                        MatchedKeywords = new List<string> { $"错误: {ex.Message}" }
                    });
                }
            }
            
            var duration = DateTime.Now - startTime;
            _logger.LogInfo($"分类完成，耗时 {duration.TotalSeconds:F2} 秒");
            
            LogClassificationSummary(results);
            
            return results;
        }
        
        /// <summary>
        /// 批量对设备的ST脚本进行分类
        /// </summary>
        /// <param name="devices">设备列表</param>
        /// <returns>分类结果列表</returns>
        public List<CategorizedScript> ClassifyDeviceScripts(List<Device> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                _logger.LogWarning("设备列表为空，返回空结果");
                return new List<CategorizedScript>();
            }
            
            var results = new List<CategorizedScript>();
            var startTime = DateTime.Now;
            
            _logger.LogInfo($"开始分类 {devices.Count} 个设备的ST脚本...");
            
            foreach (var device in devices)
            {
                try
                {
                    // 需要生成设备的ST代码，然后分类
                    // 这里需要与STGenerationService集成
                    // 目前先实现一个简化版本
                    
                    var deviceScripts = GenerateDeviceScripts(device);
                    
                    foreach (var script in deviceScripts)
                    {
                        var categorizedScript = ClassifyScript(script);
                        categorizedScript.DeviceTag = device.DeviceTag;
                        
                        // 添加相关的点位名称
                        categorizedScript.PointNames.AddRange(device.GetAllVariableNames());
                        
                        results.Add(categorizedScript);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"处理设备 {device.DeviceTag} 时出错: {ex.Message}");
                    // 添加失败的结果
                    results.Add(new CategorizedScript($"设备 {device.DeviceTag} 处理失败", ScriptCategory.UNKNOWN)
                    {
                        DeviceTag = device.DeviceTag,
                        ConfidenceScore = 0,
                        MatchedKeywords = new List<string> { $"错误: {ex.Message}" }
                    });
                }
            }
            
            var duration = DateTime.Now - startTime;
            _logger.LogInfo($"设备脚本分类完成，耗时 {duration.TotalSeconds:F2} 秒");
            
            LogClassificationSummary(results);
            
            return results;
        }
        
        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        /// <param name="categorizedScripts">已分类的脚本列表</param>
        /// <returns>统计信息</returns>
        public Dictionary<ScriptCategory, int> GetClassificationStatistics(List<CategorizedScript> categorizedScripts)
        {
            if (categorizedScripts == null || categorizedScripts.Count == 0)
            {
                return new Dictionary<ScriptCategory, int>();
            }
            
            var statistics = new Dictionary<ScriptCategory, int>();
            
            foreach (var script in categorizedScripts)
            {
                if (statistics.ContainsKey(script.Category))
                {
                    statistics[script.Category]++;
                }
                else
                {
                    statistics[script.Category] = 1;
                }
            }
            
            return statistics;
        }
        
        /// <summary>
        /// 查找最佳匹配
        /// </summary>
        /// <param name="scriptContent">脚本内容</param>
        /// <returns>最佳匹配结果</returns>
        private (ScriptCategory Category, int ConfidenceScore, List<string> MatchedKeywords) FindBestMatch(string scriptContent)
        {
            ScriptCategory bestCategory = ScriptCategory.UNKNOWN;
            int bestScore = 0;
            List<string> bestMatchedKeywords = new List<string>();
            string bestReason = "无匹配结果";
            
            foreach (var matcher in _matchers)
            {
                try
                {
                    var result = matcher.IsMatch(scriptContent);
                    
                    if (result.IsMatch && result.ConfidenceScore > bestScore)
                    {
                        bestCategory = matcher.SupportedCategory;
                        bestScore = result.ConfidenceScore;
                        bestMatchedKeywords = result.MatchedKeywords;
                        bestReason = result.Reason;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"在使用 {matcher.GetType().Name} 匹配器时出错: {ex.Message}");
                }
            }
            
            if (bestCategory != ScriptCategory.UNKNOWN)
            {
                _logger.LogDebug($"匹配结果: {bestCategory.GetDescription()} (置信度: {bestScore}%) - {bestReason}");
            }
            else
            {
                _logger.LogDebug($"未找到匹配的分类，标记为 UNKNOWN");
            }
            
            return (bestCategory, bestScore, bestMatchedKeywords);
        }
        
        /// <summary>
        /// 生成设备的ST脚本（简化版本）
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <returns>ST脚本列表</returns>
        private List<string> GenerateDeviceScripts(Device device)
        {
            var scripts = new List<string>();
            
            // TODO: 这里需要和现有的STGenerationService集成
            // 目前先返回一个模拟的ST脚本
            // 基于设备的模板名称生成不同的脚本
            
            if (device.TemplateName?.Contains("AI", StringComparison.OrdinalIgnoreCase) == true)
            {
                scripts.Add($"程序名称:AI_CONVERT\n" +
                          $"(* AI点位: {device.DeviceTag} *)\n" +
                          $"AI_ALARM_{device.DeviceTag}(\n" +
                          $"    IN:=%IW100,\n" +
                          $"    ENG_MAX:=100.0,\n" +
                          $"    ENG_MIN:=0.0\n" +
                          $");");
            }
            else if (device.TemplateName?.Contains("AO", StringComparison.OrdinalIgnoreCase) == true)
            {
                scripts.Add($"程序名称:AO_CTRL\n" +
                          $"(* AO点位: {device.DeviceTag} *)\n" +
                          $"ENGIN_HEX_{device.DeviceTag}(\n" +
                          $"    AV:={device.DeviceTag},\n" +
                          $"    MU:=100.0,\n" +
                          $"    MD:=0.0\n" +
                          $");");
            }
            else
            {
                // 默认生成一个通用脚本
                scripts.Add($"(* 设备: {device.DeviceTag} 模板: {device.TemplateName} *)\n" +
                          $"{device.DeviceTag}_CTRL := TRUE;");
            }
            
            return scripts;
        }
        
        /// <summary>
        /// 记录分类统计信息
        /// </summary>
        /// <param name="results">分类结果列表</param>
        private void LogClassificationSummary(List<CategorizedScript> results)
        {
            var statistics = GetClassificationStatistics(results);
            
            _logger.LogInfo("分类统计结果:");
            foreach (var stat in statistics.OrderByDescending(s => s.Value))
            {
                _logger.LogInfo($"  {stat.Key.GetDescription()}: {stat.Value} 个");
            }
            
            // 统计置信度分布
            var highConfidence = results.Count(r => r.ConfidenceScore >= 80);
            var mediumConfidence = results.Count(r => r.ConfidenceScore >= 60 && r.ConfidenceScore < 80);
            var lowConfidence = results.Count(r => r.ConfidenceScore > 0 && r.ConfidenceScore < 60);
            var unknown = results.Count(r => r.ConfidenceScore == 0);
            
            _logger.LogInfo("置信度分布:");
            _logger.LogInfo($"  高置信度 (>=80%): {highConfidence} 个");
            _logger.LogInfo($"  中置信度 (60-79%): {mediumConfidence} 个");
            _logger.LogInfo($"  低置信度 (1-59%): {lowConfidence} 个");
            _logger.LogInfo($"  未知类型 (0%): {unknown} 个");
        }
    }
}
