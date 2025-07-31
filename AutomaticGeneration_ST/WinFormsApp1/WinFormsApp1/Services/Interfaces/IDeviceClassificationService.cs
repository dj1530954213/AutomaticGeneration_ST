using AutomaticGeneration_ST.Models;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 设备分类服务接口 - 负责解析设备分类表并构建设备对象
    /// </summary>
    public interface IDeviceClassificationService
    {
        /// <summary>
        /// 从设备分类表数据构建设备对象集合
        /// </summary>
        /// <param name="classificationData">设备分类表数据</param>
        /// <param name="allPoints">全部点位主列表</param>
        /// <returns>设备对象集合和已分配的点位名称集合</returns>
        (List<Device> devices, HashSet<string> assignedPointNames) BuildDevicesFromClassification(
            List<Dictionary<string, object>> classificationData, 
            Dictionary<string, Models.Point> allPoints);
    }
}