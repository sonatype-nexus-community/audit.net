using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.Lib.OSSIndex
{
    public class ApiClientException : Exception
    {
        const string apiErrorMessage = "Unexpected server response! (Code: {0}, Error: {1})";

        public ApiClientException(string message)
            : base(message)
        {
            
        }

        public ApiClientException(string message, Exception innerException)
            : base(message, innerException)
        {

        }       
    }
}
