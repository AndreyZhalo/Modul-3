using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modul_3.Models
{
    public class Product
    {
        public string Name { get; set; }
        public string ConnectorsFolderPath { get; set; }
        public List<Connector> Connectors { get; set; } = new List<Connector>();
    }
}
