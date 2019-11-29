using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleEventBus.UnitTests
{
    static class MockExtensions
    {
        public static void VerifyLogged<T>(this Mock<T> mockLogger, LogLevel logLevel, string expectedMessage)
             where T : class, ILogger
        {
            mockLogger.Verify(
                m => m.Log(
                    logLevel,
                    0,
                    It.Is<It.IsAnyType>((values, type) => VerifyLogMessage(values, expectedMessage)),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        public static void VerifyLoggedMessageContains<T>(this Mock<T> mockLogger, LogLevel logLevel, string expectedPhrase)
             where T : class, ILogger
        {
            mockLogger.Verify(
                m => m.Log(
                    logLevel,
                    0,
                    It.Is<It.IsAnyType>((values, type) => VerifyLogMessageContains(values, expectedPhrase)),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        private static bool VerifyLogMessage(object values, string expectedMessage)
        {
            var typedValues = (IReadOnlyList<KeyValuePair<string, object>>)values;
            return typedValues.Single().Key == "{OriginalFormat}" && (string)typedValues.Single().Value == expectedMessage;
        }

        private static bool VerifyLogMessageContains(object values, string expectedPhrase)
        {
            var typedValues = (IReadOnlyList<KeyValuePair<string, object>>)values;
            return typedValues.Single().Key == "{OriginalFormat}" && ((string)typedValues.Single().Value).Contains(expectedPhrase, StringComparison.Ordinal);
        }
    }
}
