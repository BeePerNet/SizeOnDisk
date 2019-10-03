using System;
using System.Runtime.InteropServices;

namespace SizeOnDisk.ViewModel
{
    /// <summary>Fournit des attributs pour les fichiers et répertoires.</summary>
    [Flags]
    [ComVisible(true)]
    [Serializable]
    public enum FileAttributesEx : uint
    {
        Error = 0x0,
        /// <summary>Le fichier est en lecture seule.</summary>
        ReadOnly = 0x1,
        /// <summary>Le fichier est masqué et n’est donc pas compris dans un listing de répertoires ordinaire.</summary>
        Hidden = 0x2,
        /// <summary>Le fichier est un fichier système. Autrement dit, le fichier fait partie du système d’exploitation ou est utilisé exclusivement par le système d’exploitation.</summary>
        System = 0x4,
        /// <summary>Le fichier est un répertoire.</summary>
        Directory = 0x10, // 0x00000010
                          /// <summary>Le fichier est candidat pour la sauvegarde ou la suppression.</summary>
        Archive = 0x20, // 0x00000020
                        /// <summary>Réservé à un usage ultérieur.</summary>
        Device = 0x40, // 0x00000040
                       /// <summary>Le fichier est un fichier standard qui n’a pas d’attributs spéciaux. Cet attribut est valide uniquement s’il est utilisé seul.</summary>
        Normal = 0x80, // 0x00000080
                       /// <summary>Le fichier est temporaire. Un fichier temporaire contient les données nécessaires quand une application s’exécute, mais qui ne le sont plus une fois l’exécution terminée. Les systèmes de fichiers tentent de conserver toutes les données en mémoire pour un accès plus rapide plutôt que de les vider dans le stockage de masse. Un fichier temporaire doit être supprimé par l’application dès qu’il n’est plus nécessaire.</summary>
        Temporary = 0x100, // 0x00000100
                           /// <summary>Le fichier est un fichier partiellement alloué. Les fichiers partiellement alloués sont généralement de gros fichiers dont les données sont principalement des zéros.</summary>
        SparseFile = 0x200, // 0x00000200
                            /// <summary>Le fichier contient un point d’analyse, qui est un bloc de données définies par l’utilisateur associé à un fichier ou à un répertoire.</summary>
        ReparsePoint = 0x400, // 0x00000400
                              /// <summary>Le fichier est compressé.</summary>
        Compressed = 0x800, // 0x00000800
                            /// <summary>Le fichier est hors connexion. Les données du fichier ne sont pas immédiatement disponibles.</summary>
        Offline = 0x1000, // 0x00001000
                          /// <summary>Le fichier ne sera pas indexé par le service d’indexation de contenu du système d’exploitation.</summary>
        NotContentIndexed = 0x2000, // 0x00002000
                                    /// <summary>Le fichier ou le répertoire est chiffré. Cela signifie pour un fichier, que toutes ses données sont chiffrées. Pour un répertoire, cela signifie que tous les fichiers et répertoires créés sont chiffrés par défaut.</summary>
        Encrypted = 0x4000, // 0x00004000

        WriteThrough = 0x80000000,
        Overlapped = 0x40000000,
        NoBuffering = 0x20000000,
        RandomAccess = 0x10000000,
        SequentialScan = 0x08000000,
        DeleteOnClose = 0x04000000,
        BackupSemantics = 0x02000000,
        PosixSemantics = 0x01000000,
        OpenReparsePoint = 0x00200000,
        OpenNoRecall = 0x00100000,
        FirstPipeInstance = 0x00080000,

        /// <summary>Le fichier ou le répertoire inclut la prise en charge de l’intégrité des données. Quand cette valeur est appliquée à un fichier, tous les flux de données du fichier bénéficient de la prise en charge de l’intégrité des données. Quand cette valeur est appliquée à un répertoire, tous les nouveaux fichiers et sous-répertoires de ce répertoire incluent par défaut la prise en charge de l’intégrité.</summary>
        [ComVisible(false)] IntegrityStream = 0x8000, // 0x00008000
                                                      /// <summary>Le fichier ou le répertoire est exclu de l’analyse d’intégrité des données. Quand cette valeur est appliquée à un répertoire, tous les nouveaux fichiers et sous-répertoires de ce répertoire sont exclus par défaut de l’analyse d’intégrité des données.</summary>
        [ComVisible(false)] NoScrubData = 0x00020000, // 0x00020000
                     
        Protected = 0x1000000,

        Selected = 0x2000000,

        Expanded = 0x4000000,

        TreeSelected = 0x8000000,

        FileSizeValue = 0x10000000,

        DiskSizeValue = 0x20000000,

        FileTotalValue = 0x40000000,

        FolderTotalValue = 0x80000000,

        Mask=0xFF000000
    }
}
