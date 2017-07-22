using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NCMBRest
{
    public class NCMBRestController : MonoBehaviour
    {
        private static readonly string API_PROTOCOL = "https";
        private static readonly string API_DOMAIN = "mb.api.cloud.nifty.com";
        private static readonly string API_VERSION = "2013-09-01";

        private static readonly string KEY_SIGNATURE_METHOD = "SignatureMethod";
        private static readonly string KEY_SIGNATURE_VERSION = "SignatureVersion";

        //NCMB 共通リクエストヘッダ//
        private static readonly string KEY_APPLICATION = "X-NCMB-Application-Key";

        private static readonly string KEY_TIMESTAMP = "X-NCMB-Timestamp";

        //private static readonly string KEY_SESSION_TOKEN = "X-NCMB-Apps-Session-Token";
        private static readonly string KEY_SIGNATURE = "X-NCMB-Signature";

        private static readonly string KEY_CONTENT_TYPE = "Content-Type";
        private static readonly string VAL_SIGNATURE_METHOD = "HmacSHA256";
        private static readonly string VAL_SIGNATURE_VERSION = "2";
        private static readonly string VAL_CONTENT_TYPE = "application/json";

        public string applicationKey;
        public string clientKey;

        private string baseParamString;
        private string timestamp;

        public enum RequestType
        {
            GET,
            POST,
            PUT
        }

        public IEnumerator Call(RequestType method, string path, NCMBDataStoreParamSet ncmbObjectRest)
        {
            string endpoint = this.Endpoint(path);
            string queryString = this.QueryString(ncmbObjectRest);

            this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            this.baseParamString = this.ParamString();

            UnityWebRequest request;
            switch (method)
            {
                case RequestType.GET:
                    if (!string.IsNullOrEmpty(queryString))
                    {
                        endpoint += "?" + queryString.Trim('&');
                    }
                    request = UnityWebRequest.Get(endpoint);
                    break;

                default:
                    request = new UnityWebRequest(endpoint, method.ToString());
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(ncmbObjectRest.FieldsAsJson));

                    break;
            }

            request.SetRequestHeader(KEY_APPLICATION, this.applicationKey);
            request.SetRequestHeader(KEY_SIGNATURE, this.Signature(method.ToString(), endpoint, queryString));
            request.SetRequestHeader(KEY_TIMESTAMP, this.timestamp);
            request.SetRequestHeader(KEY_CONTENT_TYPE, VAL_CONTENT_TYPE);

            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.Send();

            if (request.isError)
            {
                Debug.Log(request.error);
            }
            else
            {
                if (request.responseCode == 200 || request.responseCode == 201)
                {
                    Debug.Log("Request succeed");
                }
                else
                {
                    //登録完了 201
                    Debug.LogWarning("Request Failed" + request.responseCode.ToString());
                }

                yield return request.downloadHandler.text;
            }
        }

        //署名の生成//
        private string Signature(string method, string endpoint, string queryString)
        {
            string signatureString = this.SignatureString(method, endpoint, queryString);
            HMACSHA256 sha256 = new HMACSHA256(Encoding.UTF8.GetBytes(this.clientKey));
            byte[] signatureBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureString));
            return Convert.ToBase64String(signatureBytes);
        }

        //署名用文字列の生成//
        private string SignatureString(string method, string endpoint, string queryString)
        {
            var pathAndQuery = endpoint.Replace(string.Format("{0}://{1}", API_PROTOCOL, API_DOMAIN), "");
            var parts = pathAndQuery.Split('?');
            StringBuilder builder = new StringBuilder(this.baseParamString);
            if (parts.Length == 2)
            {
                builder.Append(queryString);
            }
            return string.Format(
                "{0}\n{1}\n{2}\n{3}",
                method,
                API_DOMAIN,
                parts[0],
                builder.ToString()
            );
        }

        //クエリストリングの生成//
        private string QueryString(NCMBDataStoreParamSet ncmbObjectRest)//, bool isConvertJson)
        {
            StringBuilder builder = new StringBuilder();
            /*
            if (ncmbObjectRest.Count)
            {
                builder.Append("&count=1");
            }
            */
            if (ncmbObjectRest.Limit > 0)
            {
                builder.Append(string.Format("&limit={0}", ncmbObjectRest.Limit));
            }
            if (!string.IsNullOrEmpty(ncmbObjectRest.SortColumn))
            {
                builder.Append(string.Format("&order={0}", WWW.EscapeURL(ncmbObjectRest.SortColumn)));
            }

            if (!string.IsNullOrEmpty(ncmbObjectRest.FieldsAsJson))
            {
                builder.Append(string.Format("&where={0}", WWW.EscapeURL(ncmbObjectRest.FieldsAsJson)));
            }
            return builder.ToString();
        }

        //エンドポイントの作成//
        private string Endpoint(string path)
        {
            return string.Format("{0}://{1}/{2}/{3}", API_PROTOCOL, API_DOMAIN, API_VERSION, path);
        }

        private string ParamString()
        {
            string[] paramString = new string[] {
                string.Format("{0}={1}", KEY_SIGNATURE_METHOD, VAL_SIGNATURE_METHOD),
                string.Format("{0}={1}", KEY_SIGNATURE_VERSION, VAL_SIGNATURE_VERSION),
                string.Format("{0}={1}", KEY_APPLICATION, this.applicationKey),
                string.Format("{0}={1}", KEY_TIMESTAMP, this.timestamp),
            };
            return string.Join("&", paramString);
        }
    }

    [Serializable]
    public class NCMBDataStoreParamSet
    {
        //[NonSerialized]
        public string objectId;

        public string createDate;
        public string updateDate;
        public string FieldsAsJson { get; private set; }
        public string SortColumn { get; set; }
        public int Limit { get; set; }

        public NCMBDataStoreParamSet(object obj = null)
        {
            FieldsAsJson = JsonUtility.ToJson(obj);
        }

        public DateTime createDateTime
        {
            get
            {
                return DateTime.Parse(createDate);
                //"yyyy-MM-dd'T'HH:mm:ss'Z'"に直す//
            }
        }
    }
}