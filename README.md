Reliable Db Provider
====================

Provides a Db Provider Factory that uses the Microsoft Transient Fault Handling library to allow for reliable SQL Azure connections when using Entity Framework 4, Entity Framework 5 or Linq 2 SQL.

Using the provider
------------------

To use the provider:

1. `Install-Package ReliableDbProvider`
2. Register the reliable providers in your `web.config` or `app.config` (this shows how to register the standard Sql Azure provider - see below for a custom implementation):

```xml
		  <system.data>
		    <DbProviderFactories>
		      <add name="Sql Azure Reliable Provider" invariant="ReliableDbProvider.SqlAzure" description="Reliable Db Provider for SQL Azure" type="ReliableDbProvider.SqlAzure.SqlAzureProvider, ReliableDbProvider" />
		      <add name="Sql Azure Reliable Provider With Timeout Retries" invariant="ReliableDbProvider.SqlAzureWithTimeoutRetries" description="Reliable Db Provider for SQL Azure with Timeout Retries" type="ReliableDbProvider.SqlAzureWithTimeoutRetries.SqlAzureProvider, ReliableDbProvider" />
		    </DbProviderFactories>
		  </system.data>
```
3. Set the provider name of your connection string to match the `invariant` of the provider:

```xml
		  <connectionStrings>
		    <connectionString name="Name" connectionString="ConnectionString" providerName="ReliableDbProvider.SqlAzure" />
		  </connectionStrings>
```
4. Use the connection string name when initialising the context (note: if you use the Azure Web Sites connection string replacement it replaces the provider name so you have to use the other approach about to be mentioned) or pass into the context a connection created using the provider, e.g.:

```c#
		var connection = ReliableDbProvider.SqlAzure.SqlAzureProvider.Instance.CreateConnection();
		connection.ConnectionString = ConfigurationManager.ConnectionStrings["Name"].ConnectionString;
```
5. If you would like to perform an action when a retry occurs then you can using:

```c#
		ReliableDbProvider.SqlAzure.SqlAzureDbProvider.CommandRetry += (sender, args) => ...;
		ReliableDbProvider.SqlAzure.SqlAzureDbProvider.ConnectionRetry += (sender, args) => ...;
```

Retrying for timeouts
---------------------

It's possible for Timeout exceptions to be both a [transient error caused by Azure and a legitimate timeout caused by unoptimised queries](http://social.msdn.microsoft.com/Forums/en-US/ssdsgetstarted/thread/7a50985d-92c2-472f-9464-a6591efec4b3/) so we've included a transient error detection strategy that detects these timeout exceptions as a transient error and retries. To use it simply change your provider from `ReliableDbProvider.SqlAzure.SqlAzureProvider` to `ReliableDbProvider.SqlAzureWithTimeoutRetries.SqlAzureProvider` (provider name from `ReliableDbProvider.SqlAzure` to `ReliableDbProvider.SqlAzureWithTimeoutRetries`).

There are a few things to note:

* We recommend you try the `ReliableDbProvider.SqlAzure` provider first and then add the one that detects timeouts as transient errors only after you experience timeout errors that you are sure are caused by SQL Azure and not your code
* If the timeout happened in the first place it means that the user's request has already taken a long time so applying a retry policy to that query will make it take even longer (and if the retries also timeout then the page request might even time out (for a web application)).
* If you want visibility of retries then see the above instruction for hooking into the retry events

Creating your own custom reliable provider
------------------------------------------

If you want to customise the transient error detection strategy, the retry strategies or other aspects then you can create your own provider.

1. Extend `ReliableSqlClientProvider`, e.g.:
```c#
		    public class MyReliableProvider : ReliableSqlClientProvider<ATransientErrorDetectionStrategy>
		    {
		        public static readonly MyReliableProvider Instance = new MyReliableProvider();
		
		        public static event EventHandler<RetryingEventArgs> CommandRetry;
		        public static event EventHandler<RetryingEventArgs> ConnectionRetry;
		
		        protected override RetryStrategy GetCommandRetryStrategy()
		        {
		            return /* command retry strategy, e.g. ReliableDbProvider.SqlAzure.RetryStrategies.DefaultCommandStrategy */;
		        }
		
		        protected override RetryStrategy GetConnectionRetryStrategy()
		        {
		            return /* connection retry strategy, e.g. ReliableDbProvider.SqlAzure.RetryStrategies.DefaultConnectionStrategy */;
		        }
		
		        protected override DbConnection GetConnection(ReliableSqlConnection connection)
		        {
		            connection.CommandRetryPolicy.Retrying += CommandRetry;
		            connection.ConnectionRetryPolicy.Retrying += ConnectionRetry;
		            return new MyReliableConnection(connection);
		        }
		    }
```
2. Extend `ReliableSqlDbConnection`; specifying the Db Provider you created as the factory, e.g.:
```c#
		    public class MyReliableConnection : ReliableSqlDbConnection
		    {
		        public MyReliableConnection(ReliableSqlConnection connection) : base(connection) { }
		        protected override DbProviderFactory GetProviderFactory()
		        {
		            return MyReliableProvider.Instance;
		        }
		    }
```
3. Add the provider to your `web.config`/`app.config`, e.g.:
```xml
		  <system.data>
		    <DbProviderFactories>
		      <add name="My Reliable Provider" invariant="MyAssemblyBaseNamespace.MyReliableProviderNamespace" description="Reliable Db Provider for something..." type="MyAssemblyBaseNamespace.MyReliableProviderNamespace.MyReliableProvider, MyAssembly" />
		    </DbProviderFactories>
		  </system.data>
```
4. Set the provider name of your connection string to match the `invariant` of your new provider:
```xml
		  <connectionStrings>
		    <connectionString name="Name" connectionString="ConnectionString" providerName="MyAssemblyBaseNamespace.MyReliableProviderNamespace" />
		  </connectionStrings>
```

Running the tests
-----------------

If you want to contribute to this library then you need to:

1. Load the solution (allow the NuGet package restore to grab all the packages)
2. Compile the solution (.NET 4, AnyCPU)
3. Create a database on your local SQLExpress instance called `ReliableDbProviderTests` and grant the user running the NUnit runner `dbowner` access.
    * If you want to use a different database simply change the `Database` ConnectionString in `App.config`, but note: you may also need to change the service name to stop / start in `Config\DbTestBase.cs`
4. Run the `ReliableDbProvider.Tests` project with your NUnit test runner of choice
    * The user running the tests must have Administrator access on the computer so that the Windows Service for the database can be shutdown and restarted
    * Note: Your database will be taken down and brought back up repeatedly when running the tests so only run them against a development database.
