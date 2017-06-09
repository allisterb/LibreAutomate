using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Xml.Linq;
//using System.Xml.XPath;

using Catkeys;
using static Catkeys.NoClass;
using static Program;

static class CommandLine
{
	/// <summary>
	/// Processes command line.
	/// Returns true if finds previous program instance. Then this instance must exit. Then sends the command line to it if need.
	/// </summary>
	public static bool OnProgramStarted(string[] a)
	{
		string s = null;
		int cmd = 0;
		if(a.Length > 0) {
			//Print(a);

			for(int i = 0; i < a.Length; i++) if(a[i].StartsWith_('-')) a[i] = a[i].ReplaceAt_(0, 1, "/");

			s = a[0];
			if(s.StartsWith_('/')) {

			} else { //one or more files
				if(a.Length == 1 && FilesModel.IsCollectionDirectory(s)) {
					switch(cmd = TaskDialog.ShowEx("Collection", s, "1 Import|2 Open|0 Cancel", flags: TDFlags.Wider, footerText: FilesModel.GetSecurityInfo(true))) {
					case 1: _importCollection = s; break;
					case 2: CollectionDirectory = s; break;
					}
				} else {
					cmd = 3;
					_importFiles = a;
				}
			}
		}

		var w = Wnd.FindFast(null, _msgClass);
		if(!w.Is0) {
			//activate main window
			if(a.Length == 0) {
				try {
					Wnd wMain = Wnd.Misc.FindThreadWindow(w.ThreadId, "Catkeys*", "WindowsForms*", WFFlags.HiddenToo, t => !t.IsPopupWindow);
					if(!wMain.Is0) wMain.Activate();
				}
				catch { }
			}

			switch(cmd) {
			case 3:
				s = string.Join("\0", a);
				break;
			}
			if(cmd != 0) {
				Wnd.Misc.InterProcessSendData(w, cmd, s);
			}
			return true;
		}

		Wnd.Misc.InterProcessEnableReceivingWM_COPYDATA();
		Wnd.Misc.WindowClass.InterDomainRegister(_msgClass, _WndProc);
		_msgWnd = Wnd.Misc.WindowClass.InterDomainCreateMessageWindow(_msgClass);
		return false;
	}

	public static void OnAfterCreatedFormAndOpenedCollection()
	{
		try {
			if(_importCollection != null) MainForm.Model.ImportCollection(_importCollection);
			else if(_importFiles != null) MainForm.Model.ImportFiles(_importFiles);
		}
		catch(Exception ex) { Print(ex.Message); }
	}

	/// <summary>
	/// null or collection file specified in command line.
	/// </summary>
	public static string CollectionDirectory;

	static string _importCollection;
	static string[] _importFiles;

	const string _msgClass = "CatEdit.Command";
	static Wnd _msgWnd;

	static LPARAM _WndProc(Wnd w, uint msg, LPARAM wParam, LPARAM lParam)
	{
		switch(msg) {
		case Api.WM_COPYDATA:
			try { return _WmCopyData(wParam, lParam); }
			catch(Exception ex) { Print(ex.Message); }
			return false;
		}

		return Wnd.Misc.DefWindowProc(w, msg, wParam, lParam);
	}

	static bool _WmCopyData(LPARAM wParam, LPARAM lParam)
	{
		//Wnd wSender = (Wnd)wParam;
		var s = Wnd.Misc.InterProcessGetData(lParam, out int id);
		//Print(id);
		switch(id) {
		case 1:
			MainForm.Model.ImportCollection(s);
			break;
		case 2:
			MainForm.Panels.Files.LoadCollection(s);
			break;
		case 3:
			Api.ReplyMessage(1); //avoid 'wait' cursor while we'll show task dialog
			MainForm.Model.ImportFiles(s.Split('\0'));
			break;
		default:
			Debug.Assert(false);
			return false;
		}
		return true;
	}
}
