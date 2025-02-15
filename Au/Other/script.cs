namespace Au;

/// <summary>
/// Script task functions. Run, get properties, set options, etc.
/// A script task is a running script, except if role editorExtension. Each script task is a separate process.
/// </summary>
/// <seealso cref="process"/>
public static class script {
	/// <summary>
	/// Gets the script name, like <c>"Script123"</c>.
	/// </summary>
	/// <remarks>
	/// If role miniProgram (default), returns the script file name without extension.
	/// Else returns <see cref="AppDomain.FriendlyName"/>, like <c>"MainAssemblyName"</c>.
	/// </remarks>
	public static string name => s_name ??= AppDomain.CurrentDomain.FriendlyName; //info: in framework 4 with ".exe", now without (now it is the entry assembly name)
	static string s_name;

	/// <summary>
	/// Gets the script role (miniProgram, exeProgram or editorExtension).
	/// </summary>
	public static SRole role { get; internal set; }

	/// <summary>
	/// Gets the script file path in the workspace.
	/// </summary>
	/// <remarks>
	/// The default compiler adds <see cref="PathInWorkspaceAttribute"/> to the main assembly. Then at run time this property returns its value. Returns null if compiled by some other compiler.
	/// </remarks>
	public static string path => s_pathInWorkspace ??= AssemblyUtil_.GetEntryAssembly()?.GetCustomAttribute<PathInWorkspaceAttribute>()?.Path;
	static string s_pathInWorkspace;
	//note: GetEntryAssembly returns null in func called by host through coreclr_create_delegate. 

	/// <summary>
	/// Returns true if this script task was started from editor with the Run button or menu command.
	/// Always false if role editorExtension.
	/// </summary>
	public static bool testing { get; internal set; }

	/// <summary>
	/// Returns true if the build configuration of the main assembly is Debug (default). Returns false if Release (optimize true).
	/// </summary>
	public static bool isDebug => s_debug ??= AssemblyUtil_.IsDebug(AssemblyUtil_.GetEntryAssembly());
	static bool? s_debug;
	//note: GetEntryAssembly returns null in func called by host through coreclr_create_delegate.

	/// <summary>
	/// Returns true if running in WPF preview mode.
	/// </summary>
	public static bool isWpfPreview {
		get {
			if (role != SRole.MiniProgram) return false;
			var s = Environment.CommandLine;
			//return s.Contains(" WPF_PREVIEW ") && s.RxIsMatch(@" WPF_PREVIEW (-?\d+) (-?\d+)$"); //slower JIT
			return s.Contains(" WPF_PREVIEW ") && _IsWpfPreview(s);

			//[MethodImpl(MethodImplOptions.NoInlining)]
			static bool _IsWpfPreview(string s) => s.RxIsMatch(@" WPF_PREVIEW (-?\d+) (-?\d+)$");

			//don't cache. It makes JIT slower. Now fast after JIT.
		}
	}

	#region run

	/// <summary>
	/// Starts executing a script. Does not wait.
	/// </summary>
	/// <param name="script">Script name like <c>"Script5.cs"</c>, or path like <c>@"\Folder\Script5.cs"</c>.</param>
	/// <param name="args">Command line arguments. In the script it will be variable <i>args</i>. Should not contain <c>'\0'</c> characters.</param>
	/// <returns>
	/// Native process id of the task process.
	/// Returns -1 if failed, for example if the script contains errors or cannot run second task instance.
	/// Returns 0 if task start is deferred because the script is running (ifRunning wait/wait_restart).
	/// If role editorExtension, waits until the script ends, then returns 0.
	/// </returns>
	/// <exception cref="FileNotFoundException">Script file not found.</exception>
	public static int run([ParamString(PSFormat.CodeFile)] string script, params string[] args)
		=> _Run(0, script, args, out _);

	/// <summary>
	/// Starts executing a script and waits until the task ends.
	/// </summary>
	/// <returns>The exit code of the task process. See <see cref="Environment.ExitCode"/>.</returns>
	/// <exception cref="FileNotFoundException">Script file not found.</exception>
	/// <exception cref="AuException">Failed to start script task, for example if the script contains errors or cannot start second task instance.</exception>
	/// <inheritdoc cref="run"/>
	public static int runWait([ParamString(PSFormat.CodeFile)] string script, params string[] args)
		=> _Run(1, script, args, out _);

	/// <summary>
	/// Starts executing a script, waits until the task ends and then gets <see cref="writeResult"/> text.
	/// </summary>
	/// <param name="results">Receives <see cref="writeResult"/> text.</param>
	/// <returns>The exit code of the task process. See <see cref="Environment.ExitCode"/>.</returns>
	/// <exception cref="FileNotFoundException">Script file not found.</exception>
	/// <exception cref="AuException">Failed to start script task, for example if the script contains errors or cannot start second task instance.</exception>
	/// <inheritdoc cref="run"/>
	public static int runWait(out string results, [ParamString(PSFormat.CodeFile)] string script, params string[] args)
		=> _Run(3, script, args, out results);

	/// <summary>
	/// Starts executing a script, waits until the task ends and gets <see cref="writeResult"/> text in real time.
	/// </summary>
	/// <param name="results">Receives <see cref="writeResult"/> output whenever the task calls it.</param>
	/// <returns>The exit code of the task process. See <see cref="Environment.ExitCode"/>.</returns>
	/// <exception cref="FileNotFoundException">Script file not found.</exception>
	/// <exception cref="AuException">Failed to start script task.</exception>
	/// <inheritdoc cref="run"/>
	public static int runWait(Action<string> results, [ParamString(PSFormat.CodeFile)] string script, params string[] args)
		=> _Run(3, script, args, out _, results);

