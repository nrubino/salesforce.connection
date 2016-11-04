using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using salesforce.connection.Salesforce;

namespace salesforce.connection
{
    /// <summary>
    /// Class SalesforceConnection.
    /// </summary>
    /// <seealso cref="ISalesforceConnection" />
    public class SalesforceConnection : ISalesforceConnection
    {
        /// <summary>
        /// The binding
        /// </summary>
        private SforceService _binding;
        /// <summary>
        /// The connection pool
        /// </summary>
        private ISalesforceConnectionPool _connectionPool;

        /// <summary>
        /// Gets the created.
        /// </summary>
        /// <value>The created.</value>
        public DateTime Created { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="SalesforceConnection"/> class.
        /// </summary>
        /// <param name="connectionPool">The connection pool.</param>
        public SalesforceConnection(ISalesforceConnectionPool connectionPool)
        {
            Created = DateTime.Now;
            var username = ConfigurationManager.AppSettings["SalesforceUsername"];
            var password = ConfigurationManager.AppSettings["SalesforcePassword"];
            _connectionPool = connectionPool;
            Login(username, password);
        }

        /// <summary>
        /// Logins the specified username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <exception cref="System.Exception">Salesforce Password has expired</exception>
        private void Login(string username, string password)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Create a service object 
            _binding = new SforceService { Timeout = 15000 };

            // Try logging in
            var lr = _binding.login(username, password);

            // Check if the password has expired 
            if (lr.passwordExpired)
            {
                throw new Exception("Salesforce Password has expired");
            }

            // Set returned service endpoint URL
            _binding.Url = lr.serverUrl;

            /** Now have an instance of the SforceService
             * that is pointing to the correct endpoint. Next, the sample client
             * application sets a persistent SOAP header (to be included on all
             * subsequent calls that are made with SforceService) that contains the
             * valid sessionId for our login credentials. To do this, the sample
             * client application creates a new SessionHeader object and persist it to
             * the SforceService. Add the session ID returned from the login to the
             * session header
             */
            _binding.SessionHeaderValue = new SessionHeader { sessionId = lr.sessionId };
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _connectionPool.PutConnection(this);
        }

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        public void Logout()
        {
            _binding.logout();
            _binding.Dispose();
        }
        /// <summary>
        /// Queries the specified soql query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="soqlQuery">The soql query.</param>
        /// <returns>List&lt;T&gt;.</returns>
        public List<T> Query<T>(string soqlQuery) where T : sObject
        {
            QueryResult qr = _binding.query(soqlQuery);
            bool done = false;

            var l = new List<T>();
            if (qr.size <= 0) return l;
            while (!done)
            {
                if (qr.records != null)
                    l.AddRange(qr.records.Select(r => (T)r));

                if (qr.done)
                    done = true;
                else
                    qr = _binding.queryMore(qr.queryLocator);
            }
            return l;
        }

        /// <summary>
        /// Saves the specified so.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="so">The so.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.Exception"></exception>
        public T Save<T>(T so) where T : sObject
        {
            SaveResult[] saveResults;

            if (string.IsNullOrEmpty(so.Id))
            {
                saveResults = _binding.create(new sObject[] { so });
            }
            else
            {
                saveResults = _binding.update(new sObject[] { so });
            }


            if (saveResults.Any(sr => !sr.success))
            {
                var errorString = string.Join("", saveResults.Where(sr => !sr.success)
                    .Select(
                            (sr, i) => $"\r\n\tError {i}:\r\n\t\t" + string.Join("\r\n\t\t", sr.errors.Select(e => e.message))));

                throw new Exception($"Salesforce save failed with following errors: {errorString}");
            }

            so.Id = saveResults[0].id;
            return so;
        }


        /// <summary>
        /// Bulks the save.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sos">The sos.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception">
        /// </exception>
        public int BulkSave<T>(List<T> sos) where T : sObject
        {
            sObject[] newObjects = sos.Where(so => string.IsNullOrEmpty(so.Id)).ToArray();
            sObject[] updateObjects = sos.Where(so => !string.IsNullOrEmpty(so.Id)).ToArray();
            var updatedEntities = 0;

            if (newObjects.Any())
            {
                foreach (var sosPartition in newObjects.Partition(200).ToList())
                {
                    var t = sosPartition.ToArray();
                    var saveResults = _binding.create(t);


                    if (saveResults.Any(sr => !sr.success))
                    {
                        var errorString = string.Join("", saveResults.Where(sr => !sr.success)
                            .Select(
                                (sr, i) => $"\r\n\tError {i}:\r\n\t\t" + string.Join("\r\n\t\t", sr.errors.Select(e => e.message))));

                        throw new Exception($"Salesforce bulk save failed with following errors: {errorString}");
                    }

                    for (int i = 0; i < t.Length; i++)
                    {
                        t[i].Id = saveResults[i].id;
                    }

                    updatedEntities += saveResults.Length;
                }
            }
            if (updateObjects.Any())
            {
                foreach (var sosPartition in updateObjects.Partition(200).ToList())
                {
                    var t = sosPartition.ToArray();
                    var saveResults = _binding.create(t);


                    if (saveResults.Any(sr => !sr.success))
                    {
                        var errorString = string.Join("", saveResults.Where(sr => !sr.success)
                            .Select(
                                (sr, i) => $"\r\n\tError {i}:\r\n\t\t" + string.Join("\r\n\t\t", sr.errors.Select(e => e.message))));

                        throw new Exception($"Salesforce bulk save failed with following errors: {errorString}");
                    }

                    for (int i = 0; i < t.Length; i++)
                    {
                        t[i].Id = saveResults[i].id;
                    }

                    updatedEntities += saveResults.Length;
                }
            }
            return updatedEntities;
        }

        /// <summary>
        /// Deletes the specified identifier list.
        /// </summary>
        /// <param name="idList">The identifier list.</param>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception"></exception>
        public int Delete(List<string> idList)
        {
            if (idList.Count <= 0) return 0;

            //Need to partition 200 at a time because salesforce limit.
            var partitionedIds = idList.Partition(200);
            var deleteResults = new List<DeleteResult>();

            foreach (var ids in partitionedIds)
                deleteResults.AddRange(_binding.delete(ids.ToArray()));

            if (deleteResults.All(sr => sr.success)) return deleteResults.Count;

            var errorString = string.Join(string.Empty, deleteResults.Where(sr => !sr.success)
                .Select(
                    (sr, i) => $"\r\n\tError {i}:\r\n\t\t" + string.Join("\r\n\t\t", sr.errors.Select(e => e.message))));

            throw new Exception($"Salesforce delete failed with following errors: {errorString}");

        }
    }
}
