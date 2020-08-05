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
using System.Windows.Forms;
using System.Drawing;
//using System.Linq;
using System.Xml.Linq;
using System.Collections;

using Au.Types;
using Au.Controls;
using SG = SourceGrid;
using Aga.Controls.Tree;
using Aga.Controls.Tree.NodeControls;

//FUTURE: add UI to format code 'w = w.Get.Right();' etc.
//FUTURE: init from code string.
//SHOULDDO: try to find and select control in current tree when captured from same window. Like in acc tool.

namespace Au.Tools
{
	partial class FormAWnd : ToolForm
	{
		AWnd _wnd, _con;
		TUtil.CaptureWindowEtcWithHotkey _capt;
		CommonInfos _commonInfos;
		bool _uncheckControl;
		string _wndName;

		public FormAWnd(AWnd wnd = default, bool uncheckControl = false)
		{
			InitializeComponent();
			splitContainer3.SplitterWidth = splitContainer1.SplitterWidth;

			AWnd.More.SavedRect.Restore(this, Program.Settings.tools_AWnd_wndPos);

			Action<SG.CellContext> f = _grid_ZValueChanged;
			_grid.ZValueChanged += f;
			_grid2.ZValueChanged += f;

			_InitTree();

			_con = wnd;
			_uncheckControl = uncheckControl;

			//_grid.ZDebug = true;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (!_con.Is0) _SetWnd(false);

			_InitInfo();

			_cCapture.Checked = true;
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			_cCapture.Checked = false;
			_capt?.Dispose();

			Program.Settings.tools_AWnd_wndPos = new AWnd.More.SavedRect(this).ToString();

			base.OnFormClosing(e);
		}

		void _SetWnd(bool captured)
		{
			//note: don't reorder all the calls.

			_bTest.Enabled = true; _bOK.Enabled = true;

			var wndOld = _wnd;
			_wnd = _con.Window;
			if(_wnd == _con) _con = default;
			bool newWindow = _wnd != wndOld;

			_FillGrid2();
			_ClearTree();
			if(!_FillGrid(newWindow)) return;
			_FormatCode(false, newWindow);
			_FillTree();
		}

