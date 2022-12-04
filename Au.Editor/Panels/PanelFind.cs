using Au.Controls;
using Au.Tools;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Media;
using System.Windows.Documents;

//CONSIDER: right-click "Find" - search backward. The same for "Replace" (reject "find next"). Rarely used.
//CONSIDER: option to replace and don't find next until next click. Eg Eclipse has buttons "Replace" and "Replace/Find". Or maybe delay to preview.

class PanelFind : UserControl {
	TextBox _tFind, _tReplace;
	KCheckBox _cFolder, _cCase, _cWord, _cRegex;
	KPopup _ttRegex, _ttNext;
	WatermarkAdorner _adorner1;

	public PanelFind() {
		this.UiaSetName("Find panel");

		var b = new wpfBuilder(this).Columns(-1).Brush(SystemColors.ControlBrush);
		b.Options(modifyPadding: false, margin: new Thickness(2));
		b.AlsoAll((b, _) => { if (b.Last is Button k) k.Padding = new(1, 0, 1, 1); });
		b.Row((-1, 22..)).Add<AdornerDecorator>()
			.Add(out _tFind, flags: WBAdd.ChildOfLast).Margin(-1, 0, -1, 2).Multiline(wrap: TextWrapping.Wrap).Name("Find_text", true).Watermark(out _adorner1, "Find");
		b.Row((-1, 22..)).Add<AdornerDecorator>()
			.Add(out _tReplace, flags: WBAdd.ChildOfLast).Margin(-1, 0, -1, 2).Multiline(wrap: TextWrapping.Wrap).Name("Replace_text", true).Watermark("Replace");
		b.R.StartGrid().Columns((-1, ..80), (-1, ..80), (-1, ..80), 0);
		b.R.AddButton("Find", _bFind_Click).Tooltip("Find next in editor");
		b.AddButton(out var bReplace, "Replace", _bReplace_Click).Tooltip("Replace current found text in editor and find next.\nRight click - find next.");
		bReplace.MouseRightButtonUp += (_, _) => _bFind_Click(null);
		b.AddButton("Repl. all", _bReplaceAll_Click).Tooltip("Replace all in editor");

		b.R.AddButton("In files", _bFindIF_Click).Tooltip("Find text in files");
		b.StartStack();
		_cFolder = b.xAddCheckIcon("*Material.FolderSearchOutline" + Menus.green, "Let 'In files' search only in current project or root folder");
		b.Padding(1, 0, 1, 1);
		b.xAddButtonIcon("*EvaIcons.Options2" + Menus.green, _bOptions_Click, "More options");

		var cmd1 = App.Commands[nameof(Menus.File.OpenCloseGo.Go_back)];
		var bBack = b.xAddButtonIcon(Menus.iconBack, _ => Menus.File.OpenCloseGo.Go_back(), "Go back");
		b.Disabled(!cmd1.Enabled);
		cmd1.CanExecuteChanged += (o, e) => bBack.IsEnabled = cmd1.Enabled;

		b.End();

		b.R.Add(out _cCase, "Case").Tooltip("Match case");
		b.Add(out _cWord, "Word").Tooltip("Whole word");
		b.Add(out _cRegex, "Regex").Tooltip("Regular expression.\nF1 - Regex tool and help.");
		b.End().End();

		this.IsVisibleChanged += (_, _) => {
			Panels.Editor.ZActiveDoc?.EInicatorsFind_(IsVisible ? _aEditor : null);
		};

		_tFind.TextChanged += (_, _) => ZUpdateQuickResults();

		//prevent tooltip on set focus.
		//	Broken in .NET 6:  AppContext.SetSwitch("Switch.UseLegacyToolTipDisplay", true); //must be before creating Application object
		_tFind.ToolTipOpening += (o, e) => { if (o is UIElement k && !k.IsMouseOver) e.Handled = true; };

		foreach (var v in new TextBox[] { _tFind, _tReplace }) {
			v.AcceptsTab = true;
			v.IsInactiveSelectionHighlightEnabled = true;
			v.GotKeyboardFocus += _tFindReplace_KeyboardFocus;
			v.LostKeyboardFocus += _tFindReplace_KeyboardFocus;
			v.ContextMenu = new KWpfMenu();
			v.ContextMenuOpening += _tFindReplace_ContextMenuOpening;
			v.PreviewMouseUp += (o, e) => { //use up, else up will close popup. Somehow on up ClickCount always 1.
				if (e.ChangedButton == MouseButton.Middle) {
					var tb = o as TextBox;
					if (tb.Text.NE()) _RecentPopupList(tb); else tb.Clear();
				}
			};
		}

		foreach (var v in new KCheckBox[] { _cCase, _cWord, _cRegex }) v.CheckChanged += _CheckedChanged;
	}

