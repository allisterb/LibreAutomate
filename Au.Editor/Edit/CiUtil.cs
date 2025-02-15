extern alias CAW;

using System.Collections.Immutable;
using Au.Controls;
using Au.Compiler;
using EStyle = CiStyling.EStyle;

using Microsoft.CodeAnalysis;
using CAW::Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using CAW::Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Classification;
using CAW::Microsoft.CodeAnalysis.Classification;
using CAW::Microsoft.CodeAnalysis.Tags;
using CAW::Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Completion;

static class CiUtil {
	/// <summary>
	/// Gets statement or member declaration or using directive etc from position.
	/// Returns null if at the end of file.
	/// </summary>
	public static SyntaxNode GetStatementEtcFromPos(CodeInfo.Context cd, int pos) {
		var cu = cd.syntaxRoot;
		var node = cu.FindToken(pos).Parent;
		SyntaxNode n = node.FirstAncestorOrSelf<StatementSyntax>();
		n ??= node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
		n ??= node.FirstAncestorOrSelf<SyntaxNode>(o => o.Parent is CompilationUnitSyntax); //using directive etc
		return n;
	}
	
	public static (ISymbol symbol, CodeInfo.Context cd) GetSymbolFromPos(bool andZeroLength = false) {
		if (!CodeInfo.GetContextAndDocument(out var cd)) return default;
		return (GetSymbolFromPos(cd, andZeroLength), cd);
	}
	
	public static ISymbol GetSymbolFromPos(CodeInfo.Context cd, bool andZeroLength = false) {
		if (andZeroLength && _TryGetAltSymbolFromPos(cd) is ISymbol s1) return s1;
		var sym = SymbolFinder.FindSymbolAtPositionAsync(cd.document, cd.pos).Result;
		if (sym is IMethodSymbol ims) sym = ims.PartialImplementationPart ?? sym;
		return sym;
	}
	
	static ISymbol _TryGetAltSymbolFromPos(CodeInfo.Context cd) {
		if (cd.code.Eq(cd.pos, '[')) { //indexer?
			var t = cd.syntaxRoot.FindToken(cd.pos, true);
			if (t.IsKind(SyntaxKind.OpenBracketToken) && t.Parent is BracketedArgumentListSyntax b && b.Parent is ElementAccessExpressionSyntax es) {
				return cd.semanticModel.GetSymbolInfo(es).Symbol;
			}
		}
		//rejected: in the same way get cast operator if pos is before '('. Not very useful.
		return null;
	}
	
	public static (ISymbol symbol, string keyword, HelpKind helpKind, SyntaxToken token) GetSymbolEtcFromPos(CodeInfo.Context cd, bool forHelp = false) {
		if (_TryGetAltSymbolFromPos(cd) is ISymbol s1) return (s1, null, default, default);
		
		int pos = cd.pos; if (pos > 0 && SyntaxFacts.IsIdentifierPartCharacter(cd.code[pos - 1])) pos--;
		if (!cd.syntaxRoot.FindTouchingToken(out var token, pos, findInsideTrivia: true)) return default;
		
		string word = cd.code[token.Span.ToRange()];
		
		var k = token.Kind();
		if (k == SyntaxKind.IdentifierToken) {
			switch (word) {
			case "var" when forHelp: //else get the inferred type
			case "dynamic":
			case "nameof":
			case "unmanaged": //tested cases
				return (null, word, HelpKind.ContextualKeyword, token);
			}
		} else if (token.Parent is ImplicitObjectCreationExpressionSyntax && (!forHelp || cd.pos == token.Span.End)) {
			//for 'new(' get the ctor or type
		} else {
			//print.it(
			//	//token.IsKeyword(), //IsReservedKeyword||IsContextualKeyword, but not IsPreprocessorKeyword
			//	SyntaxFacts.IsReservedKeyword(k), //also true for eg #if
			//	SyntaxFacts.IsContextualKeyword(k)
			//	//SyntaxFacts.IsQueryContextualKeyword(k) //included in IsContextualKeyword
			//	//SyntaxFacts.IsAccessorDeclarationKeyword(k),
			//	//SyntaxFacts.IsPreprocessorKeyword(k), //true if #something or can be used in #something context. Also true for eg if without #.
			//	//SyntaxFacts.IsPreprocessorContextualKeyword(k) //badly named. True only if #something.
			//	);
			
			if (SyntaxFacts.IsReservedKeyword(k)) {
				bool pp = (word == "if" || word == "else") && token.GetPreviousToken().IsKind(SyntaxKind.HashToken);
				if (pp) word = "#" + word;
				return (null, word, pp ? HelpKind.PreprocKeyword : HelpKind.ReservedKeyword, token);
			}
			if (SyntaxFacts.IsContextualKeyword(k)) {
				return (null, word, SyntaxFacts.IsAttributeTargetSpecifier(k) ? HelpKind.AttributeTarget : HelpKind.ContextualKeyword, token);
			}
			if (SyntaxFacts.IsPreprocessorKeyword(k)) {
				//if(SyntaxFacts.IsPreprocessorContextualKeyword(k)) word = "#" + word; //better don't use this internal func
				if (token.GetPreviousToken().IsKind(SyntaxKind.HashToken)) word = "#" + word;
				return (null, word, HelpKind.PreprocKeyword, token);
			}
			switch (token.IsInString(cd.pos, cd.code, out _)) {
			case true: return (null, null, HelpKind.String, token);
			case null: return default;
			}
		}
		//note: don't pass contextual keywords to FindSymbolAtPositionAsync or GetSymbolInfo.
		//	It may get info for something other, eg 'new' -> ctor or type, or 'int' -> type 'Int32'.
		
		return (GetSymbolFromPos(cd), null, default, token);
	}
	
