﻿
using static Au.wnd.Internal_;

namespace Au
{
	/// <summary>
	/// Finds window controls (child windows). Contains name and other parameters of controls to find.
	/// </summary>
	/// <remarks>
	/// Can be used instead of <see cref="wnd.Child"/> or <see cref="wnd.ChildAll"/>.
	/// Also can be used to find window that contains certain control, like in the example.
	/// </remarks>
	/// <example>
	/// Find window that contains certain control, and get the control too.
	/// <code><![CDATA[
	/// var f = new wndChildFinder("Password*", "Static"); //control properties
	/// wnd w = wnd.find(cn: "#32770", also: t => f.Exists(t));
	/// print.it(w);
	/// print.it(f.Result);
	/// ]]></code>
	/// </example>
	public class wndChildFinder
	{
		enum _NameIs { name, id, text, elmName, wfName }

		readonly wildex _name;
		readonly wildex _className;
		readonly Func<wnd, bool> _also;
		WinformsControlNames _wfControls;
		readonly int _skipCount;
		readonly WCFlags _flags;
		readonly _NameIs _nameIs;
		readonly int _id;

		/// <summary>
		/// See <see cref="wnd.Child"/>.
		/// </summary>
		/// <exception cref="ArgumentException">See <see cref="wnd.Child"/>.</exception>
		public wndChildFinder(
			[ParamString(PSFormat.wildex)] string name = null,
			[ParamString(PSFormat.wildex)] string cn = null,
			WCFlags flags = 0, Func<wnd, bool> also = null, int skip = 0
			) {
			if (cn != null) {
				if (cn.Length == 0) throw new ArgumentException("Class name cannot be \"\". Use null.");
				_className = cn;
			}
			if (name != null) {
				switch (StringUtil.ParseParam3Stars(ref name, "id", "text", "elmName", "wfName"/*, "label"*/)) {
				case -1: throw new ArgumentException("Invalid name prefix. Can be: \"***id \", \"***text \", \"***elmName \", \"***wfName \"."); //, \"***label \"
				case 1: _nameIs = _NameIs.id; _id = name.ToInt(); break;
				case 2: _nameIs = _NameIs.text; break;
				case 3: _nameIs = _NameIs.elmName; break;
				case 4: _nameIs = _NameIs.wfName; break;
					//case 5: _nameIs = _NameIs.label; break;
				}
				if (_nameIs != _NameIs.id) _name = name;
			}
			_flags = flags;
			_also = also;
			_skipCount = skip;
		}

		/// <summary>
		/// The found control.
		/// </summary>
		public wnd Result { get; internal set; }

		/// <summary>
		/// Finds the specified child control, like <see cref="wnd.Child"/>.
		/// </summary>
		/// <returns>If found, returns <see cref="Result"/>, else default(wnd).</returns>
		/// <param name="wParent">Direct or indirect parent window. Can be top-level window or control.</param>
		/// <exception cref="AuWndException">Invalid <i>wParent</i>.</exception>
		/// <remarks>
		/// Functions <b>Find</b> and <b>Exists</b> differ only in their return types.
		/// </remarks>
		public wnd Find(wnd wParent) => Exists(wParent) ? Result : default;

		/// <summary>
		/// Finds the specified child control, like <see cref="wnd.Child"/>. Can wait and throw <b>NotFoundException</b>.
		/// </summary>
		/// <returns>If found, returns <see cref="Result"/>. Else throws exception or returns default(wnd) (if <i>waitS</i> negative).</returns>
		/// <param name="wParent">Direct or indirect parent window. Can be top-level window or control.</param>
		/// <param name="waitS">The wait timeout, seconds. If 0, does not wait. If negative, does not throw exception when not found.</param>
		/// <exception cref="AuWndException">Invalid <i>wParent</i>.</exception>
		/// <exception cref="NotFoundException" />
		/// <remarks>
		/// Functions <b>Find</b> and <b>Exists</b> differ only in their return types.
		/// </remarks>
		public wnd Find(wnd wParent, double waitS) => Exists(wParent, waitS) ? Result : default;

		/// <inheritdoc cref="Find(wnd)"/>
		/// <returns>If found, sets <see cref="Result"/> and returns true, else false.</returns>
		public bool Exists(wnd wParent) {
			using var k = new WndList_(_AllChildren(wParent));
			return _FindInList(wParent, k) >= 0;
		}

		/// <inheritdoc cref="Find(wnd, double)"/>
		/// <returns>If found, sets <see cref="Result"/> and returns true. Else throws exception or returns false (if <i>waitS</i> negative).</returns>
		public bool Exists(wnd wParent, double waitS) {
			var r = waitS == 0d ? Exists(wParent) : wait.forCondition(waitS < 0 ? waitS : -waitS, () => Exists(wParent));
			return r || waitS < 0 ? r : throw new NotFoundException();
		}

		ArrayBuilder_<wnd> _AllChildren(wnd wParent) {
			wParent.ThrowIfInvalid();
			return EnumWindows2(EnumAPI.EnumChildWindows,
				onlyVisible: 0 == (_flags & WCFlags.HiddenToo),
				sortFirstVisible: true,
				wParent: wParent,
				directChild: 0 != (_flags & WCFlags.DirectChild));
		}

