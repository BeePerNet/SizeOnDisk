using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using WPFByYourCommand.Exceptions;
using WPFLocalizeExtension.Extensions;

namespace SizeOnDisk.Shell
{
    public static partial class ShellHelper
    {


        private static ConcurrentDictionary<string, string> associations = new ConcurrentDictionary<string, string>();
        private static string currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;


        public static void Activate(string appId, string arguments)
        {
            ApplicationActivationManager appActiveManager = new ApplicationActivationManager();//Class not registered
            appActiveManager.ActivateApplication(appId, arguments, ActivateOptions.None, out _);
        }

        public static void Activate(string appId, string file, string verb)
        {
            ApplicationActivationManager appActiveManager = new ApplicationActivationManager();//Class not registered
            IShellItem pShellItem = SafeNativeMethods.SHCreateItemFromParsingNameIShellItem(file, IntPtr.Zero, typeof(IShellItem).GUID);
            if (pShellItem != null)
            {
                if (SafeNativeMethods.SHCreateShellItemArrayFromShellItem(pShellItem, typeof(IShellItemArray).GUID, out IShellItemArray pShellItemArray) == (int)HResult.Ok)
                {
                    appActiveManager.ActivateForFile(appId, pShellItemArray, verb, out _);
                }
            }
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
            {
                return SafeNativeMethods.FindClose(base.handle);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive")]
        public static string GetShellLinkPath(string file)
        {
            if (System.IO.Path.GetExtension(file).ToLower() != ".lnk")
            {
                throw new Exception("Supplied file must be a .LNK file");
            }

            FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            using (System.IO.BinaryReader fileReader = new BinaryReader(fileStream))
            {
                fileStream.Seek(0x14, SeekOrigin.Begin);     // Seek to flags
                uint flags = fileReader.ReadUInt32();        // Read flags
                if ((flags & 1) == 1)
                {                      // Bit 1 set means we have to
                                       // skip the shell item ID list
                    fileStream.Seek(0x4c, SeekOrigin.Begin); // Seek to the end of the header
                    uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
                    fileStream.Seek(offset, SeekOrigin.Current); // Seek past it (to the file locator info)
                }

                long fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                             // structure begins
                uint totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
                fileStream.Seek(0xc, SeekOrigin.Current); // seek to offset to base pathname
                uint fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                           // the offset is from the beginning of the file info struct (fileInfoStartsAt)
                fileStream.Seek((fileInfoStartsAt + fileOffset), SeekOrigin.Begin); // Seek to beginning of
                                                                                    // base pathname (target)
                long pathLength = (totalStructLength + fileInfoStartsAt) - fileStream.Position - 1; // read
                                                                                                    // the base pathname. I don't need the 2 terminating nulls.
                char[] linkTarget = fileReader.ReadChars((int)pathLength); // should be unicode safe
                var link = new string(linkTarget);

                int begin = link.IndexOf("\0\0");
                if (begin > -1)
                {
                    int end = link.IndexOf("\\\\", begin + 2) + 2;
                    end = link.IndexOf('\0', end) + 1;

                    string firstPart = link.Substring(0, begin);
                    string secondPart = link.Substring(end);

                    return firstPart + secondPart;
                }
                else
                {
                    return link;
                }
            }
        }



        public static IEnumerable<LittleFileInfo> GetFiles(string folderPath)
        {
            WIN32_FIND_DATA win_find_data = new WIN32_FIND_DATA();
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
                                WIN32_FILE_ATTRIBUTE_DATA data = new WIN32_FILE_ATTRIBUTE_DATA();
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
        internal static void FillAttributeInfo(string path, ref WIN32_FILE_ATTRIBUTE_DATA data, bool tryagain = false)
        {
            int num;
            if (tryagain)
            {
                WIN32_FIND_DATA win_find_data = new WIN32_FIND_DATA();
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
                            {
                                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
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
        public static ulong? GetCompressedFileSize(string filename)
        {
            uint losize = SafeNativeMethods.GetCompressedFileSize(filename, out uint hosize);
            int error = Marshal.GetLastWin32Error();
            if (hosize == 0 && losize == 0xFFFFFFFF && error != 0)
            {
                return null;
            }

            return ((ulong)hosize << 32) + losize;
        }

        public static uint GetClusterSize(string path)
        {
            string drive = System.IO.Path.GetPathRoot(path);
            bool result = SafeNativeMethods.GetDiskFreeSpace(drive, out uint sectorsPerCluster, out uint bytesPerSector, out uint _, out _);
            if (!result)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            return sectorsPerCluster * bytesPerSector;
        }


        public static string GetFriendlyName(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return string.Empty;
            }

            if (currentCulture != CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                associations = new ConcurrentDictionary<string, string>();
                currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            }
            if (!associations.ContainsKey(extension))
            {
                string fileType = ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.FriendlyDocName, extension);
                if (!associations.ContainsKey(extension))
                {
                    // Cache the association so we don't traverse the registry again
                    while (!associations.TryAdd(extension, fileType)) { }
                }
            }
            return associations[extension];
        }

        public static BitmapSource GetIcon(string path, int size = 16, bool thumbnail = false, bool cache = false)
        {
            // Create a native shellitem from our path
            int retCode = SafeNativeMethods.SHCreateItemFromParsingNameIShellItemImageFactory(path, IntPtr.Zero, typeof(SafeNativeMethods.IShellItemImageFactory).GUID, out SafeNativeMethods.IShellItemImageFactory imageFactory);
            if (retCode != 0 || imageFactory == null)
            {
                //return new BitmapImage(new Uri("pack://application:,,,/SizeOnDisk;component/Icons/File.png"));
                return null;
            }
            //throw new ExternalException("ShellObjectFactoryUnableToCreateItem", Marshal.GetExceptionForHR(retCode));


            Size nativeSIZE = new Size(
                Convert.ToInt32(size),
                Convert.ToInt32(size)
            );

            SIIGBF options = SIIGBF.ResizeToFit;
            if (!thumbnail)
            {
                options = SIIGBF.IconOnly;
            }

            if (cache)
            {
                options |= SIIGBF.MemoryOnly;
            }

            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                imageFactory.GetImage(nativeSIZE, options, out hBitmap);
            }
            finally
            {
                Marshal.ReleaseComObject(imageFactory);
            }
            if (hBitmap == IntPtr.Zero)
            {
                return null;
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


        private static BitmapSource GetIcon(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string[] values = value.Split(',');
                if (values != null && values.Length == 2)
                {
                    return ExtractIconFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
                }
                else if (values != null && values.Length == 1)
                {
                    string icon = values[0];
                    if (values[0].StartsWith("@{", StringComparison.Ordinal))
                    {
                        StringBuilder outBuff = new StringBuilder(1024);
                        if (SafeNativeMethods.SHLoadIndirectString(icon, outBuff, outBuff.Capacity, IntPtr.Zero) == 0)
                        {
                            icon = outBuff.ToString();
                            if (icon.Contains(','))
                            {
                                values = outBuff.ToString().Split(',');
                                if (values != null && values.Length == 2)
                                {
                                    return ExtractIconFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
                                }
                            }
                            else
                            {
                                return new BitmapImage(new Uri(icon));
                            }
                        }
                    }
                    else
                    {
                        return ExtractIconFromDLL(values[0], 0);
                    }
                }
            }
            return null;
        }


        private static string GetText(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                string[] values = value.Split(',');
                if (values != null && values.Length == 2)
                {
                    return ExtractStringFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
                }
                else if (values != null && values.Length == 1)
                {
                    string text = values[0];
                    if (values[0].StartsWith("@{", StringComparison.Ordinal))
                    {
                        StringBuilder outBuff = new StringBuilder(1024);
                        if (SafeNativeMethods.SHLoadIndirectString(text, outBuff, outBuff.Capacity, IntPtr.Zero) == 0)
                        {
                            text = outBuff.ToString();
                            if (text.Contains(','))
                            {
                                values = outBuff.ToString().Split(',');
                                if (values != null && values.Length == 2)
                                {
                                    return ExtractStringFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
                                }
                            }
                            else
                            {
                                return text;
                            }
                        }
                    }
                    else
                    {
                        return ExtractStringFromDLL(values[0], 0);
                    }
                }
            }
            return null;
        }

        private static ShellCommandVerb GetVerb(RegistryKey verbkey, string id, string appUserModeId)
        {
            if (id.ToUpperInvariant() == "RUNAS")
            {
                return null;
            }
            //We are not taking DDE
            RegistryKey cmd = verbkey.OpenSubKey("ddeexec");
            if (cmd != null && !string.IsNullOrEmpty(cmd.GetValue(string.Empty, string.Empty).ToString()))
            {
                return null;
            }

            ShellCommandVerb verb = new ShellCommandVerb
            {
                Verb = id,
                Name = id
            };

            string name = verbkey.GetValue("MUIVerb", string.Empty).ToString();
            if (string.IsNullOrEmpty(name))
            {
                name = verbkey.GetValue(string.Empty, string.Empty).ToString();
            }

            if (name.StartsWith("@", StringComparison.Ordinal))
            {
                StringBuilder outBuff = new StringBuilder(1024);
                if (SafeNativeMethods.SHLoadIndirectString(name, outBuff, outBuff.Capacity, IntPtr.Zero) == 0)
                {
                    verb.Name = outBuff.ToString();
                }
            }
            else
            {
                string locname = LocExtension.GetLocalizedValue<string>($"PresentationCore:ExceptionStringTable:{id}Text");
                if (string.IsNullOrEmpty(locname))
                {
                    locname = LocExtension.GetLocalizedValue<string>(id);
                }

                if (!string.IsNullOrEmpty(locname))
                {
                    verb.Name = locname;
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    verb.Name = name;
                }
            }

            if (id.ToUpperInvariant() == "RUNASUSER")
            {
                verb.Command = "cmd:runasuser";
                return verb;
            }

            cmd = verbkey.OpenSubKey("command");
            if (cmd != null)
            {
                verb.Command = cmd.GetValue(string.Empty, string.Empty).ToString();
                if (string.IsNullOrEmpty(verb.Command))
                {
                    name = cmd.GetValue("DelegateExecute", string.Empty).ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        cmd = Registry.ClassesRoot.OpenSubKey("CLSID\\" + name);
                        if (cmd != null)
                        {
                            cmd = cmd.OpenSubKey("LocalServer32");
                            if (cmd != null)
                            {
                                name = cmd.GetValue(string.Empty, string.Empty).ToString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    verb.Command = name;
                                }
                            }
                            if (string.IsNullOrEmpty(name))
                            {
                                cmd = cmd.OpenSubKey("InProcServer32");
                                if (cmd != null)
                                {
                                    name = cmd.GetValue(string.Empty, string.Empty).ToString();
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        verb.Command = "dll:" + name;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(verb.Command))
            {
                if (!string.IsNullOrEmpty(appUserModeId))
                {
                    verb.Command = "Id:" + appUserModeId;
                }
            }

            if (string.IsNullOrEmpty(verb.Command))
            {

            }
            return verb;
        }



        private static ShellCommandSoftware GetSoftware(RegistryKey appkey, string id)
        {
            ShellCommandSoftware soft = new ShellCommandSoftware
            {
                Id = id,
                Name = id
            };

            RegistryKey subkey;

            string name = appkey.GetValue(string.Empty, string.Empty).ToString();
            if (string.IsNullOrEmpty(name))
            {
                name = ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.FriendlyAppName, id);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                name = appkey.GetValue("FriendlyTypeName", string.Empty).ToString();
                name = GetText(name);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                name = appkey.GetValue("DisplayName", string.Empty).ToString();
                name = GetText(name);
            }
            if (!string.IsNullOrEmpty(name))
            {
                soft.Name = name;
            }

            string value = ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.DefaultIcon, id);
            soft.Icon = GetIcon(value);
            if (soft.Icon == null)
            {
                value = ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.AppIconReference, id);
                soft.Icon = GetIcon(value);
            }

            if (soft.Icon == null)
            {
                subkey = appkey.OpenSubKey("DefaultIcon");
                if (subkey != null)
                {
                    value = subkey.GetValue(string.Empty, string.Empty).ToString();
                    soft.Icon = GetIcon(value);
                }
            }

            string appid = string.Empty;
            subkey = appkey.OpenSubKey("Application");
            if (subkey != null)
            {
                appid = subkey.GetValue("AppUserModelID", string.Empty).ToString();

                if (soft.Icon == null)
                {
                    value = subkey.GetValue(string.Empty, string.Empty).ToString();
                    soft.Icon = GetIcon(value);
                }

                if (string.IsNullOrEmpty(soft.Name))
                {
                    value = subkey.GetValue("ApplicationName", string.Empty).ToString();
                    soft.Name = GetText(value);
                }
            }

            subkey = appkey.OpenSubKey("Shell");
            if (subkey != null)
            {
                string defaultverb = subkey.GetValue(string.Empty, string.Empty).ToString();
                string[] appverbs = subkey.GetSubKeyNames();
                if (appverbs.Contains("OPEN", StringComparer.OrdinalIgnoreCase) && appverbs[0].ToUpperInvariant() != "OPEN")
                {
                    string openOne = appverbs.First(T => T.ToUpperInvariant() == "OPEN");
                    int pos = Array.FindIndex(appverbs, T => T == openOne);
                    appverbs[pos] = appverbs[0];
                    appverbs[0] = openOne;
                }
                foreach (string appverb in appverbs)
                {
                    RegistryKey verbkey = subkey.OpenSubKey(appverb);
                    ShellCommandVerb verb = GetVerb(verbkey, appverb, appid);
                    if (verb != null)
                    {
                        soft.Verbs.Add(verb);
                        if (defaultverb == appverb)
                        {
                            soft.Default = verb;
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(soft.Name))
            {
                string application = appkey.GetValue(string.Empty, string.Empty).ToString();
                if (!string.IsNullOrWhiteSpace(application))
                {
                    soft.Name = application;
                }
                else
                {
                    soft.Name = id;
                }
            }

            if (soft.Verbs.Count <= 0)
            {
                return null;
            }

            if (soft.Icon == null)
            {
                appid = soft.Verbs.FirstOrDefault(T => T.Command.ToUpperInvariant().Contains(".EXE"))?.Command;
                if (!string.IsNullOrEmpty(appid))
                {
                    appid = SplitCommandAndParameters(appid).Item1;
                    soft.Icon = GetIcon(appid + ",0");
                }
            }

            return soft;
        }


        public static ShellCommandRoot GetShellCommands(string path, bool isDirectory)
        {
            ShellCommandRoot result = new ShellCommandRoot();
            if (isDirectory)
            {
                RegistryKey appkey = Registry.ClassesRoot.OpenSubKey("Directory");
                if (appkey != null)
                {
                    ShellCommandSoftware soft = GetSoftware(appkey, "Directory");
                    result.Softwares.Add(soft);
                    result.Default = soft;
                }
            }
            else
            {
                string ext = Path.GetExtension(path);
                RegistryKey extkey = Registry.ClassesRoot.OpenSubKey(ext);
                if (extkey != null)
                {
                    result.ContentType = extkey.GetValue("Content Type", string.Empty).ToString();
                    result.PerceivedType = extkey.GetValue("PerceivedType", string.Empty).ToString();

                    string defaultApp = extkey.GetValue(string.Empty, string.Empty).ToString();
                    RegistryKey key = extkey.OpenSubKey("OpenWithProgids");

                    List<string> subvalues = new List<string>(); ;
                    if (key != null)
                    {
                        subvalues.AddRange(key.GetValueNames());
                    }

                    if (!string.IsNullOrWhiteSpace(defaultApp))
                    {
                        if (subvalues.Contains(defaultApp))
                        {
                            subvalues.Remove(defaultApp);
                        }

                        subvalues.Insert(0, defaultApp);
                    }

                    foreach (string subkey in subvalues)
                    {
                        //Comme folder à vérifier comment simplifier
                        RegistryKey appkey = Registry.ClassesRoot.OpenSubKey(subkey);
                        if (appkey != null)
                        {
                            string application = appkey.GetValue(string.Empty, string.Empty).ToString();

                            ShellCommandSoftware soft = GetSoftware(appkey, subkey);
                            if (soft != null)
                            {
                                result.Softwares.Add(soft);
                                if (application == subkey)
                                {
                                    result.Default = soft;
                                }
                            }
                        }
                    }

                    subvalues.Clear();
                    key = extkey.OpenSubKey("OpenWithList");
                    if (key != null)
                    {
                        foreach (string subkey in key.GetSubKeyNames())
                        {
                            RegistryKey appkey = Registry.ClassesRoot.OpenSubKey("Applications\\" + subkey);
                            ShellCommandSoftware soft = GetSoftware(appkey, subkey);
                            if (soft != null)
                            {
                                result.Softwares.Add(soft);
                            }
                        }
                    }
                }
            }
            return result;
        }


        #region public functions

        public static void ShellExecuteSelect(string path)
        {
            ShellHelper.ShellExecute("explorer.exe", $"/select,\"{path}\"");
        }


        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "<En attente>")]
        public static void ShellExecute(string fileName, string parameters = null, string verb = null, bool normal = false)
        {

            IntPtr window = new WindowInteropHelper(Application.Current.MainWindow)?.Handle ?? IntPtr.Zero;

            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO
            {
                lpVerb = verb,
                lpFile = fileName,
                lpParameters = parameters,
                nShow = (int)(normal ? ShowCommands.SW_NORMAL : ShowCommands.SW_SHOW)
            };
            info.hwnd = window;
            info.cbSize = Marshal.SizeOf(info);
            ShellExecuteFlags flags = (!string.IsNullOrWhiteSpace(verb) && (verb != "find") ? ShellExecuteFlags.INVOKEIDLIST : ShellExecuteFlags.DEFAULT) | ShellExecuteFlags.NOUI | ShellExecuteFlags.UNICODE;
            info.fMask = (uint)flags;
            SafeNativeMethods.ShellExecuteEx(ref info);
            if (info.hInstApp.ToInt64() <= 32)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Invalid operation ({1}) on file {0}", fileName,
                    (ShellExecuteReturnCodes)info.hInstApp.ToInt32()));
            }
        }

        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "<En attente>")]
        private static string FileExtentionInfo(AssocStr assocStr, string doctype)
        {
            IntPtr pcchOut = IntPtr.Zero;
            if (SafeNativeMethods.AssocQueryString(AssocF.Verify, assocStr, doctype, null, null, ref pcchOut) != 1)
            {
                return null;
            }

            StringBuilder pszOut = new StringBuilder((int)pcchOut);
            if (SafeNativeMethods.AssocQueryString(AssocF.Verify, assocStr, doctype, null, pszOut, ref pcchOut) != 0)
            {
                return null;
            }

            return pszOut.ToString();
        }

