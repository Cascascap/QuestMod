using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;
using FreeImageAPI;
using System.Diagnostics;
using WMPLib;
using DDW.Swf;

namespace ALDExplorer2
{
    using ImageFileFormats;

    /// <summary>
    /// The main form which contains a file explorer
    /// </summary>
    public partial class ExplorerForm : Form
    {
        /// <summary>
        /// The set of loaded archive files, must stay in sync with fileOperations.loadedArchiveFiles
        /// </summary>
        ArchiveFileCollection loadedArchiveFiles = null;
        /// <summary>
        /// The file operations object, fileOperations.loadedArchiveFiles must stay in sync with this.loadedArchiveFiles
        /// </summary>
        FileOperations fileOperations = new FileOperations();

        /// <summary>
        /// Creates a new ExplorerForm
        /// </summary>
        public ExplorerForm()
        {
            InitializeComponent();
        }
        
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private bool OpenFile()
        {
            if (fileOperations.OpenFile())
            {
                this.loadedArchiveFiles = fileOperations.loadedArchiveFiles;
                LoadTreeView();
                return true;
            }
            return false;
        }

        private bool OpenFile(string fileName)
        {
            if (fileOperations.OpenFile(fileName))
            {
                this.loadedArchiveFiles = fileOperations.loadedArchiveFiles;
                LoadTreeView();
                return true;
            }
            return false;
        }

        void LoadTreeView()
        {
            LoadTreeView(false);
        }

        private void LoadTreeView(bool preserveItems)
        {
            if (loadedArchiveFiles == null) return;

            try
            {
                listView1.BeginUpdate();

                HideFlashPlayer();
                HideMediaPlayer();
                int firstVisibleIndex = 0;
                int focusedIndex = -1;

                HashSet<ArchiveFileEntry> selectedEntries = new HashSet<ArchiveFileEntry>();
                HashSet<ArchiveFileEntry> expandedEntries = new HashSet<ArchiveFileEntry>();

                if (preserveItems)
                {
                    var firstVisibleItem = listView1.TopItem;
                    if (firstVisibleItem != null)
                    {
                        firstVisibleIndex = firstVisibleItem.Index;
                    }
                    //note focused index
                    var focusedItem = listView1.FocusedItem;
                    if (focusedItem != null)
                    {
                        focusedIndex = focusedItem.Index;
                    }

                    //remember expanded items and selected items
                    expandedEntries.AddRange(GetExpandedItems());
                    selectedEntries.AddRange(GetSelectedEntries());
                }


                var itemCollection = listView1.Items;
                listView1.Items.Clear();
                //treeView1.BeginUpdate();
                //treeView1.Nodes.Clear();
                int lastFileLetter = -1;

                if (loadedArchiveFiles.FileEntries.Count == 0)
                {
                    var archiveFile = loadedArchiveFiles.ArchiveFiles.FirstOrDefault();
                    //add new node
                    var newNode2 = new ListViewItem();
                    if (archiveFile != null)
                    {
                        newNode2.Text = Path.GetFileName(archiveFile.ArchiveFileName);
                    }
                    else
                    {
                        newNode2.Text = "Invalid Archive File";
                    }
                    newNode2.Tag = archiveFile;
                    newNode2.ImageIndex = 1;
                    itemCollection.Add(newNode2);
                }

                foreach (var entry in loadedArchiveFiles.FileEntries)
                {
                    if (entry.FileLetter != lastFileLetter)
                    {
                        lastFileLetter = entry.FileLetter;
                        var archiveFile = loadedArchiveFiles.GetArchiveFileByLetter(lastFileLetter);

                        //add new node
                        var newNode2 = new ListViewItem();
                        if (archiveFile != null)
                        {
                            newNode2.Text = Path.GetFileName(archiveFile.ArchiveFileName);
                        }
                        else
                        {
                            newNode2.Text = "Invalid Archive File";
                        }
                        newNode2.Tag = archiveFile;
                        newNode2.ImageIndex = 1;
                        itemCollection.Add(newNode2);
                    }
                    //var newNode = new TreeNode();
                    var newNode = new ListViewItem();
                    if (entry.FileName == null)
                    {
                        entry.FileName = "CORRUPT FILE " + entry.FileNumber.ToString();
                    }
                    newNode.Text = entry.FileName;
                    newNode.Tag = entry;
                    newNode.SetFileIndentCount();

                    string ext = Path.GetExtension(entry.FileName).ToLowerInvariant();
                    int imageIndex = GetImageIndex(ext);
                    newNode.ImageIndex = imageIndex;
                    //if (Debugger.IsAttached)
                    {
                        newNode.ToolTipText = "File Number: " + (entry.FileNumber) + /*", File Type: " + entry.FileType + */ ", File Size: " + entry.FileSize + ", File Address: " + entry.FileAddress.ToString("X");
                    }
                    itemCollection.Add(newNode);
                }
                listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                if (preserveItems)
                {
                    if (firstVisibleIndex >= 0 && firstVisibleIndex < listView1.Items.Count)
                    {
                        var lastItem = listView1.Items[listView1.Items.Count - 1];
                        lastItem.EnsureVisible();
                        var newTopItem = listView1.Items[firstVisibleIndex];
                        newTopItem.EnsureVisible();
                    }
                    if (focusedIndex >= 0 && focusedIndex < listView1.Items.Count)
                    {
                        var listItem = listView1.Items[focusedIndex];
                        listItem.Focused = true;
                    }
                    ExpandItems(expandedEntries);
                    listView1.SelectedIndices.Clear();
                    SelectItems(selectedEntries);
                }
            }
            finally
            {
                listView1.EndUpdate();
            }
        }

        private void SelectItems(IEnumerable<ArchiveFileEntry> selectedEntriesSequence)
        {
            HashSet<ArchiveFileEntry> selectedEntries = new HashSet<ArchiveFileEntry>(selectedEntriesSequence);
            foreach (ListViewItem item in listView1.Items)
            {
                var entry = item.Tag as ArchiveFileEntry;
                if (entry != null && selectedEntries.Contains(entry))
                {
                    item.Selected = true;
                }
            }
        }

