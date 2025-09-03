////--NEED DELETE: 设备管理（本地设备模型/拓扑），与Excel->生成->导出主链路重复且存在编译错误引用
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Linq;
//using System.Text.Json;
//using System.Threading.Tasks;
//using AutomaticGeneration_ST.Models;
//using WinFormsApp1.Devices.Interfaces;
//using WinFormsApp1.Templates;
//using Point = AutomaticGeneration_ST.Models.Point;

//namespace WinFormsApp1.Devices
//{
//    /// <summary>
//    /// 组合设备基类 - 定义组合设备的通用属性和方法
//    /// </summary>
//    public abstract class CompositeDevice : ICompositeDevice, IConfigurableDevice, IMonitorableDevice
//    {
//        #region 私有字段

//        private readonly List<Point> _associatedPoints = new();
//        private readonly Dictionary<string, object> _parameters = new();
//        private DeviceState _currentState = DeviceState.Stopped;
//        private ControlMode _controlMode = ControlMode.Manual;
//        private bool _isEnabled = true;
//        private bool _isOnline = false;
//        private DateTime _lastUpdateTime = DateTime.Now;
//        private bool _isConfigurationMode = false;
//        private bool _isMonitoringEnabled = false;
//        private int _monitoringInterval = 1000;

//        #endregion

//        #region 构造函数

//        protected CompositeDevice(string deviceId, string deviceName, CompositeDeviceType deviceType)
//        {
//            DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
//            DeviceName = deviceName ?? throw new ArgumentNullException(nameof(deviceName));
//            DeviceType = deviceType;
            
//            InitializeDefaultParameters();
//            RegisterEventHandlers();
//        }

//        #endregion

//        #region ICompositeDevice 基本属性

//        public string DeviceId { get; }
        
//        // 兼容性属性 - 映射到DeviceId
//        public string Id => DeviceId;

//        public string DeviceName { get; set; }

//        public string Description { get; set; } = "";

//        public CompositeDeviceType DeviceType { get; }
        
//        // 兼容性属性 - 映射到DeviceType
//        public CompositeDeviceType Type => DeviceType;

//        public string Position { get; set; } = "";

//        // 额外的设备属性
//        public string Name => DeviceName; // 兼容性属性
//        public string Manufacturer { get; set; } = "";
//        public string Model { get; set; } = "";
        
//        // 新增的缺失属性
//        public DateTime CreatedTime { get; set; } = DateTime.Now;
//        public DateTime ModifiedTime { get; set; } = DateTime.Now;
//        public string Author { get; set; } = Environment.UserName;
//        public string Version { get; set; } = "1.0.0";
//        public bool IsActive { get; set; } = true;

//        public DeviceState CurrentState
//        {
//            get => _currentState;
//            protected set
//            {
//                if (_currentState != value)
//                {
//                    var oldState = _currentState;
//                    _currentState = value;
//                    _lastUpdateTime = DateTime.Now;
                    
//                    OnStateChanged(new DeviceEventArgs
//                    {
//                        DeviceId = DeviceId,
//                        EventType = "StateChanged",
//                        Message = $"设备状态从 {oldState} 变更为 {value}",
//                        Data = new Dictionary<string, object>
//                        {
//                            ["OldState"] = oldState,
//                            ["NewState"] = value
//                        }
//                    });
//                }
//            }
//        }

//        public ControlMode ControlMode
//        {
//            get => _controlMode;
//            set
//            {
//                if (_controlMode != value)
//                {
//                    var oldMode = _controlMode;
//                    _controlMode = value;
//                    _lastUpdateTime = DateTime.Now;
                    
//                    OnParameterChanged(new DeviceEventArgs
//                    {
//                        DeviceId = DeviceId,
//                        EventType = "ControlModeChanged",
//                        Message = $"控制模式从 {oldMode} 变更为 {value}",
//                        Data = new Dictionary<string, object>
//                        {
//                            ["OldMode"] = oldMode,
//                            ["NewMode"] = value
//                        }
//                    });
//                }
//            }
//        }

//        public bool IsEnabled
//        {
//            get => _isEnabled;
//            set
//            {
//                if (_isEnabled != value)
//                {
//                    _isEnabled = value;
//                    _lastUpdateTime = DateTime.Now;
                    
