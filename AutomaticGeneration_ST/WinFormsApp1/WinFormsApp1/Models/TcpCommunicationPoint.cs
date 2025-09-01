//NEED DELETE
//REASON: This model is part of the unused TCP communication architecture and is not integrated into the main UI workflow.

using System;

namespace WinFormsApp1.Models
{
    /// <summary>
    /// TCP通讯点位基类
    /// </summary>
    public abstract class TcpCommunicationPoint
    {
        /// <summary>
        /// 点位标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// HMI标签名称
        /// </summary>
        public string HmiTagName { get; set; } = "";

        /// <summary>
        /// 点位描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 数据类型 (REAL, INT, DINT, BOOL等)
        /// </summary>
        public string DataType { get; set; } = "";

        /// <summary>
        /// TCP通道地址
        /// </summary>
        public string Channel { get; set; } = "";

        /// <summary>
        /// TCP设备地址
        /// </summary>
        public string TcpAddress { get; set; } = "";

        /// <summary>
        /// 字节序 (用于数据转换)
        /// </summary>
        /// <summary>
        /// 字节顺序，如 "ABCD"、"DCBA" 等；保持原始字符串
        /// </summary>
        public string ByteOrder { get; set; } = "";

        /// <summary>
        /// 类型编号
        /// </summary>
        public int? TypeNumber { get; set; }

        /// <summary>
        /// 是否启用通讯
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 通讯状态
        /// </summary>
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        /// <summary>
        /// 验证点位数据的有效性
        /// </summary>
        public virtual bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(HmiTagName) 
                && !string.IsNullOrWhiteSpace(DataType)
                && !string.IsNullOrWhiteSpace(Channel);
        }

        /// <summary>
        /// 获取点位的完整标识
        /// </summary>
        public virtual string GetFullIdentifier()
        {
            return $"{HmiTagName}_{DataType}_{Channel}";
        }
    }
}