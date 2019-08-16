using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using System;
using System.Linq;

namespace SimpleEventBus.AzureServiceBusTransport.UnitTests
{
    static class MockExtensions
    {
        public static void VerifyLogWarningException<T, TException>(this Mock<T> mockLogger, string expectedMessage, string expectedExceptionMessage)
             where T : class, ILogger
        {
            mockLogger.Verify(
                m => m.Log(
                    LogLevel.Warning,
                    0,
                    It.Is<FormattedLogValues>(
                        values => VerifyLogMessage(values, expectedMessage)),
                    It.Is<Exception>(
                        actual => VerifyExceptionTypeAndMessage<TException>(expectedExceptionMessage, actual)),
                    It.IsAny<Func<object, Exception, string>>()),
                Times.Once);
        }

        private static bool VerifyLogMessage(FormattedLogValues values, string expectedMessage)
        {
            return values.Single().Key == "{OriginalFormat}" && (string)values.Single().Value == expectedMessage;
        }

        private static bool VerifyExceptionTypeAndMessage<TExpectedException>(string expectedExceptionMessage, Exception actualException)
        {
            return actualException.GetType() == typeof(TExpectedException)
                && actualException.Message == expectedExceptionMessage;
        }
    }
}
