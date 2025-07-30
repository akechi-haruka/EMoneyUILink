using System.Net;
using System.Text;
using Haruka.Arcade.EXMoney.Debugging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Haruka.Arcade.EXMoney {
    class PaymentRequest {
        public int version;
        public string keychip;
        public string cardid;
        public string request;
        public uint brand;
        public int amount;
        public int count;
        public string item_name;
    }

    enum PaymentRequestType {
        Balance,
        PayToCoin,
        PayAmount
    }

    class PaymentResponse {
        public bool success;
        public string error;
        public int balance_after;
    }

    static class OpenMoney {
        private static readonly ILogger LOG = Logging.Factory.CreateLogger(nameof(PaymentProcess));

        private const int VERSION = 2;

        private static string url;
        private static string keychip;

        public static void Configure(string serverUrl, string paymentKeychip) {
            url = serverUrl;
            keychip = paymentKeychip;
        }

        public static bool IsConfigured() {
            return url != null;
        }

        private static string OpenMoneyWebRequest(string path, string request, int version) {
            LOG.LogInformation("OpenMoneyWebRequest: Network Request: {p}", path);
            LOG.LogDebug("OpenMoneyWebRequest: Send: {r}", request);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(path);
            req.UserAgent = "OpenMoney/" + version;
            req.ContentType = "application/json";
            req.Method = "POST";
            req.Timeout = 10000;
            req.ReadWriteTimeout = 10000;

            byte[] bytes = Encoding.ASCII.GetBytes(request);

            req.ContentLength = bytes.Length;
            LOG.LogTrace("OpenMoneyWebRequest: Request length: {l}", bytes.Length);

            Stream rs = req.GetRequestStream();
            rs.Write(bytes, 0, bytes.Length);
            rs.Flush();

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            int len = (int)resp.ContentLength;
            LOG.LogTrace("OpenMoneyWebRequest: Response length: {l}", len);
            LOG.LogDebug("OpenMoneyWebRequest: Response code: {c}", resp.StatusCode);
            if (len == -1) {
                throw new Exception("Server Error: " + resp.StatusCode);
            }

            byte[] indata = new byte[len];
            Stream res = resp.GetResponseStream();
            res.ReadExactly(indata, 0, len);
            string data = Encoding.ASCII.GetString(indata);

            LOG.LogDebug("OpenMoneyWebRequest: Received: " + data);

            rs.Close();
            res.Close();

            return data;
        }

        public static PaymentResponse OpenMoneyRequest(string cardid, uint brandID, int amount, int count, PaymentRequestType requestType, string itemName) {
            string path = url;
            PaymentRequest req = new PaymentRequest() {
                version = VERSION,
                request = requestType.ToString(),
                amount = amount,
                brand = brandID,
                cardid = cardid,
                count = count,
                item_name = itemName,
                keychip = keychip
            };
            return JsonConvert.DeserializeObject<PaymentResponse>(OpenMoneyWebRequest(path, JsonConvert.SerializeObject(req), VERSION));
        }
    }
}