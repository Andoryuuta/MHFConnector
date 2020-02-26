using Microsoft.Win32;
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
using PSHostsFile;

namespace MHFConnector
{
    public partial class Form1 : Form
    {
        List<MhfManager> clients;

        public Form1()
        {
            clients = new List<MhfManager>();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: check the hosts file for an existing IP.
            hostTextBox.Text = "127.0.0.1";
        }

        private void launchButton_Click(object sender, EventArgs e)
        {
            // Resolved the entered host to an IPv4 address.
            IPAddress hostAddress = null;
            try
            {
                // Get first "InterNetwork" (IPv4) address.
                hostAddress = Dns.GetHostEntry(hostTextBox.Text).AddressList
                    .First(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK);
                return;
            }
            
            // Set the hosts in /etc/hosts file.
            string[] hosts = { "mhfg.capcom.com.tw", "mhf-n.capcom.com.tw", "cog-members.mhf-z.jp", "www.capcom-onlinegames.jp", "srv-mhf.capcom-networks.jp" };
            foreach (var host in hosts)
            {
                HostsFile.Set(host, hostAddress.ToString());
            }

            
            // Get MHF exec path from the registry.
            string execPath = "";
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\CAPCOM\\Monster Hunter Frontier Online"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("ExecPath");
                        if (o != null)
                        {
                            execPath = o as String;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK);
            }

            // Verify the path exists.
            if (!File.Exists(execPath))
            {
                MessageBox.Show(String.Format("File at: {0} doesn't exist.", execPath), "Error", MessageBoxButtons.OK);
                return;
            }
            
            // Finally create our client manager and launch the game.
            var client = new MhfManager(hostTextBox.Text, execPath);
            try
            {
                client.LaunchGame();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK);
                return;
            }

            // Add it to the list for later usage.
            clients.Add(client);

            MessageBox.Show("Multi-client hooked launcher started.", "MHF Connector", MessageBoxButtons.OK);
        }
    }
}