//                    OnParameterChanged(new DeviceEventArgs
//                    {
//                        DeviceId = DeviceId,
//                        EventType = "EnabledChanged",
//                        Message = $"设备启用状态变更为 {value}",
//                        Data = new Dictionary<string, object>
//                        {
//                            ["IsEnabled"] = value
//                        }
//                    });
//                }
//            }
//        }

//        public bool IsOnline
//        {
//            get => _isOnline;
//            protected set
//            {
//                if (_isOnline != value)
//                {
//                    _isOnline = value;
//                    _lastUpdateTime = DateTime.Now;
                    
//                    OnParameterChanged(new DeviceEventArgs
//                    {
//                        DeviceId = DeviceId,
//                        EventType = "OnlineStatusChanged",
//                        Message = $"设备在线状态变更为 {value}",
//                        Data = new Dictionary<string, object>
//                        {
//                            ["IsOnline"] = value
//                        }
//                    });
//                }
//            }
//        }

//        public DateTime LastUpdateTime => _lastUpdateTime;

//        #endregion

//        #region ICompositeDevice 点位管理

//        public IReadOnlyList<Point> AssociatedPoints => new ReadOnlyCollection<Point>(_associatedPoints);
        
//        // 兼容性属性 - 映射到AssociatedPoints
//        public IReadOnlyList<Point> Points => AssociatedPoints;

//        public IReadOnlyList<Point> InputPoints => new ReadOnlyCollection<Point>(
//            _associatedPoints.Where(p => p.PointType == "AI" || p.PointType == "DI").ToList());

//        public IReadOnlyList<Point> OutputPoints => new ReadOnlyCollection<Point>(
//            _associatedPoints.Where(p => p.PointType == "AO" || p.PointType == "DO").ToList());

//        public virtual DeviceOperationResult AddPoint(Point point)
//        {
//            try
//            {
//                if (point == null)
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = "点位不能为空",
//                        ErrorCode = "NULL_POINT"
//                    };

//                if (_associatedPoints.Any(p => p.HmiTagName == point.HmiTagName))
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = $"点位 {point.HmiTagName} 已存在",
//                        ErrorCode = "DUPLICATE_POINT"
//                    };

//                // 验证点位是否适合当前设备类型
//                var validationResult = ValidatePointForDevice(point);
//                if (!validationResult.Success)
//                    return validationResult;

//                _associatedPoints.Add(point);
//                _lastUpdateTime = DateTime.Now;

//                OnParameterChanged(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "PointAdded",
//                    Message = $"点位 {point.HmiTagName} 已添加到设备",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Point"] = point
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = $"点位 {point.HmiTagName} 添加成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"添加点位时出错: {ex.Message}",
//                    ErrorCode = "ADD_POINT_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual DeviceOperationResult RemovePoint(string pointId)
//        {
//            try
//            {
//                var point = _associatedPoints.FirstOrDefault(p => 
//                    p.HmiTagName == pointId || p.DevicePointName == pointId);
                
//                if (point == null)
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = $"未找到点位 {pointId}",
//                        ErrorCode = "POINT_NOT_FOUND"
//                    };

//                _associatedPoints.Remove(point);
//                _lastUpdateTime = DateTime.Now;

//                OnParameterChanged(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "PointRemoved",
//                    Message = $"点位 {pointId} 已从设备移除",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["PointId"] = pointId
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = $"点位 {pointId} 移除成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"移除点位时出错: {ex.Message}",
//                    ErrorCode = "REMOVE_POINT_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual DeviceOperationResult ClearPoints()
//        {
//            try
//            {
//                _associatedPoints.Clear();
//                OnPointsChanged(new List<Point>(), "CLEAR_ALL");
                
//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = "所有点位已清除"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"清除点位时出错: {ex.Message}",
//                    ErrorCode = "CLEAR_POINTS_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public IReadOnlyList<Point> GetPointsByType(string pointType)
//        {
//            return new ReadOnlyCollection<Point>(
//                _associatedPoints.Where(p => p.PointType.Equals(pointType, StringComparison.OrdinalIgnoreCase)).ToList());
//        }

//        public Point? GetPointByHmiTag(string hmiTagName)
//        {
//            return _associatedPoints.FirstOrDefault(p => 
//                p.HmiTagName.Equals(hmiTagName, StringComparison.OrdinalIgnoreCase));
//        }

