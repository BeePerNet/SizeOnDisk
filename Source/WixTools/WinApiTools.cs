using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WixTools
{
    internal static class WinApiTools
    {


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
    }
}
