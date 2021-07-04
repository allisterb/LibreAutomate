using System;
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
using System.Windows;
using System.Windows.Controls;
//using System.Linq;

using Au;
using Au.Types;
using Au.Controls;
using static Au.Controls.Sci;
using Au.More;

class PanelOutput : DockPanel
{
	_SciOutput _c;
	Queue<PrintServerMessage> _history;

	public KScintilla ZOutput => _c;

	public PanelOutput() {
		_c = new _SciOutput(this) { Name = "Output_text" };
		this.Children.Add(_c);
		_history = new Queue<PrintServerMessage>();
		App.Commands.BindKeysTarget(this, "Output");
	}

	public void ZClear() { _c.zClearText(); _c.Call(SCI_SETSCROLLWIDTH, 1); }

	public void ZCopy() { _c.Call(SCI_COPY); }

	public void ZFind() { Panels.Find.ZCtrlF(_c); }

	public void ZHistory() {
		var p = new KPopupListBox { PlacementTarget = this, Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint };
		p.Control.ItemsSource = _history;
		p.OK += o => print.it((o as PrintServerMessage).Text);
		Dispatcher.InvokeAsync(() => p.IsOpen = true);
	}

	void _c_HandleCreated() {
		_inInitSettings = true;
		if (ZWrapLines) ZWrapLines = true;
		if (ZWhiteSpace) ZWhiteSpace = true;
		if (ZTopmost) App.Commands[nameof(Menus.Tools.Output.Topmost_when_floating)].Checked = true; //see also OnParentChanged, below
		_inInitSettings = false;
		Panels.PanelManager["Output"].FloatingChanged += (_, floating) => { if (floating && ZTopmost) _SetTopmost(true); };
	}
	bool _inInitSettings;

	public bool ZWrapLines {
		get => App.Settings.output_wrap;
		set {
			Debug.Assert(!_inInitSettings || value);
			if (!_inInitSettings) App.Settings.output_wrap = value;
			//_c.Call(SCI_SETWRAPVISUALFLAGS, SC_WRAPVISUALFLAG_START | SC_WRAPVISUALFLAG_END); //in KScintilla.ZOnHandleCreated
			//_c.Call(SCI_SETWRAPINDENTMODE, SC_WRAPINDENT_INDENT); //in KScintilla.ZOnHandleCreated
			_c.Call(SCI_SETWRAPMODE, value ? SC_WRAP_WORD : 0);
			App.Commands[nameof(Menus.Tools.Output.Wrap_lines_in_output)].Checked = value;
		}
	}

	public bool ZWhiteSpace {
		get => App.Settings.output_white;
		set {
			Debug.Assert(!_inInitSettings || value);
			if (!_inInitSettings) App.Settings.output_white = value;
			_c.Call(SCI_SETWHITESPACEFORE, 1, 0xFF0080);
			_c.Call(SCI_SETVIEWWS, value);
			App.Commands[nameof(Menus.Tools.Output.White_space_in_output)].Checked = value;
		}
	}

	public bool ZTopmost {
		get => App.Settings.output_topmost;
		set {
			if (Panels.PanelManager["Output"].Floating) _SetTopmost(value);
			App.Settings.output_topmost = value;
			App.Commands[nameof(Menus.Tools.Output.Topmost_when_floating)].Checked = value;
		}
	}

	void _SetTopmost(bool on) {
		var w = this.Hwnd().Window;
		if (on) {
			w.OwnerWindow = default;
			w.ZorderTopmost();
			//w.SetExStyle(WSE.APPWINDOW, SetAddRemove.Add);
			//wnd.getwnd.root.ActivateL(); w.ActivateL(); //let taskbar add button
		} else {
			w.ZorderNoTopmost();
			w.OwnerWindow = App.Wmain.Hwnd();
		}
	}

	class _SciOutput : KScintilla
	{
		PanelOutput _p;
		StringBuilder _sb;

		public _SciOutput(PanelOutput panel) {
			_p = panel;

			ZInitReadOnlyAlways = true;
			ZInitTagsStyle = ZTagsStyle.AutoWithPrefix;
			ZInitImages = true;

			//App.Commands[nameof(Menus.Tools.Output)].SetKeysTarget(this);
		}

		protected override void ZOnHandleCreated() {
			_p._c_HandleCreated();
			zSetMarginWidth(1, 3);
			zStyleBackColor(STYLE_DEFAULT, 0xF7F7F7);
			//zStyleFont(STYLE_DEFAULT, "Courier New", 8); //maybe better, except <b>
			zStyleFont(STYLE_DEFAULT, "Consolas", 9);
			zStyleClearAll();

			SciTags.AddCommonLinkTag("open", s => _OpenLink(s));
			SciTags.AddCommonLinkTag("script", s => _RunScript(s));
			ZTags.AddLinkTag("+properties", fid => {
				var f = App.Model.FindCodeFile(fid);
				if (f == null || !App.Model.SetCurrentFile(f)) return;
				Menus.File.Properties();
			});

			App.PrintServer.SetNotifications(Hwnd, Api.WM_APP);

			base.ZOnHandleCreated();
		}

		protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			//wnd.more.printMsg(out var s, default, msg, wParam, lParam); print.qm2.write(s);
			switch (msg) {
			case Api.WM_APP:
				ZTags.PrintServerProcessMessages(App.PrintServer, _onServerMessage ??= _OnServerMessage);
				return default;
			case Api.WM_MBUTTONDOWN:
				_p.ZClear();
				return default;
			case Api.WM_CONTEXTMENU:
				var m = new ContextMenu { PlacementTarget = this };
				App.Commands[nameof(Menus.Tools.Output)].CopyToMenu(m);
				m.IsOpen = true;
				return default;
			}
			return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
		}

