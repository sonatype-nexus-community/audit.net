// Copyright (c) 2015-2016, Vör Security Ltd.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of Vör Security, OSS Index, nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL VÖR SECURITY BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
