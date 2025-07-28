using System.Collections.Generic;

namespace WinFormsApp1.Models
{
    public class DoPoint : BasePoint
    {
        public string 输出通道 { get; set; } = string.Empty;
        public string 控制变量 { get; set; } = string.Empty;
        public string 反馈变量 { get; set; } = string.Empty;
        public string 默认状态 { get; set; } = "FALSE";
        public string 互锁条件 { get; set; } = string.Empty;
        public string 手动模式 { get; set; } = "FALSE";
        public string 强制状态 { get; set; } = string.Empty;
        
        public override Dictionary<string, object> ToDataDictionary()
        {
            var data = base.ToDataDictionary();
            data["输出通道"] = 输出通道;
            data["控制变量"] = 控制变量;
            data["反馈变量"] = 反馈变量;
            data["默认状态"] = 默认状态;
            data["互锁条件"] = 互锁条件;
            data["手动模式"] = 手动模式;
            data["强制状态"] = 强制状态;
            return data;
        }
        
        public override void FromDataDictionary(Dictionary<string, object> data)
        {
            base.FromDataDictionary(data);
            
            if (data.TryGetValue("输出通道", out var outChannel))
                输出通道 = outChannel?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("控制变量", out var ctrlVar))
                控制变量 = ctrlVar?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("反馈变量", out var fbVar))
                反馈变量 = fbVar?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("默认状态", out var defState))
                默认状态 = defState?.ToString() ?? "FALSE";
                
            if (data.TryGetValue("互锁条件", out var interlockCond))
                互锁条件 = interlockCond?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("手动模式", out var manualMode))
                手动模式 = manualMode?.ToString() ?? "FALSE";
                
            if (data.TryGetValue("强制状态", out var forceState))
                强制状态 = forceState?.ToString() ?? string.Empty;
        }
    }
}