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
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Au;
using Au.Types;
using Aga.Controls.Tree;
using Au.Compiler;

partial class FileNode : Au.Util.ATreeBase<FileNode>
{
	#region types

	//Not saved in file.
	[Flags]
	enum _State : byte
	{
		Link = 1,
		Deleted = 2,

	}

	//Saved in file.
	[Flags]
	enum _Flags : byte
	{
		//HasTriggers = 1,
	}

	#endregion

	#region fields, ctors, load/save

	FilesModel _model;
	string _name;
	string _displayName;
	uint _id;
	EFileType _type;
	_State _state;
	_Flags _flags;
	string _iconOrLinkTarget;
	uint _testScriptId;

	//public Microsoft.CodeAnalysis.DocumentId CaDocumentId;

	void _CtorMisc(string linkTarget)
	{
		if(!IsFolder) {
			if(!linkTarget.NE()) {
				_state |= _State.Link;
				_iconOrLinkTarget = linkTarget;
			}
		}
	}

	//this ctor is used when creating new item of known type
	public FileNode(FilesModel model, string name, EFileType type, string linkTarget = null)
	{
		_model = model;
		_type = type;
		_name = name;
		_id = _model.AddGetId(this);
		_CtorMisc(linkTarget);
	}

	//this ctor is used when importing items from files etc.
	//name is filename with extension.
	//sourcePath is used to get file text to detect type when !isFolder.
	public FileNode(FilesModel model, string name, string sourcePath, bool isFolder, string linkTarget = null)
	{
		_model = model;
		_type = isFolder ? EFileType.Folder : DetectFileType(sourcePath);
		_name = name;
		_id = _model.AddGetId(this);
		_CtorMisc(linkTarget);
	}

	//this ctor is used when copying or importing a workspace.
	//Deep-copies fields from f, except _model, _name, _id (generates new) and _testScriptId.
	FileNode(FilesModel model, FileNode f, string name)
	{
		_model = model;
		_name = name;
		_type = f._type;
		_state = f._state;
		_flags = f._flags;
		_iconOrLinkTarget = f._iconOrLinkTarget;
		_id = _model.AddGetId(this);
	}

	//this ctor is used when reading files.xml
	FileNode(XmlReader x, FileNode parent, FilesModel model)
	{
		_model = model;
		if(parent == null) { //the root node
			if(x.Name != "files") throw new ArgumentException("XML root element name must be 'files'");
			x["max-i"].ToInt(out uint u);
			_model.MaxId = u;
		} else {
			_type = XmlTagToFileType(x.Name, canThrow: true);
			uint id = 0, testScriptId = 0; string linkTarget = null, icon = null;
			while(x.MoveToNextAttribute()) {
				var v = x.Value;
				switch(x.Name) {
				case "n": _name = v; break;
				case "i": v.ToInt(out id); break;
				case "f": _flags = (_Flags)v.ToInt(); break;
				case "path": linkTarget = v; break;
				case "icon": icon = v; break;
				case "run": v.ToInt(out testScriptId); break;
				}
			}
			if(_name.NE()) throw new ArgumentException("no 'n' attribute in XML");
			_id = _model.AddGetId(this, id);
			_CtorMisc(linkTarget);
			if(icon != null && linkTarget == null) _iconOrLinkTarget = icon;
			if(testScriptId != 0) _testScriptId = testScriptId;
		}
	}

	public static FileNode Load(string file, FilesModel model) => XmlLoad(file, (x, p) => new FileNode(x, p, model));

	public void Save(string file) => XmlSave(file, (x, n) => n._XmlWrite(x, false));

