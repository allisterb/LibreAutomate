﻿using System;
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
using System.Drawing;
//using System.Linq;

using Au.Types;
using Au.Util;

namespace Au
{
	/// <summary>
	/// Gets icons for files etc.
	/// </summary>
	public static class AIcon
	{
		/// <summary>
		/// Gets file icon.
		/// Extracts icon directly from the file, or gets shell icon, depending on file type, icon index, etc.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		/// <param name="file">
		/// Can be:
		/// - Path of any file or folder. Supports environment variables.
		/// - Any shell object, like <c>":: ITEMIDLIST"</c>, <c>@"::{CLSID-1}\::{CLSID-2}"</c>, <c>@"shell:AppsFolder\WinStoreAppId"</c>.
		/// - File type or protocol, like <c>".txt"</c>, <c>"http:"</c>.
		/// - Icon resource index or negative id, like "c:\file.dll,4", "c:\file.dll,-4".
		/// - URL.
		/// </param>
		/// <param name="size">Icon width and height. If 0 (default), uses <see cref="SizeSmall"/>.</param>
		/// <param name="flags"></param>
		/// <remarks>
		/// If not full path, the function uses <see cref="AFolders.ThisAppImages"/> and <see cref="AFile.SearchPath"/>.
		/// 
		/// ITEMIDLIST can be of any file, folder, URL or virtual object like Control Panel. See <see cref="APidl.ToBase64String"/>.
		/// </remarks>
		public static Icon GetFileIcon(string file, int size = 0, IconGetFlags flags = 0)
			=> HandleToIcon(GetFileIconHandle(file, size, flags), true);
		///// If not full path, the function will look in <see cref="AFolders.ThisAppImages"/>. See also <see cref="IconGetFlags"/>.

		/// <summary>
		/// Gets file icon.
		/// More info: <see cref="GetFileIcon"/>.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		public static Bitmap GetFileIconImage(string file, int size = 0, IconGetFlags flags = 0)
			=> HandleToImage(GetFileIconHandle(file, size, flags), true);

		/// <summary>
		/// Gets file icon.
		/// More info: <see cref="GetFileIcon"/>.
		/// </summary>
		/// <returns>Returns icon handle, or default(IntPtr) if failed. Later call <see cref="DestroyIconHandle"/> or some <b>HandleToX</b> function that will destroy it.</returns>
		public static IntPtr GetFileIconHandle(string file, int size = 0, IconGetFlags flags = 0)
		{
			if(file.NE()) return default;
			size = _NormalizeIconSizeArgument(size);
			file = APath.ExpandEnvVar(file);

			//var perf = APerf.Create();
			IntPtr R = _GetFileIcon(file, size, flags);
			//perf.Next(); AOutput.Write(perf.ToString(), file);
			//AOutput.Write($"<><c 0xff0000>{file}</c>");
			return R;
		}

		/// <summary>
		/// Gets icon of a file or other shell object specified as ITEMIDLIST.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		/// <param name="pidl">ITEMIDLIST pointer (PIDL).</param>
		/// <param name="size">Icon width and height. If 0 (default), uses <see cref="SizeSmall"/>.</param>
		public static Icon GetPidlIcon(APidl pidl, int size = 0)
			=> HandleToIcon(GetPidlIconHandle(pidl, size), true);

		/// <summary>
		/// Gets icon of a file or other shell object specified as ITEMIDLIST.
		/// More info: <see cref="GetPidlIcon"/>.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		public static Bitmap GetPidlIconImage(APidl pidl, int size = 0)
			=> HandleToImage(GetPidlIconHandle(pidl, size), true);

		/// <summary>
		/// Gets icon of a file or other shell object specified as ITEMIDLIST.
		/// More info: <see cref="GetPidlIcon"/>.
		/// </summary>
		/// <returns>Returns icon handle, or default(IntPtr) if failed. Later call <see cref="DestroyIconHandle"/> or some <b>HandleToX</b> function that will destroy it.</returns>
		public static IntPtr GetPidlIconHandle(APidl pidl, int size = 0)
		{
			if(pidl?.IsNull ?? false) return default;
			size = _NormalizeIconSizeArgument(size);
			return _GetShellIcon(true, null, pidl, size);
		}

