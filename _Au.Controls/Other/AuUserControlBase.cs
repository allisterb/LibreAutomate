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
//using System.Linq;
using System.Windows.Forms;
using System.Drawing;

using Au.Types;

namespace Au.Controls
{
	/// <summary>
	/// Can be used as base class for user controls instead of UserControl when you want correct auto-scaling when high DPI (AutoScaleMode.Font).
	/// </summary>
	/// <seealso cref="DialogForm"/>
	public class AuUserControlBase : UserControl
	{
		///
		public AuUserControlBase()
		{
			this.AutoScaleMode = AutoScaleMode.Font;

			//this.TabStop = false; //no, breaks tabstopping
			//this.SetStyle(ControlStyles.Selectable, false); //the same
		}
	}
}