        public static Tuple<string, string> SplitCommandAndParameters(string cmd)
        {
            string parameters = string.Empty;
            int pos = 1;
            if (!string.IsNullOrEmpty(cmd))
            {
                if (cmd.StartsWith("\"", StringComparison.Ordinal) && cmd.Count(T => T == '\"') > 1)
                {
                    while (pos < cmd.Length)
                    {
                        pos = cmd.IndexOf('\"', pos);
                        if (pos < 0 || pos >= cmd.Length)
                        {
                            pos = cmd.Length;
                        }
                        else if (cmd[pos + 1] != '\"')
                        {
                            parameters = cmd.Substring(pos + 1);
                            cmd = cmd.Substring(1, pos - 1);
                            pos = cmd.Length;
                        }
                    }
                }
                else
                {
                    pos = cmd.IndexOf(".exe ", StringComparison.OrdinalIgnoreCase);
                    if (pos < 0)
                    {
                        pos = cmd.IndexOf(".cmd ", StringComparison.OrdinalIgnoreCase);
                    }

                    if (pos < 0)
                    {
                        pos = cmd.IndexOf(".bat ", StringComparison.OrdinalIgnoreCase);
                    }

                    if (pos > 0 && pos < cmd.Length)
                    {
                        parameters = cmd.Substring(pos + 5);
                        cmd = cmd.Substring(0, pos + 4);
                    }
                    else
                    {
                        pos = cmd.IndexOf(' ');
                        if (pos > 0 && pos < cmd.Length)
                        {
                            parameters = cmd.Substring(pos + 1);
                            cmd = cmd.Substring(0, pos);
                        }
                    }
                }
            }
            else
            {
                cmd = string.Empty;
            }

            return new Tuple<string, string>(cmd.Trim(), parameters.Trim());
        }