		Action<PrintServerMessage> _onServerMessage;
		void _OnServerMessage(PrintServerMessage m) {
			if (m.Type != PrintServerMessageType.Write) {
				if (m.Type == PrintServerMessageType.TaskEvent) RecentTT.TriggerEvent(m);
				return;
			}

			//create links in compilation errors/warnings or run-time stack trace
			var s = m.Text; int i;
			if (s.Length >= 22) {
				if (s.Starts("<><Z #") && s.Eq(12, ">Compilation: ")) { //compilation
					s_rx1 ??= new regexp(@"(?m)^\[(.+?)(\((\d+),(\d+)\))?\]: ");
					m.Text = s_rx1.Replace(s, x => {
						var f = App.Model.FindByFilePath(x[1].Value);
						if (f == null) return x[0].Value;
						return $"<open \"{f.IdStringWithWorkspace}|{x[3].Value}|{x[4].Value}\">{f.Name}{x[2].Value}<>: ";
					});
				} else if ((i = s.Find("\n   at ") + 1) > 0 && s.Find(":line ", i) > 0) { //stack trace with source file info
					var b = _sb ??= new StringBuilder(s.Length + 2000);
					b.Clear();
					//print.qm2.write("'" + s + "'");
					int iLiteral = 0;
					if (!s.Starts("<>")) b.Append("<>");
					else {
						iLiteral = i - 1; if (s[iLiteral - 1] == '\r') iLiteral--;
						if (0 == s.Eq(iLiteral -= 3, false, "<_>", "<\a>")) iLiteral = 0;
					}
					if (iLiteral > 0) b.Append(s, 0, iLiteral).AppendLine(); else b.Append(s, 0, i);
					s_rx2 ??= new regexp(@" in (.+?):line (?=\d+$)");
					bool replaced = false, isMain = false;
					int stackEnd = s.Length/*, stackEnd2 = 0*/;
					foreach (var k in s.Segments(SegSep.Line, range: i..)) {
						//print.qm2.write("'"+k+"'");
						if (s.Eq(k.start, "   at ")) {
							if (isMain) {
								//if(stackEnd2 == 0 && s.Eq(k.start, "   at Script.Main(String[] args) in ")) stackEnd2 = k.start; //rejected. In some cases may cut something important.
								continue;
							}
							if (!s_rx2.MatchG(s, out var g, 1, (k.start + 6)..k.end)) continue; //note: no "   at " if this is an inner exception marker. Also in aggregate exception stack trace.
							var f = App.Model.FindByFilePath(g.Value); if (f == null) continue;
							int i1 = g.End + 6, len1 = k.end - i1;
							b.Append("   at ")
							.Append("<open \"").Append(f.IdStringWithWorkspace).Append('|').Append(s, i1, len1).Append("\">")
							.Append("line ").Append(s, i1, len1).Append("<> in <z 0xFAFAD2>").Append(f.Name).Append("<>");

							isMain
								= s.Eq(k.start, "   at Script..ctor(String[] args) in ")
								|| s.Eq(k.start, "   at Program..ctor(String[] args) in ")
								|| s.Eq(k.start, "   at <Program>$.<Main>$(String[] args) in "); //top-level statements
							if (!isMain || !f.IsScript) b.Append(", <\a>").Append(s, k.start + 6, g.Start - k.start - 10).Append("</\a>");
							b.AppendLine();

							replaced = true;
						} else if (!(s.Eq(k.start, "   ---") || s.Eq(k.start, "---"))) {
							stackEnd = k.start;
							break;
						}
					}
					if (replaced) {
						int j = stackEnd; //int j = stackEnd2 > 0 ? stackEnd2 : stackEnd;
						if (s[j - 1] == '\n') { if (s[--j - 1] == '\r') j--; }
						b.Append("   <fold><\a>   --- Raw stack trace ---\r\n").Append(s, i, j - i).Append("</\a></fold>");
						if (iLiteral > 0 && 0 != s.Eq(stackEnd, false, "</_>", "</\a")) stackEnd += 4;
						int more = s.Length - stackEnd;
						if (more > 0) {
							if (!s.Eq(stackEnd, "</fold>")) b.AppendLine();
							b.Append(s, stackEnd, more);
						}
						m.Text = b.ToString();
						//print.qm2.write("'" + m.Text + "'");
					}
					if (_sb.Capacity > 10_000) _sb = null; //let GC free it. Usually < 4000.
				}
			}

			if (s.Length <= 10_000) { //* 50 = 1 MB
				if (!ReferenceEquals(s, m.Text)) m = new PrintServerMessage(PrintServerMessageType.Write, s, m.TimeUtc, m.Caller);
				var h = _p._history;
				h.Enqueue(m);
				if (h.Count > 50) h.Dequeue();
			}

			(_iPanel ??= Panels.PanelManager["Output"]).Visible = true; //SHOULDDO: if(App.Win.IsVisible) ?
		}
		static regexp s_rx1, s_rx2;
		KPanels.ILeaf _iPanel;

		static void _OpenLink(string s) {
			//print.it(s);
			var a = s.Split('|');
			App.Model.OpenAndGoTo2(a[0], a.Length > 1 ? a[1] : null, a.Length > 2 ? a[2] : null);
		}

		static void _RunScript(string s) {
			var a = s.Split('|');
			var f = App.Model.FindCodeFile(a[0]); if (f == null) return;
			CompileRun.CompileAndRun(true, f, a.Length == 1 ? null : a.RemoveAt(0));
		}
	}
}
