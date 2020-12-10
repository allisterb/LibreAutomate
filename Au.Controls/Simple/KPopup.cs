﻿using Au.Types;
using Au.Util;
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
using System.Windows.Interop;
using System.Windows;
using System.Windows.Controls;
//using System.Linq;

namespace Au.Controls
{
	public class KPopup
	{
		WS _style;
		WS2 _exStyle;
		SizeToContent _sizeToContent;
		bool _shadow;
		HwndSource _hs;
		AWnd _w;
		bool _inSizeMove;

		///
		public KPopup(WS style = WS.POPUP | WS.THICKFRAME, WS2 exStyle = WS2.TOOLWINDOW | WS2.NOACTIVATE, bool shadow = false, SizeToContent sizeToContent = default) {
			_style = style;
			_exStyle = exStyle;
			_shadow = shadow;
			_sizeToContent = sizeToContent;
		}

		HwndSource _Create() {
			if (_hs == null) {
				var p = new HwndSourceParameters {
					WindowStyle = (int)_style,
					ExtendedWindowStyle = (int)_exStyle,
					WindowClassStyle = _shadow && !_style.Has(WS.THICKFRAME) ? (int)Api.CS_DROPSHADOW : 0,
					WindowName = _windowName,
					//AcquireHwndFocusInMenuMode = false,
					//RestoreFocusMode = System.Windows.Input.RestoreFocusMode.None,
					//TreatAsInputRoot = false,
					HwndSourceHook = _Hook
				};
				_border ??= new Border { Child = _content }; //workaround for: if content is eg FlowDocumentScrollViewer, it has focus problems if it is RootVisual. Eg context menu items disabled. Need a container, eg Border or Panel.
				_hs = new _HwndSource(p) { kpopup = this, RootVisual = _border, SizeToContent = default };
				//AOutput.Write(_hs.AcquireHwndFocusInMenuMode, _hs.RestoreFocusMode, p.TreatAsInputRoot); //True, Auto, True
			}
			return _hs;
		}

		class _HwndSource : HwndSource
		{
			public _HwndSource(HwndSourceParameters p) : base(p) { }
			public KPopup kpopup;
		}

		public static KPopup FromHwnd(AWnd w) {
			if (w.IsAlive && HwndSource.FromHwnd(w.Handle) is _HwndSource hs) return hs.kpopup;
			return null;
		}

		/// <summary>
		/// Gets popup window handle. Returns default(AWnd) if not created (also after destroying).
		/// </summary>
		public AWnd Hwnd => _w;

		//public HwndSource HwndSource => _hs ?? _Create();

		/// <summary>
		/// Gets or sets window name.
		/// </summary>
		public string WindowName {
			get => _windowName;
			set {
				_windowName = value;
				if (!_w.Is0) _w.SetText(value);
			}
		}
		string _windowName;

		/// <summary>
		/// Gets or sets this <b>KPopup</b> object name. It is not window name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets WPF content. It is child of <see cref="Border"/>.
		/// </summary>
		public UIElement Content {
			get => _border?.Child ?? _content;
			set {
				_content = value;
				if (_border != null) _border.Child = value;
			}
		}
		UIElement _content;

		/// <summary>
		/// Gets the WPF root object (<see cref="HwndSource.RootVisual"/>) of the popup window. Its child is <see cref="Content"/>.
		/// </summary>
		public Border Border => _border;
		Border _border;

		/// <summary>
		/// Desired window size. WPF logical pixels.
		/// Actual size can be smaller if would not fit in screen.
		/// </summary>
		public SIZE Size {
			get => _size;
			set {
				_size = value;
				if (IsVisible) {
					var z = ADpi.Scale(_size, _w);
					_w.ResizeLL(z.width, z.height);
				}
			}
		}
		SIZE _size;

