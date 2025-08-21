using System;
using System.Collections.Generic;
using System.Linq;
using WinFormsApp1.Models;
using Scriban;

namespace WinFormsApp1.Generators
{
    /// <summary>
    /// TCP通讯代码生成器
    /// </summary>
    public class TcpCodeGenerator : IPointGenerator
    {
        private readonly Dictionary<string, Scriban.Template> _templates;

        public TcpCodeGenerator(Dictionary<string, Scriban.Template> templates)
        {
            _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        }

        public string PointType => "TCP";

        public string Generate(Dictionary<string, object> row)
        {
            // 从行数据确定数据类型
            var dataType = GetValue<string>(row, "数据类型")?.ToUpper() ?? "";

            if (dataType == "BOOL")
            {
                var digitalPoint = CreateTcpDigitalPointFromRow(row);
                return GenerateTcpDigitalCode(new List<TcpDigitalPoint> { digitalPoint });
            }
            else if (dataType is "REAL" or "INT" or "DINT")
            {
                var analogPoint = CreateTcpAnalogPointFromRow(row);
                return GenerateTcpAnalogCode(new List<TcpAnalogPoint> { analogPoint });
            }

            return $"// 不支持的数据类型: {dataType}\n";
        }

        public bool CanGenerate(Dictionary<string, object> row)
        {
            var dataType = GetValue<string>(row, "数据类型")?.ToUpper() ?? "";
            return dataType is "BOOL" or "REAL" or "INT" or "DINT";
        }

        /// <summary>
        /// 生成TCP模拟量通讯代码
        /// </summary>
        public string GenerateTcpAnalogCode(List<TcpAnalogPoint> points)
        {
            if (!points?.Any() == true)
            {
                Console.WriteLine("[INFO] 无TCP模拟量点位数据");
                return "// 无TCP模拟量点位数据\n";
            }

            // 尝试使用TCP专用模板，如果没有则使用现有ANALOG模板
            Scriban.Template template = null;
            string templateName = "";

            if (_templates.TryGetValue("TCP_ANALOG", out template))
            {
                templateName = "TCP_ANALOG";
            }
            else if (_templates.TryGetValue("ANALOG", out template))
            {
                templateName = "ANALOG";
                Console.WriteLine("[INFO] 使用现有ANALOG模板生成TCP模拟量代码");
            }
            else
            {
                throw new InvalidOperationException("未找到TCP模拟量模板");
            }

            // 为每个点位单独生成代码，然后合并
            var generatedCodes = new List<string>();
            
            foreach (var p in points)
            {
                // 为单个点位准备模板数据
                var templateData = new
                {
                    point = new
                    {
                        // 现有模板使用的字段名
                        hmi_tag_name = p.HmiTagName,
                        DESCRIBE = p.Description,
                        DATA_TYPE = p.DataType,
                        CHANNEL = p.Channel,
                        BYTE_ORDER = p.ByteOrder,
                        TYPE_NUMBER = p.TypeNumber,
                        SCALE = p.Scale,

                        // snake_case aliases for Scriban default renamer
                        data_type = p.DataType?.Trim().ToUpper(),
                        channel = p.Channel,
                        byte_order = p.ByteOrder,
                        type_number = p.TypeNumber,
                        scale = p.Scale,
                        
                        // 报警相关字段
                        shh_value = p.ShhValue?.ToString() ?? "",
                        sh_value = p.ShValue?.ToString() ?? "",
                        sl_value = p.SlValue?.ToString() ?? "",
                        sll_value = p.SllValue?.ToString() ?? "",
                        
                        // 维护相关字段
                        MaintenanceStatusTag = p.MaintenanceStatusTag,
                        MaintenanceValueTag = p.MaintenanceValueTag
                    }
                };

                try
                {
                    string pointCode = template.Render(templateData);
                    Console.WriteLine($"[DEBUG] 模拟量点位 {p.HmiTagName} 渲染结果长度: {pointCode.Length} 字符");
                    Console.WriteLine($"[DEBUG] 渲染内容预览: {pointCode.Substring(0, Math.Min(200, pointCode.Length))}...");
                    generatedCodes.Add(pointCode);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"TCP模拟量模板渲染失败 ({p.HmiTagName}): {ex.Message}", ex);
                }
            }

            Console.WriteLine($"[INFO] TCP模拟量代码生成成功，使用模板: {templateName}");
            return string.Join(Environment.NewLine + Environment.NewLine, generatedCodes);
        }

