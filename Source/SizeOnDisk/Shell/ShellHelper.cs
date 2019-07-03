using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;

namespace SizeOnDisk.Shell
{
    public static class ShellHelper
    {
        private readonly static Dictionary<string, string> verbReplacementList;

        [SuppressMessage("Microsoft.Performance", "CA1810")]
        static ShellHelper()
        {
            verbReplacementList = new Dictionary<string, string>();
            verbReplacementList.Add("print", "printto");
        }

        #region public functions

        [SuppressMessage("Microsoft.Globalization", "CA1308")]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Assertion)]
        public static bool CanCallShellCommand(string fileName, string verb)
        {
            if (string.IsNullOrWhiteSpace(verb))
                return false;
            verb = verb.ToLowerInvariant();
            if (verbReplacementList.ContainsKey(verb))
                verb = verbReplacementList[verb];
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = fileName;
            return processStartInfo.Verbs.SingleOrDefault(T => T.ToLowerInvariant() == verb) != null;
        }

        public static void ShellExecute(string fileName, string verb, string parameters, IntPtr ownerWindow)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.hwnd = ownerWindow;
            info.lpVerb = verb;
            info.lpFile = fileName;
            info.lpParameters = parameters;
            info.nShow = SW_SHOW;
            info.fMask = (uint)ShellExecuteFlags.SEE_MASK_FLAG_NO_UI
                | (uint)ShellExecuteFlags.SEE_MASK_UNICODE
                | (!string.IsNullOrWhiteSpace(verb) && (verb != "find") ? (uint)ShellExecuteFlags.SEE_MASK_INVOKEIDLIST : 0);
            info.cbSize = Marshal.SizeOf(info);
            ShellExecuteEx(ref info);
            if (info.hInstApp.ToInt64() <= 32)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Invalid operation ({1}) on file {0}", fileName,
                    (ShellExecuteReturnCodes)info.hInstApp.ToInt32()));
            }
        }

        public static void ShellExecute(string fileName, string verb, IntPtr ownerWindow)
        {
            ShellExecute(fileName, verb, string.Empty, ownerWindow);
        }

        #endregion public functions


        #region ShellexecuteEx

        // .NET classes representing runtime callable wrappers.

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string path,
        // The following parameter is not used - binding context.
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);


        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            public string lpVerb;
            public string lpFile;
            public string lpParameters;
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        /// <summary>
        /// A Wrapper for a SIZE struct
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Size
        {
            private int width;
            private int height;

            /// <summary>
            /// Width
            /// </summary>
            public int Width { get { return width; } set { width = value; } }

            /// <summary>
            /// Height
            /// </summary>
            public int Height { get { return height; } set { height = value; } }
        };


        #region COM

        [ComImport, ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate), Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        internal class FileOpenDialogRCW { }


        [ComImport(), Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IFileDialog
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig()]
            uint Show([In, Optional] IntPtr hwndOwner); //IModalWindow 


            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypeIndex([In] uint iFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileTypeIndex(out uint piFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Advise([In, MarshalAs(UnmanagedType.Interface)] IntPtr pfde, out uint pdwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Unadvise([In] uint dwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOptions([In] FileOpenOptions fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetOptions(out FileOpenOptions fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Close([MarshalAs(UnmanagedType.Error)] uint hr);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetClientGuid([In] ref Guid guid);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint ClearClientData();

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        }


        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItem
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint BindToHandler([In] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IntPtr ppvOut);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetDisplayName([In] SIGDN sigdnName, out IntPtr ppszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
        }

        #endregion


        [ComImportAttribute()]
        [GuidAttribute("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(
            [In, MarshalAs(UnmanagedType.Struct)] ShellHelper.Size size,
            [In] SIIGBF flags,
            [Out] out IntPtr phbm);
        }





        [Flags]
        private enum ShellExecuteFlags : uint
        {
            SEE_MASK_CLASSNAME = 0x00000001,		// Use the class name given by the lpClass member. 
            SEE_MASK_CLASSKEY = 0x00000003,		// Use the class key given by the hkeyClass member.
            SEE_MASK_IDLIST = 0x00000004,		// Use the item identifier list given by the lpIDList member. 
            // The lpIDList member must point to an ITEMIDLIST structure.
            SEE_MASK_INVOKEIDLIST = 0x0000000c,		// Use the IContextMenu interface of the selected item's 
            // shortcut menu handler.
            SEE_MASK_ICON = 0x00000010,		// Use the icon given by the hIcon member.
            SEE_MASK_HOTKEY = 0x00000020,		// Use the hot key given by the dwHotKey member.
            SEE_MASK_NOCLOSEPROCESS = 0x00000040,		// Use to indicate that the hProcess member receives the 
            // process handle. 
            SEE_MASK_CONNECTNETDRV = 0x00000080,		// Validate the share and connect to a drive letter.
            SEE_MASK_FLAG_DDEWAIT = 0x00000100,		// Wait for the Dynamic Data Exchange (DDE) conversation to 
            // terminate before returning
            SEE_MASK_DOENVSUBST = 0x00000200,		// Expand any environment variables specified in the string 
            // given by the lpDirectory or lpFile member. 
            SEE_MASK_FLAG_NO_UI = 0x00000400,		// Do not display an error message box if an error occurs. 
            SEE_MASK_UNICODE = 0x00004000,		// Use this flag to indicate a Unicode application.
            SEE_MASK_NO_CONSOLE = 0x00008000,		// Use to create a console for the new process instead of 
            // having it inherit the parent's console.
            SEE_MASK_ASYNCOK = 0x00100000,
            SEE_MASK_HMONITOR = 0x00200000,		// Use this flag when specifying a monitor on 
            // multi-monitor systems.
            SEE_MASK_NOQUERYCLASSSTORE = 0x01000000,
            SEE_MASK_WAITFORINPUTIDLE = 0x02000000,
            SEE_MASK_FLAG_LOG_USAGE = 0x04000000		// Keep track of the number of times this application has 
            // been launched. 
        }

        private enum ShellExecuteReturnCodes
        {
            ERROR_OUT_OF_MEMORY = 0,	// The operating system is out of memory or resources.
            ERROR_FILE_NOT_FOUND = 2,	// The specified file was not found. 
            ERROR_PATH_NOT_FOUND = 3,	// The specified path was not found. 
            ERROR_BAD_FORMAT = 11,	// The .exe file is invalid (non-Microsoft Win32® .exe or error in .exe image). 
            SE_ERR_ACCESSDENIED = 5,	// The operating system denied access to the specified file.  
            SE_ERR_ASSOCINCOMPLETE = 27,	// The file name association is incomplete or invalid. 
            SE_ERR_DDEBUSY = 30,	// The Dynamic Data Exchange (DDE) transaction could not be completed because other DDE transactions were being processed. 
            SE_ERR_DDEFAIL = 29,	// The DDE transaction failed. 
            SE_ERR_DDETIMEOUT = 28,	// The DDE transaction could not be completed because the request timed out. 
            SE_ERR_DLLNOTFOUND = 32,	// The specified dynamic-link library (DLL) was not found.  
            SE_ERR_FNF = 2,	// The specified file was not found.  
            SE_ERR_NOASSOC = 31,	// There is no application associated with the given file name extension. This error will also be returned if you attempt to print a file that is not printable. 
            SE_ERR_OOM = 8,	// There was not enough memory to complete the operation. 
            SE_ERR_PNF = 3,	// The specified path was not found. 
            SE_ERR_SHARE = 26,	// A sharing violation occurred. 
        }

        private const int SW_SHOW = 5;

        #endregion ShellexecuteEx


        /// <summary>
        /// Run process in elevated privilege
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031")]
        //[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Execution)]
        public static void Restart(bool runAsAdministrator)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();

            processStartInfo.FileName = Assembly.GetExecutingAssembly().Location;
            if (runAsAdministrator)
            {
                processStartInfo.Verb = "runas";
            }

            try
            {
                System.Diagnostics.Process.Start(processStartInfo);
                Application.Current.Shutdown(0);
            }
            catch (Win32Exception ex)
            {
                if (!(ex.ErrorCode == -2147467259 && ex.NativeErrorCode == 1223))
                    ExceptionBox.ShowException(ex);
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
            }
        }


        public static class ShellCLSIDGuid
        {

            // CLSID GUID strings for relevant coclasses.
            internal const string FileOpenDialog = "DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7";
            internal const string FileSaveDialog = "C0B4E2F3-BA21-4773-8DBA-335EC946EB8B";
            internal const string KnownFolderManager = "4DF0C730-DF9D-4AE3-9153-AA6B82E9795A";
            internal const string ShellLibrary = "D9B3211D-E57F-4426-AAEF-30A806ADD397";
            internal const string SearchFolderItemFactory = "14010e02-bbbd-41f0-88e3-eda371216584";
            internal const string ConditionFactory = "E03E85B0-7BE3-4000-BA98-6C13DE9FA486";
            internal const string QueryParserManager = "5088B39A-29B4-4d9d-8245-4EE289222F66";
        }


        internal static class ShellIIDGuid
        {

            // IID GUID strings for relevant Shell COM interfaces.
            internal const string IModalWindow = "B4DB1657-70D7-485E-8E3E-6FCB5A5C1802";
            internal const string IFileDialog = "42F85136-DB7E-439C-85F1-E4075D135FC8";
            internal const string IFileOpenDialog = "D57C7288-D4AD-4768-BE02-9D969532D960";
            internal const string IFileSaveDialog = "84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB";
            internal const string IFileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
            internal const string IFileDialogControlEvents = "36116642-D713-4B97-9B83-7484A9D00433";
            internal const string IFileDialogCustomize = "E6FDD21A-163F-4975-9C8C-A69F1BA37034";

            internal const string IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
            internal const string IShellItem2 = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";
            internal const string IShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";
            internal const string IShellLibrary = "11A66EFA-382E-451A-9234-1E0E12EF3085";
            internal const string IThumbnailCache = "F676C15D-596A-4ce2-8234-33996F445DB1";
            internal const string ISharedBitmap = "091162a4-bc96-411f-aae8-c5122cd03363";
            internal const string IShellFolder = "000214E6-0000-0000-C000-000000000046";
            internal const string IShellFolder2 = "93F2F68C-1D1B-11D3-A30E-00C04F79ABD1";
            internal const string IEnumIDList = "000214F2-0000-0000-C000-000000000046";
            internal const string IShellLinkW = "000214F9-0000-0000-C000-000000000046";
            internal const string CShellLink = "00021401-0000-0000-C000-000000000046";

            internal const string IPropertyStore = "886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99";
            internal const string IPropertyStoreCache = "3017056d-9a91-4e90-937d-746c72abbf4f";
            internal const string IPropertyDescription = "6F79D558-3E96-4549-A1D1-7D75D2288814";
            internal const string IPropertyDescription2 = "57D2EDED-5062-400E-B107-5DAE79FE57A6";
            internal const string IPropertyDescriptionList = "1F9FC1D0-C39B-4B26-817F-011967D3440E";
            internal const string IPropertyEnumType = "11E1FBF9-2D56-4A6B-8DB3-7CD193A471F2";
            internal const string IPropertyEnumType2 = "9B6E051C-5DDD-4321-9070-FE2ACB55E794";
            internal const string IPropertyEnumTypeList = "A99400F4-3D84-4557-94BA-1242FB2CC9A6";
            internal const string IPropertyStoreCapabilities = "c8e2d566-186e-4d49-bf41-6909ead56acc";

            internal const string ICondition = "0FC988D4-C935-4b97-A973-46282EA175C8";
            internal const string ISearchFolderItemFactory = "a0ffbc28-5482-4366-be27-3e81e78e06c2";
            internal const string IConditionFactory = "A5EFE073-B16F-474f-9F3E-9F8B497A3E08";
            internal const string IRichChunk = "4FDEF69C-DBC9-454e-9910-B34F3C64B510";
            internal const string IPersistStream = "00000109-0000-0000-C000-000000000046";
            internal const string IPersist = "0000010c-0000-0000-C000-000000000046";
            internal const string IEnumUnknown = "00000100-0000-0000-C000-000000000046";
            internal const string IQuerySolution = "D6EBC66B-8921-4193-AFDD-A1789FB7FF57";
            internal const string IQueryParser = "2EBDEE67-3505-43f8-9946-EA44ABC8E5B0";
            internal const string IQueryParserManager = "A879E3C4-AF77-44fb-8F37-EBD1487CF920";
        }
    }


    [Flags]
    internal enum FileOpenOptions
    {
        OverwritePrompt = 0x00000002,
        StrictFileTypes = 0x00000004,
        NoChangeDirectory = 0x00000008,
        PickFolders = 0x00000020,
        // Ensure that items returned are filesystem items.
        ForceFilesystem = 0x00000040,
        // Allow choosing items that have no storage.
        AllNonStorageItems = 0x00000080,
        NoValidate = 0x00000100,
        AllowMultiSelect = 0x00000200,
        PathMustExist = 0x00000800,
        FileMustExist = 0x00001000,
        CreatePrompt = 0x00002000,
        ShareAware = 0x00004000,
        NoReadOnlyReturn = 0x00008000,
        NoTestFileCreate = 0x00010000,
        HideMruPlaces = 0x00020000,
        HidePinnedPlaces = 0x00040000,
        NoDereferenceLinks = 0x00100000,
        DontAddToRecent = 0x02000000,
        ForceShowHidden = 0x10000000,
        DefaultNoMiniMode = 0x20000000
    }

    [Flags]
    public enum SIIGBF
    {
        ResizeToFit = 0x00,
        BiggerSizeOk = 0x01,
        MemoryOnly = 0x02,
        IconOnly = 0x04,
        ThumbnailOnly = 0x08,
        InCacheOnly = 0x10,
    }


    public enum SIGDN : uint
    {
        SIGDN_NORMALDISPLAY = 0x00000000,
        SIGDN_PARENTRELATIVEPARSING = 0x80018001,
        SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
        SIGDN_PARENTRELATIVEEDITING = 0x80031001,
        SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
        SIGDN_FILESYSPATH = 0x80058000,
        SIGDN_URL = 0x80068000,
        SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
        SIGDN_PARENTRELATIVE = 0x80080001,
        SIGDN_PARENTRELATIVEFORUI = 0x80094001
    }

    public enum HResult
    {
        /// <summary>     
        /// S_OK          
        /// </summary>    
        Ok = 0x0000,

        /// <summary>
        /// S_FALSE
        /// </summary>        
        False = 0x0001,

        /// <summary>
        /// E_INVALIDARG
        /// </summary>
        InvalidArguments = unchecked((int)0x80070057),

        /// <summary>
        /// E_OUTOFMEMORY
        /// </summary>
        OutOfMemory = unchecked((int)0x8007000E),

        /// <summary>
        /// E_NOINTERFACE
        /// </summary>
        NoInterface = unchecked((int)0x80004002),

        /// <summary>
        /// E_FAIL
        /// </summary>
        Fail = unchecked((int)0x80004005),

        /// <summary>
        /// E_ELEMENTNOTFOUND
        /// </summary>
        ElementNotFound = unchecked((int)0x80070490),

        /// <summary>
        /// TYPE_E_ELEMENTNOTFOUND
        /// </summary>
        TypeElementNotFound = unchecked((int)0x8002802B),

        /// <summary>
        /// NO_OBJECT
        /// </summary>
        NoObject = unchecked((int)0x800401E5),

        /// <summary>
        /// Win32 Error code: ERROR_CANCELLED
        /// </summary>
        Win32ErrorCanceled = 1223,

        /// <summary>
        /// ERROR_CANCELLED
        /// </summary>
        Canceled = unchecked((int)0x800704C7),

        /// <summary>
        /// The requested resource is in use
        /// </summary>
        ResourceInUse = unchecked((int)0x800700AA),

        /// <summary>
        /// The requested resources is read-only.
        /// </summary>
        AccessDenied = unchecked((int)0x80030005)
    }

}
