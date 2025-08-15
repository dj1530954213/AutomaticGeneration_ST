using System;
using System.Collections.Generic;
using System.Linq;

namespace WinFormsApp1.Tests
{
    /// <summary>
    /// 测试套件
    /// </summary>
    /// <remarks>
    /// 状态: @test-code-mixed
    /// 优先级: P1 (低风险)
    /// 建议: 应移至独立的测试项目
    /// 风险级别: 低风险
    /// 分析时间: 2025-08-15
    /// 影响范围: 仅开发阶段，不影响生产功能
    /// </remarks>
    public class TestSuite
    {
        public string Name { get; set; } = "";
        public List<TestCase> TestCases { get; set; } = new();
        public int PassedCount => TestCases.Count(tc => tc.Passed);
        public int FailedCount => TestCases.Count(tc => tc.Failed);
        public int SkippedCount => TestCases.Count(tc => tc.Skipped);
        public double SuccessRate => TestCases.Count > 0 ? (double)PassedCount / TestCases.Count * 100 : 0;
    }

    /// <summary>
    /// 测试用例
    /// </summary>
    /// <remarks>
    /// 状态: @test-code-mixed
    /// 优先级: P1 (低风险)
    /// 建议: 应移至独立的测试项目
    /// 风险级别: 低风险
    /// 分析时间: 2025-08-15
    /// 影响范围: 仅开发阶段，不影响生产功能
    /// </remarks>
    public class TestCase
    {
        public string Name { get; set; } = "";
        public bool Passed { get; set; }
        public bool Failed { get; set; }
        public bool Skipped { get; set; }
        public string Message { get; set; } = "";
        public TimeSpan Duration { get; set; }
    }
}