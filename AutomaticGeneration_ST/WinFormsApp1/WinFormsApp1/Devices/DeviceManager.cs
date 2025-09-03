////NEED DELETE: 设备管理服务（遗留），非核心导入-生成-导出链路
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using WinFormsApp1.Templates;
//using WinFormsApp1.Devices.Interfaces;

//namespace WinFormsApp1.Devices
//{
//    /// <summary>
//    /// 设备类型
//    /// </summary>
//    public enum DeviceType
//    {
//        Motor,          // 电机
//        Valve,          // 阀门
//        Pump,           // 泵
//        Tank,           // 储罐
//        Sensor,         // 传感器
//        Controller,     // 控制器
//        Custom          // 自定义设备
//    }

//    /// <summary>
//    /// 设备状态
//    /// </summary>
//    public enum DeviceStatus
//    {
//        Stopped,        // 停止
//        Running,        // 运行
//        Error,          // 故障
//        Maintenance,    // 维护
//        Unknown         // 未知
//    }

//    /// <summary>
//    /// 设备点位信息
//    /// </summary>
//    public class DevicePoint
//    {
//        public string Name { get; set; } = "";
//        public PointType Type { get; set; }
//        public string Address { get; set; } = "";
//        public string Description { get; set; } = "";
//        public string Unit { get; set; } = "";
//        public double MinValue { get; set; }
//        public double MaxValue { get; set; }
//        public bool IsAlarmEnabled { get; set; }
//        public double AlarmHigh { get; set; }
//        public double AlarmLow { get; set; }
//        public Dictionary<string, object> Properties { get; set; } = new();
//    }


//    /// <summary>
//    /// 设备模板定义
//    /// </summary>
//    public class DeviceTemplate
//    {
//        public string Id { get; set; } = Guid.NewGuid().ToString();
//        public string Name { get; set; } = "";
//        public DeviceType Type { get; set; }
//        public string Description { get; set; } = "";
//        public List<DevicePoint> DefaultPoints { get; set; } = new();
//        public string CodeTemplate { get; set; } = "";
//        public Dictionary<string, object> DefaultProperties { get; set; } = new();
//        public bool IsBuiltIn { get; set; }
//    }

//    /// <summary>
//    /// 设备管理器
//    /// </summary>
//    /// <remarks>
//    /// 状态: @zombie-complete
//    /// 优先级: P0 (零风险级别)
//    /// 调用情况: 零调用 (经MCP工具扫描确认)
//    /// 扫描工具: MCP代码分析工具 v1.0
//    /// 扫描时间: 2025-08-15
//    /// 功能完整性: 功能完整但从未被使用
//    /// 创建时间: v1.0版本引入
//    /// 预期用途: 静态设备管理功能，提供设备CRUD操作
//    /// 文件依赖: 无
//    /// 与CompositeDeviceManager重复: 功能重叠
//    /// 风险评估: 零风险 (无任何引用)
//    /// 注释时间: 2025-08-15
//    /// 注释人: 配置专家
//    /// 可安全移除: 是
//    /// </remarks>
//    public static class DeviceManager
//    {
//        private static readonly Dictionary<string, CompositeDevice> _devices = new();
//        private static readonly Dictionary<string, DeviceTemplate> _templates = new();

//        public static event EventHandler<CompositeDevice>? DeviceAdded;
//        public static event EventHandler<CompositeDevice>? DeviceUpdated;
//        public static event EventHandler<string>? DeviceRemoved;

//        static DeviceManager()
//        {
//            InitializeBuiltInTemplates();
//        }

//        /// <summary>
//        /// 初始化内置设备模板
//        /// </summary>
//        private static void InitializeBuiltInTemplates()
//        {
//            // 电机设备模板
//            var motorTemplate = new DeviceTemplate
//            {
//                Name = "标准电机",
//                Type = DeviceType.Motor,
//                Description = "标准三相异步电机控制",
//                IsBuiltIn = true,
//                DefaultPoints = new List<DevicePoint>
//                {
//                    new DevicePoint { Name = "启动命令", Type = PointType.DO, Description = "电机启动控制" },
//                    new DevicePoint { Name = "停止命令", Type = PointType.DO, Description = "电机停止控制" },
//                    new DevicePoint { Name = "运行反馈", Type = PointType.DI, Description = "电机运行状态反馈" },
//                    new DevicePoint { Name = "故障反馈", Type = PointType.DI, Description = "电机故障状态反馈" },
//                    new DevicePoint { Name = "电流反馈", Type = PointType.AI, Description = "电机运行电流", Unit = "A", MinValue = 0, MaxValue = 100 },
//                    new DevicePoint { Name = "频率设定", Type = PointType.AO, Description = "变频器频率设定", Unit = "Hz", MinValue = 0, MaxValue = 50 }
//                },
//                CodeTemplate = @"
//// 电机控制块: {{设备名称}}
//TYPE Motor_{{设备名称}}_Type :
//STRUCT
//    Start_Cmd : BOOL;           // 启动命令
//    Stop_Cmd : BOOL;            // 停止命令
//    Running_FB : BOOL;          // 运行反馈
//    Fault_FB : BOOL;            // 故障反馈
//    Current_FB : REAL;          // 电流反馈
//    Frequency_SP : REAL;        // 频率设定
//    Status : INT;               // 状态字
//    Auto_Mode : BOOL := TRUE;   // 自动模式
//    Enable : BOOL := TRUE;      // 使能
//END_STRUCT
//END_TYPE

