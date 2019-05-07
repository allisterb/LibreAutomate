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
using System.Linq;
using Microsoft.Win32.SafeHandles;

using Au.Types;
using static Au.NoClass;

namespace Au.Util
{
	unsafe struct LibProcessStarter
	{
		public char[] cl;
		public Api.STARTUPINFO si;
		public uint flags;
		public string curDir;
		public string envVar;
		string _exe; //for errors only

		/// <summary>
		/// Prepares parameters for API <msdn>CreateProcess</msdn> and similar.
		/// </summary>
		/// <param name="exe">
		/// Full path of program file. If not full path, uses <see cref="Folders.ThisApp"/>. Uses <see cref="Path_.Normalize"/>.
		/// If <i>rawExe</i> true, does not use <b>Normalize</b>/<b>ThisApp</b>.
		/// </param>
		/// <param name="args">null or command line arguments.</param>
		/// <param name="curDir">
		/// Initial current directory of the new process.
		/// - If null, uses <c>Directory.GetCurrentDirectory()</c>.
		/// - Else if <i>rawCurDir</i>==true, uses raw <i>curDir</i> value.
		/// - Else if "", calls <c>Path_.GetDirectoryPath(exe)</c>.
		/// - Else calls <see cref="Path_.ExpandEnvVar"/>.
		/// </param>
		/// <param name="envVar">null or environment variables to pass to the new process together with variables of this process. Format: "var1=value1\0var2=value2\0". If ends with "\0\0", will pass only these variables.</param>
		/// <param name="rawExe">Don't normalize <i>exe</i>.</param>
		/// <param name="rawCurDir">Don't normalize <i>curDir</i>.</param>
		public LibProcessStarter(string exe, string args = null, string curDir = null, string envVar = null, bool rawExe = false, bool rawCurDir = false) : this()
		{
			if(!rawExe) exe = Path_.Normalize(exe, Folders.ThisApp, PNFlags.DontExpandDosPath | PNFlags.DontPrefixLongPath);
			_exe = exe;
			cl = (args == null ? ("\"" + exe + "\"" + "\0") : ("\"" + exe + "\" " + args + "\0")).ToCharArray();
			if(curDir == null) this.curDir = Directory.GetCurrentDirectory(); //if null passed to CreateProcessWithTokenW, the new process does not inherit current directory of this process
			else this.curDir = rawCurDir ? curDir : (curDir.Length == 0 ? Path_.GetDirectoryPath(exe) : Path_.ExpandEnvVar(curDir));

			si.cb = Api.SizeOf<Api.STARTUPINFO>();
			si.dwFlags = Api.STARTF_FORCEOFFFEEDBACK;

			flags = Api.CREATE_UNICODE_ENVIRONMENT;

			if(envVar != null && !envVar.Ends("\0\0")) {
				var es = Api.GetEnvironmentStrings();
				int len1; for(var k = es; ; k++) if(k[0] == 0 && k[1] == 0) { len1 = (int)(k - es) + 2; break; }
				int len2 = envVar.Length;
				var t = new string('\0', len1 + len2);
				fixed (char* p = t) {
					Api.memcpy(p, es, --len1 * 2);
					for(int i = 0; i < envVar.Length; i++) p[len1 + i] = envVar[i];
				}
				this.envVar = t;
				Api.FreeEnvironmentStrings(es);
			} else this.envVar = null;
		}

		/// <summary>
		/// Starts process using API CreateProcess or CreateProcessAsUser, without the feedback hourglass cursor.
		/// </summary>
		/// <param name="pi">Receives CreateProcessX results. Will need to close handles in pi, eg pi.Dispose.</param>
		/// <param name="inheritUiaccess">If this process has UAC integrity level uiAccess, let the new process inherit it.</param>
		/// <param name="inheritHandles">API parameter <i>bInheritHandles</i>.</param>
		public bool StartLL(out Api.PROCESS_INFORMATION pi, bool inheritUiaccess = false, bool inheritHandles = false)
		{
			if(inheritUiaccess && Api.OpenProcessToken(Process_.CurrentProcessHandle, Api.TOKEN_QUERY | Api.TOKEN_DUPLICATE | Api.TOKEN_ASSIGN_PRIMARY, out LibHandle hToken)) {
				using(hToken) return Api.CreateProcessAsUser(hToken, null, cl, null, null, inheritHandles, flags, envVar, curDir, si, out pi);
			} else {
				return Api.CreateProcess(null, cl, null, null, inheritHandles, flags, envVar, curDir, si, out pi);
			}
		}

