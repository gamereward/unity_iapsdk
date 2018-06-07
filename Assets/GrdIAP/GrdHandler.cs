using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Grd
{
    internal class GrdHandler : MonoBehaviour
    {

        private string apiUrl = "https://gamereward.io/appapi/";
        private string apiId = "cc8b8744dbb1353393aac31d371af9a55a67df16";
        private string apiSecret = "1679091c5a880faf6fb5e6087eb1b2dc4daa3db355ef2b0e64b472968cb70f0df4be00279ee2e0a53eafdaa94a151e2ccbe3eb2dad4e422a7cba7b261d923784";
        private string token = "";
        public void Init(string appid, string appsecret, string appurl)
        {
            this.apiId = appid;
            this.apiSecret = appsecret;
            this.apiUrl = appurl;
            DontDestroyOnLoad(this.gameObject);
        }
        public void Post(GrdNetworkEventHandler callback, string action, Dictionary<string, string> pars = null)
        {
            StartCoroutine(PostData(callback, action, pars));
        }
        public void Get(GrdNetworkEventHandler callback, string action, Dictionary<string, string> pars = null)
        {
            StartCoroutine(GetData(callback, action, pars));
        }
        private IEnumerator PostData(GrdNetworkEventHandler callback, string action, Dictionary<string, string> pars = null)
        {
            WWWForm wwwForm = new WWWForm();
            foreach (string key in pars.Keys)
            {
                wwwForm.AddField(key, pars[key]);
            }
            wwwForm.AddField("api_id", apiId);
            wwwForm.AddField("api_key", GetApiKey());
            if (token.Length > 0)
                wwwForm.AddField("token", token);
            WWW www = new WWW(apiUrl + action, wwwForm);
            yield return www;
            string data = www.text;
            int ipos = data.IndexOf("{");
            if (ipos > 0)
            {
                data = data.Substring(ipos);
            }
            callback(data);
        }

        private string GetApiKey()
        {
            long t = GrdManager.GetEpochTime();
            t = t / 15;
            var k = (int)(t % 20);
            var len = apiSecret.Length / 20;
            var str = apiSecret.Substring(k * len, len);
            str = GrdManager.Md5Sum(str+t.ToString());
            return str;
        }
        private IEnumerator GetData(GrdNetworkEventHandler callback, string action, Dictionary<string, string> pars = null)
        {
            string url = apiUrl + action;
            if (pars != null && pars.Count > 0)
            {
                url = url + "/api_id=" + apiId + "&api_key=" + GetApiKey();
                if (token.Length > 0)
                {
                    url += "&token=" + token;
                }
                foreach (string key in pars.Keys)
                {
                    url +=  "&"+key + "=" +WWW.EscapeURL(pars[key]);
                }
            }
            WWW www = new WWW(url);
            yield return www;
            string data = www.text;
            int ipos = data.IndexOf("{");
            if (ipos > 0)
            {
                data = data.Substring(ipos);
            }
            callback(data);
        }
       
    }
}