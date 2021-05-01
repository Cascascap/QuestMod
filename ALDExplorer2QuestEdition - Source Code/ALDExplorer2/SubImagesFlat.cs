using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace ALDExplorer2
{
    public static partial class ArchiveFileSubimages
    {
        static partial void SupportsFlat(ref bool supportsFlat)
        {
            supportsFlat = true;
        }

        public partial class SubImageFinder
        {
            class Tag
            {
                public byte[] TagData;
                public string TagName;
                public int TagLength;
            }

            Tag ReadTag(BinaryReader br)
            {
                var tag = new Tag();
                var tagNameBytes = br.ReadBytes(4);
                tag.TagName = ASCIIEncoding.ASCII.GetString(tagNameBytes);
                tag.TagLength = br.ReadInt32();
                if (tag.TagLength < 0 || tag.TagLength > br.BaseStream.Length - br.BaseStream.Position)
                {
                    return null;
                }
                tag.TagData = br.ReadBytes(tag.TagLength);
                return tag;
            }

            private void WriteTag(BinaryWriter bw, Tag tag)
            {
                bw.WriteStringFixedSize(tag.TagName, 4);
                bw.Write((int)tag.TagData.Length);
                bw.Write(tag.TagData);
            }

            static Encoding shiftJis = Encoding.GetEncoding("shift_jis");

            partial void GetSubImageNodesFlat(byte[] bytes, ref Node[] nodes)
            {
                List<Node> list = new List<Node>();
                var ms1 = new MemoryStream(bytes);
                var br1 = new BinaryReader(ms1);

                bool isVersion2 = false;

                while (br1.BaseStream.Position < br1.BaseStream.Length)
                {
                    long offset1 = br1.BaseStream.Position;
                    var tag = ReadTag(br1);
                    if (tag.TagName == "ELNA")
                    {
                        isVersion2 = true;
                    }
                    if (tag.TagName == "LIBL")
                    {
                        var dataBytes = tag.TagData;
                        var ms = new MemoryStream(dataBytes);
                        var br = new BinaryReader(ms);
                        int fileCount = br.ReadInt32(); //9
                        for (int fileNumber = 0; fileNumber < fileCount; fileNumber++)
                        {
                            int fileNameLength = br.ReadInt32(); //8
                            var fileNameBytes = br.ReadBytes(fileNameLength);

                            if (isVersion2)
                            {
                                for (int i = 0; i < fileNameBytes.Length; i++)
                                {
                                    fileNameBytes[i] ^= 0x55;
                                }
                            }

                            string fileName = shiftJis.GetString(fileNameBytes);
                            br.BaseStream.Position = ((br.BaseStream.Position - 1) | 3) + 1;
                            int unknown1 = br.ReadInt32();  //
                            int dataLength = br.ReadInt32();

                            int unknown2 = br.ReadInt32();
                            byte[] imageBytes; ;
                            long offset2 = br.BaseStream.Position + offset1 + 8;
                            if (unknown2 == 0x00544E51) //QNT
                            {
                                br.BaseStream.Position -= 4;
                                offset2 -= 4;
                                imageBytes = br.ReadBytes(dataLength);
                            }
                            else
                            {
                                imageBytes = br.ReadBytes(dataLength - 4);
                            }

                            list.Add(new Node() { Bytes = imageBytes, FileName = fileName, Offset = offset2, Parent = this.entry });
                            br.BaseStream.Position = ((br.BaseStream.Position - 1) | 3) + 1;
                        }
                    }
                }
                nodes = list.ToArray();
            }

            partial void ReplaceSubImageNodesFlat(byte[] bytes, Node[] nodes, ref byte[] outputBytes)
            {
                var ms1 = new MemoryStream(bytes);
                var br1 = new BinaryReader(ms1);

                var msOutput = new MemoryStream();
                var bw = new BinaryWriter(msOutput);

                bool isVersion2 = false;

                while (br1.BaseStream.Position < br1.BaseStream.Length)
                {
                    var tag = ReadTag(br1);
                    if (tag.TagName == "ELNA")
                    {
                        isVersion2 = true;
                    }
                    if (tag.TagName == "LIBL")
                    {
                        bw.WriteStringFixedSize("LIBL", 4);
                        long tagLengthPos = bw.BaseStream.Position;
                        bw.Write((int)0);

                        var dataBytes = tag.TagData;
                        var ms = new MemoryStream(dataBytes);
                        var br = new BinaryReader(ms);
                        int fileCount = br.ReadInt32();
                        bw.Write((int)fileCount);
                        for (int fileNumber = 0; fileNumber < fileCount; fileNumber++)
                        {
                            int fileNameLength = br.ReadInt32();
                            var fileNameBytes = br.ReadBytes(fileNameLength);
                            if (isVersion2)
                            {
                                for (int i = 0; i < fileNameBytes.Length; i++)
                                {
                                    fileNameBytes[i] ^= 0x55;
                                }
                            }
                            string fileName = shiftJis.GetString(fileNameBytes);
                            long lastPosition = br.BaseStream.Position;
                            br.BaseStream.Position = ((br.BaseStream.Position - 1) | 3) + 1;
                            int paddingCount1 = (int)(br.BaseStream.Position - lastPosition);

                            int unknown1 = br.ReadInt32();
                            int dataLength = br.ReadInt32();
                            int unknown2 = -1;
                            bool hasWordBeforeImage = false;
                            unknown2 = br.ReadInt32();
                            if (unknown2 == 0x00544E51)
                            {
                                br.BaseStream.Position -= 4;
                                hasWordBeforeImage = false;
                            }
                            else
                            {
                                dataLength -= 4;
                                hasWordBeforeImage = true;
                            }
                            //int unknown2 = br.ReadInt32();

                            byte[] imageBytes;
                            //br.BaseStream.Position -= 4;
                            imageBytes = br.ReadBytes(dataLength);

                            lastPosition = br.BaseStream.Position;
                            br.BaseStream.Position = ((br.BaseStream.Position - 1) | 3) + 1;
                            int paddingCount2 = (int)(br.BaseStream.Position - lastPosition);

                            var node = nodes[fileNumber];
                            //write replacement node
                            var newFileNameBytes = shiftJis.GetBytes(node.FileName);

                            long outputPosition = bw.BaseStream.Position;

                            if (isVersion2)
                            {
                                for (int i = 0; i < newFileNameBytes.Length; i++)
                                {
                                    newFileNameBytes[i] ^= 0x55;
                                }
                            }

                            bw.Write((int)newFileNameBytes.Length);
                            bw.Write(newFileNameBytes);

                            int outputPadding1 = (int)(((((bw.BaseStream.Position - outputPosition) - 1) | 3) + 1) - (bw.BaseStream.Position - outputPosition));
                            for (int i = 0; i < outputPadding1; i++)
                            {
                                bw.Write((byte)0);
                            }

                            bw.Write((int)unknown1);
                            bw.Write((int)(node.Bytes.Length + (hasWordBeforeImage ? 4 : 0)));

                            if (hasWordBeforeImage)
                            {
                                bw.Write((int)unknown2);
                            }
                            
                            bw.Write(node.Bytes);

                            int outputPadding2 = (int)(((((bw.BaseStream.Position - outputPosition) - 1) | 3) + 1) - (bw.BaseStream.Position - outputPosition));

                            for (int i = 0; i < outputPadding2; i++)
                            {
                                bw.Write((byte)0);
                            }
                        }

                        long lastPos = bw.BaseStream.Position;
                        bw.BaseStream.Position = tagLengthPos;
                        int tagLength = (int)(lastPos - (tagLengthPos + 4));
                        bw.Write((int)tagLength);
                        bw.BaseStream.Position = lastPos;
                    }
                    else
                    {
                        WriteTag(bw, tag);
                    }

                }
                outputBytes = msOutput.ToArray();
            }


        }
    }
}
