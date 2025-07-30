using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinFormsApp1.ProjectManagement
{
    /// <summary>
    /// 项目管理器
    /// </summary>
    public class ProjectManager
    {
        /// <summary>
        /// 当前项目
        /// </summary>
        public string CurrentProject { get; set; } = "";

        /// <summary>
        /// 项目列表
        /// </summary>
        public List<string> Projects { get; set; } = new();

        /// <summary>
        /// 创建新项目
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <returns>创建结果</returns>
        public async Task<bool> CreateProjectAsync(string projectName)
        {
            await Task.Delay(100);
            Projects.Add(projectName);
            return true;
        }

        /// <summary>
        /// 打开项目
        /// </summary>
        /// <param name="projectName">项目名称</param>
        /// <returns>打开结果</returns>
        public async Task<bool> OpenProjectAsync(string projectName)
        {
            await Task.Delay(100);
            CurrentProject = projectName;
            return true;
        }

        /// <summary>
        /// 保存项目
        /// </summary>
        /// <returns>保存结果</returns>
        public async Task<bool> SaveProjectAsync()
        {
            await Task.Delay(100);
            return true;
        }
    }
}