﻿using Au.Types;
using Au.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Reflection.Emit;

namespace Au
{
	/// <summary>
	/// Based on WPF <see cref="ContextMenu"/>, makes simpler to use it.
	/// </summary>
	/// <example>
	/// <code><![CDATA[
	/// var m = new AContextMenu(inScript: true);
	/// m["One"] = o => AOutput.Write(o);
	/// using(m.Submenu("Sub")) {
	/// 	m["Three"] = o => AOutput.Write(o);
	/// 	m["Four"] = o => AOutput.Write(o);
	/// }
	/// m.Separator();
	/// m["Two"] = o => { AOutput.Write(o); };
	/// m.Show(); //or m.IsOpen=true;
	/// ]]></code>
	/// </example>
	public class AContextMenu : ContextMenu
	{
		/// <param name="inScript">Sets <see cref="CanEditScript"/> and <see cref="ExtractIconPathFromCode"/>.</param>
		/// <param name="actionThread">Sets <see cref="ActionThread"/>.</param>
		public AContextMenu(bool inScript = false, bool actionThread = false) {
			CanEditScript = inScript;
			ExtractIconPathFromCode = inScript;
			ActionThread = actionThread;
		}

		/// <summary>
		/// Creates new <see cref="MenuItem"/> and adds to the menu. Returns it.
		/// </summary>
		/// <param name="text">Label. See <see cref="HeaderedItemsControl.Header"/>.</param>
		/// <param name="icon">See <see cref="this[string, object, string, int]"/>.</param>
		/// <param name="click">Action called on click.</param>
		/// <param name="f">[CallerFilePath]</param>
		/// <param name="l">[CallerLineNumber]</param>
		/// <remarks>
		/// Usually it's easier to use the indexer instead. It just calls this function. See example.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// m["Example"] = o => AOutput.Write(o);
		/// m.LastItem.IsChecked=true;
		/// ]]></code>
		/// </example>
		public MenuItem Add(object text, object icon = null, Action<CMActionArgs> click = null, [CallerFilePath] string f = null, [CallerLineNumber] int l = 0) {
			var i = new _MenuItem(this) { action = click, sourceFile = f, sourceLine = l, exceptOpt = ExceptionHandling, startThread = ActionThread, Header = text };
			i.Icon = MenuItemIcon_(icon, click, ExtractIconPathFromCode);
			CurrentAddMenu.Items.Add(LastItem = i);
			ItemAdded?.Invoke(i);
			return i;
		}

		/// <summary>
		/// Creates new <see cref="MenuItem"/> and adds to the menu.
		/// </summary>
		/// <param name="text">Label. See <see cref="HeaderedItemsControl.Header"/>.</param>
		/// <param name="icon">
		/// Can be:
		/// - <see cref="Image"/> or other WPF control to assign directly to <see cref="MenuItem.Icon"/>.
		/// - string - image file path, or resource name with prefix "resource:", or png image as Base-64 string with prefix "image:". Supports environment variables. If not full path, looks in <see cref="AFolders.ThisAppImages"/>.
		/// - <see cref="Uri"/> - image file path, or resource pack URI, or URL. Does not support environment variables and <see cref="AFolders.ThisAppImages"/>.
		/// - <see cref="AIcon"/> - icon handle. Example: <c>AIcon.Stock(StockIcon.DELETE)]</c>. This function disposes it.
		/// - <b>IntPtr</b> - icon handle. This function does not dispose it. You can dispose at any time.
		/// - <see cref="ImageSource"/> - a WPF image.
		/// 
		/// Prints warning if failed to find or load image file.
		/// To create Base-64 string, use menu Code -> AWinImage.
		/// To add resource in Visual Studio, use build action "Resource".
		/// </param>
		/// <param name="f">[CallerFilePath]</param>
		/// <param name="l">[CallerLineNumber]</param>
		/// <value>Action called on click.</value>
		/// <remarks>
		/// Calls <see cref="Add(object, object, Action{CMActionArgs}, string, int)"/>.
		/// </remarks>
		/// <example>
		/// <code><![CDATA[
		/// m["Example"] = o => AOutput.Write(o);
		/// m.LastItem.IsChecked=true;
		/// ]]></code>
		/// </example>
		public Action<CMActionArgs> this[string text, object icon = null, [CallerFilePath] string f = null, [CallerLineNumber] int l = 0] { set => Add(text, icon, value, f, l); }

		/// <summary>
		/// Adds separator.
		/// </summary>
		public void Separator() { CurrentAddMenu.Items.Add(new Separator()); }

