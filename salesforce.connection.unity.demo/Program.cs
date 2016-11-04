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

            //using the resolve method of the container, when using with Web API or other applications this will be 
            //take care of with the Constuctor resolution.
            using (var salesforceConnection = container.Resolve<ISalesforceConnection>())
            {
                //Query for all contacts and print information to console
                var contacts = salesforceConnection.Query<Contact>("SELECT Id, FirstName, LastName FROM Contact");
                Console.WriteLine("All Contacts");
                contacts.ForEach(c => Console.WriteLine($"{c.FirstName} {c.LastName} (Id: {c.Id})"));
                Console.WriteLine();

            }

            //This resolution of the salesforce connection will be faster due to it being 
            //ready in the pool from the above connection
            using (var salesforceConnection = container.Resolve<ISalesforceConnection>())
            {
                //Create new contact
                var newContact = new Contact {FirstName = "Sales", LastName = "Force"};
                salesforceConnection.Save(newContact);
                Console.WriteLine("New Contact");
                Console.WriteLine($"{newContact.FirstName} {newContact.LastName} (Id: {newContact.Id})");
                Console.WriteLine();

                //Delete Contact
                salesforceConnection.Delete(new List<string> {newContact.Id});

            }
            using (var salesforceConnection = container.Resolve<ISalesforceConnection>())
            {
                //Bulk Create Contacts
                var contactList = new List<Contact>();
                for (int i = 0; i < 10; i++)
                {
                    contactList.Add(new Contact {FirstName = $"{i}_Sales_{i}", LastName = $"{i}_Force_{i}"});
                }
                salesforceConnection.BulkSave(contactList);

                //Print contacts to show new Ids
                Console.WriteLine("New Contacts Saved using BulkSave");
                contactList.ForEach(c => Console.WriteLine($"{c.FirstName} {c.LastName} (Id: {c.Id})"));

                //Bulk Delete Contacts
                salesforceConnection.Delete(contactList.Select(c => c.Id).ToList());
            }
        }
    }
}
