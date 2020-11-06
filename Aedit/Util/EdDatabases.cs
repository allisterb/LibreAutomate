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
//using System.Drawing;
using System.Linq;

using Au;
using Au.Types;

/// <summary>
/// Creates and opens databases ref.db, doc.db, winapi.db.
/// </summary>
static class EdDatabases
{
	public static ASqlite OpenRef() {
		string copy = App.Settings.db_copy_ref;
		if (copy != null) {
			App.Settings.db_copy_ref = null;
			AFile.CopyTo(copy, AFolders.ThisApp, FIfExists.Delete);
		}
		return new ASqlite(AFolders.ThisAppBS + "ref.db", SLFlags.SQLITE_OPEN_READONLY);
	}

	public static ASqlite OpenDoc() {
		string copy = App.Settings.db_copy_doc;
		if (copy != null) {
			App.Settings.db_copy_doc = null;
			AFile.CopyTo(copy, AFolders.ThisApp, FIfExists.Delete);
		}
		return new ASqlite(AFolders.ThisAppBS + "doc.db", SLFlags.SQLITE_OPEN_READONLY);
	}

	public static ASqlite OpenWinapi() {
		string copy = App.Settings.db_copy_winapi;
		if (copy != null) {
			App.Settings.db_copy_winapi = null;
			AFile.CopyTo(copy, AFolders.ThisApp, FIfExists.Delete);
		}
		return new ASqlite(AFolders.ThisAppBS + "winapi.db", SLFlags.SQLITE_OPEN_READONLY);
	}

	#region create ref and doc

	/// <summary>
	/// Creates SQLite databases containing design-time assemblies and XML documentation files of a .NET Core runtime. The SDK must be installed.
	/// </summary>
	/// <remarks>
	/// Shows a list dialog.
	///		If selected All, creates for all runtime versions starting from 3.1, with names ref.version.db (eg ref.3.1.0.db) and doc.version.db, in AFolders.ThisAppBS.
	///		Else creates only for the selected runtime version, with names ref.db and doc.db, in dataDir, and sets to copy to AFolders.ThisAppBS when opening next time after process restarts.
	/// We ship and at run time load databases of single version, named ref.db and doc.db. In the future should allow to download and use multiple versions.
	/// Also this function allows users to create databases from SDKs installed on their PC, but currently this feature is not exposed. Would need to add UI and exception handling.
	/// ref.db contains dlls from 'dotnet\packs' folder. They contain only metadata of public API, not all code like dlls in the 'dotnet\shared' folder.
	///		Why need it when we can load PortableExecutableReference from 'dotnet\shared' folder? Because:
	///			1. They are big and may add 100 MB of process memory. We need to load all, because cannot know which are actually used in various stages of compilation.
	///			2. When loading from dll files, Windows Defender makes it as slow as 2.5 s or more, unless the files already are in OS file buffers.
	///			3. Better compatibility. See https://github.com/dotnet/standard/blob/master/docs/history/evolution-of-design-time-assemblies.md
	///	doc.db contains XML documentation files of .NET Core assemblies. From the same 'dotnet\packs' folder.
	///		Why need it:
	///			1. Else users would have to download whole .NET Core SDK. Now need only runtimes.
	///			2. Parsed XML files can use eg 200 MB of process memory. Now we get doc of a single type/method/etc from database only when need; all other data is not in memory.
	///			
	/// Need to run this after changing Core version of C# projects (<TargetFramework>netcoreapp3.1</TargetFramework>). Also update COREVER2 etc in AppHost.cpp.
	/// </remarks>
	public static void CreateRefAndDoc(string dataDir = @"Q:\app\Au\Other\Data") {
		Cursor.Current = Cursors.WaitCursor;
		string dirPacks = APath.Normalize_(AFolders.NetRuntimeBS + @"..\..\..\packs");
		string dirCore = dirPacks + @"\Microsoft.NETCore.App.Ref\";
		var a = new List<string>();
		foreach (var f in AFile.Enumerate(dirCore, FEFlags.UseRawPath)) { //for each version
			if (!f.IsDirectory) continue;
			var s = f.Name;
			int v1 = s.ToInt(0, out int ne), v2 = s.ToInt(ne + 1);
			//if (v1 < 3 || (v1 == 3 && v2 < 1)) continue; //must be 3.1 or later
			if (v1 < 5 || (v1 == 5 && v2 < 0)) continue; //must be 5.0 or later
			a.Add(s);
		}
		a.Add("All");
		int i = ADialog.ShowList(a, "Create database", "For runtime") - 1;
		if (i < 0) return;
		int n = a.Count - 1;
		if (i < n) {
			_CreateRefAndDoc(dirPacks, dirCore, a[i], false, dataDir);
		} else {
			for (i = 0; i < n; i++) _CreateRefAndDoc(dirPacks, dirCore, a[i], true, dataDir);
		}
		AOutput.Write("CreateRefAndDoc done.");
		Cursor.Current = Cursors.Arrow;
	}