		/// <summary>
		/// Creates new <see cref="MenuItem"/> for a submenu and adds to the menu.
		/// </summary>
		/// <param name="text">Label. See <see cref="HeaderedItemsControl.Header"/>.</param>
		/// <param name="icon"><see cref="MenuItem.Icon"/>.</param>
		/// <param name="click">Action called on click. Rarely used.</param>
		/// <param name="f">[CallerFilePath]</param>
		/// <param name="l">[CallerLineNumber]</param>
		/// <remarks>
		/// Then the add-item functions will add items to the submenu, until the returned variable is disposed.
		/// </remarks>
		/// <example><see cref="AContextMenu"/></example>
		public UsingEndAction Submenu(object text, object icon = null, Action<CMActionArgs> click = null, [CallerFilePath] string f = null, [CallerLineNumber] int l = 0) {
			var mi = Add(text, icon, click, f, l);
			_submenuStack.Push(mi);
			return new UsingEndAction(() => _submenuStack.Pop());
			//CONSIDER: copy some properties of current menu. Or maybe WPF copies automatically, need to test.
		}

		Stack<MenuItem> _submenuStack = new Stack<MenuItem>();
		//	bool _AddingSubmenuItems => _submenuStack.Count > 0;

		/// <summary>
		/// Gets <see cref="ItemsControl"/> of the menu or submenu where new items currently would be added.
		/// </summary>
		public ItemsControl CurrentAddMenu => _submenuStack.Count > 0 ? _submenuStack.Peek() : (ItemsControl)this;

		/// <summary>
		/// Gets the last added <see cref="MenuItem"/>.
		/// </summary>
		public MenuItem LastItem { get; private set; }

		/// <summary>
		/// Called when added a non-separator item.
		/// </summary>
		public Action<MenuItem> ItemAdded { get; set; }

		/// <summary>
		/// On menu item right-click open the source file and line in editor, if possible.
		/// Recommended for automation scripts.
		/// </summary>
		public bool CanEditScript { get; set; }

		/// <summary>
		/// Execute item actions asynchronously in new threads.
		/// Applied to menu items added afterwards.
		/// </summary>
		/// <remarks>
		/// If current thread is a UI thread (has windows etc) or has triggers or hooks, and item action functions execute some long automations etc in current thread, current thread probably is hung during that time. Set this property = true to avoid it.
		/// </remarks>
		public bool ActionThread { get; set; }

		/// <summary>
		/// Whether/how to handle exceptions in menu item action code. Default: <b>Warning</b>.
		/// Applied to menu items added afterwards.
		/// </summary>
		public CMExceptions ExceptionHandling { get; set; }

		/// <summary>
		/// Sets <see cref="ContextMenu.IsOpen"/> = true, waits until closed, closes on Esc or mouse click/far.
		/// </summary>
		/// <remarks>
		/// Use this function when <see cref="ContextMenu.IsOpen"/> would not work well, for example if this thread does not have an active WPF window. Else use the standard ways - set <b>IsOpen</b> = true or assign this object to a WPF control.
		/// </remarks>
		public void Show() {
			IsOpen = true;
			AHookWin kh = null, mh = null;
			var wa = AWnd.ThisThread.Active;
			bool wpfInactive = wa.Is0 || null == HwndSource.FromHwnd(wa.Handle);
			if (wpfInactive)
				kh = AHookWin.Keyboard(k => {
					if (k.IsUp) return;
					//how to enable standard keyboard navigation?
					//			AOutput.Write(k.Key);
					//			switch(k.Key) {
					//			case KKey.Escape: case KKey.Down: case KKey.Up: case KKey.Right: case KKey.Left: case KKey.End: case KKey.Home: case KKey.Enter:
					//				AWnd.More.PostThreadMessage(AThread.NativeId, 0x100, (int)k.Key, 0); //WM_KEYDOWN //does not work, althoug works for an active WPF window.
					//				//var key = Key.Down; RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(this), 0, key) { RoutedEvent = Keyboard.KeyDownEvent }); //does not work
					//				k.BlockEvent();
					//				break;
					//			}
					switch (k.Key) {
					case KKey.Escape:
						IsOpen = false;
						k.BlockEvent();
						break;
					}
				});
			_mouseWasNear = false;
			if (wpfInactive || MouseClosingDistance > 0)
				mh = AHookWin.Mouse(k => {
					if (k.IsButtonDown && wpfInactive) {
						var w = this.Hwnd();
						var z = AWnd.FromMouse(WXYFlags.NeedWindow);
						if (z == w) return;
						if (z.IsOfThisThread && z.ZorderIsAbove(w)) return; //eg a submenu
						IsOpen = false;
					} else if (k.IsMove && MouseClosingDistance > 0) {
						var w = this.Hwnd(); if (!w.IsAlive) return;
						var r = w.Rect;
						foreach (var t in AWnd.GetWnd.ThreadWindows(AThread.NativeId, true)) if (t.ZorderIsAbove(w)) r.Union(t.Rect); //submenus etc
						var p = AMouse.XY;
						int d = (int)AMath.Distance(r, p), d2 = ADpi.Scale(MouseClosingDistance, w);
						if (!_mouseWasNear) _mouseWasNear = d <= d2 / 2; else if (d > d2) IsOpen = false;
					}
				});
			_dispFrame = new DispatcherFrame();
			try { Dispatcher.PushFrame(_dispFrame); }
			finally {
				kh?.Dispose();
				mh?.Dispose();
			}
		}
		DispatcherFrame _dispFrame;
		bool _mouseWasNear;

