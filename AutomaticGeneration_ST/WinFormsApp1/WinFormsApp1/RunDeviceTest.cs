using System;

namespace AutomaticGeneration_ST
{
    /// <summary>
    /// 运行设备ST程序生成测试的入口程序
    /// </summary>
    /// <remarks>
    /// 状态: @demo-code
    /// 优先级: P1 (低风险)
    /// 建议: 应移至独立的演示项目或示例目录
    /// 风险级别: 低风险
    /// 分析时间: 2025-08-15
    /// 影响范围: 仅开发阶段，不影响生产功能
    /// 说明: 设备ST程序生成功能的演示入口点
    /// </remarks>
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