using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using FreeImageAPI;
using ALDExplorer2.ImageFileFormats;
using System.Diagnostics;
using DDW.Swf;
using System.Windows.Forms;

namespace ALDExplorer2
{
    /// <summary>
    /// Settings for FileOperations when running in Console Mode
    /// </summary>
    public class ConsoleModeArguments
    {
        /// <summary>
        /// When opening an archive file, use this filename
        /// </summary>
        public string InputArchiveFileName = "";
        /// <summary>
        /// When saving an archive file, use this filename
        /// </summary>
        public string OutputArchiveFileName = "";
        /// <summary>
        /// When importing multiple files from a directory, use this as the input directory
        /// </summary>
        public string ImportDirectory = "";
        /// <summary>
        /// When exporting multiple files to a directory, use this as the output directory
        /// </summary>
        public string ExportDirectory = "";
        /// <summary>
        /// The filter to use when importing multiple files, such as "*.png" or "*.png;*.swf".
        /// </summary>
        public string ImportFileFilter = "*.*";
        /// <summary>
        /// The file extension to use on imported images when they are imported
        /// </summary>
        public string NewImageExtension = null;
        /// <summary>
        /// The file extension to use on imported flash files when they are imported
        /// </summary>
        public string NewFlashExtension = ".aff";
        /// <summary>
        /// Whether or not to keep directory names when importing (for AFA files)
        /// </summary>
        public bool KeepDirectoryNamesWhenImporting = false;
        /// <summary>
        /// A prefix to put at the beginning of files, such as a common direcory (often "Patch\")
        /// </summary>
        public string ImportFilePrefix = "";
        /// <summary>
        /// If set, only imports files if their modification date is after this minimum date
        /// </summary>
        public DateTime minDate = DateTime.MinValue;
        /// <summary>
        /// When creating an archive file, use this version (currently valid for 1 or 2)
        /// </summary>
        public int Version = 1;
    }

    public partial class FileOperations
    {
        //This part of the parital class FileOperations contains prompts and user interface stuff
        
        /// <summary>
        /// Whether the program is running in Console Mode or not, and should read arguments from the ConsoleModeArguments object instead of opening dialog boxes
        /// </summary>
        public bool ConsoleMode;
        /// <summary>
        /// The arguments for Console Mode
        /// </summary>
        public ConsoleModeArguments consoleModeArguments = new ConsoleModeArguments();

        /// <summary>
        /// When opening an archive file, prompts with an Open dialog, or uses a consoleModeArgument for Console Mode.
        /// </summary>
        /// <returns>The filename</returns>
        private string PromptForArchiveFileName()
        {
            if (this.ConsoleMode)
            {
                return consoleModeArguments.InputArchiveFileName;
            }
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Supported Archive Files|" + ArchiveFile.GetFileFilter() + "|All Files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }
            return null;
        }

        /// <summary>
        /// Displays an error message either in a Message Box, or on the console
        /// </summary>
        /// <param name="errorMessage">The error message to display</param>
        void RaiseError(string errorMessage)
        {
            if (this.ConsoleMode)
            {
                Console.Error.WriteLine(errorMessage);
            }
            else
            {
                MessageBox.Show(errorMessage, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        /// <summary>
        /// Prompts for a directory when importing or exporting files
        /// </summary>
        /// <param name="saving">Set to true if we are exporting files</param>
        /// <returns>The directory</returns>
        private string PromptForDirectory(bool saving)
        {
            string outputPath = null;
            FileDialog fileDialog = null;
            try
            {
                if (saving)
                {
                    var saveFileDialog = new SaveFileDialog();
                    saveFileDialog.OverwritePrompt = false;
                    fileDialog = saveFileDialog;
                    fileDialog.FileName = "SELECT DIRECTORY TO EXPORT FILES TO";
                    fileDialog.Title = "Select directory to export files to";
                }
                else
                {
                    fileDialog = new OpenFileDialog();
                    fileDialog.FileName = "SELECT DIRECTORY TO IMPORT FILES FROM";
                    fileDialog.Title = "Select directory to import files from";
                }
                fileDialog.CheckFileExists = false;
                fileDialog.CheckPathExists = true;
                fileDialog.Filter = "All Files (*.*)|*.*";
                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (Directory.Exists(fileDialog.FileName))
                    {
                        outputPath = fileDialog.FileName;
                    }
                    else
                    {
                        outputPath = Path.GetDirectoryName(fileDialog.FileName);
                    }
                }
                else
                {
                    outputPath = null;
                }
            }
            finally
            {
                if (fileDialog != null)
                {
                    fileDialog.Dispose();
                }
            }
            return outputPath;
        }

        /// <summary>
        /// Prompts for a directory to import from using a Open dialog, or uses a consoleModeArgument in Console Mode.
        /// </summary>
        /// <returns>The directory to import from</returns>
        private string PromptForImportDirectory()
        {
            if (this.ConsoleMode)
            {
                return consoleModeArguments.ImportDirectory;
            }
            return PromptForDirectory(false);
        }

        /// <summary>
        /// Prompts for a directory to extract to using a Save As dialog, or uses a consoleModeArgument in Console Mode.
        /// </summary>
        /// <returns>The directory to extract to</returns>
        private string PromptForExportDirectory()
        {
            if (this.ConsoleMode)
            {
                return consoleModeArguments.ExportDirectory;
            }
            return PromptForDirectory(true);
        }

        /// <summary>
        /// Prompts for an individual file name to export to using a Save As dialog, or returns inputFileName for Console Mode
        /// </summary>
        /// <param name="inputFileName">The original filename to save</param>
        /// <returns>The selected filename</returns>
        private string PromptForOutputFileName(string inputFileName)
        {
            return PromptForOutputFileName(inputFileName, Path.GetExtension(inputFileName).ToLowerInvariant(), null);
        }

        /// <summary>
        /// Prompts for an individual file name to export to using a Save As dialog, or returns inputFileName for Console Mode.
        /// Picks a file extension based on whether there is a bitmap or not.
        /// </summary>
        /// <param name="inputFileName">The original filename to save</param>
        /// <param name="extension">The extension of the file</param>
        /// <param name="bitmap">A bitmap to know whether it is saving an image or not</param>
        /// <returns>The selected filename</returns>
        private string PromptForOutputFileName(string inputFileName, string extension, FreeImageBitmap bitmap)
        {
            if (this.ConsoleMode)
            {
                return inputFileName;
            }

            inputFileName = inputFileName.Replace('/', '\\');
            string outputFileName;
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.OverwritePrompt = true;
                saveFileDialog.CheckFileExists = false;
                saveFileDialog.CheckPathExists = true;

                if (bitmap != null)
                {
                    saveFileDialog.Filter =
                        "PNG Files (*.png)|*.png|" +
                        "BMP Files (*.bmp)|*.bmp|" +
                        "GIF Files (*.gif)|*.gif|" +
                        "JPG Files (*.jpg)|*.jpg|" +
                        "TGA Files (*.tga)|*.tga|" +
                        "TIF Files (*.tif)|*.tif|" +
                        "QNT Files (*.qnt)|*.qnt|" +
                        "PMS Files (*.pms)|*.pms|" +
                        "AJP Files (*.ajp)|*.ajp|" +
                        "IPH Files (*.iph)|*.iph|" +
                        "AGF Files (*.agf)|*.agf|" +
                        "All Files (*.*)|*.*";
                }
                else if (extension == ".swf" || extension == ".aff")
                {
                    saveFileDialog.Filter =
                        "SWF Files (*.swf)|*.swf|" +
                        "AFF Files (*.aff)|*.aff|" +
                        "All Files (*.*)|*.*";
                }
                else
                {
                    saveFileDialog.Filter = "All Files (*.*)|*.*";
                }
                string desiredExt = GetDesiredExtension(extension, bitmap);

                saveFileDialog.DefaultExt = desiredExt;
                saveFileDialog.FileName = Path.ChangeExtension(inputFileName, desiredExt);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    outputFileName = saveFileDialog.FileName;
                }
                else
                {
                    outputFileName = null;
                }
            }
            return outputFileName;
        }

