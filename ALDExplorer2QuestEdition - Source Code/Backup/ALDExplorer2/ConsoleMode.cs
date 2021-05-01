using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace ALDExplorer2
{
    class ConsoleMode
    {
        bool keepDirectories = false;
        bool noConvert = false;
        string filePrefix = "";
        DateTime minDate = DateTime.MinValue;
        string archiveFileName = "";
        string newImageExtension = "";
        bool addNewFiles = false;
        int version = 1;
        bool createNew = false;
        bool buildPatch = false;
        bool extract = false;
        bool doNotExtractDirectories = false;
        List<string> fileNames = new List<string>();

        public void RunConsoleMode()
        {
            ConsoleTools.CreateOrAttachConsole();
            ParseCommandLineArguments();

            if (String.IsNullOrEmpty(archiveFileName))
            {
                return;
            }

            archiveFileName = Path.GetFullPath(archiveFileName);
            string path = Path.GetDirectoryName(archiveFileName);
            if (!Directory.Exists(path))
            {
                Console.Error.WriteLine("Archive Directory " + path + " does not exist.");
                return;
            }
            if (Directory.Exists(archiveFileName))
            {
                Console.Error.WriteLine("Archive file name is a Directory: " + archiveFileName);
                return;
            }
            if (Path.GetFileName(archiveFileName).Length == 0)
            {
                Console.Error.WriteLine("No filename for archive file.");
                return;
            }
            //TODO: validate path
            //if (xxxx)           
            //{
            //    Console.Error.WriteLine("Archive file name is invalid: " + archiveFileName);
            //    return;
            //}

            if (!createNew && !extract && !File.Exists(archiveFileName))
            {
                createNew = true;
            }

            var fileOperations = new FileOperations();
            fileOperations.ConsoleMode = true;
            fileOperations.DuplicateFileNamesAllowed = false;
            fileOperations.DoNotConvertImageFiles = noConvert;
            fileOperations.consoleModeArguments.InputArchiveFileName = archiveFileName;
            fileOperations.consoleModeArguments.ImportFilePrefix = filePrefix;
            fileOperations.consoleModeArguments.Version = version;
            fileOperations.consoleModeArguments.minDate = minDate;
            fileOperations.consoleModeArguments.KeepDirectoryNamesWhenImporting = keepDirectories;
            fileOperations.consoleModeArguments.OutputArchiveFileName = archiveFileName;
            fileOperations.consoleModeArguments.NewImageExtension = newImageExtension;
            fileOperations.IncludeDirectoriesWhenExportingFiles = !doNotExtractDirectories;

            if (createNew)
            {
                if (fileNames.Count == 0)
                {
                    Console.Error.WriteLine("No files or directories to add.");
                    return;
                }

                fileOperations.NewFile(archiveFileName, version);

                //parse filenames for files or directories to import
                foreach (var inputFileName in fileNames)
                {
                    string directory;
                    string fileName;
                    string filter;
                    ParseFileName(inputFileName, out directory, out fileName, out filter);
                    if (!String.IsNullOrEmpty(fileName))
                    {
                        fileOperations.ImportNewFiles(null, new string[] { fileName }, null, null, directory, false);
                    }
                    else
                    {
                        fileOperations.consoleModeArguments.ImportFileFilter = filter;
                        fileOperations.consoleModeArguments.ImportDirectory = directory;
                        fileOperations.ImportNewFiles();
                    }
                }
                fileOperations.Save();
            }
            else if (buildPatch)
            {
                bool okay;
                okay = fileOperations.OpenFile();
                if (!okay) { return; }
                string archiveExt = Path.GetExtension(archiveFileName).ToLowerInvariant();

                string outputArchiveFileName = archiveFileName;
                foreach (var inputFileName in fileNames)
                {
                    string directory;
                    string fileName;
                    string filter;
                    ParseFileName(inputFileName, out directory, out fileName, out filter);
                    string ext = Path.GetExtension(fileName).ToLowerInvariant();
                    if (ext == archiveExt)
                    {
                        outputArchiveFileName = fileName;
                    }
                    else
                    {
                        ImportOrAddFiles(addNewFiles, fileOperations, directory, fileName, filter);
                    }
                }

                fileOperations.SavePatch();
            }
            else if (extract)
            {
                bool okay;
                okay = fileOperations.OpenFile();
                if (!okay) { return; }

                string outputDirectoryName = "";
                string filter = "";

                if (fileNames.Count == 0)
                {
                    //extract to directory named after archive file
                    outputDirectoryName = Path.GetFileNameWithoutExtension(archiveFileName);
                }

                foreach (var inputFileName in fileNames)
                {
                    string directory;
                    string fileName;
                    string thisFilter;
                    ParseFileName(inputFileName, out directory, out fileName, out thisFilter);
                    fileOperations.consoleModeArguments.ExportDirectory = directory;
                    if (fileNames.Count == 1 && !String.IsNullOrEmpty(fileName) && String.IsNullOrEmpty(Path.GetExtension(fileName)))
                    {
                        outputDirectoryName = Path.Combine(directory, fileName);
                        filter = thisFilter;
                        break;
                    }
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        //look for files to export
                        fileOperations.ExportFile(fileName, thisFilter);
                    }
                    else
                    {
                        outputDirectoryName = directory;
                        filter = thisFilter;
                    }
                }

                if (!String.IsNullOrEmpty(outputDirectoryName))
                {
                    fileOperations.consoleModeArguments.ExportDirectory = outputDirectoryName;
                    fileOperations.ExportAll(filter);
                }
            }
            else //update or add new files
            {
                bool okay;
                okay = fileOperations.OpenFile();
                if (!okay) { return; }
                foreach (var inputFileName in fileNames)
                {
                    string directory;
                    string fileName;
                    string filter;
                    ParseFileName(inputFileName, out directory, out fileName, out filter);
                    ImportOrAddFiles(addNewFiles, fileOperations, directory, fileName, filter);
                }
                fileOperations.SaveAs();
            }
            return;
        }

        private static bool FileOrDirectoryExists(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return false;
            }
            try
            {
                if (File.Exists(path))
                {
                    return true;
                }
                if (Directory.Exists(path))
                {
                    return true;
                }
                string pathDirectory = "";
                try
                {
                    pathDirectory = Path.GetDirectoryName(path);
                }
                catch
                {

                }
                if (!String.IsNullOrEmpty(pathDirectory))
                {
                    if (Directory.Exists(pathDirectory))
                    {
                        //directory name must contain non-ascii characters
                        bool containsNonAscii = false;
                        foreach (char c in pathDirectory)
                        {
                            if (c >= 128)
                            {
                                containsNonAscii = true;
                                break;
                            }
                        }

                        if (containsNonAscii)
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private static string FixMojibake(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return path;
            }
            bool pathIsAscii = true;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] >= 128)
                {
                    pathIsAscii = false;
                    break;
                }
            }
            if (pathIsAscii)
            {
                return path;
            }

            var defaultEncodingWithThrow = Encoding.GetEncoding(0, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            var shiftJisWithThrow = Encoding.GetEncoding(932, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

            bool IsInSystemCodePage = false;
            bool IsInJapanese = false;
            try
            {
                byte[] shiftJisBytes = shiftJisWithThrow.GetBytes(path);
                string shiftJisPath = shiftJisWithThrow.GetString(shiftJisBytes);
                if (path == shiftJisPath)
                {
                    IsInJapanese = true;
                }
            }
            catch
            {

            }

            string decodedPath = null;
            byte[] bytes = null;
            if (defaultEncodingWithThrow.CodePage != 932)
            {
                try
                {
                    bytes = defaultEncodingWithThrow.GetBytes(path);
                    IsInSystemCodePage = true;
                    decodedPath = shiftJisWithThrow.GetString(bytes);
                }
                catch
                {

                }
            }

            if (!IsInJapanese && !String.IsNullOrEmpty(decodedPath))
            {
                if (FileOrDirectoryExists(decodedPath))
                {
                    return decodedPath;
                }
                if (FileOrDirectoryExists(path))
                {
                    return path;
                }
                return decodedPath;
            }

            if (IsInJapanese && String.IsNullOrEmpty(decodedPath))
            {
                return path;
            }

            if (FileOrDirectoryExists(path))
            {
                return path;
            }
            
            if (!string.IsNullOrEmpty(decodedPath))
            {
                return decodedPath;
            }
            return path;
        }
        
        private void ParseCommandLineArguments()
        {
            var args = Environment.GetCommandLineArgs();
            //ALDExplorer2 --help
            //ALDExplorer2 [--version 2] --build <file.ext> [--prefix <PrefixName>] [--keep-directories] <files/dirs...>
            //ALDExplorer2 [--update] <file.ext> <files/dirs>
            //ALDExplorer2 --extract <file.ext> <outputdir>
            //ALDExplorer2 --build-patch <file.ext> [--prefix <PrefixName>] <files/dirs...>

            createNew = false;
            buildPatch = false;
            extract = false;
            version = 1;
            addNewFiles = false;
            newImageExtension = "";
            archiveFileName = "";
            minDate = DateTime.MinValue;
            filePrefix = "";
            noConvert = false;
            keepDirectories = false;

            for (int argIndex = 1; argIndex < args.Length; argIndex++)
            {
                string arg = args[argIndex];
                string argLower = arg.ToLowerInvariant();
                switch (argLower)
                {
                    case "--help":
                        goto InvalidArgs;
                        break;
                    case "--build":
                    case "--create":
                    case "--create-new":
                    case "--createnew":
                        createNew = true;
                        break;
                    case "--build-patch":
                    case "--buildpatch":
                    case "--patch":
                        buildPatch = true;
                        break;
                    case "--extract":
                    case "--extract-files":
                    case "--extractfiles":
                    case "--export":
                    case "--export-files":
                    case "--exportfiles":
                        extract = true;
                        break;
                    case "--add-new":
                    case "--addnew":
                    case "--add":
                    case "--add-new-files":
                    case "--addnewfiles":
                    case "--allow-add-new":
                    case "--allow-add":
                    case "--allowaddnew":
                    case "--allow-add-new-files":
                    case "--allowaddnewfiles":
                        addNewFiles = true;
                        break;
                    case "--update":
                    case "--update-files":
                    case "--updatefiles":
                        //do nothing
                        break;
                    case "--version":
                        {
                            argIndex++;
                            arg = args.GetOrNull(argIndex);
                            if (arg != null && Int32.TryParse(arg, out version))
                            {
                                //accept version
                            }
                            else
                            {
                                goto InvalidArgs;
                            }
                        }
                        break;
                    case "--afa2":
                    case "--vfs2":
                    case "--vfs20":
                        version = 2;
                        break;
                    case "--no-convert":
                    case "--noconvert":
                    case "--do-not-convert":
                    case "--donotconvert":
                    case "--do-not-convert-images":
                    case "--donotconvertimages":
                        noConvert = true;
                        break;
                    case "--min-date":
                    case "--mindate":
                    case "--minimum-date":
                    case "--minimumdate":
                        {
                            argIndex++;
                            arg = args.GetOrNull(argIndex);
                            if (arg != null && DateTime.TryParse(arg, out minDate))
                            {
                                //accept new minDate
                            }
                            else
                            {
                                goto InvalidArgs;
                            }
                        }
                        break;
                    case "--new-extension":
                    case "--newextension":
                    case "--new-ext":
                    case "--newext":
                    case "--extension":
                    case "--ext":
                        {
                            argIndex++;
                            arg = args.GetOrNull(argIndex);
                            if (arg != null)
                            {
                                newImageExtension = arg.ToLowerInvariant();
                                if (newImageExtension.StartsWith("*"))
                                {
                                    newImageExtension = newImageExtension.Substring(1);
                                }
                                if (!newImageExtension.StartsWith("."))
                                {
                                    newImageExtension = "." + newImageExtension.ToLowerInvariant();
                                }
                                //Uri dummy;
                                //if (!Uri.TryCreate(newImageExtension, UriKind.RelativeOrAbsolute, out dummy))
                                //{
                                //    goto InvalidArgs;
                                //}
                            }
                            else
                            {
                                goto InvalidArgs;
                            }
                        }
                        break;
                    case "--prefix":
                    case "--file-prefix":
                    case "--fileprefix":
                        {
                            argIndex++;
                            arg = args.GetOrNull(argIndex);
                            if (arg != null)
                            {
                                filePrefix = FixMojibake(arg);
                            }
                            else
                            {
                                goto InvalidArgs;
                            }
                        }
                        break;
                    case "--keep-directories":
                    case "--keepdirectories":
                        keepDirectories = true;
                        break;
                    case "--no-extract-directories":
                    case "--do-not-extract-directories":
                    case "--dont-extract-directories":
                    case "--noextractdirectories":
                    case "--donotextractdirectories":
                    case "--dontextractdirectories":
                        doNotExtractDirectories = true;
                        break;
                    default:
                        {
                            //Uri dummy;
                            //if (!Uri.TryCreate(newImageExtension, UriKind.RelativeOrAbsolute, out dummy))
                            //{
                            //    goto InvalidArgs;
                            //}
                            arg = FixMojibake(arg);
                            if (String.IsNullOrEmpty(archiveFileName))
                            {
                                archiveFileName = arg;
                            }
                            else
                            {
                                fileNames.Add(arg);
                            }
                        }
                        break;
                }
            }
            if (archiveFileName == "") goto InvalidArgs;
            return;
        InvalidArgs:
            ShowHelp();
            return;
        }

        private static void ShowHelp()
        {
            //80 column text mode: ("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            Console.Error.WriteLine("ALD Explorer 2 - extracts, replaces, or builds ALD, AFA, ALK, DAT, VFS");
            Console.Error.WriteLine("                 or ARC files");
            Console.Error.WriteLine("Syntax:");
            Console.Error.WriteLine("--help : shows this help");
            Console.Error.WriteLine("<filename.ext> : opens the file");
            Console.Error.WriteLine("--extract <filename.ext> <outputdirectory> : extracts the contents of the");
            Console.Error.WriteLine("          archive file to outputdirectory");
            Console.Error.WriteLine("--build <filename.ext> [--prefix <prefix>] [--keep-directories]");
            Console.Error.WriteLine("        <files/directories> : builds a new archive file from the selected files");
            Console.Error.WriteLine("        Supports ALD/AFA/ALK/DAT/VFS/ARC files");
            Console.Error.WriteLine("--update <filename.ext> <files/directories> : Replaces files inside the archive");
            Console.Error.WriteLine("         that have matching names with the selected files");
            Console.Error.WriteLine("--build-patch <filename.ald> <files/directories> : Replaces files then saves as");
            Console.Error.WriteLine("              a patch (ALD files only)");
            Console.Error.WriteLine();
            Console.Error.WriteLine("  --add-new : allows adding new files when using update or build-patch");
            Console.Error.WriteLine("  --prefix : puts something at the beginning of filenames ");
            Console.Error.WriteLine("             (such as directory names for AFA files)");
            Console.Error.WriteLine("  --keep-directories : If importing an entire directory, keep subdirectory");
            Console.Error.WriteLine("                        names as part of the filenames (for AFA files)");
            Console.Error.WriteLine("  --version 2 : Create new archive files as version 2 (for AFA and VFS files)");
            Console.Error.WriteLine("  --no-convert : Do not perform any image file conversion");
            Console.Error.WriteLine("  --min-date <mm-dd-yyyy> : Only include files that were modified later than");
            Console.Error.WriteLine("                            min-date");
            Console.Error.WriteLine("  --ext <.ext> : When importing new files, use this file extension for images");
            Console.Error.WriteLine("  --do-not-extract-directories : When extracting files from AFA archives,");
            Console.Error.WriteLine("                                 do not include directoy names");
        }

        private static void ImportOrAddFiles(bool addNewFiles, FileOperations fileOperations, string directory, string fileName, string filter)
        {
            if (addNewFiles)
            {
                if (!String.IsNullOrEmpty(fileName) && String.IsNullOrEmpty(filter))
                {
                    fileOperations.ImportNewFiles(null, new string[] { fileName }, null, null, directory, false);
                }
                else
                {
                    fileOperations.consoleModeArguments.ImportFileFilter = fileName + filter;
                    fileOperations.consoleModeArguments.ImportDirectory = directory;
                    fileOperations.ImportNewFiles();
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(fileName))
                {
                    fileOperations.TryImportFile(Path.Combine(directory, fileName));
                }
                else
                {
                    fileOperations.consoleModeArguments.ImportFileFilter = filter;
                    fileOperations.consoleModeArguments.ImportDirectory = directory;
                    fileOperations.ImportAll();
                }
            }
        }

        private static void ParseFileName(string inputFileName, out string directory, out string fileName, out string filter)
        {
            directory = "";
            fileName = "";
            filter = "";
            if (Directory.Exists(inputFileName))
            {
                directory = inputFileName;
                fileName = "";
            }
            else
            {
                directory = Path.GetDirectoryName(inputFileName);
                fileName = Path.GetFileName(inputFileName);
            }
            directory = Path.GetFullPath(directory);
            if (fileName.Contains('*') || fileName.Contains('?'))
            {
                filter = fileName;
                fileName = "";
            }
        }
    }
}
