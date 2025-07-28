using System.Collections.Generic;

namespace WinFormsApp1.Models
{
    public abstract class BasePoint
    {
        public string 变量名 { get; set; } = string.Empty;
        public string 描述 { get; set; } = string.Empty;
        public string 类型 { get; set; } = string.Empty;
        
        public virtual Dictionary<string, object> ToDataDictionary()
        {
            return new Dictionary<string, object>
            {
                { "变量名", 变量名 },
                { "描述", 描述 },
                { "类型", 类型 }
            };
        }
        
        public virtual void FromDataDictionary(Dictionary<string, object> data)
        {
            if (data.TryGetValue("变量名", out var varName))
                变量名 = varName?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("描述", out var desc))
                描述 = desc?.ToString() ?? string.Empty;
                
            if (data.TryGetValue("类型", out var type))
                类型 = type?.ToString() ?? string.Empty;
        }
    }
}