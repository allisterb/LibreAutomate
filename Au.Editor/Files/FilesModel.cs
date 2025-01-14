//CONSIDER: tool to merge workspaces.
//	Eg show window with differences (added, deleted and modified items). Then user can copy each file in whatever direction.
//	Could be useful to sync portable.

using System.Xml.Linq;
using System.IO.Compression;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls;

partial class FilesModel {
	public readonly FileNode Root;
	public readonly int WorkspaceSN; //sequence number of workspace opened in this process: 1, 2...
	static int s_workspaceSN;
	public readonly string WorkspaceName;
	public readonly string WorkspaceDirectory;
	public readonly string WorkspaceFile; //.xml file containing the tree of .cs etc files
	public readonly string FilesDirectory; //.cs etc files
	public readonly string TempDirectory; //any temporary files
	public readonly string NugetDirectory; //NuGet libraries
	public readonly string NugetDirectoryBS; //NugetDirectory\
	public readonly string DllDirectory; //user-created or downloaded libraries
	public readonly string DllDirectoryBS; //LibrariesDirectory\
	public readonly AutoSave Save;
	readonly Dictionary<uint, FileNode> _idMap;
	internal readonly Dictionary<string, object> _nameMap;
	readonly string _dbFile;
	public readonly sqlite DB;
	public readonly string SettingsFile;
	public readonly WorkspaceSettings WSSett;
	readonly bool _importing;
	readonly bool _initedFully;
	internal object CompilerContext;
	internal IDisposable UndoContext;
	
	/// <param name="file">Path of workspace tree file (files.xml).</param>
	/// <exception cref="ArgumentException">Invalid or not full path.</exception>
	/// <exception cref="Exception">XElement.Load exceptions. And possibly more.</exception>
	public FilesModel(string file, bool importing) {
		_importing = importing;
		WorkspaceFile = pathname.normalize(file);
		WorkspaceDirectory = pathname.getDirectory(WorkspaceFile);
		WorkspaceName = pathname.getName(WorkspaceDirectory);
		FilesDirectory = WorkspaceDirectory + @"\files";
		//CacheDirectory = WorkspaceDirectory + @"\.cache";
		TempDirectory = WorkspaceDirectory + @"\.temp";
		NugetDirectory = WorkspaceDirectory + @"\.nuget";
		NugetDirectoryBS = NugetDirectory + @"\";
		DllDirectory = WorkspaceDirectory + @"\dll";
		DllDirectoryBS = DllDirectory + @"\";
		//ExeDirectory = WorkspaceDirectory + @"\exe";
		if (!_importing) {
			WorkspaceSN = ++s_workspaceSN;
			filesystem.createDirectory(FilesDirectory);
			Save = new AutoSave(this);
		}
		_idMap = new();
		_nameMap = new(StringComparer.OrdinalIgnoreCase);
		
		Root = FileNode.LoadWorkspace(WorkspaceFile, this); //recursively creates whole model tree; caller handles exceptions
		
		if (!_importing) {
			WSSett = WorkspaceSettings.Load(SettingsFile = WorkspaceDirectory + @"\settings.json");
			
			_dbFile = WorkspaceDirectory + @"\state.db";
			try {
				DB = new sqlite(_dbFile, sql:
					//"PRAGMA journal_mode=WAL;" + //no, it does more bad than good
					"CREATE TABLE IF NOT EXISTS _misc (key TEXT PRIMARY KEY, data TEXT);" +
					"CREATE TABLE IF NOT EXISTS _editor (id INTEGER PRIMARY KEY, top INTEGER, pos INTEGER, lines BLOB);"
					);
			}
			catch (Exception ex) {
				print.it($"Failed to open file '{_dbFile}'. Will not load/save workspace state: lists of open files, expanded folders, markers, folding, etc.\r\n\t{ex.ToStringWithoutStack()}");
			}
			
			folders.Workspace = new FolderPath(WorkspaceDirectory);
			Environment.SetEnvironmentVariable("dll", DllDirectory);
		}
		_initedFully = true;
	}
	
	public void Dispose() {
		if (_importing) return;
		if (_initedFully) {
			App.Tasks.OnWorkspaceClosed();
			RecentTT.Clear();
			//Save.AllNowIfNeed(); //owner FilesPanel calls this before calling this func. Because may need more code in between.
		}
		Save?.Dispose();
		DB?.Dispose();
		UndoContext?.Dispose();
		WSSett?.Dispose();
		EditGoBack.DisableUI();
	}
	
	#region tree control
	
	public static FilesView TreeControl => Panels.Files?.TreeControl;
	
	/// <summary>
	/// Updates control when changed number or order of visible items (added, removed, moved, etc).
	/// </summary>
	public void UpdateControlItems() { TreeControl.SetItems(Root.Children(), true); }
	
	/// <summary>
	/// When need to redraw an item in controls that display it.
	/// If the parameter is null, redraw all items.
	/// </summary>
	public static event Action<RedrawEventData> NeedRedraw;
	
	public record class RedrawEventData(FileNode f, bool remeasure, bool renamed);
	
	/// <summary>
	/// Raises <see cref="NeedRedraw"/> event.
	/// </summary>
	/// <param name="f"></param>
	public static void Redraw(FileNode f = null, bool remeasure = false, bool renamed = false) {
		NeedRedraw?.Invoke(new(f, remeasure, renamed));
	}
	
	#endregion
	
	#region load workspace
	
	public static void LoadWorkspace(string wsDir = null) {
		wsDir ??= App.Settings.workspace;
		if (wsDir.NE()) wsDir = folders.ThisAppDocuments + "Main"; else wsDir = pathname.normalize(wsDir);
		
		var xmlFile = wsDir + @"\files.xml";
		var oldModel = App.Model;
		FilesModel m = null;
		g1:
		try {
			//SHOULDDO: if editor runs as admin, the workspace directory should be write-protected from non-admin processes.
			
			if (s_isNewWorkspace = !filesystem.exists(xmlFile).File) {
				filesystem.copy(folders.ThisAppBS + @"Default\Workspace", wsDir);
			}
			
			oldModel?.UnloadingWorkspace_(); //saves all, closes documents, sets current file = null
			
			m = new FilesModel(xmlFile, importing: false);
		}
		catch (Exception ex) {
			m?.Dispose();
			m = null;
			//print.it($"Failed to load '{wsDir}'. {ex.Message}");
			switch (dialog.showError("Failed to load workspace", wsDir,
				"1 Retry|2 Load another|3 Create new|0 Cancel",
				owner: TreeControl, expandedText: ex.ToString())) {
			case 1: goto g1;
			case 2: OpenWorkspaceUI(); break;
			case 3: NewWorkspaceUI(); break;
			}
			if (App.Model == null) Environment.Exit(1);
			return;
		}
		
		oldModel?.Dispose();
		App.Model = m;
		
		//this code is important for portable
		filesystem.more.getFinalPath(wsDir, out wsDir);
		filesystem.more.getFinalPath(folders.ThisAppDocuments, out var tad);
		var unexpanded = folders.unexpandPath(wsDir, (tad, "%folders.ThisAppDocuments%"));
		
		if (unexpanded != App.Settings.workspace) {
			if (App.Settings.workspace != null) {
				var ar = App.Settings.recentWS ?? Array.Empty<string>();
				int i = Array.IndexOf(ar, unexpanded);
				if (i < 0) i = Array.IndexOf(ar, wsDir);
				if (i >= 0) ar = ar.RemoveAt(i);
				App.Settings.recentWS = ar.InsertAt(0, App.Settings.workspace);
			}
			App.Settings.workspace = unexpanded;
		}
		
		if (App.Loaded == AppState.LoadedUI) m.WorkspaceLoadedWithUI(onUiLoaded: false);
	}
	
	[MethodImpl(MethodImplOptions.NoInlining)] //avoid loading WPF at startup before loading UI
	public void WorkspaceLoadedWithUI(bool onUiLoaded) {
		if (!s_isNewWorkspace) LoadState(expandFolders: true);
		TreeControl.SetItems();
		if (s_isNewWorkspace) {
			s_isNewWorkspace = false;
			AddMissingDefaultFiles(true, true);
			SetCurrentFile(Root.FirstChild, newFile: true);
		} else {
			LoadState(openFiles: true);
		}
		ThisWorkspaceLoadedAndDocumentsOpened?.Invoke();
		AnyWorkspaceLoadedAndDocumentsOpened?.Invoke();
		if (!onUiLoaded) RunStartupScripts(true);
	}
	
	static bool s_isNewWorkspace;
	internal bool NoGlobalCs_; //used by MetaComments.Parse
	
	public event Action ThisWorkspaceLoadedAndDocumentsOpened;
	public static event Action AnyWorkspaceLoadedAndDocumentsOpened;
	
