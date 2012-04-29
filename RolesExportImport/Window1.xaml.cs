using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using URM = SourceCode.Security.UserRoleManager.Management;
using SourceCode.Hosting.Client;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using SourceCode.Hosting.Client.BaseAPI;


namespace RolesExportImport
{

    public partial class Window1 : System.Windows.Window
    {
        enum ButtonStatus
        {
            SelectServer,
            ServerSelected,
            EmptyFileSelected,
            FileSelected
        }


        private string k2ConnectionString = string.Empty;

        public Window1()
        {
            InitializeComponent();
        }

        private string ConnectionString
        {
            get
            {
                string selectedServer = cmbServers.SelectedValue as string;

                if (string.IsNullOrEmpty(selectedServer))
                {
                    throw new ApplicationException("We need a connection, but no server is selected. Should not happen!");
                }
                string[] server = selectedServer.Split(':');

                //TODO: add some parsing on the port, etc.s

                SCConnectionStringBuilder conBuilder = new SCConnectionStringBuilder();
                conBuilder.Port = uint.Parse(server[1]);
                conBuilder.Host = server[0];
                conBuilder.Integrated = true;
                conBuilder.IsPrimaryLogin = true;

                return conBuilder.ConnectionString;
            }
        }


        private void ServerSelected(object sender, EventArgs args)
        {
            SetButtonStatus(ButtonStatus.ServerSelected);
            WriteLog("Server selected, select a file for import/export");
        }

        public void FileLocationClicked(object sender, EventArgs e)
        {
            FileDialog fd = new OpenFileDialog();
            fd.CheckFileExists = false;
            fd.ShowDialog();

            txtFileLocation.Text = fd.FileName;
            if (fd.FileName != string.Empty)
            {
                SetButtonStatus(ButtonStatus.FileSelected);
            }
        }


        public void ExportClicked(object sender, EventArgs e)
        {
            WriteLog("exporting");
            List<Role> roles = new List<Role>();
            URM.UserRoleManager urmServer = new URM.UserRoleManager();
            WriteLog("exporting 2");
            using (urmServer.CreateConnection())
            {
                WriteLog("exporting 3");
                urmServer.Connection.Open(ConnectionString);
                WriteLog("Connected to K2 server");

                string[] serverRoles = urmServer.GetRoleNameList();

                foreach (string role in serverRoles)
                {
                    WriteLog("Exporting role {0}", role);
                    URM.Role urmRole = urmServer.GetRole(role);
                    Role r = this.CopyToLocalRole(urmRole);
                    roles.Add(r);
                }

                WriteLog("Writing {0} roles to XML file", roles.Count);

                XmlSerializer xmlSer = new XmlSerializer(roles.GetType());
                XmlTextWriter writer = new XmlTextWriter(txtFileLocation.Text, Encoding.Unicode);
                writer.Formatting = Formatting.Indented;
                xmlSer.Serialize(writer, roles);
                writer.Flush();
                writer.Close();


                WriteLog("Closing K2 connection.");
            }
        }



        public void ImportClicked(object sender, EventArgs e)
        {
            WriteLog("Reading XML file");
            XmlSerializer xmlSer = new XmlSerializer(typeof(List<Role>));
            XmlTextReader reader = new XmlTextReader(txtFileLocation.Text);
            List<Role> roles = (List<Role>)xmlSer.Deserialize(reader);
            reader.Close();
            WriteLog("Read {0} roles. Starting import.", roles.Count);

            URM.UserRoleManager urmServer = new URM.UserRoleManager();
            using (urmServer.CreateConnection())
            {
                urmServer.Connection.Open(ConnectionString);
                WriteLog("Connected to K2 server");

                foreach (Role importRole in roles)
                {
                    WriteLog("Importing {0}", importRole.Name);
                    URM.Role editRole = this.CopyToURMRole(importRole);
                    if (urmServer.GetRole(editRole.Name) != null)
                    {
                        urmServer.UpdateRole(editRole);
                    }
                    else
                    {
                        urmServer.CreateRole(editRole);
                    }
                }

                WriteLog("Closing K2 connection.");
            }
        }

        /// <summary>
        /// This function takes the K2 URM role and copies it to a local object that is serializable.
        /// This also invokes the GetRolesItems on the includes and excludes.
        /// </summary>
        /// <param name="urmRole">The K2 URM role to copy.</param>
        /// <returns>The local role</returns>
        private Role CopyToLocalRole(URM.Role urmRole)
        {
            Role r = new Role();
            r.Description = urmRole.Description;
            r.ExtraData = urmRole.ExtraData;
            r.Guid = urmRole.Guid;
            r.IsDynamic = urmRole.IsDynamic;
            r.Name = urmRole.Name;

            r.Includes = this.CopyRoleItems(urmRole.Include);
            r.Excludes = this.CopyRoleItems(urmRole.Exclude);
            return r;
        }

