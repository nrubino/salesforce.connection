using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace salesforce.connection
{
    /// <summary>
    /// Class SalesforceConnectionPool.
    /// </summary>
    /// <seealso cref="salesforce.connection.ISalesforceConnectionPool" />
    public class SalesforceConnectionPool : ISalesforceConnectionPool
    {
        /// <summary>
        /// The connections
        /// </summary>
        private readonly ConcurrentBag<ISalesforceConnection> _connections;
        /// <summary>
        /// The salesforce connection pool capacity
        /// </summary>
        private readonly int _salesforceConnectionPoolCapacity;
        /// <summary>
        /// The salesforce connection pool get connection timeout
        /// </summary>
        private readonly int _salesforceConnectionPoolGetConnectionTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="SalesforceConnectionPool"/> class.
        /// </summary>
        public SalesforceConnectionPool()
        {
            _connections = new ConcurrentBag<ISalesforceConnection>();

            _salesforceConnectionPoolCapacity =
                int.Parse(ConfigurationManager.AppSettings["SalesforceConnectionPoolCapacity"]);
            _salesforceConnectionPoolGetConnectionTimeout =
                int.Parse(ConfigurationManager.AppSettings["SalesforceConnectionPoolGetConnectionTimeout"]);
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="timeoutTime">The timeout time.</param>
        /// <returns>ISalesforceConnection.</returns>
        /// <exception cref="System.Exception">Connection Pool Timed out</exception>
        public ISalesforceConnection GetConnection(DateTime? timeoutTime)
        {
            ISalesforceConnection connection;
            //If connection is available return it..
            if (_connections.TryTake(out connection))
            {
                //If the connection has been alive for less 60 minutes return it
                if ((DateTime.Now - connection.Created).Minutes < 60) return connection;

                //Otherwise logout and create a new connection.
                connection.Logout();
                return new SalesforceConnection(this);
            }

            //If there is enough space create another connection
            if (_salesforceConnectionPoolCapacity >= _connections.Count) return new SalesforceConnection(this);

            //Wait 1/2 second for connection to free up
            Thread.Sleep(500);

            //If it has been the timeout span for a connection to free up throw an exception
            if (timeoutTime.HasValue && timeoutTime.Value == DateTime.Now) throw new Exception("Connection Pool Timed out");

            //Call get connection method again recursivley
            return GetConnection(timeoutTime ?? DateTime.Now.AddMilliseconds(_salesforceConnectionPoolGetConnectionTimeout));
        }

        /// <summary>
        /// Puts the connection.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public void PutConnection(ISalesforceConnection connection)
        {
            _connections.Add(connection);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _connections.ToList().ForEach(c => c.Logout());
        }
    }
}