	void _XmlWrite(XmlWriter x, bool exporting)
	{
		if(Parent == null) {
			x.WriteStartElement("files");
			if(_model != null) x.WriteAttributeString("max-i", _model.MaxId.ToString()); //null when exporting
		} else {
			string t = "n";
			switch(_type) {
			case EFileType.Folder: t = "d"; break;
			case EFileType.Script: t = "s"; break;
			case EFileType.Class: t = "c"; break;
			}
			x.WriteStartElement(t);
			x.WriteAttributeString("n", _name);
			if(!exporting) x.WriteAttributeString("i", _id.ToString());
			if(_flags != 0) x.WriteAttributeString("f", ((int)_flags).ToString());
			if(IsLink) x.WriteAttributeString("path", LinkTarget);
			var ico = CustomIcon; if(ico != null) x.WriteAttributeString("icon", ico);
			if(!exporting && _testScriptId != 0) x.WriteAttributeString("run", _testScriptId.ToString());
		}
	}

	public static void Export(FileNode[] a, string file) => new FileNode().XmlSave(file, (x, n) => n._XmlWrite(x, true), children: a);

	FileNode() { } //used by Export

	#endregion

	#region properties

	/// <summary>
	/// Gets workspace that contains this file.
	/// </summary>
	public FilesModel Model => _model;

	/// <summary>
	/// Gets the root node. It is <see cref="Model"/>.Root.
	/// </summary>
	public FileNode Root => _model.Root;

	/// <summary>
	/// Gets treeview control that displays this file.
	/// Returns null if this workspace is unloaded.
	/// </summary>
	public TreeViewAdv TreeControl => _model.TreeControl;

	/// <summary>
	/// File type.
	/// </summary>
	public EFileType FileType => _type;

	/// <summary>
	/// true if folder or root.
	/// </summary>
	public bool IsFolder => _type == EFileType.Folder;

	/// <summary>
	/// true if script file.
	/// </summary>
	public bool IsScript => _type == EFileType.Script;

	/// <summary>
	/// true if class file.
	/// </summary>
	public bool IsClass => _type == EFileType.Class;

	/// <summary>
	/// true if script or class file.
	/// </summary>
	public bool IsCodeFile => _type == EFileType.Script || _type == EFileType.Class;

	/// <summary>
	/// File name with extension.
	/// </summary>
	public string Name => _name;

	/// <summary>
	/// File name with or without extension.
	/// If ends with ".cs", returns without extension.
	/// </summary>
	public string DisplayName => _displayName ??= _name.RemoveSuffix(".cs", true);

	/// <summary>
	/// Unique id in this workspace. To find faster, with database, etc.
	/// Root id is 0.
	/// Ids of deleted items are not reused.
	/// </summary>
	public uint Id => _id;

	/// <summary>
	/// <see cref="Id"/> as string.
	/// </summary>
	public string IdString => _id.ToString();

	/// <summary>
	/// Formats string like "&lt;0x10000000A&gt;", with <see cref="Id"/> in low-order int and <see cref="FilesModel.WorkspaceSN"/> in high-order int.
	/// Such string can be passed to <see cref="FilesModel.Find"/>.
	/// </summary>
	public string IdStringWithWorkspace => "<0x" + (_id | ((long)_model.WorkspaceSN << 32)).ToString("X") + ">";

	/// <summary>
	/// Formats SciTags &lt;open&gt; link tag to open this file.
	/// </summary>
	public string SciLink => $"<open \"{IdStringWithWorkspace}\">{_name}<>";

	/// <summary>
	/// true if is external file, ie not in this workspace folder.
	/// </summary>
	public bool IsLink => 0 != (_state & _State.Link);

	/// <summary>
	/// If <see cref="IsLink"/>, returns target path, else null.
	/// </summary>
	public string LinkTarget => IsLink ? _iconOrLinkTarget : null;

	/// <summary>
	/// Gets or sets custom icon path or null. For links always returns null; use LinkTarget.
	/// The setter will save workspace.
	/// </summary>
	public string CustomIcon {
		get => IsLink ? null : _iconOrLinkTarget;
		set {
			Debug.Assert(!IsLink);
			_iconOrLinkTarget = value;
			_model.Save.WorkspaceLater();
			//FUTURE: call event to update other controls. It probably will be event of FilesModel.
		}
	}

