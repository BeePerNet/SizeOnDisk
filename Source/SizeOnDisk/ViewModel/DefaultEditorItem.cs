using System;
using System.Xml.Serialization;

namespace SizeOnDisk.ViewModel
{
    [Serializable]
    public class DefaultEditorItem
    {
        [XmlAttribute("display")]
        public string Display { get; set; }

        [XmlAttribute("definition")]
        public DefaultEditorDefinitionType Definition { get; set; }

        [XmlAttribute("parameter1")]
        public string Parameter1 { get; set; }

        [XmlAttribute("parameter2")]
        public string Parameter2 { get; set; }
    }
}