	public enum HelpKind {
		None, ReservedKeyword, ContextualKeyword, AttributeTarget, PreprocKeyword, String
	}
	
	public static void OpenSymbolEtcFromPosHelp() {
		string url = null;
		if (!CodeInfo.GetContextAndDocument(out var cd)) return;
		var (sym, keyword, helpKind, _) = GetSymbolEtcFromPos(cd, forHelp: true);
		if (sym != null) {
			url = GetSymbolHelpUrl(sym);
		} else if (keyword != null) {
			var s = helpKind switch {
				HelpKind.PreprocKeyword => "preprocessor directive",
				HelpKind.AttributeTarget => "attributes, ",
				_ => "keyword"
			};
			s = $"C# {s} \"{keyword}\"";
			//print.it(s); return;
			url = _GoogleURL(s);
		} else if (helpKind == HelpKind.String) {
			int i = popupMenu.showSimple("1 C# strings|2 String formatting|3 Wildcard expression|11 Regex tool (Ctrl+Space)|12 Keys tool (Ctrl+Space)", PMFlags.ByCaret);
			switch (i) {
			case 1: url = "C# strings"; break;
			case 2: url = "C# string formatting"; break;
			case 3: HelpUtil.AuHelp("articles/Wildcard expression"); break;
			case 11: CiTools.CmdShowRegexWindow(); break;
			case 12: CiTools.CmdShowKeysWindow(); break;
			}
			if (url != null) url = _GoogleURL(url);
		}
		if (url != null) run.itSafe(url);
	}
	
	static string _GoogleURL(string query) => "https://www.google.com/search?q=" + System.Net.WebUtility.UrlEncode(query);
	
	public static string GetSymbolHelpUrl(ISymbol sym) {
		//print.it(sym);
		//print.it(sym.IsInSource(), sym.IsFromSource());
		if (sym is IParameterSymbol or ITypeParameterSymbol) return null;
		string query;
		IModuleSymbol metadata = null;
		foreach (var loc in sym.Locations) {
			if ((metadata = loc.MetadataModule) != null) break;
		}
		if (metadata != null) {
			bool au = metadata.Name == "Au.dll";
			if (au && sym.IsEnumMember()) sym = sym.ContainingType;
			//print.it(sym, sym.GetType(), sym.GetType().GetInterfaces());
			if (sym is INamedTypeSymbol nt && nt.IsGenericType) {
				var qn = sym.QualifiedName(noDirectName: true);
				if (au) query = qn + "." + sym.MetadataName.Replace('`', '-');
				else query = $"{qn}.{sym.Name}<{string.Join(", ", nt.TypeParameters)}>";
			} else {
				query = sym.QualifiedName();
			}
			
			if (query.Ends("..ctor")) query = query.ReplaceAt(^6.., au ? ".-ctor" : " constructor");
			else if (query.Ends(".this[]")) query = query.ReplaceAt(^7.., ".Item");
			
			if (au) return HelpUtil.AuHelpUrl(query);
			if (metadata.Name.Starts("Au.")) return null;
			
			string kind = (sym is INamedTypeSymbol ints) ? ints.TypeKind.ToString() : sym.Kind.ToString();
			query = query + " " + kind.Lower();
		} else if (!sym.IsInSource()) { //eg an operator of string etc
			if (!(sym is IMethodSymbol me && me.MethodKind == MethodKind.BuiltinOperator)) return null;
			//print.it(sym, sym.Kind, sym.QualifiedName());
			//query = "C# " + sym.ToString(); //eg "string.operator +(string, string)", and Google finds just Equality
			//query = "C# " + sym.QualifiedName(); //eg "System.String.op_Addition", and Google finds nothing
			query = "C# " + sym.ToString().RxReplace(@"\(.+\)$", "", 1).Replace('.', ' '); //eg C# string operator +, not bad
		} else if (sym.IsExtern) { //[DllImport]
			query = sym.Name + " function";
		} else if (sym is INamedTypeSymbol nt1 && nt1.IsComImport) { //[ComImport] interface or coclass
			query = sym.Name + " " + nt1.TypeKind.ToString().Lower();
		} else if (sym.ContainingType?.IsComImport == true) { //[ComImport] interface method
			query = sym.ContainingType.Name + "." + sym.Name;
		} else if (_IsNativeApiClass(sym.ContainingType)) {
			if (sym is INamedTypeSymbol nt2) query = sym.Name + " " + (nt2.TypeKind switch { TypeKind.Struct => "structure", TypeKind.Enum => "enumeration", TypeKind.Delegate => "callback function", _ => null });
			else query = sym.Name; //constant or Guid/etc
		} else if (sym is IFieldSymbol && _IsNativeApiClass(sym.ContainingType.ContainingType)) {
			query = sym.ContainingType.Name + "." + sym.Name; //struct field or enum member
		} else {
			return null;
		}
		
		static bool _IsNativeApiClass(INamedTypeSymbol t)
			=> t?.TypeKind is TypeKind.Class or TypeKind.Struct
			&& (t.BaseType?.Name == "NativeApi" || t.Name.Contains("Native") || t.Name.Ends("Api"));
		
		return _GoogleURL(query);
	}
	
