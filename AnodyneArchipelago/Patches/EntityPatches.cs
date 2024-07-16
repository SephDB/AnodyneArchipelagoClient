using AnodyneArchipelago.Entities;
using Microsoft.Xna.Framework;
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
            node.Name = nameof(FreeStandingAP);
            node.SetAttributeValue("type", location);
            node.SetAttributeValue("p", 2);
        }

        public void SetCicada(Guid guid, string location)
        {
            var node = GetByID(guid);
            if(node.Parent!.Elements("Event").Where(e=>(string)e.Attribute("type")! == "entrance").Any())
            {
                node.Name = nameof(BossItemAP);
            }
            else
            {
                node.Name = nameof(FreeStandingAP);
            }
            node.SetAttributeValue("type", location);
            node.SetAttributeValue("p", 2);
        }

        public void SetTreasureChest(Guid guid, string location)
        {
            var node = GetByID(guid);
            node.AddBeforeSelf(
                new XElement(
                    nameof(ChestAPInserter),
                    new XAttribute("guid", NextID()),
                    new XAttribute("type", location),
                    new XAttribute("frame", 0),
                    new XAttribute("x", node.Attribute("x")!.Value),
                    new XAttribute("y", node.Attribute("y")!.Value),
                    new XAttribute("p", 2)
                    )
                );
        }

        public void SetWindmillCheck()
        {
            var map = root.Elements().Where(m => (string)m.Attribute("name")! == "WINDMILL").First();
            map.Add(
                new XElement("WindmillCheckAP",
                    new XAttribute("guid",NextID()),
                    new XAttribute("x",192),
                    new XAttribute("y", 368),
                    new XAttribute("frame",0),
                    new XAttribute("p", 2)
                )
            );
        }

        public Guid SetNexusPad(string locationName)
        {
            string map = ArchipelagoManager.GetNexusGateMapName(locationName[..^11]);

            var nexusPad = root.Descendants("Door").Where(p => (string)p.Parent!.Attribute("name")! == map && (string)p.Attribute("type")! == "16").First();

            nexusPad.SetAttributeValue("p", 2); //Make sure it despawns

            nexusPad.AddAfterSelf(
                new XElement(nameof(FreeStandingAP),
                        new XAttribute("guid",NextID()),
                        new XAttribute("type", locationName),
                        new XAttribute("frame", 0),
                        new XAttribute("x", (int)nexusPad.Attribute("x")!+16),
                        new XAttribute("y", (int)nexusPad.Attribute("y")!+16),
                        new XAttribute("p", 2)
                    )
                );

            return (Guid)nexusPad.Attribute("guid")!;
        }
    }
}
