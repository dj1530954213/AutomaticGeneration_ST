using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinFormsApp1.Config
{
    /// <summary>
    /// 字段映射配置 - 支持Excel列名自定义别名和映射规则
    /// </summary>
    public class FieldMappingConfiguration
    {
        #region 数据结构定义

        /// <summary>
        /// 字段映射规则
        /// </summary>
        public class FieldMapping
        {
            /// <summary>
            /// 标准字段名（系统内部使用）
            /// </summary>
            [JsonPropertyName("standardField")]
            public string StandardField { get; set; } = "";

            /// <summary>
            /// 显示名称（用户界面显示）
            /// </summary>
            [JsonPropertyName("displayName")]
            public string DisplayName { get; set; } = "";

            /// <summary>
            /// Excel列名别名列表（支持多个别名）
            /// </summary>
            [JsonPropertyName("aliases")]
            public List<string> Aliases { get; set; } = new();

            /// <summary>
            /// 是否必需字段
            /// </summary>
            [JsonPropertyName("required")]
            public bool Required { get; set; } = false;

            /// <summary>
            /// 字段类型
            /// </summary>
            [JsonPropertyName("fieldType")]
            public FieldType FieldType { get; set; } = FieldType.String;

            /// <summary>
            /// 默认值
            /// </summary>
            [JsonPropertyName("defaultValue")]
            public string DefaultValue { get; set; } = "";

            /// <summary>
            /// 验证规则
            /// </summary>
            [JsonPropertyName("validationRule")]
            public string ValidationRule { get; set; } = "";

            /// <summary>
            /// 字段描述
            /// </summary>
            [JsonPropertyName("description")]
            public string Description { get; set; } = "";

            /// <summary>
            /// 转换规则（用于数据格式转换）
            /// </summary>
            [JsonPropertyName("transformRule")]
            public TransformRule? TransformRule { get; set; }
        }

        /// <summary>
        /// 字段类型枚举
        /// </summary>
        public enum FieldType
        {
            String,     // 字符串
            Integer,    // 整数
            Double,     // 浮点数
            Boolean,    // 布尔值
            DateTime,   // 日期时间
            Enum        // 枚举
        }

        /// <summary>
        /// 数据转换规则
        /// </summary>
        public class TransformRule
        {
            /// <summary>
            /// 转换类型
            /// </summary>
            [JsonPropertyName("type")]
            public TransformType Type { get; set; } = TransformType.None;

            /// <summary>
            /// 转换参数
            /// </summary>
            [JsonPropertyName("parameters")]
            public Dictionary<string, string> Parameters { get; set; } = new();
        }

        /// <summary>
        /// 转换类型枚举
        /// </summary>
        public enum TransformType
        {
            None,           // 无转换
            Trim,           // 去除空白
            ToUpper,        // 转换为大写
            ToLower,        // 转换为小写
            Replace,        // 字符串替换
            Regex,          // 正则表达式转换
            Mapping,        // 值映射转换
            Formula         // 公式计算
        }

        /// <summary>
        /// 映射配置集合
        /// </summary>
        public class MappingCollection
        {
            /// <summary>
            /// 配置名称
            /// </summary>
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            /// <summary>
            /// 配置描述
            /// </summary>
            [JsonPropertyName("description")]
            public string Description { get; set; } = "";

            /// <summary>
            /// 版本号
            /// </summary>
            [JsonPropertyName("version")]
            public string Version { get; set; } = "1.0";

            /// <summary>
            /// 创建时间
            /// </summary>
            [JsonPropertyName("createdDate")]
            public DateTime CreatedDate { get; set; } = DateTime.Now;

            /// <summary>
            /// 最后修改时间
            /// </summary>
            [JsonPropertyName("lastModified")]
            public DateTime LastModified { get; set; } = DateTime.Now;

            /// <summary>
            /// 字段映射列表
            /// </summary>
            [JsonPropertyName("fieldMappings")]
            public List<FieldMapping> FieldMappings { get; set; } = new();

            /// <summary>
            /// 工作表映射（支持不同工作表使用不同映射）
            /// </summary>
            [JsonPropertyName("worksheetMappings")]
            public Dictionary<string, List<string>> WorksheetMappings { get; set; } = new();
        }

        #endregion

        #region 静态属性和方法

        private static MappingCollection? _currentMapping;
        private static readonly string _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "STGenerator", "field-mapping.json");

        /// <summary>
        /// 获取当前映射配置
        /// </summary>
        public static MappingCollection CurrentMapping => _currentMapping ??= LoadDefaultMapping();

        /// <summary>
        /// 加载默认映射配置
        /// </summary>
        public static MappingCollection LoadDefaultMapping()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var mapping = JsonSerializer.Deserialize<MappingCollection>(json);
                    if (mapping != null)
                    {
                        return mapping;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载字段映射配置失败: {ex.Message}");
            }

            // 返回默认配置
            return CreateDefaultMapping();
        }

        /// <summary>
        /// 创建默认映射配置
        /// </summary>
        public static MappingCollection CreateDefaultMapping()
        {
            var mapping = new MappingCollection
            {
                Name = "默认字段映射",
                Description = "ST自动生成器默认字段映射配置",
                Version = "1.0"
            };

            // 添加标准字段映射
            mapping.FieldMappings.AddRange(new[]
            {
                new FieldMapping
                {
                    StandardField = "HmiTagName",
                    DisplayName = "变量名称（HMI）",
                    Aliases = new List<string> { "变量名称（HMI）", "HMI标签名", "变量名", "TagName", "HMI_TAG_NAME" },
                    Required = true,
                    FieldType = FieldType.String,
                    Description = "HMI系统中使用的变量标签名称"
                },
                new FieldMapping
                {
                    StandardField = "Description",
                    DisplayName = "工艺描述",
                    Aliases = new List<string> { "工艺描述", "描述", "工艺名称", "Description", "DESCRIPTION" },
                    Required = true,
                    FieldType = FieldType.String,
                    Description = "点位的工艺描述信息"
                },
                new FieldMapping
                {
                    StandardField = "PointType",
                    DisplayName = "点类型",
                    Aliases = new List<string> { "点类型", "类型", "Type", "POINT_TYPE", "IO类型" },
                    Required = true,
                    FieldType = FieldType.String,
                    Description = "点位类型：AI、AO、DI、DO"
                },
                new FieldMapping
                {
                    StandardField = "ModuleType",
                    DisplayName = "模块类型",
                    Aliases = new List<string> { "模块类型", "模块", "Module", "MODULE_TYPE" },
                    Required = true,
                    FieldType = FieldType.String,
                    Description = "硬件模块类型"
                },
                new FieldMapping
                {
                    StandardField = "Channel",
                    DisplayName = "通道",
                    Aliases = new List<string> { "通道", "通道号", "Channel", "CHANNEL", "CH" },
                    Required = true,
                    FieldType = FieldType.Integer,
                    Description = "硬件通道号"
                },
                new FieldMapping
                {
                    StandardField = "Unit",
                    DisplayName = "单位",
                    Aliases = new List<string> { "单位", "工程单位", "Unit", "UNIT" },
                    Required = false,
                    FieldType = FieldType.String,
                    DefaultValue = "",
                    Description = "测量值的工程单位"
                },
                new FieldMapping
                {
                    StandardField = "RangeMin",
                    DisplayName = "量程下限",
                    Aliases = new List<string> { "量程下限", "下限", "最小值", "Min", "RANGE_MIN", "量程低限" },
                    Required = false,
                    FieldType = FieldType.Double,
                    DefaultValue = "0.0",
                    Description = "测量范围的下限值"
                },
                new FieldMapping
                {
                    StandardField = "RangeMax",
                    DisplayName = "量程上限",
                    Aliases = new List<string> { "量程上限", "上限", "最大值", "Max", "RANGE_MAX", "量程高限" },
                    Required = false,  
                    FieldType = FieldType.Double,
                    DefaultValue = "100.0",
                    Description = "测量范围的上限值"
                },
                new FieldMapping
                {
                    StandardField = "AlarmHH",
                    DisplayName = "高高报警",
                    Aliases = new List<string> { "高高报警", "HH报警", "SHH值", "HH", "ALARM_HH" },
                    Required = false,
                    FieldType = FieldType.Double,
                    Description = "高高报警设定值"
                },
                new FieldMapping
                {
                    StandardField = "AlarmH",
                    DisplayName = "高报警",
                    Aliases = new List<string> { "高报警", "H报警", "SH值", "H", "ALARM_H" },
                    Required = false,
                    FieldType = FieldType.Double,
                    Description = "高报警设定值"
                },
                new FieldMapping
                {
                    StandardField = "AlarmL",
                    DisplayName = "低报警",
                    Aliases = new List<string> { "低报警", "L报警", "SL值", "L", "ALARM_L" },
                    Required = false,
                    FieldType = FieldType.Double,
                    Description = "低报警设定值"
                },
                new FieldMapping
                {
                    StandardField = "AlarmLL",
                    DisplayName = "低低报警",
                    Aliases = new List<string> { "低低报警", "LL报警", "SLL值", "LL", "ALARM_LL" },
                    Required = false,
                    FieldType = FieldType.Double,
                    Description = "低低报警设定值"
                },
                new FieldMapping
                {
                    StandardField = "PlcAddress",
                    DisplayName = "PLC绝对地址",
                    Aliases = new List<string> { "PLC绝对地址", "PLC地址", "地址", "Address", "PLC_ADDRESS" },
                    Required = false,
                    FieldType = FieldType.String,
                    Description = "PLC中的绝对地址"
                },
                new FieldMapping
                {
                    StandardField = "Position",
                    DisplayName = "位号",
                    Aliases = new List<string> { "位号", "标识符", "Position", "TAG", "POS" },
                    Required = false,
                    FieldType = FieldType.String,
                    Description = "设备位号标识"
                }
            });

            return mapping;
        }

        /// <summary>
        /// 保存映射配置
        /// </summary>
        public static bool SaveMapping(MappingCollection mapping)
        {
            try
            {
                mapping.LastModified = DateTime.Now;
                
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(mapping, options);
                File.WriteAllText(_configFilePath, json);

                _currentMapping = mapping;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存字段映射配置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据Excel列名查找对应的标准字段
        /// </summary>
        public static string? FindStandardField(string excelColumnName)
        {
            if (string.IsNullOrWhiteSpace(excelColumnName))
                return null;

            var trimmedName = excelColumnName.Trim();
            
            foreach (var mapping in CurrentMapping.FieldMappings)
            {
                // 检查显示名称
                if (string.Equals(mapping.DisplayName, trimmedName, StringComparison.OrdinalIgnoreCase))
                    return mapping.StandardField;

                // 检查别名列表
                if (mapping.Aliases.Any(alias => 
                    string.Equals(alias, trimmedName, StringComparison.OrdinalIgnoreCase)))
                    return mapping.StandardField;
            }

            return null;
        }

        /// <summary>
        /// 获取字段映射信息
        /// </summary>
        public static FieldMapping? GetFieldMapping(string standardField)
        {
            return CurrentMapping.FieldMappings
                .FirstOrDefault(m => string.Equals(m.StandardField, standardField, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 添加或更新字段映射
        /// </summary>
        public static void AddOrUpdateMapping(FieldMapping mapping)
        {
            var existing = CurrentMapping.FieldMappings
                .FirstOrDefault(m => string.Equals(m.StandardField, mapping.StandardField, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // 更新现有映射
                var index = CurrentMapping.FieldMappings.IndexOf(existing);
                CurrentMapping.FieldMappings[index] = mapping;
            }
            else
            {
                // 添加新映射
                CurrentMapping.FieldMappings.Add(mapping);
            }

            SaveMapping(CurrentMapping);
        }

        /// <summary>
        /// 验证必需字段是否都有映射
        /// </summary>
        public static List<string> ValidateRequiredFields(IEnumerable<string> availableColumns)
        {
            var missingFields = new List<string>();
            var columnList = availableColumns.ToList();

            foreach (var mapping in CurrentMapping.FieldMappings.Where(m => m.Required))
            {
                var found = columnList.Any(col => 
                    string.Equals(mapping.DisplayName, col?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    mapping.Aliases.Any(alias => 
                        string.Equals(alias, col?.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (!found)
                {
                    missingFields.Add(mapping.DisplayName);
                }
            }

            return missingFields;
        }

        #endregion
    }
}