//NEED DELETE: 设备管理接口（遗留），与核心生成链路无关
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutomaticGeneration_ST.Models;
using Point = AutomaticGeneration_ST.Models.Point;

namespace WinFormsApp1.Devices.Interfaces
{
    /// <summary>
    /// 设备状态枚举
    /// </summary>
    public enum DeviceState
    {
        /// <summary>
        /// 停止状态
        /// </summary>
        Stopped = 0,
        
        /// <summary>
        /// 启动中
        /// </summary>
        Starting = 1,
        
        /// <summary>
        /// 运行中
        /// </summary>
        Running = 2,
        
        /// <summary>
        /// 停止中
        /// </summary>
        Stopping = 3,
        
        /// <summary>
        /// 故障状态
        /// </summary>
        Fault = 4,
        
        /// <summary>
        /// 维护模式
        /// </summary>
        Maintenance = 5,
        
        /// <summary>
        /// 手动模式
        /// </summary>
        Manual = 6,
        
        /// <summary>
        /// 自动模式
        /// </summary>
        Auto = 7
    }

    /// <summary>
    /// 设备类型枚举
    /// </summary>
    public enum CompositeDeviceType
    {
        /// <summary>
        /// 阀门控制器
        /// </summary>
        ValveController,
        
        /// <summary>
        /// 泵站控制器
        /// </summary>
        PumpController,
        
        /// <summary>
        /// 变频器控制器
        /// </summary>
        VFDController,
        
        /// <summary>
        /// 储罐控制器
        /// </summary>
        TankController,
        
        /// <summary>
        /// 换热器控制器
        /// </summary>
        HeatExchangerController,
        
        /// <summary>
        /// 反应器控制器
        /// </summary>
        ReactorController,
        
        /// <summary>
        /// 自定义设备
        /// </summary>
        CustomDevice
    }

    /// <summary>
    /// 设备控制模式
    /// </summary>
    public enum ControlMode
    {
        /// <summary>
        /// 手动控制
        /// </summary>
        Manual,
        
        /// <summary>
        /// 自动控制
        /// </summary>
        Auto,
        
        /// <summary>
        /// 级联控制
        /// </summary>
        Cascade,
        
        /// <summary>
        /// 远程控制
        /// </summary>
        Remote
    }