		/// <summary>
		/// Starts process using API CreateProcess or CreateProcessAsUser, without the feedback hourglass cursor.
		/// </summary>
		/// <param name="need">Which field to set in <b>Result</b>.</param>
		/// <param name="inheritUiaccess">If this process has UAC integrity level uiAccess, let the new process inherit it.</param>
		/// <exception cref="AuException">Failed.</exception>
		internal Result Start(Result.Need need = 0, bool inheritUiaccess = false)
		{
			bool suspended = need == Result.Need.NetProcess && !_NetProcessObject.IsFast, resetSuspendedFlag = false;
			if(suspended && 0 == (flags & Api.CREATE_SUSPENDED)) { flags |= Api.CREATE_SUSPENDED; resetSuspendedFlag = true; }
			bool ok = StartLL(out var pi, inheritUiaccess);
			if(resetSuspendedFlag) flags &= ~Api.CREATE_SUSPENDED;
			if(!ok) throw new AuException(0, $"*start process '{_exe}'");
			return new Result(pi, need, suspended);
		}

		/// <summary>
		/// Starts UAC Medium integrity level (IL) process from this admin process.
		/// </summary>
		/// <param name="need">Which field to set in <b>Result</b>.</param>
		/// <exception cref="AuException">Failed.</exception>
		/// <remarks>
		/// Actually the process will have the same IL and user session as the shell process (normally explorer).
		/// Fails if there is no shell process (API GetShellWindow fails) for more than 2 s from calling this func.
		/// Asserts and fails if this is not admin/system process. Caller should at first call <see cref="Uac.IsAdmin"/> or <see cref="Uac.IntegrityLevel"/>.
		/// </remarks>
		internal Result StartUserIL(Result.Need need = 0)
		{
			if(s_userToken == null) {
				Debug.Assert(Uac.IsAdmin); //else cannot set privilege
				if(!Util.Security_.SetPrivilege("SeIncreaseQuotaPrivilege", true)) goto ge;

				//Perf.First();
#if false //works, but slow, eg 60 ms, even if we don't create task everytime
				var s = $"\"{Folders.ThisAppBS}Dll\\{(Ver.Is64BitProcess ? "64" : "32")}bit\\AuCpp.dll\",Cpp_RunDll";
				Au.Util.LibTaskScheduler.CreateTaskToRunProgramOnDemand("Au", "rundll32", false, Folders.System + "rundll32.exe", s);
				//Au.Util.LibTaskScheduler.CreateTaskToRunProgramOnDemand("Au", "rundll32", false, Folders.System + "notepad.exe"); //slow too
				//Perf.Next();
				int pid = Au.Util.LibTaskScheduler.RunTask("Au", "rundll32");
				//Perf.Next();
				//Print(pid);
				var hUserProcess = LibHandle.OpenProcess(pid);
				//Print((IntPtr)hUserProcess);
				if(hUserProcess.Is0) goto ge;
#else
				bool retry = false;
				g1:
				var w = Api.GetShellWindow();
				if(w.Is0) { //if Explorer process killed or crashed, wait until it restarts
					if(!WaitFor.Condition(2, () => !Api.GetShellWindow().Is0)) throw new AuException($"*start process '{_exe}' as user. There is no shell process.");
					500.ms();
					w = Api.GetShellWindow();
				}

				var hUserProcess = LibHandle.OpenProcess(w);
				if(hUserProcess.Is0) {
					if(retry) goto ge;
					retry = true; 500.ms(); goto g1;
				}

				//two other ways:
				//1. Enum processes and find one that has Medium IL. Unreliable, eg its token may be modified.
				//2. Start a service process. Let it start a Medium IL process like in QM2. Because LocalSystem can get token with WTSQueryUserToken.
				//tested: does not work with GetTokenInformation(TokenLinkedToken). Even if would work, in non-admin session it is wrong token.
#endif
				//Perf.NW();

				using(hUserProcess) {
					if(Api.OpenProcessToken(hUserProcess, Api.TOKEN_DUPLICATE, out LibHandle hShellToken)) {
						using(hShellToken) {
							const uint access = Api.TOKEN_QUERY | Api.TOKEN_ASSIGN_PRIMARY | Api.TOKEN_DUPLICATE | Api.TOKEN_ADJUST_DEFAULT | Api.TOKEN_ADJUST_SESSIONID;
							if(Api.DuplicateTokenEx(hShellToken, access, null, Api.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, Api.TOKEN_TYPE.TokenPrimary, out var userToken))
								s_userToken = new SafeAccessTokenHandle(userToken);
						}
					}
				}
				if(s_userToken == null) goto ge;
			}

			bool suspended = need == Result.Need.NetProcess && !_NetProcessObject.IsFast, resetSuspendedFlag = false;
			if(suspended && 0 == (flags & Api.CREATE_SUSPENDED)) { flags |= Api.CREATE_SUSPENDED; resetSuspendedFlag = true; }
			bool ok = Api.CreateProcessWithTokenW(s_userToken.DangerousGetHandle(), 0, null, cl, flags, envVar, curDir, si, out var pi);
			if(resetSuspendedFlag) flags &= ~Api.CREATE_SUSPENDED;
			if(!ok) goto ge;
			return new Result(pi, need, suspended);

			ge: throw new AuException(0, $"*start process '{_exe}' as user");
		}
		static SafeAccessTokenHandle s_userToken;

