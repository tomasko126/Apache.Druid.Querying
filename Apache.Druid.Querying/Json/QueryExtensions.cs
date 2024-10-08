﻿using Apache.Druid.Querying.Internal;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Apache.Druid.Querying.Json
{
    public static class QueryExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo[]> cache = new();

        internal static JsonObject MapToJson(
            this IQueryWith.State query,
            QueryToJsonMappingContext context)
        {
            if (!cache.TryGetValue(query.GetType(), out var methods))
                methods = query
                    .GetGenericInterfaces(typeof(IQueryWithInternal.JsonApplicableState<>))
                    .Select(@interface => @interface.GetMethod(nameof(IQueryWithInternal.JsonApplicableState<None>.ApplyOnJson), BindingFlags.NonPublic | BindingFlags.Instance))
                    .ToArray()!;
            var result = new JsonObject();
            var parameters = new object[] { result, context };
            foreach (var method in methods)
                method.Invoke(query, parameters);
            if (query is IQueryWith.OnMapToJson onMapToJson && onMapToJson.State is not null)
                foreach (var onMap in onMapToJson.State)
                {
                    onMap(query, result);
                }
            return result;
        }

        public static JsonObject MapToJson<TSource>(
            this IQueryWith.Source<TSource> query,
            JsonSerializerOptions? querySerializerOptions = null,
            JsonSerializerOptions? dataSerializerOptions = null)
            => MapToJson(
                query,
                new QueryToJsonMappingContext(
                    querySerializerOptions ?? DefaultSerializerOptions.Query.ReadOnlySingleton,
                    dataSerializerOptions ?? DefaultSerializerOptions.Data.ReadOnlySingleton,
                    PropertyColumnNameMapping.ImmutableBuilder.Create<TSource>()));
    }
}
