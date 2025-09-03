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
            mainMenuStrip = new MenuStrip();
            fileMenu = new ToolStripMenuItem();
            openFileMenuItem = new ToolStripMenuItem();
            exportFileMenuItem = new ToolStripMenuItem();
            regenerateMenuItem = new ToolStripMenuItem();
            exitMenuItem = new ToolStripMenuItem();
            editMenu = new ToolStripMenuItem();
            clearLogMenuItem = new ToolStripMenuItem();
            //NEED DELETE: 视图菜单/工具栏与核心导入-导出流程无关，仅用于主题切换等显示效果，请后续删除相关菜单与事件绑定
            //viewMenu = new ToolStripMenuItem();
            //themeMenu = new ToolStripMenuItem();
            //lightThemeMenuItem = new ToolStripMenuItem();
            //darkThemeMenuItem = new ToolStripMenuItem();
            //systemThemeMenuItem = new ToolStripMenuItem();
            //NEED DELETE: 帮助菜单/工具栏与核心功能无关（关于/帮助弹窗等），可删除以简化界面
            //helpMenu = new ToolStripMenuItem();
            //aboutMenuItem = new ToolStripMenuItem();
            mainStatusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            mainSplitContainer = new SplitContainer();
            leftPanel = new Panel();
            fileListBox = new ListBox();
            button_categorized_export = new Button();
            button_export = new Button();
            button_upload = new Button();
            rightSplitContainer = new SplitContainer();
            previewTabControl = new TabControl();
            richTextBox1 = new RichTextBox();
            logFilterPanel = new Panel();
            clearLogButton = new Button();
            logFilterComboBox = new ComboBox();
            logSearchBox = new TextBox();
            //NEED DELETE: configPanel 为遗留配置面板，未参与核心流程显示
            configPanel = new Panel();
            mainMenuStrip.SuspendLayout();
            mainStatusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
            mainSplitContainer.Panel1.SuspendLayout();
            mainSplitContainer.Panel2.SuspendLayout();
            mainSplitContainer.SuspendLayout();
            leftPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)rightSplitContainer).BeginInit();
            rightSplitContainer.Panel1.SuspendLayout();
            rightSplitContainer.Panel2.SuspendLayout();
            rightSplitContainer.SuspendLayout();
            logFilterPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.ImageScalingSize = new Size(24, 24);
            //NEED DELETE: 以下加入的 viewMenu、helpMenu 属于非核心菜单项（视图/帮助），建议移除
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu});
            //mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, helpMenu });
            mainMenuStrip.Location = new Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(1200, 32);
            mainMenuStrip.TabIndex = 0;
            mainMenuStrip.Text = "主菜单";
            // 
            // fileMenu
            // 
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openFileMenuItem, exportFileMenuItem, regenerateMenuItem, exitMenuItem });
            fileMenu.Name = "fileMenu";
            fileMenu.Size = new Size(84, 28);
            fileMenu.Text = "文件(&F)";
            // 
            // openFileMenuItem
            // 
            openFileMenuItem.Name = "openFileMenuItem";
            openFileMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            openFileMenuItem.Size = new Size(308, 34);
            openFileMenuItem.Text = "📁 打开点表(&O)";
            openFileMenuItem.ToolTipText = "打开点表文件";
            openFileMenuItem.Click += OpenFileMenuItem_Click;
            // 
            // exportFileMenuItem
            // 
            exportFileMenuItem.Name = "exportFileMenuItem";
            exportFileMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            exportFileMenuItem.Size = new Size(308, 34);
            exportFileMenuItem.Text = "💾 导出结果(&S)";
            exportFileMenuItem.ToolTipText = "导出ST脚本";
            exportFileMenuItem.Click += ExportFileMenuItem_Click;
            // 
            // regenerateMenuItem
            // 
            regenerateMenuItem.Name = "regenerateMenuItem";
            regenerateMenuItem.ShortcutKeys = Keys.F5;
            regenerateMenuItem.Size = new Size(308, 34);
            regenerateMenuItem.Text = "🔄 重新生成(&R)";
            regenerateMenuItem.ToolTipText = "重新生成代码";
            regenerateMenuItem.Click += RegenerateMenuItem_Click;
            // 
            // exitMenuItem
            // 
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitMenuItem.Size = new Size(308, 34);
            exitMenuItem.Text = "退出(&X)";
            exitMenuItem.Click += ExitMenuItem_Click;
            // 
            // editMenu
            // 
            editMenu.DropDownItems.AddRange(new ToolStripItem[] { clearLogMenuItem });
            editMenu.Name = "editMenu";
            editMenu.Size = new Size(84, 28);
            editMenu.Text = "编辑(&E)";
            // 
            // clearLogMenuItem
            // 
            clearLogMenuItem.Name = "clearLogMenuItem";
            clearLogMenuItem.ShortcutKeys = Keys.Control | Keys.L;
            clearLogMenuItem.Size = new Size(299, 34);
            clearLogMenuItem.Text = "🗑️ 清空日志(&C)";
            clearLogMenuItem.ToolTipText = "清空日志内容";
            clearLogMenuItem.Click += ClearLogMenuItem_Click;
            // 
            // viewMenu
            // 
            //viewMenu.DropDownItems.AddRange(new ToolStripItem[] { themeMenu });
            //viewMenu.Name = "viewMenu";
            //viewMenu.Size = new Size(86, 28);
            //viewMenu.Text = "视图(&V)";
            // 
            // themeMenu
            // 
            //themeMenu.DropDownItems.AddRange(new ToolStripItem[] { lightThemeMenuItem, darkThemeMenuItem, systemThemeMenuItem });
            //themeMenu.Name = "themeMenu";
            //themeMenu.Size = new Size(168, 34);
            //themeMenu.Text = "主题(&T)";
            // 
            // lightThemeMenuItem
            // 
            //lightThemeMenuItem.Checked = true;
            //lightThemeMenuItem.CheckState = CheckState.Checked;
            //lightThemeMenuItem.Name = "lightThemeMenuItem";
            //lightThemeMenuItem.Size = new Size(208, 34);
            //lightThemeMenuItem.Text = "浅色主题(&L)";
            //lightThemeMenuItem.Click += LightThemeMenuItem_Click;
            // 
            // darkThemeMenuItem
            // 
            //darkThemeMenuItem.Name = "darkThemeMenuItem";
            //darkThemeMenuItem.Size = new Size(208, 34);
            //darkThemeMenuItem.Text = "深色主题(&D)";
            //darkThemeMenuItem.Click += DarkThemeMenuItem_Click;
            //// 
            //// systemThemeMenuItem
            //// 
            //systemThemeMenuItem.Name = "systemThemeMenuItem";
            //systemThemeMenuItem.Size = new Size(208, 34);
            //systemThemeMenuItem.Text = "跟随系统(&S)";
            //systemThemeMenuItem.Click += SystemThemeMenuItem_Click;
            //// 
            //// helpMenu
            //// 
            //helpMenu.DropDownItems.AddRange(new ToolStripItem[] { aboutMenuItem });
            //helpMenu.Name = "helpMenu";
            //helpMenu.Size = new Size(88, 28);
            //helpMenu.Text = "帮助(&H)";
            //// 
            //// aboutMenuItem
            //// 
            //aboutMenuItem.Name = "aboutMenuItem";
            //aboutMenuItem.ShortcutKeys = Keys.F1;
            //aboutMenuItem.Size = new Size(232, 34);
            //aboutMenuItem.Text = "❓ 关于(&A)";
            //aboutMenuItem.ToolTipText = "显示关于信息";
            //aboutMenuItem.Click += AboutMenuItem_Click;
            // 
            // mainStatusStrip
            // 
            mainStatusStrip.ImageScalingSize = new Size(24, 24);
            mainStatusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar });
            mainStatusStrip.Location = new Point(0, 556);
            mainStatusStrip.Name = "mainStatusStrip";
            mainStatusStrip.Size = new Size(1200, 33);
            mainStatusStrip.TabIndex = 9;
            mainStatusStrip.Text = "状态栏";
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(725, 26);
            statusLabel.Spring = true;
            statusLabel.Text = "就绪";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            progressBar.Alignment = ToolStripItemAlignment.Right;
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(200, 25);
            // 
            // mainSplitContainer
            // 
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Location = new Point(0, 32);
            mainSplitContainer.Name = "mainSplitContainer";
            // 
            // mainSplitContainer.Panel1
            // 
            mainSplitContainer.Panel1.Controls.Add(leftPanel);
            mainSplitContainer.Panel1MinSize = 100;
            // 
            // mainSplitContainer.Panel2
            // 
            mainSplitContainer.Panel2.Controls.Add(rightSplitContainer);
            mainSplitContainer.Panel2MinSize = 100;
            mainSplitContainer.Size = new Size(1200, 524);
            mainSplitContainer.SplitterDistance = 400;
            mainSplitContainer.TabIndex = 7;
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(fileListBox);
            leftPanel.Controls.Add(button_categorized_export);
            leftPanel.Controls.Add(button_export);
            leftPanel.Controls.Add(button_upload);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new Size(400, 524);
            leftPanel.TabIndex = 6;
            // 
            // fileListBox
            // 
            fileListBox.Dock = DockStyle.Fill;
            fileListBox.Font = new Font("微软雅黑", 10F);
            fileListBox.FormattingEnabled = true;
            fileListBox.Location = new Point(0, 0);
            fileListBox.Name = "fileListBox";
            fileListBox.Size = new Size(400, 524);
            fileListBox.TabIndex = 2;
            // 
            // button_categorized_export
            // 
            button_categorized_export.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            button_categorized_export.Location = new Point(390, 10);
            button_categorized_export.Name = "button_categorized_export";
            button_categorized_export.Size = new Size(200, 45);
            button_categorized_export.TabIndex = 2;
            button_categorized_export.Text = "🗂️ 分类导出ST脚本";
            button_categorized_export.UseVisualStyleBackColor = true;
            button_categorized_export.Click += button_categorized_export_Click;
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
            // rightSplitContainer
            // 
            rightSplitContainer.Dock = DockStyle.Fill;
            rightSplitContainer.Location = new Point(0, 0);
            rightSplitContainer.Name = "rightSplitContainer";
            rightSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // rightSplitContainer.Panel1
            // 
            rightSplitContainer.Panel1.Controls.Add(previewTabControl);
            rightSplitContainer.Panel1MinSize = 100;
            // 
            // rightSplitContainer.Panel2
            // 
            rightSplitContainer.Panel2.Controls.Add(richTextBox1);
            rightSplitContainer.Panel2.Controls.Add(logFilterPanel);
            rightSplitContainer.Panel2MinSize = 100;
            rightSplitContainer.Size = new Size(796, 524);
            rightSplitContainer.SplitterDistance = 262;
            rightSplitContainer.TabIndex = 8;
            // 
            // previewTabControl
            // 
            previewTabControl.Dock = DockStyle.Fill;
            previewTabControl.Font = new Font("微软雅黑", 10F);
            previewTabControl.Location = new Point(0, 0);
            previewTabControl.Name = "previewTabControl";
            previewTabControl.SelectedIndex = 0;
            previewTabControl.Size = new Size(796, 262);
            previewTabControl.TabIndex = 3;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Font = new Font("Consolas", 10F);
            richTextBox1.Location = new Point(0, 35);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(796, 223);
            richTextBox1.TabIndex = 5;
            richTextBox1.Text = "";
            // 
            // logFilterPanel
            // 
            logFilterPanel.Controls.Add(clearLogButton);
            logFilterPanel.Controls.Add(logFilterComboBox);
            logFilterPanel.Controls.Add(logSearchBox);
            logFilterPanel.Dock = DockStyle.Top;
            logFilterPanel.Location = new Point(0, 0);
            logFilterPanel.Name = "logFilterPanel";
            logFilterPanel.Size = new Size(796, 35);
            logFilterPanel.TabIndex = 10;
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
            // logFilterComboBox
            // 
            logFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            logFilterComboBox.Items.AddRange(new object[] { "全部", "信息", "成功", "警告", "错误", "调试" });
            logFilterComboBox.Location = new Point(215, 7);
            logFilterComboBox.Name = "logFilterComboBox";
            logFilterComboBox.Size = new Size(100, 32);
            logFilterComboBox.TabIndex = 1;
            // 
            // logSearchBox
            // 
            logSearchBox.Location = new Point(5, 7);
            logSearchBox.Name = "logSearchBox";
            logSearchBox.PlaceholderText = "🔍 搜索日志...";
            logSearchBox.Size = new Size(200, 30);
            logSearchBox.TabIndex = 0;
            // 
            //NEED DELETE: 遗留配置面板（未用于导入/导出链路）
            // configPanel
            // 
            configPanel.Dock = DockStyle.Fill;
            configPanel.Location = new Point(0, 0);
            configPanel.Name = "configPanel";
            configPanel.Size = new Size(300, 400);
            configPanel.TabIndex = 4;
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
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            mainStatusStrip.ResumeLayout(false);
            mainStatusStrip.PerformLayout();
            mainSplitContainer.Panel1.ResumeLayout(false);
            mainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
            mainSplitContainer.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            rightSplitContainer.Panel1.ResumeLayout(false);
            rightSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)rightSplitContainer).EndInit();
            rightSplitContainer.ResumeLayout(false);
            logFilterPanel.ResumeLayout(false);
            logFilterPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // 原有控件
        private Button button_upload;
        private Button button_export;
        private Button button_categorized_export;
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
        
        
        // 帮助菜单项
        private ToolStripMenuItem aboutMenuItem;
    }
}
