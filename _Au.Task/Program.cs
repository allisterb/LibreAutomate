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

using Au;
using Au.Types;

[module: DefaultCharSet(CharSet.Unicode)]

//note: All this info is with Framework. Less tested with Core.
//PROBLEM: slow startup.
//When Au.dll not ngened, a minimal script starts in 98 ms. Else in 150 ms.
//One of reasons is: when Au.dll ngened, together with it is always loaded System.dll and System.Core.dll, even if not used.
//	Don't know why. Didn't find a way to avoid it. Loading Au.dll with Assembly.LoadFrom does not help (loads ngened anyway).
//	Also then no AppDomain.AssemblyLoad event for these two .NET assemblies.
//	Luckily other assemblies used by Au.dll are not loaded when not used.
//	The same is for our-compiled .exe files.
//Workaround:
//	Preload this process. Let it wait for next task.
//	Then a script can start in ~20 ms.
//	We don't preload first time or if exe. Also not faster if several scripts are started without a delay. Never mind.

//Smaller problem: many threads.
//Initially 4 or 7 treads. After 20-30 s becomes 5 (+1 or -2). With [STAThread] would be +2.

//PROBLEM: preloaded task's windows start inactive, behind one or more windows. Unless they activate self, like ADialog.
//	It does not depend on the foreground lock setting/API. The setting/API just enable SetForegroundWindow, but most windows don't call it.
//Workaround: use CBT hook. It receives HCBT_ACTIVATE even when the window does not become the foreground window.
//	On HCBT_ACTIVATE, async-call SetForegroundWindow. Also, editor calls AllowSetForegroundWindow before starting task.

static unsafe class Program
{
	//[STAThread] //we use TrySetApartmentState instead
	static void Main(string[] args)
	{
		string asmFile, fullPathRefs; int pdbOffset, flags;

		if(args.Length != 1) return;
		string pipeName = args[0]; //if(!pipeName.Starts(@"\\.\pipe\Au.Task-")) return;

		int nr = 0;
#if false
			//With NamedPipeClientStream faster by 1 ms, because don't need to JIT. But problems:
			//1. Loads System and System.Core immediately, making slower startup.
			//2. This process does not end when editor process ended, because then Connect spin-waits for server created.
			using(var pipe = new NamedPipeClientStream(".", pipeName.Substring(9), PipeDirection.In)) {
				pipe.Connect();
				var b = new byte[10000];
				nr=pipe.Read(b, 0, b.Length);
			}
#else
		//APerf.First();
		//_PrepareTest();
		//APerf.NW();

		for(int i = 0; i < 3; i++) {
			if(Api.WaitNamedPipe(pipeName, i == 2 ? -1 : 100)) break;
			if(Marshal.GetLastWin32Error() != Api.ERROR_SEM_TIMEOUT) return;
			//APerf.First();
			switch(i) {
			case 0: _Prepare1(); break; //~25 ms with cold CPU
			//case 1: _Prepare2(); break; //~15 ms with cold CPU
			}
			//APerf.NW();
		}
		//APerf.First();
		using(var pipe = Api.CreateFile(pipeName, Api.GENERIC_READ, 0, default, Api.OPEN_EXISTING, 0)) {
			if(pipe.Is0) { ADebug.PrintNativeError_(); return; }
			//APerf.Next();
			int size; if(!Api.ReadFile(pipe, &size, 4, out nr, default) || nr != 4) return;
			//APerf.Next();
			if(!Api.ReadFileArr(pipe, out var b, size, out nr) || nr != size) return;
			//APerf.Next();

			//ADebug.PrintLoadedAssemblies(true, true);
			//APerf.First();

			var a = Au.Util.Serializer_.Deserialize(b);
			ATask.Init_(ATRole.MiniProgram, a[0]);
			asmFile = a[1]; pdbOffset = a[2]; flags = a[3]; args = a[4]; fullPathRefs = a[5];
			string wrp = a[6]; if(wrp != null) Environment.SetEnvironmentVariable("ATask.WriteResult.pipe", wrp);
			AFolders.Workspace = (string)a[7];
		}
#endif
		//APerf.Next();

		bool mtaThread = 0 != (flags & 2); //app without [STAThread]
		if(mtaThread == s_isSTA) _SetComApartment(mtaThread ? ApartmentState.MTA : ApartmentState.STA);

		if(0 != (flags & 4)) Api.AllocConsole(); //meta console true

		//if(0 != (flags & 1)) { //hasConfig
		//	var config = asmFile + ".config";
		//	if(AFile.ExistsAsFile(config, true)) AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", config);
		//}

		if(s_hook == null) _Hook();

		//APerf.Next();
		try { RunAssembly.Run(asmFile, args, pdbOffset, fullPathRefs: fullPathRefs); }
		catch(Exception ex) { AOutput.Write(ex); }
		finally { s_hook?.Dispose(); }
	}