		bool _FillGrid(bool newWindow)
		{
			bool isCon = !_con.Is0;

			var g = _grid;

			if(newWindow) g.ZClear(); else g.RowsCount = 5;

			_WinInfo f = default;

			if(!_GetClassName(_wnd, out f.wClass)) return false; //note: get even if !newWindow, to detect closed window
			if(isCon && !_GetClassName(_con, out f.cClass)) return false;
			_propError = null;

			_noeventGridValueChanged = true;

			var wndName = _wnd.NameTL_;
			if(newWindow) {
				g.ZAddHeaderRow("Window");
				g.ZAdd(null, "name", TUtil.EscapeWindowName(wndName, true), true, info: "Window name.$");
				g.ZAdd(null, "class", TUtil.StripWndClassName(f.wClass, true), true, info: "Window class name.$");
				f.wProg = _wnd.ProgramName;
				var ap = new List<string> { f.wProg, "WOwner.Process(processId)", "WOwner.Thread(threadId)" }; if(!_wnd.OwnerWindow.Is0) ap.Add("WOwner.Window(ow)");
				g.ZAdd(null, "program", ap, wndName.NE(), info: "Program.$", etype: ParamGrid.EditType.ComboText, comboIndex: 0);
				g.ZAdd(null, "contains", (Func<string[]>)_ContainsCombo_DropDown, false, info: "An accessible object in the window. Format: 'role' name.\r\nName$$", etype: ParamGrid.EditType.ComboText);
			} else if(wndName != _wndName) {
				if(TUtil.ShouldChangeGridWildex(g.ZGetCellText("name", 1), wndName))
					g.ZSetCellText("name", 1, TUtil.EscapeWindowName(wndName, true));
			}
			f.wName = _wndName = wndName;

			if(isCon) {
				g.ZAddHeaderRow("Control", check: !_uncheckControl);
				g.ZAddHidden = _uncheckControl;

				//name combo
				f.cName = _con.Name;
				int iSel = f.cName.NE() ? -1 : 0;
				var an = new List<string> { TUtil.EscapeWildex(f.cName) };
				_ConNameAdd("***wfName ", f.cWF = _con.NameWinForms);
				/*bool isAcc =*/
				_ConNameAdd("***accName ", f.cAcc = _con.NameAcc);
				//bool isLabel = _ConNameAdd("***label ", f.cLabel = _con.NameLabel);
				//if(isAcc && isLabel && iSel == an.Count - 2 && f.cAcc == f.cLabel) iSel++; //if label == accName, prefer label
				if(iSel < 0) iSel = 0; //never select text, even if all others unavailable
				_ConNameAdd("***text ", f.cText = _con.ControlText);
				bool _ConNameAdd(string prefix, string value)
				{
					if(value.NE()) return false;
					if(iSel < 0) iSel = an.Count;
					an.Add(prefix + TUtil.EscapeWildex(value));
					return true;
				}

				bool idUseful = TUtil.GetUsefulControlId(_con, _wnd, out f.cId);
				if(idUseful) g.ZAdd(null, "id", f.cId, true); else an.Add("***id " + f.cId + " (probably not useful)");
				g.ZAdd("nameC", "name", an, !idUseful, info: "Control name.$", etype: ParamGrid.EditType.ComboText, comboIndex: iSel);
				g.ZAdd("classC", "class", TUtil.StripWndClassName(f.cClass, true), !idUseful, info: "Control class name.$");
				g.ZAddHidden = false;
			}

			_uncheckControl = false;
			_noeventGridValueChanged = false;
			g.ZAutoSize();
			_FillWindowInfo(f);
			return true;

			string[] _ContainsCombo_DropDown()
			{
				try {
					var a1 = new List<string>();
					//child
					foreach(var c in _wnd.Get.Children(onlyVisible: true)) {
						var cn = c.Name; if(cn.NE()) continue;
						cn = "c '" + TUtil.StripWndClassName(c.ClassName, true) + "' " + TUtil.EscapeWildex(cn);
						if(!a1.Contains(cn)) a1.Add(cn);
					}
					//acc
					var a2 = new List<string>();
					var a3 = AAcc.FindAll(_wnd, name: "?*", prop: "notin=SCROLLBAR\0maxcc=100", flags: AFFlags.ClientArea); //all that have a name
					string prevName = null;
					for(int i = a3.Length; --i >= 0;) {
						if(!a3[i].GetProperties("Rn", out var prop)) continue;
						if(prop.Name == prevName && prop.Role == "WINDOW") continue; prevName = prop.Name; //skip parent WINDOW
						string rn = "a '" + prop.Role + "' " + TUtil.EscapeWildex(prop.Name);
						if(!a2.Contains(rn)) a2.Add(rn);
					}
					a2.Reverse();
					a1.AddRange(a2);

					return a1.ToArray();
					//rejected: sort
				}
				catch(Exception ex) { ADebug.Print(ex); return null; }
			}

			bool _GetClassName(AWnd w, out string cn)
			{
				cn = w.ClassName;
				if(cn != null) return true;
				_propError = "Failed to get " + (w == _wnd ? "window" : "control") + " properties: \r\n" + ALastError.Message;
				_grid.ZClear();
				_grid.Invalidate();
				_winInfo.Z.ClearText();
				return false;
			}
		}

		void _grid_ZValueChanged(SG.CellContext sender)
		{
			//AOutput.Write(sender.DisplayText);
			//AOutput.Write(_inSetGrid);

			if(_noeventGridValueChanged) return; _noeventGridValueChanged = true;
			var g = sender.Grid as ParamGrid;
			var pos = sender.Position;
			switch(pos.Column) {
			case 0:
				if(g == _grid) {
					bool on = (sender.Cell as SG.Cells.CheckBox).Checked.GetValueOrDefault();
					switch(g.ZGetRowKey(pos.Row)) {
					case "nameC": if(on) g.ZCheckIfExists("id", false); break;
					case "id": if(on) g.ZCheck("nameC", false); break;
					case "Control":
						g.ZShowRows(on, pos.Row + 1, -1);
						_grid2.ZShowRows(on, _grid2.ZFindRow("Control"), 4);
						break;
					}
				}
				break;
			case 1:
				break;
			}
			_noeventGridValueChanged = false;

			_FormatCode();
		}
		bool _noeventGridValueChanged;

