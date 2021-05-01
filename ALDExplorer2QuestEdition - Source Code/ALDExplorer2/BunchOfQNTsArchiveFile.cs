using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Windows.Forms;

namespace ALDExplorer2
{
    class BunchOfQNTFilesArchiveFile : BunchOfFilesArchiveFileBase
    {
        public override string[] SupportedExtensions
        {
            get { return new[] { ".dlf", ".dtx", ".red" }; }
        }

        protected override string ExtensionOfFiles
        {
            get { return ".qnt"; }
        }

        //static readonly byte[] pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A };
        static readonly byte[] qntSignature = new byte[] { 0x51, 0x4E, 0x54, 0x00, 0x02, 0x00 };

        protected override void FindOffsets(string fileName)
        {
            int lookForLength = qntSignature.Length;
            int bufferSize = 256 * 1024;
            int bufferExtra = 16;
            int fileOffset = 0;
            int fileSize;

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileSize = (int)fs.Length;
                int remainingBytes = fileSize;

                byte[] buffer = new byte[bufferSize + bufferExtra];
                while (remainingBytes > 0)
                {
                    //copy previous last 16 bytes to start of buffer
                    Array.Copy(buffer, bufferSize + bufferExtra - bufferExtra, buffer, 0, bufferExtra);

                    int readCount = bufferSize;
                    if (readCount > remainingBytes)
                    {
                        readCount = remainingBytes;
                    }
                    remainingBytes -= readCount;
                    fs.Read(buffer, bufferExtra, readCount);

                    int startIndex = 0;
                    while (true)
                    {
                        int index = FindBytes(buffer, qntSignature, startIndex, readCount + bufferExtra);
                        if (index == -1)
                        {
                            break;
                        }
                        startIndex = index + 1;

                        int foundIndex = index + fileOffset - bufferExtra;
                        if (this.Offsets.GetOrDefault(this.Offsets.Count - 1, -1) != foundIndex)
                        {
                            this.Offsets.Add(foundIndex);
                        }
                    }
                    fileOffset += readCount;
                }
            }
        }

