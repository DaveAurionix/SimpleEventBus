using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureSubscriptionDescription = Microsoft.Azure.ServiceBus.Management.SubscriptionDescription;
using SimpleSubscriptionDescription = SimpleEventBus.Abstractions.Incoming.SubscriptionDescription;

namespace SimpleEventBus.AzureServiceBusTransport
{
    internal class SubscriptionInitialiser : ISubscriptionInitialiser
    {
        private readonly AzureServiceBusTransportSettings settings;
        private readonly ILogger logger;
        private const int RuleSqlExpressionMaximumLength = 1024;

        public SubscriptionInitialiser(AzureServiceBusTransportSettings settings, ILogger logger)
        {
            this.settings = settings;
            this.logger = logger;
        }

        public async Task EnsureInitialised(SimpleSubscriptionDescription subscription, string connectionString, CancellationToken cancellationToken)
        {
            var client = new ManagementClient(connectionString);

            await EnsureTopicExists(client, cancellationToken)
                .ConfigureAwait(false);

            await EnsureSubscriptionExists(subscription, client, cancellationToken)
                .ConfigureAwait(false);

            await EnsureRulesUpToDate(subscription, client, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task EnsureTopicExists(ManagementClient client, CancellationToken cancellationToken)
        {
            if (await client
                .TopicExistsAsync(settings.SafeEffectiveTopicName, cancellationToken)
                .ConfigureAwait(false))
            {
                return;
            }

            logger.LogInformation(
                $"Topic {settings.SafeEffectiveTopicName} doesn't exist on the bus; creating the topic.");

            try
            {
                await client
                    .CreateTopicAsync(
                        new TopicDescription(settings.SafeEffectiveTopicName)
                        {
                            EnableBatchedOperations = true,
                            EnablePartitioning = settings.EnablePartitioning,
                            RequiresDuplicateDetection = false,
                            SupportOrdering = false
                        },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                logger.LogWarning(
                    $"Topic {settings.SafeEffectiveTopicName} has been created by another process that is competing with this process.");
            }
        }

        private async Task EnsureSubscriptionExists(SimpleSubscriptionDescription subscription, ManagementClient client, CancellationToken cancellationToken)
        {
            if (await client
                .SubscriptionExistsAsync(
                    settings.SafeEffectiveTopicName,
                    subscription.SafeSubscriptionName())
                .ConfigureAwait(false))
            {
                return;
            }

            logger.LogInformation(
                $"Subscription {subscription.SafeSubscriptionName()} doesn't exist on the topic; creating the subscription.");

            try
            {
                await client
                    .CreateSubscriptionAsync(
                        new AzureSubscriptionDescription(settings.SafeEffectiveTopicName, subscription.SafeSubscriptionName())
                        {
                            EnableBatchedOperations = true,
                            EnableDeadLetteringOnMessageExpiration = true,
                            LockDuration = TimeSpan.FromMinutes(1),
                            MaxDeliveryCount = 10,
                            RequiresSession = false
                        },
                        new RuleDescription("Default", new FalseFilter()),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MessagingEntityAlreadyExistsException)
            {
                logger.LogWarning(
                    $"Subscription {subscription.SafeSubscriptionName()} has been created by another process that is competing with this process.");
            }
        }

        private async Task EnsureRulesUpToDate(SimpleSubscriptionDescription subscription, ManagementClient client, CancellationToken cancellationToken)
        {
            var newRules = BuildRules(subscription)
                .ToArray();

            var existingRules = await client
                .GetRulesAsync(settings.SafeEffectiveTopicName, subscription.SafeSubscriptionName(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (var newRule in newRules)
            {
                if (existingRules.Any(existingRule => existingRule.Name == newRule.Name))
                {
                    continue;
                }

                logger.LogInformation(
                    $"Rule {newRule.Name} doesn't exist on the subscription; creating the rule.");

                try
                {
                    await client
                        .CreateRuleAsync(settings.SafeEffectiveTopicName, subscription.SafeSubscriptionName(), newRule, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (MessagingEntityAlreadyExistsException)
                {
                    logger.LogWarning(
                        $"Rule {newRule.Name} has been created by another process that is competing with this process.");
                }
            }

            foreach (var existingRule in existingRules)
            {
                if (newRules.Any(newRule => newRule.Name == existingRule.Name))
                {
                    continue;
                }

                logger.LogInformation(
                    $"Rule {existingRule.Name} is no longer neeed; deleting the rule.");

                try
                {
                    await client
                        .DeleteRuleAsync(settings.SafeEffectiveTopicName, subscription.SafeSubscriptionName(), existingRule.Name, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (MessagingEntityNotFoundException)
                {
                    logger.LogWarning(
                        $"Rule {existingRule.Name} has already been removed by another process that is competing with this process.");
                }
            }
        }

        private static IEnumerable<RuleDescription> BuildRules(SimpleSubscriptionDescription subscription)
        {
            var index = 0;
            var hashCode = subscription.GetHashCode();

            var domainUnderTestClause = BuildDomainUnderTestRuleClause(subscription.SafeSubscriptionName());

            yield return new RuleDescription(
                hashCode + "_" + (index++),
                new SqlFilter(
                    $"{domainUnderTestClause} AND EXISTS({TransportHeaders.SpecificEndpoint}) AND {TransportHeaders.SpecificEndpoint} = '{subscription.SafeSubscriptionName()}'"));

            var ruleStart = $"{domainUnderTestClause} AND NOT EXISTS({TransportHeaders.SpecificEndpoint}) AND (";

            var sql = new StringBuilder();
            var requireDelimiter = false;

            sql.Append(ruleStart);
            foreach (var messageType in subscription.MessageTypeNames)
            {
                if (sql.Length + messageType.Length + ruleStart.Length + 40 > RuleSqlExpressionMaximumLength)
                {
                    yield return BuildRuleAndResetBuilder(sql, hashCode, index, ruleStart);
                    index++;
                }
                else if (requireDelimiter)
                {
                    sql.Append(" OR ");
                }
                requireDelimiter = true;

                // TODO Enforce that type name does not contain a semi-colon or single quote or square bracket or underscore
                sql.Append("MessageTypeNames LIKE '%;")
                    .Append(messageType)
                    .Append(";%'");
            }
            sql.Append(")");

            yield return new RuleDescription(
                hashCode + "_" + (index++),
                new SqlFilter(
                    sql.ToString()));
        }

        private static string BuildDomainUnderTestRuleClause(string safeSubscriptionName)
            => $"(NOT EXISTS(DomainUnderTest) OR '{safeSubscriptionName}.' LIKE DomainUnderTest + '.%')";

        private static RuleDescription BuildRuleAndResetBuilder(StringBuilder sql, int hashCode, int index, string newRuleStart)
        {
            sql.Append(")");
            var rule = new RuleDescription(
                hashCode + "_" + index,
                new SqlFilter(
                    sql.ToString()));
            sql.Clear();
            sql.Append(newRuleStart);

            return rule;
        }
    }
}