//VAR
//    {{设备名称}} : Motor_{{设备名称}}_Type;
//    {{设备名称}}_FB : FB_Motor_Control;
//END_VAR

//// 电机控制逻辑
//{{设备名称}}_FB(
//    Start_Cmd := {{设备名称}}.Start_Cmd,
//    Stop_Cmd := {{设备名称}}.Stop_Cmd,
//    Auto_Mode := {{设备名称}}.Auto_Mode,
//    Enable := {{设备名称}}.Enable,
//    Running_FB => {{设备名称}}.Running_FB,
//    Fault_FB => {{设备名称}}.Fault_FB,
//    Status => {{设备名称}}.Status
//);

//// IO映射
//DO_{{启动命令地址}} := {{设备名称}}.Start_Cmd AND {{设备名称}}.Enable;
//DO_{{停止命令地址}} := {{设备名称}}.Stop_Cmd OR NOT {{设备名称}}.Enable;
//{{设备名称}}.Running_FB := DI_{{运行反馈地址}};
//{{设备名称}}.Fault_FB := DI_{{故障反馈地址}};
//{{设备名称}}.Current_FB := AI_{{电流反馈地址}};
//AO_{{频率设定地址}} := {{设备名称}}.Frequency_SP;
//"
//            };

//            // 阀门设备模板
//            var valveTemplate = new DeviceTemplate
//            {
//                Name = "标准阀门",
//                Type = DeviceType.Valve,
//                Description = "标准开关阀门控制",
//                IsBuiltIn = true,
//                DefaultPoints = new List<DevicePoint>
//                {
//                    new DevicePoint { Name = "开阀命令", Type = PointType.DO, Description = "阀门开启控制" },
//                    new DevicePoint { Name = "关阀命令", Type = PointType.DO, Description = "阀门关闭控制" },
//                    new DevicePoint { Name = "开到位反馈", Type = PointType.DI, Description = "阀门开到位反馈" },
//                    new DevicePoint { Name = "关到位反馈", Type = PointType.DI, Description = "阀门关到位反馈" },
//                    new DevicePoint { Name = "故障反馈", Type = PointType.DI, Description = "阀门故障反馈" },
//                    new DevicePoint { Name = "开度反馈", Type = PointType.AI, Description = "阀门开度反馈", Unit = "%", MinValue = 0, MaxValue = 100 }
//                },
//                CodeTemplate = @"
//// 阀门控制块: {{设备名称}}
//TYPE Valve_{{设备名称}}_Type :
//STRUCT
//    Open_Cmd : BOOL;            // 开阀命令
//    Close_Cmd : BOOL;           // 关阀命令
//    Open_FB : BOOL;             // 开到位反馈
//    Close_FB : BOOL;            // 关到位反馈
//    Fault_FB : BOOL;            // 故障反馈
//    Position_FB : REAL;         // 开度反馈
//    Status : INT;               // 状态字
//    Auto_Mode : BOOL := TRUE;   // 自动模式
//    Enable : BOOL := TRUE;      // 使能
//    Interlock : BOOL := TRUE;   // 联锁条件
//END_STRUCT
//END_TYPE

//VAR
//    {{设备名称}} : Valve_{{设备名称}}_Type;
//    {{设备名称}}_FB : FB_Valve_Control;
//END_VAR

//// 阀门控制逻辑
//{{设备名称}}_FB(
//    Open_Cmd := {{设备名称}}.Open_Cmd,
//    Close_Cmd := {{设备名称}}.Close_Cmd,
//    Auto_Mode := {{设备名称}}.Auto_Mode,
//    Enable := {{设备名称}}.Enable,
//    Interlock := {{设备名称}}.Interlock,
//    Open_FB => {{设备名称}}.Open_FB,
//    Close_FB => {{设备名称}}.Close_FB,
//    Fault_FB => {{设备名称}}.Fault_FB,
//    Status => {{设备名称}}.Status
//);

//// IO映射
//DO_{{开阀命令地址}} := {{设备名称}}.Open_Cmd AND {{设备名称}}.Enable AND {{设备名称}}.Interlock;
//DO_{{关阀命令地址}} := {{设备名称}}.Close_Cmd AND {{设备名称}}.Enable;
//{{设备名称}}.Open_FB := DI_{{开到位反馈地址}};
//{{设备名称}}.Close_FB := DI_{{关到位反馈地址}};
//{{设备名称}}.Fault_FB := DI_{{故障反馈地址}};
//{{设备名称}}.Position_FB := AI_{{开度反馈地址}};
//"
//            };

//            _templates[motorTemplate.Id] = motorTemplate;
//            _templates[valveTemplate.Id] = valveTemplate;
//        }

