using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemImportMultiThreaded
{
    public class ItemImporter
    {
        private IN202500.Screen _itemsScreen;
        private static object _itemsSchemaLock = new object();
        private static IN202500.Content _itemsSchema;
        
        public void Login(string url, string username, string password, string company)
        {
            Console.WriteLine("[{0}] Logging in to {1}...", System.Threading.Thread.CurrentThread.ManagedThreadId, url);

            _itemsScreen = new IN202500.Screen();
            _itemsScreen.Url = url + "/Soap/IN202500.asmx";
            _itemsScreen.EnableDecompression = true;
            _itemsScreen.CookieContainer = new System.Net.CookieContainer();
            _itemsScreen.Timeout = 36000;
            _itemsScreen.Login(username, password);

            Console.WriteLine("[{0}] Logged in to {1}.", System.Threading.Thread.CurrentThread.ManagedThreadId, url);
            
            lock (_itemsSchemaLock)
            {
                // Threads can share the same schema.
                if (_itemsSchema == null)
                {
                    Console.WriteLine("[{0}] Retrieving IN202500 schema...", System.Threading.Thread.CurrentThread.ManagedThreadId);
                    _itemsSchema = _itemsScreen.GetSchema();
                    if (_itemsSchema == null) throw new Exception("IN202500 GetSchema returned null. See AC-73433.");
                }
            }
        }

        public void Logout()
        {
            _itemsScreen.Logout();
        }

        public void Import(List<Item> items)
        {
            Console.WriteLine("[{0}] Submitting {1} items to Acumatica...", System.Threading.Thread.CurrentThread.ManagedThreadId, items.Count);

            var commands = new IN202500.Command[]
            {
                _itemsSchema.StockItemSummary.InventoryID,
                _itemsSchema.StockItemSummary.Description,
                _itemsSchema.GeneralSettingsUnitOfMeasureBaseUnit.BaseUnit,
                _itemsSchema.PriceCostInfoPriceManagement.DefaultPrice,
                _itemsSchema.PackagingDimensions.Weight,
                _itemsSchema.VendorDetails.VendorID,
                _itemsSchema.VendorDetails.VendorInventoryID,
                _itemsSchema.Actions.Save
            };

            string[][] data = new string[items.Count][];

            int count = 0;
            foreach(Item item in items)
            {
                data[count] = new string[7];
                data[count][0] = item.InventoryID;
                data[count][1] = item.Description.Trim();
                data[count][2] = item.Uom;
                data[count][3] = item.CurrentPrice.ToString(System.Globalization.CultureInfo.InvariantCulture);
                data[count][4] = item.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture);
                data[count][5] = item.SupplierName;
                data[count][6] = item.SupplierPartNo;
                count++;
            }

            _itemsScreen.Import(commands, null, data, false, true, true);

            Console.WriteLine("[{0}] Submitted {1} items to Acumatica.", System.Threading.Thread.CurrentThread.ManagedThreadId, items.Count);
        }
    }
}
