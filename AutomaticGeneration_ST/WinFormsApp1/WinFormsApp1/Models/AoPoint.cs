using System.Collections.Generic;

namespace AutomaticGeneration_ST.Models
{
    public class AoPoint : BasePoint
    {
        public string 输出通道 { get; set; } = string.Empty;
        public double 最大值 { get; set; }
        public double 最小值 { get; set; }
        public string 控制变量 { get; set; } = string.Empty;
        public string 反馈变量 { get; set; } = string.Empty;
        public string 单位 { get; set; } = string.Empty;
        public string 默认值 { get; set; } = string.Empty;
        
        public override Dictionary<string, object> ToDataDictionary()
        {
            var data = base.ToDataDictionary();
            data["输出通道"] = 输出通道;
            data["最大值"] = 最大值;
            data["最小值"] = 最小值;
            data["控制变量"] = 控制变量;
            data["反馈变量"] = 反馈变量;
            data["单位"] = 单位;
            data["默认值"] = 默认值;
            return data;
        }
        
        public override void FromDataDictionary(Dictionary<string, object> data)
        {
            base.FromDataDictionary(data);
            
            if (data.TryGetValue("输出通道", out var outChannel))
                输出通道 = outChannel?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("最大值", out var maxVal) && double.TryParse(maxVal?.ToString(), out var maxValue))
                最大值 = maxValue;
                
            if (data.TryGetValue("最小值", out var minVal) && double.TryParse(minVal?.ToString(), out var minValue))
                最小值 = minValue;
                
            if (data.TryGetValue("控制变量", out var ctrlVar))
                控制变量 = ctrlVar?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("反馈变量", out var fbVar))
                反馈变量 = fbVar?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("单位", out var unit))
                单位 = unit?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("默认值", out var defVal))
                默认值 = defVal?.ToString() ?? string.Empty;
        }
    }
}