	/// <summary>
	/// Gets rectangle of caret if it was at the specified UTF-16 position.
	/// If <i>pos16</i> less than 0, uses current caret position.
	/// </summary>
	public static RECT GetCaretRectFromPos(SciCode doc, int pos16 = -1, bool inScreen = false) {
		int pos8 = pos16 < 0 ? doc.aaaCurrentPos8 : doc.aaaPos8(pos16);
		int x = doc.Call(Sci.SCI_POINTXFROMPOSITION, 0, pos8), y = doc.Call(Sci.SCI_POINTYFROMPOSITION, 0, pos8);
		var r = new RECT(x, y, 1, doc.Call(Sci.SCI_TEXTHEIGHT, doc.aaaLineFromPos(false, pos8)) + 2);
		if (inScreen) doc.AaWnd.MapClientToScreen(ref r);
		return r;
	}
	
	public static PSFormat GetParameterStringFormat(SyntaxNode node, SemanticModel semo, bool isString) {
		var kind = node.Kind();
		//print.it(kind);
		SyntaxNode parent;
		if (isString || kind == SyntaxKind.StringLiteralExpression) parent = node.Parent;
		else if (kind == SyntaxKind.InterpolatedStringText) parent = node.Parent.Parent;
		else return PSFormat.None;
		
		while (parent is BinaryExpressionSyntax && parent.IsKind(SyntaxKind.AddExpression)) parent = parent.Parent; //"string"+"string"+...
		
		PSFormat format = PSFormat.None;
		if (parent is ArgumentSyntax asy) {
			if (parent.Parent is ArgumentListSyntax alis) {
				if (alis.Parent is ExpressionSyntax es && es is BaseObjectCreationExpressionSyntax or InvocationExpressionSyntax) {
					if (semo.GetSymbolInfo(es).Symbol is IMethodSymbol m) {
						format = _GetFormat(m, alis);
						if (format == 0) {
							var ct = m.ContainingType.ToString();
							if (es is BaseObjectCreationExpressionSyntax) {
								if (ct is "System.Text.RegularExpressions.Regex" or "System.Text.RegularExpressions.RegexCompilationInfo")
									format = PSFormat.NetRegex;
							} else {
								if (ct is "System.Text.RegularExpressions.Regex" && m.Name is "IsMatch" or "Match" or "Matches" or "Replace" or "Split") {
									var aa = alis.Arguments;
									if (aa.Count >= 2 && (object)asy == aa[1]) format = PSFormat.NetRegex;
								}
							}
						}
					}
				}
			} else if (parent.Parent is BracketedArgumentListSyntax balis && balis.Parent is ElementAccessExpressionSyntax eacc) {
				if (semo.GetSymbolInfo(eacc).Symbol is IPropertySymbol ips && ips.IsIndexer) {
					var ims = ips.SetMethod;
					if (ims != null) format = _GetFormat(ims, balis);
				}
			}
			
			PSFormat _GetFormat(IMethodSymbol ims, BaseArgumentListSyntax alis) {
				IParameterSymbol p = null;
				var pa = ims.Parameters;
				var nc = asy.NameColon;
				if (nc != null) {
					var name = nc.Name.Identifier.Text;
					foreach (var v in pa) if (v.Name == name) { p = v; break; }
				} else {
					int i; var aa = alis.Arguments;
					for (i = 0; i < aa.Count; i++) if ((object)aa[i] == asy) break;
					if (i >= pa.Length && pa[^1].IsParams) i = pa.Length - 1;
					if (i < pa.Length) p = pa[i];
				}
				if (p != null) {
					var fa = p.GetAttributes().FirstOrDefault(o => o.AttributeClass.Name == "ParamStringAttribute");
					if (fa != null) return fa.GetConstructorArgument<PSFormat>(0, SpecialType.None);
				}
				return PSFormat.None;
			}
		}
		return format;
	}
	
