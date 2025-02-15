namespace Au.More {
	/// <summary>
	/// Functions useful to debug code.
	/// </summary>
	/// <remarks>
	/// The Debug_.PrintX functions write to the same output as <see cref="print.it"/>, not to the trace listeners like <see cref="Debug.Print(string)"/> etc do. Also they add caller's name, file and line number.
	/// Functions Print, PrintIf, PrintFunc and Dialog work only if DEBUG is defined, which normally is when the caller project is in Debug configuration. Else they are not called, and arguments not evaluated at run time. This is because they have [<see cref="ConditionalAttribute"/>("DEBUG")].
	/// Note: when used in a library, the above functions depend on DEBUG of the library project and not on DEBUG of the consumer project of the library. For example, the library may be in Release configuration even if its consumer project is in Debug configuration. If your library wants to show some info only if its consumer project is in Debug config, instead you can use code like <c>if(opt.warnings.Verbose) print.warning("text");</c>; see <see cref="print.warning"/>, opt.warnings.<see cref="OWarnings.Verbose"/>.
	/// </remarks>
	internal static class Debug_ //FUTURE: make public, when will be more tested and if really need.
	{
		static void _Print(object text, string f_, int l_, string m_) {
			string s = print.util.toString(text);
			string prefix = null; if (s.Starts("<>")) { prefix = "<>"; s = s[2..]; }
			s = $"{prefix}Debug: {m_} ({pathname.getName(f_)}:{l_}):  {s}";
			_Print2(s);
		}
		
		static void _Print2(object o) {
			string s = o?.ToString();
			if (UseQM2) print.qm2.write(s); else print.it(s);
		}
		
		internal static bool UseQM2;
		
		/// <summary>
		/// Calls <see cref="print.it"/> to show some debug info. Also shows current function name/file/line.
		/// Works only if DEBUG is defined. Read more in class help.
		/// The 3 optional arguments are not used explicitly.
		/// If text starts with "&lt;&gt;", it can contain output tags.
		/// </summary>
		[Conditional("DEBUG")]
		public static void Print(object text, [CallerFilePath] string f_ = null, [CallerLineNumber] int l_ = 0, [CallerMemberName] string m_ = null)
			=> _Print(text, f_, l_, m_);
		
		/// <summary>
		/// If condition is true, calls <see cref="print.it"/> to show some debug info. Also shows current function name/file/line.
		/// Works only if DEBUG is defined. Read more in class help.
		/// If <i>text</i> null, uses [CallerArgumentExpression("condition")]. Other optional parameters are not used explicitly.
		/// If text starts with "&lt;&gt;", it can contain output tags.
		/// </summary>
		[Conditional("DEBUG")]
		public static void PrintIf(bool condition, object text = null, [CallerFilePath] string f_ = null, [CallerLineNumber] int l_ = 0, [CallerMemberName] string m_ = null, [CallerArgumentExpression("condition")] string ae_ = null) {
			if (condition) _Print(text ?? ae_, f_, l_, m_);
		}
		
		/// <summary>
		/// Calls <see cref="print.it"/> with current function name.
		/// Works only if DEBUG is defined. Read more in class help.
		/// The optional argument is not used explicitly.
		/// </summary>
		[Conditional("DEBUG")]
		public static void PrintFunc([CallerMemberName] string m_ = null)
			=> _Print2(m_);
		
		/// <summary>
		/// In DEBUG config prints lastError.message. Only if condition true (default).
		/// </summary>
		[Conditional("DEBUG")]
		internal static void PrintNativeError_(bool condition = true, [CallerFilePath] string f_ = null, [CallerLineNumber] int l_ = 0, [CallerMemberName] string m_ = null) {
			if (condition) _Print(lastError.message, f_, l_, m_);
		}
		
		/// <summary>
		/// In DEBUG config prints lastError.messageFor(code).
		/// </summary>
		[Conditional("DEBUG")]
		internal static void PrintNativeError_(int code, [CallerFilePath] string f_ = null, [CallerLineNumber] int l_ = 0, [CallerMemberName] string m_ = null)
			=> _Print(lastError.messageFor(code), f_, l_, m_);
		
		/// <summary>
		/// Calls <see cref="dialog.show"/> to show some debug info.
		/// Works only if DEBUG is defined. Read more in class help.
		/// The 3 optional arguments are not used explicitly.
		/// </summary>
		[Conditional("DEBUG")]
		public static void Dialog(object text, [CallerFilePath] string f_ = null, [CallerLineNumber] int l_ = 0, [CallerMemberName] string m_ = null) {
			string s = print.util.toString(text);
			dialog.show("Debug", s, flags: DFlags.ExpandDown, expandedText: $"{m_} ({pathname.getName(f_)}:{l_})");
		}
		
		//rejected: use if(opt.warnings.Verbose) dialog.showWarning(...). It adds stack trace.
		///// <summary>
		///// If opt.warnings.<see cref="OWarnings.Verbose"/> == true, calls <see cref="dialog.show"/> with text and stack trace.
		///// Read more in class help.
		///// </summary>
		//[MethodImpl(MethodImplOptions.NoInlining)]
		//public static void DialogOpt(string text)
		//{
		//	if(!opt.warnings.Verbose) return;
		//	var x = new StackTrace(1, true);
		//	dialog.show("Debug", text, flags: DFlags.ExpandDown | DFlags.Wider, expandedText: x.ToString());
		//}
		
		//rejected: Not used in this library. Not useful for debug because don't show the stack trace. Instead use print.warning; it supports prefix "Debug: ", "Note: ", "Info :"; it also supports disabling warnings etc.
		///// <summary>
		///// If opt.warnings.<see cref="OWarnings.Verbose"/> == true, calls <see cref="print.it(string)"/>.
		///// Read more in class help.
		///// </summary>
		//public static void PrintOpt(string text)
		//{
		//	if(opt.warnings.Verbose) _Print("Debug: " + text);
		//}
		
		//rejected: Don't need multiple warning functions. Now print.warning does not show more than 1 warning/second if opt.warnings.Verbose is false.
		///// <summary>
		///// If opt.warnings.<see cref="OWarnings.Verbose"/> == true, calls <see cref="print.warning"/>.
		///// Read more in class help.
		///// </summary>
		//[MethodImpl(MethodImplOptions.NoInlining)]
		//public static void WarningOpt(string text)
		//{
		//	if(opt.warnings.Verbose) print.warning(text, 1);
		//}
		
		/// <summary>
		/// Checks flags and throws ArgumentException if some flags are invalid. The error message includes valid flag names.
		/// </summary>
		/// <param name="flags">Flags to check.</param>
		/// <param name="goodFlags">Valid flags.</param>
		/// <remarks>
		/// Can be used in functions that have an enum flags parameter but not all passed flags are valid for that function or object state.
		/// Does nothing if opt.warnings.<see cref="OWarnings.Verbose"/> == false.
		/// When flags are valid, this function is fast.
		/// </remarks>
		internal static unsafe void CheckFlagsOpt_<T>(T flags, T goodFlags) where T : unmanaged, Enum {
			//FUTURE: if this is really often useful, make it public. If not used - remove.
			
			Debug.Assert(sizeof(T) == 4);
			int a = *(int*)&flags;
			int b = *(int*)&goodFlags;
			if (a != (a & b)) _CheckFlagsOpt(typeof(T), b);
		}
		
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void _CheckFlagsOpt(Type t, int goodFlags) {
			if (!opt.warnings.Verbose) return;
			if (!t.IsEnum) throw new ArgumentException("Bad type.");
			var s = new StringBuilder("Invalid flags. Only these flags can be used: "); bool added = false;
			for (int i = 1; i != 0; i <<= 1) {
				if (0 == (i & goodFlags)) continue;
				if (added) s.Append(", "); else added = true;
				s.Append(t.GetEnumName(i));
			}
			s.Append('.');
			//print.warning(s.ToString(), 1);
			throw new ArgumentException(s.ToString());
		}
		
		/// <summary>
		/// Returns true if using Debug configuration of Au.dll.
		/// </summary>
		public static bool IsAuDebugConfiguration {
			get {
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}
		
		//CONSIDER: move the MemoryX functions to perf as public.
		
#if TRACE
		/// <summary>
		/// Calls Marshal.AddRef(obj), then calls/returns Marshal.Release(obj).
		/// Available if TRACE defined.
		/// </summary>
		internal static int GetComObjRefCount_(IntPtr obj) {
			Marshal.AddRef(obj);
			return Marshal.Release(obj);
		}
		
		/// <summary>
		/// Returns managed memory size as formatted string. Uses GC.GetTotalMemory.
		/// Available if TRACE defined.
		/// </summary>
		/// <param name="fromAnchor">Get the difference from previous call to <b>MemorySetAnchor_</b>.</param>
		internal static string MemoryGet_(bool fromAnchor = true) {
			var mem = GC.GetTotalMemory(false);
			//if(s_mem0 == 0) s_mem0 = mem;
			if (fromAnchor) mem -= s_mem0;
			return (mem / 1024d / 1024d).ToS("F3");
		}
		static long s_mem0;
		
		/// <summary>
		/// Prints managed memory size. Uses GC.GetTotalMemory.
		/// Available if TRACE defined.
		/// </summary>
		/// <param name="fromAnchor">Get the difference from previous call to <b>MemorySetAnchor_</b>.</param>
		internal static void MemoryPrint_(bool fromAnchor = true) => _Print2(MemoryGet_(fromAnchor));
		
		/// <summary>
		/// Memorizes current managed memory size, so that next call to another <b>MemoryX</b> function with fromAnchor=true (default) will get memory size difference from current memory size.
		/// Available if TRACE defined.
		/// </summary>
		internal static void MemorySetAnchor_() { s_mem0 = GC.GetTotalMemory(false); }
		
		/// <summary>
		/// Temporarily suspends GC collections if possible. Restores in <b>Dispose</b>.
		/// Available if TRACE defined.
		/// </summary>
		internal struct NoGcRegion : IDisposable {
			bool _restore, _print;
			
			/// <summary>
			/// Suspends GC collections if possible.
			/// Does nothing in 32-bit process.
			/// </summary>
			/// <param name="memSize">Recommended 100_000_000. Still works if 200_000_000, but fails if 300_000_000. Not tested in 32-bit process.</param>
			/// <param name="print">Let <b>Dispose</b> print size of managed memory added since ctor.</param>
			public NoGcRegion(long memSize, bool print = true) {
				_restore = false;
				_print = print;
				if (osVersion.is32BitProcess) return;
				if (_print) Debug_.MemorySetAnchor_();
				//print.it(System.Runtime.GCSettings.LatencyMode);
				try { _restore = GC.TryStartNoGCRegion(memSize); }
				catch (InvalidOperationException ex) { Debug_.Print(ex.Message); }
			}
			
			/// <summary>
			/// Restores suspended GC collections.
			/// </summary>
			public void Dispose() {
				if (_restore) {
					_restore = false;
					//print.it(System.Runtime.GCSettings.LatencyMode == System.Runtime.GCLatencyMode.NoGCRegion);
					//if(System.Runtime.GCSettings.LatencyMode == System.Runtime.GCLatencyMode.NoGCRegion) GC.EndNoGCRegion();
					try { GC.EndNoGCRegion(); } //note: need to call even if not in nogc region (then exception); else TryStartNoGCRegion will throw exception.
					catch (InvalidOperationException ex) { Debug_.Print(ex.Message); }
					if (_print) Debug_.MemoryPrint_();
					ThreadPool.QueueUserWorkItem(_ => GC.Collect());
				}
			}
		}
		
		/// <summary>
		/// Prints assemblies already loaded or/and loaded in the future.
		/// Works in Release too, if TRACE defined.
		/// </summary>
		internal static void PrintLoadedAssemblies(bool now, bool future, bool stackTrace = false) {
			if (now) {
				var a = AppDomain.CurrentDomain.GetAssemblies();
				_Print2("-- now --");
				foreach (var v in a) _Print2("# " + v.FullName);
			}
			if (future) {
				if (stackTrace) new StackTrace(1, true); //load assemblies used by stack trace
				_Print2("-- future --");
				AppDomain.CurrentDomain.AssemblyLoad += (object sender, AssemblyLoadEventArgs e) => {
					_Print2("# " + e.LoadedAssembly.FullName);
					if (stackTrace) _Print2(new StackTrace(1, true));
				};
				//var stack = new Stack<string>();
			}
		}
#endif
	}
}
