using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1.Template;
using WinFormsApp1.Utils;

namespace WinFormsApp1.Generators
{
    /// <summary>
    /// AO点位代码生成器 - 重构后版本
    /// </summary>
    /// <remarks>
    /// 重构状态: ✅ 已完成 - 继承BasePointGenerator
    /// 重构前: 137行，包含大量重复逻辑
    /// 重构后: 显著简化，仅保留AO特定的差异化逻辑
    /// 重构收益: 消除DUP-001和DUP-007重复代码问题
    /// 创建时间: 2025-08-15
    /// </remarks>
    public class AoGenerator : BasePointGenerator
    {
        public override string PointType => "AO";
        
        protected override void ValidateRequiredFields(Dictionary<string, object> row)
        {
            var requiredFields = new[] { "变量名称（HMI）" };
            
            foreach (var field in requiredFields)
            {
                if (!row.TryGetValue(field, out var value) || string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    throw new ArgumentException($"AO点位缺少必要字段: {field}");
                }
            }
        }
        
        protected override Dictionary<string, object> PreprocessData(Dictionary<string, object> row)
        {
            // 使用基类的通用通道处理逻辑（输出通道）
            var processedData = ProcessChannelData(row, "输出通道");
            
            // 设置量程默认值
            SetDefaultIfEmpty(processedData, "量程低限", 0.0);
            SetDefaultIfEmpty(processedData, "量程高限", 100.0);
            
            return processedData;
        }
    }
}