        #endregion public functions

        /// <summary>
        /// Run process in elevated privilege
        /// </summary>
        //[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Execution)]
        public static void Restart(bool runAsAdministrator)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = Assembly.GetExecutingAssembly().Location
            };
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
                {
                    ExceptionBox.ShowException(ex);
                }
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
            }
        }

        public static BitmapSource ExtractIconFromDLL(string file, int index)
        {
            if (file.StartsWith("@%", StringComparison.Ordinal))
            {
                file = file.Substring(1);
            }

            IntPtr[] handles = new IntPtr[1];
            System.IntPtr hIcon = IntPtr.Zero;

            if (SafeNativeMethods.ExtractIconEx(file, index, null, handles, 1) > 0)
            {
                hIcon = handles[0];
            }

            //System.IntPtr hIcon = ExtractIcon(Process.GetCurrentProcess().Handle, file, number);
            if (hIcon == IntPtr.Zero)
            {
                // extraction error
                return null;
            }
            try
            {
                Icon icon = Icon.FromHandle(hIcon);

                Bitmap bitmap = icon.ToBitmap();
                IntPtr hBitmap = bitmap.GetHbitmap();

                BitmapSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                if (!SafeNativeMethods.DeleteObject(hBitmap))
                {
                    throw new Win32Exception();
                }

                return wpfBitmap;
            }
            finally
            {
                // Release the handle
                SafeNativeMethods.DestroyIcon(hIcon);
            }
        }




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

        [HostProtection(SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalProcessMgmt, UI = true)]
        private static bool ShellDeleteOperation(FileOperationFlags OperationFlags, string[] FullSource)
        {
            int num;
            SHFILEOPSTRUCT lpFileOp = GetShellOperationInfo(FileOperationType.FO_DELETE, OperationFlags, FullSource, null);
            num = SafeNativeMethods.SHFileOperation(ref lpFileOp);
            SafeNativeMethods.SHChangeNotify(0x2381f, 3, IntPtr.Zero, IntPtr.Zero);
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
        {
            return ShellDeleteOperation(FileOperationFlags.FOF_ALLOWUNDO | FileOperationFlags.FOF_WANTNUKEWARNING, path);
        }


        /// <summary>
        /// Send file to recycle bin
        /// </summary>
        /// <param name="path">Location of directory or file to recycle</param>
        /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
        public static bool PermanentDelete(params string[] path)
        {
            return ShellDeleteOperation(0, path);
        }


        public static string ExtractStringFromDLL(string file, int number)
        {
            if (file.StartsWith("@", StringComparison.Ordinal))
            {
                file = file.Substring(1);
            }

            IntPtr lib = SafeNativeMethods.LoadLibrary(file);
            StringBuilder result = new StringBuilder(256);
            try
            {
                int r = SafeNativeMethods.LoadString(lib, number, result, result.Capacity);
            }
            finally
            {
                SafeNativeMethods.FreeLibrary(lib);
            }
            return result.ToString();
        }




        public static string ShowVistaDialog(System.Windows.Forms.IWin32Window owner, string initialFolder, string defaultFolder)
        {
            Guid riid = typeof(IShellItem).GUID;
            IFileDialog frm = (IFileDialog)(new FileOpenDialogRCW());
            frm.GetOptions(out FileOpenOptions options);
            options |= FileOpenOptions.PickFolders | FileOpenOptions.ForceFilesystem | FileOpenOptions.NoValidate | FileOpenOptions.NoTestFileCreate | FileOpenOptions.DontAddToRecent;
            frm.SetOptions(options);
            if (initialFolder != null)
            {
                IShellItem shellItem = SafeNativeMethods.SHCreateItemFromParsingNameIShellItem(initialFolder, IntPtr.Zero, riid);
                if (shellItem != null)
                {
                    frm.SetFolder(shellItem);
                }
            }
            if (defaultFolder != null)
            {
                IShellItem shellItem = SafeNativeMethods.SHCreateItemFromParsingNameIShellItem(defaultFolder, IntPtr.Zero, riid);
                if (shellItem != null)
                {
                    frm.SetDefaultFolder(shellItem);
                }
            }

            if (frm.Show(owner.Handle) == (int)HResult.Ok)
            {
                if (frm.GetResult(out IShellItem shellItem) == (int)HResult.Ok)
                {
                    if (shellItem.GetDisplayName(SIGDN.FILESYSPATH, out IntPtr pszString) == (int)HResult.Ok)
                    {
                        if (pszString != IntPtr.Zero)
                        {
                            try
                            {
                                return Marshal.PtrToStringAuto(pszString);
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(pszString);
                            }
                        }
                    }

                }
            }
            return null;
        }




    }



}
