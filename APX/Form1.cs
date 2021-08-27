using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using libAPX;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;

namespace APX
{
    public partial class Form1 : Form
    {

        private ModManager manager = new ModManager();
        private Steam steam = new Steam();
        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Shown(object sender, EventArgs e)
        {
            this.updateTreeview();


        }

        private void steamInventoryCallback(InventoryItem[] items)
        {

            if (items != null)
            {
                TreeNode node = treeView1.Nodes.Find("Paid content", false).First();
                foreach (InventoryItem item in items)
                {
                    if (item.Def != null)
                    {
                        TreeNode child = new TreeNode();
                        child.ContextMenuStrip = workshopItemContextMenu;
                        child.Tag = item.Id;
                        child.Text = item.Def.Name;
                        node.Nodes.Add(child);
                    }
                }
            }
        }
        private void steamWorkshopCallback(List<Item> allItems)
        {
            if (allItems != null)
            {
                List<string> foundIds = new List<string>();
                string rootPath = null;
                TreeNode node = treeView1.Nodes.Find("Steam", false).First();
                foreach (Steamworks.Ugc.Item item in allItems)
                {
                    String id = item.Id.ToString();
                    String title = item.Title;
                    Boolean isInstalled = item.IsInstalled;
                    String path = item.Directory;
                    if (rootPath == null)
                    {
                        string replacement = @"\" + id;
                        rootPath = path.Replace(replacement, "");
                    }
                    TreeNode child = new TreeNode();
                    child.ContextMenuStrip = workshopItemContextMenu;
                    child.Tag = id;
                    child.Text = String.Format("{0}: {1}", id, title);
                    if (Directory.Exists(path))
                    {
                        String[] files = Directory.GetFiles(path);
                        foreach (String file in files)
                        {
                            TreeNode fileNode = new TreeNode();
                            fileNode.Text = file;
                            fileNode.Tag = file;
                            if (file.EndsWith(".rfcmp"))
                            {
                                fileNode.ContextMenuStrip = packageContextMenuStrip;
                            }
                            child.Nodes.Add(fileNode);
                        }
                    }
                    foundIds.Add(id);
                    node.Nodes.Add(child);
                }

            }
        }

        private void steamServersCallback(ServerInfo item)
        {
            string[] row = new string[] { item.Name, item.Map, item.Ping.ToString(), item.Players.ToString(), item.MaxPlayers.ToString()};
            dataGridView2.Rows.Add(row);
        }

        private void updateTreeview()
        {

            steam.getSubscriptions(steamInventoryCallback, steamWorkshopCallback);
            steam.getServers(steamServersCallback);
            List<Mod> mods = manager.getInstalledMods();
            treeView1.BeginUpdate();
            // find out which base node were selected
            List<string> expandedNodes = new List<string>();
            foreach (TreeNode root in treeView1.Nodes)
            {
                if (root.IsExpanded)
                {
                    expandedNodes.Add(root.Text);
                }
            }
            treeView1.Nodes.Clear();

            // Add packages nodes

            List<string> packages = manager.getPackages();
            TreeNode packageNode = new TreeNode();
            packageNode.Text = "Packages available for installation";
            foreach (string package in packages)
            {
                TreeNode packageFileNode = new TreeNode();
                packageFileNode.Text = package;
                packageFileNode.Tag = package;
                packageFileNode.ContextMenuStrip = packageContextMenuStrip;
                packageNode.Nodes.Add(packageFileNode);
            }

            treeView1.Nodes.Add(packageNode);

            // Add steam node

            TreeNode steamnode = new TreeNode();
            steamnode.Text = "Steam";
            steamnode.Name = "Steam";
            treeView1.Nodes.Add(steamnode);



            // Add events

            TreeNode eventNode = new TreeNode();
            eventNode.Text = "Events";

            List<string> eventFiles = manager.getManifests();
            foreach (string eventFile in eventFiles)
            {
                TreeNode eventFileNode = new TreeNode();
                FileInfo fi = new FileInfo(eventFile);
                eventFileNode.Text = fi.Name;
                eventFileNode.Tag = fi.Name;
                eventFileNode.ImageKey = "Events";
                eventNode.Nodes.Add(eventFileNode);
            }

            treeView1.Nodes.Add(eventNode);


            // Create category nodes first
            List<TreeNode> categoryNodes = new List<TreeNode>();
            foreach (Mod mod in mods)
            {
                if (categoryNodes.FindAll(l => l.Text == mod.Type.ToString()).Count == 0)
                {
                    TreeNode node = new TreeNode();
                    node.Text = mod.Type.ToString();
                    node.ImageKey = node.Text;
                    categoryNodes.Add(node);
                }
            }
            foreach (Mod mod in mods)
            {
                TreeNode node = new TreeNode();
                node.Text = String.Format("{0} {1}", mod.Name, mod.Version);
                node.Tag = mod;
                node.ImageKey = mod.Type.ToString();
                node.ContextMenuStrip = modContextMenuStrip;
                if (mod.UsedBy.Count > 0)
                {
                    node.Text += " used by ";
                    foreach (string usage in mod.UsedBy)
                    {
                        node.Text += usage + ",";
                    }
                }
                node.Text = node.Text.Trim(',');
                {
                    foreach (Mod children in mod.Children)
                    {
                        TreeNode child = new TreeNode();
                        child.Text = String.Format("{0} {1}", children.Name, children.Version);
                        child.ContextMenuStrip = modContextMenuStrip;
                        child.ImageKey = children.Type.ToString();
                        if (children.UsedBy.Count > 0)
                        {
                            child.Text += " used by ";
                            foreach (string usage in children.UsedBy)
                            {
                                child.Text += usage + ",";
                            }
                        }
                        child.Text = child.Text.Trim(',');
                        child.Tag = children;
                        node.Nodes.Add(child);
                    }
                }
                foreach (TreeNode categoryNode in categoryNodes)
                {
                    if (categoryNode.Text == mod.Type.ToString())
                    {
                        categoryNode.Nodes.Add(node);
                        break;
                    }
                }
            }
            foreach (TreeNode categoryNode in categoryNodes)
            {
                treeView1.Nodes.Add(categoryNode);
            }
            foreach (TreeNode rootNode in categoryNodes)
            {
                if (expandedNodes.Contains(rootNode.Text))
                {
                    rootNode.Expand();
                }
            }

            treeView1.EndUpdate();
        }

