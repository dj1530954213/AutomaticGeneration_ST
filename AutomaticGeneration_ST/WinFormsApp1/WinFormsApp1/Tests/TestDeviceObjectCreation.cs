using System;
using AutomaticGeneration_ST.Tests;

namespace AutomaticGeneration_ST.Tests
{
    /// <summary>
    /// 独立的设备对象创建测试程序入口
    /// </summary>
    public class TestDeviceObjectCreation
    {
        public static void Main(string[] args)
        {
            Console.Title = "设备对象创建功能测试";
            
            try
            {
                DeviceObjectTestRunner.RunDeviceObjectTest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序执行失败: {ex.Message}");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
        }
    }
}