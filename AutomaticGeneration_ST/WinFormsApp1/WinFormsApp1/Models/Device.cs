using System.Collections.Generic;
using System.Linq;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 代表一个逻辑设备，如阀门、调节阀等。
    /// 它聚合了设备自身的描述信息以及构成该设备的所有点位。
    /// 支持两种不同类型的点位：IO点位（来自IO表）和设备点位（来自设备表）
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
        /// IO点位集合（来自IO表，硬件映射点位）
        /// Key: 变量名称（HMI名）
        /// Value: 包含所有字段的字典（序号,模块名称,模块类型,信号类型等完整的IO表字段）
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> IoPoints { get; private set; }

        /// <summary>
        /// 设备点位集合（来自设备表，软点位）
        /// Key: 变量名称
        /// Value: 包含所有字段的字典（站点名,变量描述,数据类型,设定值,PLC地址等设备表字段）
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> DevicePoints { get; private set; }

        /// <summary>
        /// 别名索引：别名 -> 变量名称（HMI）
        /// 说明：HMI 名允许为空字符串，用于模板占位符填充空字符串的场景
        /// </summary>
        public Dictionary<string, string> AliasIndex { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// 兼容性保留：旧的Points属性，现在合并了IoPoints和DevicePoints中的Point对象
        /// </summary>
        [System.Obsolete("建议使用IoPoints和DevicePoints分别访问不同类型的点位数据")]
        public Dictionary<string, Point> Points 
        { 
            get 
            {
                var points = new Dictionary<string, Point>();
                
                // 将IO点位转换为Point对象
                foreach (var ioPoint in IoPoints)
                {
                    var point = ConvertToPoint(ioPoint.Key, ioPoint.Value, isIoPoint: true);
                    if (point != null)
                    {
                        points[ioPoint.Key] = point;
                    }
                }
                
                // 将设备点位转换为Point对象
                foreach (var devicePoint in DevicePoints)
                {
                    var point = ConvertToPoint(devicePoint.Key, devicePoint.Value, isIoPoint: false);
                    if (point != null)
                    {
                        points[devicePoint.Key] = point;
                    }
                }
                
                return points;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="deviceTag">设备位号</param>
        /// <param name="templateName">模板名称</param>
        public Device(string deviceTag, string templateName)
        {
            DeviceTag = deviceTag;
            TemplateName = templateName;
            IoPoints = new Dictionary<string, Dictionary<string, object>>();
            DevicePoints = new Dictionary<string, Dictionary<string, object>>();
        }

        /// <summary>
        /// 添加别名映射（别名 -> 变量名称（HMI））。HMI 可为空字符串。
        /// 同一设备内若出现相同别名但指向不同 HMI，则抛出异常，避免歧义。
        /// </summary>
        /// <param name="alias">别名（来自设备分类表“别名”列）</param>
        /// <param name="hmi">变量名称（HMI），允许为空字符串</param>
        public void AddAlias(string alias, string hmi)
        {
            if (string.IsNullOrWhiteSpace(alias)) return;

            var key = NormalizeAlias(alias);
            var value = hmi?.Trim() ?? string.Empty;

            if (AliasIndex.TryGetValue(key, out var existing))
            {
                if (!string.Equals(existing ?? string.Empty, value, System.StringComparison.Ordinal))
                {
                    throw new System.InvalidOperationException($"设备[{DeviceTag}] 存在重复别名 '{alias}' 指向不同HMI: '{existing}' vs '{value}'");
                }
                // 相同映射则忽略
                return;
            }

            AliasIndex[key] = value;
        }

        /// <summary>
        /// 根据别名获取HMI变量名（允许为空字符串）。
        /// </summary>
        public bool TryGetHmiByAlias(string alias, out string hmi)
        {
            hmi = string.Empty;
            if (string.IsNullOrWhiteSpace(alias)) return false;
            var key = NormalizeAlias(alias);
            if (AliasIndex.TryGetValue(key, out var value))
            {
                hmi = value ?? string.Empty;
                return true;
            }
            return false;
        }

        private static string NormalizeAlias(string s)
        {
            return s?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// 添加IO点位（来自IO表）
        /// </summary>
        /// <param name="variableName">变量名称</param>
        /// <param name="pointData">包含所有IO表字段的字典</param>
        public void AddIoPoint(string variableName, Dictionary<string, object> pointData)
        {
            if (!string.IsNullOrWhiteSpace(variableName) && pointData != null && !IoPoints.ContainsKey(variableName))
            {
                IoPoints[variableName] = new Dictionary<string, object>(pointData);
            }
        }

        /// <summary>
        /// 添加设备点位（来自设备表）
        /// </summary>
        /// <param name="variableName">变量名称</param>
        /// <param name="pointData">包含所有设备表字段的字典</param>
        public void AddDevicePoint(string variableName, Dictionary<string, object> pointData)
        {
            if (!string.IsNullOrWhiteSpace(variableName) && pointData != null && !DevicePoints.ContainsKey(variableName))
            {
                DevicePoints[variableName] = new Dictionary<string, object>(pointData);
            }
        }

        /// <summary>
        /// 向设备中添加一个Point对象（兼容性方法）
        /// </summary>
        /// <param name="point">要添加的点位对象</param>
        [System.Obsolete("建议使用AddIoPoint或AddDevicePoint方法")]
        public void AddPoint(Point point)
        {
            if (point != null && !string.IsNullOrWhiteSpace(point.HmiTagName))
            {
                // 将Point对象转换为字典格式并添加到IoPoints（默认假设是IO点位）
                var pointData = ConvertPointToDictionary(point);
                AddIoPoint(point.HmiTagName, pointData);
            }
        }

        /// <summary>
        /// 获取设备的所有点位变量名（包括IO点位和设备点位）
        /// </summary>
        /// <returns>所有点位的变量名列表</returns>
        public List<string> GetAllVariableNames()
        {
            var allNames = new List<string>();
            allNames.AddRange(IoPoints.Keys);
            allNames.AddRange(DevicePoints.Keys);
            return allNames;
        }

        /// <summary>
        /// 根据变量名查找点位数据（先在IO点位中查找，再在设备点位中查找）
        /// </summary>
        /// <param name="variableName">变量名称</param>
        /// <returns>点位数据字典，如果未找到则返回null</returns>
        public Dictionary<string, object> FindPointData(string variableName)
        {
            if (IoPoints.ContainsKey(variableName))
            {
                return IoPoints[variableName];
            }
            
            if (DevicePoints.ContainsKey(variableName))
            {
                return DevicePoints[variableName];
            }
            
            return null;
        }

        /// <summary>
        /// 将字典数据转换为Point对象（用于兼容性）
        /// </summary>
        private Point ConvertToPoint(string variableName, Dictionary<string, object> pointData, bool isIoPoint)
        {
            try
            {
                var point = new Point(variableName);
                
                if (isIoPoint)
                {
                    // IO点位字段映射
                    if (pointData.ContainsKey("描述信息")) point.Description = pointData["描述信息"]?.ToString();
                    if (pointData.ContainsKey("数据类型")) point.DataType = pointData["数据类型"]?.ToString();
                    if (pointData.ContainsKey("模块类型")) point.ModuleType = pointData["模块类型"]?.ToString();
                    if (pointData.ContainsKey("信号类型")) point.InstrumentType = pointData["信号类型"]?.ToString();
                }
                else
                {
                    // 设备点位字段映射
                    if (pointData.ContainsKey("变量描述")) point.Description = pointData["变量描述"]?.ToString();
                    if (pointData.ContainsKey("数据类型")) point.DataType = pointData["数据类型"]?.ToString();
                }
                
                return point;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 将Point对象转换为字典（用于兼容性）
        /// </summary>
        private Dictionary<string, object> ConvertPointToDictionary(Point point)
        {
            var dict = new Dictionary<string, object>
            {
                ["变量名称（HMI名）"] = point.HmiTagName,
                ["描述信息"] = point.Description ?? "",
                ["数据类型"] = point.DataType ?? "",
                ["模块类型"] = point.ModuleType ?? "",
                ["信号类型"] = point.InstrumentType ?? ""
            };
            
            return dict;
        }
    }
}