﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Foundatio.Parsers.LuceneQueries.Extensions;
using Foundatio.Parsers.LuceneQueries.Nodes;

namespace Foundatio.Parsers.LuceneQueries.Visitors {
    public class GetReferencedFieldsQueryVisitor : QueryNodeVisitorWithResultBase<ISet<string>> {
        private readonly HashSet<string> _fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public override Task VisitAsync(GroupNode node, IQueryVisitorContext context) {
            if (!String.IsNullOrEmpty(node.Field))
                AddField(node);

            return base.VisitAsync(node, context);
        }

        public override void Visit(TermNode node, IQueryVisitorContext context) {
            if (!String.IsNullOrEmpty(node.Field)) {
                AddField(node);
            } else {
                var nameParts = node.GetNameParts();
                if (nameParts.Length == 0)
                    _fields.Add("_all");
            }
        }

        public override void Visit(TermRangeNode node, IQueryVisitorContext context) {
            if (!String.IsNullOrEmpty(node.Field)) {
                AddField(node);
            } else {
                var nameParts = node.GetNameParts();
                if (nameParts.Length == 0)
                    _fields.Add("_all");
            }
        }

        public override void Visit(ExistsNode node, IQueryVisitorContext context) {
            if (!String.IsNullOrEmpty(node.Field))
                AddField(node);
        }

        public override void Visit(MissingNode node, IQueryVisitorContext context) {
            if (!String.IsNullOrEmpty(node.Field))
                AddField(node);
        }

        private void AddField(IFieldQueryNode node) {
            string field = String.Equals(node.GetQueryType(), QueryType.Query) ? node.GetFullName() : node.Field;
            if (!field.StartsWith("@"))
                _fields.Add(field);
        }

        public override Task<ISet<string>> AcceptAsync(IQueryNode node, IQueryVisitorContext context) {
            node.AcceptAsync(this, context);
            return Task.FromResult<ISet<string>>(_fields);
        }

        public static Task<ISet<string>> RunAsync(IQueryNode node, IQueryVisitorContext context = null) {
            return new GetReferencedFieldsQueryVisitor().AcceptAsync(node, context);
        }

        public static ISet<string> Run(IQueryNode node, IQueryVisitorContext context = null) {
            return RunAsync(node, context).GetAwaiter().GetResult();
        }
    }
}
