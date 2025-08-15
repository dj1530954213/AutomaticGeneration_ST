using AutomaticGeneration_ST.Services.Interfaces;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutomaticGeneration_ST.Services.Implementations
{
    //TODO: 重复代码(ID:DUP-003) - [Excel解析：工作表解析逻辑分散在多个类中] 
    //TODO: 建议重构为统一的WorksheetParsingEngine，提取公共解析流程 优先级:中等
    /// <summary>
    /// Excel工作簿解析器实现类
    /// </summary>
    /// <remarks>
    /// 状态: @duplicate
    /// 优先级: P2 (中风险)
    /// 重复度: 70%
    /// 重复位置: ExcelReader.cs
    /// 建议: 重构为统一的WorksheetParsingEngine，提取公共的单元格读取和数据转换逻辑
    /// 风险级别: 中风险 - 需要分析调用关系后重构
    /// 分析时间: 2025-08-15
    /// 重复方法: GetCellValue, ParseHeaderRow, ProcessDataRow, ReadWorksheet
    /// </remarks>
    public class ExcelWorkbookParser : IExcelWorkbookParser
    {
        static ExcelWorkbookParser()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<Dictionary<string, object>> ParseWorksheet(string filePath, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentException("工作表名称不能为空", nameof(sheetName));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel文件不存在: {filePath}");

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    throw new ArgumentException($"工作表 '{sheetName}' 不存在");
                }

                return ParseWorksheetData(worksheet);
            }
        }

        public List<string> GetWorksheetNames(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel文件不存在: {filePath}");

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                return package.Workbook.Worksheets.Select(w => w.Name).ToList();
            }
        }

        public bool WorksheetExists(string filePath, string sheetName)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(sheetName))
                return false;

            if (!File.Exists(filePath))
                return false;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    return package.Workbook.Worksheets[sheetName] != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<string, List<Dictionary<string, object>>> ParseMultipleWorksheets(string filePath, List<string> sheetNames)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            if (sheetNames == null || sheetNames.Count == 0)
                throw new ArgumentException("工作表名称列表不能为空", nameof(sheetNames));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel文件不存在: {filePath}");

            var result = new Dictionary<string, List<Dictionary<string, object>>>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                foreach (var sheetName in sheetNames)
                {
                    var worksheet = package.Workbook.Worksheets[sheetName];
                    if (worksheet != null)
                    {
                        result[sheetName] = ParseWorksheetData(worksheet);
                    }
                    else
                    {
                        Console.WriteLine($"[WARNING] 工作表 '{sheetName}' 不存在，已跳过");
                        result[sheetName] = new List<Dictionary<string, object>>();
                    }
                }
            }

            return result;
        }

        private List<Dictionary<string, object>> ParseWorksheetData(ExcelWorksheet worksheet)
        {
            var result = new List<Dictionary<string, object>>();

            if (worksheet.Dimension == null)
            {
                return result; // 空工作表
            }

            // 获取列标题（第一行）
            var headers = new List<string>();
            for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
            {
                var headerValue = GetCellValue<string>(worksheet.Cells[1, col]);
                headers.Add(headerValue ?? $"Column{col}");
            }

            // 解析数据行（从第二行开始）
            for (int row = worksheet.Dimension.Start.Row + 1; row <= worksheet.Dimension.End.Row; row++)
            {
                var rowData = new Dictionary<string, object>();
                bool hasData = false;

                for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
                {
                    var headerIndex = col - worksheet.Dimension.Start.Column;
                    var header = headerIndex < headers.Count ? headers[headerIndex] : $"Column{col}";
                    var cellValue = GetCellValue<object>(worksheet.Cells[row, col]);
                    
                    rowData[header] = cellValue;
                    
                    if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                    {
                        hasData = true;
                    }
                }

                // 只添加有数据的行
                if (hasData)
                {
                    result.Add(rowData);
                }
            }

            return result;
        }

        //TODO: 重复代码(ID:DUP-006) - [单元格值获取：GetCellValue逻辑重复] 
        //TODO: 建议重构为共享的CellValueExtractor工具类 优先级:中等
        private T GetCellValue<T>(ExcelRange cell)
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

        public List<Dictionary<string, object>> ParseWorksheetSmart(string filePath, string expectedSheetName, IWorksheetLocatorService locatorService)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径不能为空", nameof(filePath));

            if (string.IsNullOrWhiteSpace(expectedSheetName))
                throw new ArgumentException("工作表名称不能为空", nameof(expectedSheetName));

            if (locatorService == null)
                throw new ArgumentNullException(nameof(locatorService));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel文件不存在: {filePath}");

            var actualSheetName = locatorService.LocateWorksheet(filePath, expectedSheetName);
            if (string.IsNullOrEmpty(actualSheetName))
            {
                var availableSheets = GetWorksheetNames(filePath);
                var errorMsg = $"在Excel文件中未找到名为'{expectedSheetName}'的工作表。\n" +
                              $"可用的工作表: {string.Join(", ", availableSheets)}";
                throw new ArgumentException(errorMsg);
            }

            Console.WriteLine($"[INFO] 智能匹配工作表: '{expectedSheetName}' -> '{actualSheetName}'");
            return ParseWorksheet(filePath, actualSheetName);
        }

        public WorksheetValidationResult ValidateWorksheetSmart(string filePath, string expectedSheetName, IWorksheetLocatorService locatorService)
        {
            if (locatorService == null)
                throw new ArgumentNullException(nameof(locatorService));

            return locatorService.ValidateWorksheet(filePath, expectedSheetName);
        }
    }
}