		static IntPtr _GetFileIcon(string file, int size, IconGetFlags flags)
		{
			IntPtr R = default;
			int index = 0;
			bool extractFromFile = false, isFileType = false, isURL = false, isShellPath = false, isPath = true;
			//bool getDefaultIfFails = 0!=(flags&IconGetFlags.DefaultIfFails);

			bool searchPath = 0 == (flags & IconGetFlags.DontSearch);

			if(0 == (flags & IconGetFlags.LiteralPath)) {
				//is ".ext" or "protocol:"?
				isFileType = APath.IsExtension_(file) || (isURL = APath.IsProtocol_(file));
				if(!isFileType) isURL = APath.IsUrl(file);
				if(isFileType || isURL || (isShellPath = (file[0] == ':'))) isPath = false;
				if(isPath) {
					//get icon index from "path,index" and remove ",index"
					extractFromFile = ParseIconLocation(ref file, out index);

					if(!searchPath) {
						if(!APath.IsFullPath(file)) file = AFolders.ThisAppImages + file;
						file = APath.Normalize_(file, PNFlags.DontPrefixLongPath, noExpandEV: true);
					}
				}
			}

			if(isPath) {
				if(searchPath) {
					file = AFile.SearchPath(file, AFolders.ThisAppImages);
					if(file == null) return default; //ignore getDefaultIfFails
				}
				file = APath.UnprefixLongPath(file);
			}

			if(isPath /*&& (extractFromFile || 0==(flags&IconGetFlags.Shell))*/) {
				int ext = 0;
				if(!extractFromFile && file.Length > 4) ext = file.Ends(true, ".exe", ".scr", ".ico", ".cur", ".ani");
				if(extractFromFile || ext > 0) {
					R = LoadIconHandle(file, index, size);
					if(R != default || extractFromFile) return R;
					switch(AFile.ExistsAs(file, true)) {
					case FileDir.NotFound:
						return default;
					case FileDir.File:
						var siid = StockIcon.DOCNOASSOC;
						if(ext >= 1 && ext <= 2) siid = StockIcon.APPLICATION;
						return GetStockIconHandle(siid, size);
						//case FileDir.Directory: //folder name ends with .ico etc
					}
				} else if(file.Ends(".lnk", true)) {
					R = _GetLnkIcon(file, size);
					if(R != default) return R;
					//AOutput.Write("_GetLnkIcon failed", file);
				}

				//note: here we don't cache icons.
				//Fast enough for where we use this. OS file buffers remain in memory for some time.
				//Where need, should instead use imagelists or some external cache that saves eg full toolbar bitmap.
				//SHGetFileInfo has its own cache. In some cases it makes faster (except first time in process), but using it to get all icons is much slower.
			}

			bool isExt = isFileType && !isURL;

			//Can use this code to avoid slow shell API if possible.
			//In some test cases can make ~2 times faster (with thread pool), especially in MTA thread.
			//But now, after other optimizations applied, in real life makes faster just 10-20%.
#if false
			//if(0==(flags&IconGetFlags.Shell)){
			string progId = isShellPath ? null : AFile.More.GetFileTypeOrProtocolRegistryKey(file, isFileType, isURL);

			RegistryKey rk = (progId == null) ? null : ARegistry.Open(progId, Registry.ClassesRoot);
			//AOutput.Write(file, progId, isFileType, isURL, rk != null);

			if(rk == null) {
				//Unregistered file type/protocol, no extension, folder, ::{CLSID}, shell:AppsFolder\WinStoreAppId, or no progId key in HKCR
				//AOutput.Write(@"unregistered", file, progId);
				if(progId != null) goto gr; //the file type is known, but no progid key in HKCR. Let shell API figure out. Rare.
				if(isExt || (isPath && AFile.FileExists(file))) return GetStockIconHandle(StockIcon.DOCNOASSOC, size);
				goto gr;
			}

			//Registered file type/protocol.
			using(rk) {
				if(ARegistry.KeyExists(@"ShellEx\IconHandler", rk)) {
					//AOutput.Write(@"handler", file);
					goto gr;
				}

				string icon;
				if(ARegistry.GetString(out icon, "", @"DefaultIcon", rk) && icon.Length > 0) {
					//AOutput.Write("registry: DefaultIcon", file, icon);
					if(icon[0] == '@') icon = null; //eg @{Microsoft.Windows.Photos_16.622.13140.0_x64__8wekyb3d8bbwe?ms-resource://Microsoft.Windows.Photos/Files/Assets/PhotosLogoExtensions.png}
					else ParseIconLocation(ref icon, out index);
				} else if(ARegistry.GetString(out icon, "", @"shell\open\command", rk) && icon.Length > 0) {
					//AOutput.Write(@"registry: shell\open\command", file, icon);
					var a = icon.SegSplit((icon[0] == '\"') ? "\"" : " ", StringSplitOptions.RemoveEmptyEntries);
					icon = (a.Length == 0) ? null : a[0];
					if(icon.Ends("rundll32.exe", true)) icon = null;
				} else {
					icon = null;
					//AOutput.Write("registry: no", file);
					//Usually shell API somehow gets icon.
					//For example also looks in .ext -> PerceivedType -> HKCR\SystemFileAssociations.
					//We can use AssocQueryString(ASSOCSTR_DEFAULTICON), but it is slow and not always gets correct icon.
				}

				//if(icon != null) AOutput.Write(file, icon);

				if(icon == "%1") {
					//AOutput.Write(file);
					if(isPath) icon = file;
					else icon = null;
				}

				if(icon != null) {
					icon = APath.ExpandEnvVar(icon);
					if(!APath.IsFullPath(icon)) icon = AFolders.System + icon;
					R = GetFileIconHandleRaw(icon, index, size);
					if(R != default) return R;
				}
			}
			//}
			gr:
#endif
			return _GetShellIcon(!isExt, file, null, size);
		}

