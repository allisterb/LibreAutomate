extern alias CAW;

//Code colors. Also calls functions of folding, images, errors.

using Au.Controls;
using static Au.Controls.Sci;

using Microsoft.CodeAnalysis;
using CAW::Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
//using Microsoft.CodeAnalysis.Classification;
using CAW::Microsoft.CodeAnalysis.Classification;

//SHOULDDO: now preprocessor symbol bold if followed by a bold style. Example code:
/*
#if DEBUG
#endif
Fun();
void Fun() {  }
*/

partial class CiStyling {
	public void DocHandleDestroyed(SciCode doc) {
		if (doc == _doc) {
			_doc = null; //GC. Not important, but helps when trying to detect memory leaks.
		}
	}
	
	/// <summary>
	/// Called after setting editor control text when a document opened (not just switched active document).
	/// </summary>
	public static void DocTextAdded() => CodeInfo._styling._DocTextAdded();
	void _DocTextAdded() {
		if (CodeInfo.IsReadyForStyling) {
			_timerOpened ??= new(_ => _DocChanged());
			_timerOpened.After(10); //not Dispatcher.InvokeAsync
		} else { //at program startup
			CodeInfo.ReadyForStyling += () => _DocChanged();
		}
	}
	
	/// <summary>
	/// Sets timer to updates styling and folding from 0 to the end of the visible area.
	/// </summary>
	public void Update() => _update = true;
	
	SciCode _doc; //to detect when the active document changed
	bool _update;
	bool _folded;
	Sci_VisibleRange _visibleLines;
	timer _timerModified, _timerOpened;
	int _modStart;
	int _modFromEnd; //like SCI_GETENDSTYLED, but from end
	int _diagCounter;
	CancellationTokenSource _cancelTS;
	
	void _DocChanged(SciCode doc = null) {
		//bool opened = doc == null;
		doc ??= Panels.Editor.ActiveDoc;
		if (doc == null) return;
		_doc = doc;
		_update = false;
		_folded = false;
		_visibleLines = default;
		_timerModified?.Stop();
		_modStart = _modFromEnd = int.MaxValue;
		_diagCounter = 0;
		_Work(doc, cancel: true);
		//if (opened) {
		//}
	}
	
	/// <summary>
	/// Called every 250 ms while editor is visible.
	/// </summary>
	public void Timer250msWhenVisibleAndWarm(SciCode doc) {
		//We can't use Scintilla styling notifications, mostly because of Roslyn slowness.
		//To detect when need styling and folding we use 'opened' and 'modified' events and 250 ms timer.
		//When modified, we do styling for the modified line(s). Redraws faster, but unreliable, eg does not update new/deleted identifiers.
		//The timer does styling and folding for all visible lines. Redraws with a bigger delay, but updates everything after modified, scrolled, resized, folded, etc.
		
		if (_cancelTS != null || (_timerModified?.IsRunning ?? false)) return;
		if (doc != _doc || _update) {
			_update = false;
			if (doc != _doc) _DocChanged(doc);
			else _Work(doc, cancel: true);
		} else {
			Sci_GetVisibleRange(doc.AaSciPtr, out var vr); //fast
			if (vr != _visibleLines) {
				_Work(doc);
			} else if (_diagCounter > 0 && --_diagCounter == 0) {
				CodeInfo._diag.Indicators(doc.aaaPos16(vr.posFrom), doc.aaaPos16(vr.posTo));
			}
		}
	}
	
	/// <summary>
	/// Called when editor text modified.
	/// </summary>
	public void SciModified(SciCode doc, in SCNotification n) {
		//Delay to avoid multiple styling/folding/cancelling on multistep actions (replace text range, find/replace all, autocorrection) and fast automated text input.
		_cancelTS?.Cancel(); _cancelTS = null;
		_modStart = Math.Min(_modStart, n.position);
		_modFromEnd = Math.Min(_modFromEnd, doc.aaaLen8 - n.FinalPosition);
		_folded = false;
		//using var p1 = perf.local();
#if true
		_timerModified ??= new timer(_ModifiedTimer);
		if (!_timerModified.IsRunning) { _timerModified.Tag = doc; _timerModified.After(25); }
#else
		_StylingAndFolding(doc, doc.aaaLineEndFromPos(false, doc.aaaLen8 - _modFromEnd, withRN: true));
#endif
		//workaround for:
		//	On Undo, if the undo text contains hidden text, Scintilla it seems tries to show that unstyled text before styleneeded notification.
		//	If the hidden text is long, it adds horz scrollbar and scrolls.
		//	Not if the undo text ends with newline.
		if (n.modificationType.Has(MOD.SC_LASTSTEPINUNDOREDO | MOD.SC_MOD_INSERTTEXT)) {
			doc.EHideImages_(n.position, doc.aaaLineEndFromPos(false, n.position + n.length));
			doc.aaaSetStyled();
		}
	}
	
