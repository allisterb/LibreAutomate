using Au.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;

//SHOULDDO: when disabling main window, also disable its owned windows. At least floating panels.
//	Because for dialogs often is used 'owner: App.Hmain'.

partial class MainWindow : Window {
	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
		if (e.Property == VisibilityProperty && (Visibility)e.NewValue == Visibility.Visible) {
			//This is the first OnX when showing window.
			//	Window.Show just sets the Visibility property.
			//	Then WPF changes Left, Top, some other properties, and calls OnInitialized.
			if (!_inited) { _inited = true; _Init(); }
		}
		base.OnPropertyChanged(e);
	}
	bool _inited;

	void _Init() {
		//_StartProfileOptimization();

		Application.Current.Resources = Application.LoadComponent(new("/Au.Editor;component/app/app-resources.xaml", UriKind.Relative)) as ResourceDictionary;

		Title = App.AppNameShort; //don't append document name etc

		if (App.Settings.wndpos.main == null) {
			Width = 1000;
			Height = 700;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			//and will EnsureInScreen
		}
		WndSavedRect.Restore(this, App.Settings.wndpos.main, o => App.Settings.wndpos.main = o);

		Panels.LoadAndCreateToolbars();

		App.Commands = new KMenuCommands(typeof(Menus), Panels.Menu);

		App.Commands[nameof(Menus.File.New)].SubmenuOpened = (o, _) => FilesModel.FillMenuNew(o as MenuItem);
		App.Commands[nameof(Menus.File.Workspace)].SubmenuOpened = (o, _) => FilesModel.FillMenuRecentWorkspaces(o as MenuItem);

		App.Commands.OnCustomizingError = (c, s, ex) => print.it($"<>Customization error in <+DCustomize>{c.Name}<>: {s}. {ex?.ToStringWithoutStack()}");
		var atb = new ToolBar[7] { Panels.THelp, Panels.TTools, Panels.TFile, Panels.TRun, Panels.TEdit, Panels.TCustom1, Panels.TCustom2 };
		App.Commands.InitToolbarsAndCustomize(folders.ThisAppBS + @"Default\Commands.xml", AppSettings.DirBS + "Commands.xml", atb);

		var bRun = App.Commands[nameof(Menus.Run.Run_script)].FindButtonInToolbar(Panels.TRun);
		if (bRun != null) { bRun.Width = 50; bRun.Margin = new(10, 0, 10, 0); } //make Run button bigger //SHOULDDO: bad if vertical toolbar

		var bNew = App.Commands[nameof(Menus.File.New)].FindMenuButtonInToolbar(Panels.TFile);
		if (bNew != null) bNew.MouseDoubleClick += (_, e) => { e.Handled = true; Menus.File.New.New_script(); };

		Panels.CreatePanels();

		App.Commands.BindKeysTarget(this, "");

		Panels.PanelManager.Container = g => { this.Content = g; };

		_NormalizeMouseWheel();

		//timer.after(100, _ => DOptions.aaShow());
		//timer.after(100, _ => App.Model.Properties());
		//timer.after(100, _ => Menus.File.Workspace.New_workspace());
		//timer.after(100, _ => DIcons.aaShow());
		//timer.after(600, _ => Au.Tools.Dwnd.Dialog(wnd.find(null, "Shell_TrayWnd")));
		//timer.after(600, _ => Au.Tools.Dwnd.Dialog(wnd.findOrRun(null, "Notepad", run: () => run.it(folders.System + "notepad.exe"))));
		//timer.after(500, _ => Au.Tools.Delm.Dialog(new POINT(806, 1580)));
		//timer.after(500, _ => Au.Tools.Delm.Dialog());
		//timer.after(400, _ => Au.Tools.Duiimage.Dialog());
		//timer2.every(200, _ => { GC.Collect(); });
		//timer.after(100, _ => Menus.Tools.NuGet());

#if DEBUG
		App.Timer1s += () => {
			var e = Keyboard.FocusedElement as FrameworkElement;
			Debug_.PrintIf(e != null && !e.IsVisible, "focused invisible");
			//print.it(e, FocusManager.GetFocusedElement(App.Wmain));
		};
#endif
	}

	protected override void OnSourceInitialized(EventArgs e) {
		base.OnSourceInitialized(e);
		var hs = PresentationSource.FromVisual(this) as HwndSource;
		App.Hmain = (wnd)hs.Handle;

		if (App.Settings.wndpos.main == null) App.Hmain.EnsureInScreen();

		//workaround for: sometimes OS does not set foreground window. Then we have a fake active/focused state (blinking caret, called OnActivated, etc).
		//	1. When started hidden, and now clicked tray icon first time. Is it because of the "lock foreground window"? Or WPF shows window somehow incorrectly?
		//	2. When starting visible, if VMWare Player is active. Same with some other programs too (WPF, appstore, some other).
		//this.Activate(); //does not work with VMWare, also if user clicks a window after starting this process
		App.Hmain.ActivateL(); //works always, possibly with workarounds

		Panels.PanelManager["Output"].Visible = true;

		App.Model.WorkspaceLoadedWithUI(onUiLoaded: true);

		App.Loaded = AppState.LoadedUI;

		CodeInfo.UiLoaded();

		UacDragDrop.AdminProcess.Enable(true); //rejected: disable when hiding main window. Some other window may be visible.

		hs.AddHook(_WndProc);

		Au.Tools.QuickCapture.RegisterHotkeys();

		App.OnMainWindowLoaded_();

		//Created?.Invoke();

		Loaded += (_, _) => {
			EditorExtension.WindowLoaded_();
			CommandLine.UILoaded();
		};
	}

	///// <summary>
	///// When window handle created.
	///// Documents are open, etc.
	///// </summary>
	//public event Action Created;

	protected override void OnClosing(CancelEventArgs e) {
		//note: called by Window.Close (sync) and Application.Shutdown (async) even if the window never was loaded.

		if (App.Loaded == AppState.LoadedUI) {
			App.Model.Save.AllNowIfNeed();
			Panels.PanelManager.Save();
			Au.Tools.TUtil.CloseDialogsInNonmainThreads(); //let they save rects etc
		}

		EditorExtension.ClosingWorkspace_(onExit: true); //must be called before closing documents

		base.OnClosing(e);
	}

	protected override void OnClosed(EventArgs e) {
		//note: called by Window.Close (sync) and Application.Shutdown (async) even if the window never was loaded.

		bool loaded = App.Loaded == AppState.LoadedUI;
		App.Loaded = AppState.Unloading;
		base.OnClosed(e);
		if (loaded) {
			UacDragDrop.AdminProcess.Enable(false);
			CodeInfo.Stop();
			App.Model.Save.AllNowIfNeed();
		}
	}

	unsafe nint _WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled) {
		var w = (wnd)hwnd;

		switch (msg) {
		case Api.WM_DPICHANGED:
			this.DpiChangedWorkaround();
			break;
		case Api.WM_HOTKEY:
			handled = true;
			switch ((AppHotkeyId)(int)wParam) {
			case AppHotkeyId.QuickCaptureMenu: Au.Tools.QuickCapture.Menu(); break;
			case AppHotkeyId.QuickCaptureDwnd: Au.Tools.QuickCapture.AoolDwnd(); break;
			case AppHotkeyId.QuickCaptureDelm: Au.Tools.QuickCapture.ToolDelm(); break;
			case AppHotkeyId.QuickCaptureDuiimage: Au.Tools.QuickCapture.ToolDuiimage(); break;
			}
			break;
		case Api.WM_ACTIVATEAPP:
			if (wParam != 0) {
				_appActivatedTimer ??= new(_ => {
					Panels.Editor.OnAppActivated_();
					if (App.Settings.checkForUpdates) App.CheckForUpdates(false);
				});
				_appActivatedTimer.After(250);
			} else {
				_appActivatedTimer?.Stop();
			}
			break;
		case Api.WM_SYSCOMMAND when (wParam & 0xFFF0) == Api.SC_CLOSE:
			if (handled = App.Settings.runHidden) Hide_();
			break;
		}

		return default;
	}
	timer _appActivatedTimer;

	internal void Hide_() {
		if (IsVisible) {
			App.Model.Save.AllNowIfNeed();
			Panels.PanelManager.Save();
			Hide();
			process.ThisProcessMinimizePhysicalMemory_(1000);
		}
	}

	//this could be a workaround for the inactive window at startup, but probably don't need when we call Activete() in OnSourceInitialized
	//protected override void OnActivated(EventArgs e) {
	//	var w = this.Hwnd();
	//	if (wnd.active != w && _activationWorkaroundTime < Environment.TickCount64 - 5000) {
	//		//print.it(new StackTrace());
	//		_activationWorkaroundTime = Environment.TickCount64;
	//		timer.after(10, _ => {
	//			Debug_.Print("OnActivated workaround, " + wnd.active);
	//			//w.ActivateL(); //in some cases does not work, or need key etc
	//			if (!w.IsMinimized) {
	//				w.ShowMinimized(noAnimation: true);
	//				w.ShowNotMinimized(noAnimation: true);
	//			}
	//		});
	//	}

	//	base.OnActivated(e);
	//}
	//long _activationWorkaroundTime;

	//this was for testing document tabs. Now we don't use document tabs. All documents now are in single panel.
	//void _OpenDocuments() {
	//	var docLeaf = _AddDoc("Document 1");
	//	_AddDoc("Document 2");
	//	_AddDoc("Document 3");
	//	_AddDoc("Document 4");
	//	docLeaf.Visible = true;
	//	//Panels.DocPlaceholder_.Visible = false;
	//	docLeaf.Content.Focus();

	//	KPanels.ILeaf _AddDoc(string name) {
	//		//var docPlaceholder = App.Panels["Open"]; //in stack
	//		var docPlaceholder = Panels.DocPlaceholder_; //in tab
	//		var v = docPlaceholder.AddSibling(false, KPanels.LeafType.Document, name, true);
	//		v.Closing += (_, e) => { e.Cancel = !dialog.showOkCancel("Close?"); };
	//		v.ContextMenuOpening += (o, m) => {
	//			var k = o as KPanels.ILeaf;
	//			m.Separator();
	//			m["Close 2"] = o => k.Delete();
	//		};
	//		v.TabSelected += (_, _) => _OpenDoc(v);

	//		return v;
	//	}

	//	static void _OpenDoc(KPanels.ILeaf leaf) {
	//		if (leaf.Content != null) return;
	//		leaf.Content = new KScintilla();
	//	}
	//}

	//Used to make faster, but now with tiered JIS makes faster only by ~100 ms.
	static void _StartProfileOptimization() {
#if !DEBUG
		var fProfile = folders.ThisAppDataLocal + "ProfileOptimization";
		filesystem.createDirectory(fProfile);
		System.Runtime.ProfileOptimization.SetProfileRoot(fProfile);
		System.Runtime.ProfileOptimization.StartProfile("Au.Editor.startup");
#endif
	}

	public void AaShowAndActivate() {
		Show();
		var w = App.Hmain;
		w.ShowNotMinimized();
		w.ActivateL();
	}

	//If winver < 10 or disabled normal mouse scrolling, sets a mouse hook to scroll the mouse control.
	//	Without it can't scroll any KTreeView even if focused.
	static unsafe void _NormalizeMouseWheel() {
		if (osVersion.minWin10 && Api.SystemParametersInfo(Api.SPI_GETMOUSEWHEELROUTING, 0) >= 2) return;
		WindowsHook.ThreadGetMessage(k => {
			if (k.PM_NOREMOVE) return;
			if (k.msg->message is Api.WM_MOUSEWHEEL or Api.WM_MOUSEHWHEEL) {
				var w = wnd.fromMouse(WXYFlags.Raw);
				if (w.IsOfThisThread) k.msg->hwnd = w;
			}
		});
		//never mind other threads. Eg the wnd and elm tools.
	}
}
