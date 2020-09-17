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
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Au;
using Au.Types;
using Au.Controls;
using System.Windows.Controls;

static class Menus
{
	[Command]
	public static class File
	{
		[Command(keys = "F2")]
		public static void Rename() { }

		[Command(keys = "Delete")]
		public static void Delete() { }

		[Command("...", image = "resources/images/property_16x.xaml")]
		public static void Properties() { }

		[Command("Open, close")]
		public static class OpenClose
		{

		}

		[Command]
		public static class More
		{
			[Command('t', keysText = "Ctrl+X", name = "File-Cut")]
			public static void Cut() { }

			[Command(keysText = "Ctrl+C", name = "File-Copy")]
			public static void Copy() { }

			[Command(keysText = "Ctrl+V", name = "File-Paste")]
			public static void Paste() { }

			[Command('r', separator = true)]
			public static void Copy_relative_path() { }

			[Command('f')]
			public static void Copy_full_path() { }

			//[Command("...", separator = true)]
			//public static void Print_setup() { }

			//[Command("...")]
			//public static void Print() { }

			[Command(separator = true, keys = "Ctrl+S", image = "resources/images/saveall_16x.xaml")]
			public static void Save_now() { }
		}

		[Command("Export, import", separator = true)]
		public static class ExportImport
		{

		}

		[Command]
		public static class Workspace
		{

		}

		[Command(separator = true, keysText = "Alt+F4")]
		public static void Close_window() { }

		[Command]
		public static void Exit() { }
	}

	[Command]
	public static class New
	{
		[Command('s', keys = "Ctrl+N", image = "resources/images/newfile_16x.xaml")]
		public static void New_script() { }

		[Command('c')]
		public static void New_class() { }

		[Command('p')]
		public static void New_partial() { }

		[Command('f')]
		public static void New_folder() { }
	}

	[Command]
	public static class Edit
	{
		[Command(keysText = "Ctrl+Z", image = "resources/images/undo_16x.xaml")]
		public static void Undo() { }

		[Command(keysText = "Ctrl+Y", image = "resources/images/redo_16x.xaml")]
		public static void Redo() { }

		[Command('t', separator = true, keysText = "Ctrl+X", image = "resources/images/cut_16x.xaml")]
		public static void Cut() { }

		[Command(keysText = "Ctrl+C", image = "resources/images/copy_16x.xaml")]
		public static void Copy() { }

		[Command(keysText = "Ctrl+V", image = "resources/images/paste_16x.xaml")]
		public static void Paste() { }

		[Command(separator = true, keys = "Ctrl+F", image = "resources/images/findinfile_16x.xaml")]
		public static void Find() { }

		[Command(separator = true)]
		public static class Selection
		{
			[Command]
			public static void Indent() { }
		}

		[Command]
		public static class View
		{
			[Command(checkable = true, keys = "Ctrl+W", image = "resources/images/wordwrap_16x.xaml")]
			public static void Wrap_lines() { }

			[Command(checkable = true, image = "resources/images/image_16x.xaml")]
			public static void Images_in_code() { }
		}
	}

	[Command]
	public static class Code
	{
		[Command('W')]
		public static void AWnd() { }
	}

	[Command]
	public static class Run
	{
		[Command(keys = "F5", image = "resources/images/startwithoutdebug_16x.xaml")]
		public static void Start() { }

		[Command(image = "resources/images/stop_16x.xaml")]
		public static void End() { }

		//[Command(image = "resources/images/pause_16x.xaml")]
		//public static void Pause() { }

		[Command(keys = "F7", image = "resources/images/buildselection_16x.xaml")]
		public static void Compile() { }

		[Command("...")]
		public static void Recent() { }

		[Command(separator = true)]
		public static void Debug_break() { }
	}

	[Command]
	public static class TT
	{
		[Command('k')]
		public static void Edit_hotkey_triggers() { }
	}

	[Command]
	public static class Tools
	{
		[Command(image = "resources/images/settingsgroup_16x.xaml")]
		public static void Options() { }
	}

	[Command]
	public static class Help
	{
		[Command]
		public static void Program_help() { }

		[Command]
		public static void Library_help() { }

		[Command(keys = "F1", image = "resources/images/statushelp_16x.xaml")]
		public static void Context_help() { }
	}

#if TRACE
	[Command]
	public static void TEST() {
		//if (_testCM == null) {
		//	_testCM = new AContextMenu();
		//	//Program.Commands["Wrap_lines"].CopyToMenu(_testCM.Add(null));
		//	Program.Commands["View"].CopyToMenu(_testCM);
		//}
		//_testCM.IsOpen = true;

		//var m = new Menu();
		//var b = new MenuItem();
		//App.Commands["View"].CopyToMenu(b);
		//m.Items.Add(b);
		////Program.Toolbars[0].Items.Add(m);
		////(Program.Toolbars[0].Parent as ToolBarTray).ToolBars.Add(m);
		////var p = (Program.Toolbars[0].Parent as ToolBarTray).Parent as DockPanel;

		////var p = new DockPanel();
		////p.Children.Add(m);

		//var p = new Border();
		//p.Child = m;
		//App.Toolbars[0].Items.Add(p);
	}
	//static AContextMenu _testCM;
#endif
}
