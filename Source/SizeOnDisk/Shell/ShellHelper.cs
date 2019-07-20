using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using SizeOnDisk.Utilities;
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
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
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
            //IApplicationActivationManager iappActiveManager = (IApplicationActivationManager)appActiveManager;
            appActiveManager.ActivateApplication(appId, arguments, SafeNativeMethods.ActivateOptions.None, out uint pid);
        }

        public static void Activate(string appId, string file, string verb)
        {
            SafeNativeMethods.ApplicationActivationManager appActiveManager = new SafeNativeMethods.ApplicationActivationManager();//Class not registered
            Guid guid = new Guid(SafeNativeMethods.IShellItemGuid);
            if (SafeNativeMethods.SHCreateItemFromParsingName(file, IntPtr.Zero, ref guid, out SafeNativeMethods.IShellItem pShellItem) == (int)SafeNativeMethods.HResult.Ok)
            {
                if (SafeNativeMethods.SHCreateShellItemArrayFromShellItem(pShellItem, typeof(SafeNativeMethods.IShellItemArray).GUID, out SafeNativeMethods.IShellItemArray pShellItemArray) == (int)SafeNativeMethods.HResult.Ok)
                {
                    appActiveManager.ActivateForFile(appId, pShellItemArray, verb, out uint pid);
                }
            }
        }

        public static string GetFriendlyName(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;
            //return ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.FriendlyDocName, extension);
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
                                    return SafeNativeMethods.ExtractStringFromDLL(values[0], int.Parse(values[1], CultureInfo.InvariantCulture));
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
            ShellCommandVerb verb = new ShellCommandVerb
            {
                Verb = id,
                Name = id
            };

            string name = verbkey.GetValue("MUIVerb", string.Empty).ToString();
            if (string.IsNullOrEmpty(name))
                name = verbkey.GetValue(string.Empty, string.Empty).ToString();
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
                name = LocExtension.GetLocalizedValue<string>($"PresentationCore:ExceptionStringTable:{id}Text");
                if (!string.IsNullOrEmpty(name))
                    verb.Name = name;
                name = LocExtension.GetLocalizedValue<string>($"{id}");
                if (!string.IsNullOrEmpty(name))
                    verb.Name = name;
            }

            if (id.ToUpperInvariant() == "RUNASUSER")
            {
                verb.Command = "cmd:runasuser";
                return verb;
            }

            RegistryKey cmd = verbkey.OpenSubKey("command");
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
                                    verb.Command = name;
                            }
                            if (string.IsNullOrEmpty(name))
                            {
                                cmd = cmd.OpenSubKey("InProcServer32");
                                if (cmd != null)
                                {
                                    name = cmd.GetValue(string.Empty, string.Empty).ToString();
                                    if (!string.IsNullOrEmpty(name))
                                        verb.Command = "dll:" + name;
                                }
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(verb.Command))
            {
                if (!string.IsNullOrEmpty(appUserModeId))
                    verb.Command = "Id:" + appUserModeId;
            }
            if (string.IsNullOrEmpty(verb.Command))
            {

            }
            return verb;
        }

        public enum ShellIconSize
        {
            SmallIcon, LargeIcon
        }










        private static ShellCommandSoftware GetSoftware(RegistryKey appkey, string id)
        {
            ShellCommandSoftware soft = new ShellCommandSoftware
            {
                Id = id
            };

            RegistryKey subkey;

            soft.Name = ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.FriendlyAppName, id);
            string value = ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.DefaultIcon, id);
            soft.Icon = GetIcon(value);
            if (soft.Icon == null)
            {
                value = ShellHelper.FileExtentionInfo(ShellHelper.AssocStr.AppIconReference, id);
                soft.Icon = GetIcon(value);
            }
            if (string.IsNullOrWhiteSpace(soft.Name))
            {
                value = appkey.GetValue("FriendlyTypeName", string.Empty).ToString();
                soft.Name = GetText(value);
            }
            if (string.IsNullOrWhiteSpace(soft.Name))
            {
                value = appkey.GetValue("DisplayName", string.Empty).ToString();
                soft.Name = GetText(value);
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
                    soft.Verbs.Add(verb);
                    if (defaultverb == appverb)
                        soft.Default = verb;
                }
            }
            if (string.IsNullOrEmpty(soft.Name))
            {
                string application = appkey.GetValue(string.Empty, string.Empty).ToString();
                if (!string.IsNullOrWhiteSpace(application))
                    soft.Name = application;
                else
                    soft.Name = id;
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
                        subvalues.AddRange(key.GetValueNames());

                    if (!string.IsNullOrWhiteSpace(defaultApp))
                    {
                        if (subvalues.Contains(defaultApp))
                            subvalues.Remove(defaultApp);
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
                            result.Softwares.Add(soft);
                            if (application == subkey)
                            {
                                result.Default = soft;
                            }
                        }
                    }








                }
            }
            return result;
        }


        /*public static void Test()
{
    List<(string, string)> items = new List<(string, string)>();

    const string extension = ".jpg";

    IntPtr pEnumAssocHandlers;
    SafeNativeMethods.SHAssocEnumHandlers(extension, SafeNativeMethods.ASSOC_FILTER.ASSOC_FILTER_RECOMMENDED, out pEnumAssocHandlers);

    IntPtr pFuncNext = Marshal.ReadIntPtr(Marshal.ReadIntPtr(pEnumAssocHandlers) + 3 * sizeof(int));
    SafeNativeMethods.FuncNext next = (SafeNativeMethods.FuncNext)Marshal.GetDelegateForFunctionPointer(pFuncNext, typeof(SafeNativeMethods.FuncNext));

    IntPtr[] pArrayAssocHandlers = new IntPtr[255];
    int num;

    int resNext = next(pEnumAssocHandlers, 255, pArrayAssocHandlers, out num);
    if (resNext == 0)
    {
        for (int i = 0; i < num; i++)
        {
            IntPtr pAssocHandler = pArrayAssocHandlers[i];
            IntPtr pFuncGetName = Marshal.ReadIntPtr(Marshal.ReadIntPtr(pAssocHandler) + 3 * sizeof(int));
            SafeNativeMethods.FuncGetName getName = (SafeNativeMethods.FuncGetName)Marshal.GetDelegateForFunctionPointer(pFuncGetName, typeof(SafeNativeMethods.FuncGetName));
            IntPtr pName;
            int resGetName = getName(pAssocHandler, out pName);
            string path = Marshal.PtrToStringUni(pName);

            IntPtr pFuncGetUiName = Marshal.ReadIntPtr(Marshal.ReadIntPtr(pAssocHandler) + 4 * sizeof(int));
            SafeNativeMethods.FuncGetUiName getUiName = (SafeNativeMethods.FuncGetUiName)Marshal.GetDelegateForFunctionPointer(pFuncGetUiName, typeof(SafeNativeMethods.FuncGetUiName));
            IntPtr pUiName;
            int resGetUiName = getUiName(pAssocHandler, out pUiName);
            string uiname = Marshal.PtrToStringUni(pUiName);

            Marshal.Release(pArrayAssocHandlers[i]);

            items.Add((path, uiname));
        }
    }
    Marshal.Release(pEnumAssocHandlers);
}*/





        /*public static Icon GetIconForExtension(string extension, ShellIconSize size = ShellIconSize.SmallIcon)
        {
            RegistryKey keyForExt = Registry.ClassesRoot.OpenSubKey(extension);
            if (keyForExt == null) return null;

            string className = Convert.ToString(keyForExt.GetValue(null));
            RegistryKey keyForClass = Registry.ClassesRoot.OpenSubKey(className);
            if (keyForClass == null) return null;

            RegistryKey keyForIcon = keyForClass.OpenSubKey("DefaultIcon");
            if (keyForIcon == null)
            {
                RegistryKey keyForCLSID = keyForClass.OpenSubKey("CLSID");
                if (keyForCLSID == null) return null;

                string clsid = "CLSID\\"
                    + Convert.ToString(keyForCLSID.GetValue(null))
                    + "\\DefaultIcon";
                keyForIcon = Registry.ClassesRoot.OpenSubKey(clsid);
                if (keyForIcon == null) return null;
            }

            string[] defaultIcon = Convert.ToString(keyForIcon.GetValue(null)).Split(',');
            int index = (defaultIcon.Length > 1) ? Int32.Parse(defaultIcon[1]) : 0;

            IntPtr[] handles = new IntPtr[1];
            if (SafeNativeMethods.ExtractIconEx(defaultIcon[0], index,
                (size == ShellIconSize.LargeIcon) ? handles : null,
                (size == ShellIconSize.SmallIcon) ? handles : null, 1) > 0)
                return Icon.FromHandle(handles[0]);
            else
                return null;
        }*/


        [SuppressMessage("Microsoft.Performance", "CA1810")]
        static ShellHelper()
        {
        }

        #region public functions

        public static string FileExtentionInfo(AssocStr assocStr, string doctype)
        {
            IntPtr pcchOut = IntPtr.Zero;
            if (SafeNativeMethods.AssocQueryString(AssocF.Verify, assocStr, doctype, null, null, ref pcchOut) != 1)
                return null;
            StringBuilder pszOut = new StringBuilder((int)pcchOut);
            if (SafeNativeMethods.AssocQueryString(AssocF.Verify, assocStr, doctype, null, pszOut, ref pcchOut) != 0)
                return null;
            return pszOut.ToString();
        }

        /*public static void VerbExecute(string command)
        {
            uint bufferSize = 260;
            var buffer = new StringBuilder((int)bufferSize);
            SafeNativeMethods.AssocQueryString(AssocF.IsProtocol, AssocStr.Command, "http", "open", buffer, ref bufferSize);
            var template = buffer.ToString();

            string application, commandLine, parameters;
            SafeNativeMethods.SHEvaluateSystemCommandTemplate(template, out application, out commandLine, out parameters);

            parameters = parameters.Replace("%1", "\"? " + command + "\"");

            Process.Start(application, parameters);
        }*/
        public static void ShellExecuteOpenAs(string filename)
        {
            ShellHelper.ShellExecute("Rundll32.exe", $"Shell32.dll,OpenAs_RunDLL {filename}");
        }


        public static void ShellExecute(string fileName, string arguments = null, string verb = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Verb = verb,
                Arguments = arguments,
                CreateNoWindow = true,
            };
            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            /*SafeNativeMethods.SHELLEXECUTEINFO info = new SafeNativeMethods.SHELLEXECUTEINFO
            {
                hwnd = ownerWindow,
                lpVerb = verb,
                lpFile = fileName,
                lpParameters = parameters,
                nShow = SafeNativeMethods.SW_SHOW,
                fMask = (uint)SafeNativeMethods.ShellExecuteFlags.SEE_MASK_FLAG_NO_UI
                | (uint)SafeNativeMethods.ShellExecuteFlags.SEE_MASK_UNICODE
                | (!string.IsNullOrWhiteSpace(verb) && (verb != "find") ? (uint)SafeNativeMethods.ShellExecuteFlags.SEE_MASK_INVOKEIDLIST : 0)
            };
            info.cbSize = Marshal.SizeOf(info);
            SafeNativeMethods.ShellExecuteEx(ref info);
            if (info.hInstApp.ToInt64() <= 32)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Invalid operation ({1}) on file {0}", fileName,
                    (SafeNativeMethods.ShellExecuteReturnCodes)info.hInstApp.ToInt32()));
            }*/
        }


        public static BitmapSource GetIcon(string path, int size = 16, bool thumbnail = false, bool cache = false)
        {
            // Create a native shellitem from our path
            Guid guid = new Guid(SafeNativeMethods.IShellItemGuid);
            int retCode = SafeNativeMethods.SHCreateItemFromParsingName(path, IntPtr.Zero, ref guid, out SafeNativeMethods.IShellItem nativeShellItem);
            if (retCode < 0)
                throw new ExternalException("ShellObjectFactoryUnableToCreateItem", Marshal.GetExceptionForHR(retCode));


            SafeNativeMethods.Size nativeSIZE = new SafeNativeMethods.Size
            {
                Width = Convert.ToInt32(size),
                Height = Convert.ToInt32(size)
            };

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
                    ExceptionBox.ShowException(ex);
            }
            catch (Exception ex)
            {
                ExceptionBox.ShowException(ex);
            }
        }

        public static long? GetCompressedFileSize(string fileName)
        {
            using (SafeFileHandle handle = SafeNativeMethods.OpenHandle(fileName))
            {
                if (handle != null)
                {

                    if (!SafeNativeMethods.GetFileInformationByHandleEx(handle, SafeNativeMethods.FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo, out SafeNativeMethods.FILE_STANDARD_INFO dirinfo, (uint)Marshal.SizeOf(typeof(SafeNativeMethods.FILE_STANDARD_INFO))))
                    {
                        int win32Error = Marshal.GetLastWin32Error();
                        if (win32Error != 0)
                            throw new Win32Exception(win32Error);
                        return dirinfo.AllocationSize.ToInt64();
                    }
                }
            }
            return null;
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
                IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
                IntPtr ActivateForFile([In] String appUserModelId, [In] IShellItemArray /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
                IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
            }

            [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]//Application Activation Manager
            internal class ApplicationActivationManager : IApplicationActivationManager
            {
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
                public extern IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public extern IntPtr ActivateForFile([In] String appUserModelId, [In] IShellItemArray /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                public extern IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
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
                public Guid FormatId => this.fmtid;
                public int PropertyId => this.pid;

                public REFPROPERTYKEY(Guid formatId, int propertyId)
                {
                    this.fmtid = formatId;
                    this.pid = propertyId;
                }
                public static readonly REFPROPERTYKEY PKEY_DateCreated = new REFPROPERTYKEY(new Guid("B725F130-47EF-101A-A5F1-02608C9EEBAC"), 15);
            }

            public enum SIATTRIBFLAGS
            {
                SIATTRIBFLAGS_AND = 0x1,
                SIATTRIBFLAGS_OR = 0x2,
                SIATTRIBFLAGS_APPCOMPAT = 0x3,
                SIATTRIBFLAGS_MASK = 0x3,
                SIATTRIBFLAGS_ALLITEMS = 0x4000
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


            [Flags]
            public enum ASSOC_FILTER
            {
                ASSOC_FILTER_NONE = 0x00000000,
                ASSOC_FILTER_RECOMMENDED = 0x00000001
            }

            // IEnumAssocHandlers
            [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            internal delegate int FuncNext(IntPtr refer, int celt, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 1)] IntPtr[] rgelt, [Out] out int pceltFetched);

            // IAssocHandler
            [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            internal delegate int FuncGetName(IntPtr refer, out IntPtr ppsz);

            [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            internal delegate int FuncGetUiName(IntPtr refer, out IntPtr ppsz);



            [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
            internal static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);


            [DllImport("Shlwapi.dll", EntryPoint = "AssocQueryStringW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true, ThrowOnUnmappableChar = true)]
            internal static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, [In][Out] ref IntPtr pcchOut);


            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
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

                /// <summary>
                /// Width
                /// </summary>
                public int Width { get; set; }

                /// <summary>
                /// Height
                /// </summary>
                public int Height { get; set; }
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

            [DllImport("user32.dll", EntryPoint = "DestroyIcon",
                SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

            public static BitmapSource ExtractIconFromDLL(string file, int index)
            {
                if (file.StartsWith("@%", StringComparison.Ordinal))
                    file = file.Substring(1);

                IntPtr[] handles = new IntPtr[1];
                System.IntPtr hIcon = IntPtr.Zero;

                if (SafeNativeMethods.ExtractIconEx(file, index, null, handles, 1) > 0)
                    hIcon = handles[0];

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

                    return wpfBitmap;                    //returnValue.Freeze();
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
                    file = file.Substring(1);
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
