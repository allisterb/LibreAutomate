using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
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
//using System.Linq;
//using System.Xml.Linq;

using Au;
using Au.Types;
using static Au.NoClass;
using Au.Util;

/*
Most tags are like in QM2.

NEW TAGS:
   <bi> - bold italic.
   <mono> - monospace font.
   <size n> - font size (1-127).
   <fold> - collapsed lines.
   <explore> - select file in File Explorer.

NEW PARAMETERS:
   <c ColorName> - .NET color name for text color. Also color can be #RRGGBB.
   <z ColorName> - .NET color name for background color. Also color can be #RRGGBB.
   <Z ColorName> - .NET color name for background color, whole line. Also color can be #RRGGBB.

RENAMED TAGS:
	<script>, was <macro>.

REMOVED TAGS:
	<tip>.
	<mes>, <out>. Now use <fold>.

DIFFERENT SYNTAX:
	Most tags can be closed with <> or </> or </anything>.
		Except these: <_>text</_>, <code>code</code>, <fold>text</fold>.
		No closing tag: <image>.
	Attributes can be enclosed with "" or '' or non-enclosed (except for <image>).
		Does not support escape sequences. An attribute ends with "> (if starts with ") or '> (if starts with ') or > (if non-enclosed).
		In QM2 need "" for most; some can be non-enclosed. QM2 supports escape sequences.
	Link tag attribute parts now are separated with "|". In QM2 was " /".

OTHER CHANGES:
	Supports user-defined link tags. Need to provide delegates of functions that implement them. Use SciTags.AddCommonLinkTag or SciTags.AddLinkTag.
	These link tags are not implemented by this class, but you can provide delegates of functions that implement them:
		<open>, <script> (QM2 <macro>).
	<help> by default calls Au.Util.Help.AuHelp, which opens a topic in "Au Help.chm". Use like <help topicFileNameWithoutHtm>Link text</help>. You can override it with SciTags.AddCommonLinkTag or SciTags.AddLinkTag.
	<code> attributes are not used. Currently supports only C# code; for it uses the C++ lexer.

CHANGES IN <image>:
	Don't need the closing tag (</image>).
	Supports managed image resources of the entry assembly. Syntax: <image "resource:ResourceName". Does not support resources from forms or other assemblies.
	Supports images embedded directly in text, like "~:BmpFileData_EncodedBase64_CompressedDeflateStream".
	Currently supports only 16x16 icons. Does not support icon resources.
	
*/

namespace Au.Controls
{
	using static Sci;

