using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using FreeImageAPI;
using System.Drawing;
using ZLibNet;
using System.Diagnostics;

namespace ALDExplorer2.ImageFileFormats
{
    [StructLayout(LayoutKind.Sequential)]
    public struct QntHeaderRawV0
    {
        public int Signature, FileVersion;
        public int X, Y, Width, Height, ColorDepth, Reserved, PixelSize, AlphaSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QntHeaderRaw
    {
        public int Signature, FileVersion;
        public int HeaderSize;
        public int X, Y, Width, Height, ColorDepth, Reserved, PixelSize, AlphaSize;
    }

    public class QntHeader : ICloneable, IImageHeader
    {
        public int Signature;
        public int FileVersion;   //always 2, but could be 0
        public int HeaderSize;     /* header size */
        public int X;           /* display location x */
        public int Y;           /* display location y */
        public int Width;        /* image width        */
        public int Height;       /* image height       */
        public int ColorDepth;          /* image data depth   */
        public int Reserved;          /* reserved data      */
        public int PixelSize;   /* compressed pixel size       */
        public int AlphaSize;   /* compressed alpha pixel size */
        public byte[] ExtraData;

        public string GetComment()
        {
            return CommentUtil.GetComment(this);
        }

        public bool ParseComment(string comment)
        {
            return CommentUtil.ParseComment(this, comment);
        }

        public QntHeader Clone()
        {
            var clone = (QntHeader)this.MemberwiseClone();
            if (clone.ExtraData != null) clone.ExtraData = (byte[])clone.ExtraData.Clone();
            return clone;
        }

        public bool Validate()
        {
            var qnt = this;
            if (qnt.Signature != 0x00544e51)
            {
                return false;
            }
            if (qnt.Width < 0 || qnt.Height < 0 || qnt.Width > 65535 || qnt.Height > 65535 || qnt.Width * qnt.Height >= 64 * 1024 * 1024)
            {
                return false;
            }
            if (qnt.ColorDepth == 1 || qnt.ColorDepth == 4 || qnt.ColorDepth == 8 || qnt.ColorDepth == 16 || qnt.ColorDepth == 24 || qnt.ColorDepth == 32)
            {

            }
            else
            {
                return false;
            }
            return true;
        }

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        #region IImageHeader Members

        string IImageHeader.Signature
        {
            get
            {
                return BinaryUtil.IntToString(this.Signature);
            }
            set
            {
                this.Signature = BinaryUtil.StringToInt(value);
            }
        }

        public int Version
        {
            get
            {
                return Version;
            }
            set
            {
                Version = value;
            }
        }

        int IImageHeader.X
        {
            get
            {
                return X;
            }
            set
            {
                X = value;
            }
        }

        int IImageHeader.Y
        {
            get
            {
                return Y;
            }
            set
            {
                Y = value;
            }
        }

        int IImageHeader.Width
        {
            get
            {
                return Width;
            }
            set
            {
                Width = value;
            }
        }

        int IImageHeader.Height
        {
            get
            {
                return Height;
            }
            set
            {
                Height = value;
            }
        }

        int IImageHeader.ColorDepth
        {
            get
            {
                return ColorDepth;
            }
            set
            {
                ColorDepth = value;
            }
        }

        public bool HasAlphaChannel
        {
            get
            {
                return ColorDepth == 32;
            }
            //set
            //{

            //}
        }

        #endregion
    }

