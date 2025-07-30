using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormsApp1.Devices.Base;
using WinFormsApp1.Devices.Interfaces;
// using WinFormsApp1.Devices.Controllers; // 暂时禁用
using WinFormsApp1.Forms;
using AutomaticGeneration_ST.Models;
using Newtonsoft.Json;
using Point = AutomaticGeneration_ST.Models.Point;
using System.IO;

namespace WinFormsApp1.Devices
{
    /// <summary>
    /// 组合设备管理器 - 统一管理所有组合设备的创建、配置和监控
    /// </summary>
    public class CompositeDeviceManager
    {
        #region 单例模式

        private static readonly Lazy<CompositeDeviceManager> _instance = 
            new Lazy<CompositeDeviceManager>(() => new CompositeDeviceManager());

        public static CompositeDeviceManager Instance => _instance.Value;

        private CompositeDeviceManager()
        {
            InitializeManager();
        }

        #endregion

        #region 私有字段

        private readonly ConcurrentDictionary<string, ICompositeDevice> _devices = new();
        private readonly ConcurrentDictionary<CompositeDeviceType, Func<string, string, ICompositeDevice>> _deviceFactories = new();
        private readonly Dictionary<string, Dictionary<string, object>> _deviceConfigurations = new();
        private readonly Dictionary<string, List<Point>> _devicePointMappings = new();
        
        private bool _isInitialized = false;
        private string _configurationFilePath = "CompositeDevices.json";
        private System.Threading.Timer? _monitoringTimer;
        private readonly object _lockObject = new object();

        #endregion

        #region 事件定义

        /// <summary>
        /// 设备状态变更事件参数
        /// </summary>
        public class DeviceStatusChangedEventArgs : EventArgs
        {
            public string DeviceId { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public CompositeDeviceType DeviceType { get; set; }
            public string OldStatus { get; set; } = "";
            public string NewStatus { get; set; } = "";
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// 设备报警事件参数
        /// </summary>
        public class DeviceAlarmEventArgs : EventArgs
        {
            public string DeviceId { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public string AlarmType { get; set; } = "";
            public string AlarmMessage { get; set; } = "";
            public bool IsActive { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.Now;
        }

        #endregion

        #region 事件

        /// <summary>
        /// 设备添加事件
        /// </summary>
        public event EventHandler<ICompositeDevice>? DeviceAdded;

        /// <summary>
        /// 设备移除事件
        /// </summary>
        public event EventHandler<string>? DeviceRemoved;

        /// <summary>
        /// 设备状态变更事件
        /// </summary>
        public event EventHandler<DeviceStatusChangedEventArgs>? DeviceStatusChanged;

        /// <summary>
        /// 设备报警事件
        /// </summary>
        public event EventHandler<DeviceAlarmEventArgs>? DeviceAlarmOccurred;

        /// <summary>
        /// 管理器状态变更事件
        /// </summary>
        public event EventHandler<string>? ManagerStatusChanged;

        #endregion

        #region 公共属性

        /// <summary>
        /// 所有设备的只读集合
        /// </summary>
        public IReadOnlyDictionary<string, ICompositeDevice> Devices => _devices;

        /// <summary>
        /// 设备总数
        /// </summary>
        public int DeviceCount => _devices.Count;

        /// <summary>
        /// 运行中的设备数量
        /// </summary>
        public int RunningDeviceCount => _devices.Values.Count(d => d.CurrentState == DeviceState.Running);

        /// <summary>
        /// 故障设备数量
        /// </summary>
        public int FaultDeviceCount => _devices.Values.Count(d => d.CurrentState == DeviceState.Fault);

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 配置文件路径
        /// </summary>
        public string ConfigurationFilePath
        {
            get => _configurationFilePath;
            set
            {
                if (_configurationFilePath != value)
                {
                    _configurationFilePath = value;
                }
            }
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化管理器
        /// </summary>
        private void InitializeManager()
        {
            try
            {
                RegisterDeviceFactories();
                LoadConfiguration();
                StartMonitoring();
                
                _isInitialized = true;
                ManagerStatusChanged?.Invoke(this, "管理器初始化完成");
            }
            catch (Exception ex)
            {
                ManagerStatusChanged?.Invoke(this, $"管理器初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册设备工厂
        /// </summary>
        private void RegisterDeviceFactories()
        {
            _deviceFactories[CompositeDeviceType.ValveController] = 
                (id, name) => new BasicCompositeDevice(id, name, CompositeDeviceType.ValveController);
            
            _deviceFactories[CompositeDeviceType.PumpController] = 
                (id, name) => new BasicCompositeDevice(id, name, CompositeDeviceType.PumpController);
            
            _deviceFactories[CompositeDeviceType.VFDController] = 
                (id, name) => new BasicCompositeDevice(id, name, CompositeDeviceType.VFDController);
            
            _deviceFactories[CompositeDeviceType.TankController] = 
                (id, name) => new BasicCompositeDevice(id, name, CompositeDeviceType.TankController);
            
            _deviceFactories[CompositeDeviceType.HeatExchangerController] = 
                (id, name) => new BasicCompositeDevice(id, name, CompositeDeviceType.HeatExchangerController);
            
            _deviceFactories[CompositeDeviceType.ReactorController] = 
                (id, name) => new BasicCompositeDevice(id, name, CompositeDeviceType.ReactorController);
        }

        /// <summary>
        /// 启动监控
        /// </summary>
        private void StartMonitoring()
        {
            _monitoringTimer = new System.Threading.Timer(
                MonitoringCallback,
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10)); // 每10秒监控一次
        }

        /// <summary>
        /// 监控回调
        /// </summary>
        private void MonitoringCallback(object? state)
        {
            try
            {
                foreach (var device in _devices.Values)
                {
                    // 检查设备健康状态
                    CheckDeviceHealth(device);
                }
            }
            catch
            {
                // 忽略监控错误
            }
        }

        #endregion

        #region 设备管理方法

        /// <summary>
        /// 创建设备
        /// </summary>
        public async Task<DeviceOperationResult> CreateDeviceAsync(
            CompositeDeviceType deviceType, 
            string deviceId, 
            string deviceName = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = "设备ID不能为空"
                    };
                }

                if (_devices.ContainsKey(deviceId))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"设备ID '{deviceId}' 已存在"
                    };
                }

                if (!_deviceFactories.ContainsKey(deviceType))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"不支持的设备类型: {deviceType}"
                    };
                }

                // 创建设备实例
                var device = _deviceFactories[deviceType](deviceId, 
                    string.IsNullOrWhiteSpace(deviceName) ? deviceId : deviceName);

                // 订阅设备事件
                SubscribeToDeviceEvents(device);

                // 初始化设备
                var initResult = await device.InitializeAsync();
                if (!initResult.Success)
                {
                    return initResult;
                }

                // 添加到管理器
                _devices[deviceId] = device;

                // 保存配置
                await SaveConfigurationAsync();

                DeviceAdded?.Invoke(this, device);

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"设备 '{deviceId}' 创建成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"创建设备失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 删除设备
        /// </summary>
        public async Task<DeviceOperationResult> RemoveDeviceAsync(string deviceId)
        {
            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"设备 '{deviceId}' 不存在"
                    };
                }