//        #endregion

//        #region ICompositeDevice 设备操作

//        public virtual async Task<DeviceOperationResult> InitializeAsync()
//        {
//            try
//            {
//                CurrentState = DeviceState.Starting;
                
//                // 执行设备特定的初始化逻辑
//                var initResult = await OnInitializeAsync();
//                if (!initResult.Success)
//                {
//                    CurrentState = DeviceState.Fault;
//                    return initResult;
//                }

//                // 验证关联点位
//                var pointValidationErrors = ValidateAssociatedPoints();
//                if (pointValidationErrors.Any())
//                {
//                    CurrentState = DeviceState.Fault;
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = $"点位验证失败: {string.Join(", ", pointValidationErrors)}",
//                        ErrorCode = "POINT_VALIDATION_FAILED"
//                    };
//                }

//                CurrentState = DeviceState.Stopped;
//                IsOnline = true;

//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = "设备初始化成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                CurrentState = DeviceState.Fault;
//                IsOnline = false;
                
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "InitializeError",
//                    Message = $"设备初始化失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"设备初始化失败: {ex.Message}",
//                    ErrorCode = "INITIALIZE_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual async Task<DeviceOperationResult> StartAsync()
//        {
//            try
//            {
//                if (!IsEnabled)
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = "设备未启用，无法启动",
//                        ErrorCode = "DEVICE_DISABLED"
//                    };

//                if (!IsOnline)
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = "设备离线，无法启动",
//                        ErrorCode = "DEVICE_OFFLINE"
//                    };

//                if (CurrentState == DeviceState.Running)
//                    return new DeviceOperationResult
//                    {
//                        Success = true,
//                        Message = "设备已在运行中"
//                    };

//                CurrentState = DeviceState.Starting;

//                // 执行设备特定的启动逻辑
//                var startResult = await OnStartAsync();
//                if (!startResult.Success)
//                {
//                    CurrentState = DeviceState.Fault;
//                    return startResult;
//                }

//                CurrentState = DeviceState.Running;
                
//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = "设备启动成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                CurrentState = DeviceState.Fault;
                
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "StartError",
//                    Message = $"设备启动失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"设备启动失败: {ex.Message}",
//                    ErrorCode = "START_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual async Task<DeviceOperationResult> StopAsync()
//        {
//            try
//            {
//                if (CurrentState == DeviceState.Stopped)
//                    return new DeviceOperationResult
//                    {
//                        Success = true,
//                        Message = "设备已停止"
//                    };

//                CurrentState = DeviceState.Stopping;

//                // 执行设备特定的停止逻辑
//                var stopResult = await OnStopAsync();
//                if (!stopResult.Success)
//                {
//                    CurrentState = DeviceState.Fault;
//                    return stopResult;
//                }

//                CurrentState = DeviceState.Stopped;
                
//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = "设备停止成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                CurrentState = DeviceState.Fault;
                
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "StopError", 
//                    Message = $"设备停止失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"设备停止失败: {ex.Message}",
//                    ErrorCode = "STOP_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual async Task<DeviceOperationResult> ResetAsync()
//        {
//            try
//            {
//                CurrentState = DeviceState.Starting;

//                // 执行设备特定的重置逻辑
//                var resetResult = await OnResetAsync();
//                if (!resetResult.Success)
//                {
//                    CurrentState = DeviceState.Fault;
//                    return resetResult;
//                }

//                // 重置参数到默认值
//                InitializeDefaultParameters();
                
//                CurrentState = DeviceState.Stopped;
                
//                return new DeviceOperationResult  
//                {
//                    Success = true,
//                    Message = "设备重置成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                CurrentState = DeviceState.Fault;
                
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "ResetError",
//                    Message = $"设备重置失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"设备重置失败: {ex.Message}",
//                    ErrorCode = "RESET_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual async Task<DeviceOperationResult> DiagnoseAsync()
//        {
//            var diagnosticResults = new List<string>();