	/// <summary>
	/// Shows "Open workspace" dialog. On OK loads the selected workspace.
	/// </summary>
	public static void OpenWorkspaceUI() {
		var d = new FileOpenSaveDialog("{4D1F3AFB-DA1A-45AC-8C12-41DDA5C51CDA}") { Title = "Open workspace" };
		if (!d.ShowOpen(out string s, App.Hmain, selectFolder: true)) return;
		if (!filesystem.exists(s + @"\files.xml").File) dialog.showError(null, "The folder must contain file files.xml", owner: TreeControl);
		else LoadWorkspace(s);
	}
	
	/// <summary>
	/// Shows dialog to create new workspace. On OK creates and loads new workspace.
	/// </summary>
	public static void NewWorkspaceUI() {
		var path = GetDirectoryPathForNewWorkspace();
		if (path != null) LoadWorkspace(path);
	}
	
	#endregion
	
	#region find, id
	
	/// <summary>
	/// Finds file or folder by name or @"\relative path" or id or full path.
	/// </summary>
	/// <param name="name">
	/// Can be:
	/// Name, like "name.cs" or just "name".
	/// Relative path like @"\name.cs" or @"\subfolder\name.cs".
	/// Full path in this workspace or of a linked external file.
	/// &lt;id&gt; - enclosed <see cref="FileNode.IdString"/>, or <see cref="FileNode.IdStringWithWorkspace"/>.
	/// 
	/// Case-insensitive. If enclosed in &lt;&gt;, can be followed by any text.
	/// If just "name" and not found and name does not end with ".cs", tries to find name + ".cs".
	/// </param>
	/// <param name="kind">Ignored if <i>name</i> is id.</param>
	/// <param name="silent">Don't print warning "Found multiple...".</param>
	public FileNode Find(string name, FNFind kind = FNFind.Any, bool silent = false) {
		FoundMultiple = null;
		if (name.NE()) return null;
		if (name[0] == '<') {
			name.ToInt(out long id, 1);
			return FindById(id);
		}
		if (pathname.isFullPath(name)) return FindByFilePath(name, kind);
		if (name[0] == '\\') return Root.FindDescendant(name, kind);
		return _FindByName(name, kind);
		
		FileNode _FindByName(string name, FNFind kind) {
			if (_nameMap.MultiGet_(name, out FileNode v, out var a)) {
				if (v != null) return KindFilter_(v, kind);
				FileNode first = null;
				foreach (var f in a) {
					if (KindFilter_(f, kind) == null) continue;
					if (first == null) first = f; else (FoundMultiple ??= new() { first }).Add(f);
				}
				if (FoundMultiple == null) return first; //note: don't return the first if found multiple. Unsafe.
				if (!silent) {
					var paths = string.Join(", ", FoundMultiple.Select(o => o.SciLink(path: true)));
					print.warning($"Found multiple '{name}'. Use path if possible, or rename.\r\n\tPaths: {paths}."/*, -1*/);
				}
				return null;
			}
			if (kind != FNFind.Folder && !name.Ends(".cs", true)) return _FindByName(name + ".cs", kind);
			return null;
		}
	}
	
	/// <summary>
	/// When <see cref="Find"/> returns null because exists multiple items with the specified name/kind, this property contains them. Else null.
	/// </summary>
	public List<FileNode> FoundMultiple;
	
	internal static FileNode KindFilter_(FileNode f, FNFind kind) => kind switch {
		FNFind.File => !f.IsFolder ? f : null,
		FNFind.Folder => f.IsFolder ? f : null,
		FNFind.CodeFile => f.IsCodeFile ? f : null,
		FNFind.Class => f.IsClass ? f : null,
		//FNFind.Script => f.IsScript ? f : null,
		_ => f,
	};
	
	/// <summary>
	/// Calls <see cref="Find(string, FNFind, bool)"/>(name, FNFind.CodeFile).
	/// </summary>
	public FileNode FindCodeFile(string name, bool silent = false) => Find(name, FNFind.CodeFile, silent);
	
	/// <summary>
	/// Finds file or folder by its file path (<see cref="FileNode.FilePath"/>).
	/// </summary>
	/// <param name="path">Full path of a file in this workspace or of a linked external file.</param>
	/// <param name="kind"></param>
	public FileNode FindByFilePath(string path, FNFind kind = FNFind.Any) {
		var d = FilesDirectory;
		if (path.Starts(d, true) && path.Eq(d.Length, '\\')) return Root.FindDescendant(path[d.Length..], kind); //is in workspace folder
		if (kind != FNFind.Folder) foreach (var f in Root.Descendants()) if (f.IsLink && path.Eqi(f.LinkTarget)) return KindFilter_(f, kind); //find link
		return null;
	}
	
	///// <summary>
	///// If path starts with <see cref="FilesDirectory"/> and '\\', removes the FilesDirectory part and returns true.
	///// </summary>
	///// <param name="path">Full or relative path or name.</param>
	//bool _FullPathToRelative(ref string path) {
	//	var d = FilesDirectory;
	//	bool full = path.Starts(d, true) && path.Eq(d.Length, '\\');
	//	if (full) path = path[d.Length..];
	//	return full;
	//}
	
	/// <summary>
	/// Adds id/f to the dictionary that is used by <see cref="FindById"/> etc.
	/// If id is 0 or duplicate, generates new.
	/// Returns <i>id</i> or the generated id.
	/// </summary>
	public uint AddGetId(FileNode f, uint id = 0) {
		g1:
		if (id == 0) {
			//Normally we don't reuse ids of deleted items.
			//	Would be problems with something that we cannot/fail/forget to delete when deleting items.
			//	We save MaxId in XML: <files max-i="MaxId">.
			id = ++MaxId;
			if (id == 0) { //if new item created every 8 s, we have 1000 years, but anyway
				for (uint u = 1; u < uint.MaxValue; u++) if (!_idMap.ContainsKey(u)) { MaxId = u - 1; break; } //fast
				goto g1;
			} else if (_idMap.ContainsKey(id)) { //damaged XML file, or maybe a bug?
				Debug_.Print("id already exists:" + id);
				MaxId = _idMap.Keys.Max();
				id = 0;
				goto g1;
			}
			Save?.WorkspaceLater(); //null when importing this workspace
		}
		try { _idMap.Add(id, f); }
		catch (ArgumentException) {
			print.warning($"Duplicate id of '{f.Name}'. Creating new.");
			id = 0;
			goto g1;
		}
		return id;
	}
	
	/// <summary>
	/// Current largest id, used to generate new id.
	/// The root FileNode's ctor reads it from XML attribute 'max-i' and sets this property.
	/// </summary>
	public uint MaxId { get; set; }
	
	/// <summary>
	/// Finds file or folder by its <see cref="FileNode.Id"/>.
	/// Returns null if id is 0 or not found.
	/// id can contain <see cref="WorkspaceSN"/> in high-order int.
	/// </summary>
	public FileNode FindById(long id) {
		int idc = (int)(id >> 32); if (idc != 0 && idc != WorkspaceSN) return null;
		uint idf = (uint)id;
		if (idf == 0) return null;
		if (_idMap.TryGetValue(idf, out var f)) {
			Debug_.PrintIf(f == null, "deleted: " + idf);
			return f;
		}
		Debug_.Print("id not found: " + idf);
		return null;
	}
	
	/// <summary>
	/// Finds file or folder by its <see cref="FileNode.IdString"/>.
	/// Note: it must not be as returned by <see cref="FileNode.IdStringWithWorkspace"/>.
	/// </summary>
	public FileNode FindById(string id) {
		id.ToInt(out long n);
		return FindById(n);
	}
	
	/// <summary>
	/// Finds all files (and not folders) with the specified name.
	/// Returns empty array if not found.
	/// </summary>
	/// <param name="name">File name, like "name.cs". If starts with backslash, works like <see cref="Find"/>. Does not support <see cref="FileNode.IdStringWithWorkspace"/> string and filename without extension.</param>
	public FileNode[] FindAllFiles(string name) {
		return Root.FindAllDescendantFiles(name);
	}
	
	#endregion
	
	#region open/close, select, current, selected, context menu
	
	/// <summary>
	/// Returns true if f is null or isn't in this workspace or is deleted.
	/// </summary>
	public bool IsAlien(FileNode f) => f?.Model != this || f.IsDeleted;
	
	/// <summary>
	/// Closes f if open.
	/// Saves text if need, removes from OpenItems, deselects in treeview.
	/// </summary>
	/// <param name="f">Can be any item or null. Does nothing if it is null, folder or not open.</param>
	/// <param name="activateOther">When closing current file, if there are more open files, activate another open file.</param>
	/// <param name="selectOther">Select the activated file.</param>
	public bool CloseFile(FileNode f, bool activateOther = true, bool selectOther = false) {
		if (IsAlien(f)) return false;
		if (!_openFiles.Remove(f)) return false;
		
		bool isCurrent = f == _currentFile;
		bool setFocus = isCurrent && activateOther && f.OpenDoc.IsFocused;
		
		Panels.Editor.Close(f);
		f.IsSelected = false;
		
		if (isCurrent) {
			if (activateOther) {
				if (_openFiles.Count > 0) {
					var ff = _openFiles[0];
					if (selectOther) ff.SelectSingle();
					if (_SetCurrentFile(ff, focusEditor: setFocus)) return true;
				} else if (setFocus) {
					App.Wmain.Focus(); //else would not work any hotkeys and even Alt+menu keys until something focused
				}
			}
			_currentFile = null;
		}
		f.UpdateControlRow();
		
		_UpdateOpenFiles(_currentFile);
		Save.StateLater();
		
		return true;
	}
	
