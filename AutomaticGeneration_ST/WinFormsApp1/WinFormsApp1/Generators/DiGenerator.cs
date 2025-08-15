using System;
using System.Collections.Generic;
using System.IO;
using WinFormsApp1.Template;
using WinFormsApp1.Utils;

namespace WinFormsApp1.Generators
{
    /// <summary>
    /// DI点位代码生成器 - 重构后版本
    /// </summary>
    /// <remarks>
    /// 重构状态: ✅ 已完成 - 继承BasePointGenerator
    /// 重构前: 72行，简化版实现但仍有重复逻辑
    /// 重构后: 整合进基类架构，仅保留DI特定的简化逻辑
    /// 重构收益: 消除DUP-001和DUP-007重复代码问题
    /// 创建时间: 2025-08-15
    /// </remarks>
    public class DiGenerator : BasePointGenerator
    {
        public override string PointType => "DI";
        
        protected override void ValidateRequiredFields(Dictionary<string, object> row)
        {
            // DI简化版本，不进行特殊验证，使用基类默认处理
        }
        
        protected override Dictionary<string, object> PreprocessData(Dictionary<string, object> row)
        {
            // 使用基类的通用通道处理逻辑（输入通道）
            return ProcessChannelData(row, "输入通道");
        }
    }
}