using System.Web;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp;

using System.Windows;
using System.Windows.Controls;
using Au.Controls;

//FUTURE: try source link. Like now VS.

class CiGoTo {
	struct _SourceLocation {
		public _SourceLocation(string file, int line, int column) {
			this.file = file; this.line = line; this.column = column;
		}
		public string file;
		public int line, column;
	}
	
	bool _canGoTo, _inSource;
	//if in source
	List<_SourceLocation> _sourceLocations;
	//if in metadata
	string _assembly, _repo, _type, _member, _namespace, _filename, _alt;
	
	/// <summary>
	/// true if can go to the symbol source. Then caller eg can add link to HTML.
	/// May return true even if can't go to. Then on link click nothing happens. Next time will return false.
	/// </summary>
	public bool CanGoTo => _canGoTo;
	
	CiGoTo(bool inSource) { _canGoTo = true; _inSource = inSource; }
	
	/// <summary>
	/// Gets info required to go to symbol source file/line/position or website.
	/// This function is fast. The slower code is in the <b>GoTo</b> functions.
	/// </summary>
	public CiGoTo(ISymbol sym) {
		if (_inSource = sym.IsFromSource()) {
			_sourceLocations = new();
			foreach (var loc in sym.Locations) {
				Debug_.PrintIf(!loc.IsVisibleSourceLocation());
				//if (!loc.IsVisibleSourceLocation()) continue;
				
				var v = loc.GetLineSpan();
				_sourceLocations.Add(new _SourceLocation(v.Path, v.StartLinePosition.Line, v.StartLinePosition.Character));
			}
			_canGoTo = _sourceLocations.Count > 0;
		} else {
			var asm = sym.ContainingAssembly; if (asm == null) return;
			if (sym is INamespaceSymbol) return; //not useful
			_canGoTo = true;
			_assembly = asm.Name;
			
			if (asm.GetAttributes().FirstOrDefault(o => o.AttributeClass.Name == "AssemblyMetadataAttribute" && "RepositoryUrl" == o.ConstructorArguments[0].Value as string)?.ConstructorArguments[1].Value is string s) {
				if (s.Starts("git:")) s = s.ReplaceAt(0, 3, "https"); //eg .NET
				if (s.Starts("https://github.com/")) _repo = s[19..];
			}
			
			//Unfortunately the github search engine is so bad. Gives lots of garbage. Randomly returns not all results.
			//To remove some garbage, can include namespace, filename, path (can be partial, without filename).
			//There is no best way for all casses. GoTo() will show UI, and users can try several alternatives.
			//At first this class used referencesource, not github. Can jump directly to the class or method etc.
			//	But it seems it is now almost dead. Many API either don't exist or are obsolete (I guess) or only Unix version.
			//	And it was only for .NET and Roslyn.
			
			if (sym is not INamedTypeSymbol ts) {
				ts = sym.ContainingType;
				_member = sym.Name;
				if (_member.Starts('.')) _member = null; //".ctor"
				
				//CONSIDER: for methods etc prepend the return type, like "int Method" instead of Method.
			}
			ts = ts.OriginalDefinition; //eg List<int> -> List<T>
			string kind = null;
			switch (ts.TypeKind) {
			case TypeKind.Class: kind = ts.IsRecord ? "record" : "class"; break;
			case TypeKind.Struct: kind = "struct"; break; //don't need "record". And currently IsRecord returns false for record struct.
			case TypeKind.Enum: kind = "enum"; break;
			case TypeKind.Interface: kind = "interface"; break;
			case TypeKind.Delegate: kind = "delegate " + ts.DelegateInvokeMethod?.ReturnType.GetShortName(); break; //never mind: can be generic. Rare.
			}
			if (kind == "record") _type = $"{kind} {ts.GetShortName()}"; //without quotes. Can be 'record Name' or 'record class Name'. Github does not support OR.
			else _type = $"\"{kind} {ts.GetShortName()}\""; //GetShortName gets name like int or List. Github ignores <T> and fails if eg <TEventArgs>.
			_namespace = $"\"namespace {ts.ContainingNamespace.QualifiedName()}\"";
			_filename = ts.Name;
			_alt = ts.ToString(); //for source.dot.net need exact generic, preferably fully qualified, like Namespace.List<T>, but without in/out like ToNameDisplayString
		}
	}
	
