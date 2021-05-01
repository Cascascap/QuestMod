using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ALDExplorer2
{
    class TempFileManager
    {
        List<string> myTempFiles = new List<string>();

        public static bool DefaultInstanceCreated
        {
            get
            {
                return _defaultInstance != null;
            }
        }

        static TempFileManager _defaultInstance = null;
        public static TempFileManager DefaultInstance
        {
            get
            {
                if (_defaultInstance == null)
                {
                    _defaultInstance = new TempFileManager();
                }
                return _defaultInstance;
            }
        }

        public TempFileManager()
        {
            Create();

        }

        static int GetLastNumberInString(string fileName, out int numberIndex, out int numberLength)
        {
            numberIndex = fileName.Length;
            numberLength = 0;
            for (int i = fileName.Length - 1; i >= 0; i--)
            {
                char c = fileName[i];
                if (c >= '0' && c <= '9')
                {
                    numberIndex = i;
                    numberLength++;
                }
                else
                {
                    if (numberLength > 0)
                    {
                        break;
                    }
                }
            }

            int number = 0;
            if (numberIndex >= 0)
            {
                string substr = fileName.Substring(numberIndex, numberLength);
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

        static int GetLongestNumberFromString(string fileName, out int numberIndex, out int numberLength)
        {
            int maxIndex = -1;
            int maxLength = 0;

            int currentIndex = -1;
            int currentLength = 0;
            //find the longest number in the filename
            for (int i = 0; i < fileName.Length; i++)
            {
                char c = fileName[i];
                if (c >= '0' && c <= '9')
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
            numberIndex = maxIndex;
            numberLength = maxLength;
            if (numberIndex == -1)
            {
                numberIndex = fileName.Length;
            }
            return number;
        }

        static string IncrementFileName(string fileName)
        {
            int number;
            int numberIndex;
            int numberLength;
            number = GetLastNumberInString(fileName, out numberIndex, out numberLength);
            number++;

            string beforeNumber = fileName.Substring(0, numberIndex);
            string afterNumber = fileName.Substring(numberIndex + numberLength);
            string newNumber = number.ToString("".PadLeft(numberLength, '0'));
            return beforeNumber + newNumber + afterNumber;
        }

        public void Destroy()
        {
            this.DeleteMyTempFiles();
            if (!AnyOtherInstancesRunning())
            {
                Directory.Delete(this.TempDirectory, true);
            }
        }

        public string TempDirectory;

        private void Create()
        {
            string appName = Application.ProductName;
            string tempDirectory = Path.GetTempPath();
            string appTempDirectory = Path.Combine(tempDirectory, appName);
            if (File.Exists(appTempDirectory))
            {
                appName = appName + ".000";
                do
                {
                    appName = IncrementFileName(appName);
                    appTempDirectory = Path.Combine(tempDirectory, appName);
                } while (File.Exists(appTempDirectory));
            }
            if (!Directory.Exists(appTempDirectory))
            {
                Directory.CreateDirectory(appTempDirectory);
            }
            this.TempDirectory = appTempDirectory;

            bool anyOtherRunning = AnyOtherInstancesRunning();
            if (!anyOtherRunning)
            {
                if (Directory.GetFiles(appTempDirectory, "*", SearchOption.AllDirectories).Length > 0)
                {
                    try
                    {
                        Directory.Delete(appTempDirectory, true);
                        Directory.CreateDirectory(appTempDirectory);
                    }
                    catch (IOException ex)
                    {

                    }
                }
            }
        }

        public string CreateTempFile(string fileName)
        {
            string newFileName = GetNewFileName(fileName);
            File.WriteAllBytes(newFileName, new byte[0]);
            this.myTempFiles.Add(newFileName);
            return newFileName;
        }

        //public void CreateTempFile2(string fileName, out FileStream stream, out string outputFileName)
        //{
        //    outputFileName = GetNewFileName(fileName);
        //    //stream = CreateTempFileStream(outputFileName);
        //    stream = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
        //}

        //[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        //private static extern Microsoft.Win32.SafeHandles.SafeFileHandle CreateFile(string lpFileName, System.UInt32 dwDesiredAccess, System.UInt32 dwShareMode, IntPtr pSecurityAttributes, System.UInt32 dwCreationDisposition, System.UInt32 dwFlagsAndAttributes, IntPtr hTemplateFile);

        //public static FileStream CreateTempFileStream(string fileName)
        //{
        //    var stream2 = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, FileOptions.DeleteOnClose);
        //    return stream2;

            
        //    var handle = CreateFile(fileName, 0xC0000000, 0x07, (IntPtr)0, 1, 0x04000100, (IntPtr)0);
        //    var stream = new FileStream(handle, FileAccess.ReadWrite);
            
        //    try
        //    {
        //        byte[] test = File.ReadAllBytes(fileName);
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //    return stream;
        //}

        private string GetNewFileName(string fileName)
        {
            string newBaseName = Path.GetFileName(fileName);
            newBaseName = ValidCharsOnly(newBaseName);
            if (newBaseName.Length == 0)
            {
                newBaseName = "temp";
            }
            string newFileName = Path.Combine(this.TempDirectory, newBaseName);
            if (File.Exists(newFileName))
            {
                newBaseName = Path.GetFileNameWithoutExtension(newBaseName);
                newBaseName = newBaseName + "_1";
                newBaseName = Path.ChangeExtension(newBaseName, Path.GetExtension(fileName));
                newFileName = Path.Combine(this.TempDirectory, newBaseName);
                while (File.Exists(newFileName))
                {
                    newBaseName = IncrementFileName(newBaseName);
                    newFileName = Path.Combine(this.TempDirectory, newBaseName);
                }
            }
            return newFileName;
        }

        static HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

        private static string ValidCharsOnly(string newBaseName)
        {
            StringBuilder sb = new StringBuilder(newBaseName.Length);
            foreach (var c in newBaseName)
            {
                if (!invalidChars.Contains(c))
                {
                    sb.Append(c);
                }
            }
            string fileName = sb.ToString();
            return sb.ToString();
        }

        public void DeleteMyTempFiles()
        {
            if (this.myTempFiles.Count > 0)
            {
                foreach (var fileName in this.myTempFiles)
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                }
                this.myTempFiles.Clear();
            }
        }

        private static bool AnyOtherInstancesRunning()
        {
            //any other instances running?
            int myPid;
            string myProcessName;
            using (var myProcess = Process.GetCurrentProcess())
            {
                myPid = myProcess.Id;
                myProcessName = myProcess.ProcessName;
            }
            bool anyOtherRunning = false;
            using (var processes = new DisposableCollection<Process>(Process.GetProcessesByName(myProcessName)))
            {
                if (processes.Count > 1)
                {
                    anyOtherRunning = true;
                }
            }
            return anyOtherRunning;
        }
    }
}
