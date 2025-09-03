////NEED DELETE: 最小示例/控制台入口（与WinForms主流程无关），仅示例用途
//using System;
//using System.Collections.Generic;
//using System.IO;
//using AutomaticGeneration_ST.Models;
//using OfficeOpenXml;
//using Point = AutomaticGeneration_ST.Models.Point;

//namespace WinFormsApp1.Minimal
//{
//    /// <summary>
//    /// 最小化的ST代码生成器 - 用于验证核心功能
//    /// </summary>
//    public class MinimalSTGenerator
//    {
//        /// <summary>
//        /// 控制台程序入口点
//        /// </summary>
//        public static void Main()
//        {
//            Console.WriteLine("ST自动生成器 - 最小化版本");
//            Console.WriteLine("==========================");

//            try
//            {
//                // 测试基础ST代码生成
//                TestBasicSTGeneration();

//                // 测试Excel解析（模拟）
//                TestExcelParsing();

//                Console.WriteLine("\n✅ 核心功能测试通过！");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
//            }

//            Console.WriteLine("\n按任意键退出...");
//            Console.ReadKey();
//        }

//        /// <summary>
//        /// 测试基础ST代码生成
//        /// </summary>
//        private static void TestBasicSTGeneration()
//        {
//            Console.WriteLine("\n1. 测试基础ST代码生成:");

//            // 创建测试点位数据
//            var points = new List<Point>
//            {
//                new Point("TEMP_01")
//                { 
//                    ModuleType = "AI", 
//                    Description = "反应器温度",
//                    Unit = "℃",
//                    DataType = "REAL"
//                },
//                new Point("PUMP_01")
//                { 
//                    ModuleType = "DO", 
//                    Description = "循环泵",
//                    DataType = "BOOL"
//                },
//                new Point("LEVEL_SW")
//                { 
//                    ModuleType = "DI", 
//                    Description = "液位开关",
//                    DataType = "BOOL"
//                }
//            };

//            // 生成ST代码
//            var stCode = GenerateSTCode(points);
            
//            Console.WriteLine("生成的ST代码:");
//            Console.WriteLine(stCode);

//            // 保存到文件
//            var outputPath = Path.Combine(Environment.CurrentDirectory, "generated_st_code.txt");
//            File.WriteAllText(outputPath, stCode);
//            Console.WriteLine($"代码已保存到: {outputPath}");
//        }

//        /// <summary>
//        /// 测试Excel解析（模拟）
//        /// </summary>
//        private static void TestExcelParsing()
//        {
//            Console.WriteLine("\n2. 测试Excel解析功能（模拟）:");

//            // 模拟从Excel解析的数据
//            var mockExcelData = new List<Dictionary<string, object>>
//            {
//                new Dictionary<string, object>
//                {
//                    ["点位号"] = "AI002",
//                    ["点位名称"] = "Pressure_01", 
//                    ["点位类型"] = "AI",
//                    ["HMI标签"] = "PRESS_01",
//                    ["描述"] = "系统压力",
//                    ["单位"] = "bar"
//                },
//                new Dictionary<string, object>
//                {
//                    ["点位号"] = "AO001",
//                    ["点位名称"] = "Valve_01",
//                    ["点位类型"] = "AO", 
//                    ["HMI标签"] = "VALVE_01",
//                    ["描述"] = "调节阀开度",
//                    ["单位"] = "%"
//                }
//            };

//            // 转换为Point对象
//            var points = ConvertExcelDataToPoints(mockExcelData);
            
//            Console.WriteLine($"从Excel解析到 {points.Count} 个点位:");
//            foreach (var point in points)
//            {
//                Console.WriteLine($"  - {point.HmiTagName}: {point.Description} ({point.ModuleType})");
//            }

//            // 生成ST代码
//            var stCode = GenerateSTCode(points);
//            Console.WriteLine("\n生成的ST代码:");
//            Console.WriteLine(stCode.Substring(0, Math.Min(200, stCode.Length)) + "...");
//        }

//        /// <summary>
//        /// 将Excel数据转换为Point对象
//        /// </summary>
//        private static List<Point> ConvertExcelDataToPoints(List<Dictionary<string, object>> excelData)
//        {
//            var points = new List<Point>();

//            foreach (var row in excelData)
//            {
//                // 从HMI标签获取变量名，这是必需的构造函数参数
//                var hmiTagName = row.TryGetValue("HMI标签", out var hmiTag) ? hmiTag.ToString()! : "DEFAULT_TAG";
//                var point = new Point(hmiTagName);

//                if (row.TryGetValue("点位类型", out var type))
//                    point.ModuleType = type.ToString();

//                if (row.TryGetValue("描述", out var desc))
//                    point.Description = desc.ToString();

//                if (row.TryGetValue("单位", out var unit))
//                    point.Unit = unit.ToString();

//                // 根据类型设置数据类型
//                if (point.ModuleType == "AI" || point.ModuleType == "AO")
//                    point.DataType = "REAL";
//                else
//                    point.DataType = "BOOL";

//                points.Add(point);
//            }

//            return points;
//        }

//        /// <summary>
//        /// 生成ST代码的核心方法
//        /// </summary>
//        public static string GenerateSTCode(List<Point> points)
//        {
//            var code = "";

//            // 生成程序头部
//            code += "// ST代码 - 自动生成\n";
//            code += $"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
//            code += $"// 点位数量: {points.Count}\n\n";

//            // 生成变量声明部分
//            code += "PROGRAM Main\n";
//            code += "VAR\n";