		//usePidl - if pidl not null/IsNull, use pidl, else convert file to PIDL. If false, pidl must be null.
		static IntPtr _GetShellIcon(bool usePidl, string file, APidl pidl, int size, bool freePidl = false)
		{
			//info:
			//	We support everything that can have icon - path, URL, protocol (eg "http:"), file extension (eg ".txt"), shell item parsing name (eg "::{CLSID}"), "shell:AppsFolder\WinStoreAppId".
			//	We call PidlFromString here and pass it to SHGetFileInfo. It makes faster when using thread pool, because multiple threads can call PidlFromString (slow) simultaneously.
			//	PidlFromString does not support file extension. SHGetFileInfo does not support URL and protocol, unless used PIDL.
			//	SHGetFileInfo gets the most correct icons, but only of standard sizes, which also depends on DPI and don't know what.
			//	IExtractIcon for some types fails or gets wrong icon. Even cannot use it to get correct-size icons, because for most file types it uses system imagelists, which are DPI-dependent.
			//	SHMapPIDLToSystemImageListIndex+SHGetImageList also is not better.

			var pidl2 = pidl?.UnsafePtr ?? default;
			if(usePidl) {
				if(pidl2 == default) {
					pidl2 = APidl.FromString_(file);
					if(pidl2 == default) usePidl = false; else freePidl = true;
				}
			}

			if(!usePidl) {
				Debug.Assert(pidl2 == default && file != null);
				pidl2 = Marshal.StringToCoTaskMemUni(file);
				freePidl = true;
			}

			if(pidl2 == default) return default;

			//This is faster but fails with some files etc, randomly with others.
			//It means that shell API and/or extensions are not thread-safe, even if can run in MTA.
			//return _GetShellIcon2(pidl2, size, usePidl);

			IntPtr R = default;
			try {
				if(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
					R = _GetShellIcon2(usePidl, pidl2, size);
				} else {
					//tested: switching thread does not make slower. The speed depends mostly on locking, because then thread pool threads must wait.
					using var work = ThreadPoolSTA_.CreateWork(null, o => { R = _GetShellIcon2(usePidl, pidl2, size); });
					work.Submit();
					work.Wait();
				}
			}
			finally { if(freePidl) Marshal.FreeCoTaskMem(pidl2); }
			GC.KeepAlive(pidl);
			return R;
		}

