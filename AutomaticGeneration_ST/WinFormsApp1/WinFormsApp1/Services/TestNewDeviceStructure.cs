using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Implementations;
using System;
using System.Collections.Generic;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// 测试新的设备数据结构和功能
    /// </summary>
    public class TestNewDeviceStructure
    {
        private readonly LogService _logger = LogService.Instance;
        private readonly DeviceTemplateDataBinder _dataBinder = new DeviceTemplateDataBinder();

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public void RunTests()
        {
            _logger.LogInfo("🧪 开始测试新的设备数据结构和安全字段处理...");

            try
            {
                TestDeviceCreation();
                TestDataBinding();
                TestPointSearching();
                TestSafeFieldHandling();
                
                _logger.LogSuccess("✅ 所有测试通过！新的设备数据结构和安全字段处理工作正常");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ 测试失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 测试安全字段处理
        /// </summary>
        private void TestSafeFieldHandling()
        {
            _logger.LogInfo("📝 测试4: 安全字段处理");

            var device = new Device("TEST_DEVICE", "TEST_TEMPLATE");

            // 测试添加包含空值的IO点位
            var ioPointWithNulls = new Dictionary<string, object>
            {
                ["变量名称（HMI名）"] = "TEST_IO_POINT",
                ["描述信息"] = null, // 空值
                ["数据类型"] = "BOOL",
                ["模块类型"] = null, // 空值
                ["PLC绝对地址"] = "", // 空字符串
                ["上位机通讯地址"] = "1000"
            };

            device.AddIoPoint("TEST_IO_POINT", ioPointWithNulls);

            // 测试添加包含空值的设备点位
            var devicePointWithNulls = new Dictionary<string, object>
            {
                ["变量名称"] = "TEST_DEVICE_POINT",
                ["变量描述"] = null, // 空值
                ["数据类型"] = "BOOL",
                ["PLC地址"] = null, // 空值
                ["MODBUS地址"] = "2000"
            };

            device.AddDevicePoint("TEST_DEVICE_POINT", devicePointWithNulls);

            // 验证数据存储
            if (device.IoPoints.Count != 1 || device.DevicePoints.Count != 1)
                throw new Exception("包含空值的点位数据未能正确存储");

            var storedIoPoint = device.FindPointData("TEST_IO_POINT");
            var storedDevicePoint = device.FindPointData("TEST_DEVICE_POINT");

            if (storedIoPoint == null || storedDevicePoint == null)
                throw new Exception("无法找到包含空值的点位数据");

            _logger.LogInfo("   ✓ 安全字段处理测试通过");
        }

        /// <summary>
        /// 测试设备创建和点位添加
        /// </summary>
        private void TestDeviceCreation()
        {
            _logger.LogInfo("📝 测试1: 设备创建和点位添加");

            var device = new Device("ESDV1101", "ESDV_CTRL");

            // 添加IO点位
            device.AddIoPoint("XS_1101", new Dictionary<string, object>
            {
                ["变量名称（HMI名）"] = "XS_1101",
                ["描述信息"] = "阀门开到位反馈",
                ["数据类型"] = "BOOL",
                ["模块类型"] = "DI",
                ["信号类型"] = "数字量"
            });

            device.AddIoPoint("ZSH_1101", new Dictionary<string, object>
            {
                ["变量名称（HMI名）"] = "ZSH_1101",
                ["描述信息"] = "阀门开限位",
                ["数据类型"] = "BOOL",
                ["模块类型"] = "DI",
                ["信号类型"] = "数字量"
            });

            // 添加设备点位
            device.AddDevicePoint("AM_1101", new Dictionary<string, object>
            {
                ["变量名称"] = "AM_1101",
                ["变量描述"] = "站点阀门切断阀自动状态",
                ["数据类型"] = "BOOL",
                ["PLC地址"] = "%MX56.2",
                ["MODBUS地址"] = "3451"
            });

            device.AddDevicePoint("C_AM_1101", new Dictionary<string, object>
            {
                ["变量名称"] = "C_AM_1101",
                ["变量描述"] = "站点阀门切断阀命令自动手模切换命令",
                ["数据类型"] = "BOOL",
                ["PLC地址"] = "%MX56.3",
                ["MODBUS地址"] = "3452"
            });

            // 验证结果
            if (device.IoPoints.Count != 2)
                throw new Exception($"IO点位数量错误，期望2个，实际{device.IoPoints.Count}个");

            if (device.DevicePoints.Count != 2)
                throw new Exception($"设备点位数量错误，期望2个，实际{device.DevicePoints.Count}个");

            _logger.LogInfo($"   ✓ 设备 [{device.DeviceTag}] 创建成功: IO点位={device.IoPoints.Count}, 设备点位={device.DevicePoints.Count}");
        }

        /// <summary>
        /// 测试数据绑定功能
        /// </summary>
        private void TestDataBinding()
        {
            _logger.LogInfo("📝 测试2: 模板数据绑定");

            var device = CreateTestDevice();
            var templateContent = @"
ESDV{{device_tag}}_CTRL(
CS:=CS01,
ZIX:={{XS}},
ZIO:={{ZSH}},
ZIC:={{ZSL}},
C_AM:={{C_AM}},
S_AM:={{S_AM}},
C_OPEN:={{C_OPEN}},
C_CLOSE:={{C_CLOSE}}
);";

            var bindingResult = _dataBinder.BindDeviceTemplateData(device, templateContent);

            // 验证绑定结果
            if (!bindingResult.ContainsKey("device_tag") || bindingResult["device_tag"].ToString() != device.DeviceTag)
                throw new Exception("device_tag绑定失败");

            var expectedBindings = 0;
            var placeholders = new[] { "XS", "ZSH", "ZSL", "C_AM", "S_AM", "C_OPEN", "C_CLOSE" };
            
            foreach (var placeholder in placeholders)
            {
                if (bindingResult.ContainsKey(placeholder))
                {
                    var value = bindingResult[placeholder].ToString();
                    if (!value.StartsWith("<") || !value.EndsWith(">"))
                    {
                        expectedBindings++;
                        _logger.LogInfo($"   ✓ {placeholder} -> {value}");
                    }
                    else
                    {
                        _logger.LogWarning($"   ⚠️ {placeholder} 未找到匹配点位");
                    }
                }
            }

            _logger.LogInfo($"   📊 成功绑定 {expectedBindings} 个占位符");
        }

        /// <summary>
        /// 测试点位搜索功能
        /// </summary>
        private void TestPointSearching()
        {
            _logger.LogInfo("📝 测试3: 点位搜索功能");

            var device = CreateTestDevice();
            var allVariables = device.GetAllVariableNames();

            _logger.LogInfo($"   📊 设备总共有 {allVariables.Count} 个点位变量");

            // 测试点位查找
            var testVariables = new[] { "XS_1101", "AM_1101", "C_AM_1101" };
            foreach (var variable in testVariables)
            {
                var pointData = device.FindPointData(variable);
                if (pointData != null)
                {
                    var description = pointData.GetValueOrDefault("描述信息")?.ToString() ?? 
                                    pointData.GetValueOrDefault("变量描述")?.ToString() ?? "无描述";
                    _logger.LogInfo($"   ✓ 找到点位 {variable}: {description}");
                }
                else
                {
                    _logger.LogWarning($"   ⚠️ 未找到点位: {variable}");
                }
            }
        }

        /// <summary>
        /// 创建测试设备
        /// </summary>
        private Device CreateTestDevice()
        {
            var device = new Device("ESDV1101", "ESDV_CTRL");

            // IO点位
            var ioPoints = new Dictionary<string, Dictionary<string, object>>
            {
                ["XS_1101"] = new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "XS_1101",
                    ["描述信息"] = "阀门开到位反馈",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI"
                },
                ["ZSH_1101"] = new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "ZSH_1101",
                    ["描述信息"] = "阀门开限位",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI"
                },
                ["ZSL_1101"] = new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "ZSL_1101",
                    ["描述信息"] = "阀门关限位",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI"
                }
            };

            foreach (var kvp in ioPoints)
            {
                device.AddIoPoint(kvp.Key, kvp.Value);
            }

            // 设备点位
            var devicePoints = new Dictionary<string, Dictionary<string, object>>
            {
                ["AM_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "AM_1101",
                    ["变量描述"] = "站点阀门切断阀自动状态",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.2"
                },
                ["C_AM_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "C_AM_1101",
                    ["变量描述"] = "站点阀门切断阀命令自动手模切换命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.3"
                },
                ["S_AM_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "S_AM_1101",
                    ["变量描述"] = "站点阀门自动手模切换反馈",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.7"
                },
                ["C_OPEN_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "C_OPEN_1101",
                    ["变量描述"] = "站点阀门切断阀的开命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.5"
                },
                ["C_CLOSE_1101"] = new Dictionary<string, object>
                {
                    ["变量名称"] = "C_CLOSE_1101",
                    ["变量描述"] = "站点阀门切断阀的关命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.4"
                }
            };

            foreach (var kvp in devicePoints)
            {
                device.AddDevicePoint(kvp.Key, kvp.Value);
            }

            return device;
        }
    }
}