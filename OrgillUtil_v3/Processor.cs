using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using ExcelDataReader;
using System.Text;
using System.Diagnostics;

namespace OrgillUtil_v3
{
    public class Processor {
        private MainWindow window;
        private OrgillHandler handler;
        private OpenFileDialog load;
        private List<Product> products;
        private List<Product> outofstock;

        public Processor(MainWindow window, OrgillHandler handler) {
            this.window = window;
            this.handler = handler;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async void Start() {
            // set filter limit
            int limit = -1;
            while (limit == -1) {
                var dialog = new InputDialog("Limit to how many lines? (0 for no limit)", window.Title, 350);
                dialog.ShowDialog();
                if (dialog.Canceled) {
                    window.Close();
                    return;
                }
                limit = dialog.GetInt();
            }

            // input excel file
            load = new OpenFileDialog();
            load.Title = "Open orgill order file";
            load.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            load.Filter = "Excel File | *.xls";
            if (load.ShowDialog() != DialogResult.OK) {
                window.Close();
                return;
            }

            // read excel file
            if ((products = ReadExcelFile(load.FileName)) == null) return;
            window.println(products.Count + " products found. Estimated time: " + readTime((int)(products.Count * 2.5f)), Colors.CadetBlue);
            window.totalProgress.Value += 20;

            // Get Data
            outofstock = new List<Product>();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            await GatherData();
            window.totalProgress.Value += 40;
            sw.Stop();
            window.println("Total elapsed time: " + readTime(sw.Elapsed), Colors.Cyan);

            // Trim Data
            TrimProductList(limit, products, window);
            window.totalProgress.Value += 20;

            // Write to file
            writeToFile(products);
            File.WriteAllLines(generateFilename("OrgillWarehouseOuts", ".csv"), outofstock.Select(x => x.ToString()));
            window.totalProgress.Value += 20;

            new Dialog("Operation Complete; Please upload file to orgill.", "Completed").Show();
        }

        /******* Time Utility *******/
        public static string readTime(int seconds) {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return readTime(ts);
        }

        public static string readTime(TimeSpan ts) {
            return string.Format("[{0}:{1:00}:{2:00}]", ts.Hours, ts.Minutes, ts.Seconds);
        }
        /***** End Time Utility *****/

        /**
         * Parses excel file for relevant data
         */
        private List<Product> ReadExcelFile(string filepath) {
            try {
                using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read)) {
                    using (var reader = ExcelReaderFactory.CreateReader(stream)) {
                        var table = reader.AsDataSet().Tables[0];

                        // Get header indexes
                        int skuIndex = 2, upcIndex = 4, invIndex = 12, ordIndex = 13;
                        var arr = table.Rows[10].ItemArray;
                        for (int i = 0; i < arr.Length; i++) {
                            if (arr[i].ToString().Equals("SKU")) skuIndex = i;
                            if (arr[i].ToString().Contains("Product")) upcIndex = i;
                            if (arr[i].ToString().Equals("Inv")) invIndex = i;
                            if (arr[i].ToString().Equals("Ord Qty")) ordIndex = i;
                        }

                        // Generate List
                        List<Product> products = new List<Product>();
                        List<string> skipped = new List<string>();

                        window.currentProgress.IsIndeterminate = false;
                        window.currentProgress.Maximum = table.Rows.Count;
                        window.currentProgress.Value = 0;
                        window.println("Reading excel file...", Colors.CadetBlue);
                        
                        for (int i = 13; i < table.Rows.Count; i++) {
                            // exit loop if end of file
                            if (table.Rows[i][7].ToString().Contains("Record")) break;
                            // Row contains a upc number
                            if (long.TryParse(table.Rows[i][upcIndex].ToString(), out var upc))
                            {
                                // valid sku and on hand is numeric
                                if (int.TryParse(table.Rows[i][skuIndex].ToString(), out var sku) &&
                                    sku.ToString().Length <= 7 && sku.ToString().Length > 3 &&
                                    float.TryParse(table.Rows[i][invIndex].ToString(), out var inv)) 
                                {
                                    // shift indexes if order qty is null
                                    if (!float.TryParse(table.Rows[i][ordIndex].ToString(), out var ord))
                                    {
                                        ord = inv;
                                        if (!float.TryParse(table.Rows[i][invIndex - 1].ToString(), out inv))
                                        {
                                            window.println(string.Format("Skipped\t{0} | {1} | {2}", table.Rows[i][skuIndex], table.Rows[i][invIndex], table.Rows[i][ordIndex]));
                                            skipped.Add(upc.ToString());
                                        }
                                    }
                                    // Add product to list
                                    string skuS = sku.ToString("D7");
                                    //window.println(string.Format("Found\t{0} | {1} | {2}" , skuS, inv, ord));
                                    products.Add(new Product(skuS, inv, ord));
                                } else
                                {
                                    // skip product if info is invalid
                                    //window.println(string.Format("Skipped\t{0} | {1} | {2}", table.Rows[i][skuIndex], table.Rows[i][invIndex], table.Rows[i][ordIndex]));
                                    skipped.Add(upc.ToString());
                                }
                            }
						}

                        if (skipped.Count > 0)
                        {
                            window.println(skipped.Count + " products skipped and added to correction file.");
                            skipped.Insert(0, "UPC's to be corrected:");
                            File.WriteAllLines(generateFilename("Corrections", ".txt"), skipped);
                        }
                        return products;
                    }
                }
            } catch (IOException) {
                // Error if file is already open
                var result = MessageBox.Show("Please close the excel file before continuing.", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Cancel) {
                    window.Close();
                    return null;
                }
                return ReadExcelFile(filepath);
            }
        }