	static void _CreateRefAndDoc(string dirPacks, string dirCore, string version, bool all, string dataDir) {
		//string subdirRN = @"\ref\netcoreapp" + version.RegexReplace(@"^\d+\.\d+\K.+", @"\", 1); //Core 3.x
		string subdirRN = @"\ref\net" + version.RegexReplace(@"^\d+\.\d+\K.+", @"\", 1); //.NET 5

		var dir1 = dirCore + version + subdirRN;
		if (!AFile.ExistsAsDirectory(dir1, true)) throw new DirectoryNotFoundException("Not found: " + dir1);

		//find WindowsDesktop folder. Must have same X.X.X version. Preview version may be different.
		bool preview; int i = version.Find("-p", true);
		if (preview = i >= 0) version = version[..(i + 2)];
		string verDesktop = null;
		string dirDesktop = dirPacks + @"\Microsoft.WindowsDesktop.App.Ref\";
		foreach (var f in AFile.Enumerate(dirDesktop, FEFlags.UseRawPath)) { //for each version
			if (!f.IsDirectory) continue;
			var s = f.Name;
			if (preview ? s.Starts(version, true) : s == version) { verDesktop = s; break; }
		}
		if (verDesktop == null) throw new DirectoryNotFoundException("Not found: WindowsDesktop SDK");
		var dir2 = dirDesktop + verDesktop + subdirRN;
		if (!AFile.ExistsAsDirectory(dir2, true)) throw new DirectoryNotFoundException("Not found: " + dir2);

		string dbRef, dbDoc;
		if (all) {
			dbRef = AFolders.ThisAppBS + "ref." + version + ".db";
			dbDoc = AFolders.ThisAppBS + "doc." + version + ".db";
		} else {
			dbRef = dataDir + @"\ref.db";
			dbDoc = dataDir + @"\doc.db";
		}
		_CreateRef(dbRef, dir1, dir2);
		_CreateDoc(dbDoc, dir1, dir2);

		if (!all) {
			App.Settings.db_copy_ref = dbRef;
			App.Settings.db_copy_doc = dbDoc;
		}
	}

	static void _CreateRef(string dbFile, string dir1, string dir2) {
		AFile.Delete(dbFile);
		using var d = new ASqlite(dbFile);
		using var trans = d.Transaction();
		d.Execute("CREATE TABLE ref (name TEXT PRIMARY KEY, data BLOB)");
		using var statInsert = d.Statement("INSERT OR REPLACE INTO ref VALUES (?, ?)");

		_AddDir(dir1, "WindowsBase", "System.Drawing");
		_AddDir(dir2);

		trans.Commit();
		d.Execute("VACUUM");

		AOutput.Write("Created " + dbFile);

		void _AddDir(string dir, params string[] skip) {
			foreach (var f in AFile.Enumerate(dir)) {
				if (f.IsDirectory) continue;
				if (!f.Name.Ends(".dll", true)) continue;
				var asmName = f.Name.RemoveSuffix(4);
				if (skip.Contains(asmName)) continue;
				_AddFile(asmName, f.FullPath);
				//break;
			}
		}

		void _AddFile(string asmName, string asmFile) {
			//AOutput.Write(asmName);
			statInsert.Bind(1, asmName);
			statInsert.Bind(2, File.ReadAllBytes(asmFile));
			statInsert.Step();
			statInsert.Reset();
		}
	}

	static void _CreateDoc(string dbFile, string dir1, string dir2) {
		AFile.Delete(dbFile);
		using var d = new ASqlite(dbFile, sql: "PRAGMA page_size = 8192;"); //8192 makes file smaller by 2-3 MB.
		using var trans = d.Transaction();
		d.Execute("CREATE TABLE doc (name TEXT PRIMARY KEY, xml TEXT)");
		using var statInsert = d.Statement("INSERT INTO doc VALUES (?, ?)");
		using var statDupl = d.Statement("SELECT xml FROM doc WHERE name=?");
		var haveRefs = new List<string>();
		var uniq = new Dictionary<string, string>(); //name -> asmName

		//using var textFile = File.CreateText(Path.ChangeExtension(dbFile, "txt")); //test. Compresses almost 2 times better than db.

		_AddDir(dir1, "WindowsBase");
		_AddDir(dir2);

		statInsert.BindAll(".", string.Join("\n", haveRefs)).Step();

		trans.Commit();
		d.Execute("VACUUM");

		AOutput.Write("Created " + dbFile);

		void _AddDir(string dir, params string[] skip) {
			foreach (var f in AFile.Enumerate(dir)) {
				if (f.IsDirectory) continue;
				if (!f.Name.Ends(".xml", true)) continue;
				var asmName = f.Name.RemoveSuffix(4);
				if (skip.Contains(asmName)) continue;
				if (!AFile.ExistsAsFile(dir + asmName + ".dll")) {
					AOutput.Write("<><c 0x808080>" + f.Name + "</c>");
					continue;
				}
				_AddFile(asmName, f.FullPath);
				//break;
			}
		}

		void _AddFile(string asmName, string xmlFile) {
			//AOutput.Write(asmName);
			haveRefs.Add(asmName);
			var xr = AExtXml.LoadElem(xmlFile);
			foreach (var e in xr.Descendants("member")) {
				var name = e.Attr("name");

				//remove <remarks> and <example>. Does not save much space, because .NET xmls don't have it.
				foreach (var v in e.Descendants("remarks").ToArray()) v.Remove();
				foreach (var v in e.Descendants("example").ToArray()) v.Remove();

				using var reader = e.CreateReader();
				reader.MoveToContent();
				var xml = reader.ReadInnerXml();
				//AOutput.Write(name, xml);

				//textFile.WriteLine(name); textFile.WriteLine(xml); textFile.WriteLine("\f");

				if (uniq.TryGetValue(name, out var prevRef)) {
					if (!statDupl.Bind(1, name).Step()) throw new AuException();
					var prev = statDupl.GetText(0);
					if (xml != prev && asmName != "System.Linq") AOutput.Write($"<>\t{name} already defined in {prevRef}\r\n<c 0xc000>{prev}</c>\r\n<c 0xff0000>{xml}</c>");
					statDupl.Reset();
				} else {
					statInsert.BindAll(name, xml).Step();
					uniq.Add(name, asmName);
				}
				statInsert.Reset();
			}
		}
	}

	#endregion

	/// <summary>
	/// Creates SQLite database containing Windows API declarations.
	/// </summary>
	public static void CreateWinapi(string csDir = @"Q:\app\Au\Other\Api", string dataDir = @"Q:\app\Au\Other\Data") {
		Cursor.Current = Cursors.WaitCursor;
		string dbFile = dataDir + @"\winapi.db";
		AFile.Delete(dbFile);

		string s = File.ReadAllText(csDir + @"\Api.cs");

		using var d = new ASqlite(dbFile);
		using var trans = d.Transaction();
		d.Execute("CREATE TABLE api (name TEXT, def TEXT)"); //note: no PRIMARY KEY. Don't need index.
		using var statInsert = d.Statement("INSERT INTO api VALUES (?, ?)");

		string rxType = @"(?ms)^(?:\[[^\r\n]+\r\n)*internal (?:struct|enum|interface|class) (\w+)[^\r\n\{]+\{(?:\}$|.+?^\})";
		string rxFunc = @"(?m)^(?:\[[^\r\n]+\r\n)*internal (?:static extern|delegate) \w+\** (\w+)\(.+;$";
		string rxVarConst = @"(?m)^internal (?:const|readonly|static) \w+ (\w+) =.+;$";

		foreach (var m in s.RegexFindAll(rxType)) _Add(m);
		foreach (var m in s.RegexFindAll(rxFunc)) _Add(m);
		foreach (var m in s.RegexFindAll(rxVarConst)) _Add(m);

		void _Add(RXMatch m) {
			statInsert.Bind(1, m[1].Value);
			statInsert.Bind(2, m.Value);
			statInsert.Step();
			statInsert.Reset();
		}

		trans.Commit();
		d.Execute("VACUUM");

		App.Settings.db_copy_winapi = dbFile;

		AOutput.Write("CreateWinapi done.");
		Cursor.Current = Cursors.Arrow;
	}
}
