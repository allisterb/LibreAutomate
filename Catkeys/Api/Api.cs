﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;

[module: DefaultCharSet(CharSet.Unicode)] //change default DllImport CharSet from ANSI to Unicode

//[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32|DllImportSearchPath.UserDirectories)]

[assembly: InternalsVisibleTo("G.Controls, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7836581375ad28892abd6476a89a68f879d2df07404cfcddf2899cd05616f8fb45c9bab78b972a2ca99339af3774b0a2b6f2a5768acdf2995a255106943fffa9aa65d66a37829f7ebbc7c0ffc75b6d2bf95c1964ec84774834c07438584125afdfb58b77b5411c1401589adbefadef502893b8c8cff8b682b05043703ca479e")]
[assembly: InternalsVisibleTo("CatEdit, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7836581375ad28892abd6476a89a68f879d2df07404cfcddf2899cd05616f8fb45c9bab78b972a2ca99339af3774b0a2b6f2a5768acdf2995a255106943fffa9aa65d66a37829f7ebbc7c0ffc75b6d2bf95c1964ec84774834c07438584125afdfb58b77b5411c1401589adbefadef502893b8c8cff8b682b05043703ca479e")]
[assembly: InternalsVisibleTo("CatkeysTasks, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7836581375ad28892abd6476a89a68f879d2df07404cfcddf2899cd05616f8fb45c9bab78b972a2ca99339af3774b0a2b6f2a5768acdf2995a255106943fffa9aa65d66a37829f7ebbc7c0ffc75b6d2bf95c1964ec84774834c07438584125afdfb58b77b5411c1401589adbefadef502893b8c8cff8b682b05043703ca479e")]
[assembly: InternalsVisibleTo("Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7836581375ad28892abd6476a89a68f879d2df07404cfcddf2899cd05616f8fb45c9bab78b972a2ca99339af3774b0a2b6f2a5768acdf2995a255106943fffa9aa65d66a37829f7ebbc7c0ffc75b6d2bf95c1964ec84774834c07438584125afdfb58b77b5411c1401589adbefadef502893b8c8cff8b682b05043703ca479e")]
[assembly: InternalsVisibleTo("Catkeys.Compiler, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7836581375ad28892abd6476a89a68f879d2df07404cfcddf2899cd05616f8fb45c9bab78b972a2ca99339af3774b0a2b6f2a5768acdf2995a255106943fffa9aa65d66a37829f7ebbc7c0ffc75b6d2bf95c1964ec84774834c07438584125afdfb58b77b5411c1401589adbefadef502893b8c8cff8b682b05043703ca479e")]
[assembly: InternalsVisibleTo("Catkeys.Triggers, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7836581375ad28892abd6476a89a68f879d2df07404cfcddf2899cd05616f8fb45c9bab78b972a2ca99339af3774b0a2b6f2a5768acdf2995a255106943fffa9aa65d66a37829f7ebbc7c0ffc75b6d2bf95c1964ec84774834c07438584125afdfb58b77b5411c1401589adbefadef502893b8c8cff8b682b05043703ca479e")]
[assembly: InternalsVisibleTo("SdkConverter, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d7836581375ad28892abd6476a89a68f879d2df07404cfcddf2899cd05616f8fb45c9bab78b972a2ca99339af3774b0a2b6f2a5768acdf2995a255106943fffa9aa65d66a37829f7ebbc7c0ffc75b6d2bf95c1964ec84774834c07438584125afdfb58b77b5411c1401589adbefadef502893b8c8cff8b682b05043703ca479e")]

namespace Catkeys
{
	[DebuggerStepThrough]
	//[CLSCompliant(false)]
	internal static unsafe partial class Api
	{
		/// <summary>
		/// Gets the native size of a struct variable.
		/// Returns (uint)Marshal.SizeOf(typeof(T)).
		/// Speed: the same (in Release config) as of Marshal.SizeOf(typeof(T)) and 2 times faster than Marshal.SizeOf(v).
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="v"></param>
		internal static uint SizeOf<T>(T v) { return (uint)Marshal.SizeOf(typeof(T)); }

		/// <summary>
		/// Gets the native size of a type.
		/// Returns (uint)Marshal.SizeOf(typeof(T)).
		/// </summary>
		/// <typeparam name="T"></typeparam>
		internal static uint SizeOf<T>() { return (uint)Marshal.SizeOf(typeof(T)); }

		//Tried to make function that creates new struct and sets its first int member = sizeof struct. But cannot get address of generic parameter.
		//internal static void StructInitSize<T>(out T v) where T :struct
		//{
		//	v=new T();
		//	int* cbSize = &v; //error when generic
		//	*cbSize=Marshal.SizeOf(typeof(T));
		//}

		/// <summary>
		/// If o is not null, calls <see cref="Marshal.ReleaseComObject"/>.
		/// </summary>
		internal static void ReleaseComObject(object o)
		{
			if(o != null) Marshal.ReleaseComObject(o);
		}

		//USER32

		internal struct COPYDATASTRUCT
		{
			public LPARAM dwData;
			public int cbData;
			public IntPtr lpData;

			public COPYDATASTRUCT(LPARAM dwData, int cbData, IntPtr lpData)
			{
				this.dwData = dwData; this.cbData = cbData; this.lpData = lpData;
			}
		}