//            try
//            {
//                // 基础诊断检查
//                diagnosticResults.Add($"设备ID: {DeviceId}");
//                diagnosticResults.Add($"设备名称: {DeviceName}");
//                diagnosticResults.Add($"设备类型: {DeviceType}");
//                diagnosticResults.Add($"当前状态: {CurrentState}");
//                diagnosticResults.Add($"控制模式: {ControlMode}");
//                diagnosticResults.Add($"是否启用: {IsEnabled}");
//                diagnosticResults.Add($"是否在线: {IsOnline}");
//                diagnosticResults.Add($"关联点位数量: {AssociatedPoints.Count}");
//                diagnosticResults.Add($"最后更新时间: {LastUpdateTime}");

//                // 执行设备特定的诊断
//                var deviceDiagnosticResult = await OnDiagnoseAsync();
//                if (deviceDiagnosticResult.Data.ContainsKey("DiagnosticResults"))
//                {
//                    var deviceResults = (List<string>)deviceDiagnosticResult.Data["DiagnosticResults"];
//                    diagnosticResults.AddRange(deviceResults);
//                }

//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = "设备诊断完成",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["DiagnosticResults"] = diagnosticResults
//                    }
//                };
//            }
//            catch (Exception ex)
//            {
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "DiagnoseError",
//                    Message = $"设备诊断失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"设备诊断失败: {ex.Message}",
//                    ErrorCode = "DIAGNOSE_ERROR",
//                    Exception = ex,
//                    Data = new Dictionary<string, object>
//                    {
//                        ["DiagnosticResults"] = diagnosticResults
//                    }
//                };
//            }
//        }

//        #endregion

//        #region ICompositeDevice 参数管理

//        public virtual object? GetParameter(string parameterName)
//        {
//            return _parameters.GetValueOrDefault(parameterName);
//        }

//        public virtual DeviceOperationResult SetParameter(string parameterName, object value)
//        {
//            try
//            {
//                if (!ValidateParameter(parameterName, value))
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = $"参数 {parameterName} 的值 {value} 无效",
//                        ErrorCode = "INVALID_PARAMETER_VALUE"
//                    };

//                var oldValue = _parameters.GetValueOrDefault(parameterName);
//                _parameters[parameterName] = value;
//                _lastUpdateTime = DateTime.Now;

//                OnParameterChanged(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "ParameterChanged",
//                    Message = $"参数 {parameterName} 从 {oldValue} 变更为 {value}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["ParameterName"] = parameterName,
//                        ["OldValue"] = oldValue ?? "null",
//                        ["NewValue"] = value
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = $"参数 {parameterName} 设置成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"设置参数时出错: {ex.Message}",
//                    ErrorCode = "SET_PARAMETER_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual IReadOnlyDictionary<string, object> GetAllParameters()
//        {
//            return new ReadOnlyDictionary<string, object>(_parameters);
//        }
        
//        // 兼容性属性 - 映射到参数字典
//        public IReadOnlyDictionary<string, object> Properties => GetAllParameters();

//        public virtual bool ValidateParameter(string parameterName, object value)
//        {
//            // 基础验证逻辑，子类可以重写
//            if (string.IsNullOrWhiteSpace(parameterName))
//                return false;

//            // 执行设备特定的参数验证
//            return OnValidateParameter(parameterName, value);
//        }

//        #endregion

//        #region ICompositeDevice 代码生成

//        public virtual async Task<string> GenerateSTCodeAsync()
//        {
//            try
//            {
//                var codeBuilder = new System.Text.StringBuilder();
                
//                // 生成变量声明
//                codeBuilder.AppendLine(GenerateVariableDeclarations());
//                codeBuilder.AppendLine();
                
//                // 生成控制逻辑
//                codeBuilder.AppendLine(GenerateControlLogic());
//                codeBuilder.AppendLine();
                
//                // 生成报警处理
//                codeBuilder.AppendLine(GenerateAlarmHandling());
                
//                return codeBuilder.ToString();
//            }
//            catch (Exception ex)
//            {
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "CodeGenerationError",
//                    Message = $"生成ST代码失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });
                
//                return $"// 生成ST代码失败: {ex.Message}";
//            }
//        }

//        public virtual string GenerateVariableDeclarations()
//        {
//            var builder = new System.Text.StringBuilder();
//            builder.AppendLine($"// {DeviceName} ({DeviceId}) 变量声明");
            
