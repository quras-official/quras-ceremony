using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CeremonyClientFinal.Network
{
    class RpcClient
    {
        private string ServerUrl;

        public void SetServerInfo(string strServerUrl)
        {
            ServerUrl = strServerUrl;
        }
        public string SendRequest(string queryString, string encodeType = "UTF-8")
        {
            WebRequest request = WebRequest.Create(ServerUrl);
            request.ContentType = "application/json";
            request.Method = "POST";

            byte[] buffer = Encoding.GetEncoding(encodeType).GetBytes(queryString);
            string result = System.Convert.ToBase64String(buffer);
            Stream reqstr = request.GetRequestStream();
            reqstr.Write(buffer, 0, buffer.Length);
            reqstr.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }
    }
}
