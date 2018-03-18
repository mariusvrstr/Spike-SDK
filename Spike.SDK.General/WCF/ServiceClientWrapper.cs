/*  // Create a new service client [Configured in Config] ()
  *  ServiceClientWrapper<[ServiceClient], [Service]>([Binding], [Endpoint]);
  *  Tip: Not sure which types to use? Check the inheritence of the generated client.
  *  
  *  e.g. var consumer = new ServiceClientWrapper<AuthorServiceClient, AuthorService>();                    // Configured In Config
  *  e.g. var consumer = new ServiceClientWrapper<AuthorServiceClient, AuthorService>(binding, endpoint);   // Configured In Code
  *    
  *  // Use the client wrapper to excecute the client operations
  *  author = consumer.Excecute(service => service.AddAuthor(request));
  */

namespace Spike.SDK.General.WCF
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Threading;
    using Instrumentation.Logging;

    public class ServiceClientWrapper<TClient, TIService> : IDisposable
            where TClient : ClientBase<TIService>, TIService
            where TIService : class
    {
        private TClient _serviceClient;
        private readonly Binding _binding;
        private readonly EndpointAddress _endpoint;
        private const int RetryCoolDownInSeconds = 1;

        public ServiceClientWrapper() { }
        public ServiceClientWrapper(Binding binding, EndpointAddress endpointAddress)
        {
            _binding = binding;
            _endpoint = endpointAddress;
        }

        public TClient ServiceClient
        {
            get
            {
                return _serviceClient = _serviceClient ?? CreateClient();
            }
        }

        public void Excecute(
            Action<TIService> serviceCall,
            int retryAttempts = 1,
            Action<CommunicationException> exceptionHandler = null)
        {
            Excecute<object>(
                service => { serviceCall.Invoke(service); return null; },
                retryAttempts,
                exceptionHandler);
        }

        public TResult Excecute<TResult>(
            Func<TIService, TResult> serviceCall,
            int retryAttempts = 1,
            Action<CommunicationException> exceptionHandler = null)
        {
            var errors = 0;
            var completed = false;
            CommunicationException exception = null;
            var response = default(TResult);

            while (!completed && errors < retryAttempts)
            {
                try
                {
                    if (!ServiceClient.State.IsReady())
                    {
                        DisposeClient();

                        if (!ServiceClient.State.IsReady())
                        {
                            throw new CommunicationObjectFaultedException(
                                $"WCF Client state is not valid. Connection Status [{ServiceClient.State}]");
                        }
                    }

                    response = serviceCall.Invoke(ServiceClient);
                    completed = true;
                }
                catch (CommunicationException comsException)
                {
                    var logger = LogFactory.Create();
                    exception = comsException;
                    if (exceptionHandler != null)
                    {
                        try
                        {
                            exceptionHandler.Invoke(exception);
                        }
                        catch (CommunicationException reThrowException)
                        {
                            exception = reThrowException;
                        }
                    }

                    errors++;
                    var logErrorMessage =
                        $"WCF Operation Failure: Service [{typeof(TClient)}].[{serviceCall.Method.Name}] Attempt ({errors}/{retryAttempts}). Exception [{exception.Message}]";
                    logger.Info(logErrorMessage);

                    if (retryAttempts <= 1) continue;

                    var logSleepMessage = $"Retry cooldown initiated ({RetryCoolDownInSeconds}s)";
                    logger.Info(logSleepMessage);

                    Thread.Sleep(new TimeSpan(0, 0, RetryCoolDownInSeconds));
                }
                finally
                {
                    if (!completed)
                    {
                        DisposeClient();
                    }
                    else
                    {
                        ServiceClient.Close();
                    }
                }
            }

            if (!completed)
            {
                throw exception ?? new CommunicationException(
                          $"WCF Operation Failure: Service [{typeof(TClient)}].[{serviceCall.Method.Name}]");
            }

            return response;
        }

        public void Dispose()
        {
            DisposeClient();
        }

        protected virtual TClient CreateClient()
        {
            if (_binding != null && _endpoint != null)
            {
                return (TClient)Activator.CreateInstance(typeof(TClient), _binding, _endpoint);
            }

            return (TClient)Activator.CreateInstance(typeof(TClient));
        }

        private void DisposeClient()
        {
            if (_serviceClient == null)
            {
                return;
            }

            try
            {
                switch (_serviceClient.State)
                {
                    case CommunicationState.Faulted:
                        _serviceClient.Abort();
                        break;

                    default:
                        _serviceClient.Close();
                        break;
                }
            }
            catch
            {
                _serviceClient.Abort();
            }
            finally
            {
                _serviceClient = null;
            }
        }
    }
    public static class ServiceExtentions
    {
        public static bool IsReady(this CommunicationState original)
        {
            switch (original)
            {
                case CommunicationState.Created:
                case CommunicationState.Opened:
                case CommunicationState.Opening:
                    return true;

                default:
                    return false;
            }
        }
    }

}
