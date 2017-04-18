using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetDefaultBrowser
{
    class EnvironmentException : Exception
    {
        public EnvironmentException(string message) : base(message)
        {
        }

        public EnvironmentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
