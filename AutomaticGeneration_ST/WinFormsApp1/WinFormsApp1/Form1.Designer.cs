namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 主要控件
            mainMenuStrip = new MenuStrip();
            mainStatusStrip = new StatusStrip();
            mainSplitContainer = new SplitContainer();
            leftPanel = new Panel();
            rightSplitContainer = new SplitContainer();
            
            // 原有控件
            button_upload = new Button();
            button_export = new Button();
            richTextBox1 = new RichTextBox();
            
            // 新增控件
            fileListBox = new ListBox();
            previewTabControl = new TabControl();
            configPanel = new Panel();
            progressBar = new ToolStripProgressBar();
            statusLabel = new ToolStripStatusLabel();
            logFilterPanel = new Panel();
            logSearchBox = new TextBox();
            logFilterComboBox = new ComboBox();
            clearLogButton = new Button();
            
            // 菜单项
            fileMenu = new ToolStripMenuItem();
            editMenu = new ToolStripMenuItem();
            viewMenu = new ToolStripMenuItem();
            toolsMenu = new ToolStripMenuItem();
            helpMenu = new ToolStripMenuItem();
            
            // 文件菜单项
            openFileMenuItem = new ToolStripMenuItem();
            exportFileMenuItem = new ToolStripMenuItem();
            regenerateMenuItem = new ToolStripMenuItem();
            exitMenuItem = new ToolStripMenuItem();
            
            // 编辑菜单项
            clearLogMenuItem = new ToolStripMenuItem();
            
            // 帮助菜单项
            aboutMenuItem = new ToolStripMenuItem();
            
            // 主题菜单项
            themeMenu = new ToolStripMenuItem();
            lightThemeMenuItem = new ToolStripMenuItem();
            darkThemeMenuItem = new ToolStripMenuItem();
            systemThemeMenuItem = new ToolStripMenuItem();
            
            // 工具菜单项
            templateEditorMenuItem = new ToolStripMenuItem();
            settingsMenuItem = new ToolStripMenuItem();
            performanceMonitorMenuItem = new ToolStripMenuItem();
            testRunnerMenuItem = new ToolStripMenuItem();
            
            SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            ((System.ComponentModel.ISupportInitialize)rightSplitContainer).BeginInit();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.ImageScalingSize = new Size(24, 24);
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, toolsMenu, helpMenu });
            mainMenuStrip.Location = new Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(1200, 32);
            mainMenuStrip.TabIndex = 0;
            mainMenuStrip.Text = "主菜单";
            // 
            // fileMenu
            // 
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openFileMenuItem, exportFileMenuItem, regenerateMenuItem, new ToolStripSeparator(), exitMenuItem });
            fileMenu.Name = "fileMenu";
            fileMenu.Size = new Size(58, 28);
            fileMenu.Text = "文件(&F)";
            // 
            // openFileMenuItem
            // 
            openFileMenuItem.Name = "openFileMenuItem";
            openFileMenuItem.Size = new Size(180, 26);
            openFileMenuItem.Text = "📁 打开点表(&O)";
            openFileMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openFileMenuItem.ToolTipText = "打开点表文件";
            openFileMenuItem.Click += OpenFileMenuItem_Click;
            // 
            // exportFileMenuItem
            // 
            exportFileMenuItem.Name = "exportFileMenuItem";
            exportFileMenuItem.Size = new Size(180, 26);
            exportFileMenuItem.Text = "💾 导出结果(&S)";
            exportFileMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            exportFileMenuItem.ToolTipText = "导出ST脚本";
            exportFileMenuItem.Click += ExportFileMenuItem_Click;
            // 
            // regenerateMenuItem
            // 
            regenerateMenuItem.Name = "regenerateMenuItem";
            regenerateMenuItem.Size = new Size(180, 26);
            regenerateMenuItem.Text = "🔄 重新生成(&R)";
            regenerateMenuItem.ShortcutKeys = Keys.F5;
            regenerateMenuItem.ToolTipText = "重新生成代码";
            regenerateMenuItem.Click += RegenerateMenuItem_Click;
            // 
            // exitMenuItem
            // 
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new Size(180, 26);
            exitMenuItem.Text = "退出(&X)";
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitMenuItem.Click += ExitMenuItem_Click;
            // 
            // editMenu
            // 
            editMenu.DropDownItems.AddRange(new ToolStripItem[] { clearLogMenuItem });
            editMenu.Name = "editMenu";
            editMenu.Size = new Size(58, 28);
            editMenu.Text = "编辑(&E)";
            // 
            // clearLogMenuItem
            // 
            clearLogMenuItem.Name = "clearLogMenuItem";
            clearLogMenuItem.Size = new Size(180, 26);
            clearLogMenuItem.Text = "🗑️ 清空日志(&C)";
            clearLogMenuItem.ShortcutKeys = Keys.Control | Keys.L;
            clearLogMenuItem.ToolTipText = "清空日志内容";
            clearLogMenuItem.Click += ClearLogMenuItem_Click;
            // 
            // viewMenu
            // 
            viewMenu.DropDownItems.AddRange(new ToolStripItem[] { themeMenu });
            viewMenu.Name = "viewMenu";
            viewMenu.Size = new Size(58, 28);
            viewMenu.Text = "视图(&V)";
            // 
            // themeMenu
            // 
            themeMenu.DropDownItems.AddRange(new ToolStripItem[] { lightThemeMenuItem, darkThemeMenuItem, systemThemeMenuItem });
            themeMenu.Name = "themeMenu";
            themeMenu.Size = new Size(180, 26);
            themeMenu.Text = "主题(&T)";
            // 
            // lightThemeMenuItem
            // 
            lightThemeMenuItem.Checked = true;
            lightThemeMenuItem.Name = "lightThemeMenuItem";
            lightThemeMenuItem.Size = new Size(180, 26);
            lightThemeMenuItem.Text = "浅色主题(&L)";
            lightThemeMenuItem.Click += LightThemeMenuItem_Click;
            // 
            // darkThemeMenuItem
            // 
            darkThemeMenuItem.Name = "darkThemeMenuItem";
            darkThemeMenuItem.Size = new Size(180, 26);
            darkThemeMenuItem.Text = "深色主题(&D)";
            darkThemeMenuItem.Click += DarkThemeMenuItem_Click;
            // 
            // systemThemeMenuItem
            // 
            systemThemeMenuItem.Name = "systemThemeMenuItem";
            systemThemeMenuItem.Size = new Size(180, 26);
            systemThemeMenuItem.Text = "跟随系统(&S)";
            systemThemeMenuItem.Click += SystemThemeMenuItem_Click;
            // 
            // templateEditorMenuItem
            // 
            templateEditorMenuItem.Name = "templateEditorMenuItem";
            templateEditorMenuItem.Size = new Size(180, 26);
            templateEditorMenuItem.Text = "模板编辑器(&E)";
            templateEditorMenuItem.ToolTipText = "打开模板编辑器";
            templateEditorMenuItem.Click += TemplateEditorMenuItem_Click;
            // 
            // settingsMenuItem
            // 
            settingsMenuItem.Name = "settingsMenuItem";
            settingsMenuItem.Size = new Size(180, 26);
            settingsMenuItem.Text = "设置(&S)";
            settingsMenuItem.ToolTipText = "打开应用程序设置";
            settingsMenuItem.Click += SettingsMenuItem_Click;
            // 
            // toolsMenu
            // 
            toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { templateEditorMenuItem, performanceMonitorMenuItem, testRunnerMenuItem, new ToolStripSeparator(), settingsMenuItem });
            toolsMenu.Name = "toolsMenu";
            toolsMenu.Size = new Size(58, 28);
            toolsMenu.Text = "工具(&T)";
            // 
            // performanceMonitorMenuItem
            // 
            performanceMonitorMenuItem.Name = "performanceMonitorMenuItem";
            performanceMonitorMenuItem.Size = new Size(180, 26);
            performanceMonitorMenuItem.Text = "性能监控(&P)";
            performanceMonitorMenuItem.ToolTipText = "查看模板渲染性能和缓存统计";
            performanceMonitorMenuItem.Click += PerformanceMonitorMenuItem_Click;
            // 
            // testRunnerMenuItem
            // 
            testRunnerMenuItem.Name = "testRunnerMenuItem";
            testRunnerMenuItem.Size = new Size(180, 26);
            testRunnerMenuItem.Text = "系统测试(&T)";
            testRunnerMenuItem.ToolTipText = "运行模板系统功能测试";
            testRunnerMenuItem.Click += TestRunnerMenuItem_Click;
            toolsMenu.Name = "toolsMenu";
            toolsMenu.Size = new Size(58, 28);
            toolsMenu.Text = "工具(&T)";
            // 
            // helpMenu
            // 
            helpMenu.DropDownItems.AddRange(new ToolStripItem[] { aboutMenuItem });
            helpMenu.Name = "helpMenu";
            helpMenu.Size = new Size(58, 28);
            helpMenu.Text = "帮助(&H)";
            // 
            // aboutMenuItem
            // 
            aboutMenuItem.Name = "aboutMenuItem";
            aboutMenuItem.Size = new Size(180, 26);
            aboutMenuItem.Text = "❓ 关于(&A)";
            aboutMenuItem.ShortcutKeys = Keys.F1;
            aboutMenuItem.ToolTipText = "显示关于信息";
            aboutMenuItem.Click += AboutMenuItem_Click;
            // 
            // button_upload
            // 
            button_upload.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            button_upload.Location = new Point(10, 10);
            button_upload.Name = "button_upload";
            button_upload.Size = new Size(180, 45);
            button_upload.TabIndex = 0;
            button_upload.Text = "📁 上传点表";
            button_upload.UseVisualStyleBackColor = true;
            button_upload.Click += button_upload_Click;
            // 
            // button_export
            // 
            button_export.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            button_export.Location = new Point(200, 10);
            button_export.Name = "button_export";
            button_export.Size = new Size(180, 45);
            button_export.TabIndex = 1;
            button_export.Text = "💾 导出结果";
            button_export.UseVisualStyleBackColor = true;
            button_export.Click += button_export_Click;
            // 
            // fileListBox
            // 
            fileListBox.Dock = DockStyle.Fill;
            fileListBox.Font = new Font("微软雅黑", 10F);
            fileListBox.FormattingEnabled = true;
            fileListBox.ItemHeight = 20;
            fileListBox.Location = new Point(0, 65);
            fileListBox.Name = "fileListBox";
            fileListBox.Size = new Size(250, 400);
            fileListBox.TabIndex = 2;
            // 
            // previewTabControl
            // 
            previewTabControl.Dock = DockStyle.Fill;
            previewTabControl.Font = new Font("微软雅黑", 10F);
            previewTabControl.Location = new Point(0, 0);
            previewTabControl.Name = "previewTabControl";
            previewTabControl.SelectedIndex = 0;
            previewTabControl.Size = new Size(600, 400);
            previewTabControl.TabIndex = 3;
            // 
            // configPanel
            // 
            configPanel.Dock = DockStyle.Fill;
            configPanel.Location = new Point(0, 0);
            configPanel.Name = "configPanel";
            configPanel.Size = new Size(300, 400);
            configPanel.TabIndex = 4;
            // 
            // logFilterPanel
            // 
            logFilterPanel.Controls.Add(clearLogButton);
            logFilterPanel.Controls.Add(logFilterComboBox);
            logFilterPanel.Controls.Add(logSearchBox);
            logFilterPanel.Dock = DockStyle.Top;
            logFilterPanel.Height = 35;
            logFilterPanel.Name = "logFilterPanel";
            logFilterPanel.Size = new Size(950, 35);
            logFilterPanel.TabIndex = 10;
            // 
            // logSearchBox
            // 
            logSearchBox.Location = new Point(5, 7);
            logSearchBox.Name = "logSearchBox";
            logSearchBox.PlaceholderText = "🔍 搜索日志...";
            logSearchBox.Size = new Size(200, 23);
            logSearchBox.TabIndex = 0;
            // 
            // logFilterComboBox
            // 
            logFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            logFilterComboBox.Items.AddRange(new object[] { "全部", "信息", "成功", "警告", "错误", "调试" });
            logFilterComboBox.Location = new Point(215, 7);
            logFilterComboBox.Name = "logFilterComboBox";
            logFilterComboBox.SelectedIndex = 0;
            logFilterComboBox.Size = new Size(100, 23);
            logFilterComboBox.TabIndex = 1;
            // 
            // clearLogButton
            // 
            clearLogButton.Location = new Point(325, 6);
            clearLogButton.Name = "clearLogButton";
            clearLogButton.Size = new Size(75, 25);
            clearLogButton.TabIndex = 2;
            clearLogButton.Text = "🗑️ 清空";
            clearLogButton.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Font = new Font("Consolas", 10F);
            richTextBox1.Location = new Point(0, 35);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(950, 165);
            richTextBox1.TabIndex = 5;
            richTextBox1.Text = "";
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(fileListBox);
            leftPanel.Controls.Add(button_export);
            leftPanel.Controls.Add(button_upload);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new Size(250, 500);
            leftPanel.TabIndex = 6;
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Location = new Point(0, 32);
            mainSplitContainer.Name = "mainSplitContainer";
            mainSplitContainer.Panel1.Controls.Add(leftPanel);
            mainSplitContainer.Panel2.Controls.Add(rightSplitContainer);
            mainSplitContainer.Size = new Size(1200, 525);
            mainSplitContainer.SplitterDistance = 250;
            mainSplitContainer.TabIndex = 7;
            // 
            // rightSplitContainer
            // 
            rightSplitContainer.Dock = DockStyle.Fill;
            rightSplitContainer.Location = new Point(0, 0);
            rightSplitContainer.Name = "rightSplitContainer";
            rightSplitContainer.Orientation = Orientation.Horizontal;
            rightSplitContainer.Panel1.Controls.Add(previewTabControl);
            rightSplitContainer.Panel2.Controls.Add(richTextBox1);
            rightSplitContainer.Panel2.Controls.Add(logFilterPanel);
            rightSplitContainer.Size = new Size(950, 500);
            rightSplitContainer.SplitterDistance = 300;
            rightSplitContainer.TabIndex = 8;
            // 
            // mainStatusStrip
            // 
            mainStatusStrip.ImageScalingSize = new Size(24, 24);
            mainStatusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, 
                new ToolStripSeparator(), 
                new ToolStripStatusLabel("📊") { Name = "statsIcon" },
                new ToolStripStatusLabel("总点位: 0") { Name = "totalPointsLabel" },
                new ToolStripSeparator(),
                new ToolStripStatusLabel("⏱️") { Name = "timeIcon" },
                new ToolStripStatusLabel(DateTime.Now.ToString("HH:mm:ss")) { Name = "timeLabel" },
                new ToolStripSeparator(),
                progressBar });
            mainStatusStrip.Location = new Point(0, 557);
            mainStatusStrip.Name = "mainStatusStrip";
            mainStatusStrip.Size = new Size(1200, 32);
            mainStatusStrip.TabIndex = 9;
            mainStatusStrip.Text = "状态栏";
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(56, 25);
            statusLabel.Text = "就绪";
            statusLabel.Spring = true;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(200, 25);
            progressBar.Alignment = ToolStripItemAlignment.Right;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 589);
            Controls.Add(mainSplitContainer);
            Controls.Add(mainMenuStrip);
            Controls.Add(mainStatusStrip);
            MainMenuStrip = mainMenuStrip;
            Name = "Form1";
            Text = "ST脚本自动生成器 v2.0";
            WindowState = FormWindowState.Maximized;
            
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)rightSplitContainer).EndInit();
            rightSplitContainer.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // 原有控件
        private Button button_upload;
        private Button button_export;
        private RichTextBox richTextBox1;
        
        // 新增主要控件
        private MenuStrip mainMenuStrip;
        private StatusStrip mainStatusStrip;
        private SplitContainer mainSplitContainer;
        private SplitContainer rightSplitContainer;
        private Panel leftPanel;
        private ListBox fileListBox;
        private TabControl previewTabControl;
        private Panel configPanel;
        private ToolStripProgressBar progressBar;
        private ToolStripStatusLabel statusLabel;
        private Panel logFilterPanel;
        private TextBox logSearchBox;
        private ComboBox logFilterComboBox;
        private Button clearLogButton;
        
        // 菜单项
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem editMenu;
        private ToolStripMenuItem viewMenu;
        private ToolStripMenuItem toolsMenu;
        private ToolStripMenuItem helpMenu;
        
        // 主题菜单项
        private ToolStripMenuItem themeMenu;
        private ToolStripMenuItem lightThemeMenuItem;
        private ToolStripMenuItem darkThemeMenuItem;
        private ToolStripMenuItem systemThemeMenuItem;
        
        // 文件菜单项
        private ToolStripMenuItem openFileMenuItem;
        private ToolStripMenuItem exportFileMenuItem;
        private ToolStripMenuItem regenerateMenuItem;
        private ToolStripMenuItem exitMenuItem;
        
        // 编辑菜单项
        private ToolStripMenuItem clearLogMenuItem;
        
        // 工具菜单项
        private ToolStripMenuItem templateEditorMenuItem;
        private ToolStripMenuItem settingsMenuItem;
        private ToolStripMenuItem performanceMonitorMenuItem;
        private ToolStripMenuItem testRunnerMenuItem;
        
        // 帮助菜单项
        private ToolStripMenuItem aboutMenuItem;
    }
}
