using System.Net;
using System.Text;
using System.Xml.Linq;
using AnodyneArchipelago.Entities;
using AnodyneArchipelago.Helpers;
using AnodyneSharp.Entities;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Logging;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Patches
{
    public class EntityPatches
    {
        private XDocument Document;
        private XElement root;
        private Dictionary<LocationType, Dictionary<RegionID, XElement[]>> entity_cache = new();

        public EntityPatches(Stream s)
        {
            Document = XDocument.Load(s);
            root = Document.Root!;
        }

        public Stream Get()
        {
            return new MemoryStream(Encoding.Default.GetBytes(Document.ToString()));
        }

        public Dictionary<RegionID, XElement[]> GetCache(LocationType type, string lookup)
        {
            if (entity_cache.TryGetValue(type, out var result))
            {
                return result;
            }
            result = root!.Elements("map")
                .Where(m => m.Element(lookup) != null)
                .ToDictionary(m => Enum.Parse<RegionID>((string)m.Attribute("name")!), m => m.Elements(lookup).ToArray());
            entity_cache[type] = result;
            return result;
        }

        private Guid GenID(long location_id, byte gen = 0)
        {
            byte[] bytes = new byte[16];

            Encoding.ASCII.GetBytes("AnoArch").CopyTo(bytes, 0);

            bytes[7] = gen;

            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(location_id)).CopyTo(bytes, 8);

            return new Guid(bytes);
        }

        private XElement GetByID(Guid id)
        {
            return root.Descendants().Where(e => e.Name != "map" && (Guid)e.Attribute("guid")! == id).First();
        }

        public void SetDust(Location location)
        {
            var node = GetCache(LocationType.Dust, "Dust")[location.Region][location.Index];
            node.Name = nameof(DustAP);
            node.SetAttributeValue("type", location.ID.ToString());
        }

        public void RemoveNexusBlockers()
        {
            root!.Elements("map")
                        .Where(m => (string)m.Attribute("name")! == "NEXUS")
                        .SelectMany(m => m.Elements("Gate").Concat(m.Elements("Button")))
                        .Remove();
        }

        public void FixHotelSoftlock()
        {
            var node = GetByID(new("CFE9CC42-595F-EDED-6720-90848DB38C19"));
            node.SetAttributeValue("x", 552);
            node.SetAttributeValue("y", 118);
        }

        public void OpenSmallKeyGates()
        {
            root.Descendants("KeyBlock").Where(k => (int)k.Attribute("frame")! == 0).Remove();
        }

        public void MakeKeyRingGates()
        {
            var gates = root.Descendants("KeyBlock").Where(k => (int)k.Attribute("frame")! == 0 && (string)k.Parent!.Attribute("name")! != "BOSSRUSH");

            foreach (var gate in gates)
            {
                gate.Name = "KeyRingBlock";
            }
        }

        public void OpenBigKeyGates()
        {
            root.Descendants("KeyBlock").Where(k => (int)k.Attribute("frame")! > 0).Remove();
        }

        public void LockMiao()
        {
            var node = root.Descendants("Trade_NPC").Where(k => (int)k.Attribute("frame")! == 0).First();
            node.Name = "MiaoAP";
        }

        public void RemoveMitraCutscenes()
        {
            root.Descendants("Mitra").Where(m => (string)m.Parent!.Attribute("name")! == "CLIFF").First().Remove();
            root.Descendants("Mitra").Where(m => (string)m.Parent!.Attribute("name")! == "OVERWORLD").First().Remove();
        }

        public void ForestChestJoke()
        {
            var node = GetByID(new("737247BF-3343-677C-0A6D-0B8C4AF030D9"));
            node.Name = nameof(JokeTreasureChest);
        }

        public IEnumerable<Guid> GetSages()
        {
            return root.Descendants("Sage").Select(s => (Guid)s.Attribute("guid")!);
        }

        void SetColorPuzzleNotifier(Vector2 location, string map)
        {
            var mapNode = root.Elements().Where(m => (string)m.Attribute("name")! == map).First();
            mapNode.Add(new XElement(nameof(ColorPuzzleNotifier),
                new XAttribute("guid", GenID(map[0])),
                new XAttribute("x", location.X),
                new XAttribute("y", location.Y),
                new XAttribute("frame", 0),
                new XAttribute("p", 2)
                )
            );
        }

        public void SetColorPuzzle(ColorPuzzle puzzle)
        {
            Point circusPoint = puzzle.CircusPos;
            Point hotelPoint = puzzle.HotelPos;
            Point apartmentPoint = puzzle.ApartmentPos;
            string typeval = $"{circusPoint.X},{circusPoint.Y};{hotelPoint.X},{hotelPoint.Y};{apartmentPoint.X},{apartmentPoint.Y};1,1";

            GetByID(new("ED2195E9-9798-B9B3-3C15-105C40F7C501")).SetAttributeValue("type", typeval);

            SetColorPuzzleNotifier(new Vector2(circusPoint.X + 72, circusPoint.Y + 11) * 16, "CIRCUS");
            SetColorPuzzleNotifier(new Vector2(hotelPoint.X + 73, hotelPoint.Y + 113) * 16, "HOTEL");
            SetColorPuzzleNotifier(new Vector2(apartmentPoint.X + 82, apartmentPoint.Y + 51) * 16, "APARTMENT");
        }

        public void SetBigKey(Location location)
        {
            var node = root.Descendants("NPC")
                .Where(n => (string)n.Parent!.Attribute("name")! == location.Region.ToString()
                    && (string)n.Attribute("type")! == "big_key")
                .First();
            node.Name = nameof(FreeStandingAP);
            node.SetAttributeValue("type", location.ID.ToString());
            node.SetAttributeValue("p", 2);
        }

        public void SetTentacle(Location location)
        {
            var node = GetCache(location.Type, nameof(Red_Pillar))[location.Region][location.Index];
            node.Name = nameof(FreeStandingAP);
            node.SetAttributeValue("type", location.ID.ToString());
            node.SetAttributeValue("p", 2);
        }

        public void SetCicada(Location location)
        {
            var node = GetCache(location.Type, "Health_Cicada")[location.Region][location.Index];
            if (node.Parent!.Elements("Event").Where(e => (string)e.Attribute("type")! == "entrance").Any())
            {
                node.Name = nameof(BossItemAP);
            }
            else
            {
                node.Name = nameof(FreeStandingAP);
            }
            node.SetAttributeValue("type", location.ID.ToString());
            node.SetAttributeValue("p", 2);
        }

        public void SetTreasureChest(Location location)
        {
            var node = GetCache(location.Type, "Treasure")[location.Region][location.Index];
            node.AddBeforeSelf(
                new XElement(
                    nameof(ChestAPInserter),
                    new XAttribute("guid", GenID(location.ID)),
                    new XAttribute("type", location.ID.ToString()),
                    new XAttribute("frame", 0),
                    new XAttribute("x", node.Attribute("x")!.Value),
                    new XAttribute("y", node.Attribute("y")!.Value),
                    new XAttribute("p", 2)
                    )
                );
        }

        public void SetWindmillCheck(Location location)
        {
            var map = root.Elements().Where(m => (string)m.Attribute("name")! == "WINDMILL").First();
            var node = new XElement(nameof(WindmillCheckAP),
                    new XAttribute("guid", GenID(location.ID)),
                    new XAttribute("x", 192),
                    new XAttribute("y", 368),
                    new XAttribute("frame", 0),
                    new XAttribute("p", 2)
                );
            map.Add(
                node
            );
            foreach (var n in root.Descendants("Dungeon_Statue"))
            {
                n.Name = "DungeonStatueAP";
            }
        }

        public XElement GetNexuspad(RegionID map)
        {
            return root.Descendants("Door").Where(p => (string)p.Parent!.Attribute("name")! == map.ToString() && (string)p.Attribute("type")! == "16").First();
        }

        public void FixHappyNexusPad()
        {
            var nexusPad = GetNexuspad(RegionID.HAPPY);
            nexusPad.SetAttributeValue("x", (int)nexusPad.Attribute("x")! + 72);
            nexusPad.SetAttributeValue("y", (int)nexusPad.Attribute("y")! - 16);
        }

        public Guid SetNexusPad(RegionID map, long id)
        {
            var nexusPad = GetNexuspad(map);

            nexusPad.SetAttributeValue("p", 2); //Make sure it despawns

            nexusPad.AddAfterSelf(
                new XElement(nameof(FreeStandingAP),
                        new XAttribute("guid", GenID(id)),
                        new XAttribute("type", id.ToString()),
                        new XAttribute("frame", 0),
                        new XAttribute("x", (int)nexusPad.Attribute("x")! + 8),
                        new XAttribute("y", (int)nexusPad.Attribute("y")! + 8),
                        new XAttribute("p", 2)
                    ),
                new XElement(nameof(InactiveNexusPad),
                        new XAttribute("guid", GenID(id, 1)),
                        new XAttribute("type", id.ToString()),
                        new XAttribute("frame", 0),
                        new XAttribute("x", (int)nexusPad.Attribute("x")!),
                        new XAttribute("y", (int)nexusPad.Attribute("y")!),
                        new XAttribute("p", 0)
                    )
                );

            return (Guid)nexusPad.Attribute("guid")!;
        }

        public void SetBoxTradeCheck(long id)
        {
            var node = root.Descendants("Trade_NPC").Where(e => (int)e.Attribute("frame")! == 2).First();
            node.AddAfterSelf(
                new XElement(
                    nameof(TradeQuestStarterAP),
                    new XAttribute("guid", GenID(id)),
                        new XAttribute("frame", 0),
                        new XAttribute("x", (int)node.Attribute("x")!),
                        new XAttribute("y", (int)node.Attribute("y")!),
                        new XAttribute("p", 2)
                    )
                );
        }

        public void SetAllCardsVictory()
        {
            root.Descendants("Console").Where(c => (string)c.Parent!.Attribute("name")! == "BLANK" && (int)c.Attribute("frame")! == 1).First().Name = nameof(BlankConsoleAP);
        }

        public void SetMitraTradeCheck()
        {
            var node = root.Descendants("Mitra").Where(m => (string)m.Parent!.Attribute("name")! == "FIELDS").First();
            node.Name = nameof(MitraTradeQuestAP);
        }

        public void SetShopkeepTradeCheck()
        {
            var node = root.Descendants("Trade_NPC").Where(e => (int)e.Attribute("frame")! == 3).First();
            node.Name = nameof(ShopKeepAP);
        }

        public void SetBigGateReq(Guid id, string value)
        {
            var node = GetByID(id);
            if (value.EndsWith("key"))
            {
                node.Name = "KeyBlock";
                List<string> indices = ["blue_key", "green_key", "red_key"];
                node.SetAttributeValue("frame", indices.IndexOf(value) + 1);
            }
            else if (value.StartsWith("cards_"))
            {
                node.Name = "CardGate";
                node.SetAttributeValue("frame", value.Split('_')[1]);
            }
            else if (value.StartsWith("bosses_"))
            {
                node.Name = nameof(BigBossGate);
                node.SetAttributeValue("frame", value.Split('_')[1]);
            }
            else if (value == "unlocked")
            {
                node.Remove();
            }
        }

        internal void PatchHappyAndBlue()
        {
            //Go Happy blocker
            root.Descendants("NPC").Where(e => (int)e.Attribute("frame")! == 11 && (string)e.Attribute("type")! == "generic").First().Remove();

            //Blue gate
            GetByID(new("FCADDA8E-CF50-3724-2724-C845AE08CEE2")).SetAttributeValue("frame", 5);

            //Blue switches
            GetByID(new("796D6CE8-0913-6746-4380-A3A983BA8ECA")).Remove();
            GetByID(new("2CC68767-D79C-7E4B-2941-BCE76BAA8198")).Remove();

            //Go cutscenes
            GetByID(new("EB109441-3637-BFA7-0E9A-92B1EBD0A20C")).Remove();
        }

        public void SetBlueHappyReward(Location location)
        {
            XElement node = root!.Elements("map")
                        .Where(m => (string)m.Attribute("name")! == location.Region.ToString())
                        .First().Element("Shadow_Briar")!;

            node.Name = nameof(FreeStandingAP);
            node.SetAttributeValue("type", location.ID.ToString());
            node.SetAttributeValue("p", 2);

            switch (location.Region)
            {
                case RegionID.BLUE:
                    node.SetAttributeValue("x", 5 * 16);
                    node.SetAttributeValue("y", 2 * 16);
                    break;
                case RegionID.HAPPY:
                    node.SetAttributeValue("x", 41 * 16);
                    node.SetAttributeValue("y", 11 * 16);
                    break;
                default:
                    DebugLogger.AddError($"Found Shadow Briar in unexpected region '{location.Region}'!");
                    return;
            }
        }
    }
}