	/// <summary>
	/// Adds links and text formatting to a <see cref="AuScintilla"/> control.
	/// Links and formatting is specified in text, using tags like in HTML. Reference is in Remarks.
	/// Tags are supported by the <b>Print</b> function when it writes to the Au script editor.
	/// </summary>
	/// <remarks>
	/// Links and text formatting is specified in control text using tags similar to HTML. Depending on control style, may need prefix <c><![CDATA[<>]]></c>.
	/// 
	/// For most tags use this format: <c><![CDATA[<tag>text<>]]></c> or <c><![CDATA[<tag attribute>text<>]]></c>. Attribute can be enclosed in ' or " (must be enclosed if contains &gt;).
	/// For these tags use different format: <c><![CDATA[<_>literal text</_>]]></c>, <c><![CDATA[<code>code</code>]]></c>, <c><![CDATA[<fold>text</fold>]]></c>, <c><![CDATA[<image "attribute">]]></c>.
	/// Tags can be nested, like <c><![CDATA[<b><c green>text<><>]]></c> or <c><![CDATA[<b>text <c green>text<> text<>]]></c>.
	/// 
	/// Simple formatting tags:
	/// <list type="table">
	/// <listheader>
	/// <term>Examples</term>
	/// <term>Comments</term>
	/// </listheader>
	/// <item>
	/// <description><c><![CDATA[<b>text<>]]></c></description>
	/// <description>Bold text.</description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<i>text<>]]></c></description>
	/// <description>Italic text.</description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<bi>text<>]]></c></description>
	/// <description>Bold italic.</description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<u>text<>]]></c></description>
	/// <description>Underline.</description>
	/// </item>
	/// <item>
	/// <description>
	/// <para><c><![CDATA[<c 0xE0A000>text<>]]></c></para>
	/// <para><c><![CDATA[<c #E0A000>text<>]]></c></para>
	/// <para><c><![CDATA[<c green>text<>]]></c></para>
	/// </description>
	/// <description>
	/// Text color.
	/// Color can be 0xRRGGBB, #RRGGBB or .NET color name.
	/// </description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<z yellow>text<>]]></c></description>
	/// <description>Text background color.</description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<Z wheat>text<>]]></c></description>
	/// <description>Line background color, when followed by new line.</description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<size 10>text<>]]></c></description>
	/// <description>
	/// Font height.
	/// Note: it can increase height of all lines.
	/// </description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<mono>text<>]]></c></description>
	/// <description>Monospace font.</description>
	/// </item>
	/// </list>
	/// 
	/// Links:
	/// <list type="table">
	/// <listheader>
	/// <term>Examples</term>
	/// <term>Comments</term>
	/// </listheader>
	/// <item>
	/// <description>
	/// <para><c><![CDATA[<link http://www.example.com>link text<>]]></c></para>
	/// <para><c><![CDATA[<link C:\files\example.exe>link text<>]]></c></para>
	/// <para><c><![CDATA[<link>http://www.example.com<>]]></c></para>
	/// <para><c><![CDATA[<link>C:\files\example.exe<>]]></c></para>
	/// <para><c><![CDATA[<link C:\example.exe|args>link text<>]]></c></para>
	/// </description>
	/// <description>
	/// Opens a web page or runs a program or any file.
	/// Calls function <see cref="Shell.TryRun"/>.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <para><c><![CDATA[<google find this>link text<>]]></c></para>
	/// <para><c><![CDATA[<google>find this<>]]></c></para>
	/// <para><c><![CDATA[<google find this|append>link text<>]]></c></para>
	/// </description>
	/// <description>
	/// Google.
	/// Uses this code:
	/// <c>Shell.TryRun("http://www.google.com/search?q=" + Uri.EscapeDataString(s1) + s2);</c>
	/// Here s1 and s2 are attribute parts separated by |.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <c><![CDATA[<help M_Au_Wnd_Find>link text<>]]></c>
	/// </description>
	/// <description>
	/// Shows an Au class library help topic (file "Au Help.chm").
	/// Calls <see cref="Util.Help_.AuWeb"/>.
	/// Attribute: right click in the help topic, click Properties, copy filename from the Address field.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <para><c><![CDATA[<open document>link text<>]]></c></para>
	/// <para><c><![CDATA[<open>document<>]]></c></para>
	/// <para><c><![CDATA[<open document|5>link text<>]]></c></para>
	/// <para><c><![CDATA[<open document|5|15>link text<>]]></c></para>
	/// </description>
	/// <description>
	/// Should open a document or script or some other item in the program.
	/// This control does not implement this tag. If used, it must be implemented by the program.
	/// See <see cref="AddLinkTag"/>, <see cref="AddCommonLinkTag"/>.
	/// The 5 is 1-based line index; 15 is 1-based character index in line.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <para><c><![CDATA[<explore>C:\files\example<>]]></c></para>
	/// </description>
	/// <description>
	/// Selects file or folder in File Explorer.
	/// Calls function <see cref="Shell.SelectFileInExplorer"/>.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <para><c><![CDATA[<script script>link text<>]]></c></para>
	/// <para><c><![CDATA[<script>script<>]]></c></para>
	/// <para><c><![CDATA[<script script|args0|args1>link text<>]]></c></para>
	/// </description>
	/// <description>
	/// Should run/execute a script.
	/// This control does not implement this tag. If used, it must be implemented by the program.
	/// See <see cref="AddLinkTag"/>, <see cref="AddCommonLinkTag"/>.
	/// </description>
	/// </item>
	/// </list>
	/// Also you can register custom link tags that call your callback functions. See <see cref="AddLinkTag"/>, <see cref="AddCommonLinkTag"/>.
	/// 
	/// Other tags:
	/// <list type="table">
	/// <listheader>
	/// <term>Examples</term>
	/// <term>Comments</term>
	/// </listheader>
	/// <item>
	/// <description><c><![CDATA[<_>text</_>]]></c></description>
	/// <description>Literal text. Tags in it are ignored.</description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<code>var s="example";</code>]]></c></description>
	/// <description>Colored C# code. Tags in it are ignored.</description>
	/// </item>
	/// <item>
	/// <description><c><![CDATA[<fold>text</fold>]]></c></description>
	/// <description>
	/// Folded (hidden) lines. Adds a link to unfold (show).
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// <para><c><![CDATA[<image "c:\images\example.png">]]></c></para>
	/// <para><c><![CDATA[<image "c:\files\example.txt">]]></c></para>
	/// <para><c><![CDATA[<image "image:PngBase64">]]></c></para>
	/// <para><c><![CDATA[<image "~:BmpCompressedBase64">]]></c></para>
	/// <para><c><![CDATA[<image "resource:ResourceName">]]></c></para>
	/// </description>
	/// <description>
	/// Image.
	/// Images are displayed below this line.
	/// Supports images of formats: png, bmp, jpg, gif, ico (only 16x16).
	/// For other file types and folders displays small file icon.
	/// Supports managed image resources of the entry assembly. Not icons.
	/// Image file data is encoded as Base64 string. For it can be used dialog
	/// "Find image or color in window" (form <b>Au.Tools.Form_WinImage</b> in Au.Tools.dll)
	/// or function Au.Controls.ImageUtil.ImageToString (in Au.Controls.dll).
	/// </description>
	/// </item>
	/// </list>
	/// 
	/// Tags are supported by some existing controls based on <see cref="AuScintilla"/>. In the Au script editor it is the output (use <b>Print</b>, like in the example below). In this library - the <see cref="AuInfoBox"/> control. To enable tags in other <see cref="AuScintilla"/> controls, use <see cref="AuScintilla.InitTagsStyle"/> and optionally <see cref="AuScintilla.InitImagesStyle"/>.
	/// </remarks>
	/// <example>
	/// <code><![CDATA[
	/// Print("<>Text with <i>tags<>.");
	/// ]]></code>
	/// </example>
	public unsafe class SciTags
	{
		const int STYLE_FIRST_EX = STYLE_LASTPREDEFINED + 1;
		const int NUM_STYLES_EX = STYLE_MAX - STYLE_LASTPREDEFINED;

