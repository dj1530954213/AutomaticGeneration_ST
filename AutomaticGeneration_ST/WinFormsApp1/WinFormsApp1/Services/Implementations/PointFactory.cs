//NEED DELETE
// REASON: This service belongs to a new architecture that is not integrated into the main UI and is currently unused.

using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using WinFormsApp1.Excel;

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

            var hmiTagName = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "变量名称（HMI）");
            if (string.IsNullOrWhiteSpace(hmiTagName))
            {
                // 尝试其他可能的列名
                var possibleTagColumns = new[] { "变量名称", "HMI标签", "TagName", "标签名称", "变量名" };
                foreach (var col in possibleTagColumns)
                {
                    hmiTagName = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, col);
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
                ModuleName = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "模块名称"),
                ModuleType = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "模块类型"),
                PowerSupplyType = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "供电类型"),
                WireSystem = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "线制"),
                ChannelNumber = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "通道位号"),
                StationName = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "场站名"),
                StationId = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "场站编号"),
                Description = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "变量描述"),
                DataType = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "数据类型"),

                // 地址信息
                PlcAbsoluteAddress = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "PLC绝对地址"),
                ScadaCommAddress = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "上位机通讯地址"),

                // 配置信息
                StoreHistory = DataExtractorHelper.GetValueWithMapping<bool?>(ioPointData, "是否历史存储"),
                PowerDownProtection = DataExtractorHelper.GetValueWithMapping<bool?>(ioPointData, "是否掉电保护"),
                RangeLow = DataExtractorHelper.GetValueWithMapping<double?>(ioPointData, "量程低"),
                RangeHigh = DataExtractorHelper.GetValueWithMapping<double?>(ioPointData, "量程高"),
                Unit = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "单位"),
                InstrumentType = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "仪表类型"),

                // 报警设定值
                SLL_Value = DataExtractorHelper.GetValueWithMapping<double?>(ioPointData, "SLL设定值"),
                SL_Value = DataExtractorHelper.GetValueWithMapping<double?>(ioPointData, "SL设定值"),
                SH_Value = DataExtractorHelper.GetValueWithMapping<double?>(ioPointData, "SH设定值"),
                SHH_Value = DataExtractorHelper.GetValueWithMapping<double?>(ioPointData, "SHH设定值"),

                // 报警点位
                SLL_Point = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SLL设定单位"),
                SL_Point = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SL设定单位"),
                SH_Point = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SH设定单位"),
                SHH_Point = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SHH设定单位"),

                // 报警地址
                SLL_PlcAddress = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SLL设定单位_PLC地址"),
                SL_PlcAddress = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SL设定单位_PLC地址"),
                SH_PlcAddress = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SH设定单位_PLC地址"),
                SHH_PlcAddress = DataExtractorHelper.GetValueWithMapping<string>(ioPointData, "SHH设定单位_PLC地址"),

                // 维护相关（如果存在）
                // 这些字段在IO点表中可能存在，根据实际情况调整
            };

            return point;
        }

        public Models.Point CreateFromDevicePoint(Dictionary<string, object> devicePointData, Models.Point existingPoint = null)
        {
            if (devicePointData == null)
                throw new ArgumentNullException(nameof(devicePointData));

            var hmiTagName = DataExtractorHelper.GetValueWithMapping<string>(devicePointData, "变量名称（HMI）");
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
            var description = DataExtractorHelper.GetValueWithMapping<string>(devicePointData, "变量描述");
            if (!string.IsNullOrWhiteSpace(description))
            {
                point.Description = description;
            }

            var dataType = DataExtractorHelper.GetValueWithMapping<string>(devicePointData, "数据类型");
            if (!string.IsNullOrWhiteSpace(dataType))
            {
                point.DataType = dataType;
            }

            // 设置点位类型（从设备分类表推断）
            var pointType = DataExtractorHelper.GetValueWithMapping<string>(devicePointData, "点位类型（硬点、软点、通讯点）");
            if (!string.IsNullOrWhiteSpace(pointType))
            {
                point.PointType = pointType;
            }

            // 设置设备相关信息
            var deviceTag = DataExtractorHelper.GetValueWithMapping<string>(devicePointData, "设备位号");
            if (!string.IsNullOrWhiteSpace(deviceTag))
            {
                point.DevicePointName = deviceTag;
            }

            // 根据设备表的其他可用字段进行增强
            // 例如PLC地址、MODBUS地址等
            var plcAddress = DataExtractorHelper.GetValueWithMapping<string>(devicePointData, "PLC地址");
            if (!string.IsNullOrWhiteSpace(plcAddress))
            {
                point.PlcAbsoluteAddress = plcAddress;
            }

            var modbusAddress = DataExtractorHelper.GetValueWithMapping<string>(devicePointData, "MODBUS地址");
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

        // 已重构：DataExtractorHelper.GetValueWithMapping<T>方法已移至DataExtractorHelper工具类，消除DUP-007重复代码

        // 已重构：FindActualColumnName方法已移至DataExtractorHelper工具类，消除DUP-007重复代码
    }
}