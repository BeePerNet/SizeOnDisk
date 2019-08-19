using System;
using System.Diagnostics.CodeAnalysis;

namespace SizeOnDisk.Shell
{
    public static partial class ShellHelper
    {
        private enum ActivateOptions
        {
            None = 0x00000000,  // No flags set
            DesignMode = 0x00000001,  // The application is being activated for design mode, and thus will not be able to
                                      // to create an immersive window. Window creation must be done by design tools which
                                      // load the necessary components by communicating with a designer-specified service on
                                      // the site chain established on the activation manager.  The splash screen normally
                                      // shown when an application is activated will also not appear.  Most activations
                                      // will not use this flag.
            NoErrorUI = 0x00000002,  // Do not show an error dialog if the app fails to activate.                                
            NoSplashScreen = 0x00000004,  // Do not show the splash screen when activating the app.
        }
        private enum GETPROPERTYSTOREFLAGS
        {
            GPS_DEFAULT = 0,
            GPS_HANDLERPROPERTIESONLY = 0x1,
            GPS_READWRITE = 0x2,
            GPS_TEMPORARY = 0x4,
            GPS_FASTPROPERTIESONLY = 0x8,
            GPS_OPENSLOWITEM = 0x10,
            GPS_DELAYCREATION = 0x20,
            GPS_BESTEFFORT = 0x40,
            GPS_NO_OPLOCK = 0x80,
            GPS_PREFERQUERYPROPERTIES = 0x100,
            GPS_EXTRINSICPROPERTIES = 0x200,
            GPS_EXTRINSICPROPERTIESONLY = 0x400,
            GPS_MASK_VALID = 0x7FF
        }

        private enum SIATTRIBFLAGS
        {
            AND = 0x1,
            OR = 0x2,
            APPCOMPAT = 0x3,
            MASK = 0x3,
            ALLITEMS = 0x4000
        }

        /// <summary>
        /// Possible flags for the SHFileOperation method.
        /// </summary>
        [Flags]
        private enum FileOperationFlags : ushort
        {
            /// <summary>
            /// Do not show a dialog during the process
            /// </summary>
            FOF_SILENT = 0x0004,
            /// <summary>
            /// Do not ask the user to confirm selection
            /// </summary>
            FOF_NOCONFIRMATION = 0x0010,
            /// <summary>
            /// Delete the file to the recycle bin.  (Required flag to send a file to the bin
            /// </summary>
            FOF_ALLOWUNDO = 0x0040,
            /// <summary>
            /// Do not show the names of the files or folders that are being recycled.
            /// </summary>
            FOF_SIMPLEPROGRESS = 0x0100,
            /// <summary>
            /// Surpress errors, if any occur during the process.
            /// </summary>
            FOF_NOERRORUI = 0x0400,
            /// <summary>
            /// Warn if files are too big to fit in the recycle bin and will need
            /// to be deleted completely.
            /// </summary>
            FOF_WANTNUKEWARNING = 0x4000,
        }

        /// <summary>
        /// File Operation Function Type for SHFileOperation
        /// </summary>
        private enum FileOperationType : uint
        {
            /// <summary>
            /// Move the objects
            /// </summary>
            FO_MOVE = 0x0001,
            /// <summary>
            /// Copy the objects
            /// </summary>
            FO_COPY = 0x0002,
            /// <summary>
            /// Delete (or recycle) the objects
            /// </summary>
            FO_DELETE = 0x0003,
            /// <summary>
            /// Rename the object(s)
            /// </summary>
            FO_RENAME = 0x0004,
        }


        [Flags]
        private enum AssocF : int
        {
            None = 0,
            InitNoRemapClsid = 0x1,
            InitByExeName = 0x2,
            OpenByExeName = 0x2,
            InitDefaultToStar = 0x4,
            InitDefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200,
            InitIgnoreUnknown = 0x400,
            InitFixedProgId = 0x800,
            IsProtocol = 0x1000,
            InitForFile = 0x2000,
        }

        private enum AssocStr
        {
            None = 0,
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic,
            InfoTip,
            QuickTip,
            TileInfo,
            ContentType,
            DefaultIcon,
            ShellExtension,
            DropTarget,
            DelegateExecute,
            SupportedUriProtocols,
            // The values below ('Max' excluded) have been introduced in W10 1511
            ProgId,
            AppId,
            AppPublisher,
            AppIconReference,
            Max
        }