	//rejected. Was useful when we did not have global usings. May cause confusion. May remove directives needed in the future.
	//public static string GetTextWithoutUnusedUsingDirectives() {
	//	if (!CodeInfo.GetContextAndDocument(out var cd, 0, metaToo: true)) return cd.code;
	//	var code = cd.code;
	//	var semo = cd.semanticModel;
	//	var a = semo.GetDiagnostics(null)
	//		.Where(d => d.Severity == DiagnosticSeverity.Hidden && d.Code == 8019)
	//		.Select(d => d.Location.SourceSpan)
	//		.OrderBy(span => span.Start);
	//	if (!a.Any()) return code;
	//	var b = new StringBuilder();
	//	int i = 0;
	//	foreach (var span in a) {
	//		int start = span.Start;
	//		if (start > i && code[start - 1] == ' ') start--;
	//		if (start > i) b.Append(code, i, start - i);
	//		i = span.End;
	//		if (b.Length == 0 || b[^1] == '\n') {
	//			if (code.Eq(i, "\r\n")) i += 2;
	//			else if (code.Eq(i, ' ')) i++;
	//		}
	//	}
	//	b.Append(code, i, code.Length - i);
	//	return b.ToString();
	//}
	
	/// <summary>
	/// Gets "global using Namespace;" directives from all files of compilation. Skips aliases and statics.
	/// </summary>
	public static IEnumerable<UsingDirectiveSyntax> GetAllGlobalUsings(SemanticModel model) {
		foreach (var st in model.Compilation.SyntaxTrees) {
			foreach (var u in st.GetCompilationUnitRoot().Usings) {
				if (u.GlobalKeyword.RawKind == 0) break;
				if (u.Alias != null || u.StaticKeyword.RawKind != 0) continue;
				yield return u;
			}
		}
	}
	
	/// <summary>
	/// From C# code creates a Roslyn workspace+project+document for code analysis.
	/// If <i>needSemantic</i>, adds default references and a document with default global usings (same as in default global.cs).
	/// </summary>
	/// <param name="ws"><c>using var ws = new AdhocWorkspace(); //need to dispose</c></param>
	/// <param name="code">Any C# code fragment, valid or not.</param>
	/// <param name="needSemantic">Add default references (.NET and Au.dll) and global usings.</param>
	public static Document CreateDocumentFromCode(AdhocWorkspace ws, string code, bool needSemantic) {
		ProjectId projectId = ProjectId.CreateNewId();
		DocumentId documentId = DocumentId.CreateNewId(projectId);
		var pi = ProjectInfo.Create(projectId, VersionStamp.Default, "l", "l", LanguageNames.CSharp, null, null,
			new CSharpCompilationOptions(OutputKind.WindowsApplication, allowUnsafe: true),
			new CSharpParseOptions(LanguageVersion.Preview),
			metadataReferences: needSemantic ? new MetaReferences().Refs : null //tested: does not make slower etc
			);
		var sol = ws.CurrentSolution.AddProject(pi);
		if (needSemantic) {
			sol = sol.AddDocument(DocumentId.CreateNewId(projectId), "g.cs", c_globalUsingsText);
		}
		return sol.AddDocument(documentId, "l.cs", code).GetDocument(documentId);
		
		//It seems it's important to dispose workspaces.
		//	In the docs project at first didn't dispose. After maybe 300_000 times: much slower, process memory 3 GB, sometimes hangs.
	}
	
	public const string c_globalUsingsText = """
global using Au;
global using Au.Types;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Collections.Concurrent;
global using System.Diagnostics;
global using System.Globalization;
global using System.IO;
global using System.IO.Compression;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Win32;
global using Au.More;
global using Au.Triggers;
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Media;
""";
	
