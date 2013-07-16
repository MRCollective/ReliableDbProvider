using System;
using System.Configuration;
using System.Data.Common;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using NUnit.Framework;

namespace ReliableDbProvider.Tests.Config
{
    public abstract class PooledDbTestBase<T> : DbTestBase<T>
        where T : DbProviderFactory
    {
        protected override string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Database"].ConnectionString; }
        }
    }

    public abstract class NonPooledDbTestBase<T> : DbTestBase<T>
        where T : DbProviderFactory
    {
        protected override string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Database"].ConnectionString + ";Pooling=false"; }
        }
    }

    public abstract class LowTimeoutDbTestBase<T> : DbTestBase<T>
        where T : DbProviderFactory
    {
        protected override string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Database"].ConnectionString + ";Connection Timeout=1"; }
        }
    }

    public abstract class DbTestBase<T>
        where T : DbProviderFactory
    {
        protected abstract string ConnectionString { get; }
        
        protected virtual Context GetContext()
        {
            var provider = GetProvider();
            var connection = provider.CreateConnection();
            connection.ConnectionString = ConnectionString;
            return new Context(connection);
        }

        protected static DbProviderFactory GetProvider()
        {
            return (DbProviderFactory) typeof (T).GetField("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }

        #region SQLExpress shutdown code
        private readonly ServiceController _serviceController = new ServiceController { MachineName = Environment.MachineName, ServiceName = "MSSQL$SQLEXPRESS" };

        [TearDown]
        public void TearDown()
        {
            // Make sure that the service is running before stopping the test
            _serviceController.Refresh();
            if (_serviceController.Status == ServiceControllerStatus.PausePending)
                _serviceController.WaitForStatus(ServiceControllerStatus.Paused);
            if (_serviceController.Status == ServiceControllerStatus.ContinuePending)
                _serviceController.WaitForStatus(ServiceControllerStatus.Running);

            if (_serviceController.Status != ServiceControllerStatus.Running)
            {
                Console.WriteLine("SQLExpress service currently at {0} state; restarting...", _serviceController.Status);
                _serviceController.Continue();
                _serviceController.WaitForStatus(ServiceControllerStatus.Running);
            }
        }

        protected ThreadKiller TemporarilyShutdownSqlServerExpress()
        {
            var t = new Thread(MakeSqlTransient);
            t.Start();
            return new ThreadKiller(t);
        }

        private void MakeSqlTransient()
        {
            try
            {
                while (true)
                {
                    _serviceController.Refresh();
                    if (_serviceController.Status == ServiceControllerStatus.Running)
                        _serviceController.Pause();
                    _serviceController.WaitForStatus(ServiceControllerStatus.Paused);

                    _serviceController.Refresh();
                    _serviceController.Continue();
                    _serviceController.WaitForStatus(ServiceControllerStatus.Running);

                    Thread.Sleep(20);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while making SQL transient: {0}", e);
            }
        }

        protected class ThreadKiller : IDisposable
        {
            private readonly Thread _threadToWaitFor;

            public ThreadKiller(Thread threadToWaitFor)
            {
                _threadToWaitFor = threadToWaitFor;
            }

            public void Dispose()
            {
                _threadToWaitFor.Abort();
            }
        }
        #endregion
    }
}
