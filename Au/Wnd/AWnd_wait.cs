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
//using System.Linq;

using Au.Types;

namespace Au
{
	public partial struct AWnd
	{
		/// <summary>
		/// Waits until window exists, is visible (optionally) and active (optionally).
		/// Returns window handle. On timeout returns default(AWnd) if <i>secondsTimeout</i> is negative; else exception.
		/// Parameters etc are the same as <see cref="Find"/>.
		/// </summary>
		/// <param name="secondsTimeout">Timeout, seconds. Can be 0 (infinite), &gt;0 (exception) or &lt;0 (no exception). More info: [](xref:wait_timeout).</param>
		/// <param name="active">The window must be the active window (<see cref="Active"/>), and not minimized.</param>
		/// <exception cref="TimeoutException"><i>secondsTimeout</i> time has expired (if &gt; 0).</exception>
		/// <exception cref="Exception">Exceptions of <see cref="Find"/>.</exception>
		/// <remarks>
		/// By default ignores invisible and cloaked windows. Use flags if need.
		/// If you have a window's AWnd variable, to wait until it is active/visible/etc use <see cref="WaitForCondition"/> instead.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// AWnd w = AWnd.Wait(10, false, "* Notepad");
		/// AOutput.Write(w);
		/// ]]></code>
		/// Using in a Form/Control event handler.
		/// <code><![CDATA[
		/// var f = new Form();
		/// f.Click += async (_, _) =>
		///   {
		/// 	  AOutput.Write("waiting for Notepad...");
		/// 	  AWnd w = await Task.Run(() => AWnd.Wait(-10, false, "* Notepad"));
		/// 	  if(w.Is0) AOutput.Write("timeout"); else AOutput.Write(w);
		///   };
		/// f.ShowDialog();
		/// ]]></code>
		/// </example>
		public static AWnd Wait(double secondsTimeout, bool active,
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
			[ParamString(PSFormat.AWildex)] string name = null,
			[ParamString(PSFormat.AWildex)] string cn = null,
			[ParamString(PSFormat.AWildex)] WOwner of = default,
			WFlags flags = 0, Func<AWnd, bool> also = null, WContains contains = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
		{
			var f = new Finder(name, cn, of, flags, also, contains);
			var to = new AWaitFor.Loop(secondsTimeout);
			for(; ; ) {
				if(active) {
					AWnd w = Active;
					if(f.IsMatch(w) && !w.IsMinimized) return w;
				} else {
					if(f.Find()) return f.Result;
				}
				if(!to.Sleep()) return default;
			}
		}
		//SHOULDDO: if wait for active, also wait until released mouse buttons.

		/// <summary>
		/// Waits until any of specified windows exists, is visible (optionally) and active (optionally).
		/// Returns 1-based index and window handle. On timeout returns <c>(0, default(AWnd))</c> if <i>secondsTimeout</i> is negative; else exception.
		/// </summary>
		/// <param name="secondsTimeout">Timeout, seconds. Can be 0 (infinite), &gt;0 (exception) or &lt;0 (no exception). More info: [](xref:wait_timeout).</param>
		/// <param name="active">The window must be the active window (<see cref="Active"/>), and not minimized.</param>
		/// <param name="windows">One or more variables containing window properties. Can be strings, see <see cref="Finder.op_Implicit(string)"/>.</param>
		/// <exception cref="TimeoutException"><i>secondsTimeout</i> time has expired (if &gt; 0).</exception>
		/// <remarks>
		/// By default ignores invisible and cloaked windows. Use finder flags if need.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// var (i, w) = AWnd.WaitAny(10, true, "* Notepad", new AWnd.Finder("* Word"));
		/// AOutput.Write(i, w);
		/// ]]></code>
		/// </example>
		public static (int index, AWnd wnd) WaitAny(double secondsTimeout, bool active, params Finder[] windows)
		{
			foreach(var f in windows) f.Result = default;
			WFCache cache = active && windows.Length > 1 ? new WFCache() : null;
			var to = new AWaitFor.Loop(secondsTimeout);
			for(; ; ) {
				if(active) {
					AWnd w = Active;
					for(int i = 0; i < windows.Length; i++) {
						if(windows[i].IsMatch(w, cache) && !w.IsMinimized) return (i + 1, w);
					}
				} else {
					for(int i = 0; i < windows.Length; i++) {
						var f = windows[i];
						if(f.Find()) return (i + 1, f.Result);
					}
					//FUTURE: optimization: get list of windows once (Lib.EnumWindows2).
					//	Problem: list filtering depends on Finder flags. Even if all finders have same flags, its easy to make bugs.
				}
				if(!to.Sleep()) return default;
			}
		}

		//rejected. Not useful. Use the non-static WaitForClosed.
		//		/// <summary>
		//		/// Waits until window does not exist.
		//		/// Parameters etc are the same as <see cref="Find"/>.
		//		/// </summary>
		//		/// <param name="secondsTimeout">Timeout, seconds. Can be 0 (infinite), &gt;0 (exception) or &lt;0 (no exception). More info: [](xref:wait_timeout).</param>
		//		/// <returns>Returns true. On timeout returns false if <i>secondsTimeout</i> is negative; else exception.</returns>
		//		/// <exception cref="TimeoutException"><i>secondsTimeout</i> time has expired (if &gt; 0).</exception>
		//		/// <exception cref="Exception">Exceptions of <see cref="Find"/>.</exception>
		//		/// <remarks>
		//		/// By default ignores invisible and cloaked windows. Use flags if need.
		//		/// If you have a window's AWnd variable, to wait until it is closed use <see cref="WaitForClosed"/> instead.
		//		/// Examples: <see cref="Wait"/>.
		//		/// </remarks>
		//		public static bool WaitNot(double secondsTimeout,
		//#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
		//			[ParamString(PSFormat.AWildex)] string name = null,
		//			[ParamString(PSFormat.AWildex)] string cn = null,
		//			[ParamString(PSFormat.AWildex)] WOwner of = default,
		//			WFlags flags = 0, Func<AWnd, bool> also = null, WContents contains = default)
		//#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
		//		{
		//			var f = new Finder(name, cn, of, flags, also, contains);
		//			return WaitNot(secondsTimeout, out _, f);
		//		}

		//		/// <summary>
		//		/// Waits until window does not exist.
		//		/// </summary>
		//		/// <param name="secondsTimeout"></param>
		//		/// <param name="wFound">On timeout receives the first found matching window that exists.</param>
		//		/// <param name="f">Window properties etc. Can be string, see <see cref="Finder.op_Implicit(string)"/>.</param>
		//		/// <exception cref="TimeoutException"><i>secondsTimeout</i> time has expired (if &gt; 0).</exception>
		//		public static bool WaitNot(double secondsTimeout, out AWnd wFound, Finder f)
		//		{
		//			wFound = default;
		//			var to = new AWaitFor.Loop(secondsTimeout);
		//			AWnd w = default;
		//			for(; ; ) {
		//				if(!w.IsAlive || !f.IsMatch(w)) { //if first time, or closed (!IsAlive), or changed properties (!IsMatch)
		//					if(!f.Find()) { wFound = default; return true; }
		//					wFound = w = f.Result;
		//				}
		//				if(!to.Sleep()) return false;
		//			}
		//		}

		//rejected. Cannot use implicit conversion string to Finder.
		//public static bool WaitNot(double secondsTimeout, Finder f)
		//	=> WaitNot(secondsTimeout, out _, f);

		//Not often used. It's easy with await Task.Run. Anyway, need to provide an example of similar size.
		//public static async Task<AWnd> WaitAsync(double secondsTimeout, string name)
		//{
		//	return await Task.Run(() => Wait(secondsTimeout, name));
		//}

		/// <summary>
		/// Waits for a user-defined state/condition of this window. For example active, visible, enabled, closed, contains control.
		/// </summary>
		/// <param name="secondsTimeout">Timeout, seconds. Can be 0 (infinite), &gt;0 (exception) or &lt;0 (no exception). More info: [](xref:wait_timeout).</param>
		/// <param name="condition">Callback function (eg lambda). It is called repeatedly, until returns true.</param>
		/// <param name="dontThrowIfClosed">
		/// Do not throw exception when the window handle is invalid or the window was closed while waiting.
		/// In such case the callback function must return false, like in the examples with <see cref="IsAlive"/>. Else exception is thrown (with a small delay) to prevent infinite waiting.
		/// </param>
		/// <returns>Returns true. On timeout returns false if <i>secondsTimeout</i> is negative; else exception.</returns>
		/// <exception cref="TimeoutException"><i>secondsTimeout</i> time has expired (if &gt; 0).</exception>
		/// <exception cref="AuWndException">The window handle is invalid or the window was closed while waiting.</exception>
		/// <example>
		/// <code><![CDATA[
		/// AWnd w = AWnd.Find("* Notepad");
		/// 
		/// //wait max 30 s until window w is active. Exception on timeout or if closed.
		/// w.WaitForCondition(30, t => t.IsActive);
		/// AOutput.Write("active");
		/// 
		/// //wait max 30 s until window w is enabled. Exception on timeout or if closed.
		/// w.WaitForCondition(30, t => t.IsEnabled);
		/// AOutput.Write("enabled");
		/// 
		/// //wait until window w is closed
		/// w.WaitForCondition(0, t => !t.IsAlive, true); //same as w.WaitForClosed()
		/// AOutput.Write("closed");
		/// 
		/// //wait until window w is minimized or closed
		/// w.WaitForCondition(0, t => t.IsMinimized || !t.IsAlive, true);
		/// if(!w.IsAlive) { AOutput.Write("closed"); return; }
		/// AOutput.Write("minimized");
		/// 
		/// //wait until window w contains focused control classnamed "Edit"
		/// var c = new AWnd.ChildFinder(cn: "Edit");
		/// w.WaitForCondition(10, t => c.Find(t) && c.Result.IsFocused);
		/// AOutput.Write("control focused");
		/// ]]></code>
		/// </example>
		public bool WaitForCondition(double secondsTimeout, Func<AWnd, bool> condition, bool dontThrowIfClosed = false)
		{
			bool wasInvalid = false;
			var to = new AWaitFor.Loop(secondsTimeout);
			for(; ; ) {
				if(!dontThrowIfClosed) ThrowIfInvalid();
				if(condition(this)) return true;
				if(dontThrowIfClosed) {
					if(wasInvalid) ThrowIfInvalid();
					wasInvalid = !IsAlive;
				}
				if(!to.Sleep()) return false;
			}
		}

		/// <summary>
		/// Waits until this window has the specified name.
		/// </summary>
		/// <param name="secondsTimeout">Timeout, seconds. Can be 0 (infinite), &gt;0 (exception) or &lt;0 (no exception). More info: [](xref:wait_timeout).</param>
		/// <param name="name">
		/// Window name. Usually it is the title bar text.
		/// String format: [](xref:wildcard_expression).
		/// </param>
		/// <returns>Returns true. On timeout returns false if <i>secondsTimeout</i> is negative; else exception.</returns>
		/// <exception cref="TimeoutException"><i>secondsTimeout</i> time has expired (if &gt; 0).</exception>
		/// <exception cref="AuWndException">The window handle is invalid or the window was closed while waiting.</exception>
		/// <exception cref="ArgumentException">Invalid wildcard expression.</exception>
		public bool WaitForName(double secondsTimeout, [ParamString(PSFormat.AWildex)] string name)
		{
			AWildex x = name; //ArgumentNullException
			return WaitForCondition(secondsTimeout, t => x.Match(t.Name));
		}

		/// <summary>
		/// Waits until this window is closed/destroyed or until its process ends.
		/// </summary>
		/// <param name="secondsTimeout">Timeout, seconds. Can be 0 (infinite), &gt;0 (exception) or &lt;0 (no exception). More info: [](xref:wait_timeout).</param>
		/// <param name="waitUntilProcessEnds">Wait until the process of this window ends.</param>
		/// <returns>Returns true. On timeout returns false if <i>secondsTimeout</i> is negative; else exception.</returns>
		/// <exception cref="TimeoutException"><i>secondsTimeout</i> time has expired (if &gt; 0).</exception>
		/// <exception cref="AuException">Failed to open process handle when <i>waitUntilProcessEnds</i> is true.</exception>
		/// <remarks>
		/// If the window is already closed, immediately returns true.
		/// </remarks>
		public bool WaitForClosed(double secondsTimeout, bool waitUntilProcessEnds = false)
		{
			if(!waitUntilProcessEnds) return WaitForCondition(secondsTimeout, t => !t.IsAlive, true);

			//SHOULDDO: if window of this thread or process...

			if(!IsAlive) return true;
			using var ph = Handle_.OpenProcess(this, Api.SYNCHRONIZE);
			if(ph.Is0) {
				var e = new AuException(0, "*open process handle"); //info: with SYNCHRONIZE can open process of higher IL
				if(!IsAlive) return true;
				throw e;
			}
			return 0 != AWaitFor.Handle(secondsTimeout, AOpt.WaitFor.DoEvents ? WHFlags.DoEvents : 0, ph);
		}
	}
}
