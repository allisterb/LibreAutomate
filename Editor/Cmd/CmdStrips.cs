﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;

using Catkeys;
using static Catkeys.NoClass;
using static Program;
using G.Controls;

partial class ThisIsNotAFormFile { }

partial class EForm
{
	public class EStrips //not struct because of warning CS1690
	{
		public MenuStrip Menubar;
		public CatToolStrip tbFile, tbEdit, tbRun, tbTools, tbHelp, tbCustom1, tbCustom2; //toolbars
		public ToolStripDropDownMenu ddFile, ddFileNew, ddEdit, ddOutput, ddStatusBar;
		public ToolStripSpringTextBox cHelpFind;
	}
	public EStrips Strips;

	_Commands _cmd; //all menu/toolbar commands. Contains command handlers, their names and delegates.
	GStripManager _strips;

	void _Strips_Init()
	{
		//var p = new Perf.Inst(true);

		Strips = new EStrips();

		//map command handler names/delegates etc
		_cmd = new _Commands();
		//p.Next();

		_strips = new GStripManager(this, _cmd);
		_strips.BuildAll(Folders.ThisApp + @"Default\Strips.xml", Folders.ThisAppDocuments + @"!Settings\Strips.xml", new GDockPanel.DockedToolStripRenderer());
		//p.Next();

		//get top-level toolstrips (menu bar and toolbars)
		Strips.Menubar = _strips.MenuBar;
		Strips.tbFile = _strips.Toolbars["File"];
		Strips.tbEdit = _strips.Toolbars["Edit"];
		Strips.tbRun = _strips.Toolbars["Run"];
		Strips.tbTools = _strips.Toolbars["Tools"];
		Strips.tbHelp = _strips.Toolbars["Help"];
		Strips.tbCustom1 = _strips.Toolbars["Custom1"];
		Strips.tbCustom2 = _strips.Toolbars["Custom2"];

		//get submenus that will be filled later or used separately etc
		_strips.Submenus["File_Templates"].Opening += _Strips_MenuOpening_Templates;
		_strips.Submenus["File_RecentCollections"].Opening += (o, e) => MainForm.Panels.Files.FillMenuRecentCollections(o as ToolStripDropDownMenu);
		(Strips.ddFileNew = _strips.Submenus["File_New"]).Opening += _Strips_MenuOpening_New;
		_strips.Submenus["Tools_Panels"].Opening += (se, da) => PanelManager.AddShowPanelsToMenu(se as ToolStripDropDown, false, true);
		_strips.Submenus["Tools_Toolbars"].Opening += (se, da) => PanelManager.AddShowPanelsToMenu(se as ToolStripDropDown, true, true);
		Strips.ddFile = _strips.Submenus["Menu_File"];
		Strips.ddEdit = _strips.Submenus["Menu_Edit"];
		Strips.ddOutput = _strips.Submenus["Tools_Output"];
		Strips.ddStatusBar = _strips.Submenus["Tools_StatusBar"];

		//get controls
		Strips.cHelpFind = Strips.tbHelp.Items["Help_Find"] as ToolStripSpringTextBox;

		//p.NW();

		this.MainMenuStrip = Strips.Menubar;

#if DEBUG
		//all commands have menu items?
		//var p = new Perf.Inst(true);
		foreach(var k in _cmd.Commands.Keys) {
			//Print(k);
			if(_strips.Xml.Descendant_(k) == null) Output.Warning("no menu item for command " + k);
		}
		//p.NW(); //450
		//for vice versa, GStripManager takes care
#endif
	}

	/// <summary>
	/// Currently not used.
	/// </summary>
	void _Strips_MenuOpening_New(object sender, CancelEventArgs e)
	{
		var dd = sender as ToolStripDropDownMenu;
		dd.SuspendLayout();

		dd.ResumeLayout();
	}

	/// <summary>
	/// Fills submenu File -> New -> Templates.
	/// </summary>
	void _Strips_MenuOpening_Templates(object sender, CancelEventArgs e)
	{
		var dd = sender as ToolStripDropDownMenu;
		dd.SuspendLayout();
		//dd.Items.Clear();
		//TODO
		dd.ResumeLayout();
	}

	/// <summary>
	/// Checks or unchecks command's menu item and toolbar buttons.
	/// </summary>
	/// <param name="cmd">Command name. See Strips.xml.</param>
	/// <param name="check"></param>
	public void CheckCmd(string cmd, bool check)
	{
		var a = _strips.Find(cmd);
		int i, n = a.Count;
		if(n == 0) { DebugPrint("item not found: " + cmd); return; }
		for(i = 0; i < n; i++) {
			switch(a[i]) {
			case ToolStripMenuItem m:
				m.Checked = check;
				break;
			case ToolStripButton b:
				b.Checked = check;
				break;
			}
		}
	}

	/// <summary>
	/// Enables or disables command's menu item and toolbar buttons.
	/// </summary>
	/// <param name="cmd">Command name. See Strips.xml.</param>
	/// <param name="enable"></param>
	public void EnableCmd(string cmd, bool enable)
	{
		var a = _strips.Find(cmd);
		int i, n = a.Count;
		if(n == 0) { DebugPrint("item not found: " + cmd); return; }
		for(i = 0; i < n; i++) {
			a[i].Enabled = enable;
		}
	}

