using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WinFormsApp1.Utils
{
    public static class ChannelConverter
    {
        private static readonly LogService logger = LogService.Instance;
        
        // 缓存正则表达式以提高性能
        private static readonly Regex LegacyChannelPattern = new Regex(@"^(\d+)_(\d+)_(AI|AO|DI|DO)_(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PlcMemoryPattern = new Regex(@"^%M([DX])(\d+)(?:\.(\d+))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        // PLC地址配置映射表
        private static readonly Dictionary<string, PlcAddressConfig> AddressConfigs = new Dictionary<string, PlcAddressConfig>(StringComparer.OrdinalIgnoreCase)
        {
            // AI类型 - 使用%MDxxx格式
            { "AI", new PlcAddressConfig { BaseAddress = 320, ChannelSize = 24, Rack = 2, Slot = 1, BaseChannel = 1 } },
            // AO类型 - 使用%MDxxx格式  
            { "AO", new PlcAddressConfig { BaseAddress = 896, ChannelSize = 4, Rack = 2, Slot = 2, BaseChannel = 1 } },
            // DI类型 - 使用%MXxx.y格式
            { "DI", new PlcAddressConfig { BaseAddress = 25, ChannelSize = 1, Rack = 2, Slot = 3, BaseChannel = 0 } },
            // DO类型 - 使用%MXxx.y格式
            { "DO", new PlcAddressConfig { BaseAddress = 26, ChannelSize = 1, Rack = 2, Slot = 4, BaseChannel = 0 } }
        };
        
        /// <summary>
        /// 将通道位号或PLC地址转换为硬点通道号
        /// 支持两种格式：
        /// 1. 传统格式: 1_1_AI_0 -> DPIO_2_1_2_1
        /// </summary>
        public static string ConvertToHardChannel(string channelPosition)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(channelPosition))
                {
                    logger.LogWarning("通道位号为空，使用默认值");
                    return "DPIO_2_1_2_1";
                }
                
                var trimmed = channelPosition.Trim();
                
                // 首先尝试解析PLC地址格式
                var plcResult = TryConvertPlcAddress(trimmed);
                if (plcResult.IsSuccess)
                {
                    logger.LogDebug($"PLC地址转换成功: {trimmed} -> {plcResult.HardChannel}");
                    return plcResult.HardChannel;
                }
                
                // 如果PLC地址解析失败，尝试传统格式
                var legacyResult = TryConvertLegacyChannel(trimmed);
                if (legacyResult.IsSuccess)
                {
                    logger.LogDebug($"传统格式转换成功: {trimmed} -> {legacyResult.HardChannel}");
                    return legacyResult.HardChannel;
                }
                
                // 两种格式都失败，记录详细信息并返回默认值
                logger.LogWarning($"通道位号格式不识别: '{trimmed}'，支持格式: 机架_槽_类型_通道 (如 1_1_AI_0) 或 PLC地址 (如 %MD320, %MX25.0)，使用默认值");
                return "DPIO_2_1_2_1";
            }
            catch (Exception ex)
            {
                logger.LogError($"通道位号转换失败: {channelPosition}, 错误: {ex.Message}");
                return "DPIO_2_1_2_1";
            }
        }
        
        /// <summary>
        /// 尝试转换PLC地址格式
        /// 支持 %MDxxx (AI/AO) 和 %MXxx.y (DI/DO) 格式
        /// </summary>
        private static ConversionResult TryConvertPlcAddress(string plcAddress)
        {
            var match = PlcMemoryPattern.Match(plcAddress);
            if (!match.Success)
            {
                return ConversionResult.CreateFailed();
            }
            
            var memoryType = match.Groups[1].Value.ToUpper(); // D 或 X
            var address = int.Parse(match.Groups[2].Value);
            var bitPosition = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : -1;
            
            // 根据地址范围和格式推断点位类型
            var pointType = InferPointType(memoryType, address, bitPosition);
            if (string.IsNullOrEmpty(pointType))
            {
                logger.LogWarning($"无法推断PLC地址的点位类型: {plcAddress}");
                return ConversionResult.CreateFailed();
            }
            
            if (!AddressConfigs.TryGetValue(pointType, out var config))
            {
                logger.LogWarning($"未配置的点位类型: {pointType}");
                return ConversionResult.CreateFailed();
            }
            
            // 计算通道号
            var channelIndex = CalculateChannelIndex(memoryType, address, bitPosition, config);
            var hardChannel = $"DPIO_{config.Rack}_{config.Slot}_{config.BaseChannel}_{channelIndex}";
            
            return ConversionResult.CreateSuccess(hardChannel);
        }
        
        /// <summary>
        /// 尝试转换传统通道格式 (向后兼容)
        /// </summary>
        private static ConversionResult TryConvertLegacyChannel(string channelPosition)
        {
            var match = LegacyChannelPattern.Match(channelPosition);
            if (!match.Success)
            {
                return ConversionResult.CreateFailed();
            }
            
            // 提取各部分
            if (!int.TryParse(match.Groups[1].Value, out var rack) ||
                !int.TryParse(match.Groups[2].Value, out var slot) ||
                !int.TryParse(match.Groups[4].Value, out var channel))
            {
                return ConversionResult.CreateFailed();
            }
            
            // 应用传统转换规则
            //var hardRack = rack + 1;      // 机架号+1
            //var hardSlot = slot + 2;      // 槽号+1  
            //var hardChannel = channel + 1; // 通道号+1

            /*能处理和利时多机架且固定为11槽的机架，后续需要结合敖果点表软件生成的硬件信息表来做适配*/
            int hardRack = 2;
            int hardSlot = (rack - 1) * 10 + slot + 2;
            int hardChannel = channel + 1;

            var result = $"DPIO_{hardRack}_1_{hardSlot}_{hardChannel}";
            return ConversionResult.CreateSuccess(result);
        }
        
        /// <summary>
        /// 根据PLC地址推断点位类型
        /// </summary>
        private static string InferPointType(string memoryType, int address, int bitPosition)
        {
            if (memoryType == "D")
            {
                // %MDxxx 格式用于模拟量
                if (address >= 320 && address < 896) return "AI";  // AI范围: %MD320-895
                if (address >= 896) return "AO";                   // AO范围: %MD896+
            }
            else if (memoryType == "X")
            {
                // %MXxx.y 格式用于数字量  
                if (address == 25) return "DI";  // DI: %MX25.x
                if (address == 26) return "DO";  // DO: %MX26.x
            }
            
            return "";
        }
        
        /// <summary>
        /// 计算通道索引
        /// </summary>
        private static int CalculateChannelIndex(string memoryType, int address, int bitPosition, PlcAddressConfig config)
        {
            if (memoryType == "D")
            {
                // 模拟量通道计算：基于地址偏移和通道大小
                var offset = address - config.BaseAddress;
                return (offset / config.ChannelSize) + 1;  // 从1开始编号
            }
            else if (memoryType == "X")
            {
                // 数字量通道计算：直接使用位位置
                return bitPosition + 1;  // 从1开始编号
            }
            
            return 1;  // 默认通道号
        }
        
        /// <summary>
        /// 验证通道位号格式是否正确 (支持两种格式)
        /// </summary>
        public static bool IsValidChannelPosition(string channelPosition)
        {
            if (string.IsNullOrWhiteSpace(channelPosition))
                return false;
                
            var trimmed = channelPosition.Trim();
            
            // 检查PLC地址格式
            if (PlcMemoryPattern.IsMatch(trimmed))
                return true;
                
            // 检查传统格式
            return LegacyChannelPattern.IsMatch(trimmed);
        }
        
        /// <summary>
        /// 获取通道位号的类型部分
        /// </summary>
        public static string GetChannelType(string channelPosition)
        {
            if (string.IsNullOrWhiteSpace(channelPosition))
                return "";
                
            var trimmed = channelPosition.Trim();
            
            // 尝试从PLC地址推断类型
            var plcMatch = PlcMemoryPattern.Match(trimmed);
            if (plcMatch.Success)
            {
                var memoryType = plcMatch.Groups[1].Value.ToUpper();
                var address = int.Parse(plcMatch.Groups[2].Value);
                var bitPosition = plcMatch.Groups[3].Success ? int.Parse(plcMatch.Groups[3].Value) : -1;
                return InferPointType(memoryType, address, bitPosition);
            }
            
            // 尝试从传统格式获取类型
            var legacyMatch = LegacyChannelPattern.Match(trimmed);
            return legacyMatch.Success ? legacyMatch.Groups[3].Value.ToUpper() : "";
        }
        
        /// <summary>
        /// 获取详细的转换信息（用于调试和验证）
        /// </summary>
        public static ConversionInfo GetConversionInfo(string channelPosition)
        {
            if (string.IsNullOrWhiteSpace(channelPosition))
            {
                return new ConversionInfo
                {
                    IsValid = false,
                    OriginalInput = channelPosition,
                    ErrorMessage = "输入为空"
                };
            }
            
            var trimmed = channelPosition.Trim();
            
            // 尝试PLC地址格式
            var plcMatch = PlcMemoryPattern.Match(trimmed);
            if (plcMatch.Success)
            {
                var memoryType = plcMatch.Groups[1].Value.ToUpper();
                var address = int.Parse(plcMatch.Groups[2].Value);
                var bitPosition = plcMatch.Groups[3].Success ? int.Parse(plcMatch.Groups[3].Value) : -1;
                var pointType = InferPointType(memoryType, address, bitPosition);
                
                return new ConversionInfo
                {
                    IsValid = !string.IsNullOrEmpty(pointType),
                    OriginalInput = trimmed,
                    DetectedFormat = "PLC地址",
                    PointType = pointType,
                    MemoryType = memoryType,
                    Address = address,
                    BitPosition = bitPosition,
                    HardChannel = !string.IsNullOrEmpty(pointType) ? ConvertToHardChannel(trimmed) : null,
                    ErrorMessage = string.IsNullOrEmpty(pointType) ? "无法识别的PLC地址范围" : null
                };
            }
            
            // 尝试传统格式
            var legacyMatch = LegacyChannelPattern.Match(trimmed);
            if (legacyMatch.Success)
            {
                return new ConversionInfo
                {
                    IsValid = true,
                    OriginalInput = trimmed,
                    DetectedFormat = "传统格式",
                    PointType = legacyMatch.Groups[3].Value.ToUpper(),
                    Rack = int.Parse(legacyMatch.Groups[1].Value),
                    Slot = int.Parse(legacyMatch.Groups[2].Value),
                    Channel = int.Parse(legacyMatch.Groups[4].Value),
                    HardChannel = ConvertToHardChannel(trimmed)
                };
            }
            
            return new ConversionInfo
            {
                IsValid = false,
                OriginalInput = trimmed,
                ErrorMessage = "不支持的格式，支持: 机架_槽_类型_通道 或 %MDxxx/%MXxx.y"
            };
        }
    }
    
    /// <summary>
    /// PLC地址配置
    /// </summary>
    public class PlcAddressConfig
    {
        public int BaseAddress { get; set; }    // 基础地址
        public int ChannelSize { get; set; }    // 通道大小（字节）
        public int Rack { get; set; }           // 机架号
        public int Slot { get; set; }           // 槽号
        public int BaseChannel { get; set; }    // 基础通道号
    }
    
    /// <summary>
    /// 转换结果
    /// </summary>
    public class ConversionResult
    {
        public bool IsSuccess { get; set; }
        public string HardChannel { get; set; } = "";
        
        public static ConversionResult CreateSuccess(string hardChannel) => new ConversionResult { IsSuccess = true, HardChannel = hardChannel };
        public static ConversionResult CreateFailed() => new ConversionResult { IsSuccess = false };
    }
    
    /// <summary>
    /// 详细转换信息
    /// </summary>
    public class ConversionInfo
    {
        public bool IsValid { get; set; }
        public string OriginalInput { get; set; } = "";
        public string? DetectedFormat { get; set; }
        public string? PointType { get; set; }
        public string? MemoryType { get; set; }
        public int? Address { get; set; }
        public int? BitPosition { get; set; }
        public int? Rack { get; set; }
        public int? Slot { get; set; }
        public int? Channel { get; set; }
        public string? HardChannel { get; set; }
        public string? ErrorMessage { get; set; }
    }
}