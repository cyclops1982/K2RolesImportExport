using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace RolesExportImport
{
    
    [Serializable]
    [XmlInclude(typeof(UserRoleItem))]
    [XmlInclude(typeof(GroupRoleItem))]
    public abstract class RoleItem
    {
        private string name;
        private string extraData;

        [XmlAttribute()]
        public string ExtraData
        {
            get { return extraData; }
            set { extraData = value; }
        }
	
        [XmlAttribute()]
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
    }
}
