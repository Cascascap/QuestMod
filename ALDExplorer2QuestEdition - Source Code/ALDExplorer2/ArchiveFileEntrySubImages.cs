using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace ALDExplorer2
{
    using Node = ArchiveFileSubimages.SubImageFinder.Node;
    using SubImageFinder = ArchiveFileSubimages.SubImageFinder;
using FreeImageAPI;

    public partial class ArchiveFileEntry : IWithIndex, IWithParent<ArchiveFile>
    {
        internal ArchiveFileEntry[] subImages = null;
        internal bool alreadyLookedForSubImages = false;

        partial void CheckForSubImages(ref bool hasSubImages)
        {
            hasSubImages = this.HasSubImages();
        }

        partial void WriteDataToStreamSubImages(Stream stream, bool doNotConvert)
        {
            if (this.subImages == null && !this.alreadyLookedForSubImages)
            {
                WriteDataToStream2(stream, doNotConvert);
                return;
            }

            bool anyDirty = false;
            //are subimages clean?  Return original container
            foreach (var subimage in this.subImages)
            {
                if (subimage.HasReplacementData())
                {
                    anyDirty = true;
                    break;
                }
            }

            if (!anyDirty)
            {
                WriteDataToStream2(stream, doNotConvert);
                return;
            }

            byte[] containerBytes;
            {
                var msContainer = new MemoryStream();
                WriteDataToStream2(msContainer, doNotConvert);
                containerBytes = msContainer.ToArray();
            }

            var subImages = this.GetSubImages();
            List<Node> nodes = new List<Node>();
            foreach (var subImage in subImages)
            {
                var oldNode = subImage.Tag as Node;
                if (oldNode != null)
                {
                    var ms = new MemoryStream();
                    subImage.WriteDataToStream(ms);
                    var newBytes = ms.ToArray();
                    var newNode = oldNode.Clone();
                    newNode.Bytes = newBytes;
                    nodes.Add(newNode);
                }
            }
            var subImageFinder = new SubImageFinder(this);
            var newFileBytes = subImageFinder.ReplaceSubImageNodes(containerBytes, nodes.ToArray());
            stream.Write(newFileBytes, 0, newFileBytes.Length);
            return;
        }

        partial void TryWriteOriginalSubImage(Stream stream, ref bool wroteOriginalSubImage)
        {
            var node = this.Tag as Node;
            if (node != null)
            {
                if (node.Bytes != null)
                {
                    stream.Write(node.Bytes, 0, node.Bytes.Length);
                    wroteOriginalSubImage = true;
                }
            }
        }

        partial void TryWriteModifiedSubImage(Stream stream, bool doNotConvert, ref bool wroteModifiedSubImage)
        {
            var node = this.Tag as Node;
            if (node != null)
            {
                if (node.Bytes != null)
                {
                    stream.Write(node.Bytes, 0, node.Bytes.Length);
                    wroteModifiedSubImage = true;
                }
                else
                {
                    var parent = node.Parent;
                    var ms = new MemoryStream();
                    parent.WriteDataToStream(ms, doNotConvert);
                    ms.Position = this.FileAddress;
                    ms.WriteToStream(stream, FileSize);
                    wroteModifiedSubImage = true;
                }
            }
        }

        partial void ClearSubImages()
        {
            this.subImages = null;
            this.alreadyLookedForSubImages = false;
        }

        partial void GetParentFileEntry(ref ArchiveFileEntry parentEntry)
        {
            var node = this.Tag as Node;
            if (node != null)
            {
                parentEntry = node.Parent;
            }
        }
    }

    public static partial class ArchiveFileSubimages
    {
        static bool SupportsSwf()
        {
            bool supportsSwf = false;
            SupportsSwf(ref supportsSwf);
            return supportsSwf;
        }

        static bool SupportsFlat()
        {
            bool supportsFlat = false;
            SupportsFlat(ref supportsFlat);
            return supportsFlat;
        }

        static bool SupportsWipf()
        {
            bool supportsWipf = false;
            SupportsWipf(ref supportsWipf);
            return supportsWipf;
        }

        static partial void SupportsSwf(ref bool supportsSwf);
        static partial void SupportsFlat(ref bool supportsFlat);
        static partial void SupportsWipf(ref bool supportsWipf);

        public static bool HasSubImages(this ArchiveFileEntry entry)
        {
            var node = entry.Tag as Node;
            if (node != null) return false;

            string ext = Path.GetExtension(entry.FileName).ToLowerInvariant();
            if (((ext == ".swf" || ext == ".aff") && SupportsSwf()) || (ext == ".flat" && SupportsFlat()) || ((ext == ".wip" || ext == ".msk") && SupportsWipf()))
            {
                return true;
            }
            return false;
        }

        public static ArchiveFileEntry[] GetSubImages(this ArchiveFileEntry entry)
        {
            if (!entry.HasSubImages())
            {
                return null;
            }
            if (entry.subImages != null)
            {
                return entry.subImages;
            }
            if (entry.alreadyLookedForSubImages)
            {
                return null;
            }

            var subImageFinder = new SubImageFinder(entry);
            var nodes = subImageFinder.GetSubImageNodes();
            entry.alreadyLookedForSubImages = true;
            if (nodes != null)
            {
                List<ArchiveFileEntry> entriesList = new List<ArchiveFileEntry>();
                foreach (var node in nodes)
                {
                    var dummyEntry = GetDummyEntry(node);
                    entriesList.Add(dummyEntry);
                }
                var subImages = entriesList.ToArray();
                entry.subImages = subImages;
                return subImages;
            }
            return null;
        }

        internal static ArchiveFileEntry GetDummyEntry(Node node)
        {
            var dummyEntry = new ArchiveFileEntry();
            dummyEntry.Index = -1;
            dummyEntry.Parent = node.Parent.Parent;
            dummyEntry.FileAddress = node.Parent.FileAddress + node.Offset;
            dummyEntry.FileSize = node.Bytes.Length;
            dummyEntry.FileName = node.FileName;
            dummyEntry.Tag = node;
            return dummyEntry;
        }

        public partial class SubImageFinder
        {
            ArchiveFileEntry entry;
            public SubImageFinder(ArchiveFileEntry entry)
            {
                this.entry = entry;
            }

            public Node[] GetSubImageNodes()
            {
                var bytes = entry.GetFileData();

                return GetSubImageNodes(bytes);
            }

            public Node[] GetSubImageNodes(byte[] bytes)
            {
                string sig = ASCIIEncoding.ASCII.GetString(bytes, 0, 3);
                string sig4 = ASCIIEncoding.ASCII.GetString(bytes, 0, 4);
                if (sig == "FLA")
                {
                    return GetSubImageNodesFlat(bytes);
                }
                if (sig == "AFF")
                {
                    return GetSubImageNodesAff(bytes);
                }
                if (sig == "FWS" || sig == "CWS")
                {
                    return GetSubImageNodesSwf(bytes);
                }
                if (sig4 == "WIPF")
                {
                    return GetSubImageNodesWipf(bytes);
                }
                if (sig4 == "ELNA")
                {
                    return GetSubImageNodesFlat(bytes);
                }
                return null;
            }

            public byte[] ReplaceSubImageNodes(byte[] bytes, Node[] nodes)
            {
                string sig = ASCIIEncoding.ASCII.GetString(bytes, 0, 3);
                string sig4 = ASCIIEncoding.ASCII.GetString(bytes, 0, 4);
                if (sig == "FLA")
                {
                    return ReplaceSubImageNodesFlat(bytes, nodes);
                }
                if (sig == "AFF")
                {
                    return ReplaceSubImageNodesAff(bytes, nodes);
                }
                if (sig == "FWS" || sig == "CWS")
                {
                    return ReplaceSubImageNodesSwf(bytes, nodes);
                }
                if (sig4 == "WIPF")
                {
                    return ReplaceSubImageNodesWipf(bytes, nodes);
                }
                if (sig4 == "ELNA")
                {
                    return ReplaceSubImageNodesFlat(bytes, nodes);
                }
                return null;
            }

            public class Node : ICloneable
            {
                public string FileName;
                public byte[] Bytes;
                public long Offset;
                public ArchiveFileEntry Parent;
                public object Tag;

                /// <summary>
                /// Performs a shallow clone of the node object
                /// </summary>
                /// <returns>A shallow clone</returns>
                public Node Clone()
                {
                    return (Node)this.MemberwiseClone();
                }

                #region ICloneable Members

                object ICloneable.Clone()
                {
                    return Clone();
                }

                #endregion
            }

            public Node[] GetSubImageNodesFlat(byte[] bytes)
            {
                Node[] nodes = null;
                GetSubImageNodesFlat(bytes, ref nodes);
                return nodes;
            }

            public byte[] ReplaceSubImageNodesFlat(byte[] bytes, Node[] nodes)
            {
                byte[] outputBytes = null;
                ReplaceSubImageNodesFlat(bytes, nodes, ref outputBytes);
                return outputBytes;
            }
            partial void GetSubImageNodesFlat(byte[] bytes, ref Node[] nodes);
            partial void ReplaceSubImageNodesFlat(byte[] bytes, Node[] nodes, ref byte[] outputBytes);

            public Node[] GetSubImageNodesAff(byte[] bytes)
            {
                return GetSubImageNodesSwf(ConvertAffToSwf(bytes));
            }

            byte[] ConvertAffToSwf(byte[] bytes)
            {
                byte[] outputBytes = null;
                ConvertAffToSwf(bytes, ref outputBytes);
                return outputBytes;
            }

            byte[] ConvertSwfToAff(byte[] bytes)
            {
                byte[] outputBytes = null;
                ConvertSwfToAff(bytes, ref outputBytes);
                return outputBytes;
            }

            public Node[] GetSubImageNodesSwf(byte[] bytes)
            {
                Node[] nodes = null;
                GetSubImageNodesSwf(bytes, ref nodes);
                return nodes;
            }

            public byte[] ReplaceSubImageNodesAff(byte[] bytes, Node[] nodes)
            {
                return ConvertSwfToAff(ReplaceSubImageNodesSwf(ConvertAffToSwf(bytes), nodes));
            }

            public byte[] ReplaceSubImageNodesSwf(byte[] bytes, Node[] nodes)
            {
                byte[] outputBytes = null;
                ReplaceSubImageNodesSwf(bytes, nodes, ref outputBytes);
                return outputBytes;
            }

            partial void ConvertAffToSwf(byte[] bytes, ref byte[] outputBytes);
            partial void ConvertSwfToAff(byte[] bytes, ref byte[] outputBytes);
            partial void GetSubImageNodesSwf(byte[] bytes, ref Node[] nodesResult);
            partial void ReplaceSubImageNodesSwf(byte[] bytes, Node[] nodes, ref byte[] outputBytes);

            private Node[] GetSubImageNodesWipf(byte[] bytes)
            {
                Node[] nodes = null;
                GetSubImageNodesWipf(bytes, ref nodes);
                return nodes;
            }

            private byte[] ReplaceSubImageNodesWipf(byte[] bytes, Node[] nodes)
            {
                byte[] outputBytes = null;
                ReplaceSubImageNodesWipf(bytes, nodes, ref outputBytes);
                return outputBytes;
            }

            partial void GetSubImageNodesWipf(byte[] bytes, ref Node[] nodes);
            partial void ReplaceSubImageNodesWipf(byte[] bytes, Node[] nodes, ref byte[] outputBytes);
        
        }
    }

}