        private void ExpandItems(IEnumerable<ArchiveFileEntry> expandedEntriesSequence)
        {
            HashSet<ArchiveFileEntry> expandedEntries = new HashSet<ArchiveFileEntry>(expandedEntriesSequence);
            foreach (ListViewItem item in listView1.Items)
            {
                var entry = item.Tag as ArchiveFileEntry;
                if (entry != null && expandedEntries.Contains(entry))
                {
                    TryExpandItem(item.Index);
                }
            }
        }

        private int[] GetExpandedIndices()
        {
            return listView1.Items.OfType<ListViewItem>().Where(i => i.IsFileNode() && i.NextNodeIsSubFileNode()).Select(i => i.Index).ToArray();
        }

        private ArchiveFileEntry[] GetExpandedItems()
        {
            return GetExpandedIndices().Select(i => listView1.Items[i].Tag as ArchiveFileEntry).ToArray();
        }

        private static int GetImageIndex(string ext)
        {
            int imageIndex = 0;
            if (ext == ".vsp" || ext == ".pms" || ext == ".jpg" || ext == ".ajp" || ext == ".bmp" || ext == ".qnt" || ext == ".dcf" || ext == ".png" || ext == ".iph" || ext == ".agf" || ext == ".tif" || ext == ".tga")
            {
                imageIndex = 2;
            }
            else if (ext == ".mp3" || ext == ".wav" || ext == ".ogg" || ext == ".mid" || ext == ".aog")
            {
                imageIndex = 3;
            }
            else if (ext == ".swf" || ext == ".aff")
            {
                imageIndex = 6;
            }
            else if (ext == ".sco")
            {
                imageIndex = 4;
            }
            else if (ext == ".flat")
            {
                imageIndex = 7;
            }
            else
            {
                imageIndex = 0;
            }
            return imageIndex;
        }

        AxWMPLib.AxWindowsMediaPlayer axMediaPlayer = null;
        AxShockwaveFlashObjects.AxShockwaveFlash axFlashPlayer = null;
        bool mediaPlayerCreated = false;
        bool flashPlayerCreated = false;

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var entry = e.Node.Tag as ArchiveFileEntry;
            DisplayEntry(entry);
        }

