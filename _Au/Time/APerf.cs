﻿#define PREPAREMETHOD

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
//using System.Linq;

using Au.Types;

//rejected: store the static instance in shared memory. This is how it was implemented initially.
//	Then would be easy to measure speed of process startup.
//	However then too slow APerf startup (JIT + opening shared memory), eg 5 ms vs 1 ms.
//	Also rejected Serialize: pass the string eg as command line args, then in that process create new Local variable.
//	Instead use ATime.PerfMicroseconds, eg with command line.

namespace Au
{
	/// <summary>
	/// Code speed measurement. Easier to use than <see cref="Stopwatch"/>.
	/// </summary>
	[DebuggerStepThrough]
	public static unsafe class APerf
	{
		/// <summary>
		/// The same as <see cref="APerf"/> class, but allows to have multiple independent speed measurements.
		/// </summary>
		/// <remarks>
		/// Stores data in the variable, not in a static variable.
		/// 
		/// Don't need to dispose variables of this type. The <see cref="Dispose"/> function just calls <see cref="NW"/>.
		/// </remarks>
		public unsafe struct Local : IDisposable
		{
			static Local()
			{
				//Prevent JIT delay when calling Next etc if not ngened.
				//if(!Util.Assembly_.IsAuNgened) { //unnecessary and makes slower
#if PREPAREMETHOD
				Util.AJit.Compile(typeof(Local), "Next", "NW", "Dispose");
#if DEBUG //else these methods are inlined
				Util.AJit.Compile(typeof(APerf), "Next", "NW");
#endif
#else //similar speed
				APerf.Next(); APerf.NW(); APerf.First(); //JIT-compiles everything we need. s_enabled prevents calling AOutput.Write etc.
				s_enabled = true;
#endif

				//JIT speed: 1 ms.
			}

#if !PREPAREMETHOD
			static bool s_enabled;
#endif
			const int _nElem = 16;

			fixed long _a[_nElem];
			fixed char _aMark[_nElem];
			volatile int _counter;
			bool _incremental;
			int _nMeasurements; //used with incremental to display n measurements and average times
			long _time0;

			/// <summary>See <see cref="APerf.Incremental"/>.</summary>
			/// <example>
			/// <code><![CDATA[
			/// var perf = new APerf.Local { Incremental = true };
			/// for(int i = 0; i < 5; i++) {
			/// 	Thread.Sleep(100); //not included in the measurement
			/// 	perf.First();
			/// 	Thread.Sleep(30); //will make sum ~150000
			/// 	perf.Next();
			/// 	Thread.Sleep(10); //will make sum ~50000
			/// 	perf.Next();
			/// 	Thread.Sleep(100); //not included in the measurement
			/// }
			/// perf.Write(); //speed:  154317  51060  (205377)
			/// perf.Incremental = false;
			/// ]]></code>
			/// </example>
			public bool Incremental {
				get => _incremental;
				set {
					if(_incremental = value) {
						fixed(long* p = _a) { for(int i = 0; i < _nElem; i++) p[i] = 0; }
						_nMeasurements = 0;
					}
				}
			}

			/// <summary><see cref="APerf.First()"/></summary>
			public void First()
			{
#if !PREPAREMETHOD
				if(!s_enabled) return; //called by the static ctor
#endif
				//info: we don't use Stopwatch because it loads System.dll, which can take 15 ms and make speed measurement incorrect and confusing in some cases.
				Api.QueryPerformanceCounter(out _time0);
				_counter = 0;
				_nMeasurements++;
			}

			//rejected. See comments in static.
			///// <summary>
			///// Calls <see cref="Cpu"/> and <see cref="First()"/>.
			///// </summary>
			//public void First(int timeSpeedUpCPU)
			//{
			//	Cpu(timeSpeedUpCPU);
			//	First();
			//}

