namespace WinFormsAgentUI
{
    partial class AgentDebug
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
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            RootTableLayoutPanel = new TableLayoutPanel();
            MainTableLayoutPanel = new TableLayoutPanel();
            ChatRichTextBox = new RichTextBox();
            panel1 = new Panel();
            InputTableLayoutPanel = new TableLayoutPanel();
            InputButtonTableLayoutPanel = new TableLayoutPanel();
            SendButton = new Button();
            AddFileButton = new Button();
            InputRichTextBox = new RichTextBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            SystemRichTextBox = new RichTextBox();
            listBox1 = new ListBox();
            menuStrip1.SuspendLayout();
            RootTableLayoutPanel.SuspendLayout();
            MainTableLayoutPanel.SuspendLayout();
            panel1.SuspendLayout();
            InputTableLayoutPanel.SuspendLayout();
            InputButtonTableLayoutPanel.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(840, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // statusStrip1
            // 
            statusStrip1.Location = new Point(0, 527);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(840, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // RootTableLayoutPanel
            // 
            RootTableLayoutPanel.ColumnCount = 2;
            RootTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            RootTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            RootTableLayoutPanel.Controls.Add(MainTableLayoutPanel, 0, 0);
            RootTableLayoutPanel.Controls.Add(tableLayoutPanel1, 1, 0);
            RootTableLayoutPanel.Dock = DockStyle.Fill;
            RootTableLayoutPanel.Location = new Point(0, 24);
            RootTableLayoutPanel.Name = "RootTableLayoutPanel";
            RootTableLayoutPanel.RowCount = 1;
            RootTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            RootTableLayoutPanel.Size = new Size(840, 503);
            RootTableLayoutPanel.TabIndex = 2;
            // 
            // MainTableLayoutPanel
            // 
            MainTableLayoutPanel.AutoScroll = true;
            MainTableLayoutPanel.ColumnCount = 1;
            MainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            MainTableLayoutPanel.Controls.Add(ChatRichTextBox, 0, 0);
            MainTableLayoutPanel.Controls.Add(panel1, 0, 1);
            MainTableLayoutPanel.Dock = DockStyle.Fill;
            MainTableLayoutPanel.Location = new Point(3, 3);
            MainTableLayoutPanel.Name = "MainTableLayoutPanel";
            MainTableLayoutPanel.RowCount = 2;
            MainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 73.64185F));
            MainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 26.3581486F));
            MainTableLayoutPanel.Size = new Size(414, 497);
            MainTableLayoutPanel.TabIndex = 0;
            // 
            // ChatRichTextBox
            // 
            ChatRichTextBox.Dock = DockStyle.Fill;
            ChatRichTextBox.Location = new Point(3, 3);
            ChatRichTextBox.Name = "ChatRichTextBox";
            ChatRichTextBox.ReadOnly = true;
            ChatRichTextBox.Size = new Size(408, 360);
            ChatRichTextBox.TabIndex = 0;
            ChatRichTextBox.Text = "";
            // 
            // panel1
            // 
            panel1.AutoScroll = true;
            panel1.Controls.Add(InputTableLayoutPanel);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 369);
            panel1.Name = "panel1";
            panel1.Size = new Size(408, 125);
            panel1.TabIndex = 1;
            // 
            // InputTableLayoutPanel
            // 
            InputTableLayoutPanel.ColumnCount = 1;
            InputTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            InputTableLayoutPanel.Controls.Add(InputButtonTableLayoutPanel, 0, 1);
            InputTableLayoutPanel.Controls.Add(InputRichTextBox, 0, 0);
            InputTableLayoutPanel.Dock = DockStyle.Fill;
            InputTableLayoutPanel.Location = new Point(0, 0);
            InputTableLayoutPanel.Name = "InputTableLayoutPanel";
            InputTableLayoutPanel.RowCount = 2;
            InputTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            InputTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            InputTableLayoutPanel.Size = new Size(408, 125);
            InputTableLayoutPanel.TabIndex = 0;
            // 
            // InputButtonTableLayoutPanel
            // 
            InputButtonTableLayoutPanel.ColumnCount = 3;
            InputButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10.0917435F));
            InputButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 89.90826F));
            InputButtonTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));
            InputButtonTableLayoutPanel.Controls.Add(SendButton, 2, 0);
            InputButtonTableLayoutPanel.Controls.Add(AddFileButton, 0, 0);
            InputButtonTableLayoutPanel.Dock = DockStyle.Fill;
            InputButtonTableLayoutPanel.Location = new Point(3, 98);
            InputButtonTableLayoutPanel.MaximumSize = new Size(0, 40);
            InputButtonTableLayoutPanel.MinimumSize = new Size(0, 30);
            InputButtonTableLayoutPanel.Name = "InputButtonTableLayoutPanel";
            InputButtonTableLayoutPanel.RowCount = 1;
            InputButtonTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            InputButtonTableLayoutPanel.Size = new Size(402, 30);
            InputButtonTableLayoutPanel.TabIndex = 0;
            // 
            // SendButton
            // 
            SendButton.Dock = DockStyle.Fill;
            SendButton.Location = new Point(330, 3);
            SendButton.MaximumSize = new Size(80, 20);
            SendButton.MinimumSize = new Size(60, 20);
            SendButton.Name = "SendButton";
            SendButton.Size = new Size(69, 20);
            SendButton.TabIndex = 0;
            SendButton.Text = "Send";
            SendButton.UseVisualStyleBackColor = true;
            SendButton.Click += SendButton_Click;
            // 
            // AddFileButton
            // 
            AddFileButton.Location = new Point(3, 3);
            AddFileButton.Name = "AddFileButton";
            AddFileButton.Size = new Size(27, 23);
            AddFileButton.TabIndex = 1;
            AddFileButton.Text = "+";
            AddFileButton.UseVisualStyleBackColor = true;
            AddFileButton.Click += AddFileButton_Click;
            // 
            // InputRichTextBox
            // 
            InputRichTextBox.Dock = DockStyle.Fill;
            InputRichTextBox.Location = new Point(3, 3);
            InputRichTextBox.MinimumSize = new Size(0, 100);
            InputRichTextBox.Name = "InputRichTextBox";
            InputRichTextBox.Size = new Size(402, 100);
            InputRichTextBox.TabIndex = 1;
            InputRichTextBox.Text = "";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(SystemRichTextBox, 0, 1);
            tableLayoutPanel1.Controls.Add(listBox1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(423, 3);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.1991959F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 66.8008041F));
            tableLayoutPanel1.Size = new Size(414, 497);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // SystemRichTextBox
            // 
            SystemRichTextBox.Dock = DockStyle.Fill;
            SystemRichTextBox.Location = new Point(3, 168);
            SystemRichTextBox.Name = "SystemRichTextBox";
            SystemRichTextBox.Size = new Size(408, 326);
            SystemRichTextBox.TabIndex = 0;
            SystemRichTextBox.Text = "";
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.ItemHeight = 15;
            listBox1.Location = new Point(3, 3);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(329, 154);
            listBox1.TabIndex = 1;
            // 
            // AgentDebug
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(840, 549);
            Controls.Add(RootTableLayoutPanel);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "AgentDebug";
            Text = "Lombda Agent UI";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            RootTableLayoutPanel.ResumeLayout(false);
            MainTableLayoutPanel.ResumeLayout(false);
            panel1.ResumeLayout(false);
            InputTableLayoutPanel.ResumeLayout(false);
            InputButtonTableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private StatusStrip statusStrip1;
        private TableLayoutPanel RootTableLayoutPanel;
        private TableLayoutPanel MainTableLayoutPanel;
        private RichTextBox ChatRichTextBox;
        private Panel panel1;
        private TableLayoutPanel InputTableLayoutPanel;
        private TableLayoutPanel InputButtonTableLayoutPanel;
        private Button SendButton;
        private RichTextBox InputRichTextBox;
        private TableLayoutPanel tableLayoutPanel1;
        private RichTextBox SystemRichTextBox;
        private ListBox listBox1;
        private Button AddFileButton;
    }
}