		///
		protected override void OnClosed(RoutedEventArgs e) {
			if (_dispFrame != null) {
				_dispFrame.Continue = false;
				_dispFrame = null;
			}
			base.OnClosed(e);
			//small problem: OnClose called with 160 ms delay. Same with native message loop.
		}

		/// <summary>
		/// Let <see cref="Show"/> close the menu when the mouse cursor moves away from it to this distance.
		/// </summary>
		/// <remarks>
		/// Default = <see cref="DefaultMouseClosingDistance"/>, default 200.
		/// At first the mouse must move at less than half of the distance.
		/// Set = 0 to disable closing.
		/// The unit is WPF logical pixels, ie for 100% DPI. For example, if the value is 200 and screen DPI is 200%, the actual distance is 400 physical pixels.
		/// </remarks>
		/// <seealso cref="DefaultMouseClosingDistance"/>
		public int MouseClosingDistance { get; set; } = DefaultMouseClosingDistance;

		/// <summary>
		/// Default <see cref="MouseClosingDistance"/> value. Default 200.
		/// </summary>
		public static int DefaultMouseClosingDistance { get; set; } = 200;

		/// <summary>
		/// When adding items without explicitly specified icon, extract icon from item action code.
		/// </summary>
		/// <remarks>
		/// This property is applied to items added afterwards.
		/// </remarks>
		public bool ExtractIconPathFromCode { get; set; }

		/// <summary>
		/// Gets icon path from code that contains string like <c>@"c:\windows\system32\notepad.exe"</c> or <c>@"%AFolders.System%\notepad.exe"</c> or URL/shell.
		/// Also supports code patterns like 'AFolders.System + "notepad.exe"' or 'AFolders.Virtual.RecycleBin'.
		/// Returns null if no such string/pattern.
		/// </summary>
		internal static string IconPathFromCode_(MethodInfo mi) {
			//support code pattern like 'AFolders.System + "notepad.exe"'.
			//	Opcodes: call(AFolders.System), ldstr("notepad.exe"), FolderPath.op_Addition.
			//also code pattern like 'AFolders.System' or 'AFolders.Virtual.RecycleBin'.
			//	Opcodes: call(AFolders.System), FolderPath.op_Implicit(FolderPath to string).
			//also code pattern like 'AFile.TryRun("notepad.exe")'.
			//AOutput.Write(mi.Name);
			int i = 0, patternStart = -1; MethodInfo f1 = null; string filename = null, filename2 = null;
			try {
				var reader = new ILReader(mi);
				foreach (var instruction in reader.Instructions) {
					if (++i > 100) break;
					var op = instruction.Op;
					//AOutput.Write(op);
					if (op == OpCodes.Nop) {
						i--;
					} else if (op == OpCodes.Ldstr) {
						var s = instruction.Data as string;
						//AOutput.Write(s);
						if (i == patternStart + 1) filename = s;
						else {
							if (APath.IsFullPathExpandEnvVar(ref s)) return s; //eg AFile.TryRun(@"%AFolders.System%\notepad.exe");
							if (APath.IsUrl(s) || APath.IsShellPath_(s)) return s;
							filename = null; patternStart = -1;
							if (i == 1) filename2 = s;
						}
					} else if (op == OpCodes.Call && instruction.Data is MethodInfo f && f.IsStatic) {
						//AOutput.Write(f, f.DeclaringType, f.Name, f.MemberType, f.ReturnType, f.GetParameters().Length);
						var dt = f.DeclaringType;
						if (dt == typeof(AFolders) || dt == typeof(AFolders.Virtual)) {
							if (f.ReturnType == typeof(FolderPath) && f.GetParameters().Length == 0) {
								//AOutput.Write(1);
								f1 = f;
								patternStart = i;
							}
						} else if (dt == typeof(FolderPath)) {
							if (i == patternStart + 2 && f.Name == "op_Addition") {
								//AOutput.Write(2);
								var fp = (FolderPath)f1.Invoke(null, null);
								if ((string)fp == null) return null;
								return fp + filename;
							} else if (i == patternStart + 1 && f.Name == "op_Implicit" && f.ReturnType == typeof(string)) {
								//AOutput.Write(3);
								return (FolderPath)f1.Invoke(null, null);
							}
						}
					}
				}
				if (filename2 != null && filename2.Ends(".exe", true)) return AFile.SearchPath(filename2);
			}
			catch (Exception ex) { ADebug.Print(ex); }
			return null;
		}