                // 停止设备
                if (device.CurrentState == DeviceState.Running)
                {
                    await device.StopAsync();
                }

                // 取消事件订阅
                UnsubscribeFromDeviceEvents(device);

                // 从管理器中移除
                _devices.TryRemove(deviceId, out _);

                // 清理配置
                _deviceConfigurations.Remove(deviceId);
                _devicePointMappings.Remove(deviceId);

                // 保存配置
                await SaveConfigurationAsync();

                DeviceRemoved?.Invoke(this, deviceId);

                return new DeviceOperationResult
                {
                    Success = true,
                    Message = $"设备 '{deviceId}' 删除成功"
                };
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"删除设备失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 获取设备
        /// </summary>
        public ICompositeDevice? GetDevice(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var device) ? device : null;
        }

        /// <summary>
        /// 获取指定类型的设备
        /// </summary>
        public IEnumerable<ICompositeDevice> GetDevicesByType(CompositeDeviceType deviceType)
        {
            return _devices.Values.Where(d => d.DeviceType == deviceType);
        }

        /// <summary>
        /// 获取指定状态的设备
        /// </summary>
        public IEnumerable<ICompositeDevice> GetDevicesByState(DeviceState state)
        {
            return _devices.Values.Where(d => d.CurrentState == state);
        }

        #endregion

        #region 批量操作方法

        /// <summary>
        /// 启动所有设备
        /// </summary>
        public async Task<Dictionary<string, DeviceOperationResult>> StartAllDevicesAsync()
        {
            var results = new Dictionary<string, DeviceOperationResult>();

            foreach (var device in _devices.Values)
            {
                if (device.CurrentState == DeviceState.Stopped)
                {
                    results[device.DeviceId] = await device.StartAsync();
                }
            }

            return results;
        }

