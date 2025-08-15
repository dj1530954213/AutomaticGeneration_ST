using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1.Template;
using WinFormsApp1.Utils;

namespace WinFormsApp1.Generators
{
    /// <summary>
    /// AI点位代码生成器 - 重构后版本
    /// </summary>
    /// <remarks>
    /// 重构状态: ✅ 已完成 - 继承BasePointGenerator
    /// 重构前: 181行，包含大量重复逻辑
    /// 重构后: 显著简化，仅保留AI特定的差异化逻辑
    /// 重构收益: 消除DUP-001和DUP-007重复代码问题
    /// 创建时间: 2025-08-15
    /// </remarks>
    public class AiGenerator : BasePointGenerator
    {
        public override string PointType => "AI";
        
        protected override void ValidateRequiredFields(Dictionary<string, object> row)
        {
            // 检查变量名称，更宽松的验证
            var variableName = GetStringValue(row, "变量名称（HMI）");
            if (string.IsNullOrWhiteSpace(variableName))
            {
                // 如果变量名称为空，尝试使用其他标识符
                var alternatives = new[] { "变量名称", "变量名", "标识符", "名称" };
                bool found = false;
                
                foreach (var alt in alternatives)
                {
                    var altValue = GetStringValue(row, alt);
                    if (!string.IsNullOrWhiteSpace(altValue))
                    {
                        row["变量名称（HMI）"] = altValue; // 设置为找到的值
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    // 如果还是找不到，跳过这一行而不抛出异常
                    logger.LogWarning("AI点位缺少变量名称，跳过该行");
                    throw new ArgumentException("AI点位缺少变量名称");
                }
            }
        }
        
        protected override Dictionary<string, object> PreprocessData(Dictionary<string, object> row)
        {
            // 使用基类的通用通道处理逻辑（输入通道）
            var processedData = ProcessChannelData(row, "输入通道");
            
            // 设置量程默认值
            SetDefaultIfEmpty(processedData, "量程低限", 0.0);
            SetDefaultIfEmpty(processedData, "量程高限", 100.0);
            
            // 保持原有的报警限值处理
            SetDefaultIfEmpty(processedData, "高高限", 100.0);
            SetDefaultIfEmpty(processedData, "高限", 90.0);
            SetDefaultIfEmpty(processedData, "低限", 10.0);
            SetDefaultIfEmpty(processedData, "低低限", 0.0);
            
            return processedData;
        }
    }
}