using System.Collections.Generic;
using System.Threading.Tasks;
using WinFormsApp1.Models;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// TCP数据处理服务接口
    /// </summary>
    public interface ITcpDataService
    {
        /// <summary>
        /// 解析TCP通讯表并创建TCP点位
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <returns>TCP点位列表</returns>
        List<TcpCommunicationPoint> ProcessTcpCommunicationTable(string excelFilePath);

        /// <summary>
        /// 异步解析TCP通讯表
        /// </summary>
        /// <param name="excelFilePath">Excel文件路径</param>
        /// <returns>TCP点位列表</returns>
        Task<List<TcpCommunicationPoint>> ProcessTcpCommunicationTableAsync(string excelFilePath);

        /// <summary>
        /// 验证TCP点位配置
        /// </summary>
        /// <param name="tcpPoints">TCP点位列表</param>
        /// <returns>验证结果</returns>
        ValidationResult ValidateTcpPoints(List<TcpCommunicationPoint> tcpPoints);

        /// <summary>
        /// 获取TCP模拟量点位
        /// </summary>
        /// <param name="tcpPoints">所有TCP点位</param>
        /// <returns>模拟量点位列表</returns>
        List<TcpAnalogPoint> GetAnalogPoints(List<TcpCommunicationPoint> tcpPoints);

        /// <summary>
        /// 获取TCP数字量点位
        /// </summary>
        /// <param name="tcpPoints">所有TCP点位</param>
        /// <returns>数字量点位列表</returns>
        List<TcpDigitalPoint> GetDigitalPoints(List<TcpCommunicationPoint> tcpPoints);
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public void AddError(string error)
        {
            IsValid = false;
            Errors.Add(error);
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}