	/// <summary>
	/// Gets or sets other item to run instead of this. None if null.
	/// The setter will save workspace.
	/// </summary>
	public FileNode TestScript {
		get {
			if(_testScriptId != 0) {
				var f = _model.FindById(_testScriptId); if(f != null) return f;
				TestScript = null;
			}
			return null;
		}
		set {
			uint id = value?._id ?? 0;
			if(_testScriptId == id) return;
			_testScriptId = id;
			_model.Save.WorkspaceLater();
		}
	}

	/// <summary>
	/// Gets or sets 'Delete' flag. Does nothing more.
	/// </summary>
	public bool IsDeleted {
		get => 0 != (_state & _State.Deleted);
		set { Debug.Assert(value); _state |= _State.Deleted; }
	}

	/// <summary>
	/// true if is deleted or is not in current workspace.
	/// </summary>
	public bool IsAlien => IsDeleted || _model != Program.Model;

	/// <summary>
	/// Returns item path in workspace, like @"\Folder\Name.cs" or @"\Name.cs".
	/// Returns null if this item is deleted.
	/// </summary>
	public string ItemPath => _ItemPath();

	string _ItemPath(string prefix = null)
	{
		var a = t_pathStack ??= new Stack<string>();
		a.Clear();
		for(FileNode f = this, root = Root; f != root; f = f.Parent) {
			if(f == null) { Debug.Assert(IsDeleted); return null; }
			a.Push(f._name);
		}
		using(new Au.Util.StringBuilder_(out var b)) {
			b.Append(prefix);
			while(a.Count > 0) b.Append('\\').Append(a.Pop());
			return b.ToString();
		}
	}
	[ThreadStatic] static Stack<string> t_pathStack;

	/// <summary>
	/// Gets full path of the file.
	/// If this is a link, it is the link target.
	/// </summary>
	public string FilePath {
		get {
			if(this == Root) return _model.FilesDirectory;
			if(IsDeleted) return null;
			if(IsLink) return LinkTarget;
			return _ItemPath(_model.FilesDirectory);
		}
	}

	/// <summary>
	/// Gets text from file or editor.
	/// Returns "" if file not found.
	/// </summary>
	/// <param name="saved">Always get text from file. If false (default), gets editor text if this is current file.</param>
	/// <param name="warningIfNotFound">Print warning if file not found. If false, prints only other exceptions.</param>
	/// <param name="cache">Cache text. Next time return that text. Not used if gets text from editor.</param>
	public string GetText(bool saved = false, bool warningIfNotFound = false, bool cache = false)
	{
		if(IsFolder) return "";
		if(!saved && this == _model.CurrentFile) {
			return Panels.Editor.ZActiveDoc.Text;
		}
		//if(cache) AOutput.Write("GetText", Name, _text != null);
		if(_text != null) return _text;
		string r = null, es = null, path = FilePath;
		try {
			using var sr = AFile.WaitIfLocked(() => new StreamReader(path, Encoding.UTF8));
			if(sr.BaseStream.Length > 100_000_000) es = "File too big, > 100_000_000.";
			else r = sr.ReadToEnd();
		}
		catch(Exception ex) {
			if(warningIfNotFound || !(ex is FileNotFoundException || ex is DirectoryNotFoundException)) es = ex.ToStringWithoutStack();
		}
		r ??= "";
		if(es != null) {
			AWarning.Write($"{es}\r\n\tFailed to get text of <open>{ItemPath}<>, file <explore>{path}<>", -1);
		} else if(cache && Model.IsWatchingFileChanges && !this.IsLink && r.Length < 1_000_000) { //don't cache links because we don't watch their file folders
			_text = r; //FUTURE: set = null after some time if not used
		}
		return r;
	}
	string _text;

	public void UnCacheText(bool fromWatcher = false)
	{
		//AOutput.Write("UnCacheText", Name, _text != null);
		_text = null;
		if(fromWatcher) Panels.Editor.ZGetOpenDocOf(this)?._FileModifiedExternally();
	}

