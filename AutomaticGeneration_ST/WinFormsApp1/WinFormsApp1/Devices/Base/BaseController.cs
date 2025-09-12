// NEED DELETE: Legacy device controller base class. Not part of current import→generate→export flow.
// Reason: The entire `Devices/` legacy stack (composite devices/controllers) is unused in the
// active architecture centered on `ImportPipeline` and Scriban generators. Other device classes
// in this folder are already marked NEED DELETE; this base is likewise safe to remove.
// 说明：设备控制器基类属遗留代码，当前主流程未使用；与已标注删除的设备管理代码一致，可安全删除。
using System;
using System.Collections.Generic;

namespace WinFormsApp1.Devices.Base
{
    /// <summary>
    /// 设备控制器基类
    /// </summary>
    public abstract class BaseController
    {
        /// <summary>
        /// 控制器ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 控制器名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 控制器描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 配置参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// 初始化控制器
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// 启动控制器
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// 停止控制器
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// 获取状态
        /// </summary>
        public abstract object GetStatus();
    }
}