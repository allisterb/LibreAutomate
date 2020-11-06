﻿namespace Au.Tools
{
	partial class FormAWnd
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
			if(disposing && (components != null)) {
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
			this.components = new System.ComponentModel.Container();
			this._info = new Au.Controls.InfoBox();
			this._lSpeed = new System.Windows.Forms.Label();
			this._bTest = new Au.Controls.AuButton();
			this._bOK = new Au.Controls.AuButtonOK();
			this._bCancel = new Au.Controls.AuButtonCancel();
			this._cCapture = new Au.Controls.AuCheckBox();
			this.splitContainer1 = new Au.Controls.AuSplitContainer();
			this.splitContainer2 = new Au.Controls.AuSplitContainer();
			this._grid = new Au.Controls.ParamGrid();
			this._grid2 = new Au.Controls.ParamGrid();
			this.splitContainer3 = new Au.Controls.AuSplitContainer();
			this._code = new Au.Tools.CodeBox();
			this.splitContainer4 = new Au.Controls.AuSplitContainer();
			this._tree = new Aga.Controls.Tree.TreeViewAdv();
			this._winInfo = new Au.Controls.InfoBox();
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
			this.splitContainer3.Panel1.SuspendLayout();
			this.splitContainer3.Panel2.SuspendLayout();
			this.splitContainer3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
			this.splitContainer4.Panel1.SuspendLayout();
			this.splitContainer4.Panel2.SuspendLayout();
			this.splitContainer4.SuspendLayout();
			this.SuspendLayout();
			// 
			// _info
			// 
			this._info.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._info.Location = new System.Drawing.Point(8, 8);
			this._info.Name = "_info";
			this._info.Size = new System.Drawing.Size(600, 56);
			this._info.TabIndex = 6;
			// 
			// _lSpeed
			// 
			this._lSpeed.AutoSize = true;
			this._lSpeed.Location = new System.Drawing.Point(87, 77);
			this._lSpeed.Name = "_lSpeed";
			this._lSpeed.Size = new System.Drawing.Size(0, 15);
			this._lSpeed.TabIndex = 7;
			this._toolTip.SetToolTip(this._lSpeed, "Shows the Test execution time. Red if not found.");
			// 
			// _bTest
			// 
			this._bTest.Enabled = false;
			this._bTest.Location = new System.Drawing.Point(8, 72);
			this._bTest.Name = "_bTest";
			this._bTest.Size = new System.Drawing.Size(72, 24);
			this._bTest.TabIndex = 9;
			this._bTest.Text = "&Test";
			this._toolTip.SetToolTip(this._bTest, "Executes the \'find\' code (without wait, etc). If window/control found, shows its " +
        "rectangle.");
			this._bTest.Click += new System.EventHandler(this._bTest_Click);
			// 
			// _bOK
			// 
			this._bOK.Enabled = false;
			this._bOK.Location = new System.Drawing.Point(192, 72);
			this._bOK.Name = "_bOK";
			this._bOK.Size = new System.Drawing.Size(72, 24);
			this._bOK.TabIndex = 10;
			this._bOK.Click += new System.EventHandler(this._bOK_Click);
			// 
			// _bCancel
			// 
			this._bCancel.Location = new System.Drawing.Point(272, 72);
			this._bCancel.Name = "_bCancel";
			this._bCancel.Size = new System.Drawing.Size(72, 24);
			this._bCancel.TabIndex = 11;
			// 
			// _cCapture
			// 
			this._cCapture.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._cCapture.Location = new System.Drawing.Point(536, 75);
			this._cCapture.Name = "_cCapture";
			this._cCapture.Size = new System.Drawing.Size(80, 19);
			this._cCapture.TabIndex = 8;
			this._cCapture.Text = "&Capture";
			this._toolTip.SetToolTip(this._cCapture, "Enables key F3. Shows window/control rectangles when moving the mouse.");
			this._cCapture.CheckedChanged += new System.EventHandler(this._cCapture_CheckedChanged);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer1.Location = new System.Drawing.Point(8, 104);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.splitContainer3);
			this.splitContainer1.Size = new System.Drawing.Size(600, 448);
			this.splitContainer1.SplitterDistance = 195;
			this.splitContainer1.SplitterWidth = 8;
			this.splitContainer1.TabIndex = 13;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this._grid);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this._grid2);
			this.splitContainer2.Size = new System.Drawing.Size(600, 195);
			this.splitContainer2.SplitterDistance = 405;
			this.splitContainer2.TabIndex = 0;
			// 
			// _grid
			// 
			this._grid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this._grid.ClipboardMode = SourceGrid.ClipboardMode.Copy;
			this._grid.ColumnsCount = 2;
			this._grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this._grid.EnableSort = false;
			this._grid.Location = new System.Drawing.Point(0, 0);
			this._grid.MinimumHeight = 18;
			this._grid.Name = "_grid";
			this._grid.OptimizeMode = SourceGrid.CellOptimizeMode.ForRows;
			this._grid.SelectionMode = SourceGrid.GridSelectionMode.Cell;
			this._grid.Size = new System.Drawing.Size(405, 195);
			this._grid.SpecialKeys = ((SourceGrid.GridSpecialKeys)((((((SourceGrid.GridSpecialKeys.Arrows | SourceGrid.GridSpecialKeys.PageDownUp) 
            | SourceGrid.GridSpecialKeys.Enter) 
            | SourceGrid.GridSpecialKeys.Escape) 
            | SourceGrid.GridSpecialKeys.Control) 
            | SourceGrid.GridSpecialKeys.Shift)));
			this._grid.TabIndex = 0;
			this._grid.TabStop = true;
			this._grid.ToolTipText = "";
			this._grid.ZAddHidden = false;
			// 
			// _grid2
			// 
			this._grid2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this._grid2.ClipboardMode = SourceGrid.ClipboardMode.Copy;
			this._grid2.ColumnsCount = 2;
			this._grid2.Dock = System.Windows.Forms.DockStyle.Fill;
			this._grid2.EnableSort = false;
			this._grid2.Location = new System.Drawing.Point(0, 0);
			this._grid2.MinimumHeight = 18;
			this._grid2.Name = "_grid2";
			this._grid2.OptimizeMode = SourceGrid.CellOptimizeMode.ForRows;
			this._grid2.SelectionMode = SourceGrid.GridSelectionMode.Cell;
			this._grid2.Size = new System.Drawing.Size(191, 195);
			this._grid2.SpecialKeys = ((SourceGrid.GridSpecialKeys)((((((SourceGrid.GridSpecialKeys.Arrows | SourceGrid.GridSpecialKeys.PageDownUp) 
            | SourceGrid.GridSpecialKeys.Enter) 
            | SourceGrid.GridSpecialKeys.Escape) 
            | SourceGrid.GridSpecialKeys.Control) 
            | SourceGrid.GridSpecialKeys.Shift)));
			this._grid2.TabIndex = 0;
			this._grid2.TabStop = true;
			this._grid2.ToolTipText = "";
			this._grid2.ZAddHidden = false;
			// 
			// splitContainer3
			// 
			this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer3.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer3.Location = new System.Drawing.Point(0, 0);
			this.splitContainer3.Name = "splitContainer3";
			this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer3.Panel1
			// 
			this.splitContainer3.Panel1.Controls.Add(this._code);
			// 
			// splitContainer3.Panel2
			// 
			this.splitContainer3.Panel2.Controls.Add(this.splitContainer4);
			this.splitContainer3.Size = new System.Drawing.Size(600, 245);
			this.splitContainer3.SplitterDistance = 60;
			this.splitContainer3.TabIndex = 0;
			// 
			// _code
			// 
			this._code.Dock = System.Windows.Forms.DockStyle.Fill;
			this._code.Location = new System.Drawing.Point(0, 0);
			this._code.Name = "_code";
			this._code.Size = new System.Drawing.Size(600, 60);
			this._code.TabIndex = 12;
			// 
			// splitContainer4
			// 
			this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer4.Location = new System.Drawing.Point(0, 0);
			this.splitContainer4.Name = "splitContainer4";
			// 
			// splitContainer4.Panel1
			// 
			this.splitContainer4.Panel1.Controls.Add(this._tree);
			// 
			// splitContainer4.Panel2
			// 
			this.splitContainer4.Panel2.Controls.Add(this._winInfo);
			this.splitContainer4.Size = new System.Drawing.Size(600, 181);
			this.splitContainer4.SplitterDistance = 296;
			this.splitContainer4.TabIndex = 0;
			// 
			// _tree
			// 
			this._tree.ZAccessibleCount = 1000;
			this._tree.AccessibleName = "_tree";
			this._tree.BackColor = System.Drawing.SystemColors.Window;
			this._tree.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this._tree.DefaultToolTipProvider = null;
			this._tree.Dock = System.Windows.Forms.DockStyle.Fill;
			this._tree.DragDropMarkColor = System.Drawing.Color.MidnightBlue;
			this._tree.LineColor = System.Drawing.SystemColors.ControlDark;
			this._tree.Location = new System.Drawing.Point(0, 0);
			this._tree.Model = null;
			this._tree.Name = "_tree";
			this._tree.SelectedNode = null;
			this._tree.Size = new System.Drawing.Size(296, 181);
			this._tree.TabIndex = 0;
			// 
			// _winInfo
			// 
			this._winInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			this._winInfo.ZInitBorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this._winInfo.Location = new System.Drawing.Point(0, 0);
			this._winInfo.Name = "_winInfo";
			this._winInfo.Size = new System.Drawing.Size(300, 181);
			this._winInfo.TabIndex = 0;
			this._winInfo.ZWrapLines = false;
			// 
			// FormAWnd
			// 
			this.AcceptButton = this._bOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.CancelButton = this._bCancel;
			this.ClientSize = new System.Drawing.Size(616, 560);
			this.Controls.Add(this._info);
			this.Controls.Add(this._lSpeed);
			this.Controls.Add(this._bTest);
			this.Controls.Add(this._bCancel);
			this.Controls.Add(this._bOK);
			this.Controls.Add(this._cCapture);
			this.Controls.Add(this.splitContainer1);
			this.MinimumSize = new System.Drawing.Size(500, 398);
			this.Name = "FormAWnd";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Find window or control";
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.splitContainer3.Panel1.ResumeLayout(false);
			this.splitContainer3.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
			this.splitContainer3.ResumeLayout(false);
			this.splitContainer4.Panel1.ResumeLayout(false);
			this.splitContainer4.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
			this.splitContainer4.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private CodeBox _code;
		private Controls.InfoBox _info;
		private System.Windows.Forms.Label _lSpeed;
		private Au.Controls.AuButton _bTest;
		private Au.Controls.AuButtonOK _bOK;
		private Au.Controls.AuButtonCancel _bCancel;
		private Au.Controls.AuCheckBox _cCapture;
		private Au.Controls.AuSplitContainer splitContainer1;
		private Au.Controls.AuSplitContainer splitContainer2;
		private Controls.ParamGrid _grid;
		private Controls.ParamGrid _grid2;
		private Aga.Controls.Tree.TreeViewAdv _tree;
		private Au.Controls.AuSplitContainer splitContainer3;
		private Au.Controls.AuSplitContainer splitContainer4;
		private Controls.InfoBox _winInfo;
		private System.Windows.Forms.ToolTip _toolTip;
	}
}