	static int _Run(int mode, string script, string[] args, out string resultS, Action<string> resultA = null) {
		resultS = null;

		var w = ScriptEditor.WndMsg_; if (w.Is0) throw new AuException("Editor process not found.");
		//CONSIDER: run editor program, if installed

		bool wait = 0 != (mode & 1), needResult = 0 != (mode & 2);
		using var tr = new _TaskResults();
		if (needResult && !tr.Init()) throw new AuException("*get task results");

		var data = Serializer_.Serialize(script, args, tr.pipeName);
		int pid = (int)WndCopyData.Send<byte>(w, 100, data, mode);
		if (pid == 0) pid--; //RunResult_.failed

		switch ((RunResult_)pid) {
		case RunResult_.failed:
			return !wait ? -1 : throw new AuException("*start task");
		case RunResult_.notFound:
			throw new FileNotFoundException($"Script '{script}' not found.");
		case RunResult_.deferred: //possible only if !wait
		case RunResult_.editorThread: //the script ran sync and already returned
			return 0;
		}

		if (wait) {
			using var hProcess = WaitHandle_.FromProcessId(pid, Api.SYNCHRONIZE | Api.PROCESS_QUERY_LIMITED_INFORMATION);
			if (hProcess == null) throw new AuException("*wait for task");

			if (!needResult) hProcess.WaitOne(-1);
			else if (!tr.WaitAndRead(hProcess, resultA)) throw new AuException("*get task result");
			else if (resultA == null) resultS = tr.ResultString;

			if (!Api.GetExitCodeProcess(hProcess.SafeWaitHandle.DangerousGetHandle(), out pid)) pid = int.MinValue;
		}
		return pid;
	}

	//Called from editor's CommandLine. Almost same as _Run. Does not throw.
	internal static int RunCL_(wnd w, int mode, string script, string[] args, Action<string> resultA) {
		bool wait = 0 != (mode & 1), needResult = 0 != (mode & 2);
		using var tr = new _TaskResults();
		if (needResult && !tr.Init()) return (int)RunResult_.cannotGetResult;

		var data = Serializer_.Serialize(script, args, tr.pipeName);
		int pid = (int)WndCopyData.Send<byte>(w, 101, data, mode);
		if (pid == 0) pid--; //RunResult_.failed

		switch ((RunResult_)pid) {
		case RunResult_.failed:
		case RunResult_.notFound:
			return pid;
		case RunResult_.deferred: //possible only if !wait
		case RunResult_.editorThread: //the script ran sync and already returned. Ignore needResult, as it it auto-detected, not explicitly specified.
			return 0;
		}

		if (wait) {
			using var hProcess = WaitHandle_.FromProcessId(pid, Api.SYNCHRONIZE | Api.PROCESS_QUERY_LIMITED_INFORMATION);
			if (hProcess == null) return (int)RunResult_.cannotWait;

			if (!needResult) hProcess.WaitOne(-1);
			else if (!tr.WaitAndRead(hProcess, resultA)) return (int)RunResult_.cannotWaitGetResult;

			if (!Api.GetExitCodeProcess(hProcess.SafeWaitHandle.DangerousGetHandle(), out pid)) pid = int.MinValue;
		}
		return pid;
	}

	internal enum RunResult_ {
		//errors returned by sendmessage(wm_copydata)
		failed = -1, //script contains errors, or cannot run because of ifRunning, or sendmessage(wm_copydata) failed
		notFound = -2, //script not found
		deferred = -3, //script cannot run now, but will run later if don't need to wait. If need to wait, in such case cannot be deferred (then failed).
		editorThread = -4, //role editorExtension

		//other errors
		noEditor = -5,
		cannotWait = -6,
		cannotGetResult = -7,
		cannotWaitGetResult = -8,
	}

	unsafe struct _TaskResults : IDisposable {
		Handle_ _hPipe;
		public string pipeName;
		string _s;
		StringBuilder _sb;

		public bool Init() {
			var tid = Api.GetCurrentThreadId();
			pipeName = @"\\.\pipe\Au.CL-" + tid.ToString(); //will send this string to the task
			_hPipe = Api.CreateNamedPipe(pipeName,
				Api.PIPE_ACCESS_INBOUND | Api.FILE_FLAG_OVERLAPPED, //use async pipe because also need to wait for task process exit
				Api.PIPE_TYPE_MESSAGE | Api.PIPE_READMODE_MESSAGE | Api.PIPE_REJECT_REMOTE_CLIENTS,
				1, 0, 0, 0, Api.SECURITY_ATTRIBUTES.ForPipes);
			return !_hPipe.Is0;
		}

		public bool WaitAndRead(WaitHandle hProcess, Action<string> results) {
			bool R = false;
			char* b = null; const int bLen = 7900;
			var ev = new ManualResetEvent(false);
			try {
				var ha = new WaitHandle[2] { ev, hProcess };
				for (bool useSB = false; ; useSB = results == null) {
					var o = new Api.OVERLAPPED { hEvent = ev.SafeWaitHandle.DangerousGetHandle() };
					if (!Api.ConnectNamedPipe(_hPipe, &o)) {
						int e = lastError.code;
						if (e != Api.ERROR_PIPE_CONNECTED) {
							if (e != Api.ERROR_IO_PENDING) break;
							int wr = WaitHandle.WaitAny(ha);
							if (wr != 0) { Api.CancelIo(_hPipe); R = true; break; } //task ended
							if (!Api.GetOverlappedResult(_hPipe, ref o, out _, false)) { Api.DisconnectNamedPipe(_hPipe); break; }
						}
					}

					if (b == null) b = (char*)MemoryUtil.Alloc(bLen);
					bool readOK;
					while (((readOK = Api.ReadFile(_hPipe, b, bLen, out int n, null)) || (lastError.code == Api.ERROR_MORE_DATA)) && n > 0) {
						n /= 2;
						if (!readOK) useSB = true;
						if (useSB) { //rare
							_sb ??= new StringBuilder(bLen);
							if (results == null && _s != null) _sb.Append(_s);
							_s = null;
							_sb.Append(b, n);
						} else {
							_s = new string(b, 0, n);
						}
						if (readOK) {
							if (results != null) {
								results(ResultString);
								_sb?.Clear();
							}
							break;
						}
						//note: MSDN says must use OVERLAPPED with ReadFile too, but works without it.
					}
					Api.DisconnectNamedPipe(_hPipe);
					if (!readOK) break;
				}
			}
			finally {
				ev.Dispose();
				MemoryUtil.Free(b);
			}
			return R;
		}

		public string ResultString => _s ?? _sb?.ToString();

		public void Dispose() => _hPipe.Dispose();
	};