	void _ModifiedTimer(timer t) {
		//var p1 = perf.local();
		var doc = t.Tag as SciCode;
		if (doc != Panels.Editor.ActiveDoc) return;
		if (_cancelTS != null) return;
		_Work(doc, doc.aaaLineStartFromPos(false, _modStart), doc.aaaLineEndFromPos(false, doc.aaaLen8 - _modFromEnd, withRN: true));
		//p1.NW('a'); //we return without waiting for the async task to complete
	}
	
	async void _Work(SciCode doc, int start8 = 0, int end8 = -1, bool cancel = false) {
#if PRINT
		using var p1 = perf.local();
#endif
		void _PN(char ch = default) {
#if PRINT
			p1.Next(ch);
#endif
		}
		
		if (cancel) { _cancelTS?.Cancel(); _cancelTS = null; }
		Debug.Assert(_cancelTS == null);
		var cancelTS = _cancelTS = new CancellationTokenSource();
		var cancelToken = cancelTS.Token;
		
		var cd = new CodeInfo.Context(0);
		Debug.Assert(doc == cd.sci);
		if (!cd.GetDocument()) return;
		var document = cd.document;
		var code = cd.code;
		_PN('d');
		try {
			Sci_GetVisibleRange(doc.AaSciPtr, out var vr);
			//print.it(vr);
			
			bool minimal = end8 >= 0;
			bool needFolding = !minimal && !_folded;
			List<SciFoldPoint> af = null;
			
			if (needFolding) {
				await Task.Run(() => {
					_PN('s');
					af = CiFolding.GetFoldPoints(cd.syntaxRoot, code, cancelToken);
				});
				if (_Cancelled()) return;
			}
			_PN('p');
			
			if (minimal) {
				start8 = Math.Max(start8, vr.posFrom);
				end8 = Math.Min(end8, vr.posTo);
			} else {
				if (needFolding) {
					CiFolding.Fold(doc, af);
					_folded = true;
				}
				Sci_GetVisibleRange(doc.AaSciPtr, out vr);
				_PN('F');
				start8 = vr.posFrom;
				end8 = vr.posTo;
			}
			//if (end8 == vr.posTo) _modFromEnd = doc.aaaLen8 - end8; //old code, now don't know its purpose. If need, then maybe do the same for _modStart.
			if (end8 <= start8) return;
			
#if PRINT
			//print.it($"<><c green>lines {doc.aaaLineFromPos(false, start8) + 1}-{doc.aaaLineFromPos(false, end8)}, range {start8}-{end8}, {vr}<>");
#endif
			
			var ar8 = _GetVisibleRanges();
			List<StartEnd> _GetVisibleRanges() {
				//print.it(vr);
				List<StartEnd> a = new();
				StartEnd r = new(start8, end8);
				for (int dline = doc.aaaLineFromPos(false, start8), dlinePrev = dline - 1, vline = doc.Call(SCI_VISIBLEFROMDOCLINE, dline); ; dline = doc.Call(SCI_DOCLINEFROMVISIBLE, ++vline)) {
					int i = doc.aaaLineStart(false, dline); if (i >= end8) break;
					//print.it(dline + 1);
					if (dline > dlinePrev + 1) {
						a.Add(r);
						r.start = i;
					}
					r.end = i + doc.Call(SCI_LINELENGTH, dline);
					dlinePrev = dline;
				}
				a.Add(r);
				//print.it("a", a);
				return a;
			}
			
			var ar = new (IEnumerable<ClassifiedSpan> a, StartEnd r)[ar8.Count];
			for (int i = 0; i < ar8.Count; i++) ar[i].r = new StartEnd(doc.aaaPos16(ar8[i].start), doc.aaaPos16(ar8[i].end));
			SemanticModel semo = null;
			
			await Task.Run(async () => {
				semo = await document.GetSemanticModelAsync(cancelToken).ConfigureAwait(false);
				_PN('m'); //BAD: slow when [re]opening a file in a large project
				for (int i = 0; i < ar8.Count; i++) {
					var r = ar[i].r;
					ar[i].a = Classifier.GetClassifiedSpansAsync(document, TextSpan.FromBounds(r.start, r.end), cancelToken).Result;
				}
				//info: GetClassifiedSpansAsync calls GetSemanticModelAsync and GetClassifiedSpans, like here.
				//GetSemanticModelAsync+GetClassifiedSpans are slow, ~ 90% of total time.
				//Tried to implement own "GetClassifiedSpans", but slow too, often slower, because GetSymbolInfo is slow.
			});
			if (_Cancelled()) return;
			_PN('c');
			
			var b = new byte[end8 - start8];
			
			foreach (var (a, r) in ar) {
				foreach (var v in a) {
					//print.it(v.ClassificationType, code[v.TextSpan.Start..v.TextSpan.End]);
					EStyle style = StyleFromClassifiedSpan(v, semo);
					
					if (style == EStyle.None) {
#if DEBUG
						switch (v.ClassificationType) {
						case ClassificationTypeNames.Identifier: break;
						case ClassificationTypeNames.LabelName: break;
						case ClassificationTypeNames.PreprocessorText: break;
						case ClassificationTypeNames.StaticSymbol: break;
						default: Debug_.PrintIf(!v.ClassificationType.Starts("regex"), $"<><c gray>{v.ClassificationType}, {v.TextSpan}<>"); break;
						}
#endif
						continue;
					}
					
					//int spanStart16 = v.TextSpan.Start, spanEnd16 = v.TextSpan.End;
					int spanStart16 = Math.Max(v.TextSpan.Start, r.start), spanEnd16 = Math.Min(v.TextSpan.End, r.end);
					int spanStart8 = doc.aaaPos8(spanStart16), spanEnd8 = doc.aaaPos8(spanEnd16);
					_SetStyleRange((byte)style);
					
					void _SetStyleRange(byte style) {
						for (int i = spanStart8; i < spanEnd8; i++) b[i - start8] = style;
					}
				}
			}
			doc.EHideImages_(start8, end8, b);
			_PN();
			doc.Call(SCI_STARTSTYLING, start8);
			unsafe { fixed (byte* bp = b) doc.Call(SCI_SETSTYLINGEX, b.Length, bp); }
			doc.aaaSetStyled(minimal ? int.MaxValue : end8);
			
			_modStart = _modFromEnd = int.MaxValue;
			_visibleLines = minimal ? default : vr;
			_PN('S');
			if (!minimal) {
				doc.EImagesGet_(cd, ar.SelectMany(o => o.a), vr);
				_diagCounter = 4; //update diagnostics after 1 s
			} else {
				CodeInfo._diag.EraseIndicatorsInLine(doc, doc.aaaCurrentPos8);
			}
		}
		catch (OperationCanceledException) { }
		catch (AggregateException e1) when (e1.InnerException is TaskCanceledException) { }
		catch (Exception e1) { Debug_.Print(e1); } //InvalidOperationException when this code: wpfBuilder ... .Also(b=>b.Panel.for)
		finally {
			cancelTS.Dispose();
			if (cancelTS == _cancelTS) _cancelTS = null;
		}
		
		bool _Cancelled() {
			if (cancelToken.IsCancellationRequested) {
				_PN();
#if PRINT
				print.it($"<><c orange>canceled.  {p1.ToString()}<>");
#endif
				return true;
			}
			if (doc != Panels.Editor.ActiveDoc) {
#if PRINT
				print.it("<><c red>switched doc<>");
#endif
				return true;
			}
			return false;
		}
#if TRACE
		//if(!s_debugPerf) { s_debugPerf = true; perf.nw('s'); }
#endif
	}
#if TRACE
	//static bool s_debugPerf;
#endif
	