	/// <summary>
	/// Closes specified files that are open.
	/// </summary>
	/// <param name="files">Any <b>IEnumerable</b> except <b>OpenFiles</b>.</param>
	/// <param name="dontClose">null or <b>FileNode</b> or <b>BitArray</b> to not close.</param>
	public void CloseFiles(IEnumerable<FileNode> files, object dontClose = null) {
		if (files == _openFiles) files = _openFiles.ToArray();
		bool closeCurrent = false;
		int i = 0;
		foreach (var f in files) {
			if (dontClose is System.Collections.BitArray ba) {
				if (i < ba.Length && ba[i++]) continue;
			} else if (f == dontClose) continue;
			if (f == _currentFile) closeCurrent = true; else CloseFile(f, activateOther: false);
		}
		if (closeCurrent) CloseFile(_currentFile);
	}
	
	/// <summary>
	/// Updates PanelOpen, enables/disables Previous command.
	/// </summary>
	void _UpdateOpenFiles(FileNode current) {
		Panels.Open.UpdateList();
		App.Commands[nameof(Menus.File.OpenCloseGo.Previous_document)].Enabled = _openFiles.Count > 1;
	}
	
	/// <summary>
	/// Called by <see cref="LoadWorkspace"/> before opening another workspace and disposing this.
	/// Saves all, raises <see cref="UnloadingThisWorkspace"/> event, closes documents, sets _currentFile = null.
	/// </summary>
	internal void UnloadingWorkspace_() {
		Save.AllNowIfNeed();
		EditorExtension.ClosingWorkspace_(onExit: false);
		UnloadingThisWorkspace?.Invoke(); //closes dialogs that contain workspace-specific data, eg Properties
		UnloadingAnyWorkspace?.Invoke();
		_currentFile = null;
		Panels.Editor.CloseAll(saveTextIfNeed: false);
		_openFiles.Clear();
		_UpdateOpenFiles(null);
	}
	
	/// <summary>
	/// Note: unsubscribe to avoid memory leaks.
	/// </summary>
	public event Action UnloadingThisWorkspace;
	public static event Action UnloadingAnyWorkspace;
	
	//rejected. Let unsubscribe in OnClosed: App.Model.UnloadingWorkspaceEvent -= Close;
	///// <summary>
	///// Closes window <i>w</i> when unloading workspace.
	///// </summary>
	//public void UnloadingWorkspaceCloseWindow(Window w) {
	//	Action aClose = w.Close;
	//	UnloadingWorkspaceEvent += aClose;
	//	EventHandler closed = null;
	//	closed = (_, _) => { UnloadingWorkspaceEvent -= aClose; w.Closed -= closed; };
	//	w.Closed += closed;
	//}
	
	/// <summary>
	/// Files that are displayed in the Open panel.
	/// Some of them are open in editor (see <see cref="PanelEdit.OpenDocs"/>, <see cref="FileNode.OpenDoc"/>), others just were open when closing this workspace the last time.
	/// </summary>
	public IReadOnlyList<FileNode> OpenFiles => _openFiles;
	List<FileNode> _openFiles = new();
	
	/// <summary>
	/// Gets the current file. It is open/active in the code editor.
	/// </summary>
	public FileNode CurrentFile => _currentFile;
	FileNode _currentFile;
	
	/// <summary>
	/// Selects the node. If not folder, opens its file in the code editor.
	/// Returns false if failed to open, for example if <i>f</i> is a folder.
	/// </summary>
	public bool SetCurrentFile(FileNode f, bool dontChangeTreeSelection = false, bool newFile = false, bool? focusEditor = true, bool noTemplate = false) {
		if (IsAlien(f)) return false;
		if (!dontChangeTreeSelection) f.SelectSingle();
		if (_currentFile != f) _SetCurrentFile(f, newFile, focusEditor, noTemplate);
		return _currentFile == f;
	}
	
	/// <summary>
	/// If f!=_currentFile and not folder:
	/// - Opens it in editor, adds to OpenFiles, sets _currentFile, saves state later, updates UI.
	/// - Saves and hides current document.
	/// Returns false if fails to read file or if f is folder.
	/// </summary>
	/// <param name="f"></param>
	/// <param name="newFile">Should be true if opening the file first time after creating.</param>
	/// <param name="focusEditor">If null, focus later, when mouse enters editor. Ignored if editor was focused (sets focus). Also depends on <i>newFile</i>.</param>
	bool _SetCurrentFile(FileNode f, bool newFile = false, bool? focusEditor = true, bool noTemplate = false) {
		Debug.Assert(!IsAlien(f));
		if (f == _currentFile) return true;
		//print.it(f);
		if (f.IsFolder) return false;
		
		CodeInfo.WaitUntilReadyForStyling();
		
		if (_currentFile != null) Save.TextNowIfNeed();
		
		var fPrev = _currentFile;
		_currentFile = f;
		
		if (!Panels.Editor.Open(f, newFile, focusEditor, noTemplate)) {
			_currentFile = fPrev;
			if (_openFiles.Contains(f)) _UpdateOpenFiles(_currentFile); //?
			return false;
		}
		
		fPrev?.UpdateControlRow();
		_currentFile?.UpdateControlRow();
		
		_openFiles.Remove(f);
		_openFiles.Insert(0, f);
		_UpdateOpenFiles(f);
		Save.StateLater();
		
		return true;
	}
	
	void _ItemRightClicked(FileNode f) { //Dispatcher.InvokeAsync
		if (IsAlien(f)) return;
		if (!f.IsSelected) f.SelectSingle();
		_ContextMenu();
	}
	static ContextMenu s_contextMenu;
	bool s_inContextMenu;
	
	void _ContextMenu() {
		if (s_inContextMenu) return; //workaround for: sometimes, when dying mouse generates >1 rclick, somehow the menu is at screen 0 0
		if (s_contextMenu == null) {
			var m = new ContextMenu { PlacementTarget = TreeControl };
			//m.ItemsSource = App.Commands[nameof(Menus.File)].MenuItem.Items; //shows menu but then closes on mouse over
			App.Commands[nameof(Menus.File)].CopyToMenu(m);
			m.Closed += (_, _) => s_inContextMenu = false;
			s_contextMenu = m;
		}
		s_contextMenu.IsOpen = true;
		s_inContextMenu = true;
	}
	
	//Called when editor control focused, etc.
	public void EnsureCurrentSelected() {
		//if(_currentFile != null && !_currentFile.IsSelected && _control.SelectedIndices.Count < 2) _currentFile.SelectSingle();
		if (_currentFile != null && !_currentFile.IsSelected) _currentFile.SelectSingle();
	}
	
	/// <summary>
	/// Selects the node, opens its file in the code editor, optionally goes to the specified position or line or line/column.
	/// Returns false if failed to open, for example if it is a folder (then just selects folder).
	/// </summary>
	/// <param name="f"></param>
	/// <param name="line">If not negative, goes to this 0-based line.</param>
	/// <param name="columnOrPos">If not negative, goes to this 0-based position in text (if line negative) or to this 0-based column in line.</param>
	/// <param name="findText">If not null, finds this text (<b>FindWord</b>), and goes there if found. Then <i>line</i> and <i>columnPos</i> not used.</param>
	public bool OpenAndGoTo(FileNode f, int line = -1, int columnOrPos = -1, string findText = null) {
		App.Wmain.AaShowAndActivate();
		bool wasOpen = _currentFile == f;
		if (!SetCurrentFile(f)) return false;
		var doc = Panels.Editor.ActiveDoc;
		if (findText != null) {
			line = -1;
			columnOrPos = doc.aaaText.FindWord(findText);
		}
		if (line >= 0 || columnOrPos >= 0) {
			if (line >= 0) {
				int i = doc.aaaLineStart(true, line);
				if (columnOrPos > 0) i = Math.Min(i + columnOrPos, doc.aaaLen16); //not SCI_FINDCOLUMN, it calculates tabs
				columnOrPos = i;
			}
			if (!wasOpen) wait.doEvents(); //else scrolling does not work well if now opened the file. Can't async, because caller may use the new pos immediately.
			doc.aaaGoToPos(true, columnOrPos);
		} else {
			if (!wasOpen) wait.doEvents(); //caller then may call aaaGoToPos or aaaSelect etc
		}
		doc.Focus();
		return true;
	}
	