//            // 生成设备状态变量
//            builder.AppendLine($"VAR");
//            builder.AppendLine($"    {DeviceId}_State : INT;        // 设备状态");
//            builder.AppendLine($"    {DeviceId}_Mode : INT;         // 控制模式");  
//            builder.AppendLine($"    {DeviceId}_Enable : BOOL;      // 设备启用");
//            builder.AppendLine($"    {DeviceId}_Online : BOOL;      // 设备在线");
            
//            // 生成点位相关变量
//            foreach (var point in AssociatedPoints)
//            {
//                builder.AppendLine($"    {point.HmiTagName} : {GetPointVariableType(point)};    // {point.Description}");
//            }
            
//            // 生成设备特定变量
//            builder.AppendLine(OnGenerateDeviceSpecificVariables());
            
//            builder.AppendLine($"END_VAR");
            
//            return builder.ToString();
//        }

//        public virtual string GenerateControlLogic()
//        {
//            var builder = new System.Text.StringBuilder();
//            builder.AppendLine($"// {DeviceName} ({DeviceId}) 控制逻辑");
            
//            // 生成基础控制逻辑
//            builder.AppendLine($"// 设备状态管理");
//            builder.AppendLine($"{DeviceId}_State := {(int)CurrentState};");
//            builder.AppendLine($"{DeviceId}_Mode := {(int)ControlMode};");
//            builder.AppendLine($"{DeviceId}_Enable := {IsEnabled.ToString().ToUpper()};");
//            builder.AppendLine($"{DeviceId}_Online := {IsOnline.ToString().ToUpper()};");
//            builder.AppendLine();
            
//            // 生成设备特定控制逻辑
//            builder.AppendLine(OnGenerateDeviceControlLogic());
            
//            return builder.ToString();
//        }

//        public virtual string GenerateAlarmHandling()
//        {
//            var builder = new System.Text.StringBuilder();
//            builder.AppendLine($"// {DeviceName} ({DeviceId}) 报警处理");
            
//            // 生成基础报警逻辑
//            builder.AppendLine($"IF NOT {DeviceId}_Online THEN");
//            builder.AppendLine($"    // 设备离线报警");
//            builder.AppendLine($"END_IF;");
//            builder.AppendLine();
            
//            builder.AppendLine($"IF {DeviceId}_State = {(int)DeviceState.Fault} THEN");
//            builder.AppendLine($"    // 设备故障报警");
//            builder.AppendLine($"END_IF;");
//            builder.AppendLine();
            
//            // 生成设备特定报警处理
//            builder.AppendLine(OnGenerateDeviceAlarmHandling());
            
//            return builder.ToString();
//        }

//        public virtual string GetTemplateName()
//        {
//            return $"{DeviceType}_Template";
//        }

//        #endregion

//        #region IConfigurableDevice 配置功能

//        public bool IsConfigurationMode
//        {
//            get => _isConfigurationMode;
//            set
//            {
//                if (_isConfigurationMode != value)
//                {
//                    _isConfigurationMode = value;
//                    _lastUpdateTime = DateTime.Now;
                    
//                    OnParameterChanged(new DeviceEventArgs
//                    {
//                        DeviceId = DeviceId,
//                        EventType = "ConfigurationModeChanged",
//                        Message = $"配置模式变更为 {value}",
//                        Data = new Dictionary<string, object>
//                        {
//                            ["IsConfigurationMode"] = value
//                        }
//                    });
//                }
//            }
//        }

//        public virtual async Task<DeviceOperationResult> EnterConfigurationModeAsync()
//        {
//            try
//            {
//                if (CurrentState == DeviceState.Running)
//                {
//                    await StopAsync();
//                }
                
//                IsConfigurationMode = true;
//                CurrentState = DeviceState.Maintenance;
                
//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = "已进入配置模式"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"进入配置模式失败: {ex.Message}",
//                    ErrorCode = "ENTER_CONFIG_MODE_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual async Task<DeviceOperationResult> ExitConfigurationModeAsync()
//        {
//            try
//            {
//                IsConfigurationMode = false;
//                CurrentState = DeviceState.Stopped;
                
//                // 重新初始化设备
//                return await InitializeAsync();
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"退出配置模式失败: {ex.Message}",
//                    ErrorCode = "EXIT_CONFIG_MODE_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual async Task<DeviceOperationResult> ApplyConfigurationChangesAsync()
//        {
//            try
//            {
//                // 验证配置
//                var validationErrors = ValidateConfiguration();
//                if (validationErrors.Any())
//                {
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = $"配置验证失败: {string.Join(", ", validationErrors)}",
//                        ErrorCode = "CONFIGURATION_VALIDATION_FAILED"
//                    };
//                }
                
