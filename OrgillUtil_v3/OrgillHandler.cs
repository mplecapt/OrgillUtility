using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NHtmlUnit.Html;
using HtmlAgilityPack;

namespace OrgillUtil_v3
{
    public class OrgillHandler
    {
        private NHtmlUnit.WebClient client;
        private CookieContainer cookies;
        private HtmlPage page;
        private bool IsLoggedIn = false;
        private Task loadPage;

        public OrgillHandler()
        {
            client = new NHtmlUnit.WebClient();
            client.Options.UseInsecureSsl = true;
            client.Options.JavaScriptEnabled = false;
        }

        public void Init() {
            loadPage = Task.Run(() => {
                try {
                    page = (HtmlPage)client.GetPage("https://www.orgill.com/");
                    Console.WriteLine("Done preloading");
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                    client.Close();
                }
            });
        }

        public bool Login(string user, string pass)
        {
            if (IsLoggedIn) throw new Exception("Already logged in");
            if (loadPage.Status == TaskStatus.Running) loadPage.Wait();
            
            try
            {
                page.GetElementById("lvwOrgill_ucPublicHeader_loginOrgill_UserName").SetAttribute("Value", user);
                page.GetElementById("lvwOrgill_ucPublicHeader_loginOrgill_Password").SetAttribute("Value", pass); ;
                page = page.GetElementById("lvwOrgill_ucPublicHeader_loginOrgill_LoginButton").Click() as HtmlPage;

                if (!page.Url.Equals("https://www.orgill.com"))
                {
                    cookies = new CookieContainer();
                    cookies.Add(createCookie(".ASPXAUTH", client.CookieManager.GetCookie(".ASPXAUTH").Value.ToString()));
                    client.Close();
                    client = null;
                    IsLoggedIn = true;
                    return true;
                } else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                client.Close();
                return false;
            }
        }

        private Cookie createCookie(string name, string value)
        {
            Cookie cookie = new Cookie();
            cookie.Name = name;
            cookie.Value = HttpUtility.UrlEncode(value);
            cookie.Domain = "orgill.com";
            cookie.Path = "/";
            cookie.Expires = DateTime.MaxValue;
            return cookie;
        }

        private HttpWebRequest GenerateWebRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new System.Uri(url));

            request.CookieContainer = cookies;
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.0.8) Gecko/2009021910 Firefox/3.0.7 (.NET CLR 3.5.30729)";
            request.Headers.Add("Pragma", "no-cache");
            request.Timeout = 40000;

            return request;
        }

        private async Task<string> GetWebDataAsync(string url)
        {
            string data = string.Empty;

            using (HttpWebResponse response = (HttpWebResponse)await GenerateWebRequest(url).GetResponseAsync()) {
                using (Stream receiveStream = response.GetResponseStream()) {
                    StreamReader readStream;

                    readStream = (string.IsNullOrWhiteSpace(response.CharacterSet))
                        ? new StreamReader(receiveStream)
                        : new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                    data = await readStream.ReadToEndAsync();
                    readStream.Close();
                }
                if (response.ResponseUri.ToString().Equals(url))
                    Console.WriteLine(" ~~ " + response.ResponseUri.ToString());
                //return response.ResponseUri.ToString();
            }

            return data;
        }

        public async Task<Product> GetWarehouseDataAsync(Product p) {
            string html = await GetWebDataAsync("https://orgill.com/index.aspx?tab=7&sku=" + p.SKU);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var element = doc.GetElementbyId("cphMainContent_ctl00_lblAvailableQty");
            p.WarehouseQty = (element != null) ? int.Parse(element.InnerHtml) : -1;
            return p;
        }
    }
}
