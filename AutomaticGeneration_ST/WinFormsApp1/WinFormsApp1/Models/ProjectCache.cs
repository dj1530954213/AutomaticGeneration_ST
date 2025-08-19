using System;
using System.Collections.Generic;
using AutomaticGeneration_ST.Services.Interfaces;

namespace AutomaticGeneration_ST.Models
{
    /// <summary>
    /// 项目缓存 - 所有处理结果的单一容器
    /// 实现"上传一次、处理一次，后续仅读取缓存"的核心数据结构
    /// </summary>
    public class ProjectCache
    {
        /// <summary>
        /// 源Excel文件路径
        /// </summary>
        public string SourceFilePath { get; init; } = "";

        /// <summary>
        /// 源文件最后修改时间
        /// </summary>
        public DateTime SourceLastWriteTime { get; init; }

        /// <summary>
        /// 缓存创建时间
        /// </summary>
        public DateTime CacheCreatedTime { get; init; } = DateTime.Now;

        /// <summary>
        /// Excel解析和设备分类的完整数据上下文
        /// </summary>
        public DataContext DataContext { get; init; } = new();

        /// <summary>
        /// 设备ST程序生成结果 (按模板分组)
        /// </summary>
        public Dictionary<string, List<string>> DeviceSTPrograms { get; init; } = new();

        /// <summary>
        /// IO映射代码生成结果
        /// </summary>
        public List<string> IOMappingScripts { get; init; } = new();

        /// <summary>
        /// TCP通讯程序生成结果
        /// </summary>
        public List<string> TcpCommunicationPrograms { get; init; } = new();

        /// <summary>
        /// 生成统计信息
        /// </summary>
        public ProjectStatistics Statistics { get; init; } = new();

        /// <summary>
        /// 项目元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; init; } = new();

        /// <summary>
        /// 验证缓存是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SourceFilePath) &&
                   System.IO.File.Exists(SourceFilePath) &&
                   DataContext?.AllPointsMasterList != null &&
                   DataContext.AllPointsMasterList.Count > 0;
        }

        /// <summary>
        /// 检查源文件是否已更新
        /// </summary>
        public bool IsSourceFileUpdated()
        {
            if (!System.IO.File.Exists(SourceFilePath))
                return true;

            var currentLastWriteTime = System.IO.File.GetLastWriteTime(SourceFilePath);
            return currentLastWriteTime != SourceLastWriteTime;
        }
    }

    /// <summary>
    /// 项目统计信息
    /// </summary>
    public class ProjectStatistics
    {
        public int TotalDevices { get; set; }
        public int TotalPoints { get; set; }
        public int IoPoints { get; set; }
        public int DevicePoints { get; set; }
        public int StandalonePoints { get; set; }
        public int GeneratedSTFiles { get; set; }
        public int GeneratedIOMappingFiles { get; set; }
        public DateTime ProcessingStartTime { get; set; }
        public DateTime ProcessingEndTime { get; set; }
        public TimeSpan ProcessingDuration => ProcessingEndTime - ProcessingStartTime;
        public Dictionary<string, int> DevicesByTemplate { get; set; } = new();
        public Dictionary<string, int> PointsByType { get; set; } = new();
    }
}