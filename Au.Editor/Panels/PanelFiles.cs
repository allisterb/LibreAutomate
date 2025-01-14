using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using Au.Controls;

partial class PanelFiles {
	FilesModel.FilesView _tv;
	TextBox _tFind;
	timer _timerFind;
	
	public PanelFiles() {
		P.UiaSetName("Files panel");
		P.Background = SystemColors.ControlBrush;
		
		var b = new wpfBuilder(P).Columns(-1).Options(margin: new());
		b.Row(-1).Add(out _tv).Name("Files_list", true);
		
		b.Row(2) //maybe 4 would look better, but then can be confused with a splitter
			.Add<Border>().Border(thickness2: new(0, 1, 0, 1));
		
		_tFind = new() { BorderThickness = default };
		b.R.Add<AdornerDecorator>().Add(_tFind, flags: WBAdd.ChildOfLast).Name("Find_file", true)
			.Watermark("Find file").Tooltip(@"Part of file name, or wildcard expression.
Examples: part, start*, *end.cs, **r regex, **m green.cs||blue.cs.");
		
		//CONSIDER: File bookmarks. And/or tags. Probably not very useful. Unless many users will want it.
		
		b.End();
		
		_tFind.TextChanged += (_, _) => { (_timerFind ??= new(_ => _Find())).After(_tFind.Text.Length switch { 1 => 1200, 2 => 600, _ => 300 }); };
		_tFind.GotKeyboardFocus += (_, _) => P.Dispatcher.InvokeAsync(() => _tFind.SelectAll());
		_tFind.PreviewMouseUp += (_, e) => { if (e.ChangedButton == MouseButton.Middle) _tFind.Clear(); };
		
		EditGoBack.DisableUI();
	}
	
	public UserControl P { get; } = new();
	
	public FilesModel.FilesView TreeControl => _tv;
	
	private void _Find() {
		var s = _tFind.Text;
		if (s.NE()) {
			Panels.Found.ClearResults(PanelFound.Found.Files);
			return;
		}
		
		var workingState = Panels.Found.Prepare(PanelFound.Found.Files, s, out var b);
		
		var wild = wildex.hasWildcardChars(s) ? new wildex(s, noException: true) : null;
		
		foreach (var f in App.Model.Root.Descendants()) {
			var name = f.Name;
			int i = -1;
			if (wild != null) {
				if (!wild.Match(name)) continue;
			} else {
				i = name.Find(s, true);
				if (i < 0) continue;
			}
			
			var path = f.ItemPath;
			int i1 = path.Length - name.Length;
			b.Link2(f).Gray(path.AsSpan(0, i1)).Text(name);
			if (i >= 0) {
				i += b.Length - name.Length;
				b.Indic(PanelFound.Indicators.HiliteY, i, i + s.Length);
			}
			b.Link_();
			if (f.IsFolder) b.Green("    //folder");
			b.NL();
		}
		
		if (b.Length == 0) return;
		
		Panels.Found.SetResults(workingState, b);
	}
}