		struct _TagStyle
		{
			uint u1, u2;

			//u1
			public int Color { get => (int)(u1 & 0xffffff); set => u1 = (u1 & 0xff000000) | ((uint)value & 0xffffff) | 0x1000000; }
			public bool HasColor => 0 != (u1 & 0x1000000);
			public int Size { get => (int)(u1 >> 25); set => u1 = (u1 & 0x1ffffff) | ((uint)Math_.MinMax(value, 0, 127) << 25); }

			//u2
			public int BackColor { get => (int)(u2 & 0xffffff); set => u2 = (u2 & 0xff000000) | ((uint)value & 0xffffff) | 0x1000000; }
			public bool HasBackColor => 0 != (u2 & 0x1000000);
			public bool Bold { get => 0 != (u2 & 0x2000000); set { if(value) u2 |= 0x2000000; else u2 &= unchecked((uint)~0x2000000); } }
			public bool Italic { get => 0 != (u2 & 0x4000000); set { if(value) u2 |= 0x4000000; else u2 &= unchecked((uint)~0x4000000); } }
			public bool Underline { get => 0 != (u2 & 0x8000000); set { if(value) u2 |= 0x8000000; else u2 &= unchecked((uint)~0x8000000); } }
			public bool Eol { get => 0 != (u2 & 0x10000000); set { if(value) u2 |= 0x10000000; else u2 &= unchecked((uint)~0x10000000); } }
			public bool Hotspot { get => 0 != (u2 & 0x40000000); set { if(value) u2 |= 0x40000000; else u2 &= unchecked((uint)~0x40000000); } }
			public bool Mono { get => 0 != (u2 & 0x80000000); set { if(value) u2 |= 0x80000000; else u2 &= unchecked((uint)~0x80000000); } }

			public bool Equals(_TagStyle x) { return x.u1 == u1 && x.u2 == u2; }
			public void Merge(_TagStyle x)
			{
				var t1 = x.u1;
				if(HasColor) t1 &= 0xff000000;
				if(Size > 0) t1 &= 0x1ffffff;
				u1 |= t1;
				var t2 = x.u2;
				if(HasBackColor) t2 &= 0xff000000;
				u2 |= t2;
			}
			public bool IsEmpty => u1 == 0 & u2 == 0;
		}

		AuScintilla _c;
		SciText _t;
		List<_TagStyle> _styles = new List<_TagStyle>();
		List<int> _stack = new List<int>();

		internal SciTags(AuScintilla c)
		{
			_c = c;
			_t = c.ST;
		}

		void _SetUserStyles(int from)
		{
			int i, j;
			for(i = from; i < _styles.Count; i++) {
				_TagStyle st = _styles[i];
				j = i + STYLE_FIRST_EX;
				if(st.HasColor) _t.StyleForeColor(j, st.Color);
				if(st.HasBackColor) { _t.StyleBackColor(j, st.BackColor); if(st.Eol) _t.StyleEolFilled(j, true); }
				if(st.Bold) _t.StyleBold(j, true);
				if(st.Italic) _t.StyleItalic(j, true);
				if(st.Underline) _t.StyleUnderline(j, true);
				if(st.Mono) _t.StyleFont(j, "Courier New");
				if(st.Hotspot) _t.StyleHotspot(j, true);
				int size = st.Size;
				if(size > 0) {
					if(size < 6 && st.Hotspot) size = 6;
					_t.StyleFontSize(j, size);
				}
			}
		}

		/// <summary>
		/// Clears user-defined (through tags) styles.
		/// Max number of user styles is NUM_STYLES_EX (216). Need to clear old styles before new styles can be defined.
		/// This func is usually called after clearing control text.
		/// </summary>
		void _ClearUserStyles()
		{
			if(_styles.Count > 0) {
				_t.StyleClearRange(STYLE_FIRST_EX);
				_styles.Clear();
			}
			//QM2 also cleared the image cache, but now it is shared by all controls of this thread.
		}

		internal void LibOnTextChanged(bool inserted, ref SCNotification n)
		{
			//if deleted or replaced all text, clear user styles
			if(!inserted && n.position == 0 && _t.TextLengthBytes == 0) {
				_ClearUserStyles();
				//_linkDelegates.Clear(); //no
			}
		}

