namespace ALDExplorer2
{
    partial class ExplorerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExplorerForm));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.exportFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.propertiesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            this.copyToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyFilenamesToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsWithPatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dummyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.newArchiveFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyImageToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyFilenamesToClipboardToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.findFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importExportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportSelectedItemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
            this.importAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.importNewFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.exportTextFromSCOFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.doNotConvertImageFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.alwaysRemapPaletteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugCommandToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pictureBox1);
            this.splitContainer1.Size = new System.Drawing.Size(721, 421);
            this.splitContainer1.SplitterDistance = 239;
            this.splitContainer1.TabIndex = 0;
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listView1.ContextMenuStrip = this.contextMenuStrip1;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listView1.LabelEdit = true;
            this.listView1.Location = new System.Drawing.Point(0, 0);
            this.listView1.Name = "listView1";
            this.listView1.OwnerDraw = true;
            this.listView1.ShowItemToolTips = true;
            this.listView1.Size = new System.Drawing.Size(239, 421);
            this.listView1.SmallImageList = this.imageList1;
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseDoubleClick);
            this.listView1.Resize += new System.EventHandler(this.listView1_Resize);
            this.listView1.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.listView1_DrawItem);
            this.listView1.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.listView1_AfterLabelEdit);
            this.listView1.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
            this.listView1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseDown);
            this.listView1.BeforeLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.listView1_BeforeLabelEdit);
            this.listView1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listView1_KeyDown);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportFileToolStripMenuItem,
            this.replaceFileToolStripMenuItem,
            this.deleteFileToolStripMenuItem,
            this.propertiesToolStripMenuItem,
            this.toolStripMenuItem5,
            this.copyToClipboardToolStripMenuItem,
            this.copyFilenamesToClipboardToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(211, 142);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // exportFileToolStripMenuItem
            // 
            this.exportFileToolStripMenuItem.Name = "exportFileToolStripMenuItem";
            this.exportFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.exportFileToolStripMenuItem.Text = "&Export files...";
            this.exportFileToolStripMenuItem.Click += new System.EventHandler(this.exportFileToolStripMenuItem_Click);
            // 
            // replaceFileToolStripMenuItem
            // 
            this.replaceFileToolStripMenuItem.Name = "replaceFileToolStripMenuItem";
            this.replaceFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.replaceFileToolStripMenuItem.Text = "&Replace files...";
            this.replaceFileToolStripMenuItem.Click += new System.EventHandler(this.replaceFileToolStripMenuItem_Click);
            // 
            // deleteFileToolStripMenuItem
            // 
            this.deleteFileToolStripMenuItem.Name = "deleteFileToolStripMenuItem";
            this.deleteFileToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.deleteFileToolStripMenuItem.Text = "&Delete Selected Files";
            this.deleteFileToolStripMenuItem.Click += new System.EventHandler(this.deleteFileToolStripMenuItem_Click);
            // 
            // propertiesToolStripMenuItem
            // 
            this.propertiesToolStripMenuItem.Name = "propertiesToolStripMenuItem";
            this.propertiesToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.propertiesToolStripMenuItem.Text = "&Properties...";
            this.propertiesToolStripMenuItem.Click += new System.EventHandler(this.propertiesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(207, 6);
            // 
            // copyToClipboardToolStripMenuItem
            // 
            this.copyToClipboardToolStripMenuItem.Name = "copyToClipboardToolStripMenuItem";
            this.copyToClipboardToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.copyToClipboardToolStripMenuItem.Text = "&Copy to Clipboard";
            this.copyToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyToClipboardToolStripMenuItem_Click);
            // 
            // copyFilenamesToClipboardToolStripMenuItem
            // 
            this.copyFilenamesToClipboardToolStripMenuItem.Name = "copyFilenamesToClipboardToolStripMenuItem";
            this.copyFilenamesToClipboardToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
            this.copyFilenamesToClipboardToolStripMenuItem.Text = "Copy &Filenames to Clipboard";
            this.copyFilenamesToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyFilenamesToClipboardToolStripMenuItem_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "generic_icon.png");
            this.imageList1.Images.SetKeyName(1, "folder.png");
            this.imageList1.Images.SetKeyName(2, "flash_image_icon.png");
            this.imageList1.Images.SetKeyName(3, "flash_sound_icon.png");
            this.imageList1.Images.SetKeyName(4, "script.png");
            this.imageList1.Images.SetKeyName(5, "music.png");
            this.imageList1.Images.SetKeyName(6, "flash_icon.png");
            this.imageList1.Images.SetKeyName(7, "alicefile2.png");
            // 
            // pictureBox1
            // 
            this.pictureBox1.ContextMenuStrip = this.contextMenuStrip1;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(374, 157);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.importExportToolStripMenuItem,
            this.debugCommandToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(721, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.saveAsWithPatchToolStripMenuItem,
            this.toolStripSeparator2,
            this.recentFilesToolStripMenuItem,
            this.toolStripMenuItem3,
            this.newArchiveFileToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler(this.fileToolStripMenuItem_DropDownOpening);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
            this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // saveAsWithPatchToolStripMenuItem
            // 
            this.saveAsWithPatchToolStripMenuItem.Name = "saveAsWithPatchToolStripMenuItem";
            this.saveAsWithPatchToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.saveAsWithPatchToolStripMenuItem.Text = "Save As &Patch...";
            this.saveAsWithPatchToolStripMenuItem.Click += new System.EventHandler(this.saveAsWithPatchToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(219, 6);
            // 
            // recentFilesToolStripMenuItem
            // 
            this.recentFilesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dummyToolStripMenuItem});
            this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
            this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.recentFilesToolStripMenuItem.Text = "&Recent Files";
            // 
            // dummyToolStripMenuItem
            // 
            this.dummyToolStripMenuItem.Name = "dummyToolStripMenuItem";
            this.dummyToolStripMenuItem.Size = new System.Drawing.Size(108, 22);
            this.dummyToolStripMenuItem.Text = "dummy";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(219, 6);
            // 
            // newArchiveFileToolStripMenuItem
            // 
            this.newArchiveFileToolStripMenuItem.Name = "newArchiveFileToolStripMenuItem";
            this.newArchiveFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newArchiveFileToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.newArchiveFileToolStripMenuItem.Text = "&New empty archive file";
            this.newArchiveFileToolStripMenuItem.Click += new System.EventHandler(this.newArchiveFileToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(219, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(222, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyImageToClipboardToolStripMenuItem,
            this.copyFilenamesToClipboardToolStripMenuItem1,
            this.findFileToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            this.editToolStripMenuItem.DropDownOpening += new System.EventHandler(this.editToolStripMenuItem_DropDownOpening);
            // 
            // copyImageToClipboardToolStripMenuItem
            // 
            this.copyImageToClipboardToolStripMenuItem.Name = "copyImageToClipboardToolStripMenuItem";
            this.copyImageToClipboardToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
            this.copyImageToClipboardToolStripMenuItem.Text = "Copy &Image to clipboard";
            this.copyImageToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyImageToClipboardToolStripMenuItem_Click);
            // 
            // copyFilenamesToClipboardToolStripMenuItem1
            // 
            this.copyFilenamesToClipboardToolStripMenuItem1.Name = "copyFilenamesToClipboardToolStripMenuItem1";
            this.copyFilenamesToClipboardToolStripMenuItem1.Size = new System.Drawing.Size(208, 22);
            this.copyFilenamesToClipboardToolStripMenuItem1.Text = "Copy File&names to clipboard";
            this.copyFilenamesToClipboardToolStripMenuItem1.Click += new System.EventHandler(this.copyFilenamesToClipboardToolStripMenuItem1_Click);
            // 
            // findFileToolStripMenuItem
            // 
            this.findFileToolStripMenuItem.Name = "findFileToolStripMenuItem";
            this.findFileToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.findFileToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
            this.findFileToolStripMenuItem.Text = "&Find File...";
            this.findFileToolStripMenuItem.Click += new System.EventHandler(this.findFileToolStripMenuItem_Click);
            // 
            // importExportToolStripMenuItem
            // 
            this.importExportToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exportAllToolStripMenuItem,
            this.exportSelectedItemsToolStripMenuItem,
            this.toolStripMenuItem6,
            this.importAllToolStripMenuItem,
            this.toolStripMenuItem2,
            this.importNewFilesToolStripMenuItem,
            this.toolStripMenuItem4,
            this.exportTextFromSCOFilesToolStripMenuItem,
            this.toolStripMenuItem1,
            this.doNotConvertImageFilesToolStripMenuItem,
            this.alwaysRemapPaletteToolStripMenuItem,
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem});
            this.importExportToolStripMenuItem.Name = "importExportToolStripMenuItem";
            this.importExportToolStripMenuItem.Size = new System.Drawing.Size(87, 20);
            this.importExportToolStripMenuItem.Text = "&Import/Export";
            this.importExportToolStripMenuItem.DropDownOpening += new System.EventHandler(this.importExportToolStripMenuItem_DropDownOpening);
            // 
            // exportAllToolStripMenuItem
            // 
            this.exportAllToolStripMenuItem.Name = "exportAllToolStripMenuItem";
            this.exportAllToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.exportAllToolStripMenuItem.Text = "&Export All...";
            this.exportAllToolStripMenuItem.Click += new System.EventHandler(this.exportAllToolStripMenuItem_Click_1);
            // 
            // exportSelectedItemsToolStripMenuItem
            // 
            this.exportSelectedItemsToolStripMenuItem.Name = "exportSelectedItemsToolStripMenuItem";
            this.exportSelectedItemsToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.exportSelectedItemsToolStripMenuItem.Text = "Export Selected Items...";
            this.exportSelectedItemsToolStripMenuItem.Click += new System.EventHandler(this.exportSelectedItemsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(259, 6);
            // 
            // importAllToolStripMenuItem
            // 
            this.importAllToolStripMenuItem.Name = "importAllToolStripMenuItem";
            this.importAllToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.importAllToolStripMenuItem.Text = "&Import All...";
            this.importAllToolStripMenuItem.Click += new System.EventHandler(this.importAllToolStripMenuItem_Click_1);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(259, 6);
            // 
            // importNewFilesToolStripMenuItem
            // 
            this.importNewFilesToolStripMenuItem.Name = "importNewFilesToolStripMenuItem";
            this.importNewFilesToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.importNewFilesToolStripMenuItem.Text = "Import and add new files...";
            this.importNewFilesToolStripMenuItem.Click += new System.EventHandler(this.importNewFilesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(259, 6);
            // 
            // exportTextFromSCOFilesToolStripMenuItem
            // 
            this.exportTextFromSCOFilesToolStripMenuItem.Name = "exportTextFromSCOFilesToolStripMenuItem";
            this.exportTextFromSCOFilesToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.exportTextFromSCOFilesToolStripMenuItem.Text = "Export &Text from SCO files...";
            this.exportTextFromSCOFilesToolStripMenuItem.Click += new System.EventHandler(this.exportTextFromSCOFilesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(259, 6);
            // 
            // doNotConvertImageFilesToolStripMenuItem
            // 
            this.doNotConvertImageFilesToolStripMenuItem.CheckOnClick = true;
            this.doNotConvertImageFilesToolStripMenuItem.Name = "doNotConvertImageFilesToolStripMenuItem";
            this.doNotConvertImageFilesToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.doNotConvertImageFilesToolStripMenuItem.Text = "Do not convert image files";
            this.doNotConvertImageFilesToolStripMenuItem.Click += new System.EventHandler(this.doNotConvertImageFilesToolStripMenuItem_Click);
            // 
            // alwaysRemapPaletteToolStripMenuItem
            // 
            this.alwaysRemapPaletteToolStripMenuItem.Checked = true;
            this.alwaysRemapPaletteToolStripMenuItem.CheckOnClick = true;
            this.alwaysRemapPaletteToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.alwaysRemapPaletteToolStripMenuItem.Name = "alwaysRemapPaletteToolStripMenuItem";
            this.alwaysRemapPaletteToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.alwaysRemapPaletteToolStripMenuItem.Text = "Always Remap Palette";
            this.alwaysRemapPaletteToolStripMenuItem.Click += new System.EventHandler(this.alwaysRemapPaletteToolStripMenuItem_Click);
            // 
            // includeDirectoriesWhenExportingFilesToolStripMenuItem
            // 
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem.Checked = true;
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem.CheckOnClick = true;
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem.Name = "includeDirectoriesWhenExportingFilesToolStripMenuItem";
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem.Size = new System.Drawing.Size(262, 22);
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem.Text = "Include directories when exporting files";
            this.includeDirectoriesWhenExportingFilesToolStripMenuItem.Click += new System.EventHandler(this.includeDirectoriesWhenExportingFilesToolStripMenuItem_Click);
            // 
            // debugCommandToolStripMenuItem
            // 
            this.debugCommandToolStripMenuItem.Name = "debugCommandToolStripMenuItem";
            this.debugCommandToolStripMenuItem.Size = new System.Drawing.Size(100, 20);
            this.debugCommandToolStripMenuItem.Text = "&Debug Command";
            this.debugCommandToolStripMenuItem.Click += new System.EventHandler(this.debugCommandToolStripMenuItem_Click);
            // 
            // ExplorerForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(721, 445);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ExplorerForm";
            this.Text = "ALD Explorer";
            this.Load += new System.EventHandler(this.ExplorerForm_Load);
            this.DragLeave += new System.EventHandler(this.ExplorerForm_DragLeave);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.ExplorerForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.ExplorerForm_DragEnter);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ExplorerForm_FormClosing);
            this.DragOver += new System.Windows.Forms.DragEventHandler(this.ExplorerForm_DragOver);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ToolStripMenuItem importExportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportSelectedItemsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem importNewFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem newArchiveFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem doNotConvertImageFilesToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exportFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem replaceFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem propertiesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem copyToClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportTextFromSCOFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ToolStripMenuItem saveAsWithPatchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem alwaysRemapPaletteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem includeDirectoriesWhenExportingFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem debugCommandToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyFilenamesToClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyImageToClipboardToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyFilenamesToClipboardToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem findFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dummyToolStripMenuItem;
    }
}

