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
using Microsoft.Win32;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using System.Drawing;
//using System.Linq;

using Au.Types;
using static Au.NoClass;

#pragma warning disable 282 //intellisense bug: it thinks that Wnd has multiple fields.

namespace Au
{
	public partial struct Wnd
	{
		/// <summary>
		/// Contains miscellaneous static window-related functions and classes, rarely used or useful only for programmers.
		/// </summary>
		public static partial class Misc
		{
			/// <summary>
			/// Calculates window rectangle from client area rectangle and style.
			/// Calls API <msdn>AdjustWindowRectEx</msdn>.
			/// </summary>
			/// <param name="r">Input - client area rectangle in screen. Output - window rectangle in screen.</param>
			/// <param name="style">Native.WS_ styles.</param>
			/// <param name="exStyle">Native.WS_EX_ styles.</param>
			/// <param name="hasMenu"></param>
			/// <remarks>
			/// Ignores styles WS_VSCROLL, WS_HSCROLL and wrapped menu bar.
			/// </remarks>
			public static bool WindowRectFromClientRect(ref RECT r, uint style, uint exStyle, bool hasMenu = false)
			{
				return Api.AdjustWindowRectEx(ref r, style, hasMenu, exStyle);
			}

			/// <summary>
			/// Calculates window border width from style.
			/// </summary>
			/// <param name="style">Native.WS_ styles.</param>
			/// <param name="exStyle">Native.WS_EX_ styles.</param>
			public static int BorderWidth(uint style, uint exStyle)
			{
				RECT r = default;
				Api.AdjustWindowRectEx(ref r, style, false, exStyle);
				return r.right;
			}

			/// <summary>
			/// Gets window border width.
			/// </summary>
			/// <param name="w"></param>
			public static int BorderWidth(Wnd w)
			{
				w.LibGetWindowInfo(out var x);
				return (int)x.cxWindowBorders;
			}

			/// <summary>
			/// Calls API <msdn>GetGUIThreadInfo</msdn>, which can get some GUI info, eg mouse capturing, menu mode, move/size mode, focus, caret.
			/// </summary>
			/// <param name="g">Variable that receives the info.</param>
			/// <param name="idThread">Thread id. If 0 - the foreground (active window) thread. See <see cref="ThreadId"/>.</param>
			public static bool GetGUIThreadInfo(out Native.GUITHREADINFO g, int idThread = 0)
			{
				g = new Native.GUITHREADINFO(); g.cbSize = Api.SizeOf(g);
				return Api.GetGUIThreadInfo(idThread, ref g);
			}

			//public void ShowAnimate(bool show)
			//{
			//	//Don't add Wnd function, because:
			//		//Rarely used.
			//		//Api.AnimateWindow() works only with windows of current thread.
			//		//Only programmers would need it, and they can call the API directly.
			//}

			/// <summary>
			/// Creates native/unmanaged window.
			/// Calls API <msdn>CreateWindowEx</msdn>.
			/// Later call <see cref="DestroyWindow"/> or <see cref="Close"/>.
			/// For style and exStyle you can use Native.WS_ constants.
			/// Usually don't need to specify hInstance.
			/// </summary>
			public static Wnd CreateWindow(string className, string name = null, uint style = 0, uint exStyle = 0, int x = 0, int y = 0, int width = 0, int height = 0, Wnd parent = default, LPARAM controlId = default, IntPtr hInstance = default, LPARAM param = default)
			{
				return Api.CreateWindowEx(exStyle, className, name, style, x, y, width, height, parent, controlId, hInstance, param);
			}

			/// <summary>
			/// Creates native/unmanaged window like <see cref="CreateWindow"/> and sets font.
			/// If customFontHandle not specified, sets the system UI font, usually it is Segoe UI, 9.
			/// Later call <see cref="DestroyWindow"/> or <see cref="Close"/>.
			/// </summary>
			public static Wnd CreateWindowAndSetFont(string className, string name = null, uint style = 0, uint exStyle = 0, int x = 0, int y = 0, int width = 0, int height = 0, Wnd parent = default, LPARAM controlId = default, IntPtr hInstance = default, LPARAM param = default, IntPtr customFontHandle = default)
			{
				var w = Api.CreateWindowEx(exStyle, className, name, style, x, y, width, height, parent, controlId, hInstance, param);
				if(!w.Is0) SetFontHandle(w, (customFontHandle == default) ? _msgBoxFont : customFontHandle);
				return w;
			}
			static Util.LibNativeFont _msgBoxFont = new Util.LibNativeFont(SystemFonts.MessageBoxFont.ToHfont());