	public Bitmap GetIcon(bool expandedFolder = false)
	{
		string k;
		if(IsDeleted) {
			k = "delete";
		} else {
			switch(_type) {
			case EFileType.Script: k = nameof(Au.Editor.Resources.Resources.fileScript); break;
			case EFileType.Class: k = nameof(Au.Editor.Resources.Resources.fileClass); break;
			case EFileType.Folder:
				//if(IsProjectFolder()) k = nameof(Au.Editor.Resources.Resources.project); else //rejected. Name starts with '@' character, it's visible without a different icon.
				k = expandedFolder ? nameof(Au.Editor.Resources.Resources.folderOpen) : nameof(Au.Editor.Resources.Resources.folder);
				break;
			default: //_Type.NotCodeFile
				return IconCache.GetImage(FilePath, useExt: true);
			}
		}
		return EdResources.GetImageUseCache(k);
	}

	public static AIconCache IconCache = new AIconCache(AFolders.ThisAppDataLocal + @"fileIconCache.xml", 16);

	///// <summary>
	///// Gets or sets 'has triggers' flag.
	///// The setter will save workspace.
	///// </summary>
	//public bool HasTriggers {
	//	get => 0 != (_flags & _Flags.HasTriggers);
	//	set {
	//		if(value != HasTriggers) {
	//			_flags.SetFlag(_Flags.HasTriggers, value);
	//			_model.Save.WorkspaceLater();
	//		}
	//	}
	//}

	/// <summary>
	/// Returns Name.
	/// </summary>
	public override string ToString() => _name;

	#endregion

	#region find

	/// <summary>
	/// Finds descendant file or folder by name or @"\relative path".
	/// Returns null if not found; also if name is null/"".
	/// </summary>
	/// <param name="name">Name like "name.cs" or relative path like @"\name.cs" or @"\subfolder\name.cs".</param>
	/// <param name="folder">true - folder, false - file, null - any (prefer file if not relative).</param>
	public FileNode FindDescendant(string name, bool? folder)
	{
		if(name.NE()) return null;
		if(name[0] == '\\') return _FindRelative(name, folder);
		return _FindIn(Descendants(), name, folder, true);
	}

	static FileNode _FindIn(IEnumerable<FileNode> e, string name, bool? folder, bool preferFile)
	{
		if(preferFile) {
			if(!folder.GetValueOrDefault()) { //any or file
				var f = _FindIn(e, name, false); if(f != null) return f;
			}
			if(!folder.HasValue || folder.GetValueOrDefault()) { //any or folder
				return _FindIn(e, name, true);
			}
		} else {
			if(folder.HasValue) return _FindIn(e, name, folder.GetValueOrDefault());
			foreach(var f in e) if(f._name.Eqi(name)) return f;
		}
		return null;
	}

	static FileNode _FindIn(IEnumerable<FileNode> e, string name, bool folder)
	{
		foreach(var f in e) if(f.IsFolder == folder && f._name.Eqi(name)) return f;
		return null;
	}

