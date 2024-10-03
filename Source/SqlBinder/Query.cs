﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SqlBinder.ConditionValues;
using SqlBinder.Parsing;
using SqlBinder.Parsing.Tokens;
using SqlBinder.Properties;

namespace SqlBinder
{
    [Serializable]
    public class ParserException : Exception
    {
        public ParserException(Exception innerException) : base(Exceptions.ParserFailure, innerException)
        {
        }

        public ParserException(string errorMessage) : base(string.Format(Exceptions.ScriptNotValid, errorMessage))
        {
        }
    }

    [Serializable]
    public class UnmatchedConditionsException : Exception
    {
        public UnmatchedConditionsException(string[] conditions) : base(string.Format(Exceptions.NoMatchingParams,
            string.Join(", ", conditions)))
        {
        }
    }

    [Serializable]
    public class InvalidConditionException : Exception
    {
        public InvalidConditionException(ConditionValue value, Operator op, string message)
            : base(string.Format(Exceptions.InvalidCondition, value.GetType().Name, op, message))
        {
        }

        public InvalidConditionException(ConditionValue value, Operator op, Exception innerException)
            : base(string.Format(Exceptions.InvalidCondition, value.GetType().Name, op, innerException.Message),
                innerException)
        {
        }
    }

    public class FormatParameterEventArgs : EventArgs
    {
        /// <summary>
        /// Gets parameter name as it appears in the SqlBinder script.
        /// </summary>
        public string ParameterName { get; internal set; }

        /// <summary>
        /// Gets or sets parameter name formatted for passing to the <see cref="Query.SqlParameters"/> collection.
        /// </summary>
        public string FormattedName { get; set; }

        /// <summary>
        /// Gets or sets parameter name formatted for inserting into the output SQL string.
        /// </summary>
        public string FormattedForSqlPlaceholder { get; set; }
    }

    public delegate void FormatParameterEventHandler(object sender, FormatParameterEventArgs e);

    /// <summary>
    /// Provides capability to parse and execute an SqlBinder scripts.
    /// </summary>
    public class Query
    {
        private RootToken _parserResult;
        private ParserHints _parserHints;
        private string _sqlBinderScript;
        private static readonly ConcurrentDictionary<string, RootToken> ParserCache = new();

        public Query()
        {
        }

        public Query(string script)
        {
            SqlBinderScript = script;
        }

        /// <summary>
        /// Occurs when parameter is to be formatted for the SQL output. You can use this to specify custom parameter tags.
        /// </summary>
        public event FormatParameterEventHandler FormatParameterName;

        /// <summary>
        /// Gets or sets default parameter format string for SQL placeholders, i.e. '@{0}'. See <see cref="FormatParameterName"/> event. The
        /// {0} placeholder will be name already formatted with <see cref="DefaultParameterFormat"/>.
        /// </summary>
        protected virtual string DefaultParameterSqlPlaceholderFormat { get; } = "{0}";

        /// <summary>
        /// Gets or sets the default parameter format string e.g. 'param_{0}_{1}'. The {0} placeholder is the parameter name from the 
        /// SqlBinder script whereas {1} is parameter ordinal.
        /// </summary>
        protected virtual string DefaultParameterFormat { get; } = "p{0}_{1}";

        private FormatParameterEventArgs FormatParameterNameInternal(Parameter parameter, int ordinal)
        {
            var paramName = string.Format(DefaultParameterFormat, parameter.Name, ordinal);
            var sqlParamName = string.Format(DefaultParameterSqlPlaceholderFormat,
                ((parameter as BindVariableParameter)?.OpeningTag ?? "") + paramName);

            var e = new FormatParameterEventArgs
            {
                ParameterName = parameter.Name,
                FormattedName = paramName,
                FormattedForSqlPlaceholder = sqlParamName,
            };

            OnFormatParameterName(this, e);

            return e;
        }

        /// <summary>
        /// Fires an event which can be used to format parameter names.
        /// </summary>
        protected virtual void OnFormatParameterName(object sender, FormatParameterEventArgs e) =>
            FormatParameterName?.Invoke(sender, e);

        /// <summary>
        /// Gets or sets an SqlBinder script that was passed to this query.
        /// </summary>
        public string SqlBinderScript
        {
            get => _sqlBinderScript;
            set
            {
                _sqlBinderScript = value;
                _parserResult = null;
            }
        }

