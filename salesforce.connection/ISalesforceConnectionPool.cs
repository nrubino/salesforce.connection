using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salesforce.connection
{
    public interface ISalesforceConnectionPool : IDisposable
    {
        ISalesforceConnection GetConnection(DateTime? timeoutTime = null);
        void PutConnection(ISalesforceConnection connection);
    }
}