	#region control events

	private void _tFindReplace_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
		var c = sender as TextBox;
		var m = c.ContextMenu as KWpfMenu;
		m.Items.Clear();
		m["_Undo\0" + "Ctrl+Z", c.CanUndo] = o => c.Undo();
		m["_Redo\0" + "Ctrl+Y", c.CanRedo] = o => c.Redo();
		m["Cu_t\0" + "Ctrl+X", c.SelectionLength > 0] = o => c.Cut();
		m["_Copy\0" + "Ctrl+C", c.SelectionLength > 0] = o => c.Copy();
		m["_Paste\0" + "Ctrl+V", Clipboard.ContainsText()] = o => c.Paste();
		m["_Select All\0" + "Ctrl+A"] = o => c.SelectAll();
		m["Cl_ear\0" + "M-click"] = o => c.Clear();
		m["Rece_nt\0" + "M-click"] = o => _RecentPopupList(c);
	}

	protected override void OnKeyDown(KeyEventArgs e) {
		base.OnKeyDown(e);
		switch ((e.Key, Keyboard.Modifiers)) {
		case (Key.F1, 0):
			if (_cRegex.IsChecked) _ShowRegexInfo((e.OriginalSource as TextBox) ?? _tFind, F1: true);
			break;
		default: return;
		}
		e.Handled = true;
	}

	private void _tFindReplace_KeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
		if (!_cRegex.IsChecked) return;
		var tb = sender as TextBox;
		if (e.NewFocus == tb) {
			//use timer to avoid temporary focus problems, for example when tabbing quickly or closing active Regex window (this was for forms, now not tested without)
			timer.after(70, _ => { if (tb.IsFocused) _ShowRegexInfo(tb, F1: false); });
		} else if ((_regexWindow?.IsVisible ?? false)) {
			timer.after(70, _ => {
				if ((_regexWindow?.IsVisible ?? false) && !_regexWindow.Hwnd.IsActive) {
					var c = Keyboard.FocusedElement;
					if (c != _tFind && c != _tReplace) _regexWindow.Hwnd.ShowL(false);
				}
			});
		}
	}

	private void _CheckedChanged(object sender, RoutedEventArgs e) {
		if (sender == _cWord) {
			if (_cWord.IsChecked) _cRegex.IsChecked = false;
		} else if (sender == _cRegex) {
			if (_cRegex.IsChecked) {
				_cWord.IsChecked = false;
				_adorner1.Text = "Find  (F1 - regex tool)";
			} else {
				_regexWindow?.Close();
				_regexWindow = null;
				_adorner1.Text = "Find";
			}
		}
		ZUpdateQuickResults();
	}

	RegexWindow _regexWindow;
	string _regexTopic;

	void _ShowRegexInfo(TextBox tb, bool F1) {
		if (F1) {
			_regexWindow ??= new RegexWindow();
			_regexWindow.UserClosed = false;
		} else {
			if (_regexWindow == null || _regexWindow.UserClosed) return;
		}

		if (_regexWindow.Hwnd.Is0) {
			var r = this.RectInScreen();
			r.Offset(0, -20);
			_regexWindow.ShowByRect(App.Wmain, Dock.Right, r, true);
		} else _regexWindow.Hwnd.ShowL(true);

		_regexWindow.InsertInControl = tb;

		bool replace = tb == _tReplace;
		var s = _regexWindow.CurrentTopic;
		if (s == "replace") {
			if (!replace) _regexWindow.CurrentTopic = _regexTopic;
		} else if (replace) {
			_regexTopic = s;
			_regexWindow.CurrentTopic = "replace";
		}
	}

	private void _bFind_Click(WBButtonClickArgs e) {
		if (!_GetTextToFind(out var f)) return;
		_FindNextInEditor(f, false);
	}

	private void _bFindIF_Click(WBButtonClickArgs e) {
		//using var _ = new _TempDisableControl(_bFindIF);
		_FindAllInFiles();

		//SHOULDDO: disabled button view now not updated because UI is blocked.
		//	Should search in other thread; at least get text.
	}

	private void _bReplace_Click(WBButtonClickArgs e) {
		if (!_GetTextToFind(out var f, forReplace: true)) return;
		_FindNextInEditor(f, true);
	}

	private void _bOptions_Click(WBButtonClickArgs e) {
		var b = new wpfBuilder("Find Options").WinSize(350);
		b.R.StartGrid<GroupBox>("Find in files");
		b.R.Add("Search in", out ComboBox cbFileType, true).Items("All files|C# files (*.cs)|C# script files|C# class files|Other files").Select(_SearchIn);
		b.R.Add("Skip files where path matches wildcard", out TextBox tSkip, string.Join("\r\n", _SkipWildcards), row2: 0).Multiline(100, TextWrapping.NoWrap);
		int iSlow = App.Settings.find_printSlow; string sSlow = iSlow > 0 ? iSlow.ToS() : null;
		b.R.StartStack().Add("Print file open+search times >=", out TextBox tSlow, sSlow).Width(50).Add<Label>("ms").End();
		b.End();
		b.R.AddOkCancel();
		b.End();
		b.Window.ShowInTaskbar = false;
		if (!b.ShowDialog(App.Wmain)) return;
		App.Settings.find_searchIn = _searchIn = cbFileType.SelectedIndex;
		App.Settings.find_skip = tSkip.Text; _aSkipWildcards = null;
		App.Settings.find_printSlow = tSlow.Text.ToInt();

		//FUTURE: option to use cache to make faster.
		//	Now, if many files, first time can be very slow because of AV eg Windows Defender.
		//	To make faster, I added Windows Defender exclusion for cs file type. Remove when testing cache etc.
		//	When testing WD impact, turn off/on its real-time protection and restart this app.
		//	For cache use SQLite database in App.Model.CacheDirectory. Add text of files of size eg < 100 KB.
	}

	#endregion

	#region common

	/// <summary>
	/// Makes visible and sets find text = s (should be selected text of a control; can be null/"").
	/// </summary>
	public void ZCtrlF(string s/*, bool findInFiles = false*/) {
		Panels.PanelManager[this].Visible = true;
		_tFind.Focus();
		if (s.NE()) {
			_tFind.SelectAll(); //often user wants to type new text
			return;
		}
		_tFind.Text = s;
		//_tFind.SelectAll(); //no, somehow WPF makes selected text gray like disabled when non-focused
		//if (findInFiles) _FindAllInFiles(false); //rejected. Not so useful.
	}

	/// <summary>
	/// Makes visible and sets find text = selected text of e.
	/// Supports KScintilla and TextBox. If other type or null or no selected text, just makes visible etc.
	/// </summary>
	public void ZCtrlF(FrameworkElement e/*, bool findInFiles = false*/) {
		string s = null;
		switch (e) {
		case KScintilla c:
			s = c.aaaSelectedText();
			break;
		case TextBox c:
			s = c.SelectedText;
			break;
		}
		ZCtrlF(s/*, findInFiles*/);
	}

	//rejected. Could be used for global keyboard shortcuts, but currently they work only if the main window is active.
	///// <summary>
	///// Makes visible and sets find text = selected text of focused control.
	///// </summary>
	//public void ZCtrlF() => ZCtrlF(FocusManager.GetFocusedElement(App.Wmain));

	/// <summary>
	/// Called when changed find text or options. Also when activated another document.
	/// Async-updates find-hiliting in editor.
	/// </summary>
	public void ZUpdateQuickResults() {
		if (!IsVisible) return;

		_timerUQR ??= new timer(_ => {
			_FindAllInEditor();
			Panels.Editor.ZActiveDoc?.EInicatorsFind_(_aEditor);
		});

		_timerUQR.After(150);
	}
	timer _timerUQR;

	class _TextToFind {
		public string findText;
		public string replaceText;
		public regexp rx;
		public bool wholeWord;
		public bool matchCase;

		public bool IsSameFindTextAndOptions(_TextToFind f)
			=> f.findText == findText
			&& f.matchCase == matchCase
			&& f.wholeWord == wholeWord
			&& (f.rx != null) == (rx != null);
	}

	bool _GetTextToFind(out _TextToFind f, bool forReplace = false, bool noRecent = false, bool noErrorTooltip = false) {
		_ttRegex?.Close();
		string text = _tFind.Text; if (text.NE()) { f = null; return false; }
		f = new() { findText = text, matchCase = _cCase.IsChecked };
		try {
			if (_cRegex.IsChecked) {
				var fl = RXFlags.MULTILINE;
				if (!f.matchCase) fl |= RXFlags.CASELESS;
				f.rx = new regexp(f.findText, flags: fl);
			} else {
				f.wholeWord = _cWord.IsChecked;
			}
		}
		catch (ArgumentException e) { //regexp ctor throws if invalid
			if (!noErrorTooltip) TUtil.InfoTooltip(ref _ttRegex, _tFind, e.Message);
			return false;
		}
		if (forReplace) f.replaceText = _tReplace.Text;

		if (!noRecent) _AddToRecent(f);

		if (forReplace && (Panels.Editor.ZActiveDoc?.aaaIsReadonly ?? true)) return false;
		return true;
	}

	void _FindAllInString(string text, _TextToFind f, List<Range> a) {
		a.Clear();
		if (f.rx != null) {
			foreach (var g in f.rx.FindAllG(text, 0)) a.Add(g.Start..g.End);
		} else {
			for (int i = 0; i < text.Length; i += f.findText.Length) {
				i = f.wholeWord ? text.FindWord(f.findText, i.., !f.matchCase, "_") : text.Find(f.findText, i, !f.matchCase);
				if (i < 0) break;
				a.Add(i..(i + f.findText.Length));
			}
		}
	}

	#endregion

	#region in editor

	void _FindNextInEditor(_TextToFind f, bool replace) {
		_ttNext?.Close();
		var doc = Panels.Editor.ZActiveDoc; if (doc == null) return;
		var text = doc.aaaText; if (text.Length == 0) return;
		int i, to, len = 0, from8 = replace ? doc.aaaSelectionStart8 : doc.aaaSelectionEnd8, from = doc.aaaPos16(from8), to8 = doc.aaaSelectionEnd8;
		RXMatch rm = null;
		bool retryFromStart = false, retryRx = false;
		g1:
		if (f.rx != null) {
			//this code solves this problem: now will not match if the regex contains \K etc, because 'from' is different
			if (replace && _lastFind.doc == doc && _lastFind.from8 == from8 && _lastFind.to8 == to8 && _lastFind.text == text && f.IsSameFindTextAndOptions(_lastFind.f)) {
				i = from8; to = to8; rm = _lastFind.rm;
				goto g2;
			}

			if (f.rx.Match(text, out rm, from..)) {
				i = rm.Start;
				len = rm.Length;
				//print.it(i, len);
				if (i == from && len == 0 && !(replace | retryRx | retryFromStart)) {
					if (++i > text.Length) i = -1;
					else {
						if (i < text.Length) if (text.Eq(i - 1, "\r\n") || char.IsSurrogatePair(text, i - 1)) i++;
						from = i; retryRx = true; goto g1;
					}
				}
				if (len == 0) doc.Focus();
			} else i = -1;
		} else {
			i = f.wholeWord ? text.FindWord(f.findText, from.., !f.matchCase, "_") : text.Find(f.findText, from, !f.matchCase);
			len = f.findText.Length;
		}
		//print.it(from, i, len);
		if (i < 0) {
			SystemSounds.Asterisk.Play();
			_lastFind.f = null;
			if (retryFromStart || from8 == 0) return;
			from = 0; retryFromStart = true; replace = false;
			goto g1;
		}
		if (retryFromStart) TUtil.InfoTooltip(ref _ttNext, _tFind, "Info: searching from start.");
		to = doc.aaaPos8(i + len);
		i = doc.aaaPos8(i);
		g2:
		if (replace && i == from8 && to == to8) {
			var repl = f.replaceText;
			if (rm != null) if (!_TryExpandRegexReplacement(rm, repl, out repl)) return;
			//doc.zReplaceRange(i, to, repl); //also would need to set caret pos = to
			doc.aaaReplaceSel(repl);
			_FindNextInEditor(f, false);
		} else {
			if (CiStyling.IsProtected(doc, i, to)) {
				//print.it("hidden");
				//if (1 != dialog.show("Select hidden text?", "The found text is in a hidden text range. Do you want to select it?", "Yes|No", owner: this, defaultButton: 2)) {
				doc.aaaGoToPos(false, CiStyling.SkipProtected(doc, to));
				return;
				//}
			}

			App.Model.EditGoBack.RecordNext();
			doc.aaaSelect(false, i, to, true);

			_lastFind.f = f;
			_lastFind.doc = doc;
			_lastFind.text = text;
			_lastFind.from8 = i;
			_lastFind.to8 = to;
			_lastFind.rm = rm;
		}
	}

	(_TextToFind f, SciCode doc, string text, int from8, int to8, RXMatch rm) _lastFind;

	private void _bReplaceAll_Click(WBButtonClickArgs e) {
		if (!_GetTextToFind(out var f, forReplace: true)) return;
		_ReplaceAllInEditor(f);
	}

	void _ReplaceAllInEditor(_TextToFind f) {
		var doc = Panels.Editor.ZActiveDoc;
		if (doc.aaaIsReadonly) return;
		var text = doc.aaaText;
		var repl = f.replaceText;
		if (f.rx != null) {
			if (f.rx.FindAll(text, out var ma)) {
				using var undo = new KScintilla.UndoAction(doc);
				for (int i = ma.Length; --i >= 0;) {
					var m = ma[i];
					if (!_TryExpandRegexReplacement(m, repl, out var r)) return;
					_ReplaceRange(m.Start, m.End, r);
				}
			}
		} else {
			var a = _aEditor;
			_FindAllInString(text, f, a);
			if (a.Count > 0) {
				using var undo = new KScintilla.UndoAction(doc);
				for (int i = a.Count; --i >= 0;) {
					var v = a[i];
					_ReplaceRange(v.Start.Value, v.End.Value, repl);
				}
			}
		}

		void _ReplaceRange(int from, int to, string s) {
			doc.aaaNormalizeRange(true, ref from, ref to);
			if (CiStyling.IsProtected(doc, from, to)) return;
			doc.aaaReplaceRange(false, from, to, s);
		}

		//Easier/faster would be to create new text and call zSetText. But then all non-text data is lost: markers, folds, caret position...
	}

	bool _TryExpandRegexReplacement(RXMatch m, string repl, out string result) {
		try {
			result = m.ExpandReplacement(repl);
			return true;
		}
		catch (Exception e) {
			TUtil.InfoTooltip(ref _ttRegex, _tReplace, e.Message);
			result = null;
			return false;
		}
	}

	bool _ValidateReplacement(_TextToFind f, FileNode file) {
		//SHOULDDO: add a ValidateReplacement function to the regex class.
		if (f.rx != null
			&& file.GetCurrentText(out var s, silent: true)
			&& f.rx.Match(s, out RXMatch m)
			&& !_TryExpandRegexReplacement(m, _tReplace.Text, out _)
			) return false;
		return true;
	}

	List<Range> _aEditor = new(); //all found in editor text

	void _FindAllInEditor() {
		_aEditor.Clear();
		if (!_GetTextToFind(out var f, noRecent: true, noErrorTooltip: true)) return;
		var text = Panels.Editor.ZActiveDoc?.aaaText; if (text.NE()) return;
		_FindAllInString(text, f, _aEditor);
	}

	#endregion

	#region in files

	int _SearchIn => _searchIn >= 0 ? _searchIn : (_searchIn = App.Settings.find_searchIn);
	int _searchIn = -1;

	string[] _SkipWildcards => _aSkipWildcards ??= (App.Settings.find_skip ?? "").Lines(true);
	string[] _aSkipWildcards;
	readonly string[] _aSkipImages = new string[] { ".png", ".bmp", ".jpg", ".jpeg", ".gif", ".tif", ".tiff", ".ico", ".cur", ".ani" };
	bool _init1;
	const int c_indic = 0;

	public KScintilla PrepareFindResultsPanel() {
		Panels.PanelManager["Found"].Visible = true;

		var cFound = Panels.Found.ZControl;
		cFound.aaaClearText();

		if (!_init1) {
			_init1 = true;

			App.Model.WorkspaceLoadedAndDocumentsOpened += () => cFound.aaaClearText();

			cFound.aaTags.AddLinkTag("+open", s => {
				_OpenLinkClicked(s);
			});

			cFound.aaTags.AddLinkTag("+ra", s => {
				if (!_OpenLinkClicked(s, replaceAll: true)) return;
				timer.after(10, _ => _ReplaceAllInFile());
				//info: without timer sometimes does not set cursor pos correctly
			});

			cFound.aaTags.AddLinkTag("+f", s => {
				var a = s.Split(' ');
				if (!_OpenLinkClicked(a[0])) return;
				var doc = Panels.Editor.ZActiveDoc;
				//doc.Focus();
				int from = a[1].ToInt(), to = a[2].ToInt();
				timer.after(10, _ => {
					if (to >= doc.aaaLen16) return;
					App.Model.EditGoBack.RecordNext();
					doc.aaaSelect(true, from, to, true);
				});
				//info: scrolling works better with async when now opened the file
			});

			bool _OpenLinkClicked(string file, bool replaceAll = false) {
				var f = App.Model.Find(file); //<id>
				if (f == null) return false;
				if (f.IsFolder) f.SelectSingle();
				else {
					if (replaceAll && !_ValidateReplacement(_lastFindAll.f, f)) return false; //avoid opening the file in editor when invalid regex replacement
					if (!App.Model.SetCurrentFile(f)) return false;
				}
				//add indicator to make it easier to find later
				cFound.aaaIndicatorClear(c_indic);
				var v = cFound.aaaLineStartEndFromPos(false, cFound.aaaCurrentPos8);
				cFound.aaaIndicatorAdd(false, c_indic, v.start..v.end);
				return true;
			}

			cFound.aaTags.AddLinkTag("+raif", s => _ReplaceAllInFiles(s));

			cFound.aaTags.AddLinkTag("+caf", s => {
				App.Model.CloseFiles(_lastFindAll.files, _lastFindAll.wasOpen);
				App.Model.CollapseAll(exceptWithOpenFiles: true);
			});

			cFound.aaTags.AddLinkTag("+caff", s => Panels.Files.CloseAll());

			cFound.Call(Sci.SCI_INDICSETSTYLE, c_indic, Sci.INDIC_BOX);
			cFound.Call(Sci.SCI_INDICSETFORE, c_indic, 0x0080e0);
		}

		return cFound;
	}

	void _FindAllInFiles(/*, bool forReplace*/) {
		var cFound = PrepareFindResultsPanel();

		if (!_GetTextToFind(out var f)) return;

		cFound.aaaText = "<c #A0A0A0>... searching ...<>";
		//Api.UpdateWindow(cFound.Hwnd); //ok if was visible, but not if made visible now
		wait.doEvents();

		var b = new StringBuilder();
		var a = new List<Range>();
		int timeSlow = App.Settings.find_printSlow;
		StringBuilder bSlow = timeSlow > 0 ? new() : null;
		bool jited = false;
		int searchIn = _SearchIn;
		int nFound = 0;
		List<FileNode> aFiles = new();

		var folder = App.Model.Root;
		if (_cFolder.IsChecked && Panels.Editor.ZActiveDoc?.EFile is FileNode fn) {
			if (fn.FindProject(out var proj, out _, ofAnyScript: true)) folder = proj;
			else folder = fn.AncestorsFromRoot(noRoot: true).FirstOrDefault() ?? folder;
		}

		foreach (var v in folder.Descendants()) {
			//using var p1 = new _Perf();
			string text = null, path = null;
			//perf.first();
			if (v.IsCodeFile) {
				switch (searchIn) { //0 all, 1 C#, 2 script, 3 class, 4 other
				case 4: continue;
				case 2 when !v.IsScript: continue;
				case 3 when !v.IsClass: continue;
				}
			} else {
				if (searchIn >= 1 && searchIn <= 3) continue;
				if (v.IsFolder) continue;
				if (v.Name.Ends(true, _aSkipImages) > 0) continue;
			}
			var sw = _SkipWildcards; if (sw.Length != 0 && 0 != (path = v.ItemPath).Like(true, sw)) continue;
			//p1.Start(v.Name);
			if (!v.GetCurrentText(out text, silent: true) || text.Length == 0 || text.Contains('\0')) continue;
			//perf.nw();

			long time = bSlow != null ? perf.ms : 0;

			_FindAllInString(text, f, a);

			if (a.Count != 0) {
				b.Append("<lc #C0E0C0>");
				path ??= v.ItemPath;
				string link = v.IdStringWithWorkspace;
				if (v.IsFolder) {
					b.AppendFormat("<+open \"{0}\"><c #808080>{1}<><>    <c #008000>//folder<>", link, path);
				} else {
					int i1 = path.Length - v.Name.Length;
					string s1 = path[..i1], s2 = path[i1..];
					aFiles.Add(v); nFound += a.Count;
					int ns = 120 - path.Length * 7 / 4;
#if true //open and select the first found text
					b.AppendFormat("<+f \"{0} {1} {2}\"><c #808080>{3}<><b>{4}{5}      <><>    <+ra \"{0}\"><c #80ff>Replace all<><>",
						link, a[0].Start.Value, a[0].End.Value, s1, s2, ns > 0 ? new string(' ', ns) : null);
#else //just open
						b.AppendFormat("<+open \"{0}\"><c #808080>{1}<><b>{2}{3}      <><>    <+ra \"{0}\"><c #80ff>Replace all<><>",
							link, s1, s2, ns > 0 ? new string(' ', ns) : null);
#endif
				}
				b.AppendLine("<>");
				if (b.Length < 10_000_000) {
					for (int i = 0; i < a.Count; i++) {
						var range = a[i];
						int start = range.Start.Value, end = range.End.Value, lineStart = start, lineEnd = end;
						int lsMax = Math.Max(lineStart - 100, 0), leMax = Math.Min(lineEnd + 200, text.Length); //start/end limits like in VS
						for (; lineStart > lsMax; lineStart--) { char c = text[lineStart - 1]; if (c == '\n' || c == '\r') break; }
						bool limitStart = lineStart == lsMax && lineStart > 0;
						for (; lineEnd < leMax; lineEnd++) { char c = text[lineEnd]; if (c == '\r' || c == '\n') break; }
						bool limitEnd = lineEnd == leMax && lineEnd < text.Length;
						b.AppendFormat("<+f \"{0} {1} {2}\">", link, start.ToString(), end.ToString())
							.Append(limitStart ? "…<\a>" : "<\a>").Append(text, lineStart, start - lineStart).Append("</\a>")
							.Append("<bc #ffff5f><\a>").Append(text, start, end - start).Append("</\a><>")
							.Append("<\a>").Append(text, end, lineEnd - end).Append(limitEnd ? "</\a>…" : "</\a>")
							.AppendLine("<>");
					}
				}
			}

			if (bSlow != null) {
				time = perf.ms - time;
				if (time >= timeSlow + (jited ? 0 : 100)) {
					if (bSlow.Length == 0) bSlow.AppendLine("<lc #FFC000>Slow files:<>");
					bSlow.Append(time).Append(" ms <open>").Append(v.ItemPath).Append("<> , length ").Append(text.Length).AppendLine();
				}
				jited = true;
			}
		}

		if (nFound > 0) {
			var guid = Guid.NewGuid().ToString(); ; //probably don't need, but safer
			b.AppendFormat("<bc #FFC000>Found {0} in {1} files.    <+raif \"{2}\"><c #80ff>Replace all...<><>    <+caf><c #80ff>Close all<><>", nFound, aFiles.Count, guid).AppendLine("<>");
			_lastFindAll = (f, aFiles, guid, null);
		}

		if (folder != App.Model.Root)
			b.Append("<bc #FFC000>Note: searched only in folder ").Append(folder.Name).AppendLine(".<>");
		if (searchIn > 0)
			b.Append("<bc #FFC000>Note: searched only in ")
			   .Append(searchIn switch { 1 => "C#", 2 => "C# script", 3 => "C# class", _ => "non-C#" })
			   .AppendLine(" files. It is set in Find Options dialog.<>");
		b.Append(bSlow);

		cFound.aaaSetText(b.ToString());
	}

	(_TextToFind f, List<FileNode> files, string guid, System.Collections.BitArray wasOpen) _lastFindAll;

	void _ReplaceAllInFiles(string sGuid) {
		var (f, files, guid, _) = _lastFindAll;
		if (guid != sGuid) return;
		if (!_ValidateReplacement(f, files[0])) return; //avoid opening files in editor when invalid regex replacement
		if (!_CanReplaceInFiles()) return;

		if (1 != dialog.show("Replace text in files",
			"""
Replaces text in all files displayed in the Found panel.
Opens files to enable Undo.

Before replacing you may want to backup the workspace <a href="backup">folder</a>.
""",
			"Replace|Cancel",
			flags: DFlags.CenterMouse,
			owner: App.Hmain,
			onLinkClick: e => run.selectInExplorer(folders.Workspace))) return;

		var d = dialog.showProgress(marquee: false, "Replacing", owner: App.Hmain);
		try {
			App.Wmain.IsEnabled = false;
			bool needWasOpen = _lastFindAll.wasOpen == null;
			for (int i = 0; i < files.Count; i++) {
				var v = files[i];
				if (needWasOpen && App.Model.OpenFiles.Contains(v)) (_lastFindAll.wasOpen ??= new(files.Count))[i] = true;
				if (!App.Model.SetCurrentFile(v)) { print.it("Failed to open " + v.Name); continue; }
				if ((i & 15) == 15) wait.doEvents(); //makes slower, but visually better. Without it the progress dialog almost does not respond, although other thread; maybe clicking it tries to change wmain Z order etc.
				_ReplaceAllInEditor(f);
				if (!d.IsOpen) break;
				d.Send.Progress(Math2.PercentFromValue(files.Count, i + 1));
			}
		}
		finally {
			d.Send.Close();
			App.Wmain.IsEnabled = true;
		}
	}

	//when clicked link "Replace all" in "find in files" results. The file is already open.
	void _ReplaceAllInFile() {
		if (!_CanReplaceInFiles()) return;
		_ReplaceAllInEditor(_lastFindAll.f);
	}

	bool _CanReplaceInFiles() {
		var f = _lastFindAll.f;
		bool ok = f.findText == _tFind.Text
			&& f.matchCase == _cCase.IsChecked
			&& f.wholeWord == _cWord.IsChecked
			&& (f.rx != null) == _cRegex.IsChecked;
		if (!ok) {
			dialog.show(null, "Please click 'In files' to update the Found panel.", owner: this);
			return false;
		}
		f.replaceText = _tReplace.Text;
		_AddToRecent(f, onlyRepl: true);
		return true;
	}

	//struct _Perf : IDisposable
	//{
	//	long _t;
	//	string _file;

	//	public void Start(string file) {
	//		_t = perf.mcs;
	//		_file = file;
	//	}

	//	public void Dispose() {
	//		if (_t == 0) return;
	//		long t=perf.mcs-_t;
	//		if (t < 20000) return;
	//		print.it(t, _file);
	//	}
	//}

	//struct _TempDisableControl : IDisposable
	//{
	//	UIElement _e;
	//	int _enableAfter;

	//	public _TempDisableControl(UIElement e, int enableAfter = 0) {
	//		_e = e;
	//		_enableAfter = enableAfter;
	//		e.IsEnabled = false;
	//	}

	//	public void Dispose() {
	//		if (_enableAfter == 0) _e.IsEnabled = true;
	//		else { var e = _e; timer.after(_enableAfter, _ => e.IsEnabled = true); }
	//	}
	//}

	#endregion

	#region recent

	string _recentPrevFind, _recentPrevReplace;
	int _recentPrevOptions;

	//temp is false when clicked a button, true when changed the find text or a checkbox.
	void _AddToRecent(_TextToFind f, bool onlyRepl = false) {
		if (!onlyRepl) {
			int k = f.matchCase ? 1 : 0; if (f.wholeWord) k |= 2; else if (f.rx != null) k |= 4;
			if (f.findText != _recentPrevFind || k != _recentPrevOptions) _Add(false, _recentPrevFind = f.findText, _recentPrevOptions = k);
		}
		if (!f.replaceText.NE() && f.replaceText != _recentPrevReplace) _Add(true, _recentPrevReplace = f.replaceText, 0);

		static void _Add(bool replace, string text, int options) {
			if (text.Length > 1000) {
				//if(0 != (options & 4)) print.warning("The find text of length > 1000 will not be saved to 'recent'.", -1);
				return;
			}
			var ri = new FRRecentItem { t = text, o = options };
			//var a = _RecentLoad(replace);
			var a = (replace ? App.Settings.find_recentReplace : App.Settings.find_recent) ?? new FRRecentItem[0];
			if (a.NE_()) a = new FRRecentItem[] { ri };
			else if (a[0].t == text) a[0] = ri;
			else {
				for (int i = a.Length; --i > 0;) if (a[i].t == text) a = a.RemoveAt(i); //no duplicates
				if (a.Length > 19) a = a[0..19]; //limit count
				a = a.InsertAt(0, ri);
			}
			//_RecentSave(replace, a);
			if (replace) App.Settings.find_recentReplace = a; else App.Settings.find_recent = a;
		}
	}

	void _RecentPopupList(TextBox tb) {
		bool replace = tb == _tReplace;
		//var a = _RecentLoad(replace);
		var a = replace ? App.Settings.find_recentReplace : App.Settings.find_recent;
		if (a == null) return;
		var p = new KPopupListBox { PlacementTarget = tb };
		var k = p.Control;
		foreach (var v in a) k.Items.Add(v);
		p.OK += o => {
			var r = o as FRRecentItem;
			tb.Text = r.t;
			if (!replace) {
				int k = r.o;
				_cCase.IsChecked = 0 != (k & 1);
				_cWord.IsChecked = 0 != (k & 2);
				_cRegex.IsChecked = 0 != (k & 4);
			}
		};
		Dispatcher.InvokeAsync(() => p.IsOpen = true);
	}

	//rejected: save recent find/replace strings in separate csv files, not in App.Settings
	//static FRRecentItem[] _RecentLoad(bool replace) {
	//	var file = _RecentFile(replace);
	//	var x = filesystem.exists(file, true).File ? csvTable.load(file) : null;
	//	if (x == null) return null;
	//	var a = new FRRecentItem[x.RowCount];
	//	for (int i = 0; i < a.Length; i++) {
	//		a[i] = new() { t = x[i][1], o = x[i][0].ToInt() };
	//	}
	//	return a;
	//}

	//static void _RecentSave(bool replace, FRRecentItem[] a) {
	//	var x = new csvTable { ColumnCount = 2, RowCount = a.Length };
	//	for (int i = 0; i < a.Length; i++) {
	//		x[i][0] = a[i].o.ToS();
	//		x[i][1] = a[i].t;
	//	}
	//	x.Save(_RecentFile(replace));
	//}

	//static string _RecentFile(bool replace) => AppSettings.DirBS + (replace ? "Recent replace.csv" : "Recent find.csv");

	#endregion
}

record FRRecentItem //not nested in PanelFind because used with JSettings (would load UI dlls).
{
	public string t;
	public int o;

	public override string ToString() => t.Limit(200); //ListBox item display text
}