	partial class _Commands :IGStripManagerCallbacks
	{
		internal delegate void CommandHandler();
		Dictionary<string, CommandHandler> _commands = new Dictionary<string, CommandHandler>(200);

		internal Dictionary<string, CommandHandler> Commands { get { return _commands; } }

		EventHandler _onClick;
		EForm _form;

		//Common Click even handler of all items.
		//Calls true item's command handler.
		void _OnClick(object sender, EventArgs args)
		{
			var item = sender as ToolStripItem;
			//Print(item.Name);
			if(!_commands.TryGetValue(item.Name, out var d)) { Debug.Assert(false); return; }
			d();
		}

		public EventHandler GetClickHandler(string itemName)
		{
			if(_commands.ContainsKey(itemName)) return _onClick;
			return null;
		}

		public Image GetImage(string imageName)
		{
			return EResources.GetImageUseCache(imageName);
		}

		public void ItemAdding(ToolStripItem item, ToolStrip owner)
		{
			//PrintList(item, owner);
		}

		internal _Commands()
		{
			_onClick = _OnClick;
			_form = MainForm;

			#region add to _commands
			//Code generated by macro 'Generate Catkeys menu-toolbar command code'.

			_commands.Add(nameof(File_NewScript), File_NewScript);
			_commands.Add(nameof(File_NewLibrary), File_NewLibrary);
			_commands.Add(nameof(File_NewFolder), File_NewFolder);
			_commands.Add(nameof(File_Import), File_Import);
			_commands.Add(nameof(File_Disable), File_Disable);
			_commands.Add(nameof(File_Rename), File_Rename);
			_commands.Add(nameof(File_Delete), File_Delete);
			_commands.Add(nameof(File_Properties), File_Properties);
			_commands.Add(nameof(File_Open), File_Open);
			_commands.Add(nameof(File_OpenInNewWindow), File_OpenInNewWindow);
			_commands.Add(nameof(File_OpenInDefaultApp), File_OpenInDefaultApp);
			_commands.Add(nameof(File_SelectInExplorer), File_SelectInExplorer);
			_commands.Add(nameof(File_PreviousDocument), File_PreviousDocument);
			_commands.Add(nameof(File_Close), File_Close);
			_commands.Add(nameof(File_CloseAll), File_CloseAll);
			_commands.Add(nameof(File_CollapseFolders), File_CollapseFolders);
			_commands.Add(nameof(File_Cut), File_Cut);
			_commands.Add(nameof(File_Copy), File_Copy);
			_commands.Add(nameof(File_Paste), File_Paste);
			_commands.Add(nameof(File_CopyRelativePath), File_CopyRelativePath);
			_commands.Add(nameof(File_CopyFullPath), File_CopyFullPath);
			_commands.Add(nameof(File_PrintSetup), File_PrintSetup);
			_commands.Add(nameof(File_Print), File_Print);
			_commands.Add(nameof(File_OpenCollection), File_OpenCollection);
			_commands.Add(nameof(File_NewCollection), File_NewCollection);
			_commands.Add(nameof(File_ExportCollection), File_ExportCollection);
			_commands.Add(nameof(File_ImportCollection), File_ImportCollection);
			_commands.Add(nameof(File_FindInCollections), File_FindInCollections);
			_commands.Add(nameof(File_CollectionProperties), File_CollectionProperties);
			_commands.Add(nameof(File_SaveAllNow), File_SaveAllNow);
			_commands.Add(nameof(File_Exit), File_Exit);
			_commands.Add(nameof(Edit_Undo), Edit_Undo);
			_commands.Add(nameof(Edit_Redo), Edit_Redo);
			_commands.Add(nameof(Edit_Cut), Edit_Cut);
			_commands.Add(nameof(Edit_Copy), Edit_Copy);
			_commands.Add(nameof(Edit_Paste), Edit_Paste);
			_commands.Add(nameof(Edit_Find), Edit_Find);
			_commands.Add(nameof(Edit_Members), Edit_Members);
			_commands.Add(nameof(Edit_ContextHelp), Edit_ContextHelp);
			_commands.Add(nameof(Edit_GoToDefinition), Edit_GoToDefinition);
			_commands.Add(nameof(Edit_PeekDefinition), Edit_PeekDefinition);
			_commands.Add(nameof(Edit_FindReferences), Edit_FindReferences);
			_commands.Add(nameof(Edit_Indent), Edit_Indent);
			_commands.Add(nameof(Edit_Unindent), Edit_Unindent);
			_commands.Add(nameof(Edit_Comment), Edit_Comment);
			_commands.Add(nameof(Edit_Uncomment), Edit_Uncomment);
			_commands.Add(nameof(Edit_HideRegion), Edit_HideRegion);
			_commands.Add(nameof(Edit_SelectAll), Edit_SelectAll);
			_commands.Add(nameof(Edit_Output), Edit_Output);
			_commands.Add(nameof(Edit_ImagesInCode), Edit_ImagesInCode);
			_commands.Add(nameof(Edit_WrapLines), Edit_WrapLines);
			_commands.Add(nameof(Edit_LineNumbers), Edit_LineNumbers);
			_commands.Add(nameof(Edit_IndentationGuides), Edit_IndentationGuides);
			_commands.Add(nameof(Run_Run), Run_Run);
			_commands.Add(nameof(Run_End), Run_End);
			_commands.Add(nameof(Run_Pause), Run_Pause);
			_commands.Add(nameof(Run_Compile), Run_Compile);
			_commands.Add(nameof(Run_AutoMinimize), Run_AutoMinimize);
			_commands.Add(nameof(Run_DisableTriggers), Run_DisableTriggers);
			_commands.Add(nameof(Run_MakeExe), Run_MakeExe);
			_commands.Add(nameof(Debug_RunToBreakpoint), Debug_RunToBreakpoint);
			_commands.Add(nameof(Debug_RunToCursor), Debug_RunToCursor);
			_commands.Add(nameof(Debug_StepInto), Debug_StepInto);
			_commands.Add(nameof(Debug_StepOver), Debug_StepOver);
			_commands.Add(nameof(Debug_StepOut), Debug_StepOut);
			_commands.Add(nameof(Debug_ToggleBreakpoint), Debug_ToggleBreakpoint);
			_commands.Add(nameof(Debug_PersistentBreakpoint), Debug_PersistentBreakpoint);
			_commands.Add(nameof(Debug_ClearLocalBreakpoints), Debug_ClearLocalBreakpoints);
			_commands.Add(nameof(Debug_ClearAllBreakpoints), Debug_ClearAllBreakpoints);
			_commands.Add(nameof(Debug_DebugOptions), Debug_DebugOptions);
			_commands.Add(nameof(Tools_Record), Tools_Record);
			_commands.Add(nameof(Tools_RecordMenu), Tools_RecordMenu);
			_commands.Add(nameof(Tools_RecordSingleAction), Tools_RecordSingleAction);
			_commands.Add(nameof(Tools_FilesAndTriggers), Tools_FilesAndTriggers);
			_commands.Add(nameof(Tools_DialogEditor), Tools_DialogEditor);
			_commands.Add(nameof(Tools_ToolbarEditor), Tools_ToolbarEditor);
			_commands.Add(nameof(Tools_MenuEditor), Tools_MenuEditor);
			_commands.Add(nameof(Tools_ImagelistEditor), Tools_ImagelistEditor);
			_commands.Add(nameof(Tools_Resources), Tools_Resources);
			_commands.Add(nameof(Tools_Icons), Tools_Icons);
			_commands.Add(nameof(Tools_HelpEditor), Tools_HelpEditor);
			_commands.Add(nameof(Tools_RegularExpressions), Tools_RegularExpressions);
			_commands.Add(nameof(Tools_ExploreWindows), Tools_ExploreWindows);
			_commands.Add(nameof(Tools_RemapKeys), Tools_RemapKeys);
			_commands.Add(nameof(Tools_Components), Tools_Components);
			_commands.Add(nameof(Tools_Portable), Tools_Portable);
			_commands.Add(nameof(Tools_Options), Tools_Options);
			_commands.Add(nameof(Tools_Output_Clear), Tools_Output_Clear);
			_commands.Add(nameof(Tools_Output_Copy), Tools_Output_Copy);
			_commands.Add(nameof(Tools_Output_FindSelectedText), Tools_Output_FindSelectedText);
			_commands.Add(nameof(Tools_Output_History), Tools_Output_History);
			_commands.Add(nameof(Tools_Output_LogWindowEvents), Tools_Output_LogWindowEvents);
			_commands.Add(nameof(Tools_Output_LogAccEvents), Tools_Output_LogAccEvents);
			_commands.Add(nameof(Tools_Output_WrapLines), Tools_Output_WrapLines);
			_commands.Add(nameof(Tools_Output_WhiteSpace), Tools_Output_WhiteSpace);
			_commands.Add(nameof(Tools_Output_Topmost), Tools_Output_Topmost);
			_commands.Add(nameof(Tools_Statusbar_Floating), Tools_Statusbar_Floating);
			_commands.Add(nameof(Tools_Statusbar_MouseInfo), Tools_Statusbar_MouseInfo);
			_commands.Add(nameof(Tools_Statusbar_AutoHeight), Tools_Statusbar_AutoHeight);
			_commands.Add(nameof(Tools_Statusbar_SendToOutput), Tools_Statusbar_SendToOutput);
			_commands.Add(nameof(Help_QuickStart), Help_QuickStart);
			_commands.Add(nameof(Help_Reference), Help_Reference);
			_commands.Add(nameof(Help_ContextHelp), Help_ContextHelp);
			_commands.Add(nameof(Help_Download), Help_Download);
			_commands.Add(nameof(Help_Forum), Help_Forum);
			_commands.Add(nameof(Help_Email), Help_Email);
			_commands.Add(nameof(Help_Donate), Help_Donate);
			_commands.Add(nameof(Help_About), Help_About);

			#endregion add

		}

	}
}
