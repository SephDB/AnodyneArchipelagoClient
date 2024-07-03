using AnodyneSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AnodyneArchipelago.Patches
{
    public static class EntityPatches
    {
        public static Stream Patch(Stream s)
        {
            using StreamReader reader = new(s);
            XmlDocument xmlDocument = new();
            xmlDocument.LoadXml(reader.ReadToEnd());

            return new MemoryStream(Encoding.Default.GetBytes(xmlDocument.InnerXml));
        }
    }
}
