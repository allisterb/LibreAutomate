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

namespace Au.Types
{
	[DebuggerStepThrough]
	internal static unsafe partial class Cpp
	{
		static Cpp()
		{
			AFile.More.LoadDll64or32Bit("AuCpp.dll");
		}

		//speed:
		//	Calling DllImport functions is 4-5 times slower than C# functions.
		//	Calling COM functions is 2-10 times slower than DllImport functions.
		//	Tested with int and string parameters, with default marshaling and with 'fixed'.
		//	If only int parameters, DllImport is only 50% slower than C#. But COM slow anyway.
		//	Strings passed to COM methods by default are converted to BSTR, and a new BSTR is allocated/freed.

		internal struct Cpp_Acc
		{
			public IntPtr acc;
			public int elem;
			public AAcc.Misc_ misc;

			public Cpp_Acc(IntPtr iacc, int elem_) { acc = iacc; elem = elem_; misc = default; }
			public Cpp_Acc(AAcc a) { acc = a._iacc; elem = a._elem; misc = a._misc; }
			public static implicit operator Cpp_Acc(AAcc a) => new Cpp_Acc(a);
		}

		internal delegate int AccCallbackT(Cpp_Acc a);

		internal struct Cpp_AccParams
		{
			string _role, _name, _prop;
			int _roleLength, _nameLength, _propLength;
			public AFFlags flags;
			public int skip;
			char resultProp; //AAcc.Finder.RProp

			public Cpp_AccParams(string role, string name, string prop, AFFlags flags, int skip, char resultProp) : this()
			{
				if(role != null) { _role = role; _roleLength = role.Length; }
				if(name != null) { _name = name; _nameLength = name.Length; }
				if(prop != null) { _prop = prop; _propLength = prop.Length; }
				this.flags = flags;
				this.skip = skip;
				this.resultProp = resultProp;
			}
		}

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern EError Cpp_AccFind(AWnd w, Cpp_Acc* aParent, in Cpp_AccParams ap, AccCallbackT also, out Cpp_Acc aResult, [MarshalAs(UnmanagedType.BStr)] out string sResult);

		internal enum EError
		{
			NotFound = 0x1001, //AO not found. With FindAll - no errors. This is actually not an error.
			InvalidParameter = 0x1002, //invalid parameter, for example wildcard expression (or regular expression in it)
			WindowClosed = 0x1003, //the specified window handle is invalid or the window was destroyed while injecting
			WaitChromeDisabled = 0x1004, //need to wait while enabling Chrome AOs finished
		}

		internal static bool IsCppError(int hr)
		{
			return hr >= (int)EError.NotFound && hr <= (int)EError.WaitChromeDisabled;
		}

		/// <summary>
		/// flags: 1 not inproc, 2 get only name.
		/// </summary>
		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccFromWindow(int flags, AWnd w, AccOBJID objId, out Cpp_Acc aResult, out BSTR sResult);

		//flags: 1 get UIA, 2 prefer LINK.
		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccFromPoint(POINT p, AXYFlags flags, out Cpp_Acc aResult);

		//flags: 1 get UIA.
		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccGetFocused(AWnd w, int flags, out Cpp_Acc aResult);

		//These are called from AAcc class functions like Cpp.Cpp_Func(this, ...); GC.KeepAlive(this);.
		//We can use 'this' because Cpp_Acc has an implicit conversion from AAcc operator.
		//Need GC.KeepAlive(this) everywhere. Else GC can collect the AAcc (and release _iacc) while in the Cpp func.
		//Alternatively could make the Cpp parameter 'const Cpp_Acc&', and pass AAcc directly. But I don't like it.

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccNavigate(Cpp_Acc aFrom, string navig, out Cpp_Acc aResult);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccGetProp(Cpp_Acc a, char prop, out BSTR sResult);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccWeb(Cpp_Acc a, string what, out BSTR sResult);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccGetRect(Cpp_Acc a, out RECT r);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccGetRole(Cpp_Acc a, out AccROLE roleInt, out BSTR roleStr);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccGetInt(Cpp_Acc a, char what, out int R);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccAction(Cpp_Acc a, char action, [MarshalAs(UnmanagedType.BStr)] string param = null);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccSelect(Cpp_Acc a, AccSELFLAG flagsSelect);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccGetSelection(Cpp_Acc a, out BSTR sResult);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_AccGetProps(Cpp_Acc a, string props, out BSTR sResult);

#if DEBUG
		internal static void DebugUnload()
		{
			//run GC to release Firefox acc wrappers. Else may not unload from Firefox.
			GC.Collect();
			GC.WaitForPendingFinalizers();
			//Cpp_Unload(0); //in Setup32.dll, it's 32-bit
			AFile.Run(@"Q:\app\Au\Other\Programs\unload AuCpp dll.exe", null, RFlags.WaitForExit); //loads Setup32.dll and calls Cpp_Unload
		}
#endif

		// STRING

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern char* Cpp_LowercaseTable();

		// OTHER

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr Cpp_Clipboard(IntPtr hh);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Cpp_CallIDroptarget(IntPtr dt, int ddEvent, [MarshalAs(UnmanagedType.IUnknown)] object d, int keyState, POINT pt, ref int pdwEffect);

		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern bool Cpp_ShellExec(in Api.SHELLEXECUTEINFO x, out int pid, out int injectError, out int execError);

		// TEST

		//FUTURE: remove tests
		[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern void Cpp_Test();

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern int* EnumWindowsEx(out int len, bool onlyVisible, int api);

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern LPARAM Cpp_InputSync(int action, int tid, LPARAM hh);

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern void Cpp_TestWildex(string s, string w);

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern int Cpp_TestInt(int a);

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern int Cpp_TestString(string a, int b, int c);

		//[ComImport, Guid("3426CF3C-F7C2-4322-A292-463DB8729B54"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		//internal interface ICppTest
		//{
		//	[PreserveSig] int TestInt(int a, int b, int c);
		//	[PreserveSig] int TestString([MarshalAs(UnmanagedType.LPWStr)] string a, int b, int c);
		//	[PreserveSig] int TestBSTR(string a, int b, int c);
		//}

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern ICppTest Cpp_Interface();


		//[ComImport, Guid("57017F56-E7CA-4A7B-A8F8-2AE36077F50D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		//internal interface IThreadExitEvent
		//{
		//	[PreserveSig] int Unsubscribe();
		//}

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern IThreadExitEvent Cpp_ThreadExitEvent(IntPtr callback);

		//[DllImport("AuCpp.dll", CallingConvention = CallingConvention.Cdecl)]
		//internal static extern void Cpp_ThreadExitEvent2(IntPtr callback);
	}
}
