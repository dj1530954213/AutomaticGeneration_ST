//NEED DELETE
// REASON: This is a test class for the classified export function and is not used in the main program logic.

using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Interfaces;
using AutomaticGeneration_ST.Services.Implementations;
using System;
using System.IO;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services.Implementations
{
    /// <summary>
    /// 测试分类导出功能
    /// </summary>
    public static class TestCategorizedExport
    {
        /// <summary>
        /// 运行分类导出测试
        /// </summary>
        public static void RunTest()
        {
            try
            {
                Console.WriteLine("✨ 开始测试ST脚本分类导出功能...");
                Console.WriteLine();
                
                // 设置输出目录
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var outputDirectory = Path.Combine(baseDirectory, "classified_export_test");
                
                // 确保目录存在
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                
                Console.WriteLine($"📁 输出目录: {outputDirectory}");
                Console.WriteLine();
                
                // 运行演示
                var result = CategorizedExportExample.DemonstrateClassifiedExport(outputDirectory);
                
                // 显示结果
                Console.WriteLine(result);
                
                // 列出生成的文件
                Console.WriteLine();
                Console.WriteLine("💾 生成的文件:");
                if (Directory.Exists(outputDirectory))
                {
                    var files = Directory.GetFiles(outputDirectory, "*.txt");
                    if (files.Length > 0)
                    {
                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            Console.WriteLine($"  • {Path.GetFileName(file)} ({fileInfo.Length} bytes)");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  ⚠️ 没有找到生成的txt文件");
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("✅ 测试完成!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"堆栈信息: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 主函数 - 可以在命令行中运行
        /// </summary>
        /// <param name="args">命令行参数</param>
        public static void Main(string[] args)
        {
            Console.WriteLine("🚀 ST脚本分类导出功能测试程序");
            Console.WriteLine(new string('=', 50));
            RunTest();
            
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
