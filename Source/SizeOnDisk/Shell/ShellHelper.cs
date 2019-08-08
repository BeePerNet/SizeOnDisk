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
using System.Runtime.CompilerServices;
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
    public static class ShellHelper
    {


        private static ConcurrentDictionary<string, string> associations = new ConcurrentDictionary<string, string>();
        private static string currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;


        public static void Activate(string appId, string arguments)
        {
            SafeNativeMethods.ApplicationActivationManager appActiveManager = new SafeNativeMethods.ApplicationActivationManager();//Class not registered
            appActiveManager.ActivateApplication(appId, arguments, SafeNativeMethods.ActivateOptions.None, out _);
        }

        public static void Activate(string appId, string file, string verb)
        {
            SafeNativeMethods.ApplicationActivationManager appActiveManager = new SafeNativeMethods.ApplicationActivationManager();//Class not registered
            SafeNativeMethods.IShellItem pShellItem = SafeNativeMethods.SHCreateItemFromParsingNameIShellItem(file, IntPtr.Zero, typeof(SafeNativeMethods.IShellItem).GUID);
            if (pShellItem != null)
            {
                if (SafeNativeMethods.SHCreateShellItemArrayFromShellItem(pShellItem, typeof(SafeNativeMethods.IShellItemArray).GUID, out SafeNativeMethods.IShellItemArray pShellItemArray) == (int)SafeNativeMethods.HResult.Ok)
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


        public static string GetShellLinkPath(string file)
        {
            try
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
            catch
            {
                return "";
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
        public static void FillAttributeInfo(string path, ref WIN32_FILE_ATTRIBUTE_DATA data, bool tryagain = false)
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
        public static long? GetCompressedFileSize(string filename)
        {
            uint losize = SafeNativeMethods.GetCompressedFileSize(filename, out uint hosize);
            int error = Marshal.GetLastWin32Error();
            if (hosize == 0 && losize == 0xFFFFFFFF && error != 0)
            {
                return null;
            }

            return ((long)hosize << 32) + losize;
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


            SafeNativeMethods.Size nativeSIZE = new SafeNativeMethods.Size(
                Convert.ToInt32(size),
                Convert.ToInt32(size)
            );

            SafeNativeMethods.SIIGBF options = SafeNativeMethods.SIIGBF.ResizeToFit;
            if (!thumbnail)
            {
                options = SafeNativeMethods.SIIGBF.IconOnly;
            }

            if (cache)
            {
                options |= SafeNativeMethods.SIIGBF.MemoryOnly;
            }

            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                imageFactory.GetImage(nativeSIZE, options, out hBitmap);
            }
            finally
            {
                Marshal.FinalReleaseComObject(imageFactory);
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
                    return SafeNativeMethods.ExtractIconFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
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
                                    return SafeNativeMethods.ExtractIconFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
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
                        return SafeNativeMethods.ExtractIconFromDLL(values[0], 0);
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
                    return SafeNativeMethods.ExtractStringFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
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
                                    return SafeNativeMethods.ExtractStringFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
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
                        return SafeNativeMethods.ExtractStringFromDLL(values[0], 0);
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
        public static void ShellExecute(string fileName, string parameters = null, string verb = null, bool normal = false, ShellExecuteFlags? flags = null)
        {

            IntPtr window = new WindowInteropHelper(Application.Current.MainWindow)?.Handle ?? IntPtr.Zero;

            SafeNativeMethods.SHELLEXECUTEINFO info = new SafeNativeMethods.SHELLEXECUTEINFO
            {
                lpVerb = verb,
                lpFile = fileName,
                lpParameters = parameters,
                nShow = (int)(normal ? SafeNativeMethods.ShowCommands.SW_NORMAL : SafeNativeMethods.ShowCommands.SW_SHOW)
            };
            info.hwnd = window;
            info.cbSize = Marshal.SizeOf(info);
            if (!flags.HasValue)
            {
                flags = !string.IsNullOrWhiteSpace(verb) && (verb != "find") ? ShellExecuteFlags.INVOKEIDLIST : ShellExecuteFlags.DEFAULT;
            }

            flags = flags | ShellExecuteFlags.NOUI | ShellExecuteFlags.UNICODE;
            info.fMask = (uint)flags;
            SafeNativeMethods.ShellExecuteEx(ref info);
            if (info.hInstApp.ToInt64() <= 32)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Invalid operation ({1}) on file {0}", fileName,
                    (SafeNativeMethods.ShellExecuteReturnCodes)info.hInstApp.ToInt32()));
            }
        }

        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "<En attente>")]
        public static string FileExtentionInfo(AssocStr assocStr, string doctype)
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


        #region ShellexecuteEx


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


        [Flags]
        public enum AssocF : int
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

        public enum AssocStr
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
        public enum ShellExecuteFlags : int
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
            public void PopulateFrom(WIN32_FIND_DATA findData)
            {
                fileAttributes = findData.dwFileAttributes;
                ftCreationTimeLow = findData.ftCreationTime_dwLowDateTime;
                ftCreationTimeHigh = findData.ftCreationTime_dwHighDateTime;
                ftLastAccessTimeLow = findData.ftLastAccessTime_dwLowDateTime;
                ftLastAccessTimeHigh = findData.ftLastAccessTime_dwHighDateTime;
                ftLastWriteTimeLow = findData.ftLastWriteTime_dwLowDateTime;
                ftLastWriteTimeHigh = findData.ftLastWriteTime_dwHighDateTime;
                fileSizeHigh = findData.nFileSizeHigh;
                fileSizeLow = findData.nFileSizeLow;
            }
        }

        /// <summary>
        /// Win32Api file attributes structure
        /// </summary>
        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), BestFitMapping(false)]
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




        internal static class SafeNativeMethods
        {

            internal enum ActivateOptions
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

            [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IApplicationActivationManager
            {
                // Activates the specified immersive application for the "Launch" contract, passing the provided arguments
                // string into the application.  Callers can obtain the process Id of the application instance fulfilling this contract.
                IntPtr ActivateApplication([In] string appUserModelId, [In] string arguments, [In] ActivateOptions options, [Out] out uint processId);
                IntPtr ActivateForFile([In] string appUserModelId, [In] IShellItemArray /*IShellItemArray* */ itemArray, [In] string verb, [Out] out uint processId);
                IntPtr ActivateForProtocol([In] string appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out uint processId);
            }

            [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]//Application Activation Manager
            internal class ApplicationActivationManager : IApplicationActivationManager
            {
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
                public extern IntPtr ActivateApplication([In] string appUserModelId, [In] string arguments, [In] ActivateOptions options, [Out] out uint processId);
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public extern IntPtr ActivateForFile([In] string appUserModelId, [In] IShellItemArray /*IShellItemArray* */ itemArray, [In] string verb, [Out] out uint processId);
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public extern IntPtr ActivateForProtocol([In] string appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out uint processId);
            }

            public enum GETPROPERTYSTOREFLAGS
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

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct REFPROPERTYKEY
            {
                private readonly Guid fmtid;
                private readonly int pid;
                public Guid FormatId => fmtid;
                public int PropertyId => pid;

                public REFPROPERTYKEY(Guid formatId, int propertyId)
                {
                    fmtid = formatId;
                    pid = propertyId;
                }
                public static readonly REFPROPERTYKEY PKEY_DateCreated = new REFPROPERTYKEY(new Guid("B725F130-47EF-101A-A5F1-02608C9EEBAC"), 15);
            }

            public enum SIATTRIBFLAGS
            {
                AND = 0x1,
                OR = 0x2,
                APPCOMPAT = 0x3,
                MASK = 0x3,
                ALLITEMS = 0x4000
            }

            [ComImport()]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
            public interface IShellItemArray
            {
                int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, ref IntPtr ppvOut);
                int GetPropertyStore(GETPROPERTYSTOREFLAGS flags, ref Guid riid, ref IntPtr ppv);
                int GetPropertyDescriptionList(REFPROPERTYKEY keyType, ref Guid riid, ref IntPtr ppv);
                int GetAttributes(SIATTRIBFLAGS AttribFlags, int sfgaoMask, ref int psfgaoAttribs);
                int GetCount(ref int pdwNumItems);
                int GetItemAt(int dwIndex, ref IShellItem ppsi);
                int EnumItems(ref IntPtr ppenumShellItems);
            }



            [DllImport("Shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int SHCreateShellItemArrayFromShellItem(IShellItem psi, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItemArray ppv);



            [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
            internal static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);


            [DllImport("Shlwapi.dll", EntryPoint = "AssocQueryStringW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
            internal static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref IntPtr pcchOut);


            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeleteObject([In]IntPtr hObject);





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
            {
                return GetShellOperationInfo(OperationType, OperationFlags, SourcePath, null);
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



            /// <summary>
            /// A Wrapper for a SIZE struct
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct Size
            {
                public int cx;
                public int cy;
                public Size(int x, int y)
                {
                    cx = x;
                    cy = y;
                }
            }


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

            [DllImport("user32.dll", EntryPoint = "DestroyIcon",
                SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            private static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

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

                    if (!DeleteObject(hBitmap))
                    {
                        throw new Win32Exception();
                    }

                    return wpfBitmap;
                }
                finally
                {
                    // Release the handle
                    DestroyIcon(hIcon);
                }
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true, BestFitMapping = false)]
            private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern int LoadString(IntPtr hInstance, int ID, StringBuilder lpBuffer, int nBufferMax);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool FreeLibrary(IntPtr hModule);

            public static string ExtractStringFromDLL(string file, int number)
            {
                if (file.StartsWith("@", StringComparison.Ordinal))
                {
                    file = file.Substring(1);
                }

                IntPtr lib = LoadLibrary(file);
                StringBuilder result = new StringBuilder(256);
                try
                {
                    int r = LoadString(lib, number, result, result.Capacity);
                }
                finally
                {
                    FreeLibrary(lib);
                }
                return result.ToString();
            }

            [ComImport()]
            [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            internal interface IShellItemImageFactory
            {
                void GetImage(
                    [In] [MarshalAs(UnmanagedType.Struct)] Size size,
                    [In] SIIGBF flags,
                    [Out] out IntPtr phbm);
            }

            [Flags()]
            public enum SLGP_FLAGS
            {
                /// <summary>Retrieves the standard short (8.3 format) file name</summary>
                SLGP_SHORTPATH = 0x1,
                /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
                SLGP_UNCPRIORITY = 0x2,
                /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
                SLGP_RAWPATH = 0x4
            }

            [Flags()]
            public enum SLR_FLAGS
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

            /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
            [ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
            public interface IShellLinkW
            {
                /// <summary>Retrieves the path and file name of a Shell link object</summary>
                void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATA pfd, SLGP_FLAGS fFlags);
                /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
                void GetIDList(out IntPtr ppidl);
                /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
                void SetIDList(IntPtr pidl);
                /// <summary>Retrieves the description string for a Shell link object</summary>
                void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
                /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
                void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
                /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
                void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
                /// <summary>Sets the name of the working directory for a Shell link object</summary>
                void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
                /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
                void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
                /// <summary>Sets the command-line arguments for a Shell link object</summary>
                void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
                /// <summary>Retrieves the hot key for a Shell link object</summary>
                void GetHotkey(out short pwHotkey);
                /// <summary>Sets a hot key for a Shell link object</summary>
                void SetHotkey(short wHotkey);
                /// <summary>Retrieves the show command for a Shell link object</summary>
                void GetShowCmd(out int piShowCmd);
                /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
                void SetShowCmd(int iShowCmd);
                /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
                void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                    int cchIconPath, out int piIcon);
                /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
                void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
                /// <summary>Sets the relative path to the Shell link object</summary>
                void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
                /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
                void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
                /// <summary>Sets the path and file name of a Shell link object</summary>
                void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
            }


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
                    return ((long)High << 32) | (uint)Low;
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

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SHELLEXECUTEINFO
            {
                public int cbSize;
                public uint fMask;
                public IntPtr hwnd;
                [MarshalAs(UnmanagedType.LPTStr)]
                public string lpVerb;
                [MarshalAs(UnmanagedType.LPTStr)]
                public string lpFile;
                [MarshalAs(UnmanagedType.LPTStr)]
                public string lpParameters;
                [MarshalAs(UnmanagedType.LPTStr)]
                public string lpDirectory;
                public int nShow;
                public IntPtr hInstApp;
                public IntPtr lpIDList;
                [MarshalAs(UnmanagedType.LPTStr)]
                public string lpClass;
                public IntPtr hkeyClass;
                public uint dwHotKey;
                public IntPtr hIcon;
                public IntPtr hProcess;
            }


            public enum ShowCommands : int
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