        /// <summary>
        /// When importing a file, prompts for a file name to import, using an Open File dialog.
        /// </summary>
        /// <param name="fileEntry">The file entry to pick a replacement file for</param>
        /// <returns>The selected filename</returns>
        private string PromptForInputFileName(ArchiveFileEntry fileEntry)
        {
            string defaultInputFileName = GetDefaultInputFileName(fileEntry);
            if (this.ConsoleMode)
            {
                if (File.Exists(defaultInputFileName))
                {
                    return defaultInputFileName;
                }
                else
                {
                    return null;
                }
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                string extension = ".png";
                string initialFileName = "file";
                if (defaultInputFileName != null)
                {
                    extension = Path.GetExtension(fileEntry.FileName).ToLowerInvariant();
                    initialFileName = defaultInputFileName;
                }

                if (extension == ".vsp" || extension == ".pms" || extension == ".qnt" || extension == ".iph" || extension == ".bmp" || extension == ".ajp" || extension == ".agf")
                {
                    openFileDialog.Filter = "PNG Files (*.png)|*.png|All Files (*.*)|*.*";
                    initialFileName = Path.ChangeExtension(initialFileName, ".png");
                }
                else if (extension == ".jpg")
                {
                    openFileDialog.Filter = "JPG Files (*.jpg)|*.png|All Files (*.*)|*.*";
                }
                else
                {
                    openFileDialog.Filter = "All Files (*.*)|*.*";
                }
                openFileDialog.FileName = initialFileName;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Picks a default filename based on the file entry and the import directory, and if that file exists, returns that.
        /// </summary>
        /// <param name="fileEntry">The file entry to find a filename for</param>
        /// <returns>The default filename</returns>
        private string GetDefaultInputFileName(ArchiveFileEntry fileEntry)
        {
            if (fileEntry == null)
            {
                return null;
            }
            string imageFileName = fileEntry.FileName;
            string outputDirectory = "";
            if (this.ConsoleMode)
            {
                outputDirectory = this.consoleModeArguments.ImportDirectory ?? "";
            }
            if (this.IncludeDirectoriesWhenExportingFiles)
            {
                string defaultFileName = Path.Combine(outputDirectory, imageFileName);
                if (File.Exists(defaultFileName))
                {
                    return defaultFileName;
                }
            }
            {
                string defaultFileName = Path.Combine(outputDirectory, Path.GetFileName(imageFileName));
                if (File.Exists(defaultFileName))
                {
                    return defaultFileName;
                }
            }
            return Path.GetFileName(imageFileName);
        }

        /// <summary>
        /// Prompts for a filename to save the archive file as, using a Save As dialog, or using a consoleModeArgument for Console Mode
        /// </summary>
        /// <returns>The filename to save the archive as</returns>
        private string PromptForArchiveOutputFileName()
        {
            if (this.ConsoleMode)
            {
                return this.consoleModeArguments.OutputArchiveFileName;
            }

        TryAgain: ;
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.FileName = loadedArchiveFiles.ArchiveFileName;
                saveFileDialog.Filter = "All Supported Archive Files|" + ArchiveFile.GetFileFilter(Path.GetExtension(loadedArchiveFiles.ArchiveFileName)) + "|All Files (*.*)|*.*";
                saveFileDialog.DefaultExt = Path.GetExtension(loadedArchiveFiles.ArchiveFileName);
                //saveFileDialog.Filter = "AliceSoft Archive Files (*.ALD;*.AFA;*.ALK;*.DAT)|*.ald;*.afa;*.alk;*.dat|All Files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = saveFileDialog.FileName;
                    if (!ValidateAldFilename(fileName))
                    {
                        goto TryAgain;
                    }

                    return fileName;
                }
            }
            return null;
        }

