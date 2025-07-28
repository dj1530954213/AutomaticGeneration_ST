using System.Text;
using WinFormsApp1.Excel;
using WinFormsApp1.Generators;
using WinFormsApp1.Output;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private LogService logger = null!;
        private string uploadedFilePath = "";
        private List<string> generatedScripts = new List<string>();
        private List<Dictionary<string, object>> pointData = new List<Dictionary<string, object>>();

        public Form1()
        {
            InitializeComponent();
            InitializeLogger();
        }

        private void InitializeLogger()
        {
            logger = LogService.Instance;
            logger.Initialize(richTextBox1);
            logger.LogInfo("ST脚本自动生成器已启动");
            logger.LogInfo("支持的点位类型: AI, AO, DI, DO");
        }

        private void button_upload_Click(object sender, EventArgs e)
        {
            try
            {
                logger.LogInfo("开始上传点表文件...");
                
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "选择点表文件";
                    openFileDialog.Filter = "Excel文件|*.xlsx|CSV文件|*.csv|所有文件|*.*";
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        uploadedFilePath = openFileDialog.FileName;
                        logger.LogInfo($"选择文件: {Path.GetFileName(uploadedFilePath)}");
                        
                        ProcessExcelFile(uploadedFilePath);
                    }
                    else
                    {
                        logger.LogWarning("用户取消了文件选择");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"上传文件时出错: {ex.Message}");
                MessageBox.Show($"上传文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessExcelFile(string filePath)
        {
            try
            {
                logger.LogInfo("正在读取Excel点表文件...");
                
                var excelReader = new ExcelReader();
                pointData = excelReader.ReadPoints(filePath);
                
                logger.LogInfo($"成功读取{pointData.Count}行点位数据");
                
                // 生成ST脚本
                GenerateSTScripts();
                
                logger.LogSuccess("点表文件处理完成，可以进行导出");
            }
            catch (Exception ex)
            {
                logger.LogError($"处理Excel文件时出错: {ex.Message}");
                MessageBox.Show($"处理Excel文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateSTScripts()
        {
            try
            {
                logger.LogInfo("开始生成ST脚本...");
                generatedScripts.Clear();
                
                int successCount = 0;
                int errorCount = 0;
                
                foreach (var row in pointData)
                {
                    try
                    {
                        // 获取点位类型
                        var pointType = row.TryGetValue("模块类型", out var type) ? type?.ToString()?.Trim().ToUpper() : null;
                        
                        if (string.IsNullOrWhiteSpace(pointType))
                        {
                            logger.LogWarning($"跳过没有类型的行: {GetVariableName(row)}");
                            continue;
                        }
                        
                        // 检查是否支持该类型
                        if (!GeneratorFactory.IsSupported(pointType))
                        {
                            logger.LogWarning($"不支持的点位类型: {pointType}，变量名: {GetVariableName(row)}");
                            continue;
                        }
                        
                        // 获取生成器并生成代码
                        var generator = GeneratorFactory.GetGenerator(pointType);
                        var script = generator.Generate(row);
                        
                        if (!string.IsNullOrWhiteSpace(script))
                        {
                            generatedScripts.Add(script);
                            successCount++;
                            logger.LogDebug($"成功生成{pointType}点位: {GetVariableName(row)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        logger.LogError($"生成点位脚本失败: {GetVariableName(row)} - {ex.Message}");
                    }
                }
                
                logger.LogSuccess($"ST脚本生成完成: 成功{successCount}个，失败{errorCount}个");
                
                if (generatedScripts.Any())
                {
                    // 显示预览
                    logger.LogInfo("生成预览:");
                    logger.LogInfo("=" + new string('=', 50));
                    
                    // 显示前几个脚本作为预览
                    int previewCount = Math.Min(3, generatedScripts.Count);
                    for (int i = 0; i < previewCount; i++)
                    {
                        var script = generatedScripts[i];
                        var lines = script.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines.Take(3)) // 每个脚本只显示前3行
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                logger.LogInfo(line.Trim());
                        }
                        if (lines.Length > 3)
                            logger.LogInfo("...");
                        logger.LogInfo("");
                    }
                    
                    if (generatedScripts.Count > previewCount)
                        logger.LogInfo($"... 还有{generatedScripts.Count - previewCount}个脚本");
                    
                    logger.LogInfo("=" + new string('=', 50));
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"生成ST脚本时出错: {ex.Message}");
                throw;
            }
        }

        private string GetVariableName(Dictionary<string, object> row)
        {
            // 尝试多个可能的字段名
            var possibleNames = new[] { "变量名称（HMI）", "变量名称", "变量名", "标识符", "名称" };
            
            foreach (var name in possibleNames)
            {
                if (row.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    return value.ToString()!.Trim();
                }
            }
            
            return "未知";
        }

        private void button_export_Click(object sender, EventArgs e)
        {
            try
            {
                logger.LogInfo("开始导出ST脚本...");
                
                if (!generatedScripts.Any())
                {
                    logger.LogWarning("没有可导出的ST脚本，请先上传并处理点表文件");
                    MessageBox.Show("没有可导出的ST脚本，请先上传并处理点表文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "选择输出文件夹";
                    folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    folderDialog.ShowNewFolderButton = true;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportSTScripts(folderDialog.SelectedPath);
                    }
                    else
                    {
                        logger.LogWarning("用户取消了文件夹选择");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"导出ST脚本时出错: {ex.Message}");
                MessageBox.Show($"导出ST脚本失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportSTScripts(string selectedPath)
        {
            try
            {
                logger.LogInfo($"正在分类保存ST脚本到: {selectedPath}");
                
                var outputDirectory = OutputWriter.WriteCategorizedFiles(generatedScripts, pointData, selectedPath);
                
                logger.LogSuccess($"ST脚本分类导出成功");
                logger.LogInfo($"共导出{generatedScripts.Count}个点位的ST代码");
                
                MessageBox.Show($"ST脚本分类导出成功!\n" +
                               $"输出文件夹: {Path.GetFileName(outputDirectory)}\n" +
                               $"位置: {outputDirectory}\n" +
                               $"点位数量: {generatedScripts.Count}\n" +
                               $"文件格式: 按AI/AO/DI/DO分类的txt文件", 
                               "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                logger.LogError($"保存ST脚本文件时出错: {ex.Message}");
                throw;
            }
        }
    }
}
