using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
//using System.Xml.Linq;

using Au;
using Au.Types;
using static Au.NoClass;

namespace Au.Tools
{
	internal static class TUtil
	{
#region text

		/// <summary>
		/// Appends ', ' and string argument to this StringBuilder.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="s">Argument value. If null, appends 'null'. If verbatim (like '@"text"'), appends 's'. Else appends '"escaped s"'.</param>
		/// <param name="param">If not null, appends 'param: s'. By default appends only 's'. If "null", appends 'null, s'.</param>
		/// <param name="noComma">Don't append ', '. Use for the first parameter. If false, does not append only if b.Length is less than 2.</param>
		internal static StringBuilder AppendStringArg(this StringBuilder t, string s, string param = null, bool noComma = false)
		{
			_AppendArgPrefix(t, param, noComma);
			if(s == null) t.Append("null");
			else if(IsVerbatim(s, out _)) t.Append(s);
			else t.Append('\"').Append(s.Escape()).Append('\"'); //FUTURE: make verbatim if contains \ and no newlines/tabs/etc
			return t;
		}

		/// <summary>
		/// Appends ', ' and non-string argument to this StringBuilder.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="s">Argument value. Must not be empty.</param>
		/// <param name="param">If not null, appends 'param: s'. By default appends only 's'. If "null", appends 'null, s'.</param>
		/// <param name="noComma">Don't append ', '. Use for the first parameter. If false, does not append only if b.Length is less than 2.</param>
		internal static StringBuilder AppendOtherArg(this StringBuilder t, string s, string param = null, bool noComma = false)
		{
			Debug.Assert(!Empty(s));
			_AppendArgPrefix(t, param, noComma);
			t.Append(s);
			return t;
		}

		static void _AppendArgPrefix(StringBuilder t, string param, bool noComma)
		{
			if(!noComma && t.Length > 1) t.Append(", ");
			if(param != null) t.Append(param).Append(param == "null" ? ", " : ": ");
		}

		/// <summary>
		/// If in grid are checked some flags, appends ', ' and the checked flags.
		/// Returns true if checked.
		/// Grid row keys must be enum member names.
		/// </summary>
		/// <param name="t"></param>
		/// <param name="flagsEnum">The type of the flags enum.</param>
		/// <param name="grid"></param>
		/// <param name="param">If not null, appends 'param: flags'. By default appends only 'flags'. If "null", appends 'null, s'.</param>
		/// <param name="prefix">Row keys of these flags in grid have this prefix. Use when the grid contains flags of another enum with same member names.</param>
		internal static bool AppendFlagsFromGrid(this StringBuilder t, Type flagsEnum, Controls.ParamGrid grid, string param = null, string prefix = null)
		{
			bool isFlags = false;
			string[] flagNames = flagsEnum.GetEnumNames();
			for(int r = 0, n = grid.RowsCount; r < n; r++) {
				var key = grid.ZGetRowKey(r); if(Empty(key)) continue;
				if(prefix != null) { if(key.Starts(prefix)) key = key.Substring(prefix.Length); else continue; }
				if(!flagNames.Contains(key)) continue;
				if(!grid.ZIsChecked(r)) continue;
				var flag = flagsEnum.Name + "." + key;
				if(!isFlags) {
					isFlags = true;
					AppendOtherArg(t, flag, param);
				} else {
					t.Append('|').Append(flag);
				}
			}
			return isFlags;
		}

		/// <summary>
		/// Appends waitTime. If !orThrow, appends "-" if need.
		/// </summary>
		internal static StringBuilder AppendWaitTime(this StringBuilder t, string waitTime, bool orThrow)
		{
			if(Empty(waitTime)) waitTime = "0"; else if(!orThrow && waitTime != "0" && !waitTime.Starts('-')) t.Append('-');
			t.Append(waitTime);
			return t;
		}

