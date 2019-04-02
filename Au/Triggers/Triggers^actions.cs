using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
//using System.Windows.Forms;
//using System.Drawing;
//using System.Linq;
//using System.Xml.Linq;

using Au;
using Au.Types;
using static Au.NoClass;

namespace Au.Triggers
{
	class TOptions
	{
		public Action<TriggerOptions.BAArgs> before;
		public Action<TriggerOptions.BAArgs> after;
		public short thread;
		public bool noWarning;
		public int ifRunning;

		public TOptions Clone() => this.MemberwiseClone() as TOptions;

		//CONSIDER: before calling 'before' or action, reset all Opt options. Or use main thread's options set before adding trigger.
	}

	/// <summary>
	/// Allows to set some options for multiple triggers and their actions.
	/// </summary>
	/// <remarks>
	/// You set options through a thread-static property <see cref="ActionTriggers.Options"/>.
	/// Changed options are applied to all triggers/actions added afterwards in this thread.
	/// </remarks>
	/// <example>
	/// <code><![CDATA[
	/// Triggers.Options.RunActionInThreadPool(singleInstance: false);
	/// Triggers.Options.BeforeAction = o => { Opt.Key.KeySpeed = 10; };
	/// Triggers.Hotkey["Ctrl+K"] = o => Print(Opt.Key.KeySpeed); //10
	/// Triggers.Hotkey["Ctrl+Shift+K"] = o => Print(Opt.Key.KeySpeed); //10
	/// Triggers.Options.BeforeAction = o => { Opt.Key.KeySpeed = 20; };
	/// Triggers.Hotkey["Ctrl+L"] = o => Print(Opt.Key.KeySpeed); //20
	/// Triggers.Hotkey["Ctrl+Shift+L"] = o => Print(Opt.Key.KeySpeed); //20
	/// ]]></code>
	/// </example>
	public class TriggerOptions
	{
		TOptions _new, _prev;

		TOptions _New() => _new ?? (_new = _prev?.Clone() ?? new TOptions());

		/// <summary>
		/// Run actions in a dedicated thread that does not end when actions end.
		/// </summary>
		/// <param name="thread">A number that you want to use to identify the thread. Can be 0-32767 (short.MaxValue). Default 0.</param>
		/// <param name="ifRunningWaitMS">Defines when to start an action if an action (other or same) is currently running in this thread. If 0 (default), don't run. If -1 (<b>Timeout.Infinite</b>), run when that action ends (and possibly other queed actions). If &gt; 0, run when that action ends, if it ends within this time from now; the time is in milliseconds.</param>
		/// <param name="noWarning">No warning when cannot start an action because an action is running and ifRunningWaitMS==0.</param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <remarks>
		/// Multiple actions in same thread cannot run simultaneously. Actions in different threads can run simultaneously.
		/// There is no "end old running action" feature. If need it, use other script. Example: <c>Triggers.Hotkey["Ctrl+M"] = o => AuTask.RunWait("Other Script");</c>.
		/// There is no "temporarily pause old running action to run new action" feature. As well as for scripts.
		/// The thread has <see cref="ApartmentState.STA"/>.
		/// The <b>RunActionInX</b> functions are mutually exclusive: only the last called function is active. If none called, it is the same as called this function without arguments.
		/// </remarks>
		public void RunActionInThread(int thread = 0, int ifRunningWaitMS = 0, bool noWarning = false)
		{
			_New();
			if((uint)thread > short.MaxValue) throw new ArgumentOutOfRangeException();
			_new.thread = (short)thread;
			_new.ifRunning = ifRunningWaitMS >= -1 ? ifRunningWaitMS : throw new ArgumentOutOfRangeException();
			_new.noWarning = noWarning;
		}
		//CONSIDER: make default ifRunningWaitMS = 1000 if it is another action.

		/// <summary>
		/// Run actions in new threads.
		/// </summary>
		/// <remarks>
		/// Use if need to run actions simultaneously with other actions or other instances of self, especially if the action is long-running (maybe 5 s and more).
		/// The thread has <see cref="ApartmentState.STA"/>.
		/// The <b>RunActionInX</b> functions are mutually exclusive: only the last called function is active.
		/// </remarks>
		/// <param name="singleInstance">Don't run if this action is already running. If false, multiple action instances can run paralelly in multiple threads.</param>
		public void RunActionInNewThread(bool singleInstance)
		{
			_New();
			_new.thread = -1;
			_new.ifRunning = singleInstance ? 0 : 1;
		}

