using AutomaticGeneration_ST.Models;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 定义数据加载服务的接口
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// 从指定的单个Excel文件路径加载所有数据，并将其整合。
        /// </summary>
        /// <param name="excelFilePath">单个.xlsx文件的完整物理路径。</param>
        /// <returns>一个包含所有设备、独立点位和全量点位主列表的数据上下文对象。</returns>
        DataContext LoadData(string excelFilePath);
    }

    /// <summary>
    /// 一个数据传输对象（DTO），用于封装从IDataService返回的所有已处理数据。
    /// </summary>
    public class DataContext
    {
        public List<Device> Devices { get; set; } = new List<Device>();
        public List<Models.Point> StandalonePoints { get; set; } = new List<Models.Point>();
        public Dictionary<string, Models.Point> AllPointsMasterList { get; set; } = new Dictionary<string, Models.Point>();
    }
}