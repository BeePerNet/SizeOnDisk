using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WixTools
{
    public static class WixActions
    {
        [CustomAction]
        public static ActionResult GetLanguages(Session session)
        {
            try
            {
                ICollection<CultureInfo> cultures = WinApiTools.GetInstalledCultures();

                string WinInstalledLanguages = string.Join(",", cultures.Select(T => T.TwoLetterISOLanguageName).Distinct().ToArray());

                session.Log("WinInstalledLanguages {0}", string.Join(",", cultures.Select(T => T.Name).ToArray()));
                session["WinInstalledLanguages"] = WinInstalledLanguages;
            }
            catch (Exception ex)
            {
                session.Log("ERROR in custom action GetLanguages {0}", ex.ToString());
                return ActionResult.Failure;
            }
            return ActionResult.Success;
        }
    }
}
