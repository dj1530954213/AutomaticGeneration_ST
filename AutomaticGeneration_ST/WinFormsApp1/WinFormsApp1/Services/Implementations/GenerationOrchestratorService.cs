using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Interfaces;
using Scriban;
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

            foreach (var device in deviceList)
            {
                _logger.LogInfo($"   🔧 处理设备: [{device.DeviceTag}] 模板: {device.TemplateName}");
                
                // 步骤1: 从配置中查找Scriban文件名
                if (!_mappings.Mappings.TryGetValue(device.TemplateName, out var scribanFileName))
                {
                    // 关键的错误处理：如果设备分类表中的模板名在配置中找不到，则跳过并警告
                    _logger.LogError($"❌ 设备'{device.DeviceTag}'的模板名'{device.TemplateName}'在配置文件中没有找到对应的映射");
                    _logger.LogWarning($"   💡 可用的模板映射: {string.Join(", ", _mappings.Mappings.Keys)}");
                    continue;
                }
                
                _logger.LogInfo($"   ✓ 找到模板映射: {device.TemplateName} -> {scribanFileName}");

                // 步骤2: 定位并加载模板文件
                var templatePath = Path.Combine(_templateDirectory, scribanFileName);
                if (!File.Exists(templatePath))
                {
                    _logger.LogError($"❌ 映射成功，但模板文件'{templatePath}'不存在");
                    _logger.LogInfo($"   🔍 模板目录: {_templateDirectory}");
                    continue;
                }

                var templateContent = File.ReadAllText(templatePath);
                var template = Template.Parse(templateContent);
                
                if (template.HasErrors)
                {
                    _logger.LogError($"❌ 模板解析错误: {string.Join(", ", template.Messages.Select(m => m.Message))}");
                    continue;
                }
                
                _logger.LogInfo($"   ✓ 模板文件加载成功: {templatePath}");

                // 步骤3: 调用已注入的生成器服务执行渲染
                try
                {
                    var result = _deviceGenerator.Generate(device, template);
                    if (result != null)
                    {
                        allResults.Add(result);
                        _logger.LogInfo($"   ✅ 设备 [{device.DeviceTag}] ST代码生成成功");
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
            
            _logger.LogSuccess($"🎯 设备ST代码生成完成，共生成 {allResults.Count} 个文件");
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

            // 步骤 1: 筛选并分组硬点
            var groupedPoints = allPoints
                .Where(p => p.PointType == "硬点" && !string.IsNullOrWhiteSpace(p.PlcAbsoluteAddress))
                .GroupBy(p => p.ModuleType);

            // 步骤 2: 遍历每个IO分组 (AI, DI, DO, AO)
            foreach (var group in groupedPoints)
            {
                var moduleType = group.Key; // "AI", "DI", etc.
                if (string.IsNullOrWhiteSpace(moduleType)) continue;

                // 步骤 3: 动态构建用于在配置中查找的Key
                // 例如，如果 moduleType 是 "AI"，那么 mappingKey 就是 "AI_MAPPING"
                var mappingKey = $"{moduleType.ToUpper()}_MAPPING";

                // 步骤 4: 从已加载的配置中，查找该IO类型对应的Scriban文件名
                if (!_mappings.Mappings.TryGetValue(mappingKey, out var scribanFileName))
                {
                    // 如果在 template-mapping.json 中找不到如 "DI_MAPPING" 的配置，则跳过并警告
                    System.Diagnostics.Debug.WriteLine($"警告: IO类型 '{moduleType}' 在配置文件中没有找到对应的映射 '{mappingKey}'。");
                    continue;
                }

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
                allIoResults.Add(singleResult);
            }

            return allIoResults;
        }
    }
}