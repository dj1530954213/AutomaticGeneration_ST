using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 点位工厂实现类
    /// </summary>
    public class PointFactory : IPointFactory
    {
        public Models.Point CreateFromIoPoint(Dictionary<string, object> ioPointData)
        {
            if (ioPointData == null)
                throw new ArgumentNullException(nameof(ioPointData));

            var hmiTagName = GetValue<string>(ioPointData, "变量名称（HMI）");
            if (string.IsNullOrWhiteSpace(hmiTagName))
            {
                // 尝试其他可能的列名
                var possibleTagColumns = new[] { "变量名称", "HMI标签", "TagName", "标签名称", "变量名" };
                foreach (var col in possibleTagColumns)
                {
                    hmiTagName = GetValue<string>(ioPointData, col);
                    if (!string.IsNullOrWhiteSpace(hmiTagName))
                        break;
                }
                
                if (string.IsNullOrWhiteSpace(hmiTagName))
                {
                    // 记录调试信息
                    var availableColumns = string.Join(", ", ioPointData.Keys);
                    throw new ArgumentException($"变量名称不能为空。可用列: {availableColumns}");
                }
            }

            var point = new Models.Point(hmiTagName)
            {
                // 基础信息
                ModuleName = GetValue<string>(ioPointData, "模块名称"),
                ModuleType = GetValue<string>(ioPointData, "模块类型"),
                PowerSupplyType = GetValue<string>(ioPointData, "供电类型"),
                WireSystem = GetValue<string>(ioPointData, "线制"),
                ChannelNumber = GetValue<string>(ioPointData, "通道位号"),
                StationName = GetValue<string>(ioPointData, "场站名"),
                StationId = GetValue<string>(ioPointData, "场站编号"),
                Description = GetValue<string>(ioPointData, "变量描述"),
                DataType = GetValue<string>(ioPointData, "数据类型"),

                // 地址信息
                PlcAbsoluteAddress = GetValue<string>(ioPointData, "PLC绝对地址"),
                ScadaCommAddress = GetValue<string>(ioPointData, "上位机通讯地址"),

                // 配置信息
                StoreHistory = GetValue<bool?>(ioPointData, "是否历史存储"),
                PowerDownProtection = GetValue<bool?>(ioPointData, "是否掉电保护"),
                RangeLow = GetValue<double?>(ioPointData, "量程低"),
                RangeHigh = GetValue<double?>(ioPointData, "量程高"),
                Unit = GetValue<string>(ioPointData, "单位"),
                InstrumentType = GetValue<string>(ioPointData, "仪表类型"),

                // 报警设定值
                SLL_Value = GetValue<double?>(ioPointData, "SLL设定值"),
                SL_Value = GetValue<double?>(ioPointData, "SL设定值"),
                SH_Value = GetValue<double?>(ioPointData, "SH设定值"),
                SHH_Value = GetValue<double?>(ioPointData, "SHH设定值"),

                // 报警点位
                SLL_Point = GetValue<string>(ioPointData, "SLL设定单位"),
                SL_Point = GetValue<string>(ioPointData, "SL设定单位"),
                SH_Point = GetValue<string>(ioPointData, "SH设定单位"),
                SHH_Point = GetValue<string>(ioPointData, "SHH设定单位"),

                // 报警地址
                SLL_PlcAddress = GetValue<string>(ioPointData, "SLL设定单位_PLC地址"),
                SL_PlcAddress = GetValue<string>(ioPointData, "SL设定单位_PLC地址"),
                SH_PlcAddress = GetValue<string>(ioPointData, "SH设定单位_PLC地址"),
                SHH_PlcAddress = GetValue<string>(ioPointData, "SHH设定单位_PLC地址"),

                // 维护相关（如果存在）
                // 这些字段在IO点表中可能存在，根据实际情况调整
            };

            return point;
        }

        public Models.Point CreateFromDevicePoint(Dictionary<string, object> devicePointData, Models.Point existingPoint = null)
        {
            if (devicePointData == null)
                throw new ArgumentNullException(nameof(devicePointData));

            var hmiTagName = GetValue<string>(devicePointData, "变量名称（HMI）");
            if (string.IsNullOrWhiteSpace(hmiTagName))
                throw new ArgumentException("HMI标签名称不能为空");

            Models.Point point;
            if (existingPoint != null)
            {
                // 基于现有点位对象进行增强
                point = existingPoint;
            }
            else
            {
                // 创建新的点位对象
                point = new Models.Point(hmiTagName);
            }

            // 从设备表数据增强点位信息
            var description = GetValue<string>(devicePointData, "变量描述");
            if (!string.IsNullOrWhiteSpace(description))
            {
                point.Description = description;
            }

            var dataType = GetValue<string>(devicePointData, "数据类型");
            if (!string.IsNullOrWhiteSpace(dataType))
            {
                point.DataType = dataType;
            }

            // 设置点位类型（从设备分类表推断）
            var pointType = GetValue<string>(devicePointData, "点位类型（硬点、软点、通讯点）");
            if (!string.IsNullOrWhiteSpace(pointType))
            {
                point.PointType = pointType;
            }

            // 设置设备相关信息
            var deviceTag = GetValue<string>(devicePointData, "设备位号");
            if (!string.IsNullOrWhiteSpace(deviceTag))
            {
                point.DevicePointName = deviceTag;
            }

            // 根据设备表的其他可用字段进行增强
            // 例如PLC地址、MODBUS地址等
            var plcAddress = GetValue<string>(devicePointData, "PLC地址");
            if (!string.IsNullOrWhiteSpace(plcAddress))
            {
                point.PlcAbsoluteAddress = plcAddress;
            }

            var modbusAddress = GetValue<string>(devicePointData, "MODBUS地址");
            if (!string.IsNullOrWhiteSpace(modbusAddress))
            {
                point.ScadaCommAddress = modbusAddress;
            }

            return point;
        }

        public Dictionary<string, Models.Point> CreatePointsBatch(List<Dictionary<string, object>> pointDataList)
        {
            if (pointDataList == null)
                throw new ArgumentNullException(nameof(pointDataList));

            var result = new Dictionary<string, Models.Point>();
            var errors = new List<string>();

            foreach (var pointData in pointDataList)
            {
                try
                {
                    var point = CreateFromIoPoint(pointData);
                    if (!result.ContainsKey(point.HmiTagName))
                    {
                        result.Add(point.HmiTagName, point);
                    }
                    else
                    {
                        errors.Add($"重复的点位名称: {point.HmiTagName}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"创建点位失败: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                Console.WriteLine($"[WARNING] 点位创建过程中遇到 {errors.Count} 个问题:");
                errors.ForEach(error => Console.WriteLine($"  - {error}"));
            }

            return result;
        }

        private T GetValue<T>(Dictionary<string, object> data, string key)
        {
            if (data == null || string.IsNullOrWhiteSpace(key))
                return default(T);

            // 尝试通过可能的列名映射找到值
            var actualKey = FindActualColumnName(data, key);
            if (actualKey == null || !data.TryGetValue(actualKey, out var value) || value == null)
                return default(T);

            try
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value.ToString();
                }
                else if (typeof(T) == typeof(double?) || typeof(T) == typeof(double))
                {
                    if (double.TryParse(value.ToString(), out double doubleValue))
                        return (T)(object)doubleValue;
                    return default(T);
                }
                else if (typeof(T) == typeof(bool?) || typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(value.ToString(), out bool boolValue))
                        return (T)(object)boolValue;

                    var stringValue = value.ToString()?.ToLower();
                    if (stringValue == "是" || stringValue == "y" || stringValue == "yes")
                        return (T)(object)true;
                    if (stringValue == "否" || stringValue == "n" || stringValue == "no")
                        return (T)(object)false;

                    return default(T);
                }
                else
                {
                    return (T)value;
                }
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 根据标准列名查找实际的Excel列名
        /// </summary>
        /// <param name="data">数据行</param>
        /// <param name="standardKey">标准列名</param>
        /// <returns>实际的列名，如果未找到返回null</returns>
        private string FindActualColumnName(Dictionary<string, object> data, string standardKey)
        {
            // 首先尝试精确匹配
            if (data.ContainsKey(standardKey))
                return standardKey;

            // 定义列名映射表
            var columnMappings = new Dictionary<string, string[]>
            {
                ["变量名称（HMI）"] = new[] { "变量名称（HMI）", "变量名称", "HMI标签", "TagName", "标签名称", "变量名" },
                ["变量描述"] = new[] { "变量描述", "描述", "Description", "说明", "备注" },
                ["数据类型"] = new[] { "数据类型", "类型", "DataType", "Type" },
                ["模块类型"] = new[] { "模块类型", "IO类型", "IOType", "模块", "类型" },
                ["模块名称"] = new[] { "模块名称", "模块", "Module", "模块型号" },
                ["供电类型"] = new[] { "供电类型", "供电", "PowerType", "电源类型" },
                ["线制"] = new[] { "线制", "接线", "WireSystem", "线路" },
                ["通道位号"] = new[] { "通道位号", "通道", "Channel", "位号" },
                ["场站名"] = new[] { "场站名", "场站", "Station", "站点" },
                ["场站编号"] = new[] { "场站编号", "编号", "StationId", "场站ID" },
                ["PLC绝对地址"] = new[] { "PLC绝对地址", "PLC地址", "地址", "绝对地址", "Address" },
                ["上位机通讯地址"] = new[] { "上位机通讯地址", "通讯地址", "MODBUS地址", "通信地址", "CommAddress" },
                ["是否历史存储"] = new[] { "是否历史存储", "历史存储", "存储", "History", "历史" },
                ["是否掉电保护"] = new[] { "是否掉电保护", "掉电保护", "保护", "PowerProtection", "掉电" },
                ["量程低"] = new[] { "量程低", "下限", "MinValue", "最小值", "低限" },
                ["量程高"] = new[] { "量程高", "上限", "MaxValue", "最大值", "高限" },
                ["单位"] = new[] { "单位", "Unit", "工程单位" },
                ["仪表类型"] = new[] { "仪表类型", "仪表", "InstrumentType", "仪器类型" },
                ["SLL设定值"] = new[] { "SLL设定值", "SLL", "SLL_Value" },
                ["SL设定值"] = new[] { "SL设定值", "SL", "SL_Value" },
                ["SH设定值"] = new[] { "SH设定值", "SH", "SH_Value" },
                ["SHH设定值"] = new[] { "SHH设定值", "SHH", "SHH_Value" },
                ["点位类型（硬点、软点、通讯点）"] = new[] { "点位类型（硬点、软点、通讯点）", "点位类型", "点类型", "PointType" },
                ["设备位号"] = new[] { "设备位号", "设备标签", "DeviceTag", "设备名称", "设备" },
                ["设备类型"] = new[] { "设备类型", "DeviceType", "Type", "类型" },
                ["模板名称"] = new[] { "模板名称", "模板", "Template", "模版" }
            };

            // 查找映射
            if (columnMappings.TryGetValue(standardKey, out var possibleNames))
            {
                foreach (var possibleName in possibleNames)
                {
                    if (data.ContainsKey(possibleName))
                        return possibleName;
                }
            }

            // 如果没有找到精确匹配，尝试部分匹配（忽略大小写和空格）
            var normalizedKey = standardKey.Replace("（", "").Replace("）", "").Replace(" ", "").ToLower();
            foreach (var key in data.Keys)
            {
                var normalizedExistingKey = key.Replace("（", "").Replace("）", "").Replace(" ", "").ToLower();
                if (normalizedExistingKey.Contains(normalizedKey) || normalizedKey.Contains(normalizedExistingKey))
                {
                    return key;
                }
            }

            return null;
        }
    }
}