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
            button_upload = new Button();
            button_export = new Button();
            richTextBox1 = new RichTextBox();
            SuspendLayout();
            // 
            // button_upload
            // 
            button_upload.Font = new Font("宋体", 22F, FontStyle.Bold, GraphicsUnit.Point, 134);
            button_upload.Location = new Point(12, 12);
            button_upload.Name = "button_upload";
            button_upload.Size = new Size(488, 101);
            button_upload.TabIndex = 0;
            button_upload.Text = "上传点表";
            button_upload.UseVisualStyleBackColor = true;
            button_upload.Click += button_upload_Click;
            // 
            // button_export
            // 
            button_export.Font = new Font("宋体", 22F, FontStyle.Bold, GraphicsUnit.Point, 134);
            button_export.Location = new Point(521, 12);
            button_export.Name = "button_export";
            button_export.Size = new Size(430, 101);
            button_export.TabIndex = 1;
            button_export.Text = "导出结果";
            button_export.UseVisualStyleBackColor = true;
            button_export.Click += button_export_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(12, 129);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(939, 929);
            richTextBox1.TabIndex = 2;
            richTextBox1.Text = "";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(963, 1070);
            Controls.Add(richTextBox1);
            Controls.Add(button_export);
            Controls.Add(button_upload);
            Name = "Form1";
            Text = "ST自动生成器";
            ResumeLayout(false);
        }

        #endregion

        private Button button_upload;
        private Button button_export;
        private RichTextBox richTextBox1;
    }
}