		/// <summary>
		/// Shows the popup window by a window, WPF element or rectangle.
		/// </summary>
		/// <param name="owner">Provides owner window and optionally rectangle. Can be <b>FrameworkElement</b>, <b>KPopup</b>, <b>AWnd</b> or null.</param>
		/// <param name="side">Show at this side of rectangle, or opposite side if does not fit in screen.</param>
		/// <param name="rScreen">Rectangle in screen (physical pixels). If null, uses owner's rectangle. Cannot be both null.</param>
		/// <param name="exactSize">If does not fit in screen, cover part of rectangle but don't make smaller.</param>
		/// <param name="exactSide">Never show at opposite side.</param>
		/// <exception cref="NotSupportedException">Unsupported <i>owner</i> type.</exception>
		/// <exception cref="ArgumentException">Both owner and rScreen are null. Or owner handle not created.</exception>
		/// <exception cref="InvalidOperationException"><see cref="Size"/> not set.</exception>
		/// <remarks>
		/// </remarks>
		public void ShowByRect(object owner, Dock side, RECT? rScreen = null, bool exactSize = false, bool exactSide = false) {
			//CloseHides = true; //AOutput.Write(_w); //18 -> 5 ms
			//APerf.First(); if(owner is FrameworkElement test) test.Dispatcher.InvokeAsync(() => APerf.NW());

			AWnd ow = default;
			switch (owner) {
			case null:
				if (rScreen == null) throw new ArgumentException("owner and rScreen are null");
				break;
			case FrameworkElement e:
				ow = e.Hwnd().Window;
				rScreen ??= e.RectInScreen();
				break;
			case KPopup p:
				ow = p._w;
				rScreen ??= ow.Rect;
				break;
			case AWnd w:
				ow = w;
				rScreen ??= ow.Rect;
				break;
			default: throw new NotSupportedException("owner type");
			}
			if (owner != null && ow.Is0) throw new ArgumentException("owner window not created");
			if (_size == default && _sizeToContent != SizeToContent.WidthAndHeight) throw new InvalidOperationException("Size not set");

			_Create();

			//could use API CalculatePopupWindowPosition instead of this code, but it is not exactly what need here.
			RECT r = rScreen.Value;
			var screen = AScreen.Of(r);
			var rs = screen.WorkArea;
			int dpi = screen.Dpi;
			SIZE size = ADpi.Scale(Size, dpi);

			if (_sizeToContent != default) {
				RECT nc = default;
				ADpi.AdjustWindowRectEx(dpi, ref nc, _style, _exStyle);
				rs.left -= nc.left; rs.right -= nc.right; rs.top -= nc.top; rs.bottom -= nc.bottom;

				if (_sizeToContent.Has(SizeToContent.Width)) size.width = rs.Width; else size.width = Math.Min(size.width, rs.Width);
				if (_sizeToContent.Has(SizeToContent.Height)) size.height = rs.Height; else size.height = Math.Min(size.height, rs.Height);
				var c = _border;
				c.Measure(ADpi.Unscale(size, dpi));
				//never mind: measures only height. It seems FlowDocument cannot measure width.
				//	If need width, could instead use TextBlock. It does not support paragraph etc, but we can use multiple TextBlock etc in StackPanel.
				//	But then no select/copy, not so easy scrolling, etc.
				c.UpdateLayout();
				size = ADpi.Scale(c.DesiredSize, dpi);
				size.width += nc.Width + 1; size.height += nc.Height + 1;
			}
			size.width = Math.Min(size.width, rs.Width);
			size.height = Math.Min(size.height, rs.Height);

			int spaceT = r.top - rs.top, spaceB = rs.bottom - r.bottom, spaceL = r.left - rs.left, spaceR = rs.right - r.right;
			if (!exactSide) {
				switch (side) {
				case Dock.Left:
					if (size.width > spaceL && spaceR > spaceL) side = Dock.Right;
					break;
				case Dock.Right:
					if (size.width > spaceR && spaceL > spaceR) side = Dock.Left;
					break;
				case Dock.Top:
					if (size.height > spaceT && spaceB > spaceT) side = Dock.Bottom;
					break;
				default:
					if (size.height > spaceB && spaceT > spaceB) side = Dock.Top;
					break;
				}
			}
			if (!exactSize) {
				switch (side) {
				case Dock.Left:
					if (size.width > spaceL) size.width = Math.Max(spaceL, size.width / 2);
					break;
				case Dock.Right:
					if (size.width > spaceR) size.width = Math.Max(spaceR, size.width / 2);
					break;
				case Dock.Top:
					if (size.height > spaceT) size.height = Math.Max(spaceT, size.height / 2);
					break;
				default:
					if (size.height > spaceB) size.height = Math.Max(spaceB, size.height / 2);
					break;
				}
			}
			switch (side) {
			case Dock.Left: r.left -= size.width; break;
			case Dock.Right: r.left = r.right; break;
			case Dock.Top: r.top -= size.height; break;
			default: r.top = r.bottom; break;
			}
			r.left = Math.Clamp(r.left, rs.left, rs.right - size.width);
			r.top = Math.Clamp(r.top, rs.top, rs.bottom - size.height);
			r.Width = size.width; r.Height = size.height;

			_inSizeMove = false;
			_w.MoveLL(r);
			if (_w.OwnerWindow != ow) _w.OwnerWindow = ow;
			if (!ow.Is0) {
				var op = ow.Get.EnabledOwned(false);
				if (!op.Is0 && op != _w) _w.ZorderAbove(op);
			} //else should be topmost
			if (IsVisible) return;
			_w.ShowLL(true);
		}

