using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace eMoneyUILink {
    internal class OpenMoney {

        private static Dictionary<string, string> AllnetWebRequest(string path, String content) {
            EMoneyUILink.LogMessage("Allnet: Network Request: " + path);
            EMoneyUILink.LogMessage("Allnet: Send: " + content);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(path);
            req.UserAgent = "Windows/ver.3.0";
            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";

            byte[] b64 = Encoding.ASCII.GetBytes(Convert.ToBase64String(ZipStr(content)));
            req.Headers.Add("Pragma", "DFI");

            req.ContentLength = b64.Length;
            EMoneyUILink.LogMessage("Allnet: Request length: " + b64.Length);

            Stream rs = req.GetRequestStream();
            rs.Write(b64, 0, b64.Length);
            rs.Flush();

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            int len = (int)resp.ContentLength;
            EMoneyUILink.LogMessage("Allnet: Response length: " + len);
            byte[] indata = new byte[len];
            Stream res = resp.GetResponseStream();
            res.Read(indata, 0, len);
            String data = Encoding.ASCII.GetString(indata);

            EMoneyUILink.LogMessage("Allnet: Received: " + data);

            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string keyval in data.Split('&')) {
                string[] keyval2 = keyval.Split('=');
                values.Add(keyval2[0], keyval2[1]);
            }

            rs.Close();
            res.Close();

            return values;
        }

        private static byte[] ZipStr(String str) {
            using (MemoryStream output = new MemoryStream()) {
                using (DeflaterOutputStream gzip =
                  new DeflaterOutputStream(output)) {
                    using (StreamWriter writer =
                      new StreamWriter(gzip, Encoding.UTF8)) {
                        writer.Write(str);
                    }
                }

                return output.ToArray();
            }
        }

        public static Dictionary<string, string> OpenMoneyRequest(String cardid, int accountid, int brandId, int amount, int count, String requestType) {
            String path = EMoneyUILink.openmoney_url;
            String content = "_=_&cardid=" + cardid + "&accountid=" + accountid + "&brandid=" + brandId + "&amount=" + amount + "&count=" + count + "&requestType=" + requestType + "&openaime=0&emoneyuilink=1&pcbid=" + EMoneyUILink.keychip_id + "&__=_";
            return AllnetWebRequest(path, content);
        }

    }
}