//            // 按类型分组生成变量
//            var aiPoints = points.FindAll(p => p.ModuleType == "AI");
//            var aoPoints = points.FindAll(p => p.ModuleType == "AO");
//            var diPoints = points.FindAll(p => p.ModuleType == "DI");
//            var doPoints = points.FindAll(p => p.ModuleType == "DO");

//            if (aiPoints.Count > 0)
//            {
//                code += "    // 模拟量输入\n";
//                foreach (var point in aiPoints)
//                {
//                    code += $"    {point.HmiTagName} : REAL; // {point.Description} ({point.Unit})\n";
//                }
//                code += "\n";
//            }

//            if (aoPoints.Count > 0)
//            {
//                code += "    // 模拟量输出\n";
//                foreach (var point in aoPoints)
//                {
//                    code += $"    {point.HmiTagName} : REAL; // {point.Description} ({point.Unit})\n";
//                }
//                code += "\n";
//            }

//            if (diPoints.Count > 0)
//            {
//                code += "    // 数字量输入\n";
//                foreach (var point in diPoints)
//                {
//                    code += $"    {point.HmiTagName} : BOOL; // {point.Description}\n";
//                }
//                code += "\n";
//            }

//            if (doPoints.Count > 0)
//            {
//                code += "    // 数字量输出\n";
//                foreach (var point in doPoints)
//                {
//                    code += $"    {point.HmiTagName} : BOOL; // {point.Description}\n";
//                }
//                code += "\n";
//            }

//            code += "END_VAR\n\n";

//            // 生成主程序逻辑
//            code += "// 主程序逻辑\n";
//            code += "BEGIN\n";
//            code += "    // TODO: 在此处添加控制逻辑\n";
            
//            // 生成简单的示例逻辑
//            if (aiPoints.Count > 0)
//            {
//                code += "    \n    // 模拟量处理示例\n";
//                foreach (var point in aiPoints.Take(2))
//                {
//                    code += $"    IF {point.HmiTagName} > 100.0 THEN\n";
//                    code += $"        // {point.Description}超限处理\n";
//                    code += $"    END_IF;\n";
//                }
//            }

//            if (doPoints.Count > 0)
//            {
//                code += "    \n    // 数字量输出示例\n";
//                foreach (var point in doPoints.Take(2))
//                {
//                    code += $"    {point.HmiTagName} := TRUE; // 启动{point.Description}\n";
//                }
//            }

//            code += "\nEND_PROGRAM\n";

//            return code;
//        }

//        /// <summary>
//        /// 获取ST数据类型
//        /// </summary>
//        private static string GetSTType(string pointType)
//        {
//            return pointType switch
//            {
//                "AI" => "REAL",    // 模拟量输入
//                "AO" => "REAL",    // 模拟量输出
//                "DI" => "BOOL",    // 数字量输入
//                "DO" => "BOOL",    // 数字量输出
//                _ => "REAL"        // 默认类型
//            };
//        }

//        /// <summary>
//        /// 生成简化的模板代码（模拟模板引擎）
//        /// </summary>
//        public static string GenerateFromTemplate(string templateType, Point point)
//        {
//            return templateType switch
//            {
//                "AI" => GenerateAITemplate(point),
//                "AO" => GenerateAOTemplate(point),
//                "DI" => GenerateDITemplate(point),
//                "DO" => GenerateDOTemplate(point),
//                _ => $"// 未知类型: {templateType}\n"
//            };
//        }

//        private static string GenerateAITemplate(Point point)
//        {
//            return $@"// 模拟量输入: {point.Description}
//{point.HmiTagName}_RAW : INT;        // 原始AD值
//{point.HmiTagName}_SCALE : REAL;     // 缩放后的值
//{point.HmiTagName}_ALARM_H : BOOL;   // 高报警
//{point.HmiTagName}_ALARM_L : BOOL;   // 低报警

//// 缩放计算
//{point.HmiTagName}_SCALE := INT_TO_REAL({point.HmiTagName}_RAW) * 0.1;

//// 报警处理
//{point.HmiTagName}_ALARM_H := {point.HmiTagName}_SCALE > 100.0;
//{point.HmiTagName}_ALARM_L := {point.HmiTagName}_SCALE < 0.0;

//";
//        }

//        private static string GenerateAOTemplate(Point point)
//        {
//            return $@"// 模拟量输出: {point.Description}
//{point.HmiTagName}_CMD : REAL;       // 命令值
//{point.HmiTagName}_RAW : INT;        // 输出DA值

//// 转换为DA值
//{point.HmiTagName}_RAW := REAL_TO_INT({point.HmiTagName}_CMD * 10.0);

//";
//        }

//        private static string GenerateDITemplate(Point point)
//        {
//            return $@"// 数字量输入: {point.Description}
//{point.HmiTagName}_RAW : BOOL;       // 硬件输入
//{point.HmiTagName}_FILT : BOOL;      // 滤波后的值
//{point.HmiTagName}_EDGE : BOOL;      // 边沿检测

//// 简单滤波
//{point.HmiTagName}_FILT := {point.HmiTagName}_RAW;

//";
//        }

//        private static string GenerateDOTemplate(Point point)
//        {
//            return $@"// 数字量输出: {point.Description}
//{point.HmiTagName}_CMD : BOOL;       // 命令
//{point.HmiTagName}_STATUS : BOOL;    // 状态反馈

//// 输出控制
//{point.HmiTagName} := {point.HmiTagName}_CMD;

//";
//        }
//    }
//}