//                // 应用配置
//                var applyResult = await OnApplyConfigurationChangesAsync();
//                if (applyResult.Success)
//                {
//                    _lastUpdateTime = DateTime.Now;
//                }
                
//                return applyResult;
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"应用配置失败: {ex.Message}",
//                    ErrorCode = "APPLY_CONFIG_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual async Task<DeviceOperationResult> CancelConfigurationChangesAsync()
//        {
//            try
//            {
//                // 恢复原始配置
//                var cancelResult = await OnCancelConfigurationChangesAsync();
//                if (cancelResult.Success)
//                {
//                    _lastUpdateTime = DateTime.Now;
//                }
                
//                return cancelResult;
//            }
//            catch (Exception ex)
//            {
//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"取消配置失败: {ex.Message}",
//                    ErrorCode = "CANCEL_CONFIG_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        #endregion

//        #region IMonitorableDevice 监控功能

//        public int MonitoringInterval
//        {
//            get => _monitoringInterval;
//            set
//            {
//                if (_monitoringInterval != value && value > 0)
//                {
//                    _monitoringInterval = value;
//                    _lastUpdateTime = DateTime.Now;
//                }
//            }
//        }

//        public bool IsMonitoringEnabled
//        {
//            get => _isMonitoringEnabled;
//            set
//            {
//                if (_isMonitoringEnabled != value)
//                {
//                    _isMonitoringEnabled = value;
//                    _lastUpdateTime = DateTime.Now;
                    
//                    if (value)
//                    {
//                        StartMonitoring();
//                    }
//                    else
//                    {
//                        StopMonitoring();
//                    }
//                }
//            }
//        }

//        public virtual async Task<Dictionary<string, object>> GetMonitoringDataAsync()
//        {
//            var monitoringData = new Dictionary<string, object>
//            {
//                ["DeviceId"] = DeviceId,
//                ["DeviceName"] = DeviceName,
//                ["DeviceType"] = DeviceType.ToString(),
//                ["CurrentState"] = CurrentState.ToString(),
//                ["IsEnabled"] = IsEnabled,
//                ["IsOnline"] = IsOnline,
//                ["ControlMode"] = ControlMode.ToString(),
//                ["LastUpdateTime"] = LastUpdateTime,
//                ["Timestamp"] = DateTime.Now
//            };

//            // 添加点位当前值
//            foreach (var point in AssociatedPoints)
//            {
//                monitoringData[$"Point_{point.HmiTagName}"] = GetPointCurrentValue(point);
//            }

//            // 添加设备参数
//            foreach (var param in _parameters)
//            {
//                monitoringData[$"Param_{param.Key}"] = param.Value;
//            }

//            // 获取设备特定的监控数据
//            var deviceSpecificData = await OnGetDeviceSpecificMonitoringDataAsync();
//            foreach (var item in deviceSpecificData)
//            {
//                monitoringData[item.Key] = item.Value;
//            }

//            return monitoringData;
//        }

//        public virtual async Task<List<Dictionary<string, object>>> GetHistoricalDataAsync(DateTime startTime, DateTime endTime)
//        {
//            // 基础实现返回空列表，子类可以重写以提供实际的历史数据
//            return await OnGetHistoricalDataAsync(startTime, endTime);
//        }

//        public event EventHandler<Dictionary<string, object>>? MonitoringDataUpdated;

//        #endregion

//        #region ICompositeDevice 序列化和配置

//        public virtual string ExportConfiguration()
//        {
//            try
//            {
//                var config = new
//                {
//                    DeviceId,
//                    DeviceName,
//                    Description,
//                    DeviceType = DeviceType.ToString(),
//                    Position,
//                    ControlMode = ControlMode.ToString(),
//                    IsEnabled,
//                    Parameters = _parameters,
//                    AssociatedPoints = AssociatedPoints.Select(p => new
//                    {
//                        p.HmiTagName,
//                        p.PointType,
//                        p.Description,
//                        p.ModuleType,
//                        p.Channel,
//                        p.Unit,
//                        p.RangeMin,
//                        p.RangeMax
//                    }).ToList(),
//                    ExportTime = DateTime.Now,
//                    Version = "1.0"
//                };