	public static EStyle StyleFromClassifiedSpan(ClassifiedSpan cs, SemanticModel semo) {
		return cs.ClassificationType switch {
			ClassificationTypeNames.ClassName => EStyle.Type,
			ClassificationTypeNames.Comment => EStyle.Comment,
			ClassificationTypeNames.ConstantName => EStyle.Constant,
			ClassificationTypeNames.ControlKeyword => EStyle.Keyword,
			ClassificationTypeNames.DelegateName => EStyle.Type,
			ClassificationTypeNames.EnumMemberName => EStyle.Constant,
			ClassificationTypeNames.EnumName => EStyle.Type,
			ClassificationTypeNames.EventName => EStyle.Function,
			ClassificationTypeNames.ExcludedCode => EStyle.Excluded,
			ClassificationTypeNames.ExtensionMethodName => EStyle.Function,
			ClassificationTypeNames.FieldName => EStyle.Variable,
			ClassificationTypeNames.Identifier => _TryResolveMethod(),
			ClassificationTypeNames.InterfaceName => EStyle.Type,
			ClassificationTypeNames.Keyword => EStyle.Keyword,
			ClassificationTypeNames.LabelName => EStyle.Label,
			ClassificationTypeNames.LocalName => EStyle.Variable,
			ClassificationTypeNames.MethodName => EStyle.Function,
			ClassificationTypeNames.NamespaceName => EStyle.Namespace,
			ClassificationTypeNames.NumericLiteral => EStyle.Number,
			ClassificationTypeNames.Operator => EStyle.Operator,
			ClassificationTypeNames.OperatorOverloaded => EStyle.Function,
			ClassificationTypeNames.ParameterName => EStyle.Variable,
			ClassificationTypeNames.PreprocessorKeyword => EStyle.Preprocessor,
			//ClassificationTypeNames.PreprocessorText => EStyle.None,
			ClassificationTypeNames.PropertyName => EStyle.Function,
			ClassificationTypeNames.Punctuation => EStyle.Punctuation,
			ClassificationTypeNames.RecordClassName or ClassificationTypeNames.RecordStructName => EStyle.Type,
			ClassificationTypeNames.StringEscapeCharacter => EStyle.StringEscape,
			ClassificationTypeNames.StringLiteral => EStyle.String,
			ClassificationTypeNames.StructName => EStyle.Type,
			//ClassificationTypeNames.Text => EStyle.None,
			ClassificationTypeNames.VerbatimStringLiteral => EStyle.String,
			ClassificationTypeNames.TypeParameterName => EStyle.Type,
			//ClassificationTypeNames.WhiteSpace => EStyle.None,
			
			ClassificationTypeNames.XmlDocCommentText => EStyle.XmlDocText,
			ClassificationTypeNames.XmlDocCommentAttributeName => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentAttributeQuotes => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentAttributeValue => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentCDataSection => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentComment => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentDelimiter => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentEntityReference => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentName => EStyle.XmlDocTag,
			ClassificationTypeNames.XmlDocCommentProcessingInstruction => EStyle.XmlDocTag,
			
			//FUTURE: Regex. But how to apply it to regexp?
			//ClassificationTypeNames. => EStyle.,
			_ => EStyle.None
		};
		
		EStyle _TryResolveMethod() { //ClassificationTypeNames.Identifier. Possibly method name when there are errors in arguments.
			var node = semo.Root.FindNode(cs.TextSpan);
			if (node?.Parent is InvocationExpressionSyntax && !semo.GetMemberGroup(node).IsDefaultOrEmpty) return EStyle.Function; //not too slow
			return EStyle.None;
		}
	}
	
