namespace Emul809or
{
    partial class popupViewer
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
            lstLines = new System.Windows.Forms.ListBox();
            SuspendLayout();
            // 
            // lstLines
            // 
            lstLines.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lstLines.FormattingEnabled = true;
            lstLines.ItemHeight = 15;
            lstLines.Location = new System.Drawing.Point(12, 12);
            lstLines.Name = "lstLines";
            lstLines.Size = new System.Drawing.Size(776, 424);
            lstLines.TabIndex = 0;
            // 
            // popupViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(lstLines);
            Name = "popupViewer";
            Text = "popupViewer";
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ListBox lstLines;
    }
}