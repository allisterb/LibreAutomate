namespace Au.More;

/// <summary>
/// Contains functions to interact with the script editor, if available.
/// </summary>
/// <remarks>
/// Functions of this class work when editor process is running, even if current process wasn't started from it. To detect whether current process was started from editor, use <see cref="folders.Editor"/> (it is null if not).
/// </remarks>
public static class ScriptEditor {
	/// <summary>
	/// Finds editor's message-only window used with WM_COPYDATA etc.
	/// Uses <see cref="script.s_wndMsg"/> or <see cref="wnd.Cached_"/>.
	/// </summary>
	internal static wnd WndMsg_ {
		get {
			var w = script.s_wndMsg; if (!w.Is0) return w;
			return s_wndMsg.FindFast(null, c_msgWndClassName, true);
		}
	}
	static wnd.Cached_ s_wndMsg, s_wndMain;

	/// <summary>
	/// Class name of <see cref="WndMsg_"/> window.
	/// </summary>
	internal const string c_msgWndClassName = "Au.Editor.m3gVxcTJN02pDrHiQ00aSQ";

	internal static wnd WndMain_(bool show = false) {
		var w = WndMsg_;
		return w.Is0 ? default : s_wndMain.Get(() => (wnd)w.Send(Api.WM_USER, 0, show ? 1 : 0));
	}

	/// <summary>
	/// Returns true if editor is running.
	/// </summary>
	public static bool Available => !WndMsg_.Is0;

	/// <summary>
	/// The main editor window.
	/// </summary>
	/// <param name="show">Show the window (if the editor program is running).</param>
	/// <returns><c>default(wnd)</c> if the editor program isn't running or its main window still wasn't visible.</returns>
	public static wnd MainWindow(bool show = false) => WndMain_(show);

	/// <summary>
	/// Shows or hides the main editor window.
	/// </summary>
	/// <param name="show">
	/// <br/>• true - show, activate, restore if minimized.
	/// <br/>• false - hide. It does not hide the tray icon.
	/// <br/>• null - toggle.
	/// </param>
	public static void ShowMainWindow(bool? show) {
		var w = WndMsg_;
		if (!w.Is0) w.Send(Api.WM_USER, 1, show switch { true => 1, false => 2, _ => 0 });
	}

	/// <summary>
	/// Invokes an editor's menu command.
	/// </summary>
	/// <param name="command">Command name. If "" or invalid, prints all names.</param>
	/// <param name="check">If it's a checkbox-item, true checks it, false unchecks, null toggles. Else must be null (default).</param>
	/// <param name="dontWait">Don't wait until the command finishes executing. For example, if it shows a modal dialog, don't wait until it is closed.</param>
	/// <param name="activateWindow">Activate the main window. Default true. Some commands may not work correctly if the window isn't active.</param>
	/// <remarks>
	/// Shows the main window, regardless of <i>activateWindow</i>. Waits while it is disabled or not finished loading, unless script role is editorExtension and it runs in the editor's main thread (then not invoke the command).
	/// Does not invoke the command if the menu item is disabled or if it's a submenu-item.
	/// </remarks>
	public static void InvokeCommand(string command, bool? check = null, bool dontWait = false, bool activateWindow = true) {
		var w = WndMsg_; if (w.Is0) return;
		int flags = check switch { true => 1, false => 2, _ => 0 } | _EditorExtensionFlag;
		if (activateWindow) flags |= 4;
		if (dontWait) flags |= 8;
		g1:
		nint r = WndCopyData.Send<char>(w, 11, command, flags);
		if (r == -1) { _WaitWhileEditorDisabled(); goto g1; }
	}

	static int _EditorExtensionFlag => script.role == SRole.EditorExtension && Environment.CurrentManagedThreadId == 1 ? 16 : 0;

	static void _WaitWhileEditorDisabled() { MainWindow().WaitFor(0, o => o.IsEnabled()); }

	/// <summary>
	/// Gets the state of an editor's menu command (checked, disabled).
	/// </summary>
	/// <param name="command">Command name. If "" or invalid, prints all names.</param>
	/// <remarks>
	/// Shows the main window. Waits while it is disabled or not finished loading, unless script role is editorExtension and it runs in the editor's main thread (then returns <b>Disabled</b>).
	/// </remarks>
	public static ECommandState GetCommandState(string command) {
		var w = WndMsg_; if (w.Is0) return 0;
		g1:
		nint r = WndCopyData.Send<char>(w, 12, command, _EditorExtensionFlag);
		if (r == -1) { _WaitWhileEditorDisabled(); goto g1; }
		return (ECommandState)r;
	}

	/// <summary>
	/// Opens a script or other file. Also can move the text cursor.
	/// Does nothing if editor isn't running.
	/// </summary>
	/// <param name="file">A file in current workspace. Can be full path, or relative path in workspace, or file name with extension (<c>".cs"</c> etc). If folder, selects it.</param>
	/// <param name="line">If not null, goes to this 1-based line index.</param>
	/// <param name="offset">If not null, goes to this 0-based column index in line (if <i>line</i> not null) or to this 0-based position in text (if <i>line</i> null).</param>
	public static void Open([ParamString(PSFormat.FileInWorkspace)] string file, int? line = null, int? offset = null) {
		var w = WndMsg_; if (w.Is0) return;
		Api.AllowSetForegroundWindow(w.ProcessId);
		WndCopyData.Send<char>(w, 4, $"{file}|{line}|{offset}");
	}

	///
	[Obsolete("use Open"), EditorBrowsable(EditorBrowsableState.Never)]
	public static void OpenAndGoToLine([ParamString(PSFormat.FileInWorkspace)] string file, int line) => Open(file, line);