        /// <summary>
        /// 停止所有设备
        /// </summary>
        public async Task<Dictionary<string, DeviceOperationResult>> StopAllDevicesAsync()
        {
            var results = new Dictionary<string, DeviceOperationResult>();

            foreach (var device in _devices.Values)
            {
                if (device.CurrentState == DeviceState.Running)
                {
                    results[device.DeviceId] = await device.StopAsync();
                }
            }

            return results;
        }

        /// <summary>
        /// 重置所有设备
        /// </summary>
        public async Task<Dictionary<string, DeviceOperationResult>> ResetAllDevicesAsync()
        {
            var results = new Dictionary<string, DeviceOperationResult>();

            foreach (var device in _devices.Values)
            {
                results[device.DeviceId] = await device.ResetAsync();
            }

            return results;
        }

        /// <summary>
        /// 诊断所有设备
        /// </summary>
        public async Task<Dictionary<string, DeviceOperationResult>> DiagnoseAllDevicesAsync()
        {
            var results = new Dictionary<string, DeviceOperationResult>();

            foreach (var device in _devices.Values)
            {
                results[device.DeviceId] = await device.DiagnoseAsync();
            }

            return results;
        }

        #endregion

        #region 点位管理方法

        /// <summary>
        /// 为设备添加点位
        /// </summary>
        public DeviceOperationResult AddPointToDevice(string deviceId, Point point)
        {
            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,  
                        Message = $"设备 '{deviceId}' 不存在"
                    };
                }

