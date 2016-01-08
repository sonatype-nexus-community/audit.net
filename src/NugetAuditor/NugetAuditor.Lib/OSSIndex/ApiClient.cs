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
            : base("https://ossindex.net/v1.0", cachePolicy)
        { }

        private void BeforeSerialization(IRestResponse response)
        {
            if (response.StatusCode == HttpStatusCode.InternalServerError)
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
                    throw new ApiClientException(response.ErrorMessage, response.ErrorException);
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
                throw new ApiClientException(response.ErrorMessage, response.ErrorException);
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
                    throw new ApiClientException(response.ErrorMessage, response.ErrorException);
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
                throw new ApiClientException(response.ErrorMessage, response.ErrorException);
            }

            return response.Data;
        }

        public IList<Vulnerability> GetVulnerabilities(long scmId)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "scm/{id}/vulnerabilities";
            request.AddParameter("id", scmId, ParameterType.UrlSegment);

            var response = Execute<VulnerabilityResponse>(request);

            if (response.ResponseStatus == ResponseStatus.Error)
            {
                throw new ApiClientException(response.ErrorMessage, response.ErrorException);
            }

            return response.Data;
        }      
    }
}