		(string code, string wndVar) _FormatCode(bool forTest = false, bool newWindow = false)
		{
			if(_grid.RowsCount == 0) return default; //cleared on exception

			var b = new StringBuilder();

			bool isCon = !_con.Is0 && _IsChecked("Control");
			bool orThrow = !forTest && _IsChecked2("orThrow");

			b.Append(forTest ? "AWnd w;\r\n" : "var ").Append("w = ");

			string waitTime = null;
			bool isWait = !forTest && _grid2.ZGetValue("wait", out waitTime, false);
			if(isWait) {
				b.Append("AWnd.Wait(").AppendWaitTime(waitTime, orThrow || isCon);
				b.Append(", active: true");
			} else {
				if(orThrow || isCon) b.Append('+');
				b.Append("AWnd.Find(");
			}

			int m = 0;
			string sName, sClass, sAlso;
			if(_grid.ZGetValue("name", out sName, false) && sName == null) sName = ""; b.AppendStringArg(sName, noComma: !isWait);
			if(_grid.ZGetValue("class", out sClass, true)) m |= 1;
			if(_grid.ZGetValue("program", out var sProg, true)) m |= 2;
			if(m != 0) b.AppendStringArg(sClass);
			if(0 != (m & 2)) {
				if(!sProg.Starts("WOwner.")) b.AppendStringArg(sProg);
				else if(!forTest) b.AppendOtherArg(sProg);
				else m &= ~2;
			}
			b.AppendFlagsFromGrid(typeof(WFlags), _grid2, m < 2 ? "flags" : null);
			if(_grid2.ZGetValue("alsoW", out sAlso, true)) b.AppendOtherArg(sAlso, "also");
			if(_grid.ZGetValue("contains", out var sContains, true)) b.AppendStringArg(sContains, "contains");

			if(isCon) {
				b.AppendLine(");");
				m = 0;
				if(_grid.ZGetValueIfExists("id", out var sId, true)) m |= 1;
				else if(_grid.ZGetValue("nameC", out sName, false)) { m |= 2; if(sName == null) sName = ""; }
				if(_grid.ZGetValue("classC", out sClass, true)) m |= 4;
				if(_grid2.ZGetValue("alsoC", out sAlso, true)) m |= 8;
				if(_grid2.ZGetValue("skip", out var sSkip, true)) m |= 16;
				if(!forTest) b.Append("var c = ");
				if(orThrow) b.Append('+');
				b.Append("w.Child");
				if(m == 1) {
					b.Append("ById(").Append(sId);
					b.AppendFlagsFromGrid(typeof(WCFlags), _grid2, prefix: "C.");
				} else {
					b.Append('(');
					if(0 != (m & 1)) b.Append("\"***id ").Append(sId).Append('\"');
					else b.AppendStringArg(sName, noComma: true);
					if(0 != (m & 4)) b.AppendStringArg(sClass);
					b.AppendFlagsFromGrid(typeof(WCFlags), _grid2, (0 == (m & 4)) ? "null" : null, "C.");
					if(0 != (m & 8)) b.AppendOtherArg(sAlso, "also");
					if(0 != (m & 16)) b.AppendOtherArg(sSkip, "skip");
				}
			}

			b.Append(");");

			if(!forTest && isCon && 0 == (m & 2)) { //add comments controlClass controlName
				sName = sClass = null;
				if(0 == (m & 2)) sName = _grid.ZGetCellText("nameC", 1);
				if(0 == (m & 4)) sClass = _grid.ZGetCellText("classC", 1);
				m = 0; if(!sName.NE()) m |= 1; if(!sClass.NE()) m |= 2;
				if(m != 0) {
					b.Append(" // ");
					if(0 != (m & 2)) b.Append(sClass);
					if(0 != (m & 1)) {
						if(0 != (m & 2)) b.Append(' ');
						sName = sName.Limit(100).RegexReplace(@"^\*\*\*\w+ (.+)", "$1");
						b.AppendStringArg(sName, noComma: true);
					}
				}
			}

			if(!orThrow && !forTest) b.AppendLine().Append("if(").Append(isCon ? "c" : "w").Append(".Is0) { AOutput.Write(\"not found\"); }");

			var R = b.ToString();

			if(!forTest) {
				_code.ZSetText(R);
			}

			return (R, "w");
		}