	/// <summary>
	/// Writes a string result for the task that called <see cref="runWait(out string, string, string[])"/> or <see cref="runWait(Action{string}, string, string[])"/> to run this task, or for the program that started this task using command line like <c>"Au.Editor.exe *Script5.cs"</c>.
	/// </summary>
	/// <returns>false if this task was not started in such a way. Or if failed to write, except when <i>s</i> is null/<c>""</c>.</returns>
	/// <param name="s">A string. This function does not append newline characters.</param>
	/// <remarks>
	/// <see cref="runWait(Action{string}, string, string[])"/> can read the string in real time.
	/// <see cref="runWait(out string, string, string[])"/> gets all strings joined when the task ends.
	/// The program that started this task using command line like <c>"Au.Editor.exe *Script5.cs"</c> can read the string from the redirected standard output in real time, or the string is displayed to its console in real time. The string encoding is UTF8; if you use a .bat file or cmd.exe and want to get correct Unicode text, execute this before, to change console code page to UTF-8: <c>chcp 65001</c>.
	/// 
	/// Does not work if script role is editorExtension.
	/// </remarks>
#if true
	public static unsafe bool writeResult(string s) {
		if (s_wrPipeName == null) return false;
		if (s.NE()) return true;
		if (Api.WaitNamedPipe(s_wrPipeName, 3000)) { //15 mcs
			using var pipe = Api.CreateFile(s_wrPipeName, Api.GENERIC_WRITE, 0, Api.OPEN_EXISTING, 0); //7 mcs
			if (!pipe.Is0) {
				fixed (char* p = s) if (Api.WriteFile(pipe, p, s.Length * 2, out _)) return true; //17 mcs
			}
		}
		Debug_.PrintNativeError_();
		return false;
		//SHOULDDO: optimize. Eg the app may override TextWriter.Write(char) and call this on each char in a string etc.
		//	Now 40 mcs. Console.Write(char) 20 mcs.
	}
	internal static string s_wrPipeName;
#else //does not work
	public static unsafe bool writeResult(string s) {
		if (s_wrPipeName == null) return false;
		if (s.NE()) return true;
		if (s_wrPipe.Is0) {
			if (Api.WaitNamedPipe(s_wrPipeName, 3000)) { //15 mcs
				lock (s_wrPipeName) {
					if (s_wrPipe.Is0) {
						s_wrPipe = Api.CreateFile(s_wrPipeName, Api.GENERIC_WRITE, 0, default, Api.OPEN_EXISTING, 0);
					}
				}
			}
			Debug_.PrintNativeError_(s_wrPipe.Is0);
		}
		if (!s_wrPipe.Is0) {
			fixed (char* p = s)
				if (Api.WriteFile(s_wrPipe, p, s.Length * 2, out _)) return true; //17 mcs
				else Debug_.PrintNativeError_(); //No process is on the other end of the pipe (0xE9)
		}
		return false;
	}
	static string s_wrPipeName;
	static Handle_ s_wrPipe;
#endif

	#endregion

	#region end

	/// <summary>
	/// Ends this process.
	/// </summary>
	/// <remarks>
	/// Calls <see cref="Environment.Exit"/>.
	/// 
	/// It executes process exit event handlers. Does not execute <c>finally</c> code blocks. Does not execute GC.
	/// </remarks>
	public static void end() {
		Environment.Exit(0);
	}

	/// <summary>
	/// Ends another script process.
	/// </summary>
	/// <param name="processId">Script process id, for example returned by <see cref="script.run"/>.</param>
	/// <returns>true if ended, false if failed, null if wasn't running.</returns>
	/// <exception cref="ArgumentException"><i>processId</i> is 0 or id of this process.</exception>
	/// <remarks>
	/// The script process can be started from editor or not.
	/// 
	/// The process executes process exit event handlers. Does not execute <c>finally</c> code blocks and GC.
	/// 
	/// Returns null if <i>processId</i> is invalid (probably because the script is already ended). Returns false if <i>processId</i> is valid but not of a script process (probably the script ended long time ago and the id is reused for another process).
	/// </remarks>
	public static bool? end(int processId) {
		if (processId == 0 || processId == Api.GetCurrentProcessId()) throw new ArgumentException();

		using var h = Handle_.OpenProcess(processId, Api.SYNCHRONIZE | Api.PROCESS_TERMINATE); //tested: UAC OK
		if (h.Is0) {
			if (lastError.code == Api.ERROR_INVALID_PARAMETER) return null;
			return false;
		}
		if (Api.WaitForSingleObject(h, 0) == 0) return null;

		//var w1 = wnd.findFast(processId.ToS(), script.c_auxWndClassName, messageOnly: true);
		var w1 = wait.forCondition(-1d, () => wnd.findFast(processId.ToS(), script.c_auxWndClassName, messageOnly: true));
		if (!w1.Is0) {
			w1.Post(Api.WM_CLOSE);
			if (0 == Api.WaitForSingleObject(h, 1000)) return true;
			bool ok = Api.TerminateProcess(h, -1);
			if (0 == Api.WaitForSingleObject(h, ok ? 2000 : 0)) return true;
		} else {
			//don't terminate, maybe it's not a script process
			if (0 == Api.WaitForSingleObject(h, 1000)) return true;
		}

		return false;
	}

	/// <summary>
	/// Ends all task processes of a script.
	/// </summary>
	/// <param name="name">Script file name (like <c>"Script43.cs"</c>) or path in workspace (like <c>@"\Folder\Script43.cs"</c>), or full file path.</param>
	/// <returns>true if ended, false if failed (probably file not found), null if wasn't running.</returns>
	/// <exception cref="AuException">Editor process not found.</exception>
	/// <remarks>
	/// Can end only script processes started from the editor.
	/// 
	/// The process executes process exit event handlers. Does not execute <c>finally</c> code blocks and GC.
	/// </remarks>
	public static bool? end([ParamString(PSFormat.CodeFile)] string name) {
		var w = ScriptEditor.WndMsg_; if (w.Is0) throw new AuException("Editor process not found.");
		int r = (int)WndCopyData.Send<char>(w, 5, name);
		return r == 1 ? true : r == 2 ? null : false;
	}

	/// <summary>
	/// Returns true if the specified script task is running.
	/// </summary>
	/// <param name="name">Script file name (like <c>"Script43.cs"</c>) or path in workspace (like <c>@"\Folder\Script43.cs"</c>), or full file path.</param>
	public static bool isRunning([ParamString(PSFormat.CodeFile)] string name) {
		var w = ScriptEditor.WndMsg_; if (w.Is0) return false;
		return 0 != WndCopyData.Send<char>(w, 6, name);
	}

