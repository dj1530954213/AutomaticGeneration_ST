using System.Collections.Generic;

namespace WinFormsApp1.Models
{
    public class DiPoint : BasePoint
    {
        public string 输入通道 { get; set; } = string.Empty;
        public string 正常状态 { get; set; } = "FALSE";
        public string 报警状态 { get; set; } = "TRUE";
        public string 状态变量 { get; set; } = string.Empty;
        public string 报警变量 { get; set; } = string.Empty;
        public string 延时时间 { get; set; } = "0";
        public string 反向逻辑 { get; set; } = "FALSE";
        
        public override Dictionary<string, object> ToDataDictionary()
        {
            var data = base.ToDataDictionary();
            data["输入通道"] = 输入通道;
            data["正常状态"] = 正常状态;
            data["报警状态"] = 报警状态;
            data["状态变量"] = 状态变量;
            data["报警变量"] = 报警变量;
            data["延时时间"] = 延时时间;
            data["反向逻辑"] = 反向逻辑;
            return data;
        }
        
        public override void FromDataDictionary(Dictionary<string, object> data)
        {
            base.FromDataDictionary(data);
            
            if (data.TryGetValue("输入通道", out var inChannel))
                输入通道 = inChannel?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("正常状态", out var normalState))
                正常状态 = normalState?.ToString() ?? "FALSE";
                
            if (data.TryGetValue("报警状态", out var alarmState))
                报警状态 = alarmState?.ToString() ?? "TRUE";
                
            if (data.TryGetValue("状态变量", out var stateVar))
                状态变量 = stateVar?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("报警变量", out var alarmVar))
                报警变量 = alarmVar?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("延时时间", out var delayTime))
                延时时间 = delayTime?.ToString() ?? "0";
                
            if (data.TryGetValue("反向逻辑", out var inverseLogic))
                反向逻辑 = inverseLogic?.ToString() ?? "FALSE";
        }
    }
}