		void _FillGrid2()
		{
			var g = _grid2;
			bool noCon = _con.Is0 || _uncheckControl;

			if(g.RowsCount != 0) {
				int i = g.ZFindRow("Control");
				if(g.Rows[i].Visible == noCon) g.ZShowRows(!noCon, i, 4);
				return;
			}

			g.ZAddHeaderRow("Window");
			_AddFlag(nameof(WFlags.HiddenToo), "Can be invisible", tt: "Flag WFlags.HiddenToo.");
			_AddFlag(nameof(WFlags.CloakedToo), "Can be cloaked", tt: "Find cloaked windows. Flag WFlags.CloakedToo.\r\nCloaked are windows on inactive Windows 10 virtual desktops, ghost windows of hidden Windows Store apps, various hidden system windows.");
			_AddProp("alsoW", "also", "o => true", tt: "Lambda that returns true if AWnd o is the wanted window.", info: c_infoAlsoW);
			_AddProp(null, "wait", "5", tt: c_infoWait);
			g.ZAddHidden = noCon;
			g.ZAddHeaderRow("Control");
			_AddFlag("C." + nameof(WCFlags.HiddenToo), "Can be invisible", tt: "Flag WCFlags.HiddenToo.");
			//_AddFlag("C." + nameof(WCFlags.DirectChild), "Direct child of the window", tt: "Don't find indirect descendant controls (children of children and so on).\r\nFlag WCFlags.DirectChild."); //rejected: almost not useful here
			_AddProp("alsoC", "also", "o => true", tt: "Lambda that returns true if AWnd o is the wanted control.", info: c_infoAlsoC);
			_AddProp(null, "skip", "1", tt: "0-based index of matching control.\nFor example, if 1, gets the second matching control.");
			g.ZAddHidden = false;
			_AddFlag("orThrow", "Exception if not found", true, tt: "Checked - throw exception.\nUnchecked - return default(AWnd).");

			g.ZAutoSize();

			void _AddProp(string key, string name, string value, string tt = null, string info = null)
			{
				g.ZAdd(key, name, value, false, tt, info);
			}

			void _AddFlag(string flag, string name, bool check = false, string tt = null)
			{
				g.ZAddCheck(flag, name, check, tt);
			}
		}

		#region capture

		private void _cCapture_CheckedChanged(object sender, EventArgs e)
		{
			if(_capt == null) _capt = new TUtil.CaptureWindowEtcWithHotkey(this, _cCapture, () => AWnd.FromMouse().Rect);
			_capt.StartStop(_cCapture.Checked);
		}

		void _Capture()
		{
			var c = AWnd.FromMouse(); if(c.Is0) return;
			_con = c;
			_uncheckControl = false;
			_SetWnd(true);
			var w = (AWnd)this;
			if(w.IsMinimized) {
				w.ShowNotMinMax();
				w.ActivateLL();
			}
		}

		protected override void WndProc(ref Message m)
		{
			//AWnd w = (AWnd)this; LPARAM wParam = m.WParam, lParam = m.LParam;

			if(_capt != null && _capt.WndProc(ref m, out bool capture)) {
				if(capture) _Capture();
				return;
			}

			base.WndProc(ref m);
		}

		#endregion

		#region util, misc

		bool _IsChecked(int row) => _grid.ZIsChecked(row);
		bool _IsChecked(string rowKey) => _grid.ZIsChecked(rowKey);
		bool _IsChecked2(string rowKey) => _grid2.ZIsChecked(rowKey);

		#endregion

		#region OK, Test

		/// <summary>
		/// When OK clicked, the top-level window (even when <see cref="ZResultUseControl"/> is true).
		/// </summary>
		public AWnd ZResultWindow => _wnd;

