﻿using System;
using Foundatio.Parsers.ElasticQueries.Visitors;
using Foundatio.Parsers.LuceneQueries.Extensions;
using Foundatio.Parsers.LuceneQueries.Nodes;
using Foundatio.Parsers.LuceneQueries.Visitors;
using Nest;

namespace Foundatio.Parsers.ElasticQueries.Extensions {
    public static class DefaultSortNodeExtensions {
        public static IFieldSort GetDefaultSort(this TermNode node, IQueryVisitorContext context) {
            var elasticContext = context as IElasticQueryVisitorContext;
            if (elasticContext == null)
                throw new ArgumentException("Context must be of type IElasticQueryVisitorContext", nameof(context));

            string field = elasticContext.GetNonAnalyzedFieldName(node.Field);

            var sort = new SortField { Field = field };
            if (node.IsNodeOrGroupedParentNegated())
                sort.Order = SortOrder.Descending;
            else
                sort.Order = SortOrder.Ascending;

            return sort;
        }
    }
}