	/// <summary>
	/// Scintilla style indices of token types.
	/// </summary>
	public enum EStyle : byte {
		None,
		Comment,
		String,
		StringEscape,
		Number,
		Punctuation,
		Operator,
		Keyword,
		Namespace,
		Type,
		Function,
		Variable,
		Constant,
		Label,
		Preprocessor,
		Excluded,
		XmlDocText,
		XmlDocTag, //tags, CDATA, ///, etc
		
		countUserDefined,
		
		Image = countUserDefined,
		
		//STYLE_HIDDEN=31,
		//STYLE_DEFAULT=32,
		
		LineNumber = 33, //STYLE_LINENUMBER
	}
	
	public struct TStyle {
		public int color;
		public bool bold;
		
		public TStyle(int color, bool bold) {
			this.color = color;
			this.bold = bold;
		}
		
		public static implicit operator TStyle(int color) => new(color, false);
	}
	
	public record TStyles //note: must be record, because uses synthesized ==
	{
		public string FontName = "Consolas";
		public int FontSize = 9;
		public int BackgroundColor = 0xffffff;
		//public int IndicFoundColor = 0xf6b94d; //orange, like in VS
		public int IndicFoundColor = 0xffff00; //yellow, like in Chrome
		public int IndicRefsColor = 0x80C000;
		public int IndicBracesColor = 0x80C000;
		public int IndicFoundAlpha = 255;
		public int IndicRefsAlpha = 40;
		public int IndicBracesAlpha = 255;
		//public bool IndicFoundGradient;
		//public bool IndicRefsGradient;
		//public bool IndicBracesGradient;
		
