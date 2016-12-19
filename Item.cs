using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemImportMultiThreaded
{
    public class Item
    {
        public string InventoryID { get; set; }
        public string Description { get; set; }
        public string Uom { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Weight { get; set; }
        public string SupplierName { get; set; }
        public string SupplierPartNo { get; set; }
    }
}
