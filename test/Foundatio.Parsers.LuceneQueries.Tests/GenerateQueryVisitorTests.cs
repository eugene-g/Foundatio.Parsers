﻿using System;
using System.Threading.Tasks;
using Foundatio.Logging;
using Foundatio.Logging.Xunit;
using Foundatio.Parsers.ElasticQueries.Visitors;
using Foundatio.Parsers.LuceneQueries;
using Foundatio.Parsers.LuceneQueries.Nodes;
using Foundatio.Parsers.LuceneQueries.Visitors;
using Xunit;
using Xunit.Abstractions;

namespace Foundatio.Parsers.Tests {
    public class GenerateQueryVisitorTests : TestWithLoggingBase {
        public GenerateQueryVisitorTests(ITestOutputHelper output) : base(output) {}

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(":", null, false)]
        [InlineData("\":\"", "\":\"", true)]
        [InlineData("  \t", "", true)]
        [InlineData("string\"", "string\"", true)]
        [InlineData("\"quoted string\"", "\"quoted string\"", true)]
        [InlineData("criteria", "criteria", true)]
        [InlineData("(criteria)", "(criteria)", true)]
        [InlineData("field:criteria", "field:criteria", true)]
        [InlineData("field :criteria", "field:criteria", true)]
        [InlineData("-criteria", "-criteria", true)]
        [InlineData("+criteria", "+criteria", true)]
        [InlineData("criteria1 AND NOT criteria2", "criteria1 AND NOT criteria2", true)]
        [InlineData("criteria1 NOT criteria2", "criteria1 NOT criteria2", true)]
        [InlineData("field:criteria1 NOT field:criteria2", "field:criteria1 NOT field:criteria2", true)]
        [InlineData("criteria1   criteria2", "criteria1 criteria2", true)]
        [InlineData("criteria1 +criteria2", "criteria1 +criteria2", true)]
        [InlineData("criteria1 OR criteria2", "criteria1 OR criteria2", true)]
        [InlineData("criteria1 OR criteria2 OR criteria3", "criteria1 OR criteria2 OR criteria3", true)]
        [InlineData("criteria1 OR (criteria2 AND criteria3)", "criteria1 OR (criteria2 AND criteria3)", true)]
        [InlineData("field:[1 TO 2]", "field:[1 TO 2]", true)]
        [InlineData("field:{1 TO 2}", "field:{1 TO 2}", true)]
        [InlineData("field:[1 TO 2}", "field:[1 TO 2}", true)]
        [InlineData("field:(criteria1 criteria2)", "field:(criteria1 criteria2)", true)]
        [InlineData("data.field:(now criteria2)", "data.field:(now criteria2)", true)]
        [InlineData("field:(criteria1 OR criteria2)", "field:(criteria1 OR criteria2)", true)]
        [InlineData("field:*cr", "field:*cr", true)] // TODO lucene doesn't support wildcards at the beginning.
        [InlineData("field:c*r", "field:c*r", true)]
        [InlineData("field:cr*", "field:cr*", true)]
        [InlineData("field:*", "field:*", false)]
        [InlineData("date:>now", "date:>now", true)]
        [InlineData("date:<now", "date:<now", true)]
        [InlineData("_exists_:title", "_exists_:title", true)]
        [InlineData("_missing_:title", "_missing_:title", true)]
        [InlineData("book.\\*:(quick brown)", "book.\\*:(quick brown)", true)]
        [InlineData("date:[now/d-4d TO now/d+1d}", @"date:[now/d-4d TO now/d+1d}", true)]
        [InlineData("(date:[now/d-4d TO now/d+1d})", @"(date:[now/d-4d TO now/d+1d})", true)]
        [InlineData("data.date:>now", "data.date:>now", true)]
        [InlineData("data.date:[now/d-4d TO now/d+1d}", @"data.date:[now/d-4d TO now/d+1d}", true)]
        [InlineData("data.date:[2012-01-01 TO 2012-12-31]", "data.date:[2012-01-01 TO 2012-12-31]", true)]
        [InlineData("data.date:[* TO 2012-12-31]", "data.date:[* TO 2012-12-31]", true)]
        [InlineData("data.date:[2012-01-01 TO *]", "data.date:[2012-01-01 TO *]", true)]
        [InlineData("(data.date:[now/d-4d TO now/d+1d})", @"(data.date:[now/d-4d TO now/d+1d})", true)]
        [InlineData("criter~", "criter~", true)]
        [InlineData("criter~1", "criter~1", true)]
        [InlineData("roam~0.8", "roam~0.8", true)]
        [InlineData(@"date^""America/Chicago_Other""", @"date^America/Chicago_Other", true)]
        [InlineData("criter^2", "criter^2", true)]
        [InlineData("\"blah criter\"~1", "\"blah criter\"~1", true)]
        [InlineData("count:>1", "count:>1", true)]
        [InlineData(@"book.\*:test", "book.\\*:test", true)]
        [InlineData("count:>=1", "count:>=1", true)]
        [InlineData("count:[1..5}", "count:[1..5}", true)]
        [InlineData(@"count:a\:a", @"count:a\:a", true)]
        [InlineData("count:a:a", null, false)]
        [InlineData(@"count:a\:a more:stuff", @"count:a\:a more:stuff", true)]
        [InlineData("data.count:[1..5}", "data.count:[1..5}", true)]
        [InlineData("age:(>=10 AND < 20)", "age:(>=10 AND <20)", true)]
        [InlineData("age : >= 10", "age:>=10", true)]
        [InlineData("age:[1 TO 2]", "age:[1 TO 2]", true)]
        [InlineData("data.Windows-identity:ejsmith", "data.Windows-identity:ejsmith", true)]
        [InlineData("data.age:(>30 AND <=40)", "data.age:(>30 AND <=40)", true)]
        [InlineData("+>=10", "+>=10", true)]
        [InlineData(">=10", ">=10", true)]
        [InlineData("age:(+>=10)", "age:(+>=10)", true)]
        [InlineData("data.age:(+>=10 AND < 20)", "data.age:(+>=10 AND <20)", true)]
        [InlineData("data.age:(+>=10 +<20)", "data.age:(+>=10 +<20)", true)]
        [InlineData("data.age:(->=10 AND < 20)", "data.age:(->=10 AND <20)", true)]
        [InlineData("data.age:[10 TO *]", "data.age:[10 TO *]", true)]
        [InlineData("title:(full text search)^2", "title:(full text search)^2", true)]
        [InlineData("data.age:[* TO 10]", "data.age:[* TO 10]", true)]
        [InlineData("hidden:true AND data.age:(>30 AND <=40)", "hidden:true AND data.age:(>30 AND <=40)", true)]
        [InlineData("hidden:true", "hidden:true", true)]
        [InlineData("geo:\"Dallas, TX\"~75m", "geo:\"Dallas, TX\"~75m", true)]
        [InlineData("geo:\"Dallas, TX\"~75 m", "geo:\"Dallas, TX\"~75 m", true)]
        [InlineData("min:price geogrid:geo~6 count:(category count:subcategory avg:price min:price)", "min:price geogrid:geo~6 count:(category count:subcategory avg:price min:price)", true)]
        [InlineData("datehistogram:(date~2^-5\\:30 min:date)", "datehistogram:(date~2^-5\\:30 min:date)", true)]
        [InlineData("-type:404", "-type:404", true)]
        [InlineData("type:test?s", "type:test?s", true)]
        [InlineData("NOT Test", "NOT Test", true)]
        [InlineData("! Test", "! Test", true)] // The symbol ! can be used in place of the word NOT.
        [InlineData("type:?", "type:?", false)]
        // TODO: We don't yet support this.
        //[InlineData(@"type:\(1\+1\)\:2", @"type:\(1\+1\)\:2", true)] // https://lucene.apache.org/core/2_9_4/queryparsersyntax.html#Escaping%20Special%20Characters
        [InlineData("title:(+return +\"pink panther\")", "title:(+return +\"pink panther\")", true)]
        [InlineData("\"jakarta apache\" -\"Apache Lucene\"", "\"jakarta apache\" -\"Apache Lucene\"", true)]
        [InlineData("\"jakarta apache\"^4 \"Apache Lucene\"", "\"jakarta apache\"^4 \"Apache Lucene\"", true)]
        [InlineData("NOT \"jakarta apache\"", "NOT \"jakarta apache\"", true)]
        [InlineData(@"updated:2016-09-02T15\:41\:43.3385286Z", @"updated:2016-09-02T15\:41\:43.3385286Z", true)]
        [InlineData(@"updated:>2016-09-02T15\:41\:43.3385286Z", @"updated:>2016-09-02T15\:41\:43.3385286Z", true)]
        [InlineData(@"field1:""\""value1\""""", @"field1:""\""value1\""""", true)]
        public async Task CanGenerateQueryAsync(string query, string expected, bool isValid) {
            var parser = new LuceneQueryParser();
            Log.MinimumLevel = LogLevel.Information;

            IQueryNode result;
            try {
                result = await parser.ParseAsync(query);
            } catch (Exception ex) {
                Assert.False(isValid, ex.Message);
                return;
            }

            var nodes = await DebugQueryVisitor.RunAsync(result);
            _logger.Info(nodes);
            var generatedQuery = await GenerateQueryVisitor.RunAsync(result);
            Assert.Equal(expected, generatedQuery);
        }

        [Fact]
        public async Task CanGenerateSingleQueryAsync() {
            string query = "datehistogram:(date~2^-5\\:30 min:date max:date)";
            var parser = new LuceneQueryParser();

            IQueryNode result = await parser.ParseAsync(query);

            _logger.Info(await DebugQueryVisitor.RunAsync(result));
            var generatedQuery = await GenerateQueryVisitor.RunAsync(result);
            Assert.Equal(query, generatedQuery);

            await new AssignAggregationTypeVisitor().AcceptAsync(result, null);
            _logger.Info(DebugQueryVisitor.Run(result));
        }
    }
}