		[DllImport("user32.dll")]
		internal static extern bool IsWindow(Wnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowVisible(Wnd hWnd);

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
		internal static extern void ShowWindow(Wnd hWnd, int SW_X);
		//note: the returns value does not say succeeded/failed.
		//	It is non-zero if was visible, 0 if was hidden.
		//	Declared void to avoid programming errors.

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowEnabled(Wnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern void EnableWindow(Wnd hWnd, bool bEnable);
		//note: the returns value does not say succeeded/failed.
		//	It is non-zero if was disabled, 0 if was enabled.
		//	Declared void to avoid programming errors.

		[DllImport("user32.dll", EntryPoint = "FindWindowW", SetLastError = true)]
		internal static extern Wnd FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", EntryPoint = "FindWindowExW", SetLastError = true)]
		internal static extern Wnd FindWindowEx(Wnd hWndParent, Wnd hWndChildAfter, string lpszClass, string lpszWindow);

		internal struct WNDCLASSEX
		{
			public uint cbSize;
			public uint style;
			public IntPtr lpfnWndProc; //not WNDPROC to avoid auto-marshaling where don't need. Use Marshal.GetFunctionPointerForDelegate/GetDelegateForFunctionPointer.
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			public IntPtr lpszMenuName;
			public char* lpszClassName; //not string because CLR would call CoTaskMemFree
			public IntPtr hIconSm;

			public WNDCLASSEX(bool setCursorAndBrush) : this()
			{
				this.cbSize = Api.SizeOf<WNDCLASSEX>();
				if(setCursorAndBrush) {
					hCursor = Api.LoadCursor(default(IntPtr), Api.IDC_ARROW);
					hbrBackground = (IntPtr)(Api.COLOR_BTNFACE + 1);
				}
			}

			public WNDCLASSEX(Wnd.Misc.WindowClass.WndClassEx ex) : this()
			{
				this.cbSize = Api.SizeOf<WNDCLASSEX>();
				this.cbClsExtra = ex.cbClsExtra;
				this.hInstance = ex.hInstance;
				this.hIcon = ex.hIcon;
				this.hCursor = ex.hCursor;
				this.hbrBackground = ex.hbrBackground;
				this.hIconSm = ex.hIconSm;
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

		[DllImport("user32.dll", EntryPoint = "GetClassInfoExW", SetLastError = true)]
		internal static extern ushort GetClassInfoEx(IntPtr hInstance, string lpszClass, ref WNDCLASSEX lpwcx);

		[DllImport("user32.dll", EntryPoint = "UnregisterClassW", SetLastError = true)]
		internal static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

		[DllImport("user32.dll", EntryPoint = "UnregisterClassW", SetLastError = true)]
		internal static extern bool UnregisterClass(uint classAtom, IntPtr hInstance);

		[DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
		internal static extern Wnd CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, Wnd hWndParent, LPARAM hMenu, IntPtr hInstance, LPARAM lpParam);

		[DllImport("user32.dll", EntryPoint = "DefWindowProcW")]
		internal static extern LPARAM DefWindowProc(Wnd hWnd, uint msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
		internal static extern LPARAM CallWindowProc(Native.WNDPROC lpPrevWndFunc, Wnd hWnd, uint Msg, LPARAM wParam, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DestroyWindow(Wnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern void PostQuitMessage(int nExitCode);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetMessage(out Native.MSG lpMsg, Wnd hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool TranslateMessage(ref Native.MSG lpMsg);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern LPARAM DispatchMessage(ref Native.MSG lpmsg);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool WaitMessage();

		internal const uint PM_NOREMOVE = 0x0;
		internal const uint PM_REMOVE = 0x1;
		internal const uint PM_NOYIELD = 0x2;
		internal const uint PM_QS_SENDMESSAGE = 0x400000;
		internal const uint PM_QS_POSTMESSAGE = 0x980000;
		internal const uint PM_QS_PAINT = 0x200000;
		internal const uint PM_QS_INPUT = 0x1C070000;

		[DllImport("user32.dll", EntryPoint = "PeekMessageW", SetLastError = true)]
		internal static extern bool PeekMessage(out Native.MSG lpMsg, Wnd hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

		internal const int WH_MSGFILTER = -1;
		internal const int WH_KEYBOARD = 2;
		internal const int WH_GETMESSAGE = 3;
		internal const int WH_CALLWNDPROC = 4;
		internal const int WH_CBT = 5;
		internal const int WH_SYSMSGFILTER = 6;
		internal const int WH_MOUSE = 7;
		internal const int WH_DEBUG = 9;
		internal const int WH_SHELL = 10;
		internal const int WH_FOREGROUNDIDLE = 11;
		internal const int WH_CALLWNDPROCRET = 12;
		internal const int WH_KEYBOARD_LL = 13;
		internal const int WH_MOUSE_LL = 14;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr SetWindowsHookEx(int WH_X, HOOKPROC lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern LPARAM CallNextHookEx(IntPtr hhk, int nCode, LPARAM wParam, LPARAM lParam);

		internal const int HCBT_MOVESIZE = 0;
		internal const int HCBT_MINMAX = 1;
		//internal const int HCBT_QS = 2;
		internal const int HCBT_CREATEWND = 3;
		internal const int HCBT_DESTROYWND = 4;
		internal const int HCBT_ACTIVATE = 5;
		internal const int HCBT_CLICKSKIPPED = 6;
		internal const int HCBT_KEYSKIPPED = 7;
		internal const int HCBT_SYSCOMMAND = 8;
		internal const int HCBT_SETFOCUS = 9;

		internal const int HC_ACTION = 0;

		internal delegate LPARAM HOOKPROC(int code, LPARAM wParam, LPARAM lParam);

		internal const int GA_PARENT = 1;
		internal const int GA_ROOT = 2;
		internal const int GA_ROOTOWNER = 3;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd GetAncestor(Wnd hwnd, uint GA_X);

		[DllImport("user32.dll")]
		internal static extern Wnd GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetForegroundWindow(Wnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AllowSetForegroundWindow(uint dwProcessId);

		internal const uint LSFW_LOCK = 1;
		internal const uint LSFW_UNLOCK = 2;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool LockSetForegroundWindow(uint LSFW_X);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd SetFocus(Wnd hWnd);

		[DllImport("user32.dll")]
		internal static extern Wnd GetFocus();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd SetActiveWindow(Wnd hWnd);

		[DllImport("user32.dll")]
		internal static extern Wnd GetActiveWindow();

		internal struct WINDOWPOS
		{
			public Wnd hwnd;
			public Wnd hwndInsertAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public uint flags;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetWindowPos(Wnd hWnd, Wnd hWndInsertAfter, int X, int Y, int cx, int cy, uint SWP_X);

		internal struct FLASHWINFO
		{
			public uint cbSize;
			public Wnd hwnd;
			public uint dwFlags;
			public uint uCount;
			public uint dwTimeout;
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
		internal static extern Wnd GetWindow(Wnd hWnd, uint GW_X);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd GetTopWindow(Wnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd GetParent(Wnd hWnd);

		[DllImport("user32.dll")]
		internal static extern Wnd GetDesktopWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd GetShellWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd GetLastActivePopup(Wnd hWnd);

		[DllImport("user32.dll")]
		internal static extern bool IntersectRect(out RECT lprcDst, ref RECT lprcSrc1, ref RECT lprcSrc2);

		[DllImport("user32.dll")]
		internal static extern bool UnionRect(out RECT lprcDst, ref RECT lprcSrc1, ref RECT lprcSrc2);

		//Gets DPI physical cursor pos, ie always in pixels.
		//The classic GetCursorPos API gets logical pos. Also it has a bug: randomly gets physical pos, even for same point.
		//Make sure that the process is DPI-aware.
		[DllImport("user32.dll", EntryPoint = "GetPhysicalCursorPos", SetLastError = true)]
		internal static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true)]
		internal static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint LR_X);
		[DllImport("user32.dll", EntryPoint = "LoadImageW", SetLastError = true)]
		internal static extern IntPtr LoadImage(IntPtr hInst, LPARAM resId, uint type, int cx, int cy, uint LR_X);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr CopyImage(IntPtr h, uint type, int cx, int cy, uint flags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowRect(Wnd hWnd, out RECT lpRect);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetClientRect(Wnd hWnd, out RECT lpRect);

		internal const uint WPF_SETMINPOSITION = 0x1;
		internal const uint WPF_RESTORETOMAXIMIZED = 0x2;
		internal const uint WPF_ASYNCWINDOWPLACEMENT = 0x4;

		internal struct WINDOWPLACEMENT
		{
			public uint length;
			/// <summary> WPF_ </summary>
			public uint flags;
			public int showCmd;
			public POINT ptMinPosition;
			public POINT ptMaxPosition;
			public RECT rcNormalPosition;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowPlacement(Wnd hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetWindowPlacement(Wnd hWnd, ref WINDOWPLACEMENT lpwndpl);

		public struct WINDOWINFO
		{
			public uint cbSize;
			public RECT rcWindow;
			public RECT rcClient;
			public uint dwStyle;
			public uint dwExStyle;
			public uint dwWindowStatus;
			public uint cxWindowBorders;
			public uint cyWindowBorders;
			public ushort atomWindowType;
			public ushort wCreatorVersion;
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetWindowInfo(Wnd hwnd, ref WINDOWINFO pwi);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsZoomed(Wnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsIconic(Wnd hWnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint GetWindowThreadProcessId(Wnd hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsWindowUnicode(Wnd hWnd);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool IsWow64Process(IntPtr hProcess, out int Wow64Process);


		[DllImport("user32.dll", EntryPoint = "GetPropW", SetLastError = true)]
		internal static extern LPARAM GetProp(Wnd hWnd, string lpString);

		[DllImport("user32.dll", EntryPoint = "GetPropW", SetLastError = true)]
		//internal static extern LPARAM GetProp(Wnd hWnd, [MarshalAs(UnmanagedType.SysInt)] ushort atom); //exception, must be U2
		internal static extern LPARAM GetProp(Wnd hWnd, LPARAM atom);

		[DllImport("user32.dll", EntryPoint = "SetPropW", SetLastError = true)]
		internal static extern bool SetProp(Wnd hWnd, string lpString, LPARAM hData);

		[DllImport("user32.dll", EntryPoint = "SetPropW", SetLastError = true)]
		internal static extern bool SetProp(Wnd hWnd, LPARAM atom, LPARAM hData);

		[DllImport("user32.dll", EntryPoint = "RemovePropW", SetLastError = true)]
		internal static extern LPARAM RemoveProp(Wnd hWnd, string lpString);

		[DllImport("user32.dll", EntryPoint = "RemovePropW", SetLastError = true)]
		internal static extern LPARAM RemoveProp(Wnd hWnd, LPARAM atom);

		internal delegate bool PROPENUMPROCEX(Wnd hwnd, IntPtr lpszString, LPARAM hData, LPARAM dwData);

		[DllImport("user32.dll", EntryPoint = "EnumPropsExW", SetLastError = true)]
		internal static extern int EnumPropsEx(Wnd hWnd, PROPENUMPROCEX lpEnumFunc, LPARAM lParam);

		internal delegate int WNDENUMPROC(Wnd hwnd, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EnumThreadWindows(uint dwThreadId, WNDENUMPROC lpfn, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EnumChildWindows(Wnd hWndParent, WNDENUMPROC lpEnumFunc, LPARAM lParam);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd GetDlgItem(Wnd hDlg, int nIDDlgItem);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetDlgCtrlID(Wnd hWnd);

		[DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", SetLastError = true)]
		internal static extern uint RegisterWindowMessage(string lpString);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsChild(Wnd hWndParent, Wnd hWnd);

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
		internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, LPARAM pvParam, uint fWinIni);

		[DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
		internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, void* pvParam, uint fWinIni);

		#endregion

		[DllImport("user32.dll")]
		internal static extern Wnd WindowFromPoint(POINT Point);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd RealChildWindowFromPoint(Wnd hwndParent, POINT ptParentClientCoords);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ScreenToClient(Wnd hWnd, ref POINT lpPoint);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ClientToScreen(Wnd hWnd, ref POINT lpPoint);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int MapWindowPoints(Wnd hWndFrom, Wnd hWndTo, ref POINT lpPoints, uint cPoints = 1);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int MapWindowPoints(Wnd hWndFrom, Wnd hWndTo, ref RECT lpPoints, uint cPoints = 2);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int MapWindowPoints(Wnd hWndFrom, Wnd hWndTo, void* lpPoints, uint cPoints);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetGUIThreadInfo(uint idThread, ref Native.GUITHREADINFO pgui);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

		[Flags]
		internal enum IKFlag :uint
		{
			Extended = 1, Up = 2, Unicode = 4, Scancode = 8
		};

		internal struct INPUTKEY
		{
			LPARAM _type;
			public ushort wVk;
			public ushort wScan;
			public IKFlag dwFlags;
			public uint time;
			public LPARAM dwExtraInfo;
			int _u1, _u2; //need INPUT size

			public INPUTKEY(int vk, int sc, IKFlag flags = 0)
			{
				_type = INPUT_KEYBOARD;
				wVk = (ushort)vk; wScan = (ushort)sc; dwFlags = flags;
				time = 0; dwExtraInfo = CatkeysExtraInfo;
				_u2 = _u1 = 0;
				Debug.Assert(Size == INPUTMOUSE.Size);
			}

			public static readonly int Size = Marshal.SizeOf(typeof(INPUTKEY));

			public const uint CatkeysExtraInfo = 0xA1427fa5;
			const int INPUT_KEYBOARD = 1;
		}

		[Flags]
		internal enum IMFlag :uint
		{
			Move = 1,
			LeftDown = 2, LeftUp = 4,
			RightDown = 8, RightUp = 16,
			MiddleDown = 32, MiddleUp = 64,
			X1Down = 0x80, X1Up = 0x100,
			X2Down = 0x80000080, X2Up = 0x80000100,
			Wheel = 0x0800, HWheel = 0x01000,
			NoCoalesce = 0x2000,
			VirtualdDesktop = 0x4000,
			Absolute = 0x8000
		};

		internal struct INPUTMOUSE
		{
			LPARAM _type;
			public int dx;
			public int dy;
			public int mouseData;
			public IMFlag dwFlags;
			public uint time;
			public LPARAM dwExtraInfo;

			public INPUTMOUSE(IMFlag flags, int x = 0, int y = 0, int wheelTicks = 0)
			{
				_type = INPUT_MOUSE;
				dx = x; dy = y; dwFlags = flags; mouseData = wheelTicks * 120;
				time = 0; dwExtraInfo = CatkeysExtraInfo;
				if((dwFlags & (IMFlag.X1Down | IMFlag.X2Up)) != 0) {
					mouseData = ((dwFlags & (IMFlag)0x80000000U) != 0) ? 2 : 1;
					dwFlags &= (IMFlag)0x7fffffff;
				}
			}

			public static readonly int Size = Marshal.SizeOf(typeof(INPUTMOUSE));

			public const uint CatkeysExtraInfo = 0xA1427fa5;
			const int INPUT_MOUSE = 0;
		}

		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern uint SendInput(uint cInputs, ref INPUTKEY pInputs, int cbSize);
		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern uint SendInput(uint cInputs, [In] INPUTKEY[] pInputs, int cbSize);
		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern uint SendInput(uint cInputs, ref INPUTMOUSE pInputs, int cbSize);
		//[DllImport("user32.dll", SetLastError = true)]
		//internal static extern uint SendInput(uint cInputs, [In] INPUTMOUSE[] pInputs, int cbSize);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint SendInput(int cInputs, void* pInputs, int cbSize);

		internal static bool SendInputKey(ref INPUTKEY ik)
		{
			fixed (void* p = &ik) {
				return SendInput(1, p, INPUTKEY.Size) != 0;
			}
		}

		internal static bool SendInputKey(INPUTKEY[] ik)
		{
			if(ik == null || ik.Length == 0) return false;
			fixed (void* p = ik) {
				return SendInput(ik.Length, p, INPUTKEY.Size) != 0;
			}
		}

		internal static bool SendInputMouse(ref INPUTMOUSE ik)
		{
			fixed (void* p = &ik) {
				return SendInput(1, p, INPUTMOUSE.Size) != 0;
			}
		}

		internal static bool SendInputMouse(INPUTMOUSE[] ik)
		{
			if(ik == null || ik.Length == 0) return false;
			fixed (void* p = ik) {
				return SendInput(ik.Length, p, INPUTMOUSE.Size) != 0;
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool IsHungAppWindow(Wnd hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool SetLayeredWindowAttributes(Wnd hwnd, uint crKey, byte bAlpha, uint dwFlags);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr CreateIcon(IntPtr hInstance, int nWidth, int nHeight, byte cPlanes, byte cBitsPixel, byte[] lpbANDbits, byte[] lpbXORbits);

		[DllImport("user32.dll", EntryPoint = "PrivateExtractIconsW", SetLastError = true)]
		internal static extern uint PrivateExtractIcons(string szFileName, int nIconIndex, int cxIcon, int cyIcon, [Out] IntPtr[] phicon, IntPtr piconid, uint nIcons, uint flags);
		[DllImport("user32.dll", EntryPoint = "PrivateExtractIconsW", SetLastError = true)]
		internal static extern uint PrivateExtractIcons(string szFileName, int nIconIndex, int cxIcon, int cyIcon, out IntPtr phicon, IntPtr piconid, uint nIcons, uint flags);

		[DllImport("user32.dll", EntryPoint = "LoadCursorW", SetLastError = true)]
		internal static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

		internal delegate void TIMERPROC(Wnd param1, uint param2, LPARAM param3, uint param4);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern LPARAM SetTimer(Wnd hWnd, LPARAM nIDEvent, uint uElapse, TIMERPROC lpTimerFunc);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool KillTimer(Wnd hWnd, LPARAM uIDEvent);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd SetParent(Wnd hWndChild, Wnd hWndNewParent);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern short GetKeyState(int nVirtKey);

		internal const uint MWMO_WAITALL = 0x1;
		internal const uint MWMO_ALERTABLE = 0x2;
		internal const uint MWMO_INPUTAVAILABLE = 0x4;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint MsgWaitForMultipleObjectsEx(uint nCount, [In] IntPtr[] pHandles, uint dwMilliseconds = INFINITE, uint dwWakeMask = QS_ALLINPUT, uint MWMO_Flags = 0);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint MsgWaitForMultipleObjectsEx(uint nCount, ref IntPtr pHandle, uint dwMilliseconds = INFINITE, uint dwWakeMask = QS_ALLINPUT, uint MWMO_Flags = 0);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool RegisterHotKey(Wnd hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool UnregisterHotKey(Wnd hWnd, int id);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EndMenu();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool InvalidateRect(Wnd hWnd, ref RECT lpRect, bool bErase);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool InvalidateRect(Wnd hWnd, IntPtr lpRect, bool bErase);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ValidateRect(Wnd hWnd, ref RECT lpRect);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool ValidateRect(Wnd hWnd, LPARAM zero = default(LPARAM));

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetUpdateRect(Wnd hWnd, out RECT lpRect, bool bErase);
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool GetUpdateRect(Wnd hWnd, LPARAM zero, bool bErase);

		internal const int ERROR = 0;
		internal const int NULLREGION = 1;
		internal const int SIMPLEREGION = 2;
		internal const int COMPLEXREGION = 3;

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetUpdateRgn(Wnd hWnd, IntPtr hRgn, bool bErase);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool InvalidateRgn(Wnd hWnd, IntPtr hRgn, bool bErase);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool DragDetect(Wnd hwnd, POINT pt);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr SetCursor(IntPtr hCursor);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern Wnd SetCapture(Wnd hWnd);

		[DllImport("user32.dll")]
		internal static extern Wnd GetCapture();

		[DllImport("user32.dll")]
		internal static extern bool ReleaseCapture();

		[DllImport("user32.dll", EntryPoint = "CharLowerBuffW")]
		internal static unsafe extern uint CharLowerBuff(char* lpsz, uint cchLength);







		//GDI32

		[DllImport("gdi32.dll")] //this and many other GDI functions don't use SetLastError
		internal static extern bool DeleteObject(IntPtr ho);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

		internal const int RGN_AND = 1;
		internal const int RGN_OR = 2;
		internal const int RGN_XOR = 3;
		internal const int RGN_DIFF = 4;
		internal const int RGN_COPY = 5;

		[DllImport("gdi32.dll")]
		internal static extern int CombineRgn(IntPtr hrgnDst, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int iMode);

		[DllImport("gdi32.dll")]
		internal static extern bool SetRectRgn(IntPtr hrgn, int left, int top, int right, int bottom);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetDC(Wnd hWnd);

		[DllImport("user32.dll")] //note: no SetLastError = true
		internal static extern int ReleaseDC(Wnd hWnd, IntPtr hDC);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		internal static extern bool DeleteDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

		[DllImport("gdi32.dll")]
		internal static extern int GetDeviceCaps(IntPtr hdc, int index);

		[DllImport("gdi32.dll", EntryPoint = "GetTextExtentPoint32W")]
		internal static extern bool GetTextExtentPoint32(IntPtr hdc, string lpString, int c, out SIZE psizl);

		[DllImport("gdi32.dll", EntryPoint = "CreateFontW")]
		internal static extern IntPtr CreateFont(int cHeight, int cWidth = 0, int cEscapement = 0, int cOrientation = 0, int cWeight = 0, uint bItalic = 0, uint bUnderline = 0, uint bStrikeOut = 0, uint iCharSet = 0, uint iOutPrecision = 0, uint iClipPrecision = 0, uint iQuality = 0, uint iPitchAndFamily = 0, string pszFaceName = null);

		//[DllImport("user32.dll", EntryPoint = "CharUpperBuffW")]
		//internal static unsafe extern uint CharUpperBuff(char* lpsz, uint cchLength);





		//KERNEL32

		[DllImport("kernel32.dll", SetLastError = true)] //note: without 'SetLastError = true' Marshal.GetLastWin32Error is unaware that we set the code to 0 etc and returns old captured error code
		internal static extern void SetLastError(int errCode);

		[DllImport("kernel32.dll", EntryPoint = "SetDllDirectoryW", SetLastError = true)]
		internal static extern bool SetDllDirectory(string lpPathName);

		//[DllImport("kernel32.dll")]
		//internal static extern long GetTickCount64();

		[DllImport("kernel32.dll")]
		internal static extern bool QueryUnbiasedInterruptTime(out long UnbiasedTime);

		[DllImport("kernel32.dll", EntryPoint = "CreateEventW", SetLastError = true)]
		internal static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetEvent(IntPtr hEvent);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

		//[DllImport("kernel32.dll")]
		//internal static extern uint SignalObjectAndWait(IntPtr hObjectToSignal, IntPtr hObjectToWaitOn, uint dwMilliseconds, bool bAlertable);
		//note: don't know why, this often is much slower than setevent/waitforsingleobject.

		[DllImport("kernel32.dll")] //note: no SetLastError = true
		internal static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentThread();

		[DllImport("kernel32.dll")]
		internal static extern uint GetCurrentThreadId();

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll")]
		internal static extern uint GetCurrentProcessId();

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr CreateFileMapping(IntPtr hFile, SECURITY_ATTRIBUTES* lpFileMappingAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

		//[DllImport("kernel32.dll", EntryPoint = "OpenFileMappingW", SetLastError = true)]
		//internal static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, LPARAM dwNumberOfBytesToMap);

		//[DllImport("kernel32.dll", SetLastError = true)]
		//internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetProcessWorkingSetSize(IntPtr hProcess, LPARAM dwMinimumWorkingSetSize, LPARAM dwMaximumWorkingSetSize);

		[DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true)]
		internal static extern IntPtr GetModuleHandle(string name);
		//see also Util.Misc.GetModuleHandleOf(Type|Assembly).

		[DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", SetLastError = true)]
		internal static extern IntPtr LoadLibrary(string lpLibFileName);

		[DllImport("kernel32.dll", BestFitMapping = false, SetLastError = true)]
		internal static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		/// <summary>
		/// Gets dll module handle (Api.GetModuleHandle) or loads dll (Api.LoadLibrary), and returns unmanaged exported function address (Api.GetProcAddress).
		/// See also: GetDelegate.
		/// </summary>
		/// <param name="dllName"></param>
		/// <param name="funcName"></param>
		internal static IntPtr GetProcAddress(string dllName, string funcName)
		{
			IntPtr hmod = GetModuleHandle(dllName);
			if(hmod == default(IntPtr)) { hmod = LoadLibrary(dllName); if(hmod == default(IntPtr)) return hmod; }

			return GetProcAddress(hmod, funcName);
		}

		internal const uint PROCESS_TERMINATE = 0x0001;
		internal const uint PROCESS_CREATE_THREAD = 0x0002;
		internal const uint PROCESS_SET_SESSIONID = 0x0004;
		internal const uint PROCESS_VM_OPERATION = 0x0008;
		internal const uint PROCESS_VM_READ = 0x0010;
		internal const uint PROCESS_VM_WRITE = 0x0020;
		internal const uint PROCESS_DUP_HANDLE = 0x0040;
		internal const uint PROCESS_CREATE_PROCESS = 0x0080;
		internal const uint PROCESS_SET_QUOTA = 0x0100;
		internal const uint PROCESS_SET_INFORMATION = 0x0200;
		internal const uint PROCESS_QUERY_INFORMATION = 0x0400;
		internal const uint PROCESS_SUSPEND_RESUME = 0x0800;
		internal const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
		internal const uint PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFFF;
		internal const uint DELETE = 0x00010000;
		internal const uint READ_CONTROL = 0x00020000;
		internal const uint WRITE_DAC = 0x00040000;
		internal const uint WRITE_OWNER = 0x00080000;
		internal const uint SYNCHRONIZE = 0x00100000;
		internal const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
		internal const uint STANDARD_RIGHTS_READ = READ_CONTROL;
		internal const uint STANDARD_RIGHTS_WRITE = READ_CONTROL;
		internal const uint STANDARD_RIGHTS_EXECUTE = READ_CONTROL;
		internal const uint STANDARD_RIGHTS_ALL = 0x001F0000;

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		[DllImport("kernel32.dll", EntryPoint = "GetLongPathNameW", SetLastError = true)]
		internal static extern uint GetLongPathName(string lpszShortPath, [Out] StringBuilder lpszLongPath, uint cchBuffer);

		[DllImport("kernel32.dll", EntryPoint = "GetFullPathNameW", SetLastError = true)]
		internal static extern uint GetFullPathName(string lpFileName, uint nBufferLength, [Out] StringBuilder lpBuffer, char** lpFilePart);

		internal const uint TH32CS_SNAPHEAPLIST = 0x00000001;
		internal const uint TH32CS_SNAPPROCESS = 0x00000002;
		internal const uint TH32CS_SNAPTHREAD = 0x00000004;
		internal const uint TH32CS_SNAPMODULE = 0x00000008;
		internal const uint TH32CS_SNAPMODULE32 = 0x00000010;

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

		//[DllImport("kernel32.dll", SetLastError = true)]
		//internal static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		//[DllImport("kernel32.dll", SetLastError = true)]
		//internal static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		//internal struct PROCESSENTRY32
		//{
		//	public uint dwSize;
		//	public uint cntUsage;
		//	public uint th32ProcessID;
		//	public IntPtr th32DefaultHeapID;
		//	public uint th32ModuleID;
		//	public uint cntThreads;
		//	public uint th32ParentProcessID;
		//	public int pcPriClassBase;
		//	public uint dwFlags;
		//	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		//	public string szExeFile;
		//};

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

		internal const uint PAGE_NOACCESS = 0x1;
		internal const uint PAGE_READONLY = 0x2;
		internal const uint PAGE_READWRITE = 0x4;
		internal const uint PAGE_WRITECOPY = 0x8;
		internal const uint PAGE_EXECUTE = 0x10;
		internal const uint PAGE_EXECUTE_READ = 0x20;
		internal const uint PAGE_EXECUTE_READWRITE = 0x40;
		internal const uint PAGE_EXECUTE_WRITECOPY = 0x80;
		internal const uint PAGE_GUARD = 0x100;
		internal const uint PAGE_NOCACHE = 0x200;
		internal const uint PAGE_WRITECOMBINE = 0x400;

		internal const uint MEM_COMMIT = 0x1000;
		internal const uint MEM_RESERVE = 0x2000;
		internal const uint MEM_DECOMMIT = 0x4000;
		internal const uint MEM_RELEASE = 0x8000;
		internal const uint MEM_RESET = 0x80000;
		internal const uint MEM_TOP_DOWN = 0x100000;
		internal const uint MEM_WRITE_WATCH = 0x200000;
		internal const uint MEM_PHYSICAL = 0x400000;
		internal const uint MEM_RESET_UNDO = 0x1000000;
		internal const uint MEM_LARGE_PAGES = 0x20000000;

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr VirtualAlloc(IntPtr lpAddress, LPARAM dwSize, uint flAllocationType = MEM_COMMIT | MEM_RESERVE, uint flProtect = PAGE_EXECUTE_READWRITE);

		[DllImport("kernel32.dll")]
		internal static extern bool VirtualFree(IntPtr lpAddress, LPARAM dwSize = default(LPARAM), uint dwFreeType = MEM_RELEASE);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, LPARAM dwSize, uint flAllocationType = MEM_COMMIT | MEM_RESERVE, uint flProtect = PAGE_EXECUTE_READWRITE);

		[DllImport("kernel32.dll")]
		internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, LPARAM dwSize = default(LPARAM), uint dwFreeType = MEM_RELEASE);

		internal const uint FILE_ATTRIBUTE_READONLY = 0x1;
		internal const uint FILE_ATTRIBUTE_HIDDEN = 0x2;
		internal const uint FILE_ATTRIBUTE_SYSTEM = 0x4;
		internal const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
		internal const uint FILE_ATTRIBUTE_ARCHIVE = 0x20;
		//internal const uint FILE_ATTRIBUTE_DEVICE = 0x40; //reserved for system
		internal const uint FILE_ATTRIBUTE_NORMAL = 0x80;
		internal const uint FILE_ATTRIBUTE_TEMPORARY = 0x100;
		internal const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x200;
		internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;
		internal const uint FILE_ATTRIBUTE_COMPRESSED = 0x800;
		internal const uint FILE_ATTRIBUTE_OFFLINE = 0x1000;
		internal const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x2000;
		internal const uint FILE_ATTRIBUTE_ENCRYPTED = 0x4000;
		internal const uint FILE_ATTRIBUTE_INTEGRITY_STREAM = 0x8000;
		//internal const uint FILE_ATTRIBUTE_VIRTUAL = 0x10000; //reserved for system
		internal const uint FILE_ATTRIBUTE_NO_SCRUB_DATA = 0x20000;
		//internal const uint FILE_ATTRIBUTE_EA = 0x40000; //undocumented
		internal const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

		[DllImport("kernel32.dll", EntryPoint = "GetFileAttributesW", SetLastError = true)]
		internal static extern uint GetFileAttributes(string lpFileName);

		[DllImport("kernel32.dll", EntryPoint = "SetFileAttributesW", SetLastError = true)]
		internal static extern bool SetFileAttributes(string lpFileName, uint dwFileAttributes);

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		internal struct WIN32_FILE_ATTRIBUTE_DATA
		{
			public uint dwFileAttributes;
			public long ftCreationTime;
			public long ftLastAccessTime;
			public long ftLastWriteTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
		}

		[DllImport("kernel32.dll", EntryPoint = "GetFileAttributesExW", SetLastError = true)]
		internal static extern bool GetFileAttributesEx(string lpFileName, int zero, out WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

		[DllImport("kernel32.dll", EntryPoint = "SearchPathW", SetLastError = true)]
		internal static extern uint SearchPath(string lpPath, string lpFileName, string lpExtension, uint nBufferLength, [Out] StringBuilder lpBuffer, IntPtr lpFilePart);

		internal const uint BASE_SEARCH_PATH_ENABLE_SAFE_SEARCHMODE = 0x1;
		internal const uint BASE_SEARCH_PATH_DISABLE_SAFE_SEARCHMODE = 0x10000;
		internal const uint BASE_SEARCH_PATH_PERMANENT = 0x8000;

		[DllImport("kernel32.dll")]
		internal static extern bool SetSearchPathMode(uint Flags);

		internal const uint SEM_FAILCRITICALERRORS = 0x1;

		[DllImport("kernel32.dll")]
		internal static extern uint SetErrorMode(uint uMode);

		[DllImport("kernel32.dll")]
		internal static extern uint GetErrorMode();

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern IntPtr LocalAlloc(uint uFlags, LPARAM uBytes);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr LocalFree(void* hMem);

		[DllImport("kernel32.dll", EntryPoint = "lstrcpynW")]
		internal static extern char* lstrcpyn(char* sTo, char* sFrom, int sToBufferLength);

		[DllImport("kernel32.dll", EntryPoint = "lstrcpynW")]
		internal static extern char* lstrcpyn(char* sTo, string sFrom, int sToBufferLength);

		internal struct FILETIME
		{
			public uint dwLowDateTime;
			public uint dwHighDateTime;
		}








		//ADVAPI32

		internal const uint TOKEN_WRITE = STANDARD_RIGHTS_WRITE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT;
		internal const uint TOKEN_SOURCE_LENGTH = 8;
		internal const uint TOKEN_READ = STANDARD_RIGHTS_READ | TOKEN_QUERY;
		internal const uint TOKEN_QUERY_SOURCE = 16;
		internal const uint TOKEN_QUERY = 8;
		internal const uint TOKEN_IMPERSONATE = 4;
		internal const uint TOKEN_EXECUTE = STANDARD_RIGHTS_EXECUTE;
		internal const uint TOKEN_DUPLICATE = 2;
		internal const uint TOKEN_AUDIT_SUCCESS_INCLUDE = 1;
		internal const uint TOKEN_AUDIT_SUCCESS_EXCLUDE = 2;
		internal const uint TOKEN_AUDIT_FAILURE_INCLUDE = 4;
		internal const uint TOKEN_AUDIT_FAILURE_EXCLUDE = 8;
		internal const uint TOKEN_ASSIGN_PRIMARY = 1;
		internal const uint TOKEN_ALL_ACCESS_P = STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT;
		internal const uint TOKEN_ALL_ACCESS = TOKEN_ALL_ACCESS_P | TOKEN_ADJUST_SESSIONID;
		internal const uint TOKEN_ADJUST_SESSIONID = 256;
		internal const uint TOKEN_ADJUST_PRIVILEGES = 32;
		internal const uint TOKEN_ADJUST_GROUPS = 64;
		internal const uint TOKEN_ADJUST_DEFAULT = 128;

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

		internal enum TOKEN_INFORMATION_CLASS
		{
			TokenUser = 1,
			TokenGroups,
			TokenPrivileges,
			TokenOwner,
			TokenPrimaryGroup,
			TokenDefaultDacl,
			TokenSource,
			TokenType,
			TokenImpersonationLevel,
			TokenStatistics,
			TokenRestrictedSids,
			TokenSessionId,
			TokenGroupsAndPrivileges,
			TokenSessionReference,
			TokenSandBoxInert,
			TokenAuditPolicy,
			TokenOrigin,
			TokenElevationType,
			TokenLinkedToken,
			TokenElevation,
			TokenHasRestrictions,
			TokenAccessInformation,
			TokenVirtualizationAllowed,
			TokenVirtualizationEnabled,
			TokenIntegrityLevel,
			TokenUIAccess,
			TokenMandatoryPolicy,
			TokenLogonSid,
			//Win8
			TokenIsAppContainer,
			TokenCapabilities,
			TokenAppContainerSid,
			TokenAppContainerNumber,
			TokenUserClaimAttributes,
			TokenDeviceClaimAttributes,
			TokenRestrictedUserClaimAttributes,
			TokenRestrictedDeviceClaimAttributes,
			TokenDeviceGroups,
			TokenRestrictedDeviceGroups,
			TokenSecurityAttributes,
			TokenIsRestricted,
			TokenProcessTrustLevel,
			TokenPrivateNameSpace,
			MaxTokenInfoClass  // MaxTokenInfoClass should always be the last enum
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		internal static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, void* TokenInformation, uint TokenInformationLength, out uint ReturnLength);

		[DllImport("advapi32.dll")]
		internal static extern byte* GetSidSubAuthorityCount(IntPtr pSid);

		[DllImport("advapi32.dll")]
		internal static extern uint* GetSidSubAuthority(IntPtr pSid, uint nSubAuthority);

		[DllImport("advapi32.dll")]
		internal static extern int RegSetValueEx(IntPtr hKey, string lpValueName, int Reserved, Microsoft.Win32.RegistryValueKind dwType, void* lpData, int cbData);

		[DllImport("advapi32.dll")]
		internal static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, IntPtr Reserved, out Microsoft.Win32.RegistryValueKind dwType, void* lpData, ref int cbData);

		internal struct ACL
		{
			public byte AclRevision;
			public byte Sbz1;
			public ushort AclSize;
			public ushort AceCount;
			public ushort Sbz2;
		}

		internal struct SECURITY_DESCRIPTOR
		{
			public byte Revision;
			public byte Sbz1;
			public ushort Control;
			public IntPtr Owner;
			public IntPtr Group;
			public ACL* Sacl;
			public ACL* Dacl;
		}

		internal struct SECURITY_ATTRIBUTES
		{
			public uint nLength;
			public SECURITY_DESCRIPTOR* lpSecurityDescriptor;
			public int bInheritHandle;
		}

		[DllImport("advapi32.dll", EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW", SetLastError = true)]
		internal static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, uint StringSDRevision, out SECURITY_DESCRIPTOR* SecurityDescriptor, uint* SecurityDescriptorSize = null);

		//[DllImport("advapi32.dll", EntryPoint = "ConvertSecurityDescriptorToStringSecurityDescriptorW")]
		//internal static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(SECURITY_DESCRIPTOR* SecurityDescriptor, uint RequestedStringSDRevision, uint SecurityInformation, out char* StringSecurityDescriptor, out uint StringSecurityDescriptorLen);








		//SHELL32

		//[DllImport("shell32.dll")]
		//internal static extern bool IsUserAnAdmin();

		internal const uint SHGFI_ICON = 0x000000100;     // get icon;
		internal const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name;
		internal const uint SHGFI_TYPENAME = 0x000000400;     // get type name;
		internal const uint SHGFI_ATTRIBUTES = 0x000000800;     // get attributes;
		internal const uint SHGFI_ICONLOCATION = 0x000001000;     // get icon location;
		internal const uint SHGFI_EXETYPE = 0x000002000;     // return exe type;
		internal const uint SHGFI_SYSICONINDEX = 0x000004000;     // get system icon index;
		internal const uint SHGFI_LINKOVERLAY = 0x000008000;     // put a link overlay on icon;
		internal const uint SHGFI_SELECTED = 0x000010000;     // show icon in selected state;
		internal const uint SHGFI_ATTR_SPECIFIED = 0x000020000;     // get only specified attributes;
		internal const uint SHGFI_LARGEICON = 0x000000000;     // get large icon;
		internal const uint SHGFI_SMALLICON = 0x000000001;     // get small icon;
		internal const uint SHGFI_OPENICON = 0x000000002;     // get open icon;
		internal const uint SHGFI_SHELLICONSIZE = 0x000000004;     // get shell size icon;
		internal const uint SHGFI_PIDL = 0x000000008;     // pszPath is a pidl;
		internal const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute;
		internal const uint SHGFI_ADDOVERLAYS = 0x000000020;     // apply the appropriate overlays;
		internal const uint SHGFI_OVERLAYINDEX = 0x000000040;     // Get the index of the overlay;

		internal struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

		[DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW")]
		internal static extern LPARAM SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

		[DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW")]
		internal static extern LPARAM SHGetFileInfo(IntPtr pidl, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

		[DllImport("shell32.dll", PreserveSig = true)]
		internal static extern int SHGetDesktopFolder(out IShellFolder ppshf);

		[DllImport("shell32.dll")]
		internal static extern int SHParseDisplayName(string pszName, IntPtr pbc, out IntPtr pidl, uint sfgaoIn, uint* psfgaoOut);

		[DllImport("shell32.dll", PreserveSig = true)]
		internal static extern int SHGetNameFromIDList(IntPtr pidl, Native.SIGDN sigdnName, out string ppszName);

		[DllImport("shell32.dll", PreserveSig = true)]
		internal static extern int SHBindToParent(IntPtr pidl, ref Guid riid, out IShellFolder ppv, out IntPtr ppidlLast);

		[DllImport("shell32.dll", PreserveSig = true)]
		internal static extern int SHGetPropertyStoreForWindow(Wnd hwnd, ref Guid riid, out IPropertyStore ppv);

		internal static PROPERTYKEY PKEY_AppUserModel_ID = new PROPERTYKEY() { fmtid = new Guid(0x9F4C2855, 0x9F79, 0x4B39, 0xA8, 0xD0, 0xE1, 0xD4, 0x2D, 0xE1, 0xD5, 0xF3), pid = 5 };

		[DllImport("shell32.dll")]
		internal static extern IntPtr* CommandLineToArgvW(string lpCmdLine, out int pNumArgs);

		internal struct NOTIFYICONDATA
		{
			public uint cbSize;
			public Wnd hWnd;
			public uint uID;
			public uint uFlags;
			public uint uCallbackMessage;
			public IntPtr hIcon;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szTip;
			public uint dwState;
			public uint dwStateMask;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string szInfo;

			[StructLayout(LayoutKind.Explicit)]
			public struct TYPE_1
			{
				[FieldOffset(0)]
				public uint uTimeout;
				[FieldOffset(0)]
				public uint uVersion;
			}
			public TYPE_1 _11;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string szInfoTitle;
			public uint dwInfoFlags;
			public Guid guidItem;
			public IntPtr hBalloonIcon;
		}

		internal const uint NIN_SELECT = 0x400;
		internal const uint NINF_KEY = 0x1;
		internal const uint NIN_KEYSELECT = 0x401;
		internal const uint NIN_BALLOONSHOW = 0x402;
		internal const uint NIN_BALLOONHIDE = 0x403;
		internal const uint NIN_BALLOONTIMEOUT = 0x404;
		internal const uint NIN_BALLOONUSERCLICK = 0x405;
		internal const uint NIN_POPUPOPEN = 0x406;
		internal const uint NIN_POPUPCLOSE = 0x407;
		internal const uint NIM_ADD = 0x0;
		internal const uint NIM_MODIFY = 0x1;
		internal const uint NIM_DELETE = 0x2;
		internal const uint NIM_SETFOCUS = 0x3;
		internal const uint NIM_SETVERSION = 0x4;
		internal const int NOTIFYICON_VERSION = 3;
		internal const int NOTIFYICON_VERSION_4 = 4;
		internal const uint NIF_MESSAGE = 0x1;
		internal const uint NIF_ICON = 0x2;
		internal const uint NIF_TIP = 0x4;
		internal const uint NIF_STATE = 0x8;
		internal const uint NIF_INFO = 0x10;
		internal const uint NIF_GUID = 0x20;
		internal const uint NIF_REALTIME = 0x40;
		internal const uint NIF_SHOWTIP = 0x80;
		internal const uint NIS_HIDDEN = 0x1;
		internal const uint NIS_SHAREDICON = 0x2;
		internal const uint NIIF_NONE = 0x0;
		internal const uint NIIF_INFO = 0x1;
		internal const uint NIIF_WARNING = 0x2;
		internal const uint NIIF_ERROR = 0x3;
		internal const uint NIIF_USER = 0x4;
		internal const uint NIIF_ICON_MASK = 0xF;
		internal const uint NIIF_NOSOUND = 0x10;
		internal const uint NIIF_LARGE_ICON = 0x20;
		internal const uint NIIF_RESPECT_QUIET_TIME = 0x80;

		[DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")]
		internal static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

		//internal struct SHSTOCKICONINFO
		//{
		//	public uint cbSize;
		//	public IntPtr hIcon;
		//	public int iSysImageIndex;
		//	public int iIcon;
		//	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		//	public string szPath;
		//}

		internal struct SHSTOCKICONINFO
		{
			public uint cbSize;
			public IntPtr hIcon;
			public int iSysImageIndex;
			public int iIcon;
			public fixed char szPath[260];
		}

		[DllImport("shell32.dll", PreserveSig = true)]
		internal static extern int SHGetStockIconInfo(Native.SHSTOCKICONID siid, uint uFlags, ref SHSTOCKICONINFO psii);

		[DllImport("shell32.dll", EntryPoint = "#6", PreserveSig = true)]
		internal static extern int SHDefExtractIcon(string pszIconFile, int iIndex, uint uFlags, IntPtr* phiconLarge, IntPtr* phiconSmall, uint nIconSize);

		internal const int SHIL_LARGE = 0;
		internal const int SHIL_SMALL = 1;
		internal const int SHIL_EXTRALARGE = 2;
		internal const int SHIL_SYSSMALL = 3;
		internal const int SHIL_JUMBO = 4;

		//[DllImport("shell32.dll", EntryPoint = "#727", PreserveSig = true)]
		//internal static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppvObj);
		[DllImport("shell32.dll", EntryPoint = "#727", PreserveSig = true)]
		internal static extern int SHGetImageList(int iImageList, ref Guid riid, out IntPtr ppvObj);

		internal const uint SHCNE_RENAMEITEM = 0x1;
		internal const uint SHCNE_CREATE = 0x2;
		internal const uint SHCNE_DELETE = 0x4;
		internal const uint SHCNE_MKDIR = 0x8;
		internal const uint SHCNE_RMDIR = 0x10;
		internal const uint SHCNE_MEDIAINSERTED = 0x20;
		internal const uint SHCNE_MEDIAREMOVED = 0x40;
		internal const uint SHCNE_DRIVEREMOVED = 0x80;
		internal const uint SHCNE_DRIVEADD = 0x100;
		internal const uint SHCNE_NETSHARE = 0x200;
		internal const uint SHCNE_NETUNSHARE = 0x400;
		internal const uint SHCNE_ATTRIBUTES = 0x800;
		internal const uint SHCNE_UPDATEDIR = 0x1000;
		internal const uint SHCNE_UPDATEITEM = 0x2000;
		internal const uint SHCNE_SERVERDISCONNECT = 0x4000;
		internal const uint SHCNE_UPDATEIMAGE = 0x8000;
		internal const uint SHCNE_DRIVEADDGUI = 0x10000;
		internal const uint SHCNE_RENAMEFOLDER = 0x20000;
		internal const uint SHCNE_FREESPACE = 0x40000;
		internal const uint SHCNE_EXTENDED_EVENT = 0x4000000;
		internal const uint SHCNE_ASSOCCHANGED = 0x8000000;
		internal const uint SHCNE_DISKEVENTS = 0x2381F;
		internal const uint SHCNE_GLOBALEVENTS = 0xC0581E0;
		internal const uint SHCNE_ALLEVENTS = 0x7FFFFFFF;
		internal const uint SHCNE_INTERRUPT = 0x80000000;

		internal const uint SHCNF_IDLIST = 0x0;
		internal const uint SHCNF_DWORD = 0x3;
		internal const uint SHCNF_PATH = 0x5;
		internal const uint SHCNF_PRINTER = 0x6;
		internal const uint SHCNF_FLUSH = 0x1000;
		internal const uint SHCNF_FLUSHNOWAIT = 0x3000;
		internal const uint SHCNF_NOTIFYRECURSIVE = 0x10000;

		[DllImport("shell32.dll")]
		internal static extern void SHChangeNotify(uint wEventId, uint uFlags, string dwItem1, string dwItem2);





		//SHLWAPI

		[DllImport("shlwapi.dll", EntryPoint = "PathIsURLW")]
		internal static extern bool PathIsURL(string pszPath);

		//internal enum ASSOCSTR
		//{
		//	ASSOCSTR_COMMAND = 1,
		//	ASSOCSTR_EXECUTABLE,
		//	ASSOCSTR_FRIENDLYDOCNAME,
		//	ASSOCSTR_FRIENDLYAPPNAME,
		//	ASSOCSTR_NOOPEN,
		//	ASSOCSTR_SHELLNEWVALUE,
		//	ASSOCSTR_DDECOMMAND,
		//	ASSOCSTR_DDEIFEXEC,
		//	ASSOCSTR_DDEAPPLICATION,
		//	ASSOCSTR_DDETOPIC,
		//	ASSOCSTR_INFOTIP,
		//	ASSOCSTR_QUICKTIP,
		//	ASSOCSTR_TILEINFO,
		//	ASSOCSTR_CONTENTTYPE,
		//	ASSOCSTR_DEFAULTICON,
		//	ASSOCSTR_SHELLEXTENSION,
		//	ASSOCSTR_DROPTARGET,
		//	ASSOCSTR_DELEGATEEXECUTE,
		//	ASSOCSTR_SUPPORTED_URI_PROTOCOLS,
		//	ASSOCSTR_PROGID,
		//	ASSOCSTR_APPID,
		//	ASSOCSTR_APPPUBLISHER,
		//	ASSOCSTR_APPICONREFERENCE,
		//	ASSOCSTR_MAX
		//}

		//[DllImport("shlwapi.dll", PreserveSig = true, EntryPoint = "AssocQueryStringW")]
		//internal static extern int AssocQueryString(uint flags, ASSOCSTR str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, ref uint pcchOut);






		//COMCTL32

		[DllImport("comctl32.dll")]
		internal static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, uint flags);

		[DllImport("comctl32.dll")]
		internal static extern bool ImageList_GetIconSize(IntPtr himl, out int cx, out int cy);









		//OLE32

		[DllImport("ole32.dll", PreserveSig = true)]
		internal static extern int PropVariantClear(ref PROPVARIANT_LPARAM pvar);








		//MSVCRT

		[DllImport("msvcrt.dll", EntryPoint = "wcstol", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int strtoi(char* s, out char* endPtr, int numberBase = 0);

		internal static uint strtoui(char* s, out char* endPtr, int numberBase = 0)
		{
			long k = strtoi64(s, out endPtr, numberBase);
			return k < -int.MaxValue ? 0u : (k > uint.MaxValue ? uint.MaxValue : (uint)k);
		}
		//note: don't use the u API because they return 1 if the value is too big and the string contains '-'.
		//[DllImport("msvcrt.dll", EntryPoint = "wcstoul", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern uint strtoui(char* s, out char* endPtr, int _base = 0);
		[DllImport("msvcrt.dll", EntryPoint = "_wcstoui64", CallingConvention = CallingConvention.Cdecl)]
		internal static extern ulong strtoui64(char* s, out char* endPtr, int _base = 0);

		/// <summary>
		/// This overload has different parameter types.
		/// </summary>
		[DllImport("msvcrt.dll", EntryPoint = "_wcstoi64", CallingConvention = CallingConvention.Cdecl)]
		internal static extern long strtoi64(char* s, out char* endPtr, int numberBase = 0);
		//info: ntdll also has wcstol, wcstoul, _wcstoui64, but not _wcstoi64.

		/// <summary>
		/// Converts part of string to int.
		/// Returns the int value.
		/// Returns 0 if the string is null, "" or does not begin with a number; then numberEndIndex will be = startIndex.
		/// Returns int.MaxValue or int.MinValue if the value is not in int range; then numberEndIndex will also include all number characters that follow the valid part.
		/// </summary>
		/// <param name="s">String.</param>
		/// <param name="startIndex">Offset in string where to start parsing.</param>
		/// <param name="numberEndIndex">Receives offset in string where the number part ends.</param>
		/// <param name="numberBase">If 0, parses the string as hexadecimal number if begins with "0x", as octal if begins with "0", else as decimal. Else it can be 2 to 36. Examples: 10 - parse as decimal (don't support "0x" etc); 16 - as hexadecimal (eg returns 26 if string is "1A" or "0x1A"); 2 - as binary (eg returns 5 if string is "101").</param>
		/// <exception cref="ArgumentOutOfRangeException">startIndex is invalid.</exception>
		internal static int strtoi(string s, int startIndex, out int numberEndIndex, int numberBase = 0)
		{
			int R = 0, len = s == null ? 0 : s.Length - startIndex;
			if(len < 0) throw new ArgumentOutOfRangeException("startIndex");
			if(len != 0)
				fixed (char* p = s) {
					char* t = p + startIndex, e = t;
					R = strtoi(t, out e, numberBase);
					len = (int)(e - t);
				}
			numberEndIndex = startIndex + len;
			return R;
		}

		/// <summary>
		/// Converts part of string to uint.
		/// Returns the uint value.
		/// Returns 0 if the string is null, "" or does not begin with a number; then numberEndIndex will be = startIndex.
		/// Returns uint.MaxValue (0xffffffff) or uint.MinValue (0) if the value is not in uint range; then numberEndIndex will also include all number characters that follow the valid part.
		/// Supports negative number values -1 to -int.MaxValue, for example converts string "-1" to 0xffffffff.
		/// </summary>
		/// <param name="s">String.</param>
		/// <param name="startIndex">Offset in string where to start parsing.</param>
		/// <param name="numberEndIndex">Receives offset in string where the number part ends.</param>
		/// <param name="numberBase">If 0, parses the string as hexadecimal number if begins with "0x", as octal if begins with "0", else as decimal. Else it can be 2 to 36. Examples: 10 - parse as decimal (don't support "0x" etc); 16 - as hexadecimal (eg returns 26 if string is "1A" or "0x1A"); 2 - as binary (eg returns 5 if string is "101").</param>
		/// <exception cref="ArgumentOutOfRangeException">startIndex is invalid.</exception>
		internal static uint strtoui(string s, int startIndex, out int numberEndIndex, int numberBase = 0)
		{
			uint R = 0;
			int len = s == null ? 0 : s.Length - startIndex;
			if(len < 0) throw new ArgumentOutOfRangeException("startIndex");
			if(len != 0)
				fixed (char* p = s) {
					char* t = p + startIndex, e = t;
					R = strtoui(t, out e, numberBase);
					len = (int)(e - t);
				}
			numberEndIndex = startIndex + len;
			return R;
		}

		/// <summary>
		/// Converts part of string to long.
		/// Returns the long value.
		/// Returns 0 if the string is null, "" or does not begin with a number; then numberEndIndex will be = startIndex.
		/// Returns long.MaxValue or long.MinValue if the value is not in long range; then numberEndIndex will also include all number characters that follow the valid part.
		/// </summary>
		/// <param name="s">String.</param>
		/// <param name="startIndex">Offset in string where to start parsing.</param>
		/// <param name="numberEndIndex">Receives offset in string where the number part ends.</param>
		/// <param name="numberBase">If 0, parses the string as hexadecimal number if begins with "0x", as octal if begins with "0", else as decimal. Else it can be 2 to 36. Examples: 10 - parse as decimal (don't support "0x" etc); 16 - as hexadecimal (eg returns 26 if string is "1A" or "0x1A"); 2 - as binary (eg returns 5 if string is "101").</param>
		/// <exception cref="ArgumentOutOfRangeException">startIndex is invalid.</exception>
		internal static long strtoi64(string s, int startIndex, out int numberEndIndex, int numberBase = 0)
		{
			long R = 0;
			int len = s == null ? 0 : s.Length - startIndex;
			if(len < 0) throw new ArgumentOutOfRangeException("startIndex");
			if(len != 0)
				fixed (char* p = s) {
					char* t = p + startIndex, e = t;
					R = strtoi64(t, out e, numberBase);
					len = (int)(e - t);
				}
			numberEndIndex = startIndex + len;
			return R;
		}

		/// <summary>
		/// This overload does not have parameter 'out int numberEndIndex'.
		/// </summary>
		internal static int strtoi(string s, int startIndex = 0, int numberBase = 0)
		{
			int len;
			return strtoi(s, startIndex, out len, numberBase);
		}

		/// <summary>
		/// This overload does not have parameter 'out int numberEndIndex'.
		/// </summary>
		internal static uint strtoui(string s, int startIndex = 0, int numberBase = 0)
		{
			int len;
			return strtoui(s, startIndex, out len, numberBase);
		}

		/// <summary>
		/// This overload does not have parameter 'out int numberEndIndex'.
		/// </summary>
		internal static long strtoi64(string s, int startIndex = 0, int numberBase = 0)
		{
			int len;
			return strtoi64(s, startIndex, out len, numberBase);
		}

		/// <summary>
		/// This overload does not have parameter 'out char* endPtr'.
		/// </summary>
		internal static int strtoi(char* s, int numberBase = 0)
		{
			char* endPtr;
			return strtoi(s, out endPtr, numberBase);
		}

		/// <summary>
		/// This overload does not have parameter 'out char* endPtr'.
		/// </summary>
		internal static uint strtoui(char* s, int numberBase = 0)
		{
			char* endPtr;
			return strtoui(s, out endPtr, numberBase);
		}

		/// <summary>
		/// This overload does not have parameter 'out char* endPtr'.
		/// </summary>
		internal static long strtoi64(char* s, int numberBase = 0)
		{
			char* endPtr;
			return strtoi64(s, out endPtr, numberBase);
		}




		//DWMAPI

		[DllImport("dwmapi.dll")]
		internal static extern int DwmGetWindowAttribute(Wnd hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
		[DllImport("dwmapi.dll")]
		internal static extern int DwmGetWindowAttribute(Wnd hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);





		//OTHER

		[DllImport("uxtheme.dll", PreserveSig = true)]
		internal static extern int SetWindowTheme(Wnd hwnd, string pszSubAppName, string pszSubIdList);





		//UTIL

		internal static bool GetDelegate<T>(out T deleg, string dllName, string funcName) where T : class
		{
			deleg = null;
			IntPtr fa = GetProcAddress(dllName, funcName); if(fa == default(IntPtr)) return false;
			//deleg = (T)Marshal.GetDelegateForFunctionPointer(fa, typeof(T)); //error
			Type t = typeof(T);
			deleg = (T)Convert.ChangeType(Marshal.GetDelegateForFunctionPointer(fa, t), t);
			return deleg != null;
		}

		internal static bool GetDelegate<T>(out T deleg, IntPtr hModule, string funcName) where T : class
		{
			deleg = null;
			IntPtr fa = GetProcAddress(hModule, funcName); if(fa == default(IntPtr)) return false;
			//deleg = (T)Marshal.GetDelegateForFunctionPointer(fa, typeof(T)); //error
			Type t = typeof(T);
			deleg = (T)Convert.ChangeType(Marshal.GetDelegateForFunctionPointer(fa, t), t);
			return deleg != null;
		}
	}
}