        [SuppressMessage("Usage", "CA2217:Do not mark enums with FlagsAttribute")]
        [SuppressMessage("Design", "CA1008:Enums should have zero value")]
        [Flags]
        private enum ShellExecuteFlags : int
        {
            DEFAULT = 0x00000000,
            CLASSNAME = 0x00000001,
            CLASSKEY = 0x00000003,
            IDLIST = 0x00000004,
            INVOKEIDLIST = 0x0000000c,   // Note SEE_MASK_INVOKEIDLIST(0xC) implies SEE_MASK_IDLIST(0x04)
            HOTKEY = 0x00000020,
            NOCLOSEPROCESS = 0x00000040,
            CONNECTNETDRV = 0x00000080,
            NOASYNC = 0x00000100,
            DDEWAIT = NOASYNC,
            DOENVSUBST = 0x00000200,
            NOUI = 0x00000400,
            UNICODE = 0x00004000,
            NOCONSOLE = 0x00008000,
            ASYNCOK = 0x00100000,
            HMONITOR = 0x00200000,
            NOZONECHECKS = 0x00800000,
            NOQUERYCLASSSTORE = 0x01000000,
            WAITFORINPUTIDLE = 0x02000000,
            LOGUSAGE = 0x04000000,
        }



        [Flags()]
        private enum SLGP_FLAGS
        {
            /// <summary>Retrieves the standard short (8.3 format) file name</summary>
            SLGP_SHORTPATH = 0x1,
            /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
            SLGP_UNCPRIORITY = 0x2,
            /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
            SLGP_RAWPATH = 0x4
        }

        [Flags()]
        private enum SLR_FLAGS
        {
            /// <summary>
            /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
            /// the high-order word of fFlags can be set to a time-out value that specifies the
            /// maximum amount of time to be spent resolving the link. The function returns if the
            /// link cannot be resolved within the time-out duration. If the high-order word is set
            /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
            /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
            /// duration, in milliseconds.
            /// </summary>
            SLR_NO_UI = 0x1,
            /// <summary>Obsolete and no longer used</summary>
            SLR_ANY_MATCH = 0x2,
            /// <summary>If the link object has changed, update its path and list of identifiers.
            /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
            /// whether or not the link object has changed.</summary>
            SLR_UPDATE = 0x4,
            /// <summary>Do not update the link information</summary>
            SLR_NOUPDATE = 0x8,
            /// <summary>Do not execute the search heuristics</summary>
            SLR_NOSEARCH = 0x10,
            /// <summary>Do not use distributed link tracking</summary>
            SLR_NOTRACK = 0x20,
            /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
            /// removable media across multiple devices based on the volume name. It also uses the
            /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
            /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
            SLR_NOLINKINFO = 0x40,
            /// <summary>Call the Microsoft Windows Installer</summary>
            SLR_INVOKE_MSI = 0x80
        }
        private enum ShowCommands : int
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }


        private enum ShellExecuteReturnCodes
        {
            ERROR_OUT_OF_MEMORY = 0,    // The operating system is out of memory or resources.
            ERROR_FILE_NOT_FOUND = 2,   // The specified file was not found. 
            ERROR_PATH_NOT_FOUND = 3,   // The specified path was not found. 
            ERROR_BAD_FORMAT = 11,  // The .exe file is invalid (non-Microsoft Win32® .exe or error in .exe image). 
            SE_ERR_ACCESSDENIED = 5,    // The operating system denied access to the specified file.  
            SE_ERR_ASSOCINCOMPLETE = 27,    // The file name association is incomplete or invalid. 
            SE_ERR_DDEBUSY = 30,    // The Dynamic Data Exchange (DDE) transaction could not be completed because other DDE transactions were being processed. 
            SE_ERR_DDEFAIL = 29,    // The DDE transaction failed. 
            SE_ERR_DDETIMEOUT = 28, // The DDE transaction could not be completed because the request timed out. 
            SE_ERR_DLLNOTFOUND = 32,    // The specified dynamic-link library (DLL) was not found.  
            SE_ERR_FNF = 2, // The specified file was not found.  
            SE_ERR_NOASSOC = 31,    // There is no application associated with the given file name extension. This error will also be returned if you attempt to print a file that is not printable. 
            SE_ERR_OOM = 8, // There was not enough memory to complete the operation. 
            SE_ERR_PNF = 3, // The specified path was not found. 
            SE_ERR_SHARE = 26,  // A sharing violation occurred. 
        }




        [Flags]
        private enum FileOpenOptions
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
        private enum SIIGBF
        {
            ResizeToFit = 0x00,
            BiggerSizeOk = 0x01,
            MemoryOnly = 0x02,
            IconOnly = 0x04,
            ThumbnailOnly = 0x08,
            InCacheOnly = 0x10,
        }


        private enum SIGDN : uint
        {
            NORMALDISPLAY = 0x00000000,
            PARENTRELATIVEPARSING = 0x80018001,
            DESKTOPABSOLUTEPARSING = 0x80028000,
            PARENTRELATIVEEDITING = 0x80031001,
            DESKTOPABSOLUTEEDITING = 0x8004c000,
            FILESYSPATH = 0x80058000,
            URL = 0x80068000,
            PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
            PARENTRELATIVE = 0x80080001,
            PARENTRELATIVEFORUI = 0x80094001
        }

        private enum HResult
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
}