		static IntPtr _GetShellIcon2(bool isPidl, IntPtr pidl, int size)
		{
			IntPtr R = default, il = default; int index = -1, ilIndex, realSize;

			if(size < (realSize = SizeSmall) * 5 / 4) ilIndex = Api.SHIL_SMALL;
			else if(size < (realSize = SizeLarge) * 5 / 4) ilIndex = Api.SHIL_LARGE;
			else if(size < 256) {
				ilIndex = Api.SHIL_EXTRALARGE; realSize = _SizeExtraLarge;
				//info: cannot resize from 256 because GetIcon(SHIL_JUMBO) gives 48 icon if 256 icon unavailable. Getting real icon size is either impossible or quite difficult and slow (not tested).
			} else { ilIndex = Api.SHIL_JUMBO; realSize = 256; }

			//Need to lock this part, or randomly fails with some file types.
			lock("TK6Z4XiSxkGSfC14/or5Mw") {
				try {
					uint fl = Api.SHGFI_SYSICONINDEX | Api.SHGFI_SHELLICONSIZE;
					if(ilIndex == Api.SHIL_SMALL) fl |= Api.SHGFI_SMALLICON;
					if(isPidl) fl |= Api.SHGFI_PIDL; else fl |= Api.SHGFI_USEFILEATTRIBUTES;
					il = Api.SHGetFileInfo(pidl, 0, out var x, Api.SizeOf<Api.SHFILEINFO>(), fl);
					if(il != default) index = x.iIcon;
					//Marshal.Release(il); //undocumented, but without it IImageList refcount grows. Probably it's ok, because it is static, never deleted until process exits.
				}
				catch { ADebug.Print("exception"); }
				//Shell extensions may throw.
				//By default .NET does not allow to handle eg access violation exceptions.
				//	Previously we would add [HandleProcessCorruptedStateExceptions], but Core ignores it.
				//	Now our AppHost sets environment variable COMPlus_legacyCorruptedStateExceptionsPolicy=1 before loading runtime.
				//	Or could move the API call to the C++ dll.
			}
			if(index < 0) return default;

			//note: Getting icon from imagelist must be in STA thread too, else fails with some file types.
			//tested: This part works without locking. Using another lock here makes slower.

			try {
				if(ilIndex == Api.SHIL_SMALL || ilIndex == Api.SHIL_LARGE || _GetShellImageList(ilIndex, out il)) {
					//AOutput.Write(il, ADebug.GetComObjRefCount(il));
					R = Api.ImageList_GetIcon(il, index, 0);
					if(size != realSize && R != default) {
						//AOutput.Write(size, realSize, index, file);
						R = Api.CopyImage(R, Api.IMAGE_ICON, size, size, Api.LR_COPYDELETEORG | Api.LR_COPYRETURNORG);
					}
				}
			}
			catch(Exception e) { ADebug.Print(e.Message); }
			//finally { if(il != default) Marshal.Release(il); }
			return R;

			static bool _GetShellImageList(int ilIndex, out IntPtr R)
			{
				lock("vK6Z4XiSxkGSfC14/or5Mw") { //the API fails if called simultaneously by multiple threads
					if(0 == Api.SHGetImageList(ilIndex, Api.IID_IImageList, out R)) return true;
				}
				Debug.Assert(false);
				return false;
			}
		}

		//Gets shortcut (.lnk) icon.
		//Much faster than other shell API.
		//Also gets correct icon where iextracticon fails and/or shgetfileinfo gets blank document icon, don't know why.
		//Usually fails only when target does not exist. Then iextracticon also fails, and shgetfileinfo gets blank document icon.
		//If fails, returns default(IntPtr). No exceptions.
		static IntPtr _GetLnkIcon(string file, int size)
		{
			try {
				using var x = AShortcutFile.Open(file);
				var s = x.GetIconLocation(out int ii); if(s != null) return LoadIconHandle(s, ii, size);
				s = x.TargetPathRawMSI; if(s != null) return GetFileIconHandle(s, size, IconGetFlags.DontSearch);
				//AOutput.Write("need IDList", file);
				using(var pidl = x.TargetPidl) return GetPidlIconHandle(pidl, size);
			}
			catch { return default; }
		}

		/// <summary>
		/// Extracts icon from file that contains it.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		/// <param name="file">.ico, .exe, .dll or other file that contains one or more icons. Also supports cursor files - .cur, .ani. Must be full path, without icon index. Supports environment variables (see <see cref="APath.ExpandEnvVar"/>).</param>
		/// <param name="index">Icon index or negative icon resource id in the .exe/.dll file.</param>
		/// <param name="size">Icon width and height. If 0 (default), uses <see cref="SizeSmall"/>.</param>
		public static unsafe Icon LoadIcon(string file, int index = 0, int size = 0)
			=> HandleToIcon(LoadIconHandle(file, index, size), true);

		/// <summary>
		/// Extracts icon from file that contains it.
		/// More info: <see cref="LoadIcon"/>.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		public static unsafe Bitmap LoadIconImage(string file, int index = 0, int size = 0)
			=> HandleToImage(LoadIconHandle(file, index, size), true);

		/// <summary>
		/// Extracts icon from file that contains it.
		/// More info: <see cref="LoadIcon"/>.
		/// </summary>
		/// <returns>Returns icon handle, or default(IntPtr) if failed. Later call <see cref="DestroyIconHandle"/> or some <b>HandleToX</b> function that will destroy it.</returns>
		public static unsafe IntPtr LoadIconHandle(string file, int index = 0, int size = 0)
		{
			if(file.NE()) return default;
			size = _NormalizeIconSizeArgument(size);

			//We use SHDefExtractIcon because of better quality resizing (although several times slower) which matches shell and imagelist resizing.
			//With .ico it matches LoadImage speed (without resizing). PrivateExtractIcons is slightly slower.

			IntPtr R = default;
			int hr = Api.SHDefExtractIcon(file, index, 0, &R, null, size);
			if(hr != 0) return default;
			return R;

			//if(Api.PrivateExtractIcons(file, index, size, size, out R, default, 1, 0) != 1) return default;
		}

