using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace WinFormsApp1.Excel
{
    //TODO: 重复代码(ID:DUP-003) - [Excel解析：工作表解析逻辑分散在多个类中] 
    //TODO: 建议重构为统一的WorksheetParsingEngine，提取公共解析流程 优先级:中等
    public class ExcelReader
    {
        private LogService logger = LogService.Instance;
        
        public List<Dictionary<string, object>> ReadPoints(string filePath, int sheetIndex = 0)
        {
            var result = new List<Dictionary<string, object>>();
            
            try
            {
                logger.LogInfo($"开始读取文件: {Path.GetFileName(filePath)}");
                
                var extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".csv")
                {
                    return ReadCsvFile(filePath);
                }
                
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                
                IWorkbook workbook;
                if (extension == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else
                {
                    throw new NotSupportedException("仅支持.xlsx和.csv格式的文件");
                }
                
                var sheet = workbook.GetSheetAt(sheetIndex);
                if (sheet == null)
                {
                    throw new ArgumentException($"工作表索引{sheetIndex}不存在");
                }
                
                logger.LogInfo($"正在读取工作表: {sheet.SheetName}");
                
                // 读取表头
                var headerRow = sheet.GetRow(0);
                if (headerRow == null)
                {
                    throw new ArgumentException("Excel文件第一行为空，无法获取列名");
                }
                
                var columnNames = new List<string>();
                for (int cellIndex = 0; cellIndex < headerRow.LastCellNum; cellIndex++)
                {
                    var cell = headerRow.GetCell(cellIndex);
                    var columnName = GetCellValue(cell)?.ToString()?.Trim() ?? $"列{cellIndex + 1}";
                    columnNames.Add(columnName);
                }
                
                logger.LogInfo($"读取到{columnNames.Count}个列名: {string.Join(", ", columnNames)}");
                
                // 读取数据行
                int dataRowCount = 0;
                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null) continue;
                    
                    var rowData = new Dictionary<string, object>();
                    bool hasData = false;
                    
                    for (int cellIndex = 0; cellIndex < columnNames.Count; cellIndex++)
                    {
                        var cell = row.GetCell(cellIndex);
                        var cellValue = GetCellValue(cell);
                        
                        if (cellValue != null && !string.IsNullOrWhiteSpace(cellValue.ToString()))
                        {
                            hasData = true;
                        }
                        
                        rowData[columnNames[cellIndex]] = cellValue ?? string.Empty;
                    }
                    
                    if (hasData)
                    {
                        result.Add(rowData);
                        dataRowCount++;
                    }
                }
                
                logger.LogSuccess($"成功读取{dataRowCount}行数据");
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"读取Excel文件失败: {ex.Message}");
                throw;
            }
        }
        
        //TODO: 重复代码(ID:DUP-006) - [单元格值获取：GetCellValue逻辑重复] 
        //TODO: 建议重构为共享的CellValueExtractor工具类 优先级:中等
        private object? GetCellValue(ICell? cell)
        {
            if (cell == null) return null;
            
            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue,
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell) 
                    ? cell.DateCellValue.ToString()
                    : cell.NumericCellValue,
                CellType.Boolean => cell.BooleanCellValue,
                CellType.Formula => GetFormulaValue(cell),
                CellType.Blank => null,
                _ => cell.ToString()
            };
        }
        
        private object? GetFormulaValue(ICell cell)
        {
            try
            {
                return cell.CellType switch
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
        
        //TODO: 重复代码(ID:DUP-007) - [数据提取：GetCellValueAs*方法在多个类中重复实现] 
        //TODO: 建议重构为通用的DataValueExtractor工具类 优先级:中等
        public string GetCellValueAsString(Dictionary<string, object> row, string columnName)
        {
            return row.TryGetValue(columnName, out var value) ? value?.ToString()?.Trim() ?? string.Empty : string.Empty;
        }
        
        public double GetCellValueAsDouble(Dictionary<string, object> row, string columnName, double defaultValue = 0.0)
        {
            if (row.TryGetValue(columnName, out var value) && 
                double.TryParse(value?.ToString(), out var doubleValue))
            {
                return doubleValue;
            }
            return defaultValue;
        }
        
        public bool GetCellValueAsBool(Dictionary<string, object> row, string columnName, bool defaultValue = false)
        {
            if (row.TryGetValue(columnName, out var value))
            {
                var stringValue = value?.ToString()?.Trim().ToUpper();
                if (stringValue == "TRUE" || stringValue == "1" || stringValue == "是")
                    return true;
                if (stringValue == "FALSE" || stringValue == "0" || stringValue == "否")
                    return false;
            }
            return defaultValue;
        }
        
        private List<Dictionary<string, object>> ReadCsvFile(string filePath)
        {
            var result = new List<Dictionary<string, object>>();
            
            try
            {
                logger.LogInfo($"开始读取CSV文件: {Path.GetFileName(filePath)}");
                
                // 尝试多种编码读取CSV文件
                var lines = TryReadCsvWithDifferentEncodings(filePath);
                
                if (lines.Length == 0)
                {
                    throw new ArgumentException("CSV文件为空");
                }
                
                // 读取表头
                var headerLine = lines[0];
                var columnNames = ParseCsvLine(headerLine);
                
                logger.LogInfo($"读取到{columnNames.Length}个列名");
                foreach (var name in columnNames.Take(10)) // 只显示前10个列名避免日志过长
                {
                    logger.LogDebug($"列名: {name}");
                }
                
                // 读取数据行
                int dataRowCount = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var values = ParseCsvLine(line);
                    var rowData = new Dictionary<string, object>();
                    
                    for (int j = 0; j < columnNames.Length; j++)
                    {
                        var value = j < values.Length ? values[j] : string.Empty;
                        
                        // 清理值（去掉引号等）
                        value = CleanCsvValue(value);
                        
                        // 尝试转换数值
                        if (!string.IsNullOrWhiteSpace(value) && double.TryParse(value, out var numericValue))
                        {
                            rowData[columnNames[j]] = numericValue;
                        }
                        else if (!string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out var boolValue))
                        {
                            rowData[columnNames[j]] = boolValue;
                        }
                        else
                        {
                            rowData[columnNames[j]] = value;
                        }
                    }
                    
                    result.Add(rowData);
                    dataRowCount++;
                }
                
                logger.LogSuccess($"成功读取{dataRowCount}行CSV数据");
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"读取CSV文件失败: {ex.Message}");
                throw;
            }
        }
        
        private string[] TryReadCsvWithDifferentEncodings(string filePath)
        {
            var encodings = new[]
            {
                System.Text.Encoding.UTF8,
                System.Text.Encoding.GetEncoding("GB2312"),
                System.Text.Encoding.GetEncoding("GBK"),
                System.Text.Encoding.Default
            };
            
            foreach (var encoding in encodings)
            {
                try
                {
                    var lines = File.ReadAllLines(filePath, encoding);
                    if (lines.Length > 0 && !lines[0].Contains("���")) // 检查是否有乱码
                    {
                        logger.LogInfo($"使用编码 {encoding.EncodingName} 成功读取文件");
                        return lines;
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            // 如果所有编码都失败，使用UTF-8作为后备方案
            logger.LogWarning("所有编码尝试失败，使用UTF-8作为后备方案");
            return File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
        }
        
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString().Trim());
            return result.ToArray();
        }
        
        private string CleanCsvValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
                
            value = value.Trim();
            
            // 移除首尾的引号
            if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 1)
            {
                value = value.Substring(1, value.Length - 2);
            }
            
            return value.Trim();
        }
    }
}