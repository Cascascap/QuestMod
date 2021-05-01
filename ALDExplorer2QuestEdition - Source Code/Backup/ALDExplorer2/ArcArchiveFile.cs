using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace ALDExplorer2
{
    class ArcArchiveFile : ArchiveFile
    {
        public ArcArchiveFile()
        {
            this.FileType = ArchiveFileType.HoneybeeArcFile;
        }

        public override string[] SupportedExtensions
        {
            get 
            {
                return new string[] { ".arc" };
            }
        }

        public override bool ReadFile(string fileName)
        {
            var scriptFile = new ScriptFile();
            var entries = scriptFile.GetFiles(fileName);
            if (entries == null)
            {
                return false;
            }

            foreach (var entry in entries)
            {
                entry.Parent = this;
                this.FileEntries.Add(entry);
            }
            InitializeIndexAndParents();
            return true;
        }

        public override void SaveToFile(string fileName)
        {
            throw new NotImplementedException();
        }

        [StructLayout(LayoutKind.Sequential)]
        class FileExtensionEntry
        {
            [FixedLengthString(4)]
            public string FileExtension;
            public int FileCount, AddressOfFirstEntry;
            public const int Size = 4 + 4 + 4;

            public override string ToString()
            {
                return ReflectionUtil.ToString(this);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        class ArchiveEntry
        {
            [FixedLengthString(13)]
            public string FileName;
            public int Length, Address;
            public const int Size = 13 + 4 + 4;

            public override string ToString()
            {
                return ReflectionUtil.ToString(this);
            }

            public ArchiveEntry Clone()
            {
                return (ArchiveEntry)MemberwiseClone();
            }
        }

        class ArchiveHeader
        {
            public int NumberOfFileExtensions;
            public FileExtensionEntry[] fileExtensions;
            public ArchiveEntry[] entries;

            public void Read(BinaryReader br)
            {
                NumberOfFileExtensions = br.ReadInt32();

                long fileLength = br.BaseStream.Length;
                long maximumPossibleNumberOfFileExtensions = (fileLength - 4) / (ArchiveEntry.Size + FileExtensionEntry.Size);
                if (maximumPossibleNumberOfFileExtensions > 65536)
                {
                    maximumPossibleNumberOfFileExtensions = 65536;
                }

                //validate
                if (NumberOfFileExtensions > maximumPossibleNumberOfFileExtensions || NumberOfFileExtensions <= 0)
                {
                    throw new InvalidDataException("Invalid number of file extensions" + NumberOfFileExtensions.ToString());
                }

                fileExtensions = br.ReadObjects<FileExtensionEntry>(NumberOfFileExtensions);

                //validate file extensions
                int nextAddress = fileExtensions.Length * FileExtensionEntry.Size + 4;
                for (int i = 0; i < fileExtensions.Length; i++)
                {
                    int pos = i * FileExtensionEntry.Size + 4;
                    var ext = fileExtensions[i];
                    if (ext.AddressOfFirstEntry != nextAddress)
                    {
                        throw new InvalidDataException("Position of first filename entry is wrong, it should be " + nextAddress.ToString("X") + " but instead is " + ext.AddressOfFirstEntry.ToString("X"));
                    }
                    nextAddress += ext.FileCount * (ArchiveEntry.Size);
                    if (!Util.IsLegalFileName(ext.FileExtension))
                    {
                        throw new InvalidDataException("Filename \"" + ext.FileExtension + "\" contains illegal charactes.");
                    }
                }

                List<ArchiveEntry> entiresList = new List<ArchiveEntry>();
                //read filenames
                foreach (var ext in fileExtensions)
                {
                    var newEntries = br.ReadObjects<ArchiveEntry>(ext.FileCount);
                    string extension = "." + ext.FileExtension;
                    for (int i = 0; i < newEntries.Length; i++)
                    {
                        var entry = newEntries[i];
                        entry.FileName += extension;
                    }
                    entiresList.AddRange(newEntries);
                }
                this.entries = entiresList.ToArray();

                //validate entries
                nextAddress = (int)br.BaseStream.Position;

                foreach (var entry in entries)
                {
                    if (entry.Address != nextAddress)
                    {
                        throw new InvalidDataException("File data should be at address " + nextAddress.ToString("X") + " but is at address " + entry.Address.ToString("X") + " instead!");
                    }
                    if (!Util.IsLegalFileName(entry.FileName))
                    {
                        throw new InvalidDataException("File name \"" + entry.FileName + "\" is invalid!");
                    }
                    nextAddress += entry.Length;
                    if (nextAddress > fileLength)
                    {
                        throw new InvalidDataException("Address " + nextAddress.ToString("X") + " exceeds package file size!");
                    }
                }
            }

            public ArchiveHeader()
            {

            }

            public ArchiveHeader(string[] fileNames)
            {
                AddFileNames(fileNames);
            }

            private void AddFileNames(string[] fileNames)
            {
                string lastFileExtension = null;
                int countWithCurrentExtension = 0;

                List<ArchiveEntry> entries = new List<ArchiveEntry>();
                List<FileExtensionEntry> extensions = new List<FileExtensionEntry>();
                string extension = null;

                Action FinishExtension = () =>
                {
                    if (lastFileExtension != null)
                    {
                        var newExtension = new FileExtensionEntry();
                        newExtension.FileExtension = lastFileExtension;
                        newExtension.AddressOfFirstEntry = -1;
                        newExtension.FileCount = countWithCurrentExtension;
                        extensions.Add(newExtension);
                    }
                    countWithCurrentExtension = 0;
                    lastFileExtension = extension;
                };


                for (int i = 0; i < fileNames.Length; i++)
                {
                    string fileName = fileNames[i];
                    extension = Util.GetExtensionWithoutDot(fileName);
                    if (extension != lastFileExtension)
                    {
                        FinishExtension();
                    }
                    countWithCurrentExtension++;
                    var entry = new ArchiveEntry();
                    entry.Address = -1;
                    entry.Length = -1;
                    entry.FileName = fileName;
                    entries.Add(entry);
                }
                FinishExtension();

                this.entries = entries.ToArray();
                this.fileExtensions = extensions.ToArray();
                this.NumberOfFileExtensions = this.fileExtensions.Length;

                FixExtensions();
            }

            private void FixExtensions()
            {
                int pos = 4 + NumberOfFileExtensions * FileExtensionEntry.Size;
                for (int i = 0; i < NumberOfFileExtensions; i++)
                {
                    var ext = this.fileExtensions[i];
                    ext.AddressOfFirstEntry = pos;
                    pos += ext.FileCount * ArchiveEntry.Size;
                }
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(this.NumberOfFileExtensions);
                bw.WriteObjects(this.fileExtensions);
                var entriesCopy = (ArchiveEntry[])this.entries.Clone();
                for (int i = 0; i < entriesCopy.Length; i++)
                {
                    var entry = entries[i].Clone();
                    //remove extension from filenames
                    entry.FileName = Path.GetFileNameWithoutExtension(entry.FileName);
                    entriesCopy[i] = entry;
                }
                bw.WriteObjects(entriesCopy);
            }
        }

        class ScriptFile
        {
            //public bool ExtractAllCodes;
            //public bool JapaneseOnly;

            ArchiveHeader archiveHeader = new ArchiveHeader();

            FileStream fileStream;
            BinaryReader br;
            //BinaryWriter bw;

            static Encoding shiftJis = Encoding.GetEncoding("shift_jis");

            //public void ExtractAllFiles(string fileName, string exportPath)
            //{
            //    this.fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            //    this.br = new BinaryReader(this.fileStream);
            //    this.archiveHeader.Read(br);

            //    foreach (var entry in this.archiveHeader.entries)
            //    {
            //        //extract the file
            //        string outputPath = Path.Combine(exportPath, entry.FileName);

            //        if (Path.GetExtension(entry.FileName).Equals(".WSC", StringComparison.OrdinalIgnoreCase))
            //        {
            //            var bytes = br.ReadBytes(entry.Length);
            //            DecodeFile(bytes);
            //            File.WriteAllBytes(outputPath, bytes);

            //            var textConverter = new TextConverter();
            //            var originalLines = textConverter.ConvertText(bytes);
            //            var lines = originalLines;
            //            //string[] newLines = originalLines;


            //            if (!this.ExtractAllCodes)
            //            {
            //                lines = textConverter.RemoveLines(lines, JapaneseOnly);

            //                //validation
            //                //newLines = textConverter.ReplaceLines(originalLines, lines, JapaneseOnly);
            //            }
            //            //validation

            //            //var newBytes = textConverter.ConvertToBytes(newLines);
            //            //if (!newBytes.SequenceEqual(bytes))
            //            //{
            //            //
            //            //}

            //            File.WriteAllLines(Path.ChangeExtension(outputPath, ".TXT"), lines, shiftJis);
            //        }
            //        else
            //        {
            //            using (var outputFileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            //            {
            //                CopyStream(outputFileStream, this.fileStream, entry.Length);
            //                outputFileStream.Close();
            //            }
            //        }

            //    }
            //    this.Dispose();
            //}

            private void DecodeFile(byte[] bytes)
            {
                //rotate all bytes right by two bits
                for (int i = 0; i < bytes.Length; i++)
                {
                    int b = bytes[i];
                    int a = b & 3;
                    b = b >> 2;
                    b |= (a << 6);
                    bytes[i] = (byte)b;
                }
            }

            private void EncodeFile(byte[] bytes)
            {
                //rotate all bytes left by two bits
                for (int i = 0; i < bytes.Length; i++)
                {
                    int b = bytes[i];
                    int a = b & 0xC0;
                    b = (b << 2) & 0xFF;
                    b |= (a >> 6);
                    bytes[i] = (byte)b;
                }
            }

            private void CopyStream(Stream outputFileStream, Stream inputFileStream, int length)
            {
                const int bufferSize = 65536;
                byte[] buffer = new byte[bufferSize];

                int remaining = length;
                while (remaining > 0)
                {
                    int count;
                    if (remaining < bufferSize)
                    {
                        count = remaining;
                    }
                    else
                    {
                        count = bufferSize;
                    }
                    inputFileStream.Read(buffer, 0, count);
                    outputFileStream.Write(buffer, 0, count);
                    remaining -= count;
                }
            }

            //string currentFile = "";

            //public void ReplaceAllFiles(string fileName, string outputFileName, string exportPath)
            //{

            //    //list the files
            //    var filesInDirectory = Directory.GetFiles(exportPath, "*", SearchOption.TopDirectoryOnly);
            //    filesInDirectory = RemoveTextFiles(filesInDirectory);
            //    filesInDirectory = SortFileNames(filesInDirectory);

            //    var outputFileNames = MakeUppercase(RemoveInitialPath(filesInDirectory, exportPath));

            //    //prepare a header
            //    var ms = new MemoryStream();
            //    var bw2 = new BinaryWriter(ms);

            //    archiveHeader = new ArchiveHeader(outputFileNames);

            //    this.fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            //    this.bw = new BinaryWriter(this.fileStream);

            //    archiveHeader.Write(bw);

            //    //load files and write them to the package file
            //    for (int i = 0; i < filesInDirectory.Length; i++)
            //    {
            //        int position = (int)bw.BaseStream.Position;
            //        string inputFileName = filesInDirectory[i];

            //        if (Path.GetExtension(inputFileName).Equals(".WSC", StringComparison.OrdinalIgnoreCase))
            //        {
            //            var originalBytes = File.ReadAllBytes(inputFileName);
            //            var textConverter = new TextConverter();
            //            var originalLines = textConverter.ConvertText(originalBytes);

            //            byte[] bytes = originalBytes;

            //            string textFileName = Path.ChangeExtension(inputFileName, ".TXT");
            //            if (File.Exists(textFileName))
            //            {
            //                currentFile = textFileName;

            //                var replacementLines = File.ReadAllLines(textFileName, shiftJis);

            //                if (this.ExtractAllCodes)
            //                {
            //                    bytes = textConverter.ConvertToBytes(replacementLines);
            //                }
            //                else
            //                {
            //                    WordWrapLines(replacementLines);
            //                    var newLines = textConverter.ReplaceLines(originalLines, replacementLines, this.JapaneseOnly);
            //                    bytes = textConverter.ConvertToBytes(newLines);
            //                }
            //            }
            //            EncodeFile(bytes);
            //            bw.Write(bytes);
            //        }
            //        else
            //        {
            //            using (var inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //            {
            //                CopyStream(bw.BaseStream, inputFileStream, (int)inputFileStream.Length);
            //            }
            //        }

            //        int newPosition = (int)bw.BaseStream.Position;
            //        int fileLength = newPosition - position;
            //        archiveHeader.entries[i].Address = position;
            //        archiveHeader.entries[i].Length = fileLength;
            //    }

            //    this.bw.BaseStream.Position = 0;
            //    archiveHeader.Write(bw);

            //    this.Dispose();
            //}

            //private void WordWrapLines(string[] replacementLines)
            //{
            //    for (int i = 0; i < replacementLines.Length; i++)
            //    {
            //        string suffix = "";
            //        string line = replacementLines[i];

            //        if (line.EndsWith("%K%P"))
            //        {
            //            suffix = "%K%P";
            //            line = line.Substring(0, line.Length - 4);
            //        }
            //        else
            //        {
            //            //if (line.Length > 14)
            //            //{
            //            //    MessageBox.Show("warning: line " + (i + 1).ToString() + " of file \"" + currentFile + "\"" + Environment.NewLine +
            //            //        "Looks like a character name might be too long!");
            //            //}
            //        }
            //        line = TextWrap.WordWrapLines(line, 66);
            //        line += suffix;
            //        replacementLines[i] = line;
            //    }
            //}

            private string[] MakeUppercase(string[] filesInDirectory)
            {
                return filesInDirectory.Select(f => f.ToUpperInvariant()).ToArray();
            }

            private string[] RemoveInitialPath(string[] filesInDirectory, string initialPath)
            {
                string[] files = (string[])filesInDirectory.Clone();
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    if (file.StartsWith(initialPath, StringComparison.OrdinalIgnoreCase))
                    {
                        file = file.Substring(initialPath.Length);
                    }
                    if (file.StartsWith("/") || file.StartsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        file = file.Substring(1);
                    }
                    files[i] = file;
                }
                return files;
            }

            private string[] RemoveTextFiles(string[] filesInDirectory)
            {
                return filesInDirectory.Where(f => !f.EndsWith(".TXT", StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            private string[] SortFileNames(string[] filesInDirectory)
            {
                return filesInDirectory.OrderBy(f => f.ToUpperInvariant(), StringComparer.Ordinal).OrderBy(f => Path.GetExtension(f).ToUpperInvariant(), StringComparer.Ordinal).ToArray();
            }



            void Dispose()
            {
                if (fileStream != null)
                {
                    if (fileStream.CanWrite)
                    {
                        fileStream.Flush();
                    }
                    fileStream.Close();
                }
            }

            public ArchiveFileEntry[] GetFiles(string fileName)
            {
                List<ArchiveFileEntry> entries = new List<ArchiveFileEntry>();
                this.fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                this.br = new BinaryReader(this.fileStream);
                try
                {
                    this.archiveHeader.Read(br);
                }
                catch (InvalidDataException ex)
                {
                    return null;
                }

                foreach (var entry in this.archiveHeader.entries)
                {
                    var fileEntry = new ArchiveFileEntry();
                    fileEntry.FileName = entry.FileName;
                    fileEntry.FileAddress = entry.Address;
                    fileEntry.FileSize = entry.Length;
                    entries.Add(fileEntry);
                }
                return entries.ToArray();
            }
        }

    }
    /// <summary>
    /// Indicates that a string has a fixed length when being read or written
    /// </summary>
    public sealed class FixedLengthStringAttribute : Attribute
    {
        public int StringLength;
        public FixedLengthStringAttribute(int stringLength)
        {
            this.StringLength = stringLength;
        }
    }

    public static partial class Extensions
    {
        //static readonly Encoding shiftJis = Encoding.GetEncoding("shift_jis");

        public static string ReadFixedLengthString(this BinaryReader br, int length)
        {
            return ReadFixedLengthString(br, length, shiftJis);
        }

        public static string ReadFixedLengthString(this BinaryReader br, int length, Encoding encoding)
        {
            var bytes = br.ReadBytes(length);
            return ReadFixedLengthString(bytes, 0, length, encoding);
        }

        public static string ReadFixedLengthString(this byte[] bytes, int start, int length, Encoding encoding)
        {
            int nullIndex = Array.IndexOf(bytes, (byte)0, start);
            if (nullIndex == -1)
            {
                nullIndex = bytes.Length;
            }
            return encoding.GetString(bytes, start, nullIndex - start);
        }

        public static string ReadFixedLengthString(this byte[] bytes, int start, int length)
        {
            return ReadFixedLengthString(bytes, start, length, shiftJis);
        }

        public static void WriteFixedLengthString(this BinaryWriter bw, string text, int length, Encoding encoding)
        {
            var bytes = new byte[length];
            //should be zero-filled by runtime library

            encoding.GetBytes(text, 0, text.Length, bytes, 0);
            bw.Write(bytes);
        }

        public static void WriteFixedLengthString(this BinaryWriter bw, string text, int length)
        {
            WriteFixedLengthString(bw, text, length, shiftJis);
        }

        //public static int EndianSwap(this int value)
        //{
        //    unchecked
        //    {
        //        uint uvalue = (uint)value;
        //        uint uvalue2 = (((uvalue >> 24) & 0xFF) << 0) |
        //                       (((uvalue >> 16) & 0xFF) << 8) |
        //                       (((uvalue >> 8) & 0xFF) << 16) |
        //                       (((uvalue >> 0) & 0xFF) << 24);
        //        return (int)uvalue2;
        //    }
        //}

        public static T ReadObject<T>(this BinaryReader br) where T : new()
        {
            var obj = new T();
            ReadObject(br, ref obj);
            return obj;
        }

        public static T ReadStruct<T>(this BinaryReader br) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] bytes = br.ReadBytes(size);
            unsafe
            {
                fixed (byte* b = &bytes[0])
                {
                    return (T)Marshal.PtrToStructure((IntPtr)b, typeof(T));
                }
            }
        }

        public static void WriteStruct<T>(this BinaryWriter bw, T obj) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] bytes = new byte[size];
            unsafe
            {
                fixed (byte* b = &bytes[0])
                {
                    Marshal.StructureToPtr(obj, (IntPtr)b, false);
                }
                bw.Write(bytes);
            }
        }

        public static T[] ReadStructs<T>(this BinaryReader br, int count) where T : struct
        {
            T[] structs = new T[count];
            for (int i = 0; i < count; i++)
            {
                structs[i] = ReadStruct<T>(br);
            }
            return structs;
        }

        public static int[] ReadIntArray(this BinaryReader br, int count)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < count; i++)
            {
                list.Add(br.ReadInt32());
            }
            return list.ToArray();
        }

        public static void WriteIntArray(this BinaryWriter bw, IEnumerable<int> intArray)
        {
            int count = intArray.Count();
            foreach (int value in intArray)
            {
                bw.Write(value);
            }
        }

        public static T[] ReadObjects<T>(this BinaryReader br, int count) where T : new()
        {
            List<T> list = new List<T>();
            for (int i = 0; i < count; i++)
            {
                list.Add(br.ReadObject<T>());
            }
            return list.ToArray();
        }

        public static string ReadNullTerminatedString(this BinaryReader br, Encoding encoding)
        {
            List<byte> bytes = new List<byte>();
            while (true)
            {
                byte b = br.ReadByte();
                if (b != 0)
                {
                    bytes.Add(b);
                }
                else
                {
                    break;
                }
            }
            string str = encoding.GetString(bytes.ToArray());
            return str;
        }

        public static void WriteNullTerminatedString(this BinaryWriter bw, string str, Encoding encoding)
        {
            var bytes = encoding.GetBytes(str);
            bw.Write(bytes);
            bw.Write((byte)0);
        }

        public static string ReadNullTerminatedString(this BinaryReader br)
        {
            return ReadNullTerminatedString(br, shiftJis);
        }

        public static void WriteNullTerminatedString(this BinaryWriter bw, string str)
        {
            WriteNullTerminatedString(bw, str, shiftJis);
        }


        public static void ReadObject<T>(this BinaryReader br, ref T obj)
        {
            //int lastIntValue = 0;
            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                if (!field.IsInitOnly)
                {
                    if (field.FieldType == typeof(int))
                    {
                        int intValue = br.ReadInt32();
                        field.SetValue(obj, intValue);
                        //lastIntValue = intValue;
                    }
                    else if (field.FieldType == typeof(string))
                    {
                        var fixedLengthAttribute = field.GetCustomAttributes(false).OfType<FixedLengthStringAttribute>().FirstOrDefault();
                        if (fixedLengthAttribute != null)
                        {
                            string stringValue = br.ReadFixedLengthString(fixedLengthAttribute.StringLength);
                            field.SetValue(obj, stringValue);
                        }
                        else
                        {
                            string stringValue = br.ReadNullTerminatedString();
                            field.SetValue(obj, stringValue);
                        }
                    }
                    else if (field.FieldType == typeof(ushort))
                    {
                        ushort shortValue = br.ReadUInt16();
                        field.SetValue(obj, shortValue);
                    }
                    else if (field.FieldType == typeof(short))
                    {
                        short shortValue = br.ReadInt16();
                        field.SetValue(obj, shortValue);
                    }
                    else if (field.FieldType == typeof(byte))
                    {
                        byte byteValue = br.ReadByte();
                        field.SetValue(obj, byteValue);
                    }
                }
            }
        }

        public static void WriteObject(this BinaryWriter bw, object obj)
        {
            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var field in fields)
            {
                if (!field.IsInitOnly)
                {
                    if (field.FieldType == typeof(int))
                    {
                        int intValue;
                        intValue = (int)field.GetValue(obj);
                        bw.Write(intValue);
                    }
                    if (field.FieldType == typeof(string))
                    {
                        string stringValue = (string)field.GetValue(obj);
                        var fixedLengthAttribute = field.GetCustomAttributes(false).OfType<FixedLengthStringAttribute>().FirstOrDefault();
                        if (fixedLengthAttribute != null)
                        {
                            bw.WriteFixedLengthString(stringValue, fixedLengthAttribute.StringLength);
                        }
                        else
                        {
                            bw.WriteNullTerminatedString(stringValue);
                        }
                    }
                }
            }
        }

        public static void WriteObjects(this BinaryWriter bw, object[] objects)
        {
            foreach (var obj in objects)
            {
                WriteObject(bw, obj);
            }
        }

    }

    //public static partial class ReflectionUtil
    //{
    //    public static string ToString(object obj)
    //    {
    //        StringBuilder sb = new StringBuilder();
    //        var type = obj.GetType();
    //        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);

    //        bool needComma = false;
    //        foreach (var field in fields)
    //        {
    //            if (field.FieldType == typeof(int) || field.FieldType == typeof(string) || field.FieldType.IsEnum)
    //            {
    //                Util.PrintComma(sb, ref needComma);
    //                sb.Append(field.Name + " = " + (field.GetValue(obj) ?? "null").ToString());
    //            }
    //        }
    //        return sb.ToString();

    //    }
    //}

    public static partial class Util
    {
        static HashSet<char> illegalFileNameChars;

        public static bool IsLegalFileName(string fileName)
        {
            if (illegalFileNameChars == null)
            {
                illegalFileNameChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            }

            foreach (var c in fileName)
            {
                if (illegalFileNameChars.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }


        //public static void PrintComma(StringBuilder sb, ref bool needComma)
        //{
        //    if (needComma)
        //    {
        //        sb.Append(", ");
        //    }
        //    else
        //    {
        //        needComma = true;
        //    }
        //}

        public static string GetExtensionWithoutDot(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(extension))
            {
                if (extension.StartsWith("."))
                {
                    extension = extension.Substring(1);
                }
            }
            return extension;
        }
    }

}