	/// <summary>
	/// Finds code file and calls <see cref="OpenAndGoTo(FileNode, int, int, string)"/>. Does nothing if not found.
	/// </summary>
	public bool OpenAndGoTo(string file, int line = -1, int columnOrPos = -1, string findText = null) {
		var f = FindCodeFile(file); if (f == null) return false;
		return OpenAndGoTo(f, line, columnOrPos, findText);
	}
	
	/// <summary>
	/// Finds file or folder and selects the node. If not folder, opens its file in the code editor, optionally goes to the specified position or line or line/column.
	/// Returns false if failed to find or select.
	/// </summary>
	/// <param name="fileOrFolder">See <see cref="Find"/>.</param>
	/// <param name="line1Based">If not empty, goes to this 1-based line. Or can be "expand" to find and expand a folder.</param>
	/// <param name="column1BasedOrPos">If not empty, goes to this 0-based position in text (if line empty) or to this 1-based column in line.</param>
	/// <remarks>
	/// If column1BasedOrPos or line1Based not empty, searches only files, not folders.
	/// </remarks>
	public bool OpenAndGoTo2(string fileOrFolder, string line1Based = null, string column1BasedOrPos = null) {
		bool expand = line1Based == "expand"; if (expand) line1Based = "";
		var f = Find(fileOrFolder, expand ? FNFind.Folder : line1Based.NE() && column1BasedOrPos.NE() ? FNFind.Any : FNFind.CodeFile);
		if (f == null) return false;
		if (f.IsFolder) {
			f.SelectSingle();
			if (expand) TreeControl.Expand(f, true);
			return true;
		}
		int line = line1Based.NE() ? -1 : line1Based.ToInt() - 1;
		int columnOrPos = -1; if (!column1BasedOrPos.NE()) columnOrPos = column1BasedOrPos.ToInt() - (line < 0 ? 0 : 1);
		return OpenAndGoTo(f, line, columnOrPos);
	}
	
	/// <summary>
	/// Finds code file, selects the node, opens in the code editor, searches for the specified text. If finds, goes there.
	/// Returns false if failed to find file or select.
	/// </summary>
	/// <param name="fileOrFolder">See <see cref="Find"/>.</param>
	/// <param name="findText"></param>
	public bool OpenAndGoTo3(string fileOrFolder, string findText) {
		var f = FindCodeFile(fileOrFolder);
		if (f == null) return false;
		return OpenAndGoTo(f, findText: findText);
	}
	
	#endregion
	
	#region rename, delete, open/close (menu commands), properties
	
	public void RenameSelected(bool newFile = false) {
		Panels.PanelManager[Panels.Files.P].Visible = true; //exception if not visible
		TreeControl.EditLabel(ended: newFile ? ok => { if (ok && Keyboard.IsKeyDown(Key.Enter)) Panels.Editor.ActiveDoc?.Focus(); } : null);
	}
	
	public void DeleteSelected() {
		var a = TreeControl.SelectedItems; if (a.Length < 1) return;
		
		//confirmation
		var text = string.Join("\n", a.Select(f => f.Name));
		bool hasLinks = a.Any(o => o.Descendants(andSelf: true).Any(u => u.IsLink || u.IsSymlink));
		bool hasNonlinks = a.Any(o => !(o.IsLink || o.IsSymlink));
		var expandedText = (hasLinks, hasNonlinks) switch {
			(true, true) => "Files will be deleted. Will use the Recycle Bin, if possible.\nLink targets will NOT be deleted.",
			(true, false) => "The link will be deleted. Its target will NOT be deleted.",
			(false, true) => "The file will be deleted. Will use the Recycle Bin, if possible.",
			_ => null
		};
		var con = hasNonlinks ? new DControls { Checkbox = "Don't delete file" } : null;
		var r = dialog.show("Deleting", text, "1 OK|0 Cancel", owner: TreeControl, controls: con, expandedText: expandedText);
		if (r == 0) return;
		
		foreach (var f in a) {
			if (f.IsDeleted) continue; //deleted together with the parent folder
			_Delete(f, dontDeleteFile: con?.IsChecked ?? false); //info: and saves everything, now and/or later
		}
	}
	
	bool _Delete(FileNode f, bool dontDeleteFile = false, bool recycleBin = true) {
		var e = f.Descendants(true);
		
		CloseFiles(e);
		Uncut();
		App.Model.EditGoBack.OnFileDeleted(f);
		
		string filePath = f.FilePath;
		if (dontDeleteFile || f.IsLink) {
			string s1 = dontDeleteFile ? "File not deleted:" : "The deleted item was a link to";
			print.it($"<>Info: {s1} <explore>{filePath}<>");
		} else {
			string symlinkTarget = null;
			if (f.IsSymlink) filesystem.more.getFinalPath(filePath, out symlinkTarget, format: FPFormat.PrefixNever);
			
			if (!TryFileOperation(() => filesystem.delete(filePath, recycleBin ? FDFlags.RecycleBin : 0))) return false;
			
			if (symlinkTarget != null) print.it($"<>Info: The deleted item was a link to <explore>{symlinkTarget}<>");
			
			//FUTURE: move to folder 'deleted'. Moving to RB is very slow. No RB if in removable drive etc.
		}
		
		foreach (var k in e) {
			try { DB?.Execute("DELETE FROM _editor WHERE id=?", k.Id); } catch (SLException ex) { Debug_.Print(ex); }
			Au.Compiler.Compiler.Uncache(k);
			_idMap[k.Id] = null;
			_nameMap.MultiRemove_(k.Name, k);
			k.IsDeleted = true;
		}
		
		f.Remove();
		UpdateControlItems();
		//FUTURE: call event to update other controls.
		
		Save.WorkspaceLater();
		CodeInfo.FilesChanged();
		return true;
	}
	
	//SHOULDDO: once (2 times) crashed when deleting folder "@Triggers and toolbars".
	//	Both times in editor was opened a class file from the folder.
	//	First time: messagebox "exception processing message, unexpected parameters".
	//		After restarting were deleted files and tree items. Just several warnings about not found file id.
	//	Second time with debugger: access violation exception somewhere in DispatchMessage -> COM message processing.
	//		After restarting were deleted files but not tree items. Tried to reopen the file, but failed, and no editor control was created.
	//	COM wasn't used explicitly. Maybe because of Recycle Bin.
	//	Then could not reproduce (after recompiling same code).
	
	//bool _Delete(FileNode f, bool dontDeleteFile = false, bool recycleBin = true, bool canDeleteLinkTarget = false) {
	//	print.qm2.clear();
	//	var e = f.Descendants(true);
	
	//	CloseFiles(e);
	//	Uncut();
	
	//	if (!dontDeleteFile && (canDeleteLinkTarget || !f.IsLink)) {
	//		print.qm2.write("deleting files");
	//		if (!TryFileOperation(() => filesystem.delete(f.FilePath, recycleBin), deletion: true)) return false;
	//		print.qm2.write("ok");
	//		//FUTURE: move to folder 'deleted'. Moving to RB is very slow. No RB if in removable drive etc.
	//	} else {
	//		string s1 = dontDeleteFile ? "File not deleted:" : "The deleted item was a link to";
	//		print.it($"<>Info: {s1} <explore>{f.FilePath}<>");
	//	}
	
	//	foreach (var k in e) {
	//		print.qm2.write("deleting from DB", k);
	//		print.qm2.write(f);
	//		try { DB?.Execute("DELETE FROM _editor WHERE id=?", k.Id); } catch (SLException ex) { Debug_.Print(ex); }
	//		print.qm2.write("ok 1");
	//		Au.Compiler.Compiler.Uncache(k);
	//		_idMap[k.Id] = null;
	//		_nameMap.MultiRemove_(k.Name, k);
	//		k.IsDeleted = true;
	//		print.qm2.write("ok 2");
	//	}
	//	print.qm2.write("db ok");
	
	//	f.Remove();
	//	print.qm2.write(1);
	//	UpdateControlItems();
	//	print.qm2.write(2);
	//	//FUTURE: call event to update other controls.
	
	//	Save.WorkspaceLater();
	//	print.qm2.write(3);
	//	CodeInfo.FilesChanged();
	//	print.qm2.write(4);
	//	return true;
	//}
	
	/// <summary>
	/// Opens the selected item(s) in our editor or in default app or selects in Explorer.
	/// </summary>
	/// <param name="how">1 open, 2 open in new window (not impl), 3 open in default app, 4 select in Explorer.</param>
	public void OpenSelected(int how) {
		var a = TreeControl.SelectedItems; if (a.Length == 0) return;
		
		foreach (var f in a) {
			var path = f.FilePath;
			if (how is 3 or 4) {
				var e = filesystem.exists(path, useRawPath: true);
				if (!e) { print.it(f.IsFolder ? "The folder does not exist" : "The file does not exist"); continue; }
				if (how == 4 && e.IsNtfsLink && e.Directory && filesystem.more.getFinalPath(path, out var s1, format: FPFormat.PrefixNever)) path = s1;
			}
			
			switch (how) {
			case 1:
				if (f.IsFolder) TreeControl.Expand(f, true);
				else SetCurrentFile(f);
				break;
			//case 2:
			//	if(f.IsFolder) continue;
			//	//FUTURE
			//	break;
			case 3:
				run.itSafe(path);
				break;
			case 4:
				run.selectInExplorer(path);
				break;
			}
		}
	}
	
