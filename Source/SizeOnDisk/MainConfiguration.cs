﻿using SizeOnDisk.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SizeOnDisk
{
    [Serializable]
    [XmlRoot(sectionName)]
    public class MainConfiguration
    {
        [XmlArray("editors")]
        [XmlArrayItem("editor")]
        public DefaultEditorItem[] Editors { get; set; }

        private const string sectionName = "SizeOnDisk";

        private static MainConfiguration internalInstance;
        private static object _lock = new object();

        public static MainConfiguration Instance
        {
            get
            {
                if (internalInstance == null)
                {
                    lock (_lock)
                    {
                        if (internalInstance == null)
                        {
                            XmlSerializer serializer = new XmlSerializer(typeof(MainConfiguration), new XmlRootAttribute(sectionName));
                            object config = ConfigurationManager.GetSection(sectionName);

                            XmlReader reader = null;
                            if (config == null)
                            {
                                Stream stream = Application.GetResourceStream(new Uri("pack://application:,,,/SizeOnDisk;component/DefaultConfiguration.xml")).Stream;
                                reader = XmlReader.Create(stream);
                            }
                            else
                            {
                                reader = (config as XDocument).CreateReader();
                            }

                            using (reader)
                            {
                                internalInstance = serializer.Deserialize(reader) as MainConfiguration;
                            }
                        }
                    }
                }
                return internalInstance;
            }
        }
    }
}