	/// <summary>
	/// Creates Compilation from a file or project folder.
	/// Supports meta etc, like the compiler. Does not support test script, meta testInternal, project references.
	/// </summary>
	/// <param name="f">A code file or a project folder. If in a project folder, creates from the project.</param>
	/// <returns>null if can't create, for example if f isn't a code file or if meta contains errors.</returns>
	public static Compilation CreateCompilationFromFileNode(FileNode f) { //not CSharpCompilation, it creates various small problems
		if (f.FindProject(out var projFolder, out var projMain)) f = projMain;
		if (!f.IsCodeFile) return null;
		
		var m = new MetaComments(MCPFlags.ForCodeInfo);
		if (!m.Parse(f, projFolder)) return null; //with this flag never returns false, but anyway
		
		var pOpt = m.CreateParseOptions();
		var trees = new CSharpSyntaxTree[m.CodeFiles.Count];
		for (int i = 0; i < trees.Length; i++) {
			var f1 = m.CodeFiles[i];
			trees[i] = CSharpSyntaxTree.ParseText(f1.code, pOpt, f1.f.FilePath, Encoding.Default) as CSharpSyntaxTree;
		}
		
		var cOpt = m.CreateCompilationOptions();
		return CSharpCompilation.Create("Compilation", trees, m.References.Refs, cOpt);
	} //FUTURE: remove if unused
	
	/// <summary>
	/// Creates Solution from a file or project folder.
	/// Supports meta etc, like the compiler. Does not support test script, meta testInternal, project references.
	/// </summary>
	/// <param name="ws"><c>using var ws = new AdhocWorkspace(); //need to dispose</c></param>
	/// <param name="f">A code file or a project folder. If in a project folder, creates from the project.</param>
	/// <returns>null if can't create, for example if f isn't a code file or if meta contains errors.</returns>
	public static (Solution sln, MetaComments meta) CreateSolutionFromFileNode(AdhocWorkspace ws, FileNode f) {
		if (f.FindProject(out var projFolder, out var projMain)) f = projMain;
		if (!f.IsCodeFile) return default;
		
		var m = new MetaComments(MCPFlags.ForCodeInfo);
		if (!m.Parse(f, projFolder)) return default; //with this flag never returns false, but anyway
		
		var projectId = ProjectId.CreateNewId();
		var adi = new List<DocumentInfo>();
		foreach (var f1 in m.CodeFiles) {
			var docId = DocumentId.CreateNewId(projectId);
			var tav = TextAndVersion.Create(SourceText.From(f1.code, Encoding.UTF8), VersionStamp.Default, f1.f.FilePath);
			adi.Add(DocumentInfo.Create(docId, f1.f.Name, null, SourceCodeKind.Regular, TextLoader.From(tav), f1.f.ItemPath));
		}
		
		var pi = ProjectInfo.Create(projectId, VersionStamp.Default, f.Name, f.Name, LanguageNames.CSharp, null, null,
			m.CreateCompilationOptions(),
			m.CreateParseOptions(),
			adi,
			null,
			m.References.Refs);
		
		return (ws.CurrentSolution.AddProject(pi), m);
	}
	
	/// <summary>
	/// For C# code gets style bytes that can be used with SCI_SETSTYLINGEX for UTF-8 text.
	/// Uses Classifier.GetClassifiedSpansAsync, like the code editor.
	/// Controls that use this should set styles like this example, probably when handle created:
	/// var styles = new CiStyling.TStyles { FontSize = 9 };
	/// styles.ToScintilla(this);
	/// </summary>
	public static byte[] GetScintillaStylingBytes(string code) {
		var styles8 = new byte[Encoding.UTF8.GetByteCount(code)];
		var map8 = styles8.Length == code.Length ? null : Convert2.Utf8EncodeAndGetOffsets_(code).offsets;
		using var ws = new AdhocWorkspace();
		var document = CreateDocumentFromCode(ws, code, needSemantic: true);
		var semo = document.GetSemanticModelAsync().Result;
		var a = Classifier.GetClassifiedSpansAsync(document, TextSpan.FromBounds(0, code.Length)).Result;
		foreach (var v in a) {
			var ct = v.ClassificationType; if (ct == ClassificationTypeNames.StaticSymbol) continue; /*duplicate*/
			//print.it(v.TextSpan, ct, code[v.TextSpan.Start..v.TextSpan.End]);
			EStyle style = CiStyling.StyleFromClassifiedSpan(v, semo);
			if (style == EStyle.None) continue;
			int i = v.TextSpan.Start, end = v.TextSpan.End;
			if (map8 != null) { i = map8[i]; end = map8[end]; }
			while (i < end) styles8[i++] = (byte)style;
		}
		return styles8;
	}
	