		/// <summary>
		/// When OK clicked, the control (even when <see cref="ZResultUseControl"/> is false) or default(AWnd).
		/// </summary>
		public AWnd ZResultControl => _con;

		/// <summary>
		/// When OK clicked, true if a control was selected and the 'Control' checkbox checked.
		/// Use <see cref="ZResultWindow"/> or <see cref="ZResultControl"/>, depending on this property.
		/// </summary>
		public bool ZResultUseControl { get; private set; }

		/// <summary>
		/// When OK clicked, contains C# code.
		/// </summary>
		public override string ZResultCode { get; protected set; }

		private void _bOK_Click(object sender, EventArgs e)
		{
			ZResultCode = _code.Text;
			if(ZResultCode.NE()) this.DialogResult = DialogResult.Cancel;
			else ZResultUseControl = !_con.Is0 && _IsChecked("Control");
		}

		private void _bTest_Click(object sender, EventArgs ea)
		{
			var (code, wndVar) = _FormatCode(true); if(code == null) return;
			TUtil.RunTestFindObject(code, wndVar, _wnd, _bTest, _lSpeed, o => {
				var w = (AWnd)o;
				var r = w.Rect;
				if(w.IsMaximized && !w.IsChild) {
					var k = w.Screen.Bounds; k.Inflate(-2, -2);
					r.Intersect(k);
				}
				return r;
			});
		}

		#endregion

		#region tree

		//Most of this code copied from the AAcc form. Comments removed.

		(_WndNode xRoot, _WndNode xSelect) _CreateModel()
		{
			_WndNode xRoot = new _WndNode("root"), xSelect = null;

			var xWindow = new _WndNode("w") { c = _wnd };
			xRoot.Add(xWindow);
			//if(_con.Is0 || !_IsChecked("Control")) xSelect = xWindow; //no, always select control
			if(_con.Is0) xSelect = xWindow;
			_AddChildren(_wnd, xWindow);

			void _AddChildren(AWnd wParent, _WndNode nParent)
			{
				for(AWnd t = wParent.Get.FirstChild; !t.Is0; t = t.Get.Next()) {
					var x = new _WndNode("w") { c = t };
					nParent.Add(x);
					if(t == _con && xSelect == null) xSelect = x;
					_AddChildren(t, x);
				}
			}
			return (xRoot, xSelect);
		}

		void _FillTree()
		{
			var (xRoot, xSelect) = _CreateModel();
			if(xRoot == null) return;

			_tree.Model = new _WndTree(xRoot);

			if(xSelect != null) {
				var n = _tree.FindNodeByTag(xSelect);
				if(n != null) {
					_tree.Visible = false;
					_tree.EnsureVisible(n);
					n.IsSelected = true;
					_tree.Visible = true;
				}
			}
		}

		void _ClearTree()
		{
			_tree.Model = null;
		}

		NodeTextBox _ccName;

		void _InitTree()
		{
			_tree.Indent = 10;

			_ccName = new NodeTextBox();
			_tree.NodeControls.Add(_ccName);

			_ccName.ValueNeeded = node => {
				var k = node.Tag as _WndNode;
				return k.DisplayText;
			};
			_ccName.DrawText += _ccName_DrawText;

			_tree.NodeMouseClick += _tree_NodeMouseClick;
			_tree.KeyDown += _tree_KeyDown;
		}

		private void _ccName_DrawText(object sender, DrawEventArgs e)
		{
			var a = e.Node.Tag as _WndNode;
			if(e.Node.IsSelected) {
				if(e.Context.DrawSelection == DrawSelectionMode.Inactive) e.TextColor = Color.Blue;
			} else {
				if(a.IsInvisible) e.TextColor = Color.Gray;
			}
			if(a.IsException) e.TextColor = Color.Red;
		}

		private void _tree_NodeMouseClick(object sender, TreeNodeAdvMouseEventArgs e)
		{
			if(e.Button == MouseButtons.Left && e.ModifierKeys == 0) {
				_SelectFromTree(e.Node);
			}
		}

