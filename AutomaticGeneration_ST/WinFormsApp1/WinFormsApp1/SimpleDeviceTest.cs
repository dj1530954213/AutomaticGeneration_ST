using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services;
using AutomaticGeneration_ST.Services.Implementations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutomaticGeneration_ST
{
    /// <summary>
    /// 简单的设备ST程序生成测试
    /// </summary>
    /// <remarks>
    /// 状态: @demo-code
    /// 优先级: P1 (低风险)
    /// 建议: 应移至独立的演示项目或示例目录
    /// 风险级别: 低风险
    /// 分析时间: 2025-08-15
    /// 影响范围: 仅开发阶段，不影响生产功能
    /// 说明: 提供简单的设备ST程序生成功能演示和测试
    /// </remarks>
    public class SimpleDeviceTest
    {
        public static void RunTest()
        {
            Console.WriteLine("🧪 开始设备ST程序生成功能测试...");

            try
            {
                // 1. 创建测试设备
                var devices = CreateTestDevices();
                Console.WriteLine($"✓ 创建了 {devices.Count} 个测试设备");

                // 2. 测试设备ST程序生成
                var stGenerationService = new STGenerationService();
                var deviceSTPrograms = stGenerationService.GenerateDeviceSTPrograms(devices);

                // 3. 显示生成结果
                Console.WriteLine($"✓ 生成了 {deviceSTPrograms.Count} 种模板的ST程序");

                foreach (var templateGroup in deviceSTPrograms)
                {
                    Console.WriteLine($"\n🎨 模板: {templateGroup.Key}");
                    Console.WriteLine(new string('=', 40));
                    
                    foreach (var program in templateGroup.Value.Take(1)) // 只显示第一个程序
                    {
                        var lines = program.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines.Take(10)) // 只显示前10行
                        {
                            Console.WriteLine(line);
                        }
                        if (lines.Length > 10)
                        {
                            Console.WriteLine("... (更多内容已省略)");
                        }
                    }
                }

                // 4. 测试模板数据绑定
                Console.WriteLine("\n🔗 测试模板数据绑定...");
                TestTemplateDataBinding(devices.FirstOrDefault());

                Console.WriteLine("\n✅ 设备ST程序生成功能测试完成！");
                Console.WriteLine("Form1.cs中的设备ST程序窗口应该能够正常显示生成的ST代码。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        private static List<Device> CreateTestDevices()
        {
            var devices = new List<Device>();

            // 创建ESDV设备
            var esdvDevice = new Device("ESDV1101", "ESDV_CTRL");
            
            // 添加IO点位（硬点）
            esdvDevice.AddIoPoint("XS_1101", new Dictionary<string, object>
            {
                ["变量名称（HMI名）"] = "XS_1101",
                ["描述信息"] = "阀门开到位反馈",
                ["数据类型"] = "BOOL",
                ["模块类型"] = "DI"
            });

            esdvDevice.AddIoPoint("ZSO_1101", new Dictionary<string, object>
            {
                ["变量名称（HMI名）"] = "ZSO_1101",
                ["描述信息"] = "阀门开限位",
                ["数据类型"] = "BOOL",
                ["模块类型"] = "DI"
            });

            // 添加设备点位（软点）
            esdvDevice.AddDevicePoint("C_AM_1101", new Dictionary<string, object>
            {
                ["变量名称"] = "C_AM_1101",
                ["变量描述"] = "阀门自动手动切换命令",
                ["数据类型"] = "BOOL"
            });

            esdvDevice.AddDevicePoint("S_AM_1101", new Dictionary<string, object>
            {
                ["变量名称"] = "S_AM_1101",
                ["变量描述"] = "阀门自动手动切换反馈",
                ["数据类型"] = "BOOL"
            });

            devices.Add(esdvDevice);

            // 创建PV设备
            var pvDevice = new Device("PV2101", "PV_CTRL");
            
            pvDevice.AddIoPoint("PV_2101", new Dictionary<string, object>
            {
                ["变量名称（HMI名）"] = "PV_2101",
                ["描述信息"] = "压力变送器",
                ["数据类型"] = "REAL",
                ["模块类型"] = "AI"
            });

            devices.Add(pvDevice);

            return devices;
        }

        private static void TestTemplateDataBinding(Device device)
        {
            if (device == null) return;

            try
            {
                var dataBinder = new DeviceTemplateDataBinder();
                
                // 简单的测试模板内容
                var templateContent = @"
ESDV{{device_tag}}_CTRL(
    CS:=CS01,
    ZIX:={{XS}},
    ZIO:={{ZSO}},
    C_AM:={{C_AM}},
    S_AM:={{S_AM}}
);";

                var bindingResult = dataBinder.BindDeviceTemplateData(device, templateContent);
                
                Console.WriteLine($"✓ 为设备 {device.DeviceTag} 绑定了 {bindingResult.Count} 个占位符");
                
                foreach (var binding in bindingResult.Take(3)) // 只显示前3个绑定
                {
                    Console.WriteLine($"   {binding.Key} -> {binding.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 模板数据绑定测试失败: {ex.Message}");
            }
        }
    }
}