		public int SelColor = unchecked((int)0xA0A0A0A0);
		public int SelNofocusColor = 0x60A0A0A0;
		
		public TStyle None; //black
		public TStyle Comment = 0x60A000; //light green, towards yellow
		public TStyle String = 0xA07040; //brown, more green
		public TStyle StringEscape = 0xB776FB; //pink-purple like in VS
		public TStyle Number = 0x804000; //brown, more red
		public TStyle Punctuation; //black
		public TStyle Operator = 0x0000ff; //blue like keyword
		public TStyle Keyword = 0x0000ff; //blue like in VS
		public TStyle Namespace = 0x808000; //dark yellow
		public TStyle Type = 0x0080c0; //like in VS but more blue
		public TStyle Function = new(0, true);
		public TStyle Variable = 0x204020; //dark green gray
		public TStyle Constant = 0x204020; //like variable
		public TStyle Label = 0xff00ff; //magenta
		public TStyle Preprocessor = 0xff8000; //orange
		public TStyle Excluded = 0x808080; //gray
		public TStyle XmlDocText = 0x408000; //green
		public TStyle XmlDocTag = 0x808080; //gray
		
		public TStyle LineNumber = 0x808080;
		
		public static TStyles Settings {
			get => s_styles ??= new TStyles(customized: true);
			set {
				s_styles = value;
				if (value != null) value._Save();
				else filesystem.delete(s_settingsFile);
			}
		}
		static TStyles s_styles;
		internal static readonly string s_settingsFile = AppSettings.DirBS + "Font.csv";
		
		public TStyles(bool customized) {
			if (!customized || !filesystem.exists(s_settingsFile).File) return;
			csvTable csv;
			try { csv = csvTable.load(s_settingsFile); }
			catch (Exception e1) { print.it(e1.ToStringWithoutStack()); return; }
			if (csv.ColumnCount < 2) return;
			
			foreach (var a in csv.Rows) {
				switch (a[0]) {
				case "Font":
					if (!a[1].NE()) FontName = a[1];
					if (a.Length > 2) { int fs = a[2].ToInt(); if (fs >= 5 && fs <= 100) FontSize = fs; }
					break;
				case "Background": _Int(ref BackgroundColor); break;
				case nameof(None): _Style(ref None); break;
				case nameof(Comment): _Style(ref Comment); break;
				case nameof(String): _Style(ref String); break;
				case nameof(StringEscape): _Style(ref StringEscape); break;
				case nameof(Number): _Style(ref Number); break;
				case nameof(Punctuation): _Style(ref Punctuation); break;
				case nameof(Operator): _Style(ref Operator); break;
				case nameof(Keyword): _Style(ref Keyword); break;
				case nameof(Namespace): _Style(ref Namespace); break;
				case nameof(Type): _Style(ref Type); break;
				case nameof(Function): _Style(ref Function); break;
				case nameof(Variable): _Style(ref Variable); break;
				case nameof(Constant): _Style(ref Constant); break;
				case nameof(Label): _Style(ref Label); break;
				case nameof(Preprocessor): _Style(ref Preprocessor); break;
				case nameof(Excluded): _Style(ref Excluded); break;
				case nameof(XmlDocText): _Style(ref XmlDocText); break;
				case nameof(XmlDocTag): _Style(ref XmlDocTag); break;
				case nameof(LineNumber): _Style(ref LineNumber); break;
				case nameof(IndicFoundColor): _Int(ref IndicFoundColor); _Alpha(ref IndicFoundAlpha); break;
				case nameof(IndicRefsColor): _Int(ref IndicRefsColor); _Alpha(ref IndicRefsAlpha); break;
				case nameof(IndicBracesColor): _Int(ref IndicBracesColor); _Alpha(ref IndicBracesAlpha); break;
				case nameof(SelColor): _Int(ref SelColor); break;
				case nameof(SelNofocusColor): _Int(ref SelNofocusColor); break;
				}
				
				void _Style(ref TStyle r) {
					if (!a[1].NE() && a[1].ToInt(out int i)) r.color = i;
					if (a.Length > 2 && !a[2].NE() && a[2].ToInt(out int i2)) r.bold = 0 != (1 & i2); else r.bold = false;
				}
				
				void _Int(ref int value) {
					if (!a[1].NE() && a[1].ToInt(out int i)) value = i;
				}
				
				void _Alpha(ref int value) {
					if (a.Length > 2 && !a[2].NE() && a[2].ToInt(out int i)) value = Math.Clamp(i, 0, 255);
				}
			}
		}
		