			/// <summary><see cref="APerf.Next"/></summary>
			public void Next(char cMark = '\0')
			{
#if !PREPAREMETHOD
				if(!s_enabled) return; //called by the static ctor
#endif
				int n = _counter; if(n >= _nElem) return;
				_counter++;
				fixed(long* p = _a) {
					Api.QueryPerformanceCounter(out long pc); long t = pc - _time0;
					if(_incremental) p[n] += t; else p[n] = t;
					//fixed (char* c = _aMark) c[n] = cMark;
					char* c = (char*)(p + _nElem); c[n] = cMark;
				}
			}

			/// <summary>
			/// Calls <see cref="Next"/> and <see cref="Write"/>.
			/// </summary>
			/// <param name="cMark">A character to add to the results string like "A=150".</param>
			[MethodImpl(MethodImplOptions.NoInlining)]
			public void NW(char cMark = '\0') { Next(cMark); Write(); }

			/// <summary>
			/// Calls <see cref="NW"/>, which calls <see cref="Next"/> and <see cref="Write"/>.
			/// </summary>
			/// <remarks>
			/// Don't need to dispose variables of this type. This function just allows to use the 'using' pattern instead of <b>NW</b>. See example.
			/// </remarks>
			/// <example>
			/// <code><![CDATA[
			/// using(var p1 = APerf.Create()) { //p1.First();
			/// 	1.ms();
			/// 	p1.Next();
			/// 	1.ms();
			/// } //p1.NW();
			/// ]]></code>
			/// </example>
			public void Dispose() => NW();

			/// <summary>
			/// Formats a string from time values collected by calling <see cref="First"/> and <see cref="Next"/>, and shows it in the output.
			/// The string contains the number of microseconds of each code execution between calling <b>First</b> and each <b>Next</b>.
			/// </summary>
			public void Write()
			{
#if !PREPAREMETHOD
				if(!s_enabled) return; //called by the static ctor
#endif
				AOutput.Write(ToString());
			}

			/// <summary>
			/// Formats a string from time values collected by calling <see cref="First"/> and <see cref="Next"/>.
			/// The string contains the number of microseconds of each code execution between calling <b>First</b> and each <b>Next</b>.
			/// </summary>
			public override string ToString()
			{
				using(new Util.StringBuilder_(out var b)) {
					b.Append("speed:");
					_Results(Math.Min(_counter, _nElem), b, null);
					return b.ToString();
				}
			}

			/// <summary>
			/// Return array of time values collected by calling <see cref="First"/> and <see cref="Next"/>.
			/// Each element is the number of microseconds of each code execution between calling <b>First</b> and each <b>Next</b>.
			/// </summary>
			public long[] ToArray()
			{
				int n = Math.Min(_counter, _nElem);
				var a = new long[n];
				_Results(n, null, a);
				return a;
			}

			void _Results(int n, StringBuilder b, long[] a)
			{
				if(n == 0) return;
				bool average = false; int nMeasurements = 1;

				fixed(long* p = _a) fixed(char* c = _aMark) {
					g1:
					double t = 0d, tPrev = 0d;
					for(int i = 0; i < n; i++) {
						t = ATime.s_freqMCS * p[i];
						double d = t - tPrev; tPrev = t; //could add 0.5 to round up, but assume that QueryPerformanceCounter call time is 0 - 0.5.
						if(average) d /= nMeasurements;
						long dLong = (long)d;
						if(b != null) {
							b.Append("  ");
							if(c[i] != '\0') b.Append(c[i]).Append('=');
							b.Append(dLong.ToString());
						} else {
							a[i] = dLong;
						}
					}
					if(b == null) return;

					if(n > 1) {
						if(average) t /= nMeasurements;
						b.Append("  (").Append((long)t).Append(")");
					}

					if(!average && _incremental && (nMeasurements = _nMeasurements) > 1) {
						average = true;
						b.Append(";  measured ").Append(nMeasurements).Append(" times, average");
						goto g1;
					}
				}
			}

			/// <summary>
			/// Gets the number of microseconds between <see cref="First"/> and the last <see cref="Next"/>.
			/// </summary>
			public long TimeTotal {
				get {
					int n = _counter;
					if(n == 0) return 0;
					if(n > _nElem) n = _nElem;
					fixed(long* p = _a) { return (long)(ATime.s_freqMCS * p[n - 1]); }
				}
			}

