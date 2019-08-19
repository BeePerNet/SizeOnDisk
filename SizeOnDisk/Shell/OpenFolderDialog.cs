using System;
using System.IO;
using System.Windows.Forms;
using WPFLocalizeExtension.Extensions;

namespace SizeOnDisk.Shell
{
    //Copyright (c) 2011 Josip Medved <jmedved@jmedved.com>  http://www.jmedved.com

    internal class OpenFolderDialog
    {
        /// <summary>
        /// Gets/sets folder in which dialog will be open.
        /// </summary>
        public string InitialFolder { get; set; }

        /// <summary>
        /// Gets/sets directory in which dialog will be open if there is no recent directory available.
        /// </summary>
        public string DefaultFolder { get; set; }

        /// <summary>
        /// Gets selected folder.
        /// </summary>
        public string Folder { get; private set; }


        internal bool ShowDialog(IWin32Window owner)
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                return ShowVistaDialog(owner);
            }

            return ShowLegacyDialog(owner);
        }

        private bool ShowVistaDialog(IWin32Window owner)
        {
            Folder = ShellHelper.ShowVistaDialog(owner, InitialFolder, DefaultFolder);
            return !string.IsNullOrEmpty(Folder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "<En attente>")]
        private bool ShowLegacyDialog(IWin32Window owner)
        {
            using (OpenFileDialog frm = new OpenFileDialog())
            {
                frm.CheckFileExists = false;
                frm.CheckPathExists = true;
                //frm.CreatePrompt = false;
                //frm.Filter = "|" + Guid.Empty.ToString();
                //frm.FileName = "any";
                if (InitialFolder != null) { frm.InitialDirectory = InitialFolder; }
                //frm.OverwritePrompt = false;
                frm.Title = LocExtension.GetLocalizedValue<string>("ChooseFolder");
                frm.ValidateNames = false;
                if (frm.ShowDialog(owner) == DialogResult.OK)
                {
                    Folder = Path.GetDirectoryName(frm.FileName);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }


}