                var result = device.AddPoint(point);
                if (result.Success)
                {
                    // 更新点位映射
                    if (!_devicePointMappings.ContainsKey(deviceId))
                    {
                        _devicePointMappings[deviceId] = new List<Point>();
                    }
                    _devicePointMappings[deviceId].Add(point);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"添加点位失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 从设备移除点位
        /// </summary>
        public DeviceOperationResult RemovePointFromDevice(string deviceId, string pointId)
        {
            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                {
                    return new DeviceOperationResult
                    {
                        Success = false,
                        Message = $"设备 '{deviceId}' 不存在"
                    };
                }

                var result = device.RemovePoint(pointId);
                if (result.Success && _devicePointMappings.ContainsKey(deviceId))
                {
                    // 更新点位映射
                    _devicePointMappings[deviceId].RemoveAll(p => p.Id == pointId);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new DeviceOperationResult
                {
                    Success = false,
                    Message = $"移除点位失败: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// 获取设备的所有点位
        /// </summary>
        public IReadOnlyList<Point> GetDevicePoints(string deviceId)
        {
            if (_devices.TryGetValue(deviceId, out var device))
            {
                return device.AssociatedPoints;
            }
            return new List<Point>().AsReadOnly();
        }

        #endregion

        #region 代码生成方法

        /// <summary>
        /// 生成所有设备的ST代码
        /// </summary>
        public async Task<Dictionary<string, string>> GenerateAllDevicesSTCodeAsync()
        {
            var results = new Dictionary<string, string>();

            foreach (var device in _devices.Values)
            {
                try
                {
                    results[device.DeviceId] = await device.GenerateSTCodeAsync();
                }
                catch (Exception ex)
                {
                    results[device.DeviceId] = $"// 代码生成失败: {ex.Message}";
                }
            }

            return results;
        }

        /// <summary>
        /// 生成指定设备的ST代码
        /// </summary>
        public async Task<string?> GenerateDeviceSTCodeAsync(string deviceId)
        {
            if (_devices.TryGetValue(deviceId, out var device))
            {
                return await device.GenerateSTCodeAsync();
            }
            return null;
        }

        /// <summary>
        /// 生成整合的ST代码文件
        /// </summary>
        public async Task<string> GenerateIntegratedSTCodeAsync()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("(* ST自动生成器 - 组合设备控制程序 *)");
            sb.AppendLine($"(* 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss} *)");
            sb.AppendLine($"(* 设备总数: {_devices.Count} *)");
            sb.AppendLine();

            // 生成全局变量声明
            sb.AppendLine("VAR_GLOBAL");
            foreach (var device in _devices.Values)
            {
                sb.AppendLine($"    // {device.DeviceName} ({device.DeviceType})");
                foreach (var point in device.AssociatedPoints)
                {
                    sb.AppendLine($"    {point.HMITagName} : {GetSTDataType(point.Type)}; // {point.Name}");
                }
                sb.AppendLine();
            }
            sb.AppendLine("END_VAR");
            sb.AppendLine();

            // 生成每个设备的控制程序
            foreach (var device in _devices.Values)
            {
                sb.AppendLine($"// ========== {device.DeviceName} ({device.DeviceType}) ==========");
                try
                {
                    var deviceCode = await device.GenerateSTCodeAsync();
                    sb.AppendLine(deviceCode);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"// 设备代码生成失败: {ex.Message}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion

        #region 配置管理方法

        /// <summary>
        /// 保存配置
        /// </summary>
        public async Task SaveConfigurationAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    var config = new
                    {
                        Devices = _devices.Values.Select(d => new
                        {
                            d.DeviceId,
                            d.DeviceName,
                            d.DeviceType,
                            d.Position,
                            Configuration = d.ExportConfiguration(),
                            Points = d.AssociatedPoints.Select(p => new
                            {
                                p.Id,
                                p.Name,
                                p.Type,
                                p.HMITagName,
                                p.Description,
                                p.Unit
                            }).ToList()
                        }).ToList(),
                        LastUpdated = DateTime.Now
                    };

                    var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(_configurationFilePath, json, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                ManagerStatusChanged?.Invoke(this, $"保存配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configurationFilePath))
                {
                    return;
                }

                var json = File.ReadAllText(_configurationFilePath, Encoding.UTF8);
                var config = JsonConvert.DeserializeObject<dynamic>(json);

                if (config?.Devices != null)
                {
                    foreach (var deviceConfig in config.Devices)
                    {
                        var deviceType = (CompositeDeviceType)Enum.Parse(typeof(CompositeDeviceType), 
                            deviceConfig.DeviceType.ToString());
                        var deviceId = deviceConfig.DeviceId.ToString();
                        var deviceName = deviceConfig.DeviceName.ToString();

                        if (_deviceFactories.ContainsKey(deviceType))
                        {
                            var device = _deviceFactories[deviceType](deviceId, deviceName);
                            
                            // 导入配置
                            if (deviceConfig.Configuration != null)
                            {
                                device.ImportConfiguration(deviceConfig.Configuration.ToString());
                            }

                            // 添加点位
                            if (deviceConfig.Points != null)
                            {
                                foreach (var pointConfig in deviceConfig.Points)
                                {
                                    var point = new Point(pointConfig.HMITagName ?? "UnknownTag")
                                    {
                                        // 其他属性需要通过适当的设置方式设置
                                        Description = pointConfig.Description ?? "",
                                        Unit = pointConfig.Unit ?? ""
                                    };
                                    device.AddPoint(point);
                                }
                            }

                            // 订阅事件
                            SubscribeToDeviceEvents(device);

                            _devices[deviceId] = device;
                        }
                    }
                }

                ManagerStatusChanged?.Invoke(this, $"加载了 {_devices.Count} 个设备配置");
            }
            catch (Exception ex)
            {
                ManagerStatusChanged?.Invoke(this, $"加载配置失败: {ex.Message}");
            }
        }

        #endregion

        #region 监控和统计方法

        /// <summary>
        /// 获取设备统计信息
        /// </summary>
        public Dictionary<string, object> GetDeviceStatistics()
        {
            var stats = new Dictionary<string, object>();

            stats["TotalDevices"] = _devices.Count;
            stats["RunningDevices"] = RunningDeviceCount;
            stats["FaultDevices"] = FaultDeviceCount;
            stats["StoppedDevices"] = _devices.Values.Count(d => d.CurrentState == DeviceState.Stopped);

            // 按类型统计
            var typeStats = new Dictionary<string, int>();
            foreach (var deviceType in Enum.GetValues<CompositeDeviceType>())
            {
                typeStats[deviceType.ToString()] = _devices.Values.Count(d => d.DeviceType == deviceType);
            }
            stats["DevicesByType"] = typeStats;

            // 按状态统计
            var stateStats = new Dictionary<string, int>();
            foreach (var state in Enum.GetValues<DeviceState>())
            {
                stateStats[state.ToString()] = _devices.Values.Count(d => d.CurrentState == state);
            }
            stats["DevicesByState"] = stateStats;

            stats["TotalPoints"] = _devices.Values.Sum(d => d.AssociatedPoints.Count);
            stats["LastUpdate"] = DateTime.Now;

            return stats;
        }

        /// <summary>
        /// 获取设备健康报告
        /// </summary>
        public async Task<Dictionary<string, object>> GetDevicesHealthReportAsync()
        {
            var healthReport = new Dictionary<string, object>();

            foreach (var device in _devices.Values)
            {
                try
                {
                    var diagnosis = await device.DiagnoseAsync();
                    healthReport[device.DeviceId] = new
                    {
                        DeviceName = device.DeviceName,
                        DeviceType = device.DeviceType.ToString(),
                        CurrentState = device.CurrentState.ToString(),
                        HealthScore = diagnosis.Data?.GetValueOrDefault("HealthScore", 0),
                        AlarmCount = diagnosis.Data?.GetValueOrDefault("AlarmCount", 0),
                        LastUpdate = device.LastUpdateTime,
                        IsOnline = device.IsOnline
                    };
                }
                catch (Exception ex)
                {
                    healthReport[device.DeviceId] = new
                    {
                        DeviceName = device.DeviceName,
                        DeviceType = device.DeviceType.ToString(),
                        Error = ex.Message
                    };
                }
            }

            return healthReport;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 订阅设备事件
        /// </summary>
        private void SubscribeToDeviceEvents(ICompositeDevice device)
        {
            device.StateChanged += OnDeviceStateChanged;
            device.ErrorOccurred += OnDeviceErrorOccurred;
            device.WarningOccurred += OnDeviceWarningOccurred;
        }

        /// <summary>
        /// 取消设备事件订阅
        /// </summary>
        private void UnsubscribeFromDeviceEvents(ICompositeDevice device)
        {
            device.StateChanged -= OnDeviceStateChanged;
            device.ErrorOccurred -= OnDeviceErrorOccurred;
            device.WarningOccurred -= OnDeviceWarningOccurred;
        }

        /// <summary>
        /// 设备状态变更事件处理
        /// </summary>
        private void OnDeviceStateChanged(object? sender, DeviceEventArgs e)
        {
            if (sender is ICompositeDevice device)
            {
                DeviceStatusChanged?.Invoke(this, new DeviceStatusChangedEventArgs
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    DeviceType = device.DeviceType,
                    OldStatus = e.Data.GetValueOrDefault("OldState", "").ToString() ?? "",
                    NewStatus = e.Data.GetValueOrDefault("NewState", "").ToString() ?? "",
                    Timestamp = e.Timestamp
                });
            }
        }

        /// <summary>
        /// 设备错误事件处理
        /// </summary>
        private void OnDeviceErrorOccurred(object? sender, DeviceEventArgs e)
        {
            if (sender is ICompositeDevice device)
            {
                DeviceAlarmOccurred?.Invoke(this, new DeviceAlarmEventArgs
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    AlarmType = "Error",
                    AlarmMessage = e.Message,
                    IsActive = true,
                    Timestamp = e.Timestamp
                });
            }
        }

        /// <summary>
        /// 设备警告事件处理
        /// </summary>
        private void OnDeviceWarningOccurred(object? sender, DeviceEventArgs e)
        {
            if (sender is ICompositeDevice device)
            {
                DeviceAlarmOccurred?.Invoke(this, new DeviceAlarmEventArgs
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    AlarmType = "Warning",
                    AlarmMessage = e.Message,
                    IsActive = true,
                    Timestamp = e.Timestamp
                });
            }
        }

        /// <summary>
        /// 检查设备健康状态
        /// </summary>
        private void CheckDeviceHealth(ICompositeDevice device)
        {
            // 检查设备是否响应
            if (DateTime.Now - device.LastUpdateTime > TimeSpan.FromMinutes(5))
            {
                // 设备可能失去响应
                DeviceAlarmOccurred?.Invoke(this, new DeviceAlarmEventArgs
                {
                    DeviceId = device.DeviceId,
                    DeviceName = device.DeviceName,
                    AlarmType = "Communication",
                    AlarmMessage = "设备通讯超时",
                    IsActive = true,
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 获取ST数据类型
        /// </summary>
        private string GetSTDataType(string pointType)
        {
            return pointType.ToUpper() switch
            {
                "AI" => "REAL",
                "AO" => "REAL", 
                "DI" => "BOOL",
                "DO" => "BOOL",
                _ => "REAL"
            };
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                // 停止监控
                _monitoringTimer?.Dispose();

                // 停止所有设备
                foreach (var device in _devices.Values)
                {
                    try
                    {
                        if (device.CurrentState == DeviceState.Running)
                        {
                            device.StopAsync().Wait(5000); // 最多等待5秒
                        }
                        UnsubscribeFromDeviceEvents(device);
                    }
                    catch
                    {
                        // 忽略停止设备时的错误
                    }
                }

                // 保存配置
                SaveConfigurationAsync().Wait(3000);

                _devices.Clear();
                _deviceConfigurations.Clear();
                _devicePointMappings.Clear();

                ManagerStatusChanged?.Invoke(this, "管理器已关闭");
            }
            catch (Exception ex)
            {
                ManagerStatusChanged?.Invoke(this, $"管理器关闭时出错: {ex.Message}");
            }
        }

        #endregion
    }
}