		/// <summary>
		/// Destroys or hides the popup window, depending on <see cref="CloseHides"/>.
		/// </summary>
		public void Close() {
			if (CloseHides) _w.ShowLL(false);
			else _hs.Dispose();
		}

		/// <summary>
		/// Don't destroy the popup window when closing, but just hide.
		/// In any case, if destroyed, <b>ShowX</b> will create new window.
		/// </summary>
		public bool CloseHides { get; set; }

		/// <summary>
		/// Whether the popup window is currently visible.
		/// </summary>
		public bool IsVisible => _w.IsVisible;

		/// <summary>
		/// When the popup window becomes invisible. It also happend when destroying.
		/// </summary>
		public event EventHandler Hidden;

		/// <summary>
		/// When destroying the popup window (WM_NCDESTROY).
		/// </summary>
		public event EventHandler Destroyed;

		unsafe nint _Hook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled) {
			var r = _Hook((AWnd)hwnd, msg, wParam, lParam);
			handled = r != null;
			return r ?? 0;
		}

		unsafe nint? _Hook(AWnd w, int msg, nint wParam, nint lParam) {
			//AWnd.More.PrintMsg((AWnd)hwnd, msg, wParam, lParam);
			//if (msg == Api.WM_ACTIVATE && wParam != 0) AOutput.Write("ACTIVATE");

			switch (msg) {
			case Api.WM_NCCREATE:
				_w = w;
				break;
			case Api.WM_NCDESTROY:
				Destroyed?.Invoke(this, EventArgs.Empty);
				_hs.RootVisual = null;
				_hs = null;
				_w = default;
				break;
			case Api.WM_WINDOWPOSCHANGED:
				var wp = (Api.WINDOWPOS*)lParam;
				//AOutput.Write(wp->flags & Native.SWP._KNOWNFLAGS, IsVisible);
				if (wp->flags.Has(Native.SWP.HIDEWINDOW)) Hidden?.Invoke(this, EventArgs.Empty);
				if (!wp->flags.Has(Native.SWP.NOSIZE) && _inSizeMove) _size = (SIZE)ADpi.Unscale((wp->cx, wp->cy), w);
				break;
			case Api.WM_ENTERSIZEMOVE:
				_inSizeMove = true;
				break;
			case Api.WM_EXITSIZEMOVE:
				_inSizeMove = false;
				break;
			case Api.WM_MOUSEACTIVATE:
				//OS ignores WS_EX_NOACTIVATE if the active window is of this thread. Workaround: on WM_MOUSEACTIVATE return MA_NOACTIVATE.
				switch (AMath.HiShort(lParam)) {
				case Api.WM_MBUTTONDOWN:
					Close(); //never mind: we probably don't receive this message if our thread is inactive
					return Api.MA_NOACTIVATEANDEAT;
				}
				if (_exStyle.Has(WS2.NOACTIVATE)) {
					return Api.MA_NOACTIVATE;
				}
				break;
			case Api.WM_NCLBUTTONDOWN:
				if (_exStyle.Has(WS2.NOACTIVATE)) {
					//OS activates when clicked in non-client area, eg when moving or resizing. Workaround: on WM_NCLBUTTONDOWN suppress activation with a CBT hook.
					//When moving or resizing, WM_NCLBUTTONDOWN returns when moving/resizing ends. On resizing would activate on mouse button up.
					var wa = AWnd.ThisThread.Active;
					if (wa != default && wa != w) {
						using (AHookWin.ThreadCbt(d => d.code == HookData.CbtEvent.ACTIVATE && d.Hwnd == w))
							Api.DefWindowProc(w, msg, wParam, lParam);
						return 0;
					}
				}
				break;
			case Api.WM_DPICHANGED:
				_hs.DpiChangedWorkaround();
				break;
			}
			return null;
		}
	}
}