		/// <summary>
		/// Results of <see cref="LibProcessStarter"/> functions.
		/// </summary>
		public class Result
		{
			/// <summary>
			/// Which field to set.
			/// </summary>
			public enum Need
			{
				None,
				//NativeHandle,
				WaitHandle,
				NetProcess,
			}

			public int pid;
			public WaitHandle waitHandle;
			public Process netProcess;

			internal Result(in Api.PROCESS_INFORMATION pi, Need need, bool suspended)
			{
				pid = pi.dwProcessId;
				switch(need) {
				case Need.NetProcess:
					netProcess = _NetProcessObject.Create(pi, suspended: suspended);
					break;
				case Need.WaitHandle:
					pi.hThread.Dispose();
					waitHandle = new LibWaitHandle(pi.hProcess, true);
					break;
				default:
					pi.Dispose();
					break;
				}
			}
		}

		/// <summary>
		/// Creates new .NET Process object with attached handle and/or id.
		/// </summary>
		static class _NetProcessObject //FUTURE: remove if unused
		{
			/// <summary>
			/// Returns true if can create such object in a fast/reliable way. Else <see cref="Create"/> will use Process.GetProcessById.
			/// It depends on .NET framework version, because uses private methods of Process class through reflection.
			/// </summary>
			public static bool IsFast { get; } = _CanSetHandleId();

			public static bool _CanSetHandleId()
			{
				const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod;
				s_mi1 = typeof(Process).GetMethod("SetProcessHandle", flags);
				s_mi2 = typeof(Process).GetMethod("SetProcessId", flags);
				if(s_mi1 != null && s_mi2 != null) return true;
				Debug.Assert(false);
				return false;
			}
			static MethodInfo s_mi1, s_mi2;

			/// <summary>
			/// Creates new .NET Process object with attached handle and/or id.
			/// Can be specified both handle and id, or one of them (then .NET will open process or get id from handle when need).
			/// </summary>
			public static Process Create(IntPtr handle, int id)
			{
				if(!IsFast) {
					if(id == 0) id = Process_.ProcessIdFromHandle(handle);
					return Process.GetProcessById(id); //3 ms, much garbage, gets all processes, can throw
				}

				var p = new Process();
				var o = new object[1];
				if(handle != default) {
					o[0] = new SafeProcessHandle(handle, true);
					s_mi1.Invoke(p, o);
				}
				if(id != 0) {
					o[0] = id;
					s_mi2.Invoke(p, o);
				}
				return p;
			}

			/// <summary>
			/// Creates new .NET Process object with attached handle and id.
			/// Closes thread handle. If suspended, resumes thread.
			/// </summary>
			public static Process Create(in Api.PROCESS_INFORMATION pi, bool suspended)
			{
				try {
					return Create(pi.hProcess, pi.dwProcessId);
				}
				finally {
					if(suspended) Api.ResumeThread(pi.hThread);
					pi.hThread.Dispose();
				}
			}
		}
	}
}