    public static class Qnt
    {
        public static FreeImageBitmap LoadImage(byte[] bytes)
        {
            var qntHeader = GetQntHeader(bytes);
            if (qntHeader == null || !qntHeader.Validate())
            {
                return null;
            }
            var pixels = GetQntPixels(bytes, qntHeader);
            byte[] alphaPixels = null;
            if (qntHeader.AlphaSize != 0)
            {
                alphaPixels = GetQntAlpha(bytes, qntHeader);
            }

            FreeImageBitmap image = new FreeImageBitmap(qntHeader.Width, qntHeader.Height, qntHeader.Width * 3, 24, FREE_IMAGE_TYPE.FIT_BITMAP, pixels);
            var blue = image.GetChannel(FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
            var red = image.GetChannel(FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
            if (alphaPixels != null)
            {
                image.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_32_BPP);
            }
            image.SetChannel(blue, FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
            image.SetChannel(red, FREE_IMAGE_COLOR_CHANNEL.FICC_RED);

            FreeImageBitmap alpha = null;
            try
            {
                if (alphaPixels != null)
                {
                    alpha = new FreeImageBitmap(qntHeader.Width, qntHeader.Height, qntHeader.Width, 8, FREE_IMAGE_TYPE.FIT_BITMAP, alphaPixels);
                }

                if (alpha != null)
                {
                    image.SetChannel(alpha, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                }
            }
            finally
            {
                if (alpha != null) alpha.Dispose();
            }
            image.Comment = qntHeader.GetComment();
            image.Tag = qntHeader;
            return image;
        }

        public static QntHeader GetQntHeader(byte[] bytes)
        {
            var ms = new MemoryStream(bytes);
            return GetQntHeader(ms);
        }

        public static QntHeader GetQntHeader(Stream ms)
        {
            long startPosition = ms.Position;
            var br = new BinaryReader(ms);
            QntHeader qnt = new QntHeader();
            qnt.Signature = br.ReadInt32();
            qnt.FileVersion = br.ReadInt32();
            if (qnt.FileVersion == 0)
            {
                qnt.HeaderSize = 48;
            }
            else
            {
                qnt.HeaderSize = br.ReadInt32();
            }

            if (qnt.HeaderSize > 1024 * 1024 || qnt.HeaderSize < 0)
            {
                return null;
            }
            qnt.X = br.ReadInt32();
            qnt.Y = br.ReadInt32();
            qnt.Width = br.ReadInt32();
            qnt.Height = br.ReadInt32();
            qnt.ColorDepth = br.ReadInt32();

            qnt.Reserved = br.ReadInt32();
            qnt.PixelSize = br.ReadInt32();
            qnt.AlphaSize = br.ReadInt32();
            long endPosition = ms.Position - startPosition;
            int extraDataLength = qnt.HeaderSize - (int)endPosition;
            if (extraDataLength < 0 || extraDataLength > 1024 * 1024)
            {
                return null;
            }
            if (extraDataLength > 0)
            {
                qnt.ExtraData = br.ReadBytes(extraDataLength);
            }
            return qnt;
        }

        private static void SaveQntHeader(Stream stream, QntHeader qnt)
        {
            long startPosition = stream.Position;
            var bw = new BinaryWriter(stream);
            bw.WriteStringNullTerminated("QNT");
            bw.Write(qnt.FileVersion);
            if (qnt.FileVersion == 0)
            {
                qnt.HeaderSize = 48;
                bw.Write(qnt.X);
                bw.Write(qnt.Y);
                bw.Write(qnt.Width);
                bw.Write(qnt.Height);
                bw.Write(qnt.ColorDepth);
                bw.Write(qnt.Reserved);
                bw.Write(qnt.PixelSize);
                bw.Write(qnt.AlphaSize);
            }
            else
            {
                bw.Write(qnt.HeaderSize);
                bw.Write(qnt.X);
                bw.Write(qnt.Y);
                bw.Write(qnt.Width);
                bw.Write(qnt.Height);
                bw.Write(qnt.ColorDepth);
                bw.Write(qnt.Reserved);
                bw.Write(qnt.PixelSize);
                bw.Write(qnt.AlphaSize);
            }
            int position = (int)(stream.Position - startPosition);
            if (position < qnt.HeaderSize)
            {
                int remainingBytes = qnt.HeaderSize - position;
                if (qnt.ExtraData != null && qnt.ExtraData.Length >= remainingBytes)
                {
                    stream.Write(qnt.ExtraData, 0, remainingBytes);
                }
                else if (qnt.ExtraData != null && qnt.ExtraData.Length < remainingBytes)
                {
                    int sizeFromArray = qnt.ExtraData.Length;
                    stream.Write(qnt.ExtraData, 0, sizeFromArray);
                    stream.WriteZeroes(remainingBytes - sizeFromArray);
                }
                else
                {
                    //pad with zeroes
                    stream.WriteZeroes(remainingBytes);
                }
            }
        }

        public static byte[] GetQntPixels(byte[] inputBytes, QntHeader qntHeader)
        {
            byte[] pic;
            if (qntHeader.PixelSize > 0)
            {
                var ms = new MemoryStream(inputBytes, qntHeader.HeaderSize, qntHeader.PixelSize);
                //ms.Position = qntHeader.headerSize;

                var rawMs = ZLibCompressor.DeCompress(ms);
                var raw = rawMs.ToArray();
                int endPosition = (int)ms.Position;
                //int length = (int)ms.Position - qntHeader.headerSize;
                //if (length != qntHeader.pixelSize + qntHeader.alphaSize)
                //{

                //}

                int w = qntHeader.Width;
                int h = qntHeader.Height;
                if (raw.Length < w * h * 3)
                {
                    throw new InvalidDataException("Size of decompressed data is wrong");
                }

                pic = DecodeQntPixels(raw, w, h);
                //VerifyQntPixels(raw, w, h, pic);
            }
            else
            {
                int w = qntHeader.Width;
                int h = qntHeader.Height;
                pic = new byte[w * h * 3];
            }

            return pic;
        }

        private static void VerifyQntPixels(byte[] raw, int w, int h, byte[] pic)
        {
            if (Debugger.IsAttached && false)
            {
                var raw2 = EncodeQntPixels(pic, w, h);
                if (raw.SequenceEqual(raw2))
                {

                }
                else
                {
                    for (int i = 0; i < raw.Length; i++)
                    {
                        if (raw[i] != raw2[i])
                        {
                            int minIndex = i - 4;
                            if (minIndex < 0) minIndex = 0;
                            var dummy1 = raw.Skip(minIndex).Take(8).ToArray();
                            var dummy2 = raw2.Skip(minIndex).Take(8).ToArray();
                        }
                    }
                }
            }
        }

        private static byte[] DecodeQntPixels(byte[] raw, int w, int h)
        {
            byte[] pic = new byte[h * w * 3];
            if (raw.Length < pic.Length)
            {

            }
            DeinterleaveQntPixels(raw, w, h, pic);
            VerifyDeinterleaveQntPixels(raw, w, h, pic);
            var picNotFiltered = (byte[])pic.Clone();
            DecodeQntPixelsFilter(w, h, pic);
            VerifyDecodeQntPixelsFilter(w, h, pic, picNotFiltered);

            return pic;
        }

        private static void VerifyDecodeQntPixelsFilter(int w, int h, byte[] pic, byte[] picNotFiltered)
        {
            if (Debugger.IsAttached && false)
            {
                var pic2 = EncodeQntPixelsFilter(pic, w, h);
                if (pic2.SequenceEqual(picNotFiltered))
                {

                }
                else
                {
                    for (int i = 0; i < picNotFiltered.Length; i++)
                    {
                        if (picNotFiltered[i] != pic2[i])
                        {
                            int minIndex = i - 4;
                            if (minIndex < 0) minIndex = 0;
                            var dummy1 = picNotFiltered.Skip(minIndex).Take(8).ToArray();
                            var dummy2 = pic2.Skip(minIndex).Take(8).ToArray();
                        }
                    }
                }
            }
        }

        private static void DecodeQntPixelsFilter(int w, int h, byte[] pic)
        {
            int x, y;

            if (w > 1)
            {
                for (x = 1; x < w; x++)
                {
                    pic[x * 3] = (byte)(pic[(x - 1) * 3] - pic[x * 3]);
                    pic[x * 3 + 1] = (byte)(pic[(x - 1) * 3 + 1] - pic[x * 3 + 1]);
                    pic[x * 3 + 2] = (byte)(pic[(x - 1) * 3 + 2] - pic[x * 3 + 2]);
                }
            }

            if (h > 1)
            {
                for (y = 1; y < h; y++)
                {
                    pic[(y * w) * 3] = (byte)(pic[((y - 1) * w) * 3] - pic[(y * w) * 3]);
                    pic[(y * w) * 3 + 1] = (byte)(pic[((y - 1) * w) * 3 + 1] - pic[(y * w) * 3 + 1]);
                    pic[(y * w) * 3 + 2] = (byte)(pic[((y - 1) * w) * 3 + 2] - pic[(y * w) * 3 + 2]);

                    for (x = 1; x < w; x++)
                    {
                        int px, py;
                        py = pic[((y - 1) * w + x) * 3];
                        px = pic[(y * w + x - 1) * 3];
                        pic[(y * w + x) * 3] = (byte)(((py + px) >> 1) - pic[(y * w + x) * 3]);
                        py = pic[((y - 1) * w + x) * 3 + 1];
                        px = pic[(y * w + x - 1) * 3 + 1];
                        pic[(y * w + x) * 3 + 1] = (byte)(((py + px) >> 1) - pic[(y * w + x) * 3 + 1]);
                        py = pic[((y - 1) * w + x) * 3 + 2];
                        px = pic[(y * w + x - 1) * 3 + 2];
                        pic[(y * w + x) * 3 + 2] = (byte)(((py + px) >> 1) - pic[(y * w + x) * 3 + 2]);
                    }
                }
            }
        }

        private static void VerifyDeinterleaveQntPixels(byte[] raw, int w, int h, byte[] pic)
        {
            if (Debugger.IsAttached && false)
            {
                var raw2 = InterleaveQntPixels(w, h, pic);
                if (raw.SequenceEqual(raw2))
                {

                }
                else
                {
                    for (int i = 0; i < raw.Length; i++)
                    {
                        if (raw[i] != raw2[i])
                        {
                            int minIndex = i - 4;
                            if (minIndex < 0) minIndex = 0;
                            var dummy1 = raw.Skip(minIndex).Take(8).ToArray();
                            var dummy2 = raw2.Skip(minIndex).Take(8).ToArray();
                        }
                    }
                }
            }
        }

        private static void DeinterleaveQntPixels(byte[] raw, int w, int h, byte[] pic)
        {
            int i, j, x, y;
            j = 0;
            //if (pic.Length > raw.Length)
            //{
            //    int numberOfChannels = raw.Length / (pic.Length / 3);
            //    if (numberOfChannels == 1)
            //    {
            //        //this only happens on a corrupt file - non interleaved copy of the alpha channel
            //        for (y = 0; y < h; y++)
            //        {
            //            for (x = 0; x < w; x++)
            //            {
            //                i = x + y * w;
            //                pic[i * 3] = raw[i];
            //                pic[i * 3 + 1] = raw[i];
            //                pic[i * 3 + 2] = raw[i];
            //            }
            //        }
            //        return;
            //    }
            //    else
            //    {

            //    }
            //}

            for (i = 3 - 1; i >= 0; i--)
            {
                for (y = 0; y < (h - 1); y += 2)
                {
                    for (x = 0; x < (w - 1); x += 2)
                    {
                        pic[(y * w + x) * 3 + i] = raw[j];
                        pic[((y + 1) * w + x) * 3 + i] = raw[j + 1];
                        pic[(y * w + x + 1) * 3 + i] = raw[j + 2];
                        pic[((y + 1) * w + x + 1) * 3 + i] = raw[j + 3];
                        j += 4;
                    }
                    if (x != w)
                    {
                        pic[(y * w + x) * 3 + i] = raw[j];
                        pic[((y + 1) * w + x) * 3 + i] = raw[j + 1];
                        j += 4;
                    }
                }
                if (y != h)
                {
                    for (x = 0; x < (w - 1); x += 2)
                    {
                        pic[(y * w + x) * 3 + i] = raw[j];
                        pic[(y * w + x + 1) * 3 + i] = raw[j + 2];
                        j += 4;
                    }
                    if (x != w)
                    {
                        pic[(y * w + x) * 3 + i] = raw[j];
                        j += 4;
                    }
                }
            }
        }

        static byte[] GetQntAlpha(byte[] inputBytes, QntHeader qntHeader)
        {
            var ms = new MemoryStream(inputBytes);
            ms.Position = qntHeader.HeaderSize + qntHeader.PixelSize;

            var rawMs = ZLibCompressor.DeCompress(ms);
            var raw = rawMs.ToArray();

            int w = qntHeader.Width;
            int h = qntHeader.Height;
            byte[] pic = DecodeQntAlpha(raw, w, h);
            VerifyQntAlpha(raw, w, h, pic);

            return pic;
        }

        private static void VerifyQntAlpha(byte[] raw, int w, int h, byte[] pic)
        {
            if (Debugger.IsAttached && false)
            {
                var raw2 = EncodeQntAlpha(w, h, pic);
                if (raw.SequenceEqual(raw2))
                {

                }
                else
                {
                    int length = Math.Min(raw.Length, raw2.Length);
                    if (raw.Length != raw2.Length)
                    {

                    }
                    for (int i = 0; i < length; i++)
                    {
                        if (raw[i] != raw2[i])
                        {
                            int minIndex = i - 4;
                            if (minIndex < 0) minIndex = 0;
                            var dummy1 = raw.Skip(minIndex).Take(8).ToArray();
                            var dummy2 = raw2.Skip(minIndex).Take(8).ToArray();
                        }
                    }
                }
            }
        }

        private static byte[] DecodeQntAlpha(byte[] raw, int w, int h)
        {
            byte[] pic = new byte[w * h];
            int i = 1;
            int x, y;
            if (w > 1)
            {
                pic[0] = raw[0];
                for (x = 1; x < w; x++)
                {
                    pic[x] = (byte)(pic[x - 1] - raw[i]);
                    i++;
                }
                if (0 != (w % 2)) i++;
            }

            if (h > 1)
            {
                for (y = 1; y < h; y++)
                {
                    pic[y * w] = (byte)(pic[(y - 1) * w] - raw[i]); i++;
                    for (x = 1; x < w; x++)
                    {
                        int pax, pay;
                        pax = pic[y * w + x - 1];
                        pay = pic[(y - 1) * w + x];
                        pic[y * w + x] = (byte)(((pax + pay) >> 1) - raw[i]);
                        i++;
                    }
                    if (0 != (w % 2)) i++;
                }
            }
            return pic;
        }

        static void SaveQntPixels(Stream stream, FreeImageBitmap bitmap)
        {
            int w = bitmap.Width;
            int h = bitmap.Height;

            byte[] pic = GetBGRFromBitmap(bitmap, w, h);
            byte[] raw = EncodeQntPixels(pic, w, h);

            using (var zlibStream = new ZLibStream(stream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                zlibStream.Write(raw, 0, raw.Length);
                zlibStream.Flush();
            }
        }

        private static byte[] EncodeQntPixels(byte[] pic, int w, int h)
        {
            byte[] pic2 = EncodeQntPixelsFilter(pic, w, h);
            byte[] raw = InterleaveQntPixels(w, h, pic2);
            return raw;
        }

        private static byte[] InterleaveQntPixels(int w, int h, byte[] pic2)
        {
            byte[] raw = new byte[(w + 10) * (h + 10) * 3];
            int i, x, y;
            int j = 0;
            for (i = 2; i >= 0; i--)
            {
                for (y = 0; y < (h - 1); y += 2)
                {
                    for (x = 0; x < (w - 1); x += 2)
                    {
                        raw[j + 0] = pic2[(y * w + x) * 3 + i];
                        raw[j + 1] = pic2[((y + 1) * w + x) * 3 + i];
                        raw[j + 2] = pic2[(y * w + x + 1) * 3 + i];
                        raw[j + 3] = pic2[((y + 1) * w + x + 1) * 3 + i];
                        j += 4;
                    }
                    if (x != w)
                    {
                        raw[j] = pic2[(y * w + x) * 3 + i];
                        raw[j + 1] = pic2[((y + 1) * w + x) * 3 + i];
                        //raw[j + 2] = pic2[(y * w + x + 1) * 3 + i];
                        //if (((y + 1) * w + x + 1) * 3 + i < pic2.Length)
                        //{
                        //    raw[j + 3] = pic2[((y + 1) * w + x + 1) * 3 + i];
                        //}
                        //else
                        //{

                        //}
                        j += 4;
                    }
                }
                if (y != h)
                {
                    for (x = 0; x < (w - 1); x += 2)
                    {
                        raw[j + 0] = pic2[(y * w + x) * 3 + i];
                        raw[j + 2] = pic2[(y * w + x + 1) * 3 + i];
                        j += 4;
                    }
                    if (x != w)
                    {
                        raw[j] = pic2[(y * w + x) * 3 + i];
                        j += 4;
                    }
                }
            }

            MemoryStream ms = new MemoryStream(raw);
            var br = new BinaryReader(ms);
            return br.ReadBytes(j);

            //return raw;
        }

        private static byte[] EncodeQntPixelsFilter(byte[] pic, int w, int h)
        {
            byte[] pic2 = new byte[w * h * 3];

            int x, y;

            pic2[0] = pic[0];
            pic2[1] = pic[1];
            pic2[2] = pic[2];

            if (w > 1)
            {
                for (x = 1; x < w; x++)
                {
                    pic2[x * 3 + 0] = (byte)(pic[(x - 1) * 3 + 0] - pic[x * 3 + 0]);
                    pic2[x * 3 + 1] = (byte)(pic[(x - 1) * 3 + 1] - pic[x * 3 + 1]);
                    pic2[x * 3 + 2] = (byte)(pic[(x - 1) * 3 + 2] - pic[x * 3 + 2]);
                }
            }

            if (h > 1)
            {
                for (y = 1; y < h; y++)
                {
                    pic2[y * w * 3 + 0] = (byte)(pic[(y - 1) * w * 3 + 0] - pic[y * w * 3 + 0]);
                    pic2[y * w * 3 + 1] = (byte)(pic[(y - 1) * w * 3 + 1] - pic[y * w * 3 + 1]);
                    pic2[y * w * 3 + 2] = (byte)(pic[(y - 1) * w * 3 + 2] - pic[y * w * 3 + 2]);

                    for (x = 1; x < w; x++)
                    {
                        int px, py;
                        py = pic[((y - 1) * w + x) * 3];
                        px = pic[(y * w + x - 1) * 3];
                        pic2[(y * w + x) * 3] = (byte)-(pic[(y * w + x) * 3] - ((py + px) >> 1));
                        py = pic[((y - 1) * w + x) * 3 + 1];
                        px = pic[(y * w + x - 1) * 3 + 1];
                        pic2[(y * w + x) * 3 + 1] = (byte)-(pic[(y * w + x) * 3 + 1] - ((py + px) >> 1));
                        py = pic[((y - 1) * w + x) * 3 + 2];
                        px = pic[(y * w + x - 1) * 3 + 2];
                        pic2[(y * w + x) * 3 + 2] = (byte)-(pic[(y * w + x) * 3 + 2] - ((py + px) >> 1));
                    }
                }
            }

            return pic2;
        }

        private static byte[] GetBGRFromBitmap(FreeImageBitmap bitmap, int w, int h)
        {
            byte[] pic = new byte[(w + 10) * (h + 10) * 3];

            int o = 0;
            //get pic from bitmap
            for (int y = 0; y < h; y++)
            {
                var scanline = bitmap.GetScanlineFromTop8Bit(y);
                if (bitmap.ColorDepth == 24)
                {
                    for (int x = 0; x < w; x++)
                    {
                        //get image data from RGB to BGR
                        pic[o++] = scanline[x * 3 + 2];
                        pic[o++] = scanline[x * 3 + 1];
                        pic[o++] = scanline[x * 3 + 0];
                    }
                }
                else if (bitmap.ColorDepth == 32)
                {
                    for (int x = 0; x < w; x++)
                    {
                        //get image data from RGB to BGR
                        pic[o++] = scanline[x * 4 + 2];
                        pic[o++] = scanline[x * 4 + 1];
                        pic[o++] = scanline[x * 4 + 0];
                    }
                }
            }
            return pic;
        }

        static void SaveQntAlpha(Stream stream, FreeImageBitmap bitmap)
        {
            int w, h;
            w = bitmap.Width;
            h = bitmap.Height;

            byte[] pic = GetAlphaFromBitmap(bitmap, w, h);
            byte[] raw = EncodeQntAlpha(w, h, pic);

            using (var zlibStream = new ZLibStream(stream, CompressionMode.Compress, CompressionLevel.Level9, true))
            {
                zlibStream.Write(raw, 0, raw.Length);
                zlibStream.Flush();
            }
        }

        private static byte[] GetAlphaFromBitmap(FreeImageBitmap bitmap, int w, int h)
        {
            byte[] pic = new byte[(w + 10) * (h + 10)];
            int o = 0;
            //get pic from bitmap
            for (int y = 0; y < h; y++)
            {
                var scanline = bitmap.GetScanlineFromTop8Bit(y);
                for (int x = 0; x < w; x++)
                {
                    //get alpha channel
                    pic[o++] = scanline[x * 4 + 3];
                }
            }
            return pic;
        }

        private static byte[] EncodeQntAlpha(int w, int h, byte[] pic)
        {
            int outW = w;
            if (0 != (outW & 1))
            {
                outW++;
            }


            byte[] raw = new byte[outW * h];
            int i, x, y;
            i = 1;
            if (w > 1)
            {
                raw[0] = pic[0];
                for (x = 1; x < w; x++)
                {
                    raw[i] = (byte)(-pic[x] + pic[x - 1]);
                    i++;
                }
                if (0 != (w % 2)) i++;
            }

            if (h > 1)
            {
                for (y = 1; y < h; y++)
                {
                    //solve for raw[i]
                    raw[i] = (byte)(-pic[y * w] + pic[(y - 1) * w]);
                    i++;
                    for (x = 1; x < w; x++)
                    {
                        int pax, pay;
                        pax = pic[y * w + x - 1];
                        pay = pic[(y - 1) * w + x];
                        raw[i] = (byte)(-pic[y * w + x] + ((pax + pay) >> 1));
                        i++;
                    }
                    if (0 != (w % 2)) i++;
                }
            }

            return raw;
        }

        public static void SaveImage(Stream stream, FreeImageBitmap bitmap)
        {
            string comment = bitmap.Comment;
            var qntHeader = new QntHeader();
            if (string.IsNullOrEmpty(comment) || !qntHeader.ParseComment(comment))
            {
                qntHeader.HeaderSize = 0x44;
                qntHeader.FileVersion = 2;
            }
            if (qntHeader.ColorDepth == 0 || qntHeader.ColorDepth == 32)
            {
                qntHeader.ColorDepth = 24;
            }
            qntHeader.Height = bitmap.Height;
            qntHeader.Width = bitmap.Width;
            qntHeader.PixelSize = 0;
            qntHeader.AlphaSize = 0;

            long headerPosition = stream.Position;
            SaveQntHeader(stream, qntHeader);

            long imageStartPosition = stream.Position;
            long imageEndPosition;
            long alphaStartPosition = headerPosition;
            long alphaEndPosition = headerPosition;
            if (bitmap.ColorDepth == 32)
            {
                SaveQntPixels(stream, bitmap);
                imageEndPosition = stream.Position;
                alphaStartPosition = stream.Position;
                SaveQntAlpha(stream, bitmap);
                alphaEndPosition = stream.Position;
            }
            else
            {
                SaveQntPixels(stream, bitmap);
                imageEndPosition = stream.Position;
            }
            qntHeader.PixelSize = (int)(imageEndPosition - imageStartPosition);
            qntHeader.AlphaSize = (int)(alphaEndPosition - alphaStartPosition);

            long endPosition = stream.Position;
            stream.Position = headerPosition;
            //update QNT header
            SaveQntHeader(stream, qntHeader);
            stream.Position = endPosition;
        }
    }
}
