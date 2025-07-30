using System.Collections.Generic;

namespace AutomaticGeneration_ST.Models
{
    public class AiPoint : BasePoint
    {
        public string 输入通道 { get; set; } = string.Empty;
        public double 高高限 { get; set; }
        public double 高限 { get; set; }
        public double 低限 { get; set; }
        public double 低低限 { get; set; }
        public string 输出变量 { get; set; } = string.Empty;
        public string 单位 { get; set; } = string.Empty;
        public string 工程量变量 { get; set; } = string.Empty;
        
        public override Dictionary<string, object> ToDataDictionary()
        {
            var data = base.ToDataDictionary();
            data["输入通道"] = 输入通道;
            data["高高限"] = 高高限;
            data["高限"] = 高限;
            data["低限"] = 低限;
            data["低低限"] = 低低限;
            data["输出变量"] = 输出变量;
            data["单位"] = 单位;
            data["工程量变量"] = 工程量变量;
            return data;
        }
        
        public override void FromDataDictionary(Dictionary<string, object> data)
        {
            base.FromDataDictionary(data);
            
            if (data.TryGetValue("输入通道", out var inChannel))
                输入通道 = inChannel?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("高高限", out var hhLimit) && double.TryParse(hhLimit?.ToString(), out var hhValue))
                高高限 = hhValue;
                
            if (data.TryGetValue("高限", out var hLimit) && double.TryParse(hLimit?.ToString(), out var hValue))
                高限 = hValue;
                
            if (data.TryGetValue("低限", out var lLimit) && double.TryParse(lLimit?.ToString(), out var lValue))
                低限 = lValue;
                
            if (data.TryGetValue("低低限", out var llLimit) && double.TryParse(llLimit?.ToString(), out var llValue))
                低低限 = llValue;
                
            if (data.TryGetValue("输出变量", out var outVar))
                输出变量 = outVar?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("单位", out var unit))
                单位 = unit?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("工程量变量", out var engVar))
                工程量变量 = engVar?.ToString() ?? string.Empty;
        }
    }
}