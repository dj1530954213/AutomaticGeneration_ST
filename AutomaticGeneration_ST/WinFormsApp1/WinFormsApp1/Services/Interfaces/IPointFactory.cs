using AutomaticGeneration_ST.Models;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// 点位工厂接口 - 负责根据不同数据源创建点位对象
    /// </summary>
    public interface IPointFactory
    {
        /// <summary>
        /// 从IO点表数据创建点位对象
        /// </summary>
        /// <param name="ioPointData">IO点表行数据</param>
        /// <returns>点位对象</returns>
        Models.Point CreateFromIoPoint(Dictionary<string, object> ioPointData);

        /// <summary>
        /// 从设备表数据创建点位对象
        /// </summary>
        /// <param name="devicePointData">设备表行数据</param>
        /// <param name="existingPoint">已存在的点位对象（从IO表创建的）</param>
        /// <returns>增强后的点位对象</returns>
        Models.Point CreateFromDevicePoint(Dictionary<string, object> devicePointData, Models.Point existingPoint = null);

        /// <summary>
        /// 批量创建点位对象
        /// </summary>
        /// <param name="pointDataList">点位数据列表</param>
        /// <returns>点位字典，Key为HmiTagName</returns>
        Dictionary<string, Models.Point> CreatePointsBatch(List<Dictionary<string, object>> pointDataList);
    }
}