	/// <summary>
	/// Returns true if <i>code</i> contains global statements or is empty or the first method of the first class is named "Main".
	/// </summary>
	public static bool IsScript(string code) {
		var cu = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Preview)).GetCompilationUnitRoot();
		var f = cu.Members.FirstOrDefault();
		if (f != null) {
			if (f is GlobalStatementSyntax) return true;
			if (f is BaseNamespaceDeclarationSyntax nd) f = nd.Members.FirstOrDefault();
			if (f is ClassDeclarationSyntax cd && cd.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.Text == "Main") return true;
		} else {
			var u = cu.Usings.FirstOrDefault();
			if (u != null && u.GlobalKeyword.RawKind != 0) return false; //global.cs?
			return !cu.AttributeLists.Any(); //AssemblyInfo.cs?
		}
		return false;
	}
	
#if DEBUG
	public static void PrintNode(SyntaxNode x, int pos = 0, bool printNode = true, bool printErrors = false) {
		if (x == null) { print.it("null"); return; }
		if (printNode) print.it($"<><c blue>{pos}, {x.Span}, {x.FullSpan}, k={x.Kind()}, t={x.GetType().Name},<> '<c green>{(x is CompilationUnitSyntax ? null : x.ToString().Limit(10, middle: true, lines: true))}<>'");
		if (printErrors) foreach (var d in x.GetDiagnostics()) print.it(d.Code, d.Location.SourceSpan, d);
	}
	
	public static void PrintNode(SyntaxToken x, int pos = 0, bool printNode = true, bool printErrors = false) {
		if (printNode) print.it($"<><c blue>{pos}, {x.Span}, {x.Kind()},<> '<c green>{x.ToString().Limit(10, middle: true, lines: true)}<>'");
		if (printErrors) foreach (var d in x.GetDiagnostics()) print.it(d.Code, d.Location.SourceSpan, d);
	}
	
	public static void PrintNode(SyntaxTrivia x, int pos = 0, bool printNode = true, bool printErrors = false) {
		if (printNode) print.it($"<><c blue>{pos}, {x.Span}, {x.Kind()},<> '<c green>{x.ToString().Limit(10, middle: true, lines: true)}<>'");
		if (printErrors) foreach (var d in x.GetDiagnostics()) print.it(d.Code, d.Location.SourceSpan, d);
	}
	
	public static void HiliteRange(int start, int end) {
		var doc = Panels.Editor.ActiveDoc;
		doc.EInicatorsFound_(null);
		doc.EInicatorsFound_(new List<Range> { start..end });
	}
	
	public static void HiliteRange(TextSpan span) => HiliteRange(span.Start, span.End);
	
	public static void HiliteRanges(List<Range> a) {
		var doc = Panels.Editor.ActiveDoc;
		doc.EInicatorsFound_(null);
		doc.EInicatorsFound_(a);
	}
	
	static IEnumerable<string> GetSymbolInterfaces(ISymbol sym) {
		return sym.GetType().FindInterfaces((t, _) => t.Name.Ends("Symbol") && t.Name != "ISymbol", null).Select(o => o.Name);
	}
	
