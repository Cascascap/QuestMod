using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using FreeImageAPI;

namespace ALDExplorer2
{
    using ImageFileFormats;

    public partial class ArchiveFileEntry : IWithIndex, IWithParent<ArchiveFile>
    {
        partial void TryWriteReplacementImage(Stream stream, ref bool converted)
        {
            string extension = Path.GetExtension(this.FileName).ToLowerInvariant();
            string replacementFileExtension = Path.GetExtension(ReplacementFileName).ToLowerInvariant();

            FixFileExtension(ref extension);

            if (replacementFileExtension != extension)
            {
                if (extension == ".vsp")
                {
                    using (FreeImageBitmap referenceImage = GetOriginalImageVSP())
                    {
                        using (FreeImageBitmap imagefile = new FreeImageBitmap(this.ReplacementFileName))
                        {
                            if (referenceImage != null)
                            {
                                var vspHeader = new VspHeader();
                                vspHeader.ParseComment(referenceImage.Comment);
                                bool is8Bit = vspHeader.Is8Bit == 1;

                                bool paletteMatches = false;
                                if (imagefile.ColorDepth == 4 && ImageConverter.PaletteMatches(imagefile.Palette, referenceImage.Palette, 0, 16))
                                {
                                    paletteMatches = true;
                                }
                                if (imagefile.ColorDepth == 8 && ImageConverter.PaletteMatches(imagefile.Palette, referenceImage.Palette, 0, 256))
                                {
                                    paletteMatches = true;
                                }
                                if (!paletteMatches || GlobalSettings.AlwaysRemapImages)
                                {
                                    if (!is8Bit)
                                    {
                                        ImageConverter.RemapPalette(imagefile, referenceImage, 16);
                                    }
                                    else
                                    {
                                        ImageConverter.RemapPalette(imagefile, referenceImage, 256);
                                    }
                                    //convert image file to 4-bit
                                    //imagefile.Quantize(FREE_IMAGE_QUANTIZE.FIQ_NNQUANT, 16, 16, referenceImage.Palette);
                                }

                                imagefile.Comment = referenceImage.Comment;
                            }
                            else
                            {
                                var vspHeader = new VspHeader();

                                if (!String.IsNullOrEmpty(imagefile.Comment))
                                {
                                    vspHeader.ParseComment(referenceImage.Comment);
                                    bool is8Bit = vspHeader.Is8Bit == 1;
                                    imagefile.Comment = vspHeader.GetComment();
                                }
                            }
                            ImageConverter.SaveVsp(stream, imagefile);
                        }
                    }

                    converted = true;
                }
                if (extension == ".pms")
                {
                    using (FreeImageBitmap referenceImage = GetOriginalImagePMS())
                    {
                        using (FreeImageBitmap imageFile = new FreeImageBitmap(this.ReplacementFileName))
                        {
                            if (referenceImage != null)
                            {
                                bool paletteMatches = false;
                                if (imageFile.ColorDepth == 8 && ImageConverter.PaletteMatches(imageFile.Palette, referenceImage.Palette, 0, 256))
                                {
                                    paletteMatches = true;
                                }
                                if (!paletteMatches || GlobalSettings.AlwaysRemapImages)
                                {
                                    ImageConverter.RemapPalette(imageFile, referenceImage, 256);
                                }
                                //var pmsHeader = referenceImage.Tag as PmsHeader;
                                //if (pmsHeader != null) imageFile.Tag = pmsHeader.Clone();
                                imageFile.Comment = referenceImage.Comment;
                                referenceImage.Dispose();
                            }
                            ImageConverter.SavePms(stream, imageFile);
                            converted = true;
                        }
                    }
                }
                if (extension == ".qnt")
                {
                    var qntHeader = GetOriginalImageHeaderQNT();
                    using (FreeImageBitmap imageFile = new FreeImageBitmap(this.ReplacementFileName))
                    {
                        if (qntHeader != null)
                        {
                            imageFile.Comment = qntHeader.GetComment();
                        }
                        ImageConverter.SaveQnt(stream, imageFile);
                        converted = true;
                    }
                }
                if (extension == ".jpg")
                {
                    if (Path.GetExtension(this.ReplacementFileName).ToLowerInvariant() == ".jpg")
                    {

                    }
                    else
                    {
                        FreeImageBitmap bitmap = FreeImageBitmap.FromFile(this.ReplacementFileName);
                        var alphaChannel = bitmap.GetChannel(FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                        if (alphaChannel != null)
                        {
                            bitmap.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_24_BPP);
                        }

                        bitmap.Save(stream, FREE_IMAGE_FORMAT.FIF_JPEG, FREE_IMAGE_SAVE_FLAGS.JPEG_QUALITYGOOD);
                        converted = true;
                    }
                }
                if (extension == ".ajp")
                {
                    var ajpHeader = GetOriginalImageHeaderAJP();
                    if (Path.GetExtension(this.ReplacementFileName).ToLowerInvariant() == ".jpg")
                    {
                        string replacementFileName = this.ReplacementFileName;
                        string alphaFileName;
                        bool alphaFileExists = GetAlphaFileName(replacementFileName, out alphaFileName);

                        if (ajpHeader == null)
                        {
                            if (alphaFileExists)
                            {
                                FreeImageBitmap alphaImage = new FreeImageBitmap(alphaFileName);
                                alphaImage.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_08_BPP | FREE_IMAGE_COLOR_DEPTH.FICD_FORCE_GREYSCALE | FREE_IMAGE_COLOR_DEPTH.FICD_REORDER_PALETTE);
                                var jpegFile = File.ReadAllBytes(this.ReplacementFileName);
                                ImageConverter.SaveAjp(stream, jpegFile, alphaImage, ajpHeader);
                            }
                            else
                            {
                                var jpegFile = File.ReadAllBytes(this.ReplacementFileName);
                                ImageConverter.SaveAjp(stream, jpegFile, null, ajpHeader);
                            }
                            converted = true;
                            goto after;
                        }
                        else
                        {
                            if (ajpHeader.HasAlpha && alphaFileExists)
                            {
                                FreeImageBitmap alphaImage = new FreeImageBitmap(alphaFileName);
                                alphaImage.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_08_BPP | FREE_IMAGE_COLOR_DEPTH.FICD_FORCE_GREYSCALE | FREE_IMAGE_COLOR_DEPTH.FICD_REORDER_PALETTE);
                                var jpegFile = File.ReadAllBytes(this.ReplacementFileName);
                                ImageConverter.SaveAjp(stream, jpegFile, alphaImage, ajpHeader);
                                converted = true;
                                goto after;
                            }
                            else if (!ajpHeader.HasAlpha)
                            {
                                var jpegFile = File.ReadAllBytes(this.ReplacementFileName);
                                ImageConverter.SaveAjp(stream, jpegFile, null, ajpHeader);
                                converted = true;
                                goto after;
                            }
                        }
                    }
                    using (FreeImageBitmap imageFile = new FreeImageBitmap(this.ReplacementFileName))
                    {
                        if (ajpHeader != null)
                        {
                            imageFile.Comment = ajpHeader.GetComment();
                        }
                        ImageConverter.SaveAjp(stream, imageFile);
                        converted = true;
                    }
                after:
                    ;
                }
                if (extension == ".agf")
                {
                    var agfHeader = GetOriginalBytes(0x74);
                    //if (agfHeader != null)
                    //{

                    //}
                    using (var imageFile = new FreeImageBitmap(this.ReplacementFileName))
                    {
                        if (agfHeader != null)
                        {
                            imageFile.Tag = agfHeader;
                        }
                        converted = true;
                        ImageConverter.SaveAgf(stream, imageFile);
                    }

                }
            }
            if ((extension == ".swf" || extension == ".aff"))
            {
                bool wantAff = true;

                var referenceData = this.GetOriginalBytes(3);
                if (referenceData != null)
                {
                    string sig = ASCIIEncoding.ASCII.GetString(referenceData, 0, 3);
                    if (sig == "FWS" || sig == "CWS")
                    {
                        wantAff = false;
                    }
                }
                if (referenceData == null && extension == ".swf" && replacementFileExtension == ".swf")
                {
                    wantAff = false;
                }

                var swfBytes = File.ReadAllBytes(ReplacementFileName);
                bool isSwf = true;
                string sig2 = ASCIIEncoding.ASCII.GetString(swfBytes, 0, 3);
                if (sig2 == "AFF")
                {
                    isSwf = false;
                }

                if (isSwf && wantAff)
                {
                    swfBytes = SwfToAffConverter.ConvertSwfToAff(swfBytes);
                }
                if (!isSwf && !wantAff)
                {
                    swfBytes = SwfToAffConverter.ConvertAffToSwf(swfBytes);
                }

                stream.Write(swfBytes, 0, swfBytes.Length);
                converted = true;
            }
        }

        private static bool GetAlphaFileName(string replacementFileName, out string alphaFileName)
        {
            bool alphaFileExists = false;
            string alphaFileDirectory = Path.Combine(Path.GetDirectoryName(replacementFileName), "SP");
            string alphaImageName = Path.GetFileNameWithoutExtension(replacementFileName) + ".bmp";
            alphaFileName = Path.Combine(alphaFileDirectory, alphaImageName);
            alphaFileExists = false;
            alphaFileExists = File.Exists(alphaFileName);
            if (!alphaFileExists)
            {
                alphaImageName = Path.ChangeExtension(alphaImageName, ".png");
                alphaFileName = Path.Combine(alphaFileDirectory, alphaImageName);
                alphaFileExists = File.Exists(alphaFileName);
            }
            return alphaFileExists;
        }

        private QntHeader GetOriginalImageHeaderQNT()
        {
            return CallFunctionOnFile((fs) => ImageConverter.LoadQntHeader(fs));
        }

        private PmsHeader GetOriginalImageHeaderPMS()
        {
            return CallFunctionOnFile((fs) => ImageConverter.LoadPmsHeader(fs));
        }

        private AjpHeader GetOriginalImageHeaderAJP()
        {
            return CallFunctionOnFile((fs) => ImageConverter.LoadAjpHeader(fs));
        }

        private FreeImageBitmap GetOriginalImageQNT()
        {
            byte[] originalBytes = GetOriginalBytes();
            FreeImageBitmap referenceImage = null;
            if (originalBytes != null)
            {
                referenceImage = ImageConverter.LoadQnt(originalBytes);
            }
            return referenceImage;
        }

        private FreeImageBitmap GetOriginalImageAJP()
        {
            byte[] originalBytes = GetOriginalBytes();
            FreeImageBitmap referenceImage = null;
            if (originalBytes != null)
            {
                referenceImage = ImageConverter.LoadAjp(originalBytes);
            }
            return referenceImage;
        }

        private FreeImageBitmap GetOriginalImageVSP()
        {
            byte[] originalBytes = GetOriginalBytes();
            FreeImageBitmap referenceImage = null;
            if (originalBytes != null)
            {
                referenceImage = ImageConverter.LoadVsp(originalBytes);
            }
            return referenceImage;
        }

        private FreeImageBitmap GetOriginalImagePMS()
        {
            byte[] originalBytes = GetOriginalBytes();
            FreeImageBitmap referenceImage = null;
            if (originalBytes != null)
            {
                referenceImage = ImageConverter.LoadPms(originalBytes);
            }
            return referenceImage;
        }
    }
}
