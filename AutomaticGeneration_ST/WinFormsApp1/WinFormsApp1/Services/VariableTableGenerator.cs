using AutomaticGeneration_ST.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// Excel变量表生成器 - 生成.xls格式的多工作簿变量表文件
    /// </summary>
    public class VariableTableGenerator
    {
        /// <summary>
        /// 生成Excel变量表文件
        /// </summary>
        /// <param name="variableEntriesByTemplate">按模板分组的变量表条目</param>
        /// <param name="outputFilePath">输出文件路径</param>
        /// <returns>是否生成成功</returns>
        public bool GenerateVariableTable(Dictionary<string, List<VariableTableEntry>> variableEntriesByTemplate, string outputFilePath)
        {
            Console.WriteLine($"[VariableTableGenerator] 开始生成Excel变量表");
            Console.WriteLine($"[VariableTableGenerator] 输出文件路径: {outputFilePath}");
            Console.WriteLine($"[VariableTableGenerator] 模板数量: {variableEntriesByTemplate?.Count ?? 0}");
            
            if (variableEntriesByTemplate == null || !variableEntriesByTemplate.Any())
            {
                Console.WriteLine($"[VariableTableGenerator] 没有变量表条目需要生成");
                return false;
            }

            try
            {
                Console.WriteLine($"[VariableTableGenerator] 创建HSSF工作簿");
                // 创建Excel工作簿
                var workbook = new HSSFWorkbook();

                // 为每个模板创建工作表
                Console.WriteLine($"[VariableTableGenerator] 开始创建工作表，模板组数: {variableEntriesByTemplate.Count}");
                
                foreach (var templateGroup in variableEntriesByTemplate)
                {
                    var templateName = templateGroup.Key;
                    var entries = templateGroup.Value;
                    
                    Console.WriteLine($"[VariableTableGenerator] 处理模板: {templateName}，条目数: {entries.Count}");

                    if (!entries.Any())
                    {
                        Console.WriteLine($"[VariableTableGenerator] 模板 {templateName} 没有条目，跳过");
                        continue;
                    }

                    // 获取程序名称作为工作表名称
                    var programName = entries.First().ProgramName;
                    var sheetName = CleanSheetName(programName);
                    Console.WriteLine($"[VariableTableGenerator] 程序名称: {programName}, 清理后的工作表名: {sheetName}");
                    
                    var sheet = CreateWorksheet(workbook, sheetName, entries);
                    if (sheet == null)
                    {
                        Console.WriteLine($"[VariableTableGenerator] 创建工作表失败: {sheetName}");
                        continue;
                    }

                    Console.WriteLine($"[VariableTableGenerator] 创建工作表成功: {sheetName} ({entries.Count}个变量)");
                }

                // 如果没有创建任何工作表，返回失败
                Console.WriteLine($"[VariableTableGenerator] 工作簿中的工作表数量: {workbook.NumberOfSheets}");
                
                if (workbook.NumberOfSheets == 0)
                {
                    Console.WriteLine($"[VariableTableGenerator] 没有创建任何工作表");
                    return false;
                }

                // 检查输出目录是否存在
                var outputDir = Path.GetDirectoryName(outputFilePath);
                Console.WriteLine($"[VariableTableGenerator] 输出目录: {outputDir}");
                
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Console.WriteLine($"[VariableTableGenerator] 创建输出目录: {outputDir}");
                    Directory.CreateDirectory(outputDir);
                }

                // 保存文件
                Console.WriteLine($"[VariableTableGenerator] 开始保存Excel文件");
                using (var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fileStream);
                    Console.WriteLine($"[VariableTableGenerator] 文件写入完成，大小: {fileStream.Length} 字节");
                }

                // 验证文件是否生成
                if (File.Exists(outputFilePath))
                {
                    var fileInfo = new FileInfo(outputFilePath);
                    Console.WriteLine($"[VariableTableGenerator] Excel变量表生成成功: {outputFilePath} (大小: {fileInfo.Length} 字节)");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[VariableTableGenerator] 文件未生成: {outputFilePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VariableTableGenerator] 生成Excel变量表失败: {ex.Message}");
                Console.WriteLine($"[VariableTableGenerator] 异常堆栈: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 创建工作表
        /// </summary>
        /// <param name="workbook">工作簿</param>
        /// <param name="sheetName">工作表名称</param>
        /// <param name="entries">变量表条目</param>
        /// <returns>创建的工作表</returns>
        private ISheet? CreateWorksheet(HSSFWorkbook workbook, string sheetName, List<VariableTableEntry> entries)
        {
            Console.WriteLine($"[VariableTableGenerator] 创建工作表: {sheetName}, 条目数: {entries.Count}");
            
            try
            {
                var sheet = workbook.CreateSheet(sheetName);
                Console.WriteLine($"[VariableTableGenerator] 工作表创建成功: {sheetName}");

                // 创建单元格样式：文本格式 + 宋体11
                var textCellStyle = workbook.CreateCellStyle();
                textCellStyle.DataFormat = workbook.CreateDataFormat().GetFormat("@"); // 文本格式
                
                var font = workbook.CreateFont();
                font.FontName = "宋体";
                font.FontHeightInPoints = 11;
                textCellStyle.SetFont(font);
                
                Console.WriteLine($"[VariableTableGenerator] 创建文本格式样式完成（宋体11）");

                // 创建第1行：程序名称
                var row0 = sheet.CreateRow(0);
                var cell0 = row0.CreateCell(0);
                var programName = entries.First().ProgramName;
                cell0.SetCellValue(programName);
                cell0.CellStyle = textCellStyle; // 应用样式
                Console.WriteLine($"[VariableTableGenerator] 设置第1行程序名称: {programName}");

                // 创建第2行：表头
                var row1 = sheet.CreateRow(1);
                var headers = new[] { "变量名", "直接地址", "变量说明", "变量类型", "初始值", "掉电保护", "SOE使能" };
                Console.WriteLine($"[VariableTableGenerator] 设置表头，列数: {headers.Length}");
                
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = row1.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = textCellStyle; // 应用样式
                }
                Console.WriteLine($"[VariableTableGenerator] 表头设置完成");

                // 创建数据行
                Console.WriteLine($"[VariableTableGenerator] 开始创建 {entries.Count} 个数据行");
                
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    var row = sheet.CreateRow(i + 2); // 从第3行开始

                    // 变量名
                    var dataCell0 = row.CreateCell(0);
                    dataCell0.SetCellValue(entry.VariableName);
                    dataCell0.CellStyle = textCellStyle;
                    
                    // 直接地址（留空）
                    var dataCell1 = row.CreateCell(1);
                    dataCell1.SetCellValue(entry.DirectAddress);
                    dataCell1.CellStyle = textCellStyle;
                    
                    // 变量说明（留空）
                    var dataCell2 = row.CreateCell(2);
                    dataCell2.SetCellValue(entry.VariableDescription);
                    dataCell2.CellStyle = textCellStyle;
                    
                    // 变量类型
                    var dataCell3 = row.CreateCell(3);
                    dataCell3.SetCellValue(entry.VariableType);
                    dataCell3.CellStyle = textCellStyle;
                    
                    // 初始值
                    var dataCell4 = row.CreateCell(4);
                    dataCell4.SetCellValue(entry.InitialValue);
                    dataCell4.CellStyle = textCellStyle;
                    
                    // 掉电保护
                    var dataCell5 = row.CreateCell(5);
                    dataCell5.SetCellValue(entry.PowerFailureProtection);
                    dataCell5.CellStyle = textCellStyle;
                    
                    // SOE使能
                    var dataCell6 = row.CreateCell(6);
                    dataCell6.SetCellValue(entry.SOEEnable);
                    dataCell6.CellStyle = textCellStyle;
                    
                    if (i < 3 || i % 10 == 0) // 只显示前3行和每10行的进度
                    {
                        Console.WriteLine($"[VariableTableGenerator] 创建第 {i + 1} 行: {entry.VariableName} ({entry.VariableType})");
                    }
                }
                
                Console.WriteLine($"[VariableTableGenerator] 数据行创建完成，总计 {entries.Count} 行");

                // 自动调整列宽
                Console.WriteLine($"[VariableTableGenerator] 开始调整列宽");
                for (int i = 0; i < headers.Length; i++)
                {
                    sheet.AutoSizeColumn(i);
                    
                    // 限制最大列宽，避免初始值列过宽
                    if (i == 4) // 初始值列
                    {
                        var currentWidth = sheet.GetColumnWidth(i);
                        var maxWidth = 256 * 50; // 50个字符宽度
                        if (currentWidth > maxWidth)
                        {
                            sheet.SetColumnWidth(i, maxWidth);
                            Console.WriteLine($"[VariableTableGenerator] 限制初始值列宽度: {maxWidth}");
                        }
                    }
                }
                Console.WriteLine($"[VariableTableGenerator] 列宽调整完成");

                Console.WriteLine($"[VariableTableGenerator] 工作表 {sheetName} 创建完成");
                return sheet;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VariableTableGenerator] 创建工作表失败 {sheetName}: {ex.Message}");
                Console.WriteLine($"[VariableTableGenerator] 异常堆栈: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 清理工作表名称，确保符合Excel规范
        /// </summary>
        /// <param name="sheetName">原始工作表名称</param>
        /// <returns>清理后的工作表名称</returns>
        private string CleanSheetName(string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
                return "Sheet1";

            // 移除(PRG)后缀
            if (sheetName.EndsWith("(PRG)", StringComparison.OrdinalIgnoreCase))
            {
                sheetName = sheetName.Substring(0, sheetName.Length - 5);
            }

            // 替换不允许的字符
            var invalidChars = new char[] { '\\', '/', '*', '?', ':', '[', ']' };
            foreach (var invalidChar in invalidChars)
            {
                sheetName = sheetName.Replace(invalidChar, '_');
            }

            // 限制长度（Excel工作表名称最大31个字符）
            if (sheetName.Length > 31)
            {
                sheetName = sheetName.Substring(0, 31);
            }

            return sheetName.Trim();
        }

        /// <summary>
        /// 生成变量表统计信息
        /// </summary>
        /// <param name="variableEntriesByTemplate">按模板分组的变量表条目</param>
        /// <returns>统计信息字符串</returns>
        public string GenerateStatistics(Dictionary<string, List<VariableTableEntry>> variableEntriesByTemplate)
        {
            if (variableEntriesByTemplate == null || !variableEntriesByTemplate.Any())
            {
                return "没有变量表数据";
            }

            var stats = new System.Text.StringBuilder();
            stats.AppendLine("变量表生成统计:");
            stats.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            stats.AppendLine($"模板数量: {variableEntriesByTemplate.Count}");

            int totalVariables = 0;
            foreach (var templateGroup in variableEntriesByTemplate)
            {
                var templateName = templateGroup.Key;
                var entries = templateGroup.Value;
                totalVariables += entries.Count;
                
                stats.AppendLine($"  - {templateName}: {entries.Count}个变量");
            }

            stats.AppendLine($"变量总数: {totalVariables}");
            return stats.ToString();
        }
    }
}