		/// <summary>
		/// Returns true if s is like '@"*"' or '$"*"' or '$@"*"'.
		/// s can be null.
		/// </summary>
		internal static bool IsVerbatim(string s, out int prefixLength)
		{
			prefixLength = 0;
			if(s != null && s.Length >= 3 && s[s.Length - 1] == '\"') {
				if(s[0] == '$') prefixLength = 1;
				if(s[prefixLength] == '@') prefixLength++;
				if(s[prefixLength] == '\"' && prefixLength != s.Length - 1) return true;
				prefixLength = 0;
			}
			return false;
		}

		/// <summary>
		/// If s has *? characters, prepends "**t ".
		/// s can be null.
		/// </summary>
		internal static string EscapeWildex(string s)
		{
			if(Wildex.HasWildcardChars(s)) s = "**t " + s;
			return s;
		}

		/// <summary>
		/// If s has *? characters, prepends "**t ".
		/// But if s has single * character, converts to "**r regexp" that ignores it. Because single * often is used to indicate unsaved state.
		/// If canMakeVerbatim and makes regex or s contains '\' and no newlines/controlchars, prepends @" and appends " and replaces all " with "".
		/// s can be null.
		/// </summary>
		internal static string EscapeWindowName(string s, bool canMakeVerbatim)
		{
			if(s == null) return s;
			if(Wildex.HasWildcardChars(s)) {
				int i = s.IndexOf('*');
				if(i >= 0 && s.IndexOf('*', i + 1) < 0) {
					s = "**r " + Regex_.EscapeQE(s.Remove(i)) + @"\*?" + Regex_.EscapeQE(s.Substring(i + 1));
				} else s = "**t " + s;
			}
			if(canMakeVerbatim && s.IndexOf('\\') >= 0 && !s.RegexIsMatch(@"[\x00-\x1F\x85\x{2028}\x{2029}]")) {
				s = "@\"" + s.Replace("\"", "\"\"") + "\"";
			}
			return s;
		}

		/// <summary>
		/// Returns true if newRawValue does not match wildex gridValue, unless contains is like $"..." or $@"...".
		/// </summary>
		/// <param name="gridValue">A wildex string, usually from a ParamGrid control cell. Can be raw or verbatim. Can be null.</param>
		/// <param name="newRawValue">New raw string, not wildex. Can be null.</param>
		internal static bool ShouldChangeGridWildex(string gridValue, string newRawValue)
		{
			if(gridValue == null) gridValue = "";
			if(newRawValue == null) newRawValue = "";
			if(IsVerbatim(gridValue, out _)) {
				if(gridValue[0] == '$') return false;
				gridValue = gridValue.Substring(2, gridValue.Length - 3).Replace("\"\"", "\"");
			}
			Wildex x = gridValue;
			return !x.Match(newRawValue);
		}

		/// <summary>
		/// Replaces known non-constant window class names with wildcard. Eg "WindowsForms10.EDIT..." with "*.EDIT.*".
		/// </summary>
		/// <param name="s">Can be null.</param>
		/// <param name="escapeWildex">If didn't replace, call <see cref="EscapeWildex"/>.</param>
		internal static string StripWndClassName(string s, bool escapeWildex)
		{
			if(!Empty(s)) {
				int n = s.RegexReplace(@"^WindowsForms\d+(\..+?\.).+", "*$1*", out s);
				if(n == 0) n = s.RegexReplace(@"^(HwndWrapper\[.+?;).+", "$1*", out s);
				if(escapeWildex && n == 0) s = EscapeWildex(s);
			}
			return s;
		}

#endregion

#region misc