	/// <summary>
	/// Gets icon string in specified format.
	/// </summary>
	/// <returns>Returns null if editor isn't running or if the file does not exist. Read more in Remarks.</returns>
	/// <param name="file">Script file/folder path etc, or icon name. See <see cref="EGetIcon"/>.</param>
	/// <param name="what">The format of input and output strings.</param>
	/// <remarks>
	/// If <i>what</i> is <b>IconNameToXaml</b> and <i>file</i> is literal string and using default compiler, the compiler adds XAML to assembly resources and this function gets it from there, not from editor, and this function works everywhere.
	/// </remarks>
	public static string GetIcon(string file, EGetIcon what) {
		var del = IconNameToXaml_;
		if (del != null) return del(file, what);

		if (what == EGetIcon.IconNameToXaml && script.role != SRole.EditorExtension) {
			if (file.Starts("*<")) file = file[1..]; //"*<library>*icon", else "*icon"
			int i = file.IndexOf(' ');
			if (i > 0) { //color
				var rr = ResourceUtil.TryGetString_(file[..i]);
				if (rr != null) { WpfUtil_.SetColorInXaml(ref rr, file[++i..]); return rr; }
			} else { //black
				var rr = ResourceUtil.TryGetString_(file);
				if (rr != null) { WpfUtil_.SetColorInXaml(ref rr, null); return rr; }
			}
			//our compiler (_CreateManagedResources) adds XAML of icons to resources, but only from literal strings
		}

		var w = WndMsg_; if (w.Is0) return null;
		WndCopyData.SendReceive<char>(w, (int)Math2.MakeLparam(10, (int)what), file, out string r);
		return r;
		//rejected: add option to get serialized Bitmap instead. Now loads XAML in this process. It is 230 ms and +27 MB.
		//	Nothing good if the toolbar etc also uses XAML icons directly, eg for non-script items. And serializing is slow.
		//	Now not actual because of cache.
	}

	/// <summary>
	/// Editor sets this. Library uses it to avoid sendmessage when role editorExtension.
	/// </summary>
	internal static Func<string, EGetIcon, string> IconNameToXaml_;

	/// <summary>
	/// Returns true if the editor program is installed as [portable](xref:portable).
	/// </summary>
	/// <remarks>
	/// Available in the script editor process and in scripts launched from it. Elsewhere false.
	/// 
	/// If portable, these paths are different:
	/// - <see cref="folders.ThisAppDocuments"/>
	/// - <see cref="folders.ThisAppDataLocal"/>
	/// - <see cref="folders.ThisAppTemp"/>
	/// - <see cref="folders.Editor"/>
	/// - <see cref="folders.Workspace"/>
	/// </remarks>
	public static bool IsPortable { get; internal set; }

	//rejected. Use folders.Editor.
	///// <summary>
	///// Gets some special folders of editor process.
	///// </summary>
	///// <remarks>
	///// Default folders are:
	///// <b>ThisAppDocuments</b> - <c>folders.Documents + "LibreAutomate"</c>.
	///// <b>ThisAppDataLocal</b> - <c>folders.LocalAppData + "LibreAutomate"</c>.
	///// <b>ThisAppTemp</b> - <c>folders.Temp + "LibreAutomate"</c>.
	///// 
	///// Here <i>path</i> is either full path (like <c>C:\folder</c> or <c>%folders.Documents%\folder</c>) or path relative to the program's folder (like <c>ChildFolder</c> or <c>..\SiblingFolder</c>).
	///// </remarks>
	///// <seealso cref="folders.Workspace"/>
	//public static class Folders {
	//	static string _Get(int i) {
	//		if (a == null) {
	//			var w = WndMsg_;
	//			if (!w.Is0) {
	//				WndCopyData.SendReceive<char>(w, 13, null, out string r);
	//				if (!r.NE()) a = r.Split('|');
	//			}
	//			a ??= new string[] { null, null, null };
	//		}
	//		return a[i];
	//	}

	//	static string[] a;

	//	/// <summary>
	//	/// Gets <see cref="folders.ThisAppDocuments"/> of editor process.
	//	/// </summary>
	//	/// <value>null if failed.</value>
	//	public static FolderPath ThisAppDocuments => new(_Get(0));

	//	/// <summary>
	//	/// Gets <see cref="folders.ThisAppDataLocal"/> of editor process.
	//	/// </summary>
	//	/// <value>null if failed.</value>
	//	public static FolderPath ThisAppDataLocal => new(_Get(1));

	//	/// <summary>
	//	/// Gets <see cref="folders.ThisAppTemp"/> of editor process.
	//	/// </summary>
	//	/// <value>null if failed.</value>
	//	public static FolderPath ThisAppTemp => new(_Get(2));
	//}

	//[StructLayout(LayoutKind.Sequential, Size = 64)] //note: this struct is in shared memory. Size must be same in all library versions.
	//internal struct SharedMemoryData_ {
	//	int _wndEditorMsg, _wndEditorMain;

	//	internal wnd wndEditorMsg {
	//		get {
	//			if (_wndEditorMsg != 0) {
	//				var w = (wnd)_wndEditorMsg;
	//				if (w.ClassNameIs(c_msgWndClassName)) return w;
	//				//_wndEditorMsg = 0; //no, unsafe
	//			}
	//			return default;
	//		}
	//		set { _wndEditorMsg = (int)value; }
	//	}
	//	internal wnd wndEditorMain {
	//		get => wndEditorMsg.Is0 ? default : (wnd)_wndEditorMain;
	//		set { _wndEditorMain = (int)value; }
	//	}

	//}
}
