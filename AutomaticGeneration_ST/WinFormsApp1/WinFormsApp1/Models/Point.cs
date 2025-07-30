using System;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 代表一个独立的IO点位或设备中的一个点位。
    /// 该类的所有属性严格对应于"IO点表.csv"文件的列。
    /// </summary>
    public class Point
    {
        // --- 来自IO点表的12个核心字段 ---

        /// <summary>
        /// 对应中文列: 模块名称
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// 对应中文列: 模块类型 (例如: AI, AO, DI, DO)
        /// </summary>
        public string? ModuleType { get; set; }

        /// <summary>
        /// 对应中文列: 供电类型（有源/无源）
        /// </summary>
        public string? PowerSupplyType { get; set; }

        /// <summary>
        /// 对应中文列: 线制
        /// </summary>
        public string? WireSystem { get; set; }

        /// <summary>
        /// 对应中文列: 通道位号
        /// </summary>
        public string? ChannelNumber { get; set; }
        
        // 兼容性属性 - 映射到ChannelNumber
        public string? Channel => ChannelNumber;

        /// <summary>
        /// 对应中文列: 场站名
        /// </summary>
        public string? StationName { get; set; }

        /// <summary>
        /// 对应中文列: 场站编号
        /// </summary>
        public string? StationId { get; set; }

        /// <summary>
        /// 对应中文列: 变量名称（HMI）。这是点位的唯一标识符，至关重要。
        /// </summary>
        public string HmiTagName { get; set; } = "";
        
        /// <summary>
        /// 设备点位名称 - 设备中的点位标识
        /// </summary>
        public string? DevicePointName { get; set; }
        
        
        // 兼容性属性 - 添加缺失的属性
        /// <summary>
        /// 点位ID - 唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 点位名称 - 兼容性属性，映射到HmiTagName
        /// </summary>
        public string Name => HmiTagName;
        
        /// <summary>
        /// 点位类型 - 兼容性属性，映射到ModuleType或PointType
        /// </summary>
        public string? Type => ModuleType ?? PointType;
        
        /// <summary>
        /// HMI标签名 - 兼容性属性，映射到HmiTagName
        /// </summary>
        public string HMITagName => HmiTagName;
        
        /// <summary>
        /// 点位地址 - 兼容性属性，映射到PlcAbsoluteAddress
        /// </summary>
        public string? Address => PlcAbsoluteAddress;
        
        /// <summary>
        /// 最小值 - 兼容性属性，映射到RangeLow
        /// </summary>
        public double? MinValue => RangeLow;
        
        /// <summary>
        /// 最大值 - 兼容性属性，映射到RangeHigh
        /// </summary>
        public double? MaxValue => RangeHigh;

        /// <summary>
        /// 对应中文列: 变量描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 对应中文列: 数据类型 (例如: BOOL, REAL)
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// 对应中文列: PLC绝对地址 (例如: %IX0.0.0)
        /// </summary>
        public string? PlcAbsoluteAddress { get; set; }

        /// <summary>
        /// 对应中文列: 上位机通讯地址
        /// </summary>
        public string? ScadaCommAddress { get; set; }

        // --- 来自IO点表的所有其他可为空字段 ---

        /// <summary>
        /// 对应中文列: 是否历史存储
        /// </summary>
        public bool? StoreHistory { get; set; }

        /// <summary>
        /// 对应中文列: 是否掉电保护
        /// </summary>
        public bool? PowerDownProtection { get; set; }

        /// <summary>
        /// 对应中文列: 量程低
        /// </summary>
        public double? RangeLow { get; set; }

        /// <summary>
        /// 对应中文列: 量程高
        /// </summary>
        public double? RangeHigh { get; set; }
        
        // 兼容性属性 - 映射到RangeLow和RangeHigh
        public double? RangeMin => RangeLow;
        public double? RangeMax => RangeHigh;

        /// <summary>
        /// 对应中文列: 单位
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 对应中文列: 仪表类型
        /// </summary>
        public string? InstrumentType { get; set; }

        /// <summary>
        /// 对应中文列: SLL值
        /// </summary>
        public double? SLL_Value { get; set; }

        /// <summary>
        /// 对应中文列: SLL点
        /// </summary>
        public string? SLL_Point { get; set; }

        /// <summary>
        /// 对应中文列: SLL点PLC地址
        /// </summary>
        public string? SLL_PlcAddress { get; set; }

        /// <summary>
        /// 对应中文列: SL值
        /// </summary>
        public double? SL_Value { get; set; }

        /// <summary>
        /// 对应中文列: SL点
        /// </summary>
        public string? SL_Point { get; set; }

        /// <summary>
        /// 对应中文列: SL点PLC地址
        /// </summary>
        public string? SL_PlcAddress { get; set; }

        /// <summary>
        /// 对应中文列: SH值
        /// </summary>
        public double? SH_Value { get; set; }

        /// <summary>
        /// 对应中文列: SH点
        /// </summary>
        public string? SH_Point { get; set; }

        /// <summary>
        /// 对应中文列: SH点PLC地址
        /// </summary>
        public string? SH_PlcAddress { get; set; }

        /// <summary>
        /// 对应中文列: SHH值
        /// </summary>
        public double? SHH_Value { get; set; }

        /// <summary>
        /// 对应中文列: SHH点
        /// </summary>
        public string? SHH_Point { get; set; }

        /// <summary>
        /// 对应中文列: SHH点PLC地址
        /// </summary>
        public string? SHH_PlcAddress { get; set; }

        /// <summary>
        /// 对应中文列: 硬件报警对应的HMI变量
        /// </summary>
        public string? HardwareAlarmHmiTag { get; set; }

        /// <summary>
        /// 对应中文列: 硬件报警对应的PLC地址
        /// </summary>
        public string? HardwareAlarmPlcAddress { get; set; }

        /// <summary>
        /// 对应中文列: 点位类型 (例如: 硬点, 软点)
        /// </summary>
        public string? PointType { get; set; }

        // --- 报警相关属性 ---
        
        /// <summary>
        /// 是否启用报警功能
        /// </summary>
        public bool IsAlarmEnabled { get; set; } = false;

        /// <summary>
        /// 高报警阈值
        /// </summary>
        public double AlarmHigh { get; set; } = 0.0;

        /// <summary>
        /// 低报警阈值
        /// </summary>
        public double AlarmLow { get; set; } = 0.0;

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Point() 
        {
        }

        /// <summary>
        /// 构造函数，强制要求提供HMI变量名称。
        /// </summary>
        /// <param name="hmiTagName">来自"变量名称（HMI）"列的值。</param>
        public Point(string hmiTagName)
        {
            if (string.IsNullOrWhiteSpace(hmiTagName))
            {
                throw new ArgumentException("HMI Tag Name (变量名称（HMI）) cannot be null or empty.", nameof(hmiTagName));
            }
            HmiTagName = hmiTagName;
        }
    }
}