	/// <summary>
	/// Returns true if the specified script task is running.
	/// </summary>
	/// <param name="processId">Script process id, for example returned by <see cref="script.run"/>.</param>
	/// <exception cref="ArgumentException"><i>processId</i> is 0 or id of this process.</exception>
	/// <remarks>
	/// The script process can be started from editor or not.
	/// </remarks>
	public static bool isRunning(int processId) {
		if (processId == 0 || processId == Api.GetCurrentProcessId()) throw new ArgumentException();

		using var h = Handle_.OpenProcess(processId, Api.SYNCHRONIZE);
		if (h.Is0 || Api.WaitForSingleObject(h, 0) == 0) return false;

		var w1 = wait.forCondition(-0.5, () => wnd.findFast(processId.ToS(), script.c_auxWndClassName, messageOnly: true));
		return !w1.Is0;
	}

	#endregion

	/// <summary>
	/// If role miniProgram or exeProgram, default compiler adds module initializer that calls this. If using other compiler, called from <b>script.setup</b>.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static unsafe void AppModuleInit_() {
		if (s_appModuleInit) return;
		s_appModuleInit = true;

		process.thisProcessCultureIsInvariant = true;

		Cpp.Cpp_UEF(true); //2 ms. Loads the C++ dll.

		Api.SetErrorMode(Api.SEM_NOGPFAULTERRORBOX | Api.SEM_FAILCRITICALERRORS);
		//SEM_NOGPFAULTERRORBOX disables WER. See also the workaround below. //CONSIDER: add setup parameter enableWER.
		//SEM_FAILCRITICALERRORS disables some error message boxes, eg when removable media not found; MSDN recommends too.

		AppDomain.CurrentDomain.UnhandledException += _UnhandledException;

		AppDomain.CurrentDomain.ProcessExit += (_, _) => {
			Exiting_ = true;
			Cpp.Cpp_UEF(false);
		};

		if (role == SRole.ExeProgram) {
			//set STA thread if Main without [MTAThread]
			if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA) { //speed: 150 mcs
				if (null == Assembly.GetEntryAssembly().EntryPoint.GetCustomAttribute<MTAThreadAttribute>()) { //1.5 ms
					process.ThisThreadSetComApartment_(ApartmentState.STA); //1.6 ms
				}
			}

			MiniProgram_.ResolveNugetRuntimes_(AppContext.BaseDirectory);

