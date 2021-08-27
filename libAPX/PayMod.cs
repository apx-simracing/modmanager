using System;
using System.Collections.Generic;
using System.Text;

namespace libAPX
{
    class PayMod
    {
        public string Name { get; set; }
        public string ID { get; set; }


        public PayMod(string name, string id)
        {
            this.Name = name;
            this.ID = id;
        }
    }
}
