using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Interfaces;
using Scriban;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutomaticGeneration_ST.Services.Implementations
{
    public class GenerationOrchestratorService
    {
        private readonly IConfigurationService _configService;
        private readonly IDeviceStGenerator _deviceGenerator;
        private readonly string _templateDirectory;
        private readonly TemplateMapping _mappings;

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

            foreach (var device in devices)
            {
                // 步骤1: 从配置中查找Scriban文件名
                if (!_mappings.Mappings.TryGetValue(device.TemplateName, out var scribanFileName))
                {
                    // 关键的错误处理：如果设备分类表中的模板名在配置中找不到，则跳过并警告
                    System.Diagnostics.Debug.WriteLine($"错误: 设备'{device.DeviceTag}'的模板名'{device.TemplateName}'在配置文件中没有找到对应的映射。");
                    continue;
                }

                // 步骤2: 定位并加载模板文件
                var templatePath = Path.Combine(_templateDirectory, scribanFileName);
                if (!File.Exists(templatePath))
                {
                    System.Diagnostics.Debug.WriteLine($"错误: 映射成功，但模板文件'{templatePath}'不存在。");
                    continue;
                }

                var templateContent = File.ReadAllText(templatePath);
                var template = Template.Parse(templateContent);

                // 步骤3: 调用已注入的生成器服务执行渲染
                var result = _deviceGenerator.Generate(device, template);
                allResults.Add(result);
            }
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