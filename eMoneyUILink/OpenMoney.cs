using Emoney.SharedMemory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using static OpenAimeIO_Managed.Core.Services.EMoney;

namespace eMoneyUILink {

    internal class PaymentRequest {
        public int version;
        public String keychip;
        public String cardid;
        public String request;
        public EMoneyBrandEnum brand;
        public int amount;
        public int count;
        public String item_name;
    }

    internal enum PaymentRequestType {
        Balance, PayToCoin, PayAmount
    }

    internal class PaymentResponse {
        public bool success;
        public String error;
        public int balance_after;
    }

    internal class OpenMoney {

        private const int VERSION = 2;

        private static string OpenMoneyWebRequest(string path, string request, int version) {
            EMoneyUILink.LogMessage("OpenMoneyWebRequest: Network Request: " + path);
            EMoneyUILink.LogMessage("OpenMoneyWebRequest: Send: " + request);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(path);
            req.UserAgent = "OpenMoney/"+version;
            req.ContentType = "application/json";
            req.Method = "POST";
            req.Timeout = 10000;
            req.ReadWriteTimeout = 10000;

            byte[] bytes = Encoding.ASCII.GetBytes(request);

            req.ContentLength = bytes.Length;
            EMoneyUILink.LogMessage("OpenMoneyWebRequest: Request length: " + bytes.Length);

            Stream rs = req.GetRequestStream();
            rs.Write(bytes, 0, bytes.Length);
            rs.Flush();

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            int len = (int)resp.ContentLength;
            EMoneyUILink.LogMessage("OpenMoneyWebRequest: Response length: " + len);
            EMoneyUILink.LogMessage("OpenMoneyWebRequest: Response code: " + resp.StatusCode);
            if (len == -1) {
                throw new Exception("Server Error: " + resp.StatusCode);
            }
            byte[] indata = new byte[len];
            Stream res = resp.GetResponseStream();
            res.Read(indata, 0, len);
            String data = Encoding.ASCII.GetString(indata);

            EMoneyUILink.LogMessage("OpenMoneyWebRequest: Received: " + data);

            rs.Close();
            res.Close();

            return data;
        }

        public static PaymentResponse OpenMoneyRequest(String cardid, EMoneyBrandEnum brand_id, int amount, int count, PaymentRequestType requestType, String item_name) {
            String path = EMoneyUILink.openMoneyURL;
            PaymentRequest req = new PaymentRequest() {
                version = VERSION,
                request = requestType.ToString(),
                amount = amount,
                brand = brand_id,
                cardid = cardid,
                count = count,
                item_name = item_name,
                keychip = EMoneyUILink.keychipId
            };
            return JsonConvert.DeserializeObject<PaymentResponse>(OpenMoneyWebRequest(path, JsonConvert.SerializeObject(req), VERSION));
        }

    }
}