		void _Save() {
			var b = new StringBuilder(); //don't need csvTable for such simple values
			b.AppendLine($"Font, {FontName}, {FontSize}");
			_Int("Background", BackgroundColor);
			_Style(nameof(None), None);
			_Style(nameof(Comment), Comment);
			_Style(nameof(String), String);
			_Style(nameof(StringEscape), StringEscape);
			_Style(nameof(Number), Number);
			_Style(nameof(Punctuation), Punctuation);
			_Style(nameof(Operator), Operator);
			_Style(nameof(Keyword), Keyword);
			_Style(nameof(Namespace), Namespace);
			_Style(nameof(Type), Type);
			_Style(nameof(Function), Function);
			_Style(nameof(Variable), Variable);
			_Style(nameof(Constant), Constant);
			_Style(nameof(Label), Label);
			_Style(nameof(Preprocessor), Preprocessor);
			_Style(nameof(Excluded), Excluded);
			_Style(nameof(XmlDocText), XmlDocText);
			_Style(nameof(XmlDocTag), XmlDocTag);
			_Style(nameof(LineNumber), LineNumber);
			_Indic(nameof(IndicFoundColor), IndicFoundColor, IndicFoundAlpha);
			_Indic(nameof(IndicRefsColor), IndicRefsColor, IndicRefsAlpha);
			_Indic(nameof(IndicBracesColor), IndicBracesColor, IndicBracesAlpha);
			_Int(nameof(SelColor), SelColor);
			_Int(nameof(SelNofocusColor), SelNofocusColor);
			
			void _Style(string name, TStyle r) {
				b.Append(name).Append(", 0x").Append(r.color.ToString("X6"));
				if (r.bold) b.Append(", 1");
				b.AppendLine();
			}
			
			void _Int(string name, int i) => b.AppendLine($"{name}, 0x{i:X8}");
			
			void _Indic(string name, int color, int alpha) => b.AppendLine($"{name}, 0x{color:X6}, {alpha}");
			
			filesystem.saveText(s_settingsFile, b.ToString());
		}
		
		/// <summary>
		/// Gets colors, bold, but not font properties.
		/// </summary>
		public TStyles(KScintilla sci) {
			BackgroundColor = ColorInt.SwapRB(sci.Call(SCI_STYLEGETBACK));
			
			TStyle _Style(EStyle k) {
				int color = ColorInt.SwapRB(sci.Call(SCI_STYLEGETFORE, (int)k));
				bool bold = 0 != sci.Call(SCI_STYLEGETBOLD, (int)k);
				return new TStyle(color, bold);
			}
			
			None = _Style(EStyle.None);
			Comment = _Style(EStyle.Comment);
			String = _Style(EStyle.String);
			StringEscape = _Style(EStyle.StringEscape);
			Number = _Style(EStyle.Number);
			Punctuation = _Style(EStyle.Punctuation);
			Operator = _Style(EStyle.Operator);
			Keyword = _Style(EStyle.Keyword);
			Namespace = _Style(EStyle.Namespace);
			Type = _Style(EStyle.Type);
			Function = _Style(EStyle.Function);
			Variable = _Style(EStyle.Variable);
			Constant = _Style(EStyle.Constant);
			Label = _Style(EStyle.Label);
			Preprocessor = _Style(EStyle.Preprocessor);
			Excluded = _Style(EStyle.Excluded);
			XmlDocText = _Style(EStyle.XmlDocText);
			XmlDocTag = _Style(EStyle.XmlDocTag);
			
			LineNumber = _Style(EStyle.LineNumber);
			
			IndicFoundColor = ColorInt.SwapRB(sci.Call(SCI_INDICGETFORE, SciCode.c_indicFound));
			IndicRefsColor = ColorInt.SwapRB(sci.Call(SCI_INDICGETFORE, SciCode.c_indicRefs));
			IndicBracesColor = ColorInt.SwapRB(sci.Call(SCI_INDICGETFORE, SciCode.c_indicBraces));
			IndicFoundAlpha = sci.Call(SCI_INDICGETALPHA, SciCode.c_indicFound);
			IndicRefsAlpha = sci.Call(SCI_INDICGETALPHA, SciCode.c_indicRefs);
			IndicBracesAlpha = sci.Call(SCI_INDICGETALPHA, SciCode.c_indicBraces);
			
			SelColor = sci.aaaGetElementColor(SC_ELEMENT_SELECTION_BACK).argb;
			SelNofocusColor = sci.aaaGetElementColor(SC_ELEMENT_SELECTION_INACTIVE_BACK).argb;
		}
		
