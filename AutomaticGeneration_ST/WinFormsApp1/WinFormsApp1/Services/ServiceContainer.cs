using AutomaticGeneration_ST.Services.Implementations;
using AutomaticGeneration_ST.Services.Interfaces;
using AutomaticGeneration_ST.Services.Generation.Implementations;
using AutomaticGeneration_ST.Services.Generation.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// 简单的服务容器实现 - 管理依赖注入
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, Func<object>> _services;
        private readonly Dictionary<Type, object> _singletonInstances;
        private readonly string _templateDirectory;
        private readonly string _configFilePath;

        public ServiceContainer(string templateDirectory, string configFilePath)
        {
            _services = new Dictionary<Type, Func<object>>();
            _singletonInstances = new Dictionary<Type, object>();
            _templateDirectory = templateDirectory ?? throw new ArgumentNullException(nameof(templateDirectory));
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));

            RegisterServices();
        }

        /// <summary>
        /// 注册所有服务
        /// </summary>
        private void RegisterServices()
        {
            // 注册单例服务
            RegisterSingleton<IExcelWorkbookParser>(() => new ExcelWorkbookParser());
            // Removed legacy: RegisterSingleton<IPointFactory>(() => new PointFactory());
            RegisterSingleton<ITemplateRegistry>(() => 
            {
                var registry = new TemplateRegistry(_templateDirectory);
                if (File.Exists(_configFilePath))
                {
                    registry.LoadFromConfig(_configFilePath);
                }
                return registry;
            });

            // 注册需要依赖的服务
            // Removed legacy: device classification service and point factory registrations
            // RegisterTransient<IDeviceClassificationService>(() => 
            //     new DeviceClassificationService(GetService<IPointFactory>()));

            // 注册工作表定位服务
            RegisterTransient<IWorksheetLocatorService>(() => 
                new WorksheetLocatorService(GetService<IExcelWorkbookParser>()));

            // 注册TCP数据服务（供 ExcelDataService 的 TCP 处理使用）
            RegisterTransient<ITcpDataService>(() => 
                new TcpDataService(
                    GetService<IExcelWorkbookParser>(),
                    GetService<IWorksheetLocatorService>()));

            // Removed legacy: data processing orchestrator not used in core UI flow
            // RegisterTransient<IDataProcessingOrchestrator>(() =>
            //    new DataProcessingOrchestrator(
            //        GetService<IExcelWorkbookParser>(),
            //        GetService<IPointFactory>(),
            //        GetService<IDeviceClassificationService>(),
            //        GetService<ITemplateRegistry>(),
            //        GetService<IWorksheetLocatorService>(),
            //        GetService<ITcpDataService>()));

            // 兼容现有系统的IDataService
            // Use ExcelDataService directly for IDataService
            RegisterTransient<IDataService>(() => new ExcelDataService(GetService<IWorksheetLocatorService>()));

            // 注册分类导出相关服务
            RegisterSingleton<IScriptClassifier>(() => new ScriptClassificationService());
            RegisterTransient<ICategorizedExportService>(() => 
                new CategorizedFileExportService(GetService<IScriptClassifier>()));
                
            // 注册通讯相关服务
            RegisterTransient<IModbusTcpConfigGenerator>(() => new TcpCommunicationGenerator(_templateDirectory, _configFilePath));
            RegisterTransient<IModbusRtuConfigGenerator>(() => new PlaceholderCommunicationGenerator());
            
            // 注册TCP代码生成器（需要编译的模板字典）
            RegisterSingleton<WinFormsApp1.Generators.TcpCodeGenerator>(() => 
            {
                var compiledTemplates = new Dictionary<string, Scriban.Template>();
                
                // 直接从模板目录读取TCP模板文件
                var tcpTemplateDir = Path.Combine(_templateDirectory, "TCP通讯");
                var templateMappings = new Dictionary<string, string>
                {
                    ["TCP_ANALOG"] = Path.Combine(tcpTemplateDir, "ANALOG.scriban"),
                    ["TCP_DIGITAL"] = Path.Combine(tcpTemplateDir, "DIGITAL.scriban"),
                    //["ANALOG"] = Path.Combine(tcpTemplateDir, "ANALOG.scriban"),
                    //["DIGITAL"] = Path.Combine(tcpTemplateDir, "DIGITAL.scriban")
                };
                
                foreach (var mapping in templateMappings)
                {
                    if (File.Exists(mapping.Value))
                    {
                        try
                        {
                            var templateContent = File.ReadAllText(mapping.Value);
                            compiledTemplates[mapping.Key] = Scriban.Template.Parse(templateContent);
                        }
                        catch (Exception ex)
                        {
                            // 编译失败时立即抛出异常，避免后续降级导致渲染不完整
                            throw new InvalidOperationException($"TCP模板 {mapping.Key} 编译失败: {ex.Message}", ex);
                        }
                    }
                }
                
                return new WinFormsApp1.Generators.TcpCodeGenerator(compiledTemplates);
            });
        }

        /// <summary>
        /// 注册单例服务
        /// </summary>
        public void RegisterSingleton<T>(Func<T> factory)
        {
            _services[typeof(T)] = () => 
            {
                if (!_singletonInstances.TryGetValue(typeof(T), out var instance))
                {
                    instance = factory();
                    _singletonInstances[typeof(T)] = instance;
                }
                return instance;
            };
        }

        /// <summary>
        /// 注册瞬态服务
        /// </summary>
        public void RegisterTransient<T>(Func<T> factory)
        {
            _services[typeof(T)] = () => factory();
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        public T GetService<T>()
        {
            if (_services.TryGetValue(typeof(T), out var factory))
            {
                return (T)factory();
            }
            throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 获取所有已注册的服务类型
        /// </summary>
        public IEnumerable<Type> GetRegisteredServiceTypes()
        {
            return _services.Keys;
        }

        /// <summary>
        /// 创建默认配置的服务容器
        /// </summary>
        public static ServiceContainer CreateDefault(string templateDirectory, string configFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(templateDirectory))
                throw new ArgumentException("模板目录不能为空", nameof(templateDirectory));

            // 如果没有指定配置文件，使用默认路径
            if (string.IsNullOrWhiteSpace(configFilePath))
            {
                configFilePath = Path.Combine(templateDirectory, "template-mapping.json");
            }

            return new ServiceContainer(templateDirectory, configFilePath);
        }
    }

    /// <summary>
    /// 重构后的ExcelDataService，使用新的架构但保持接口兼容性
    /// </summary>
    public class RefactoredExcelDataService : IDataService
    {
        private readonly IDataProcessingOrchestrator _orchestrator;

        public RefactoredExcelDataService(IDataProcessingOrchestrator orchestrator)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        public DataContext LoadData(string excelFilePath)
        {
            try
            {
                var processingResult = _orchestrator.ProcessData(excelFilePath);

                // 转换为原有的DataContext格式以保持兼容性
                return new DataContext
                {
                    Devices = processingResult.Devices,
                    StandalonePoints = processingResult.StandalonePoints,
                    AllPointsMasterList = processingResult.AllPointsMaster
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"数据加载失败: {ex.Message}", ex);
            }
        }
    }
}
