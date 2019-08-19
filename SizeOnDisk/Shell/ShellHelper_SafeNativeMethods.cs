using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SizeOnDisk.Shell
{
    public static partial class ShellHelper
    {
        private static class SafeNativeMethods
        {
            [DllImport("Shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int SHCreateShellItemArrayFromShellItem(IShellItem psi, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItemArray ppv);



            [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
            public static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);


            [DllImport("Shlwapi.dll", EntryPoint = "AssocQueryStringW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
            public static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref IntPtr pcchOut);


            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteObject([In]IntPtr hObject);





            [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHCreateItemFromParsingName", PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)]
            public static extern IShellItem SHCreateItemFromParsingNameIShellItem(
                [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                [In]IntPtr pbc,
                [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHCreateItemFromParsingName")]
            public static extern int SHCreateItemFromParsingNameIShellItemImageFactory(
                [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                [In]IntPtr pbc,
                [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItemImageFactory ppv);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHCreateItemFromParsingName", PreserveSig = false)]
            [return: MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)]
            public static extern IShellLinkW SHCreateItemFromParsingNameIShellLinkW(
                [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                [In]IntPtr pbc,
                [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid);


            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FindNextFile(SafeFindHandle hFindFile, [Out] WIN32_FIND_DATA lpFindFileData);


            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FindClose(IntPtr hFindFile);


            [DllImport("kernel32.dll")]
            public static extern int SetErrorMode(int newMode);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetFileAttributesEx(string name, int fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeFindHandle FindFirstFile(string fileName, [In, Out] WIN32_FIND_DATA data);

            [DllImport("kernel32.dll", EntryPoint = "GetCompressedFileSizeW", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern uint GetCompressedFileSize(string lpFileName, out uint lpFileSizeHigh);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetDiskFreeSpace(string lpRootPathName,
               out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
               out uint lpTotalNumberOfClusters);

            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            public static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);


            [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);







            [DllImport("user32.dll", EntryPoint = "DestroyIcon",
                SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);


            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true, BestFitMapping = false)]
            public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern int LoadString(IntPtr hInstance, int ID, StringBuilder lpBuffer, int nBufferMax);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FreeLibrary(IntPtr hModule);


            [ComImport()]
            [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            public interface IShellItemImageFactory
            {
                void GetImage(
                    [In] [MarshalAs(UnmanagedType.Struct)] Size size,
                    [In] SIIGBF flags,
                    [Out] out IntPtr phbm);
            }

            /*
            "open"        - Opens a file or a application
            "openas"    - Opens dialog when no program is associated to the extension
            "opennew"    - see MSDN
            "runas"    - In Windows 7 and Vista, opens the UAC dialog and in others, open the Run as... Dialog
            "null"     - Specifies that the operation is the default for the selected file type.
            "edit"        - Opens the default text editor for the file.    
            "explore"    - Opens the Windows Explorer in the folder specified in lpDirectory.
            "properties"    - Opens the properties window of the file.
            "copy"        - see MSDN
            "cut"        - see MSDN
            "paste"    - see MSDN
            "pastelink"    - pastes a shortcut
            "delete"    - see MSDN
            "print"    - Start printing the file with the default application.
            "printto"    - see MSDN
            "find"        - Start a search
            */
            [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);


        }

    }
}