			//rejected: Not very useful. Not very easy to understand the purpose. Adds some overhead.
			///// <summary>
			///// Executes <i>code</i> (lambda) <i>count</i> times, and then calls <see cref="Next"/>.
			///// </summary>
			//public void Execute(int count, Action code)
			//{
			//	while(count-- > 0) code();
			//	Next();
			//}

			///// <summary>
			///// <i>countAll</i> times executes this code: <c>First(); foreach(Action a in codes) Execute(countEach, a); Write();</c>.
			///// </summary>
			//public void ExecuteMulti(int countAll, int countEach, params Action[] codes)
			//{
			//	while(countAll-- > 0) {
			//		First();
			//		foreach(Action a in codes) Execute(countEach, a);
			//		Write();
			//	}
			//}

			//rejected. It's easier to use ATime.PerfMicroseconds in the same way.
			///// <summary>
			///// Converts this variable to string that can be used to create a copy of this variable with <see cref="Local(string)"/>.
			///// </summary>
			//public string Serialize()
			//{
			//	var si = sizeof(Local);
			//	var b = new byte[si];
			//	fixed (long* p = _a) Marshal.Copy((IntPtr)p, b, 0, si);
			//	return Convert.ToBase64String(b);
			//}
			///// <summary>
			///// Initializes this variable as a copy of another variable that has been converted to string with <see cref="Serialize"/>.
			///// </summary>
			//public Local(string serialized) : this()
			//{
			//	var si = sizeof(Local);
			//	var b = Convert.FromBase64String(serialized);
			//	if(b.Length == si)
			//		fixed (long* p = _a) Marshal.Copy(b, 0, (IntPtr)p, si);
			//}
		}

		/// <summary>
		/// This static variable is used by the static functions.
		/// </summary>
		static Local StaticInst;
		//rejected: public. Needed for Serialize. Maybe in the future will be implemented somehow differently.

		/// <summary>
		/// Creates and returns new <see cref="Local"/> variable and calls its <see cref="Local.First"/>.
		/// </summary>
		public static Local Create()
		{
			var R = new Local();
			R.First();
			return R;
		}

		/// <summary>
		/// If true, times of each new First/Next/Next... measurement are added to previous measurement times.
		/// Finally you can call <see cref="Write"/> or <see cref="ToString"/> to get the sums.
		/// Usually used to measure code in loops. See example.
		/// </summary>
		/// <example>
		/// <code><![CDATA[
		/// APerf.Incremental = true;
		/// for(int i = 0; i < 5; i++) {
		/// 	Thread.Sleep(100); //not included in the measurement
		/// 	APerf.First();
		/// 	Thread.Sleep(30); //will make sum ~150000
		/// 	APerf.Next();
		/// 	Thread.Sleep(10); //will make sum ~50000
		/// 	APerf.Next();
		/// 	Thread.Sleep(100); //not included in the measurement
		/// }
		/// APerf.Write(); //speed:  154317  51060  (205377)
		/// APerf.Incremental = false;
		/// ]]></code>
		/// </example>
		public static bool Incremental {
			get => StaticInst.Incremental;
			set => StaticInst.Incremental = value;
		}

		/// <summary>
		/// Stores current time in the first element of an internal array.
		/// </summary>
		public static void First() => StaticInst.First();

		//rejected. Unclear. Can confuse int timeSpeedUpCPU with char cMark.
		///// <summary>
		///// Calls <see cref="Cpu"/> and <see cref="First()"/>.
		///// </summary>
		//public static void First(int timeSpeedUpCPU) => StaticInst.First(timeSpeedUpCPU);

		/// <summary>
		/// Stores current time in next element of an internal array.
		/// </summary>
		/// <remarks>
		/// Don't call <b>Next</b> more than 16 times after <b>First</b>, because the array has fixed size.
		/// </remarks>
		/// <param name="cMark">A character to mark this time in the results string, like "A=150".</param>
		public static void Next(char cMark = '\0') => StaticInst.Next(cMark);

