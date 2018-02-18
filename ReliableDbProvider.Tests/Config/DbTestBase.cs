using System;
using System.Configuration;
using System.Data.Common;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ServiceController _serviceController = new ServiceController { MachineName = Environment.MachineName, ServiceName = ConfigurationManager.AppSettings["SqlServerServiceName"] };

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

        protected CancellableTask TemporarilyShutdownSqlServerExpress()
        {
            var tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!tokenSource.IsCancellationRequested)
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
            }, tokenSource.Token);
            return new CancellableTask(tokenSource);
        }
        
        protected class CancellableTask : IDisposable
        {
            private readonly CancellationTokenSource _tokenSource;

            public CancellableTask(CancellationTokenSource tokenSource)
            {
                _tokenSource = tokenSource;
            }

            public void Dispose()
            {
                _tokenSource.Cancel();
            }
        }
        #endregion
    }
}
