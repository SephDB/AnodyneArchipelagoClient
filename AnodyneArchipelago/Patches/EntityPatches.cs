using AnodyneSharp.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AnodyneArchipelago.Patches
{
    public class EntityPatches
    {
        private int CurrentID = 0;
        private XmlDocument Doc;

        public EntityPatches(Stream s)
        {
            using StreamReader reader = new(s);
            Doc = new();
            Doc.LoadXml(reader.ReadToEnd());
        }

        public Stream Get()
        {
            return new MemoryStream(Encoding.Default.GetBytes(Doc.InnerXml));
        }

        private Guid NextID()
        {
            return new Guid(Encoding.ASCII.GetBytes($"Archipelago{CurrentID++:D5}"));
        }

        public void RemoveNexusBlockers()
        {
            var nodes = Doc.SelectNodes(@".//map[@name=""NEXUS""]/Gate | .//map[@name=""NEXUS""]/Button");
            foreach(XmlNode n in nodes!)
            {
                n.ParentNode!.RemoveChild(n);
            }
        }

        public void Set36CardRequirement(int required)
        {
            XmlNode gate = Doc.SelectSingleNode(@".//*[@guid=""C8CE6E18-CF07-180B-A550-9DC808A2F7E3""]")!;
            gate.Attributes!["frame"]!.Value = required.ToString();
        }

        public void OpenSmallKeyGates()
        {
            var nodes = Doc.SelectNodes(@".//KeyBlock[@frame=""0""]");
            foreach (XmlNode n in nodes!)
            {
                n.ParentNode!.RemoveChild(n);
            }
        }

        public void OpenBigKeyGates()
        {
            var nodes = Doc.SelectNodes(@".//KeyBlock[@frame>""0""] | .//NPC[@type=""big_key""]");
            foreach (XmlNode n in nodes!)
            {
                n.ParentNode!.RemoveChild(n);
            }
        }

        public void RemoveMitraCliff()
        {
            XmlNode mitra = Doc.SelectSingleNode(@".//map[@name=""CLIFF""]/Mitra")!;
            mitra.ParentNode!.RemoveChild(mitra);
        }

        public void SetColorPuzzle(ColorPuzzle puzzle)
        {
            Point circusPoint = puzzle.CircusPos;
            Point hotelPoint = puzzle.HotelPos;
            Point apartmentPoint = puzzle.ApartmentPos;
            string typeval = $"{circusPoint.X},{circusPoint.Y};{hotelPoint.X},{hotelPoint.Y};{apartmentPoint.X},{apartmentPoint.Y};1,1");

            XmlNode puzzleCheck = Doc.SelectSingleNode(@".//*[@guid=""ED2195E9-9798-B9B3-3C15-105C40F7C501""]")!;
            puzzleCheck.Attributes!["type"]!.Value = typeval;
        }
    }
}
