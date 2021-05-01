using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using FreeImageAPI;

namespace ALDExplorer2
{
    partial class ArchiveFileEntry
    {
        partial void FixFileExtension(ref string extension)
        {
            //is this a subimage of a flat file?  Then make it a qnt file
            ArchiveFileEntry parentEntry = null;
            GetParentFileEntry(ref parentEntry);
            if (parentEntry != null)
            {
                string parentExt = Path.GetExtension(parentEntry.FileName).ToLowerInvariant();
                if (parentExt == ".flat")
                {
                    if (extension == ".png")
                    {
                        extension = ".qnt";
                    }
                }
            }
        }

        partial void SetReplacementFileNameJpegCheck()
        {
            if (Path.GetExtension(this.FileName).ToLowerInvariant() == ".jpg")
            {
                var alphaEntry = this.GetAlphaFileForJpeg();
                if (alphaEntry != null)
                {
                    alphaEntry.GetReplacementFileData += new EventHandler<GetReplacementFileDataEventArgs>(alphaEntry_GetReplacementFileData);
                }
            }
        }

        private ArchiveFileEntry GetAlphaFileForJpeg()
        {
            ArchiveFileEntry alphaFileEntry = null;

            if (Path.GetExtension(this.FileName).ToLowerInvariant() == ".jpg")
            {
                //do we have a separate alpha channel bitmap?
                var parent = this.Parent;
                if (parent != null)
                {
                    var parent2 = parent.Parent;
                    if (parent2 != null)
                    {
                        alphaFileEntry = parent2.FileEntriesByNumber.GetOrNull(this.FileNumber + 10000);
                        if (alphaFileEntry != null)
                        {
                            if (Path.GetExtension(alphaFileEntry.FileName).ToLowerInvariant() != ".pms")
                            {
                                alphaFileEntry = null;
                            }
                        }
                    }
                }
            }
            return alphaFileEntry;
        }

        private ArchiveFileEntry GetJpegFileForAlpha()
        {
            ArchiveFileEntry jpegFileEntry = null;
            if (Path.GetExtension(this.FileName).ToLowerInvariant() == ".pms")
            {
                //are we an alpha channel, and have a separate JPEG file?
                var parent = this.Parent;
                if (parent != null)
                {
                    var parent2 = parent.Parent;
                    if (parent2 != null)
                    {
                        jpegFileEntry = parent2.FileEntriesByNumber.GetOrNull(this.FileNumber - 10000);
                        if (jpegFileEntry != null)
                        {
                            if (Path.GetExtension(jpegFileEntry.FileName).ToLowerInvariant() != ".jpg")
                            {
                                jpegFileEntry = null;
                            }
                        }
                    }
                }
            }
            return jpegFileEntry;
        }

        static void alphaEntry_GetReplacementFileData(object sender, ArchiveFileEntry.GetReplacementFileDataEventArgs e)
        {
            var aldFileEntry = sender as ArchiveFileEntry;
            var jpegFileEntry = aldFileEntry.GetJpegFileForAlpha();
            if (jpegFileEntry == null)
            {
                e.Handled = false;
                return;
            }

            string originalComment = null;
            var originalPmsHeader = aldFileEntry.GetOriginalImageHeaderPMS();
            if (originalPmsHeader != null)
            {
                originalComment = CommentUtil.GetComment(originalPmsHeader);
            }


            string pngFileName = Path.ChangeExtension(jpegFileEntry.ReplacementFileName, ".png");
            string replacementExt = Path.GetExtension(jpegFileEntry.ReplacementFileName).ToLowerInvariant();

            //PNG file with embedded alpha channel?
            if ((replacementExt == ".png" || replacementExt == ".jpg") && File.Exists(pngFileName))
            {
                FreeImageBitmap image = FreeImageBitmap.FromFile(pngFileName);
                var alphaChannel = image.GetChannel(FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                if (alphaChannel == null)
                {
                    string alphaFileName;
                    bool alphaFileExists = GetAlphaFileName(jpegFileEntry.ReplacementFileName, out alphaFileName);
                    if (alphaFileExists)
                    {
                        FreeImageBitmap alphaImage = FreeImageBitmap.FromFile(alphaFileName);
                        if (originalComment != null) alphaImage.Comment = originalComment;

                        alphaImage.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_08_BPP | FREE_IMAGE_COLOR_DEPTH.FICD_FORCE_GREYSCALE | FREE_IMAGE_COLOR_DEPTH.FICD_REORDER_PALETTE);
                        ImageConverter.SavePms(e.OutputStream, alphaImage);
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    if (originalComment != null) alphaChannel.Comment = originalComment;
                    ImageConverter.SavePms(e.OutputStream, alphaChannel);
                    e.Handled = true;
                    return;
                }
            }

            //check for separate Alpha file in "SP" directory (BMP or PNG)
            if (Path.GetExtension(jpegFileEntry.ReplacementFileName).ToLowerInvariant() == ".jpg")
            {
                string alphaFileName;
                bool alphaFileExists = GetAlphaFileName(jpegFileEntry.ReplacementFileName, out alphaFileName);
                if (alphaFileExists)
                {
                    FreeImageBitmap alphaImage = FreeImageBitmap.FromFile(alphaFileName);
                    alphaImage.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_08_BPP | FREE_IMAGE_COLOR_DEPTH.FICD_FORCE_GREYSCALE | FREE_IMAGE_COLOR_DEPTH.FICD_REORDER_PALETTE);
                    ImageConverter.SavePms(e.OutputStream, alphaImage);
                    e.Handled = true;
                    return;
                }
            }

            e.Handled = false;
            return;
        }
    }
}