			/// <summary>
			/// Creates native/unmanaged <msdn>message-only window</msdn>.
			/// Styles: WS_POPUP, WS_EX_NOACTIVATE.
			/// Later call <see cref="DestroyWindow"/> or <see cref="Close"/>.
			/// </summary>
			/// <param name="className">Window class name. Can be any existing class.</param>
			public static Wnd CreateMessageWindow(string className)
			{
				return CreateWindow(className, null, Native.WS_POPUP, Native.WS_EX_NOACTIVATE, parent: SpecHwnd.HWND_MESSAGE);
				//note: WS_EX_NOACTIVATE is important.
			}

			/// <summary>
			/// Destroys a native window of this thread.
			/// Calls API <msdn>DestroyWindow</msdn>.
			/// Returns false if failed. Supports <see cref="Native.GetError"/>.
			/// </summary>
			/// <seealso cref="Close"/>
			public static bool DestroyWindow(Wnd w)
			{
				return Api.DestroyWindow(w);
			}

			/// <summary>
			/// Gets window Windows Store app user model id, like "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App".
			/// Returns null if fails or if called on Windows 7.
			/// </summary>
			/// <param name="w"></param>
			/// <param name="prependShellAppsFolder">Prepend @"shell:AppsFolder\" (to run or get icon).</param>
			/// <param name="getExePathIfNotWinStoreApp">Get program path if it is not a Windows Store app.</param>
			/// <remarks>
			/// Windows Store app window class name can be "Windows.UI.Core.CoreWindow" or "ApplicationFrameWindow".
			/// </remarks>
			public static string GetWindowsStoreAppId(Wnd w, bool prependShellAppsFolder = false, bool getExePathIfNotWinStoreApp = false)
			{
				if(0 != _GetWindowsStoreAppId(w, out var R, prependShellAppsFolder, getExePathIfNotWinStoreApp)) return R;
				return null;
			}

			/// <summary>
			/// Sets native font handle.
			/// Sends message API <msdn>WM_SETFONT</msdn> with lParam 1.
			/// Does not copy the font; don't dispose it while the window is alive.
			/// Use this function only with windows of current process.
			/// </summary>
			public static void SetFontHandle(Wnd w, IntPtr fontHandle)
			{
				w.Send(Api.WM_SETFONT, fontHandle, 1);
			}

			/// <summary>
			/// Gets native font handle.
			/// Sends message API <msdn>WM_GETFONT</msdn>.
			/// Does not copy the font; don't need to dispose.
			/// Use this function only with windows of current process.
			/// </summary>
			public static IntPtr GetFontHandle(Wnd w)
			{
				return w.Send(Api.WM_GETFONT);
			}

			/// <summary>
			/// Sets native icon handle.
			/// Sends message API <msdn>WM_SETICON</msdn>.
			/// Does not copy the icon; don't dispose it while the window is alive.
			/// Use this function only with windows of current process.
			/// </summary>
			/// <seealso cref="Icons"/>
			public static void SetIconHandle(Wnd w, IntPtr iconHandle, bool size32 = false)
			{
				w.Send(Api.WM_SETICON, size32, iconHandle);
			}

