using System.Collections.Concurrent;
using System.Collections.Generic;
using AutomaticGeneration_ST.Models;

namespace AutomaticGeneration_ST.Services.VariableBlocks
{
    /// <summary>
    /// 全局变量条目注册表，用于在代码生成流程中收集并在UI阶段统一读取。
    /// </summary>
    public static class VariableEntriesRegistry
    {
        private static readonly ConcurrentDictionary<string, List<VariableTableEntry>> _entriesByTemplate = new();

        /// <summary>
        /// 向注册表添加条目。
        /// </summary>
        /// <param name="templateName">模板名或分组键</param>
        /// <param name="entries">条目列表</param>
        public static void AddEntries(string templateName, List<VariableTableEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(templateName) || entries == null || entries.Count == 0)
                return;

            var list = _entriesByTemplate.GetOrAdd(templateName, _ => new List<VariableTableEntry>());
            lock (list)
            {
                list.AddRange(entries);
            }
        }

        /// <summary>
        /// 获取全部条目副本并清空注册表。
        /// </summary>
        public static Dictionary<string, List<VariableTableEntry>> DrainAll()
        {
            var snapshot = new Dictionary<string, List<VariableTableEntry>>();
            foreach (var kvp in _entriesByTemplate)
            {
                snapshot[kvp.Key] = new List<VariableTableEntry>(kvp.Value);
            }
            _entriesByTemplate.Clear();
            return snapshot;
        }
    }
}
