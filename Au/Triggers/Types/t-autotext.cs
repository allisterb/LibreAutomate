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

#pragma warning disable CS1591 // Missing XML comment //TODO

namespace Au.Triggers
{
	public class AutotextTrigger : Trigger
	{
		string _shortString;

		internal AutotextTrigger(AuTriggers triggers, Action<AutotextTriggerArgs> action, string text) : base(triggers, action, true)
		{
			_shortString = text;
		}

		internal override void Run(TriggerArgs args) => RunT(args as AutotextTriggerArgs);

		public override string TypeString() => "Autotext";

		public override string ShortString() => _shortString;
	}

	public class AutotextTriggers : ITriggers
	{
		AuTriggers _triggers;
		Dictionary<string, Trigger> _d = new Dictionary<string, Trigger>();

		internal AutotextTriggers(AuTriggers triggers)
		{
			_triggers = triggers;
		}

		public Action<AutotextTriggerArgs> this[string text] {
			set {
				_triggers.LibThrowIfRunning();
				var t = new AutotextTrigger(_triggers, value, text);
				t.DictAdd(_d, text);
				_lastAdded = t;
			}
		}

		/// <summary>
		/// The last added trigger.
		/// </summary>
		public AutotextTrigger Last => _lastAdded;
		AutotextTrigger _lastAdded;

		bool ITriggers.HasTriggers => _lastAdded != null;

		void ITriggers.StartStop(bool start)
		{

		}

		internal bool HookProc(HookData.Keyboard k, TriggerHookContext thc)
		{
			//note: this is called after HotkeyTriggers.HookProc.
			//	It may set thc.triggers and return false to not suppress the input event. Then we should reset autotext.

			Debug.Assert(!k.IsInjectedByAu); //server must ignore

			if(!k.IsUp && 0 == k.Mod) {

			}
			return false;
		}
	}

	public class AutotextTriggerArgs : TriggerArgs
	{
		public AutotextTrigger Trigger { get; }
		public Wnd Window { get; }

		///
		public AutotextTriggerArgs(AutotextTrigger trigger, Wnd w)
		{
			Trigger = trigger;
			Window = w;
		}

		public void Replace(string replacement)
		{

		}
	}
}
