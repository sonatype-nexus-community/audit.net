using System.Collections.Generic;

namespace NugetAuditor.Lib.OSSIndex
{
    public interface IApiClient
    {
        IList<SCM> GetSCMs(IEnumerable<long> scmIds);
        IList<Vulnerability> GetScmVulnerabilities(long scmId);
        IList<Artifact> SearchArtifact(ArtifactSearch search);
        IList<Artifact> SearchArtifacts(IEnumerable<ArtifactSearch> searches);
    }
}