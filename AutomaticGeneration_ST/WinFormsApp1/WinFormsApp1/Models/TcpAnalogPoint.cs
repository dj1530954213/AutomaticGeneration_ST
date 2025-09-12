//--NEED DELETE: 未集成的TCP通信点模型
// 处理建议: 若不保留TCP链路，删除本文件并清理引用（ServiceContainer注册、Orchestrator、Tcp*服务/导出器等）。
//REASON: This model is part of the unused TCP communication architecture and is not integrated into the main UI workflow.

namespace WinFormsApp1.Models
{
    /// <summary>
    /// TCP模拟量通讯点位
    /// </summary>
    public class TcpAnalogPoint : TcpCommunicationPoint
    {
        /// <summary>
        /// 缩放因子
        /// </summary>
        public double? Scale { get; set; }

        /// <summary>
        /// 工程单位
        /// </summary>
        public string Unit { get; set; } = "";

        /// <summary>
        /// 量程最小值
        /// </summary>
        public double? RangeMin { get; set; }

        /// <summary>
        /// 量程最大值
        /// </summary>
        public double? RangeMax { get; set; }

        /// <summary>
        /// 高高报警值
        /// </summary>
        public double? ShhValue { get; set; }

        /// <summary>
        /// 高报警值
        /// </summary>
        public double? ShValue { get; set; }

        /// <summary>
        /// 低报警值
        /// </summary>
        public double? SlValue { get; set; }

        /// <summary>
        /// 低低报警值
        /// </summary>
        public double? SllValue { get; set; }

        /// <summary>
        /// 高高报警点名称
        /// </summary>
        public string ShhPoint { get; set; } = "";

        /// <summary>
        /// 高报警点名称
        /// </summary>
        public string ShPoint { get; set; } = "";

        /// <summary>
        /// 低报警点名称
        /// </summary>
        public string SlPoint { get; set; } = "";

        /// <summary>
        /// 低低报警点名称
        /// </summary>
        public string SllPoint { get; set; } = "";

        /// <summary>
        /// 维护状态变量名称
        /// </summary>
        public string MaintenanceStatusTag { get; set; } = "";

        /// <summary>
        /// 维护值变量名称
        /// </summary>
        public string MaintenanceValueTag { get; set; } = "";

        /// <summary>
        /// 第二个通道地址，供模板直接使用
        /// </summary>
        public string SecondChannel => GetSecondChannel();

        /// <summary>
        /// 是否有报警配置
        /// </summary>
        public bool HasAlarmConfiguration => 
            ShhValue.HasValue || ShValue.HasValue || SlValue.HasValue || SllValue.HasValue;

        /// <summary>
        /// 获取需要的通道数量（基于数据类型）
        /// </summary>
        public int GetRequiredChannelCount()
        {
            return DataType?.Trim().ToUpper() switch
            {
                "REAL" => 2,
                "DINT" => 2,
                "INT" => 1,
                _ => 1
            };
        }

        /// <summary>
        /// 获取第二个通道地址（用于REAL和DINT类型）
        /// </summary>
        public string GetSecondChannel()
        {
            if (GetRequiredChannelCount() <= 1 || string.IsNullOrWhiteSpace(Channel))
                return "";

            var parts = Channel.Split('_');
            if (parts.Length > 0 && int.TryParse(parts[^1], out int lastNumber))
            {
                parts[^1] = (lastNumber + 1).ToString();
                return string.Join("_", parts);
            }

            return Channel + "_1";
        }

        public override bool IsValid()
        {
            return base.IsValid() && 
                   (DataType?.ToUpper() is "REAL" or "INT" or "DINT");
        }
    }
}