        /// <summary>
        /// 生成TCP数字量通讯代码
        /// </summary>
        public string GenerateTcpDigitalCode(List<TcpDigitalPoint> points)
        {
            if (!points?.Any() == true)
            {
                Console.WriteLine("[INFO] 无TCP数字量点位数据");
                return "// 无TCP数字量点位数据\n";
            }

            // 尝试使用TCP专用模板，如果没有则使用现有DIGITAL模板
            Scriban.Template template = null;
            string templateName = "";

            if (_templates.TryGetValue("TCP_DIGITAL", out template))
            {
                templateName = "TCP_DIGITAL";
            }
            else if (_templates.TryGetValue("DIGITAL", out template))
            {
                templateName = "DIGITAL";
                Console.WriteLine("[INFO] 使用现有DIGITAL模板生成TCP数字量代码");
            }
            else
            {
                throw new InvalidOperationException("未找到TCP数字量模板");
            }

            // 为每个点位单独生成代码，然后合并
            var generatedCodes = new List<string>();
            
            foreach (var p in points)
            {
                // 为单个点位准备模板数据
                var templateData = new
                {
                    point = new
                    {
                        hmi_tag_name = p.HmiTagName,
                        DESCRIBE = p.Description,
                        CHANNEL = p.Channel,
                        InitialState = p.InitialState,
                        BitAddress = p.BitAddress
                    }
                };

                try
                {
                    string pointCode = template.Render(templateData);
                    Console.WriteLine($"[DEBUG] 数字量点位 {p.HmiTagName} 渲染结果长度: {pointCode.Length} 字符");
                    Console.WriteLine($"[DEBUG] 渲染内容预览: {pointCode.Substring(0, Math.Min(200, pointCode.Length))}...");
                    generatedCodes.Add(pointCode);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"TCP数字量模板渲染失败 ({p.HmiTagName}): {ex.Message}", ex);
                }
            }

            Console.WriteLine($"[INFO] TCP数字量代码生成成功，使用模板: {templateName}");
            return string.Join(Environment.NewLine + Environment.NewLine, generatedCodes);
        }

        #region IPointGenerator Implementation

        public string GenerateCode<T>(List<T> points) where T : class
        {
            if (typeof(T) == typeof(TcpAnalogPoint) && points is List<TcpAnalogPoint> analogPoints)
            {
                return GenerateTcpAnalogCode(analogPoints);
            }
            else if (typeof(T) == typeof(TcpDigitalPoint) && points is List<TcpDigitalPoint> digitalPoints)
            {
                return GenerateTcpDigitalCode(digitalPoints);
            }
            else
            {
                throw new NotSupportedException($"不支持的TCP点位类型: {typeof(T).Name}");
            }
        }

        public bool CanGenerate<T>() where T : class
        {
            return typeof(T) == typeof(TcpAnalogPoint) || typeof(T) == typeof(TcpDigitalPoint);
        }

        #endregion

        #region Helper Methods

        private TcpAnalogPoint CreateTcpAnalogPointFromRow(Dictionary<string, object> row)
        {
            return new TcpAnalogPoint
            {
                HmiTagName = GetValue<string>(row, "变量名称（HMI）") ?? "",
                Description = GetValue<string>(row, "变量描述") ?? GetValue<string>(row, "描述") ?? "",
                DataType = GetValue<string>(row, "数据类型") ?? "",
                Channel = GetValue<string>(row, "起始TCP通道名称") ?? GetValue<string>(row, "CHANNEL") ?? "",
                Scale = GetValue<double?>(row, "缩放倍数") ?? GetValue<double?>(row, "缩放因子"),
                ShhValue = GetValue<double?>(row, "SHH值") ?? GetValue<double?>(row, "shh_value"),
                ShValue = GetValue<double?>(row, "SH值") ?? GetValue<double?>(row, "sh_value"),
                SlValue = GetValue<double?>(row, "SL值") ?? GetValue<double?>(row, "sl_value"),
                SllValue = GetValue<double?>(row, "SLL值") ?? GetValue<double?>(row, "sll_value"),
                ByteOrder = GetValue<string>(row, "BYTE_ORDER"),
                TypeNumber = GetValue<int?>(row, "TYPE_NUMBER")
            };
        }

        private TcpDigitalPoint CreateTcpDigitalPointFromRow(Dictionary<string, object> row)
        {
            return new TcpDigitalPoint
            {
                HmiTagName = GetValue<string>(row, "变量名称（HMI）") ?? "",
                Description = GetValue<string>(row, "变量描述") ?? GetValue<string>(row, "描述") ?? "",
                DataType = GetValue<string>(row, "数据类型") ?? "",
                Channel = GetValue<string>(row, "起始TCP通道名称") ?? GetValue<string>(row, "CHANNEL") ?? "",
                InitialState = GetValue<bool>(row, "初始状态") || GetValue<bool>(row, "默认值"),
                BitAddress = GetValue<int>(row, "位地址")
            };
        }

        private T GetValue<T>(Dictionary<string, object> row, string columnName)
        {
            if (row.TryGetValue(columnName, out var value) && value != null)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)(value?.ToString() ?? "");
                    
                    if (typeof(T) == typeof(double?) || typeof(T) == typeof(double))
                    {
                        if (double.TryParse(value.ToString(), out double doubleVal))
                            return (T)(object)doubleVal;
                        return default(T);
                    }
                    
                    if (typeof(T) == typeof(int?) || typeof(T) == typeof(int))
                    {
                        if (int.TryParse(value.ToString(), out int intVal))
                            return (T)(object)intVal;
                        return default(T);
                    }
                    
                    if (typeof(T) == typeof(bool))
                    {
                        if (bool.TryParse(value.ToString(), out bool boolVal))
                            return (T)(object)boolVal;
                        return default(T);
                    }

                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }
            return default(T);
        }

        #endregion
    }
}