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

using PackageUrl;
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
    internal class ApiClient : ApiClientBase, IApiClient
    {
        private int _pageSize = 100;

        public ApiClient()
            : this(new HttpRequestCachePolicy(HttpRequestCacheLevel.Default))
        { }

        public ApiClient(HttpRequestCachePolicy cachePolicy)
            : base("https://ossindex.sonatype.org/api/v3", cachePolicy)
        { }

        private void BeforeSerialization(IRestResponse response)
        {
            if (response.StatusCode >= HttpStatusCode.BadRequest)
            {
                throw new ApiClientException(string.Format("Unexpected response status {0}", (int)response.StatusCode));
            }
        }

        public IList<Package> SearchPackages(IEnumerable<PackageURL> coords)
        {
            var result = new List<Package>(coords.Count());

            while (coords.Any())
            {
                var request = new RestRequest(Method.POST);

                ComponentReport report = new ComponentReport();
                // IEnumerable<PackageURL> purls = coords.Take(this._pageSize);
                // List<string> useCoords = new List<string>();

                // foreach (PackageURL purl in purls)
                // {
                //     useCoords.Add(purl.ToString());
                // }

                // report.coordinates = useCoords;
                report.coordinates = coords.Select(x => x.ToString());

                request.Resource = "component-report";
                request.RequestFormat = DataFormat.Json;
                request.OnBeforeDeserialization = BeforeSerialization;
                request.AddBody(report);

                var response = Execute<PackageResponse>(request);

                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
                }

                result.AddRange(response.Data);

                coords = coords.Skip(this._pageSize);
            }

            return result;
        }
    }
}