		/// <summary>
		/// Calls <see cref="Next"/> and <see cref="Write"/>.
		/// </summary>
		/// <param name="cMark">A character to add to the results string like "A=150".</param>
		public static void NW(char cMark = '\0') => StaticInst.NW(cMark);

		/// <summary>
		/// Formats a string from time values collected by calling <see cref="First"/> and <see cref="Next"/>, and shows it in the output.
		/// The string contains the number of microseconds of each code execution between calling <b>First</b> and each <b>Next</b>.
		/// </summary>
		/// <example>
		/// <code><![CDATA[
		/// APerf.First(100);
		/// CODE1;
		/// APerf.Next();
		/// CODE2;
		/// APerf.Next();
		/// APerf.Write(); //speed:  timeOfCODE1  timeOfCODE2  (totalTime)
		/// ]]></code>
		/// </example>
		public static void Write() => StaticInst.Write();

		/// <summary>
		/// Formats a string from time values collected by calling <see cref="First"/> and <see cref="Next"/>.
		/// The string contains the number of microseconds of each code execution between calling <b>First</b> and each <b>Next</b>.
		/// </summary>
		public static new string ToString() => StaticInst.ToString();

		/// <summary>
		/// Return array of time values collected by calling <see cref="First"/> and <see cref="Next"/>.
		/// Each element is the number of microseconds of each code execution between calling <b>First</b> and each <b>Next</b>.
		/// </summary>
		public static long[] ToArray() => StaticInst.ToArray();

		/// <summary>
		/// Gets the number of microseconds between <see cref="First"/> and the last <see cref="Next"/>.
		/// </summary>
		public static long TimeTotal => StaticInst.TimeTotal;

		/// <summary>
		/// Executes some code in loop for the specified amount of time. It should make CPU to run at full speed.
		/// </summary>
		/// <param name="timeMilliseconds">How long to speed up CPU, milliseconds. The minimal required time probably is about 100 ms, but depends on CPU.</param>
		/// <remarks>
		/// Code speed measurements often are misleading because of variable CPU speed. Most CPU don't run at full speed when not actively used.
		/// 
		/// You can make CPU speed constant in Control Panel -> Power Options -> ... Advanced -> Processor power management -> Minimum or maximum power state.
		/// There are programs that show current CPU speed. For example HWMonitor.
		/// </remarks>
		public static void SpeedUpCpu(int timeMilliseconds = 200)
		{
			int n = 0;
			for(long t0 = ATime.PerfMicroseconds; ATime.PerfMicroseconds - t0 < timeMilliseconds * 1000L; n++) { }
			//AOutput.Write(n);
		}

		//rejected: Not very useful. Not very easy to understand the purpose. Adds some overhead.
		///// <summary>
		///// Executes <i>code</i> (lambda) <i>count</i> times, and then calls <see cref="Next"/>.
		///// </summary>
		//public static void Execute(int count, Action code) => StaticInst.Execute(count, code);

		///// <summary>
		///// <i>countAll</i> times executes this code: <c>First(); foreach(Action a in codes) Execute(countEach, a); Write();</c>.
		///// </summary>
		//public static void ExecuteMulti(int countAll, int countEach, params Action[] codes) => StaticInst.ExecuteMulti(countAll, countEach, codes);

		/// <summary>
		/// Gets a reference to a <see cref="Local"/> variable in shared memory.
		/// </summary>
		/// <remarks>
		/// The variable can be used by multiple processes, for example to measure process startup time.
		/// Note: slow first time in process, eg 3 ms. It's because need to JIT-compile functions and open shared memory.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// ref var p = ref APerf.Shared;
		/// p.First();
		/// //or
		/// APerf.Shared.First();
		/// ]]></code>
		/// </example>
		public static ref Local Shared => ref Util.SharedMemory_.Ptr->perf;
	}
}