		/// <param name="multiFont">Set font only for code styles.</param>
		public void ToScintilla(KScintilla sci, bool multiFont = false) {
			if (!multiFont) sci.aaaStyleFont(STYLE_DEFAULT, FontName, FontSize);
			sci.aaaStyleBackColor(STYLE_DEFAULT, BackgroundColor);
			//if(None.color != 0) sci.aaaStyleForeColor(STYLE_DEFAULT, None.color); //also would need bold and in ctor above
			sci.aaaStyleClearAll(); //belowDefault could be true, but currently don't need it and would need to test everywhere
			
			void _Set(EStyle k, TStyle sty) {
				sci.aaaStyleForeColor((int)k, sty.color);
				if (sty.bold) sci.aaaStyleBold((int)k, true);
				if (multiFont) sci.aaaStyleFont((int)k, FontName, FontSize);
			}
			
			_Set(EStyle.None, None);
			_Set(EStyle.Comment, Comment);
			_Set(EStyle.String, String);
			_Set(EStyle.StringEscape, StringEscape);
			_Set(EStyle.Number, Number);
			_Set(EStyle.Punctuation, Punctuation);
			_Set(EStyle.Operator, Operator);
			_Set(EStyle.Keyword, Keyword);
			_Set(EStyle.Namespace, Namespace);
			_Set(EStyle.Type, Type);
			_Set(EStyle.Function, Function);
			_Set(EStyle.Variable, Variable);
			_Set(EStyle.Constant, Constant);
			_Set(EStyle.Label, Label);
			_Set(EStyle.Preprocessor, Preprocessor);
			_Set(EStyle.Excluded, Excluded);
			_Set(EStyle.XmlDocText, XmlDocText);
			_Set(EStyle.XmlDocTag, XmlDocTag);
			
			_Set((EStyle)STYLE_LINENUMBER, LineNumber);
			
			sci.aaaStyleForeColor(STYLE_INDENTGUIDE, 0xcccccc);
			
			_Indic(SciCode.c_indicFound, IndicFoundColor, IndicFoundAlpha, INDIC_FULLBOX);
			_Indic(SciCode.c_indicRefs, IndicRefsColor, IndicRefsAlpha, INDIC_FULLBOX);
			_Indic(SciCode.c_indicBraces, IndicBracesColor, IndicBracesAlpha, INDIC_GRADIENT);
			
			void _Indic(int indic, int color, int alpha, int style) {
				sci.aaaIndicatorDefine(indic, style, color, alpha, 255, underText: true);
			}
			//void _Indic(int indic, int color, int alpha, bool gradient) {
			//	sci.aaaIndicatorDefine(indic, gradient ? INDIC_GRADIENT : INDIC_FULLBOX, color, alpha, 255, underText: true);
			//}
			
			sci.aaaSetElementColor(SC_ELEMENT_SELECTION_BACK, SelColor);
			sci.aaaSetElementColor(SC_ELEMENT_SELECTION_INACTIVE_BACK, SelNofocusColor);
		}
		
