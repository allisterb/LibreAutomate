﻿using System;
using System.Collections.Generic;
using System.Collections;
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
using System.Linq;

using Au;
using Au.Types;
using static Au.AStatic;
using Au.Controls;

partial class FOptions : AuForm
{
	const string c_rkRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
	bool _initRunAtStartup;

	public FOptions()
	{
		InitializeComponent();
	}

	public static void ZShow()
	{
		if(s_form == null) {
			s_form = new FOptions();
			s_form.Show(Program.MainForm);
		} else {
			s_form.Activate();
		}
	}
	static FOptions s_form;

	protected override void OnFormClosed(FormClosedEventArgs e)
	{
		base.OnFormClosed(e);
		s_form = null;
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		_runAtStartup.Checked = _initRunAtStartup = ARegistry.GetString(out _, "Au.Editor", c_rkRun, ARegistry.HKEY_CURRENT_USER);
		_runHidden.Checked = Program.Settings.runHidden;
		_startupScripts.Text = Program.Model.StartupScriptsCsv;

	}

	private void _bOK_Click(object sender, EventArgs e)
	{
		if(_runAtStartup.Checked != _initRunAtStartup) {
			try {
				using var rk = Registry.CurrentUser.OpenSubKey(c_rkRun, true);
				if(_initRunAtStartup) rk.DeleteValue("Au.Editor");
				else rk.SetValue("Au.Editor", "\"" + AFolders.ThisAppBS + "Au.CL.exe\" /e");
			}
			catch(Exception ex) { Print("Failed to change 'Start with Windows'. " + ex.ToStringWithoutStack()); }
		}
		Program.Settings.runHidden = _runHidden.Checked;

		if(_startupScripts.Modified) Program.Model.StartupScriptsCsv = _startupScripts.Text;
	}

	private void _startupScripts_Validating(object sender, CancelEventArgs e)
	{
		//Print("validating");
		_errorProvider.Clear();
		string s = _startupScripts.Text; if(Empty(s)) return;
		string err = null;
		try {
			var t = ACsv.Parse(s);
			if(t.ColumnCount > 2) { err = "Too many commas in a line. If script name contains comma, enclose in \"\"."; goto ge; }
			foreach(var v in t.Data) {
				var script = v[0];
				if(Program.Model.FindScript(script) == null) { err = "Script not found: " + script; break; }
				var delay = v.Length == 1 ? null : v[1];
				if(!Empty(delay)) {
					_rxDelay ??= new ARegex(@"(?i)^\d+ *m?s$");
					if(!_rxDelay.IsMatch(delay)) { err = "Delay must be like 2 s or 500 ms"; break; }
				}
			}
		}
		catch(FormatException ex) { err = ex.Message; }
		ge:
		if(err != null) {
			_errorProvider.SetIconAlignment(_startupScripts, ErrorIconAlignment.TopLeft);
			_errorProvider.SetError(_startupScripts, err);
			e.Cancel = true;
		}
	}
	ARegex _rxDelay;

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		//workaround for:
		//	Cannot close the form with Cancel/X/Esc when failed validation of an edit control.
		//	_bCancel.CausesValidation is false, but somehow it does not prevent validation. I guess it works only with ShowDialog, but we use Show.
		e.Cancel = false;

		base.OnFormClosing(e);
	}
}

//If using DataGridView for startup scripts:
// 
// _gridScripts
// 
//this._gridScripts.AllowUserToResizeRows = false;
//this._gridScripts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
//this._gridScripts.BorderStyle = System.Windows.Forms.BorderStyle.None;
//this._gridScripts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
//this._gridScripts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {

//this._colScript,
//this._colDelay});
//this._gridScripts.Location = new System.Drawing.Point(8, 180);
//this._gridScripts.Name = "_gridScripts";
//this._gridScripts.RowHeadersWidth = 12;
//this._gridScripts.RowTemplate.Height = 19;
//this._gridScripts.Size = new System.Drawing.Size(424, 76);
//this._gridScripts.TabIndex = 4;

//_gridScripts.Rows.Clear(); //sets correct height of the empty "add new" row
//_gridScripts.Rows.Add("one", 1);
//_gridScripts.Rows.Add("two", 2);

//foreach(DataGridViewRow row in _gridScripts.Rows) {
//	Print(row.Cells[0].Value, row.Cells[1].Value);
//}
