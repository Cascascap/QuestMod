using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ALDExplorer2
{
    public class VfsFileEntryExtraData
    {
        public DateTime? DateTime;
        public UInt16 FileAttributes;
        public Byte VolumeId;
    }

    public class VfsArchiveFile : ArchiveFile
    {
        public override string[] SupportedExtensions
        {
            get
            {
                return new string[] { ".vfs" };
            }
        }

        public override bool ReadFile(string fileName)
        {
            this.FileType = ArchiveFileType.SofthouseCharaVfs11File;

            this.ArchiveFileName = fileName;
            using (var inputFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var br = new BinaryReader(inputFileStream);
                var filesInVfs = ReadFileEntries(br);
                if (filesInVfs == null)
                {
                    return false;
                }

                foreach (var entry in filesInVfs)
                {
                    entry.Parent = this;
                    this.FileEntries.Add(entry);
                }

                //foreach (var fileInfo in filesInVfs)
                //{
                //    string extension = Path.GetExtension(fileInfo.fileName).ToLowerInvariant();

                //    if (extension == ".box")
                //    {
                //        string outputDirectoryName = Path.Combine(exportPath, fileInfo.fileName);
                //        if (!Directory.Exists(outputDirectoryName))
                //        {
                //            Directory.CreateDirectory(outputDirectoryName);
                //        }
                //        ExtractBoxFile(br, outputDirectoryName, fileInfo);
                //    }
                //    else
                //    {
                //        string outputDirectoryName = exportPath;
                //        string outputFileName = Path.Combine(outputDirectoryName, fileInfo.fileName);
                //        using (var outputFileStream = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                //        {
                //            br.BaseStream.Position = fileInfo.address;
                //            if (extension == ".obj")
                //            {
                //                var bytes = br.ReadBytes(fileInfo.length);
                //                EncryptObjFile(bytes);
                //                outputFileStream.Write(bytes, 0, bytes.Length);

                //                var strings = GetStringsFromObjFile(bytes);

                //                string textFileName = Path.ChangeExtension(outputFileName, ".txt");
                //                File.WriteAllLines(textFileName, strings, shiftJis);
                //            }
                //            else
                //            {
                //                CopyStream(outputFileStream, br.BaseStream, fileInfo.length);
                //            }
                //            outputFileStream.Flush();
                //            outputFileStream.Close();
                //        }
                //        if (extension == ".iph")
                //        {
                //            ConvertToBitmap(outputFileName);
                //        }
                //    }

                //}
                //inputFileStream.Close();
            }

            return true;
        }

        //static readonly byte[] signature = UTF8Encoding.UTF8.GetBytes("VF\x01\x01");
        //static readonly byte[] signature2 = UTF8Encoding.UTF8.GetBytes("AOIMY01");  //Wizard's Climber
        //static readonly byte[] signature3 = UTF8Encoding.UTF8.GetBytes("AOIBOX8");  //Suzukuri Dragon

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct VfsHeaderRaw
        {
            public UInt16 Signature;
            public UInt16 Version;
            public UInt16 FileCount, RecordLength;
            public int FileTableLength, FileLength;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vfs20FileEntryRaw
        {
            public int FileNameOffset;
            public UInt16 FileAttributes, DosDate, DosTime;
            public int FileAddress;
            public int FileSize;
            public int FileSize2;
            public Byte VolumeId;

            public DateTime? DateTime
            {
                get
                {
                    return Util.DosDateTimeToDateTime(DosDate, DosTime);
                }
                set
                {
                    Util.DateTimeToDosDateTime(value, out DosDate, out DosTime);
                }
            }

            public ArchiveFileEntry ToArchiveFileEntry(BinaryReader br, long fileNameTableStartPosition)
            {
                var archiveFileEntry = new ArchiveFileEntry();
                archiveFileEntry.FileAddress = this.FileAddress /*- 2*/;

                long lastPosition = br.BaseStream.Position;
                br.BaseStream.Position = fileNameTableStartPosition + this.FileNameOffset * 2;
                archiveFileEntry.FileName = br.ReadUnicodeStringNullTerminated();
                br.BaseStream.Position = lastPosition;

                archiveFileEntry.FileSize = this.FileSize;
                archiveFileEntry.HeaderAddress = this.FileNameOffset;
                archiveFileEntry.ExtraInformation = new VfsFileEntryExtraData() { DateTime = this.DateTime, FileAttributes = this.FileAttributes, VolumeId = this.VolumeId };

                return archiveFileEntry;
            }

            public static ArchiveFileEntry[] ToArchiveFileEntries(IEnumerable<Vfs20FileEntryRaw> array, BinaryReader br, long fileNameTableStartPosition)
            {
                ArchiveFileEntry[] archiveFileEntries = new ArchiveFileEntry[array.Count()];
                int i = 0;
                foreach (var rawEntry in array)
                {
                    archiveFileEntries[i] = rawEntry.ToArchiveFileEntry(br, fileNameTableStartPosition);
                    archiveFileEntries[i].Index = i;
                    i++;
                }
                return archiveFileEntries;
            }

            public static ArchiveFileEntry[] ReadFileEntries(int fileCount, BinaryReader br)
            {
                var rawEntries = br.ReadStructs<Vfs20FileEntryRaw>(fileCount);
                int fileNameTableSize = br.ReadInt32() * 2;
                int unusedZeroes = br.ReadInt32();
                long fileNameTableStartPosition = br.BaseStream.Position;
                var entires = Vfs20FileEntryRaw.ToArchiveFileEntries(rawEntries, br, fileNameTableStartPosition);
                return entires;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct Vfs11FileEntryRaw
        {
            public unsafe fixed byte FileNameBytes[13];
            public UInt16 FileAttributes, DosDate, DosTime;
            public int FileAddress;
            public int FileSize;
            public int FileSize2;
            public byte VolumeId;

            private static Encoding shiftJis = Encoding.GetEncoding("shift_jis");

            public string FileName
            {
                get
                {
                    unsafe
                    {
                        fixed (byte* fileNameBytes = this.FileNameBytes)
                        {
                            return BinaryUtil.FixedBytesToString(fileNameBytes, 13, shiftJis);
                        }
                    }
                }
                set
                {
                    unsafe
                    {
                        fixed (byte* fileNameBytes = this.FileNameBytes)
                        {
                            BinaryUtil.StringToFixedBytes(fileNameBytes, 13, value, shiftJis);
                        }
                    }
                }
            }

            public DateTime? DateTime
            {
                get
                {
                    return Util.DosDateTimeToDateTime(DosDate, DosTime);
                }
                set
                {
                    Util.DateTimeToDosDateTime(value, out DosDate, out DosTime);
                }
            }

            public ArchiveFileEntry ToArchiveFileEntry()
            {
                var archiveFileEntry = new ArchiveFileEntry();
                archiveFileEntry.FileAddress = this.FileAddress;
                archiveFileEntry.FileName = this.FileName;
                archiveFileEntry.FileSize = this.FileSize;
                archiveFileEntry.ExtraInformation = new VfsFileEntryExtraData() { DateTime = this.DateTime, FileAttributes = this.FileAttributes, VolumeId = this.VolumeId };
                return archiveFileEntry;
            }

            public static ArchiveFileEntry[] ToArchiveFileEntries(IEnumerable<Vfs11FileEntryRaw> array)
            {
                ArchiveFileEntry[] archiveFileEntries = new ArchiveFileEntry[array.Count()];
                int i = 0;
                foreach (var rawEntry in array)
                {
                    archiveFileEntries[i] = rawEntry.ToArchiveFileEntry();
                    archiveFileEntries[i].Index = i;
                    i++;
                }
                return archiveFileEntries;
            }

            public static ArchiveFileEntry[] ReadFileEntries(int fileCount, BinaryReader br)
            {
                var rawEntries = br.ReadStructs<Vfs11FileEntryRaw>(fileCount);
                return Vfs11FileEntryRaw.ToArchiveFileEntries(rawEntries);
            }
        }

        static int vfSignature = BinaryUtil.StringToInt("VF");

        public ArchiveFileEntry[] ReadFileEntries(BinaryReader br)
        {
            br.BaseStream.Position = 0;
            VfsHeaderRaw header = br.ReadStruct<VfsHeaderRaw>();
            if (header.Signature != vfSignature)
            {
                return null;
            }
            if (br.BaseStream.Length < header.FileLength)
            {
                return null;
            }
            if (header.FileCount * header.RecordLength != header.FileTableLength)
            {
                return null;
            }
            if (header.FileTableLength > header.FileLength)
            {
                return null;
            }

            if (header.Version == 0x101)
            {
                this.FileType = ArchiveFileType.SofthouseCharaVfs11File;
                int desiredRecordLength = Marshal.SizeOf(typeof(Vfs11FileEntryRaw));
                if (header.RecordLength != desiredRecordLength)
                {
                    return null;
                }
                return Vfs11FileEntryRaw.ReadFileEntries(header.FileCount, br);
            }
            else if (header.Version == 0x200)
            {
                this.FileType = ArchiveFileType.SofthouseCharaVfs20File;
                int desiredRecordLength = Marshal.SizeOf(typeof(Vfs20FileEntryRaw));
                if (header.RecordLength != desiredRecordLength)
                {
                    return null;
                }
                return Vfs20FileEntryRaw.ReadFileEntries(header.FileCount, br);
            }
            return null;

            ////header should say "VF" 01 01

            //if (!header.Take(signature.Length).SequenceEqual(signature))
            //{
            //    return null;
            //}

            //int fileCount = BitConverter.ToUInt16(header, 4);
            //int fileTableLength = BitConverter.ToInt32(header, 8);
            //int fileLength = BitConverter.ToInt32(header, 12);

            //if (br.BaseStream.Length < fileLength)
            //{
            //    return null;
            //}

            //if (fileCount * 32 != fileTableLength)
            //{
            //    return null;
            //}

            //List<ArchiveFileEntry> list = new List<ArchiveFileEntry>();

            ////Dictionary<string, int> seenFileNames = new Dictionary<string, int>();

            //int lastAddress = 0, nextAddress = 0;
            //for (int i = 0; i < fileCount; i++)
            //{
            //    var entry = br.ReadBytes(32);
            //    string fileName = entry.ReadString(0, 13);

            //    int address = BitConverter.ToInt32(entry, 19);
            //    int length1 = BitConverter.ToInt32(entry, 23);
            //    int length2 = BitConverter.ToInt32(entry, 27);
            //    if (length1 != length2)
            //    {
            //        return null;
            //    }
            //    var archiveFileEntry = new ArchiveFileEntry();
            //    archiveFileEntry.FileAddress = address;
            //    archiveFileEntry.FileSize = length1;
            //    archiveFileEntry.FileName = fileName;
            //    archiveFileEntry.FileHeader = entry;
            //    list.Add(archiveFileEntry);

            //    if (address < nextAddress || address < lastAddress)
            //    {
            //        return null;
            //    }

            //    lastAddress = address;
            //    nextAddress = address + length1;

            //}
            //return list.ToArray();
        }

        public override void SaveToFile(string fileName)
        {
            int fileCount = this.FileEntries.Count;

            var vfsHeader = new VfsHeaderRaw();
            vfsHeader.FileCount = (UInt16)fileCount;
            vfsHeader.Signature = (UInt16)BinaryUtil.StringToInt("VF");

            switch (this.FileType)
            {
                case ArchiveFileType.SofthouseCharaVfs11File:
                    vfsHeader.Version = 0x101;
                    vfsHeader.RecordLength = (UInt16)Marshal.SizeOf(typeof(Vfs11FileEntryRaw)); //32
                    break;
                case ArchiveFileType.SofthouseCharaVfs20File:
                    vfsHeader.Version = 0x200;
                    vfsHeader.RecordLength = (UInt16)Marshal.SizeOf(typeof(Vfs20FileEntryRaw)); //17
                    break;
            }

            vfsHeader.FileTableLength = vfsHeader.RecordLength * vfsHeader.FileCount;
            int headerSize = vfsHeader.FileTableLength + Marshal.SizeOf(typeof(VfsHeaderRaw));
            int fileNameTableSize = 0;

            //sum the filename lengths
            if (this.FileType == ArchiveFileType.SofthouseCharaVfs20File)
            {
                int totalCharacterCount = 0;
                foreach (var entry in this.FileEntries)
                {
                    totalCharacterCount += entry.FileName.Length + 1;
                }
                fileNameTableSize = totalCharacterCount * 2 + 8 + 2;
            }

            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var bw = new BinaryWriter(fs);
                //write null placeholder for header and filename table
                fs.WriteZeroes(headerSize);
                fs.WriteZeroes(fileNameTableSize);

                //write the file contents
                foreach (var entry in this.FileEntries)
                {
                    int address = (int)fs.Position;
                    entry.WriteDataToStream(fs);
                    entry.FileAddress = address;
                    int endAddress = (int)fs.Position;
                    int length = endAddress - address;
                    entry.FileSize = length;

                    if (entry.HasReplacementData())
                    {
                        if (!String.IsNullOrEmpty(entry.ReplacementFileName))
                        {
                            FileInfo fileInfo = null;
                            try
                            {
                                fileInfo = new FileInfo(entry.ReplacementFileName);
                            }
                            catch
                            {

                            }
                            if (fileInfo != null)
                            {
                                var extraInformation = entry.ExtraInformation as VfsFileEntryExtraData;
                                if (extraInformation == null)
                                {
                                    extraInformation = new VfsFileEntryExtraData();
                                    entry.ExtraInformation = extraInformation;
                                }
                                extraInformation.DateTime = fileInfo.LastWriteTime;
                                extraInformation.FileAttributes = (ushort)(((int)fileInfo.Attributes) & 0x27);
                                extraInformation.VolumeId = 0;
                            }
                        }
                    }
                }

                fs.Flush();
                fs.Position = 0;

                vfsHeader.FileLength = (int)fs.Length;
                bw.WriteStruct(vfsHeader);

                if (this.FileType == ArchiveFileType.SofthouseCharaVfs11File)
                {
                    foreach (var entry in this.FileEntries)
                    {
                        var rawEntry = new Vfs11FileEntryRaw();
                        var extendedInformation = entry.ExtraInformation as VfsFileEntryExtraData;
                        if (extendedInformation != null)
                        {
                            rawEntry.DateTime = extendedInformation.DateTime;
                            rawEntry.FileAttributes = extendedInformation.FileAttributes;
                            rawEntry.VolumeId = extendedInformation.VolumeId;
                        }
                        rawEntry.FileName = entry.FileName;
                        rawEntry.FileAddress = (int)entry.FileAddress;
                        rawEntry.FileSize = (int)entry.FileSize;
                        rawEntry.FileSize2 = (int)entry.FileSize;

                        bw.WriteStruct(rawEntry);
                    }
                }
                else if (this.FileType == ArchiveFileType.SofthouseCharaVfs20File)
                {
                    int fileNameOffset = 0;
                    foreach (var entry in this.FileEntries)
                    {
                        var rawEntry = new Vfs20FileEntryRaw();
                        var extendedInformation = entry.ExtraInformation as VfsFileEntryExtraData;
                        if (extendedInformation != null)
                        {
                            rawEntry.DateTime = extendedInformation.DateTime;
                            rawEntry.FileAttributes = extendedInformation.FileAttributes;
                            rawEntry.VolumeId = extendedInformation.VolumeId;
                        }
                        rawEntry.FileNameOffset = fileNameOffset;
                        rawEntry.FileAddress = (int)(entry.FileAddress /*+ 2*/);
                        rawEntry.FileSize = (int)entry.FileSize;
                        rawEntry.FileSize2 = (int)entry.FileSize;

                        bw.WriteStruct(rawEntry);

                        fileNameOffset += entry.FileName.Length + 1;
                    }
                    if (bw.BaseStream.Position != headerSize)
                    {

                    }
                    fileNameOffset++;
                    bw.Write((int)fileNameOffset);
                    bw.Write((int)0);

                    foreach (var entry in this.FileEntries)
                    {
                        bw.WriteUnicodeStringNullTerminated(entry.FileName);
                    }
                }
                bw.Flush();
                fs.Flush();
                fs.Close();
                fs.Dispose();
            }
        }
    }

    public static partial class ReflectionUtil
    {
        public static string ToString(object obj)
        {
            StringBuilder sb = new StringBuilder();
            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);

            bool needComma = false;
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(int) || field.FieldType == typeof(string) || field.FieldType.IsEnum)
                {
                    Util.PrintComma(sb, ref needComma);
                    sb.Append(field.Name + " = " + (field.GetValue(obj) ?? "null").ToString());
                }
            }
            return sb.ToString();

        }
    }

    public static partial class Util
    {
        public static void PrintComma(StringBuilder sb, ref bool needComma)
        {
            if (needComma)
            {
                sb.Append(", ");
            }
            else
            {
                needComma = true;
            }
        }

    }

    //public static partial class Extensions
    //{
    //    //static readonly Encoding shiftJis = Encoding.GetEncoding("shift_jis");

    //    //public static string ReadString(this BinaryReader br, int length)
    //    //{
    //    //    return ReadString(br, length, shiftJis);
    //    //}

    //    //public static string ReadString(this BinaryReader br, int length, Encoding encoding)
    //    //{
    //    //    var bytes = br.ReadBytes(length);
    //    //    return ReadString(bytes, 0, length, encoding);
    //    //}

    //    public static string ReadString(this byte[] bytes, int start, int length, Encoding encoding)
    //    {
    //        int nullIndex = Array.IndexOf(bytes, (byte)0, start);
    //        if (nullIndex == -1)
    //        {
    //            nullIndex = bytes.Length;
    //        }
    //        return encoding.GetString(bytes, start, nullIndex - start);
    //    }

    //    public static string ReadString(this byte[] bytes, int start, int length)
    //    {
    //        return ReadString(bytes, start, length, shiftJis);
    //    }

    //    public static int EndianSwap(this int value)
    //    {
    //        unchecked
    //        {
    //            uint uvalue = (uint)value;
    //            uint uvalue2 = (((uvalue >> 24) & 0xFF) << 0) |
    //                           (((uvalue >> 16) & 0xFF) << 8) |
    //                           (((uvalue >> 8) & 0xFF) << 16) |
    //                           (((uvalue >> 0) & 0xFF) << 24);
    //            return (int)uvalue2;
    //        }
    //    }
    //}

    public static partial class Util
    {
        public static DateTime? DosDateTimeToDateTime(ushort dosDate, ushort dosTime)
        {
            if (dosDate == 0)
            {
                return null;
            }

            int year = (dosDate >> 9) + 1980;
            int month = (dosDate >> 5) & 0x0F;
            int day = dosDate & 0x1F;

            int hour = (dosTime >> 10) & 0x1F;
            int minute = (dosTime >> 5) & 0x1F;
            int second = (dosTime & 0x1F) * 2;

            bool nextDay = false;

            if (hour > 23)
            {
                hour -= 24;
                nextDay = true;
            }

            try
            {
                var dateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Local);
                if (nextDay)
                {
                    dateTime.AddDays(1);
                }
                return dateTime;
            }
            catch
            {
                return null;
            }
        }

        public static void DateTimeToDosDateTime(DateTime? dateTimeValue, out ushort dosDate, out ushort dosTime)
        {
            if (dateTimeValue == null)
            {
                dosDate = 0;
                dosTime = 0;
                return;
            }
            var dateTime = dateTimeValue.Value;

            int year = dateTime.Year;
            int month = dateTime.Month;
            int day = dateTime.Day;

            dosDate = (ushort)(((year - 1980) << 9) + (month << 5) + day);

            int hour = dateTime.Hour;
            int minute = dateTime.Minute;
            int second = dateTime.Second;

            dosTime = (ushort)((hour << 10) + (minute << 5) + (second / 2));
        }

        public static ArchiveFileType GetArchiveFileType(string ext, int version)
        {
            ext = ext.ToLowerInvariant();
            switch (ext)
            {
                case ".vfs":
                    if (version == 2 || version == 20)
                    {
                        return ArchiveFileType.SofthouseCharaVfs20File;
                    }
                    else
                    {
                        return ArchiveFileType.SofthouseCharaVfs11File;
                    }
            }
            return ArchiveFileType.Invalid;
        }
    }
}
