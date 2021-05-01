using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace ALDExplorer2
{
    using Node = ArchiveFileSubimages.SubImageFinder.Node;
    using System.Runtime.InteropServices;
    using FreeImageAPI;
    using System.Drawing.Imaging;
    public static partial class ArchiveFileSubimages
    {
        static partial void SupportsWipf(ref bool supportsWipf)
        {
            supportsWipf = true;
        }

        public partial class SubImageFinder
        {
            partial void GetSubImageNodesWipf(byte[] bytes, ref Node[] nodes)
            {
                var ms = new MemoryStream(bytes);
                WipFile wipFile = new WipFile(ms);
                nodes = wipFile.GetNodes();
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].Parent = this.entry;
                }
            }

            partial void ReplaceSubImageNodesWipf(byte[] bytes, Node[] nodes, ref byte[] outputBytes)
            {
                throw new NotImplementedException();
            }

        }
    }

    public class WipFile
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WipHeader
        {
            public int signature;
            public ushort imgCount;
            public ushort bitDepth;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WipImageHeader
        {
            public int width, height, x, y, z, compSize;
        }

        WipHeader wipHeader;
        WipImageHeader[] imageHeaders = null;

        private Stream stream;
        private long baseAddress;

        public WipFile(Stream stream)
        {
            baseAddress = stream.Position;
            this.stream = stream;

            try
            {
                var br = new BinaryReader(stream);
                this.wipHeader = br.ReadStruct<WipHeader>();

                if (BinaryUtil.IntToString(this.wipHeader.signature) != "WIPF")
                {
                    throw new InvalidDataException("Signature not WIPF");
                }

                this.imageHeaders = br.ReadStructs<WipImageHeader>(this.wipHeader.imgCount);
            }
            finally
            {
                stream.Position = baseAddress;
            }
        }

        public Node[] GetNodes()
        {
            var nodes = new List<Node>();
            var br = new BinaryReader(stream);

            long address = baseAddress + Marshal.SizeOf(typeof(WipHeader)) + Marshal.SizeOf(typeof(WipImageHeader)) * this.imageHeaders.Length;
            int i = 0;
            foreach (var imageHeader in this.imageHeaders)
            {
                var node = new Node();
                long nextAddress = address + imageHeader.compSize;
                node.Offset = address - baseAddress;
                node.Tag = imageHeader;
                node.FileName = i.ToString("000") + ".WIPF";

                stream.Position = address;
                node.Bytes = new byte[0];// br.ReadBytes(imageHeader.compSize);
                address = nextAddress;
                nodes.Add(node);

                i++;
            }
            return nodes.ToArray();
        }

        public long GetSubimageAddress(int index)
        {
            long address = baseAddress + Marshal.SizeOf(typeof(WipHeader)) + Marshal.SizeOf(typeof(WipImageHeader)) * this.imageHeaders.Length;
            for (int i = 0; i < index; i++)
            {
                var imageHeader = this.imageHeaders[i];
                address = address + imageHeader.compSize;
            }
            return address;
        }

        public static FreeImageBitmap GetBitmap(byte[] compressedBytes, WipImageHeader imageHeader, int colorDepth)
        {
            var ms = new MemoryStream(compressedBytes);
            return GetBitmap(ms, imageHeader, colorDepth);
        }

        public static FreeImageBitmap GetBitmap(Stream compressedBytes, WipImageHeader imageHeader, int colorDepth)
        {
            var br = new BinaryReader(compressedBytes);

            byte[] palette = null;

            if (colorDepth == 8)
            {
                palette = br.ReadBytes(768);
            }

            byte[] bytes = Decompress(compressedBytes, imageHeader.compSize);
            switch (colorDepth)
            {
                case 8:
                    {
                        int inputStride = imageHeader.width;
                        int stride = inputStride;
                        if (0 != (stride & 3))
                        {
                            stride |= 3;
                            stride++;
                            byte[] bytes2;
                            //copy each scanline

                            bytes2 = new byte[stride * imageHeader.height];
                            for (int y = 0; y < imageHeader.height; y++)
                            {
                                int sourceIndex = y * inputStride;
                                int destIndex = y * stride;
                                Array.Copy(bytes, sourceIndex, bytes2, destIndex, inputStride);
                            }
                            bytes = bytes2;
                        }


                        FreeImageBitmap bitmap = new FreeImageBitmap(imageHeader.width, imageHeader.height, stride, PixelFormat.Format8bppIndexed, bytes);
                        var rgbPalette = new RGBQUAD[256];
                        for (int i = 0; i < 256; i++)
                        {
                            rgbPalette[i].rgbBlue = palette[i * 4 + 0];
                            rgbPalette[i].rgbGreen = palette[i * 4 + 1];
                            rgbPalette[i].rgbRed = palette[i * 4 + 2];
                            rgbPalette[i].rgbReserved = 255;
                        }
                        bitmap.Palette.AsArray = rgbPalette;

                        bitmap.Tag = imageHeader;
                        return bitmap;
                    }
                    break;
                case 24:
                    {
                        int inputStride = imageHeader.width;
                        int stride = inputStride * 3;
                        if (0 != (stride & 3))
                        {
                            stride |= 3;
                            stride++;
                        }

                        byte[] bytes2 = new byte[stride * imageHeader.height];

                        for (int y = 0; y < imageHeader.height; y++)
                        {
                            int sourceIndex = y * inputStride;
                            int sourceIndex2 = sourceIndex + inputStride * imageHeader.height * 1;
                            int sourceIndex3 = sourceIndex + inputStride * imageHeader.height * 2;

                            int outputIndex = y * stride;

                            for (int x = 0; x < inputStride; x++)
                            {
                                bytes2[outputIndex + 0] = bytes[sourceIndex];
                                bytes2[outputIndex + 1] = bytes[sourceIndex2];
                                bytes2[outputIndex + 2] = bytes[sourceIndex3];
                                outputIndex += 3;
                                sourceIndex++;
                                sourceIndex2++;
                                sourceIndex3++;
                            }
                        }

                        FreeImageBitmap bitmap = new FreeImageBitmap(imageHeader.width, imageHeader.height, stride, PixelFormat.Format24bppRgb, bytes2);
                        bitmap.Tag = imageHeader;
                        return bitmap;
                    }
                    break;
                case 32:
                    {
                        int inputStride = imageHeader.width;
                        int stride = inputStride * 4;
                        byte[] bytes2 = new byte[stride * imageHeader.height];

                        for (int y = 0; y < imageHeader.height; y++)
                        {
                            int sourceIndex = y * inputStride;
                            int sourceIndex2 = sourceIndex + inputStride * imageHeader.height * 1;
                            int sourceIndex3 = sourceIndex + inputStride * imageHeader.height * 2;
                            int sourceIndex4 = sourceIndex + inputStride * imageHeader.height * 3;

                            int outputIndex = y * stride;

                            for (int x = 0; x < inputStride; x++)
                            {
                                bytes2[outputIndex + 0] = bytes[sourceIndex];
                                bytes2[outputIndex + 1] = bytes[sourceIndex2];
                                bytes2[outputIndex + 2] = bytes[sourceIndex3];
                                bytes2[outputIndex + 3] = bytes[sourceIndex4];
                                outputIndex += 4;
                                sourceIndex++;
                                sourceIndex2++;
                                sourceIndex3++;
                                sourceIndex4++;
                            }
                        }

                        FreeImageBitmap bitmap = new FreeImageBitmap(imageHeader.width, imageHeader.height, stride, PixelFormat.Format32bppArgb, bytes2);
                        bitmap.Tag = imageHeader;
                        return bitmap;
                    }
                    break;
            }
            return null;
        }

        public FreeImageBitmap GetBitmap(int index)
        {
            long address = GetSubimageAddress(index);
            stream.Position = address;
            var imageHeader = this.imageHeaders[index];

            int colorDepth = this.wipHeader.bitDepth;

            return GetBitmap(this.stream, imageHeader, colorDepth);
        }

        private static byte[] Decompress(Stream inputStream, int compressedSize)
        {
            byte[] slidingWindow = new byte[4096];
            int slidingWindowIndex = 0;
            int slidingWindowMask = 0xFFF;

            int bytesRemaining = compressedSize;

            var br = new BinaryReader(inputStream);
            MemoryStream outputStream = new MemoryStream();
            var bw = new BinaryWriter(outputStream);

            int temp1, temp2, EAX, ECX, EDI;
            byte AL, DI;

            temp1 = 0;
            while (bytesRemaining > 0)
            {
                EAX = temp1;
                EAX >>= 1;
                temp1 = EAX;
                if (0 == ((EAX & 0xFF00) & 0x100))
                {
                    AL = 0;
                    AL = br.ReadByte();
                    bytesRemaining--;
                    EAX = AL | 0xFF00;
                    temp1 = EAX;
                }
                if (0 != ((temp1 & 0xFF) & 0x01))
                {
                    AL = br.ReadByte();
                    bytesRemaining--;
                    bw.Write(AL);
                    slidingWindow[slidingWindowIndex] = AL;
                    slidingWindowIndex++;
                    slidingWindowIndex &= slidingWindowMask;
                }
                else
                {
                    DI = br.ReadByte();
                    AL = br.ReadByte();
                    bytesRemaining -= 2;
                    EDI = ((DI << 8) | AL) >> 4;
                    EAX = (AL & 0x0F) + 2;
                    temp2 = EAX;
                    ECX = 0;

                    if (EAX > 0)
                    {
                        while (ECX < temp2)
                        {
                            EAX = (ECX + EDI) & slidingWindowMask;
                            AL = slidingWindow[EAX];
                            bw.Write(AL);
                            slidingWindow[slidingWindowIndex] = AL;
                            slidingWindowIndex++;
                            slidingWindowIndex &= slidingWindowMask;
                            ECX++;
                        }
                    }
                }
            }
            return outputStream.ToArray();
        }
    }

}
