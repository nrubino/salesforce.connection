using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using salesforce.connection.Salesforce;

namespace salesforce.connection.unity.demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new UnityContainer();

            //Register Singleton connection pool.
            container.RegisterType<ISalesforceConnectionPool, SalesforceConnectionPool>(new ContainerControlledLifetimeManager());

            //This registration calls the SalesforceConnectionPool Singleton to get a connection everytime a connection
            //is requested.  The HierarchicalLifetimeManager is important here because it is makes sure the Dispose()
            //method is called on the SalesforceConnection that puts it back into the SalesforceConnectionPool.
            container.RegisterType<ISalesforceConnection>(new HierarchicalLifetimeManager(),
                new InjectionFactory(s => container.Resolve<ISalesforceConnectionPool>().GetConnection()));


            using (var salesforceConnection = container.Resolve<ISalesforceConnectionPool>().GetConnection())
            {
                salesforceConnection.Query<Contact>("SELECT Id, FirstName, LastName FROM Contact");
            }
        }
    }
}
