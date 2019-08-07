using System.Linq;

namespace SizeOnDisk.ViewModel
{
    public class VMFolderDetails : VMFileDetails
    {
        public VMFolderDetails(VMFolder vmFile) : base(vmFile)
        {
        }

        private bool initialized = false;

        public void ResetMaxCalculation()
        {
            initialized = false;
            this.OnPropertyChanged(nameof(MaxFileTotal));
            this.OnPropertyChanged(nameof(MaxFolderTotal));
            this.OnPropertyChanged(nameof(MaxDiskSize));
            this.OnPropertyChanged(nameof(MaxFileSize));
        }

        private void Calculate()
        {
            if (!initialized)
            {
                VMFolder folder = this.VMFile as VMFolder;
                _MaxFileTotal = folder.Folders.Max(T => T.FileTotal ?? 0);
                _MaxFolderTotal = folder.Folders.Max(T => T.FolderTotal ?? 0);
                _MaxDiskSize = folder.Folders.Max(T => T.DiskSize ?? 0);
                _MaxFileSize = folder.Folders.Max(T => T.FileSize ?? 0);
                initialized = true;
            }
        }


        private ulong _MaxFileTotal;
        public ulong MaxFileTotal
        {
            get
            {
                Calculate();
                return _MaxFileTotal;
            }
        }

        private ulong _MaxFolderTotal;
        public ulong MaxFolderTotal
        {
            get
            {
                Calculate();
                return _MaxFolderTotal;
            }
        }


        private ulong _MaxDiskSize;
        public ulong MaxDiskSize
        {
            get
            {
                Calculate();
                return _MaxDiskSize;
            }
        }

        private ulong _MaxFileSize;
        public ulong MaxFileSize
        {
            get
            {
                Calculate();
                return _MaxFileSize;
            }
        }


    }
}
