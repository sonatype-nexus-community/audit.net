using System.Collections.Generic;

namespace NugetAuditor.Lib.OSSIndex
{
    internal interface IApiClient
    {
        IList<SCM> GetSCMs(IEnumerable<long> scmIds);
        IList<SCM> GetSCM(long id);
        IList<Vulnerability> GetVulnerabilities(long id);
        IList<Artifact> SearchArtifacts(IEnumerable<ArtifactSearch> searches);
        IList<Artifact> SearchArtifact(ArtifactSearch search);
        IList<Artifact> SearchArtifact(string pm, string name, string version);
    }
}