		/// <summary>
		/// Gets a shell stock icon.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		/// <param name="icon">Shell stock icon id.</param>
		/// <param name="size">Icon width and height. If 0 (default), uses <see cref="SizeSmall"/>.</param>
		public static Icon GetStockIcon(StockIcon icon, int size = 0)
			=> HandleToIcon(GetStockIconHandle(icon, size), true);

		/// <summary>
		/// Gets a shell stock icon.
		/// More info: <see cref="GetStockIcon"/>.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		public static Bitmap GetStockIconImage(StockIcon icon, int size = 0)
			=> HandleToImage(GetStockIconHandle(icon, size), true);

		/// <summary>
		/// Gets a shell stock icon.
		/// More info: <see cref="GetStockIcon"/>.
		/// </summary>
		/// <returns>Returns icon handle, or default(IntPtr) if failed. Later call <see cref="DestroyIconHandle"/> or some <b>HandleToX</b> function that will destroy it.</returns>
		public static unsafe IntPtr GetStockIconHandle(StockIcon icon, int size = 0)
		{
			var x = new Api.SHSTOCKICONINFO(); x.cbSize = Api.SizeOf(x);
			if(0 != Api.SHGetStockIconInfo(icon, 0, ref x)) return default;
			var s = new string(x.szPath);
			return LoadIconHandle(s, x.iIcon, size);
			//note: don't cache, because of the limit of handles a process can have. Maybe only exe and document icons; maybe also folder and open folder.

			//tested: always gets 32x32 icon: Api.LoadImage(default, 32516, Api.IMAGE_ICON, 16, 16, Api.LR_SHARED); //OIC_INFORMATION
		}

		/// <summary>
		/// Gets <msdn>IDI_APPLICATION</msdn> icon from unmanaged resources of this program file.
		/// </summary>
		/// <returns>Returns null if there are no icons.</returns>
		/// <param name="size">Icon width and height. If 0 (default), uses <see cref="SizeSmall"/>.</param>
		public static Icon GetAppIcon(int size = 0)
			=> HandleToIcon(GetAppIconHandle(size), false);

		/// <summary>
		/// Gets <msdn>IDI_APPLICATION</msdn> icon from unmanaged resources of this program file.
		/// More info: <see cref="GetAppIcon"/>.
		/// </summary>
		public static Bitmap GetAppIconImage(int size = 0)
			=> HandleToImage(GetAppIconHandle(size), false);

		/// <summary>
		/// Gets <msdn>IDI_APPLICATION</msdn> icon from unmanaged resources of this program file.
		/// More info: <see cref="GetAppIcon"/>.
		/// </summary>
		/// <returns>
		/// Returns native icon handle, or default(IntPtr) if there are no icons.
		/// The icon is cached and protected from destroying, therefore don't need to destroy it, and not error to do it.
		/// </returns>
		public static IntPtr GetAppIconHandle(int size = 0)
		{
			var h = AProcess.ExeModuleHandle;
			if(h == default) return default;
			size = _NormalizeIconSizeArgument(size);
			return Api.LoadImage(h, Api.IDI_APPLICATION, Api.IMAGE_ICON, size, size, Api.LR_SHARED);

			//This is not 100% reliable because the icon id 32512 (IDI_APPLICATION) is undocumented.
			//I could not find a .NET method to get icon directly from native resources of assembly.
			//Could use the resource emumeration API...
			//info: MSDN lies that with LR_SHARED gets a cached icon regardless of size argument. Caches each size separately. Tested on Win 10, 7, XP.
		}

		/// <summary>
		/// Gets icon that is displayed in window title bar and in taskbar button.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		/// <param name="w"></param>
		/// <param name="large">Get large icon. If false, gets small icon.</param>
		/// <remarks>
		/// Icon size depends on DPI (text size) and other Windows settings.
		/// This function can be used with windows of any process.
		/// </remarks>
		public static Icon GetWindowIcon(AWnd w, bool large = false)
			=> HandleToIcon(GetWindowIconHandle(w, large), true);

		/// <summary>
		/// Gets icon that is displayed in window title bar and in taskbar button.
		/// More info: <see cref="GetWindowIcon"/>.
		/// </summary>
		/// <returns>Returns null if failed.</returns>
		public static Bitmap GetWindowIconImage(AWnd w, bool large = false)
			=> HandleToImage(GetWindowIconHandle(w, large), true);