        /// <summary>
        /// Checks that an ALD file is saved with a suitable filename, such as gamenameGA.ALD.
        /// </summary>
        /// <param name="fileName">The filename selected from the dialog box</param>
        /// <returns>True if the filename is okay, or the user accepted the warning message</returns>
        private bool ValidateAldFilename(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return false;
            }
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            string baseName = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            if (ext == ".ald" && loadedArchiveFiles.FileType == ArchiveFileType.AldFile)
            {
                bool isOkay = false;
                if (baseName.Length > 2)
                {
                    char lastChar = baseName[baseName.Length - 1];
                    char secondLastChar = baseName[baseName.Length - 2];
                    if ((lastChar >= 'a' && lastChar <= 'z') || lastChar == '@')
                    {
                        switch (secondLastChar)
                        {
                            case 'g':
                            case 'w':
                            case 'b':
                            case 'd':
                            case 's':
                            case 'm':
                                isOkay = true;
                                break;
                        }
                    }
                }
                if (!isOkay)
                {
                    var dialogResult = MessageBox.Show(
                        "An .ALD file normally has a filename similar to this:" + Environment.NewLine +
                        "<gamename>GA.ALD" + Environment.NewLine +
                        "Types of files: G = graphics, W = waves, B = background music," + Environment.NewLine +
                        "D = data, S = script (System 3.x), M = midi files" + Environment.NewLine +
                        "File letter is A-Z, but @ is also allowed." + Environment.NewLine +
                        "Your filename is not like this, and this could cause problems." + Environment.NewLine +
                        "Are you sure you want to save the file with this name?",
                        "ALDExplorer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                    if (dialogResult == DialogResult.Yes)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (ext == ".dat" && loadedArchiveFiles.FileType == ArchiveFileType.DatFile)
            {
                bool isOkay = false;
                if (baseName.Length > 2)
                {
                    char firstChar = baseName[0];
                    string suffix = baseName.Substring(1);

                    if (firstChar >= 'a' && firstChar <= 'z')
                    {
                        switch (suffix)
                        {
                            case "disk":
                            case "cg":
                            case "dat":
                            case "anim":
                            case "mus":
                            case "musva":
                                isOkay = true;
                                break;
                        }
                    }
                }
                if (!isOkay)
                {
                    var dialogResult = MessageBox.Show(
                        "A System 3 .DAT file normally has a filename similar to this:" + Environment.NewLine +
                        "ADISK.DAT" + Environment.NewLine +
                        "Types of files: DISK = scripts, CG = graphics, MUS = music," + Environment.NewLine +
                        "ANIM = animations, MUSVA = music?" + Environment.NewLine +
                        "File letter is A-Z." + Environment.NewLine +
                        "Your filename is not like this, and this could cause problems." + Environment.NewLine +
                        "Are you sure you want to save the file with this name?",
                        "ALDExplorer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                    if (dialogResult == DialogResult.Yes)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }

            return true;
        }
    }

    public partial class FileOperations
    {
        public ArchiveFileCollection loadedArchiveFiles;
        public bool DoNotConvertImageFiles = false;
        public bool IncludeDirectoriesWhenExportingFiles = true;
        /// <summary>
        /// When adding new files, allow files which match existing filenames to be added
        /// </summary>
        public bool DuplicateFileNamesAllowed = true;

        public bool OpenFile()
        {
            string fileName = PromptForArchiveFileName();
            if (String.IsNullOrEmpty(fileName))
            {
                return false;
            }
            return OpenFile(fileName);
        }

        public bool OpenFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return false;
            }

            ArchiveFileCollection archiveFiles = new ArchiveFileCollection();
            try
            {
                archiveFiles.ReadFile(fileName);
                this.loadedArchiveFiles = archiveFiles;

                if (!this.ConsoleMode) { RecentFilesList.FilesList.Add(fileName); }

                return true;
            }
            catch (IOException ex)
            {
                if (!this.ConsoleMode) { RecentFilesList.FilesList.Remove(fileName); }
                RaiseError("The file \"" + fileName + "\" was not found.");
                return false;
            }
            catch (InvalidDataException ex)
            {
                if (!this.ConsoleMode) { RecentFilesList.FilesList.Remove(fileName); }
                RaiseError("The loaded file is not a valid archive file.");
                return false;
            }
        }

        public int ImportAll()
        {
            string patchDirectory = PromptForImportDirectory();
            if (string.IsNullOrEmpty(patchDirectory))
            {
                return -1;
            }
            return ImportAll(patchDirectory);
        }

        public int ImportAll(string patchDirectory)
        {
            if (loadedArchiveFiles == null)
            {
                return -1;
            }
            if (String.IsNullOrEmpty(patchDirectory))
            {
                return -1;
            }

            var entries = loadedArchiveFiles.FileEntries;
            return ImportAll(patchDirectory, entries);
        }

        public int ImportAll(string patchDirectory, IList<ArchiveFileEntry> entries)
        {
            if (String.IsNullOrEmpty(patchDirectory) || entries == null)
            {
                return -1;
            }
            int fileCount = 0;

            foreach (var entry in entries)
            {
                string fileName = entry.FileName;
                string fileNameThisDirectory = Path.GetFileName(fileName);

                //try current directory and other path
                bool imported = TryImportFile(patchDirectory, entry, fileName);
                if (!imported && fileNameThisDirectory != fileName)
                {
                    imported = TryImportFile(patchDirectory, entry, fileNameThisDirectory);
                }

                if (entry.HasSubImages())
                {
                    var subImages = entry.GetSubImages();
                    string newDirectory1 = Path.Combine(patchDirectory, fileName + "_files");
                    string newDirectory2 = Path.Combine(patchDirectory, fileNameThisDirectory + "_files");
                    if (Directory.Exists(newDirectory1))
                    {
                        fileCount += ImportAll(newDirectory1, subImages);
                    }
                    else if (Directory.Exists(newDirectory2))
                    {
                        fileCount += ImportAll(newDirectory2, subImages);
                    }
                }

                if (imported)
                {
                    fileCount++;
                }
            }
            return fileCount;
        }

        public bool TryImportFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return false;
            }
            string patchDirectory = Path.GetFullPath(Path.GetDirectoryName(fileName));
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            foreach (var entry in this.loadedArchiveFiles.FileEntries)
            {
                string entryBaseName = Path.GetFileNameWithoutExtension(entry.FileName);
                if (baseName.Equals(entryBaseName, StringComparison.OrdinalIgnoreCase))
                {
                    return ImportAll(patchDirectory, new ArchiveFileEntry[] { entry }) >= 1;
                }
            }
            return false;
        }

        public bool TryImportFile(string patchDirectory, ArchiveFileEntry entry, string fileName)
        {
            return TryImportFile(patchDirectory, entry, fileName, null);
        }

        public bool TryImportFile(string patchDirectory, ArchiveFileEntry entry, string fileName, string newExt)
        {
            bool imported = false;
            if (String.IsNullOrEmpty(patchDirectory) || entry == null || String.IsNullOrEmpty(fileName))
            {
                return false;
            }
            if (!String.IsNullOrEmpty(newExt))
            {
                fileName = Path.ChangeExtension(fileName, newExt);
            }

            string externalFileName = Path.Combine(patchDirectory, fileName);
            if (File.Exists(externalFileName))
            {
                //check minDate
                bool okay = true;
                if (this.ConsoleMode && this.consoleModeArguments.minDate > DateTime.MinValue)
                {
                    var fileInfo = new FileInfo(externalFileName);
                    if (fileInfo.LastWriteTime.Date < this.consoleModeArguments.minDate)
                    {
                        okay = false;
                    }
                }
                if (okay)
                {
                    entry.ReplacementFileName = externalFileName;
                    imported = true;
                }
            }
            else if (!this.DoNotConvertImageFiles && String.IsNullOrEmpty(newExt))
            {
                if (TryImportFile(patchDirectory, entry, fileName, ".png")) { return true; }
                if (TryImportFile(patchDirectory, entry, fileName, ".jpg")) { return true; }
                if (TryImportFile(patchDirectory, entry, fileName, ".swf")) { return true; }
                if (TryImportFile(patchDirectory, entry, fileName, ".mp3")) { return true; }
                if (TryImportFile(patchDirectory, entry, fileName, ".ogg")) { return true; }
                if (TryImportFile(patchDirectory, entry, fileName, ".aog")) { return true; }
                if (TryImportFile(patchDirectory, entry, fileName, ".wav")) { return true; }
            }
            return imported;
        }

        public void ExportAll()
        {
            if (this.loadedArchiveFiles == null)
            {
                return;
            }
            ExportFiles(this.loadedArchiveFiles.FileEntries);
        }

        public void ExportAll(string filter)
        {
            if (!string.IsNullOrEmpty(filter) && filter.StartsWith("*."))
            {
                List<ArchiveFileEntry> filesToExport = new List<ArchiveFileEntry>();
                foreach (var entry in this.loadedArchiveFiles.FileEntries)
                {
                    if (Path.GetExtension(entry.FileName).Equals(filter.Substring(1), StringComparison.OrdinalIgnoreCase))
                    {
                        filesToExport.Add(entry);
                    }
                }
                ExportFiles(filesToExport);
            }
            else
            {
                ExportAll();
            }
        }

        public void ExportFile(string fileName, string filter)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                return;
            }
            List<ArchiveFileEntry> filesToExport = new List<ArchiveFileEntry>();
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            foreach (var entry in this.loadedArchiveFiles.FileEntries)
            {
                string entryBaseName = Path.GetFileNameWithoutExtension(entry.FileName);
                if (!String.IsNullOrEmpty(filter) && filter.StartsWith("*."))
                {
                    if (entryBaseName.Length >= baseName.Length && entryBaseName.Substring(0, baseName.Length).Equals(baseName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (Path.GetExtension(entry.FileName).Equals(filter.Substring(1), StringComparison.OrdinalIgnoreCase))
                        {
                            filesToExport.Add(entry);
                        }
                    }
                }
                else
                {
                    if (baseName.Equals(entryBaseName, StringComparison.OrdinalIgnoreCase))
                    {
                        filesToExport.Add(entry);
                    }
                }
            }
            ExportFiles(filesToExport);
        }