		/// <summary>
		/// Run actions in thread pool threads.
		/// </summary>
		/// <remarks>
		/// Use if need to run actions simultaneously with other actions or other instances of self, and the action is short-running (maybe less than 5 s) and don't need <see cref="ApartmentState.STA"/>.
		/// Thread pool threads have <see cref="ApartmentState.MTA"/>.
		/// The <b>RunActionInX</b> functions are mutually exclusive: only the last called function is active.
		/// </remarks>
		/// <param name="singleInstance">Don't run if this action is already running. If false, multiple action instances can run paralelly in multiple threads.</param>
		public void RunActionInThreadPool(bool singleInstance)
		{
			_New();
			_new.thread = -2;
			_new.ifRunning = singleInstance ? 0 : 1;
		}

		//CONSIDER: RunActionInTriggersThread. Now can use Func instead, but it is more code.

		/// <summary>
		/// A function to run before the trigger action.
		/// For example, it can set <see cref="Opt"/> options.
		/// </summary>
		/// <example>
		/// <code><![CDATA[
		/// Triggers.Options.BeforeAction = o => { Opt.Key.KeySpeed = 20; Opt.Key.TextSpeed = 5; };
		/// ]]></code>
		/// </example>
		public Action<BAArgs> BeforeAction { set => _New().before = value; }

		/// <summary>
		/// A function to run after the trigger action.
		/// For example, it can log exceptions.
		/// </summary>
		/// <example>
		/// <code><![CDATA[
		/// Triggers.Options.AfterAction = o => { if(o.Exception!=null) Print(o.Exception.Message); else Print("completed successfully"); };
		/// ]]></code>
		/// </example>
		public Action<BAArgs> AfterAction { set => _New().after = value; }

		internal TOptions Current {
			get {
				if(_new != null) { _prev = _new; _new = null; }
				return _prev ?? s_empty ?? (s_empty = new TOptions());
			}
		}
		static TOptions s_empty;

		/// <summary>
		/// If true, triggers added afterwards don't depend on <see cref="ActionTriggers.Disabled"/> and <see cref="ActionTriggers.DisabledEverywhere"/>.
		/// This property sets the <see cref="ActionTrigger.EnabledAlways"/> property of triggers added afterwards.
		/// </summary>
		public bool EnabledAlways { get; set; }

		/// <summary>
		/// Arguments for <see cref="BeforeAction"/> and <see cref="AfterAction"/>.
		/// </summary>
		public struct BAArgs
		{
			internal BAArgs(TriggerArgs args)
			{
				ActionArgs = args;
				Exception = null;
			}

			/// <summary>
			/// Trigger event info. The same variable as passed to the trigger action.
			/// To access the info, cast to <b>HotkeyTriggerArgs</b> etc, depending on trigger type.
			/// </summary>
			public TriggerArgs ActionArgs { get; }

			/// <summary>
			/// If action ended with an exception, the exception. Else null.
			/// </summary>
			public Exception Exception { get; internal set; }
		}
	}

