using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Index;

namespace NuGet.Indexing
{
    public static class LuceneQueryCreator
    {
        private const string DefaultTermName = "__default";

        private static readonly ISet<string> AllowedNuGetFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "id",
            "packageid",
            "version",
            "author",
            "authors",
            "owner",
            "owners"
        };

        public static Query Parse(string inputQuery, bool rawLuceneQuery)
        {
            if (string.IsNullOrWhiteSpace(inputQuery))
            {
                return new MatchAllDocsQuery();
            }

            if (rawLuceneQuery)
            {
                return CreateRawQuery(inputQuery);
            }

            // Parse the query our query parser
            PackageQueryParser parser = new PackageQueryParser(Lucene.Net.Util.Version.LUCENE_30, DefaultTermName, new PackageAnalyzer());
            Query query = parser.Parse(inputQuery);

            // Process the query into clauses
            List<BooleanClause> clausesCollector = new List<BooleanClause>();
            string nugetQuery = ExtractLuceneClauses(query, inputQuery, clausesCollector);

            // Rewrite Id clauses into "TokenizedId, ShingledId, Id" booleans
            IEnumerable<BooleanClause> luceneClauses = clausesCollector.Select(RewriteClauses);

            // Now, take the nuget query, if there is one, and process it
            Query nugetParsedQuery = ParseNuGetQuery(nugetQuery);

            // Build the lucene clauses query
            BooleanQuery luceneQuery = BuildBooleanQuery(luceneClauses);

            // Build the final query
            return Combine(nugetParsedQuery, luceneQuery);
        }

        private static BooleanClause RewriteClauses(BooleanClause arg)
        {
            TermQuery tq = arg.Query as TermQuery;
            if(tq == null) 
            {
                return arg;
            }

            if (String.Equals(tq.Term.Field, "id", StringComparison.OrdinalIgnoreCase))
            {
                return new BooleanClause(
                    BuildBooleanQuery(new [] {
                        new BooleanClause(new TermQuery(new Term("Id", tq.Term.Text)), Occur.SHOULD),
                        new BooleanClause(new TermQuery(new Term("TokenizedId", tq.Term.Text)), Occur.SHOULD),
                        new BooleanClause(new TermQuery(new Term("ShingledId", tq.Term.Text)), Occur.SHOULD)
                    }), arg.Occur);
            }
            else if (String.Equals(tq.Term.Field, "packageid", StringComparison.OrdinalIgnoreCase))
            {
                return new BooleanClause(new TermQuery(new Term("Id", tq.Term.Text)), arg.Occur);
            }
            else 
            {
                return arg;
            }
        }

        public static Query CreateRawQuery(string q)
        {
            if (String.IsNullOrEmpty(q))
            {
                return null;
            }

            QueryParser parser = new PackageQueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", new PackageAnalyzer());
            Query query = parser.Parse(q);

            return query;
        }

        private static string ExtractLuceneClauses(Query query, string inputQuery, List<BooleanClause> luceneClauses)
        {
            BooleanQuery boolQuery = query as BooleanQuery;
            if (boolQuery != null)
            {
                // Process the query to extract the "nuget query" part and the "lucene filters" part
                StringBuilder nugetQuery = new StringBuilder();
                foreach (BooleanClause clause in boolQuery.Clauses)
                {
                    TermQuery tq = clause.Query as TermQuery;
                    if (tq != null && !AllowedNuGetFields.Contains(tq.Term.Field))
                    {
                        // Ignore fields we don't accept in NuGet-style queries
                        // And, add in terms that aren't labelled with a field.
                        nugetQuery.Append(tq.Term.Text);
                        nugetQuery.Append(" ");
                    }
                    else
                    {
                        // Convert the clause to a MUST-have clause
                        clause.Occur = Occur.MUST;
                        luceneClauses.Add(clause);
                    }
                }
                if(nugetQuery.Length > 0) {
                    nugetQuery.Length -= 1; // Strip trailing space.
                }
                return nugetQuery.ToString();
            }
            else
            {
                TermQuery tq = query as TermQuery;
                if (tq == null || !String.Equals(tq.Term.Field, DefaultTermName, StringComparison.Ordinal))
                {
                    luceneClauses.Add(new BooleanClause(query, Occur.SHOULD));
                    return null;
                }
                else
                {
                    // Term Query is not null and refers to the default term
                    Debug.Assert(tq != null);
                    return tq.Term.Text;
                }
            }
        }