		private void _tree_KeyDown(object sender, KeyEventArgs e)
		{
			if((e.KeyCode == Keys.Space || e.KeyCode == Keys.Return) && e.Modifiers == 0) {
				_SelectFromTree(_tree.SelectedNode);
			}
		}

		void _SelectFromTree(TreeNodeAdv node)
		{
			if(node == null) return;
			var c = (node.Tag as _WndNode).c;
			_con = c == _wnd ? default : c;
			_FillGrid2(); //show-hide the control part if need
			if(!_FillGrid(false)) return;
			_FormatCode();
			if(!_con.Is0) TUtil.ShowOsdRect(_con.Rect);
		}

		class _WndTree : ITreeModel
		{
			public _WndNode Root;

			public _WndTree(_WndNode root)
			{
				Root = root;
			}

#pragma warning disable 67
			public event EventHandler<TreeModelEventArgs> NodesChanged;
			public event EventHandler<TreeModelEventArgs> NodesInserted;
			public event EventHandler<TreeModelEventArgs> NodesRemoved;
			public event EventHandler<TreePathEventArgs> StructureChanged;
#pragma warning restore 67

			public IEnumerable GetChildren(object nodeTag)
			{
				var x = nodeTag as _WndNode ?? Root;
				return x.Elements();
			}

			public bool IsLeaf(object nodeTag)
			{
				var x = nodeTag as _WndNode;
				return !x.HasElements;
			}
		}

		class _WndNode : XElement
		{
			public _WndNode(string name) : base(name) { }

			public AWnd c;
			string _displayText;

			public string DisplayText {
				get {
					if(_displayText == null) {
						var cn = c.ClassName;
						if(cn == null) {
							IsException = true;
							return _displayText = "Failed: " + ALastError.Message;
						}

						var name = c.Name;
						if(name.NE()) _displayText = cn;
						else {
							using(new Util.StringBuilder_(out var b)) {
								name = name.Escape(limit: 250);
								b.Append(cn).Append("  \"").Append(name).Append("\"");
								_displayText = b.ToString();
							}
						}

						IsInvisible = !c.IsVisible;
					}
					return _displayText;
				}
			}

			public bool IsInvisible { get; private set; }
			public bool IsException { get; private set; }
		}

		#endregion

		#region info

		struct _WinInfo
		{
			public string wClass, wName, wProg, cClass, cName, cText, /*cLabel,*/ cAcc, cWF;
			public int cId;

			public string Format(AWnd w, AWnd c, int wcp)
			{
				var b = new StringBuilder();

				if(wcp == 2 && c.Is0) wcp = 1;
				if(!w.IsAlive) return "";
				if(wcp == 1) { //window
					b.AppendLine("<Z #B0E0B0><b>Window<>    <+switch 2>Control<>    <+switch 3>Process<><>");
					if(wClass == null) {
						wClass = w.ClassName;
						wName = w.Name;
					}
					_Common(false, b, w, wName, wClass);
				} else if(wcp == 2) { //control
					b.AppendLine("<Z #B0E0B0><+switch 1>Window<>    <b>Control<>    <+switch 3>Process<><>");
					if(c.IsAlive) {
						if(cClass == null) {
							cClass = c.ClassName;
							cName = c.Name;
							cText = c.ControlText;
							//cLabel = c.NameLabel;
							cAcc = c.NameAcc;
							cWF = c.NameWinForms;
							cId = c.ControlId;
						}
						_Common(true, b, c, cName, cClass);
					}
				} else { //program
					b.AppendLine("<Z #B0E0B0><+switch 1>Window<>    <+switch 2>Control<>    <b>Process<><>");
					g1:
					if(wProg == null) {
						wProg = w.ProgramName;
					}
					b.Append("<i>ProgramName<>:    ").AppendLine(wProg);
					b.Append("<i>ProgramPath<>:    ").AppendLine(w.ProgramPath);
					b.Append("<i>ProgramDescription<>:    ").AppendLine(w.ProgramDescription);
					int pid = w.ProcessId, tid = w.ThreadId;
					b.Append("<i>ProcessId<>:    ").AppendLine(pid.ToString());
					b.Append("<i>ThreadId<>:    ").AppendLine(tid.ToString());
					b.Append("<i>Is32Bit<>:    ").AppendLine(w.Is32Bit ? "true" : "false");
					using(var uac = AUac.OfProcess(pid)) {
						b.Append("<i><help articles/UAC>UAC<> IL, elevation<>:    ")
							.Append(uac.IntegrityLevel.ToString())
							.Append(", ").AppendLine(uac.Elevation.ToString());
					}

					//if control's process or thread is different...
					if(!c.Is0) {
						int pid2 = c.ProcessId;
						if(pid2 != pid && pid2 != 0) {
							b.AppendLine("\r\n<c red>Control is in other process:<>");
							w = c; wProg = null;
							goto g1;
						}
						int tid2 = c.ThreadId;
						if(tid2 != tid && tid2 != 0) {
							b.AppendLine("\r\n<c red>Control is in other thread:<>");
							b.Append("<i>ThreadId<>:    ").AppendLine(tid2.ToString());
						}
					}
				}

				return b.ToString();
			}