	/// <summary>
	/// Closes selected or all items, or collapses folders.
	/// Used to implement menu File -> Open/Close.
	/// </summary>
	public void CloseEtc(ECloseCmd how, FileNode dontClose = null) {
		switch (how) {
		case ECloseCmd.CloseSelectedOrCurrent:
			var a = TreeControl.SelectedItems;
			if (a.Length > 0) CloseFiles(a);
			else CloseFile(_currentFile);
			break;
		case ECloseCmd.CloseAll:
			CloseFiles(_openFiles, dontClose);
			CollapseAll();
			if (dontClose != null) TreeControl.EnsureVisible(dontClose);
			break;
		case ECloseCmd.CollapseAllFolders:
			CollapseAll();
			break;
		case ECloseCmd.CollapseInactiveFolders:
			CollapseAll(exceptWithOpenFiles: true);
			break;
		}
	}
	
	public void CollapseAll(bool exceptWithOpenFiles = false) {
		bool update = false;
		foreach (var v in Root.Descendants()) {
			if (v.IsExpanded) {
				if (exceptWithOpenFiles && v.Descendants().Any(o => _openFiles.Contains(o))) continue;
				update = true;
				v.SetIsExpanded(false);
			}
		}
		if (update) UpdateControlItems();
	}
	
	public enum ECloseCmd {
		/// <summary>
		/// Closes selected files. If there are no selected files, closes current file. Does not collapse selected folders.
		/// </summary>
		CloseSelectedOrCurrent,
		CloseAll,
		CollapseAllFolders,
		CollapseInactiveFolders,
	}
	
	public void Properties() {
		FileNode f = null;
		if (s_inContextMenu) {
			var a = TreeControl.SelectedItems;
			if (a.Length == 1) f = a[0];
		} else {
			EnsureCurrentSelected();
			f = _currentFile;
		}
		if (f == null) return;
		if (f.IsCodeFile) new DProperties(f).Show();
		//else if(f.IsFolder) new DFolderProperties(f).Show();
		//else new DOtherFileProperties(f).Show();
	}
	
	#endregion
	
	#region new item
	
	/// <summary>
	/// Gets the place where item should be added in operations such as new, paste, import.
	/// If in context menu or atSelection (true when pasting), uses selection and may show a context menu to select the place. Else first item in workspace.
	/// </summary>
	(FileNode target, FNInsert pos) _GetInsertPos(bool atSelection = false) {
		FileNode target; FNInsert pos = FNInsert.Before;
		
		if (atSelection || s_inContextMenu) {
			target = TreeControl.FocusedItem as FileNode;
			if (target == null) return (Root, FNInsert.Inside);
			
			int i;
			bool isFolder = target.IsFolder && target.IsSelected && TreeControl.SelectedIndices.Count == 1;
			if (isFolder && !target.HasChildren) pos = FNInsert.Inside;
			else if (isFolder && (i = popupMenu.showSimple("1 First in the folder|2 Last in the folder|3 Above|4 Below")) > 0) {
				switch (i) {
				case 1: target = target.FirstChild; break;
				case 2: pos = FNInsert.Inside; break;
				case 4: pos = FNInsert.After; break;
				}
			} else if (target.Next == null) pos = FNInsert.After; //usually users want to add after the last, not before
		} else { //top
			target = Root.FirstChild;
			if (target == null) { target = Root; pos = FNInsert.Inside; }
		}
		
		return (target, pos);
	}
	
	/// <summary>
	/// Creates new item.
	/// Opens file, or selects folder, or opens main file of project folder. Optionally begins renaming.
	/// Loads files.xml, finds template's element and calls <see cref="NewItemX"/>; it calls <see cref="NewItemLX"/>.
	/// </summary>
	/// <param name="template">
	/// Relative path of a file or folder in the Templates\files folder. Case-sensitive, as in workspace.
	/// Examples: "File.cs", "File.txt", "Subfolder", "Subfolder\File.cs".
	/// Special names: null (creates folder), "Script.cs", "Class.cs".
	/// If folder and not null, adds descendants too; removes '!' from the start of template folder name.
	/// </param>
	/// <param name="where">If null, adds at the context menu position or top.</param>
	/// <param name="name">If not null, creates with this name (eg "name.cs"). Else gets name from template. In any case makes unique name.</param>
	public FileNode NewItem(string template, (FileNode target, FNInsert pos)? where = null, string name = null, bool beginRenaming = false, NewFileText text = null) {
		XElement x = null;
		if (template != null) {
			x = FileNode.Templates.LoadXml(template); if (x == null) return null;
		}
		return NewItemX(x, where, name, beginRenaming, text);
	}
	
	/// <summary>
	/// Creates new item.
	/// Returns the new item, or null if fails.
	/// Does not open/select/startRenaming.
	/// </summary>
	/// <param name="template">See <see cref="NewItem"/>.</param>
	/// <param name="where">If null, adds at the context menu position or top.</param>
	/// <param name="name">If not null, creates with this name (eg "name.cs"). Else gets name from template. In any case makes unique name.</param>
	public FileNode NewItemL(string template, (FileNode target, FNInsert pos)? where = null, string name = null) {
		XElement x = null;
		if (template != null) {
			x = FileNode.Templates.LoadXml(template); if (x == null) return null;
		}
		return NewItemLX(x, where, name);
	}
	
	/// <summary>
	/// Creates new item.
	/// Opens file, or selects folder, or opens main file of project folder. Optionally begins renaming.
	/// Calls <see cref="NewItemLX"/>.
	/// <param name="template">An XElement of files.xml of the Templates workspace. If null, creates folder.</param>
	/// <param name="where">If null, adds at the context menu position or top.</param>
	/// <param name="name">If not null, creates with this name (eg "name.cs"). Else gets name from template. In any case makes unique name.</param>
	/// </summary>
	public FileNode NewItemX(XElement template, (FileNode target, FNInsert pos)? where = null, string name = null, bool beginRenaming = false, NewFileText text = null) {
		string s = null;
		if (text != null && text.replaceTemplate) {
			s = text.meta.NE() ? text.text : _MetaPlusText(text.text);
			text = null;
		}
		
		var f = NewItemLX(template, where, name, s);
		if (f == null) return null;
		
		if (beginRenaming && template != null && FileNode.Templates.IsInDefault(template)) beginRenaming = false;
		
		if (f.IsFolder) {
			if (f.IsProjectFolder(out var main) && main != null) SetCurrentFile(f = main, newFile: true); //open the main file of the new project folder
			else f.SelectSingle(); //select the new folder
		} else {
			SetCurrentFile(f, newFile: true, noTemplate: text?.replaceTemplate ?? false); //open the new file
		}
		
		if (text != null && f == CurrentFile) {
			Debug.Assert(f.IsScript);
			f.GetCurrentText(out s);
			var me = Au.Compiler.MetaComments.FindMetaComments(s).end;
			if (!text.meta.NE()) {
				if (me == 0) s = _MetaPlusText(s); //never mind: should skip script doc comments at start. Rare and not important.
				else s = s.Insert(me - 3, (s[me - 4] == ' ' ? "" : " ") + text.meta + " ");
			}
			if (!text.text.NE()) {
				if (s.NE()) s = text.text;
				else if (s.RxMatch(@"\R\R", 0, out RXGroup g, range: me..)) s = s.Insert(g.End, text.text);
				else if (s.RxMatch(@"\R\z", 0, out g, range: me..)) s = s + "\r\n" + text.text;
			}
			Panels.Editor.ActiveDoc.aaaSetText(s);
		}
		
		if (beginRenaming && f.IsSelected) RenameSelected(newFile: !f.IsFolder);
		return f;
		
		string _MetaPlusText(string t) => $"/*/ {text.meta} /*/{(t.Starts("//.") ? " " : "\r\n")}{t}";
	}
	