        /// <summary>
        /// Various options that can be used to customize or optimize the parser for your specific DBMS flavor. For example, it is redundant
        /// to scan for Oracle flavors if you're not using this specific syntax.
        /// </summary>
        public ParserHints ParserHints
        {
            get => _parserHints;
            set
            {
                _parserHints = value;
                _parserResult = null;
            }
        }

        /// <summary>
        /// Disables static caching of parser output which is indexed by the SqlBinder template query script. Static caching is redundant if you are
        /// caching queries by yourself, i.e. re-using existing, previously created instances of the <see cref="Query"/>. The last used SqlBinder template
        /// script is always cached by default so any subsequent calls to <see cref="GetSql"/> regardless of changes in conditions will not invoke the
        /// parser engine.
        /// </summary>
        public bool DisableParserCache { get; set; }

        /// <summary>
        /// Gets or sets the capacity of the static parser cache, if enabled. Once the number of cached template scripts goes beyond this capacity
        /// the entire static cache will be purged and the process starts from the beginning. Default value is 256. Changing this value will cause
        /// any existing cache to be purged.
        /// </summary>
        public static int ParserCacheCapacity { get; set; } = 256;

        /// <summary>
        /// Gets or sets a value which which determines a minimum total number of characters within a SqlBinder template script required for the script's
        /// parser output to be statically cached. It is zero by default meaning that all scripts will be cached.
        /// </summary>
        public static int ParserCacheLengthThreshold { get; set; }

        /// <summary>
        /// Gets the conditions which are required in order to build a valid query. There must be a parameter placeholder in your script for each condition.
        /// </summary>
        public List<Condition> Conditions { get; internal set; } = [];

        /// <summary>
        /// Gets or sets a collection of variables that will be passed onto the parser engine.
        /// </summary>
        public Dictionary<string, object> Variables { get; set; } = new();

        /// <summary>
        /// Creates a condition for the query.
        /// </summary>
        /// <param name="parameterName">Name of the parameter for which this condition applies. It must match a placeholder in the script (i.e. '[parameterName]').</param>
        /// <param name="op">Condition operator.</param>
        /// <param name="value">Value of the condition. You can use your own or already predefined classes 
        /// such as <see cref="DateValue"/>, <see cref="NumberValue"/>, <see cref="StringValue"/> or <see cref="BoolValue"/>.</param>
        public virtual void SetCondition(string parameterName, Operator op, ConditionValue value)
        {
            if (parameterName == null)
                throw new ArgumentException(nameof(parameterName));

            RemoveCondition(parameterName);
            Conditions.Add(new Condition(parameterName, op, value));
        }

        /// <summary>
        /// Creates a <see cref="Condition"/> for the query.
        /// </summary>
        /// <param name="parameterName">Name of the parameter for which this condition applies. It must match a placeholder in the script (i.e. '[parameterName]').</param>
        /// <param name="value">Value of the condition. You can use your own or already predefined classes
        /// such as <see cref="DateValue"/>, <see cref="NumberValue"/>, <see cref="StringValue"/> or <see cref="BoolValue"/>.</param>
        public virtual void SetCondition(string parameterName, ConditionValue value)
        {
            SetCondition(parameterName, Operator.Is, value);
        }

        /// <summary>
        /// Translates a number-specific NumericOperator enum into a general purpose Operator enum.
        /// </summary>
        protected static Operator TranslateOperator(NumericOperator conditionOperator) =>
            conditionOperator switch
            {
                NumericOperator.IsNot => Operator.IsNot,
                NumericOperator.IsGreaterThan => Operator.IsGreaterThan,
                NumericOperator.IsGreaterThanOrEqualTo => Operator.IsGreaterThanOrEqualTo,
                NumericOperator.IsLessThan => Operator.IsLessThan,
                NumericOperator.IsLessThanOrEqualTo => Operator.IsLessThanOrEqualTo,
                _ => Operator.Is
            };

        protected bool MaterializeValues<T>(IEnumerable<T> values, out T[] array)
        {
            array = null;
            if (values == null)
                return false;
            array = values.ToArray();
            return array.Any();
        }