		/// <summary>
		/// Gets icon that is displayed in window title bar and in taskbar button.
		/// More info: <see cref="GetWindowIcon"/>.
		/// </summary>
		/// <returns>Returns icon handle, or default(IntPtr) if failed. Later call <see cref="DestroyIconHandle"/> or some <b>HandleToX</b> function that will destroy it.</returns>
		public static IntPtr GetWindowIconHandle(AWnd w, bool large = false)
		{
			int size = Api.GetSystemMetrics(large ? Api.SM_CXICON : Api.SM_CXSMICON);

			//support Windows Store apps
			if(1 == AWnd.Internal_.GetWindowsStoreAppId(w, out var appId, true)) {
				IntPtr hi = GetFileIconHandle(appId, size, IconGetFlags.DontSearch);
				if(hi != default) return hi;
			}

			bool ok = w.SendTimeout(2000, out LPARAM R, Api.WM_GETICON, large);
			if(R == 0 && ok) w.SendTimeout(2000, out R, Api.WM_GETICON, !large);
			if(R == 0) R = AWnd.More.GetClassLong(w, large ? Native.GCL.HICON : Native.GCL.HICONSM);
			if(R == 0) R = AWnd.More.GetClassLong(w, large ? Native.GCL.HICONSM : Native.GCL.HICON);
			//tested this code with DPI 125%. Small icon of most windows match DPI (20), some 16, some 24.
			//TEST: undocumented API InternalGetWindowIcon.

			//Copy, because will DestroyIcon, also it resizes if need.
			if(R != 0) return Api.CopyImage(R, Api.IMAGE_ICON, size, size, 0);
			return default;
		}

		/// <summary>
		/// Creates icon at run time.
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="drawCallback">Called to draw icon. If null, the icon will be completely transparent.</param>
		public static Icon CreateIcon(int width, int height, Action<Graphics> drawCallback = null)
			=> HandleToIcon(CreateIconHandle(width, height, drawCallback), true);

		/// <summary>
		/// Creates icon at run time.
		/// More info: <see cref="CreateIcon"/>.
		/// </summary>
		/// <returns>Returns native icon handle. Later call <see cref="DestroyIconHandle"/> or some <b>HandleToX</b> function that will destroy it.</returns>
		public static IntPtr CreateIconHandle(int width, int height, Action<Graphics> drawCallback = null)
		{
			if(drawCallback != null) {
				using var b = new Bitmap(width, height);
				using var g = Graphics.FromImage(b);
				g.Clear(Color.Transparent); //optional, new bitmaps are transparent, but it is undocumented, and eg .NET Bitmap.MakeTransparent does it
				drawCallback(g);
				return b.GetHicon();
			} else {
				int nb = AMath.AlignUp(width, 32) / 8 * height;
				var aAnd = new byte[nb]; for(int i = 0; i < nb; i++) aAnd[i] = 0xff;
				var aXor = new byte[nb];
				return Api.CreateIcon(default, width, height, 1, 1, aAnd, aXor);

				//speed: ~20 mcs. About 10 times faster than above. Faster than CopyImage etc.
			}
		}

		/// <summary>
		/// Destroys native icon.
		/// Calls API <msdn>DestroyIcon</msdn>. Does nothing if iconHandle is default(IntPtr).
		/// </summary>
		public static void DestroyIconHandle(IntPtr iconHandle)
		{
			if(iconHandle != default) Api.DestroyIcon(iconHandle);
		}

		/// <summary>
		/// Converts unmanaged icon to <b>Icon</b> object.
		/// Returns null if <i>hIcon</i> is default(IntPtr).
		/// </summary>
		/// <param name="hIcon">Icon handle.</param>
		/// <param name="destroyIcon">If true (default), the returned variable owns the unmanaged icon and destroys it when disposing. If false, the returned variable just uses the unmanaged icon and will not destroy; the caller later should destroy it with <see cref="DestroyIconHandle"/>.</param>
		public static Icon HandleToIcon(IntPtr hIcon, bool destroyIcon = true)
		{
			if(hIcon == default) return null;
			var R = Icon.FromHandle(hIcon);
			if(destroyIcon) LetObjectDestroyIconOrCursor_(R);
			return R;
		}

