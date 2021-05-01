using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using DDW.Swf;
using FreeImageAPI;

namespace ALDExplorer2
{
    public static partial class ArchiveFileSubimages
    {
        static partial void SupportsSwf(ref bool supportsSwf)
        {
            supportsSwf = true;
        }

        public partial class SubImageFinder
        {
            partial void ConvertAffToSwf(byte[] bytes, ref byte[] outputBytes)
            {
                outputBytes = SwfToAffConverter.ConvertAffToSwf(bytes);
            }

            partial void ConvertSwfToAff(byte[] bytes, ref byte[] outputBytes)
            {
                outputBytes = SwfToAffConverter.ConvertSwfToAff(bytes);
            }

            partial void GetSubImageNodesSwf(byte[] bytes, ref Node[] nodesResult)
            {
                List<Node> nodes = new List<Node>();

                SwfTagList2 swfTags = new SwfTagList2();
                swfTags.ReadSwf(bytes);

                foreach (var tag in swfTags)
                {
                    if (tag.TagType == TagType.DefineBitsLossless || tag.TagType == TagType.DefineBitsLossless2)
                    {
                        var node = new Node();
                        node.Bytes = tag.Data;
                        node.FileName = "img" + tag.DefineId.ToString("0000") + ".png";
                        node.Parent = this.entry;
                        nodes.Add(node);
                    }
                    if (tag.TagType == TagType.DefineSound)
                    {
                        var defineSoundTag = tag.Tag as DefineSoundTag;
                        var node = new Node();
                        var ms = new MemoryStream();
                        var bw = new BinaryWriter(ms);

                        if (defineSoundTag.SoundFormat == SoundCompressionType.MP3)
                        {
                            node.Bytes = defineSoundTag.SoundData;
                            node.FileName = "sound" + tag.DefineId.ToString("0000") + ".mp3";
                        }
                        else if (defineSoundTag.SoundFormat == SoundCompressionType.UncompressedLE)
                        {
                            int channels = defineSoundTag.IsStereo ? 2 : 1;
                            var soundData = defineSoundTag.SoundData;
                            int sampleRate = (int)defineSoundTag.SoundRate;

                            WriteWaveFile(bw, channels, soundData, sampleRate);
                            node.FileName = "sound" + tag.DefineId.ToString("0000") + ".wav";
                            node.Bytes = ms.ToArray();
                        }

                        node.Parent = this.entry;
                        nodes.Add(node);
                    }
                }

                nodesResult = nodes.ToArray();
            }

            private static void WriteWaveFile(BinaryWriter bw, int channels, byte[] soundData, int sampleRate)
            {
                bw.WriteStringFixedSize("RIFF", 4);
                bw.Write(0x24 + soundData.Length);
                bw.WriteStringFixedSize("WAVE", 4);
                bw.WriteStringFixedSize("fmt ", 4);
                bw.Write(0x10);
                bw.Write((short)1);
                bw.Write((short)channels);
                bw.Write(sampleRate);
                bw.Write((int)(sampleRate * channels * 2));
                bw.Write((short)(channels * 2));
                bw.Write((short)16);
                bw.WriteStringFixedSize("data", 4);
                bw.Write(soundData.Length);
                bw.Write(soundData);
            }

            partial void ReplaceSubImageNodesSwf(byte[] bytes, Node[] nodes, ref byte[] outputBytes)
            {
                Dictionary<int, byte[]> TagIdToBytes = new Dictionary<int, byte[]>();
                for (int i = 0; i < nodes.Length; i++)
                {
                    var node = nodes[i];
                    string fileName = node.FileName;
                    int num = GetNumberFromString(fileName);
                    if (num == -1) { num = i; }
                    TagIdToBytes[num] = node.Bytes;
                }

                SwfTagList2 swfTags = new SwfTagList2();
                swfTags.ReadSwf(bytes);

                for (int i = 0; i < swfTags.Count; i++)
                {
                    var tag = swfTags[i];
                    int defineId = tag.DefineId;
                    var nodeBytes = TagIdToBytes.GetOrNull(defineId);

                    if (nodeBytes != null)
                    {
                        if (tag.TagType == TagType.DefineBitsLossless || tag.TagType == TagType.DefineBitsLossless2)
                        {
                            FreeImageBitmap bitmap = null;
                            bitmap = FileOperations.GetImage(nodeBytes);
                            var imageTag = tag.Tag as DefineBitsLosslessTag;
                            imageTag.SetBitmap(bitmap.ToBitmap());
                            swfTags[i] = new TagWrapper(imageTag);
                        }
                        if (tag.TagType == TagType.DefineSound)
                        {
                            var defineSoundTag = tag.Tag as DefineSoundTag;

                            if (defineSoundTag.SoundFormat == SoundCompressionType.MP3)
                            {
                                defineSoundTag.SoundData = nodeBytes;
                            }
                            else if (defineSoundTag.SoundFormat == SoundCompressionType.UncompressedLE)
                            {
                                var ms = new MemoryStream(nodeBytes);
                                var br = new BinaryReader(ms);
                                //skip to DATA tag of riff wav
                                string riff = br.ReadStringFixedSize(4);
                                int fileLength = br.ReadInt32();
                                string wave = br.ReadStringFixedSize(4);

                                string tagName;
                                int tagLength;

                                while (ms.Position < ms.Length)
                                {
                                    tagName = br.ReadStringFixedSize(4);
                                    tagLength = br.ReadInt32();
                                    if (tagName == "data")
                                    {
                                        defineSoundTag.SoundData = br.ReadBytes(tagLength);
                                        break;
                                    }
                                    else
                                    {
                                        br.BaseStream.Position += tagLength;
                                    }
                                }
                                swfTags[i] = new TagWrapper(defineSoundTag);
                            }
                        }
                    }
                }

                MemoryStream output = new MemoryStream();
                swfTags.WriteSwf(output);
                outputBytes = output.ToArray();
            }

            private static int GetNumberFromString(string fileName)
            {
                //find a digit
                int i = 0;
                for (i = 0; i < fileName.Length; i++)
                {
                    char c = fileName[i];
                    if (c >= '0' && c <= '9')
                    {
                        break;
                    }
                }
                if (i >= fileName.Length)
                {
                    return -1;
                }
                int firstDigitIndex = i;

                //find a non-digit
                for (; i < fileName.Length; i++)
                {
                    char c = fileName[i];
                    if (c >= '0' && c <= '9')
                    {

                    }
                    else
                    {
                        break;
                    }
                }
                int firstNonDigitIndex = i;

                string digits = fileName.Substring(firstDigitIndex, firstNonDigitIndex - firstDigitIndex);
                int value;
                if (int.TryParse(digits, out value))
                {
                    return value;
                }
                return -1;
            }

        }
    }
}
