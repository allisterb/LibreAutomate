﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Catkeys;
using static Catkeys.NoClass;
using Catkeys.Util; using Util = Catkeys.Util;
using static Catkeys.Util.NoClass;
using Catkeys.Winapi;
using Auto = Catkeys.Automation;

namespace Catkeys.Tasks
{
	public partial class ManagerWin :Form
	{
		public ManagerWin()
		{
			Time.Next(); //3 ms
			//InitializeComponent(); //5 ms
			Time.NextWrite();
		}
	}
}
