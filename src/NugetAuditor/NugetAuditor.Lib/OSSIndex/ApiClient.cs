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
            : base("https://ossindex.net/v1.1", cachePolicy)
        { }

        private void BeforeSerialization(IRestResponse response)
        {
            if (response.StatusCode >= HttpStatusCode.BadRequest)
            {
                throw new ApiClientException(string.Format("Unexpected response status {0}", (int)response.StatusCode));
            }
        }

        public IList<Artifact> SearchArtifacts(IEnumerable<ArtifactSearch> searches)
        {
            var result = new List<Artifact>(searches.Count());

            while (searches.Any())
            {
                var request = new RestRequest(Method.POST);

                request.Resource = "search/artifact/";
                request.RequestFormat = DataFormat.Json;
                request.OnBeforeDeserialization = BeforeSerialization;
                request.AddBody(searches.Take(this._pageSize));

                var response = Execute<ArtifactResponse>(request);

                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
                }

                result.AddRange(response.Data);

                searches = searches.Skip(this._pageSize);
            }

            return result;
        }

        public IList<Artifact> SearchArtifact(ArtifactSearch search)
        {
            return SearchArtifact(search.pm, search.name, search.version);
        }

        public IList<Artifact> SearchArtifact(string pm, string name, string version)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "search/artifact/{pm}/{name}/{version}";
            request.RequestFormat = DataFormat.Json;
            request.OnBeforeDeserialization = BeforeSerialization;

            request.AddParameter("pm", pm, ParameterType.UrlSegment);
            request.AddParameter("name", name, ParameterType.UrlSegment);
            request.AddParameter("version", version, ParameterType.UrlSegment);

            var response = Execute<ArtifactResponse>(request);

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
            }

            return response.Data;
        }

        public IList<SCM> GetSCMs(IEnumerable<long> scmIds)
        {
            var scms = new List<SCM>(scmIds.Count());

            while (scmIds.Any())
            {
                var request = new RestRequest(Method.GET);

                request.Resource = "scm/{id}";
                request.RequestFormat = DataFormat.Json;
                request.OnBeforeDeserialization = BeforeSerialization;

                request.AddParameter("id", string.Join(",", scmIds.Take(this._pageSize)), ParameterType.UrlSegment);

                var response = Execute<SCMResponse>(request);

                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
                }

                scms.AddRange(response.Data);

                scmIds = scmIds.Skip(this._pageSize);
            }

            return scms;
        }

        public IList<SCM> GetSCM(long scmId)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "scm/{id}";
            request.RequestFormat = DataFormat.Json;
            request.OnBeforeDeserialization = BeforeSerialization;

            request.AddParameter("id", scmId.ToString(), ParameterType.UrlSegment);

            var response = Execute<SCMResponse>(request);

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
            }

            return response.Data;
        }

        public IList<Project> GetProjects(IEnumerable<long> projectIds)
        {
            var projects = new List<Project>(projectIds.Count());

            while (projectIds.Any())
            {
                var request = new RestRequest(Method.GET);

                request.Resource = "project/{id}";
                request.RequestFormat = DataFormat.Json;
                request.OnBeforeDeserialization = BeforeSerialization;

                request.AddParameter("id", string.Join(",", projectIds.Take(this._pageSize)), ParameterType.UrlSegment);

                var response = Execute<ProjectResponse>(request);

                if (response.ResponseStatus == ResponseStatus.Error)
                {
                    throw new ApiClientException(response.ErrorMessage, response.ErrorException);
                }

                projects.AddRange(response.Data);

                projectIds = projectIds.Skip(this._pageSize);
            }

            return projects;
        }

        public IList<Project> GetProject(long projectId)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "project/{id}";
            request.RequestFormat = DataFormat.Json;
            request.OnBeforeDeserialization = BeforeSerialization;

            request.AddParameter("id", projectId.ToString(), ParameterType.UrlSegment);

            var response = Execute<ProjectResponse>(request);

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new ApiClientException(response.ErrorMessage, response.ErrorException);
            }

            return response.Data;
        }

        public IList<Vulnerability> GetVulnerabilities(long scmId)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "project/{id}/vulnerabilities";
            request.AddParameter("id", scmId, ParameterType.UrlSegment);

            var response = Execute<VulnerabilityResponse>(request);

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new ApiClientTransportException(response.ErrorMessage, response.ErrorException);
            }

            return response.Data;
        }      
    }
}
