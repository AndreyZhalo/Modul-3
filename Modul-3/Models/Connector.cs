using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modul_3.Models
{
    public class Connector
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
        public Dictionary<int, string> Contacts { get; set; } = new Dictionary<int, string>();
    }
}