		/// <summary>
		/// Gets control id. Returns true if it can be used to identify the control in window wWindow.
		/// </summary>
		internal static bool GetUsefulControlId(Wnd wControl, Wnd wWindow, out int id)
		{
			id = wControl.ControlId;
			if(id == 0 || id == -1 || id > 0xffff || id < -0xffff) return false;
			//info: some apps use negative ids, eg FileZilla. Usually >= -0xffff. Id -1 is often used for group buttons and separators.
			//if(id == (int)wControl) return false; //.NET forms, delphi. //possible coincidence //rejected, because window handles are > 0xffff
			Debug.Assert((int)wControl > 0xffff);

			//if(wWindow.Child("***id " + id) != wControl) return false; //problem with combobox child Edit that all have id 1001
			if(wWindow.ChildAll("***id " + id).Length != 1) return false; //note: searching only visible controls; else could find controls with same id in hidden pages of tabbed dialog.
			return true;
		}

#endregion

#region OnScreenRect

		/// <summary>
		/// Creates standard <see cref="OsdRect"/>.
		/// </summary>
		internal static OsdRect CreateOsdRect(int thickness = 4) => new OsdRect() { Color = 0xFF8A2BE2, Thickness = thickness }; //Color.BlueViolet

		/// <summary>
		/// Briefly shows standard blinking on-screen rectangle.
		/// </summary>
		internal static void ShowOsdRect(RECT r, bool limitToScreen = false)
		{
			var osr = CreateOsdRect();
			r.Inflate(2, 2); //2 pixels inside, 2 outside
			if(limitToScreen) {
				var k = Screen.FromRectangle(r).Bounds;
				r.Intersect(k);
			}
			osr.Rect = r;
			osr.Show();

			int i = 0;
			Timer_.Every(250, t =>
			{
				if(i++ < 5) {
					osr.Color = (i & 1) != 0 ? 0xFFFFFF00 : 0xFF8A2BE2;
				} else {
					t.Stop();
					osr.Dispose();
				}
			});
		}

#endregion

#region capture

		/// <summary>
		/// Common code for tools that capture UI objects with F3.
		/// </summary>
		internal class CaptureWindowEtcWithHotkey
		{
			Timer_ _timer;
			long _prevTime;
			OsdRect _osr;
			Form _form;
			CheckBox _captureCheckbox;
			Func<RECT?> _cbGetRect;
			const string c_propName = "Au.Capture";
			const int c_stopMessage = Api.WM_USER + 242;

			public bool Capturing { get; private set; }

			/// <param name="form">Tool form.</param>
			/// <param name="captureCheckbox">Checkbox that turns on/off capturing.</param>
			/// <param name="cbGetRect">Called to get rectangle of object from mouse. Can return default to hide rectangle.</param>
			public CaptureWindowEtcWithHotkey(Form form, CheckBox captureCheckbox, Func<RECT?> cbGetRect)
			{
				_form = form;
				_captureCheckbox = captureCheckbox;
				_cbGetRect = cbGetRect;
			}

			/// <summary>
			/// Starts or stops capturing.
			/// Does nothing if already in that state.
			/// </summary>
			public void StartStop(bool start)
			{
				if(start == Capturing) return;
				var wForm = (Wnd)_form;
				if(start) {
					//let other forms stop capturing
					wForm.Prop.Set(c_propName, 1);
					Wnd.Find(null, "WindowsForms*", also: o =>
					{
						if(o != wForm && o.Prop[c_propName] == 1) o.Send(c_stopMessage);
						return false;
					});

					if(!Api.RegisterHotKey(wForm, 1, 0, KKey.F3)) {
						AuDialog.ShowError("Failed to register hotkey F3", owner: _form);
						return;
					}
					Capturing = true;

					//set timer that shows AO rect
					if(_timer == null) {
						_osr = TUtil.CreateOsdRect();
						_timer = new Timer_(t =>
						{
							//Don't capture too frequently.
							//	Eg if the callback is very slow. Or if multiple timer messages are received without time interval (possible in some conditions).
							long t1 = Time.PerfMilliseconds, t2 = t1 - _prevTime; _prevTime = t1; if(t2 < 100) return;

							//show rect of UI object from mouse
							Wnd w = Wnd.FromMouse(WXYFlags.NeedWindow);
							RECT? r = default;
							if(!(w.Is0 || w == wForm || w.Owner == wForm)) {
								r = _cbGetRect();
							}
							if(r.HasValue) {
								var rr = r.GetValueOrDefault();
								rr.Inflate(2, 2); //2 pixels inside, 2 outside
								_osr.Rect = rr;
								_osr.Show();
							} else {
								_osr.Visible = false;
							}
						});
					}
					_timer.Start(250, false);
				} else {
					Capturing = false;
					Api.UnregisterHotKey(wForm, 1);
					wForm.Prop.Remove(c_propName);
					_timer.Stop();
					_osr.Hide();
				}
			}

