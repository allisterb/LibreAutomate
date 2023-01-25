
using System.Runtime.Loader;

//PROBLEM: slow startup.
//A minimal script starts in 70-100 ms cold, 40 hot.
//Workaround for role miniProgram:
//	Preload task process. Let it wait for next task. While waiting, it also can JIT etc.
//	Then starts in 12/4 ms (cold/hot). With script.setup 15/5.
//	Except first time. Also not faster if several scripts are started without a delay. Never mind.
//	This is implemented in this class and in Au.AppHost (just ~10 code lines added in 1 place).

/*
//To test task startup speed, use script "task startup speed.cs":

300.ms(); //give time to preload new task process
for (int i = 0; i < 5; i++) {
//	perf.cpu();
//	perf.shared.First(); //slower
	var t=perf.ms.ToS(); t=perf.ms.ToS();
	script.run(@"miniProgram.cs", t); //cold 10, hot 3. Without Setup: 6/2. Slower on vmware Win7+Avast.
//	script.run(@"exeProgram.cs", t); //cold 80, hot 43. Slightly slower on vmware Win7+Avast.
	600.ms(); //give time for the process to exit
}

//miniProgram.cs and exeProgram.cs:

print.it(perf.ms-Int64.Parse(args[0]));
*/

//Smaller problem: .NET creates many threads. No workaround.

//PROBLEM: preloaded task's windows start inactive, behind one or more windows. Unless they activate self, like dialog.
//	It does not depend on the foreground lock setting/API. The setting/API just enable SetForegroundWindow, but most windows don't call it.
//	Workaround: use CBT hook. It receives HCBT_ACTIVATE even when the window does not become the foreground window.
//		On HCBT_ACTIVATE, async-call SetForegroundWindow. Also, editor calls AllowSetForegroundWindow before starting task.

//PROBLEM: although Main() starts fast, but the process ends slowly, because of .NET.
//	Eg if starting an empty script every <50 ms, sometimes cannot start.

//FUTURE: option to start without preloading.

namespace Au.More;

/// <summary>
/// Prepares to quickly start and execute a script with role miniProgram in this preloaded task process.
/// </summary>
static unsafe class MiniProgram_ {
	//static long s_started;
	internal static string s_scriptId;

	struct _TaskInit {
		public IntPtr asmFile;
		public IntPtr* args;
		public int nArgs;
	}