#endif
	
	public static CiItemKind MemberDeclarationToKind(MemberDeclarationSyntax m) {
		return m switch {
			ClassDeclarationSyntax => CiItemKind.Class,
			StructDeclarationSyntax => CiItemKind.Structure,
			RecordDeclarationSyntax rd => rd.IsKind(SyntaxKind.RecordStructDeclaration) ? CiItemKind.Structure : CiItemKind.Class,
			EnumDeclarationSyntax => CiItemKind.Enum,
			DelegateDeclarationSyntax => CiItemKind.Delegate,
			InterfaceDeclarationSyntax => CiItemKind.Interface,
			OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax or IndexerDeclarationSyntax => CiItemKind.Operator,
			BaseMethodDeclarationSyntax => CiItemKind.Method,
			// => CiItemKind.ExtensionMethod,
			PropertyDeclarationSyntax => CiItemKind.Property,
			EventDeclarationSyntax or EventFieldDeclarationSyntax => CiItemKind.Event,
			FieldDeclarationSyntax f => f.Modifiers.Any(o => o.Text == "const") ? CiItemKind.Constant : CiItemKind.Field,
			EnumMemberDeclarationSyntax => CiItemKind.EnumMember,
			BaseNamespaceDeclarationSyntax => CiItemKind.Namespace,
			_ => CiItemKind.None
		};
	}
	
	public static void TagsToKindAndAccess(ImmutableArray<string> tags, out CiItemKind kind, out CiItemAccess access) {
		kind = CiItemKind.None;
		access = default;
		if (tags.IsDefaultOrEmpty) return;
		kind = tags[0] switch {
			WellKnownTags.Class => CiItemKind.Class,
			WellKnownTags.Structure => CiItemKind.Structure,
			WellKnownTags.Enum => CiItemKind.Enum,
			WellKnownTags.Delegate => CiItemKind.Delegate,
			WellKnownTags.Interface => CiItemKind.Interface,
			WellKnownTags.Method => CiItemKind.Method,
			WellKnownTags.ExtensionMethod => CiItemKind.ExtensionMethod,
			WellKnownTags.Property => CiItemKind.Property,
			WellKnownTags.Operator => CiItemKind.Operator,
			WellKnownTags.Event => CiItemKind.Event,
			WellKnownTags.Field => CiItemKind.Field,
			WellKnownTags.Local => CiItemKind.LocalVariable,
			WellKnownTags.Parameter => CiItemKind.LocalVariable,
			WellKnownTags.RangeVariable => CiItemKind.LocalVariable,
			WellKnownTags.Constant => CiItemKind.Constant,
			WellKnownTags.EnumMember => CiItemKind.EnumMember,
			WellKnownTags.Keyword => CiItemKind.Keyword,
			WellKnownTags.Namespace => CiItemKind.Namespace,
			WellKnownTags.Label => CiItemKind.Label,
			WellKnownTags.TypeParameter => CiItemKind.TypeParameter,
			//WellKnownTags.Snippet => CiItemKind.Snippet,
			_ => CiItemKind.None
		};
		if (tags.Length > 1) {
			access = tags[1] switch {
				WellKnownTags.Private => CiItemAccess.Private,
				WellKnownTags.Protected => CiItemAccess.Protected,
				WellKnownTags.Internal => CiItemAccess.Internal,
				_ => default
			};
		}
	}
	
	//The order must match CiItemKind.
	public static string[] ItemKindNames { get; } = new[] {
		"Class",
		"Structure",
		"Interface",
		"Enum",
		"Delegate",
		"Method",
		"ExtensionMethod",
		"Property",
		"Operator",
		"Event",
		"Field",
		"LocalVariable",
		"Constant",
		"EnumMember",
		"Namespace",
		"Keyword",
		"Label",
		"Snippet",
		"TypeParameter"
	};
	
