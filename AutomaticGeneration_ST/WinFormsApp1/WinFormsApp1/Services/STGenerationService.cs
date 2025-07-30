using AutomaticGeneration_ST.Services.Generation.Implementations;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Implementations;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// ST代码生成的主服务，整合了所有子服务
    /// </summary>
    public class STGenerationService
    {
        private readonly IDataService _dataService;
        private readonly IConfigurationService _configService;
        private readonly IDeviceStGenerator _deviceGenerator;
        private readonly IIoMappingGenerator _ioGenerator;
        private readonly IExportService _exportService;
        private readonly ICommunicationGenerator _communicationGenerator;

        public STGenerationService()
        {
            // 实例化所有服务 (在大型应用中，这一步会由依赖注入容器自动完成)
            _dataService = new ExcelDataService();
            _configService = new JsonConfigurationService();
            _deviceGenerator = new ScribanDeviceGenerator();
            _ioGenerator = new ScribanIoMappingGenerator();
            _exportService = new FileSystemExportService();
            _communicationGenerator = new PlaceholderCommunicationGenerator();
        }

        /// <summary>
        /// 执行完整的ST代码生成流程
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <param name="templateDirectory">模板文件夹路径</param>
        /// <param name="configFilePath">配置文件路径</param>
        /// <param name="exportRootPath">导出根目录</param>
        /// <returns>成功生成的文件数量</returns>
        public int GenerateSTCode(string excelFilePath, string templateDirectory, string configFilePath, string exportRootPath)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            LogInfo($"[{operationId}] 开始ST代码生成流程");
            
            try
            {
                // =================== 1. 验证输入参数 ===================
                LogInfo($"[{operationId}] 验证输入参数...");
                ValidateInputParameters(excelFilePath, templateDirectory, configFilePath, exportRootPath);

                // =================== 2. 执行核心业务流程 ===================
                LogInfo($"[{operationId}] 开始加载Excel数据: {Path.GetFileName(excelFilePath)}");
                var dataContext = LoadDataWithRetry(excelFilePath, operationId);
                LogInfo($"[{operationId}] Excel数据加载完成 - 设备数: {dataContext.Devices.Count}, 点位数: {dataContext.AllPointsMasterList.Count}");

                // 创建编排服务
                LogInfo($"[{operationId}] 创建生成编排服务...");
                var orchestrator = new GenerationOrchestratorService(templateDirectory, configFilePath, _configService, _deviceGenerator);

                // 生成设备代码
                LogInfo($"[{operationId}] 开始生成设备代码...");
                var deviceResults = GenerateWithErrorHandling(() => orchestrator.GenerateForDevices(dataContext.Devices), 
                                                             "设备代码生成", operationId);
                LogInfo($"[{operationId}] 设备代码生成完成 - 文件数: {deviceResults.Count}");

                // 生成IO映射代码
                LogInfo($"[{operationId}] 开始生成IO映射代码...");
                var ioMappingResults = GenerateWithErrorHandling(() => orchestrator.GenerateForIoMappings(dataContext.AllPointsMasterList.Values, _ioGenerator), 
                                                                "IO映射代码生成", operationId);
                LogInfo($"[{operationId}] IO映射代码生成完成 - 文件数: {ioMappingResults.Count}");

                // (可选) 生成通讯代码（目前返回空列表）
                LogInfo($"[{operationId}] 开始生成通讯代码...");
                var commResults = GenerateWithErrorHandling(() => _communicationGenerator.Generate(dataContext), 
                                                           "通讯代码生成", operationId);
                LogInfo($"[{operationId}] 通讯代码生成完成 - 文件数: {commResults.Count}");

                // =================== 3. 汇总所有生成结果 ===================
                var allFinalResults = new List<GenerationResult>();
                allFinalResults.AddRange(deviceResults);
                allFinalResults.AddRange(ioMappingResults);
                allFinalResults.AddRange(commResults);

                // =================== 4. 执行最终的导出操作 ===================
                LogInfo($"[{operationId}] 开始导出文件到: {exportRootPath}");
                ExportWithErrorHandling(exportRootPath, allFinalResults, operationId);
                LogInfo($"[{operationId}] 文件导出完成");

                LogInfo($"[{operationId}] ST代码生成流程完成 - 总文件数: {allFinalResults.Count}");
                return allFinalResults.Count;
            }
            catch (ArgumentException ex)
            {
                LogError($"[{operationId}] 参数错误: {ex.Message}");
                throw new ArgumentException($"参数验证失败: {ex.Message}", ex);
            }
            catch (FileNotFoundException ex)
            {
                LogError($"[{operationId}] 文件未找到: {ex.Message}");
                throw;
            }
            catch (DirectoryNotFoundException ex)
            {
                LogError($"[{operationId}] 目录未找到: {ex.Message}");
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogError($"[{operationId}] 访问权限不足: {ex.Message}");
                throw new UnauthorizedAccessException($"文件访问权限不足，请检查文件是否被其他程序占用: {ex.Message}", ex);
            }
            catch (IOException ex)
            {
                LogError($"[{operationId}] 文件IO错误: {ex.Message}");
                throw new IOException($"文件操作失败，请检查磁盘空间和文件权限: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                LogError($"[{operationId}] 未预期的错误: {ex.Message}");
                LogError($"[{operationId}] 错误堆栈: {ex.StackTrace}");
                throw new Exception($"ST代码生成过程中发生未预期的错误 (操作ID: {operationId}): {ex.Message}", ex);
            }
        }

        private void ValidateInputParameters(string excelFilePath, string templateDirectory, string configFilePath, string exportRootPath)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath))
                throw new ArgumentException("Excel文件路径不能为空");
                
            if (string.IsNullOrWhiteSpace(templateDirectory))
                throw new ArgumentException("模板目录路径不能为空");
                
            if (string.IsNullOrWhiteSpace(configFilePath))
                throw new ArgumentException("配置文件路径不能为空");
                
            if (string.IsNullOrWhiteSpace(exportRootPath))
                throw new ArgumentException("导出目录路径不能为空");

            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException($"Excel文件未找到: {excelFilePath}");
            
            if (!Directory.Exists(templateDirectory))
                throw new DirectoryNotFoundException($"模板目录未找到: {templateDirectory}");
            
            if (!File.Exists(configFilePath))
                throw new FileNotFoundException($"配置文件未找到: {configFilePath}");

            // 检查导出目录的父目录是否存在
            var exportParent = Directory.GetParent(exportRootPath)?.FullName;
            if (!string.IsNullOrEmpty(exportParent) && !Directory.Exists(exportParent))
                throw new DirectoryNotFoundException($"导出目录的父目录不存在: {exportParent}");
        }

        private DataContext LoadDataWithRetry(string excelFilePath, string operationId, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return _dataService.LoadData(excelFilePath);
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    LogWarning($"[{operationId}] Excel数据加载第{attempt}次尝试失败: {ex.Message}，将重试...");
                    System.Threading.Thread.Sleep(1000 * attempt); // 递增延迟
                }
            }
            
            // 最后一次尝试，不捕获异常
            return _dataService.LoadData(excelFilePath);
        }

        private List<GenerationResult> GenerateWithErrorHandling(Func<List<GenerationResult>> generateFunc, string operationName, string operationId)
        {
            try
            {
                return generateFunc();
            }
            catch (Exception ex)
            {
                LogError($"[{operationId}] {operationName}失败: {ex.Message}");
                throw new Exception($"{operationName}失败: {ex.Message}", ex);
            }
        }

        private void ExportWithErrorHandling(string exportRootPath, List<GenerationResult> results, string operationId)
        {
            try
            {
                _exportService.Export(exportRootPath, results);
            }
            catch (Exception ex)
            {
                LogError($"[{operationId}] 文件导出失败: {ex.Message}");
                throw new Exception($"文件导出失败: {ex.Message}", ex);
            }
        }

        // 简单的日志记录方法（在实际项目中应该使用专业的日志框架如NLog或Serilog）
        private void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            System.Diagnostics.Debug.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }

        private void LogWarning(string message)
        {
            Console.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            System.Diagnostics.Debug.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }

        private void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
            System.Diagnostics.Debug.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }

        /// <summary>
        /// 获取生成统计信息
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <returns>统计信息</returns>
        public GenerationStatistics GetStatistics(string excelFilePath)
        {
            try
            {
                var dataContext = _dataService.LoadData(excelFilePath);
                
                return new GenerationStatistics
                {
                    TotalPoints = dataContext.AllPointsMasterList.Count,
                    DeviceCount = dataContext.Devices.Count,
                    StandalonePointsCount = dataContext.StandalonePoints.Count,
                    PointTypeBreakdown = GetPointTypeBreakdown(dataContext.AllPointsMasterList.Values)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"获取统计信息时发生错误: {ex.Message}", ex);
            }
        }

        private Dictionary<string, int> GetPointTypeBreakdown(IEnumerable<Models.Point> points)
        {
            var breakdown = new Dictionary<string, int>();
            
            foreach (var point in points)
            {
                var moduleType = point.ModuleType ?? "未知";
                if (breakdown.ContainsKey(moduleType))
                    breakdown[moduleType]++;
                else
                    breakdown[moduleType] = 1;
            }

            return breakdown;
        }
    }

    /// <summary>
    /// 生成统计信息
    /// </summary>
    public class GenerationStatistics
    {
        public int TotalPoints { get; set; }
        public int DeviceCount { get; set; }
        public int StandalonePointsCount { get; set; }
        public Dictionary<string, int> PointTypeBreakdown { get; set; } = new();
    }
}