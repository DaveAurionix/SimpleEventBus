using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using System;
using System.Linq;

namespace SimpleEventBus.UnitTests
{
    static class MockExtensions
    {
        public static void VerifyLogInformation<T>(this Mock<T> mockLogger, string expectedMessage)
             where T : class, ILogger
        {
            mockLogger.Verify(
                m => m.Log(
                    LogLevel.Information,
                    0,
                    It.Is<FormattedLogValues>(values => values.Single().Key == "{OriginalFormat}" && (string)values.Single().Value == expectedMessage),
                    null,
                    It.IsAny<Func<object, Exception, string>>()),
                Times.Once);
        }
    }
}
