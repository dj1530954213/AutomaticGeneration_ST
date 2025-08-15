using System;
using System.Collections.Generic;
using System.Linq;

namespace WinFormsApp1.Excel
{
    /// <summary>
    /// 数据提取辅助工具类
    /// 统一处理字典数据的提取、类型转换和列名映射，消除重复代码
    /// </summary>
    /// <remarks>
    /// 作用: 消除DUP-007最后的重复代码
    /// 重构前: DataProcessingOrchestrator、DeviceClassificationService、PointFactory都有相似的GetValue<T>方法
    /// 重构后: 统一的数据提取和类型转换逻辑，支持智能列名映射
    /// 重构时间: 2025-08-15
    /// </remarks>
    public static class DataExtractorHelper
    {
        #region 核心数据提取方法

        /// <summary>
        /// 从字典中获取指定类型的值（基础版本）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">数据字典</param>
        /// <param name="key">键名</param>
        /// <returns>转换后的值，失败时返回默认值</returns>
        public static T GetValue<T>(Dictionary<string, object> data, string key)
        {
            if (data == null || string.IsNullOrWhiteSpace(key))
                return default(T);

            if (!data.TryGetValue(key, out var value) || value == null)
                return default(T);

            return ConvertValue<T>(value);
        }

        /// <summary>
        /// 从字典中获取指定类型的值（增强版本，支持智能列名映射）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">数据字典</param>
        /// <param name="key">标准键名</param>
        /// <returns>转换后的值，失败时返回默认值</returns>
        public static T GetValueWithMapping<T>(Dictionary<string, object> data, string key)
        {
            if (data == null || string.IsNullOrWhiteSpace(key))
                return default(T);

            // 使用智能列名映射查找实际的键名
            var actualKey = FindActualColumnName(data, key);
            if (actualKey == null || !data.TryGetValue(actualKey, out var value) || value == null)
                return default(T);

            return ConvertValue<T>(value);
        }

        /// <summary>
        /// 批量获取多个字段的值
        /// </summary>
        /// <param name="data">数据字典</param>
        /// <param name="keys">键名列表</param>
        /// <returns>键值对结果</returns>
        public static Dictionary<string, object> GetMultipleValues(Dictionary<string, object> data, params string[] keys)
        {
            var result = new Dictionary<string, object>();
            
            if (data == null || keys == null)
                return result;

            foreach (var key in keys)
            {
                result[key] = GetValue<object>(data, key);
            }
            
            return result;
        }

        #endregion

        #region 类型转换方法

