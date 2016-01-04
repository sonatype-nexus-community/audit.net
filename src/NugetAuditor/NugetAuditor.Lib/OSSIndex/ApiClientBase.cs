using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
{
    public abstract class ApiClientBase
    {
        private string _baseUrl;
        private HttpRequestCachePolicy _cachePolicy;
        private RestClient _restClient;

        public ApiClientBase(string baseUrl, HttpRequestCachePolicy cachePolicy)
        {
            this._baseUrl = baseUrl;
            this._cachePolicy = cachePolicy;
            this._restClient = new RestClient(this._baseUrl);
        }

        internal T Execute<T>(RestRequest request) where T : new()
        {
            var cachePolicy = HttpWebRequest.DefaultCachePolicy;

            try
            {
                HttpWebRequest.DefaultCachePolicy = this._cachePolicy;

                var response = this._restClient.Execute<T>(request);

                if (response.ErrorException != null)
                {
                    const string message = "Error retrieving response. Check inner details for more info.";
                    var apiException = new ApplicationException(message, response.ErrorException);
                    throw apiException;
                }

                return response.Data;
            }
            finally
            {
                HttpWebRequest.DefaultCachePolicy = cachePolicy;
            }
        }
    }
}