		/// <summary>
		/// Displays <see cref="OutputServer"/> messages that are currently in its queue.
		/// </summary>
		/// <param name="os">The OutputServer instance.</param>
		/// <param name="onMessage">A callback function that can be called when this function gets/removes a message from os.</param>
		/// <remarks>
		/// Removes messages from the queue.
		/// Appends text messages + "\r\n" to the control's text, or clears etc (depends on message).
		/// Messages with tags must have prefix "&lt;&gt;".
		/// Limits text length to about 4 MB (removes oldest text when exceeded).
		/// </remarks>
		/// <seealso cref="OutputServer.SetNotifications"/>
		public void OutputServerProcessMessages(OutputServer os, Action<OutputServer.Message> onMessage = null)
		{
			//info: Cannot call _c.Write for each message, it's too slow. Need to join all messages.
			//	If multiple messages, use StringBuilder.
			//	If some messages have tags, use string "<\x15\x0\x4" to separate messages. Never mind: don't escape etc.

			string s = null;
			StringBuilder b = null;
			bool hasTags = false, hasTagsPrev = false;
			while(os.GetMessage(out var m)) {
				onMessage?.Invoke(m);
				switch(m.Type) {
				case OutputServer.MessageType.Clear:
					_c.ST.ClearText();
					s = null;
					b?.Clear();
					break;
				case OutputServer.MessageType.Write:
					if(s == null) {
						s = m.Text;
						hasTags = hasTagsPrev = s.Starts("<>");
					} else {
						if(b == null) b = new StringBuilder();
						if(b.Length == 0) b.Append(s);

						s = m.Text;

						bool hasTagsThis = m.Text.Starts("<>");
						if(hasTagsThis && !hasTags) { hasTags = true; b.Insert(0, "<\x15\x0\x4"); }

						if(!hasTags) {
							b.Append("\r\n");
						} else if(hasTagsThis) {
							b.Append("\r\n<\x15\x0\x4");
							//info: add "\r\n" here, not later, because later it would make more difficult <Z> tag
						} else {
							b.Append(hasTagsPrev ? "\r\n<\x15\x0\x4" : "\r\n");
						}
						b.Append(s);
						hasTagsPrev = hasTagsThis;
					}
					break;
				}
			}
			if(s == null) return; //0 messages, or the last message is Clear
			if(b != null && b.Length > 0) s = b.ToString();

			//if(sb!=null) s += " >>>> " + sb.Capacity.ToString();

			//_c.ST.AppendText(s, true, true, true); return;

			//limit
			int len = _c.ST.TextLengthBytes;
			if(len > 4 * 1024 * 1024) {
				len = _c.ST.LineStartFromPos(len / 2);
				if(len > 0) _c.ST.ReplaceRange(0, len, "...\r\n");
			}

			if(hasTags) AddText(s, true, true);
			else _c.ST.AppendText(s, true, true, true);

			//test slow client
			//Thread.Sleep(500);
			//Output.LibWriteQM2(s.Length / 1048576d);
		}

		/// <summary>
		/// Sets or appends styled text.
		/// </summary>
		/// <param name="text">Text with tags (optionally).</param>
		/// <param name="appendLine">Append. Also appends "\r\n". Sets caret and scrolls to the end. If false, replaces control text.</param>
		/// <param name="skipLTGT">If text starts with "&lt;&gt;", skip it.</param>
		public void AddText(string text, bool appendLine, bool skipLTGT)
		{
			//Perf.First();
			if(Empty(text) || (skipLTGT && text == "<>")) {
				if(appendLine) _t.AppendText("", true, true, true); else _t.ClearText();
				return;
			}

			int len = Convert_.Utf8LengthFromString(text);
			byte* buffer = (byte*)NativeHeap.Alloc(len * 2 + 4), s = buffer;
			try {
				Convert_.Utf8FromString(text, s, len + 1);
				if(appendLine) { s[len++] = (byte)'\r'; s[len++] = (byte)'\n'; }
				if(skipLTGT && s[0] == '<' && s[1] == '>') { s += 2; len -= 2; }
				s[len] = s[len + 1] = 0;
				_AddText(s, len, appendLine);
			}
			finally {
				NativeHeap.Free(buffer);
			}
		}

