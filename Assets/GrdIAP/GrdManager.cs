using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Grd
{
    internal delegate void GrdNetworkEventHandler(string data);
    public enum GrdNet
    {
        Main, Test
    }
    public class GrdManager
    {
        public static void Init(string appId, string secret, GrdNet net)
        {
            if (handler == null)
            {
                GameObject g = new GameObject();
                g.name = "GrdManager";
                handler = g.AddComponent<GrdHandler>();
            }
            handler.Init(appId, secret, net == GrdNet.Main ? apiMainUrl : apiTestUrl);
        }
        private const string apiTestUrl = "https://test.gamereward.io/appapi/";
        private const string apiMainUrl = "https://gamereward.io/appapi/";
        private static GrdHandler handler;
        public static long GetEpochTime()
        {

            System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            return (long)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        }
        public static string Md5Sum(string strToEncrypt)
        {
            System.Text.UTF8Encoding ue = new System.Text.UTF8Encoding();
            byte[] bytes = ue.GetBytes(strToEncrypt);

            // encrypt bytes
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] hashBytes = md5.ComputeHash(bytes);
            // Convert the encrypted bytes back to a string (base 16)
            string hashString = "";

            for (int i = 0; i < hashBytes.Length; i++)
            {
                hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, '0');
            }

            return hashString.PadLeft(32, '0');
        }
        private static void GetImageSize(byte[] imageData, out int width, out int height)
        {
            width = ReadInt(imageData, 3 + 15);
            height = ReadInt(imageData, 3 + 15 + 2 + 2);
        }
        private static int ReadInt(byte[] imageData, int offset)
        {
            return (imageData[offset] << 8) | imageData[offset + 1];
        }
        private static Dictionary<string, object> GetObjectData(string data)
        {
            Dictionary<string, object> result = null;
            try
            {
                result = (Dictionary<string, object>)MiniJSON.Json.Deserialize(data);
            }
            catch
            {
            }
            if (result == null)
            {
                result = new Dictionary<string, object>();
                result.Add("error", 100);
                result.Add("message", data);
            }
            return result;
        }
        /// <summary>
        /// Request for buy an item.
        /// </summary>
        /// <param name="callback"></param>
        public static void RequestBuyItem(string itemid, int quantity, GrdEventHandler<GrdPurchaseRequest> callback)
        {
            Dictionary<string, string> pars = new Dictionary<string, string>();
            pars["itemid"] = itemid;
            pars["quantity"] = quantity.ToString();
            handler.Post((data) =>
            {
                Dictionary<string, object> result = GetObjectData(data);
                if (callback != null)
                {
                    int error = int.Parse(result["error"].ToString());
                    GrdEventArgs<GrdPurchaseRequest> args = null;
                    if (error == 0)
                    {
                        GrdPurchaseRequest request = new GrdPurchaseRequest();
                        request.RequestId = result["requestid"].ToString();
                        args = new GrdEventArgs<GrdPurchaseRequest>(error, "", data, request);
                    }
                    else
                    {
                        args = new GrdEventArgs<GrdPurchaseRequest>(error, result["message"].ToString(), data, null);
                    }
                    callback(error, args);
                }
            }, "requestpurchase", pars);
        }
        public static void CheckItemStatus(string requestid, GrdEventHandler<GrdPurchaseStatus> callback)
        {
            Dictionary<string, string> pars = new Dictionary<string, string>();
            pars["requestid"] = requestid;
            handler.Post((data) =>
            {
                Dictionary<string, object> result = GetObjectData(data);
                if (callback != null)
                {
                    int error = int.Parse(result["error"].ToString());
                    string msg = "";
                    GrdPurchaseStatus status = GrdPurchaseStatus.NewRequest;
                    if (error != 0)
                    {
                        msg = result["message"].ToString();
                    }
                    else
                    {
                        status = (GrdPurchaseStatus)int.Parse(result["status"].ToString());
                    }
                    GrdEventArgs<GrdPurchaseStatus> args = new GrdEventArgs<GrdPurchaseStatus>(error, msg, data, status);
                    callback(error, args);
                }
            }, "checkpurchasestatus", pars);
        }
        /// <summary>
        /// Get the qrcode from text
        /// </summary>
        /// <param name="text">The text to encode to QR code</param>
        /// <param name="callback">Call when server response QR code.</param>
        public static void GetRequestPurchaseQRCode(string requestid, GrdEventHandler<Texture2D> callback)
        {
            GetQRCode("pay:" + requestid, callback);
        }
        /// <summary>
        /// Get the qrcode from text
        /// </summary>
        /// <param name="text">The text to encode to QR code</param>
        /// <param name="callback">Call when server response QR code.</param>
        public static void GetQRCode(string text, GrdEventHandler<Texture2D> callback)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("text", text);
            dic.Add("type", "1");
            handler.Post((data) =>
            {
                if (callback != null)
                {

                    Dictionary<string, object> result = GetObjectData((string)data);
                    Texture2D texture = null;
                    int error = int.Parse(result["error"].ToString());
                    string message = text;
                    if (error == 0)
                    {
                        string qrcode = result["qrcode"].ToString();
                        if (qrcode.Length > 0)
                        {
                            qrcode = qrcode.Substring("data:image/image/png;base64,".Length);
                        }
                        try
                        {
                            texture = GetTexture(qrcode);
                        }
                        catch
                        {
                            error = 1;
                        }
                    }
                    else
                    {
                        text = result["message"].ToString();
                    }
                    GrdEventArgs<Texture2D> args = new GrdEventArgs<Texture2D>(error, data, text, texture);
                    callback(error, args);
                }
            }, "qrcode", dic);
        }
        private static Texture2D GetTexture(string responseText)
        {

            Texture2D texture = null;
            byte[] array = System.Convert.FromBase64String(responseText);
            int width, height;
            GetImageSize(array, out width, out height);
            texture = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(array);
            return texture;
        }
        public static void GetItemsInfo(string[] itemCodes, GrdEventHandler<GrdItem[]> callback)
        {
            Dictionary<string, string> pars = new Dictionary<string, string>();
            pars["items"] = string.Join(",", itemCodes);
            handler.Post((data) =>
            {
                Dictionary<string, object> result = GetObjectData(data);
                if (callback != null)
                {
                    int error = int.Parse(result["error"].ToString());
                    string msg = "";
                    List<GrdItem> items = new List<GrdItem>();
                    if (error == 0)
                    {
                        List<object> ls = (List<object>)result["items"];
                        for (int i = 0; i < ls.Count; i++)
                        {
                            Dictionary<string, object> dic = (Dictionary<string, object>)ls[i];
                            GrdItem item = new GrdItem();
                            item.itemcode = dic["itemcode"].ToString();
                            item.itemname = dic["itemname"].ToString();
                            item.price = decimal.Parse(dic["price"].ToString());
                            item.itemicon = dic["itemicon"].ToString();
                            items.Add(item);
                        }
                    }
                    else
                    {
                        msg = result["message"].ToString();
                    }
                    GrdEventArgs<GrdItem[]> args = new GrdEventArgs<GrdItem[]>(error, msg, data, items.ToArray());
                    callback(error, args);
                }
            }, "getappitems", pars);
        }
    }
    /// <summary>
    /// Delegate use to handle event when gamereward server response the result
    /// </summary>
    /// <param name="error">Error value: 0 if no error, else there was an error while call</param>
    /// <param name="args">args contains properties: Text is the text data field,RawData is the json data response from server</param>
    public delegate void GrdEventHandler<T>(int error, GrdEventArgs<T> args);

    public class GrdEventArgs<T>
    {
        public GrdEventArgs(int error, string errorMessage, string rawData, T data)
        {
            this.data = data;
            this.RawData = rawData;
            this.error = error;
            this.errorMessage = errorMessage;
        }
        private int error;
        private T data;
        private string errorMessage;
        public string RawData { get; private set; }
        public T Data { get { return data; } }
        public string ErrorMessage { get { if (error == 0) { return ""; } else { return errorMessage; } } }
    }
    public enum GrdPurchaseStatus
    {
        NewRequest = 0, Success = 1, Pending = 2, Error = 3
    }
    public class GrdItem
    {
        public string itemcode;
        public string itemname;
        public string itemicon;
        public decimal price;
    }
    public class GrdPurchaseRequest
    {
        public string RequestId { get; set; }
        public GrdPurchaseStatus Status { get; set; }
    }
}