			void _Common(bool isCon, StringBuilder b, AWnd w, string name, string className)
			{
				string s, sh = w.Handle.ToString();
				b.Append("<i>Handle<>:    ").AppendLine(sh);
				b.Append("<i>ClassName<>:    ").AppendLine(className);
				if(!isCon || !name.NE()) b.Append("<i>Name<>:    ").AppendLine(name);
				if(isCon) {
					//if(!cLabel.NE()) b.Append("<i>NameLabel<>:    ").AppendLine(cLabel);
					if(!cAcc.NE()) b.Append("<i>NameAcc<>:    ").AppendLine(cAcc);
					if(!cWF.NE()) b.Append("<i>NameWinForms<>:    ").AppendLine(cWF);
					if(!cText.NE()) b.Append("<i>ControlText<>:    ").Append("<\a>").Append(cText.Escape(10000, true)).AppendLine("</\a>");
					b.Append("<i>ControlId<>:    ").AppendLine(cId.ToString());
					b.AppendFormat("<+rect {0}><i>RectInWindow<><>:    ", sh).AppendLine(w.RectInWindow.ToString());
				} else {
					var wo = w.OwnerWindow;
					if(!wo.Is0) b.AppendFormat("<+rect {0}><i>Owner<><>:    ", wo.Handle.ToString()).AppendLine(wo.ToString());
					b.AppendFormat("<+rect {0}><i>Rect<><>:    ", sh).AppendLine(w.Rect.ToString());
				}
				b.AppendFormat("<+rect {0} 1><i>ClientRect<><>:    ", sh).AppendLine(w.ClientRect.ToString());
				var style = w.Style;
				s = (style & (WS)0xffff0000).ToString();
				if(isCon) s = s.Replace("MINIMIZEBOX", "GROUP").Replace("MAXIMIZEBOX", "TABSTOP");
				uint style2 = ((uint)style) & 0xffff; //unknown styles of that class
				b.Append("<i>Style<>:  0x").Append(((uint)style).ToString("X8")).Append(" (").Append(s);
				if(style2 != 0) b.Append(", 0x").Append(style2.ToString("X4"));
				b.AppendLine(")");
				var estyle = w.ExStyle;
				b.Append("<i>ExStyle<>:  0x").Append(((uint)estyle).ToString("X8")).Append(" (").Append(estyle.ToString()).AppendLine(")");
				//b.Append("<i>Class style<>:  0x").AppendLine(((uint)AWnd.More.GetClassLong(w, Native.GCL.STYLE)).ToString("X8"));
				if(!isCon) {
					b.Append("<i>Is...<>:    ");
					_AppendIs(w.IsPopupWindow, "IsPopupWindow");
					_AppendIs(w.IsToolWindow, "IsToolWindow");
					_AppendIs(w.IsTopmost, "IsTopmost");
					_AppendIs(w.IsFullScreen, "IsFullScreen");
					_AppendIs(0 != w.IsUwpApp, "IsUwpApp");
					_AppendIs(w.IsWindows8MetroStyle, "IsWindows8MetroStyle");
					b.AppendLine();
				}
				b.Append("<i>Prop[\"...\"]<>:    "); bool isProp = false;
				foreach(var p in w.Prop.GetList()) {
					if(p.Key.Starts('#')) continue;
					if(!isProp) isProp = true; else b.Append(", ");
					b.Append(p.Key).Append(" = ").Append(p.Value.ToString());
				}
				b.AppendLine();

				void _AppendIs(bool yes, string prop)
				{
					if(b[^1] != ' ') b.Append(", ");
					if(!yes) b.Append('!');
					b.Append(prop);
				}
			}
		}

