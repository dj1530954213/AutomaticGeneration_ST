using NPOI.SS.UserModel;
using OfficeOpenXml;
using System;
using System.Collections.Generic;

namespace WinFormsApp1.Excel
{
    /// <summary>
    /// Excel单元格值处理工具类
    /// 统一处理NPOI和EPPlus的单元格值获取逻辑，消除重复代码
    /// </summary>
    /// <remarks>
    /// 作用: 消除DUP-006重复代码
    /// 重构前: ExcelReader.GetCellValue() 和 ExcelWorkbookParser.GetCellValue()存在80%重复
    /// 重构后: 统一的单元格值获取和类型转换逻辑
    /// 重构时间: 2025-08-15
    /// </remarks>
    public static class ExcelCellHelper
    {
        #region NPOI单元格值获取

        /// <summary>
        /// 获取NPOI单元格的值
        /// </summary>
        /// <param name="cell">NPOI单元格</param>
        /// <returns>单元格值</returns>
        public static object? GetCellValue(ICell? cell)
        {
            if (cell == null) return null;

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue.ToString()
                    : cell.NumericCellValue,
                CellType.Boolean => cell.BooleanCellValue,
                CellType.Formula => GetNpoiFormulaValue(cell),
                CellType.Blank => null,
                _ => cell.ToString()
            };
        }

        /// <summary>
        /// 获取NPOI公式单元格的计算值
        /// </summary>
        /// <param name="cell">公式单元格</param>
        /// <returns>计算后的值</returns>
        private static object? GetNpoiFormulaValue(ICell cell)
        {
            try
            {
                return cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue,
                    CellType.Numeric => cell.NumericCellValue,
                    CellType.Boolean => cell.BooleanCellValue,
                    _ => cell.ToString()
                };
            }
            catch
            {
                return cell.CellFormula;
            }
        }

        #endregion

        #region EPPlus单元格值获取

        /// <summary>
        /// 获取EPPlus单元格的值，支持泛型类型转换
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="cell">EPPlus单元格</param>
        /// <returns>转换后的值</returns>
        public static T GetCellValue<T>(ExcelRange cell)
        {
            if (cell?.Value == null)
                return default(T);

            try
            {
                var value = cell.Value;

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value.ToString();
                }
                else if (typeof(T) == typeof(object))
                {
                    return (T)value;
                }
                else if (typeof(T) == typeof(double?) || typeof(T) == typeof(double))
                {
                    if (double.TryParse(value.ToString(), out double doubleValue))
                        return (T)(object)doubleValue;
                    return default(T);
                }
                else if (typeof(T) == typeof(bool?) || typeof(T) == typeof(bool))
                {
                    return ConvertToBool<T>(value.ToString());
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

        #endregion

        #region 通用数据类型转换方法

        /// <summary>
        /// 从行数据中获取字符串值
        /// </summary>
        /// <param name="row">行数据</param>
        /// <param name="columnName">列名</param>
        /// <returns>字符串值</returns>
        public static string GetCellValueAsString(Dictionary<string, object> row, string columnName)
        {
            return row.TryGetValue(columnName, out var value) ? value?.ToString()?.Trim() ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// 从行数据中获取数值
        /// </summary>
        /// <param name="row">行数据</param>
        /// <param name="columnName">列名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数值</returns>
        public static double GetCellValueAsDouble(Dictionary<string, object> row, string columnName, double defaultValue = 0.0)
        {
            if (row.TryGetValue(columnName, out var value) &&
                double.TryParse(value?.ToString(), out var doubleValue))
            {
                return doubleValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 从行数据中获取布尔值
        /// </summary>
        /// <param name="row">行数据</param>
        /// <param name="columnName">列名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>布尔值</returns>
        public static bool GetCellValueAsBool(Dictionary<string, object> row, string columnName, bool defaultValue = false)
        {
            if (row.TryGetValue(columnName, out var value))
            {
                return ConvertStringToBool(value?.ToString(), defaultValue);
            }
            return defaultValue;
        }

        /// <summary>
        /// 提取行数据到字典
        /// </summary>
        /// <param name="row">NPOI行对象</param>
        /// <param name="columnNames">列名列表</param>
        /// <returns>行数据字典</returns>
        public static Dictionary<string, object> ExtractRowData(IRow row, List<string> columnNames)
        {
            var rowData = new Dictionary<string, object>();
            
            for (int cellIndex = 0; cellIndex < columnNames.Count; cellIndex++)
            {
                var cell = row.GetCell(cellIndex);
                var cellValue = GetCellValue(cell);
                rowData[columnNames[cellIndex]] = cellValue ?? string.Empty;
            }
            
            return rowData;
        }

        /// <summary>
        /// 检查行是否包含有效数据
        /// </summary>
        /// <param name="rowData">行数据</param>
        /// <returns>是否有有效数据</returns>
        public static bool HasValidData(Dictionary<string, object> rowData)
        {
            foreach (var value in rowData.Values)
            {
                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 布尔值转换（支持泛型）
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="stringValue">字符串值</param>
        /// <returns>转换后的布尔值</returns>
        private static T ConvertToBool<T>(string stringValue)
        {
            if (bool.TryParse(stringValue, out bool boolValue))
                return (T)(object)boolValue;

            var lowerValue = stringValue?.ToLower();
            if (lowerValue == "是" || lowerValue == "y" || lowerValue == "yes")
                return (T)(object)true;
            if (lowerValue == "否" || lowerValue == "n" || lowerValue == "no")
                return (T)(object)false;

            return default(T);
        }

        /// <summary>
        /// 字符串转布尔值
        /// </summary>
        /// <param name="stringValue">字符串值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>布尔值</returns>
        private static bool ConvertStringToBool(string stringValue, bool defaultValue)
        {
            var trimmedValue = stringValue?.Trim().ToUpper();
            if (trimmedValue == "TRUE" || trimmedValue == "1" || trimmedValue == "是")
                return true;
            if (trimmedValue == "FALSE" || trimmedValue == "0" || trimmedValue == "否")
                return false;
            return defaultValue;
        }

        #endregion
    }
}