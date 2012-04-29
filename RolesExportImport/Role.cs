using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace RolesExportImport
{

    [Serializable]
    public class Role
    {
        private string name;
        private string extraData;
        private string description;
        private Guid guid;
        private bool isDynamic;
        private List<RoleItem> includes;
        private List<RoleItem> excludes;

        public Role()
        {
            this.name = string.Empty;
            this.extraData = string.Empty;
            this.description = string.Empty;
            this.guid = Guid.NewGuid();
            this.isDynamic = false;
            this.includes = new List<RoleItem>();
            this.excludes = new List<RoleItem>();
        }

        [XmlArray()]
        public List<RoleItem> Includes
        {
            get { return this.includes; }
            set { this.includes = value; }
        }

        [XmlArray()]
        public List<RoleItem> Excludes
        {
            get { return this.excludes; }
            set { this.excludes = value; }
        }


        [XmlAttribute()]
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        [XmlAttribute()]
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        [XmlAttribute()]
        public string ExtraData
        {
            get { return this.extraData; }
            set { this.extraData = value; }
        }

        [XmlAttribute()]
        public Guid Guid
        {
            get { return this.guid; }
            set { this.guid = value; }
        }

        [XmlAttribute()]
        public bool IsDynamic
        {
            get { return this.isDynamic; }
            set { this.isDynamic = value; }
        }
    }
}