        public void ExportFiles(IEnumerable<ArchiveFileEntry> entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException("entries");
            }
            int count = entries.Count();
            if (count == 0)
            {
                return;
            }
            if (count == 1 && !entries.FirstOrDefault().HasSubImages())
            {
                ExportFile(entries.FirstOrDefault());
                return;
            }
            string outputPath = PromptForExportDirectory();
            if (String.IsNullOrEmpty(outputPath))
            {
                return;
            }
            ExportFiles(entries, outputPath);
        }

        public void ExportFile(ArchiveFileEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            ExportFile(entry, null);
        }

        public void ExportFiles(IEnumerable<ArchiveFileEntry> entries, string outputPath)
        {
            if (entries == null)
            {
                throw new ArgumentNullException("entries");
            }
            foreach (var entry in entries)
            {
                ExportFile(entry, outputPath);
            }
        }

        public string GetDesiredExtension(string extension, FreeImageBitmap bitmap)
        {
            string desiredExt = extension ?? "";
            if (bitmap != null)
            {
                desiredExt = ".png";
                if (extension == ".ajp" && bitmap.ColorDepth == 24)
                {
                    desiredExt = ".jpg";
                }
                if (extension == ".jpg")
                {
                    desiredExt = ".jpg";
                }
            }
            if (extension == ".aff" && !this.DoNotConvertImageFiles)
            {
                desiredExt = ".swf";
            }
            if (extension == ".aog" && !this.DoNotConvertImageFiles)
            {
                desiredExt = ".ogg";
            }
            return desiredExt;
        }

        public void ExportFile(ArchiveFileEntry entry, string outputPath)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            string fileName = entry.FileName;
            if (!this.IncludeDirectoriesWhenExportingFiles)
            {
                fileName = Path.GetFileName(fileName);
            }

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (entry.HasSubImages())
            {
                string outputFileName = null;
                if (outputPath == null)
                {
                    outputFileName = PromptForOutputFileName(fileName);
                    if (String.IsNullOrEmpty(outputFileName))
                    {
                        return;
                    }
                    outputPath = Path.GetDirectoryName(outputFileName);
                }
                else
                {
                    outputFileName = fileName;
                    outputPath = Path.GetDirectoryName(outputFileName);
                }

                string outputPathNew;
                outputPathNew = Path.Combine(outputPath, Path.GetFileName(outputFileName) + "_files");

                var subImages = entry.GetSubImages();
                foreach (var subImageEntry in subImages)
                {
                    ExportFile(subImageEntry, outputPathNew);
                }
            }

            FreeImageBitmap bitmap = null;
            if (!this.DoNotConvertImageFiles)
            {
                bitmap = GetBitmapFromFile(entry);
            }

            try
            {
                string outputFileName;
                string desiredExt = GetDesiredExtension(extension, bitmap);

                if (outputPath == null)
                {
                    outputFileName = PromptForOutputFileName(fileName, extension, bitmap);
                    if (String.IsNullOrEmpty(outputFileName))
                    {
                        return;
                    }
                }
                else
                {
                    outputFileName = Path.Combine(outputPath, Path.ChangeExtension(fileName, desiredExt));
                }

                ExportFileWithName(entry, outputFileName, bitmap);
            }
            finally
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
        }

        public void ExportFileWithName(ArchiveFileEntry entry, string outputFileName, FreeImageBitmap bitmap)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            if (String.IsNullOrEmpty(outputFileName))
            {
                outputFileName = entry.FileName;
            }

            string outputDirectory = Path.GetDirectoryName(outputFileName);
            if (!String.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string outputExt = Path.GetExtension(outputFileName).ToLowerInvariant();
            string inputExt = Path.GetExtension(entry.FileName).ToLowerInvariant();

            if (bitmap != null)
            {
                if (outputExt == ".jpg" && inputExt == ".ajp" && bitmap.ColorDepth == 24)
                {
                    byte[] jpegFile;
                    FreeImageBitmap alphaImage;
                    AjpHeader ajpHeader;
                    ImageConverter.LoadAjp(entry.GetFileData(), out jpegFile, out alphaImage, out ajpHeader);
                    File.WriteAllBytes(outputFileName, jpegFile);
                }
                else if (outputExt == ".jpg" && inputExt == ".jpg")
                {
                    File.WriteAllBytes(outputFileName, entry.GetFileData());
                }
                else if (outputExt == ".png" || outputExt == ".gif" || outputExt == ".bmp" || outputExt == ".tga" || outputExt == ".jpg")
                {
                    bitmap.Save(outputFileName);
                }
                else if (outputExt == ".ajp" && inputExt == ".jpg" && bitmap.ColorDepth == 32)
                {
                    using (var fs = File.OpenWrite(outputFileName))
                    {
                        var alphaChannel = bitmap.GetChannel(FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                        ImageConverter.SaveAjp(fs, entry.GetFileData(), alphaChannel, null);
                    }
                }
                else
                {
                    if (inputExt == outputExt)
                    {
                        using (var fs = File.OpenWrite(outputFileName))
                        {
                            entry.WriteDataToStream(fs);
                        }
                    }
                    else
                    {
                        if (outputExt == ".ajp" || outputExt == ".qnt" || outputExt == ".pms" || outputExt == ".vsp" || outputExt == ".iph" || outputExt == ".agf")
                        {
                            using (var fs = File.OpenWrite(outputFileName))
                            {
                                switch (outputExt)
                                {
                                    case ".ajp": Ajp.SaveImage(fs, bitmap); break;
                                    case ".qnt": Qnt.SaveImage(fs, bitmap); break;
                                    case ".pms": Pms.SaveImage(fs, bitmap); break;
                                    case ".vsp": Vsp.SaveImage(fs, bitmap); break;
                                    case ".iph": Iph.SaveIph(fs, bitmap); break;
                                    case ".agf": Agf.SaveAgf(fs, bitmap); break;
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                bitmap.Save(outputFileName);
                            }
                            catch (IOException)
                            {
                                RaiseError("Unable to write to " + outputFileName);
                            }
                            catch
                            {
                                RaiseError("File format " + outputExt + " is not supported.");
                            }
                        }
                    }
                }
            }
            else
            {
                if (!this.DoNotConvertImageFiles && (inputExt == ".aff" || inputExt == ".swf") && outputExt == ".swf")
                {
                    var fileBytes = entry.GetFileData();
                    if (IsAffFile(fileBytes))
                    {
                        fileBytes = SwfToAffConverter.ConvertAffToSwf(fileBytes);
                    }
                    File.WriteAllBytes(outputFileName, fileBytes);
                }
                else if (!this.DoNotConvertImageFiles && (inputExt == ".aff" || inputExt == ".swf") && outputExt == ".aff")
                {
                    var fileBytes = entry.GetFileData();
                    if (!IsAffFile(fileBytes))
                    {
                        fileBytes = SwfToAffConverter.ConvertSwfToAff(fileBytes);
                    }
                    File.WriteAllBytes(outputFileName, fileBytes);
                }
                else
                {
                    using (var fs = File.OpenWrite(outputFileName))
                    {
                        entry.WriteDataToStream(fs);
                    }
                }
            }
        }

        public static bool IsAffFile(byte[] fileBytes)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException("fileBytes");
            }
            bool isAffFile = false;
            string sig = ASCIIEncoding.ASCII.GetString(fileBytes, 0, 3);
            if (sig == "FWS" || sig == "CWS")
            {
                isAffFile = false;
            }
            else if (sig == "AFF")
            {
                isAffFile = true;
            }
            return isAffFile;
        }

        public FreeImageBitmap GetBitmapFromFile(ArchiveFileEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            string fileName = entry.FileName;
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            //bool fileExtensionHandled = false;
            bool fileExtensionSupported =
                (extension == ".vsp") ||
                (extension == ".pms") ||
                (extension == ".qnt") ||
                (extension == ".dcf") ||
                (extension == ".iph") ||
                (extension == ".agf") ||
                (extension == ".ajp") ||
                (extension == ".bmp") ||
                (extension == ".gif") ||
                (extension == ".jpg") ||
                (extension == ".msk") ||
                (extension == ".wipf") ||
                (extension == ".png");

            FreeImageBitmap bitmap = null;
            if (fileExtensionSupported)
            {
                bitmap = GetImage(entry);

                FreeImageBitmap alphaBitmap = null;

                //for JPG files, try a PMS file at +10000 for the alpha channel
                if (this.loadedArchiveFiles.GetArchiveFileType() == ArchiveFileType.AldFile && bitmap != null && extension == ".jpg" && !bitmap.IsTransparent)
                {
                    var alphaChannel = bitmap.GetChannel(FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                    if (alphaChannel == null)
                    {
                        int otherFileNumber = entry.FileNumber + 10000;
                        var otherEntry = this.loadedArchiveFiles.FileEntriesByNumber.GetOrNull(otherFileNumber);
                        if (otherEntry != null)
                        {
                            if (otherEntry.FileName.EndsWith(".pms", StringComparison.OrdinalIgnoreCase))
                            {
                                alphaBitmap = GetImage(otherEntry.GetFileData(true), ".pms");
                            }
                        }
                    }
                }
                else if (bitmap != null && extension == ".png" && !bitmap.IsTransparent)
                {
                    string mskFileName = Path.ChangeExtension(entry.FileName, ".MSK");

                    if (loadedArchiveFiles.FileEntriesByName.ContainsKey(mskFileName))
                    {
                        var alphaChannel = bitmap.GetChannel(FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                        if (alphaChannel == null)
                        {
                            var otherEntry = loadedArchiveFiles.FileEntriesByName.GetOrNull(mskFileName);
                            if (otherEntry != null)
                            {
                                alphaBitmap = GetImage(otherEntry.GetFileData(true), ".msk");
                            }
                        }
                    }
                }

                if (alphaBitmap != null)
                {
                    if (alphaBitmap.Width == bitmap.Width && alphaBitmap.Height == bitmap.Height)
                    {
                        bitmap.ConvertColorDepth(FREE_IMAGE_COLOR_DEPTH.FICD_32_BPP);
                        bitmap.SetChannel(alphaBitmap, FREE_IMAGE_COLOR_CHANNEL.FICC_ALPHA);
                    }
                }
            }

            return bitmap;
        }

        private FreeImageBitmap GetImage(ArchiveFileEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            var fileBytes = entry.GetFileData(true);
            string extension = Path.GetExtension(entry.FileName).ToLowerInvariant();
            FreeImageBitmap bitmap = GetImage(fileBytes, extension);

            if (bitmap == null)
            {
                var node = entry.Tag as ArchiveFileSubimages.SubImageFinder.Node;
                if (node != null)
                {
                    var parentEntry = node.Parent;
                    if (parentEntry != null)
                    {
                        string parentExt = Path.GetExtension(parentEntry.FileName).ToLowerInvariant();
                        if (parentExt == ".wip" || parentExt == ".msk")
                        {
                            using (var fs = parentEntry.GetFileStream())
                            {
                                var imageHeader = (WipFile.WipImageHeader)node.Tag;
                                return WipFile.GetBitmap(fs, imageHeader, 24);
                            }
                        }
                    }
                }
            }
            if (bitmap != null && extension == ".vsp")
            {
                var vspHeader = bitmap.Tag as VspHeader;
                if (vspHeader != null && vspHeader.ColorDepth == 4)
                {
                    //check for an invalid palette (such as in Rance 4, system 3.0 version)
                    bool isOkay = VspPaletteIsInvalid(bitmap);
                    if (!isOkay)
                    {
                        FixVspPalette(entry, bitmap, vspHeader);
                    }
                }
            }
            return bitmap;
        }

        private void FixVspPalette(ArchiveFileEntry entry, FreeImageBitmap bitmap, VspHeader vspHeader)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }
            if (vspHeader == null)
            {
                throw new ArgumentNullException("vspHeader");
            }
            if (this.loadedArchiveFiles == null)
            {
                throw new InvalidOperationException("no archive files loaded");
            }
            //only needed for PC98 versions?
            //search for a VSP image that is 8x2 in size
            //will still end up getting the wrong palette though, but better than the black palette
            foreach (var datFile in this.loadedArchiveFiles.ArchiveFiles)
            {
                byte[] headerBytes = new byte[32];
                if (datFile != null)
                {
                    using (FileStream fileStream = new FileStream(datFile.ArchiveFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        long fileLength = fileStream.Length;
                        foreach (var otherEntry in datFile.FileEntries)
                        {
                            if (otherEntry.FileAddress > 0 && otherEntry.FileAddress < fileLength)
                            {
                                fileStream.Position = otherEntry.FileAddress;
                                fileStream.Read(headerBytes, 0, 32);
                                var otherVspHeader = Vsp.GetHeader(headerBytes);
                                if (otherVspHeader.Height == 2 && otherVspHeader.Width == 1 && otherVspHeader.ColorDepth == 4)
                                {
                                    if (otherVspHeader.PaletteBank == vspHeader.PaletteBank)
                                    {
                                        var otherVspImage = GetImage(otherEntry);
                                        for (int i = 0; i < 16; i++)
                                        {
                                            bitmap.Palette[i] = otherVspImage.Palette[i];
                                        }
                                        break;
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }

        private static bool VspPaletteIsInvalid(FreeImageBitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException("bitmap");
            }
            //only happens in PC98 versions of the games?  Check for a black palette (besides color #4) then white at color 15
            bool isOkay = false;
            for (int i = 0; i < 15; i++)
            {
                if (i != 4 && bitmap.Palette[i].uintValue != 0xFF000000)
                {
                    isOkay = true;
                    break;
                }
            }
            if (bitmap.Palette[15].uintValue != 0xFFFFFFFF)
            {
                isOkay = true;
            }
            if (!isOkay)
            {
                return false;
            }
            return true;
        }

        public static FreeImageBitmap GetImage(byte[] fileBytes)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException("fileBytes");
            }
            return GetImage(fileBytes, "");
        }

        public static FreeImageBitmap GetImage(byte[] fileBytes, string extension)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException("fileBytes");
            }
            extension = extension ?? "";
            FreeImageBitmap bitmap = null;
            var fileStream = new MemoryStream(fileBytes);
            //&& extension == ".vsp"

            //first try just the loader for the matching file extension
            if (bitmap == null && extension == ".qnt") { try { bitmap = ImageConverter.LoadQnt(fileBytes); } catch { } }
            if (bitmap == null && extension == ".png") { try { bitmap = new FreeImageBitmap(fileStream, FREE_IMAGE_FORMAT.FIF_PNG); } catch { } }
            if (bitmap == null && extension == ".pms") { try { bitmap = ImageConverter.LoadPms(fileBytes); } catch { } }
            if (bitmap == null && extension == ".ajp") { try { bitmap = ImageConverter.LoadAjp(fileBytes); } catch { } }
            if (bitmap == null && (extension == ".jpg" || extension == ".jpeg")) { try { bitmap = new FreeImageBitmap(fileStream, FREE_IMAGE_FORMAT.FIF_JPEG); } catch { } }
            if (bitmap == null && extension == ".vsp") { try { bitmap = ImageConverter.LoadVsp(fileBytes); } catch { } }
            if (bitmap == null && extension == ".iph") { try { bitmap = ImageConverter.LoadIph(fileBytes); } catch { } }
            if (bitmap == null && extension == ".agf") { try { bitmap = ImageConverter.LoadAgf(fileBytes); } catch { } }
            if (bitmap == null && extension == ".bmp") { try { bitmap = new FreeImageBitmap(fileStream, FREE_IMAGE_FORMAT.FIF_BMP); } catch { } }
            if (bitmap == null && extension == ".dcf") { try { bitmap = ImageConverter.LoadDcf(fileBytes); } catch { } }
            //if (bitmap == null && extension == ".wipf") { try { bitmap = ImageConverter.LoadWipf(fileBytes

            //if it fails, try them all
            if (bitmap == null) { try { bitmap = ImageConverter.LoadQnt(fileBytes); } catch { } }
            if (bitmap == null) { try { bitmap = new FreeImageBitmap(fileStream, FREE_IMAGE_FORMAT.FIF_PNG); } catch { } }
            if (bitmap == null) { try { bitmap = ImageConverter.LoadPms(fileBytes); } catch { } }
            if (bitmap == null) { try { bitmap = ImageConverter.LoadAjp(fileBytes); } catch { } }
            if (bitmap == null) { try { bitmap = new FreeImageBitmap(fileStream, FREE_IMAGE_FORMAT.FIF_JPEG); } catch { } }
            if (bitmap == null) { try { bitmap = ImageConverter.LoadVsp(fileBytes); } catch { } }
            if (bitmap == null) { try { bitmap = ImageConverter.LoadIph(fileBytes); } catch { } }
            if (bitmap == null) { try { bitmap = ImageConverter.LoadAgf(fileBytes); } catch { } }
            if (bitmap == null) { try { bitmap = ImageConverter.LoadDcf(fileBytes); } catch { } }
            if (bitmap == null) { try { bitmap = new FreeImageBitmap(fileStream, FREE_IMAGE_FORMAT.FIF_BMP); } catch { } }
            if (bitmap == null) { try { bitmap = new FreeImageBitmap(fileStream); } catch { } }
            if (bitmap == null) { try { bitmap = LoadSwfBitmap(fileBytes); } catch (Exception ex) { } }

            if (bitmap == null && Debugger.IsAttached)
            {
                Debugger.Break();
            }
            return bitmap;
        }

        private static FreeImageBitmap LoadSwfBitmap(byte[] fileBytes)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException("fileBytes");
            }
            var swfTagWrapper = new TagWrapper(fileBytes);
            var tag = swfTagWrapper.Tag as DefineBitsLosslessTag;
            return new FreeImageBitmap(tag.GetBitmap());
        }

        public void NewFile(string fileName, int version)
        {
            if (String.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException("fileName");
            }
            ArchiveFile aFile = null;
            this.loadedArchiveFiles = new ArchiveFileCollection();
            aFile = ArchiveFile.CreateArchiveFile(Path.GetExtension(fileName).ToLowerInvariant(), version);
            this.loadedArchiveFiles.ArchiveFiles.Add(aFile);
            aFile.Parent = this.loadedArchiveFiles;
            aFile.ArchiveFileName = fileName;
        }

        public bool NewFile()
        {
            if (this.ConsoleMode)
            {
                return false;
            }
            var desiredFileType = FileFormatSelectionForm.SelectFileType();
            if (desiredFileType == ArchiveFileType.Invalid)
            {
                return false;
            }
            NewFile(desiredFileType);
            return true;
        }

        public void NewFile(ArchiveFileType desiredFileType)
        {
            ArchiveFile aFile = null;

            loadedArchiveFiles = new ArchiveFileCollection();
            switch (desiredFileType)
            {
                case ArchiveFileType.Afa1File:
                case ArchiveFileType.Afa2File:
                case ArchiveFileType.AldFile:
                case ArchiveFileType.AlkFile:
                case ArchiveFileType.DatFile:
                    aFile = new AldArchiveFile();
                    aFile.FileLetter = 1;
                    break;
                case ArchiveFileType.HoneybeeArcFile:
                    aFile = new ArcArchiveFile();
                    break;
                case ArchiveFileType.SofthouseCharaVfs11File:
                case ArchiveFileType.SofthouseCharaVfs20File:
                    aFile = new VfsArchiveFile();
                    break;
                case ArchiveFileType.Invalid:
                    throw new ArgumentException("desiredFileType");
                    break;
            }
            aFile.FileType = desiredFileType;
            loadedArchiveFiles.ArchiveFiles.Add(aFile);
            aFile.ArchiveFileName = "new";
            aFile.Parent = loadedArchiveFiles;

            switch (aFile.FileType)
            {
                case ArchiveFileType.AldFile:
                    aFile.ArchiveFileName += "GA.ald";
                    break;
                case ArchiveFileType.AlkFile:
                    aFile.ArchiveFileName += ".alk";
                    break;
                case ArchiveFileType.DatFile:
                    aFile.ArchiveFileName = "ACG.DAT";
                    break;
                case ArchiveFileType.Afa1File:
                case ArchiveFileType.Afa2File:
                    aFile.ArchiveFileName += ".afa";
                    break;
                case ArchiveFileType.HoneybeeArcFile:
                    aFile.ArchiveFileName += ".arc";
                    break;
                case ArchiveFileType.SofthouseCharaVfs11File:
                case ArchiveFileType.SofthouseCharaVfs20File:
                    aFile.ArchiveFileName += ".vfs";
                    break;
            }
        }

        public void ReplaceFiles(IEnumerable<ArchiveFileEntry> entriesToReplace)
        {
            if (entriesToReplace == null)
            {
                throw new ArgumentNullException("entriesToReplace");
            }
            int count = entriesToReplace.Count();
            if (count == 0)
            {
                return;
            }
            if (count == 1)
            {
                ReplaceFile(entriesToReplace.FirstOrDefault());
                return;
            }

            foreach (var f in entriesToReplace)
            {
                if (!ReplaceFile(f))
                {
                    break;
                }
            }
        }

        public bool ReplaceFile(ArchiveFileEntry fileEntry)
        {
            if (fileEntry == null)
            {
                throw new ArgumentNullException("fileEntry");
            }
            string replacementFileName = PromptForInputFileName(fileEntry);
            if (!String.IsNullOrEmpty(replacementFileName))
            {
                fileEntry.ReplacementFileName = replacementFileName;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void DeleteFiles(IEnumerable<ArchiveFileEntry> entriesToDelete)
        {
            if (entriesToDelete == null)
            {
                throw new ArgumentNullException("entriesToDelete");
            }
            var entryPairsToDelete = GetEntryPairsToDelete(entriesToDelete);
            foreach (var pair in entryPairsToDelete)
            {
                int index = pair.Key;
                var archiveFile = pair.Value;
                if (index >= 0 && index < archiveFile.FileEntries.Count)
                {
                    archiveFile.FileEntries.RemoveAt(index);
                }
            }
        }

        private KeyValuePair<int, ArchiveFile>[] GetEntryPairsToDelete(IEnumerable<ArchiveFileEntry> entriesToDelete)
        {
            if (entriesToDelete == null)
            {
                throw new ArgumentNullException("entriesToDelete");
            }
            this.loadedArchiveFiles.UpdateIndexes();
            var entryPairsToDelete = entriesToDelete.Select(e => new KeyValuePair<int, ArchiveFile>(e.Index, loadedArchiveFiles.GetArchiveFileByLetter(e.FileLetter))).OrderByDescending(pair => pair.Key).ToArray();
            return entryPairsToDelete;
        }

        public bool ImportNewFiles()
        {
            if (loadedArchiveFiles == null)
            {
                return false;
            }
            var archiveFile = this.loadedArchiveFiles.ArchiveFiles.FirstOrDefault();
            return ImportNewFiles(archiveFile);
        }

        public bool ImportNewFiles(ArchiveFile archiveFile)
        {
            if (this.loadedArchiveFiles == null)
            {
                return false;
            }

            string newImageExtension = "";
            string newSwfExtension = ".swf";
            string basePath;
            bool keepDirectoryNames;
            string[] fileNames;

            if (this.ConsoleMode)
            {
                basePath = this.consoleModeArguments.ImportDirectory;
                string filter = this.consoleModeArguments.ImportFileFilter;
                if (String.IsNullOrEmpty(filter))
                {
                    filter = "*.*";
                }
                keepDirectoryNames = false;
                fileNames = GetFileNamesForImportRecursive(basePath, filter, ref keepDirectoryNames);

                newImageExtension = consoleModeArguments.NewImageExtension;
                newSwfExtension = consoleModeArguments.NewFlashExtension;

                if (String.IsNullOrEmpty(newImageExtension))
                {
                    var fileType = this.loadedArchiveFiles.FileType;
                    newImageExtension = GetDefaultImageFileType(fileType);
                }
                if (String.IsNullOrEmpty(newSwfExtension))
                {
                    newSwfExtension = ".aff";
                }

            }
            else
            {
                keepDirectoryNames = false;
                basePath = "";
                fileNames = PromptForImportNewFiles(ref keepDirectoryNames, ref basePath);
                if (fileNames == null || fileNames.Length == 0)
                {
                    return false;
                }
                bool anyPngFiles = AnyPngFiles(fileNames);
                bool anySwfFiles = AnySwfFiles(fileNames);
                if (anyPngFiles)
                {
                    using (var fileTypesForm = new ImportFileTypeForm(".qnt", ".ajp", ".pms", ".vsp", ".iph", ".agf", ".png", ".jpg", ".bmp"))
                    {
                        fileTypesForm.ShowDialog();
                        newImageExtension = fileTypesForm.FileType;
                    }
                }
                if (anySwfFiles)
                {
                    using (var fileTypesForm = new ImportFileTypeForm(".aff", ".swf"))
                    {
                        fileTypesForm.ShowDialog();
                        newSwfExtension = fileTypesForm.FileType;
                    }
                }
            }
            return ImportNewFiles(archiveFile, fileNames, newImageExtension, newSwfExtension, basePath, keepDirectoryNames);

        }

        private static string GetDefaultImageFileType(ArchiveFileType fileType)
        {
            string newImageExtension;
            switch (fileType)
            {
                case ArchiveFileType.DatFile:
                    newImageExtension = ".vsp";
                    break;
                case ArchiveFileType.Afa1File:
                case ArchiveFileType.Afa2File:
                case ArchiveFileType.AldFile:
                case ArchiveFileType.AlkFile:
                case ArchiveFileType.BunchOfFiles:
                    newImageExtension = ".qnt";
                    break;
                case ArchiveFileType.SofthouseCharaVfs11File:
                    newImageExtension = ".iph";
                    break;
                case ArchiveFileType.SofthouseCharaVfs20File:
                    newImageExtension = ".agf";
                    break;
                case ArchiveFileType.HoneybeeArcFile:
                    newImageExtension = ".png";
                    break;
                default:
                    newImageExtension = ".png";
                    break;
            }
            return newImageExtension;
        }

        public bool ImportNewFiles(ArchiveFile archiveFile, IEnumerable<string> fileNames, string newImageExtension, string newSwfExtension, string basePath, bool keepDirectoryNames)
        {
            if (loadedArchiveFiles == null)
            {
                return false;
            }
            if (String.IsNullOrEmpty(newImageExtension))
            {
                newImageExtension = GetDefaultImageFileType(this.loadedArchiveFiles.FileType);
            }
            if (String.IsNullOrEmpty(newSwfExtension))
            {
                newSwfExtension = ".aff";
            }

            int newFileLetter = archiveFile.FileLetter;
            if (fileNames == null || fileNames.FirstOrDefault() == null)
            {
                return false;
            }

            string prefix = "";
            if (this.ConsoleMode)
            {
                prefix = this.consoleModeArguments.ImportFilePrefix;
            }
            else
            {
                var fileType = this.loadedArchiveFiles.FileType;
                if (fileType == ArchiveFileType.Afa1File || fileType == ArchiveFileType.Afa2File)
                {
                    prefix = PrefixForm.GetPrefix(Path.GetDirectoryName(fileNames.First()));
                }
            }

            string basePathUpper = basePath.ToUpperInvariant();

            List<ArchiveFileEntry> entriesToAdd = new List<ArchiveFileEntry>();
            foreach (var fileName in fileNames)
            {
                var entry = new ArchiveFileEntry();
                entry.FileAddress = 0;
                entry.FileHeader = null;

                if (keepDirectoryNames)
                {
                    string fileNameWithDirectory = RemovePathPrefix(basePathUpper, Path.GetFullPath(fileName));
                    entry.FileName = prefix + fileNameWithDirectory;
                }
                else
                {
                    entry.FileName = prefix + Path.GetFileName(fileName);
                }

                if (!this.DoNotConvertImageFiles)
                {
                    string ext = Path.GetExtension(entry.FileName).ToLowerInvariant();
                    if (ext == ".png")
                    {
                        entry.FileName = Path.ChangeExtension(entry.FileName, newImageExtension);
                    }
                    else if (ext == ".swf")
                    {
                        entry.FileName = Path.ChangeExtension(entry.FileName, newSwfExtension);
                    }
                }

                entry.FileNumber = GetFileNumber(entry.FileName);
                entry.FileLetter = newFileLetter;
                var fileInfo = new FileInfo(fileName);
                entry.FileSize = (int)fileInfo.Length;
                entry.HeaderAddress = 0;
                entry.Index = -1;
                entry.Parent = null;
                //Todo: check MIN-DATE here...
                bool okay = true;
                if (this.ConsoleMode && this.consoleModeArguments.minDate > DateTime.MinValue)
                {
                    if (fileInfo.LastWriteTime.Date < this.consoleModeArguments.minDate)
                    {
                        okay = false;
                    }
                }
                entry.ReplacementFileName = fileName;
                if (okay)
                {
                    entriesToAdd.Add(entry);
                }
            }
            SortFileEntries(entriesToAdd);
            if (archiveFile == null)
            {
                archiveFile = this.loadedArchiveFiles.ArchiveFiles.FirstOrDefault();
                if (archiveFile == null)
                {
                    return false;
                }
            }
            int oldCount = archiveFile.FileEntries.Count;
            if (this.DuplicateFileNamesAllowed)
            {
                archiveFile.FileEntries.AddRange(entriesToAdd);
            }
            else
            {
                Dictionary<string, ArchiveFileEntry> filenameToEntryUppercase = new Dictionary<string, ArchiveFileEntry>();
                foreach (var entry in this.loadedArchiveFiles.FileEntries)
                {
                    filenameToEntryUppercase[entry.FileName.ToUpperInvariant()] = entry;
                }

                foreach (var entry in entriesToAdd)
                {
                    string key = entry.FileName.ToUpperInvariant();
                    if (filenameToEntryUppercase.ContainsKey(key))
                    {
                        var existingEntry = filenameToEntryUppercase[key];
                        existingEntry.ReplacementFileName = entry.ReplacementFileName;
                    }
                    else
                    {
                        archiveFile.FileEntries.Add(entry);
                        filenameToEntryUppercase[key] = entry;
                    }
                }
            }
            for (int i = oldCount; i < archiveFile.FileEntries.Count; i++)
            {
                var entry = archiveFile.FileEntries[i];
                entry.Parent = archiveFile;
                entry.Index = i;
            }

            //archiveFile.FileEntries.AddRange(entriesToAdd);
            loadedArchiveFiles.Refresh();
            return true;
        }

        private string[] PromptForImportNewFiles(ref bool KeepDirectoryNames, ref string basePath)
        {
            string[] fileNames = null;
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All Files (*.*)|*.*";
                openFileDialog.FileName = "Add All Images";
                openFileDialog.Title = "Select Files to add...";
                openFileDialog.Multiselect = true;
                openFileDialog.CheckFileExists = false;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileNames = openFileDialog.FileNames;
                    if (fileNames.Length == 1 && !File.Exists(openFileDialog.FileName))
                    {
                        fileNames = new string[0];
                    }
                    if (fileNames.Length == 0)
                    {
                        string path = openFileDialog.FileName;
                        if (!Directory.Exists(path))
                        {
                            path = Path.GetDirectoryName(path);
                        }
                        path = Path.GetFullPath(path);
                        basePath = path;
                        fileNames = GetFileNamesForImportRecursive(path, ref KeepDirectoryNames);
                    }
                }
            }
            return fileNames;
        }

        private static void SortFileEntries(List<ArchiveFileEntry> entriesToAdd)
        {
            if (entriesToAdd == null)
            {
                throw new ArgumentNullException("entriesToAdd");
            }
            entriesToAdd.Sort(CompareArchiveFileEntry);
        }

        static string RemovePathPrefix(string pathPrefixUpper, string path)
        {
            if (path.ToUpperInvariant().StartsWith(pathPrefixUpper))
            {
                return path.Substring(pathPrefixUpper.Length).TrimStart('/', '\\');
            }
            return path;
        }

        public static string[] OnlyNewestFiles(string[] fileNames)
        {
            HashSet<string> BaseNamesSeen = new HashSet<string>();
            HashSet<string> ConflictingNames = new HashSet<string>();

            foreach (var fileName in fileNames)
            {
                string baseName = Path.GetFileName(fileName).ToUpperInvariant();
                if (BaseNamesSeen.Contains(baseName))
                {
                    ConflictingNames.Add(baseName);
                }
                else
                {
                    BaseNamesSeen.Add(baseName);
                }
            }

            List<string> Output = new List<string>();
            Dictionary<string, List<string>> ConflictingFiles = new Dictionary<string, List<string>>();
            foreach (var fileName in fileNames)
            {
                string baseName = Path.GetFileName(fileName).ToUpperInvariant();
                if (ConflictingNames.Contains(baseName))
                {
                    ConflictingFiles.GetOrAddNew(baseName).Add(fileName);
                }
                else
                {
                    Output.Add(fileName);
                }
            }

            foreach (var pair in ConflictingFiles)
            {
                string baseName = pair.Key;
                List<string> names = pair.Value;

                string latestFileName = names.OrderByDescending(fileName => new FileInfo(fileName).LastWriteTimeUtc).FirstOrDefault();
                Output.Add(latestFileName);
            }

            return Output.OrderBy(f => Path.GetFileName(f)).ToArray();
        }

        private string[] GetFileNamesForImportRecursive(string path, ref bool keepDirectoryNames)
        {
            return GetFileNamesForImportRecursive(path, "*.*", ref keepDirectoryNames);
        }

        private string[] GetFileNamesForImportRecursive(string path, string filtersString, ref bool keepDirectoryNames)
        {
            if (String.IsNullOrEmpty(filtersString))
            {
                filtersString = "*.*";
            }
            string[] filters = filtersString.Split(';');
            List<string> fileNamesList = new List<string>();
            List<string> fileNamesThisDirectory = new List<string>();
            foreach (var filter in filters)
            {
                fileNamesList.AddRange(Directory.GetFiles(path, filter, SearchOption.AllDirectories));
                fileNamesThisDirectory.AddRange(Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly));
            }
            if (fileNamesList.SequenceEqual(fileNamesThisDirectory))
            {
                return fileNamesList.ToArray();
            }

            if (this.ConsoleMode)
            {
                keepDirectoryNames = this.consoleModeArguments.KeepDirectoryNamesWhenImporting;
                if (!keepDirectoryNames)
                {
                    return OnlyNewestFiles(fileNamesList.ToArray());
                }
            }
            else
            {
                fileNamesList.AddRange(ImportRecursiveOptionsForm.GetFiles(path, ref keepDirectoryNames));
            }
            return fileNamesList.ToArray();
        }

        static int CompareArchiveFileEntry(ArchiveFileEntry me, ArchiveFileEntry other)
        {
            if (me.FileName != null)
            {
                return me.FileName.CompareTo(other.FileName);
            }
            else if (other.FileName == null)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        private static bool AnyPngFiles(IEnumerable<string> fileNames)
        {
            bool anyPngFiles = (fileNames.Any(f => f.ToLowerInvariant().EndsWith(".png")));
            return anyPngFiles;
        }

        private static bool AnySwfFiles(IEnumerable<string> fileNames)
        {
            bool anySwfFiles = (fileNames.Any(f => f.ToLowerInvariant().EndsWith(".swf")));
            return anySwfFiles;
        }

        private static int GetFileNumber(string fileName)
        {
            int maxIndex = -1;
            int maxLength = 0;

            int currentIndex = -1;
            int currentLength = 0;
            //find the longest number in the filename
            for (int i = 0; i < fileName.Length; i++)
            {
                char c = fileName[i];
                if (char.IsNumber(c))
                {
                    if (currentLength == 0)
                    {
                        currentIndex = i;
                        currentLength = 1;
                    }
                    else
                    {
                        currentLength++;
                    }
                    if (currentLength > maxLength)
                    {
                        maxIndex = currentIndex;
                        maxLength = currentLength;
                    }
                }
                else
                {
                    currentIndex = -1;
                    currentLength = 0;
                }
            }

            int number = 0;
            if (maxIndex >= 0)
            {
                string substr = fileName.Substring(maxIndex, maxLength);
                if (int.TryParse(substr, out number))
                {

                }
                else
                {
                    number = 0;
                }
            }
            return number;
        }

        public bool Save()
        {
            return SaveAs(this.loadedArchiveFiles.ArchiveFileName);
        }

        public bool SaveAs()
        {
            string fileName = PromptForArchiveOutputFileName();
            if (String.IsNullOrEmpty(fileName))
            {
                return false;
            }
            return SaveAs(fileName);
        }

        public bool SaveAs(string fileName)
        {
            if (Debugger.IsAttached)
            {
                loadedArchiveFiles.SaveFile(fileName);
                return true;
            }
            else
            {
                try
                {
                    loadedArchiveFiles.SaveFile(fileName);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool SavePatch()
        {
            if (this.loadedArchiveFiles == null)
            {
                return false;
            }

            var fileType = this.loadedArchiveFiles.FileType;
            if (fileType == ArchiveFileType.AldFile || fileType == ArchiveFileType.DatFile)
            {
                int mFileLetter = 13;
                int zFileLetter = 26;

                var aFile = loadedArchiveFiles.GetArchiveFileByLetter(1, false);
                var mFile = loadedArchiveFiles.GetArchiveFileByLetter(mFileLetter, false);

                string aFileName = null;

                if (aFile != null && mFile == null)
                {
                    aFileName = aFile.ArchiveFileName;
                    string mFileName = loadedArchiveFiles.GetArchiveFileName(aFileName, mFileLetter);
                    if (!File.Exists(mFileName))
                    {
                        string aBaseName = Path.GetFileName(aFileName);
                        string mBaseName = Path.GetFileName(mFileName);
                        string zBaseName = Path.GetFileName(loadedArchiveFiles.GetArchiveFileName(aFileName, zFileLetter));
                        string prompt = "The file " + aBaseName + " will be renamed to " + mBaseName + "." + Environment.NewLine +
                            "A new stub file named " + aBaseName + " will be created to tell the game where all the files are." + Environment.NewLine +
                            "New and modified files will be added to " + zBaseName + "." + Environment.NewLine +
                            "A user installing this patch must rename " + aBaseName + " to " + mBaseName + ", then copy over files " + aBaseName + " and " + zBaseName;

                        if (this.ConsoleMode)
                        {
                            Console.WriteLine(prompt);
                        }
                        else
                        {
                            var dialogResult = MessageBox.Show(prompt, Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                            if (dialogResult == DialogResult.Cancel)
                            {
                                return false;
                            }
                        }
                    }
                }

                loadedArchiveFiles.CreatePatchAld(mFileLetter, zFileLetter);
                return true;
                //LoadTreeView(true);
            }
            else
            {
                string fileName = PromptForArchiveOutputFileName();
                if (!String.IsNullOrEmpty(fileName))
                {
                    if (Path.GetFullPath(fileName).Equals(Path.GetFullPath(loadedArchiveFiles.ArchiveFileName)))
                    {
                        RaiseError("Cannot replace original file with a patch.");
                        return false;
                    }
                    loadedArchiveFiles.CreatePatch(fileName);
                    return true;
                }
            }
            return false;
        }
    }
}