        private void DisplayEntry(ArchiveFileEntry entry)
        {
            if (entry != null)
            {
                FreeImageBitmap bitmap = null;
                string extension = Path.GetExtension(entry.FileName).ToLowerInvariant();
                using (bitmap = fileOperations.GetBitmapFromFile(entry))
                {
                    if (false)
                    {
                        bitmap = DebugResave(bitmap, extension);
                    }

                    if (extension == ".mid" || extension == ".mp3" || extension == ".ogg" || extension == ".wav" || extension == ".aog")
                    {
                        CreateMediaPlayer(entry);
                    }

                    if (extension == ".swf" || extension == ".aff")
                    {
                        CreateFlashPlayer(entry);
                    }

                    if (bitmap != null)
                    {
                        HideMediaPlayer();
                        HideFlashPlayer();
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                            pictureBox1.Image = null;
                        }
                        pictureBox1.Image = bitmap.ToBitmap();
                        pictureBox1.Visible = true;
                    }
                }
            }
        }

        private static FreeImageBitmap DebugResave(FreeImageBitmap bitmap, string extension)
        {
            if (Debugger.IsAttached && bitmap != null)
            {
                //long originalSize = entry.FileSize;
                var ms = new MemoryStream();
                switch (extension)
                {
                    case ".ajp":
                        ImageConverter.SaveAjp(ms, bitmap);
                        bitmap.Dispose();
                        bitmap = ImageConverter.LoadAjp(ms.ToArray());
                        break;
                    case ".vsp":
                        ImageConverter.SaveVsp(ms, bitmap);
                        bitmap.Dispose();
                        bitmap = ImageConverter.LoadVsp(ms.ToArray());
                        break;
                    case ".pms":
                        ImageConverter.SavePms(ms, bitmap);
                        bitmap.Dispose();
                        bitmap = ImageConverter.LoadPms(ms.ToArray());
                        break;
                    case ".qnt":
                        ImageConverter.SaveQnt(ms, bitmap);
                        bitmap.Dispose();
                        bitmap = ImageConverter.LoadQnt(ms.ToArray());
                        break;
                    //case ".dcf":
                    //    ImageConverter.SaveDcf(ms, bitmap);
                    //    bitmap.Dispose();
                    //    bitmap = ImageConverter.LoadDcf(ms.ToArray());
                    //    break;
                    case ".iph":
                        ImageConverter.SaveIph(ms, bitmap);
                        bitmap.Dispose();
                        bitmap = ImageConverter.LoadIph(ms.ToArray());
                        break;
                    case ".agf":
                        ImageConverter.SaveAgf(ms, bitmap);
                        bitmap.Dispose();
                        bitmap = ImageConverter.LoadAgf(ms.ToArray());
                        break;
                }
            }
            return bitmap;
        }

        private void CreateFlashPlayer(ArchiveFileEntry entry)
        {
            if (axFlashPlayer == null)
            {
                if (!flashPlayerCreated)
                {
                    flashPlayerCreated = true;
                    try
                    {
                        axFlashPlayer = new AxShockwaveFlashObjects.AxShockwaveFlash();
                    }
                    catch
                    {

                    }
                    if (axFlashPlayer != null)
                    {
                        axFlashPlayer.SuspendLayout();
                        axFlashPlayer.Parent = splitContainer1.Panel2;
                        axFlashPlayer.Visible = true;
                        axFlashPlayer.Dock = DockStyle.Fill;
                        axFlashPlayer.PerformLayout();
                    }

                }
            }

            if (axFlashPlayer != null)
            {

                axFlashPlayer.LoadMovie(0, "");
                string tempFileName = GetTempFileFromEntry(entry);
                HideMediaPlayer();
                pictureBox1.Visible = false;
                axFlashPlayer.Visible = true;
                axFlashPlayer.Dock = DockStyle.None;
                axFlashPlayer.Width = axFlashPlayer.Width - 1;
                axFlashPlayer.Height = axFlashPlayer.Height;
                axFlashPlayer.Dock = DockStyle.Fill;
                axFlashPlayer.ScaleMode = 3;
                axFlashPlayer.LoadMovie(0, tempFileName);
            }
        }

        private void CreateMediaPlayer(ArchiveFileEntry entry)
        {
            if (axMediaPlayer == null)
            {
                if (!mediaPlayerCreated)
                {
                    mediaPlayerCreated = true;
                    try
                    {
                        axMediaPlayer = new AxWMPLib.AxWindowsMediaPlayer();
                    }
                    catch
                    {

                    }
                    if (axMediaPlayer != null)
                    {
                        axMediaPlayer.SuspendLayout();
                        axMediaPlayer.Parent = splitContainer1.Panel2;
                        axMediaPlayer.Visible = true;
                        axMediaPlayer.Dock = DockStyle.Fill;
                        axMediaPlayer.PerformLayout();
                    }
                }
            }

            if (axMediaPlayer != null)
            {
                axMediaPlayer.URL = "";
                string tempFileName = GetTempFileFromEntry(entry);
                pictureBox1.Visible = false;
                HideFlashPlayer();
                axMediaPlayer.Visible = true;
                axMediaPlayer.Dock = DockStyle.None;
                axMediaPlayer.Width = axMediaPlayer.Width - 1;
                axMediaPlayer.Height = axMediaPlayer.Height;
                axMediaPlayer.Dock = DockStyle.Fill;
                axMediaPlayer.URL = tempFileName;
            }
        }

        private string GetTempFileFromEntry(ArchiveFileEntry entry)
        {
            TempFileManager.DefaultInstance.DeleteMyTempFiles();
            string fileName = entry.FileName;
            string ext = Path.GetExtension(entry.FileName).ToLowerInvariant();
            string preferredExt = fileOperations.GetDesiredExtension(ext, null);

            fileName = Path.ChangeExtension(fileName, preferredExt);

            var fileBytes = entry.GetFileData();
            if ((ext == ".aff" || ext == ".swf") && FileOperations.IsAffFile(fileBytes))
            {
                //fileName = Path.ChangeExtension(fileName, ".swf");
                fileBytes = SwfToAffConverter.ConvertAffToSwf(fileBytes);
            }
            string tempFileName = TempFileManager.DefaultInstance.CreateTempFile(fileName);

            File.WriteAllBytes(tempFileName, fileBytes);
            return tempFileName;
        }

        private void HideMediaPlayer()
        {
            if (axMediaPlayer != null)
            {
                axMediaPlayer.Visible = false;
                if (axMediaPlayer.URL != "")
                {
                    axMediaPlayer.URL = "";
                    TempFileManager.DefaultInstance.DeleteMyTempFiles();
                }
            }
        }

        private void HideFlashPlayer()
        {
            if (axFlashPlayer != null)
            {
                if (axFlashPlayer.Visible == true)
                {
                    axFlashPlayer.Visible = false;
                    axFlashPlayer.Stop();
                    axFlashPlayer.LoadMovie(0, "about:blank");
                    TempFileManager.DefaultInstance.DeleteMyTempFiles();
                }
            }
        }

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            if (this.loadedArchiveFiles == null)
            {
                this.saveAsToolStripMenuItem.Enabled = false;
                this.saveAsWithPatchToolStripMenuItem.Enabled = false;
            }
            else
            {
                this.saveAsWithPatchToolStripMenuItem.Enabled = true;
                this.saveAsToolStripMenuItem.Enabled = true;
            }

            //recent files
            var items = RecentFilesList.FilesList.GetList();
            recentFilesToolStripMenuItem.DropDownItems.Clear();
            foreach (var fileName in items)
            {
                var newItem = recentFilesToolStripMenuItem.DropDownItems.Add(fileName);
                newItem.Tag = fileName;
                newItem.Click += new EventHandler(newItem_Click);
            }
            recentFilesToolStripMenuItem.Enabled = recentFilesToolStripMenuItem.DropDownItems.Count > 0;
        }
        
        //clicking on recent files
        void newItem_Click(object sender, EventArgs e)
        {
            var toolStripItem = sender as ToolStripItem;
            if (toolStripItem != null)
            {
                string fileName = toolStripItem.Tag as string;
                if (fileName != null)
                {
                    OpenFile(fileName);
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void Save()
        {
            fileOperations.SaveAs();
            this.loadedArchiveFiles = fileOperations.loadedArchiveFiles;
        }

        private void importAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportAll();
        }

        private void ImportAll()
        {
            int fileCount = fileOperations.ImportAll();

            if (fileCount == 0)
            {
                MessageBox.Show(this, "No matching files were found!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else if (fileCount >= 1)
            {
                MessageBox.Show(this, fileCount.ToString() + (fileCount > 1 ? " files" : " file") + " will be imported the next time this archive file is saved.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            LoadTreeView(true);
        }

        private void ExplorerForm_Load(object sender, EventArgs e)
        {
            //axWindowsMediaPlayer1.Visible = false;
            //axWindowsMediaPlayer1.Dock = DockStyle.Fill;
            pictureBox1.Visible = true;
            pictureBox1.Dock = DockStyle.Fill;

            this.debugCommandToolStripMenuItem.Visible = Debugger.IsAttached;

            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                OpenFile(args[1]);
            }
        }

        private void ExplorerForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    string firstFileName = files[0];

                    if (ArchiveFile.IsArchiveFileExtension(Path.GetExtension(firstFileName)))
                    {
                        OpenFile(firstFileName);
                    }
                    else
                    {
                        //TODO: rewrite this to allow importing files
                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }
                    }
                }
            }
        }

        private void ExplorerForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void ExplorerForm_DragLeave(object sender, EventArgs e)
        {

        }

        private void ExplorerForm_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void exportAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportAll();
        }

        private void ExportAll()
        {
            fileOperations.ExportAll();
        }

        private void ExportSelectedFiles()
        {
            if (loadedArchiveFiles == null)
            {
                return;
            }

            ArchiveFileEntry[] entries = GetSelectedEntries();

            fileOperations.ExportFiles(entries);
        }

        private ArchiveFileEntry[] GetSelectedEntries()
        {
            List<ArchiveFileEntry> entries = new List<ArchiveFileEntry>();

            //foreach (TreeNode node in treeView1.Nodes)
            foreach (ListViewItem node in listView1.SelectedItems)
            {
                //if (node.IsSelected)
                {
                    var entry = node.Tag as ArchiveFileEntry;
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
            }
            return entries.ToArray();
        }

        //private void ExportCheckedFiles()
        //{
        //    if (loadedArchiveFiles == null)
        //    {
        //        return;
        //    }

        //    List<ArchiveFileEntry> entries = GetCheckedEntries();

        //    fileOperations.ExportFiles(entries);
        //}

        //private List<ArchiveFileEntry> GetCheckedEntries()
        //{
        //    List<ArchiveFileEntry> entries = new List<ArchiveFileEntry>();

        //    //foreach (TreeNode node in treeView1.Nodes)
        //    foreach (ListViewItem node in listView1.CheckedItems)
        //    {
        //        if (node.Checked)
        //        {
        //            var entry = node.Tag as ArchiveFileEntry;
        //            if (entry != null)
        //            {
        //                entries.Add(entry);
        //            }
        //        }
        //    }
        //    return entries;
        //}

        private void exportAllToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ExportAll();
        }

        private void exportSelectedItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportSelectedFiles();
        }

        private void importAllToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            ImportAll();
        }

        private void importExportToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var toolStripItem = sender as ToolStripMenuItem;
            if (toolStripItem == null)
            {
                return;
            }
            bool enabled = (loadedArchiveFiles != null);
            foreach (ToolStripItem item in toolStripItem.DropDownItems)
            {
                item.Enabled = enabled;
            }

            var selectedEntries = GetSelectedEntries();
            bool anythingSelected = (selectedEntries != null && selectedEntries.Length > 0);

            doNotConvertImageFilesToolStripMenuItem.Enabled = true;
            alwaysRemapPaletteToolStripMenuItem.Enabled = true;
            includeDirectoriesWhenExportingFilesToolStripMenuItem.Enabled = true;
            
            exportSelectedItemsToolStripMenuItem.Enabled = anythingSelected;
        }

        private void newArchiveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewFile();
        }

        private void NewFile()
        {
            if (fileOperations.NewFile())
            {
                this.loadedArchiveFiles = fileOperations.loadedArchiveFiles;
                LoadTreeView(false);
            }
        }

        private void exportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportSelectedEntries();
        }

        private void ExportSelectedEntries()
        {
            fileOperations.ExportFiles(GetSelectedEntries());
        }

        private ArchiveFileEntry GetSelectedEntry()
        {
            ArchiveFileEntry selectedEntry = null;

            var selectedNode = GetSelectedNode();

            if (selectedNode != null)
            {
                selectedEntry = selectedNode.Tag as ArchiveFileEntry;
                if (selectedEntry != null)
                {
                    return selectedEntry;
                }
            }
            return null;
        }

        private ListViewItem GetSelectedNode()
        {
            var selectedNode = listView1.FocusedItem;
            if (selectedNode == null || selectedNode.Selected != true)
            {
                selectedNode = listView1.SelectedItems.OfType<ListViewItem>().FirstOrDefault();
            }
            return selectedNode;
        }

        private void replaceFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReplaceFiles(GetSelectedEntries());
        }

        private void ReplaceFiles(IEnumerable<ArchiveFileEntry> selectedEntries)
        {
            fileOperations.ReplaceFiles(selectedEntries);
        }


        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.loadedArchiveFiles == null)
            {
                return;
            }
            var selectedNodes = GetSelectedNodeAndSubnodes();
            var selectedEntries = GetSelectedEntries();
            DeleteEntries(selectedEntries, selectedNodes);
        }

        private ListViewItem[] GetSelectedNodeAndSubnodes()
        {
            List<ListViewItem> selectedNodes = new List<ListViewItem>();
            var selectedItems = listView1.GetSelectedItems();

            foreach (ListViewItem node in selectedItems)
            {
                if (node.IsFileNode())
                {
                    selectedNodes.Add(node);
                    ListViewItem nextNode = node.GetNextNode();
                    while (nextNode != null && nextNode.IsSubFileNode())
                    {
                        selectedNodes.Add(nextNode);
                        nextNode = nextNode.GetNextNode();
                    }
                }
            }
            return selectedNodes.OrderBy(n => n.Index).ToArray();
        }

        private void DeleteEntries(IEnumerable<ArchiveFileEntry> entriesToDelete, IEnumerable<ListViewItem> nodesToDelete)
        {
            if (entriesToDelete != null && entriesToDelete.Count() > 0)
            {
                if (MessageBox.Show(this, "Really delete files from archive?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    fileOperations.DeleteFiles(entriesToDelete);

                    if (nodesToDelete != null)
                    {
                        listView1.BeginUpdate();
                        var nodesIndexesToDelete = GetNodeIndexesToDelete(nodesToDelete);
                        foreach (int i in nodesIndexesToDelete)
                        {
                            var node = listView1.Items[i];
                            var nodeEntry = node.Tag as ArchiveFileEntry;
                            if (nodeEntry != null)
                            {
                                listView1.Items.RemoveAt(i);
                            }
                        }

                        listView1.EndUpdate();
                    }
                    loadedArchiveFiles.Refresh();
                }
            }
        }

        private KeyValuePair<int, ArchiveFile>[] GetEntryPairsToDelete(IEnumerable<ArchiveFileEntry> entriesToDelete)
        {
            this.loadedArchiveFiles.UpdateIndexes();
            var entryPairsToDelete = entriesToDelete.Select(e => new KeyValuePair<int, ArchiveFile>(e.Index, loadedArchiveFiles.GetArchiveFileByLetter(e.FileLetter))).OrderByDescending(pair => pair.Key).ToArray();
            return entryPairsToDelete;
        }

        private static int[] GetNodeIndexesToDelete(IEnumerable<ListViewItem> nodesToDelete)
        {
            var nodesIndexesToDelete = nodesToDelete.Select(node => node.Index).OrderByDescending(i => i).ToArray();
            return nodesIndexesToDelete;
        }

        //private void deleteCheckedFilesToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    if (loadedArchiveFiles == null)
        //    {
        //        return;
        //    }

        //    List<ArchiveFileEntry> entries = GetCheckedEntries();
        //    var checkedNodes = listView1.CheckedItems.OfType<ListViewItem>().ToArray();

        //    DeleteEntries(entries, checkedNodes);
        //}

        private void importNewFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportNewFiles();
        }

        private void ImportNewFiles()
        {
            if (loadedArchiveFiles == null)
            {
                return;
            }

            var archiveFile = GetArchiveFileForSelectedNode();
            if (fileOperations.ImportNewFiles(archiveFile))
            {
                LoadTreeView(true);
            }
        }

        private ArchiveFile GetArchiveFileForSelectedNode()
        {
            var selectedEntry = GetSelectedEntry();
            int fileLetter = 1;
            if (selectedEntry != null)
            {
                fileLetter = selectedEntry.FileLetter;
            }
            else
            {
                var selectedNode = GetSelectedNode();
                if (selectedNode == null)
                {
                    selectedNode = listView1.Items.OfType<ListViewItem>().FirstOrDefault();
                }
                if (selectedNode != null)
                {
                    var selectedArchiveFile = selectedNode.Tag as ArchiveFile;
                    if (selectedArchiveFile != null)
                    {
                        fileLetter = selectedArchiveFile.FileLetter;
                    }
                }
            }
            var archiveFile = loadedArchiveFiles.GetArchiveFileByLetter(fileLetter);
            return archiveFile;
        }

        private void copyToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyImageToClipboard();
        }

        private void CopyImageToClipboard()
        {
            var entry = GetSelectedEntry();
            if (entry != null)
            {
                using (var bitmap = fileOperations.GetBitmapFromFile(entry))
                {
                    if (bitmap != null)
                    {
                        using (var windowsBitmap = bitmap.ToBitmap())
                        {
                            try
                            {
                                Clipboard.SetImage(windowsBitmap);
                            }
                            catch
                            {
                                MessageBox.Show("Failed to put image on the clipboard, please close any programs that monitor the clipboard (such as Translation Aggregator), then try again.",
                                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            }
                        }
                    }
                }
            }
        }

        private void exportTextFromSCOFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportText();
        }

        private void ExportText()
        {
            if (this.loadedArchiveFiles == null || (this.loadedArchiveFiles.ArchiveFileName ?? "") == "")
            {
                return;
            }
            string ainFileName = Path.Combine(Path.GetDirectoryName(this.loadedArchiveFiles.ArchiveFileName), "System39.ain");
            if (!File.Exists(ainFileName))
            {
                ExportTextClassic();
                return;
            }

            string[] ainStrings = GetAinStrings(ainFileName);
            if (ainStrings == null)
            {
                return;
            }

            foreach (var fileEntry in loadedArchiveFiles.FileEntries)
            {
                string extension = Path.GetExtension(fileEntry.FileName).ToLowerInvariant();
                if (extension == ".sco")
                {
                    ExportText(fileEntry, ainStrings);
                }
            }
        }

        private void ExportTextClassic()
        {
            foreach (var fileEntry in loadedArchiveFiles.FileEntries)
            {
                string extension = Path.GetExtension(fileEntry.FileName).ToLowerInvariant();
                if (extension == ".sco")
                {
                    ExportTextClassic(fileEntry);
                }
            }
        }

        static Encoding shiftJis = Encoding.GetEncoding("shift_jis");

        private void ExportText(ArchiveFileEntry fileEntry, string[] ainStrings)
        {
            string textDirectory = Path.Combine(Path.GetDirectoryName(this.loadedArchiveFiles.ArchiveFileName), "text");
            string textBaseFileName = Path.ChangeExtension(fileEntry.FileName, ".txt");
            string fileName = Path.Combine(textDirectory, textBaseFileName);

            string[] strings = GetFileStrings(fileEntry, ainStrings);
            if (strings.Length > 0)
            {
                if (!Directory.Exists(textDirectory))
                {
                    Directory.CreateDirectory(textDirectory);
                }
                File.WriteAllLines(fileName, strings, shiftJis);
            }
        }

        private void ExportTextClassic(ArchiveFileEntry fileEntry)
        {
            string textDirectory = Path.Combine(Path.GetDirectoryName(this.loadedArchiveFiles.ArchiveFileName), "text");
            string textBaseFileName = Path.ChangeExtension(fileEntry.FileName, ".txt");
            string fileName = Path.Combine(textDirectory, textBaseFileName);

            string[] strings = GetFileStringsClassic(fileEntry);
            if (strings.Length > 0)
            {
                if (!Directory.Exists(textDirectory))
                {
                    Directory.CreateDirectory(textDirectory);
                }
                File.WriteAllLines(fileName, strings, shiftJis);
            }
        }

        private string[] GetFileStrings(ArchiveFileEntry fileEntry, string[] ainStrings)
        {
            var bytes = fileEntry.GetFileData();
            var ms = new MemoryStream(bytes);
            var br = new BinaryReader(ms);
            List<string> list = new List<string>();
            int position = -1;
            while (true)
            {
                position = bytes.IndexOf("/", position + 1);
                if (position == -1)
                {
                    break;
                }
                if (position + 5 < bytes.Length)
                {
                    if (bytes[position + 1] >= 0x7C && bytes[position + 1] <= 0x7F)
                    {
                        ms.Position = position + 2;
                        int stringIndex = br.ReadInt32();
                        if (stringIndex >= 0 && stringIndex < ainStrings.Length)
                        {
                            string str = ainStrings[stringIndex];
                            list.Add(str);
                            position = (int)ms.Position - 1;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            return list.ToArray();
        }

        private string[] GetFileStringsClassic(ArchiveFileEntry fileEntry)
        {
            var bytes = fileEntry.GetFileData();
            var ms = new MemoryStream(bytes);
            var br = new BinaryReader(ms);
            List<string> list = new List<string>();

            const int minimumLength = 2;
            List<string> strings = new List<string>();


            for (int startPosition = 0; startPosition < bytes.Length; startPosition++)
            {
                int validBytes = ShiftJisUtil.NumberOfValidBytesAtPositionNoAscii(bytes, startPosition);
                if (validBytes >= minimumLength)
                {
                    byte[] bytes2 = new byte[validBytes];
                    Array.Copy(bytes, startPosition, bytes2, 0, validBytes);
                    string text = ShiftJisUtil.HalfWidthKatakanaToHiragana(shiftJis.GetString(bytes2, 0, bytes2.Length));
                    if (text.Length == 1 && text[0] < 0x2000)
                    {
                        //reject single greek/cyrillic character
                    }
                    else
                    {
                        strings.Add(text);
                    }
                    startPosition += validBytes;
                }
            }
            return strings.ToArray();
        }

        private string[] GetAinStrings(string ainFileName)
        {
            var bytes = File.ReadAllBytes(ainFileName);
            for (int i = 4; i < bytes.Length; i++)
            {
                int b = bytes[i];
                bytes[i] = (byte)((b >> 6) | b << 2);
            }
            var ms = new MemoryStream(bytes);
            var br = new BinaryReader(ms);
            ms.Position = 4;
            int number4 = br.ReadInt32();
            string hel0 = br.ReadStringFixedSize(4);
            if (hel0 != "HEL0")
            {
                return null;
            }

            int addressOfFunc = bytes.IndexOf("FUNC\0\0\0\0", (int)ms.Position);
            if (addressOfFunc == -1) return null;
            int addressOfVari = bytes.IndexOf("VARI\0\0\0\0", addressOfFunc + 8);
            if (addressOfVari == -1) return null;
            int addressOfMsgi = bytes.IndexOf("MSGI\0\0\0\0", addressOfVari + 8);
            if (addressOfMsgi == -1) return null;

            ms.Position = addressOfMsgi + 8;
            int numberOfStrings = br.ReadInt32();
            List<string> list = new List<string>(numberOfStrings);
            for (int i = 0; i < numberOfStrings; i++)
            {
                string str = br.ReadStringNullTerminated();
                list.Add(str);
            }

            return list.ToArray();
        }

        private void ExplorerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (axMediaPlayer != null && !axMediaPlayer.IsDisposed)
            {
                this.Hide();
                axMediaPlayer.URL = "";
                axMediaPlayer.Dispose();
            }
            if (TempFileManager.DefaultInstanceCreated)
            {
                TempFileManager.DefaultInstance.DeleteMyTempFiles();
                TempFileManager.DefaultInstance.Destroy();
            }
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenProperties();
        }

        private void OpenProperties()
        {
            var selectedEntry = GetSelectedEntry();
            if (selectedEntry != null)
            {
                using (var propertiesForm = new PropertiesForm())
                {
                    propertiesForm.FileEntry = selectedEntry;
                    propertiesForm.ShowDialog();
                    if (propertiesForm.Dirty)
                    {
                        this.loadedArchiveFiles.Refresh();
                        LoadTreeView(true);
                    }
                }
            }
        }

        void WhenIdle(Action action)
        {
            EventHandler appIdleHandler = null;
            appIdleHandler = (sender, e) =>
                {
                    Application.Idle -= appIdleHandler;
                    action();
                };

            Application.Idle += new EventHandler(appIdleHandler);
        }

        bool ready = true;
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ready == true)
            {
                ready = false;
                WhenIdle(() =>
                    {
                        if (ready == false)
                        {
                            try
                            {
                                DisplayEntry(GetSelectedEntry());
                            }
                            finally
                            {
                                ready = true;
                            }
                        }
                    }
                        );
            }
        }

        private void listView1_Resize(object sender, EventArgs e)
        {
            //listView1.Columns[0].Width = listView1.ClientRectangle.Width - 20;
        }

        private void saveAsWithPatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fileOperations.SavePatch();
        }

        private void alwaysRemapPaletteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            GlobalSettings.AlwaysRemapImages = menuItem.Checked;
        }

        private void listView1_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            ArchiveFileEntry entry = GetEntry(e.Item);
            if (entry != null)
            {
                e.CancelEdit = false;
            }
            else
            {
                e.CancelEdit = true;
            }
        }

        private ArchiveFileEntry GetEntry(int i)
        {
            ArchiveFileEntry entry = null;
            if (i >= 0 && i < this.listView1.Items.Count)
            {
                var item = this.listView1.Items[i];
                entry = item.Tag as ArchiveFileEntry;
            }
            return entry;
        }

        private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            ArchiveFileEntry entry = GetEntry(e.Item);
            if (entry != null && !e.CancelEdit && !String.IsNullOrEmpty(e.Label))
            {
                //error checking maybe?
                entry.FileName = e.Label.Trim();
                var node = listView1.Items[e.Item];
                node.Text = entry.FileName;
            }
        }

        bool TryExpandItem(int i)
        {
            if (i < 0 || i >= listView1.Items.Count)
            {
                return false;
            }
            var selectedItem = listView1.Items[i];
            var selectedEntry = selectedItem.Tag as ArchiveFileEntry;
            if (selectedEntry != null)
            {
                return AddSubItems(i);
            }
            return false;
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            var selectedItem = listView1.SelectedItems.OfType<ListViewItem>().FirstOrDefault();
            int selectedIndex = listView1.SelectedIndices.OfType<int>().FirstOrDefault();
            if (selectedItem != null)
            {
                if (e.KeyData == Keys.F2)
                {
                    selectedItem.BeginEdit();
                    e.Handled = true;
                }
                if (e.KeyData == Keys.Right)
                {
                    if (TryExpandItem(selectedIndex))
                    {
                        e.Handled = true;
                    }
                }
                if (e.KeyData == Keys.Left)
                {
                    if (TryRemoveSubItems(selectedIndex))
                    {
                        e.Handled = true;
                    }
                    if (selectedItem.IsSubFileNode())
                    {
                        int i = selectedIndex;
                        while (listView1.Items[i].IsSubFileNode() && i >= 0)
                        {
                            i--;
                        }
                        if (i >= 0)
                        {
                            var item = listView1.Items[i];
                            item.EnsureVisible();
                            selectedItem.Selected = false;
                            item.Selected = true;
                            item.Focused = true;
                        }
                        e.Handled = true;
                    }
                }
            }
        }

        private bool TryRemoveSubItems(int i)
        {
            if (i < 0 || i >= listView1.Items.Count)
            {
                return false;
            }
            var selectedItem = listView1.Items[i];

            if (selectedItem.IsFileNode())
            {
                if (i + 1 < listView1.Items.Count)
                {
                    var nextItem = listView1.Items[i + 1];
                    if (nextItem.IsSubFileNode())
                    {
                        return RemoveSubItems(i);
                    }
                }
            }
            return false;
        }

        private void debugCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedEntry = GetSelectedEntry();
            //if (selectedEntry != null)
            //{
            //    var subImages = new SubImages(selectedEntry);
            //}
        }

        bool AddSubItems(int i)
        {
            if (i < 0 || i >= listView1.Items.Count)
            {
                return false;
            }
            var listViewItem = listView1.Items[i];
            if (i + 1 < listView1.Items.Count)
            {
                var nextListViewItem = listView1.Items[i + 1];
                if (listViewItem.IndentCount < nextListViewItem.IndentCount)
                {
                    return false;
                }
            }


            var entry = listViewItem.Tag as ArchiveFileEntry;
            if (entry == null)
            {
                return false;
            }
            var subImages = entry.GetSubImages();
            if (subImages == null || subImages.Length == 0)
            {
                return false;
            }

            //var subImages = new SubImages(entry);
            //var nodes = subImages.GetSubimages();
            //if (nodes == null || nodes.Length == 0)
            //{
            //    return false;
            //}

            listView1.BeginUpdate();
            foreach (var subImage in subImages)
            {
                var newItem = new ListViewItem();
                newItem.Text = subImage.FileName;
                newItem.SetSubFileIndentCount();
                newItem.ImageIndex = GetImageIndex(Path.GetExtension(newItem.Text).ToLowerInvariant());
                newItem.Tag = subImage;
                i++;
                listView1.Items.Insert(i, newItem);
            }
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.EndUpdate();
            return true;
        }

        private bool RemoveSubItems(int i)
        {
            if (i < 0 || i >= listView1.Items.Count)
            {
                return false;
            }
            i++;
            listView1.BeginUpdate();
            while (i < listView1.Items.Count && listView1.Items[i].IsSubFileNode())
            {
                listView1.Items.RemoveAt(i);
            }
            listView1.EndUpdate();
            return true;
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var item = listView1.GetItemAt(e.X, e.Y);
            if (item != null)
            {
                if (!TryExpandItem(item.Index))
                {
                    TryRemoveSubItems(item.Index);
                }
            }
        }

        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
            var item = e.Item;
            if (item != null)
            {
                var entry = item.Tag as ArchiveFileEntry;
                if (item.IsFileNode() && entry != null && entry.HasSubImages())
                {
                    int i2 = e.ItemIndex + 1;
                    bool isPlus = true;
                    if (i2 >= 0 && i2 < listView1.Items.Count)
                    {
                        var item2 = listView1.Items[i2];
                        if (item2.IsSubFileNode())
                        {
                            isPlus = false;
                        }
                    }
                    int locationOfPlusIcon = e.Bounds.Left;

                    int indentSize = 0;
                    if (listView1.SmallImageList != null)
                    {
                        indentSize = listView1.SmallImageList.ImageSize.Width;
                    }
                    if (indentSize == 0)
                    {
                        return;
                    }

                    int drawX = e.Bounds.Left + (item.IndentCount - 1) * indentSize;
                    int drawY = e.Bounds.Top;

                    if (isPlus)
                    {
                        e.Graphics.DrawImageUnscaled(Properties.Resources.plusicon, drawX, drawY);
                    }
                    else
                    {
                        e.Graphics.DrawImageUnscaled(Properties.Resources.minusicon, drawX, drawY);
                    }
                }
            }
        }

        private void listView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var item0 = listView1.GetItemAt(e.X, e.Y);
                var item = listView1.GetItemAt(e.X + 16, e.Y);
                if (item != null && item0 == null)
                {
                    if (!TryExpandItem(item.Index))
                    {
                        TryRemoveSubItems(item.Index);
                    }
                }
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void copyFilenamesToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyFilenamesToClipboard();
        }

        private void CopyFilenamesToClipboard()
        {
            string newText;
            {
                StringBuilder sb = new StringBuilder();
                var selectedEntries = GetSelectedEntries();
                foreach (var entry in selectedEntries)
                {
                    sb.AppendLine(entry.FileName);
                }
                newText = sb.ToString();
            }
            try
            {
                Clipboard.SetText(newText);
            }
            catch
            {
                MessageBox.Show("Failed to copy text to clipboard, please close any programs that monitor the clipboard (such as Translation Aggregator) and try again.", 
                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void copyImageToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyImageToClipboard();
        }

        private void copyFilenamesToClipboardToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            CopyFilenamesToClipboard();
        }

        private void findFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileFileDialog();
        }

        FindFileForm findFileForm = null;

        private void OpenFileFileDialog()
        {
            if (findFileForm == null || (!findFileForm.IsHandleCreated || findFileForm.IsDisposed))
            {
                if (findFileForm != null)
                {
                    findFileForm.SearchButton.Click -= new EventHandler(SearchButton_Click);
                }
                findFileForm = new FindFileForm();
                findFileForm.SearchButton.Click += new EventHandler(SearchButton_Click);
            }

            if (findFileForm != null && !findFileForm.IsDisposed)
            {
                findFileForm.Owner = this;
                findFileForm.StartPosition = FormStartPosition.Manual;
                //FormStartPosition.CenterParent is broken
                int x = (this.Bounds.Left + this.Bounds.Right) / 2;
                int y = (this.Bounds.Top + this.Bounds.Bottom) / 2;
                x -= findFileForm.Width / 2;
                y -= findFileForm.Height / 2;

                findFileForm.Left = x;
                findFileForm.Top = y;

                findFileForm.Show();
            }
        }

        void SearchButton_Click(object sender, EventArgs e)
        {
            if (!(findFileForm == null || (!findFileForm.IsHandleCreated || findFileForm.IsDisposed)))
            {
                string searchText = findFileForm.SearchTextBox.Text;
                if (SearchForFile(searchText))
                {
                    var control = sender as Control;
                    if (control != null)
                    {
                        var form = control.FindForm();

                        if (form != null)
                        {
                            form.Close();
                        }
                    }
                }
            }
        }

        private bool SearchForFile(string searchText)
        {
            var focusedItem = listView1.FocusedItem;
            int itemCount = listView1.Items.Count;
            int focusedIndex = 0;
            if (focusedItem != null)
            {
                focusedIndex = focusedItem.Index;
            }

            int startIndex = focusedIndex;
            if (focusedIndex < 0 || focusedIndex >= itemCount)
            {
                return false;
            }

            int i = startIndex;
            do
            {
                i++;
                if (i >= itemCount)
                {
                    i = 0;
                }

                var item = listView1.Items[i];
                if (item.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    item.EnsureVisible();
                    item.Focused = true;

                    if (listView1.SelectedIndices.Count < 2)
                    {
                        if (listView1.SelectedIndices.Count == 1)
                        {
                            listView1.Items[listView1.SelectedIndices[0]].Selected = false;
                        }
                        item.Selected = true;
                    }

                    return true;
                }
            } while (i != startIndex);
            return false;
        }

        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var selectedEntries = GetSelectedEntries();
            if (selectedEntries == null || selectedEntries.Length == 0)
            {
                copyFilenamesToClipboardToolStripMenuItem1.Enabled = false;
                copyImageToClipboardToolStripMenuItem.Enabled = false;
            }
            else
            {
                copyFilenamesToClipboardToolStripMenuItem1.Enabled = true;
                copyImageToClipboardToolStripMenuItem.Enabled = true;
            }

            if (this.loadedArchiveFiles != null && this.loadedArchiveFiles.ArchiveFiles != null && this.loadedArchiveFiles.ArchiveFiles.Count > 0)
            {
                findFileToolStripMenuItem.Enabled = true;
            }
            else
            {
                findFileToolStripMenuItem.Enabled = false;
            }

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            var selectedEntries = GetSelectedEntries();
            bool shouldEnable = true;
            if (selectedEntries == null || selectedEntries.Length == 0)
            {
                shouldEnable = false;
            }
            copyFilenamesToClipboardToolStripMenuItem.Enabled = shouldEnable;
            copyToClipboardToolStripMenuItem.Enabled = shouldEnable;

            exportFileToolStripMenuItem.Enabled = shouldEnable;
            replaceFileToolStripMenuItem.Enabled = shouldEnable;
            deleteFileToolStripMenuItem.Enabled = shouldEnable;
            propertiesToolStripMenuItem.Enabled = shouldEnable;
        }

        private void doNotConvertImageFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            this.fileOperations.DoNotConvertImageFiles = menuItem.Checked;
        }

        private void includeDirectoriesWhenExportingFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            this.fileOperations.IncludeDirectoriesWhenExportingFiles = menuItem.Checked;
        }
    }

    public static class ListViewItemExtensions
    {
        public static void SetRootIndentCount(this ListViewItem item)
        {
            item.IndentCount = 0;
        }
        public static void SetFileIndentCount(this ListViewItem item)
        {
            item.IndentCount = 1;
        }
        public static void SetSubFileIndentCount(this ListViewItem item)
        {
            item.IndentCount = 2;
        }
        public static bool IsRootNode(this ListViewItem item)
        {
            return item.IndentCount == 0;
        }
        public static bool IsFileNode(this ListViewItem item)
        {
            return item.IndentCount == 1;
        }
        public static bool IsSubFileNode(this ListViewItem item)
        {
            return item.IndentCount == 2;
        }
        //public static int FindListViewItem(this ListView.ListViewItemCollection items, string text)
        //{

        //}

        public static ListViewItem GetNextNode(this ListViewItem item)
        {
            if (item == null) return null;
            int index = item.Index;
            var parent = item.ListView;

            //if (index < 0 || index >= parent.Items.Count)
            //{
            //    return null;
            //}

            //var item2 = parent.Items[index];
            //if (item2 != item)
            //{
            //    //if index was incorrect, find the real index
            //    int i;
            //    for (i = 0; i < parent.Items.Count; i++)
            //    {
            //        var itemInList = parent.Items[i];
            //        if (itemInList.Text == item.Text)
            //        {
            //            index = i;
            //            item = itemInList;
            //            break;
            //        }
            //    }
            //    if (i == parent.Items.Count)
            //    {
            //        item = null;
            //        return null;
            //    }
            //}

            int nextIndex = index + 1;
            if (nextIndex < 0 || nextIndex >= parent.Items.Count)
            {
                return null;
            }
            return parent.Items[nextIndex];
        }


        public static bool NextNodeIsSubFileNode(this ListViewItem item)
        {
            var nextNode = item.GetNextNode();
            if (nextNode == null)
            {
                return false;
            }
            return nextNode.IsSubFileNode();
        }

        public static ListViewItem[] GetSelectedItems(this ListView listView)
        {
            int[] selectedIndices = listView.SelectedIndices.OfType<int>().OrderBy(i => i).ToArray();
            return selectedIndices.Select(i => listView.Items[i]).ToArray();
        }
    }
}
