#region begin_script
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
//using System.Reflection;
//using System.Windows.Forms;

//using qm;
//using static qm.Output;
using Util;
using static Util.Static;
using Winapi;

using Cat.Browsers;
//.ref Browsers;

public static class ThisScript2 {
[STAThread]static void Main(string[] args) {
#endregion

string s=Firefox.GetAddress();
//Out(s);
s=Regex.Replace(s, "^.+\bv=(.+?)(?=&|$)", "$1");
//Out(s);
Run("http://share.xmarks.com/folder/bookmarks/JQk2OrPIPd");
Hwnd w=0;
try { w=WaitFor.WindowActive(20, "Video - Muzika (Xmarks shared folder) - Mozilla Firefox", "MozillaWindowClass");
} catch { MsgBoxEnd("Failed. If does not open xmarks in new tab, restart firefox."); }
w.Activate();
Firefox.Wait();
Wait(1);
Key("Ctrl+f");
Wait(0.5);
Paste(s);
}
}









Selection.SetText(s);
-> String extension method defined in StringExt:
s.SetSelText();
or
StringExt.SetSelText(s);
or just
Paste(s);