        /**
         * Scrapes orgill website for warehouse data from list of products;
         * Performs web search asynchronously
         */
        private async Task GatherData() {
            window.println("SKU\t\tOn Hand\tOrder Qty\tWarehouse", Colors.CadetBlue);
            window.TimeCounter.Content = readTime((int)(products.Count * 2.5f));

            List<Task<Product>> tasks = (from product in products select handler.GetWarehouseDataAsync(product)).ToList();
            window.currentProgress.IsIndeterminate = false;
            window.currentProgress.Maximum = products.Count;
            window.currentProgress.Value = 0;

            for (int i = 0; tasks.Count > 0; i++) {
                Task<Product> t = await Task.WhenAny(tasks);
                tasks.Remove(t);
                Product p = await t;
                if (p.WarehouseQty <= 0) outofstock.Add(p);
                window.println(p.ToString());
                window.currentProgress.Value++;
                window.TimeCounter.Content = string.Format("{0} {1}/{2}", readTime((int)(tasks.Count * 2.5f)), window.currentProgress.Value, window.currentProgress.Maximum);
            }

            window.println("Done", Colors.CadetBlue);
        }

        /**
         * Trims list of products based on retrieved warehouse data and
         * gets list within acceptable threshold based on priority
         */
        private static void TrimProductList(int limit, List<Product> products, MainWindow window) {
            window.currentProgress.Maximum = products.Count - limit;
            window.currentProgress.Value = 0;

            // remove products that are unavailable in the warehouse
            int removed = products.RemoveAll(p => p.WarehouseQty <= 0);
            window.currentProgress.Value += removed;
            window.println(removed + " were out in the warehouse", Colors.CadetBlue);

            if (limit > 0) {
                // Sort by importance
                products.Sort(delegate (Product p1, Product p2) { return p1.Ratio.CompareTo(p2.Ratio); });

                // remove until within acceptable threshold
                removed = products.Count - limit;
                if (removed > 0) {
                    products.RemoveRange(0, removed);
                    window.currentProgress.Value += removed;
                    window.println(removed + " were removed based on need ratio", Colors.CadetBlue);
                }
            }
        }

        private static string generateFilename(string baseName, string ext) {
            int count = 0;
            string filename_current;
            string date = string.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now);
            do {
                filename_current = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + Path.DirectorySeparatorChar
                                 + baseName + "_" + date + ((count++ > 0) ? "(" + count + ")" : "") + ext;
            } while (File.Exists(filename_current));
            return filename_current;
        }

        private static void writeToFile(List<Product> products) {
            string[] output = new string[products.Count + 1];
            output[0] = "SKU,QTY,Retail";
            for (int i = 0; i < products.Count; i++)
            {
                if (products.ElementAt(i).WarehouseQty == -1) continue;
                output[i + 1] = products.ElementAt(i).SKU + "," + products.ElementAt(i).OrderQty + ",";
            }
            File.WriteAllLines(generateFilename("OrgillOrder", ".csv"), output);
        }
    }
}
