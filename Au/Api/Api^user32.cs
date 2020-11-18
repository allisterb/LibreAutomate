using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace Au.Types
{
	static unsafe partial class Api
	{
		[DllImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
		internal static extern LPARAM SendMessage(AWnd hWnd, int msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
		internal static extern LPARAM SendMessageTimeout(AWnd hWnd, int Msg, LPARAM wParam, LPARAM lParam, Native.SMTO flags, int uTimeout, out LPARAM lpdwResult);

		[DllImport("user32.dll", EntryPoint = "SendNotifyMessageW", SetLastError = true)]
		internal static extern bool SendNotifyMessage(AWnd hWnd, int Msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
		internal static extern bool PostMessage(AWnd hWnd, int Msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", EntryPoint = "PostThreadMessageW")]
		internal static extern bool PostThreadMessage(int idThread, int Msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM GetWindowLongW(AWnd hWnd, int nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM GetWindowLongPtrW(AWnd hWnd, int nIndex);

		//info: 32-bit user32.dll does not have GetWindowLongPtrW etc. In C++, in x86 config it is defined as GetWindowLongW etc.
		internal static LPARAM GetWindowLongPtr(AWnd w, int nIndex)
			=> IntPtr.Size == 8 ? GetWindowLongPtrW(w, nIndex) : GetWindowLongW(w, nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM SetWindowLongW(AWnd hWnd, int nIndex, LPARAM dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM SetWindowLongPtrW(AWnd hWnd, int nIndex, LPARAM dwNewLong);

		internal static LPARAM SetWindowLongPtr(AWnd w, int nIndex, LPARAM dwNewLong)
			=> IntPtr.Size == 8 ? SetWindowLongPtrW(w, nIndex, dwNewLong) : SetWindowLongW(w, nIndex, dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM GetClassLongW(AWnd hWnd, int nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM GetClassLongPtrW(AWnd hWnd, int nIndex);

		internal static LPARAM GetClassLongPtr(AWnd w, int nIndex)
			=> IntPtr.Size == 8 ? GetClassLongPtrW(w, nIndex) : GetClassLongW(w, nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM SetClassLongW(AWnd hWnd, int nIndex, LPARAM dwNewLong);

		[DllImport("user32.dll", SetLastError = true)]
		static extern LPARAM SetClassLongPtrW(AWnd hWnd, int nIndex, LPARAM dwNewLong);

		internal static LPARAM SetClassLongPtr(AWnd w, int nIndex, LPARAM dwNewLong)
			=> IntPtr.Size == 8 ? SetClassLongPtrW(w, nIndex, dwNewLong) : SetClassLongW(w, nIndex, dwNewLong);

		[DllImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true)]
		internal static extern int GetClassName(AWnd hWnd, char* lpClassName, int nMaxCount);

		[DllImport("user32.dll", EntryPoint = "InternalGetWindowText", SetLastError = true)]
		internal static extern int InternalGetWindowText(AWnd hWnd, [Out] char[] pString, int cchMaxCount);

		[DllImport("user32.dll")]
		internal static extern bool IsWindow(AWnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowVisible(AWnd hWnd);

		internal const int SW_HIDE = 0;
		internal const int SW_SHOWNORMAL = 1;
		internal const int SW_SHOWMINIMIZED = 2;
		internal const int SW_SHOWMAXIMIZED = 3;
		//internal const int SW_SHOWNOACTIVATE = 4; //restores min/max window
		internal const int SW_SHOW = 5;
		internal const int SW_MINIMIZE = 6;
		internal const int SW_SHOWMINNOACTIVE = 7;
		internal const int SW_SHOWNA = 8;
		internal const int SW_RESTORE = 9;
		internal const int SW_SHOWDEFAULT = 10;
		internal const int SW_FORCEMINIMIZE = 11;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern void ShowWindow(AWnd hWnd, int SW_X);
		//note: the returns value does not say succeeded/failed.
		//	It is non-zero if was visible, 0 if was hidden.
		//	Declared void to avoid programming errors.

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowEnabled(AWnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern void EnableWindow(AWnd hWnd, bool bEnable);
		//note: the returns value does not say succeeded/failed.
		//	It is non-zero if was disabled, 0 if was enabled.
		//	Declared void to avoid programming errors.

		[DllImport("user32.dll", EntryPoint = "FindWindowW", SetLastError = true)]
		internal static extern AWnd FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", EntryPoint = "FindWindowExW", SetLastError = true)]
		internal static extern AWnd FindWindowEx(AWnd hWndParent, AWnd hWndChildAfter, string lpszClass, string lpszWindow);

		internal struct WNDCLASSEX
		{
			public int cbSize;
			public uint style;
			public IntPtr lpfnWndProc; //not WNDPROC to avoid auto-marshaling where don't need. Use Marshal.GetFunctionPointerForDelegate/GetDelegateForFunctionPointer.
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public nint hbrBackground;
#pragma warning disable 169
			IntPtr lpszMenuName;
#pragma warning restore 169
			public char* lpszClassName; //not string because CLR would call CoTaskMemFree
			public IntPtr hIconSm;

			public WNDCLASSEX(RWCEtc ex = null) : this() {
				cbSize = sizeof(WNDCLASSEX);
				if (ex == null) {
					hCursor = LoadCursor(default, MCursor.Arrow);
				} else {
					style = ex.style;
					cbClsExtra = ex.cbClsExtra;
					cbWndExtra = ex.cbWndExtra;
					//hInstance = ex.hInstance;
					hIcon = ex.hIcon;
					if (ex.hCursor != default) hCursor = ex.hCursor; else if (ex.mCursor != default) hCursor = LoadCursor(default, ex.mCursor);
					hbrBackground = ex.hbrBackground;
					hIconSm = ex.hIconSm;
				}
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern ushort RegisterClassEx(in WNDCLASSEX lpwcx);

		[DllImport("user32.dll", EntryPoint = "GetClassInfoExW", SetLastError = true)]
		internal static extern ushort GetClassInfoEx(IntPtr hInstance, string lpszClass, ref WNDCLASSEX lpwcx);

		[DllImport("user32.dll", EntryPoint = "UnregisterClassW", SetLastError = true)]
		internal static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

		[DllImport("user32.dll", EntryPoint = "UnregisterClassW", SetLastError = true)]
		internal static extern bool UnregisterClass(uint classAtom, IntPtr hInstance);

		[DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
		internal static extern AWnd CreateWindowEx(WS2 dwExStyle, string lpClassName, string lpWindowName, WS dwStyle, int x, int y, int nWidth, int nHeight, AWnd hWndParent, LPARAM hMenu, IntPtr hInstance, LPARAM lpParam);

		[DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
		internal static extern LPARAM DefWindowProc(AWnd hWnd, int msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
		internal static extern LPARAM CallWindowProc(LPARAM lpPrevWndFunc, AWnd hWnd, int Msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DestroyWindow(AWnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern void PostQuitMessage(int nExitCode);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetMessage(out Native.MSG lpMsg, AWnd hWnd = default, int wMsgFilterMin = 0, int wMsgFilterMax = 0);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool TranslateMessage(in Native.MSG lpMsg);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern LPARAM DispatchMessage(in Native.MSG lpmsg);

		internal const uint PM_NOREMOVE = 0x0;
		internal const uint PM_REMOVE = 0x1;
		internal const uint PM_NOYIELD = 0x2;
		internal const uint PM_QS_SENDMESSAGE = 0x400000;
		internal const uint PM_QS_POSTMESSAGE = 0x980000;
		internal const uint PM_QS_PAINT = 0x200000;
		internal const uint PM_QS_INPUT = 0x1C070000;

		[DllImport("user32.dll", EntryPoint = "PeekMessageW", SetLastError = true)]
		internal static extern bool PeekMessage(out Native.MSG lpMsg, AWnd hWnd, int wMsgFilterMin, int wMsgFilterMax, uint wRemoveMsg);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ReplyMessage(LPARAM lResult);

		internal const int GA_PARENT = 1;
		internal const int GA_ROOT = 2;
		internal const int GA_ROOTOWNER = 3;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd GetAncestor(AWnd hwnd, uint GA_X);

		[DllImport("user32.dll")]
		internal static extern AWnd GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetForegroundWindow(AWnd hWnd);

		internal const int ASFW_ANY = -1;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AllowSetForegroundWindow(int dwProcessId);

		internal const uint LSFW_LOCK = 1;
		internal const uint LSFW_UNLOCK = 2;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool LockSetForegroundWindow(uint LSFW_X);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd SetFocus(AWnd hWnd);

		[DllImport("user32.dll")]
		internal static extern AWnd GetFocus();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd SetActiveWindow(AWnd hWnd);

		[DllImport("user32.dll")]
		internal static extern AWnd GetActiveWindow();

		internal struct WINDOWPOS
		{
			public AWnd hwnd;
			public AWnd hwndInsertAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public Native.SWP flags;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetWindowPos(AWnd hWnd, AWnd hWndInsertAfter, int X, int Y, int cx, int cy, Native.SWP swpFlags);

		internal struct FLASHWINFO
		{
			public int cbSize;
			public AWnd hwnd;
			public uint dwFlags;
			public int uCount;
			public int dwTimeout;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool FlashWindowEx(ref FLASHWINFO pfwi);

		internal const int GW_OWNER = 4;
		internal const int GW_HWNDPREV = 3;
		internal const int GW_HWNDNEXT = 2;
		internal const int GW_HWNDLAST = 1;
		internal const int GW_HWNDFIRST = 0;
		internal const int GW_ENABLEDPOPUP = 6;
		internal const int GW_CHILD = 5;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd GetWindow(AWnd hWnd, int GW_X);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd GetTopWindow(AWnd hWnd);

		//rejected. Obsolete, confusing.
		//	Returns owner for top-level windows with WS_POPUP style.
		//	Returns 0 for child windows without WS_CHILD style, eg message-only windows and QM2 toolbar owners.
		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern AWnd GetParent(AWnd hWnd);

		[DllImport("user32.dll")]
		internal static extern AWnd GetDesktopWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd GetShellWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd GetLastActivePopup(AWnd hWnd);

		[DllImport("user32.dll")]
		internal static extern bool IntersectRect(out RECT lprcDst, in RECT lprcSrc1, in RECT lprcSrc2);

		[DllImport("user32.dll")]
		internal static extern bool UnionRect(out RECT lprcDst, in RECT lprcSrc1, in RECT lprcSrc2);

		/// <summary>
		/// GetPhysicalCursorPos.
		/// </summary>
		/// <remarks>
		/// Gets DPI physical cursor pos, ie always in pixels.
		/// The classic GetCursorPos API behavior is undefined. Sometimes physical, sometimes logical.
		/// Make sure the process is fully DPI-aware.
		/// </remarks>
		[DllImport("user32.dll", EntryPoint = "GetPhysicalCursorPos", SetLastError = true)]
		internal static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true)]
		internal static extern IntPtr LoadImage(IntPtr hInst, string name, int type, int cx, int cy, uint LR_X);
		[DllImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true)]
		internal static extern IntPtr LoadImage(IntPtr hInst, LPARAM resId, int type, int cx, int cy, uint LR_X);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr CopyImage(IntPtr h, int type, int cx, int cy, uint flags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DestroyCursor(IntPtr hCursor);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowRect(AWnd hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetClientRect(AWnd hWnd, out RECT lpRect);

		internal const uint WPF_SETMINPOSITION = 0x1;
		internal const uint WPF_RESTORETOMAXIMIZED = 0x2;
		internal const uint WPF_ASYNCWINDOWPLACEMENT = 0x4;

		internal struct WINDOWPLACEMENT
		{
			public int length;
			/// <summary> WPF_ </summary>//TODO: don't use prefix WPF
			public uint flags;
			public int showCmd;
			public POINT ptMinPosition;
			public POINT ptMaxPosition;
			public RECT rcNormalPosition;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowPlacement(AWnd hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetWindowPlacement(AWnd hWnd, in WINDOWPLACEMENT lpwndpl);

		internal struct WINDOWINFO
		{
			public int cbSize;
			public RECT rcWindow;
			public RECT rcClient;
			public uint dwStyle;
			public uint dwExStyle;
			public uint dwWindowStatus;
			public int cxWindowBorders;
			public int cyWindowBorders;
			public ushort atomWindowType;
			public ushort wCreatorVersion;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowInfo(AWnd hwnd, ref WINDOWINFO pwi);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsZoomed(AWnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsIconic(AWnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetWindowThreadProcessId(AWnd hWnd, out int lpdwProcessId);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowUnicode(AWnd hWnd);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);


		[DllImport("user32.dll", EntryPoint = "GetPropW", SetLastError = true)]
		internal static extern LPARAM GetProp(AWnd hWnd, string lpString);

		[DllImport("user32.dll", EntryPoint = "GetPropW", SetLastError = true)]
		//internal static extern LPARAM GetProp(AWnd hWnd, [MarshalAs(UnmanagedType.SysInt)] ushort atom); //exception, must be U2
		internal static extern LPARAM GetProp(AWnd hWnd, LPARAM atom);

		[DllImport("user32.dll", EntryPoint = "SetPropW", SetLastError = true)]
		internal static extern bool SetProp(AWnd hWnd, string lpString, LPARAM hData);

		[DllImport("user32.dll", EntryPoint = "SetPropW", SetLastError = true)]
		internal static extern bool SetProp(AWnd hWnd, LPARAM atom, LPARAM hData);

		[DllImport("user32.dll", EntryPoint = "RemovePropW", SetLastError = true)]
		internal static extern LPARAM RemoveProp(AWnd hWnd, string lpString);

		[DllImport("user32.dll", EntryPoint = "RemovePropW", SetLastError = true)]
		internal static extern LPARAM RemoveProp(AWnd hWnd, LPARAM atom);

		internal delegate bool PROPENUMPROCEX(AWnd hwnd, IntPtr lpszString, LPARAM hData, LPARAM dwData);

		[DllImport("user32.dll", EntryPoint = "EnumPropsExW", SetLastError = true)]
		internal static extern int EnumPropsEx(AWnd hWnd, PROPENUMPROCEX lpEnumFunc, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd GetDlgItem(AWnd hDlg, int nIDDlgItem);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetDlgCtrlID(AWnd hWnd);

		internal delegate int WNDENUMPROC(AWnd w, void* p);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, void* p = null);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EnumThreadWindows(int dwThreadId, WNDENUMPROC lpEnumFunc, void* p = null);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EnumChildWindows(AWnd hWndParent, WNDENUMPROC lpEnumFunc, void* p = null);

		[DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", SetLastError = true)]
		internal static extern int RegisterWindowMessage(string lpString);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsChild(AWnd hWndParent, AWnd hWnd);

		//rejected. As slow as GetAncestor(GA_PARENT).
		//[DllImport("user32.dll", SetLastError = true), Obsolete("Undocumented API")]
		//internal static extern bool IsTopLevelWindow(AWnd hWnd);

		[DllImport("user32.dll")]
		internal static extern IntPtr MonitorFromPoint(POINT pt, SODefault dwFlags);

		[DllImport("user32.dll")]
		internal static extern IntPtr MonitorFromRect(in RECT lprc, SODefault dwFlags);

		[DllImport("user32.dll")]
		internal static extern IntPtr MonitorFromWindow(AWnd hwnd, SODefault dwFlags);

		internal struct MONITORINFO
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;
		}

		[DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
		internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

		internal delegate bool MONITORENUMPROC(IntPtr hmon, IntPtr hdc, IntPtr r, LPARAM dwData);

		[DllImport("user32.dll")]
		internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData);

		#region GetSystemMetrics, SystemParametersInfo

		internal const int SM_YVIRTUALSCREEN = 77;
		internal const int SM_XVIRTUALSCREEN = 76;
		internal const int SM_TABLETPC = 86;
		internal const int SM_SWAPBUTTON = 23;
		internal const int SM_STARTER = 88;
		internal const int SM_SLOWMACHINE = 73;
		internal const int SM_SHUTTINGDOWN = 8192;
		internal const int SM_SHOWSOUNDS = 70;
		internal const int SM_SERVERR2 = 89;
		internal const int SM_SECURE = 44;
		internal const int SM_SAMEDISPLAYFORMAT = 81;
		internal const int SM_RESERVED4 = 27;
		internal const int SM_RESERVED3 = 26;
		internal const int SM_RESERVED2 = 25;
		internal const int SM_RESERVED1 = 24;
		internal const int SM_REMOTESESSION = 4096;
		internal const int SM_REMOTECONTROL = 8193;
		internal const int SM_PENWINDOWS = 41;
		internal const int SM_NETWORK = 63;
		internal const int SM_MOUSEWHEELPRESENT = 75;
		internal const int SM_MOUSEPRESENT = 19;
		internal const int SM_MIDEASTENABLED = 74;
		internal const int SM_MENUDROPALIGNMENT = 40;
		internal const int SM_MEDIACENTER = 87;
		internal const int SM_IMMENABLED = 82;
		internal const int SM_DEBUG = 22;
		internal const int SM_DBCSENABLED = 42;
		internal const int SM_CYVTHUMB = 9;
		internal const int SM_CYVSCROLL = 20;
		internal const int SM_CYVIRTUALSCREEN = 79;
		internal const int SM_CYSMSIZE = 53;
		internal const int SM_CYSMICON = 50;
		internal const int SM_CYSMCAPTION = 51;
		internal const int SM_CYSIZEFRAME = SM_CYFRAME;
		internal const int SM_CYSIZE = 31;
		internal const int SM_CYSCREEN = 1;
		internal const int SM_CYMINTRACK = 35;
		internal const int SM_CYMINSPACING = 48;
		internal const int SM_CYMINIMIZED = 58;
		internal const int SM_CYMIN = 29;
		internal const int SM_CYMENUSIZE = 55;
		internal const int SM_CYMENUCHECK = 72;
		internal const int SM_CYMENU = 15;
		internal const int SM_CYMAXTRACK = 60;
		internal const int SM_CYMAXIMIZED = 62;
		internal const int SM_CYKANJIWINDOW = 18;
		internal const int SM_CYICONSPACING = 39;
		internal const int SM_CYICON = 12;
		internal const int SM_CYHSCROLL = 3;
		internal const int SM_CYFULLSCREEN = 17;
		internal const int SM_CYFRAME = 33;
		internal const int SM_CYFOCUSBORDER = 84;
		internal const int SM_CYFIXEDFRAME = SM_CYDLGFRAME;
		internal const int SM_CYEDGE = 46;
		internal const int SM_CYDRAG = 69;
		internal const int SM_CYDOUBLECLK = 37;
		internal const int SM_CYDLGFRAME = 8;
		internal const int SM_CYCURSOR = 14;
		internal const int SM_CYCAPTION = 4;
		internal const int SM_CYBORDER = 6;
		internal const int SM_CXVSCROLL = 2;
		internal const int SM_CXVIRTUALSCREEN = 78;
		internal const int SM_CXSMSIZE = 52;
		internal const int SM_CXSMICON = 49;
		internal const int SM_CXSIZEFRAME = SM_CXFRAME;
		internal const int SM_CXSIZE = 30;
		internal const int SM_CXSCREEN = 0;
		internal const int SM_CXMINTRACK = 34;
		internal const int SM_CXMINSPACING = 47;
		internal const int SM_CXMINIMIZED = 57;
		internal const int SM_CXMIN = 28;
		internal const int SM_CXMENUSIZE = 54;
		internal const int SM_CXMENUCHECK = 71;
		internal const int SM_CXMAXTRACK = 59;
		internal const int SM_CXMAXIMIZED = 61;
		internal const int SM_CXICONSPACING = 38;
		internal const int SM_CXICON = 11;
		internal const int SM_CXHTHUMB = 10;
		internal const int SM_CXHSCROLL = 21;
		internal const int SM_CXFULLSCREEN = 16;
		internal const int SM_CXFRAME = 32;
		internal const int SM_CXFOCUSBORDER = 83;
		internal const int SM_CXFIXEDFRAME = SM_CXDLGFRAME;
		internal const int SM_CXEDGE = 45;
		internal const int SM_CXDRAG = 68;
		internal const int SM_CXDOUBLECLK = 36;
		internal const int SM_CXDLGFRAME = 7;
		internal const int SM_CXCURSOR = 13;
		internal const int SM_CXBORDER = 5;
		internal const int SM_CMOUSEBUTTONS = 43;
		internal const int SM_CMONITORS = 80;
		internal const int SM_CMETRICS = 90;
		internal const int SM_CLEANBOOT = 67;
		internal const int SM_CARETBLINKINGENABLED = 8194;
		internal const int SM_ARRANGE = 56;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetSystemMetrics(int nIndex);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetSystemMetricsForDpi(int nIndex, int dpi);

		internal const int SPI_SETWORKAREA = 47;
		internal const int SPI_SETWHEELSCROLLLINES = 105;
		internal const int SPI_SETUIEFFECTS = 4159;
		internal const int SPI_SETTOOLTIPFADE = 4121;
		internal const int SPI_SETTOOLTIPANIMATION = 4119;
		internal const int SPI_SETTOGGLEKEYS = 53;
		internal const int SPI_SETSTICKYKEYS = 59;
		internal const int SPI_SETSOUNDSENTRY = 65;
		internal const int SPI_SETSNAPTODEFBUTTON = 96;
		internal const int SPI_SETSHOWSOUNDS = 57;
		internal const int SPI_SETSHOWIMEUI = 111;
		internal const int SPI_SETSERIALKEYS = 63;
		internal const int SPI_SETSELECTIONFADE = 4117;
		internal const int SPI_SETSCREENSAVETIMEOUT = 15;
		internal const int SPI_SETSCREENSAVERRUNNING = 97;
		internal const int SPI_SETSCREENSAVEACTIVE = 17;
		internal const int SPI_SETSCREENREADER = 71;
		internal const int SPI_SETPOWEROFFTIMEOUT = 82;
		internal const int SPI_SETPOWEROFFACTIVE = 86;
		internal const int SPI_SETPENWINDOWS = 49;
		internal const int SPI_SETNONCLIENTMETRICS = 42;
		internal const int SPI_SETMOUSEVANISH = 4129;
		internal const int SPI_SETMOUSETRAILS = 93;
		internal const int SPI_SETMOUSESPEED = 113;
		internal const int SPI_SETMOUSESONAR = 4125;
		internal const int SPI_SETMOUSEKEYS = 55;
		internal const int SPI_SETMOUSEHOVERWIDTH = 99;
		internal const int SPI_SETMOUSEHOVERTIME = 103;
		internal const int SPI_SETMOUSEHOVERHEIGHT = 101;
		internal const int SPI_SETMOUSECLICKLOCKTIME = 8201;
		internal const int SPI_SETMOUSECLICKLOCK = 4127;
		internal const int SPI_SETMOUSEBUTTONSWAP = 33;
		internal const int SPI_SETMOUSE = 4;
		internal const int SPI_SETMINIMIZEDMETRICS = 44;
		internal const int SPI_SETMENUUNDERLINES = SPI_SETKEYBOARDCUES;
		internal const int SPI_SETMENUSHOWDELAY = 107;
		internal const int SPI_SETMENUFADE = 4115;
		internal const int SPI_SETMENUDROPALIGNMENT = 28;
		internal const int SPI_SETMENUANIMATION = 4099;
		internal const int SPI_SETLOWPOWERTIMEOUT = 81;
		internal const int SPI_SETLOWPOWERACTIVE = 85;
		internal const int SPI_SETLISTBOXSMOOTHSCROLLING = 4103;
		internal const int SPI_SETLANGTOGGLE = 91;
		internal const int SPI_SETKEYBOARDSPEED = 11;
		internal const int SPI_SETKEYBOARDPREF = 69;
		internal const int SPI_SETKEYBOARDDELAY = 23;
		internal const int SPI_SETKEYBOARDCUES = 4107;
		internal const int SPI_SETICONTITLEWRAP = 26;
		internal const int SPI_SETICONTITLELOGFONT = 34;
		internal const int SPI_SETICONS = 88;
		internal const int SPI_SETICONMETRICS = 46;
		internal const int SPI_SETHOTTRACKING = 4111;
		internal const int SPI_SETHIGHCONTRAST = 67;
		internal const int SPI_SETHANDHELD = 78;
		internal const int SPI_SETGRIDGRANULARITY = 19;
		internal const int SPI_SETGRADIENTCAPTIONS = 4105;
		internal const int SPI_SETFOREGROUNDLOCKTIMEOUT = 8193;
		internal const int SPI_SETFOREGROUNDFLASHCOUNT = 8197;
		internal const int SPI_SETFONTSMOOTHINGTYPE = 8203;
		internal const int SPI_SETFONTSMOOTHINGORIENTATION = 8211;
		internal const int SPI_SETFONTSMOOTHINGCONTRAST = 8205;
		internal const int SPI_SETFONTSMOOTHING = 75;
		internal const int SPI_SETFOCUSBORDERWIDTH = 8207;
		internal const int SPI_SETFOCUSBORDERHEIGHT = 8209;
		internal const int SPI_SETFLATMENU = 4131;
		internal const int SPI_SETFILTERKEYS = 51;
		internal const int SPI_SETFASTTASKSWITCH = 36;
		internal const int SPI_SETDROPSHADOW = 4133;
		internal const int SPI_SETDRAGWIDTH = 76;
		internal const int SPI_SETDRAGHEIGHT = 77;
		internal const int SPI_SETDRAGFULLWINDOWS = 37;
		internal const int SPI_SETDOUBLECLKWIDTH = 29;
		internal const int SPI_SETDOUBLECLKHEIGHT = 30;
		internal const int SPI_SETDOUBLECLICKTIME = 32;
		internal const int SPI_SETDESKWALLPAPER = 20;
		internal const int SPI_SETDESKPATTERN = 21;
		internal const int SPI_SETDEFAULTINPUTLANG = 90;
		internal const int SPI_SETCURSORSHADOW = 4123;
		internal const int SPI_SETCURSORS = 87;
		internal const int SPI_SETCOMBOBOXANIMATION = 4101;
		internal const int SPI_SETCARETWIDTH = 8199;
		internal const int SPI_SETBORDER = 6;
		internal const int SPI_SETBLOCKSENDINPUTRESETS = 4135;
		internal const int SPI_SETBEEP = 2;
		internal const int SPI_SETANIMATION = 73;
		internal const int SPI_SETACTIVEWNDTRKZORDER = 4109;
		internal const int SPI_SETACTIVEWNDTRKTIMEOUT = 8195;
		internal const int SPI_SETACTIVEWINDOWTRACKING = 4097;
		internal const int SPI_SETACCESSTIMEOUT = 61;
		internal const int SPI_LANGDRIVER = 12;
		internal const int SPI_ICONVERTICALSPACING = 24;
		internal const int SPI_ICONHORIZONTALSPACING = 13;
		internal const int SPI_GETWORKAREA = 48;
		internal const int SPI_GETWINDOWSEXTENSION = 92;
		internal const int SPI_GETWHEELSCROLLLINES = 104;
		internal const int SPI_GETUIEFFECTS = 4158;
		internal const int SPI_GETTOOLTIPFADE = 4120;
		internal const int SPI_GETTOOLTIPANIMATION = 4118;
		internal const int SPI_GETTOGGLEKEYS = 52;
		internal const int SPI_GETSTICKYKEYS = 58;
		internal const int SPI_GETSOUNDSENTRY = 64;
		internal const int SPI_GETSNAPTODEFBUTTON = 95;
		internal const int SPI_GETSHOWSOUNDS = 56;
		internal const int SPI_GETSHOWIMEUI = 110;
		internal const int SPI_GETSERIALKEYS = 62;
		internal const int SPI_GETSELECTIONFADE = 4116;
		internal const int SPI_GETSCREENSAVETIMEOUT = 14;
		internal const int SPI_GETSCREENSAVERRUNNING = 114;
		internal const int SPI_GETSCREENSAVEACTIVE = 16;
		internal const int SPI_GETSCREENREADER = 70;
		internal const int SPI_GETPOWEROFFTIMEOUT = 80;
		internal const int SPI_GETPOWEROFFACTIVE = 84;
		internal const int SPI_GETNONCLIENTMETRICS = 41;
		internal const int SPI_GETMOUSEVANISH = 4128;
		internal const int SPI_GETMOUSETRAILS = 94;
		internal const int SPI_GETMOUSESPEED = 112;
		internal const int SPI_GETMOUSESONAR = 4124;
		internal const int SPI_GETMOUSEKEYS = 54;
		internal const int SPI_GETMOUSEHOVERWIDTH = 98;
		internal const int SPI_GETMOUSEHOVERTIME = 102;
		internal const int SPI_GETMOUSEHOVERHEIGHT = 100;
		internal const int SPI_GETMOUSECLICKLOCKTIME = 8200;
		internal const int SPI_GETMOUSECLICKLOCK = 4126;
		internal const int SPI_GETMOUSE = 3;
		internal const int SPI_GETMINIMIZEDMETRICS = 43;
		internal const int SPI_GETMENUUNDERLINES = SPI_GETKEYBOARDCUES;
		internal const int SPI_GETMENUSHOWDELAY = 106;
		internal const int SPI_GETMENUFADE = 4114;
		internal const int SPI_GETMENUDROPALIGNMENT = 27;
		internal const int SPI_GETMENUANIMATION = 4098;
		internal const int SPI_GETLOWPOWERTIMEOUT = 79;
		internal const int SPI_GETLOWPOWERACTIVE = 83;
		internal const int SPI_GETLISTBOXSMOOTHSCROLLING = 4102;
		internal const int SPI_GETKEYBOARDSPEED = 10;
		internal const int SPI_GETKEYBOARDPREF = 68;
		internal const int SPI_GETKEYBOARDDELAY = 22;
		internal const int SPI_GETKEYBOARDCUES = 4106;
		internal const int SPI_GETICONTITLEWRAP = 25;
		internal const int SPI_GETICONTITLELOGFONT = 31;
		internal const int SPI_GETICONMETRICS = 45;
		internal const int SPI_GETHOTTRACKING = 4110;
		internal const int SPI_GETHIGHCONTRAST = 66;
		internal const int SPI_GETGRIDGRANULARITY = 18;
		internal const int SPI_GETGRADIENTCAPTIONS = 4104;
		internal const int SPI_GETFOREGROUNDLOCKTIMEOUT = 8192;
		internal const int SPI_GETFOREGROUNDFLASHCOUNT = 8196;
		internal const int SPI_GETFONTSMOOTHINGTYPE = 8202;
		internal const int SPI_GETFONTSMOOTHINGORIENTATION = 8210;
		internal const int SPI_GETFONTSMOOTHINGCONTRAST = 8204;
		internal const int SPI_GETFONTSMOOTHING = 74;
		internal const int SPI_GETFOCUSBORDERWIDTH = 8206;
		internal const int SPI_GETFOCUSBORDERHEIGHT = 8208;
		internal const int SPI_GETFLATMENU = 4130;
		internal const int SPI_GETFILTERKEYS = 50;
		internal const int SPI_GETFASTTASKSWITCH = 35;
		internal const int SPI_GETDROPSHADOW = 4132;
		internal const int SPI_GETDRAGFULLWINDOWS = 38;
		internal const int SPI_GETDESKWALLPAPER = 115;
		internal const int SPI_GETDEFAULTINPUTLANG = 89;
		internal const int SPI_GETCURSORSHADOW = 4122;
		internal const int SPI_GETCOMBOBOXANIMATION = 4100;
		internal const int SPI_GETCARETWIDTH = 8198;
		internal const int SPI_GETBORDER = 5;
		internal const int SPI_GETBLOCKSENDINPUTRESETS = 4134;
		internal const int SPI_GETBEEP = 1;
		internal const int SPI_GETANIMATION = 72;
		internal const int SPI_GETACTIVEWNDTRKZORDER = 4108;
		internal const int SPI_GETACTIVEWNDTRKTIMEOUT = 8194;
		internal const int SPI_GETACTIVEWINDOWTRACKING = 4096;
		internal const int SPI_GETACCESSTIMEOUT = 60;

		internal const uint SPIF_UPDATEINIFILE = 0x1;
		internal const uint SPIF_SENDCHANGE = 0x2;

		[DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
		internal static extern bool SystemParametersInfo(uint uiAction, int uiParam, LPARAM pvParam, uint fWinIni);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SystemParametersInfoForDpi(uint uiAction, int uiParam, LPARAM pvParam, uint fWinIni, int dpi);

		#endregion

		/// <summary>
		/// WindowFromPhysicalPoint.
		/// </summary>
		[DllImport("user32.dll", EntryPoint = "WindowFromPhysicalPoint")]
		internal static extern AWnd WindowFromPoint(POINT pt);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ScreenToClient(AWnd hWnd, ref POINT lpPoint);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ClientToScreen(AWnd hWnd, ref POINT lpPoint);

		//internal static bool ClientToScreenIgnoreRtl(AWnd w, ref POINT p)
		//{
		//	if(!w.HasExStyle(WS2.LAYOUTRTL)) return ClientToScreen(w, ref p);
		//	if(!GetClientRect(w, out var r) || !MapWindowPoints(w, default, ref r, out _)) return false;
		//	p.Offset(r.left, r.top);
		//	return true;
		//}

		[DllImport("user32.dll", SetLastError = true)]
		static extern int MapWindowPoints(AWnd hWndFrom, AWnd hWndTo, void* lpPoints, int cPoints);

		internal static bool MapWindowPoints(AWnd wFrom, AWnd wTo, void* points, int cPoints, out int ret) {
			ALastError.Clear();
			ret = MapWindowPoints(wFrom, wTo, points, cPoints);
			return ret != 0 ? true : ALastError.Code == 0;
		}

		internal static bool MapWindowPoints(AWnd wFrom, AWnd wTo, ref RECT r, out int ret) {
			fixed (void* u = &r) return MapWindowPoints(wFrom, wTo, u, 2, out ret);
		}

		internal static bool MapWindowPoints(AWnd wFrom, AWnd wTo, ref POINT p, out int ret) {
			fixed (void* u = &p) return MapWindowPoints(wFrom, wTo, u, 1, out ret);
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetGUIThreadInfo(int idThread, ref Native.GUITHREADINFO pgui);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

		internal const uint KEYEVENTF_EXTENDEDKEY = 0x1;
		internal const uint KEYEVENTF_KEYUP = 0x2;
		internal const uint KEYEVENTF_UNICODE = 0x4;
		internal const uint KEYEVENTF_SCANCODE = 0x8;

		internal struct INPUTK
		{
			LPARAM _type;
			public ushort wVk;
			public ushort wScan;
			public uint dwFlags;
			public int time;
			public LPARAM dwExtraInfo;
#pragma warning disable 414 //never used
			int _u1, _u2; //need INPUT size
#pragma warning restore 414

			public INPUTK(KKey vk, ushort sc, uint flags = 0) {
				_type = INPUT_KEYBOARD; dwExtraInfo = AuExtraInfo;
				wVk = (ushort)vk; wScan = sc; dwFlags = flags;
				time = 0; _u2 = _u1 = 0;
				Debug.Assert(sizeof(INPUTK) == sizeof(INPUTM));
			}

			public void Set(KKey vk, ushort sc, uint flags = 0) {
				_type = INPUT_KEYBOARD; dwExtraInfo = AuExtraInfo;
				wVk = (ushort)vk; wScan = sc; dwFlags = flags;
			}

			//public void InitCommonFields()
			//{
			//	_type = INPUT_KEYBOARD; dwExtraInfo = AuExtraInfo;
			//}

			const int INPUT_KEYBOARD = 1;
		}

		[Flags]
		internal enum IMFlags : uint
		{
			Move = 1,
			LeftDown = 2, LeftUp = 4,
			RightDown = 8, RightUp = 16,
			MiddleDown = 32, MiddleUp = 64,
			XDown = 0x80, XUp = 0x100,
			Wheel = 0x0800, HWheel = 0x01000,
			NoCoalesce = 0x2000,
			VirtualdDesktop = 0x4000,
			Absolute = 0x8000,
			//not API
			X1 = 0x1000000,
			X2 = 0x2000000,
		};

		internal struct INPUTM
		{
			LPARAM _type;
			public int dx;
			public int dy;
			public int mouseData;
			public IMFlags dwFlags;
			public int time;
			public LPARAM dwExtraInfo;

			public INPUTM(IMFlags flags, int x = 0, int y = 0, int data = 0) {
				_type = INPUT_MOUSE;
				dx = x; dy = y; dwFlags = flags; mouseData = data;
				time = 0; dwExtraInfo = AuExtraInfo;
			}

			const int INPUT_MOUSE = 0;
		}

		/// <summary>
		/// Extra info value of key and mouse events sent by functions of this library.
		/// </summary>
		internal const int AuExtraInfo = 0x71427fa5;

		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern int SendInput(int cInputs, in INPUTKEY pInputs, int cbSize);
		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern int SendInput(int cInputs, [In] INPUTKEY[] pInputs, int cbSize);
		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern int SendInput(int cInputs, in INPUTMOUSE pInputs, int cbSize);
		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern int SendInput(int cInputs, [In] INPUTMOUSE[] pInputs, int cbSize);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int SendInput(int cInputs, void* pInputs, int cbSize);
		//note: the API never indicates a failure if arguments are valid. Tested UAC (documented), BlockInput, ClipCursor.

		internal static bool SendInput(INPUTK* ip, int n = 1) => SendInput(n, ip, sizeof(INPUTK)) != 0;

		internal static bool SendInput(INPUTM* ip, int n = 1) => SendInput(n, ip, sizeof(INPUTM)) != 0;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsHungAppWindow(AWnd hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetLayeredWindowAttributes(AWnd hwnd, uint crKey, byte bAlpha, uint dwFlags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr CreateIcon(IntPtr hInstance, int nWidth, int nHeight, byte cPlanes, byte cBitsPixel, byte[] lpbANDbits, byte[] lpbXORbits);

		[DllImport("user32.dll", EntryPoint = "LoadCursorW", SetLastError = true)]
		internal static extern IntPtr LoadCursor(IntPtr hInstance, MCursor cursorId);

		internal delegate void TIMERPROC(AWnd param1, int param2, LPARAM param3, uint param4);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern LPARAM SetTimer(AWnd hWnd, LPARAM nIDEvent, int uElapse, TIMERPROC lpTimerFunc);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool KillTimer(AWnd hWnd, LPARAM uIDEvent);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd SetParent(AWnd hWndChild, AWnd hWndNewParent);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AdjustWindowRectEx(ref RECT lpRect, WS dwStyle, bool bMenu, WS2 dwExStyle);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AdjustWindowRectExForDpi(ref RECT lpRect, WS dwStyle, bool bMenu, WS2 dwExStyle, int dpi);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ChangeWindowMessageFilter(int message, uint dwFlag);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern short GetKeyState(int nVirtKey); //not KKey because it is :byte

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern short GetAsyncKeyState(int vKey); //not KKey because it is :byte

		internal const uint MOD_ALT = 0x1;
		internal const uint MOD_CONTROL = 0x2;
		internal const uint MOD_SHIFT = 0x4;
		internal const uint MOD_WIN = 0x8;
		internal const uint MOD_NOREPEAT = 0x4000;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool RegisterHotKey(AWnd hWnd, int id, uint fsModifiers, KKey vk);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool UnregisterHotKey(AWnd hWnd, int id);

		internal const uint MWMO_WAITALL = 0x1;
		internal const uint MWMO_ALERTABLE = 0x2;
		internal const uint MWMO_INPUTAVAILABLE = 0x4;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int MsgWaitForMultipleObjectsEx(int nCount, IntPtr* pHandles, int dwMilliseconds, uint dwWakeMask, uint MWMO_Flags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EndMenu();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool InvalidateRect(AWnd hWnd, RECT* lpRect, bool bErase);
		internal static bool InvalidateRect(AWnd hWnd, bool bErase = false) => InvalidateRect(hWnd, null, bErase);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ValidateRect(AWnd hWnd, RECT* lpRect);
		internal static bool ValidateRect(AWnd hWnd) => ValidateRect(hWnd, null);

		internal const int ERROR = 0;
		internal const int NULLREGION = 1;
		internal const int SIMPLEREGION = 2;
		internal const int COMPLEXREGION = 3;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetUpdateRgn(AWnd hWnd, IntPtr hRgn, bool bErase);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool InvalidateRgn(AWnd hWnd, IntPtr hRgn, bool bErase);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DragDetect(AWnd hwnd, POINT pt);

		[DllImport("user32.dll")]
		internal static extern IntPtr GetCursor();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr SetCursor(IntPtr hCursor);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern AWnd SetCapture(AWnd hWnd);

		[DllImport("user32.dll")]
		internal static extern AWnd GetCapture();

		[DllImport("user32.dll")]
		internal static extern bool ReleaseCapture();

		//[DllImport("user32.dll", EntryPoint = "CharLowerBuffW")]
		//internal static unsafe extern int CharLowerBuff(char* lpsz, int cchLength);

		//[DllImport("user32.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern int wsprintfW(char* lpOut1024, string lpFmt, __arglist);
		//note: with __arglist always returns 0. Could instead use void*, but then much work to properly pack arguments.

		//tested speed (time %) of various formatting functions, with two int and one string arg, with converting to string:
		//	StringBuilder.Append + int.ToString(CultureInfo.InvariantCulture): 85% - FASTEST
		//	StringBuilder.Append: 100%
		//	StringBuilder.AppendFormat + int.ToString() (avoid int boxing): 140%
		//	StringBuilder.AppendFormat: 150%
		//	$"{var} string": 160% (probably uses StringBuilder)
		//	wsprintfW (user32.dll): 150%
		//	_snwprintf (msvcrt.dll): 500% - SLOWEST

		[DllImport("user32.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		internal static extern int wsprintfA(byte* lpOut1024, string lpFmt, __arglist);

		[DllImport("user32.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int wsprintfW(char* lpOut1024, string lpFmt, __arglist);

		internal struct PAINTSTRUCT
		{
			public IntPtr hdc;
			public bool fErase;
			public RECT rcPaint;
			public bool fRestore;
			public bool fIncUpdate;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
		}

		[DllImport("user32.dll")]
		internal static extern IntPtr BeginPaint(AWnd hWnd, out PAINTSTRUCT lpPaint);

		[DllImport("user32.dll")]
		internal static extern bool EndPaint(AWnd hWnd, in PAINTSTRUCT lpPaint);

		[DllImport("user32.dll")]
		internal static extern bool UpdateWindow(AWnd hWnd);

		[DllImport("user32.dll")]
		internal static extern LPARAM GetKeyboardLayout(int idThread);

		[DllImport("user32.dll", EntryPoint = "MapVirtualKeyExW")]
		internal static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, LPARAM dwhkl);

		[DllImport("user32.dll", EntryPoint = "VkKeyScanExW")]
		internal static extern short VkKeyScanEx(char ch, LPARAM dwhkl);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool OpenClipboard(AWnd hWndNewOwner);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool CloseClipboard();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EmptyClipboard();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr SetClipboardData(int uFormat, IntPtr hMem);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetClipboardData(int uFormat);

		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern AWnd SetClipboardViewer(AWnd hWndNewViewer);

		//[DllImport("user32.dll")]
		//internal static extern bool ChangeClipboardChain(AWnd hWndRemove, AWnd hWndNewNext);

		[DllImport("user32.dll")]
		internal static extern AWnd GetOpenClipboardWindow();

		[DllImport("user32.dll")]
		internal static extern uint GetClipboardSequenceNumber();

		[DllImport("user32.dll", EntryPoint = "RegisterClipboardFormatW")]
		internal static extern int RegisterClipboardFormat(string lpszFormat);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AddClipboardFormatListener(AWnd hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool RemoveClipboardFormatListener(AWnd hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int EnumClipboardFormats(int format);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsClipboardFormatAvailable(int format);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetPriorityClipboardFormat(int[] paFormatPriorityList, int cFormats);

		[DllImport("user32.dll", EntryPoint = "GetClipboardFormatNameW", SetLastError = true)]
		internal static unsafe extern int GetClipboardFormatName(int format, char* lpszFormatName, int cchMaxCount);

		[DllImport("user32.dll")]
		internal static extern int GetDoubleClickTime();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, int istepIfAniCur = 0, IntPtr hbrFlickerFreeDraw = default, uint diFlags = 3); //DI_NORMAL

		internal const uint CURSOR_SHOWING = 0x1;

		internal struct CURSORINFO
		{
			public int cbSize;
			public uint flags;
			public IntPtr hCursor;
			public POINT ptScreenPos;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetCursorInfo(ref CURSORINFO pci);

		internal struct ICONINFO
		{
			public bool fIcon;
			public int xHotspot;
			public int yHotspot;
			public IntPtr hbmMask;
			public IntPtr hbmColor;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

		internal struct BITMAP
		{
			public int bmType;
			public int bmWidth;
			public int bmHeight;
			public int bmWidthBytes;
			public ushort bmPlanes;
			public ushort bmBitsPixel;
			public IntPtr bmBits;
		}

		internal const int WH_MSGFILTER = -1;
		internal const int WH_KEYBOARD = 2;
		internal const int WH_GETMESSAGE = 3;
		internal const int WH_CALLWNDPROC = 4;
		internal const int WH_CBT = 5;
		//internal const int WH_SYSMSGFILTER = 6; //hook proc must be in dll
		internal const int WH_MOUSE = 7;
		internal const int WH_DEBUG = 9;
		internal const int WH_SHELL = 10;
		internal const int WH_FOREGROUNDIDLE = 11;
		internal const int WH_CALLWNDPROCRET = 12;
		internal const int WH_KEYBOARD_LL = 13;
		internal const int WH_MOUSE_LL = 14;

		internal delegate LPARAM HOOKPROC(int code, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr SetWindowsHookEx(int WH_X, HOOKPROC lpfn, IntPtr hMod, int dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern LPARAM CallNextHookEx(IntPtr hhk, int nCode, LPARAM wParam, LPARAM lParam);

		internal const uint LLKHF_EXTENDED = 0x1;
		internal const uint LLKHF_INJECTED = 0x10;
		internal const uint LLKHF_ALTDOWN = 0x20;
		internal const uint LLKHF_UP = 0x80;

		internal struct KBDLLHOOKSTRUCT
		{
			public uint vkCode;
			public uint scanCode;
			public uint flags;
			public int time;
			public LPARAM dwExtraInfo;

			public bool IsUp => 0 != (flags & LLKHF_UP);

			/// <summary>
			/// true if the event was generated by software.
			/// </summary>
			public bool IsInjected => 0 != (flags & LLKHF_INJECTED);

			/// <summary>
			/// true if the event was generated by functions of this library.
			/// </summary>
			public bool IsInjectedByAu => 0 != (flags & LLKHF_INJECTED) && dwExtraInfo == AuExtraInfo;
			//CONSIDER: also add IsInjectedByAuOrQM2. And let QM2 recognize the extra info too. Or let they use the same extra info; probably bad idea.

			/// <summary>
			/// The 'set' function adds or removes flag 0x80000000.
			/// The 'get' function returns true if flag 0x80000000 is set.
			/// </summary>
			public bool BlockEvent {
				get => 0 != (flags & 0x80000000);
				set { if (value) flags |= 0x80000000; else flags &= ~0x80000000; }
			}
		}

		internal const uint LLMHF_INJECTED = 0x1;

		internal struct MSLLHOOKSTRUCT
		{
			public POINT pt;
			public uint mouseData;
			public uint flags;
			public int time;
			public LPARAM dwExtraInfo;

			/// <summary>
			/// true if the event was generated by software.
			/// </summary>
			public bool IsInjected => 0 != (flags & LLMHF_INJECTED);

			/// <summary>
			/// true if the event was generated by functions of this library.
			/// </summary>
			public bool IsInjectedByAu => 0 != (flags & LLMHF_INJECTED) && dwExtraInfo == AuExtraInfo;

			/// <summary>
			/// The 'set' function adds or removes flag 0x80000000.
			/// The 'get' function returns true if flag 0x80000000 is set.
			/// </summary>
			public bool BlockEvent {
				get => 0 != (flags & 0x80000000);
				set { if (value) flags |= 0x80000000; else flags &= ~0x80000000; }
			}
		}

		internal const int HC_NOREMOVE = 3;

		internal delegate void WINEVENTPROC(IntPtr hWinEventHook, AccEVENT event_, AWnd hwnd, AccOBJID idObject, int idChild, int idEventThread, int dwmsEventTime);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr SetWinEventHook(AccEVENT eventMin, AccEVENT eventMax, IntPtr hmodWinEventProc, WINEVENTPROC pfnWinEventProc, int idProcess, int idThread, AccHookFlags dwFlags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);

		[Flags]
		internal enum AnimationFlags : uint
		{
			Roll = 0x0000, // Uses a roll animation.
			HorizontalPositive = 0x00001, // Animates the window from left to right. This flag can be used with roll or slide animation.
			HorizontalNegative = 0x00002, // Animates the window from right to left. This flag can be used with roll or slide animation.
			VerticalPositive = 0x00004, // Animates the window from top to bottom. This flag can be used with roll or slide animation.
			VerticalNegative = 0x00008, // Animates the window from bottom to top. This flag can be used with roll or slide animation.
			Center = 0x00010, // Makes the window appear to collapse inward if Hide is used or expand outward if the Hide is not used.
			Hide = 0x10000, // Hides the window. By default, the window is shown.
			Activate = 0x20000, // Activates the window.
			Slide = 0x40000, // Uses a slide animation. By default, roll animation is used.
			Blend = 0x80000, // Uses a fade effect. This flag can be used only with a top-level window.
			Mask = 0xfffff,
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AnimateWindow(AWnd hWnd, int dwTime, AnimationFlags dwFlags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetCaretPos(out POINT lpPoint);

		internal static bool GetCaretPosInScreen_(out POINT p) => GetCaretPos(out p) && GetFocus().MapClientToScreen(ref p);

		[DllImport("user32.dll")]
		internal static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte* lpKeyState, char* pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

		internal const uint PW_CLIENTONLY = 0x1;
		internal const uint PW_RENDERFULLCONTENT = 0x2;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool PrintWindow(AWnd hwnd, IntPtr hdcBlt, uint nFlags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetDC(AWnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetWindowDC(AWnd hWnd);

		[DllImport("user32.dll")] //note: no SetLastError = true
		internal static extern int ReleaseDC(AWnd hWnd, IntPtr hDC);

		[DllImport("user32.dll")]
		internal static extern int FillRect(IntPtr hDC, in RECT lprc, IntPtr hbr);

		internal const uint RDW_FRAME = 0x400;
		internal const uint RDW_INVALIDATE = 0x1;

		[DllImport("user32.dll")]
		internal static extern bool RedrawWindow(AWnd hWnd, RECT* lprcUpdate = null, IntPtr hrgnUpdate = default, uint flags = 0);

		/// <param name="flags">Au.Controls.PopupAlignment</param>
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool CalculatePopupWindowPosition(in POINT anchorPoint, in SIZE windowSize, uint flags, in RECT excludeRect, out RECT popupWindowPosition);

		[DllImport("user32.dll")]
		internal static extern int MenuItemFromPoint(AWnd hWnd, IntPtr hMenu, POINT ptScreen);

		[DllImport("user32.dll")]
		internal static extern int GetMenuItemID(IntPtr hMenu, int nPos);

		internal struct MENUITEMINFO
		{
			public int cbSize;
			public uint fMask;
			public uint fType;
			public uint fState;
			public int wID;
			public IntPtr hSubMenu;
			public IntPtr hbmpChecked;
			public IntPtr hbmpUnchecked;
			public LPARAM dwItemData;
			public char* dwTypeData;
			public int cch;
			public IntPtr hbmpItem;
		}

		internal const uint MIIM_TYPE = 0x10;

		[DllImport("user32.dll", EntryPoint = "GetMenuItemInfoW")]
		internal static extern bool GetMenuItemInfo(IntPtr hmenu, int item, bool fByPosition, ref MENUITEMINFO lpmii);

		[DllImport("user32.dll")]
		internal static extern IntPtr GetSystemMenu(AWnd hWnd, bool bRevert);

		[DllImport("user32.dll")]
		internal static extern IntPtr CreatePopupMenu();

		[DllImport("user32.dll")]
		internal static extern bool DestroyMenu(IntPtr hMenu);

		internal const uint TPM_RETURNCMD = 0x100;

		[DllImport("user32.dll")]
		internal static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, AWnd hwnd, IntPtr lptpm = default);

		internal const uint MF_SEPARATOR = 0x800;

		[DllImport("user32.dll", EntryPoint = "AppendMenuW")]
		internal static extern bool AppendMenu(IntPtr hMenu, uint uFlags = MF_SEPARATOR, LPARAM uIDNewItem = default, string lpNewItem = null);

		internal const uint SIF_RANGE = 0x1;
		internal const uint SIF_PAGE = 0x2;
		internal const uint SIF_POS = 0x4;
		internal const uint SIF_TRACKPOS = 0x10;

		internal struct SCROLLINFO
		{
			public int cbSize;
			public uint fMask;
			public int nMin;
			public int nMax;
			public int nPage;
			public int nPos;
			public int nTrackPos;
		}

		[DllImport("user32.dll")]
		internal static extern int SetScrollInfo(AWnd hwnd, bool vert, in SCROLLINFO lpsi, bool redraw);

		[DllImport("user32.dll")]
		internal static extern bool GetScrollInfo(AWnd hwnd, bool vert, ref SCROLLINFO lpsi);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetScrollBarInfo(AWnd hwnd, AccOBJID idObject, ref SCROLLBARINFO psbi);

		internal struct SCROLLBARINFO
		{
			public int cbSize;
			public RECT rcScrollBar;
			public int dxyLineButton;
			public int xyThumbTop;
			public int xyThumbBottom;
			public int reserved;
			//public unsafe fixed uint rgstate[6];
			public uint stateScrollbar, stateArrowTopRight, statePageUpRight, stateThumb, statePageDownLeft, stateArrowBottomLeft;
		}
		internal const uint STATE_SYSTEM_INVISIBLE = 0x8000;
		internal const uint STATE_SYSTEM_OFFSCREEN = 0x10000;
		internal const uint STATE_SYSTEM_PRESSED = 0x8;
		internal const uint STATE_SYSTEM_UNAVAILABLE = 0x1;

		[DllImport("user32.dll", EntryPoint = "MessageBoxW")]
		internal static extern int MessageBox(AWnd hWnd, string lpText, string lpCaption, uint uType);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetWindowRgn(AWnd hWnd, IntPtr hRgn);

		//[DllImport("user32.dll")]
		//internal static extern bool PhysicalToLogicalPoint(AWnd hWnd, ref POINT lpPoint);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetDpiForWindow(AWnd hWnd);

		[DllImport("shcore.dll", PreserveSig = true)]
		internal static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out int dpiX, out int dpiY);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern LPARAM SetThreadDpiAwarenessContext(LPARAM dpiContext);

		[DllImport("user32.dll")]
		internal static extern int GetSysColor(int nIndex);

	}

}
