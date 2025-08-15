using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using WinFormsApp1;
using WinFormsApp1.Excel;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 设备分类服务实现类
    /// </summary>
    public class DeviceClassificationService : IDeviceClassificationService
    {
        private readonly IPointFactory _pointFactory;
        private readonly LogService _logger = LogService.Instance;

        public DeviceClassificationService(IPointFactory pointFactory)
        {
            _pointFactory = pointFactory ?? throw new ArgumentNullException(nameof(pointFactory));
        }

        public (List<Device> devices, HashSet<string> assignedPointNames) BuildDevicesFromClassification(
            List<Dictionary<string, object>> classificationData, 
            Dictionary<string, Models.Point> allPoints)
        {
            if (classificationData == null)
                throw new ArgumentNullException(nameof(classificationData));

            if (allPoints == null)
                throw new ArgumentNullException(nameof(allPoints));

            var devices = new List<Device>();
            var deviceMap = new Dictionary<string, Device>(); // 临时映射，避免重复创建设备
            var assignedPointNames = new HashSet<string>();
            var statistics = new ClassificationStatistics();

            _logger.LogInfo($"🏭 开始处理设备分类数据，共 {classificationData.Count} 行");

            foreach (var row in classificationData)
            {
                try
                {
                    var result = ProcessClassificationRow(row, deviceMap, allPoints, assignedPointNames);
                    UpdateStatistics(statistics, result);
                }
                catch (Exception ex)
                {
                    statistics.ErrorCount++;
                    _logger.LogError($"❌ 处理分类数据行时出错: {ex.Message}");
                }
            }

            // 从临时映射中提取最终的设备列表
            devices = deviceMap.Values.ToList();

            // 输出统计信息
            LogStatistics(statistics, devices.Count, assignedPointNames.Count);

            return (devices, assignedPointNames);
        }

        private ClassificationRowResult ProcessClassificationRow(
            Dictionary<string, object> row,
            Dictionary<string, Device> deviceMap,
            Dictionary<string, Models.Point> allPoints,
            HashSet<string> assignedPointNames)
        {
            var result = new ClassificationRowResult();

            // 提取关键字段
            var hmiTagName = DataExtractorHelper.GetValue<string>(row, "变量名称（HMI）");
            var pointType = DataExtractorHelper.GetValue<string>(row, "点位类型（硬点、软点、通讯点）");
            var deviceTag = DataExtractorHelper.GetValue<string>(row, "设备位号");
            var templateName = DataExtractorHelper.GetValue<string>(row, "模板名称");

            // 验证必需字段
            if (string.IsNullOrWhiteSpace(hmiTagName))
            {
                result.HasError = true;
                result.ErrorMessage = "变量名称（HMI）为空";
                return result;
            }

            // 查找对应的点位对象
            Models.Point point = null;
            if (allPoints.TryGetValue(hmiTagName, out var existingPoint))
            {
                point = existingPoint;
                result.PointFoundInMaster = true;
            }
            else
            {
                // 如果在主列表中找不到，尝试从当前行数据创建
                try
                {
                    point = _pointFactory.CreateFromDevicePoint(row);
                    allPoints.Add(hmiTagName, point); // 添加到主列表
                    result.PointCreatedFromDevice = true;
                }
                catch (Exception ex)
                {
                    result.HasError = true;
                    result.ErrorMessage = $"创建点位失败: {ex.Message}";
                    return result;
                }
            }

            // 设置或更新点位类型
            if (!string.IsNullOrWhiteSpace(pointType))
            {
                point.PointType = pointType;
            }

            // 处理设备关联
            if (!string.IsNullOrWhiteSpace(deviceTag))
            {
                // 获取或创建设备对象
                if (!deviceMap.TryGetValue(deviceTag, out var device))
                {
                    device = new Device(deviceTag, templateName ?? "");
                    deviceMap[deviceTag] = device;
                    result.DeviceCreated = true;
                    _logger.LogInfo($"   🆕 创建新设备: [{deviceTag}] 模板: {templateName ?? "未指定"}");
                }

                // 更新设备的模板名称（如果当前行有更新的信息）
                if (!string.IsNullOrWhiteSpace(templateName) && 
                    string.IsNullOrWhiteSpace(device.TemplateName))
                {
                    device.TemplateName = templateName;
                }

                // 将点位添加到设备
                device.AddPoint(point);
                assignedPointNames.Add(hmiTagName);
                result.PointAssignedToDevice = true;
            }
            else
            {
                // 没有设备信息的点位将作为独立点位处理
                result.PointStandalone = true;
            }

            return result;
        }

        private void UpdateStatistics(ClassificationStatistics stats, ClassificationRowResult result)
        {
            stats.TotalRowsProcessed++;

            if (result.HasError)
            {
                stats.ErrorCount++;
                return;
            }

            if (result.DeviceCreated)
                stats.DevicesCreated++;

            if (result.PointFoundInMaster)
                stats.PointsFoundInMaster++;

            if (result.PointCreatedFromDevice)
                stats.PointsCreatedFromDevice++;

            if (result.PointAssignedToDevice)
                stats.PointsAssignedToDevice++;

            if (result.PointStandalone)
                stats.StandalonePoints++;
        }

        private void LogStatistics(ClassificationStatistics stats, int finalDeviceCount, int finalAssignedPointCount)
        {
            _logger.LogSuccess($"🎯 设备分类处理完成:");
            _logger.LogInfo($"   📊 处理数据行: {stats.TotalRowsProcessed}");
            _logger.LogInfo($"   🏭 创建设备: {stats.DevicesCreated} (最终设备数: {finalDeviceCount})");
            _logger.LogInfo($"   🔍 主列表中找到点位: {stats.PointsFoundInMaster}");
            _logger.LogInfo($"   ➕ 从设备数据创建点位: {stats.PointsCreatedFromDevice}");
            _logger.LogInfo($"   🔗 分配到设备的点位: {stats.PointsAssignedToDevice} (最终分配数: {finalAssignedPointCount})");
            _logger.LogInfo($"   📍 独立点位: {stats.StandalonePoints}");
            if (stats.ErrorCount > 0)
            {
                _logger.LogWarning($"   ⚠️ 错误数: {stats.ErrorCount}");
            }
        }

        // 已重构：GetValue<T>方法已移至DataExtractorHelper工具类，消除DUP-007重复代码

        private class ClassificationStatistics
        {
            public int TotalRowsProcessed { get; set; }
            public int DevicesCreated { get; set; }
            public int PointsFoundInMaster { get; set; }
            public int PointsCreatedFromDevice { get; set; }
            public int PointsAssignedToDevice { get; set; }
            public int StandalonePoints { get; set; }
            public int ErrorCount { get; set; }
        }

        private class ClassificationRowResult
        {
            public bool HasError { get; set; }
            public string ErrorMessage { get; set; }
            public bool DeviceCreated { get; set; }
            public bool PointFoundInMaster { get; set; }
            public bool PointCreatedFromDevice { get; set; }
            public bool PointAssignedToDevice { get; set; }
            public bool PointStandalone { get; set; }
        }
    }
}