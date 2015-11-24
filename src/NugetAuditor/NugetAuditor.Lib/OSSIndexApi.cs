using NuGet;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib
{
	public class OSSIndexApi
	{
		const string BaseUrl = "https://ossindex.net/v1.0";
        const int PageSize = 20;

        private RequestCachePolicy _cachePolicy;

        public OSSIndexApi()
		{
            this._cachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            //this._cachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
        }

		public T Execute<T>(RestRequest request) where T : new()
		{
            var cachePolicy = HttpWebRequest.DefaultCachePolicy;

            HttpWebRequest.DefaultCachePolicy = this._cachePolicy;

            var client = new RestClient(BaseUrl);
            
            var response = client.Execute<T>(request);
            
			if (response.ErrorException != null)
			{
				const string message = "Error retrieving response.  Check inner details for more info.";
				var apiException = new ApplicationException(message, response.ErrorException);
				throw apiException;
			}

            HttpWebRequest.DefaultCachePolicy = cachePolicy;

            return response.Data;
		}

        private IEnumerable<Artifact> SearchArtifactsImpl(IEnumerable<ArtifactSearch> searches)
        {
            while (searches.Any())
            {
                var request = new RestRequest(Method.POST);

                request.Resource = "search/artifact/";
                request.RequestFormat = DataFormat.Json;

                request.AddBody(searches.Take(PageSize));

                foreach (var artifact in Execute<List<Artifact>>(request))
                {
                    yield return artifact;
                };

                searches = searches.Skip(PageSize);
            }
        }

        public IList<Artifact> SearchArtifacts(IEnumerable<ArtifactSearch> searches)
        {
            return this.SearchArtifactsImpl(searches).ToList();
        }

        private IEnumerable<Artifact> SearchArtifactImpl(ArtifactSearch search)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "search/artifact/{pm}/{name}/{version}";
            request.RequestFormat = DataFormat.Json;

            request.AddParameter("pm", search.pm, ParameterType.UrlSegment);
            request.AddParameter("name", search.name, ParameterType.UrlSegment);
            request.AddParameter("version", search.version, ParameterType.UrlSegment);

            foreach (var artifact in Execute<List<Artifact>>(request))
            {
                yield return artifact;
            };
        }

        public IList<Artifact> SearchArtifact(ArtifactSearch search)
        {
            return this.SearchArtifactImpl(search).ToList();
        }

        private IEnumerable<SCM> GetSCMsImpl(IEnumerable<long> scmIds)
        {
            while (scmIds.Any())
            {
                var request = new RestRequest(Method.GET);

                request.Resource = "scm/{id}";
                request.RequestFormat = DataFormat.Json;

                request.AddParameter("id", string.Join(",", scmIds.Take(PageSize)), ParameterType.UrlSegment);

                foreach (var scm in Execute<List<SCM>>(request))
                {
                    yield return scm;
                }

                scmIds = scmIds.Skip(PageSize);
            }
        }

        public IList<SCM> GetSCMs(IEnumerable<long> scmIds)
        {
            return this.GetSCMsImpl(scmIds).ToList();
        }

        private IEnumerable<Vulnerability> GetScmVulnerabilitiesImpl(long scmId)
        {
            var request = new RestRequest(Method.GET);

            request.Resource = "scm/{id}/vulnerabilities";
            request.AddParameter("id", scmId, ParameterType.UrlSegment);

            return Execute<List<Vulnerability>>(request);
        }

        public IList<Vulnerability> GetScmVulnerabilities(long scmId)
        {
            return this.GetScmVulnerabilitiesImpl(scmId).ToList();
        }
    }
}
