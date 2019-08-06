using System;
using System.Diagnostics;

namespace SizeOnDisk.ViewModel
{
    [DebuggerDisplay("{File.Name} {ShortText}")]
    public class VMLog
    {
        public VMLog(VMFile file, string shortText, string longText = null) : this(DateTime.Now, file, shortText, longText) { }
        public VMLog(DateTime timeStamp, VMFile file, string shortText, string longText = null)
        {
            TimeStamp = timeStamp;
            File = file;
            ShortText = shortText;
            LongText = longText;
            if (string.IsNullOrWhiteSpace(LongText))
            {
                LongText = ShortText;
            }
        }

        public DateTime TimeStamp { get; }
        public VMFile File { get; }
        public string ShortText { get; }
        public string LongText { get; }
    }
}
