using AutomaticGeneration_ST.Models;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 数据处理编排器接口 - 协调整个数据处理流程
    /// </summary>
    public interface IDataProcessingOrchestrator
    {
        /// <summary>
        /// 执行完整的数据处理流程
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <returns>处理结果上下文</returns>
        ProcessingResult ProcessData(string excelFilePath);
    }

    /// <summary>
    /// 数据处理结果上下文
    /// </summary>
    public class ProcessingResult
    {
        /// <summary>
        /// 设备集合
        /// </summary>
        public List<Device> Devices { get; set; } = new List<Device>();

        /// <summary>
        /// 独立点位（不属于任何设备的点位）
        /// </summary>
        public List<Models.Point> StandalonePoints { get; set; } = new List<Models.Point>();

        /// <summary>
        /// 硬点集合（需要进行IO映射的点位）
        /// </summary>
        public List<Models.Point> HardwarePoints { get; set; } = new List<Models.Point>();

        /// <summary>
        /// 软点集合（设备模板中使用的点位）
        /// </summary>
        public List<Models.Point> SoftwarePoints { get; set; } = new List<Models.Point>();

        /// <summary>
        /// 通讯点集合（从通讯设备采集的点位）
        /// </summary>
        public List<Models.Point> CommunicationPoints { get; set; } = new List<Models.Point>();

        /// <summary>
        /// 全部点位主列表
        /// </summary>
        public Dictionary<string, Models.Point> AllPointsMaster { get; set; } = new Dictionary<string, Models.Point>();

        /// <summary>
        /// 处理统计信息
        /// </summary>
        public ProcessingStatistics Statistics { get; set; } = new ProcessingStatistics();
    }

    /// <summary>
    /// 处理统计信息
    /// </summary>
    public class ProcessingStatistics
    {
        public int TotalPointsProcessed { get; set; }
        public int DevicesCreated { get; set; }
        public int StandalonePoints { get; set; }
        public int HardwarePoints { get; set; }
        public int SoftwarePoints { get; set; }
        public int CommunicationPoints { get; set; }
        public int ErrorsEncountered { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }
}