        private static int FindBytes(byte[] buffer, byte[] lookFor, int startIndex, int bufferLength)
        {
            int limit = bufferLength - lookFor.Length;
            for (int i = startIndex; i < limit; i++)
            {
                int i2 = 0;
                for (i2 = 0; i2 < lookFor.Length; i2++)
                {
                    if (buffer[i + i2] != lookFor[i2])
                    {
                        break;
                    }
                }
                if (i2 == lookFor.Length)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    abstract class BunchOfFilesArchiveFileBase : ArchiveFile
    {
        protected abstract string ExtensionOfFiles { get; }
        protected List<int> Offsets = new List<int>();

        public override bool ReadFile(string fileName)
        {
            this.FileEntries.Clear();
            this.FileLetter = -1;
            this.FileType = ArchiveFileType.BunchOfFiles;

            int archiveFileSize = GetFileSize(fileName);
            string offsetsFileName = ReadOffsetsFileFirst(fileName, archiveFileSize);

            if (this.Offsets.Count == 0)
            {
                //find offsets
                FindOffsets(fileName);

                if (this.Offsets.Count > 0)
                {
                    WriteOffsetFile(offsetsFileName);
                }
                else
                {
                    return false;
                }
            }

            //process offsets file to create archive file entries

            string filePrefix = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            string suffix = this.ExtensionOfFiles;
            for (int i = 0; i < this.Offsets.Count; i++)
            {
                int offset = this.Offsets[i];
                int nextOffset = this.Offsets.GetOrDefault(i + 1, archiveFileSize);
                var fileEntry = new ArchiveFileEntry();
                fileEntry.FileAddress = offset;
                fileEntry.FileHeader = null;
                fileEntry.FileLetter = 0;
                fileEntry.FileName = filePrefix + i.ToString("0000", CultureInfo.InvariantCulture) + suffix;
                fileEntry.FileNumber = i;
                fileEntry.FileSize = nextOffset - offset;
                fileEntry.ExtraInformation = fileEntry.FileSize;
                fileEntry.HeaderAddress = offset;
                this.FileEntries.Add(fileEntry);
            }
            return true;
        }

        private string ReadOffsetsFileFirst(string fileName, int archiveFileSize)
        {
            string offsetsFileName = Path.ChangeExtension(fileName, ".offsets");

            try
            {
                if (archiveFileSize >= int.MaxValue)
                {
                    throw new InvalidDataException("File " + fileName + " is too big (over 2GB)");
                }
                if (archiveFileSize < 1024)
                {
                    throw new InvalidDataException("File " + fileName + " is too small");
                }

                if (File.Exists(offsetsFileName))
                {
                    ReadOffsetFile(offsetsFileName, archiveFileSize);
                }
            }
            catch (InvalidDataException)
            {
                this.Offsets.Clear();
            }
            return offsetsFileName;
        }

        private static int GetFileSize(string fileName)
        {
            int archiveFileSize;
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length >= int.MaxValue)
                {
                    archiveFileSize = int.MaxValue;
                }
                else
                {
                    archiveFileSize = (int)fs.Length;
                }
            }
            return archiveFileSize;
        }

        protected abstract void FindOffsets(string fileName);

        private void WriteOffsetFile(string offsetsFileName)
        {
            using (FileStream fs = new FileStream(offsetsFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    foreach (var offset in this.Offsets)
                    {
                        bw.Write((int)offset);
                    }
                    bw.Flush();
                }
                //fs.Flush();
                //fs.Close();
            }
        }

        private void ReadOffsetFile(string offsetsFileName, int archiveFileSize)
        {
            List<int> offsets = new List<int>();
            int length, count;

            using (FileStream fs = new FileStream(offsetsFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fs.Length > 65536 * 4)
                {
                    throw new InvalidDataException("File is too big to be an offsets file.");
                }
                length = (int)fs.Length;
                count = (((length - 1) | 3) + 1) / 4;
                using (BinaryReader br = new BinaryReader(fs))
                {
                    for (int i = 0; i < count; i++)
                    {
                        offsets.Add(br.ReadInt32());
                    }
                }
            }
            //verify that offsets are strictly increasing, provide at least 32 bytes between files, and between 0 and archiveFileSize - 32
            int lastValue = -32;

            for (int i = 0; i < count; i++)
            {
                int value = offsets[i];
                int difference = value - lastValue;
                if (value >= 0 && value < archiveFileSize - 32 && difference >= 32)
                {

                }
                else
                {
                    throw new InvalidDataException("Offsets file contains invalid values");
                }
                lastValue = value;
            }

            this.Offsets = offsets;
        }

        public override void SaveToFile(string fileName)
        {
            fileName = Path.ChangeExtension(fileName, Path.GetExtension(this.ArchiveFileName));

            if (fileName != this.ArchiveFileName)
            {
                throw new InvalidOperationException("File must be saved in place.");
            }

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                foreach (var entry in this.FileEntries)
                {
                    if (entry.HasReplacementData())
                    {
                        int originalLength;
                        if (entry.ExtraInformation is int || entry.ExtraInformation is long || entry.ExtraInformation is uint || entry.ExtraInformation is ulong)
                        {
                            originalLength = Convert.ToInt32(entry.ExtraInformation);
                        }
                        else
                        {
                            originalLength = (int)entry.FileSize;
                        }
                        var replacementBytes = entry.GetFileData();
                        if (replacementBytes.Length > originalLength)
                        {
                            MessageBox.Show("File " + entry.FileName + " is too big, reduce its file size." + Environment.NewLine + "(Hint: converting to 256 colors may possibly help)");
                        }
                        else
                        {
                            fs.Seek(entry.FileAddress, SeekOrigin.Begin);
                            fs.Write(replacementBytes, 0, replacementBytes.Length);
                        }
                    }
                }
            }

        }

        public override void CommitTempFile(string newFileName, string tempFile)
        {
            //Do Nothing
        }
    }
}