			/// <summary>
			/// Must be called from WndProc of the tool form.
			/// If returns true, don't call base.WndProc.
			/// Whe hotkey pressed, sets capture=true and returns true.
			/// </summary>
			/// <param name="m"></param>
			public bool WndProc(ref Message m, out bool capture)
			{
				capture = false;
				switch(m.Msg) {
				case Api.WM_HOTKEY:
					if((int)m.WParam == 1) {
						capture = true; return true;
					}
					break;
				case c_stopMessage:
					_captureCheckbox.Checked = false;
					return true;
				}
				return false;
			}

			/// <summary>
			/// Must be called when closing the tool form.
			/// </summary>
			public void Dispose()
			{
				StartStop(false);
				_osr?.Dispose();
			}
		}

		#endregion

		#region test

		/// <summary>
		/// Executes test code that finds an object in window.
		/// Returns the found object and the speed.
		/// </summary>
		/// <param name="code">
		/// Must start with one or more lines that find window or control and set Wnd variable named wndVar. Can be any code.
		/// The last line must be a 'find object' function call. Example: <c>Acc.Find(...);</c>. Without 'var obj = ', without OrThrow, without Wait.
		/// </param>
		/// <param name="wndVar">Name of Wnd variable of the window or control in which to search.</param>
		/// <param name="wnd">Window or control in which to search.</param>
		/// <param name="bTest">The 'Test' button. This function disables it while executing code.</param>
		/// <param name="lSpeed">Label control that displays speed.</param>
		/// <param name="getRect">Callback function that returns object's rectangle in screen. Called when object has been found.</param>
		/// <remarks>
		/// The test code is executed in this thread. Else would get invalid Acc etc. If need, caller can use Task.Run.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// var r = await TUtil.RunTestFindObject(code, _wndVar, _wnd, _bTest, _lSpeed, o => (o as Acc).Rect);
		/// ]]></code>
		/// </example>
		internal static TestFindObjectResults RunTestFindObject(
			string code, string wndVar, Wnd wnd, Button bTest, Label lSpeed, Func<object, RECT> getRect, bool activateWindow = false)
		{
			if(Empty(code)) return default;
			Form form = lSpeed.FindForm();
			lSpeed.Text = "";

			//Print(code);
			//Perf.First();

			//FUTURE: #line
			var b = new StringBuilder();
			b.AppendLine(@"static object[] __TestFunc__() {");
			if(activateWindow) b.Append("((Wnd)").Append(wnd.Window.Handle).Append(").ActivateLL(); 200.ms(); ");
			b.AppendLine("var _p_ = Perf.StartNew();");
			var lines = code.SegSplitLines(true);
			int lastLine = lines.Length - 1;
			for(int i = 0; i < lastLine; i++) b.AppendLine(lines[i]);
			b.AppendLine("_p_.Next(); var _a_ =");
			b.AppendLine(lines[lastLine]);
			b.AppendLine($"_p_.Next(); return new object[] {{ _p_.ToArray(), _a_, {wndVar} }};");
			b.AppendLine("\r\n}");
			code = b.ToString(); //Print(code);

			(long[] speed, object obj, Wnd wnd) r = default;
			bool ok = false;
			try {
				bTest.Enabled = false;
				if(!Au.Compiler.Scripting.Compile(code, out var c, wrap: true, load: true)) {
					Dbg.Print("---- CODE ----\r\n" + code + "--------------");
					AuDialog.ShowError("Errors in code", c.errors, owner: form, flags: DFlags.OwnerCenter | DFlags.Wider/*, expandedText: code*/);
				} else {
					var rr=(object[])c.method.Invoke(null, null); //use array because fails to cast tuple, probably because in that assembly it is new type
					r = ((long[])rr[0], rr[1], (Wnd)rr[2]);
					ok = true;
				}

				//note: the script runs in this thread.
				//	Bad: blocks current UI thread. But maybe not so bad.
				//	Good: we get valid Acc result. Else it would be marshalled for a script thread.
			}
			catch(Exception e) {
				if(e is TargetInvocationException tie) e = tie.InnerException;
				string s1, s2;
				if(e is NotFoundException) { s1 = "Window not found"; s2 = "Tip: If changed window name, you can replace part of name\r\nwith * or use regexp like @\"**r .+ - Notepad\"."; } //info: throws only when window not found. This is to show time anyway when acc etc not found.
				else { s1 = e.GetType().Name; s2 = e.Message; }
				AuDialog.ShowError(s1, s2, owner: form, flags: DFlags.OwnerCenter);
			}
			finally {
				bTest.Enabled = true;
			}
			if(!ok) return default;

			//Perf.NW();
			//Print(r);

			double _SpeedMcsToMs(long tn) => Math.Round(tn / 1000d, tn < 1000 ? 2 : (tn < 10000 ? 1 : 0));
			double t0 = _SpeedMcsToMs(r.speed[0]), t1 = _SpeedMcsToMs(r.speed[1]); //times of Wnd.Find and Object.Find
			string sTime;
			if(lastLine == 1 && lines[0].Length == 6) sTime = t1.ToStringInvariant() + " ms"; //only Wnd.Find: "Wnd w;\r\nw = Wnd.Find(...);"
			else sTime = t0.ToStringInvariant() + " + " + t1.ToStringInvariant() + " ms";

			if(r.obj is Wnd w1 && w1.Is0) r.obj = null;
			if(r.obj != null) {
				lSpeed.ForeColor = Form.DefaultForeColor;
				lSpeed.Text = sTime;
				var re = getRect(r.obj);
				TUtil.ShowOsdRect(re);

				//if form or its visible owners cover the found object, temporarily activate object's window
				foreach(var ow in ((Wnd)form).Get.OwnersAndThis(true)) {
					if(re.IntersectsWith(ow.Rect)) {
						r.wnd.Window.ActivateLL();
						Time.SleepDoEvents(1500);
						break;
					}
				}
			} else {
				//AuDialog.ShowEx("Not found", owner: this, flags: DFlags.OwnerCenter, icon: DIcon.Info, secondsTimeout: 2);
				lSpeed.ForeColor = Color.Red;
				lSpeed.Text = "Not found,";
				Timer_.After(700, () => lSpeed.Text = sTime);
			}

			((Wnd)form).ActivateLL();

			if(r.wnd != wnd && !r.wnd.Is0) {
				AuDialog.ShowWarning("The code finds another " + (r.wnd.IsChild ? "control" : "window"),
				$"Need:  {wnd.ToString()}\r\n\r\nFound:  {r.wnd.ToString()}",
				owner: form, flags: DFlags.OwnerCenter | DFlags.Wider);
				TUtil.ShowOsdRect(r.wnd.Rect, true);
				return default;
			}
			return new TestFindObjectResults() { obj = r.obj, speed = r.speed[1] };
		}

		internal struct TestFindObjectResults
		{
			public object obj;
			public long speed;
		}

#endregion
	}

	//public static class Test
	//{
	//	public static void OsdRect()
	//	{
	//		RECT r = (500, 500, 30, 20);
	//		TUtil.ShowOsdRect(r);
	//		AuDialog.Show();
	//	}
	//}

	/// <summary>
	/// All tool forms of this library should inherit from this class and override its virtual functions.
	/// </summary>
	public class ToolForm :Form_
	{
		public virtual string ResultCode { get; protected set; }

	}
}