	/// <summary>
	/// Called by apphost.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoOptimization)]
	static void Init(nint pn, out _TaskInit r) {
		r = default;
		string pipeName = new((char*)pn);

		script.role = SRole.MiniProgram;

		process.ThisThreadSetComApartment_(ApartmentState.STA); //1.5 ms

		script.AppModuleInit_(); //3 ms

		//rejected. Now this is implemented in editor. To detect when failed uses process exit code. Never mind exception text, it is not very useful.
		//process.thisProcessExit += e => { //0.9 ms
		//	if (s_started != 0) print.TaskEvent_(e == null ? "TE" : "TF " + e.ToStringWithoutStack(), s_started);
		//};

#if true
		if (!Api.WaitNamedPipe(pipeName, -1)) return;
#else
//rejected: JIT some functions in other thread. Now everything much faster than with old .NET.
Speed of p1: with this 2500, without 5000 (slow Deserialize JIT).

		for (int i = 0; ; i++) {
			if (Api.WaitNamedPipe(pipeName, i == 1 ? -1 : 25)) break;
			if (Marshal.GetLastWin32Error() != Api.ERROR_SEM_TIMEOUT) return;
			if (i == 1) break;

			//rejected: ProfileOptimization. Now everything is JIT-ed and is as fast as can be.

			run.thread(() => {
				//using var p2 = perf.local();

				//JIT
				Jit_.Compile(typeof(Serializer_), "Deserialize");
				//tested: now Api functions fast, don't JIT.
				//p2.Next();
				Jit_.Compile(typeof(script), nameof(script.setup), "_AuxThread");
				//p2.Next();

				//Thread.Sleep(20);
				//p2.Next();
				//"Au".ToLowerInvariant(); //15-40 ms //now <1 ms

				//if need to preload some assemblies, use code like this. But now .NET loads assemblies fast, not like in old framework.
				//_ = typeof(TypeFromAssembly).Assembly;
			}, sta: false);
		}
#endif

		//Debug_.PrintLoadedAssemblies(true, true);

		//using var p1 = perf.local();
		using var pipe = Api.CreateFile(pipeName, Api.GENERIC_READ, 0, Api.OPEN_EXISTING, 0);
		if (pipe.Is0) { Debug_.PrintNativeError_(); return; }
		//p1.Next();
		int size; if (!Api.ReadFile(pipe, &size, 4, out int nr, default) || nr != 4) return;
		if (!Api.ReadFileArr(pipe, out var b, size, out nr) || nr != size) return;
		//p1.Next();
		var a = Serializer_.Deserialize(b);
		//p1.Next('d');
		var flags = (EFlags)(int)a[2];

		r.asmFile = Marshal.StringToCoTaskMemUTF8(a[1]);
		//p1.Next();
		string[] args = a[3];
		if (!args.NE_()) {
			r.nArgs = args.Length;
			r.args = (IntPtr*)Marshal.AllocHGlobal(args.Length * sizeof(IntPtr));
			for (int i = 0; i < args.Length; i++) r.args[i] = Marshal.StringToCoTaskMemUTF8(args[i]);
		}
		//p1.Next();

		s_scriptId = a[6];
		script.s_wndMsg = (wnd)(int)a[8];
		script.s_wrPipeName = a[4];

		if (0 != (flags & EFlags.FromEditor)) script.testing = true;
		if (0 != (flags & EFlags.IsPortable)) ScriptEditor.IsPortable = true;

		folders.Editor = new(folders.ThisApp);
		folders.Workspace = new(a[5]);

		if (0 != (flags & EFlags.RefPaths))
			AssemblyLoadContext.Default.Resolving += (alc, an)
				=> ResolveAssemblyFromRefPathsAttribute_(alc, an, Assembly.GetEntryAssembly());

		if (0 != (flags & EFlags.NativePaths))
			AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, dll)
				=> ResolveUnmanagedDllFromNativePathsAttribute_(dll, Assembly.GetEntryAssembly());

		if (0 != (flags & EFlags.MTA))
			process.ThisThreadSetComApartment_(ApartmentState.MTA);

		if (0 != (flags & EFlags.Console)) {
			Api.AllocConsole();
		} else {
			if (0 != (flags & EFlags.RedirectConsole)) print.redirectConsoleOutput = true;
			//Compiler adds this flag if the script uses System.Console assembly.
			//Else new users would not know how to test code examples with Console.WriteLine found on the internet.
		}

		script.Starting_(a[0], a[7]);
		//p1.Next();

