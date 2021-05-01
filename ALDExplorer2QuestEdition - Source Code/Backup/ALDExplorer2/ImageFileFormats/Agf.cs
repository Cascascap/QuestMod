using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using FreeImageAPI;
using System.Diagnostics;

namespace ALDExplorer2.ImageFileFormats
{
    public static class Agf
    {
        public static FreeImageBitmap LoadAgf(byte[] fileBytes)
        {
            if (!AgfHeaderIsValid(fileBytes))
            {
                return null;
            }

            string fileName1 = null, fileName2 = null, fileName3 = null;
            FreeImageBitmap bitmap = null;

            try
            {
                fileName1 = TempFileManager.DefaultInstance.CreateTempFile("temp.vfs");
                fileName2 = "image.agf";
                fileName3 = fileName1 + "~\\image.tga";

                var tempArchive = new VfsArchiveFile();
                tempArchive.FileType = ArchiveFileType.SofthouseCharaVfs20File;
                tempArchive.ArchiveFileName = fileName1;
                var tempEntry = new ArchiveFileEntry();
                tempEntry.FileName = Path.GetFileName(fileName2);
                tempArchive.FileEntries.Add(tempEntry);
                tempEntry.ReplacementBytes = fileBytes;

                tempArchive.SaveToFile(fileName1);

                string appDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                string toolExe = Path.Combine(Path.Combine(appDirectory, "tools"), "arc_conv.exe");
                string args = "--progress 0 " + "\"" + fileName1 + "\"";

                if (File.Exists(toolExe))
                {
                    var startInfo = new ProcessStartInfo(toolExe, args);
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    var process = Process.Start(startInfo);
                    process.WaitForExit();

                    using (var fs = new FileStream(fileName3, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bitmap = new FreeImageBitmap(fs, FREE_IMAGE_FORMAT.FIF_TARGA);
                        fs.Close();
                        fs.Dispose();
                        var header = fileBytes.Take(0x78).ToArray();
                        bitmap.Tag = header;
                        bitmap.Comment = header.ToHexString();
                    }
                }
            }
            finally
            {
                if (File.Exists(fileName1))
                {
                    File.Delete(fileName1);
                }
                if (File.Exists(fileName2))
                {
                    File.Delete(fileName2);
                }
                if (File.Exists(fileName3))
                {
                    File.Delete(fileName3);
                }
            }

            return bitmap;
        }

        static readonly int agfSignature = BinaryUtil.StringToInt("AGF");
        public static bool AgfHeaderIsValid(byte[] fileBytes)
        {
            int sig = BitConverter.ToInt32(fileBytes, 0);
            int sig2 = BitConverter.ToInt32(fileBytes, 0x0C);
            int sig3 = BitConverter.ToInt32(fileBytes, 0x18);

            if (sig == agfSignature && sig2 == 0x74 && sig3 == 0x5C)
            {
                return true;
            }
            return false;
        }

        public static void SaveAgf(Stream stream, FreeImageBitmap bitmap)
        {
            string fileName1 = null, fileName2 = null;
            BinaryWriter bw = new BinaryWriter(stream);

            try
            {
                fileName1 = TempFileManager.DefaultInstance.CreateTempFile("image.tga");
                fileName2 = TempFileManager.DefaultInstance.CreateTempFile("image.agf");

                bitmap.Save(fileName1, FREE_IMAGE_FORMAT.FIF_TARGA);

                string appDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                string toolExe = Path.Combine(Path.Combine(appDirectory, "tools"), "arc_conv.exe");
                string args = "--mod agf " + "\"" + fileName1 + "\" \"" + fileName2 + "\"";

                var header = bitmap.Tag as byte[];

                if (File.Exists(toolExe))
                {
                    var startInfo = new ProcessStartInfo(toolExe, args);
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    var process = Process.Start(startInfo);
                    process.WaitForExit();

                    var fileBytes = File.ReadAllBytes(fileName2);
                    if (fileBytes.Length == 0)
                    {

                    }

                    if (header != null)
                    {
                        //for (int i = 0x38; i < 0x40; i++)
                        //{
                        //    fileBytes[i] = header[i];
                        //}
                        for (int i = 0x20; i < 0x68; i++)
                        {
                            fileBytes[i] = header[i];
                        }
                    }

                    if (header != null)
                    {
                        for (int i = 0x0C; i < 0x74; i++)
                        {
                            if (fileBytes[i] != header[i])
                            {

                            }
                        }
                    }
                    bw.Write(fileBytes);
                }
            }
            finally
            {
                if (File.Exists(fileName1))
                {
                    File.Delete(fileName1);
                }
                if (File.Exists(fileName2))
                {
                    File.Delete(fileName2);
                }
            }
        }
    }
}