        /// <summary>
        /// This function copies the local Roles to URM roles. This is a little different from the CopyToLocalRole method since URM.Role.Include/Exclude is readonly.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        private URM.Role CopyToURMRole(Role role)
        {
            URM.Role r = new URM.Role();
            r.Description = role.Description;
            r.ExtraData = role.ExtraData;
            r.Guid = role.Guid;
            r.IsDynamic = role.IsDynamic;
            r.Name = role.Name;


            SetRoleItems(r.Include, role.Includes);
            SetRoleItems(r.Exclude, role.Excludes);

            return r;
        }



        private void SetRoleItems(URM.RoleItemCollection<URM.Role, URM.RoleItem> urmRoleItems, List<RoleItem> roleItems)
        {
            foreach (RoleItem roleItem in roleItems)
            {
                if (roleItem is UserRoleItem)
                {
                    UserRoleItem userRoleItem = (UserRoleItem)roleItem;
                    URM.UserItem urmUserItem = new URM.UserItem();

                    urmUserItem.ExtraData = userRoleItem.ExtraData;
                    urmUserItem.Name = userRoleItem.Name;

                    urmRoleItems.Add(urmUserItem);
                }
                else if (roleItem is GroupRoleItem)
                {
                    GroupRoleItem groupRoleItem = (GroupRoleItem)roleItem;
                    URM.GroupItem urmGroupItem = new URM.GroupItem();

                    urmGroupItem.ExtraData = groupRoleItem.ExtraData;
                    urmGroupItem.Name = groupRoleItem.Name;

                    urmRoleItems.Add(urmGroupItem);
                }
                else
                {
                    throw new NotSupportedException("SmartObject RoleItems aren't supported.");
                }
            }
        }

        private List<RoleItem> CopyRoleItems(URM.RoleItemCollection<URM.Role, URM.RoleItem> urmRoles)
        {
            List<RoleItem> roles = new List<RoleItem>();

            foreach (URM.RoleItem urmRoleItem in urmRoles)
            {
                //TODO: check if this can be refactored to something simpler since UserItem and GroupItem have the same properties.
                if (urmRoleItem is URM.UserItem)
                {
                    URM.UserItem urmUserItem = (URM.UserItem)urmRoleItem;
                    UserRoleItem uri = new UserRoleItem();

                    uri.ExtraData = urmUserItem.ExtraData;
                    uri.Name = urmUserItem.Name;

                    roles.Add(uri);
                }
                else if (urmRoleItem is URM.GroupItem)
                {
                    URM.GroupItem urmGroupItem = (URM.GroupItem)urmRoleItem;
                    GroupRoleItem gri = new GroupRoleItem();

                    gri.ExtraData = urmGroupItem.ExtraData;
                    gri.Name = urmGroupItem.Name;

                    roles.Add(gri);
                }
                else
                {
                    throw new NotSupportedException("SmartObject RoleItems aren't supported.");
                }
            }
            return roles;
        }


        private void Window_Initialized(object sender, EventArgs e)
        {
            PopulateServers();
        }
        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            PopulateServers();
        }


        private void SetButtonStatus(ButtonStatus status)
        {
            txtFileLocation.IsEnabled = false;
            btnFileLocation.IsEnabled = false;
            btnImport.IsEnabled = false;
            btnExport.IsEnabled = false;
            if (status == ButtonStatus.ServerSelected || status == ButtonStatus.FileSelected)
            {
                txtFileLocation.IsEnabled = true;
                btnFileLocation.IsEnabled = true;
            }

            if (status == ButtonStatus.EmptyFileSelected)
            {
                btnExport.IsEnabled = true;
            }

            if (status == ButtonStatus.FileSelected)
            {
                btnExport.IsEnabled = true;
                btnImport.IsEnabled = true;
            }

        }


        private void PopulateServers()
        {
            WriteLog("Searching for K2 servers...");

            // Discovery port for server installs - 49599
            List<string> servers = SourceCode.Hosting.Client.BaseAPI.DiscoverHost.Seek(49599, 1000);
            // Discovery port for farm installs - 49600
            servers.AddRange(SourceCode.Hosting.Client.BaseAPI.DiscoverHost.Seek(49600, 1000));

            cmbServers.Items.Clear();
            foreach (string server in servers)
            {
                cmbServers.Items.Add(server.Replace('|', ':'));
            }
            WriteLog("Found {0} servers. Select a server.", servers.Count);

            SetButtonStatus(ButtonStatus.SelectServer);
        }


        private void WriteLog(string str, params object[] arg)
        {
            string message = str;
            if (arg.Length > 0)
            {
                message = string.Format(str, arg);
            }
            message = string.Format("[{0}] {1}", DateTime.Now.ToShortTimeString(), message);

            lstLog.Items.Add(message);

        }
    }
}