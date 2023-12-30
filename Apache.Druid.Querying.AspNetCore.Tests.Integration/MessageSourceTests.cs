﻿using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using static Apache.Druid.Querying.AspNetCore.Tests.Integration.ServiceProvider;

namespace Apache.Druid.Querying.AspNetCore.Tests.Integration;

internal static class TestExtensions
{
    public static TSelf Defaults<TSelf>(this IQuery<TSelf> query) where TSelf :
        IQueryWith.Granularity,
        IQueryWith.Intervals,
        IQueryWith.Filter<Message, TSelf>,
        IQueryWith.Aggregations<Message, MessageSourceTests.Aggregations, TSelf>,
        IQuery<TSelf>
    {
        var t = DateTime.Parse("2023-10-19T16:57:00.000Z", null, DateTimeStyles.AssumeUniversal).ToUniversalTime();
        return query
            .Unwrapped
            .Interval(new(t, t.AddDays(1)))
            .Granularity(Granularity.FifteenMinutes)
            .Filter(filter => filter.And(
                filter.Selector(
                    message => message.VariableName,
                    "pmPAct"),
                filter.Selector(
                    message => message.TenantId,
                    Guid.Parse("55022f5d-d9c4-4773-86e5-fbce823cd287"))))
            .Aggregations(factory => new(
                factory.Sum(message => message.Value),
                factory.Count(),
                factory.First(message => message.VariableName),
                factory.First(message => message.Value, SimpleDataType.String)
            ));
    }
}

internal class MessageSourceTests
{
    private static DataSource<Message> Messages => Services.GetRequiredService<DataSource<Message>>();

    [Test]
    public async Task TopNQuery_ReturnsAnything()
    {
        var query = new Query<Message>
            .TopN<Dimension>
            .WithNoVirtualColumns
            .WithAggregations<Aggregations>
            .WithPostAggregations<PostAggregations>()
            .Defaults()
            .PostAggregations(factory =>
                new(factory.Arithmetic(
                    ArithmeticFunction.Divide,
                    factory.FieldAccess(aggrgations => aggrgations.Sum),
                    factory.FieldAccess(aggregations => aggregations.Count))))
            .Dimension(factory => new(factory.Default(message => message.ObjectId)))
            .Metric(factory => factory.Numeric(tuple => tuple.Aggregations.Count))
            .Threshold(1000);
        try
        {
            var result = await Messages
                .ExecuteQuery(query)
                .ToListAsync();
            result.Should().NotBeEmpty();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    [Test]
    public async Task TimeSeriesQuery_ReturnsAnything()
    {
        var query = new Query<Message>
            .TimeSeries
            .WithNoVirtualColumns
            .WithAggregations<Aggregations>
            .WithPostAggregations<PostAggregations>()
            .Defaults()
            .PostAggregations(factory => new(
                factory.Arithmetic(
                    ArithmeticFunction.Divide,
                    factory.FieldAccess(aggrgations => aggrgations.Sum),
                    factory.FieldAccess(aggregations => aggregations.Count))))
            .Context(new() { SkipEmptyBuckets = true });
        var result = await Messages
            .ExecuteQuery(query)
            .ToListAsync();
        result.Should().NotBeEmpty();
    }

    public sealed record Aggregations(double Sum, int Count, string Variable, double? FirstValue);
    public sealed record PostAggregations(double Average);
    public sealed record Dimension(Guid ObjectId);
}
