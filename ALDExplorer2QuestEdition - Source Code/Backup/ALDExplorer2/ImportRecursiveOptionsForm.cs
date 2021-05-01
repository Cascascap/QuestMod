using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ALDExplorer2
{
    public partial class ImportRecursiveOptionsForm : Form
    {
        public bool KeepDirectoryNames;
        public string ImportPath;

        public static string[] GetFiles(string path, ref bool keepDirectoryNames)
        {
            using (var form = new ImportRecursiveOptionsForm(path, keepDirectoryNames))
            {
                var dialogResult = form.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    keepDirectoryNames = form.KeepDirectoryNames;
                    return form.GetFileNames();
                }
                else
                {
                    return new string[0];
                }
            }
        }

        public ImportRecursiveOptionsForm()
        {
            InitializeComponent();
        }

        public ImportRecursiveOptionsForm(string path, bool keepDirectoryNames)
            : this()
        {
            this.ImportPath = path;
            this.KeepDirectoryNames = keepDirectoryNames;
            this.checkBox1.Checked = keepDirectoryNames;
            UpdateList();
        }

        static string RemovePathPrefix(string pathPrefixUpper, string path)
        {
            if (path.ToUpperInvariant().StartsWith(pathPrefixUpper))
            {
                return path.Substring(pathPrefixUpper.Length).TrimStart('/', '\\');
            }
            return path;
        }

        private void UpdateList()
        {
            int oldCount;
            string[] fileNames = GetFileNames(out oldCount);

            string importPathUpper = Path.GetFullPath(this.ImportPath).ToUpperInvariant();

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (var fileName in fileNames)
            {
                string displayPath = Path.GetFileName(fileName);

                if (KeepDirectoryNames)
                {
                    displayPath = RemovePathPrefix(importPathUpper, Path.GetFullPath(fileName));
                }
                listBox1.Items.Add(displayPath);
            }
            listBox1.EndUpdate();

            if (oldCount == fileNames.Length)
            {
                this.label1.Text = "Will Import " + fileNames.Length.ToString() + " files.";
            }
            else
            {
                this.label1.Text = "Will Import " + fileNames.Length.ToString() + " files.  " +
                    "Removed " + (oldCount - fileNames.Length).ToString() + " duplicate named files, only latest-modified files are kept.";
            }
        }

        //string[] OnlyNewestFiles(string[] fileNames)
        //{
        //    HashSet<string> BaseNamesSeen = new HashSet<string>();
        //    HashSet<string> ConflictingNames = new HashSet<string>();

        //    foreach (var fileName in fileNames)
        //    {
        //        string baseName = Path.GetFileName(fileName).ToUpperInvariant();
        //        if (BaseNamesSeen.Contains(baseName))
        //        {
        //            ConflictingNames.Add(baseName);
        //        }
        //        else
        //        {
        //            BaseNamesSeen.Add(baseName);
        //        }
        //    }

        //    List<string> Output = new List<string>();
        //    Dictionary<string, List<string>> ConflictingFiles = new Dictionary<string, List<string>>();
        //    foreach (var fileName in fileNames)
        //    {
        //        string baseName = Path.GetFileName(fileName).ToUpperInvariant();
        //        if (ConflictingNames.Contains(baseName))
        //        {
        //            ConflictingFiles.GetOrAddNew(baseName).Add(fileName);
        //        }
        //        else
        //        {
        //            Output.Add(fileName);
        //        }
        //    }

        //    foreach (var pair in ConflictingFiles)
        //    {
        //        string baseName = pair.Key;
        //        List<string> names = pair.Value;

        //        string latestFileName = names.OrderByDescending(fileName => new FileInfo(fileName).LastWriteTimeUtc).FirstOrDefault();
        //        Output.Add(latestFileName);
        //    }

        //    return Output.OrderBy(f => Path.GetFileName(f)).ToArray();
        //}

        public string[] GetFileNames(out int OldCount)
        {
            string[] fileNames = Directory.GetFiles(this.ImportPath, "*.*", SearchOption.AllDirectories);
            //.Concat(Directory.GetFiles(this.ImportPath, "*.swf", SearchOption.AllDirectories)).ToArray();
            OldCount = fileNames.Length;

            if (this.KeepDirectoryNames == false)
            {
                return FileOperations.OnlyNewestFiles(fileNames);
            }

            return fileNames;
        }

        public string[] GetFileNames()
        {
            int dummy;
            return GetFileNames(out dummy);
        }

        private void ImportRecursiveOptionsForm_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.KeepDirectoryNames = checkBox1.Checked;
            UpdateList();
        }
    }
}
