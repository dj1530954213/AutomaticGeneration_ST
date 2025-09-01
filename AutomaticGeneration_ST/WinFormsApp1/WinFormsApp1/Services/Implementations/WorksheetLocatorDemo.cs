////NEED DELETE
//// REASON: This is a demo class for the worksheet locator service and is not used in the main program logic.

//using AutomaticGeneration_ST.Services.Interfaces;
//using AutomaticGeneration_ST.Services.Implementations;
//using System;
//using System.IO;

//namespace AutomaticGeneration_ST.Services.Implementations
//{
//    /// <summary>
//    /// 工作表定位服务演示程序 - 用于测试和演示新的智能工作表查找功能
//    /// </summary>
//    public static class WorksheetLocatorDemo
//    {
//        public static void DemonstrateWorksheetLocation(string excelFilePath)
//        {
//            if (!File.Exists(excelFilePath))
//            {
//                Console.WriteLine($"[错误] Excel文件不存在: {excelFilePath}");
//                return;
//            }

//            Console.WriteLine("=== 工作表定位服务演示 ===\n");

//            // 初始化服务
//            var excelParser = new ExcelWorkbookParser();
//            var worksheetLocator = new WorksheetLocatorService(excelParser);

//            // 获取所有工作表
//            var availableSheets = worksheetLocator.GetAvailableWorksheetNames(excelFilePath);
//            Console.WriteLine($"可用的工作表 ({availableSheets.Count} 个):");
//            foreach (var sheet in availableSheets)
//            {
//                Console.WriteLine($"  - {sheet}");
//            }
//            Console.WriteLine();

//            // 测试不同的工作表名称查找
//            var testCases = new[]
//            {
//                "IO点表",
//                "io点表",
//                "IO",
//                "IO表",
//                "Points",
//                "点位表",
//                "设备分类表",
//                "设备分类",
//                "分类表",
//                "Device",
//                "Devices",
//                "阀门",
//                "Valve",
//                "调节阀",
//                "Control Valve",
//                "不存在的表名"
//            };

//            Console.WriteLine("工作表查找测试结果:");
//            Console.WriteLine("---------------------------------------------------");
//            Console.WriteLine("期望名称\t\t\t实际找到\t\t\t匹配类型");
//            Console.WriteLine("---------------------------------------------------");

//            foreach (var testCase in testCases)
//            {
//                var validation = worksheetLocator.ValidateWorksheet(excelFilePath, testCase);
//                var actualName = validation.IsFound ? validation.ActualName : "未找到";
//                var matchType = validation.IsFound ? validation.MatchType.ToString() : "N/A";
                
//                Console.WriteLine($"{testCase,-20}\t{actualName,-20}\t{matchType}");
                
//                if (!validation.IsFound && !string.IsNullOrEmpty(validation.ErrorMessage))
//                {
//                    Console.WriteLine($"  错误: {validation.ErrorMessage.Split('\n')[0]}"); // 只显示首行错误信息
//                }
//            }

//            Console.WriteLine("\n=== 演示结束 ===\n");
//        }

//        /// <summary>
//        /// 演示使用新的智能 Excel 解析器
//        /// </summary>
//        public static void DemonstrateSmartExcelParsing(string excelFilePath)
//        {
//            if (!File.Exists(excelFilePath))
//            {
//                Console.WriteLine($"[错误] Excel文件不存在: {excelFilePath}");
//                return;
//            }

//            Console.WriteLine("=== 智能 Excel 解析演示 ===\n");

//            var excelParser = new ExcelWorkbookParser();
//            var worksheetLocator = new WorksheetLocatorService(excelParser);

//            // 测试在不同情况下解析工作表
//            var testSheets = new[] { "IO点表", "设备分类表", "阀门", "不存在的表" };

//            foreach (var sheetName in testSheets)
//            {
//                Console.WriteLine($"正在解析工作表: {sheetName}");
                
//                try
//                {
//                    var data = excelParser.ParseWorksheetSmart(excelFilePath, sheetName, worksheetLocator);
//                    Console.WriteLine($"  ✓ 成功解析，共 {data.Count} 行数据");
                    
//                    if (data.Count > 0)
//                    {
//                        Console.WriteLine($"  • 列数: {data[0].Keys.Count}");
//                        Console.WriteLine($"  • 列名: {string.Join(", ", data[0].Keys.Take(5))}{(data[0].Keys.Count > 5 ? "..." : "")}");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"  ✗ 解析失败: {ex.Message}");
//                }
                
//                Console.WriteLine();
//            }

//            Console.WriteLine("=== 演示结束 ===\n");
//        }
//    }
//}