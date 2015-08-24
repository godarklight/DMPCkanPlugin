using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Windows.Forms;
using CKAN;

namespace CkanPlugin
{
    public class CkanPlugin : CKAN.IGUIPlugin
    {
        private readonly string VERSION = "v1.0.0";
        private ListBox listBox;
        private Button connectButton;
        private Button syncButton;
        private StatusBar statusBar;
        private string playerName = "Player";
        private string selectedServer = "127.0.0.1:6702";
        private List<DMPServerInfo> dmpServers = new List<DMPServerInfo>();

        public override void Initialize()
        {
            SetupForm();
            LoadDMPInfo();
        }

        private void SetupForm()
        {
            TabPage tabPage = new TabPage();
            tabPage.Name = "DMPTabPage";
            tabPage.Text = "DMP";

            listBox = new ListBox();
            listBox.Parent = tabPage;
            listBox.Dock = DockStyle.Fill;
            listBox.SelectedValueChanged += ChangeSelection;

            FlowLayoutPanel buttonLayoutPanel = new FlowLayoutPanel();
            buttonLayoutPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonLayoutPanel.Parent = tabPage;
            buttonLayoutPanel.Dock = DockStyle.Bottom;

            syncButton = new Button();
            syncButton.Parent = buttonLayoutPanel;
            syncButton.Name = "DMPSyncButton";
            syncButton.Text = "Sync";
            syncButton.Click += Sync;
            syncButton.Enabled = false;

            connectButton = new Button();
            connectButton.Parent = buttonLayoutPanel;
            connectButton.Name = "DMPConnectButton";
            connectButton.Text = "Connect";
            connectButton.Click += Connect;
            connectButton.Enabled = false;

            buttonLayoutPanel.Controls.Add(syncButton);
            buttonLayoutPanel.Controls.Add(connectButton);

            statusBar = new StatusBar();
            statusBar.Parent = tabPage;
            statusBar.Name = "DMPStatusBar";
            statusBar.Text = "Ready";
            statusBar.Dock = DockStyle.Bottom;

            tabPage.Controls.Add(listBox);
            tabPage.Controls.Add(buttonLayoutPanel);
            tabPage.Controls.Add(statusBar);

            Main.Instance.m_TabController.m_TabPages.Add("DMPTabPage", tabPage);
            Main.Instance.m_TabController.ShowTab("DMPTabPage", 1, false);
        }

        private void ChangeSelection(object selected, EventArgs GUIargs)
        {
            Console.WriteLine(listBox.SelectedValue == null);
            if (listBox.SelectedValue == null)
            {
                Console.WriteLine("Everything is NOT ok");
                connectButton.Enabled = false;
                syncButton.Enabled = false;
                return;
            }
            connectButton.Enabled = true;
            syncButton.Enabled = true;
            DMPServerInfo dmpServerInfoSelected = (DMPServerInfo)listBox.SelectedValue;
            selectedServer = dmpServerInfoSelected.address + ":" + dmpServerInfoSelected.port;
            Console.WriteLine("Everything is ok?");
        }

        private void Sync(object sender, EventArgs GUIargs)
        {
            statusBar.Text = "Syncing";
        }

        private void Connect(object sender, EventArgs GUIargs)
        {
            statusBar.Text = "Connecting";
            //Stolens from CKAN!
            string[] split = Main.Instance.m_Configuration.CommandLineArguments.Split(' ');
            if (split.Length == 0)
            {
                return;
            }

            string binary = split[0];
            string args = string.Join(" ", split.Skip(1));
            args += " -dmp dmp://" + selectedServer;
            try
            {
                Directory.SetCurrentDirectory(Main.Instance.CurrentInstance.GameDir());
                Process.Start(binary, args);
            }
            catch (Exception exception)
            {
                Console.WriteLine("CKAN threw an error but for some reason the method is protected so I can't log it to CKAN's thing. Here we go anyway!");
                Console.WriteLine("Couldn't start KSP. {0}.", exception.Message);
                //GUI.user.RaiseError("Couldn't start KSP. {0}.", exception.Message);
            }
        }

        private void LoadDMPInfo()
        {
            string currentKSPDirectory = Main.Instance.CurrentInstance.GameDir();
            string currentDMPDataDirectory = Path.Combine(currentKSPDirectory, "GameData", "DarkMultiPlayer", "Plugins", "Data");
            string serversFile = Path.Combine(currentDMPDataDirectory, "servers.xml");
            string privateKeyFile = Path.Combine(currentDMPDataDirectory, "privatekey.txt");
            string publicKeyFile = Path.Combine(currentDMPDataDirectory, "privatekey.txt");
            if (!Directory.Exists(currentDMPDataDirectory) || !File.Exists(serversFile) || !File.Exists(privateKeyFile) || !File.Exists(publicKeyFile))
            {
                statusBar.Text = "DMP Not Installed!";
                return;
            }
            dmpServers.Clear();
            //Load XML entries
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(serversFile);
            playerName = xmlDocument.SelectSingleNode("/settings/global/@username").Value;
            XmlNodeList serverNodeList = xmlDocument.GetElementsByTagName("server");
            foreach (XmlNode xmlNode in serverNodeList)
            {
                DMPServerInfo newServer = new DMPServerInfo();
                newServer.name = xmlNode.Attributes["name"].Value;
                newServer.address = xmlNode.Attributes["address"].Value;
                Int32.TryParse(xmlNode.Attributes["port"].Value, out newServer.port);
                dmpServers.Add(newServer);
            }

            //Reload listbox
            listBox.ClearSelected();
            listBox.DataSource = dmpServers;
        }

        public override void Deinitialize()
        {
            Main.Instance.m_TabController.HideTab("DMPTabPage");
            Main.Instance.m_TabController.m_TabPages.Remove("DMPTabPage");
        }

        public override string GetName()
        {
            return "DMP";
        }

        public override CKAN.Version GetVersion()
        {
            return new CKAN.Version(VERSION);
        }
    }

    public class DMPServerInfo
    {
        public string name;
        public string address;
        public int port;

        public override string ToString()
        {
            return name;
        }
    }
}
