using System;

namespace AutomaticGeneration_ST
{
    /// <summary>
    /// 运行设备ST程序生成测试的入口程序
    /// </summary>
    public class RunDeviceTest
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Console.WriteLine("设备ST程序生成功能测试");
            Console.WriteLine("========================");
            
            // 运行简单测试
            SimpleDeviceTest.RunTest();
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}