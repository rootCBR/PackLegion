using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackLegion.Files.XML
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false, ElementName = "Root")]
    public partial class PackInfo
    {

        private List<RootFile> commonField = new List<RootFile>();

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("File", IsNullable = false)]
        public List<RootFile> common
        {
            get
            {
                return this.commonField;
            }
            set
            {
                this.commonField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class RootFile
    {

        private string pathField;

        private ulong crcField;

        private uint filePositionField;

        private uint fileSizeField;

        private ulong fileTimeField;

        private uint fileCRCField;

        private string compressionField;

        private string fATsectionField;

        private uint originalFileSizeField;

        private bool originalFileSizeFieldSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Path
        {
            get
            {
                return this.pathField;
            }
            set
            {
                this.pathField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ulong Crc
        {
            get
            {
                return this.crcField;
            }
            set
            {
                this.crcField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint FilePosition
        {
            get
            {
                return this.filePositionField;
            }
            set
            {
                this.filePositionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint FileSize
        {
            get
            {
                return this.fileSizeField;
            }
            set
            {
                this.fileSizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ulong FileTime
        {
            get
            {
                return this.fileTimeField;
            }
            set
            {
                this.fileTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint FileCRC
        {
            get
            {
                return this.fileCRCField;
            }
            set
            {
                this.fileCRCField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Compression
        {
            get
            {
                return this.compressionField;
            }
            set
            {
                this.compressionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string FATsection
        {
            get
            {
                return this.fATsectionField;
            }
            set
            {
                this.fATsectionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public uint OriginalFileSize
        {
            get
            {
                return this.originalFileSizeField;
            }
            set
            {
                this.originalFileSizeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool OriginalFileSizeSpecified
        {
            get
            {
                return this.originalFileSizeFieldSpecified;
            }
            set
            {
                this.originalFileSizeFieldSpecified = value;
            }
        }
    }


}
