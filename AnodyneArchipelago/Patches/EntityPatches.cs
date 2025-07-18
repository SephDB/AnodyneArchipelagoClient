﻿using System.Net;
using System.Text;
using System.Xml.Linq;
using AnodyneArchipelago.Entities;
using AnodyneSharp.Entities.Gadget;
using AnodyneSharp.Logging;
using Microsoft.Xna.Framework;

namespace AnodyneArchipelago.Patches
{
    public class EntityPatches
    {
        private XDocument Document;
        private XElement root;
        private long? DustStartID;
        private List<XElement> Dusts = [];

        public EntityPatches(Stream s, long? dustStartID = null)
        {
            Document = XDocument.Load(s);
            root = Document.Root!;
            DustStartID = dustStartID;
            if (DustStartID != null)
            {
                Dusts = [.. root.Descendants("Dust")];
            }
        }

        public Stream Get()
        {
            return new MemoryStream(Encoding.Default.GetBytes(Document.ToString()));
        }

        private Guid GetID(int location_id, byte gen = 0)
        {
            byte[] bytes = new byte[16];

            Encoding.ASCII.GetBytes("Archipelago").CopyTo(bytes, 0);

            bytes[11] = gen;

            BitConverter.GetBytes(IPAddress.HostToNetworkOrder(location_id)).CopyTo(bytes, 12);

            return new Guid(bytes);
        }

        private XElement GetByID(Guid id)
        {
            return root.Descendants().Where(e => e.Name != "map" && (Guid)e.Attribute("guid")! == id).First();
        }

        public bool IsDustID(long location_id)
        {
            if (DustStartID == null)
            {
                return false;
            }
            return location_id >= DustStartID.Value;
        }

        public void SetDust(long location_id, string location_name)
        {
            var node = Dusts[(int)(location_id - DustStartID!.Value)];
            node.Name = nameof(DustAP);
            node.SetAttributeValue("type", location_name);
            LogLocation(node, location_name, (int)location_id);
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
                new XAttribute("guid", GetID(map[0])),
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

        private static void LogLocation(XElement element, string location, int id = 0)
        {
#if DEBUG
            DebugLogger.AddInfo($"{id} {(int)element.Attribute("x")! + 8,4} {(int)element.Attribute("y")! + 8,4} {location}");
#endif
        }

        public void SetFreeStanding(Guid guid, string location, int id)
        {
            var node = GetByID(guid);
            node.Name = nameof(FreeStandingAP);
            node.SetAttributeValue("type", location);
            node.SetAttributeValue("p", 2);
            LogLocation(node, location, id);
        }

        public void SetCicada(Guid guid, string location)
        {
            var node = GetByID(guid);
            if (node.Parent!.Elements("Event").Where(e => (string)e.Attribute("type")! == "entrance").Any())
            {
                node.Name = nameof(BossItemAP);
            }
            else
            {
                node.Name = nameof(FreeStandingAP);
            }
            node.SetAttributeValue("type", location);
            node.SetAttributeValue("p", 2);
            LogLocation(node, location);
        }

        public void SetTreasureChest(Guid guid, string location, int id)
        {
            var node = GetByID(guid);
            node.AddBeforeSelf(
                new XElement(
                    nameof(ChestAPInserter),
                    new XAttribute("guid", GetID(id)),
                    new XAttribute("type", location),
                    new XAttribute("frame", 0),
                    new XAttribute("x", node.Attribute("x")!.Value),
                    new XAttribute("y", node.Attribute("y")!.Value),
                    new XAttribute("p", 2)
                    )
                );
            LogLocation(node, location);
        }

        public void SetWindmillCheck(int id)
        {
            var map = root.Elements().Where(m => (string)m.Attribute("name")! == "WINDMILL").First();
            var node = new XElement(nameof(WindmillCheckAP),
                    new XAttribute("guid", GetID(id)),
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
            LogLocation(node, "Windmill - Activation");
        }

        public XElement GetNexuspad(string map)
        {
            return root.Descendants("Door").Where(p => (string)p.Parent!.Attribute("name")! == map && (string)p.Attribute("type")! == "16").First();
        }

        public void FixHappyNexusPad()
        {
            var nexusPad = GetNexuspad("HAPPY");
            nexusPad.SetAttributeValue("x", (int)nexusPad.Attribute("x")! + 72);
            nexusPad.SetAttributeValue("y", (int)nexusPad.Attribute("y")! - 16);
        }

        public Guid SetNexusPad(string locationName, int id)
        {
            string map = ArchipelagoManager.GetNexusGateMapName(locationName[..^11]);

            var nexusPad = GetNexuspad(map);

            nexusPad.SetAttributeValue("p", 2); //Make sure it despawns

            nexusPad.AddAfterSelf(
                new XElement(nameof(FreeStandingAP),
                        new XAttribute("guid", GetID(id)),
                        new XAttribute("type", locationName),
                        new XAttribute("frame", 0),
                        new XAttribute("x", (int)nexusPad.Attribute("x")! + 8),
                        new XAttribute("y", (int)nexusPad.Attribute("y")! + 8),
                        new XAttribute("p", 2)
                    ),
                new XElement(nameof(InactiveNexusPad),
                        new XAttribute("guid", GetID(id, 1)),
                        new XAttribute("type", locationName),
                        new XAttribute("frame", 0),
                        new XAttribute("x", (int)nexusPad.Attribute("x")!),
                        new XAttribute("y", (int)nexusPad.Attribute("y")!),
                        new XAttribute("p", 0)
                    )
                );

            LogLocation(nexusPad, locationName);

            return (Guid)nexusPad.Attribute("guid")!;
        }

        public void SetBoxTradeCheck(int id)
        {
            var node = root.Descendants("Trade_NPC").Where(e => (int)e.Attribute("frame")! == 2).First();
            node.AddAfterSelf(
                new XElement(
                    nameof(TradeQuestStarterAP),
                    new XAttribute("guid", GetID(id)),
                        new XAttribute("frame", 0),
                        new XAttribute("x", (int)node.Attribute("x")!),
                        new XAttribute("y", (int)node.Attribute("y")!),
                        new XAttribute("p", 2)
                    )
                );
            LogLocation(node, "Fields - Cardboard Box");
        }

        public void SetAllCardsVictory()
        {
            root.Descendants("Console").Where(c => (string)c.Parent!.Attribute("name")! == "BLANK" && (int)c.Attribute("frame")! == 1).First().Name = nameof(BlankConsoleAP);
        }

        public void SetMitraTradeCheck()
        {
            var node = root.Descendants("Mitra").Where(m => (string)m.Parent!.Attribute("name")! == "FIELDS").First();
            node.Name = nameof(MitraTradeQuestAP);
            LogLocation(node, "Fields - Mitra Trade");
        }

        public void SetShopkeepTradeCheck()
        {
            var node = root.Descendants("Trade_NPC").Where(e => (int)e.Attribute("frame")! == 3).First();
            node.Name = nameof(ShopKeepAP);
            LogLocation(node, "Fields - Shopkeeper Trade");
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
    }
}
