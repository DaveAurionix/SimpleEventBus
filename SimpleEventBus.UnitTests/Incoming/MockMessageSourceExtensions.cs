using Moq;
using SimpleEventBus.Abstractions.Incoming;
using System;

namespace SimpleEventBus.UnitTests.Incoming
{
    static class MockMessageSourceExtensions
    {
        public static void VerifyCompleteCalledOnce(this Mock<IMessageSource> mock, IncomingMessage message)
            => mock.Verify(m => m.Complete(message), Times.Once);

        public static void VerifyCompleteCalledNever(this Mock<IMessageSource> mock, IncomingMessage message)
            => mock.Verify(m => m.Complete(message), Times.Never);

        public static void VerifyAbandonCalledOnce(this Mock<IMessageSource> mock, IncomingMessage message)
            => mock.Verify(m => m.Abandon(message), Times.Once);

        public static void VerifyAbandonCalledNever(this Mock<IMessageSource> mock, IncomingMessage message)
            => mock.Verify(m => m.Abandon(message), Times.Never);

        public static void VerifyDeferCalledOnce(this Mock<IMessageSource> mock, IncomingMessage message, string deferralReason)
            => mock.Verify(m => m.DeferUntil(message, It.IsAny<DateTime>(), deferralReason, It.IsAny<string>()), Times.Once);

        public static void VerifyDeferCalledNever(this Mock<IMessageSource> mock, IncomingMessage message)
            => mock.Verify(m => m.DeferUntil(message, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        public static void VerifyDeadLetterCalledOnce(this Mock<IMessageSource> mock, IncomingMessage message, string deadletterReason)
            => mock.Verify(m => m.DeadLetter(message, deadletterReason, It.IsAny<string>()), Times.Once);

        public static void VerifyDeadLetterCalledNever(this Mock<IMessageSource> mock, IncomingMessage message)
            => mock.Verify(m => m.DeadLetter(message, It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
