using System;
using System.Collections.Generic;
using System.Linq;

namespace WinFormsApp1.Tests
{
    /// <summary>
    /// 测试套件
    /// </summary>
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