	class TriggerActionThreads
	{
		public void Run(ActionTrigger trigger, TriggerArgs args)
		{
			Action actionWrapper = () => {
				var opt = trigger.options;
				try {
					switch(args) {
					case HotkeyTriggerArgs ta:
						if(0 == (ta.Trigger.flags & (TKFlags.NoModOff | TKFlags.KeyModUp | TKFlags.PassMessage))) Keyb.Lib.ReleaseModAndDisableModMenu();
						break;
					case MouseTriggerArgs ta:
						if(0 == (ta.Trigger.flags & (TMFlags.ButtonModUp | TMFlags.PassMessage))) Keyb.Lib.DisableModMenu(); //info: not ReleaseModAndDisableModMenu. Releasing mod keys makes no sense because we cannot disable the auto-repeat. Disabling menu is unreliable too for the same reason (can show menu later), but it is best we can do if we don't want to block auto-repeated keys using a low-level keyboard hook.
						break;
					}

					var baArgs = new TriggerOptions.BAArgs(args); //struct
#if true
					opt.before?.Invoke(baArgs);
#else
					if(opt.before != null) {
						bool called = false;
						if(t_beforeCalled == null) t_beforeCalled = new List<Action<bool>> { opt.before };
						else if(!t_beforeCalled.Contains(opt.before)) t_beforeCalled.Add(opt.before);
						else called = true;
						opt.before(!called);
					}
#endif
					try { trigger.Run(args); }
					catch(Exception ex) when(!(ex is ThreadAbortException)) { baArgs.Exception = ex; Print(ex); }
					opt.after?.Invoke(baArgs);
				}
				catch(Exception e2) {
					if(e2 is ThreadAbortException) Thread.ResetAbort(); //FUTURE: don't reset if eg thrown to end task process softly
					else Print(e2);
				}
				finally {
					if(opt.thread < 0 && opt.ifRunning == 0) _d.TryRemove(trigger, out _);
				}
			};
			//never mind: we should not create actionWrapper if cannot run. But such cases are rare. Fast and small, about 64 bytes.

			int threadId = trigger.options.thread;
			if(threadId >= 0) { //dedicated thread
				_Thread h = null; foreach(var v in _a) if(v.id == threadId) { h = v; break; }
				if(h == null) _a.Add(h = new _Thread(threadId));
				h.RunAction(actionWrapper, trigger);
			} else {
				bool singleInstance = trigger.options.ifRunning == 0;
				if(singleInstance) {
					if(_d == null) _d = new ConcurrentDictionary<ActionTrigger, object>();
					if(_d.TryGetValue(trigger, out var tt)) {
						//return;
						switch(tt) {
						case Thread thread:
							if(thread.IsAlive) return;
							break;
						case Task task:
							//Print(task.Status);
							switch(task.Status) { case TaskStatus.RanToCompletion: case TaskStatus.Faulted: case TaskStatus.Canceled: break; default: return; }
							break;
						}
					}
				}

				switch(threadId) {
				case -1: //new thread
					var thread = new Thread(actionWrapper.Invoke) { IsBackground = true };
					thread.SetApartmentState(ApartmentState.STA);
					if(singleInstance) _d[trigger] = thread;
					thread.Start();
					break;
				case -2: //thread pool
					var task = new Task(actionWrapper);
					if(singleInstance) _d[trigger] = task;
					task.Start();
					break;
				}
			}
		}
		//[ThreadStatic] List<Action<bool>> t_beforeCalled;

		public void Dispose()
		{
			foreach(var v in _a) v.Dispose();
		}

		List<_Thread> _a = new List<_Thread>();
		ConcurrentDictionary<ActionTrigger, object> _d;

		class _Thread
		{
			struct _Action { public Action actionWrapper; public long time; }

			IntPtr _event;
			Queue<_Action> _q;
			bool _running;
			bool _disposed;
			public readonly int id;

			public _Thread(int id) { this.id = id; }

			public void RunAction(Action actionWrapper, ActionTrigger trigger)
			{
				if(_disposed) return;
				if(_q == null) {
					_q = new Queue<_Action>();
					_event = Api.CreateEvent(false);
					Thread_.Start(() => {
						try {
							while(!_disposed && 0 == Api.WaitForSingleObject(_event, -1)) {
								while(!_disposed) {
									_Action x;
									lock(_q) {
										g1:
										if(_q.Count == 0) { _running = false; break; }
										x = _q.Dequeue();
										if(x.time != 0 && Time.PerfMilliseconds > x.time) goto g1;
										_running = true;
									}
									x.actionWrapper();
								}
							}
						}
						finally {
							Api.CloseHandle(_event);
							_q = null; _running = false; //restart if aborted
														 //Print("thread ended");
						}
					});
				}

				lock(_q) {
					int ifRunningWaitMS = trigger.options.ifRunning;
					if(_running) {
						if(ifRunningWaitMS == 0) {
							if(!trigger.options.noWarning) Print("Warning: can't run the trigger action because an action is running in this thread. To run simultaneously or wait, use one of Triggers.Options.RunActionInX functions. To disable this warning: Triggers.Options.RunActionInThread(0, 0, noWarning: true);. Trigger: " + trigger);
							return;
						}
					} else {
						_running = true;
						//if(ifRunningWaitMS > 0 && ifRunningWaitMS < 1000000000) ifRunningWaitMS += 1000;
					}
					_q.Enqueue(new _Action { actionWrapper = actionWrapper, time = ifRunningWaitMS <= 0 ? 0 : Time.PerfMilliseconds + ifRunningWaitMS });
				}
				Api.SetEvent(_event);
			}

			public void Dispose()
			{
				if(_disposed) return; _disposed = true;
				Api.SetEvent(_event);
			}
		}
	}
}
