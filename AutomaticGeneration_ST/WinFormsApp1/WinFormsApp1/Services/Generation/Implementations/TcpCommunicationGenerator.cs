using AutomaticGeneration_ST.Services;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Generation;
using AutomaticGeneration_ST.Services.VariableBlocks;
using AutomaticGeneration_ST.Services.Interfaces;
using AutomaticGeneration_ST.Models;
using WinFormsApp1.Models;
using PointModel = AutomaticGeneration_ST.Models.Point;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using WinFormsApp1.Generators;

namespace AutomaticGeneration_ST.Services.Generation.Implementations
{
    /// <summary>
    /// 基于 <see cref="TcpCodeGenerator"/> 的 Modbus TCP 通讯 ST 代码生成器，
    /// 同时负责收集、解析并注册变量模板块，确保变量表覆盖 TCP 通讯脚本。
    /// </summary>
    public class TcpCommunicationGenerator : IModbusTcpConfigGenerator
    {
        private readonly TcpCodeGenerator _codeGenerator;
        private readonly string _templateDirectory;
        private readonly TemplateMapping _mapping;

        public TcpCommunicationGenerator(string templateDirectory, string mappingFilePath)
        {
            _templateDirectory = templateDirectory ?? throw new ArgumentNullException(nameof(templateDirectory));
            if (string.IsNullOrWhiteSpace(mappingFilePath))
                throw new ArgumentNullException(nameof(mappingFilePath));
            if (!File.Exists(mappingFilePath))
            {
                // 尝试回退到应用根目录查找
                var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template-mapping.json");
                if (!File.Exists(fallback))
                    throw new FileNotFoundException($"模板映射文件不存在: {mappingFilePath} 或 {fallback}");
                mappingFilePath = fallback;
            }

            var json = File.ReadAllText(mappingFilePath);
            _mapping = JsonSerializer.Deserialize<TemplateMapping>(json) ?? new TemplateMapping();

            // 编译所需模板
            var compiled = new Dictionary<string, Template>();
            foreach (var key in new[] { "TCP_ANALOG", "TCP_DIGITAL" })
            {
                if (_mapping.Mappings.TryGetValue(key, out var relPath))
                {
                    var fullPath = Path.Combine(_templateDirectory, relPath);
                    if (File.Exists(fullPath))
                    {
                        compiled[key] = Template.Parse(File.ReadAllText(fullPath));
                    }
                }
            }
            _codeGenerator = new TcpCodeGenerator(compiled);
        }

        public List<GenerationResult> Generate(DataContext context)
        {
            var results = new List<GenerationResult>();
            if (context?.Metadata == null)
                return results;

            if (!(context.Metadata.TryGetValue("TcpProcessingEnabled", out var enabledObj) && enabledObj is bool enabled && enabled))
                return results;

            var analogPoints = context.Metadata.TryGetValue("TcpAnalogPoints", out var aObj) && aObj is List<TcpAnalogPoint> aList ? aList : new List<TcpAnalogPoint>();
            var digitalPoints = context.Metadata.TryGetValue("TcpDigitalPoints", out var dObj) && dObj is List<TcpDigitalPoint> dList ? dList : new List<TcpDigitalPoint>();

            // ---- 模拟量 ----
            if (analogPoints.Any())
            {
                var code = _codeGenerator.GenerateCode(analogPoints);
                var entries = CollectVariableEntries("TCP_ANALOG", analogPoints);
                results.Add(new GenerationResult
                {
                    FileName = "TCP_ANALOG.st",
                    Content = code,
                    Category = "Communication",
                    VariableEntries = entries
                });
            }

            // ---- 数字量 ----
            if (digitalPoints.Any())
            {
                var code = _codeGenerator.GenerateCode(digitalPoints);
                var entries = CollectVariableEntries("TCP_DIGITAL", digitalPoints);
                results.Add(new GenerationResult
                {
                    FileName = "TCP_DIGITAL.st",
                    Content = code,
                    Category = "Communication",
                    VariableEntries = entries
                });
            }

            return results;
        }

        private List<VariableTableEntry> CollectVariableEntries(string templateKey, IEnumerable<object> points)
        {
            if (!_mapping.Mappings.TryGetValue(templateKey, out var relPath))
                return new List<VariableTableEntry>();

            var mainTemplatePath = Path.Combine(_templateDirectory, relPath);
            if (!File.Exists(mainTemplatePath))
                throw new FileNotFoundException($"TCP模板不存在: {mainTemplatePath}");

            // 严格模式：让 VariableBlockCollector/Parser 的异常冒泡
            var varBlocks = VariableBlockCollector.Collect(mainTemplatePath, points);
            var entries = VariableBlockParser.Parse(varBlocks);
            var programName = TemplateMetadataCache.GetProgramName(mainTemplatePath) ?? templateKey;
            foreach (var e in entries)
            {
                e.ProgramName = programName;
            }

            VariableEntriesRegistry.AddEntries(programName, entries);
            return entries;
        }
    }
}