		/// <summary>
		/// Finds the specified control in a list of controls.
		/// Returns 0-based index, or -1 if not found.
		/// The <see cref="Result"/> property will be the control.
		/// </summary>
		/// <param name="a">List of controls, for example returned by <see cref="wnd.getwnd.Children"/>.</param>
		/// <param name="wParent">Direct or indirect parent window. Used only for flag DirectChild.</param>
		public int FindInList(IEnumerable<wnd> a, wnd wParent = default) {
			using var k = new WndList_(a);
			return _FindInList(wParent, k);
		}

		/// <summary>
		/// Finds all matching child controls, like <see cref="wnd.ChildAll"/>.
		/// Returns array containing 0 or more control handles as wnd.
		/// </summary>
		/// <param name="wParent">Direct or indirect parent window. Can be top-level window or control.</param>
		/// <exception cref="AuWndException">Invalid wParent.</exception>
		public wnd[] FindAll(wnd wParent) {
			return _FindAll(new WndList_(_AllChildren(wParent)), wParent);
		}

		/// <summary>
		/// Finds all matching controls in a list of controls.
		/// Returns array containing 0 or more control handles as wnd.
		/// </summary>
		/// <param name="a">List of controls, for example returned by <see cref="wnd.getwnd.Children"/>.</param>
		/// <param name="wParent">Direct or indirect parent window. Used only for flag DirectChild.</param>
		public wnd[] FindAllInList(IEnumerable<wnd> a, wnd wParent = default) {
			return _FindAll(new WndList_(a), wParent);
		}

		wnd[] _FindAll(WndList_ k, wnd wParent) {
			using (k) {
				using var ab = new ArrayBuilder_<wnd>();
				_FindInList(wParent, k, w => ab.Add(w)); //CONSIDER: ab could be part of _WndList. Now the delegate creates garbage.
				return ab.ToArray();
			}
		}

		/// <summary>
		/// Returns index of matching element or -1.
		/// Returns -1 if using getAll.
		/// </summary>
		/// <param name="wParent">Parent window. Can be default(wnd) if inList is true and no DirectChild flag and not using winforms name.</param>
		/// <param name="a">List of wnd. Does not dispose it.</param>
		/// <param name="getAll">If not null, calls it for all matching and returns -1.</param>
		int _FindInList(wnd wParent, WndList_ a, Action<wnd> getAll = null) {
			Result = default;
			if (a.Type == WndList_.ListType.None) return -1;
			bool inList = a.Type != WndList_.ListType.ArrayBuilder;
			int skipCount = _skipCount;

			try { //will need to dispose something
				for (int index = 0; a.Next(out wnd w); index++) {
					if (w.Is0) continue;

					if (inList) { //else the enum function did this
						if (!_flags.Has(WCFlags.HiddenToo)) {
							if (!w.IsVisibleIn_(wParent)) continue;
						}

						if (_flags.Has(WCFlags.DirectChild) && !wParent.Is0) {
							if (w.ParentGWL_ != wParent) continue;
						}
					}

					if (_nameIs == _NameIs.id) {
						if (w.ControlId != _id) continue;
					}

					if (_className != null) {
						if (!_className.Match(w.ClassName)) continue;
					}

					if (_name != null) {
						string s;
						switch (_nameIs) {
						case _NameIs.text:
							s = w.ControlText;
							break;
						//case _NameIs.label:
						//	s = w.NameLabel;
						//	break;
						case _NameIs.elmName:
							s = w.NameElm;
							break;
						case _NameIs.wfName:
							if (_wfControls == null) {
								try {
									_wfControls = new WinformsControlNames(wParent.Is0 ? w : wParent);
								}
								catch (AuWndException) { //invalid parent window
									return -1;
								}
								catch (AuException e) { //probably process of higher UAC integrity level
									print.warning($"Failed to get winforms control names. {e.Message}");
									return -1;
								}
							}
							s = _wfControls.GetControlName(w);
							break;
						default:
							Debug.Assert(_nameIs == _NameIs.name);
							s = w.Name;
							break;
						}

						if (!_name.Match(s)) continue;
					}

					if (_also != null && !_also(w)) continue;

					if (getAll != null) {
						getAll(w);
						continue;
					}

					if (skipCount-- > 0) continue;

					Result = w;
					return index;
				}
			}
			finally {
				if (_wfControls != null) { _wfControls.Dispose(); _wfControls = null; }
			}

			return -1;
		}

		/// <summary>
		/// Returns true if control c properties match the specified properties.
		/// </summary>
		/// <param name="c">A control. Can be 0/invalid, then returns false.</param>
		/// <param name="wParent">Direct or indirect parent window. If used, returns false if it isn't parent (also depends on flag DirectChild).</param>
		public bool IsMatch(wnd c, wnd wParent = default) {
			if (!wParent.Is0 && !c.IsChildOf(wParent)) {
				Result = default;
				return false;
			}
			return 0 == _FindInList(wParent, new WndList_(c));
		}
	}
}
