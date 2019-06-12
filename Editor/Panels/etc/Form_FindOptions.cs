﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Win32;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using System.Drawing;
//using System.Linq;
//using System.Xml.Linq;

using Au;
using Au.Types;
using static Au.AStatic;
using Au.Controls;
using static Program;

class Form_FindOptions : AuFormBase
{
	public Form_FindOptions()
	{
		InitializeComponent();
	}

	private AuLabel auLabel1;
	private AuButtonOK _bOK;
	private AuButtonCancel _bCancel;
	internal System.Windows.Forms.TextBox _tSkip;
	private ToolTip _toolTip;

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
			this._tSkip = new System.Windows.Forms.TextBox();
			this.auLabel1 = new Au.Controls.AuLabel();
			this._bOK = new Au.Controls.AuButtonOK();
			this._bCancel = new Au.Controls.AuButtonCancel();
			this._toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// _tSkip
			// 
			this._tSkip.AcceptsReturn = true;
			this._tSkip.Location = new System.Drawing.Point(8, 32);
			this._tSkip.Multiline = true;
			this._tSkip.Name = "_tSkip";
			this._tSkip.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this._tSkip.Size = new System.Drawing.Size(408, 116);
			this._tSkip.TabIndex = 3;
			this._toolTip.SetToolTip(this._tSkip, "Examples:\r\n*.xml\r\n*.manifest\r\n\\Folder\\*\r\n\\Folder\\Subfolder\\*\r\n\r\nDon\'t need to specify these image types:\r\npng bmp jpg gif tif ico cur ani");
			this._tSkip.WordWrap = false;
			// 
			// auLabel1
			// 
			this.auLabel1.Location = new System.Drawing.Point(8, 8);
			this.auLabel1.Name = "auLabel1";
			this.auLabel1.Size = new System.Drawing.Size(408, 24);
			this.auLabel1.TabIndex = 2;
			this.auLabel1.Text = "Don\'t find/replace in files where path matches one of these wildcards";
			// 
			// _bOK
			// 
			this._bOK.Location = new System.Drawing.Point(264, 204);
			this._bOK.Name = "_bOK";
			this._bOK.Size = new System.Drawing.Size(72, 24);
			this._bOK.TabIndex = 0;
			// 
			// _bCancel
			// 
			this._bCancel.Location = new System.Drawing.Point(344, 204);
			this._bCancel.Name = "_bCancel";
			this._bCancel.Size = new System.Drawing.Size(72, 24);
			this._bCancel.TabIndex = 1;
			// 
			// _toolTip
			// 
			this._toolTip.AutomaticDelay = 100;
			this._toolTip.AutoPopDelay = 10000;
			this._toolTip.InitialDelay = 100;
			this._toolTip.ReshowDelay = 20;
			// 
			// Form_FindOptions
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(424, 236);
			this.Controls.Add(this._bCancel);
			this.Controls.Add(this._bOK);
			this.Controls.Add(this.auLabel1);
			this.Controls.Add(this._tSkip);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form_FindOptions";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Find/Replace Options";
			this.ResumeLayout(false);
			this.PerformLayout();

	}

	#endregion

	//protected override void OnLoad(EventArgs e)
	//{
	//	base.OnLoad(e);
	//}

	//private void _bOK_Click(object sender, EventArgs e)
	//{

	//}
}
