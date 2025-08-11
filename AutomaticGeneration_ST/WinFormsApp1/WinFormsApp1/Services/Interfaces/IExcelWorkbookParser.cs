using System.Collections.Generic;
using AutomaticGeneration_ST.Services.Interfaces;

namespace AutomaticGeneration_ST.Services.Interfaces
{
    /// <summary>
    /// Excel工作簿解析器接口 - 负责解析Excel文件中的各个工作表
    /// </summary>
    public interface IExcelWorkbookParser
    {
        /// <summary>
        /// 解析指定的工作表
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <returns>工作表数据，每行作为一个字典</returns>
        List<Dictionary<string, object>> ParseWorksheet(string filePath, string sheetName);

        /// <summary>
        /// 获取Excel文件中所有工作表名称
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>工作表名称列表</returns>
        List<string> GetWorksheetNames(string filePath);

        /// <summary>
        /// 检查指定工作表是否存在
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="sheetName">工作表名称</param>
        /// <returns>是否存在</returns>
        bool WorksheetExists(string filePath, string sheetName);

        /// <summary>
        /// 批量解析多个工作表
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="sheetNames">要解析的工作表名称列表</param>
        /// <returns>工作表数据字典，Key为工作表名称</returns>
        Dictionary<string, List<Dictionary<string, object>>> ParseMultipleWorksheets(string filePath, List<string> sheetNames);

        /// <summary>
        /// 智能解析工作表 - 支持工作表名称的模糊匹配和别名
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="expectedSheetName">期望的工作表名称</param>
        /// <param name="locatorService">工作表定位服务</param>
        /// <returns>工作表数据，如果未找到则返回空列表</returns>
        List<Dictionary<string, object>> ParseWorksheetSmart(string filePath, string expectedSheetName, IWorksheetLocatorService locatorService);

        /// <summary>
        /// 智能验证工作表存在性 - 支持模糊匹配
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="expectedSheetName">期望的工作表名称</param>
        /// <param name="locatorService">工作表定位服务</param>
        /// <returns>验证结果</returns>
        WorksheetValidationResult ValidateWorksheetSmart(string filePath, string expectedSheetName, IWorksheetLocatorService locatorService);
    }
}