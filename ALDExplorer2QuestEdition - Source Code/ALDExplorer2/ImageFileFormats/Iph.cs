using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using FreeImageAPI;
using System.Diagnostics;

namespace ALDExplorer2.ImageFileFormats
{
    [StructLayout(LayoutKind.Sequential)]
    public class IPHMetadata
    {
        public int f;
        public int x, y;
        public int cellWidth, cellHeight;
        public int transparentColor;
        public int fourcc;
        public int format;

        public IPHMetadata()
        {
            f = 0;
            x = 0;
            y = 0;
            cellWidth = 0;
            cellHeight = 0;
            transparentColor = 0;
            fourcc = 0x00485049;
            format = 1;
        }

        public string GetComment()
        {
            return CommentUtil.GetComment(this);
        }
        public override string ToString()
        {
            return CommentUtil.GetComment(this);
        }

        public bool ParseComment(string comment)
        {
            return CommentUtil.ParseComment(this, comment);
        }

        public byte[] ToByteArray()
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            bw.WriteObject(this);
            return ms.ToArray();
        }
    }

    public static class Iph
    {
        public static FreeImageBitmap LoadIph(byte[] fileBytes)
        {
            //validate riff header
            if (!IphHeaderIsValid(fileBytes))
            {
                return null;
            }

            string tempFileName1 = null;
            string tempFileName2 = null;
            string tempFileName3 = null;
            //FileStream stream1 = null;
            //FileStream stream2 = null;
            //FileStream stream3 = null;
            FreeImageBitmap bitmap = null;

            try
            {
                tempFileName1 = TempFileManager.DefaultInstance.CreateTempFile("temp.iph");
                tempFileName2 = TempFileManager.DefaultInstance.CreateTempFile("temp.png");
                tempFileName3 = Path.Combine(Path.Combine(Path.GetDirectoryName(tempFileName2), "metadata"), Path.GetFileName(tempFileName2) + ".meta");
                if (!Directory.Exists(Path.GetDirectoryName(tempFileName3)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempFileName3));
                }
                //stream3 = TempFileManager.CreateTempFileStream(tempFileName3);

                string appDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                string toolExe = Path.Combine(Path.Combine(appDirectory, "tools"), "IPHtoPNG.exe");
                string args = "\"" + tempFileName1 + "\" \"" + tempFileName2 + "\"";

                if (File.Exists(toolExe))
                {
                    //stream1.Write(fileBytes, 0, fileBytes.Length);
                    File.WriteAllBytes(tempFileName1, fileBytes);
                    var startInfo = new ProcessStartInfo(toolExe, args);
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    var process = Process.Start(startInfo);
                    process.WaitForExit();
                    using (var fs = new FileStream(tempFileName2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bitmap = new FreeImageBitmap(fs, FREE_IMAGE_FORMAT.FIF_PNG);
                        fs.Close();
                        fs.Dispose();
                    }

                    //var br3 = new BinaryReader(stream3);
                    //var meta = br3.ReadBytes(32);
                    var metadataBytes = File.ReadAllBytes(tempFileName3);
                    var ms = new MemoryStream(metadataBytes);
                    var br = new BinaryReader(ms);
                    var metadata = br.ReadObject<IPHMetadata>();

                    bitmap.Tag = metadata;
                    bitmap.Comment = metadata.GetComment();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                //if (stream1 != null)
                //{
                //    stream1.Close();
                //    stream1.Dispose();
                //}
                //if (stream2 != null)
                //{
                //    stream2.Close();
                //    stream2.Dispose();
                //}
                //if (stream3 != null)
                //{
                //    stream3.Close();
                //    stream3.Dispose();
                //}
                if (File.Exists(tempFileName1))
                {
                    File.Delete(tempFileName1);
                }
                if (File.Exists(tempFileName2))
                {
                    File.Delete(tempFileName2);
                }
                if (File.Exists(tempFileName3))
                {
                    File.Delete(tempFileName3);
                }
            }
            return bitmap;
        }

        public static bool IphHeaderIsValid(byte[] fileBytes)
        {
            var ms = new MemoryStream(fileBytes);
            var br = new BinaryReader(ms);

            if (ms.Length < 0x40)
            {
                return false;
            }

            string riffSignature = br.ReadStringFixedSize(4);
            if (riffSignature != "RIFF")
            {
                return false;
            }
            int tagSize = br.ReadInt32();
            if (tagSize != 0x38)
            {
                return false;
            }
            string iph = br.ReadStringFixedSize(3);
            br.ReadByte();
            string fmt = br.ReadStringFixedSize(4);
            if (iph == "IPH" && fmt == "fmt ")
            {
                return true;
            }
            if (iph == "IPG" && fmt == "fmt ")
            {
                return true;
            }
            return false;
        }

        public static void SaveIph(Stream stream, FreeImageBitmap bitmap)
        {
            string tempFileName1 = TempFileManager.DefaultInstance.CreateTempFile("temp.png");
            string tempFileName2 = TempFileManager.DefaultInstance.CreateTempFile("temp.iph");
            string tempFileName3 = Path.Combine(Path.Combine(Path.GetDirectoryName(tempFileName2), "metadata"), Path.GetFileName(tempFileName1) + ".meta");

            try
            {
                string appDirectory = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
                string toolExe = Path.Combine(Path.Combine(appDirectory, "tools"), "PNGtoIPH.exe");
                string args = "\"" + tempFileName1 + "\" \"" + tempFileName2 + "\"";

                if (File.Exists(toolExe))
                {
                    if (Directory.Exists(Path.GetDirectoryName(tempFileName3)))
                    {

                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(tempFileName3));
                    }

                    var meta = bitmap.Tag as IPHMetadata;
                    if (meta == null)
                    {
                        meta = new IPHMetadata();
                        meta.transparentColor = 0x3FEF;
                        var meta2 = new IPHMetadata();
                        meta2.transparentColor = 0x3FEF;
                        string comment = bitmap.Comment;
                        if (!String.IsNullOrEmpty(comment))
                        {
                            if (meta2.ParseComment(comment))
                            {
                                meta = meta2;
                            }
                        }
                    }
                    var metaBytes = meta.ToByteArray();

                    if (metaBytes != null)
                    {
                        File.WriteAllBytes(tempFileName3, metaBytes);
                    }

                    bitmap.Save(tempFileName1, FREE_IMAGE_FORMAT.FIF_PNG);

                    var startInfo = new ProcessStartInfo(toolExe, args);
                    startInfo.CreateNoWindow = true;
                    startInfo.UseShellExecute = false;
                    var process = Process.Start(startInfo);
                    process.WaitForExit();

                    var iphBytes = File.ReadAllBytes(tempFileName2);
                    stream.Write(iphBytes, 0, iphBytes.Length);
                }
            }
            finally
            {
                File.Delete(tempFileName1);
                File.Delete(tempFileName2);
                File.Delete(tempFileName3);
            }
        }


    }
}
