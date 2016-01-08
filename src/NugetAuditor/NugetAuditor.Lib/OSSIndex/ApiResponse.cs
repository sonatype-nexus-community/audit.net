using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
{
    internal interface IApiResponse
    {
        int Code { get; set; }
        string Error { get; set; }
    }

    internal abstract class ApiResponse<T> : List<T>, IApiResponse
    {
        public int Code { get; set; }
        public string Error { get; set; }
    }
}
