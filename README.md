# salesforce.connection
.NET Connection and Connection Pool for Salesforce SOAP API.  Navigate to [this blog post](https://blog.tallan.com/2016/11/21/salesforce-net-connection-pool/) for design and use case explanation.

# Setup
For unity you can use a singleton registration (ContainerControlledLifetimeManager) for the connection pool and then a HierarchicalLifetimeManager to automatically call the custom dispose method on the SalesforceConnection object (the dispose method is overridden on SalesforceConnectionPool to put the instance back into the connection pool).

```csharp
//Register Singleton connection pool.
container.RegisterType<ISalesforceConnectionPool, SalesforceConnectionPool>(new ContainerControlledLifetimeManager());

//This registration calls the SalesforceConnectionPool Singleton to get a connection everytime a connection
//is requested.  The HierarchicalLifetimeManager is important here because it is makes sure the Dispose()
//method is called on the SalesforceConnection that puts it back into the SalesforceConnectionPool.
container.RegisterType<ISalesforceConnection>(new HierarchicalLifetimeManager(),
    new InjectionFactory(s => container.Resolve<ISalesforceConnectionPool>().GetConnection()));
```

# Examples
Examples of CRUD opperations provided byt the SalesforceConnection class.  These are also available in the Program.cs of the salesforce.connection.unity.demo project

## Querying

```csharp
using (var salesforceConnection = container.Resolve<ISalesforceConnection>())
{
    //Query for all contacts and print information to console
    var contacts = salesforceConnection.Query<Contact>("SELECT Id, FirstName, LastName FROM Contact");
    Console.WriteLine("All Contacts");
    contacts.ForEach(c => Console.WriteLine($"{c.FirstName} {c.LastName} (Id: {c.Id})"));
    Console.WriteLine();
}
```

## Saving and Deleting

```csharp
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
```

## Bulk operations

```csharp
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
```
