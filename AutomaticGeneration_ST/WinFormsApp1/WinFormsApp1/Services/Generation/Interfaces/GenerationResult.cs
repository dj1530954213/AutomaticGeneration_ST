namespace AutomaticGeneration_ST.Services.Generation.Interfaces
{
    /// <summary>
    /// 代表一个代码生成操作的结果
    /// </summary>
    public class GenerationResult
    {
        /// <summary>
        /// 生成的文件名
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        /// 生成的文件内容
        /// </summary>
        public string Content { get; set; } = "";

        /// <summary>
        /// 文件类别 (Device, IO, Communication)
        /// </summary>
        public string Category { get; set; } = "";
    }
}