using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SimpleEventBus.AzureServiceBusTransport.Failover
{
    static class FailoverExceptions
    {
        public static async Task<T> Try<T>(Func<Task<T>> action, Func<Exception, T> exceptionAction)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (ServiceBusException exception) when (exception.IsTransient)
            {
                return exceptionAction(exception);
            }
            catch (InvalidOperationException exception)
            {
                return exceptionAction(exception);
            }
            catch (TimeoutException exception)
            {
                return exceptionAction(exception);
            }
            catch (TaskCanceledException exception)
            {
                return exceptionAction(exception);
            }
        }

        public static async Task Try(Func<Task> action, Action<Exception> exceptionAction)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (ServiceBusException exception) when (exception.IsTransient)
            {
                exceptionAction(exception);
            }
            catch (InvalidOperationException exception)
            {
                exceptionAction(exception);
            }
            catch (TimeoutException exception)
            {
                exceptionAction(exception);
            }
            catch (TaskCanceledException exception)
            {
                exceptionAction(exception);
            }
        }

        public static Task<bool> Try(Func<Task> action, ConcurrentBag<Exception> exceptions)
            => Try(
                async () =>
                {
                    await action().ConfigureAwait(false);
                    return true;
                },
                exception =>
                {
                    exceptions.Add(exception);
                    return false;
                });
    }
}
