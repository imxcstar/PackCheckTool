using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackCheckTool
{
    public class PackFileInfo
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public bool IsDir { get; set; }

        public long Size { get; set; }

        public DateTime? LastModifiedTime { get; set; }

        private IArchiveEntry _archiveEntry;

        public PackFileInfo(IArchiveEntry archiveEntry)
        {
            _archiveEntry = archiveEntry;
        }

        public Stream? OpenStream()
        {
            return _archiveEntry.OpenEntryStream();
        }
    }
}