			/// <summary>
			/// Gets icon that is displayed in window title bar and in its taskbar button.
			/// Returns icon handle if successful, else default(IntPtr). Later call <see cref="Icons.DestroyIconHandle"/> or <see cref="Icons.HandleToImage"/>.
			/// </summary>
			/// <param name="w"></param>
			/// <param name="size32">Get 32x32 icon. If false, gets 16x16 icon.</param>
			/// <remarks>
			/// Icon size depends on DPI (text size, can be changed in Control Panel). By default small is 16, large 32.
			/// This function can be used with windows of any process.
			/// </remarks>
			/// <seealso cref="Icons"/>
			public static IntPtr GetIconHandle(Wnd w, bool size32 = false)
			{
				int size = Api.GetSystemMetrics(size32 ? Api.SM_CXICON : Api.SM_CXSMICON);

				//support Windows Store apps
				if(1 == _GetWindowsStoreAppId(w, out var appId, true)) {
					IntPtr hi = Icons.GetFileIconHandle(appId, size);
					if(hi != default) return hi;
				}

				bool ok = w.SendTimeout(2000, out LPARAM R, Api.WM_GETICON, size32);
				if(R == 0 && ok) w.SendTimeout(2000, out R, Api.WM_GETICON, !size32);
				if(R == 0) R = GetClassLong(w, size32 ? Native.GCL_HICON : Native.GCL_HICONSM);
				if(R == 0) R = GetClassLong(w, size32 ? Native.GCL_HICONSM : Native.GCL_HICON);
				//tested this code with DPI 125%. Small icon of most windows match DPI (20), some 16, some 24.

				//Copy, because will DestroyIcon, also it resizes if need.
				if(R != 0) return Api.CopyImage(R, Api.IMAGE_ICON, size, size, 0);
				return default;
			}

			/// <summary>
			/// Calls API <msdn>GetClassLong</msdn> if current process is 32-bit, GetClassLongPtr if 64-bit.
			/// </summary>
			/// <remarks>
			/// Supports <see cref="Native.GetError"/>.
			/// All Native.GCL_/GCW_ values are the same in 32-bit and 64-bit process.
			/// </remarks>
			public static LPARAM GetClassLong(Wnd w, int index)
			{
				LPARAM R;
				if(IntPtr.Size == 8) R = Api.GetClassLong64(w, index); else R = Api.GetClassLong32(w, index);
				return R;
			}

			//Not useful. Dangerous.
			///// <summary>
			///// Calls API SetClassLong if this process is 32-bit, SetClassLongPtr if 64-bit.
			///// </summary>
			///// <remarks>
			///// All Native.GCL_ values are the same in 32-bit and 64-bit process.
			///// </remarks>
			///// <exception cref="WndException"/>
			//public static LPARAM SetClassLong(Wnd w, int index, LPARAM newValue)
			//{
			//	Native.ClearError();
			//	LPARAM R;
			//	if(IntPtr.Size == 8) R = Api.SetClassLong64(w, index, newValue); else R = Api.SetClassLong32(w, index, newValue);
			//	if(R == 0 && Native.GetError() != 0) ThrowUseNative();
			//	return R;
			//}

			/// <summary>
			/// Gets atom of a window class.
			/// To get class atom when you have a window w, use <c>Wnd.Misc.GetClassLong(w, Native.GCW_ATOM)</c>.
			/// </summary>
			/// <param name="className">Class name.</param>
			/// <param name="moduleHandle">Native module handle of the exe or dll that registered the class. Don't use if it is a global class (CS_GLOBALCLASS style).</param>
			public static ushort GetClassAtom(string className, IntPtr moduleHandle = default)
			{
				var x = new Api.WNDCLASSEX();
				x.cbSize = Api.SizeOf(x);
				return Api.GetClassInfoEx(moduleHandle, className, ref x);
			}

			/// <summary>
			/// Calls API <msdn>ChangeWindowMessageFilter</msdn> for each message in the list of messages.
			/// It allows processes of lower UAC integrity level to send these messages to this process.
			/// </summary>
			public static void UacEnableMessages(params uint[] messages)
			{
				foreach(var m in messages) Api.ChangeWindowMessageFilter(m, 1);
			}

			/// <summary>
			/// Calls API <msdn>ChangeWindowMessageFilter</msdn> to enable receiving WM_COPYDATA message from lower UAC integrity level processes, for example if you'll use <see cref="InterProcessGetData"/>.
			/// Call this once in process.
			/// </summary>
			public static void InterProcessEnableReceivingWM_COPYDATA()
			{
				Api.ChangeWindowMessageFilter(Api.WM_COPYDATA, 1);
			}

