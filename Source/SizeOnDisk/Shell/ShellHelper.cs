using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SizeOnDisk.Shell
{
    public static class ShellHelper
    {
        private static Dictionary<string, string> associations = new Dictionary<string, string>();

        public static string GetFriendlyName(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;
            if (!associations.ContainsKey(extension))
            {
                string fileType = String.Empty;

                using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey("\\" + extension))
                {
                    if (rk != null)
                    {
                        string applicationType = rk.GetValue(String.Empty, String.Empty).ToString();

                        if (!string.IsNullOrEmpty(applicationType))
                        {
                            using (RegistryKey appTypeKey = Registry.ClassesRoot.OpenSubKey("\\" + applicationType))
                            {
                                if (appTypeKey != null)
                                {
                                    fileType = appTypeKey.GetValue(String.Empty, String.Empty).ToString();
                                }
                            }
                        }
                    }

                    // Couldn't find the file type in the registry. Display some default.
                    if (string.IsNullOrEmpty(fileType))
                    {
                        fileType = extension.Replace(".", String.Empty);
                    }
                }
                if (!associations.ContainsKey(extension))
                    // Cache the association so we don't traverse the registry again
                    associations.Add(extension, fileType);
            }

            return associations[extension];
        }

        public class ShellCommandRoot
        {
            public ShellCommandSoftware Default;
            public IList<ShellCommandSoftware> Softwares = new List<ShellCommandSoftware>();
        }

        public class ShellCommandSoftware
        {
            public string Name;
            public string Id;
            public ShellCommandVerb Default;
            public IList<ShellCommandVerb> Verbs = new List<ShellCommandVerb>();
        }

        public class ShellCommandVerb
        {
            public string Verb;
            public string Command;
        }

        public static ShellCommandRoot GetShellCommands(string path, bool isDirectory)
        {
            ShellCommandRoot result = new ShellCommandRoot();
            if (isDirectory)
            {
                result.Default = new ShellCommandSoftware();
                result.Softwares.Add(result.Default);
                result.Default.Id = "Directory";
                //result.Default.Name = IOHelper.;






            }
            return result;
        }




        public static IEnumerable<(string verb, string appid, string application, bool isdefaultapp, bool isdefaultverb)> GetVerbs(string path, bool isDirectory)
        {
            List<(string verb, string appid, string application, bool isdefaultapp, bool isdefaultverb)> verbs = new List<(string verb, string appid, string application, bool isdefaultapp, bool isdefaultverb)>();
            string ext = Path.GetExtension(path);
            if (isDirectory && string.IsNullOrEmpty(ext))
            {
                RegistryKey appkey = Registry.ClassesRoot.OpenSubKey("Directory");
                if (appkey != null)
                {
                    string application = appkey.GetValue(string.Empty, string.Empty).ToString();
                    if (application == string.Empty)
                    {
                        RegistryKey applicationkey = appkey.OpenSubKey("Application");
                        if (applicationkey != null)
                        {
                            application = applicationkey.GetValue("AppUserModelID", string.Empty).ToString();
                            if (application.Contains('!'))
                            {
                                string[] splits = application.Split('!');
                                application = splits.Last();
                            }
                        }
                    }
                    appkey = appkey.OpenSubKey("Shell");
                    if (appkey != null)
                    {
                        string defaultverb = appkey.GetValue(string.Empty, string.Empty).ToString();
                        if (appkey != null)
                        {
                            defaultverb = appkey.GetValue(string.Empty, string.Empty).ToString();
                            string[] appverbs = appkey.GetSubKeyNames();
                            foreach (string appverb in appverbs)
                            {
                                verbs.Add((appverb, "Directory", application, false, defaultverb == appverb));
                            }
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(ext))
            {
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(ext);
                if (key != null)
                {
                    string defaultApp = key.GetValue(string.Empty, string.Empty).ToString();
                    key = key.OpenSubKey("OpenWithProgids");
                    string[] subvalues = new string[] { };
                    if (key == null)
                        subvalues = new string[] { defaultApp };
                    else
                        subvalues = key.GetValueNames();
                    foreach (string subkey in subvalues)
                    {
                        RegistryKey appkey = Registry.ClassesRoot.OpenSubKey(subkey);
                        if (appkey != null)
                        {
                            string application = appkey.GetValue(string.Empty, string.Empty).ToString();
                            if (application == string.Empty)
                            {
                                RegistryKey applicationkey = appkey.OpenSubKey("Application");
                                if (applicationkey != null)
                                {
                                    application = applicationkey.GetValue("AppUserModelID", string.Empty).ToString();
                                    if (application.Contains('!'))
                                    {
                                        string[] splits = application.Split('!');
                                        application = splits.Last();
                                    }
                                }
                            }
                            appkey = appkey.OpenSubKey("Shell");
                            if (appkey != null)
                            {
                                string defaultverb = appkey.GetValue(string.Empty, string.Empty).ToString();
                                if (appkey != null)
                                {
                                    defaultverb = appkey.GetValue(string.Empty, string.Empty).ToString();
                                    string[] appverbs = appkey.GetSubKeyNames();
                                    foreach (string appverb in appverbs)
                                    {
                                        verbs.Add((appverb, subkey, application, defaultApp == subkey, defaultverb == appverb));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return verbs;
        }





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
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = fileName;
            bool result = processStartInfo.Verbs.SingleOrDefault(T => T.ToLowerInvariant() == verb) != null;
            if (!result && verbReplacementList.ContainsKey(verb))
            {
                verb = verbReplacementList[verb];
                result = processStartInfo.Verbs.SingleOrDefault(T => T.ToLowerInvariant() == verb) != null;
            }
            return result;
        }

        public static void ShellExecute(string fileName, string verb, string parameters, IntPtr ownerWindow)
        {
            SafeNativeMethods.SHELLEXECUTEINFO info = new SafeNativeMethods.SHELLEXECUTEINFO();
            info.hwnd = ownerWindow;
            info.lpVerb = verb;
            info.lpFile = fileName;
            info.lpParameters = parameters;
            info.nShow = SafeNativeMethods.SW_SHOW;
            info.fMask = (uint)SafeNativeMethods.ShellExecuteFlags.SEE_MASK_FLAG_NO_UI
                | (uint)SafeNativeMethods.ShellExecuteFlags.SEE_MASK_UNICODE
                | (!string.IsNullOrWhiteSpace(verb) && (verb != "find") ? (uint)SafeNativeMethods.ShellExecuteFlags.SEE_MASK_INVOKEIDLIST : 0);
            info.cbSize = Marshal.SizeOf(info);
            SafeNativeMethods.ShellExecuteEx(ref info);
            if (info.hInstApp.ToInt64() <= 32)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Invalid operation ({1}) on file {0}", fileName,
                    (SafeNativeMethods.ShellExecuteReturnCodes)info.hInstApp.ToInt32()));
            }
        }

        public static void ShellExecute(string fileName, string verb, IntPtr ownerWindow)
        {
            ShellExecute(fileName, verb, string.Empty, ownerWindow);
        }

        /*public static BitmapSource GetIconAsync(string path, int size = 16, bool thumbnail = false)
        {
            BitmapSource result = null;
            var task = Task.Factory.StartNew(() =>
            {
                result = GetIcon(path, size, thumbnail);
            });
            task.Wait(100);
            return result;
        }*/


        public static BitmapSource GetIcon(string path, int size = 16, bool thumbnail = false, bool cache = false)
        {
            // Create a native shellitem from our path
            Guid guid = new Guid(SafeNativeMethods.IShellItemGuid);
            int retCode = SafeNativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref guid, out SafeNativeMethods.IShellItem nativeShellItem);
            if (retCode < 0)
                throw new Exception("ShellObjectFactoryUnableToCreateItem", Marshal.GetExceptionForHR(retCode));


            SafeNativeMethods.Size nativeSIZE = new SafeNativeMethods.Size();
            nativeSIZE.Width = Convert.ToInt32(size);
            nativeSIZE.Height = Convert.ToInt32(size);

            SafeNativeMethods.SIIGBF options = SafeNativeMethods.SIIGBF.ResizeToFit;
            if (!thumbnail)
                options = SafeNativeMethods.SIIGBF.IconOnly;
            if (cache)
                options |= SafeNativeMethods.SIIGBF.MemoryOnly;

            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                retCode = ((SafeNativeMethods.IShellItemImageFactory)nativeShellItem).GetImage(nativeSIZE, options, out hBitmap);
                if (retCode < 0)
                    return null;
                //throw new Exception("ShellObjectFactoryUnableToCreateItem", Marshal.GetExceptionForHR(retCode));
            }
            finally
            {
                Marshal.ReleaseComObject(nativeShellItem);
            }

            try
            {
                // return a System.Media.Imaging.BitmapSource
                // Use interop to create a BitmapSource from hBitmap.
                BitmapSource returnValue = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                returnValue.Freeze();

                return returnValue;
            }
            finally
            {
                // delete HBitmap to avoid memory leaks
                SafeNativeMethods.DeleteObject(hBitmap);
            }
        }


        #endregion public functions


        #region ShellexecuteEx


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

        public static long? GetCompressedFileSize(string filename)
        {
            SafeNativeMethods.FILE_STANDARD_INFO dirinfo;
            using (SafeFileHandle handle = SafeNativeMethods.OpenHandle(filename))
            {
                if (handle != null)
                {

                    bool result = SafeNativeMethods.GetFileInformationByHandleEx(handle, SafeNativeMethods.FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo, out dirinfo, (uint)Marshal.SizeOf(typeof(SafeNativeMethods.FILE_STANDARD_INFO)));
                    //bool result = SafeNativeMethods.GetFileInformationByHandleEx(handle, SafeNativeMethods.FILE_INFO_BY_HANDLE_CLASS.FileAllocationInfo, out dirinfo, (uint)Marshal.SizeOf(typeof(SafeNativeMethods.FILE_ID_BOTH_DIR_INFO)));
                    int win32Error = Marshal.GetLastWin32Error();
                    if (win32Error != 0)
                        throw new Win32Exception(win32Error);
                    return dirinfo.AllocationSize.ToInt64();
                }
            }
            return null;
        }



        internal static class SafeNativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool GetFileInformationByHandleEx(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS infoClass, out FILE_STANDARD_INFO dirInfo, uint dwBufferSize);

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
            internal struct Size
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


            internal const string IShellItemGuid = "43826D1E-E718-42EE-BC55-A1E261C37BFE";

            [ComImport, Guid(IShellItemGuid), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
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

            [DllImport("shell32.dll", EntryPoint = "ExtractIconW",
                CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
            private static extern IntPtr ExtractIcon(int hInst, string lpszExeFileName, uint nIconIndex);

            [DllImport("shell32.dll", EntryPoint = "DestroyIcon",
                CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
            private static extern bool DestroyIcon(IntPtr hIcon);

            [ComImport()]
            [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IShellItemImageFactory
            {
                [PreserveSig]
                int GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] Size size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
            }

            [DllImport("kernel32.dll")]
            internal static extern int GetFileType(SafeFileHandle handle);

            [SecurityCritical]
            internal static SafeFileHandle OpenHandle(string path)
            {
                /*string fullPathInternal = Path.GetFullPathInternal(path);
                string pathRoot = Path.GetPathRoot(fullPathInternal);
                if (pathRoot == fullPathInternal && (int)pathRoot[1] == (int)Path.VolumeSeparatorChar)
                    throw new ArgumentException(Environment.GetResourceString("Arg_PathIsVolume"));
                FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, Directory.GetDemandDir(fullPathInternal, true), false, false);*/
                //SafeFileHandle file = SafeCreateFile(path, 1073741824, FileShare.Write | FileShare.Delete, (SECURITY_ATTRIBUTES)null, FileMode.Open, 33554432, IntPtr.Zero);
                SafeFileHandle file = SafeCreateFile(path, 0, FileShare.ReadWrite | FileShare.Delete, (SECURITY_ATTRIBUTES)null, FileMode.Open, 0, IntPtr.Zero);
                if (file.IsInvalid)
                    //throw new Win32Exception();
                    return null;
                //__Error.WinIOError(Marshal.GetLastWin32Error(), fullPathInternal);
                return file;
            }

            [SecurityCritical]
            internal static SafeFileHandle SafeCreateFile(
        string lpFileName,
        int dwDesiredAccess,
        FileShare dwShareMode,
        SECURITY_ATTRIBUTES securityAttrs,
        FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile)
            {
                SafeFileHandle file = CreateFile(lpFileName, dwDesiredAccess, dwShareMode, securityAttrs, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
                if (!file.IsInvalid && GetFileType(file) != 1)
                {
                    file.Dispose();
                    throw new NotSupportedException("NotSupported_FileStreamOnNonFiles");
                }
                return file;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
            internal static extern SafeFileHandle CreateFile(
        string lpFileName,
        int dwDesiredAccess,
        FileShare dwShareMode,
        SECURITY_ATTRIBUTES securityAttrs,
        FileMode dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile);


            [StructLayout(LayoutKind.Explicit)]
            internal struct LargeInteger
            {
                [FieldOffset(0)]
                public int Low;
                [FieldOffset(4)]
                public int High;
                [FieldOffset(0)]
                public long QuadPart;

                // use only when QuadPart canot be passed
                public long ToInt64()
                {
                    return ((long)this.High << 32) | (uint)this.Low;
                }

                // just for demonstration
                public static LargeInteger FromInt64(long value)
                {
                    return new LargeInteger
                    {
                        Low = (int)(value),
                        High = (int)((value >> 32))
                    };
                }

            }

            [StructLayout(LayoutKind.Sequential)]
            internal class SECURITY_ATTRIBUTES
            {
                internal unsafe byte* pSecurityDescriptor = (byte*)null;
                internal int nLength;
                internal int bInheritHandle;
            }


            [StructLayout(LayoutKind.Sequential)]
            internal struct FILE_STANDARD_INFO
            {
                public LargeInteger AllocationSize;
                public LargeInteger EndOfFile;
                public uint NumberOfLinks;
                public bool DeletePending;
                public bool Directory;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct FILE_ID_BOTH_DIR_INFO
            {
                public uint NextEntryOffset;
                public uint FileIndex;
                public LargeInteger CreationTime;
                public LargeInteger LastAccessTime;
                public LargeInteger LastWriteTime;
                public LargeInteger ChangeTime;
                public LargeInteger EndOfFile;
                public LargeInteger AllocationSize;
                public uint FileAttributes;
                public uint FileNameLength;
                public uint EaSize;
                public char ShortNameLength;
                [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 12)]
                public string ShortName;
                public LargeInteger FileId;
                [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 1)]
                public string FileName;
            }

            public enum FILE_INFO_BY_HANDLE_CLASS
            {
                FileBasicInfo = 0,
                FileStandardInfo = 1,
                FileNameInfo = 2,
                FileRenameInfo = 3,
                FileDispositionInfo = 4,
                FileAllocationInfo = 5,
                FileEndOfFileInfo = 6,
                FileStreamInfo = 7,
                FileCompressionInfo = 8,
                FileAttributeTagInfo = 9,
                FileIdBothDirectoryInfo = 10,// 0x0A
                FileIdBothDirectoryRestartInfo = 11, // 0xB
                FileIoPriorityHintInfo = 12, // 0xC
                FileRemoteProtocolInfo = 13, // 0xD
                FileFullDirectoryInfo = 14, // 0xE
                FileFullDirectoryRestartInfo = 15, // 0xF
                FileStorageInfo = 16, // 0x10
                FileAlignmentInfo = 17, // 0x11
                FileIdInfo = 18, // 0x12
                FileIdExtdDirectoryInfo = 19, // 0x13
                FileIdExtdDirectoryRestartInfo = 20, // 0x14
                MaximumFileInfoByHandlesClass
            }


            [Flags]
            internal enum ShellExecuteFlags : uint
            {
                SEE_MASK_CLASSNAME = 0x00000001,        // Use the class name given by the lpClass member. 
                SEE_MASK_CLASSKEY = 0x00000003,     // Use the class key given by the hkeyClass member.
                SEE_MASK_IDLIST = 0x00000004,       // Use the item identifier list given by the lpIDList member. 
                                                    // The lpIDList member must point to an ITEMIDLIST structure.
                SEE_MASK_INVOKEIDLIST = 0x0000000c,     // Use the IContextMenu interface of the selected item's 
                                                        // shortcut menu handler.
                SEE_MASK_ICON = 0x00000010,     // Use the icon given by the hIcon member.
                SEE_MASK_HOTKEY = 0x00000020,       // Use the hot key given by the dwHotKey member.
                SEE_MASK_NOCLOSEPROCESS = 0x00000040,       // Use to indicate that the hProcess member receives the 
                                                            // process handle. 
                SEE_MASK_CONNECTNETDRV = 0x00000080,        // Validate the share and connect to a drive letter.
                SEE_MASK_FLAG_DDEWAIT = 0x00000100,     // Wait for the Dynamic Data Exchange (DDE) conversation to 
                                                        // terminate before returning
                SEE_MASK_DOENVSUBST = 0x00000200,       // Expand any environment variables specified in the string 
                                                        // given by the lpDirectory or lpFile member. 
                SEE_MASK_FLAG_NO_UI = 0x00000400,       // Do not display an error message box if an error occurs. 
                SEE_MASK_UNICODE = 0x00004000,      // Use this flag to indicate a Unicode application.
                SEE_MASK_NO_CONSOLE = 0x00008000,       // Use to create a console for the new process instead of 
                                                        // having it inherit the parent's console.
                SEE_MASK_ASYNCOK = 0x00100000,
                SEE_MASK_HMONITOR = 0x00200000,     // Use this flag when specifying a monitor on 
                                                    // multi-monitor systems.
                SEE_MASK_NOQUERYCLASSSTORE = 0x01000000,
                SEE_MASK_WAITFORINPUTIDLE = 0x02000000,
                SEE_MASK_FLAG_LOG_USAGE = 0x04000000        // Keep track of the number of times this application has 
                                                            // been launched. 
            }

            internal enum ShellExecuteReturnCodes
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

            internal const int SW_SHOW = 5;




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
            internal enum SIIGBF
            {
                ResizeToFit = 0x00,
                BiggerSizeOk = 0x01,
                MemoryOnly = 0x02,
                IconOnly = 0x04,
                ThumbnailOnly = 0x08,
                InCacheOnly = 0x10,
            }


            internal enum SIGDN : uint
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

            internal enum HResult
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

        #endregion ShellexecuteEx


    }



}
