﻿using System.Configuration;
using System.Xml;
using System.Xml.Linq;

namespace SizeOnDisk
{
    public class MainConfigurationSection : ConfigurationSection
    {
        // This may be fetched multiple times: XmlReaders can't be reused, so load it into an XDocument instead
        private XDocument document;

        public MainConfigurationSection()
        {
        }

        protected override void DeserializeSection(XmlReader reader)
        {
            this.document = XDocument.Load(reader);
        }

        protected override object GetRuntimeObject()
        {
            // This is cached by ConfigurationManager, so no point in duplicating it to stop other people from modifying it
            return this.document;
        }
    }
}
