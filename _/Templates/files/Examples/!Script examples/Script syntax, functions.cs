/// Script description.
/// It is optional.

/*/ runSingle true; /*/ //.
using Au;
using System.Text;
;ATask.Setup(trayIcon: true); //;

/*
The programming language is C#.

In scripts you can use classes/functions of the automation library provided by
this program, as well as of .NET and everything that can be used in C#.
Also you can create and use new functions, classes, libraries and .exe programs.

A script can optionally start with a description as /// comments.
Then can be /*/ /*/ comments with script properties used by the editor program.
Then 'using' directives.
Then ATask.Setup or/and other code that sets run time properties. Optional.
Then your script. It can contain local functions anywhere.
Then optionally you can define classes and other types.

This syntax is known as "C# top-level statements". It is simple and concise,
but has some limitations. You can instead use a class with Main function. Try
menu Edit -> Convert -> To script class.

The //. and //; are used to fold (hide) code. Click the small [+] box at
the top-left to see and edit that code when need. 

Script properties are saved in /*/ /*/ comments at the start of script.
You can change them in the Properties dialog or edit directly in script.
Before /*/ /*/ comments can be only other comments, empty lines and spaces.

More properties can be set in code with ATask.Setup and other functions.
For example, if don't need tray icon, remove 'trayIcon: true'.

To change default properties and code for new scripts: Options -> Templates.

To run a script, you can click the ► Run button on the toolbar, or use command line,
or call ATask.Run from another script, or in Options set to run at startup.

Triggers such as hotkeys, autotext, mouse and window are used to execute code
in a running script. That code also can launch other scripts.
Also you can create custom toolbars and menus.
To access triggers and toolbars you can use menu TT.
*/

//Examples of automation functions.

AOutput.Write("Script example");

ADialog.Show("Message box", "example");

AFile.Run(AFolders.System + "notepad.exe");
var w = AWnd.Wait(0, true, "*- Notepad");
AKeys.Key("F5 Enter*2");
AKeys.Text(w.Name);
2.s();
w.Close();
var w2 = AWnd.Wait(-3, true, "Notepad", "#32770");
if (!w2.Is0) {
	500.ms();
	var c = +w2.Child(null, "Button", skip: 1); // "Don't Save"
	AMouse.Click(c);
	500.ms();
}


//Examples of .NET functions.

string s = "Example";
var b = new StringBuilder();
for (int i = 0; i < s.Length; i++) {
	b.Append(s[i]).AppendLine();
}
System.Windows.Forms.MessageBox.Show(b.ToString());


//Example of your function. It is a local function and can use variables defined before it.

int variable = 1;

FunctionExample("Function example"); //calls the function
AOutput.Write(variable);

void FunctionExample(string s) { //a function
	AOutput.Write(s, variable);
	variable++;
}
