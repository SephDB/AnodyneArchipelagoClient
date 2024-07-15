﻿using Microsoft.Xna.Framework;
using System.Text;
using System.Xml.Linq;

namespace AnodyneArchipelago.Patches
{
    public class EntityPatches
    {
        private int CurrentID = 0;
        private XDocument Document;
        private XElement root;

        public EntityPatches(Stream s)
        {
            Document = XDocument.Load(s);
            root = Document.Root!;
        }

        public Stream Get()
        {
            return new MemoryStream(Encoding.Default.GetBytes(Document.ToString()));
        }

        private Guid NextID()
        {
            return new Guid(Encoding.ASCII.GetBytes($"Archipelago{CurrentID++:D5}"));
        }

        private XElement GetByID(Guid id)
        {
            return root.Descendants().Where(e => e.Name != "map" && (Guid)e.Attribute("guid")! == id).First();
        }

        public void RemoveNexusBlockers()
        {
            root!.Elements("map")
                        .Where(m => (string)m.Attribute("name")! == "NEXUS")
                        .SelectMany(m => m.Elements("Gate").Concat(m.Elements("Button")))
                        .Remove();
        }

        public void Set36CardRequirement(int required)
        {
            var gate = GetByID(new("C8CE6E18-CF07-180B-A550-9DC808A2F7E3"));
            gate.SetAttributeValue("frame", required.ToString());
        }

        public void OpenSmallKeyGates()
        {
            root.Descendants("KeyBlock").Where(k => (int)k.Attribute("frame")! == 0).Remove();
        }

        public void OpenBigKeyGates()
        {
            root.Descendants("KeyBlock").Where(k => (int)k.Attribute("frame")! > 0).Remove();
        }

        public void RemoveMitraCliff()
        {
            root.Descendants("Mitra").Where(m => (string)m.Parent!.Attribute("name")! == "CLIFF").First().Remove();
        }

        public void SetColorPuzzle(ColorPuzzle puzzle)
        {
            Point circusPoint = puzzle.CircusPos;
            Point hotelPoint = puzzle.HotelPos;
            Point apartmentPoint = puzzle.ApartmentPos;
            string typeval = $"{circusPoint.X},{circusPoint.Y};{hotelPoint.X},{hotelPoint.Y};{apartmentPoint.X},{apartmentPoint.Y};1,1";

            GetByID(new("ED2195E9-9798-B9B3-3C15-105C40F7C501")).SetAttributeValue("type",typeval);
        }

        public void SetFreeStanding(Guid guid, string location)
        {
            var node = GetByID(guid);
            node.Name = "FreeStandingAP";
            node.SetAttributeValue("type", location);
        }
    }
}