	/// <summary>
	/// Creates new item.
	/// Returns the new item, or null if fails.
	/// Does not open/select/startRenaming.
	/// </summary>
	/// <param name="template">An XElement of files.xml of the Templates workspace. If null, creates folder.</param>
	/// <param name="where">If null, adds at the context menu position or top.</param>
	/// <param name="name">If not null, creates with this name (eg "name.cs"). Else gets name from template. In any case makes unique name.</param>
	/// <param name="text">If not null, sets this text. If null, sets default text (template etc). Not used for folders.</param>
	public FileNode NewItemLX(XElement template, (FileNode target, FNInsert pos)? where = null, string name = null, string text = null) {
		var (target, pos) = where ?? _GetInsertPos();
		FileNode newParent = (pos == FNInsert.Inside) ? target : target.Parent;
		
		//create unique name
		bool isFolder = template == null || template.Name.LocalName == "d";
		if (name == null) {
			bool append1 = true;
			if (template == null) {
				name = "Folder";
			} else {
				name = template.Attr("n");
				if (isFolder && name.Starts('!')) name = name[1..];
				append1 = !FileNode.Templates.IsInDefault(template);
			}
			//let unique names start from 1
			if (append1) {
				int i;
				if (!isFolder && (i = name.LastIndexOf('.')) > 0) name = name.Insert(i, "1"); else name += "1";
			}
		}
		name = FileNode.CreateNameUniqueInFolder(newParent, name, isFolder, autoGenerated: true);
		
		return _NewItem(target, pos, template, name, text);
	}
	
	FileNode _NewItem(FileNode target, FNInsert pos, XElement template, string name, string text) {
		var fileType = template == null ? FNType.Folder : FileNode.XmlTagToFileType(template.Name.LocalName, canThrow: false);
		Debug.Assert(fileType is not (FNType.Script or FNType.Class) || name.Ends(".cs"));
		
		if (text == null && fileType != FNType.Folder) {
			string relPath = template.Attr("n");
			for (var p = template; (p = p.Parent).Name.LocalName != "files";) relPath = p.Attr("n") + "\\" + relPath;
			if (fileType == FNType.Other) {
				text = filesystem.loadText(FileNode.Templates.DefaultDirBS + relPath);
			} else if (FileNode.Templates.IsStandardTemplateName(relPath, out var tt)) {
				text = FileNode.Templates.Load(tt);
				//if (tt == FileNode.ETempl.Script) text = text.RxReplace(@"\bScript\s*\{", "Script {", 1); //no. The user will see warning when compiling, and let update custom template.
			} else {
				text = filesystem.loadText(FileNode.Templates.DefaultDirBS + relPath);
				if (text.Length < 20 && text.Starts("//#")) { //load default or custom template?
					tt = text switch { "//#script" => FileNode.ETempl.Script, "//#class" => FileNode.ETempl.Class, _ => 0 };
					if (tt != 0) text = FileNode.Templates.Load(tt);
				}
			}
		}
		
		FileNode parent = (pos == FNInsert.Inside) ? target : target.Parent;
		var path = parent.FilePath + "\\" + name;
		if (!TryFileOperation(() => {
			if (fileType == FNType.Folder) filesystem.createDirectory(path);
			else filesystem.saveText(path, text, tempDirectory: TempDirectory);
		})) return null;
		
		var f = new FileNode(this, name, fileType);
		f.Common_MoveCopyNew(target, pos);
		
		if (template != null) {
			if (template.Attr(out string icon, "icon")) f.CustomIconName = icon;
			
			if (fileType == FNType.Folder) {
				foreach (var x in template.Elements()) {
					_NewItem(f, FNInsert.Inside, x, x.Attr("n"), null);
				}
			}
		}
		
		return f;
	}
	
	#endregion
	
	#region clipboard
	
	struct _Clipboard {
		public FileNode[] items;
		public bool cut;
		public uint clipSN;
		
		public bool IsCut(FileNode f) => cut && items.Contains(f);
	}
	_Clipboard _clipboard;
	
	public void CutCopySelected(bool cut) {
		Uncut();
		var a = TreeControl.SelectedItems; if (a.NE_()) return;
		_clipboard = new _Clipboard { items = a, cut = cut };
		if (cut) {
			//we don't support cut to outside this workspace. Much work, rarely used, can copy/delete.
			//	The same with copy/paste between workspaces.
			clipboard.clear();
			TreeControl.Redraw();
		} else {
			var d = new clipboardData();
			d.AddFiles(a.Select(o => o.FilePath).ToArray());
			d.AddText(string.Join("\r\n", a.Select(o => o.Name)));
			d.SetClipboard();
		}
		_clipboard.clipSN = Api.GetClipboardSequenceNumber();
	}
	
	public void Paste() {
		if (_clipboard.items != null && _clipboard.clipSN != Api.GetClipboardSequenceNumber()) Uncut();
		var (target, pos) = _GetInsertPos(true);
		if (_clipboard.items != null) {
			_MultiCopyMove(!_clipboard.cut, _clipboard.items, target, pos);
			Uncut();
		} else {
			using (new clipboard.OpenClipboard_(false)) {
				var h = Api.GetClipboardData(Api.CF_HDROP);
				if (h != default) {
					var a = clipboardData.HdropToFiles_(h);
					_DroppedOrPasted(null, a, false, target, pos);
				} else if (clipboardData.GetText_(0) is string s && s.Length > 0) {
					SciCode.EIsForumCode_(s, newFile: true);
				}
			}
		}
	}
	
	public void Uncut() {
		bool cut = _clipboard.cut;
		if (!cut && _clipboard.items != null && _clipboard.clipSN == Api.GetClipboardSequenceNumber()) clipboard.clear();
		_clipboard = default;
		if (cut) TreeControl.Redraw();
	}
	
	public bool IsCut(FileNode f) => _clipboard.IsCut(f);
	
	public void SelectedCopyPath(bool full) {
		var a = TreeControl.SelectedItems; if (a.Length == 0) return;
		clipboard.text = string.Join("\r\n", a.Select(f => full ? f.FilePath : f.ItemPath));
	}
	
	#endregion
	
	#region import, move, copy
	
	void _DroppedOrPasted(FileNode[] nodes, string[] files, bool copy, FileNode target, FNInsert pos) {
		if (nodes != null) {
			_MultiCopyMove(copy, nodes, target, pos);
		} else {
			if (target == null) { target = Root; pos = FNInsert.Inside; }
			if (files.Length == 1 && IsWorkspaceDirectoryOrZip_ShowDialogOpenImport(files[0], out int dialogResult)) {
				switch (dialogResult) {
				case 1: timer.after(1, _ => LoadWorkspace(files[0])); break;
				case 2: ImportWorkspace(files[0], (target, pos)); break;
				}
				return;
			}
			ImportFiles(files, target, pos, copySilently: copy);
		}
	}
	
	/// <summary>
	/// Imports one or more files into the workspace.
	/// </summary>
	/// <param name="a">Files. If null, shows dialog to select files.</param>
	public void ImportFiles(string[] a = null) {
		if (a == null) if (!new FileOpenSaveDialog("{4D1F3AFB-DA1A-45AC-8C12-41DDA5C51CDA}") { Title = "Import files" }.ShowOpen(out a)) return;
		
		var (target, pos) = _GetInsertPos();
		ImportFiles(a, target, pos);
	}
	
	/// <summary>
	/// Imports another workspace folder or zip file (workspace or not) into this workspace.
	/// </summary>
	/// <param name="wsDirOrZip">Workspace directory or any .zip file.</param>
	/// <param name="where">If null, calls _GetInsertPos.</param>
	public void ImportWorkspace(string wsDirOrZip = null, (FileNode target, FNInsert pos)? where = null) {
		try {
			string wsDir, folderName;
			bool isZip = wsDirOrZip.Ends(".zip") && filesystem.exists(wsDirOrZip).File, notWorkspace = false;
			
			if (isZip) {
				folderName = pathname.getNameNoExt(wsDirOrZip);
				wsDir = folders.ThisAppTemp + folderName;
				filesystem.delete(wsDir);
				ZipFile.ExtractToDirectory(wsDirOrZip, wsDir);
				notWorkspace = !IsWorkspaceDirectoryOrZip(wsDir, out _);
			} else {
				wsDir = wsDirOrZip;
				folderName = pathname.getName(wsDir);
			}
			
			//create new folder for workspace's items
			var folder = NewItemLX(null, where, folderName);
			if (folder == null) return;
			
			if (notWorkspace) {
				ImportFiles(Directory.GetFileSystemEntries(wsDir), folder, FNInsert.Inside, copySilently: true);
			} else {
				var m = new FilesModel(wsDir + @"\files.xml", importing: true);
				var a = m.Root.Children().ToArray();
				_MultiCopyMove(true, a, folder, FNInsert.Inside, true);
				m.Dispose(); //currently does nothing
				print.it($"Info: Imported '{wsDirOrZip}' to folder '{folder.Name}'.\r\n\t{GetSecurityInfo()}");
			}
			
			folder.SelectSingle();
			if (isZip) filesystem.delete(wsDir);
		}
		catch (Exception ex) { print.it(ex); }
		
		//CONSIDER: rename "global.cs" and maybe "@Triggers and toolbars"
	}
	