        protected void TranslateGreaterLessThanOperators(out Operator lessThan, out Operator greaterThan,
            bool inclusive, bool isNot)
        {
            greaterThan = inclusive
                ? isNot ? Operator.IsLessThan : Operator.IsGreaterThanOrEqualTo
                : isNot
                    ? Operator.IsLessThanOrEqualTo
                    : Operator.IsGreaterThan;
            lessThan = inclusive
                ? isNot ? Operator.IsGreaterThan : Operator.IsLessThanOrEqualTo
                : isNot
                    ? Operator.IsGreaterThanOrEqualTo
                    : Operator.IsLessThan;
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue"/> for the query.
        /// </summary>
        public virtual void SetConditionRange(string parameterName, decimal? from = null, decimal? to = null,
            bool inclusive = true, bool isNot = false)
        {
            TranslateGreaterLessThanOperators(out var lessthan, out var grthan, inclusive, isNot);

            if (from.HasValue && to.HasValue)
            {
                if (!inclusive)
                    throw new ArgumentException(Exceptions.SqlBetweenCanOnlyBeInclusive, nameof(inclusive));
                SetCondition(parameterName, isNot ? Operator.IsNotBetween : Operator.IsBetween,
                    new NumberValue(from.Value, to.Value));
            }
            else if (from.HasValue)
                SetCondition(parameterName, grthan, new NumberValue(from.Value));
            else if (to.HasValue)
                SetCondition(parameterName, lessthan, new NumberValue(to.Value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, decimal? value,
            NumericOperator conditionOperator = NumericOperator.Is, bool ignoreIfNull = false)
        {
            if (ignoreIfNull && !value.HasValue)
                return;
            SetCondition(parameterName, TranslateOperator(conditionOperator), new NumberValue(value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, IEnumerable<decimal> values, bool isNot = false)
        {
            if (!MaterializeValues(values, out var valuesMaterialized))
                return;
            SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf,
                new NumberValue(valuesMaterialized));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue"/> for the query.
        /// </summary>
        public virtual void SetConditionRange(string parameterName, int? from = null, int? to = null,
            bool inclusive = true, bool isNot = false)
        {
            TranslateGreaterLessThanOperators(out var lessthan, out var grthan, inclusive, isNot);

            if (from.HasValue && to.HasValue)
            {
                if (!inclusive)
                    throw new ArgumentException(Exceptions.SqlBetweenCanOnlyBeInclusive, nameof(inclusive));
                SetCondition(parameterName, isNot ? Operator.IsNotBetween : Operator.IsBetween,
                    new NumberValue(from.Value, to.Value));
            }
            else if (from.HasValue)
                SetCondition(parameterName, grthan, new NumberValue(from.Value));
            else if (to.HasValue)
                SetCondition(parameterName, lessthan, new NumberValue(to.Value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, int? value,
            NumericOperator conditionOperator = NumericOperator.Is, bool ignoreIfNull = false)
        {
            if (ignoreIfNull && !value.HasValue)
                return;
            SetCondition(parameterName, TranslateOperator(conditionOperator), new NumberValue(value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, IEnumerable<int> values, bool isNot = false)
        {
            if (!MaterializeValues(values, out var valuesMaterialized))
                return;
            SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf,
                new NumberValue(valuesMaterialized));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, long? value,
            NumericOperator conditionOperator = NumericOperator.Is, bool ignoreIfNull = false)
        {
            if (ignoreIfNull && !value.HasValue)
                return;
            SetCondition(parameterName, TranslateOperator(conditionOperator), new NumberValue(value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="NumberValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, IEnumerable<long> values, bool isNot = false)
        {
            if (!MaterializeValues(values, out var valuesMaterialized))
                return;
            SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf,
                new NumberValue(valuesMaterialized));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="DateValue"/> for the query.
        /// </summary>
        public virtual void SetConditionRange(string parameterName, DateTime? from = null, DateTime? to = null,
            bool inclusive = true, bool isNot = false)
        {
            TranslateGreaterLessThanOperators(out var lessthan, out var grthan, inclusive, isNot);

            if (from.HasValue && to.HasValue)
            {
                if (!inclusive)
                    throw new ArgumentException(Exceptions.SqlBetweenCanOnlyBeInclusive, nameof(inclusive));
                SetCondition(parameterName, isNot ? Operator.IsNotBetween : Operator.IsBetween,
                    new DateValue(from.Value, to.Value));
            }
            else if (from.HasValue)
                SetCondition(parameterName, grthan, new DateValue(from.Value));
            else if (to.HasValue)
                SetCondition(parameterName, lessthan, new DateValue(to.Value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="DateValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, DateTime? value,
            NumericOperator conditionOperator = NumericOperator.Is, bool ignoreIfNull = false)
        {
            if (ignoreIfNull && !value.HasValue)
                return;
            SetCondition(parameterName, TranslateOperator(conditionOperator), new DateValue(value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="DateValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, IEnumerable<DateTime> values, bool isNot = false)
        {
            if (!MaterializeValues(values, out var valuesMaterialized))
                return;
            SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf,
                new DateValue(valuesMaterialized));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="DateValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, bool? value, bool ignoreIfNull = false)
        {
            if (ignoreIfNull && !value.HasValue)
                return;
            SetCondition(parameterName, Operator.Is, new BoolValue(value));
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="StringValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, string value,
            StringOperator conditionOperator = StringOperator.Is, bool isNot = false, bool ignoreIfNull = false)
        {
            if ((conditionOperator != StringOperator.Is || ignoreIfNull) && string.IsNullOrEmpty(value))
                return;
            var op = TranslateStringOperator(conditionOperator, ref value, isNot);
            SetCondition(parameterName, op, new StringValue(value));
        }

        /// <summary>
        /// Translates the string-specific StringOperator enum into a general purpose Operator enum.
        /// </summary>
        protected static Operator TranslateStringOperator(StringOperator conditionOperator, ref string value,
            bool isNot)
        {
            switch (conditionOperator)
            {
                case StringOperator.IsLike: return isNot ? Operator.DoesNotContain : Operator.Contains;
                case StringOperator.Is: return isNot ? Operator.IsNot : Operator.Is;
                case StringOperator.Contains:
                    value = $"%{value}%";
                    return isNot ? Operator.DoesNotContain : Operator.Contains;
                case StringOperator.BeginsWith:
                    value = $"{value}%";
                    return isNot ? Operator.DoesNotContain : Operator.Contains;
                case StringOperator.EndsWith:
                    value = $"%{value}";
                    return isNot ? Operator.DoesNotContain : Operator.Contains;

                default: return Operator.Is;
            }
        }

        /// <summary>
        /// Creates a <see cref="Condition" /> with <see cref="StringValue" /> for the query.
        /// </summary>
        public virtual void SetCondition(string parameterName, IEnumerable<string> values, bool isNot = false)
        {
            if (!MaterializeValues(values, out var valuesMaterialized))
                return;
            SetCondition(parameterName, isNot ? Operator.IsNotAnyOf : Operator.IsAnyOf,
                new StringValue(valuesMaterialized));
        }

        /// <summary>
        /// Returns a condition by its associated query parameter or null if not found.
        /// </summary>
        public virtual Condition GetCondition(string parameterName) =>
            Conditions.FirstOrDefault(c => c.Parameter == parameterName);

        /// <summary>
        /// Removes a condition (if any) by its associated query parameter.
        /// </summary>
        public virtual void RemoveCondition(string parameterName) => Conditions.Remove(GetCondition(parameterName));

        /// <summary>
        /// Defines a user variable that can be used when this query is executed by the template parser engine.
        /// </summary>
        /// <param name="name">The name of the variable that will be matched in the script as '[variableName]'.</param>
        /// <param name="value">The value.</param>
        public virtual void DefineVariable(string name, object value) => Variables[name] = value;

        private readonly HashSet<string> _processedConditions = [];

        /// <summary>
        /// A collection of SQL parameters that were produced after processing conditions.
        /// </summary>
        public Dictionary<string, object> SqlParameters { get; set; } = new();

        /// <summary>
        /// A resulting SQL produced by the last call to the method <see cref="GetSql"/>.
        /// </summary>
        public string OutputSql { get; set; }

        /// <summary>
        /// Parses the SqlBinder script, processes given conditions and returns the resulting SQL.
        /// </summary>
        /// <exception cref="ParserException">Thrown when the SqlBinder script is not valid. For example, when number of opening and closing []{} braces don't match.</exception>
        /// <exception cref="UnmatchedConditionsException">Thrown when there is a condition which wasn't found in the script. Mostly causes by mis-typed parameter 
        /// placeholders or condition names as they must be matched.</exception>
        /// <exception cref="InvalidConditionException">Thrown when some <see cref="ConditionValue"/> instance fails to generate the SQL.</exception>
        public string GetSql()
        {
            var processor = new SqlBinderProcessor();

            processor.RequestParameterValue += Parser_RequestParameterValue;

            OutputSql = processor.ProcessTemplate(GetRecycledParseResults());

            var unprocessedConditions = Conditions.Select(c => c.Parameter).Except(_processedConditions).ToArray();
            if (unprocessedConditions.Any())
                throw new UnmatchedConditionsException(unprocessedConditions);

            return OutputSql;
        }

        private RootToken GetRecycledParseResults()
        {
            if (DisableParserCache || SqlBinderScript.Length < ParserCacheLengthThreshold)
            {
                if (_parserResult != null)
                    return _parserResult;
                return _parserResult ??= new SqlBinderParser(ParserHints).Parse(SqlBinderScript);
            }

            if (ParserCache.TryGetValue(SqlBinderScript, out var cachedResult))
                return cachedResult;

            if (ParserCache.Count == ParserCacheCapacity)
                ParserCache.Clear();

            return ParserCache[SqlBinderScript] = new SqlBinderParser(ParserHints).Parse(SqlBinderScript);
        }

        /// <summary>
        /// Adds an SQL parameter to the collection. Use <see cref="Conditions"/> and <see cref="SetCondition(string, Operator, ConditionValue)"/> to set conditions which will
        /// add parameters automatically.
        /// </summary>
        public virtual void AddSqlParameter(string paramName, object paramValue)
        {
            SqlParameters[paramName] = paramValue;
        }

        private void Parser_RequestParameterValue(object sender, RequestParameterValueArgs e)
        {
            var condition = Conditions.FirstOrDefault(c => string.CompareOrdinal(c.Parameter, e.Parameter.Name) == 0);

            if (condition != null)
            {
                _processedConditions.Add(condition.Parameter);
                e.Value = ConstructParameterSql(condition, e.Parameter);
            }
            else
            {
                var variableName = e.Parameter.Name;
                if (Variables.TryGetValue(variableName, out var v))
                    e.Value = v.ToString();
            }
        }

        /// <summary>
        /// Compiles a parameter sql based on query parameter, operator and value.
        /// </summary>
        protected virtual string ConstructParameterSql(Condition condition, Parameter parameter)
        {
            var sqlOperator = condition.Operator;
            var conditionValue = condition.Value;

            try
            {
                var sql = conditionValue.GetSql(sqlOperator);

                if (string.IsNullOrEmpty(sql))
                    throw new InvalidOperationException(Exceptions.EmptySqlReturned);

                var values = conditionValue.GetValues();

                if (values is not { Length: > 0 })
                    return sql;

                var paramsSql = new object[values.Length];
                var paramCnt = 1;

                // Create parameter(s) for each value
                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i];

                    if (value is not string && value is IEnumerable valueEnumerable)
                    {
                        if (conditionValue.UseBindVariables)
                        {
                            // This value is enumerable (e.g. IN, NOT IN)
                            var sqlParamNames = new List<string>();
                            foreach (var subValue in valueEnumerable)
                            {
                                var formatResults = FormatParameterNameInternal(parameter, paramCnt);
                                AddSqlParameter(formatResults.FormattedName, subValue);
                                conditionValue.ProcessParameter(parameter.Name, subValue);
                                sqlParamNames.Add(formatResults.FormattedForSqlPlaceholder);
                                paramCnt++;
                            }

                            paramsSql[i] = string.Join(", ", sqlParamNames.ToArray());
                        }
                        else
                            paramsSql[i] = string.Join(", ", valueEnumerable);
                    }
                    else
                    {
                        if (conditionValue.UseBindVariables)
                        {
                            var formatResults = FormatParameterNameInternal(parameter, paramCnt);
                            AddSqlParameter(formatResults.FormattedName, value);
                            conditionValue.ProcessParameter(parameter.Name, value);
                            paramsSql[i] = formatResults.FormattedForSqlPlaceholder;
                        }
                        else
                            paramsSql[i] = value;
                    }

                    paramCnt++;
                }

                sql = string.Format(sql, paramsSql);

                return sql;
            }
            catch (Exception ex)
            {
                throw new InvalidConditionException(conditionValue, sqlOperator, ex);
            }
        }
    }
}