        private void removePackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Mod mod = (Mod)treeView1.SelectedNode.Tag;
            Boolean result = manager.deleteMod(mod);
            if (result)
            {
                this.updateTreeview();
            } else
            {
                MessageBox.Show("Error while deleting mod " + mod.Name, "Mod delete error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void installPackageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool result = manager.installMod((string)treeView1.SelectedNode.Tag);
            if (result)
            {
                this.updateTreeview();
            }
            else
            {
                MessageBox.Show("Error while installing mod " + treeView1.SelectedNode.Tag, "Mod install error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            manager.deletePackageFile((string)treeView1.SelectedNode.Tag);
            this.updateTreeview();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void triggerDownloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PublishedFileId id = new PublishedFileId();
            id.Value = ulong.Parse((string)treeView1.SelectedNode.Tag);
            steam.triggerDownload(id);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WebClient test = new WebClient();
            String content = test.DownloadString("http://localhost:8080/signatures");
            dynamic signatures = JsonConvert.DeserializeObject(content);

            dynamic mod = signatures.mod;

            JArray fileSignatures = signatures.signatures;


            // Check rFactor 2 for mods suitable to the BaseSignatur

            foreach(JObject foundMod in fileSignatures)
            {
                String name = (String)foundMod.GetValue("Name");
                Boolean isVehicle = (int)foundMod.GetValue("Type") == 2;

                String path = "Installed\\" + (isVehicle ? "Vehicles" : "Locations") + "\\" + name;
                List<String> foldersToKeep = new List<string>();
                if (foundMod.ContainsKey("BaseSignature"))
                {
                    String rootPath = @"F:\Steam\steamapps\common\rFactor 2\" + path;
                    String baseSignature = (String)foundMod.GetValue("BaseSignature");
                    String overallBaseSignature = null;
                    Boolean noParent = false;
                    do
                    {
                        foreach (string file in Directory.EnumerateFiles(rootPath, "*.mft", SearchOption.AllDirectories))
                        {

                            Dictionary<string, object> iniContent = this.manager.parseManifest(file);
                            string modSignature = (string)iniContent.GetValueOrDefault("Signature");

                            String parentPath = Directory.GetParent(file).FullName;

                            if (modSignature == baseSignature)
                            {
                                // The found manifest features the wanted
                                foldersToKeep.Add(parentPath);
                            }

                            // TODO if the desired mod has a parent -> search for dir

                            if ((string)iniContent.GetValueOrDefault("BaseSignature") != null)
                            {
                                overallBaseSignature = (string)iniContent.GetValueOrDefault("BaseSignature");
                            } else
                            {
                                noParent = true;
                            }
                        }
                    }
                    while (overallBaseSignature == null && !noParent) ;
                }
            }

        }
    }
}