		internal static void LetObjectDestroyIconOrCursor_(object o)
		{
			var ty = o.GetType(); //Icon or Cursor
			var fi = ty.GetField("_ownHandle", BindingFlags.NonPublic | BindingFlags.Instance); //new Icon code
			if(fi == null) fi = ty.GetField("ownHandle", BindingFlags.NonPublic | BindingFlags.Instance); //Cursor, old Icon code
			Debug.Assert(fi != null);
			fi?.SetValue(o, true);

			//Don't allow to exceed the process handle limit when the program does not dispose them.
			//Default limit for USER and GDI objects is 10000, min 200.
			//Icons are USER objects. Most icons also create 3 GDI objects, some 2. So a process can have max 3333 icons.
			//If GC starts working when pressure is 100 KB, then the number of icons is ~50 and GDI handles ~150.
			//We don't care about icon memory size.
			AGC.AddObjectMemoryPressure(o, 2000);
		}

		/// <summary>
		/// Converts unmanaged icon to <b>Bitmap</b> object and destroys the unmanaged icon.
		/// Returns null if <i>hIcon</i> is default(IntPtr) or if fails to convert.
		/// </summary>
		/// <param name="hIcon">Icon handle.</param>
		/// <param name="destroyIcon">If true (default), destroys the unmanaged icon object.</param>
		public static Bitmap HandleToImage(IntPtr hIcon, bool destroyIcon = true)
		{
			//note: don't use Bitmap.FromHicon. It just calls GdipCreateBitmapFromHICON which does not support alpha etc.

			if(hIcon == default) return null;
			//var perf = APerf.Create();
			Icon ic = Icon.FromHandle(hIcon);
			Bitmap im = null;
			try { im = ic.ToBitmap(); }
			catch(Exception e) { AWarning.Write(e.ToString(), -1); }
			ic.Dispose();
			if(destroyIcon) Api.DestroyIcon(hIcon);
			//perf.NW();
			return im;
		}

		/// <summary>
		/// Converts unmanaged icon to <b>ImageSource</b> object and destroys the unmanaged icon.
		/// Returns null if <i>hIcon</i> is default(IntPtr) or if fails to convert.
		/// </summary>
		/// <param name="hIcon">Icon handle.</param>
		/// <param name="destroyIcon">If true (default), destroys the unmanaged icon object.</param>
		public static System.Windows.Media.ImageSource HandleToImageSource(IntPtr hIcon, bool destroyIcon = true) {
			if (hIcon == default) return null;
			System.Windows.Media.Imaging.BitmapSource source = null;
			try { source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(hIcon, default, default); }
			catch (Exception e) { AWarning.Write(e.ToString(), -1); }
			if(destroyIcon) Api.DestroyIcon(hIcon);
			return source;
		}

		/// <summary>
		/// Parses icon location string.
		/// Returns true if it includes icon index or resource id.
		/// </summary>
		/// <param name="s">Icon location. Can be <c>"path,index"</c> or <c>"path,-id"</c> or just path. Receives path.</param>
		/// <param name="index">Receives the number or 0.</param>
		/// <remarks>Also supports path enclosed in double quotes like <c>"\"path\",index"</c>, and spaces between comma and index like <c>"path, index"</c>.</remarks>
		public static bool ParseIconLocation(ref string s, out int index)
		{
			index = 0;
			if(s.NE()) return false;
			if(s[0] == '\"') s = s.Replace("\"", ""); //can be eg "path",index
			if(s.Length < 3) return false;
			if(!AChar.IsAsciiDigit(s[^1])) return false;
			int i = s.LastIndexOf(','); if(i < 1) return false;
			index = s.ToInt(i + 1, out int e); if(e != s.Length) return false;
			s = s.Remove(i);
			return true;

			//note: API PathParseIconLocation has bugs. Eg splits "path,5moreText". Or from "path, 5" removes ", 5" and returns 0.
		}

		static int _NormalizeIconSizeArgument(int size)
		{
			if(size == 0) return SizeSmall;
			return (uint)size <= 256 ? size : throw new ArgumentOutOfRangeException("size", "Must be 0 - 256");
		}

		/// <summary>
		/// Gets size of small icons displayed in UI.
		/// Depends on DPI; 16 when DPI 100%.
		/// </summary>
		public static int SizeSmall => ADpi.OfThisProcess / 6; //eg 96/6=16

		/// <summary>
		/// Gets size of large icons displayed in UI.
		/// Depends on DPI; 32 when DPI 100%.
		/// </summary>
		public static int SizeLarge => ADpi.OfThisProcess / 3;

		static int _SizeExtraLarge => ADpi.OfThisProcess / 2;

		//tested: shell imagelist icon sizes match these.
		//note: don't use GetSystemMetrics(SM_CXSMICON/SM_CXICON). They are for other purposes, eg window title bar, tray icon. On Win7 they can be different because can be changed in Control Panel. Used by SystemInformation.SmallIconSize etc.
	}
}

