using AutomaticGeneration_ST.Services.Generation.Implementations;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using AutomaticGeneration_ST.Services.Implementations;
using AutomaticGeneration_ST.Services.Interfaces;
using AutomaticGeneration_ST.Models;
using WinFormsApp1.Templates;
using WinFormsApp1;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private readonly ServiceContainer _serviceContainer;
        private readonly DeviceTemplateDataBinder _deviceTemplateBinder;
        private readonly LogService _logger = LogService.Instance;

        public STGenerationService()
        {
            // 使用新的服务容器架构
            var templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            var configPath = Path.Combine(templateDirectory, "template-mapping.json");
            
            _serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);
            
            // 从服务容器获取服务实例
            _dataService = _serviceContainer.GetService<IDataService>();
            _configService = new JsonConfigurationService();
            _deviceGenerator = new ScribanDeviceGenerator();
            _ioGenerator = new ScribanIoMappingGenerator();
            _exportService = new FileSystemExportService();
            _communicationGenerator = new PlaceholderCommunicationGenerator();
            _deviceTemplateBinder = new DeviceTemplateDataBinder();
        }

        /// <summary>
        /// 使用指定配置的构造函数
        /// </summary>
        /// <param name="templateDirectory">模板目录</param>
        /// <param name="configPath">配置文件路径</param>
        public STGenerationService(string templateDirectory, string configPath)
        {
            _serviceContainer = ServiceContainer.CreateDefault(templateDirectory, configPath);
            
            // 从服务容器获取服务实例
            _dataService = _serviceContainer.GetService<IDataService>();
            _configService = new JsonConfigurationService();
            _deviceGenerator = new ScribanDeviceGenerator();
            _ioGenerator = new ScribanIoMappingGenerator();
            _exportService = new FileSystemExportService();
            _communicationGenerator = new PlaceholderCommunicationGenerator();
            _deviceTemplateBinder = new DeviceTemplateDataBinder();
        }

        /// <summary>
        /// 执行完整的ST代码生成流程（使用预加载的数据上下文）
        /// </summary>
        /// <param name="dataContext">预加载的数据上下文</param>
        /// <param name="templateDirectory">模板文件夹路径</param>
        /// <param name="configFilePath">配置文件路径</param>
        /// <param name="exportRootPath">导出根目录</param>
        /// <returns>成功生成的文件数量</returns>
        public int GenerateSTCode(AutomaticGeneration_ST.Services.Interfaces.DataContext dataContext, string templateDirectory, string configFilePath, string exportRootPath)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            LogInfo($"[{operationId}] 开始ST代码生成流程（使用预加载数据）");
            
            try
            {
                // =================== 1. 验证输入参数 ===================
                LogInfo($"[{operationId}] 验证输入参数...");
                if (dataContext == null)
                    throw new ArgumentNullException(nameof(dataContext), "数据上下文不能为空");
                    
                ValidateInputParametersForDataContext(templateDirectory, configFilePath, exportRootPath);

                // =================== 2. 使用预加载的数据执行核心业务流程 ===================
                LogInfo($"[{operationId}] 使用预加载的数据 - 设备数: {dataContext.Devices.Count}, 点位数: {dataContext.AllPointsMasterList.Count}");

                // 创建编排服务
                LogInfo($"[{operationId}] 创建生成编排服务...");
                var orchestrator = new GenerationOrchestratorService(templateDirectory, configFilePath, _configService, _deviceGenerator);

                // 生成设备代码
                LogInfo($"[{operationId}] 开始生成设备代码...");
                var deviceResults = GenerateWithErrorHandling(() => orchestrator.GenerateForDevices(dataContext.Devices), 
                                                             "设备代码生成", operationId);
                LogInfo($"[{operationId}] 设备代码生成完成 - 文件数: {deviceResults.Count}");

                // 生成IO映射代码 - 只使用独立点位，不包含设备关联点位
                LogInfo($"[{operationId}] 开始生成IO映射代码...");
                var ioMappingResults = GenerateWithErrorHandling(() => orchestrator.GenerateForIoMappings(dataContext.StandalonePoints, _ioGenerator), 
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

        /// <summary>
        /// 执行完整的ST代码生成流程（兼容性方法，会重新加载Excel文件）
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <param name="templateDirectory">模板文件夹路径</param>
        /// <param name="configFilePath">配置文件路径</param>
        /// <param name="exportRootPath">导出根目录</param>
        /// <returns>成功生成的文件数量</returns>
        [Obsolete("建议使用接受DataContext参数的重载方法，以避免重复解析Excel文件")]
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

                // 生成IO映射代码 - 只使用独立点位，不包含设备关联点位
                LogInfo($"[{operationId}] 开始生成IO映射代码...");
                var ioMappingResults = GenerateWithErrorHandling(() => orchestrator.GenerateForIoMappings(dataContext.StandalonePoints, _ioGenerator), 
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

        private void ValidateInputParametersForDataContext(string templateDirectory, string configFilePath, string exportRootPath)
        {
            if (string.IsNullOrWhiteSpace(templateDirectory))
                throw new ArgumentException("模板目录路径不能为空");
                
            if (string.IsNullOrWhiteSpace(configFilePath))
                throw new ArgumentException("配置文件路径不能为空");
                
            if (string.IsNullOrWhiteSpace(exportRootPath))
                throw new ArgumentException("导出目录路径不能为空");
            
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

        // 使用用户可见的日志服务
        private void LogInfo(string message)
        {
            _logger.LogInfo(message);
            System.Diagnostics.Debug.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }

        private void LogWarning(string message)
        {
            _logger.LogWarning(message);
            System.Diagnostics.Debug.WriteLine($"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }

        private void LogError(string message)
        {
            _logger.LogError(message);
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

        /// <summary>
        /// 从已有的数据上下文获取统计信息，避免重复加载数据
        /// </summary>
        public GenerationStatistics GetStatistics(AutomaticGeneration_ST.Services.Interfaces.DataContext dataContext)
        {
            try
            {
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

        /// <summary>
        /// 生成设备ST程序代码
        /// </summary>
        /// <param name="devices">设备列表</param>
        /// <returns>设备ST程序生成结果</returns>
        public Dictionary<string, List<string>> GenerateDeviceSTPrograms(List<Device> devices)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];
            LogInfo($"[{operationId}] 开始生成设备ST程序，设备数量: {devices.Count}");

            var result = new Dictionary<string, List<string>>();

            try
            {
                // 确保模板库已初始化
                TemplateLibraryManager.Initialize();

                // 按模板名称分组处理设备
                var devicesByTemplate = devices
                    .Where(d => !string.IsNullOrWhiteSpace(d.TemplateName))
                    .GroupBy(d => d.TemplateName)
                    .ToList();

                LogInfo($"[{operationId}] 发现 {devicesByTemplate.Count} 种不同的设备模板");

                foreach (var templateGroup in devicesByTemplate)
                {
                    var templateName = templateGroup.Key;
                    var templateDevices = templateGroup.ToList();

                    LogInfo($"[{operationId}] 开始处理模板: {templateName}, 设备数量: {templateDevices.Count}");

                    try
                    {
                        var templateCode = GenerateDeviceTemplateCode(templateName, templateDevices, operationId);
                        if (!string.IsNullOrWhiteSpace(templateCode))
                        {
                            if (!result.ContainsKey(templateName))
                            {
                                result[templateName] = new List<string>();
                            }
                            result[templateName].Add(templateCode);
                            LogInfo($"[{operationId}] 模板 {templateName} 代码生成完成");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[{operationId}] 模板 {templateName} 代码生成失败: {ex.Message}");
                        // 继续处理其他模板
                    }
                }

                // 处理没有模板名称的设备
                var devicesWithoutTemplate = devices.Where(d => string.IsNullOrWhiteSpace(d.TemplateName)).ToList();
                if (devicesWithoutTemplate.Any())
                {
                    LogInfo($"[{operationId}] 发现 {devicesWithoutTemplate.Count} 个没有指定模板的设备，将生成通用代码");
                    var genericCode = GenerateGenericDeviceCode(devicesWithoutTemplate, operationId);
                    if (!string.IsNullOrWhiteSpace(genericCode))
                    {
                        result["通用设备"] = new List<string> { genericCode };
                    }
                }

                LogInfo($"[{operationId}] 设备ST程序生成完成，共生成 {result.Count} 个模板的代码");
                return result;
            }
            catch (Exception ex)
            {
                LogError($"[{operationId}] 设备ST程序生成失败: {ex.Message}");
                throw new Exception($"设备ST程序生成失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 为指定模板生成设备代码
        /// </summary>
        private string GenerateDeviceTemplateCode(string templateName, List<Device> devices, string operationId)
        {
            try
            {
                // 查找模板文件
                var templateFilePath = FindDeviceTemplateFile(templateName);
                if (string.IsNullOrWhiteSpace(templateFilePath) || !File.Exists(templateFilePath))
                {
                    LogWarning($"[{operationId}] 未找到模板文件: {templateName}");
                    return GenerateGenericDeviceCode(devices, operationId);
                }

                LogInfo($"[{operationId}] 找到模板文件: {templateFilePath}");

                // 读取模板内容
                var templateContent = File.ReadAllText(templateFilePath);
                var template = Template.Parse(templateContent);

                if (template.HasErrors)
                {
                    var errors = string.Join(", ", template.Messages.Select(m => m.Message));
                    LogError($"[{operationId}] 模板解析错误: {errors}");
                    return GenerateGenericDeviceCode(devices, operationId);
                }

                var generatedCode = new List<string>();
                
                // 为每个设备生成代码
                foreach (var device in devices)
                {
                    try
                    {
                        LogInfo($"[{operationId}] 为设备 [{device.DeviceTag}] 生成代码...");

                        // 使用设备模板数据绑定器生成数据上下文
                        var dataContext = _deviceTemplateBinder.GenerateDeviceTemplateContext(device, templateContent);

                        // 渲染模板
                        var deviceCode = template.Render(dataContext);
                        
                        if (!string.IsNullOrWhiteSpace(deviceCode))
                        {
                            generatedCode.Add($"\n(* 设备: {device.DeviceTag} - 模板: {templateName} *)");
                            generatedCode.Add(deviceCode);
                            LogInfo($"[{operationId}] 设备 [{device.DeviceTag}] 代码生成完成");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"[{operationId}] 设备 [{device.DeviceTag}] 代码生成失败: {ex.Message}");
                        // 添加错误注释
                        generatedCode.Add($"\n(* 设备: {device.DeviceTag} - 代码生成失败: {ex.Message} *)");
                    }
                }

                return string.Join("\n", generatedCode);
            }
            catch (Exception ex)
            {
                LogError($"[{operationId}] 模板 {templateName} 处理失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 查找设备模板文件
        /// </summary>
        private string FindDeviceTemplateFile(string templateName)
        {
            try
            {
                var templatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                
                // 首先在阀门文件夹中查找
                var valveDir = Path.Combine(templatesDir, "阀门");
                if (Directory.Exists(valveDir))
                {
                    var valveTemplates = Directory.GetFiles(valveDir, "*.scriban");
                    var matchedValveTemplate = valveTemplates.FirstOrDefault(f => 
                        Path.GetFileNameWithoutExtension(f).Equals(templateName, StringComparison.OrdinalIgnoreCase));
                    
                    if (!string.IsNullOrEmpty(matchedValveTemplate))
                    {
                        return matchedValveTemplate;
                    }
                }

                // 在其他自定义文件夹中查找
                var subDirs = Directory.GetDirectories(templatesDir);
                foreach (var subDir in subDirs)
                {
                    var folderName = Path.GetFileName(subDir);
                    
                    // 跳过标准点位类型文件夹
                    if (Enum.TryParse<WinFormsApp1.Templates.PointType>(folderName, true, out var _))
                        continue;

                    var templateFiles = Directory.GetFiles(subDir, "*.scriban");
                    var matchedTemplate = templateFiles.FirstOrDefault(f =>
                        Path.GetFileNameWithoutExtension(f).Equals(templateName, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(matchedTemplate))
                    {
                        return matchedTemplate;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogError($"查找模板文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 生成通用设备代码
        /// </summary>
        private string GenerateGenericDeviceCode(List<Device> devices, string operationId)
        {
            try
            {
                LogInfo($"[{operationId}] 为 {devices.Count} 个设备生成通用代码");

                var genericCode = new List<string>();
                genericCode.Add("(* 通用设备代码 *)");

                foreach (var device in devices)
                {
                    genericCode.Add($"\n(* 设备: {device.DeviceTag} *)");
                    genericCode.Add("VAR");
                    
                    if (device.Points != null && device.Points.Any())
                    {
                        foreach (var point in device.Points.Values)
                        {
                            var pointTypeName = GetSTDataType(point);
                            genericCode.Add($"    {point.HmiTagName} : {pointTypeName};  (* {point.Description ?? "设备点位"} *)");
                        }
                    }
                    
                    genericCode.Add("END_VAR");
                    genericCode.Add("");
                }

                return string.Join("\n", genericCode);
            }
            catch (Exception ex)
            {
                LogError($"[{operationId}] 通用设备代码生成失败: {ex.Message}");
                return $"(* 通用设备代码生成失败: {ex.Message} *)";
            }
        }

        /// <summary>
        /// 根据点位信息推断ST数据类型
        /// </summary>
        private string GetSTDataType(Models.Point point)
        {
            if (point.DataType?.ToUpper().Contains("BOOL") == true)
                return "BOOL";
            if (point.DataType?.ToUpper().Contains("INT") == true)
                return "INT";
            if (point.DataType?.ToUpper().Contains("REAL") == true || 
                point.DataType?.ToUpper().Contains("FLOAT") == true)
                return "REAL";
            if (point.DataType?.ToUpper().Contains("WORD") == true)
                return "WORD";
            if (point.DataType?.ToUpper().Contains("DWORD") == true)
                return "DWORD";
            
            // 根据模块类型推断
            var moduleType = point.ModuleType?.ToUpper() ?? "";
            if (moduleType.Contains("AI") || moduleType.Contains("AO"))
                return "REAL";
            if (moduleType.Contains("DI") || moduleType.Contains("DO"))
                return "BOOL";
            
            // 默认类型
            return "BOOL";
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