	string _GetLinkData() {
		if (!_canGoTo) return null;
		if (_inSource) {
			var b = new StringBuilder();
			foreach (var v in _sourceLocations) b.AppendFormat("|{0}?{1},{2}", v.file, v.line, v.column);
			return b.ToString();
		} else {
			return $"||{_assembly}|{_repo}|{_type}|{_member}|{_namespace}|{_filename}|{_alt}";
		}
	}
	
	/// <summary>
	/// Gets link data string for <see cref="LinkGoTo"/>. Returns null if unavailable.
	/// This function is fast. The slower code is in the <b>GoTo</b> function.
	/// </summary>
	public static string GetLinkData(ISymbol sym) => new CiGoTo(sym)._GetLinkData();
	
	/// <summary>
	/// Opens symbol source file/line/position or shows github search UI. Used on link click.
	/// </summary>
	/// <param name="linkData">String returned by <see cref="GetLinkData"/>.</param>
	public static void LinkGoTo(string linkData) {
		if (linkData == null) return;
		bool inSource = !linkData.Starts("||");
		var g = new CiGoTo(inSource);
		var a = linkData.Split('|');
		if (inSource) {
			g._sourceLocations = new List<_SourceLocation>();
			for (int i = 1; i < a.Length; i++) {
				var s = a[i];
				int line = s.LastIndexOf('?'), column = s.LastIndexOf(',');
				g._sourceLocations.Add(new _SourceLocation(s.Remove(line), s.ToInt(line + 1), s.ToInt(column + 1)));
			}
		} else {
			g._assembly = a[2];
			g._repo = a[3];
			g._type = a[4];
			g._member = a[5];
			g._namespace = a[6];
			g._filename = a[7];
			g._alt = a[8];
		}
		g.GoTo();
	}
	
