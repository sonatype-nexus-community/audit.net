using RestSharp;
using RestSharp.Extensions;
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
            this._restClient.CachePolicy = cachePolicy;
        }

        private void BeforeSerialization(IRestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                throw new ApiClientException(string.Format("Unexpected response status {0}", (int)response.StatusCode));
            }
        }

        internal IRestResponse<T> Execute<T>(RestRequest request) where T : IApiResponse, new()
        {
            //var cachePolicy = HttpWebRequest.DefaultCachePolicy;

            try
            {
                //HttpWebRequest.DefaultCachePolicy = this._cachePolicy;

                //request.OnBeforeDeserialization = BeforeSerialization;
             
                return this._restClient.Execute<T>(request);

                //return ParseResponseData(response);

            }
            finally
            {
            //    HttpWebRequest.DefaultCachePolicy = cachePolicy;
            }
        }

        //internal async Task<T> ExecuteAsync<T>(RestRequest request) where T : IApiResponse, new()
        //{
        //    var cachePolicy = HttpWebRequest.DefaultCachePolicy;

        //    try
        //    {
        //        HttpWebRequest.DefaultCachePolicy = this._cachePolicy;

        //        request.OnBeforeDeserialization = BeforeSerialization;

        //        var response = await this._restClient.ExecuteTaskAsync<T>(request);

        //        return ParseResponseData<T>(response);
        //    }
        //    finally
        //    {
        //        HttpWebRequest.DefaultCachePolicy = cachePolicy;
        //    }
        //}

        private T ParseResponseData<T>(IRestResponse<T> response) where T : IApiResponse
        {
            if (response.ErrorException != null)
            {
                throw new ApiClientException(string.Format("Error retrieving response: \"{0}\"", response.ErrorMessage), response.ErrorException);
            }

            return response.Data;
        }
    }
}