	void _MultiCopyMove(bool copy, FileNode[] a, FileNode target, FNInsert pos, bool importingWorkspace = false) {
		if (copy) TreeControl.UnselectAll();
		try {
			bool movedCurrentFile = false;
			var a2 = new List<FileNode>(a.Length);
			foreach (var f in (pos == FNInsert.After) ? a.Reverse() : a) {
				if (!importingWorkspace && !this.IsMyFileNode(f)) continue; //deleted?
				if (copy) {
					var fCopied = f.FileCopy(target, pos, this);
					if (fCopied != null) a2.Add(fCopied);
				} else {
					if (!f.FileMove(target, pos)) continue;
					if (!movedCurrentFile && _currentFile != null) {
						if (f == _currentFile || (f.IsFolder && _currentFile.IsDescendantOf(f))) movedCurrentFile = true;
					}
				}
			}
			if (movedCurrentFile) TreeControl.EnsureVisible(_currentFile);
			if (copy && (pos != FNInsert.Inside || target.IsExpanded)) {
				bool focus = true;
				foreach (var f in a2) {
					f.IsSelected = true;
					if (focus) { focus = false; TreeControl.SetFocusedItem(f); }
				}
			}
		}
		catch (Exception e1) { print.it(e1); }
		
		//info: don't need to schedule saving here. FileCopy and FileMove did it.
	}
	