		void _AddText(byte* s, int len, bool append)
		{
			//Perf.Next();
			byte* s0 = s, sEnd = s + len; //source text
			byte* t = s0; //destination text, ie without some tags
			byte* r0 = s0 + (len + 2), r = r0; //destination style bytes

			int prevStylesCount = _styles.Count;
			bool hasTags = false;
			byte currentStyle = STYLE_DEFAULT;
			_stack.Clear();
			List<POINT> codes = null;
			List<POINT> folds = null;

			while(s < sEnd) {
				//find '<'
				var ch = *s++;
				if(ch != '<') {
					_Write(ch, currentStyle);
					continue;
				}

				var tag = s;

				//end tag. Support <> and </tag>, but don't care what tag it is. The </tag> form can be used just to make the code more readable.
				if(s[0] == '/') {
					s++; while(Char_.IsAsciiAlpha(*s)) s++;
					if(s[0] != '>') goto ge;
				}
				if(s[0] == '>') {
					int n = _stack.Count - 1;
					if(n < 0) goto ge; //<> without tag
					s++;
					int i = _stack[n]; _stack.RemoveAt(n);
					if(i >= 0) { //the tag is a style tag or some other styled tag (eg link)
						if(currentStyle >= STYLE_FIRST_EX && _styles[currentStyle - STYLE_FIRST_EX].Eol) {
							if(*s == '\r') _Write(*s++, currentStyle);
							if(*s == '\n') _Write(*s++, currentStyle);
						}
						currentStyle = (byte)i;
					} else {
						i &= 0x7fffffff;
						if(s - tag == 6 && LibCharPtr.AsciiStartsWith(tag + 1, "fold")) {
							(folds ?? (folds = new List<POINT>())).Add((i, (int)(t - s0)));
							//if(s < sEnd && *s != '\r' && *s != '\n') _WriteString("\r\n", STYLE_DEFAULT); //no, can be an end of tag there
						}
					}
					continue;
				}

				//multi-message separator
				if(s[0] == 0x15 && s[1] == 0 && s[2] == 4 && (s - s0 == 1 || s[-2] == 10)) {
					s += 3;
					if(s[0] == '<' && s[1] == '>') s += 2; //message with tags
					else { //one or more messages without tags
						while(s < sEnd && !(s[0] == '<' && s[1] == 0x15 && s[2] == 0 && s[3] == 4 && s[-1] == 10)) _Write(*s++, STYLE_DEFAULT);
					}
					currentStyle = STYLE_DEFAULT;
					_stack.Clear();
					continue;
				}

				//read tag name
				ch = *s; if(ch == '_' || ch == '+') s++;
				while(Char_.IsAsciiAlpha(*s)) s++;
				int tagLen = (int)(s - tag);
				if(tagLen == 0) goto ge;

				//read attribute
				byte* attr = null; int attrLen = 0;
				if(*s == 32) {
					s++;
					var quot = *s;
					if(quot == '\'' || quot == '\"') s++; else quot = (byte)'>'; //never mind: escape sequences \\, \', \"
					int n = (int)(sEnd - s);
					int i = (quot == '>') ? LibCharPtr.AsciiFindChar(s, n, quot) : LibCharPtr.AsciiFindString(s, n, (quot == '\'') ? "'>" : "\">");
					if(i < 0) goto ge;
					attr = s; s += i + 1; attrLen = i;
					if(quot != '>') s++;
					else if(s[-2] == '<') goto ge; //<tag attr TEXT<>
				} else {
					if(*s != '>') goto ge;
					s++;
				}

				//tags
				_TagStyle style = default;
				bool hideTag = false, noEndTag = false, userTag = false;
				string linkTag = null;
				int stackInt = 0;
				int i2;
				ch = *tag;
				switch(tagLen << 16 | ch) {
				case 1 << 16 | 'b':
					style.Bold = true;
					break;
				case 1 << 16 | 'i':
					style.Italic = true;
					break;
				case 2 << 16 | 'b':
					if(tag[1] == 'i') style.Bold = style.Italic = true;
					else goto ge;
					break;
				case 1 << 16 | 'u':
					style.Underline = true;
					break;
				case 1 << 16 | 'c':
				case 1 << 16 | 'z':
				case 1 << 16 | 'Z':
					if(attr == null) goto ge;
					int color;
					if(Char_.IsAsciiDigit(*attr)) color = Api.strtoi(attr);
					else if(*attr == '#') color = Api.strtoi(attr + 1, radix: 16);
					else {
						var c = Color.FromName(new string((sbyte*)attr, 0, attrLen));
						if(c.A == 0) break; //invalid color name
						color = c.ToArgb() & 0xffffff;
					}
					if(ch == 'c') style.Color = color; else style.BackColor = color;
					if(ch == 'Z') style.Eol = true;
					break;
				case 4 << 16 | 's':
					if(attr == null) goto ge;
					if(LibCharPtr.AsciiStartsWith(tag + 1, "ize")) style.Size = Api.strtoi(attr);
					else goto ge;
					break;
				case 4 << 16 | 'm':
					if(LibCharPtr.AsciiStartsWith(tag + 1, "ono")) style.Mono = true;
					else goto ge;
					break;
				//case 6 << 16 | 'h': //rejected. Not useful; does not hide newlines.
				//	if(LibCharPtr.AsciiStartsWith(tag + 1, "idden")) style.Hidden = true;
				//	else goto ge;
				//	break;
				case 5 << 16 | 'i':
					if(attr == null) goto ge;
					if(LibCharPtr.AsciiStartsWith(tag + 1, "mage")) hideTag = noEndTag = true;
					else goto ge;
					break;
				case 1 << 16 | '_': //<_>text where tags are ignored</_>
					i2 = LibCharPtr.AsciiFindString(s, (int)(sEnd - s), "</_>"); if(i2 < 0) goto ge;
					//use </_> because <> is much more often used, eg as operator or our tag ending. Could also support <> if </_> not found, but it is not good.
					while(i2-- > 0) _Write(*s++, currentStyle);
					s += 4;
					//hasTags = true;
					continue;
				case 4 << 16 | 'c': //<code>code</code>
					if(!LibCharPtr.AsciiStartsWith(tag + 1, "ode")) goto ge;
					i2 = LibCharPtr.AsciiFindString(s, (int)(sEnd - s), "</code>"); if(i2 < 0) goto ge;
					if(codes == null) codes = new List<POINT>();
					int iStartCode = (int)(t - s0);
					codes.Add((iStartCode, iStartCode + i2));
					while(i2-- > 0) _Write(*s++, STYLE_DEFAULT);
					s += 7;
					hasTags = true;
					continue;
				case 4 << 16 | 'f': //<fold>text</fold>
					if(!LibCharPtr.AsciiStartsWith(tag + 1, "old")) goto ge;
					stackInt = (int)(t - s0);
					//add 'expand/collapse' link in this line. Max 6 characters, because overwriting "<fold>".
					_WriteString(" ", STYLE_HIDDEN); //it is how we later detect links
					_WriteString(">>", _GetStyleIndex(new _TagStyle { Hotspot = true, Underline = true, Color = 0x80FF }, currentStyle));
					_WriteString("\r\n", currentStyle); //let the folded text start from next line
					break;
				case 4 << 16 | 'l':
					linkTag = "link";
					break;
				case 6 << 16 | 'g':
					linkTag = "google";
					break;
				case 4 << 16 | 'h':
					linkTag = "help";
					break;
				case 7 << 16 | 'e':
					linkTag = "explore";
					break;
				case 4 << 16 | 'o':
					linkTag = "open";
					break;
				case 6 << 16 | 's':
					linkTag = "script";
					break;
				default:
					//user-defined tag or unknown.
					//user-defined tags must start with '+'.
					//don't hide unknown tags, unless start with '+'. Can be either misspelled (hiding would make harder to debug) or not intended for us (forgot <_>).
					if(ch != '+') goto ge;
					//if(!_userLinkTags.ContainsKey(linkTag = new string((sbyte*)tag, 0, tagLen))) goto ge; //no, it makes slower and creates garbage. Also would need to look in the static dictionary too. It's not so important to check now because we use '+' prefix.
					//info: initially was used '_', not '+'. But it creates more problems. Eg C# stack trace can contain "... at Script.<>c.<_Main>b__1_0() ...".
					linkTag = "";
					userTag = true;
					break;
				}

				if(linkTag != null) {
					if(!userTag && !LibCharPtr.AsciiStartsWith(tag, linkTag)) goto ge;
					//if(attr == null) goto ge; //no, use text as attribute
					style.Hotspot = true;
					style.Color = 0x80FF;
					style.Underline = true;
					hideTag = true;
				}

				if(hideTag) {
					bool isSingleQuote = attr != null && attr[-1] == '\'';
					if(isSingleQuote) attr[-1] = attr[attrLen] = (byte)'\"';
					for(var h = tag - 1; h < s; h++) _Write(*h, STYLE_HIDDEN);
					if(isSingleQuote) attr[-1] = attr[attrLen] = (byte)'\'';
				}

				hasTags = true;
				if(noEndTag) continue;

				if(!style.IsEmpty) {
					byte si = _GetStyleIndex(style, currentStyle);
					_stack.Add(currentStyle);
					currentStyle = si;
				} else {
					int k = unchecked((int)0x80000000); //no-style flag
					k |= stackInt;
					_stack.Add(k);
				}

				continue;
				ge: //invalid format of the tag
				_Write((byte)'<', currentStyle);
				s = tag;
			}

			Debug.Assert(t <= s0 + len);
			Debug.Assert(r <= r0 + len);
			Debug.Assert(t - s0 == r - r0);
			*t = 0; len = (int)(t - s0);

			if(_styles.Count > prevStylesCount) _SetUserStyles(prevStylesCount);

			//Perf.Next();
			_t.LibAddText(append, s0, len);
			if(!hasTags) return;

			int endStyled = 0, prevLen = append ? _t.TextLengthBytes - len : 0;

			if(folds != null) {
				for(int i = folds.Count - 1; i >= 0; i--) { //need reverse for nested folds
					var v = folds[i];
					int lineStart = _c.Call(SCI_LINEFROMPOSITION, v.x + prevLen), lineEnd = _c.Call(SCI_LINEFROMPOSITION, v.y + prevLen);
					int level = _c.Call(SCI_GETFOLDLEVEL, lineStart) & SC_FOLDLEVELNUMBERMASK;
					_c.Call(SCI_SETFOLDLEVEL, lineStart, level | SC_FOLDLEVELHEADERFLAG);
					for(int j = lineStart + 1; j <= lineEnd; j++) _c.Call(SCI_SETFOLDLEVEL, j, level + 1);
					_c.Call(SCI_FOLDLINE, lineStart);
				}

			}

			if(codes != null) {
				//info: tested various ways to add code coloured by a lexer, and only this way works. And it is good. Fast etc.
				//	At first need to add non-styled text (SCI_COLOURISE does not work if the text is already styled).
				//	Then set lexer and call SCI_COLOURISE for each code range.
				//	Then remove lexer, but don't clear style, keywords etc.
				//	Then call SCI_STARTSTYLING/SCI_SETSTYLINGEX for each non-code range.
				//	In any case, adding text is much slower than styling it. Appending is faster than adding, but only when don't need to scroll.
				//	Scrolling is very slow. //CONSIDER: try to scroll async (SCI_GOTOPOS); but no, will not need it when output will be buffered.
				//	//FUTURE: see maybe it's possible to get styling from lexers without attaching them to Scintilla control. Creating a hidden control for it is not good, eg because setting text is much slower.

				//Perf.Next();
				_SetLexer(LexLanguage.SCLEX_CPP);
				//Perf.Next();
				for(int i = 0; i < codes.Count; i++) {
					_c.Call(SCI_COLOURISE, codes[i].x + prevLen, codes[i].y + prevLen);
				}
				//Perf.Next();
				_SetLexer(LexLanguage.SCLEX_NULL);
				//Perf.Next();

				for(int i = 0; i < codes.Count; i++) {
					_StyleRange(codes[i].x);
					endStyled = codes[i].y;
				}
			}
			_StyleRange(len);
			//Perf.NW();


			void _StyleRange(int to)
			{
				if(endStyled < to) {
					_c.Call(SCI_STARTSTYLING, endStyled + prevLen);
					_c.Call(SCI_SETSTYLINGEX, to - endStyled, r0 + endStyled);
				}
			}

			void _Write(byte ch, byte style)
			{
				*t++ = ch; *r++ = style;
			}

			void _WriteString(string ss, byte style)
			{
				for(int i_ = 0; i_ < ss.Length; i_++) _Write((byte)ss[i_], style);
			}
		}

