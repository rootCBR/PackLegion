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
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false, ElementName = "Config")]
    public partial class XmlConfig
    {

        private string originalCommonPathField;

        private string originalPatchPathField;

        private string filelistPathField;

        /// <remarks/>
        public string OriginalCommonPath
        {
            get
            {
                return this.originalCommonPathField;
            }
            set
            {
                this.originalCommonPathField = value;
            }
        }

        /// <remarks/>
        public string OriginalPatchPath
        {
            get
            {
                return this.originalPatchPathField;
            }
            set
            {
                this.originalPatchPathField = value;
            }
        }

        /// <remarks/>
        public string FilelistPath
        {
            get
            {
                return this.filelistPathField;
            }
            set
            {
                this.filelistPathField = value;
            }
        }
    }
}
