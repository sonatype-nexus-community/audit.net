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
    public class ApiClient : ApiClientBase, IApiClient
    {
        private int _pageSize = 100;

        public ApiClient()
            : this(new HttpRequestCachePolicy(HttpRequestCacheLevel.Default))
        { }

        public ApiClient(HttpRequestCachePolicy cachePolicy)
            : base("https://ossindex.net/v1.0", cachePolicy)
        { }

        public IList<Artifact> SearchArtifacts(IEnumerable<ArtifactSearch> searches)
        {
            var artifacts = new List<Artifact>(searches.Count());

            while (searches.Any())
            {
                var request = new RestRequest(Method.POST);

                request.Resource = "search/artifact/";
                request.RequestFormat = DataFormat.Json;

                request.AddBody(searches.Take(this._pageSize));

                artifacts.AddRange(Execute<List<Artifact>>(request));

                searches = searches.Skip(this._pageSize);
            }

            return artifacts;
        }

        public IList<Artifact> SearchArtifact(ArtifactSearch search)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "search/artifact/{pm}/{name}/{version}";
            request.RequestFormat = DataFormat.Json;

            request.AddParameter("pm", search.pm, ParameterType.UrlSegment);
            request.AddParameter("name", search.name, ParameterType.UrlSegment);
            request.AddParameter("version", search.version, ParameterType.UrlSegment);

            return Execute<List<Artifact>>(request);            
        }

        public IList<SCM> GetSCMs(IEnumerable<long> scmIds)
        {
            var scms = new List<SCM>(scmIds.Count());

            while (scmIds.Any())
            {
                var request = new RestRequest(Method.GET);

                request.Resource = "scm/{id}";
                request.RequestFormat = DataFormat.Json;

                request.AddParameter("id", string.Join(",", scmIds.Take(this._pageSize)), ParameterType.UrlSegment);

                scms.AddRange(Execute<List<SCM>>(request));

                scmIds = scmIds.Skip(this._pageSize);
            }

            return scms;
        }

        public IList<Vulnerability> GetScmVulnerabilities(long scmId)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "scm/{id}/vulnerabilities";
            request.AddParameter("id", scmId, ParameterType.UrlSegment);

            return Execute<List<Vulnerability>>(request);
        }      
    }
}