			/// <summary>
			/// Sends data to a window of another process using message <msdn>WM_COPYDATA</msdn>.
			/// Returns true if the window received the message and returned true from its window procedure.
			/// See also: <see cref="InterProcessGetData"/>.
			/// </summary>
			/// <param name="w">The window of that process that will receive the message.</param>
			/// <param name="stringId">An integer identifier of the string, to store in COPYDATASTRUCT.dwData.</param>
			/// <param name="s">String containing data of any format. Can have '\0' characters.</param>
			/// <param name="wSender">Optional. A window of this process that sends the message. The receiving window procedure receives it in wParam.</param>
			/// <seealso cref="Util.SharedMemory"/>
			public static unsafe bool InterProcessSendData(Wnd w, int stringId, string s, Wnd wSender = default)
			{
				var c = new Api.COPYDATASTRUCT() { dwData = stringId, cbData = s.Length * 2 };
				fixed (char* p = s) {
					c.lpData = (IntPtr)p;
					return w.Send(Api.WM_COPYDATA, wSender.Handle, &c);
				}
			}

			/// <summary>
			/// Gets data stored in <msdn>COPYDATASTRUCT</msdn> structure received by a window procedure with <msdn>WM_COPYDATA</msdn> message.
			/// See also: <see cref="InterProcessSendData"/>, <see cref="InterProcessEnableReceivingWM_COPYDATA"/>.
			/// </summary>
			/// <param name="lParam">lParam of the window procedure when it received WM_COPYDATA message. It is COPYDATASTRUCT pointer.</param>
			/// <param name="stringId">Receives string id stored in COPYDATASTRUCT.dwData.</param>
			/// <remarks>
			/// <note type="note">UAC blocks messages sent from processes of lower integrity level. Call <see cref="InterProcessEnableReceivingWM_COPYDATA"/> before (once).</note>
			/// </remarks>
			public static unsafe string InterProcessGetData(LPARAM lParam, out int stringId)
			{
				var c = (Api.COPYDATASTRUCT*)lParam;
				stringId = c->dwData;
				return Marshal.PtrToStringUni(c->lpData, c->cbData / 2);
			}

			/// <summary>
			/// Removes '&amp;' characters from string.
			/// Replaces "&amp;&amp;" to "&amp;".
			/// Returns new string if s has '&amp;' characters, else returns s.
			/// </summary>
			/// <remarks>
			/// Character '&amp;' is used to underline next character in displayed text of dialog controls and menu items. Two '&amp;' are used to display single '&amp;'.
			/// The underline is displayed when using the keyboard (eg Alt key) to select dialog controls and menu items.
			/// </remarks>
			public static string StringRemoveUnderlineAmpersand(string s)
			{
				if(!Empty(s)) {
					for(int i = 0; i < s.Length; i++) if(s[i] == '&') goto g1;
					return s;
					g1:
					var b = Util.Buffers.LibChar(s.Length);
					int j = 0;
					for(int i = 0; i < s.Length; i++) {
						if(s[i] == '&') {
							if(i < s.Length - 1 && s[i + 1] == '&') i++;
							else continue;
						}
						b.A[j++] = s[i];
					}
					s = b.LibToStringCached(j);
				}
				return s;
			}

			/// <summary>
			/// Writes a Windows message to the output.
			/// </summary>
			/// <param name="m"></param>
			/// <param name="ignore">Messages to not show.</param>
			public static void PrintMsg(ref Message m, params uint[] ignore)
			{
				uint msg = (uint)m.Msg;
				if(ignore != null) foreach(uint t in ignore) { if(t == msg) return; }

				Wnd w = (Wnd)m.HWnd;
				uint counter = w.Prop["PrintMsg"]; w.Prop.Set("PrintMsg", ++counter);
				Print(counter.ToString(), m.ToString());
			}

			/// <summary>
			/// Writes a Windows message to the output.
			/// </summary>
			/// <param name="w"></param>
			/// <param name="msg"></param>
			/// <param name="wParam"></param>
			/// <param name="lParam"></param>
			/// <param name="ignore">Messages to not show.</param>
			public static void PrintMsg(Wnd w, uint msg, LPARAM wParam, LPARAM lParam, params uint[] ignore)
			{
				if(ignore != null) foreach(uint t in ignore) { if(t == msg) return; }
				var m = Message.Create(w.Handle, (int)msg, wParam, lParam);
				PrintMsg(ref m);
			}

