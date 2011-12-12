using System.IO;
using System.Net;

namespace BouvetContacts
{
    class WebRetriever
    {
        protected const string Site = "https://traffic.bouvet.no";

        private readonly NetworkCredential _credentials;

        public WebRetriever(NetworkCredential credentials)
        {
            _credentials = credentials;
        }

        protected string GetHTML(string url)
        {
            using (var reader = new StreamReader(GetStream(url)))
            {
                return reader.ReadToEnd();
            }
        }

        protected Stream GetStream(string url)
        {
            var get = (HttpWebRequest)WebRequest.Create(Site + url);
            get.Credentials = _credentials;
            var response = get.GetResponse();
            return response.GetResponseStream();            
        }
        
    }
}