        /// <summary>
        /// 将对象转换为指定类型
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="value">源值</param>
        /// <returns>转换后的值</returns>
        private static T ConvertValue<T>(object value)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)(value?.ToString()?.Trim() ?? string.Empty);
                }
                else if (typeof(T) == typeof(double?) || typeof(T) == typeof(double))
                {
                    if (double.TryParse(value?.ToString(), out double doubleValue))
                        return (T)(object)doubleValue;
                    return default(T);
                }
                else if (typeof(T) == typeof(int?) || typeof(T) == typeof(int))
                {
                    if (int.TryParse(value?.ToString(), out int intValue))
                        return (T)(object)intValue;
                    return default(T);
                }
                else if (typeof(T) == typeof(bool?) || typeof(T) == typeof(bool))
                {
                    return ConvertToBool<T>(value?.ToString());
                }
                else if (typeof(T) == typeof(object))
                {
                    return (T)value;
                }
                else
                {
                    // 尝试直接转换
                    return (T)value;
                }
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 字符串转布尔值（支持中文）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="stringValue">字符串值</param>
        /// <returns>布尔值</returns>
        private static T ConvertToBool<T>(string stringValue)
        {
            if (bool.TryParse(stringValue, out bool boolValue))
                return (T)(object)boolValue;

            var lowerValue = stringValue?.Trim().ToLower();
            if (lowerValue == "是" || lowerValue == "y" || lowerValue == "yes" || lowerValue == "true" || lowerValue == "1")
                return (T)(object)true;
            if (lowerValue == "否" || lowerValue == "n" || lowerValue == "no" || lowerValue == "false" || lowerValue == "0")
                return (T)(object)false;

            return default(T);
        }

        #endregion

        #region 智能列名映射

        /// <summary>
        /// 根据标准列名查找实际的Excel列名
        /// </summary>
        /// <param name="data">数据行</param>
        /// <param name="standardKey">标准列名</param>
        /// <returns>实际的列名，如果未找到返回null</returns>
        public static string FindActualColumnName(Dictionary<string, object> data, string standardKey)
        {
            if (data == null || string.IsNullOrWhiteSpace(standardKey))
                return null;

            // 首先尝试精确匹配
            if (data.ContainsKey(standardKey))
                return standardKey;

            // 定义列名映射表（从PointFactory复制并优化）
            var columnMappings = GetStandardColumnMappings();

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
            var normalizedKey = NormalizeKey(standardKey);
            foreach (var key in data.Keys)
            {
                var normalizedExistingKey = NormalizeKey(key);
                if (normalizedExistingKey.Contains(normalizedKey) || normalizedKey.Contains(normalizedExistingKey))
                {
                    return key;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取标准列名映射表
        /// </summary>
        /// <returns>列名映射字典</returns>
        private static Dictionary<string, string[]> GetStandardColumnMappings()
        {
            return new Dictionary<string, string[]>
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
                ["量程低"] = new[] { "量程低", "下限", "MinValue", "最小值", "低限", "量程低限" },
                ["量程高"] = new[] { "量程高", "上限", "MaxValue", "最大值", "高限", "量程高限" },
                ["单位"] = new[] { "单位", "Unit", "工程单位" },
                ["仪表类型"] = new[] { "仪表类型", "仪表", "InstrumentType", "仪器类型" },
                ["SLL设定值"] = new[] { "SLL设定值", "SLL", "SLL_Value", "低低限" },
                ["SL设定值"] = new[] { "SL设定值", "SL", "SL_Value", "低限" },
                ["SH设定值"] = new[] { "SH设定值", "SH", "SH_Value", "高限" },
                ["SHH设定值"] = new[] { "SHH设定值", "SHH", "SHH_Value", "高高限" },
                ["点位类型（硬点、软点、通讯点）"] = new[] { "点位类型（硬点、软点、通讯点）", "点位类型", "点类型", "PointType" },
                ["设备位号"] = new[] { "设备位号", "设备标签", "DeviceTag", "设备名称", "设备" },
                ["设备类型"] = new[] { "设备类型", "DeviceType", "Type", "类型" },
                ["模板名称"] = new[] { "模板名称", "模板", "Template", "模版" },
                ["输入通道"] = new[] { "输入通道", "输入", "InputChannel", "通道" },
                ["输出通道"] = new[] { "输出通道", "输出", "OutputChannel", "通道" }
            };
        }

        /// <summary>
        /// 标准化键名（用于模糊匹配）
        /// </summary>
        /// <param name="key">原始键名</param>
        /// <returns>标准化后的键名</returns>
        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            return key.Replace("（", "").Replace("）", "").Replace("(", "").Replace(")", "")
                     .Replace(" ", "").Replace("\t", "").ToLower().Trim();
        }

        #endregion

        #region 数据验证和清理

        /// <summary>
        /// 验证字典是否包含必要的字段
        /// </summary>
        /// <param name="data">数据字典</param>
        /// <param name="requiredFields">必要字段列表</param>
        /// <returns>验证结果，包含缺失的字段</returns>
        public static (bool IsValid, string[] MissingFields) ValidateRequiredFields(
            Dictionary<string, object> data, params string[] requiredFields)
        {
            if (data == null || requiredFields == null)
                return (false, requiredFields ?? new string[0]);

            var missingFields = new List<string>();

            foreach (var field in requiredFields)
            {
                var actualKey = FindActualColumnName(data, field);
                if (actualKey == null || !data.TryGetValue(actualKey, out var value) || 
                    string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    missingFields.Add(field);
                }
            }

            return (missingFields.Count == 0, missingFields.ToArray());
        }

        /// <summary>
        /// 清理字典数据，移除空白值和无效数据
        /// </summary>
        /// <param name="data">原始数据字典</param>
        /// <returns>清理后的数据字典</returns>
        public static Dictionary<string, object> CleanDataDictionary(Dictionary<string, object> data)
        {
            if (data == null)
                return new Dictionary<string, object>();

            var cleanedData = new Dictionary<string, object>();

            foreach (var kvp in data)
            {
                var cleanedValue = CleanValue(kvp.Value);
                if (cleanedValue != null && !string.IsNullOrWhiteSpace(cleanedValue.ToString()))
                {
                    cleanedData[kvp.Key.Trim()] = cleanedValue;
                }
            }

            return cleanedData;
        }

        /// <summary>
        /// 清理单个值
        /// </summary>
        /// <param name="value">原始值</param>
        /// <returns>清理后的值</returns>
        private static object CleanValue(object value)
        {
            if (value == null)
                return null;

            var stringValue = value.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            return stringValue.Trim();
        }

        #endregion
    }
}