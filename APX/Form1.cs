using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using libAPX;
using Steamworks;
using Steamworks.Data;

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

            steam.getSubscriptions(steamInventoryCallback);
            steam.getServers(steamServersCallback);

        }

        private void steamInventoryCallback(InventoryItem[] items)
        {
            foreach(InventoryItem item in items)
            {
                string[] row = new string[] { item.Id.ToString(), item.Def.Name };
                dataGridView1.Rows.Add(row);
            }
        }

        private void steamServersCallback(ServerInfo item)
        {
            string[] row = new string[] { item.Name};
            dataGridView2.Rows.Add(row);
        }

        private void updateTreeview()
        {
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
    }
}
