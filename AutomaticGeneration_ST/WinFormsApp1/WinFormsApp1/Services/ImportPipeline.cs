using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Implementations;
using AutomaticGeneration_ST.Services.Interfaces;
using AutomaticGeneration_ST.Services.Generation.Implementations;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// 导入管道 - 单一入口处理整个数据流
    /// 实现"Excel解析 → 设备分类 → 代码生成 → 统计信息"的完整链路
    /// </summary>
    public class ImportPipeline
    {
        private readonly LogService _logger = LogService.Instance;
        private readonly ExcelDataService _excelDataService;
        private readonly STGenerationService _stGenerationService;
        private readonly GenerationOrchestratorService _orchestratorService;

        public ImportPipeline()
        {
            _excelDataService = new ExcelDataService();
            _stGenerationService = new STGenerationService();
            
            // 初始化编排服务
            var templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            var configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template-mapping.json");
            var configService = new JsonConfigurationService();
            var deviceGenerator = new ScribanDeviceGenerator();
            
            _orchestratorService = new GenerationOrchestratorService(
                templateDirectory, configFilePath, configService, deviceGenerator);
        }

        /// <summary>
        /// 执行完整的导入和处理管道
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <returns>完整的项目缓存对象</returns>
        public async Task<ProjectCache> ImportAsync(string excelFilePath)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            var startTime = DateTime.Now;
            
            _logger.LogInfo($"[{operationId}] 🚀 开始执行导入管道: {Path.GetFileName(excelFilePath)}");

            try
            {
                // =================== 第1步: Excel解析和设备分类 ===================
                _logger.LogInfo($"[{operationId}] 📂 步骤1: 解析Excel文件和构建设备...");
                var dataContext = await Task.Run(() => _excelDataService.LoadData(excelFilePath));
                
                if (dataContext?.AllPointsMasterList == null || dataContext.AllPointsMasterList.Count == 0)
                {
                    throw new InvalidDataException("Excel解析失败或没有有效的点位数据");
                }

                _logger.LogSuccess($"[{operationId}] ✅ Excel解析完成 - 设备:{dataContext.Devices.Count}, 点位:{dataContext.AllPointsMasterList.Count}");

                // =================== 第2步: IO映射代码生成 ===================
                _logger.LogInfo($"[{operationId}] 🔧 步骤2: 生成IO映射代码...");
                var ioGenerator = new ScribanIoMappingGenerator();
                var ioResults = await Task.Run(() => 
                    _orchestratorService.GenerateForIoMappings(dataContext.AllPointsMasterList.Values, ioGenerator));
                
                var ioMappingScripts = ioResults.Select(r => r.Content).ToList();
                _logger.LogSuccess($"[{operationId}] ✅ IO映射生成完成 - 文件数:{ioResults.Count}");

                // =================== 第3步: 设备ST程序生成 ===================
                _logger.LogInfo($"[{operationId}] 🎨 步骤3: 生成设备ST程序...");
                var deviceSTPrograms = new Dictionary<string, List<string>>();
                
                if (dataContext.Devices?.Any() == true)
                {
                    deviceSTPrograms = await Task.Run(() => 
                        _stGenerationService.GenerateDeviceSTPrograms(dataContext.Devices));
                    
                    var totalSTFiles = deviceSTPrograms.Values.Sum(list => list.Count);
                    _logger.LogSuccess($"[{operationId}] ✅ 设备ST生成完成 - 模板数:{deviceSTPrograms.Count}, 文件数:{totalSTFiles}");
                }

                // =================== 第4步: 构建统计信息 ===================
                _logger.LogInfo($"[{operationId}] 📊 步骤4: 生成统计信息...");
                var statistics = BuildStatistics(dataContext, deviceSTPrograms, ioMappingScripts, startTime);

                // =================== 第5步: 构建完整缓存 ===================
                var fileInfo = new FileInfo(excelFilePath);
                var projectCache = new ProjectCache
                {
                    SourceFilePath = excelFilePath,
                    SourceLastWriteTime = fileInfo.LastWriteTime,
                    CacheCreatedTime = DateTime.Now,
                    DataContext = dataContext,
                    DeviceSTPrograms = deviceSTPrograms,
                    IOMappingScripts = ioMappingScripts,
                    Statistics = statistics,
                    Metadata = new Dictionary<string, object>
                    {
                        ["OperationId"] = operationId,
                        ["ProcessingDuration"] = DateTime.Now - startTime,
                        ["ToolVersion"] = "2.0",
                        ["ImportedBy"] = Environment.UserName,
                        ["ImportedAt"] = DateTime.Now
                    }
                };

                var endTime = DateTime.Now;
                _logger.LogSuccess($"[{operationId}] 🎉 导入管道完成! 总耗时: {(endTime - startTime).TotalSeconds:F2}秒");
                _logger.LogInfo($"[{operationId}] 📈 处理结果汇总:");
                _logger.LogInfo($"[{operationId}]   • 设备总数: {statistics.TotalDevices}");
                _logger.LogInfo($"[{operationId}]   • 点位总数: {statistics.TotalPoints}");
                _logger.LogInfo($"[{operationId}]   • ST文件数: {statistics.GeneratedSTFiles}");
                _logger.LogInfo($"[{operationId}]   • IO映射数: {statistics.GeneratedIOMappingFiles}");

                return projectCache;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{operationId}] ❌ 导入管道失败: {ex.Message}");
                throw new Exception($"导入管道执行失败 (操作ID: {operationId}): {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 构建统计信息
        /// </summary>
        private ProjectStatistics BuildStatistics(
            DataContext dataContext, 
            Dictionary<string, List<string>> deviceSTPrograms,
            List<string> ioMappingScripts,
            DateTime startTime)
        {
            var statistics = new ProjectStatistics
            {
                ProcessingStartTime = startTime,
                ProcessingEndTime = DateTime.Now,
                TotalDevices = dataContext.Devices?.Count ?? 0,
                TotalPoints = dataContext.AllPointsMasterList?.Count ?? 0,
                StandalonePoints = dataContext.StandalonePoints?.Count ?? 0,
                GeneratedSTFiles = deviceSTPrograms.Values.Sum(list => list.Count),
                GeneratedIOMappingFiles = ioMappingScripts.Count
            };

            // 按模板分组设备统计
            if (dataContext.Devices?.Any() == true)
            {
                statistics.DevicesByTemplate = dataContext.Devices
                    .Where(d => !string.IsNullOrWhiteSpace(d.TemplateName))
                    .GroupBy(d => d.TemplateName)
                    .ToDictionary(g => g.Key, g => g.Count());
            }

            // 按类型分组点位统计  
            if (dataContext.AllPointsMasterList?.Any() == true)
            {
                statistics.PointsByType = dataContext.AllPointsMasterList.Values
                    .Where(p => !string.IsNullOrWhiteSpace(p.ModuleType))
                    .GroupBy(p => p.ModuleType)
                    .ToDictionary(g => g.Key, g => g.Count());
            }

            // 计算IO点位和设备点位数量
            if (dataContext.Devices?.Any() == true)
            {
                statistics.IoPoints = dataContext.Devices.Sum(d => d.IoPoints?.Count ?? 0);
                statistics.DevicePoints = dataContext.Devices.Sum(d => d.DevicePoints?.Count ?? 0);
            }

            return statistics;
        }

        /// <summary>
        /// 验证Excel文件是否有效
        /// </summary>
        public bool ValidateExcelFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    return false;

                var extension = Path.GetExtension(filePath).ToLower();
                return extension == ".xlsx" || extension == ".xls";
            }
            catch
            {
                return false;
            }
        }
    }
}