		internal static object MenuItemIcon_(object icon, Delegate click, bool extractFromCode) {
			if (icon == null && extractFromCode && click != null) {
				var path = IconPathFromCode_(click.Method);
				if (path != null) icon = AIcon.OfFile(path, 16, IconGetFlags.DontSearch);
			}
			if (icon == null) return null;
			try {
				ImageSource iso = null; bool other = false;
				switch (icon) {
				case string s:
					iso = AImageUtil.LoadWpfImageFromFileOrResourceOrString(s);
					break;
				case Uri s:
					iso = BitmapFrame.Create(s);
					break;
				case AIcon h:
					iso = h.ToWpfImage();
					break;
				case IntPtr h:
					iso = new AIcon(h).ToWpfImage(false);
					break;
				case ImageSource s:
					iso = s;
					break;
				default:
					other = true;
					break;
				}
				if (iso != null) icon = new Image { Source = iso };
				else if (!other) icon = null;
			}
			catch (Exception ex) { AWarning.Write(ex.ToStringWithoutStack()); }
			return icon;
		}

		class _MenuItem : MenuItem
		{
			AContextMenu _m;
			public Action<CMActionArgs> action;
			public CMExceptions exceptOpt;
			public bool startThread;
			public string sourceFile;
			public int sourceLine;

			public _MenuItem(AContextMenu m) { _m = m; }

			protected override void OnClick() {
				if (action != null) {
					if (startThread) AThread.Start(() => _ExecItem(), background: false); else _ExecItem();
					void _ExecItem() {
						try {
							action(new CMActionArgs(this));
						}
						catch (Exception ex) when (exceptOpt != CMExceptions.Exception) {
							if (exceptOpt == CMExceptions.Warning) AWarning.Write(ex.ToString(), -1);
						}
					}
				}
				base.OnClick();
			}

			protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
				switch (e.ChangedButton) {
				case MouseButton.Right:
					if (this.HasItems && e.Source != this) break; //workaround for: cannot edit submenu items because then this func at first called for parent item
					e.Handled = true;
					_m.IsOpen = false;
					if (_m.CanEditScript && !sourceFile.NE()) AScriptEditor.GoToEdit(sourceFile, sourceLine);
					//could instead use a AContextMenu here, but: dangerous; closes this menu before showing it.
					break;
				case MouseButton.Middle:
					_m.IsOpen = false;
					break;
				}
				base.OnPreviewMouseUp(e);
			}
		}
	}
}

namespace Au.Types
{

	/// <summary>
	/// Used with <see cref="AContextMenu.ExceptionHandling"/>;
	/// </summary>
	public enum CMExceptions
	{
		/// <summary>Handle exceptions. On exception call <see cref="AWarning.Write"/>. This is default.</summary>
		Warning,

		/// <summary>Don't handle exceptions.</summary>
		Exception,

		/// <summary>Handle exceptions. On exception do nothing.</summary>
		Silent,
	}

	/// <summary>
	/// Arguments for <see cref="AContextMenu"/> item actions.
	/// </summary>
	public class CMActionArgs
	{
		///
		public CMActionArgs(MenuItem item) { Item = item; }

		/// <summary>
		/// The menu item object.
		/// If <see cref="AContextMenu.ActionThread"/> true, it cannot be used directly. A workaround is <c>o.Item.Dispatcher.Invoke(()=>...);</c>.
		/// </summary>
		public MenuItem Item { get; }
		///
		public override string ToString() {
			var d = Item.Dispatcher;
			if (d.Thread == Thread.CurrentThread) return Item.Header.ToString();
			return d.Invoke(() => Item.Header.ToString());
		}
	}
}