//                return JsonSerializer.Serialize(config, new JsonSerializerOptions
//                {
//                    WriteIndented = true,
//                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
//                });
//            }
//            catch (Exception ex)
//            {
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "ExportConfigurationError",
//                    Message = $"导出配置失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });
                
//                return $"{{\"error\": \"导出配置失败: {ex.Message}\"}}";
//            }
//        }

//        public virtual DeviceOperationResult ImportConfiguration(string configurationJson)
//        {
//            try
//            {
//                using var document = JsonDocument.Parse(configurationJson);
//                var root = document.RootElement;

//                // 验证配置格式
//                if (!root.TryGetProperty("DeviceId", out var deviceIdElement) ||
//                    deviceIdElement.GetString() != DeviceId)
//                {
//                    return new DeviceOperationResult
//                    {
//                        Success = false,
//                        Message = "配置文件设备ID不匹配",
//                        ErrorCode = "DEVICE_ID_MISMATCH"
//                    };
//                }

//                // 导入基础配置
//                if (root.TryGetProperty("DeviceName", out var deviceNameElement))
//                    DeviceName = deviceNameElement.GetString() ?? DeviceName;

//                if (root.TryGetProperty("Description", out var descriptionElement))
//                    Description = descriptionElement.GetString() ?? Description;

//                if (root.TryGetProperty("Position", out var positionElement))
//                    Position = positionElement.GetString() ?? Position;

//                if (root.TryGetProperty("ControlMode", out var controlModeElement) &&
//                    Enum.TryParse<ControlMode>(controlModeElement.GetString(), out var controlMode))
//                    ControlMode = controlMode;

//                if (root.TryGetProperty("IsEnabled", out var isEnabledElement))
//                    IsEnabled = isEnabledElement.GetBoolean();

//                // 导入参数
//                if (root.TryGetProperty("Parameters", out var parametersElement))
//                {
//                    _parameters.Clear();
//                    foreach (var paramProperty in parametersElement.EnumerateObject())
//                    {
//                        _parameters[paramProperty.Name] = paramProperty.Value.ToString() ?? "";
//                    }
//                }

//                _lastUpdateTime = DateTime.Now;

//                return new DeviceOperationResult
//                {
//                    Success = true,
//                    Message = "配置导入成功"
//                };
//            }
//            catch (Exception ex)
//            {
//                OnErrorOccurred(new DeviceEventArgs
//                {
//                    DeviceId = DeviceId,
//                    EventType = "ImportConfigurationError",
//                    Message = $"导入配置失败: {ex.Message}",
//                    Data = new Dictionary<string, object>
//                    {
//                        ["Exception"] = ex
//                    }
//                });

//                return new DeviceOperationResult
//                {
//                    Success = false,
//                    Message = $"导入配置失败: {ex.Message}",
//                    ErrorCode = "IMPORT_CONFIG_ERROR",
//                    Exception = ex
//                };
//            }
//        }

//        public virtual List<string> ValidateConfiguration()
//        {
//            var errors = new List<string>();

//            // 基础验证
//            if (string.IsNullOrWhiteSpace(DeviceId))
//                errors.Add("设备ID不能为空");

//            if (string.IsNullOrWhiteSpace(DeviceName))
//                errors.Add("设备名称不能为空");

//            // 验证关联点位
//            errors.AddRange(ValidateAssociatedPoints());

//            // 验证设备特定配置
//            errors.AddRange(OnValidateDeviceSpecificConfiguration());

//            return errors;
//        }

//        public abstract ICompositeDevice Clone();

//        #endregion

//        #region 事件

//        public event EventHandler<DeviceEventArgs>? StateChanged;
//        public event EventHandler<DeviceEventArgs>? ParameterChanged;
//        public event EventHandler<DeviceEventArgs>? ErrorOccurred;
//        public event EventHandler<DeviceEventArgs>? WarningOccurred;
//        public event Action<List<Point>, string>? PointsChanged;

//        #endregion

//        #region 受保护的虚拟方法（供子类重写）

//        protected virtual void InitializeDefaultParameters()
//        {
//            _parameters.Clear();
//            _parameters["CreatedTime"] = DateTime.Now;
//            _parameters["Version"] = "1.0";
//        }

