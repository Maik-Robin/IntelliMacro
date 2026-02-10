namespace IntelliMacro.CoreCommands
{
    partial class BrowseWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowseWindow));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.backButton = new System.Windows.Forms.ToolStripButton();
            this.stopButton = new System.Windows.Forms.ToolStripLabel();
            this.reloadButton = new System.Windows.Forms.ToolStripButton();
            this.forwardButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.urlText = new System.Windows.Forms.ToolStripTextBox();
            this.goButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.clickActButton = new System.Windows.Forms.ToolStripButton();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.backButton,
            this.stopButton,
            this.reloadButton,
            this.forwardButton,
            this.toolStripSeparator1,
            this.urlText,
            this.goButton,
            this.toolStripSeparator2,
            this.clickActButton});
            this.toolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(792, 25);
            this.toolStrip.TabIndex = 1;
            this.toolStrip.Text = "toolStrip";
            // 
            // backButton
            // 
            this.backButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.backButton.Enabled = false;
            this.backButton.Image = ((System.Drawing.Image)(resources.GetObject("backButton.Image")));
            this.backButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(33, 22);
            this.backButton.Text = "Back";
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            // 
            // stopButton
            // 
            this.stopButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(29, 22);
            this.stopButton.Text = "Stop";
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // reloadButton
            // 
            this.reloadButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.reloadButton.Image = ((System.Drawing.Image)(resources.GetObject("reloadButton.Image")));
            this.reloadButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.reloadButton.Name = "reloadButton";
            this.reloadButton.Size = new System.Drawing.Size(44, 22);
            this.reloadButton.Text = "Reload";
            this.reloadButton.Click += new System.EventHandler(this.reloadButton_Click);
            // 
            // forwardButton
            // 
            this.forwardButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.forwardButton.Enabled = false;
            this.forwardButton.Image = ((System.Drawing.Image)(resources.GetObject("forwardButton.Image")));
            this.forwardButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.forwardButton.Name = "forwardButton";
            this.forwardButton.Size = new System.Drawing.Size(51, 22);
            this.forwardButton.Text = "Forward";
            this.forwardButton.Click += new System.EventHandler(this.forwardButton_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // urlText
            // 
            this.urlText.Name = "urlText";
            this.urlText.Size = new System.Drawing.Size(300, 25);
            this.urlText.Text = "about:blank";
            this.urlText.KeyDown += new System.Windows.Forms.KeyEventHandler(this.urlText_KeyDown);
            this.urlText.Enter += new System.EventHandler(this.urlText_Enter);
            // 
            // goButton
            // 
            this.goButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.goButton.Image = ((System.Drawing.Image)(resources.GetObject("goButton.Image")));
            this.goButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(24, 22);
            this.goButton.Text = "Go";
            this.goButton.Click += new System.EventHandler(this.goButton_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // clickActButton
            // 
            this.clickActButton.CheckOnClick = true;
            this.clickActButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.clickActButton.Image = ((System.Drawing.Image)(resources.GetObject("clickActButton.Image")));
            this.clickActButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.clickActButton.Name = "clickActButton";
            this.clickActButton.Size = new System.Drawing.Size(61, 22);
            this.clickActButton.Text = "Click && Act";
            this.clickActButton.ToolTipText = "Click & Act";
            this.clickActButton.Click += new System.EventHandler(this.clickActButton_Click);
            // 
            // webBrowser
            // 
            this.webBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowser.Location = new System.Drawing.Point(0, 28);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(792, 445);
            this.webBrowser.TabIndex = 0;
            this.webBrowser.Url = new System.Uri("about:blank", System.UriKind.Absolute);
            this.webBrowser.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.webBrowser_Navigating);
            this.webBrowser.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser_DocumentCompleted);
            this.webBrowser.LocationChanged += new System.EventHandler(this.webBrowser_LocationChanged);
            // 
            // BrowseWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 473);
            this.Controls.Add(this.webBrowser);
            this.Controls.Add(this.toolStrip);
            this.Name = "BrowseWindow";
            this.Text = "IntelliMacro.NET Mini Web Browser";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.BrowseWindow_FormClosed);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripLabel stopButton;
        private System.Windows.Forms.ToolStripButton reloadButton;
        private System.Windows.Forms.ToolStripButton forwardButton;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripTextBox urlText;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton goButton;
        private System.Windows.Forms.ToolStripButton clickActButton;
        private System.Windows.Forms.ToolStripButton backButton;
        private System.Windows.Forms.WebBrowser webBrowser;
    }
}