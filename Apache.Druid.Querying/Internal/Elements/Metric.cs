﻿using System.Text.Json.Nodes;

namespace Apache.Druid.Querying.Internal.Elements
{
    internal abstract class Metric : WithType, IMetric
    {
        public sealed class Numeric : Metric
        {
            public Numeric(string metric)
            {
                Metric = metric;
            }

            public string Metric { get; }

        }

        public sealed class Dimension<TColumn> : Metric
        {
            public Dimension(SortingOrder ordering, JsonNode? previousStop)
            {
                Ordering = ordering;
                PreviousStop = previousStop;
            }

            public SortingOrder Ordering { get; }
            public JsonNode? PreviousStop { get; }
        }

        public sealed class Inverted : Metric
        {
            public IMetric Metric { get; }

            public Inverted(IMetric metric)
            {
                Metric = metric;
            }
        }
    }
}