	/// <summary>
	/// Opens symbol source file/line/position or website.
	/// If need to open website, runs async task.
	/// </summary>
	public void GoTo() {
		if (!_canGoTo) return;
		if (_inSource) {
			if (_sourceLocations.Count == 1) {
				_GoTo(_sourceLocations[0]);
			} else {
				int i = popupMenu.showSimple(_sourceLocations.Select(v => v.file + ", line " + v.line.ToString()).ToArray());
				if (i > 0) _GoTo(_sourceLocations[i - 1]);
			}
			
			static void _GoTo(_SourceLocation v) => App.Model.OpenAndGoTo(v.file, v.line, v.column);
		} else {
			AssemblySett se = null;
			App.Settings.ci_gotoAsm?.TryGetValue(_assembly, out se);
			
			var b = new wpfBuilder("GitHub search").WinSize(450).Columns(-1);
			b.StartGrid<GroupBox>("Query").Columns(70, -1);
			b.R.Add("Type", out TextBox tType, _type);
			b.R.Add("Member", out TextBox tMember, _member).Tooltip("A member of the type (method, property, etc), or any word in the file.\r\nCan be multiple, like M1 M2 M3, if all are in same file.");
			b.R.Add(out KCheckBox cText, "Text", out TextBox tText, _namespace).Tooltip("The file also must contain this text");
			b.R.Add(out KCheckBox cFile, "File", out TextBox tFile, _filename).Tooltip("The filename must contain this text");
			b.End();
			b.StartGrid<GroupBox>("Source (saved for each assembly)").Columns(70, -1);
			b.R.Add("Assembly", out Label _, _assembly).And(50).Add(out KCheckBox cSharp, "C#").Checked(se?.csharp ?? true);
			b.R.Add("Repository", out TextBox tRepo, se?.repo ?? _repo)
				.Tooltip("Repository name, like dotnet/runtime\r\nIf empty, try to find the repository in github and copy its name from its URL.")
				.Validation(_ => tRepo.Text.NE() ? "Repository cannot be empty" : null);
			b.R.Add("Path", out TextBox tPath, se?.path).Tooltip("Optional full or partial path without filename.\r\nExample: /src/libraries");
			b.End();
			b.StartGrid<GroupBox>("Info");
			b.Add(out TextBlock info).Text(@"This dialog creates and opens a GitHub search URL.
Note: GitHub results are random. Reload the page several times.
Note: GitHub skips large source files.
See also: ", "<a>source.dot.net", new Action(_Link1));
			info.TextWrapping = TextWrapping.Wrap;
			b.End();
			b.AddOkCancel(apply: "Search");
			b.End();
			
			//b.Window.ShowInTaskbar = false; b.Window.Owner = App.Wmain;
			b.Window.Show();
			
			b.OkApply += _ => {
				string repo = tRepo.Text.NullIfEmpty_(), path = tPath.Text.NullIfEmpty_(); bool csharp = cSharp.IsChecked;
				if (repo == _repo && path == null && csharp) {
					//print.it("remove");
					App.Settings.ci_gotoAsm?.Remove(_assembly);
				} else if (repo != (se?.repo ?? _repo) || path != se?.path || csharp != (se?.csharp ?? true)) {
					//print.it("save");
					(App.Settings.ci_gotoAsm ??= new())[_assembly] = new() { repo = repo == _repo ? null : repo, path = path, csharp = csharp };
				}
				
				var q = new StringBuilder(tType.Text);
				if (!tMember.Text.NE()) q.Append(' ').Append(tMember.Text);
				if (cText.IsChecked && !tText.Text.NE()) q.Append(' ').Append(tText.Text);
				if (cFile.IsChecked && !tFile.Text.NE()) q.Append(" filename:").Append(tFile.Text);
				if (path != null) q.Append(" path:").Append(path);
				//print.it(q);
				var url = $"https://github.com/{repo}/search?{(csharp ? "l=C%23&" : null)}q={HttpUtility.UrlEncode(q.ToString())}";
				//print.it(url);
				//if (!keys.isScrollLock) return;
				run.itSafe(url);
			};
			
			void _Link1() {
				var s = "https://source.dot.net/#q=" + _alt;
				if (_member != null) s = s + "." + _member;
				run.itSafe(s);
				b.Window.Close();
			}
		}
	}
	
	internal record AssemblySett {
		public string repo, path;
		public bool csharp;
	}
	
	//This was used with referencesource. Not sure whether need it now too.
	///// <summary>
	///// If _filename or its ancestor type is forwarded to another assembly, replaces _assembly with the name of that assembly.
	///// For example initially we get that String is in System.Runtime. But actually it is in System.Private.CoreLib, and its name must be passed to https://source.dot.net.
	///// Speed: usually < 10 ms.
	///// </summary>
	//void _GetAssemblyNameOfForwardedType() {
	//	var path = folders.NetRuntimeBS + _assembly + ".dll";
	//	if (!(filesystem.exists(path).File || filesystem.exists(path = folders.NetRuntimeDesktopBS + _assembly + ".dll").File)) return;
	
	//	var alc = new System.Runtime.Loader.AssemblyLoadContext(null, true);
	//	try {
	//		var asm = alc.LoadFromAssemblyPath(path);
	//		var ft = asm.GetForwardedTypes()?.FirstOrDefault(ty => ty.FullName == _filename);
	//		if (ft != null) _assembly = ft.Assembly.GetName().Name;
	//	}
	//	catch (Exception ex) { Debug_.Print(ex); }
	//	finally { alc.Unload(); }
	//}
	
	public static void GoToDefinition() {
		if (!CodeInfo.GetContextAndDocument(out var cd, metaToo: true)) return;
		var (sym, _, helpKind, token) = CiUtil.GetSymbolEtcFromPos(cd);
		if (sym != null) {
			if (sym is IParameterSymbol or ITypeParameterSymbol && !sym.IsInSource()) return;
			if (_GetFoldersPath(token, out var fp, false)) {
				run.it(fp);
			} else {
				var g = new CiGoTo(sym);
				if (g.CanGoTo) g.GoTo();
			}
		} else if (cd.sci.aaaHasSelection) {
			_Open(cd.sci.aaaSelectedText());
		} else if (helpKind == CiUtil.HelpKind.String && token.Parent.IsKind(SyntaxKind.StringLiteralExpression)) {
			_Open(token.ValueText, token);
		} else if (helpKind == CiUtil.HelpKind.None && cd != null) { //maybe path in comments or disabled code
			int pos = cd.pos;
			if (pos < cd.meta.end && pos > cd.meta.start) {
				foreach (var t in Au.Compiler.MetaComments.EnumOptions(cd.code, cd.meta)) {
					if (pos >= t.valueStart && pos <= t.valueStart + t.valueLen) {
						_Open(cd.code[t.valueStart..(t.valueStart + t.valueLen)]);
						break;
					}
				}
			} else {
				var root = cd.syntaxRoot;
				var trivia = root.FindTrivia(pos); if (trivia.RawKind == 0) return;
				var code = cd.code;
				var span = trivia.Span;
				int from = pos, to = from;
				static bool _IsPathOrUrlChar(char c) => !pathname.isInvalidPathChar(c) || c == '?';
				while (to < span.End && _IsPathOrUrlChar(code[to])) to++;
				while (from > span.Start && _IsPathOrUrlChar(code[from - 1])) from--;
				while (to > pos && code[to - 1] is <= ' ' or '.' or ';' or ',') to--; //trim right
				while (from < pos && code[from] <= ' ') from++; //trim left
				if (to == from) return;
				//rejected: try to find path even if without "". How to know which text part it is? Better let the user select the text.
				if (!(code.Eq(from - 1, '\"') && code.Eq(to, '\"'))) return;
				_Open(code[from..to]);
			}
		}
		
		//if s is script, opens it. If file path, selects in Explorer. If folder path, opens the folder. If URL, opens the web page.
		static bool _Open(string s, SyntaxToken token = default) {
			if (s.NE()) return false;
			if (pathname.isUrl(s)) {
				if (0 == s.Starts(false, "http:", "https:")) return false;
				run.itSafe(s);
			} else {
				if (!pathname.isFullPathExpand(ref s, strict: false)) {
					var f = App.Model.Find(s);
					if (f != null) { App.Model.SetCurrentFile(f); return true; }
					if (token.RawKind == 0 || !_GetFoldersPath(token, out var fp, true)) return false;
					s = pathname.combine(fp, s);
				}
				var fe = filesystem.exists(s); if (!fe) return false;
				if (fe.Directory) run.itSafe(s);
				else run.selectInExplorer(s);
			}
			return true;
		}
		
		//If t is "string" in 'folders.Folder + "string"' or Folder in 'folders.Folder', gets folder path and return true.
		static bool _GetFoldersPath(SyntaxToken t, out string s, bool isString) {
			s = null;
			if (isString) {
				t = t.GetPreviousToken(); if (!t.IsKind(SyntaxKind.PlusToken)) return false;
				t = t.GetPreviousToken();
			}
			if (!t.IsKind(SyntaxKind.IdentifierToken)) return false;
			var s2 = t.Text;
			t = t.GetPreviousToken(); if (!t.IsKind(SyntaxKind.DotToken)) return false;
			t = t.GetPreviousToken(); if (!t.IsKind(SyntaxKind.IdentifierToken) || t.Text != "folders") return false;
			s = folders.getFolder(s2);
			return s != null;
		}
	}
	
	public static void GoToBase() {
		var (sym, cd) = CiUtil.GetSymbolFromPos(andZeroLength: true);
		List<ISymbol> a = new();
		if (sym is INamedTypeSymbol nt && nt.TypeKind is TypeKind.Class or TypeKind.Interface) {
			if (nt.TypeKind == TypeKind.Interface) {
				a.AddRange(nt.AllInterfaces);
			} else if (nt.BaseType != null) { //else object
				a.AddRange(nt.GetBaseTypes().SkipLast(1).Concat(nt.AllInterfaces));
			}
		} else if (sym.Kind is SymbolKind.Method or SymbolKind.Property or SymbolKind.Event) {
			a.AddRange(Microsoft.CodeAnalysis.FindSymbols.FindReferences.BaseTypeFinder.FindOverriddenAndImplementedMembersAsync(sym, cd.document.Project.Solution, default).Result);
		}
		if (a.Count == 0) return;
		if (a.Count == 1) sym = a[0];
		else {
			int i = popupMenu.showSimple(a.Select(o => o.Name).ToArray());
			if (--i < 0) return;
			sym = a[i];
		}
		var g = new CiGoTo(sym);
		if (g.CanGoTo) g.GoTo();
	}
}

//Also tried github API. Can get search results, but not always can find the match in the garbage.
//The returned code snippets are small and often don't contain the search words. Can download entire file, but it's too slow and dirty.
//The test code is in some script. Tested octokit too, but don't need it, it's just wraps the REST API, which is easy to use.