		//Api.QueryPerformanceCounter(out s_started);
		//print.TaskEvent_("TS", s_started);
	}

	//for assemblies used in miniProgram and editorExtension scripts
	internal static Assembly ResolveAssemblyFromRefPathsAttribute_(AssemblyLoadContext alc, AssemblyName an, Assembly scriptAssembly) {
		//print.it("managed", an);
		//note: don't cache GetCustomAttribute/split results. It's many times faster than LoadFromAssemblyPath and JIT.
		var attr = scriptAssembly.GetCustomAttribute<RefPathsAttribute>();
		if (attr != null) {
			string name = an.Name;
			foreach (var v in attr.Paths.Split('|')) {
				//print.it(v);
				int iName = v.Length - name.Length - 4;
				if (iName <= 0 || v[iName - 1] != '\\' || !v.Eq(iName, name, true)) continue;
				if (!filesystem.exists(v).File) continue;
				//try {
				return alc.LoadFromAssemblyPath(v);
				//} catch(Exception ex) { Debug_.Print(ex.ToStringWithoutStack()); break; }
			}
		}
		return null;
	}

	//for assemblies used in miniProgram and editorExtension scripts
	internal static IntPtr ResolveUnmanagedDllFromNativePathsAttribute_(string name, Assembly scriptAssembly) {
		//print.it("native", name);
		var attr = scriptAssembly.GetCustomAttribute<NativePathsAttribute>();
		if (attr != null) {
			if (!name.Ends(".dll", true)) name += ".dll";
			foreach (var v in attr.Paths.Split('|')) {
				//print.it(v);
				if (!v.Ends(name, true) || !v.Eq(v.Length - name.Length - 1, '\\')) continue;
				if (NativeLibrary.TryLoad(v, out var h)) return h;
			}
		}
		return default;
	}

	/// <summary>
	/// Used by exeProgram.
	/// </summary>
	/// <param name="rootDir">Directory that may contain subdir "runtimes".</param>
	internal static void ResolveNugetRuntimes_(string rootDir) {
		var runtimesDir = pathname.combine(rootDir, "runtimes");
		if (!filesystem.exists(runtimesDir).Directory) return;

		//This code is similar as in Compiler._GetDllPaths:_AddGroup. There we get paths from XML, here from filesystem.

		int verPC = osVersion.minWin10 ? 100 : osVersion.minWin8_1 ? 81 : osVersion.minWin8 ? 80 : 70; //don't need Win11

		var flags = FEFlags.AllDescendants | FEFlags.IgnoreInaccessible | FEFlags.NeedRelativePaths | FEFlags.UseRawPath;
		List<(FEFile f, int ver)> aNet = new(), aNative = new();
		foreach (var f in filesystem.enumFiles(runtimesDir, "*.dll", flags)) {
			var s = f.Name;
			if (!s.Starts(@"\win", true) || s.Length < 10) continue;

			int i = 4, verDll = 0;
			if (s[i] != '-') {
				verDll = s.ToInt(i, out i);
				if (verDll != 81) verDll *= 10;
				if (verDll > verPC) continue;
			}

			if (s.Eq(i, osVersion.is32BitProcess ? @"-x64\" : @"-x86\", true)) continue;

			var a = s.Eq(i + 5, @"native\", true) ? aNative : aNet;
			a.Add((f, verDll));
		}

		var dr = _Do(aNet);
		var dn = _Do(aNative);

		static Dictionary<string, string> _Do(List<(FEFile f, int ver)> a) {
			if (a.Count == 0) return null;
			Dictionary<string, string> d = null;
			foreach (var group in a.ToLookup(o => pathname.getNameNoExt(o.f.Name), StringComparer.OrdinalIgnoreCase)) {
				//print.it($"<><c blue>{group.Key}<>");

				int verBest = -1;
				string sBest = null;
				foreach (var (f, verDll) in group) {
					if (verDll > verBest) {
						verBest = verDll;
						sBest = f.FullPath;
					}
				}

				if (sBest != null) {
					//print.it(sBest);
					d ??= new(StringComparer.OrdinalIgnoreCase);
					d[group.Key] = sBest;
				}
			}

			return d;
		}

		if (dr != null) AssemblyLoadContext.Default.Resolving += (alc, an) => {
			//print.it("lib", an.Name);
			if (!dr.TryGetValue(an.Name, out var path)) return null;
			return alc.LoadFromAssemblyPath(path);
		};
		if (dn != null) AssemblyLoadContext.Default.ResolvingUnmanagedDll += (_, name) => {
			//print.it("native", name);
			if (name.Ends(".dll", true)) name = name[..^4];
			if (!dn.TryGetValue(name, out var path)) return default;
			if (!NativeLibrary.TryLoad(path, out var r)) return default;
			return r;
		};
	}

	[Flags]
	public enum EFlags {
		/// <summary>Has [RefPaths] attribute. It is when using meta r or nuget.</summary>
		RefPaths = 1,

		/// <summary>Main() with [MTAThread].</summary>
		MTA = 2,

		/// <summary>Has meta console true.</summary>
		Console = 4,

		/// <summary>Uses System.Console assembly.</summary>
		RedirectConsole = 8,

		/// <summary>Has [NativePaths] attribute. It is when using nuget packages with native dlls.</summary>
		NativePaths = 16,

		/// <summary>Started from editor with the Run button or menu command. Used for <see cref="script.testing"/>.</summary>
		FromEditor = 32,

		/// <summary>Started from portable editor.</summary>
		IsPortable = 64,

		//Config = 256, //meta hasConfig
	}
}