		void _FillWindowInfo(in _WinInfo f)
		{
			if(_wiWCP == 0) {
				_wiWCP = 1;
				_winInfo.ZTags.AddLinkTag("+switch", s => {
					_wiWCP = s.ToInt();
					_SetText(default);
				});
				_winInfo.ZTags.AddLinkTag("+rect", s => {
					var w = (AWnd)s.ToInt(0, out int e);
					int client = s.ToInt(e);
					var r = client == 1 ? w.ClientRectInScreen : w.Rect;
					TUtil.ShowOsdRect(r, limitToScreen: w.IsMaximized);
				});
			}
			_SetText(f);

			void _SetText(in _WinInfo wi)
			{
				var s1 = wi.Format(_wnd, _con, _wiWCP);
				_winInfo.Z.SetText(s1);
			}
		}
		int _wiWCP; //0 not inited, 1 window, 2 control, 3 program

		//Called by OnLoad.
		void _InitInfo()
		{
			_SetFormInfo(null);
			Action<SG.CellContext, string> infoDelegate = (sender, info) => _SetFormInfo(info);
			_grid.ZShowEditInfo += infoDelegate;
			_grid2.ZShowEditInfo += infoDelegate;

			_tree.Paint += (object sender, PaintEventArgs e) => {
				if(_tree.Model == null) {
					e.Graphics.Clear(this.BackColor); //like grids
					_OnPaintDrawBackText(sender, e, "Control tree.");
				}
			};

			_grid.Paint += (sender, e) => { if(_wnd.Is0 || _propError != null) _OnPaintDrawBackText(sender, e, _propError ?? "Window/control properties."); };
			_grid2.Paint += (sender, e) => { if(_wnd.Is0) _OnPaintDrawBackText(sender, e, "Other parameters and search settings."); };

			void _OnPaintDrawBackText(object sender, PaintEventArgs e, string text)
			{
				var c = sender as Control;
				TextRenderer.DrawText(e.Graphics, text, Font, c.ClientRectangle, Color.FromKnownColor(KnownColor.GrayText), TextFormatFlags.WordBreak);
			}

			_commonInfos = new CommonInfos(_info);

			_info.ZTags.AddLinkTag("+resetInfo", _ => _SetFormInfo(null));
		}

		string _propError;

		void _SetFormInfo(string info)
		{
			if(info == null) {
				info = c_infoForm;
			} else if(info.Ends('$')) {
				_commonInfos.SetTextWithWildexInfo(info.RemoveSuffix(1));
				return;
			}
			_info.Z.SetText(info);
		}

		const string c_infoForm =
@"Creates code to <help AWnd.Find>find window<> or <help AWnd.Child>control<>.
1. Move the mouse to a window or control. Press key <b>F3<>.
2. Click the Test button. It finds and shows the window/control and the search time.
3. If need, check/uncheck/edit some fields or select another window/control; click Test.
4. Click OK, it inserts C# code in editor. Or copy/paste.
5. In editor add code to use the window/control. If need, rename variables, delete duplicate AWnd.Find lines, replace part of window name with *, etc.";
		const string c_infoWait = @"Wait timeout, seconds.
If unchecked, does not wait. Else if 0 or empty, waits infinitely. Else waits max this time interval; on timeout returns default(AWnd) or throws exception, depending on the 'Exception...' checkbox.";
		const string c_infoAlsoW = @"<help>AWnd.Find<> <i>also<> lambda.
Can be multiline. For newline use Ctrl+Enter.";
		const string c_infoAlsoC = @"<help>AWnd.Child<> <i>also<> lambda.
Can be multiline. For newline use Ctrl+Enter.";

		#endregion
	}
}