namespace Au.Types
{
	/// <summary>
	/// Flags for <see cref="AIcon.GetFileIcon"/> and similar functions.
	/// </summary>
	[Flags]
	public enum IconGetFlags
	{
		/// <summary>
		/// The <i>file</i> argument is literal full path. Don't parse "path,index", don't support ".ext" (file type icon), don't make fully-qualified, etc.
		/// </summary>
		LiteralPath = 1,

		/// <summary>
		/// Don't call <see cref="AFile.SearchPath"/>.
		/// </summary>
		DontSearch = 2,

#if false
		/// <summary>
		/// Scale the specified size according to DPI (text size) specified in Windows Settings.
		/// </summary>
		DpiScale = 4, //rejected. In most cases can use standard-size icons, they are DPI-scaled. Or pass Au.Util.ADpi.ScaleInt(size).

		/// Use shell API for all file types, including exe and ico.
		Shell=8, //rejected because SHGetFileInfo does not get exe icon with shield overlay

		/// <summary>
		/// If file does not exist or fails to get its icon, get common icon for that file type, or default document icon if cannot get common icon.
		/// </summary>
		DefaultIfFails = 16, //rejected. Now for exe/ico/etc is like with shell API: if file exists, gets default icon (exe or document), else returns default(IntPtr).

		/// <summary>
		/// Used only with AsyncIcons class. If the thread pool has spare time, let it convert icon handle to Image object. The callback will receive either handle or Image, it must check both for default(IntPtr) and null. This is to make whole process as fast as possible.
		/// </summary>
		NeedImage = 128, //rejected because with our menu/toolbar almost always makes slower
#endif
	}

#pragma warning disable 1591 //missing XML documentation
	/// <summary>See <msdn>SHSTOCKICONID</msdn>.</summary>
	/// <seealso cref="AIcon.GetStockIcon"/>
	public enum StockIcon
	{
		DOCNOASSOC,
		DOCASSOC,
		APPLICATION,
		FOLDER,
		FOLDEROPEN,
		DRIVE525,
		DRIVE35,
		DRIVEREMOVE,
		DRIVEFIXED,
		DRIVENET,
		DRIVENETDISABLED,
		DRIVECD,
		DRIVERAM,
		WORLD,
		SERVER = 15,
		PRINTER,
		MYNETWORK,
		FIND = 22,
		HELP,
		SHARE = 28,
		LINK,
		SLOWFILE,
		RECYCLER,
		RECYCLERFULL,
		MEDIACDAUDIO = 40,
		LOCK = 47,
		AUTOLIST = 49,
		PRINTERNET,
		SERVERSHARE,
		PRINTERFAX,
		PRINTERFAXNET,
		PRINTERFILE,
		STACK,
		MEDIASVCD,
		STUFFEDFOLDER,
		DRIVEUNKNOWN,
		DRIVEDVD,
		MEDIADVD,
		MEDIADVDRAM,
		MEDIADVDRW,
		MEDIADVDR,
		MEDIADVDROM,
		MEDIACDAUDIOPLUS,
		MEDIACDRW,
		MEDIACDR,
		MEDIACDBURN,
		MEDIABLANKCD,
		MEDIACDROM,
		AUDIOFILES,
		IMAGEFILES,
		VIDEOFILES,
		MIXEDFILES,
		FOLDERBACK,
		FOLDERFRONT,
		SHIELD,
		WARNING,
		INFO,
		ERROR,
		KEY,
		SOFTWARE,
		RENAME,
		DELETE,
		MEDIAAUDIODVD,
		MEDIAMOVIEDVD,
		MEDIAENHANCEDCD,
		MEDIAENHANCEDDVD,
		MEDIAHDDVD,
		MEDIABLURAY,
		MEDIAVCD,
		MEDIADVDPLUSR,
		MEDIADVDPLUSRW,
		DESKTOPPC,
		MOBILEPC,
		USERS,
		MEDIASMARTMEDIA,
		MEDIACOMPACTFLASH,
		DEVICECELLPHONE,
		DEVICECAMERA,
		DEVICEVIDEOCAMERA,
		DEVICEAUDIOPLAYER,
		NETWORKCONNECT,
		INTERNET,
		ZIPFILE,
		SETTINGS,
		DRIVEHDDVD = 132,
		DRIVEBD,
		MEDIAHDDVDROM,
		MEDIAHDDVDR,
		MEDIAHDDVDRAM,
		MEDIABDROM,
		MEDIABDR,
		MEDIABDRE,
		CLUSTEREDDRIVE,
		MAX_ICONS = 181
	}
#pragma warning restore 1591
}
