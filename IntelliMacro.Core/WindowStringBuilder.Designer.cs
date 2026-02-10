namespace IntelliMacro.Core
{
    partial class WindowStringBuilder
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WindowStringBuilder));
            this.ok = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.resultBox = new System.Windows.Forms.TextBox();
            this.crosshair = new ManagedWinapi.Crosshair();
            this.label1 = new System.Windows.Forms.Label();
            this.relative = new System.Windows.Forms.CheckBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.itemList = new System.Windows.Forms.ListBox();
            this.errorLabel = new System.Windows.Forms.Label();
            this.editBox = new System.Windows.Forms.TextBox();
            this.itemValues = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.accobj = new System.Windows.Forms.CheckBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ok
            // 
            this.ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.ok.Location = new System.Drawing.Point(497, 314);
            this.ok.Name = "ok";
            this.ok.Size = new System.Drawing.Size(75, 23);
            this.ok.TabIndex = 0;
            this.ok.Text = "OK";
            this.ok.UseVisualStyleBackColor = true;
            this.ok.Click += new System.EventHandler(this.ok_Click);
            // 
            // cancel
            // 
            this.cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Location = new System.Drawing.Point(416, 314);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(75, 23);
            this.cancel.TabIndex = 1;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            // 
            // resultBox
            // 
            this.resultBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.resultBox.Location = new System.Drawing.Point(12, 316);
            this.resultBox.Name = "resultBox";
            this.resultBox.ReadOnly = true;
            this.resultBox.Size = new System.Drawing.Size(398, 20);
            this.resultBox.TabIndex = 2;
            // 
            // crosshair
            // 
            this.crosshair.Location = new System.Drawing.Point(12, 12);
            this.crosshair.Name = "crosshair";
            this.crosshair.Size = new System.Drawing.Size(36, 36);
            this.crosshair.TabIndex = 3;
            this.crosshair.CrosshairDragged += new System.EventHandler(this.crosshair_CrosshairDragged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(54, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(490, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Drag the crosshair control over the window to build a window string for it. Selec" +
                "t an item of it and edit it.";
            // 
            // relative
            // 
            this.relative.AutoSize = true;
            this.relative.Location = new System.Drawing.Point(57, 31);
            this.relative.Name = "relative";
            this.relative.Size = new System.Drawing.Size(344, 17);
            this.relative.TabIndex = 5;
            this.relative.Text = "Build a relative window path (used for commands like MouseRelCtrl)";
            this.relative.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(12, 54);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.itemList);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.errorLabel);
            this.splitContainer1.Panel2.Controls.Add(this.editBox);
            this.splitContainer1.Panel2.Controls.Add(this.itemValues);
            this.splitContainer1.Size = new System.Drawing.Size(560, 254);
            this.splitContainer1.SplitterDistance = 112;
            this.splitContainer1.TabIndex = 6;
            // 
            // itemList
            // 
            this.itemList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.itemList.FormattingEnabled = true;
            this.itemList.IntegralHeight = false;
            this.itemList.Location = new System.Drawing.Point(0, 0);
            this.itemList.Name = "itemList";
            this.itemList.Size = new System.Drawing.Size(112, 254);
            this.itemList.TabIndex = 0;
            this.itemList.SelectedIndexChanged += new System.EventHandler(this.itemList_SelectedIndexChanged);
            this.itemList.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.itemList_Format);
            // 
            // errorLabel
            // 
            this.errorLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.errorLabel.ForeColor = System.Drawing.Color.Red;
            this.errorLabel.Location = new System.Drawing.Point(3, 26);
            this.errorLabel.Name = "errorLabel";
            this.errorLabel.Size = new System.Drawing.Size(401, 15);
            this.errorLabel.TabIndex = 2;
            this.errorLabel.Text = "Errors appear here.";
            // 
            // editBox
            // 
            this.editBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.editBox.Location = new System.Drawing.Point(3, 3);
            this.editBox.Name = "editBox";
            this.editBox.Size = new System.Drawing.Size(438, 20);
            this.editBox.TabIndex = 1;
            this.editBox.TextChanged += new System.EventHandler(this.editBox_TextChanged);
            // 
            // itemValues
            // 
            this.itemValues.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.itemValues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.itemValues.FullRowSelect = true;
            this.itemValues.HideSelection = false;
            this.itemValues.Location = new System.Drawing.Point(3, 44);
            this.itemValues.Name = "itemValues";
            this.itemValues.Size = new System.Drawing.Size(438, 207);
            this.itemValues.TabIndex = 0;
            this.itemValues.UseCompatibleStateImageBehavior = false;
            this.itemValues.View = System.Windows.Forms.View.Details;
            this.itemValues.DoubleClick += new System.EventHandler(this.itemValues_DoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 100;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Value";
            this.columnHeader2.Width = 300;
            // 
            // accobj
            // 
            this.accobj.AutoSize = true;
            this.accobj.Location = new System.Drawing.Point(407, 31);
            this.accobj.Name = "accobj";
            this.accobj.Size = new System.Drawing.Size(151, 17);
            this.accobj.TabIndex = 7;
            this.accobj.Text = "&Include AccessibleObjects";
            this.accobj.UseVisualStyleBackColor = true;
            // 
            // WindowStringBuilder
            // 
            this.AcceptButton = this.ok;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancel;
            this.ClientSize = new System.Drawing.Size(584, 349);
            this.Controls.Add(this.accobj);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.relative);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.crosshair);
            this.Controls.Add(this.resultBox);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.ok);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "WindowStringBuilder";
            this.Text = "Window String Builder";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.TextBox resultBox;
        private ManagedWinapi.Crosshair crosshair;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox relative;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListBox itemList;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.TextBox editBox;
        private System.Windows.Forms.ListView itemValues;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.CheckBox accobj;
    }
}