			/// <summary>
			/// Writes a Windows message to the output.
			/// </summary>
			/// <param name="m"></param>
			/// <param name="ignore">Messages to not show.</param>
			public static void PrintMsg(in Native.MSG m, params uint[] ignore)
			{
				PrintMsg(m.hwnd, m.message, m.wParam, m.lParam, ignore);
			}
			//TODO: in whole library replace ref with in where need.

			/// <summary>API <msdn>SetWindowSubclass</msdn></summary>
			[DllImport("comctl32.dll", EntryPoint = "#410")]
			public static extern bool SetWindowSubclass(Wnd hWnd, SUBCLASSPROC pfnSubclass, LPARAM uIdSubclass, IntPtr dwRefData);

			/// <summary>API <msdn>GetWindowSubclass</msdn></summary>
			[DllImport("comctl32.dll", EntryPoint = "#411")] //this is exported only by ordinal
			public static extern bool GetWindowSubclass(Wnd hWnd, SUBCLASSPROC pfnSubclass, LPARAM uIdSubclass, out IntPtr pdwRefData);

			/// <summary>API <msdn>RemoveWindowSubclass</msdn></summary>
			[DllImport("comctl32.dll", EntryPoint = "#412")]
			public static extern bool RemoveWindowSubclass(Wnd hWnd, SUBCLASSPROC pfnSubclass, LPARAM uIdSubclass);

			/// <summary>API <msdn>DefSubclassProc</msdn></summary>
			[DllImport("comctl32.dll", EntryPoint = "#413")]
			public static extern LPARAM DefSubclassProc(Wnd hWnd, uint uMsg, LPARAM wParam, LPARAM lParam);

			/// <summary>API <msdn>DefWindowProc</msdn></summary>
			[DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
			public static extern LPARAM DefWindowProc(Wnd hWnd, uint Msg, LPARAM wParam, LPARAM lParam);

		}

		/// <summary>
		/// Returns true if w contains a non-zero special handle value (see <see cref="SpecHwnd"/>).
		/// Note that HWND_TOP is 0.
		/// </summary>
		public static bool IsSpecHwnd(Wnd w)
		{
			int i = (LPARAM)w;
			return (i <= 1 && i >= -3) || i == 0xffff;
		}
	}
}

namespace Au.Types
{
	/// <summary>
	/// Contains static properties that return special window handle values that can be used with some API functions and with some Wnd functions too.
	/// </summary>
	public class SpecHwnd
	{
		/// <summary>API HWND_TOP (0). Used with SetWindowPos.</summary>
		public static Wnd HWND_TOP => (Wnd)(LPARAM)0;
		/// <summary>API HWND_BOTTOM (1). Used with SetWindowPos.</summary>
		public static Wnd HWND_BOTTOM => (Wnd)(LPARAM)1;
		/// <summary>API HWND_TOPMOST (-1). Used with SetWindowPos.</summary>
		public static Wnd HWND_TOPMOST => (Wnd)(LPARAM)(-1);
		/// <summary>API HWND_NOTOPMOST (-2). Used with SetWindowPos.</summary>
		public static Wnd HWND_NOTOPMOST => (Wnd)(LPARAM)(-2);
		/// <summary>API HWND_MESSAGE (-3). Used with API CreateWindowEx, Wnd.WndOwner etc.</summary>
		public static Wnd HWND_MESSAGE => (Wnd)(LPARAM)(-3);
		/// <summary>API HWND_BROADCAST (0xffff). Used with API SendMessage, Wnd.Send etc.</summary>
		public static Wnd HWND_BROADCAST => (Wnd)(LPARAM)0xffff;
	}
	//TODO: move to Native

	/// <summary>API <msdn>SUBCLASSPROC</msdn></summary>
	public delegate LPARAM SUBCLASSPROC(Wnd hWnd, uint msg, LPARAM wParam, LPARAM lParam, LPARAM uIdSubclass, IntPtr dwRefData);
}