		byte _GetStyleIndex(_TagStyle style, byte currentStyle)
		{
			//merge nested style with ancestors
			int k = currentStyle;
			if(k >= STYLE_FIRST_EX) style.Merge(_styles[k - STYLE_FIRST_EX]);
			for(int j = _stack.Count - 1; j > 0; j--) {
				k = _stack[j];
				if(k < 0) continue; //a non-styled tag
				k &= 0xff; //remove other possible flags
				if(k >= STYLE_FIRST_EX) style.Merge(_styles[k - STYLE_FIRST_EX]);
			}

			//find or add style
			int i, n = _styles.Count;
			for(i = 0; i < n; i++) if(_styles[i].Equals(style)) break;
			if(i == NUM_STYLES_EX) {
				i = currentStyle;
				//CONSIDER: overwrite old styles added in previous calls. Now we just clear styles when control text cleared.
			} else {
				if(i == n) _styles.Add(style);
				i += STYLE_FIRST_EX;
			}
			return (byte)i;
		}

		void _SetLexer(LexLanguage lang)
		{
			if(lang == _currentLexer) return;
			_currentLexer = lang;
			if(lang != LexLanguage.SCLEX_NULL) _t.StyleClearRange(0, STYLE_HIDDEN); //STYLE_DEFAULT - 1
			_c.Call(SCI_SETLEXER, (int)lang);

			//for(int i=0; i< STYLE_DEFAULT; i++) { //creates problems
			//	_t.StyleBackColor(i, 0xffffff);
			//	_t.StyleEolFilled(i, true);
			//}

			switch(lang) {
			case LexLanguage.SCLEX_CPP:
				_t.SetLexerCpp(noClear: true);
				break;
			}
		}
		LexLanguage _currentLexer;

