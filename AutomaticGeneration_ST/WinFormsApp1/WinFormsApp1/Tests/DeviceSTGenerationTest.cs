using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Implementations;
using AutomaticGeneration_ST.Services;
using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Tests
{
    /// <summary>
    /// 设备ST程序生成功能测试类
    /// 验证设备模板套用和代码生成功能
    /// </summary>
    /// <remarks>
    /// 状态: @test-code-mixed
    /// 优先级: P1 (低风险)
    /// 建议: 应移至独立的测试项目
    /// 风险级别: 低风险
    /// 分析时间: 2025-08-15
    /// 影响范围: 仅开发阶段，不影响生产功能
    /// 说明: 测试设备ST代码生成功能，验证模板套用和代码生成
    /// </remarks>
    public class DeviceSTGenerationTest
    {
        private readonly LogService _logger = LogService.Instance;
        private readonly STGenerationService _stGenerationService;

        public DeviceSTGenerationTest()
        {
            _stGenerationService = new STGenerationService();
        }

        /// <summary>
        /// 测试设备ST程序生成功能
        /// </summary>
        public TestResult TestDeviceSTGeneration()
        {
            var result = new TestResult
            {
                TestName = "设备ST程序生成测试",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInfo("🧪 开始测试设备ST程序生成功能...");

                // 1. 创建测试设备
                var testDevices = CreateTestDevices();
                _logger.LogInfo($"   📋 创建了 {testDevices.Count} 个测试设备");

                // 2. 生成设备ST程序
                var deviceSTPrograms = _stGenerationService.GenerateDeviceSTPrograms(testDevices);
                
                // 3. 验证生成结果
                var validationErrors = new List<string>();

                if (deviceSTPrograms == null || deviceSTPrograms.Count == 0)
                {
                    validationErrors.Add("未生成任何设备ST程序");
                }
                else
                {
                    _logger.LogInfo($"   ✓ 生成了 {deviceSTPrograms.Count} 种模板的ST程序");
                    
                    foreach (var kvp in deviceSTPrograms)
                    {
                        var templateName = kvp.Key;
                        var programs = kvp.Value;
                        
                        _logger.LogInfo($"   📄 模板 [{templateName}]: {programs.Count} 个程序");
                        
                        foreach (var program in programs)
                        {
                            if (string.IsNullOrWhiteSpace(program))
                            {
                                validationErrors.Add($"模板 {templateName} 生成了空程序");
                            }
                            else
                            {
                                // 检查程序是否包含设备标签
                                if (!program.Contains("ESDV1101") && !program.Contains("PV2101"))
                                {
                                    validationErrors.Add($"模板 {templateName} 生成的程序未包含设备标签");
                                }
                                
                                _logger.LogInfo($"   ✓ 程序长度: {program.Length} 字符");
                            }
                        }
                    }
                }

                if (validationErrors.Any())
                {
                    result.Success = false;
                    result.Message = "设备ST程序生成验证失败";
                    result.Details = validationErrors;
                }
                else
                {
                    result.Success = true;
                    result.Message = $"成功生成 {deviceSTPrograms.Count} 种模板的设备ST程序";
                    result.Details = deviceSTPrograms.Select(kvp => 
                        $"✓ {kvp.Key}: {kvp.Value.Count} 个程序").ToList();
                }

                result.EndTime = DateTime.Now;
                _logger.LogInfo($"设备ST程序生成测试完成: {(result.Success ? "通过" : "失败")}");
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"测试执行失败: {ex.Message}";
                result.EndTime = DateTime.Now;
                _logger.LogError($"❌ 设备ST程序生成测试失败: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 测试模板数据绑定功能
        /// </summary>
        public TestResult TestTemplateDataBinding()
        {
            var result = new TestResult
            {
                TestName = "模板数据绑定测试",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInfo("🔗 开始测试模板数据绑定功能...");

                var dataBinder = new DeviceTemplateDataBinder();
                var testDevice = CreateTestESDVDevice();
                
                // 简单的测试模板内容
                var templateContent = @"
ESDV{{device_tag}}_CTRL(
    CS:=CS01,
    ZIX:={{XS}},
    ZIO:={{ZSO}},
    C_AM:={{C_AM}},
    S_AM:={{S_AM}},
    C_OPEN:={{C_OPEN}},
    C_CLOSE:={{C_CLOSE}}
);";

                var bindingResult = dataBinder.BindDeviceTemplateData(testDevice, templateContent);

                var validationErrors = new List<string>();

                // 验证device_tag绑定
                if (!bindingResult.ContainsKey("device_tag") || 
                    bindingResult["device_tag"].ToString() != testDevice.DeviceTag)
                {
                    validationErrors.Add("device_tag绑定失败");
                }

                // 验证点位绑定（至少应该有一些成功的绑定）
                var successfulBindings = 0;
                var expectedPlaceholders = new[] { "XS", "ZSO", "C_AM", "S_AM", "C_OPEN", "C_CLOSE" };
                
                foreach (var placeholder in expectedPlaceholders)
                {
                    if (bindingResult.ContainsKey(placeholder))
                    {
                        var value = bindingResult[placeholder].ToString();
                        if (!value.StartsWith("<") || !value.EndsWith(">"))
                        {
                            successfulBindings++;
                            _logger.LogInfo($"   ✓ {placeholder} -> {value}");
                        }
                        else
                        {
                            _logger.LogWarning($"   ⚠️ {placeholder} 未找到匹配点位");
                        }
                    }
                }

                if (successfulBindings == 0)
                {
                    validationErrors.Add("没有成功绑定任何点位占位符");
                }

                if (validationErrors.Any())
                {
                    result.Success = false;
                    result.Message = "模板数据绑定验证失败";
                    result.Details = validationErrors;
                }
                else
                {
                    result.Success = true;
                    result.Message = $"成功绑定 {successfulBindings} 个占位符";
                    result.Details = bindingResult.Select(kvp => 
                        $"✓ {kvp.Key} = {kvp.Value}").ToList();
                }

                result.EndTime = DateTime.Now;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"测试执行失败: {ex.Message}";
                result.EndTime = DateTime.Now;
                _logger.LogError($"❌ 模板数据绑定测试失败: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 创建测试设备列表
        /// </summary>
        private List<Device> CreateTestDevices()
        {
            var devices = new List<Device>();
            
            // 创建ESDV设备
            devices.Add(CreateTestESDVDevice());
            
            // 创建PV设备
            devices.Add(CreateTestPVDevice());

            return devices;
        }

        /// <summary>
        /// 创建测试ESDV设备
        /// </summary>
        private Device CreateTestESDVDevice()
        {
            var device = new Device("ESDV1101", "ESDV_CTRL");
            
            // 添加IO点位（硬点）
            var ioPoints = new Dictionary<string, Dictionary<string, object>>
            {
                ["XS_1101"] = new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "XS_1101",
                    ["描述信息"] = "阀门开到位反馈",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI",
                    ["信号类型"] = "数字量"
                },
                ["ZSO_1101"] = new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "ZSO_1101",
                    ["描述信息"] = "阀门开限位",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI",
                    ["信号类型"] = "数字量"
                }
            };

            foreach (var kvp in ioPoints)
            {
                device.AddIoPoint(kvp.Key, kvp.Value);
            }
            
            // 添加设备点位（软点）
            var devicePoints = new Dictionary<string, Dictionary<string, object>>
            {
                ["C_AM_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "C_AM_1101",
                    ["变量描述"] = "阀门自动手动切换命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.0",
                    ["MODBUS地址"] = "3400"
                },
                ["S_AM_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "S_AM_1101",
                    ["变量描述"] = "阀门自动手动切换反馈",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.1",
                    ["MODBUS地址"] = "3401"
                },
                ["C_OPEN_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "C_OPEN_1101",
                    ["变量描述"] = "阀门开命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.2",
                    ["MODBUS地址"] = "3402"
                },
                ["C_CLOSE_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "C_CLOSE_1101",
                    ["变量描述"] = "阀门关命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.3",
                    ["MODBUS地址"] = "3403"
                }
            };

            foreach (var kvp in devicePoints)
            {
                device.AddDevicePoint(kvp.Key, kvp.Value);
            }

            return device;
        }

        /// <summary>
        /// 创建测试PV设备
        /// </summary>
        private Device CreateTestPVDevice()
        {
            var device = new Device("PV2101", "PV_CTRL");
            
            // 添加IO点位（硬点）
            var ioPoints = new Dictionary<string, Dictionary<string, object>>
            {
                ["PV_2101"] = new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "PV_2101",
                    ["描述信息"] = "压力变送器",
                    ["数据类型"] = "REAL",
                    ["模块类型"] = "AI",
                    ["信号类型"] = "模拟量"
                }
            };

            foreach (var kvp in ioPoints)
            {
                device.AddIoPoint(kvp.Key, kvp.Value);
            }
            
            // 添加设备点位（软点）
            var devicePoints = new Dictionary<string, Dictionary<string, object>>
            {
                ["PV_H_2101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "PV_H_2101",
                    ["变量描述"] = "压力高报警",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX60.0"
                },
                ["PV_L_2101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "PV_L_2101",
                    ["变量描述"] = "压力低报警",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX60.1"
                }
            };

            foreach (var kvp in devicePoints)
            {
                device.AddDevicePoint(kvp.Key, kvp.Value);
            }

            return device;
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public List<TestResult> RunAllTests()
        {
            var results = new List<TestResult>();

            _logger.LogInfo("🧪 开始设备ST程序生成功能测试套件...");
            
            results.Add(TestTemplateDataBinding());
            results.Add(TestDeviceSTGeneration());

            var passedCount = results.Count(r => r.Success);
            var totalCount = results.Count;

            _logger.LogInfo($"🎯 测试完成: {passedCount}/{totalCount} 通过");
            
            if (passedCount == totalCount)
            {
                _logger.LogSuccess("✅ 所有设备ST程序生成功能测试通过！");
            }
            else
            {
                _logger.LogWarning($"⚠️ {totalCount - passedCount} 个测试未通过，需要检查相关功能");
            }

            return results;
        }
    }
}