		//rejected
		//public void InvertAllColors() {
		//	None.color ^= 0xffffff;
		//	Comment.color ^= 0xffffff;
		//	String.color ^= 0xffffff;
		//	StringEscape.color ^= 0xffffff;
		//	Number.color ^= 0xffffff;
		//	Punctuation.color ^= 0xffffff;
		//	Operator.color ^= 0xffffff;
		//	Keyword.color ^= 0xffffff;
		//	Namespace.color ^= 0xffffff;
		//	Type.color ^= 0xffffff;
		//	Function.color ^= 0xffffff;
		//	Variable.color ^= 0xffffff;
		//	Constant.color ^= 0xffffff;
		//	Label.color ^= 0xffffff;
		//	Preprocessor.color ^= 0xffffff;
		//	Excluded.color ^= 0xffffff;
		//	XmlDocText.color ^= 0xffffff;
		//	XmlDocTag.color ^= 0xffffff;
		//	BackgroundColor ^= 0xffffff;
		//	... ^= 0xffffff;
		//}
		
		//not used
		//public TStyle GetStyle(EStyle k) {
		//	return k switch {
		//		EStyle.None => None,
		//		EStyle.Comment => Comment,
		//		EStyle.String => String,
		//		EStyle.StringEscape => StringEscape,
		//		EStyle.Number => Number,
		//		EStyle.Punctuation => Punctuation,
		//		EStyle.Operator => Operator,
		//		EStyle.Keyword => Keyword,
		//		EStyle.Namespace => Namespace,
		//		EStyle.Type => Type,
		//		EStyle.Function => Function,
		//		EStyle.Variable => Variable,
		//		EStyle.Constant => Constant,
		//		EStyle.Label => Label,
		//		EStyle.Preprocessor => Preprocessor,
		//		EStyle.Excluded => Excluded,
		//		EStyle.XmlDocText => XmlDocText,
		//		EStyle.XmlDocTag => XmlDocTag,
		//		EStyle.LineNumber => LineNumber,
		//		_ => default,
		//	};
		//}
		
		//public void SetStyle(EStyle k, TStyle style) {
		//	switch(k) {
		//	case EStyle.None: None = style; break;
		//	case EStyle.Comment: Comment = style; break;
		//	case EStyle.String: String = style; break;
		//	case EStyle.StringEscape: StringEscape = style; break;
		//	case EStyle.Number: Number = style; break;
		//	case EStyle.Punctuation: Punctuation = style; break;
		//	case EStyle.Operator: Operator = style; break;
		//	case EStyle.Keyword: Keyword = style; break;
		//	case EStyle.Namespace: Namespace = style; break;
		//	case EStyle.Type: Type = style; break;
		//	case EStyle.Function: Function = style; break;
		//	case EStyle.Variable: Variable = style; break;
		//	case EStyle.Constant: Constant = style; break;
		//	case EStyle.Label: Label = style; break;
		//	case EStyle.Preprocessor: Preprocessor = style; break;
		//	case EStyle.Excluded: Excluded = style; break;
		//	case EStyle.XmlDocText: XmlDocText = style; break;
		//	case EStyle.XmlDocTag: XmlDocTag = style; break;
		//	case EStyle.LineNumber: LineNumber = style; break;
		//	}
		//}
	}
	
	/// <summary>
	/// Returns true if character at pos8 is in a hidden text.
	/// </summary>
	public static bool IsProtected(KScintilla sci, int pos8) => sci.Call(Sci.SCI_GETSTYLEAT, pos8) == STYLE_HIDDEN;
	
	/// <summary>
	/// Returns true if range from8..to8 intersects a hidden text, except when it is greater or equal than the hidden text range.
	/// It means the range should not be selected or modified.
	/// </summary>
	public static bool IsProtected(KScintilla sci, int from8, int to8) {
		bool p1 = IsProtected(sci, from8);
		if (to8 <= from8) return p1 && IsProtected(sci, from8 - 1);
		if (p1) return IsProtected(sci, from8 - 1) || (IsProtected(sci, to8 - 1) && IsProtected(sci, to8));
		if (IsProtected(sci, to8 - 1)) return IsProtected(sci, to8);
		return false;
	}
	
	public static int SkipProtected(KScintilla sci, int pos8) {
		while (IsProtected(sci, pos8)) pos8++;
		return pos8;
	}
}