	//[MethodImpl(MethodImplOptions.NoInlining)]
	//static void _PrepareTest()
	//{
	//	_ = typeof(Stack<string>).Assembly; //System.Collections. 0.7 ms
	//}

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void _Prepare1()
	{
		_SetComApartment(ApartmentState.STA);

		//JIT slowest-to-JIT methods
		//APerf.First();
		//if(!Au.Util.Assembly_.IsAuNgened) {
			Au.Util.AJit.Compile(typeof(RunAssembly), nameof(RunAssembly.Run));
			Au.Util.AJit.Compile(typeof(Au.Util.Serializer_), "Deserialize");
			AFile.WaitIfLocked(() => (FileStream)null);
		//}
		//APerf.NW(); //Core ~15 ms

		//Core assembly loading is fast, but let's save several ms anyway
		_ = typeof(Stack<string>).Assembly; //System.Collections

		using(var stream = AFile.WaitIfLocked(() => File.OpenRead(Assembly.GetExecutingAssembly().Location))) stream.ReadByte(); //Core is not JIT-ed therefore opens file first time much slower than Framework. //TODO: too dirty

		_Hook();
	}

	//rejected
	//[MethodImpl(MethodImplOptions.NoInlining)]
	//static void _Prepare2()
	//{
	//	_ = typeof(System.Windows.Forms.Control).Assembly; //System.Windows.Forms, System.Drawing.Primitives, System.ComponentModel.Primitives
	//	//SetProcessWorkingSetSize(Api.GetCurrentProcess(), -1, -1); //makes starting slower
	//}
	////[DllImport("kernel32.dll")]
	////internal static extern bool SetProcessWorkingSetSize(IntPtr hProcess, LPARAM dwMinimumWorkingSetSize, LPARAM dwMaximumWorkingSetSize);

	[MethodImpl(MethodImplOptions.NoInlining)]
	static void _SetComApartment(ApartmentState state)
	{
		Thread.CurrentThread.TrySetApartmentState(ApartmentState.Unknown);
		Thread.CurrentThread.TrySetApartmentState(state);
		s_isSTA = state == ApartmentState.STA;

		//This is undocumented, but works if we set ApartmentState.Unknown at first.
		//With [STAThread] the process initially has +2 threads. Also now slightly faster.
	}
	static bool s_isSTA;

	static void _Hook()
	{
		s_hook = AHookWin.ThreadCbt(m => {
			//AOutput.Write(m.code, m.wParam, m.lParam);
			//switch(m.code) {
			//case HookData.CbtEvent.ACTIVATE:
			//case HookData.CbtEvent.SETFOCUS:
			//	AOutput.Write((AWnd)m.wParam);
			//	AOutput.Write(AWnd.Active);
			//	AOutput.Write(AWnd.ThisThread.Active);
			//	AOutput.Write(AWnd.Focused);
			//	AOutput.Write(AWnd.ThisThread.Focused);
			//	break;
			//}
			if(m.code == HookData.CbtEvent.ACTIVATE) {
				var w = (AWnd)m.wParam;
				if(!w.HasExStyle(WS2.NOACTIVATE)) {
					//AOutput.Write(w);
					//AOutput.Write(w.ExStyle);
					//Api.SetForegroundWindow(w); //does not work
					ATimer.After(1, _ => {
						if(s_hook == null) return;
						//AOutput.Write(AWnd.Active);
						//AOutput.Write(AWnd.ThisThread.Active);
						bool isActive = w == AWnd.Active, activate = !isActive && w == AWnd.ThisThread.Active;
						if(isActive || activate) { s_hook.Dispose(); s_hook = null; }
						if(activate) {
							Api.SetForegroundWindow(w);
							//w.ActivateLL(); //no, it's against Windows rules, and works differently with meta outputPath
							//Before starting task, editor calls AllowSetForegroundWindow. But if clicked etc a window after that:
							//	SetForegroundWindow fails always or randomly;
							//	Activate[LL] fails if that window is of higher UAC IL, unless the foreground lock timeout is 0.
						}
					});
				}
			}
			return false;
		});
	}
	static AHookWin s_hook;

}