	FileNode _FindRelative(string name, bool? folder)
	{
		if(name.Starts(@"\\")) return null;
		var f = this; int lastSegEnd = -1;
		foreach(var v in name.Segments(@"\", SegFlags.NoEmpty)) {
			var e = f.Children();
			var s = name[v.start..v.end];
			if((lastSegEnd = v.end) == name.Length) {
				f = _FindIn(e, s, folder, false);
			} else {
				f = _FindIn(e, s, true);
			}
			if(f == null) return null;
		}
		if(lastSegEnd != name.Length) return null; //prevents finding when name is "" or @"\" or @"xxx\".
		return f;
	}

	/// <summary>
	/// Finds file or folder by name or path relative to: this folder, parent folder (if this is file) or root (if relativePath starts with @"\").
	/// Returns null if not found; also if name is null/"".
	/// </summary>
	/// <param name="relativePath">Examples: "name.cs", @"subfolder\name.cs", @".\subfolder\name.cs", @"..\parent\name.cs", @"\root path\name.cs".</param>
	/// <param name="folder">true - folder, false - file, null - any.</param>
	public FileNode FindRelative(string relativePath, bool? folder)
	{
		if(!IsFolder) return Parent.FindRelative(relativePath, folder);
		var s = relativePath;
		if(s.NE()) return null;
		FileNode p = this;
		if(s[0] == '\\') p = Root;
		else if(s[0] == '.') {
			int i = 0;
			for(; s.Eq(i, @"..\"); i += 3) { p = p.Parent; if(p == null) return null; }
			if(i == 0 && s.Starts(@".\")) i = 2;
			if(i != 0) {
				if(i == s.Length) return (p == Root || !(folder ?? true)) ? null : p;
				s = s.Substring(i);
			}
		}
		return p._FindRelative(s, folder);
	}

	/// <summary>
	/// Finds all descendant files (and not folders) that have the specified name.
	/// Returns empty array if not found.
	/// </summary>
	/// <param name="name">File name. If starts with backslash, works like <see cref="FindDescendant"/>.</param>
	public FileNode[] FindAllDescendantFiles(string name)
	{
		if(!name.NE()) {
			if(name[0] == '\\') {
				var f1 = _FindRelative(name, false);
				if(f1 != null) return new FileNode[] { f1 };
			} else {
				return Descendants().Where(k => !k.IsFolder && k._name.Eqi(name)).ToArray();
			}
		}
		return Array.Empty<FileNode>();
	}

	/// <summary>
	/// Finds ancestor (including self) project folder and its main file.
	/// If both found, sets folder and main and returns true. If some not found, sets folder=null, main=null, and returns false.
	/// If ofAnyScript, gets project even if this is a non-main script in project folder. 
	/// </summary>
	public bool FindProject(out FileNode folder, out FileNode main, bool ofAnyScript = false)
	{
		folder = main = null;
		for(FileNode r = Root, f = IsFolder ? this : Parent; f != r && f != null; f = f.Parent) {
			if(!f.IsProjectFolder(out main)) continue;
			if(main == null) break;
			if(this.IsScript && this != main && !ofAnyScript) { //non-main scripts are not part of project
				main = null;
				break;
			}
			folder = f;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Returns true if this is a folder and Name starts with '@' and contains main code file.
	/// </summary>
	/// <param name="main">Receives the main code file. It is the first direct child code file.</param>
	public bool IsProjectFolder(out FileNode main)
	{
		main = null;
		if(IsFolder && _name[0] == '@') {
			foreach(var f in Children()) {
				if(f.IsCodeFile) { main = f; return true; }
			}
		}
		return false;
	}

	public IEnumerable<FileNode> EnumProjectClassFiles(FileNode fSkip = null)
	{
		foreach(var f in Descendants()) {
			if(f._type == EFileType.Class && f != fSkip) yield return f;
		}
	}

	/// <summary>
	/// Gets class file role from metacomments.
	/// Note: can be slow, because loads file text if this is a class file.
	/// </summary>
	public EClassFileRole GetClassFileRole()
	{
		if(_type != EFileType.Class) return EClassFileRole.None;
		var code = GetText();
		int endOfMeta = MetaComments.FindMetaComments(code);
		if(endOfMeta == 0) return EClassFileRole.Class;
		foreach(var v in MetaComments.EnumOptions(code, endOfMeta)) {
			if(!v.NameIs("role")) continue;
			if(v.ValueIs("classLibrary")) return EClassFileRole.Library;
			if(v.ValueIs("classFile")) break;
			return EClassFileRole.App;
		}
		return EClassFileRole.Class;
	}

	public enum EClassFileRole
	{
		/// <summary>Not a class file.</summary>
		None,
		/// <summary>Has meta role miniProgram/exeProgram/editorExtension.</summary>
		App,
		/// <summary>Has meta role classLibrary.</summary>
		Library,
		/// <summary>Has meta role classFile, or no meta role.</summary>
		Class,
	}

	#endregion

	#region tree

	/// <summary>
	/// Gets control's object of this item.
	/// </summary>
	public TreeNodeAdv TreeNodeAdv {
		get {
			var c = TreeControl;
			if(this == Root) return c.Root;
			var tp = TreePath;
			if(tp == null) return null; //deleted node
			return c.FindNode(tp, true);

			//CONSIDER: cache in a field. But can be difficult to manage. Currently this func is not called frequently.
			//note: don't use c.FindNodeByTag. It does not find in never-expanded folders, unless c.LoadOnDemand is false. And slower.
		}
	}

	/// <summary>
	/// Creates TreePath used to communicate with the control.
	/// </summary>
	internal TreePath TreePath {
		get {
			var r = Root;
			if(this == r) return TreePath.Empty;
			var a = AncestorsReverse(true, true);
			if(a[0].Parent != r) { Debug.Assert(IsDeleted); return null; }
			return new TreePath(a);
		}
	}

	/// <summary>
	/// Unselects all and selects this. Does not open document.
	/// If this is root, just unselects all.
	/// </summary>
	public void SelectSingle()
	{
		var c = TreeControl;
		if(this == Root) c.ClearSelection();
		else if(!IsAlien) c.SelectedNode = TreeNodeAdv;
	}

	public bool IsSelected {
		get => TreeNodeAdv?.IsSelected ?? false; //shoulddo: test: maybe faster _model.SelectedItems.Contains(this);
		set => TreeNodeAdv.IsSelected = value;
	}

	/// <summary>
	/// Call this to update/redraw control row view when changed node data (text, image, checked, color, etc) and don't need to change row height.
	/// </summary>
	public void UpdateControlRow() => TreeControl.UpdateNode(TreeNodeAdv);

	/// <summary>
	/// Call this to update/redraw control view when changed node data (text, image, etc) and need to change row height.
	/// </summary>
	public void UpdateControlRowHeight() => _model.OnNodeChanged(this);

	#endregion

	#region new item

	public static string CreateNameUniqueInFolder(FileNode folder, string fromName, bool forFolder)
	{
		if(!_Exists(fromName)) return fromName;

		string ext = null;
		if(!forFolder) {
			int i = fromName.LastIndexOf('.');
			if(i >= 0) { ext = fromName.Substring(i); fromName = fromName.Remove(i); }
		}
		fromName = fromName.RegexReplace(@"\d+$", "");
		for(int i = 2; ; i++) {
			var s = fromName + i + ext;
			if(!_Exists(s)) return s;
		}

		bool _Exists(string s)
		{
			if(null != _FindIn(folder.Children(), s, null, false)) return true;
			if(AFile.ExistsAsAny(folder.FilePath + "\\" + s)) return true; //orphaned file?
			return false;
		}
	}

	public static class Templates
	{
		public static readonly string DefaultDirBS = AFolders.ThisAppBS + @"Templates\files\";
		public static readonly string UserDirBS = ProgramSettings.DirBS + @"Templates\";

		public static string FileName(ETempl templ) => templ switch { ETempl.Class => "Class.cs", ETempl.Partial => "Partial.cs", _ => "Script.cs" };

		public static string FilePathRaw(ETempl templ, bool user) => (user ? UserDirBS : DefaultDirBS) + FileName(templ);

		public static string FilePathReal(ETempl templ, bool? user = null)
		{
			bool u = user ?? Program.Settings.templ_use.Has(templ);
			var file = FilePathRaw(templ, u);
			if(u && !AFile.ExistsAsFile(file, true)) file = FilePathRaw(templ, false);
			return file;
		}

		public static string Load(ETempl templ, bool? user = null)
		{
			return AFile.LoadText(FilePathReal(templ, user));
		}

		public static bool IsStandardTemplateName(string template, out ETempl result, bool ends = false)
		{
			int i = ends ? template.Ends(false, s_names) : template.Eq(false, s_names);
			if(i-- == 0) { result = 0; return false; }
			result = (ETempl)(1 << i);
			return true;
		}

		static string[] s_names = { "Script.cs", "Class.cs", "Partial.cs" };

		/// <summary>
		/// Loads Templates\files.xml and optionally finds a template in it.
		/// Returns null if template not found. Exception if fails to load file.
		/// Uses caching to avoid loading file each time, but reloads if file modified; don't modify the XML DOM.
		/// </summary>
		/// <param name="template">null or relative path of template in Templates\files. Case-sensitive.</param>
		public static (XElement x, bool cached) LoadXml(string template = null)
		{
			//load files.xml first time, or reload if file modified
			AFile.GetProperties(s_xmlFilePath, out var fp, FAFlags.UseRawPath);
			bool cached = s_xml != null && fp.LastWriteTimeUtc == s_xmlFileTime;
			if(!cached) {
				s_xml = AExtXml.LoadElem(s_xmlFilePath);
				s_xmlFileTime = fp.LastWriteTimeUtc;
			}

			var x = s_xml;
			if(template != null) {
				var a = template.Split('\\');
				for(int i = 0; i < a.Length; i++) x = x?.Elem(i < a.Length - 1 ? "d" : null, "n", a[i]);
				Debug.Assert(x != null);
			}
			return (x, cached);
		}
		static XElement s_xml;
		static readonly string s_xmlFilePath = AFolders.ThisAppBS + @"Templates\files.xml";
		static DateTime s_xmlFileTime;

		public static bool IsInExamples(XElement x) => x.Ancestors().Any(o => o.Attr("n") == "Examples");
	}

	[Flags]
	public enum ETempl { Script = 1, Class = 2, Partial = 4 }

	#endregion

	#region rename, move, copy

	/// <summary>
	/// Changes Name of this object and renames its file (if not link).
	/// Returns false if name is empty or fails to rename its file.
	/// </summary>
	/// <param name="name">
	/// Name, like "New name.cs" or "New name".
	/// If not folder, adds previous extension if no extension or changed code file extension.
	/// If invalid filename, replaces invalid characters etc.
	/// </param>
	/// <param name="userEdited">true if called from the control edit notification.</param>
	public bool FileRename(string name, bool userEdited = false)
	{
		name = APath.CorrectName(name);
		if(!IsFolder) {
			var ext = APath.GetExtension(_name);
			if(ext.Length > 0) if(name.IndexOf('.') < 0 || (IsCodeFile && !name.Ends(ext, true))) name += ext;
		}
		if(name == _name) return true;

		if(!IsLink) {
			if(!_model.TryFileOperation(() => AFile.Rename(this.FilePath, name, FIfExists.Fail))) return false;
		}

		_name = name;
		_displayName = null;
		_model.Save.WorkspaceLater();
		if(!userEdited) UpdateControlRow();
		if(this == _model.CurrentFile) Program.MainForm.ZSetTitle();
		CodeInfo.FilesChanged();
		return true;
	}

	/// <summary>
	/// Returns true if can move the tree node into the specified position.
	/// For example, cannot move parent into child etc.
	/// Does not check whether can move the file.
	/// </summary>
	public bool CanMove(FileNode target, FNPosition pos)
	{
		//cannot move into self or descendants
		if(target == this || target.IsDescendantOf(this)) return false;

		//cannot move into a non-folder or before/after self
		switch(pos) {
		case FNPosition.Inside:
			if(!target.IsFolder) return false;
			break;
		case FNPosition.Before:
			if(Next == target) return false;
			break;
		case FNPosition.After:
			if(Previous == target) return false;
			break;
		}
		return true;
	}

	/// <summary>
	/// Moves this into, before or after target.
	/// Also moves file if need.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="pos"></param>
	public bool FileMove(FileNode target, FNPosition pos)
	{
		if(!CanMove(target, pos)) return false;

		//move file or directory
		if(!IsLink) {
			var oldParent = Parent;
			var newParent = (pos == FNPosition.Inside) ? target : target.Parent;
			if(newParent != oldParent) {
				if(!_model.TryFileOperation(() => AFile.Move(this.FilePath, newParent.FilePath + "\\" + _name, FIfExists.Fail))) return false;
			}
		}

		//move tree node
		_model.OnNodeRemoved(this);
		Remove();
		Common_MoveCopyNew(target, pos);
		return true;
	}

	public void Common_MoveCopyNew(FileNode target, FNPosition pos)
	{
		target.AddChildOrSibling(this, pos, true);
		CodeInfo.FilesChanged();
	}

	/// <summary>
	/// Adds f to the tree, updates control, optionally sets to save workspace.
	/// </summary>
	public void AddChildOrSibling(FileNode f, FNPosition inBeforeAfter, bool setSaveWorkspace)
	{
		if(inBeforeAfter == FNPosition.Inside) AddChild(f); else AddSibling(f, inBeforeAfter == FNPosition.After);
		_model.OnNodeInserted(f);
		if(setSaveWorkspace) _model.Save.WorkspaceLater();
	}

	/// <summary>
	/// Copies this into, before or after target.
	/// Also copies file if need.
	/// Returns the copy, or null if fails.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="pos"></param>
	/// <param name="newModel">Used when importing workspace.</param>
	internal FileNode FileCopy(FileNode target, FNPosition pos, FilesModel newModel = null)
	{
		_model.Save?.TextNowIfNeed(true);

		//create unique name
		var newParent = (pos == FNPosition.Inside) ? target : target.Parent;
		string name = CreateNameUniqueInFolder(newParent, _name, IsFolder);

		//copy file or directory
		if(!IsLink) {
			if(!_model.TryFileOperation(() => AFile.Copy(FilePath, newParent.FilePath + "\\" + name, FIfExists.Fail))) return null;
		}

		//create new FileNode with descendants
		var model = newModel ?? _model;
		var f = new FileNode(model, this, name);
		_CopyChildren(this, f);

		void _CopyChildren(FileNode from, FileNode to)
		{
			if(!from.IsFolder) return;
			foreach(var v in from.Children()) {
				var t = new FileNode(model, v, v._name);
				to.AddChild(t);
				_CopyChildren(v, t);
			}
		}

		//insert at the specified place and set to save
		f.Common_MoveCopyNew(target, pos);
		return f;
	}

	#endregion

	#region util

	/// <summary>
	/// Gets file type from XML tag which should be "d", "s", "c" or "n".
	/// If none, throws ArgumentException if canThrow, else returns EFileType.NotCodeFile.
	/// </summary>
	public static EFileType XmlTagToFileType(string tag, bool canThrow) => tag switch
	{
		"d" => EFileType.Folder,
		"s" => EFileType.Script,
		"c" => EFileType.Class,
		"n" => EFileType.NotCodeFile,
		_ => !canThrow ? EFileType.NotCodeFile : throw new ArgumentException("XML element name must be 'd', 's', 'c' or 'n'")
	};

	/// <summary>
	/// Detects file type from extension or text.
	/// If .cs, uses text.
	/// Must be not folder.
	/// </summary>
	public static EFileType DetectFileType(string path)
	{
		var type = EFileType.NotCodeFile;
		if(path.Ends(".cs", true)) {
			type = EFileType.Class;
			try { if(AFile.LoadText(path).RegexIsMatch(@"\bclass Script\s*:\s*AScript\b")) type = EFileType.Script; }
			catch(Exception ex) { ADebug.Print(ex); }
		}
		return type;
	}

	#endregion
}

/// <summary>
/// File type of a <see cref="FileNode"/>.
/// Saved in XML as tag name: d folder, s script, c class, n other.
/// </summary>
enum EFileType : byte
{
	Folder, //must be 0
	Script,
	Class,
	NotCodeFile,
}
