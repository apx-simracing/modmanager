using System;
using System.Collections.Generic;
using System.Text;

namespace libAPX
{
    public enum ModType: int
    {
        Locations = 1,
        Vehicles = 2,
        Sounds = 3,
        HUD = 4,
        Commentary = 6,
        Nations = 9,
        Showroom = 10,
    }
    public class Mod
    {
        public ModType Type { get; set; }
        public String Name { get; set; }
        public String Version { get; set; }
        public String BaseSignature { get; set; }
        public String Signature { get; set; }
        public List<Mod> Children { get; set; }
        public List<String> UsedBy { get; set; }

    }
}
