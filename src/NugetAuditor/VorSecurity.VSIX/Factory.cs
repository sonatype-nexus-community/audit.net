using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetAuditor.VSIX
{
    static class Factory
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider ServiceProvider
        {
            set
            {
                _serviceProvider = value;
                ProjectUtilities.SetServiceProvider(_serviceProvider);
            }

            get { return _serviceProvider; }
        }

        private static ErrorListProvider _errorListProvider;

        public static ErrorListProvider GetErrorListProvider()
        {
            if (_errorListProvider == null)
            {
                _errorListProvider = new ErrorListProvider(_serviceProvider);
            }
            return _errorListProvider;
        }

        public static void CleanupFactory()
        {
            _errorListProvider = null;
        }
    }
}
