using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace SizeOnDisk.Shell
{
    /// <summary>
    /// Profide all needed Win32Api to bypass the PathTooLongException.
    /// </summary>
    internal static class IOHelper
    {

        internal static class SafeNativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindNextFile(SafeFindHandle hFindFile, [Out] WIN32_FIND_DATA lpFindFileData);


            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindClose(IntPtr hFindFile);


            [DllImport("kernel32.dll")]
            internal static extern int SetErrorMode(int newMode);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetFileAttributesEx(string name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern SafeFindHandle FindFirstFile(string fileName, [In, Out] WIN32_FIND_DATA data);

            [DllImport("kernel32.dll", EntryPoint = "GetCompressedFileSizeW", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern uint GetCompressedFileSize(string lpFileName, out uint lpFileSizeHigh);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetDiskFreeSpace(string lpRootPathName,
               out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
               out uint lpTotalNumberOfClusters);

            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

            private static SHFILEOPSTRUCT GetShellOperationInfo(FileOperationType OperationType, FileOperationFlags OperationFlags, string[] SourcePath)
                => GetShellOperationInfo(OperationType, OperationFlags, SourcePath, null);

            private static string GetShellPath(string[] FullPaths)
            {
                StringBuilder builder = new StringBuilder();
                foreach (string str2 in FullPaths)
                {
                    builder.Append(str2 + "\0");
                }
                return builder.ToString();
            }

            private static SHFILEOPSTRUCT GetShellOperationInfo(FileOperationType OperationType, FileOperationFlags OperationFlags, string[] SourcePaths, string TargetPath)
            {
                SHFILEOPSTRUCT shfileopstruct2 = new SHFILEOPSTRUCT
                {
                    wFunc = OperationType,
                    fFlags = OperationFlags,
                    pFrom = GetShellPath(SourcePaths)
                };
                if (TargetPath == null)
                {
                    shfileopstruct2.pTo = null;
                }
                else
                {
                    shfileopstruct2.pTo = TargetPath;
                }
                shfileopstruct2.hNameMappings = IntPtr.Zero;
                try
                {
                    shfileopstruct2.hwnd = Process.GetCurrentProcess().MainWindowHandle;
                }
                catch (Exception exception)
                {
                    if ((!(exception is SecurityException) && !(exception is InvalidOperationException)) && !(exception is NotSupportedException))
                    {
                        throw;
                    }
                    shfileopstruct2.hwnd = IntPtr.Zero;
                }
                shfileopstruct2.lpszProgressTitle = string.Empty;
                return shfileopstruct2;
            }

            [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

            [HostProtection(SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalProcessMgmt, UI = true)]
            private static bool ShellDeleteOperation(FileOperationFlags OperationFlags, string[] FullSource)
            {
                int num;
                SHFILEOPSTRUCT lpFileOp = GetShellOperationInfo(FileOperationType.FO_DELETE, OperationFlags, FullSource);
                num = SHFileOperation(ref lpFileOp);
                SHChangeNotify(0x2381f, 3, IntPtr.Zero, IntPtr.Zero);
                if (lpFileOp.fAnyOperationsAborted)
                {
                    return false;
                }
                else if (num != 0)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                return true;
            }

            /// <summary>
            /// Send file to recycle bin
            /// </summary>
            /// <param name="path">Location of directory or file to recycle</param>
            /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
            public static bool MoveToRecycleBin(params string[] path)
                => ShellDeleteOperation(FileOperationFlags.FOF_ALLOWUNDO | FileOperationFlags.FOF_WANTNUKEWARNING, path);


            /// <summary>
            /// Send file to recycle bin
            /// </summary>
            /// <param name="path">Location of directory or file to recycle</param>
            /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
            public static bool PermanentDelete(params string[] path)
                => ShellDeleteOperation(0, path);

        }

        /// <summary>
        /// SHFILEOPSTRUCT for SHFileOperation from COM
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct SHFILEOPSTRUCT
        {
            internal IntPtr hwnd;
            internal FileOperationType wFunc;
            [MarshalAs(UnmanagedType.LPTStr)]
            internal string pFrom;
            [MarshalAs(UnmanagedType.LPTStr)]
            internal string pTo;
            internal FileOperationFlags fFlags;
            internal bool fAnyOperationsAborted;
            internal IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPTStr)]
            internal string lpszProgressTitle;
        }


        /// <summary>
        /// Win32Api file attributes structure
        /// </summary>
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct WIN32_FILE_ATTRIBUTE_DATA
        {
            internal int fileAttributes;
            internal uint ftCreationTimeLow;
            internal uint ftCreationTimeHigh;
            internal uint ftLastAccessTimeLow;
            internal uint ftLastAccessTimeHigh;
            internal uint ftLastWriteTimeLow;
            internal uint ftLastWriteTimeHigh;
            internal uint fileSizeHigh;
            internal uint fileSizeLow;
            [SecurityCritical]
            public void PopulateFrom(IOHelper.WIN32_FIND_DATA findData)
            {
                this.fileAttributes = findData.dwFileAttributes;
                this.ftCreationTimeLow = findData.ftCreationTime_dwLowDateTime;
                this.ftCreationTimeHigh = findData.ftCreationTime_dwHighDateTime;
                this.ftLastAccessTimeLow = findData.ftLastAccessTime_dwLowDateTime;
                this.ftLastAccessTimeHigh = findData.ftLastAccessTime_dwHighDateTime;
                this.ftLastWriteTimeLow = findData.ftLastWriteTime_dwLowDateTime;
                this.ftLastWriteTimeHigh = findData.ftLastWriteTime_dwHighDateTime;
                this.fileSizeHigh = findData.nFileSizeHigh;
                this.fileSizeLow = findData.nFileSizeLow;
            }
        }

        /// <summary>
        /// Win32Api file attributes structure
        /// </summary>
        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto), BestFitMapping(false)]
        public class WIN32_FIND_DATA
        {
            internal int dwFileAttributes;
            internal uint ftCreationTime_dwLowDateTime;
            internal uint ftCreationTime_dwHighDateTime;
            internal uint ftLastAccessTime_dwLowDateTime;
            internal uint ftLastAccessTime_dwHighDateTime;
            internal uint ftLastWriteTime_dwLowDateTime;
            internal uint ftLastWriteTime_dwHighDateTime;
            internal uint nFileSizeHigh;
            internal uint nFileSizeLow;
            internal int dwReserved0;
            internal int dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            internal string cAlternateFileName;
        }

        /// <summary>
        /// Possible flags for the SHFileOperation method.
        /// </summary>
        [Flags]
        public enum FileOperationFlags : ushort
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
        public enum FileOperationType : uint
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







        /// <summary>
        /// Contain file handle
        /// </summary>
        internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            // Methods
            internal SafeFindHandle()
                : base(true) { }

            protected override bool ReleaseHandle()
                => SafeNativeMethods.FindClose(base.handle);
        }


        public static IEnumerable<LittleFileInfo> GetFiles(string folderPath)
        {
            IOHelper.WIN32_FIND_DATA win_find_data = new IOHelper.WIN32_FIND_DATA();
            string prefixedfolderPath = folderPath.TrimEnd(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            prefixedfolderPath = string.Concat("\\\\?\\", prefixedfolderPath);
            int num2 = SafeNativeMethods.SetErrorMode(1);
            try
            {
                SafeFindHandle handle = SafeNativeMethods.FindFirstFile(prefixedfolderPath + @"\*", win_find_data);
                try
                {
                    if (handle.IsInvalid)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                    else
                    {
                        bool found = true;
                        while (found)
                        {
                            if (win_find_data.cFileName != "." && win_find_data.cFileName != "..")
                            {
                                IOHelper.WIN32_FILE_ATTRIBUTE_DATA data = new WIN32_FILE_ATTRIBUTE_DATA();
                                data.PopulateFrom(win_find_data);
                                yield return new LittleFileInfo(folderPath, win_find_data.cFileName, data);
                            }
                            found = SafeNativeMethods.FindNextFile(handle, win_find_data);
                        }
                    }
                }
                finally
                {
                    handle.Close();
                }
            }
            finally
            {
                _ = SafeNativeMethods.SetErrorMode(num2);
            }
        }

        /// <summary>
        /// Get file attributes
        /// </summary>
        /// <param name="path">Path to file or folder</param>
        /// <param name="data">The file attribute structure to fill</param>
        /// <param name="tryagain">If false try get file attributes with GetFileAttributesEx function. If true try with the FindFirstFile function.</param>
        public static void FillAttributeInfo(string path, ref IOHelper.WIN32_FILE_ATTRIBUTE_DATA data, bool tryagain = false)
        {
            int num = 0;
            if (tryagain)
            {
                IOHelper.WIN32_FIND_DATA win_find_data = new IOHelper.WIN32_FIND_DATA();
                string fileName = path.TrimEnd(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
                int num2 = SafeNativeMethods.SetErrorMode(1);
                try
                {
                    SafeFindHandle handle = SafeNativeMethods.FindFirstFile(fileName, win_find_data);
                    try
                    {
                        if (handle.IsInvalid)
                        {
                            num = Marshal.GetLastWin32Error();
                            if (num != 0)
                                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        }
                    }
                    finally
                    {
                        handle.Close();
                    }
                }
                finally
                {
                    _ = SafeNativeMethods.SetErrorMode(num2);
                }
                data.PopulateFrom(win_find_data);
                return;
            }
            bool flag2 = false;
            int newMode = SafeNativeMethods.SetErrorMode(1);
            try
            {
                flag2 = SafeNativeMethods.GetFileAttributesEx(path, 0, ref data);
            }
            finally
            {
                _ = SafeNativeMethods.SetErrorMode(newMode);
            }
            if (!flag2)
            {
                num = Marshal.GetLastWin32Error();
                if (((num != 2) && (num != 3)) && (num != 0x15))
                {
                    FillAttributeInfo(path, ref data, true);
                    //return;
                }
                //else if (num == 2)
                //return;
                //else if (num != 0) //throw new Win32Exception(num);
                //return;
            }
        }

        /// <summary>
        /// Get the compressed file size
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="data">The file attribute structure to fill</param>
        public static long? GetCompressedFileSize(string filename)
        {
            uint losize = SafeNativeMethods.GetCompressedFileSize(filename, out uint hosize);
            int error = Marshal.GetLastWin32Error();
            if (hosize == 0 && losize == 0xFFFFFFFF && error != 0)
                return null;
            return ((long)hosize << 32) + losize;
        }

        public static uint GetClusterSize(string path)
        {
            string drive = System.IO.Path.GetPathRoot(path);
            bool result = SafeNativeMethods.GetDiskFreeSpace(drive, out uint sectorsPerCluster, out uint bytesPerSector, out uint _, out _);
            if (!result)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            return sectorsPerCluster * bytesPerSector;
        }



    }
}