    /// <summary>
    /// 设备事件参数
    /// </summary>
    public class DeviceEventArgs : EventArgs
    {
        public string DeviceId { get; set; } = "";
        public string EventType { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// 设备操作结果
    /// </summary>
    public class DeviceOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string ErrorCode { get; set; } = "";
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// 组合设备接口 - 定义组合设备的基本契约
    /// </summary>
    public interface ICompositeDevice
    {
        #region 基本属性

        /// <summary>
        /// 设备唯一标识符
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// 设备名称
        /// </summary>
        string DeviceName { get; set; }

        /// <summary>
        /// 设备描述
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        CompositeDeviceType DeviceType { get; }

        /// <summary>
        /// 设备位号
        /// </summary>
        string Position { get; set; }

        /// <summary>
        /// 设备当前状态
        /// </summary>
        DeviceState CurrentState { get; }

        /// <summary>
        /// 控制模式
        /// </summary>
        ControlMode ControlMode { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 是否在线
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        DateTime LastUpdateTime { get; }

        #endregion

        #region 点位管理

        /// <summary>
        /// 关联的点位列表
        /// </summary>
        IReadOnlyList<Point> AssociatedPoints { get; }

        /// <summary>
        /// 输入点位（AI/DI）
        /// </summary>
        IReadOnlyList<Point> InputPoints { get; }

        /// <summary>
        /// 输出点位（AO/DO）
        /// </summary>
        IReadOnlyList<Point> OutputPoints { get; }

        /// <summary>
        /// 添加关联点位
        /// </summary>
        /// <param name="point">要添加的点位</param>
        /// <returns>操作结果</returns>
        DeviceOperationResult AddPoint(Point point);

        /// <summary>
        /// 移除关联点位
        /// </summary>
        /// <param name="pointId">点位ID</param>
        /// <returns>操作结果</returns>
        DeviceOperationResult RemovePoint(string pointId);

        /// <summary>
        /// 获取指定类型的点位
        /// </summary>
        /// <param name="pointType">点位类型</param>
        /// <returns>点位列表</returns>
        IReadOnlyList<Point> GetPointsByType(string pointType);

        /// <summary>
        /// 根据HMI标签名获取点位
        /// </summary>
        /// <param name="hmiTagName">HMI标签名</param>
        /// <returns>匹配的点位，未找到返回null</returns>
        Point? GetPointByHmiTag(string hmiTagName);

        #endregion

        #region 设备操作

        /// <summary>
        /// 初始化设备
        /// </summary>
        /// <returns>初始化结果</returns>
        Task<DeviceOperationResult> InitializeAsync();

        /// <summary>
        /// 启动设备
        /// </summary>
        /// <returns>启动结果</returns>
        Task<DeviceOperationResult> StartAsync();

        /// <summary>
        /// 停止设备
        /// </summary>
        /// <returns>停止结果</returns>
        Task<DeviceOperationResult> StopAsync();

        /// <summary>
        /// 重置设备
        /// </summary>
        /// <returns>重置结果</returns>
        Task<DeviceOperationResult> ResetAsync();

        /// <summary>
        /// 诊断设备状态
        /// </summary>
        /// <returns>诊断结果</returns>
        Task<DeviceOperationResult> DiagnoseAsync();

        #endregion

        #region 参数管理

        /// <summary>
        /// 获取设备参数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <returns>参数值</returns>
        object? GetParameter(string parameterName);

        /// <summary>
        /// 设置设备参数
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">参数值</param>
        /// <returns>设置结果</returns>
        DeviceOperationResult SetParameter(string parameterName, object value);

        /// <summary>
        /// 获取所有参数
        /// </summary>
        /// <returns>参数字典</returns>
        IReadOnlyDictionary<string, object> GetAllParameters();

        /// <summary>
        /// 验证参数有效性
        /// </summary>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">参数值</param>
        /// <returns>验证结果</returns>
        bool ValidateParameter(string parameterName, object value);

        #endregion

        #region 代码生成

        /// <summary>
        /// 生成设备的ST控制代码
        /// </summary>
        /// <returns>生成的ST代码</returns>
        Task<string> GenerateSTCodeAsync();

        /// <summary>
        /// 生成设备的变量声明代码
        /// </summary>
        /// <returns>变量声明代码</returns>
        string GenerateVariableDeclarations();

        /// <summary>
        /// 生成设备的控制逻辑代码
        /// </summary>
        /// <returns>控制逻辑代码</returns>
        string GenerateControlLogic();

        /// <summary>
        /// 生成设备的报警处理代码
        /// </summary>
        /// <returns>报警处理代码</returns>
        string GenerateAlarmHandling();

        /// <summary>
        /// 获取设备使用的模板名称
        /// </summary>
        /// <returns>模板名称</returns>
        string GetTemplateName();

        #endregion

        #region 事件处理

        /// <summary>
        /// 设备状态变更事件
        /// </summary>
        event EventHandler<DeviceEventArgs>? StateChanged;

        /// <summary>
        /// 设备参数变更事件
        /// </summary>
        event EventHandler<DeviceEventArgs>? ParameterChanged;

        /// <summary>
        /// 设备错误事件
        /// </summary>
        event EventHandler<DeviceEventArgs>? ErrorOccurred;

        /// <summary>
        /// 设备警告事件
        /// </summary>
        event EventHandler<DeviceEventArgs>? WarningOccurred;

        #endregion

        #region 序列化和配置

        /// <summary>
        /// 导出设备配置
        /// </summary>
        /// <returns>配置JSON字符串</returns>
        string ExportConfiguration();

        /// <summary>
        /// 导入设备配置
        /// </summary>
        /// <param name="configurationJson">配置JSON字符串</param>
        /// <returns>导入结果</returns>
        DeviceOperationResult ImportConfiguration(string configurationJson);

        /// <summary>
        /// 验证设备配置
        /// </summary>
        /// <returns>验证结果列表</returns>
        List<string> ValidateConfiguration();

        /// <summary>
        /// 克隆设备实例
        /// </summary>
        /// <returns>设备副本</returns>
        ICompositeDevice Clone();

        #endregion
    }

    /// <summary>
    /// 可配置设备接口 - 扩展设备配置功能
    /// </summary>
    public interface IConfigurableDevice : ICompositeDevice
    {
        /// <summary>
        /// 设备配置模式
        /// </summary>
        bool IsConfigurationMode { get; set; }

        /// <summary>
        /// 进入配置模式
        /// </summary>
        /// <returns>操作结果</returns>
        Task<DeviceOperationResult> EnterConfigurationModeAsync();

        /// <summary>
        /// 退出配置模式
        /// </summary>
        /// <returns>操作结果</returns>
        Task<DeviceOperationResult> ExitConfigurationModeAsync();

        /// <summary>
        /// 应用配置更改
        /// </summary>
        /// <returns>操作结果</returns>
        Task<DeviceOperationResult> ApplyConfigurationChangesAsync();

        /// <summary>
        /// 取消配置更改
        /// </summary>
        /// <returns>操作结果</returns>
        Task<DeviceOperationResult> CancelConfigurationChangesAsync();
    }

    /// <summary>
    /// 可监控设备接口 - 扩展设备监控功能
    /// </summary>
    public interface IMonitorableDevice : ICompositeDevice
    {
        /// <summary>
        /// 监控数据更新间隔（毫秒）
        /// </summary>
        int MonitoringInterval { get; set; }

        /// <summary>
        /// 是否启用监控
        /// </summary>
        bool IsMonitoringEnabled { get; set; }

        /// <summary>
        /// 获取实时监控数据
        /// </summary>
        /// <returns>监控数据字典</returns>
        Task<Dictionary<string, object>> GetMonitoringDataAsync();

        /// <summary>
        /// 获取历史数据
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>历史数据列表</returns>
        Task<List<Dictionary<string, object>>> GetHistoricalDataAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// 监控数据更新事件
        /// </summary>
        event EventHandler<Dictionary<string, object>>? MonitoringDataUpdated;
    }

    /// <summary>
    /// 可通讯设备接口 - 扩展设备通讯功能
    /// </summary>
    public interface ICommunicableDevice : ICompositeDevice
    {
        /// <summary>
        /// 通讯协议类型
        /// </summary>
        string CommunicationProtocol { get; set; }

        /// <summary>
        /// 通讯地址
        /// </summary>
        string CommunicationAddress { get; set; }

        /// <summary>
        /// 通讯超时时间（毫秒）
        /// </summary>
        int CommunicationTimeout { get; set; }

        /// <summary>
        /// 建立通讯连接
        /// </summary>
        /// <returns>连接结果</returns>
        Task<DeviceOperationResult> ConnectAsync();

        /// <summary>
        /// 断开通讯连接
        /// </summary>
        /// <returns>断开结果</returns>
        Task<DeviceOperationResult> DisconnectAsync();

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <returns>发送结果</returns>
        Task<DeviceOperationResult> SendDataAsync(byte[] data);

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <returns>接收到的数据</returns>
        Task<byte[]> ReceiveDataAsync();

        /// <summary>
        /// 通讯状态变更事件
        /// </summary>
        event EventHandler<bool>? CommunicationStatusChanged;
    }
}
