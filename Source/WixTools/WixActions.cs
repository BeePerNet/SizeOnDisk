using Microsoft.Deployment.WindowsInstaller;
using System;
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
                string WinInstalledLanguages = string.Join(",", WinApiTools.GetCultures(WinApiTools.LocaleType.LocaleAll).Select(T => T.TwoLetterISOLanguageName).Distinct().ToArray());

                session.Log("WinInstalledLanguages {0}", WinInstalledLanguages);
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
