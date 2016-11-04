using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using salesforce.connection.Salesforce;

namespace salesforce.connection
{
    public interface ISalesforceConnection : IDisposable
    {
        List<T> Query<T>(string soqlQuery) where T : sObject;
        T Save<T>(T so) where T : sObject;
        int Delete(List<string> ids);
        DateTime Created { get; }
        void Logout();
        int BulkSave<T>(List<T> sos) where T : sObject;
    }
}
