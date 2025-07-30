using System.Collections.Generic;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 代表一个逻辑设备，如阀门、调节阀等。
    /// 它聚合了设备自身的描述信息以及构成该设备的所有点位。
    /// </summary>
    public class Device
    {
        /// <summary>
        /// 设备的唯一位号，来自"设备分类表"中的"设备位号"列。
        /// </summary>
        public string DeviceTag { get; set; }

        /// <summary>
        /// 该设备应使用的ST代码模板名称，来自"设备分类表"中的"模板名称"列。
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// 包含属于此设备的所有点位对象的集合。
        /// Key: Point的HmiTagName (变量名称（HMI）)
        /// Value: Point对象本身
        /// </summary>
        public Dictionary<string, Point> Points { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deviceTag">设备位号</param>
        /// <param name="templateName">模板名称</param>
        public Device(string deviceTag, string templateName)
        {
            DeviceTag = deviceTag;
            TemplateName = templateName;
            Points = new Dictionary<string, Point>();
        }

        /// <summary>
        /// 向设备中添加一个点位。
        /// </summary>
        /// <param name="point">要添加的点位对象。</param>
        public void AddPoint(Point point)
        {
            if (point != null && !Points.ContainsKey(point.HmiTagName))
            {
                Points.Add(point.HmiTagName, point);
            }
        }
    }
}