			int pidEditor = 0;
			var cd = Environment.CurrentDirectory;
			const string c_ep = "\\Roslyn\\.exeProgram";
			if (cd.Ends(c_ep, true)) { //started from editor
				Environment.CurrentDirectory = folders.ThisApp;

				var p = &SharedMemory_.Ptr->script;
				pidEditor = p->pidEditor;
				s_wndMsg = (wnd)p->hwndMsg;
				if (0 != (p->flags & 2)) script.testing = true;
				if (0 != (p->flags & 4)) ScriptEditor.IsPortable = true;
				if (0 != (p->flags & 8)) s_wrPipeName = p->pipe;
				folders.Editor = new(cd[..^c_ep.Length]);
				folders.Workspace = new(p->workspace);

				var hevent = Api.OpenEvent(Api.EVENT_MODIFY_STATE, false, "Au.event.exeProgram.1");
				if (!Api.SetEvent(hevent)) Environment.Exit(4);
				Api.CloseHandle(hevent);
			}
			Starting_(AppDomain.CurrentDomain.FriendlyName, pidEditor);
		}
	}
	static bool s_appModuleInit;
	static UExcept s_setupException = UExcept.Print;
	internal static Exception s_unhandledException; //for process.thisProcessExit
	internal static wnd s_wndMsg;

	internal static bool Exiting_ { get; private set; }

	static void _UnhandledException(object sender, UnhandledExceptionEventArgs u) {
		if (!u.IsTerminating) return; //never seen, but anyway
		Exiting_ = true;
		Cpp.Cpp_UEF(false);
		var e = (Exception)u.ExceptionObject; //probably non-Exception object is impossible in C#
		s_unhandledException = e;
		if (s_setupException.Has(UExcept.Print)) print.it(e);
		if (s_setupException.Has(UExcept.Dialog)) {
			var text = e.ToStringWithoutStack();
			if (ScriptEditor.Available) {
				var st = new StackTrace(e, true);
				var b = new StringBuilder(text);
				b.Append("\n");
				for (int i = 0; i < st.FrameCount; i++) {
					var f = st.GetFrame(i);
					if (f.HasSource()) b.Append($"\n<a href=\"{i}\">{f.GetMethod()?.Name}</a> in {pathname.getName(f.GetFileName())}:{f.GetFileLineNumber()}");
				}
				dialog.show("Task failed", b.ToString(), "Close", flags: DFlags.ExpandDown, expandedText: e.ToString(), onLinkClick: _Link1);
				void _Link1(DEventArgs e) {
					if (s_setupException.Has(UExcept.Print)) e.d.Send.Close();
					var f = st.GetFrame(e.LinkHref.ToInt());
					ScriptEditor.Open(f.GetFileName(), f.GetFileLineNumber(), Math.Max(0, f.GetFileColumnNumber() - 1));
				}
			} else {
				dialog.show("Task failed", text, "Close", expandedText: e.ToString());
			}
		}

		//workaround for .NET bug: randomly changes error mode.
		//	Usually 0x3 -> 0x8001 (removed SEM_NOGPFAULTERRORBOX), sometimes even 0x0. Usually never restores.
		//	Then on unhandled exception starts werfault.exe (with "wait" cursor), and the process exits with 1 s delay, even if WER disabled.
		//	Tested: same in a standard simplest .NET program. But less frequently.
		//	Usually it happens while _AuxThread is starting, often in Cpp_UEF (now removed). Rarely if _AuxThread not used.
		//	Several ms before or after the script code starts.
		//	The bug is in several places in CLR code.
		//		It sets error mode, then executes code without lock and exception handling, then restores (if no exception).
		//		Why it does not use SetThreadErrorMode? Why it uses the obsolete flag SEM_NOOPENFILEERRORBOX? Why it does not check maybe SEM_FAILCRITICALERRORS is already set (as recommended in doc)? Why it does not | the new error mode flags with current flags?
		//	Could move _AuxThread to the C++ dll (not very easy).
		//		Or move most of the _AuxThread startup code to the main thread (making script startup slower).
		//		But it just would make this less frequent.
		//		Anyway, moved Cpp_UEF here. It's better to load the dll sync, not at a random time later.
		//	Never mind. This workaround solves the biggest problem for this library. Maybe future .NET will fix it.
		Api.SetErrorMode(Api.SEM_NOGPFAULTERRORBOX | Api.SEM_FAILCRITICALERRORS);
	}

	[StructLayout(LayoutKind.Sequential, Size = 256 + 1024)] //note: this struct is in shared memory. Size must be same in all library versions.
	internal unsafe struct SharedMemoryData_ {
		public int flags; //1 not received (let editor wait), 2 testing, 4 isPortable, 8 has pipe
		public int pidEditor;
		public int hwndMsg;
		int _pipeLen;
		fixed char _pipeData[64];
		int _workspaceLen;
		fixed char _workspaceData[1024];

		public string pipe {
			get { fixed (char* p = _pipeData) return new(p, 0, _pipeLen); }
			set {
				fixed (char* p = _pipeData) value.AsSpan().CopyTo(new Span<char>(p, 64));
				_pipeLen = value.Length;
			}
		}

		public string workspace {
			get { fixed (char* p = _workspaceData) return new(p, 0, _workspaceLen); }
			set {
				fixed (char* p = _workspaceData) value.AsSpan().CopyTo(new Span<char>(p, 1024));
				_workspaceLen = value.Length;
			}
		}
	}

	/// <summary>
	/// Adds various features to this script task (running script): tray icon, exit on Ctrl+Alt+Delete, etc.
	/// </summary>
	/// <param name="trayIcon">Add tray icon. See <see cref="trayIcon"/>.</param>
	/// <param name="sleepExit">End this process when computer is going to sleep or hibernate.</param>
	/// <param name="lockExit">
	/// End this process when the active desktop has been switched (PC locked, Ctrl+Alt+Delete, screen saver, etc, except UAC consent).
	/// Then to end this process you can use hotkeys Win+L (lock computer) and Ctrl+Alt+Delete.
	/// Most mouse, keyboard, clipboard and window functions don't work when other desktop is active. Many of them then throw exception, and the script would end anyway.
	/// </param>
	/// <param name="debug">Call <see cref="DebugTraceListener.Setup"/> with <i>usePrint</i> true.</param>
	/// <param name="exception">What to do on unhandled exception (event <see cref="AppDomain.UnhandledException"/>).</param>
	/// <param name="exitKey">
	/// If not 0, the script task will end when this key pressed. Will call <see cref="Environment.Exit"/>.
	/// Example: <c>exitKey: KKey.MediaStop</c>.
	/// <para>
	/// Recommended keys: media, volume, browser and applaunch keys. They work even when the process of the active window is admin (UAC) and this script isn't; other keys then work only with Ctrl, Alt or Win. In any case, the key does not work if somewhere used for a global hotkey, trigger, <i>exitKey</i> or <i>pauseKey</i>.
	/// </para>
	/// </param>
	/// <param name="pauseKey">
	/// Let <see cref="pause"/> pause/resume when this key pressed. Default: ScrollLock (Fn+S, Fn+K or similar).
	/// If CapsLock, pauses when it is toggled (even if was toggled at startup) and resumes when untoggled.
	/// <para>
	/// ScrollLock, CapsLock and NumLock are the most reliable. Other keys don't work if somewhere used for a global hotkey, trigger, <i>exitKey</i> or <i>pauseKey</i>. Also other keys (maybe except keys like MediaPlayPause) don't work when the process of the active window is admin (UAC) and this script isn't, unless pressed with Ctrl, Alt or Win. For other keys the function registers all hotkey combinations with Ctrl, Shift, Alt and Win, and they can be used when the key alone does not work because of UAC.
	/// </para>
	/// </param>
	/// <param name="f_">[](xref:caller_info). Don't use. Or use like <c>f_: null</c> to disable script editing via tray icon.</param>
	/// <exception cref="InvalidOperationException">Already called.</exception>
	/// <remarks>
	/// Tip: in Options -> Templates you can set default code for new scripts.
	/// 
	/// Calling this function is optional. However it should be called if compiling the script with a non-default compiler (eg Visual Studio) if you want the task behave the same (invariant culture, <b>STAThread</b>, unhandled exception action).
	/// 
	/// Does nothing if role editorExtension or if running in WPF preview mode.
	/// </remarks>
	public static void setup(bool trayIcon = false, bool sleepExit = false, bool lockExit = false, bool debug = false, UExcept exception = UExcept.Print, KKey exitKey = 0, KKey pauseKey = KKey.ScrollLock, [CallerFilePath] string f_ = null) {
		if (role == SRole.EditorExtension || isWpfPreview) return;
		if (s_setupOnce) throw new InvalidOperationException("script.setup already called");
		s_setupOnce = true;

		s_setupException = exception;
		if (!s_appModuleInit) AppModuleInit_(); //if role miniProgram, called by MiniProgram_.Init; else if default compiler, the call is compiled into code; else called now.

		if (debug) DebugTraceListener.Setup(usePrint: true); //info: default false, because slow and rarely used.

		s_exitKey = exitKey;
		s_pauseKey = pauseKey;
		s_pauseSetupDone = true;

		if (sleepExit || lockExit || exitKey != 0 || pauseKey is not (0 or KKey.CapsLock)) {
			s_sleepExit = sleepExit;
			s_lockExit = lockExit;
			s_exitKey = exitKey;
			_AuxQueueAPC(static _ => {
				if (s_sleepExit) {
					if (osVersion.minWin8) {
						//if Modern Standby, need RegisterSuspendResumeNotification to receive WM_POWERBROADCAST.
						//	The API and MS are unavailable on Win7.
						//	The API supports window handle and callback. With handle less problems.
						var h1 = Api.RegisterSuspendResumeNotification(s_auxWnd.Handle, 0);
						process.thisProcessExit += _ => { Api.UnregisterSuspendResumeNotification(h1); };
					} else {
						WndUtil.CreateWindowDWP_(messageOnly: false, t_eocWP = (w, m, wp, lp) => {
							if (m == Api.WM_POWERBROADCAST && wp == Api.PBT_APMSUSPEND) _SleepLockExit(true);
							return Api.DefWindowProc(w, m, wp, lp);
						}); //message-only windows don't receive WM_POWERBROADCAST, unless used RegisterSuspendResumeNotification
					}
				}

				if (s_lockExit) {
					new WinEventHook(EEvent.SYSTEM_DESKTOPSWITCH, 0, k => {
						if (miscInfo.isInputDesktop()) return;
						if (0 != process.getProcessId("consent.exe")) return; //UAC
						k.hook.Dispose();
						_SleepLockExit(false);
					});
					//tested: on Win+L works immediately. OS switches desktop 2 times. At first briefly, then makes defaul again, then on key etc switches again to show password field.
				}

				if (s_exitKey != 0) {
					if (!_RegisterKey(s_exitKey, 16)) {
						print.warning("script.setup failed to register the exit key.", -1);
					}
				}

				if (s_pauseKey is not (0 or KKey.CapsLock)) _PauseSetKey();
			});
		}

		if (trayIcon) TrayIcon_(f_: f_);
	}
	static bool s_setupOnce, s_sleepExit, s_lockExit;
	static KKey s_exitKey;
	[ThreadStatic] static WNDPROC t_eocWP;

	/// <summary>
	/// Ensures that multiple script tasks that call this function don't run simultaneously. Like C# <c>lock</c> keyword for threads.
	/// </summary>
	/// <param name="mutex">Mutex name. Only tasks that use same mutex cannot run simultaneously.</param>
	/// <param name="wait">If a task is running (other or same), wait max this milliseconds. If 0 (default), does not wait. If -1, waits without a timeout.</param>
	/// <param name="silent">Don't print <c>"cannot run"</c>.</param>
	/// <exception cref="InvalidOperationException">Already called.</exception>
	/// <remarks>
	/// If cannot run because a task is running (other or same), calls <c>Environment.Exit(3);</c>.
	/// </remarks>
	public static void single(string mutex = "Au-mutex-script.single", int wait = 0, bool silent = false) {
		//FUTURE: parameter bool endOther. Like meta ifRunning restart.

		var m = Api.CreateMutex(null, false, mutex ?? "Au-mutex-script.single"); //tested: don't need Api.SECURITY_ATTRIBUTES.ForLowIL
		if (default != Interlocked.CompareExchange(ref s_singleMutex, m, default)) { Api.CloseHandle(m); throw new InvalidOperationException(); }
		var r = Api.WaitForSingleObject(s_singleMutex, wait);
		//print.it(r);
		if (r is not (0 or Api.WAIT_ABANDONED)) {
			if (!silent) print.it($"<>Note: script task <open {path}|||script.single>{name}<> cannot run because a task is running.");
			Environment.Exit(3);
		}
		//never mind: should release mutex.
		//	Cannot release in process exit event. It runs in another thread.
		//	Cannot use UsingEndAction, because then caller code must be like 'using var single = script.single();'.
		//return new(() => Api.ReleaseMutex(s_singleMutex));
	}
	static IntPtr s_singleMutex;

	/// <summary>
	/// Adds standard tray icon.
	/// </summary>
	/// <param name="delay">Delay, milliseconds.</param>
	/// <param name="init">Called before showing the tray icon. Can set its properties and event handlers.</param>
	/// <param name="menu">Called before showing context menu. Can add menu items. Menu item actions must not block messages etc for long time; if need, run in other thread or process (<see cref="script.run"/>).</param>
	/// <param name="f_">[](xref:caller_info). Don't use. Or set = null to disable script editing via the tray icon.</param>
	/// <remarks>
	/// Uses other thread. The <i>init</i> and <i>menu</i> actions run in that thread too. It dispatches messages, therefore they also can set timers (<see cref="timer"/>), create hidden windows, etc. Current thread does not have to dispatch messages.
	/// 
	/// Does nothing if role editorExtension.
	/// </remarks>
	/// <example>
	/// Shows how to change icon and tooltip.
	/// <code><![CDATA[
	/// script.trayIcon(init: t => { t.Icon = icon.stock(StockIcon.HELP); t.Tooltip = "Example"; });
	/// ]]></code>
	/// Shows how to add menu items.
	/// <code><![CDATA[
	/// script.trayIcon(menu: (t, m) => {
	/// 	m["Example"] = o => { dialog.show("Example"); };
	/// 	m["Run other script"] = o => { script.run("Example"); };
	/// });
	/// ]]></code>
	/// </example>
	/// <seealso cref="Au.trayIcon"/>
	public static void trayIcon(int delay = 500, Action<trayIcon> init = null, Action<trayIcon, popupMenu> menu = null, [CallerFilePath] string f_ = null) {
		if (role == SRole.EditorExtension) return;
		if (!s_appModuleInit) AppModuleInit_();
		TrayIcon_(delay, init, menu, f_);
	}

	internal static void TrayIcon_(int delay = 500, Action<trayIcon> init = null, Action<trayIcon, popupMenu> menu = null, [CallerFilePath] string f_ = null) {
		_AuxQueueAPC(_ => timer.after(delay, _Delayed));

		void _Delayed(timer t_) {
			var ti = new trayIcon { Tooltip = script.name };
			init?.Invoke(ti);
			ti.Icon ??= icon.trayIcon();
			bool canEdit = f_ != null && ScriptEditor.Available;
			if (canEdit) ti.Click += _ => ScriptEditor.Open(f_);
			ti.MiddleClick += _ => Environment.Exit(2);
			ti.RightClick += e => {
				var m = new popupMenu();
				if (menu != null) {
					menu(ti, m);
					if (m.Last != null && !m.Last.IsSeparator) m.Separator();
				}
				if (canEdit) m["Open script\tClick"] = _ => ScriptEditor.Open(f_);
				m["End task\tM-click" + (s_sleepExit ? ", Sleep" : null) + (s_lockExit ? ", Win+L, Ctrl+Alt+Delete" : null)] = _ => Environment.Exit(2);
				if (canEdit) m["End and open"] = _ => { ScriptEditor.Open(f_); Environment.Exit(2); };
				m.Show(PMFlags.AlignCenterH | PMFlags.AlignRectBottomTop, /*excludeRect: ti.GetRect(out var r1) ? r1 : null,*/ owner: ti.Hwnd);
			};
			ti.Visible = true;
		}
	}

	/// <summary>
	/// Waits for a .NET debugger attached to this process.
	/// Does nothing if already attached.
	/// </summary>
	/// <param name="showDialog">Show dialog with process name and id. If false, prints that info in the output pane.</param>
	/// <remarks>
	/// When debugger is attached, this function returns and the script continues to run. The step mode begins when the script encounters one of:
	/// - <see cref="Debugger.Break"/> in code.
	/// - breakpoint (set in debugger).
	/// - exception (if debugger is configured to break on exception).
	/// 
	/// Some best free programs that have a .NET debugger:
	/// - Visual Studio (Community edition). It's the best, but huge (~10 GB).
	/// - Visual Studio Code. It's much smaller.
	/// - (possibly free) JetBrains Rider.
	/// 
	/// Unlike <see cref="Debugger.Launch"/>, this function does not launch the Visual Studio debugger. It waits until you attach a debugger to this process. To attach:
	/// - Visual Studio: menu Debug -> Attach to process. Then select the process (this function displays its name and id).
	/// - Visual Studio Code: in the Run view select combo box item ".NET Core Attach" and click button "Start debugging". Then select the process.
	/// - JetBrains Rider: menu Run -> Attach to Process. Then select the process.
	/// 
	/// This function can launch a script to automate attaching a debugger. See Options -> General -> Debugger script. More info in <see href="/cookbook/Script testing and debugging.html">Cookbook</see>.
	/// 
	/// <note>If the script process is running as administrator, the debugger process must run as administrator too.</note>
	/// 
	/// Visual Studio Code debugger setup:
	/// - Install extension "C#".
	/// - Open a folder where you want to save debugger settings.
	/// - Click menu Run -> Add configuration. Select ".NET 5+ and .NET Core".
	/// - Click "Add configuration" again and select ".NET attach to local...".
	/// - Save.
	/// </remarks>
	[DebuggerStepThrough]
	public static void debug(bool showDialog = false) {
		if (!Debugger.IsAttached) {
			if (showDialog) {
				var d = new dialog("Waiting for debugger to attach", $"Process {process.thisExeName}  {process.thisProcessId}.");
				d.Screen = screen.ofMouse;
				d.ShowDialogNoWait();
				wait.forCondition(0, () => Debugger.IsAttached);
				d.Send.Close();
			} else {
				var w = ScriptEditor.WndMsg_;
				if (!w.Is0 && 0 != w.Send(Api.WM_USER, 30, process.thisProcessId)) { //run debugger script specified in Options
					wait.forCondition(0, () => Debugger.IsAttached);
				} else {
					print.it($"Process {process.thisExeName} {process.thisProcessId}. Waiting for debugger to attach...");
					wait.forCondition(0, () => Debugger.IsAttached);
					print.it("Debugger attached.");
				}
			}
		}
		//note: don't add Debugger.Break(); here. It creates problems.
	}

	#region aux thread

	internal static unsafe void Starting_(string name, int pidEditor) {
		s_name = name;
		s_auxHthread = Api.CreateThread(default, 0, &_AuxThread, pidEditor, 0, out _);
		//using CreateThread because need thread handle ASAP
	}
	static IntPtr s_auxHthread;

	//Auxiliary thread for various tasks:
	//	Exit when editor process terminated or crashed.
	//	Terminate script processes in a less brutal way.
	//	Tray icon.
	//	script.setup(sleepExit, lockExit)
	//	Cpp_InactiveWindowWorkaround for miniProgram.
	//	Can be used for various triggers.
	//	Etc.
	[UnmanagedCallersOnly]
	static unsafe void _AuxThread(nint param) {
		//CONSIDER: for miniProgram create thread earlier.

		Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
		WndUtil.UacEnableMessages(Api.WM_COPYDATA, Api.WM_USER, Api.WM_CLOSE);
		WndUtil.RegisterWindowClass(c_auxWndClassName, _AuxWndProc);
		s_auxWnd = WndUtil.CreateMessageOnlyWindow(c_auxWndClassName, Api.GetCurrentProcessId().ToS());

		_MessageLoop((int)param);

		[MethodImpl(MethodImplOptions.NoInlining)] //need fast JIT of the main func, to make s_auxWnd available ASAP
		static void _MessageLoop(int pidEditor) {
			//pidEditor 0 if exeProgram started not from editor

			//Cpp.Cpp_UEF(true); //moved to AppModuleInit_

			if (role == SRole.MiniProgram) Cpp.Cpp_InactiveWindowWorkaround(true);

			var hp = pidEditor == 0 ? default : (IntPtr)Handle_.OpenProcess(pidEditor, Api.SYNCHRONIZE);
			int nh = hp == default ? 0 : 1;
			for (; ; ) {
				var k = Api.MsgWaitForMultipleObjectsEx(nh, &hp, -1, Api.QS_ALLINPUT, Api.MWMO_ALERTABLE | Api.MWMO_INPUTAVAILABLE);
				if (k == nh) {
					if (!wait.doEvents()) break;
				} else if (k == 0) { //editor process terminated or crashed
					_AuxExit();
				} else if (k != Api.WAIT_IO_COMPLETION) {
					Debug_.Print(k);
					break;
				}
			}
		}
	}

	/// <summary>
	/// Class name of the auxiliary message-only window.
	/// </summary>
	internal const string c_auxWndClassName = "Au.Task.m3gVxcTJN02pDrHiQ00aSQ";

	static nint _AuxWndProc(wnd w, int message, nint wp, nint lp) {
		switch (message) {
		//case Api.WM_COPYDATA:
		//	return 0;
		//case Api.WM_USER:
		//	return 0;
		case Api.WM_POWERBROADCAST:
			if (s_sleepExit && osVersion.minWin8 && wp == Api.PBT_APMSUSPEND) _SleepLockExit(true);
			break;
		case Api.WM_HOTKEY:
			if (wp is >= 0 and <= 15) {
				s_paused ^= true;
			} else if (wp is >= 16 and <= 31) {
				Environment.Exit(2);
			}
			break;
		}

		var R = Api.DefWindowProc(w, message, wp, lp);

		if (message == Api.WM_DESTROY) _AuxExit();

		return R;
	}

	static void _AuxExit() {
		Environment.Exit(1);

		//same speed
		//process.thisProcessExitInvoke();
		//Api.ExitProcess(1);
	}

	//CONSIDER: make public, maybe hidden. Or internal, to use with testInternal.
	static void _AuxQueueAPC(Api.PAPCFUNC apc, nint param = 0) {
		Api.QueueUserAPC(apc, s_auxHthread, param);
		(s_apcGC ??= new()).Add(apc);
	}
	static List<Delegate> s_apcGC;

	//currently unused
	//static wnd _AuxWnd {
	//	get {
	//		while (s_auxWnd.Is0) {
	//			Debug_.Print("waiting for s_auxWnd");
	//			Thread.Sleep(5);
	//		}
	//		return s_auxWnd;
	//	}
	//}
	static wnd s_auxWnd;

	#endregion

	#region pause

	/// <summary>
	/// If was pressed the pause key, waits until the user presses it again.
	/// </summary>
	/// <param name="text">Text to display in the 'Paused script' UI.</param>
	/// <param name="doEvents">Process Windows messages and other events while waiting. For example, windows of this thread can respond, and timers of this thread can run.</param>
	/// <remarks>
	/// The default pause key is ScrollLock (Fn+S, Fn+K or similar). To change, use <see cref="setup"/> parameter <i>pauseKey</i>. If <b>script.setup</b> not called, this function uses ScrollLock but does not pause when called the first time.
	///
	/// A script can be paused only if it calls this function. Pausing at a random place would be dangerous and is not supported. Call this function in places where it is safe to pause, and where it makes sense, for example in a loop that preses keys or mouse buttons. To pause/resume, let the user press the pause key.
	///
	/// If the pause key is CapsLock, waits if it is toggled, even if was toggled when this script started.
	/// </remarks>
	/// <example>
	/// <code><![CDATA[
	/// script.setup(trayIcon: true, pauseKey: KKey.MediaPlayPause);
	/// using var t = osdText.showTransparentText("         ", -1);
	/// for (int i = 0; i < 1000; i++) {
	/// 	script.pause();
	/// 	//script.pause("Next: continue the loop.");
	/// 	t.Text = i.ToS();
	/// 	250.ms();
	/// }
	/// ]]></code>
	/// </example>
	public static void pause(string text = null, bool doEvents = false) {
		if (!s_pauseSetupDone) { //script.setup not called
			s_pauseSetupDone = true;
			s_pauseWhenUntoggled = keys.gui.isToggled(s_pauseKey = KKey.ScrollLock);
			return;
		}

		if (paused) {
			var s = $"Paused: {script.name}.";
			if (s_pauseKey != 0) s += $"\nKey: {s_pauseKey}.";
			if (!text.NE()) s += $"\n{text}";
			using var icon = ImageUtil.LoadGdipBitmapFromXaml("<Viewbox Width='32' Height='32' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Path Data='M13,16V8H15V16H13M9,16V8H11V16H9M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20A8,8 0 0,0 20,12A8,8 0 0,0 12,4Z' Stretch='Uniform' Fill='#FFD631' UseLayoutRounding='False' SnapsToDevicePixels='False' /></Viewbox>", screen.primary.Dpi);
			using (osdText.showText(s, -1, new(y: ^10), icon, 0xffffff, 0x444444, showMode: OsdMode.WeakThread)) {
				OWait ow = new(2, doEvents);
				wait.forCondition(0, () => !paused, ow);
			}
		}
	}
	//FUTURE: UI to end task when paused.
	//CONSIDER: add option to save-restore mouse xy, active window, its state.
	//CONSIDER: auto call pause in key/mouse/etc functions if there are no pressed modifier keys and mouse buttons.

	/// <summary>
	/// If true, next call to <see cref="pause"/> will wait until false, or already is waiting.
	/// </summary>
	public static bool paused {
		get => s_paused || (_PauseIsLockKey && keys.gui.isToggled(s_pauseKey) != s_pauseWhenUntoggled);
		set { s_paused = value; }
	}

	//in aux thread
	static void _PauseSetKey() {
		if (_PauseIsLockKey) {
			s_pauseWhenUntoggled = keys.gui.isToggled(s_pauseKey);
		} else {
			if (!_RegisterKey(s_pauseKey, 0)) {
				print.warning("script.setup failed to register the pause key. Will use ScrollLock.", -1);
				s_pauseWhenUntoggled = keys.gui.isToggled(s_pauseKey = KKey.ScrollLock);
			}
		}
	}
	static KKey s_pauseKey;
	static bool s_paused, s_pauseSetupDone, s_pauseWhenUntoggled;

	static bool _PauseIsLockKey => s_pauseKey is KKey.ScrollLock or KKey.CapsLock or KKey.NumLock;

	#endregion

	#region util

	static void _SleepLockExit(bool sleep) {
		print.it($"<>Info: task <open {path}|||script.setup>{name}<> ended because of {(sleep ? "PC sleep" : "switched desktop")} at {DateTime.Now.ToShortTimeString()}.");
		Task.Run(() => Environment.Exit(2));
		//why Task.Run: with RegisterSuspendResumeNotification does not work well in same thread.
	}

	static bool _RegisterKey(KKey key, int idBase) {
		//register all mod combinations. Else does not work if script pressed a mod key at that time.
		for (int i = 0; i < 16; i++) {
			var k = key;
			if (k == KKey.Pause && 0 != (i & Api.MOD_CONTROL)) k = KKey.Break; //Ctrl+Pause = Break
			if (!Api.RegisterHotKey(s_auxWnd, idBase + i, (uint)i | Api.MOD_NOREPEAT, k)) {
				if (k == KKey.Pause && i == 8) continue; //Win+Pause opens something in Windows Settings
				while (--i >= 0) Api.UnregisterHotKey(s_auxWnd, idBase + i);
				return false;
				//any failed to register hotkey would be dangerous, because the user-pressed key combined with script-pressed modifiers would invoke the hotkey in its owner app
			}
		}
		return true;
	}

	#endregion
}
