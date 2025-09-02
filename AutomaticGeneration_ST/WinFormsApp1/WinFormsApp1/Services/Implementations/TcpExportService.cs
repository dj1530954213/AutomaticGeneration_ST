//NEED DELETE
// REASON: This service belongs to a new architecture that is not integrated into the main UI and is currently unused.
using WinFormsApp1.Generators;
using WinFormsApp1.Models;
using WinFormsApp1.Output;

namespace WinFormsApp1.Services.Implementations
{
    /// <summary>
    /// 将 TCP 模拟量 / 数字量 ST 脚本导出为 txt 文件
    /// </summary>
    // NEED DELETE
    // 原因: 此服务属于未集成的TCP通讯新架构，当前未在主UI流程中激活。
    public class TcpExportService
    {
        private readonly TcpCodeGenerator _codeGenerator;

        public TcpExportService(TcpCodeGenerator codeGenerator)
        {
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        }

        /// <summary>
        /// 导出 TCP 模拟量脚本到指定 txt 文件
        /// </summary>
        public void ExportAnalog(IEnumerable<TcpAnalogPoint> points, string filePath)
        {
            if (points == null || !points.Any())
                throw new ArgumentException("模拟量点位集合为空", nameof(points));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径为空", nameof(filePath));

            var codeText = _codeGenerator.GenerateTcpAnalogCode(points.ToList());
            var segments = SplitSegments(codeText);
            OutputWriter.WriteToFile(segments, filePath);
        }

        /// <summary>
        /// 导出 TCP 数字量脚本到指定 txt 文件
        /// </summary>
        public void ExportDigital(IEnumerable<TcpDigitalPoint> points, string filePath)
        {
            if (points == null || !points.Any())
                throw new ArgumentException("数字量点位集合为空", nameof(points));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("文件路径为空", nameof(filePath));

            var codeText = _codeGenerator.GenerateTcpDigitalCode(points.ToList());
            var segments = SplitSegments(codeText);
            OutputWriter.WriteToFile(segments, filePath);
        }

        /// <summary>
        /// 将生成器返回的整体字符串拆分为单段脚本，便于 OutputWriter 追加文件头等
        /// </summary>
        private static List<string> SplitSegments(string codeText)
        {
            if (string.IsNullOrWhiteSpace(codeText))
                return new List<string>();

            // 生成器已用两个空行分割各点位脚本
            return codeText
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
    }
}
