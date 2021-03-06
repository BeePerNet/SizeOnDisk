﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WixTools
{
    public static class WinApiTools
    {
        public static ICollection<CultureInfo> GetInstalledCultures()
        {
            List<CultureInfo> result = new List<CultureInfo>();
            bool enumCallback(IntPtr lpUILanguageString, IntPtr lParam)
            {
                try
                {
                    if (lpUILanguageString != IntPtr.Zero)
                    {
                        result.Add(new CultureInfo(Convert.ToInt32(Marshal.PtrToStringAuto(lpUILanguageString), 16)));
                    }
                }
                catch (Exception)
                {
                    // This culture is not supported by .NET (not happened so far)
                    // Must be ignored.
                }
                return true;
            }

            if (EnumUILanguages(enumCallback, 0, IntPtr.Zero) == false)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return result;
        }



        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumUILanguages(
            EnumUILanguagesProcDelegate lpUILanguageEnumProc,
            uint dwFlags,
            System.IntPtr lParam
            );

        private delegate bool EnumUILanguagesProcDelegate(
            System.IntPtr lpUILanguageString,
            System.IntPtr lParam
            );



        public static IEnumerable<CultureInfo> GetInstalledInputLanguages()
        {
            // first determine the number of installed languages
            uint size = GetKeyboardLayoutList(0, null);
            IntPtr[] ids = new IntPtr[size];

            // then get the handles list of those languages
            GetKeyboardLayoutList(ids.Length, ids);

            foreach (int id in ids) // note the explicit cast IntPtr -> int
            {
                yield return new CultureInfo(id & 0xFFFF);
            }
        }


        [DllImport("user32.dll")]
        private static extern uint GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);




        #region Windows API

        private delegate bool EnumLocalesProcExDelegate(
           [MarshalAs(UnmanagedType.LPWStr)]string lpLocaleString,
           LocaleType dwFlags, int lParam);

        [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool EnumSystemLocalesEx(EnumLocalesProcExDelegate pEnumProcEx,
           LocaleType dwFlags, int lParam, IntPtr lpReserved);

        [Flags]
        public enum LocaleType : uint
        {
            LocaleAll = 0x00000000,             // Enumerate all named based locales
            LocaleWindows = 0x00000001,         // Shipped locales and/or replacements for them
            LocaleSupplemental = 0x00000002,    // Supplemental locales only
            LocaleAlternateSorts = 0x00000004,  // Alternate sort locales
            LocaleNeutralData = 0x00000010,     // Locales that are "neutral" (language only, region data is default)
            LocaleSpecificData = 0x00000020,    // Locales that contain language and region data
        }

        #endregion

        [Flags]
        public enum CultureTypes : uint
        {
            SpecificCultures = LocaleType.LocaleSpecificData,
            NeutralCultures = LocaleType.LocaleNeutralData,
            AllCultures = LocaleType.LocaleWindows
        }



        public static ICollection<CultureInfo> GetCultures(
           LocaleType cultureTypes)
        {
            List<CultureInfo> cultures = new List<CultureInfo>();
            bool enumCallback(string locale, LocaleType flags, int lParam)
            {
                try
                {
                    cultures.Add(new CultureInfo(locale));
                }
                catch (Exception)
                {
                    // This culture is not supported by .NET (not happened so far)
                    // Must be ignored.
                }
                return true;
            }

            if (EnumSystemLocalesEx(enumCallback, cultureTypes, 0, IntPtr.Zero) == false)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            // Add the two neutral cultures that Windows misses 
            // (CultureInfo.GetCultures adds them also):
            if (cultureTypes == LocaleType.LocaleNeutralData ||
               cultureTypes == LocaleType.LocaleAll)
            {
                cultures.Add(new CultureInfo("zh-CHS"));
                cultures.Add(new CultureInfo("zh-CHT"));
            }

            return new ReadOnlyCollection<CultureInfo>(cultures);
        }
    }
}
