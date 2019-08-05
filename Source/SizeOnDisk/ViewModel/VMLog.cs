using System;
using System.Diagnostics;

namespace SizeOnDisk.ViewModel
{
    [DebuggerDisplay("{File.Name} {ShortText}")]
    public class VMLog
    {
        public VMLog(VMFile file, string shortText, string LongText = null) : this(DateTime.UtcNow, file, shortText, LongText) { }
        public VMLog(DateTime timeStamp, VMFile file, string shortText, string longText = null)
        {
            this.TimeStamp = timeStamp;
            this.File = file;
            this.ShortText = shortText;
            this.LongText = LongText;
            if (string.IsNullOrWhiteSpace(LongText))
                LongText = ShortText;
        }

        public DateTime TimeStamp { get; }
        public VMFile File { get; }
        public string ShortText { get; }
        public string LongText { get; }
    }
}