        private static Query ParseNuGetQuery(string nugetQuery)
        {
            Query nugetParsedQuery = null;
            if (!String.IsNullOrEmpty(nugetQuery))
            {
                // Process the query
                StringBuilder luceneQuery = new StringBuilder();

                CreateFieldClause(luceneQuery, "Id", nugetQuery);
                CreateFieldClause(luceneQuery, "Version", nugetQuery);
                CreateFieldClause(luceneQuery, "TokenizedId", nugetQuery);
                CreateFieldClauseAND(luceneQuery, "TokenizedId", nugetQuery, 4);
                CreateFieldClause(luceneQuery, "ShingledId", nugetQuery);
                CreateFieldClause(luceneQuery, "Title", nugetQuery, 2);
                CreateFieldClauseAND(luceneQuery, "Title", nugetQuery, 4);
                CreateFieldClause(luceneQuery, "Tags", nugetQuery);
                CreateFieldClause(luceneQuery, "Description", nugetQuery);
                CreateFieldClause(luceneQuery, "Authors", nugetQuery);
                CreateFieldClause(luceneQuery, "Owners", nugetQuery);

                PackageQueryParser parser = new PackageQueryParser(Lucene.Net.Util.Version.LUCENE_30, "Title", new PackageAnalyzer());
                nugetParsedQuery = parser.Parse(luceneQuery.ToString());
            }
            return nugetParsedQuery;
        }

        private static BooleanQuery BuildBooleanQuery(IEnumerable<BooleanClause> luceneClauses)
        {
            BooleanQuery luceneQuery = null;
            if (luceneClauses.Any())
            {
                luceneQuery = new BooleanQuery();
                foreach (BooleanClause clause in luceneClauses)
                {
                    luceneQuery.Add(clause);
                }
            }
            return luceneQuery;
        }

        private static Query Combine(Query nugetParsedQuery, BooleanQuery luceneQuery)
        {
            if (nugetParsedQuery == null)
            {
                return luceneQuery;
            }
            else if (luceneQuery == null)
            {
                return nugetParsedQuery;
            }
            else
            {
                BooleanQuery q = new BooleanQuery();
                q.Clauses.Add(new BooleanClause(nugetParsedQuery, Occur.SHOULD));
                q.Clauses.AddRange(luceneQuery.Clauses);
                return q;
            }
        }

        static void CreateFieldClause(StringBuilder luceneQuery, string field, string query, float boost = 1.0f)
        {
            List<string> subterms = GetTerms(query);
            if (subterms.Count > 0)
            {
                if (subterms.Count == 1)
                {
                    luceneQuery.AppendFormat("{0}:{1}", field, subterms[0]);
                }
                else
                {
                    luceneQuery.AppendFormat("({0}:{1}", field, subterms[0]);
                    for (int i = 1; i < subterms.Count; i += 1)
                    {
                        luceneQuery.AppendFormat(" OR {0}:{1}", field, subterms[i]);
                    }
                    luceneQuery.Append(")");
                }

                if (boost != 1.0f)
                {
                    luceneQuery.AppendFormat("^{0} ", boost);
                }
                else
                {
                    luceneQuery.AppendFormat(" ");
                }
            }
        }

        private static void CreateFieldClauseAND(StringBuilder luceneQuery, string field, string query, float boost)
        {
            List<string> subterms = GetTerms(query);
            if (subterms.Count > 1)
            {
                luceneQuery.AppendFormat("({0}:{1}", field, subterms[0]);
                for (int i = 1; i < subterms.Count; i += 1)
                {
                    luceneQuery.AppendFormat(" AND {0}:{1}", field, subterms[i]);
                }
                luceneQuery.Append(')');
                if (boost != 1)
                {
                    luceneQuery.AppendFormat("^{0} ", boost);
                }
            }
        }

        private static List<string> GetTerms(string query)
        {
            List<string> result = new List<string>();

            if (query.StartsWith("\"") && query.EndsWith("\""))
            {
                result.Add(query);
            }
            else
            {
                bool literal = false;
                int start = 0;
                for (int i = 0; i < query.Length; i++)
                {
                    char ch = query[i];
                    if (ch == '"')
                    {
                        literal = !literal;
                    }
                    if (!literal)
                    {
                        if (ch == ' ')
                        {
                            string s = query.Substring(start, i - start);
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                result.Add(s);
                            }
                            start = i + 1;
                        }
                    }
                }

                string t = query.Substring(start, query.Length - start);
                if (!string.IsNullOrWhiteSpace(t))
                {
                    result.Add(t);
                }
            }

            return result;
        }

        private static string Quote(string term)
        {
            if (term.StartsWith("\"") && term.EndsWith("\""))
            {
                return term;
            }
            return string.Format("\"{0}\"", term);
        }
    }
}