		/// <summary>
		/// Called on SCN_HOTSPOTRELEASECLICK.
		/// </summary>
		internal void LibOnLinkClick(int pos, bool ctrl)
		{
			if(Keyb.UI.IsAlt) return;

			int iTag, iText, k;
			//to find beginning of link text (after <tag>), search for STYLE_HIDDEN before
			for(iText = pos; iText > 0; iText--) if(_t.GetStyleAt(iText - 1) == STYLE_HIDDEN) break;
			if(iText == 0) return;
			//to find beginning of <tag>, search for some other style before
			for(iTag = iText - 1; iTag > 0; iTag--) if(_t.GetStyleAt(iTag - 1) != STYLE_HIDDEN) break;
			//to find end of link text, search for a non-hotspot style after
			for(pos++; /*SCI_GETSTYLEAT returns 0 if index invalid, it is documented*/; pos++) {
				k = _t.GetStyleAt(pos);
				if(k < STYLE_FIRST_EX || !_t.StyleHotspot(k)) break;
			}
			//get text <tag>LinkText
			var s = _t.RangeText(iTag, pos);
			//Print(iTag, iText, pos, s);

			//is it <fold>?
			if(s == " >>") {
				int line = _c.Call(SCI_LINEFROMPOSITION, iTag);
				_c.Call(SCI_TOGGLEFOLD, line);
				return;
			}
			//get tag, attribute and text
			if(!s.RegexMatch(@"(?s)^<(\+?\w+)(?: ""([^""]*)""| ([^>]*))?>(.+)", out var m)) return;
			string tag = m[1].Value, attr = m[2].Value ?? m[3].Value ?? m[4].Value;
			//Print($"'{tag}'  '{attr}'  '{m[4].Value}'");

			//process it async, because bad things happen if now we remove focus or change control text etc
			_c.BeginInvoke(new Action(() => _OnLinkClick(tag, attr)));
		}

