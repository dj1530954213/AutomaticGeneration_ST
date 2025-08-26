using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Interfaces;
using Scriban;
using AutomaticGeneration_ST.Services.VariableBlocks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.Implementations
{
    public class GenerationOrchestratorService
    {
        private readonly IConfigurationService _configService;
        private readonly IDeviceStGenerator _deviceGenerator;
        private readonly string _templateDirectory;
        private readonly TemplateMapping _mappings;
        private readonly LogService _logger = LogService.Instance;
        
        // 添加模板缓存避免重复读取和解析
        private readonly Dictionary<string, Template> _templateCache = new();
        private readonly Dictionary<string, DateTime> _templateCacheTime = new();
        private readonly TimeSpan _templateCacheTimeout = TimeSpan.FromMinutes(10);

        // 使用构造函数注入依赖，这是现代软件设计的最佳实践
        public GenerationOrchestratorService(
            string templateDirectory, 
            string configFilePath,
            IConfigurationService configService, 
            IDeviceStGenerator deviceGenerator)
        {
            _templateDirectory = templateDirectory;
            _configService = configService;
            _deviceGenerator = deviceGenerator;

            // 在构造时就加载好配置，一次加载，多次使用
            _mappings = _configService.LoadTemplateMappings(configFilePath);
        }

        public List<GenerationResult> GenerateForDevices(IEnumerable<Device> devices)
        {
            var allResults = new List<GenerationResult>();
            var deviceList = devices.ToList();
            
            _logger.LogInfo($"🏭 开始处理 {deviceList.Count} 个设备的ST代码生成");

            // 按模板名称分组，避免重复加载相同模板
            var devicesByTemplate = deviceList
                .Where(d => !string.IsNullOrWhiteSpace(d.TemplateName))
                .GroupBy(d => d.TemplateName)
                .ToList();

            _logger.LogInfo($"📊 发现 {devicesByTemplate.Count} 种不同的模板类型");

            foreach (var templateGroup in devicesByTemplate)
            {
                var templateName = templateGroup.Key;
                var templateDevices = templateGroup.ToList();
                
                _logger.LogInfo($"🔧 批量处理模板: [{templateName}]，设备数量: {templateDevices.Count}");
                
                // 步骤1: 从配置中查找Scriban文件名
                if (!_mappings.Mappings.TryGetValue(templateName, out var scribanFileName))
                {
                    _logger.LogError($"❌ 模板名'{templateName}'在配置文件中没有找到对应的映射");
                    _logger.LogWarning($"   💡 可用的模板映射: {string.Join(", ", _mappings.Mappings.Keys)}");
                    
                    // 跳过整个模板组
                    foreach (var device in templateDevices)
                    {
                        _logger.LogWarning($"   ⚠️ 跳过设备: [{device.DeviceTag}]");
                    }
                    continue;
                }
                
                _logger.LogInfo($"   ✓ 找到模板映射: {templateName} -> {scribanFileName}");

                // 步骤2: 获取或加载模板（使用缓存）
                var template = GetCachedTemplate(scribanFileName);
                if (template == null)
                {
                    _logger.LogError($"❌ 无法加载模板文件: {scribanFileName}");
                    continue;
                }

                // 步骤3: 批量处理同一模板的所有设备
                foreach (var device in templateDevices)
                {
                    try
                    {
                        var result = _deviceGenerator.Generate(device, template);

                        // >>> Populate VariableEntries via VariableBlockCollector & VariableBlockParser
                        if (result != null)
                        {
                            // 严格模式：任何变量模板声明/解析错误都应中止该设备生成
                            var varBlocks = VariableBlockCollector.Collect(
                                Path.Combine(_templateDirectory, scribanFileName),
                                device.Points.Values,
                                device.DeviceTag,
                                renderOnce: true);
                            var entries = VariableBlockParser.Parse(varBlocks);
                            // 填充 ProgramName 方便后续生成变量表
                            foreach (var entry in entries)
                            {
                                // 使用模板分组键作为 ProgramName，确保设备级变量按模板归类（如 XV_CTRL）
                                // 如果需要与主模板声明的程序名一致，可在此处扩展解析主模板以获取 ProgramName
                                entry.ProgramName = templateName;
                            }
                            result.VariableEntries = entries;
                            VariableEntriesRegistry.AddEntries(templateName, entries);
                            _logger.LogInfo($"   ⇢ 已为模板 [{templateName}] 注册 {entries.Count} 条设备级变量");
                        }
                        if (result != null)
                        {
                            allResults.Add(result);
                        }
                        else
                        {
                            _logger.LogWarning($"   ⚠️ 设备 [{device.DeviceTag}] 生成结果为空");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError($"❌ 设备 [{device.DeviceTag}] ST代码生成失败: {ex.Message}");
                    }
                }
                
                _logger.LogSuccess($"   ✅ 模板 [{templateName}] 批量处理完成，成功生成 {templateDevices.Count} 个设备的代码");
            }

            // 处理没有模板名称的设备
            var devicesWithoutTemplate = deviceList.Where(d => string.IsNullOrWhiteSpace(d.TemplateName)).ToList();
            if (devicesWithoutTemplate.Any())
            {
                _logger.LogWarning($"⚠️ 发现 {devicesWithoutTemplate.Count} 个没有指定模板名称的设备，将跳过处理");
                foreach (var device in devicesWithoutTemplate)
                {
                    _logger.LogWarning($"   ⚠️ 跳过设备: [{device.DeviceTag}] (无模板名称)");
                }
            }
            
            _logger.LogSuccess($"🎯 设备ST代码生成完成，共生成 {allResults.Count} 个文件");
            
            // 定期清理过期缓存
            CleanExpiredTemplateCache();
            
            return allResults;
        }

        /// <summary>
        /// 编排整个IO映射的生成流程。
        /// 它负责分组点位，并为每个组查找、加载并分发正确的Scriban模板。
        /// </summary>
        /// <param name="allPoints">所有点位的集合。</param>
        /// <param name="ioGenerator">注入的IO生成器实例。</param>
        /// <returns>所有IO映射文件的生成结果列表。</returns>
        public List<GenerationResult> GenerateForIoMappings(IEnumerable<Models.Point> allPoints, IIoMappingGenerator ioGenerator)
        {
            var allIoResults = new List<GenerationResult>();

            // 步骤 1: 调试 - 检查输入数据
            var allPointsList = allPoints.ToList();
            _logger.LogInfo($"📊 IO映射输入数据统计 - 总点位数: {allPointsList.Count}");
            
            // 分析点位类型分布
            var pointTypeStats = allPointsList.GroupBy(p => p.PointType ?? "空").ToDictionary(g => g.Key, g => g.Count());
            foreach (var stat in pointTypeStats)
            {
                _logger.LogInfo($"   点位类型 [{stat.Key}]: {stat.Value} 个");
            }
            
            // 分析模块类型分布
            var moduleTypeStats = allPointsList.Where(p => !string.IsNullOrWhiteSpace(p.ModuleType))
                .GroupBy(p => p.ModuleType).ToDictionary(g => g.Key, g => g.Count());
            _logger.LogInfo($"📊 模块类型统计 - 总数: {moduleTypeStats.Count}");
            foreach (var stat in moduleTypeStats)
            {
                _logger.LogInfo($"   模块类型 [{stat.Key}]: {stat.Value} 个");
            }

            // 步骤 2: 筛选并分组IO点位 - 放宽筛选条件
            // 只要有ModuleType（AI/AO/DI/DO）就认为是IO点位，不再严格要求PointType="硬点"
            var filteredPoints = allPointsList
                .Where(p => !string.IsNullOrWhiteSpace(p.ModuleType) && 
                           (p.ModuleType == "AI" || p.ModuleType == "AO" || p.ModuleType == "DI" || p.ModuleType == "DO"))
                .ToList();
            
            _logger.LogInfo($"🔍 筛选结果 - 符合条件的IO点位数: {filteredPoints.Count}");
            _logger.LogInfo($"   新筛选条件: ModuleType 为 AI/AO/DI/DO");
            
            if (filteredPoints.Count == 0)
            {
                _logger.LogWarning($"⚠️ 没有找到符合条件的IO点位！");
                _logger.LogWarning($"   筛选条件: ModuleType 必须是 AI/AO/DI/DO 之一");
                return allIoResults;
            }

            var groupedPoints = filteredPoints.GroupBy(p => p.ModuleType);
            
            // 调试分组结果
            var groupStats = groupedPoints.ToDictionary(g => g.Key ?? "空", g => g.Count());
            _logger.LogInfo($"🎯 IO分组统计 - 分组数: {groupStats.Count}");
            foreach (var stat in groupStats)
            {
                _logger.LogInfo($"   分组 [{stat.Key}]: {stat.Value} 个硬点");
            }

            // 步骤 3: 遍历每个IO分组 (AI, DI, DO, AO)
            foreach (var group in groupedPoints)
            {
                var moduleType = group.Key; // "AI", "DI", etc.
                if (string.IsNullOrWhiteSpace(moduleType)) 
                {
                    _logger.LogWarning($"⚠️ 跳过空的模块类型");
                    continue;
                }

                _logger.LogInfo($"🔧 处理模块类型: [{moduleType}], 点位数: {group.Count()}");

                // 步骤 4: 动态构建用于在配置中查找的Key
                // 例如，如果 moduleType 是 "AI"，那么 mappingKey 就是 "AI_MAPPING"
                var mappingKey = $"{moduleType.ToUpper()}_MAPPING";
                _logger.LogInfo($"   🔍 查找配置映射: {mappingKey}");

                // 步骤 5: 从已加载的配置中，查找该IO类型对应的Scriban文件名
                if (!_mappings.Mappings.TryGetValue(mappingKey, out var scribanFileName))
                {
                    // 如果在 template-mapping.json 中找不到如 "DI_MAPPING" 的配置，则跳过并警告
                    _logger.LogError($"❌ IO类型 '{moduleType}' 在配置文件中没有找到对应的映射 '{mappingKey}'");
                    _logger.LogWarning($"   💡 可用的配置映射: {string.Join(", ", _mappings.Mappings.Keys)}");
                    continue;
                }
                
                _logger.LogInfo($"   ✓ 找到模板文件: {scribanFileName}");

                // 步骤 5: 定位并加载该组专属的模板文件
                var templatePath = Path.Combine(_templateDirectory, scribanFileName);
                if (!File.Exists(templatePath))
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 映射成功，但IO模板文件 '{templatePath}' 不存在。");
                    continue;
                }

                var templateContent = File.ReadAllText(templatePath);
                var template = Template.Parse(templateContent);

                // 步骤 6: 调用职责单一的渲染器，传入当前组的点位和专属模板
                var singleResult = ioGenerator.Generate(moduleType, group.ToList(), template);

                // === 追加变量模板处理，确保IO映射脚本的变量也被变量表捕获 ===
                // 严格模式：IO映射的变量模板声明/解析错误必须中止
                var varBlocks = VariableBlockCollector.Collect(templatePath, group.ToList());
                var varEntries = VariableBlockParser.Parse(varBlocks);
                // 为每条条目设置 ProgramName，保持与模板声明一致，如 AI_CONVERT/DI_CONVERT 等
                var programName = $"{moduleType.ToUpper()}_CONVERT";
                foreach (var ve in varEntries)
                {
                    ve.ProgramName = programName;
                }
        
                // 保存至结果对象以及全局注册表
                if (singleResult != null)
                {
                    singleResult.VariableEntries = varEntries;
                }
                VariableEntriesRegistry.AddEntries(programName, varEntries);

                allIoResults.Add(singleResult);
            }

            return allIoResults;
        }

        /// <summary>
        /// 获取缓存的模板，如果不存在则加载并缓存
        /// </summary>
        /// <param name="scribanFileName">模板文件名</param>
        /// <returns>模板对象，加载失败返回null</returns>
        private Template GetCachedTemplate(string scribanFileName)
        {
            var cacheKey = scribanFileName;
            var now = DateTime.Now;

            // 检查缓存是否存在且未过期
            if (_templateCache.ContainsKey(cacheKey) && 
                _templateCacheTime.ContainsKey(cacheKey) &&
                now - _templateCacheTime[cacheKey] < _templateCacheTimeout)
            {
                return _templateCache[cacheKey];
            }

            // 加载模板文件
            var templatePath = Path.Combine(_templateDirectory, scribanFileName);
            if (!File.Exists(templatePath))
            {
                _logger.LogError($"❌ 模板文件不存在: {templatePath}");
                return null;
            }

            try
            {
                var templateContent = File.ReadAllText(templatePath);
                var template = Template.Parse(templateContent);
                
                if (template.HasErrors)
                {
                    var errors = string.Join(", ", template.Messages.Select(m => m.Message));
                    _logger.LogError($"❌ 模板解析错误: {errors}");
                    return null;
                }

                // 保存到缓存
                _templateCache[cacheKey] = template;
                _templateCacheTime[cacheKey] = now;
                
                _logger.LogInfo($"📦 模板已缓存: {scribanFileName}");
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ 模板加载失败: {scribanFileName}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清理过期的模板缓存
        /// </summary>
        private void CleanExpiredTemplateCache()
        {
            var now = DateTime.Now;
            var expiredKeys = _templateCacheTime
                .Where(kvp => now - kvp.Value > _templateCacheTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _templateCache.Remove(key);
                _templateCacheTime.Remove(key);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogInfo($"🧹 清理了 {expiredKeys.Count} 个过期的模板缓存项");
            }
        }

        /// <summary>
        /// 清理所有模板缓存
        /// </summary>
        public void ClearAllTemplateCache()
        {
            _templateCache.Clear();
            _templateCacheTime.Clear();
            _logger.LogInfo("🧹 已清理所有模板缓存");
        }
    }
}