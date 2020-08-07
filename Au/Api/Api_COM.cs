﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Au.Types
{
	static unsafe partial class Api
	{
		internal struct STRRET
		{
			public uint uType;

			[StructLayout(LayoutKind.Explicit)]
			internal struct TYPE_1
			{
				[FieldOffset(0)]
				public IntPtr pOleStr;
				[FieldOffset(0)]
				public uint uOffset;
				[FieldOffset(0)]
				public fixed sbyte cStr[260];
			}
			public TYPE_1 _2;
		}

		internal static Guid IID_IShellFolder = new Guid(0x000214E6, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

		[ComImport, Guid("000214E6-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IShellFolder
		{
			[PreserveSig] int ParseDisplayName(AWnd hwnd, IntPtr pbc, [MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, uint* pchEaten, out IntPtr ppidl, uint* pdwAttributes);
			[PreserveSig] int EnumObjects(AWnd hwnd, uint grfFlags, out IEnumIDList ppenumIDList);
			[PreserveSig] int BindToObject(IntPtr pidl, IntPtr pbc, in Guid riid, out IntPtr ppv);
			[PreserveSig] int BindToStorage(IntPtr pidl, IntPtr pbc, in Guid riid, out IntPtr ppv);
			[PreserveSig] int CompareIDs(LPARAM lParam, IntPtr pidl1, IntPtr pidl2);
			[PreserveSig] int CreateViewObject(AWnd hwndOwner, in Guid riid, out IntPtr ppv);
			[PreserveSig] int GetAttributesOf(uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, ref uint rgfInOut);
			[PreserveSig] //int GetUIObjectOf(AWnd hwndOwner, uint cidl, [MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, in Guid riid, IntPtr rgfReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
			int GetUIObjectOf(AWnd hwndOwner, uint cidl, IntPtr* apidl, in Guid riid, IntPtr rgfReserved, [MarshalAs(UnmanagedType.Interface)] out object ppv);
			[PreserveSig] int GetDisplayNameOf(IntPtr pidl, uint uFlags, out STRRET pName);
			[PreserveSig] int SetNameOf(AWnd hwnd, IntPtr pidl, [MarshalAs(UnmanagedType.LPWStr)] string pszName, uint uFlags, out IntPtr ppidlOut);
		}

		internal static Guid IID_IShellItem = new Guid(0x43826D1E, 0xE718, 0x42EE, 0xBC, 0x55, 0xA1, 0xE2, 0x61, 0xC3, 0x7B, 0xFE);

		[ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IShellItem
		{
			[PreserveSig] int BindToHandler(IntPtr pbc, in Guid bhid, in Guid riid, out IntPtr ppv); //IBindCtx
			[PreserveSig] int GetParent(out IShellItem ppsi);
			[PreserveSig] int GetDisplayName(Native.SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
			[PreserveSig] int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
			[PreserveSig] int Compare(IShellItem psi, uint hint, out int piOrder);
		}

		[ComImport, Guid("000214F2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IEnumIDList
		{
			[PreserveSig] int Next(int celt, [MarshalAs(UnmanagedType.LPArray)] [Out] IntPtr[] rgelt, out int pceltFetched);
			[PreserveSig] int Skip(int celt);
			[PreserveSig] int Reset();
			[PreserveSig] int Clone(out IEnumIDList ppenum);
		}

		//internal const uint GIL_OPENICON = 0x1;
		//internal const uint GIL_FORSHELL = 0x2;
		//internal const uint GIL_ASYNC = 0x20;
		//internal const uint GIL_DEFAULTICON = 0x40;
		//internal const uint GIL_FORSHORTCUT = 0x80;
		//internal const uint GIL_CHECKSHIELD = 0x200;
		//internal const uint GIL_SIMULATEDOC = 0x1;
		//internal const uint GIL_PERINSTANCE = 0x2;
		//internal const uint GIL_PERCLASS = 0x4;
		//internal const uint GIL_NOTFILENAME = 0x8;
		//internal const uint GIL_DONTCACHE = 0x10;
		//internal const uint GIL_SHIELD = 0x200;
		//internal const uint GIL_FORCENOSHIELD = 0x400;

		//internal static Guid IID_IExtractIcon = new Guid(0x000214FA, 0x0000, 0x0000, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

		//[ComImport, Guid("000214fa-0000-0000-c000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		//internal interface IExtractIcon
		//{
		//	[PreserveSig] int GetIconLocation(uint uFlags, [MarshalAs(UnmanagedType.LPArray)] [Out] char[] pszIconFile, int cchMax, out int piIndex, out uint pwFlags);
		//	[PreserveSig] int Extract([MarshalAs(UnmanagedType.LPWStr)] string pszFile, int nIconIndex, IntPtr* phiconLarge, IntPtr* phiconSmall, int nIconSize);
		//}

		[ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IShellLink
		{
			[PreserveSig] int GetPath([MarshalAs(UnmanagedType.LPArray)] [Out] char[] pszFile, int cch, IntPtr pfd = default, uint fFlags = 0);
			[PreserveSig] int GetIDList(out IntPtr ppidl);
			[PreserveSig] int SetIDList(IntPtr pidl);
			[PreserveSig] int GetDescription([MarshalAs(UnmanagedType.LPArray)] [Out] char[] pszName, int cch);
			[PreserveSig] int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
			[PreserveSig] int GetWorkingDirectory([MarshalAs(UnmanagedType.LPArray)] [Out] char[] pszDir, int cch);
			[PreserveSig] int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
			[PreserveSig] int GetArguments([MarshalAs(UnmanagedType.LPArray)] [Out] char[] pszArgs, int cch);
			[PreserveSig] int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
			[PreserveSig] int GetHotkey(out ushort pwHotkey);
			[PreserveSig] int SetHotkey(ushort wHotkey);
			[PreserveSig] int GetShowCmd(out int piShowCmd);
			[PreserveSig] int SetShowCmd(int iShowCmd);
			[PreserveSig] int GetIconLocation([MarshalAs(UnmanagedType.LPArray)] [Out] char[] pszIconPath, int cch, out int piIcon);
			[PreserveSig] int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
			[PreserveSig] int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved = 0);
			[PreserveSig] int Resolve(AWnd hwnd, uint fFlags);
			[PreserveSig] int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);

			//info: default string marshaling in COM interfaces is BSTR, but in this interface strings are LPWSTR. Cannot use plain string and char[].
			//	Instead of [MarshalAs(UnmanagedType.LPArray)] [Out] char[] can be just char*. Then also need fixed when calling.
		}

		[ComImport, Guid("00021401-0000-0000-C000-000000000046"), ClassInterface(ClassInterfaceType.None)]
		internal class ShellLink { }

		[ComImport, Guid("0000010b-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IPersistFile
		{
			// IPersist
			[PreserveSig] int GetClassID(out Guid pClassID);
			// IPersistFile
			[PreserveSig] int IsDirty();
			[PreserveSig] int Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
			[PreserveSig] int Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
			[PreserveSig] int SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
			[PreserveSig] int GetCurFile(out IntPtr ppszFileName);
		}

		//see also VARIANT in Struct.cs
		internal struct PROPVARIANT :IDisposable
		{
			public VARENUM vt; //ushort
			public ushort _u1;
			public uint _u2;
			public LPARAM value;
			public LPARAM value2;

			/// <summary>
			/// Calls PropVariantClear.
			/// </summary>
			public void Dispose()
			{
				PropVariantClear(ref this);
			}
		}

		internal struct PROPERTYKEY
		{
			public Guid fmtid;
			public uint pid;
		}

		internal static Guid IID_IPropertyStore = new Guid(0x886D8EEB, 0x8CF2, 0x4446, 0x8D, 0x02, 0xCD, 0xBA, 0x1D, 0xBD, 0xCF, 0x99);

		[ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IPropertyStore
		{
			[PreserveSig] int GetCount(out int cProps);
			[PreserveSig] int GetAt(int iProp, out PROPERTYKEY pkey);
			[PreserveSig] int GetValue(in PROPERTYKEY key, out PROPVARIANT pv);
			[PreserveSig] int SetValue(in PROPERTYKEY key, ref PROPVARIANT propvar);
			[PreserveSig] int Commit();
		}

		//internal struct IMAGEINFO
		//{
		//	public IntPtr hbmImage;
		//	public IntPtr hbmMask;
		//	public int Unused1;
		//	public int Unused2;
		//	public RECT rcImage;
		//}

		//note: this is used in the lib, even if IImageList isn't.
		internal static Guid IID_IImageList = new Guid(0x46EB5926, 0x582E, 0x4017, 0x9F, 0xDF, 0xE8, 0x99, 0x8D, 0xAA, 0x09, 0x50);

		//[ComImport, Guid("46EB5926-582E-4017-9FDF-E8998DAA0950"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		//internal interface IImageList
		//{
		//	[PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, out int pi);
		//	[PreserveSig] int ReplaceIcon(int i, IntPtr hicon, out int pi);
		//	[PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
		//	[PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
		//	[PreserveSig] int AddMasked(IntPtr hbmImage, uint crMask, out int pi);
		//	[PreserveSig] int Draw(IntPtr pimldp); //ref IMAGELISTDRAWPARAMS
		//	[PreserveSig] int Remove(int i);
		//	[PreserveSig] int GetIcon(int i, uint flags, out IntPtr picon);
		//	[PreserveSig] int GetImageInfo(int i, out IMAGEINFO pImageInfo);
		//	[PreserveSig] int Copy(int iDst, [MarshalAs(UnmanagedType.IUnknown)] Object punkSrc, int iSrc, uint uFlags);
		//	[PreserveSig] int Merge(int i1, [MarshalAs(UnmanagedType.IUnknown)] Object punk2, int i2, int dx, int dy, in Guid riid, out IntPtr ppv);
		//	[PreserveSig] int Clone(in Guid riid, out IntPtr ppv);
		//	[PreserveSig] int GetImageRect(int i, out RECT prc);
		//	[PreserveSig] int GetIconSize(out int cx, out int cy);
		//	[PreserveSig] int SetIconSize(int cx, int cy);
		//	[PreserveSig] int GetImageCount(out int pi);
		//	[PreserveSig] int SetImageCount(int uNewCount);
		//	[PreserveSig] int SetBkColor(uint clrBk, out uint pclr);
		//	[PreserveSig] int GetBkColor(out uint pclr);
		//	[PreserveSig] int BeginDrag(int iTrack, int dxHotspot, int dyHotspot);
		//	[PreserveSig] int EndDrag();
		//	[PreserveSig] int DragEnter(AWnd hwndLock, int x, int y);
		//	[PreserveSig] int DragLeave(AWnd hwndLock);
		//	[PreserveSig] int DragMove(int x, int y);
		//	[PreserveSig] int SetDragCursorImage([MarshalAs(UnmanagedType.IUnknown)] Object punk, int iDrag, int dxHotspot, int dyHotspot);
		//	[PreserveSig] int DragShowNolock([MarshalAs(UnmanagedType.Bool)] bool fShow);
		//	[PreserveSig] int GetDragImage(out POINT ppt, out Point pptHotspot, in Guid riid, out IntPtr ppv);
		//	[PreserveSig] int GetItemFlags(int i, out uint dwFlags);
		//	[PreserveSig] int GetOverlayImage(int iOverlay, out int piIndex);
		//}

		//[ComImport, Guid("00020400-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
		//internal interface IDispatch
		//{
		//}

		//[ComImport, Guid("618736e0-3c3d-11cf-810c-00aa00389b71")]
		//internal interface IAccessible
		//{
		//	[PreserveSig] int get_accParent(out IAccessible a);
		//	int get_accChildCount();
		//	[PreserveSig] int get_accChild(object varChild, out IAccessible a);
		//	string get_accName(object varChild);
		//	string get_accValue(object varChild);
		//	string get_accDescription(object varChild);
		//	object get_accRole(object varChild);
		//	object get_accState(object varChild);
		//	string get_accHelp(object varChild);
		//	int get_accHelpTopic(out string pszHelpFile, object varChild);
		//	string get_accKeyboardShortcut(object varChild);
		//	object get_accFocus();
		//	object get_accSelection();
		//	string get_accDefaultAction(object varChild);
		//	void accSelect(int flagsSelect, object varChild);
		//	void accLocation(out int pxLeft, out int pyTop, out int pcxWidth, out int pcyHeight, object varChild);
		//	object accNavigate(int navDir, object varStart);
		//	object accHitTest(int xLeft, int yTop);
		//	void accDoDefaultAction(object varChild);
		//	void put_accName(object varChild, string szName);
		//	void put_accValue(object varChild, string szValue);

		//	//[PreserveSig] int get_accParent(out IAccessible ppdispParent);
		//	//[PreserveSig] int get_accChildCount(out int pcountChildren);
		//	//[PreserveSig] int get_accChild(object varChild, out IAccessible ppdispChild);
		//	//[PreserveSig] int get_accName(object varChild, char** pszName);
		//	//[PreserveSig] int get_accValue(object varChild, char** pszValue);
		//	//[PreserveSig] int get_accDescription(object varChild, char** pszDescription);
		//	//[PreserveSig] int get_accRole(object varChild, out object pvarRole);
		//	//[PreserveSig] int get_accState(object varChild, out object pvarState);
		//	//[PreserveSig] int get_accHelp(object varChild, char** pszHelp);
		//	//[PreserveSig] int get_accHelpTopic(char** pszHelpFile, object varChild, out int pidTopic);
		//	//[PreserveSig] int get_accKeyboardShortcut(object varChild, char** pszKeyboardShortcut);
		//	//[PreserveSig] int get_accFocus(out object pvarChild);
		//	//[PreserveSig] int get_accSelection(out object pvarChildren);
		//	//[PreserveSig] int get_accDefaultAction(object varChild, char** pszDefaultAction);
		//	//[PreserveSig] int accSelect(int flagsSelect, object varChild);
		//	//[PreserveSig] int accLocation(out int pxLeft, out int pyTop, out int pcxWidth, out int pcyHeight, object varChild);
		//	//[PreserveSig] int accNavigate(int navDir, object varStart, out object pvarEndUpAt);
		//	//[PreserveSig] int accHitTest(int xLeft, int yTop, out object pvarChild);
		//	//[PreserveSig] int accDoDefaultAction(object varChild);
		//	//[PreserveSig] int put_accName(object varChild, string szName);
		//	//[PreserveSig] int put_accValue(object varChild, string szValue);
		//}

		//[ComImport, Guid("00020404-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		//internal interface IEnumVARIANT
		//{
		//	[PreserveSig] int Next(uint celt, ref object rgVar, ref uint pCeltFetched);
		//	[PreserveSig] int Skip(uint celt);
		//	[PreserveSig] int Reset();
		//	[PreserveSig] int Clone(out IEnumVARIANT ppEnum);
		//}


		[ComImport, Guid("0f87369f-a4e5-4cfc-bd3e-73e6154572dd"), ClassInterface(ClassInterfaceType.None)]
		internal class TaskScheduler { }

		[ComImport, Guid("2faba4c7-4da9-4013-9697-20cc3fd40f85")]
		internal interface ITaskService
		{
			ITaskFolder GetFolder(string path);
			//[PreserveSig] int GetRunningTasks(int flags, out IRunningTaskCollection ppRunningTasks);
			//[PreserveSig] int NewTask(uint flags, out ITaskDefinition ppDefinition);
			[PreserveSig] int _1();
			[PreserveSig] int _2();
			void Connect(object serverName = null, object user = null, object domain = null, object password = null);
			//[PreserveSig] int get_Connected(out short pConnected);
			//[PreserveSig] int get_TargetServer(char** pServer);
			//[PreserveSig] int get_ConnectedUser(char** pUser);
			//[PreserveSig] int get_ConnectedDomain(char** pDomain);
			//[PreserveSig] int get_HighestVersion(out uint pVersion);
		}

		[ComImport, Guid("8cfac062-a080-4c15-9a88-aa7c2af80dfc")]
		internal interface ITaskFolder
		{
			//[PreserveSig] int get_Name(char** pName);
			//[PreserveSig] int get_Path(char** pPath);
			//[PreserveSig] int GetFolder(string path, out ITaskFolder ppFolder);
			//[PreserveSig] int GetFolders(int flags, out ITaskFolderCollection ppFolders);
			[PreserveSig] int _0();
			[PreserveSig] int _1();
			[PreserveSig] int _2();
			[PreserveSig] int _3();
			ITaskFolder CreateFolder(string subFolderName, object sddl);
			//[PreserveSig] int DeleteFolder(string subFolderName, int flags);
			[PreserveSig] int _5();
			IRegisteredTask GetTask(string path);
			//[PreserveSig] int GetTasks(int flags, out IRegisteredTaskCollection ppTasks);
			[PreserveSig] int _7();
			int DeleteTask(string name, int flags);
			IRegisteredTask RegisterTask(string path, string xmlText, TASK_CREATION flags, object userId, object password, TASK_LOGON_TYPE logonType, object sddl);
			//[PreserveSig] int RegisterTaskDefinition(string path, ITaskDefinition pDefinition, int flags, object userId, object password, TASK_LOGON_TYPE logonType, object sddl, out IRegisteredTask ppTask);
			//[PreserveSig] int GetSecurityDescriptor(int securityInformation, char** pSddl);
			//[PreserveSig] int SetSecurityDescriptor(string sddl, int flags);
		}

		[ComImport, Guid("9c86f320-dee3-4dd1-b972-a303f26b061e")]
		internal interface IRegisteredTask
		{
			//[PreserveSig] int get_Name(char** pName);
			//[PreserveSig] int get_Path(char** pPath);
			//[PreserveSig] int get_State(out TASK_STATE pState);
			//[PreserveSig] int get_Enabled(out short pEnabled);
			//[PreserveSig] int put_Enabled(short enabled);
			[PreserveSig] int _0();
			[PreserveSig] int _1();
			[PreserveSig] int _2();
			[PreserveSig] int _3();
			[PreserveSig] int _4();
			IRunningTask Run(object @params=null);
			//[PreserveSig] int RunEx(object @params, int flags, int sessionID, string user, out IRunningTask ppRunningTask);
			//[PreserveSig] int GetInstances(int flags, out IRunningTaskCollection ppRunningTasks);
			//[PreserveSig] int get_LastRunTime(out DateTime pLastRunTime);
			//[PreserveSig] int get_LastTaskResult(out int pLastTaskResult);
			//[PreserveSig] int get_NumberOfMissedRuns(out int pNumberOfMissedRuns);
			//[PreserveSig] int get_NextRunTime(out DateTime pNextRunTime);
			//[PreserveSig] int get_Definition(out ITaskDefinition ppDefinition);
			//[PreserveSig] int get_Xml(char** pXml);
			//[PreserveSig] int GetSecurityDescriptor(int securityInformation, char** pSddl);
			//[PreserveSig] int SetSecurityDescriptor(string sddl, int flags);
			//[PreserveSig] int Stop(int flags);
			//[PreserveSig] int GetRunTimes(in SYSTEMTIME pstStart, in SYSTEMTIME pstEnd, ref uint pCount, out SYSTEMTIME* pRunTimes);
		}

		[ComImport, Guid("653758fb-7b9a-4f1e-a471-beeb8e9b834e")]
		internal interface IRunningTask
		{
			//[PreserveSig] int get_Name(char** pName);
			//[PreserveSig] int get_InstanceGuid(char** pGuid);
			//[PreserveSig] int get_Path(char** pPath);
			//[PreserveSig] int get_State(out TASK_STATE pState);
			//[PreserveSig] int get_CurrentAction(char** pName);
			//[PreserveSig] int Stop();
			//[PreserveSig] int Refresh();
			[PreserveSig] int _0();
			[PreserveSig] int _1();
			[PreserveSig] int _2();
			[PreserveSig] int _3();
			[PreserveSig] int _4();
			[PreserveSig] int _5();
			[PreserveSig] int _6();
			[PreserveSig] int get_EnginePID(out int pPID);
		}

		[Flags]
		internal enum TASK_CREATION :uint
		{
			TASK_VALIDATE_ONLY = 0x1,
			TASK_CREATE,
			TASK_UPDATE = 0x4,
			TASK_CREATE_OR_UPDATE = 0x6,
			TASK_DISABLE = 0x8,
			TASK_DONT_ADD_PRINCIPAL_ACE = 0x10,
			TASK_IGNORE_REGISTRATION_TRIGGERS = 0x20
		}

		internal enum TASK_LOGON_TYPE
		{
			TASK_LOGON_NONE,
			TASK_LOGON_PASSWORD,
			TASK_LOGON_S4U,
			TASK_LOGON_INTERACTIVE_TOKEN,
			TASK_LOGON_GROUP,
			TASK_LOGON_SERVICE_ACCOUNT,
			TASK_LOGON_INTERACTIVE_TOKEN_OR_PASSWORD
		}

		//[ComImport, Guid("6a67614b-6828-4fec-aa54-6d52e8f1f2db")]
		//internal interface IRunningTaskCollection
		//{
		//	[PreserveSig] int get_Count(out int pCount);
		//	[PreserveSig] int get_Item(object index, out IRunningTask ppRunningTask);
		//	[PreserveSig] int get__NewEnum([MarshalAs(UnmanagedType.IUnknown)] out Object ppEnum);
		//}

		//[ComImport, Guid("f5bc8fc5-536d-4f77-b852-fbc1356fdeb6")]
		//internal interface ITaskDefinition
		//{
		//	[PreserveSig] int get_RegistrationInfo(ref IRegistrationInfo ppRegistrationInfo);
		//	[PreserveSig] int put_RegistrationInfo(IRegistrationInfo pRegistrationInfo);
		//	[PreserveSig] int get_Triggers(ref ITriggerCollection ppTriggers);
		//	[PreserveSig] int put_Triggers(ITriggerCollection pTriggers);
		//	[PreserveSig] int get_Settings(ref ITaskSettings ppSettings);
		//	[PreserveSig] int put_Settings(ITaskSettings pSettings);
		//	[PreserveSig] int get_Data(char** pData);
		//	[PreserveSig] int put_Data(string data);
		//	[PreserveSig] int get_Principal(ref IPrincipal ppPrincipal);
		//	[PreserveSig] int put_Principal(IPrincipal pPrincipal);
		//	[PreserveSig] int get_Actions(ref IActionCollection ppActions);
		//	[PreserveSig] int put_Actions(IActionCollection pActions);
		//	[PreserveSig] int get_XmlText(char** pXml);
		//	[PreserveSig] int put_XmlText(string xml);
		//}

		//[ComImport, Guid("79184a66-8664-423f-97f1-637356a5d812")]
		//internal interface ITaskFolderCollection
		//{
		//	[PreserveSig] int get_Count(out int pCount);
		//	[PreserveSig] int get_Item(object index, out ITaskFolder ppFolder);
		//	[PreserveSig] int get__NewEnum([MarshalAs(UnmanagedType.IUnknown)] out Object ppEnum);
		//}

		//[ComImport, Guid("86627eb4-42a7-41e4-a4d9-ac33a72f2d52")]
		//internal interface IRegisteredTaskCollection
		//{
		//	[PreserveSig] int get_Count(out int pCount);
		//	[PreserveSig] int get_Item(object index, out IRegisteredTask ppRegisteredTask);
		//	[PreserveSig] int get__NewEnum([MarshalAs(UnmanagedType.IUnknown)] out Object ppEnum);
		//}

		[ComImport, Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IVirtualDesktopManager
		{
			[PreserveSig] int IsWindowOnCurrentVirtualDesktop(AWnd topLevelWindow, [MarshalAs(UnmanagedType.Bool)] out bool onCurrentDesktop);
			[PreserveSig] int GetWindowDesktopId(AWnd topLevelWindow, out Guid desktopId);
			[PreserveSig] int MoveWindowToDesktop(AWnd topLevelWindow, in Guid desktopId);
		}

		[ComImport, Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a"), ClassInterface(ClassInterfaceType.None)]
		internal class VirtualDesktopManager { }
	}
}