		//note: attr can be ""
		void _OnLinkClick(string tag, string attr)
		{
			//Print($"'{tag}'  '{attr}'");

			if(_userLinkTags.TryGetValue(tag, out var d) || s_userLinkTags.TryGetValue(tag, out d)) {
				d.Invoke(attr);
				return;
			}

			var a = attr.SegSplit("|", 2);
			bool one = a.Length == 1;
			string s1 = a[0], s2 = one ? null : a[1];

			switch(tag) {
			case "link":
				Shell.TryRun(s1, s2);
				break;
			case "google":
				Shell.TryRun("http://www.google.com/search?q=" + Uri.EscapeDataString(s1) + s2);
				break;
			case "help":
				Util.Help_.AuWeb(attr);
				break;
			case "explore":
				Shell.SelectFileInExplorer(attr);
				break;
			default:
				//case "open": case "script": //the control recognizes but cannot implement these. The lib user can implement.
				//others are unregistered tags. Only if start with '+' (others are displayed as text).
				if(Opt.Debug.Verbose) AuDialog.ShowWarning("Debug", "Tag '" + tag + "' is not implemented.\nUse SciTags.AddCommonLinkTag or SciTags.AddLinkTag.");
				break;
			}
		}

		Dictionary<string, Action<string>> _userLinkTags = new Dictionary<string, Action<string>>();
		static ConcurrentDictionary<string, Action<string>> s_userLinkTags = new ConcurrentDictionary<string, Action<string>>();

		/// <summary>
		/// Adds (registers) a user-defined link tag for this control.
		/// </summary>
		/// <param name="name">
		/// Tag name, like "+myTag".
		/// Must start with '+'. Other characters must be 'a'-'z', 'A'-'Z'. Case-sensitive.
		/// Or can be one of predefined link tags, if you want to override or implement it (some are not implemented by the control).
		/// If already exists, replaces the delegate.
		/// </param>
		/// <param name="a">
		/// A delegate of a callback function (probably you'll use a lambda) that is called on link click.
		/// It's string parameter contains tag's attribute (if "&lt;name "attribute"&gt;TEXT&lt;&gt;) or link text (if "&lt;name&gt;TEXT&lt;&gt;).
		/// The function is called in control's thread. The mouse button is already released. It is safe to do anything with the control, eg replace text.
		/// </param>
		/// <seealso cref="AddCommonLinkTag"/>
		public void AddLinkTag(string name, Action<string> a)
		{
			_userLinkTags[name] = a;
		}

		/// <summary>
		/// Adds (registers) a user-defined link tag for all controls.
		/// </summary>
		/// <param name="name">
		/// Tag name, like "+myTag".
		/// Must start with '+'. Other characters must be 'a'-'z', 'A'-'Z'. Case-sensitive.
		/// Or can be one of predefined link tags, if you want to override or implement it (some are not implemented by the control).
		/// If already exists, replaces the delegate.
		/// </param>
		/// <param name="a">
		/// A delegate of a callback function (probably you'll use a lambda) that is called on link click.
		/// It's string parameter contains tag's attribute (if "&lt;name "attribute"&gt;TEXT&lt;&gt;) or link text (if "&lt;name&gt;TEXT&lt;&gt;).
		/// The function is called in control's thread. The mouse button is already released. It is safe to do anything with the control, eg replace text.
		/// </param>
		/// <seealso cref="AddLinkTag"/>
		public static void AddCommonLinkTag(string name, Action<string> a)
		{
			s_userLinkTags[name] = a;
		}

		//internal void LibOnMessage(ref Message m)
		//{

		//}

		internal void LibOnLButtonDownWhenNotFocused(ref Message m, ref bool setFocus)
		{
			if(setFocus && _c.InitReadOnlyAlways && !Keyb.UI.IsAlt) {
				int pos = _c.Call(SCI_CHARPOSITIONFROMPOINTCLOSE, Math_.LoShort(m.LParam), Math_.HiShort(m.LParam));
				//Print(pos);
				if(pos >= 0 && _t.StyleHotspot(_t.GetStyleAt(pos))) setFocus = false;
			}
		}

		//FUTURE: add control-tags, like <clear> (clear output), <scroll> (ensure line visible), <mark x> (add some marker etc).
		//FUTURE: let our links be accessible objects.
	}
}
