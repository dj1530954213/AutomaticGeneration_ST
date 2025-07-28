using System;
using System.Text.RegularExpressions;

namespace WinFormsApp1.Utils
{
    public static class ChannelConverter
    {
        private static readonly LogService logger = LogService.Instance;
        
        /// <summary>
        /// 将通道位号转换为硬点通道号
        /// 例如: 1_1_AI_0 -> DPIO_2_1_2_1
        /// 规则: 机架号+1, 槽号+1, 通道号+1
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
                
                // 匹配格式: 机架_槽_类型_通道  例如: 1_1_AI_0
                var pattern = @"^(\d+)_(\d+)_(AI|AO|DI|DO)_(\d+)$";
                var match = Regex.Match(channelPosition.Trim(), pattern, RegexOptions.IgnoreCase);
                
                if (!match.Success)
                {
                    logger.LogWarning($"通道位号格式不正确: {channelPosition}，使用默认值");
                    return "DPIO_2_1_2_1";
                }
                
                // 提取各部分
                var rackStr = match.Groups[1].Value;
                var slotStr = match.Groups[2].Value;
                var typeStr = match.Groups[3].Value.ToUpper();
                var channelStr = match.Groups[4].Value;
                
                // 转换为数字并按规则处理
                if (!int.TryParse(rackStr, out var rack) ||
                    !int.TryParse(slotStr, out var slot) ||
                    !int.TryParse(channelStr, out var channel))
                {
                    logger.LogWarning($"通道位号数字解析失败: {channelPosition}，使用默认值");
                    return "DPIO_2_1_2_1";
                }
                
                // 应用转换规则
                var hardRack = rack + 1;      // 机架号+1
                var hardSlot = slot + 1;      // 槽号+1  
                var hardChannel = channel + 1; // 通道号+1
                
                var result = $"DPIO_{hardRack}_1_{hardSlot}_{hardChannel}";
                
                logger.LogDebug($"通道转换: {channelPosition} -> {result}");
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError($"通道位号转换失败: {channelPosition}, 错误: {ex.Message}");
                return "DPIO_2_1_2_1";
            }
        }
        
        /// <summary>
        /// 验证通道位号格式是否正确
        /// </summary>
        public static bool IsValidChannelPosition(string channelPosition)
        {
            if (string.IsNullOrWhiteSpace(channelPosition))
                return false;
                
            var pattern = @"^(\d+)_(\d+)_(AI|AO|DI|DO)_(\d+)$";
            return Regex.IsMatch(channelPosition.Trim(), pattern, RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// 获取通道位号的类型部分
        /// </summary>
        public static string GetChannelType(string channelPosition)
        {
            if (string.IsNullOrWhiteSpace(channelPosition))
                return "";
                
            var pattern = @"^(\d+)_(\d+)_(AI|AO|DI|DO)_(\d+)$";
            var match = Regex.Match(channelPosition.Trim(), pattern, RegexOptions.IgnoreCase);
            
            return match.Success ? match.Groups[3].Value.ToUpper() : "";
        }
    }
}