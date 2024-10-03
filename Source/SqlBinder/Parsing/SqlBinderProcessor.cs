using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqlBinder.Parsing.Tokens;

namespace SqlBinder.Parsing
{
    public class RequestParameterValueArgs : EventArgs
    {
        public Parameter Parameter { get; internal set; }
        public string Value { get; set; }
    }

    public delegate void RequestParameterValueEventHandler(object sender, RequestParameterValueArgs e);

    /// <summary>
    /// Processes an SqlBinder template script or a compiled SqlBinder <see cref="NestedToken"/> into a valid SQL based on the feedback
    /// provided by the <see cref="RequestParameterValue"/> event.
    /// </summary>
    public class SqlBinderProcessor
    {
        public const int BUFFER_CAPACITY = 4096;

        /// <summary>
        /// Event fired when parameter value should be provided. Value returned by handling this event is the crucial factor in how the output
        /// SQL will look like.
        /// </summary>
        public event RequestParameterValueEventHandler RequestParameterValue;

        protected virtual void OnRequestParameterValue(RequestParameterValueArgs e) =>
            RequestParameterValue?.Invoke(this, e);

        /// <summary>
        /// Processes an existing <see cref="NestedToken"/> returning the SQL.
        /// </summary>
        /// <param name="compiledToken">Compiled <see cref="NestedToken"/> as a result of previously parsed SqlBinder script.</param>
        public string ProcessTemplate(NestedToken compiledToken) =>
            ConstructSql(ProcessToken(compiledToken)).ToString().Trim();

        /// <summary>
        /// Parses and then processes an SqlBinder script returning the SQL.
        /// </summary>
        /// <param name="sqlBinderTemplateScript">SqlBinder script to be parsed and then processed. This is much slower than the 
        /// <see cref="ProcessTemplate(NestedToken)"/> method as it performs script parsing every time.</param>
        public string ProcessTemplate(string sqlBinderTemplateScript) =>
            ProcessTemplate(new SqlBinderParser().Parse(sqlBinderTemplateScript));

        private ProcessedToken ProcessToken(Token token, ProcessedToken parentToken = null,
            ProcessedToken previousSibling = null)
        {
            ProcessedToken newToken = null;

            if (token is NestedToken nestedToken)
            {
                ProcessedToken prevTokenBuf = null;

                if (nestedToken is Scope scopeToken)
                {
                    var parsedScope = (ProcessedScope)(newToken = new ProcessedScope(scopeToken));
                    parentToken?.Children.Add(parsedScope);
                    foreach (var childToken in scopeToken.Children)
                        prevTokenBuf = ProcessToken(childToken, parsedScope, prevTokenBuf);
                    // Validate this scope
                    parsedScope.IsValid = parsedScope.Children.OfType<ProcessedScope>().Any(s => s.IsValid) ||
                                          parsedScope.Children.OfType<ProcessedParameter>().Any();
                    // Validate scope separator that came before it
                    if (previousSibling is ProcessedScopeSeparator thisScopeSeparator)
                    {
                        if (!(thisScopeSeparator.IsValid =
                                parsedScope.IsValid && (parentToken?.Children.OfType<ProcessedScope>()
                                    .Any(s => s.IsValid && s != parsedScope) ?? false)))
                            return newToken;

                        if (parentToken is ProcessedScope parentTokenScope)
                        {
                            if (parentTokenScope.ParserToken.Flags.Contains('@'))
                                thisScopeSeparator.Suffix = "OR ";
                        }

                        if (parsedScope.ParserToken.Flags.Contains('+'))
                            thisScopeSeparator.Suffix = ""; // The + sign means NO suffix
                        else if (thisScopeSeparator.Suffix == null)
                            thisScopeSeparator.Suffix = "AND ";
                    }
                }
                else
                {
                    var parsedChild = newToken = new ProcessedToken(nestedToken);
                    parentToken?.Children.Add(parsedChild);
                    foreach (var childToken in nestedToken.Children)
                        prevTokenBuf = ProcessToken(childToken, parsedChild, prevTokenBuf);
                }
            }
            else if (token is ScopeSeparator separatorToken)
                parentToken?.Children.Add(newToken = new ProcessedScopeSeparator(separatorToken));
            else if (token is SqlBinderComment)
                return previousSibling;
            else if (token is Parameter parameterToken)
            {
                var parsedParam = (ProcessedParameter)(newToken = ParseParameter(parameterToken));
                if (!string.IsNullOrEmpty(parsedParam.ConditionSql))
                    parentToken?.Children.Add(parsedParam);
            }
            else if (token is ContentToken contentToken)
            {
                var parsedContent = newToken = new ProcessedToken(contentToken);
                parentToken?.Children.Add(parsedContent);
                ProcessToken(contentToken.Content, parsedContent);
            }
            else
                parentToken?.Children.Add(newToken = new ProcessedToken(token));

            return newToken;
        }

        private ProcessedParameter ParseParameter(Parameter parameter)
        {
            var args = new RequestParameterValueArgs { Parameter = parameter };
            OnRequestParameterValue(args);
            return new ProcessedParameter(parameter, args.Value);
        }

        private static StringBuilder ConstructSql(ProcessedToken token, StringBuilder buffer = null)
        {
            buffer ??= new StringBuilder(BUFFER_CAPACITY);

            if (!token.IsValid)
                return buffer;

            switch (token)
            {
                case ProcessedParameter parsedParameter:
                    buffer.Append(parsedParameter.ConditionSql);
                    break;
                case ProcessedScopeSeparator scopeSeparator:
                    buffer.Append(scopeSeparator.ParserToken.Text);
                    buffer.Append(scopeSeparator.Suffix);
                    break;
                default:
                    switch (token.ParserToken)
                    {
                        case Sql sql:
                            buffer.Append(sql.Text);
                            break;
                        case ContentToken contentToken:
                            buffer.Append(contentToken.OpeningTag);
                            if (contentToken.Content != null)
                                ConstructSql(token.Children.First(), buffer);
                            buffer.Append(contentToken.ClosingTag);
                            break;
                        case TextToken textToken:
                            buffer.Append(textToken.Text);
                            break;
                        case NestedToken:
                            foreach (var childToken in token.Children)
                                ConstructSql(childToken, buffer);
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    break;
            }

            return buffer;
        }
    }

    internal class ProcessedToken
    {
        public List<ProcessedToken> Children { get; } = [];
        public Token ParserToken { get; }
        internal ProcessedToken(Token token) => ParserToken = token;
        public bool IsValid { get; internal set; } = true;
    }

    internal class ProcessedScope : ProcessedToken
    {
        internal ProcessedScope(Scope scope) : base(scope)
        {
            ParserToken = scope;
        }

        public new Scope ParserToken { get; }
    }

    internal class ProcessedScopeSeparator : ProcessedToken
    {
        internal ProcessedScopeSeparator(ScopeSeparator scopeSeparator) : base(scopeSeparator)
        {
            ParserToken = scopeSeparator;
        }

        public new ScopeSeparator ParserToken { get; }
        public string Suffix { get; internal set; }
    }

    internal class ProcessedParameter : ProcessedToken
    {
        public string ConditionSql { get; }

        internal ProcessedParameter(Parameter parameter, string sql) : base(parameter) => ConditionSql = sql;
    }
}