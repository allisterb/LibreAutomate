namespace Au.Controls;

using static Sci;

public unsafe partial class KScintilla {

	public void aaaStyleFont(int style, string name) {
		aaaSetString(SCI_STYLESETFONT, style, name);
	}

	//public string aaaStyleFont(int style)
	//{
	//	return aaaGetString(SCI_STYLEGETFONT, style, 100);
	//}

	public void aaaStyleFont(int style, string name, int size) {
		aaaStyleFont(style, name);
		aaaStyleFontSize(style, size);
	}

	/// <summary>Uses only font name and size. Not style etc.</summary>
	public void aaaStyleFont(int style, System.Windows.Controls.Control c) {
		aaaStyleFont(style, c.FontFamily.ToString(), c.FontSize.ToInt() * 72 / 96);
	}

	/// <summary>Segoe UI, 9.</summary>
	public void aaaStyleFont(int style) {
		aaaStyleFont(style, "Segoe UI", 9);
	}

	public void aaaStyleFontSize(int style, int value) {
		Call(SCI_STYLESETSIZE, style, value);
	}

	//public int aaaStyleFontSize(int style)
	//{
	//	return Call(SCI_STYLEGETSIZE, style);
	//}

	public void aaaStyleHidden(int style, bool value) {
		Call(SCI_STYLESETVISIBLE, style, !value);
	}

	//public bool aaaStyleHidden(int style)
	//{
	//	return 0 == Call(SCI_STYLEGETVISIBLE, style);
	//}

	public void aaaStyleBold(int style, bool value) {
		Call(SCI_STYLESETBOLD, style, value);
	}

	public void aaaStyleItalic(int style, bool value) {
		Call(SCI_STYLESETITALIC, style, value);
	}

	public void aaaStyleUnderline(int style, bool value) {
		Call(SCI_STYLESETUNDERLINE, style, value);
	}

	public void aaaStyleEolFilled(int style, bool value) {
		Call(SCI_STYLESETEOLFILLED, style, value);
	}

	public void aaaStyleHotspot(int style, bool value) {
		Call(SCI_STYLESETHOTSPOT, style, value);
	}

	public bool aaaStyleHotspot(int style) {
		return 0 != Call(SCI_STYLEGETHOTSPOT, style);
	}

	public void aaaStyleForeColor(int style, ColorInt color) {
		Call(SCI_STYLESETFORE, style, color.ToBGR());
	}

	public void aaaStyleBackColor(int style, ColorInt color) {
		Call(SCI_STYLESETBACK, style, color.ToBGR());
	}

	/// <summary>
	/// SCI_TEXTWIDTH.
	/// </summary>
	public int aaaStyleMeasureStringWidth(int style, string s) {
		return aaaSetString(SCI_TEXTWIDTH, style, s);
	}

	/// <summary>
	/// Calls SCI_STYLECLEARALL, which sets all styles to be the same as STYLE_DEFAULT.
	/// Then also sets some special styles: STYLE_HIDDEN, SC_ELEMENT_HOT_SPOT_ACTIVE.
	/// </summary>
	/// <param name="belowDefault">Clear only styles 0..STYLE_DEFAULT.</param>
	public void aaaStyleClearAll(bool belowDefault = false) {
		if (belowDefault) aaaStyleClearRange(0, STYLE_DEFAULT);
		else Call(SCI_STYLECLEARALL);
		aaaStyleHidden(STYLE_HIDDEN, true);
		aaaSetElementColor(SC_ELEMENT_HOT_SPOT_ACTIVE, 0x8000FF);
	}

	/// <summary>
	/// Calls SCI_STYLECLEARALL(styleFrom, styleToNotIncluding), which sets range of styles to be the same as STYLE_DEFAULT.
	/// If styleToNotIncluding is 0, clears all starting from styleFrom.
	/// </summary>
	public void aaaStyleClearRange(int styleFrom, int styleToNotIncluding = 0) {
		Call(SCI_STYLECLEARALL, styleFrom, styleToNotIncluding);
	}

	/// <summary>
	/// Gets style at position.
	/// Uses SCI_GETSTYLEAT.
	/// Returns 0 if pos is invalid.
	/// </summary>
	public int aaaStyleGetAt(int pos) {
		return Call(SCI_GETSTYLEAT, pos);
	}
	
	/// <summary>
	/// Sets scintilla's "end-styled position" = to (default int.MaxValue), to avoid SCN_STYLENEEDED notifications.
	/// Fast, just sets a field in scintilla.
	/// </summary>
	/// <remarks>
	/// Scintilla sends SCN_STYLENEEDED, unless a lexer is set. In some cases 1 or several, in some cases many, in some cases every 500 ms.
	/// </remarks>
	public void aaaSetStyled(int to = int.MaxValue) => Call(SCI_STARTSTYLING, to);
	
	/// <summary>
	/// SCI_SETELEMENTCOLOUR.
	/// </summary>
	/// <param name="element">SC_ELEMENT_.</param>
	/// <param name="color">Color. Can be with alpha.</param>
	public void aaaSetElementColor(int element, ColorInt color) {
		Call(SCI_SETELEMENTCOLOUR, element, color.ToBGR(zeroAlpha: false));
	}
	
	/// <summary>
	/// SCI_GETELEMENTCOLOUR.
	/// </summary>
	/// <param name="element">SC_ELEMENT_.</param>
	public ColorInt aaaGetElementColor(int element) {
		return ColorInt.FromBGR(Call(SCI_GETELEMENTCOLOUR, element), false);
	}
}