#if DEBUG
	//unfinished. Just prints what we can get from CSharpSyntaxContext.
	public static /*CiContextType*/void GetContextType(/*in CodeInfo.Context cd,*/ CSharpSyntaxContext c) {
		//print.it("--------");
		print.clear();
		//print.it(cd.pos);
		_Print("IsInNonUserCode", c.IsInNonUserCode);
		_Print("IsGlobalStatementContext", c.IsGlobalStatementContext);
		_Print("IsAnyExpressionContext", c.IsAnyExpressionContext);
		//_Print("IsAtStartOfPattern", c.IsAtStartOfPattern);
		//_Print("IsAtEndOfPattern", c.IsAtEndOfPattern);
		_Print("IsAttributeNameContext", c.IsAttributeNameContext);
		//_Print("IsCatchFilterContext", c.IsCatchFilterContext);
		_Print("IsConstantExpressionContext", c.IsConstantExpressionContext);
		_Print("IsCrefContext", c.IsCrefContext);
		_Print("IsDeclarationExpressionContext", c.IsDeclarationExpressionContext);
		//_Print("IsDefiniteCastTypeContext", c.IsDefiniteCastTypeContext);
		//_Print("IsEnumBaseListContext", c.IsEnumBaseListContext);
		//_Print("IsFixedVariableDeclarationContext", c.IsFixedVariableDeclarationContext);
		//_Print("IsFunctionPointerTypeArgumentContext", c.IsFunctionPointerTypeArgumentContext);
		//_Print("IsGenericTypeArgumentContext", c.IsGenericTypeArgumentContext);
		//_Print("IsImplicitOrExplicitOperatorTypeContext", c.IsImplicitOrExplicitOperatorTypeContext);
		_Print("IsInImportsDirective", c.IsInImportsDirective);
		//_Print("IsInQuery", c.IsInQuery);
		_Print("IsInstanceContext", c.IsInstanceContext);
		//_Print("IsIsOrAsOrSwitchOrWithExpressionContext", c.IsIsOrAsOrSwitchOrWithExpressionContext);
		//_Print("IsIsOrAsTypeContext", c.IsIsOrAsTypeContext);
		_Print("IsLabelContext", c.IsLabelContext);
		_Print("IsLocalVariableDeclarationContext", c.IsLocalVariableDeclarationContext);
		//_Print("IsMemberAttributeContext", c.IsMemberAttributeContext(new HashSet<SyntaxKind>(), default));
		_Print("IsMemberDeclarationContext", c.IsMemberDeclarationContext());
		_Print("IsNameOfContext", c.IsNameOfContext);
		_Print("IsNamespaceContext", c.IsNamespaceContext);
		_Print("IsNamespaceDeclarationNameContext", c.IsNamespaceDeclarationNameContext);
		_Print("IsNonAttributeExpressionContext", c.IsNonAttributeExpressionContext);
		_Print("IsObjectCreationTypeContext", c.IsObjectCreationTypeContext);
		_Print("IsOnArgumentListBracketOrComma", c.IsOnArgumentListBracketOrComma);
		_Print("IsParameterTypeContext", c.IsParameterTypeContext);
		_Print("IsPossibleLambdaOrAnonymousMethodParameterTypeContext", c.IsPossibleLambdaOrAnonymousMethodParameterTypeContext);
		_Print("IsPossibleTupleContext", c.IsPossibleTupleContext);
		_Print("IsPreProcessorDirectiveContext", c.IsPreProcessorDirectiveContext);
		_Print("IsPreProcessorExpressionContext", c.IsPreProcessorExpressionContext);
		_Print("IsPreProcessorKeywordContext", c.IsPreProcessorKeywordContext);
		_Print("IsPrimaryFunctionExpressionContext", c.IsPrimaryFunctionExpressionContext);
		_Print("IsRightOfNameSeparator", c.IsRightOfNameSeparator);
		_Print("IsRightSideOfNumericType", c.IsRightSideOfNumericType);
		_Print("IsStatementAttributeContext", c.IsStatementAttributeContext());
		_Print("IsStatementContext", c.IsStatementContext);
		_Print("IsTypeArgumentOfConstraintContext", c.IsTypeArgumentOfConstraintContext);
		_Print("IsTypeAttributeContext", c.IsTypeAttributeContext(default));
		_Print("IsTypeContext", c.IsTypeContext);
		_Print("IsTypeDeclarationContext", c.IsTypeDeclarationContext());
		_Print("IsTypeOfExpressionContext", c.IsTypeOfExpressionContext);
		_Print("IsWithinAsyncMethod", c.IsWithinAsyncMethod);
		//_Print("", c.);
		//_Print("", c.);
		//_Print("", c.);
		//_Print("", c.);
		
		static void _Print(string s, bool value) {
			if (value) print.it($"<><c red>{s}<>");
			else print.it(s);
		}
		
		//return CiContextType.Namespace;
	}
#endif
	
	//unfinished. Also does not support namespaces.
	//public static CiContextType GetContextType(CompilationUnitSyntax t, int pos) {
	//	var members = t.Members;
	//	var ms = members.FullSpan;
	//	//foreach(var v in members) print.it(v.GetType().Name, v); return 0;
	//	//print.it(pos, ms);
	//	//CiUtil.HiliteRange(ms);
	//	if (ms == default) { //assume empty top-level statements
	//		var v = t.AttributeLists.FullSpan;
	//		if (v == default) {
	//			v = t.Usings.FullSpan;
	//			if (v == default) v = t.Externs.FullSpan;
	//		}
	//		if (pos >= v.End) return CiContextType.Method;
	//	} else if (pos < ms.Start) {
	//	} else if (pos >= members.Span.End) {
	//		if (members.Last() is GlobalStatementSyntax) return CiContextType.Method;
	//	} else {
	//		int i = members.IndexOf(o => o is not GlobalStatementSyntax);
	//		if (i < 0 || pos <= members[i].SpanStart) return CiContextType.Method;
	
	//		//now the difficult part
	//		ms = members[i].Span;
	//		print.it(pos, ms);
	//		CiUtil.HiliteRange(ms);
	//		//unfinished. Here should use CSharpSyntaxContext.
	//	}
	//	return CiContextType.Namespace;
	//}
}

//enum CiContextType
//{
//	/// <summary>
//	/// Outside class/method/topLevelStatements. Eg before using directives or at end of file.
//	/// Completion list must not include types.
//	/// </summary>
//	Namespace,

//	/// <summary>
//	/// Inside class but outside method.
//	/// Completion list can include types but not functions and values.
//	/// </summary>
//	Class,

//	/// <summary>
//	/// Inside method/topLevelStatements.
//	/// Completion list can include all symbols.
//	/// </summary>
//	Method
//}