//        /// <summary>
//        /// 创建组合设备
//        /// </summary>
//        public static CompositeDevice CreateDevice(string name, DeviceType type, string? templateId = null)
//        {
//            // 暂时不支持创建设备实例（CompositeDevice是抽象类）
//            throw new NotImplementedException("设备创建功能暂未实现。CompositeDevice是抽象类，需要具体的实现类。");
//        }

//        /// <summary>
//        /// 更新设备
//        /// </summary>
//        public static bool UpdateDevice(CompositeDevice device)
//        {
//            if (_devices.ContainsKey(device.Id))
//            {
//                device.ModifiedTime = DateTime.Now;
//                _devices[device.Id] = device;
//                DeviceUpdated?.Invoke(null, device);
//                return true;
//            }
//            return false;
//        }

//        /// <summary>
//        /// 删除设备
//        /// </summary>
//        public static bool RemoveDevice(string deviceId)
//        {
//            if (_devices.Remove(deviceId))
//            {
//                DeviceRemoved?.Invoke(null, deviceId);
//                return true;
//            }
//            return false;
//        }

//        /// <summary>
//        /// 获取设备
//        /// </summary>
//        public static CompositeDevice? GetDevice(string deviceId)
//        {
//            _devices.TryGetValue(deviceId, out var device);
//            return device;
//        }

//        /// <summary>
//        /// 获取所有设备
//        /// </summary>
//        public static List<CompositeDevice> GetAllDevices()
//        {
//            return _devices.Values.Where(d => d.IsActive).ToList();
//        }

//        /// <summary>
//        /// 按类型获取设备
//        /// </summary>
//        public static List<CompositeDevice> GetDevicesByType(CompositeDeviceType type)
//        {
//            return _devices.Values.Where(d => d.IsActive && d.Type == type).ToList();
//        }

//        /// <summary>
//        /// 将CompositeDeviceType转换为DeviceType
//        /// </summary>
//        private static DeviceType ConvertToDeviceType(CompositeDeviceType compositeType)
//        {
//            return compositeType switch
//            {
//                CompositeDeviceType.ValveController => DeviceType.Valve,
//                CompositeDeviceType.PumpController => DeviceType.Pump,
//                CompositeDeviceType.VFDController => DeviceType.Controller,
//                CompositeDeviceType.TankController => DeviceType.Tank,
//                _ => DeviceType.Custom
//            };
//        }

//        /// <summary>
//        /// 生成设备ST代码
//        /// </summary>
//        public static string GenerateDeviceCode(CompositeDevice device)
//        {
//            var deviceType = ConvertToDeviceType(device.Type);
//            var template = _templates.Values.FirstOrDefault(t => t.Type == deviceType);
//            if (template == null) return "";

//            var code = template.CodeTemplate;
            
//            // 替换设备名称
//            code = code.Replace("{{设备名称}}", device.DeviceName);
            
//            // 替换点位地址
//            foreach (var point in device.Points)
//            {
//                var placeholder = $"{{{{{point.Name}地址}}}}";
//                code = code.Replace(placeholder, point.Address);
//            }

//            return code;
//        }

//        /// <summary>
//        /// 获取设备模板
//        /// </summary>
//        public static List<DeviceTemplate> GetDeviceTemplates()
//        {
//            return _templates.Values.ToList();
//        }

//        /// <summary>
//        /// 获取指定类型的设备模板
//        /// </summary>
//        public static List<DeviceTemplate> GetDeviceTemplates(DeviceType type)
//        {
//            return _templates.Values.Where(t => t.Type == type).ToList();
//        }

//        /// <summary>
//        /// 添加自定义设备模板
//        /// </summary>
//        public static bool AddDeviceTemplate(DeviceTemplate template)
//        {
//            if (template.IsBuiltIn) return false;
            
//            _templates[template.Id] = template;
//            return true;
//        }

//        /// <summary>
//        /// 验证设备配置
//        /// </summary>
//        public static List<string> ValidateDevice(CompositeDevice device)
//        {
//            var errors = new List<string>();

//            if (string.IsNullOrWhiteSpace(device.DeviceName))
//                errors.Add("设备名称不能为空");

//            if (device.Points.Count == 0)
//                errors.Add("设备必须至少包含一个点位");

//            // 检查点位名称重复
//            var duplicateNames = device.Points
//                .GroupBy(p => p.Name)
//                .Where(g => g.Count() > 1)
//                .Select(g => g.Key);

//            foreach (var name in duplicateNames)
//            {
//                errors.Add($"点位名称重复: {name}");
//            }

//            // 检查地址重复
//            var duplicateAddresses = device.Points
//                .Where(p => !string.IsNullOrEmpty(p.Address))
//                .GroupBy(p => p.Address)
//                .Where(g => g.Count() > 1)
//                .Select(g => g.Key);

//            foreach (var address in duplicateAddresses)
//            {
//                errors.Add($"点位地址重复: {address}");
//            }

//            return errors;
//        }
//    }
//}