	public void ImportFiles(string[] a, FileNode target, FNInsert pos, bool copySilently = false, bool dontSelect = false, bool dontPrint = false) {
		try {
			a = a.Select(s => filesystem.more.getFinalPath(s, out s, format: FPFormat.PrefixNever) ? s : null).OfType<string>().ToArray();
			if (a.Length == 0) return;
			var newParent = (pos == FNInsert.Inside) ? target : target.Parent;
			
			//need to detect files coming from the workspace dir. When copying to a symlink folder - from that folder.
			var rootDir = newParent.Ancestors(andSelf: true, noRoot: true).FirstOrDefault(o => filesystem.exists(o.FilePath).IsNtfsLink)?.FilePath ?? FilesDirectory;
			if (!filesystem.more.getFinalPath(rootDir, out rootDir, format: FPFormat.PrefixNever)) return;
			//CONSIDER: folder symlink: later auto-update files in workspace when files added/removed not through this app.
			
			bool fromWorkspaceDir = false;
			for (int i = 0; i < a.Length; i++) {
				var s = a[i];
				if (s.Find(@"\$RECYCLE.BIN\", true) > 0) {
					var s1 = $"At first restore the file to the <a href=\"{FilesDirectory}\">workspace folder</a> or other normal folder.";
					dialog.show("Files from Recycle Bin", s1, icon: DIcon.Info, owner: TreeControl, onLinkClick: e => run.itSafe(e.LinkHref));
					return;
				}
				if (s.Eqi(rootDir) || (rootDir.Starts(s, true) && rootDir[s.Length] == '\\')) return; //s is rootDir or ancestor of rootDir
				if (!fromWorkspaceDir) {
					if (s.Starts(rootDir, true) && s[rootDir.Length] == '\\') fromWorkspaceDir = true;
				}
			}
			int action;
			if (copySilently) {
				if (fromWorkspaceDir) {
					dialog.showInfo("Files from workspace folder", "Ctrl not supported.", owner: TreeControl); //not implemented
					return;
				}
				action = 2; //copy
			} else if (fromWorkspaceDir) {
				action = 3; //move
			} else {
				var ab = new[] {
					"1 Add as a link",
					"2 Copy to the workspace folder",
					"3 Move to the workspace folder",
					"0 Cancel"
				};
				action = dialog.show("Import files", string.Join("\n", a), ab, DFlags.CommandLinks, owner: TreeControl, footer: GetSecurityInfo("v|"));
				if (action == 0) return;
			}
			
			bool select = !dontSelect && (pos != FNInsert.Inside || target.IsExpanded), focus = select;
			if (select) TreeControl.UnselectAll();
			try {
				var newParentPath = newParent.FilePath + "\\";
				var (nf1, nd1, nc1) = dontPrint ? default : _CountFilesFolders();
				
				foreach (var path in a) {
					var g = filesystem.exists(path, true);
					if (!g.Exists || g.IsNtfsLink) continue;
					bool isDir = g.Directory;
					
					if (fromWorkspaceDir) {
						var relPath = path[FilesDirectory.Length..];
						var fExists = this.Find(relPath);
						if (fExists != null) {
							fExists.FileMove(target, pos);
							continue;
						}
					}
					
					FileNode k;
					var name = pathname.getName(path);
					if (!fromWorkspaceDir) name = FileNode.CreateNameUniqueInFolder(newParent, name, isDir);
					
					if (action == 1 && !isDir) { //add as file link
						k = new FileNode(this, name, path, isDir: false, isLink: true);
					} else {
						var newPath = newParentPath + name;
						if (!TryFileOperation(() => {
							if (action == 1) {
								filesystem.more.createSymbolicLink(newPath, path, CSLink.JunctionOrSymlink, elevate: true);
							} else if (action == 2) {
								filesystem.copy(path, newPath, FIfExists.Fail);
							} else if (newPath != path) {
								filesystem.move(path, newPath, FIfExists.Fail);
							}
						})) continue;
						k = new FileNode(this, name, newPath, isDir, isLink: action == 1);
						if (isDir) _AddDir(newPath, k);
					}
					target.AddChildOrSibling(k, pos, false);
					if (select) {
						k.IsSelected = true;
						if (focus) { focus = false; TreeControl.SetFocusedItem(k); }
					}
				}
				
				if (!dontPrint) {
					var (nf2, nd2, nc2) = _CountFilesFolders();
					int nf = nf2 - nf1, nd = nd2 - nd1, nc = nc2 - nc1;
					if (nf + nd > 0) print.it($"Info: Imported {nf} files and {nd} folders.{(nc > 0 ? GetSecurityInfo("\r\n\t") : null)}");
				}
			}
			catch (Exception ex) { print.it(ex.Message); }
			Save.WorkspaceLater();
			CodeInfo.FilesChanged();
			
			void _AddDir(string path, FileNode parent) {
				foreach (var u in filesystem.enumerate(path, FEFlags.UseRawPath | FEFlags.SkipHiddenSystem)) {
					bool isDir = u.IsDirectory;
					var k = new FileNode(this, u.Name, u.FullPath, isDir, isLink: isDir && u.IsNtfsLink);
					parent.AddChild(k);
					if (isDir) _AddDir(u.FullPath, k);
				}
			}
			
			(int nf, int nd, int nc) _CountFilesFolders() {
				int nf = 0, nd = 0, nc = 0;
				foreach (var v in Root.Descendants()) if (v.IsFolder) nd++; else { nf++; if (v.IsCodeFile) nc++; }
				return (nf, nd, nc);
			}
		}
		catch (Exception e1) { print.it(e1); }
	}
	
	/// <summary>
	/// Adds to workspace 1 file (not folder) that exists in workspace folder in filesystem.
	/// </summary>
	public FileNode ImportFromWorkspaceFolder(string path, FileNode target, FNInsert pos) {
		FileNode R = null;
		try {
			if (!filesystem.exists(path, true).File) return null;
			
			var relPath = path[FilesDirectory.Length..];
			var fExists = this.Find(relPath);
			if (fExists != null) {
				if (fExists.IsFolder) return null;
				R = fExists;
			} else {
				var name = pathname.getName(path);
				R = new FileNode(this, name, path, isDir: false);
				target.AddChildOrSibling(R, pos, false);
			}
		}
		catch (Exception ex) { print.it(ex.Message); }
		Save.WorkspaceLater();
		CodeInfo.FilesChanged();
		return R;
	}
	
	#endregion
	
	#region export
	
	/// <summary>
	/// Shows dialog to get path for new or exporting workspace.
	/// Returns workspace's directory path.
	/// Does not create any files/directories.
	/// </summary>
	/// <param name="name">Default name of the workspace.</param>
	/// <param name="location">Default parent directory of the main directory of the workspace.</param>
	public static string GetDirectoryPathForNewWorkspace(string name = null, string location = null) {
		var d = new DNewWorkspace(name, location ?? folders.ThisAppDocuments) { Owner = App.Wmain, ShowInTaskbar = false };
		if (d.ShowDialog() != true) return null;
		return d.ResultPath;
	}
	
	public bool ExportSelected(string location = null, bool zip = false) {
		var a = TreeControl.SelectedItems; if (a.Length < 1) return false;
		
		string name = a[0].Name; if (!a[0].IsFolder) name = pathname.getNameNoExt(name);
		
		if (a.Length == 1 && a[0].IsFolder && a[0].HasChildren) a = a[0].Children().ToArray();
		
		string wsDir;
		if (zip) {
			var d = new FileOpenSaveDialog("{4D1F3AFB-DA1A-45AC-8C12-41DDA5C51CDA}") {
				FileTypes = "Zip files|*.zip",
				DefaultExt = "zip",
				InitFolderFirstTime = location ?? folders.ThisAppDocuments,
				FileNameText = name + ".zip",
			};
			if (!d.ShowSave(out location, App.Hmain, overwritePrompt: false)) return false;
			wsDir = folders.ThisAppTemp + "Workspace zip";
			filesystem.delete(wsDir);
		} else {
			wsDir = GetDirectoryPathForNewWorkspace(name, location);
			if (wsDir == null) return false;
		}
		
		string filesDir = wsDir + @"\files";
		try {
			filesystem.createDirectory(filesDir);
			foreach (var f in a) {
				if (!f.IsLink) filesystem.copyTo(f.FilePath, filesDir);
			}
			FileNode.Export(a, wsDir + @"\files.xml");
		}
		catch (Exception ex) {
			print.it(ex);
			return false;
		}
		
		if (zip) {
			filesystem.delete(location);
			ZipFile.CreateFromDirectory(wsDir, location);
			filesystem.delete(wsDir);
			wsDir = location;
		}
		
		print.it($"<>Exported to <explore>{wsDir}<>");
		return true;
	}
	
	#endregion
	
	#region fill menu
	
	/// <summary>
	/// Adds recent workspaces to submenu File -> Workspace.
	/// </summary>
	public static void FillMenuRecentWorkspaces(MenuItem sub) {
		void _Add(string path, int i) {
			path = pathname.expand(path);
			var mi = new MenuItem { Header = path.Replace("_", "__") };
			if (i == 0) mi.FontWeight = FontWeights.Bold;
			mi.Click += (_, _) => LoadWorkspace(path);
			sub.Items.Insert(i, mi);
		}
		
		while (sub.Items[0] is not Separator) sub.Items.RemoveAt(0);
		_Add(App.Settings.workspace, 0);
		var ar = App.Settings.recentWS;
		int j = 0, i = 0, n = ar?.Length ?? 0;
		for (; i < n; i++) {
			if (sub.Items.Count >= 15 || !filesystem.exists(ar[i]).Directory) ar[i] = null;
			else _Add(ar[i], ++j);
		}
		if (j < i) App.Settings.recentWS = ar.Where(o => o != null).ToArray();
	}
	
	/// <summary>
	/// Adds templates to File -> New.
	/// </summary>
	public static void FillMenuNew(MenuItem sub) {
		var xroot = FileNode.Templates.LoadXml();
		if (sub.Items.Count > Menus.File.New.ItemCount) { //not first time
			if (xroot == sub.Tag) return; //else rebuild menu because Templates\files.xml modified
			for (int i = sub.Items.Count; --i >= Menus.File.New.ItemCount;) sub.Items.RemoveAt(i);
		}
		sub.Tag = xroot;
		
		var templDir = FileNode.Templates.DefaultDirBS;
		_CreateMenu(sub, xroot, null, 0);
		
		void _CreateMenu(MenuItem mParent, XElement xParent, string dir, int level) {
			foreach (var x in xParent.Elements()) {
				string tag = x.Name.LocalName, name = x.Attr("n");
				int isFolder = tag == "d" ? 1 : 0;
				if (isFolder == 1) {
					isFolder = name[0] switch { '@' => 2, '!' => 3, _ => 1 }; //@ project, ! simple folder
				} else if (level == 0) {
					if (FileNode.Templates.IsStandardTemplateName(name, out _) || name == "File.txt") continue;
				}
				string relPath = dir + name;
				if (isFolder == 3) name = name[1..];
				var item = new MenuItem { Header = name.Replace("_", "__") };
				if (isFolder == 1) {
					_CreateMenu(item, x, relPath + "\\", level + 1);
				} else {
					item.Click += (_, e) => App.Model.NewItemX(x, beginRenaming: true);
					var ft = FileNode.XmlTagToFileType(tag, canThrow: false);
					item.Icon = ft == FNType.Other
						? new Image { Source = icon.of(templDir + relPath)?.ToWpfImage() }
						: ImageUtil.LoadWpfImageElement(FileNode.GetFileTypeImageSource(ft));
				}
				mParent.Items.Add(item);
			}
		}
	}
	
	#endregion
	
	#region other
	
	/// <summary>
	/// Adds some default files if missing.
	/// </summary>
	/// <param name="scriptForNewWorkspace">If empty workspace, creates new empty script from current template.</param>
	/// <param name="globalCs">If class file "global.cs" not found, creates it in existing or new folder "Classes".</param>
	public void AddMissingDefaultFiles(bool scriptForNewWorkspace = false, bool globalCs = false) {
		if (scriptForNewWorkspace && Root.FirstChild == null) {
			NewItem(@"Script.cs");
		}
		if (globalCs && null == Find("global.cs", FNFind.Class)) {
			var folder = Find(@"\Classes", FNFind.Folder);
			if (folder == null) folder = NewItemL(null, (Root, FNInsert.Inside), "Classes");
			NewItemL(@"Default\global.cs", (folder, FNInsert.Inside));
		}
	}
	
	public record UserData {
		public string guid { get; init; }
		public string startupScripts, debuggerScript;
	}
	
	public UserData CurrentUser(bool set) {
		var u = WSSett?.users?.FirstOrDefault(o => o.guid == App.UserGuid);
		if (u == null && set) {
			u = new UserData { guid = App.UserGuid };
			var a = WSSett.users ?? Array.Empty<UserData>();
			a = a.InsertAt(0, u);
			WSSett.users = a;
		}
		return u;
	}
	
	public string StartupScriptsCsv {
		get => CurrentUser(false)?.startupScripts;
		set { CurrentUser(true).startupScripts = value; }
	}
	
	public string DebuggerScript {
		get => CurrentUser(false)?.debuggerScript;
		set { CurrentUser(true).debuggerScript = value; }
	}
	
	public void RunStartupScripts(bool startAsync) {
		var csv = StartupScriptsCsv; if (csv == null) return;
		try {
			var x = csvTable.parse(csv);
			foreach (var row in x.Rows) {
				string file = row[0];
				if (file.Starts("//")) continue;
				var f = FindCodeFile(file);
				if (f == null) { print.it("Startup script not found: " + file + ". Please edit Options -> Run scripts..."); continue; }
				int delay = 0;
				if (x.ColumnCount > 1) {
					var sd = row[1];
					delay = sd.ToInt(0, out int end);
					if (end > 0 && !sd.Ends("ms", true)) delay = (int)Math.Min(delay * 1000L, int.MaxValue);
				}
				if (startAsync && delay < 10) delay = 10;
				if (delay > 0) {
					timer.after(delay, t => {
						CompileRun.CompileAndRun(true, f);
					});
				} else {
					CompileRun.CompileAndRun(true, f);
				}
			}
		}
		catch (FormatException) { }
	}
	
	//Used mostly by SciCode, but owned by workspace because can go to any file.
	internal readonly EditGoBack EditGoBack = new();
	
	#endregion
	
	#region util
	
	/// <summary>
	/// Calls Action a in try/catch. On exception prints message and returns false.
	/// </summary>
	public bool TryFileOperation(Action a) {
		try { a(); }
		catch (Exception ex) { print.it(ex); return false; }
		return true;
	}
	
	/// <summary>
	/// Returns true if FileNode f is not null and belongs to this FilesModel and is not deleted.
	/// </summary>
	public bool IsMyFileNode(FileNode f) { return Root.IsAncestorOf(f); }
	
	/// <summary>
	/// Returns true if s is path of a workspace directory or .zip file.
	/// </summary>
	public static bool IsWorkspaceDirectoryOrZip(string path, out bool zip) {
		zip = false;
		switch (filesystem.exists(path)) {
		case 2:
			string xmlFile = path + @"\files.xml";
			if (filesystem.exists(xmlFile).File && filesystem.exists(path + @"\files").Directory) {
				try { return XmlUtil.LoadElem(xmlFile).Name == "files"; } catch { }
			}
			break;
		case 1 when path.Ends(".zip", true):
			return zip = true;
		}
		return false;
	}
	
	/// <summary>
	/// If s is path of a workspace directory or .zip file, shows "Open/import" dialog and returns true.
	/// dialogResult receives: 1 Open, 2 Import, 0 Cancel.
	/// </summary>
	public static bool IsWorkspaceDirectoryOrZip_ShowDialogOpenImport(string path, out int dialogResult) {
		dialogResult = 0;
		if (!IsWorkspaceDirectoryOrZip(path, out bool zip)) return false;
		var text1 = zip ? "Import files from zip" : "Workspace";
		var buttons = zip ? "2 Import|0 Cancel" : "1 Open|2 Import|0 Cancel";
		dialogResult = dialog.show(text1, path, buttons, footer: GetSecurityInfo("v|"), owner: TreeControl);
		return true;
	}
	
	/// <summary>
	/// Security info string.
	/// </summary>
	public static string GetSecurityInfo(string prefix = null) {
		return prefix + "Security info: Unknown C# script files can contain malicious code - virus, spyware, etc. It is safe to import, open and edit C# files if you don't run them. Triggers don't work until run.";
	}
	
	#endregion
}
