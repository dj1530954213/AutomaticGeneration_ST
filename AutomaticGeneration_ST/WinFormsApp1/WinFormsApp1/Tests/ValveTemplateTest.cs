using AutomaticGeneration_ST.Models;
using AutomaticGeneration_ST.Services.Implementations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinFormsApp1;

namespace AutomaticGeneration_ST.Tests
{
    /// <summary>
    /// 阀门模板功能测试类
    /// 验证阀门模板的发现、数据绑定和代码生成功能
    /// </summary>
    public class ValveTemplateTest
    {
        private readonly LogService _logger = LogService.Instance;
        private readonly DeviceTemplateDataBinder _dataBinder;
        private readonly string _templatesPath;

        public ValveTemplateTest()
        {
            _dataBinder = new DeviceTemplateDataBinder();
            _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "阀门");
        }

        /// <summary>
        /// 测试阀门模板文件发现功能
        /// </summary>
        public TestResult TestValveTemplateDiscovery()
        {
            var result = new TestResult
            {
                TestName = "阀门模板发现测试",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInfo("🔍 开始测试阀门模板发现功能...");

                // 1. 检查阀门文件夹是否存在
                if (!Directory.Exists(_templatesPath))
                {
                    result.Success = false;
                    result.Message = $"阀门模板文件夹不存在: {_templatesPath}";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // 2. 检查模板文件
                var expectedTemplates = new[] { "MOV_CTRL.scriban", "ESDV_CTRL.scriban", "PID_CTRL.scriban" };
                var foundTemplates = Directory.GetFiles(_templatesPath, "*.scriban")
                    .Select(Path.GetFileName)
                    .ToList();

                var missingTemplates = expectedTemplates.Except(foundTemplates).ToList();
                if (missingTemplates.Any())
                {
                    result.Success = false;
                    result.Message = $"缺少模板文件: {string.Join(", ", missingTemplates)}";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // 3. 验证模板内容
                foreach (var templateFile in expectedTemplates)
                {
                    var filePath = Path.Combine(_templatesPath, templateFile);
                    var content = File.ReadAllText(filePath);
                    
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        result.Success = false;
                        result.Message = $"模板文件为空: {templateFile}";
                        result.EndTime = DateTime.Now;
                        return result;
                    }

                    // 检查必要的占位符
                    if (!content.Contains("{{device_tag}}"))
                    {
                        result.Success = false;
                        result.Message = $"模板文件缺少device_tag占位符: {templateFile}";
                        result.EndTime = DateTime.Now;
                        return result;
                    }
                }

                result.Success = true;
                result.Message = $"成功发现 {foundTemplates.Count} 个阀门模板文件";
                result.Details = foundTemplates.Select(f => $"✓ {f}").ToList();
                result.EndTime = DateTime.Now;

                _logger.LogSuccess($"✅ 阀门模板发现测试通过");
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"测试执行失败: {ex.Message}";
                result.EndTime = DateTime.Now;
                _logger.LogError($"❌ 阀门模板发现测试失败: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 测试设备模板数据绑定功能
        /// </summary>
        public TestResult TestDeviceDataBinding()
        {
            var result = new TestResult
            {
                TestName = "设备数据绑定测试",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInfo("🔗 开始测试设备数据绑定功能...");

                // 创建测试设备和点位数据
                var testDevice = CreateTestDevice();
                var templateContent = File.ReadAllText(Path.Combine(_templatesPath, "MOV_CTRL.scriban"));

                // 执行数据绑定
                var bindingResult = _dataBinder.BindDeviceTemplateData(testDevice, templateContent);

                // 验证绑定结果
                var validationErrors = new List<string>();

                // 1. 检查device_tag是否正确绑定
                if (!bindingResult.ContainsKey("device_tag") || 
                    bindingResult["device_tag"].ToString() != testDevice.DeviceTag)
                {
                    validationErrors.Add("device_tag绑定失败");
                }

                // 2. 检查其他占位符是否有绑定（根据新的点位结构调整验证）
                var expectedPlaceholders = new[] { "XS", "C_AM", "S_AM", "C_OPEN", "C_CLOSE" };
                foreach (var placeholder in expectedPlaceholders)
                {
                    if (!bindingResult.ContainsKey(placeholder))
                    {
                        validationErrors.Add($"占位符 {placeholder} 未绑定");
                    }
                    else if (bindingResult[placeholder].ToString().StartsWith("<") && 
                             bindingResult[placeholder].ToString().EndsWith(">"))
                    {
                        validationErrors.Add($"占位符 {placeholder} 未找到匹配点位");
                    }
                }

                // 3. 验证设备的新数据结构
                if (testDevice.IoPoints.Count == 0 && testDevice.DevicePoints.Count == 0)
                {
                    validationErrors.Add("设备没有任何点位数据");
                }
                else
                {
                    _logger.LogInfo($"   ✓ 设备有 {testDevice.IoPoints.Count} 个IO点位，{testDevice.DevicePoints.Count} 个设备点位");
                }

                if (validationErrors.Any())
                {
                    result.Success = false;
                    result.Message = "数据绑定验证失败";
                    result.Details = validationErrors;
                }
                else
                {
                    result.Success = true;
                    result.Message = $"成功绑定 {bindingResult.Count} 个占位符";
                    result.Details = bindingResult.Select(kvp => $"✓ {kvp.Key} = {kvp.Value}").ToList();
                }

                result.EndTime = DateTime.Now;
                _logger.LogInfo($"数据绑定测试完成: {(result.Success ? "通过" : "失败")}");
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"测试执行失败: {ex.Message}";
                result.EndTime = DateTime.Now;
                _logger.LogError($"❌ 设备数据绑定测试失败: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// 测试模板语义匹配功能
        /// </summary>
        public TestResult TestSemanticMatching()
        {
            var result = new TestResult
            {
                TestName = "语义匹配测试",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInfo("🧠 开始测试语义匹配功能...");

                // 创建具有语义相关名称的测试设备
                var testDevice = CreateSemanticTestDevice();
                var templateContent = "{{device_tag}} {{XS}} {{C_OPEN}} {{AM}}"; // 简化模板用于测试

                // 执行数据绑定
                var bindingResult = _dataBinder.BindDeviceTemplateData(testDevice, templateContent);

                var matchResults = new List<string>();
                
                // 验证语义匹配结果
                if (bindingResult.ContainsKey("XS"))
                {
                    var xsValue = bindingResult["XS"].ToString();
                    if (xsValue.Contains("OPEN_LIMIT") || xsValue.Contains("开到位"))
                    {
                        matchResults.Add("✓ XS语义匹配成功");
                    }
                    else
                    {
                        matchResults.Add($"⚠️ XS匹配结果: {xsValue}");
                    }
                }

                if (bindingResult.ContainsKey("C_OPEN"))
                {
                    var openValue = bindingResult["C_OPEN"].ToString();
                    if (openValue.Contains("OPEN_CMD") || openValue.Contains("开命令"))
                    {
                        matchResults.Add("✓ C_OPEN语义匹配成功");
                    }
                    else
                    {
                        matchResults.Add($"⚠️ C_OPEN匹配结果: {openValue}");
                    }
                }

                result.Success = matchResults.Any(r => r.StartsWith("✓"));
                result.Message = result.Success ? "语义匹配功能正常" : "语义匹配需要改进";
                result.Details = matchResults;
                result.EndTime = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"测试执行失败: {ex.Message}";
                result.EndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// 创建测试设备（使用新的字典结构）
        /// </summary>
        private Device CreateTestDevice()
        {
            var device = new Device("FV101", "MOV_CTRL");
            
            // 添加IO点位（硬件点位，来自IO表）
            var ioPoints = new[]
            {
                ("FV101_XS_OPEN", new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "FV101_XS_OPEN",
                    ["描述信息"] = "阀门开到位",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI",
                    ["信号类型"] = "数字量",
                    ["序号"] = "1"
                }),
                ("FV101_XS_CLOSE", new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "FV101_XS_CLOSE",
                    ["描述信息"] = "阀门关到位",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI",
                    ["信号类型"] = "数字量",
                    ["序号"] = "2"
                })
            };

            foreach (var (variableName, pointData) in ioPoints)
            {
                device.AddIoPoint(variableName, pointData);
            }
            
            // 添加设备点位（软点位，来自设备表）
            var devicePoints = new[]
            {
                ("C_AM_FV101", new Dictionary<string, object>
                {
                    ["站点名"] = "路灯控制站",
                    ["变量名称"] = "C_AM_FV101",
                    ["变量描述"] = "阀门自动手动切换命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.0",
                    ["MODBUS地址"] = "3400"
                }),
                ("S_AM_FV101", new Dictionary<string, object>
                {
                    ["站点名"] = "路灯控制站",
                    ["变量名称"] = "S_AM_FV101",
                    ["变量描述"] = "阀门自动手动切换反馈",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.1",
                    ["MODBUS地址"] = "3401"
                }),
                ("C_OPEN_FV101", new Dictionary<string, object>
                {
                    ["站点名"] = "路灯控制站",
                    ["变量名称"] = "C_OPEN_FV101",
                    ["变量描述"] = "阀门开命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.2",
                    ["MODBUS地址"] = "3402"
                }),
                ("C_CLOSE_FV101", new Dictionary<string, object>
                {
                    ["站点名"] = "路灯控制站",
                    ["变量名称"] = "C_CLOSE_FV101",
                    ["变量描述"] = "阀门关命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX56.3",
                    ["MODBUS地址"] = "3403"
                })
            };

            foreach (var (variableName, pointData) in devicePoints)
            {
                device.AddDevicePoint(variableName, pointData);
            }

            return device;
        }

        /// <summary>
        /// 创建语义测试设备（使用新的字典结构）
        /// </summary>
        private Device CreateSemanticTestDevice()
        {
            var device = new Device("PV201", "MOV_CTRL");
            
            // 添加具有语义含义的IO点位
            var ioPoints = new[]
            {
                ("PV201_OPEN_LIMIT", new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "PV201_OPEN_LIMIT",
                    ["描述信息"] = "开限位",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI",
                    ["信号类型"] = "数字量"
                }),
                ("PV201_CLOSE_LIMIT", new Dictionary<string, object>
                {
                    ["变量名称（HMI名）"] = "PV201_CLOSE_LIMIT",
                    ["描述信息"] = "关限位",
                    ["数据类型"] = "BOOL",
                    ["模块类型"] = "DI",
                    ["信号类型"] = "数字量"
                })
            };

            foreach (var (variableName, pointData) in ioPoints)
            {
                device.AddIoPoint(variableName, pointData);
            }
            
            // 添加设备点位
            var devicePoints = new[]
            {
                ("PV201_OPEN_CMD", new Dictionary<string, object>
                {
                    ["变量名称"] = "PV201_OPEN_CMD",
                    ["变量描述"] = "开命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX60.0"
                }),
                ("PV201_CLOSE_CMD", new Dictionary<string, object>
                {
                    ["变量名称"] = "PV201_CLOSE_CMD",
                    ["变量描述"] = "关命令",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX60.1"
                }),
                ("PV201_AUTO_MODE", new Dictionary<string, object>
                {
                    ["变量名称"] = "PV201_AUTO_MODE",
                    ["变量描述"] = "自动模式",
                    ["数据类型"] = "BOOL",
                    ["PLC地址"] = "%MX60.2"
                })
            };

            foreach (var (variableName, pointData) in devicePoints)
            {
                device.AddDevicePoint(variableName, pointData);
            }

            return device;
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public List<TestResult> RunAllTests()
        {
            var results = new List<TestResult>();

            _logger.LogInfo("🧪 开始阀门模板功能测试套件...");
            
            results.Add(TestValveTemplateDiscovery());
            results.Add(TestDeviceDataBinding());
            results.Add(TestSemanticMatching());

            var passedCount = results.Count(r => r.Success);
            var totalCount = results.Count;

            _logger.LogInfo($"🎯 测试完成: {passedCount}/{totalCount} 通过");
            
            if (passedCount == totalCount)
            {
                _logger.LogSuccess("✅ 所有阀门模板功能测试通过！");
            }
            else
            {
                _logger.LogWarning($"⚠️ {totalCount - passedCount} 个测试未通过，需要检查相关功能");
            }

            return results;
        }
    }
}