//        protected virtual void RegisterEventHandlers()
//        {
//            // 子类可以重写以注册特定的事件处理器
//        }

//        protected virtual DeviceOperationResult ValidatePointForDevice(Point point)
//        {
//            // 基础点位验证，子类可以重写以添加特定验证逻辑
//            return new DeviceOperationResult
//            {
//                Success = true,
//                Message = "点位验证通过"
//            };
//        }

//        protected virtual List<string> ValidateAssociatedPoints()
//        {
//            var errors = new List<string>();
            
//            // 检查重复的HMI标签名
//            var duplicateHmiTags = AssociatedPoints
//                .GroupBy(p => p.HmiTagName)
//                .Where(g => g.Count() > 1)
//                .Select(g => g.Key);
            
//            foreach (var duplicateTag in duplicateHmiTags)
//            {
//                errors.Add($"存在重复的HMI标签名: {duplicateTag}");
//            }
            
//            return errors;
//        }

//        protected virtual string GetPointVariableType(Point point)
//        {
//            return point.PointType switch
//            {
//                "AI" => "REAL",
//                "AO" => "REAL", 
//                "DI" => "BOOL",
//                "DO" => "BOOL",
//                _ => "WORD"
//            };
//        }

//        protected virtual object GetPointCurrentValue(Point point)
//        {
//            // 基础实现返回默认值，子类可以重写以返回实际值
//            return point.PointType switch
//            {
//                "AI" or "AO" => 0.0,
//                "DI" or "DO" => false,
//                _ => 0
//            };
//        }

//        protected virtual void StartMonitoring()
//        {
//            // 子类可以重写以实现具体的监控逻辑
//        }

//        protected virtual void StopMonitoring()
//        {
//            // 子类可以重写以实现具体的监控停止逻辑
//        }

//        // 子类必须实现的抽象方法
//        protected abstract Task<DeviceOperationResult> OnInitializeAsync();
//        protected abstract Task<DeviceOperationResult> OnStartAsync();
//        protected abstract Task<DeviceOperationResult> OnStopAsync();
//        protected abstract Task<DeviceOperationResult> OnResetAsync();
//        protected abstract Task<DeviceOperationResult> OnDiagnoseAsync();
//        protected abstract bool OnValidateParameter(string parameterName, object value);
//        protected abstract string OnGenerateDeviceSpecificVariables();
//        protected abstract string OnGenerateDeviceControlLogic();
//        protected abstract string OnGenerateDeviceAlarmHandling();
//        protected abstract Task<DeviceOperationResult> OnApplyConfigurationChangesAsync();
//        protected abstract Task<DeviceOperationResult> OnCancelConfigurationChangesAsync();
//        protected abstract Task<Dictionary<string, object>> OnGetDeviceSpecificMonitoringDataAsync();
//        protected abstract Task<List<Dictionary<string, object>>> OnGetHistoricalDataAsync(DateTime startTime, DateTime endTime);
//        protected abstract List<string> OnValidateDeviceSpecificConfiguration();

//        #endregion

//        #region 事件触发方法

//        protected virtual void OnStateChanged(DeviceEventArgs e)
//        {
//            StateChanged?.Invoke(this, e);
//        }

//        protected virtual void OnParameterChanged(DeviceEventArgs e)
//        {
//            ParameterChanged?.Invoke(this, e);
//        }

//        protected virtual void OnErrorOccurred(DeviceEventArgs e)
//        {
//            ErrorOccurred?.Invoke(this, e);
//        }

//        protected virtual void OnWarningOccurred(DeviceEventArgs e)
//        {
//            WarningOccurred?.Invoke(this, e);
//        }

//        protected virtual void OnMonitoringDataUpdated(Dictionary<string, object> data)
//        {
//            MonitoringDataUpdated?.Invoke(this, data);
//        }

//        #endregion

//        #region 私有辅助方法

//        /// <summary>
//        /// 触发点位变更事件
//        /// </summary>
//        /// <param name="points">变更的点位列表</param>
//        /// <param name="changeType">变更类型</param>
//        protected virtual void OnPointsChanged(List<Point> points, string changeType)
//        {
//            PointsChanged?.Invoke(points, changeType);
//        }

//        #endregion
//    }
//}
