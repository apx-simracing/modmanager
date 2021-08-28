using System;
using System.Collections.Generic;
using System.Text;

namespace libAPX
{
    public class APXServer
    {
        public int Port { get; set; }
        public string Host { get; set; }
        public string Name { get; set; }
        public string Session { get; set; }
        public string Track { get; set; }

        public string RecieverUrl { get; set; }

        public List<String> Vehicles { get; set; }


        public APXServer()
        {
            this.Vehicles = new List<string>();
        }
    }
}
