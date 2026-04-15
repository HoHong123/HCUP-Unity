#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HInspector.Editor {
    internal static class HInspectorExpressionUtility {
        sealed class IdentifierLiteral {
            public string Text { get; }

            public IdentifierLiteral(string text) {
                Text = text;
            }

            public override string ToString() => Text;
        }

        enum TokenType {
            Identifier,
            Number,
            String,
            Boolean,
            Null,
            Not,
            And,
            Or,
            Equal,
            NotEqual,
            Greater,
            Less,
            GreaterOrEqual,
            LessOrEqual,
            LeftParen,
            RightParen,
            End
        }

        struct Token {
            public TokenType Type;
            public string Text;

            public Token(TokenType type, string text) {
                Type = type;
                Text = text;
            }
        }

        sealed class Parser {
            readonly object targetObject;
            readonly List<Token> tokens;
            int index;

            public Parser(object targetObject, List<Token> tokens) {
                this.targetObject = targetObject;
                this.tokens = tokens;
            }

            public bool Parse() {
                object value = _ParseOr();
                _Expect(TokenType.End);
                return _ToBool(value);
            }

            object _ParseOr() {
                object left = _ParseAnd();

                while (_Match(TokenType.Or)) {
                    object right = _ParseAnd();
                    left = _ToBool(left) || _ToBool(right);
                }

                return left;
            }

            object _ParseAnd() {
                object left = _ParseEquality();

                while (_Match(TokenType.And)) {
                    object right = _ParseEquality();
                    left = _ToBool(left) && _ToBool(right);
                }

                return left;
            }

            object _ParseEquality() {
                object left = _ParseRelational();

                while (true) {
                    if (_Match(TokenType.Equal)) {
                        object right = _ParseRelational();
                        left = _Compare(left, right, HCompareType.Equals);
                        continue;
                    }

                    if (_Match(TokenType.NotEqual)) {
                        object right = _ParseRelational();
                        left = _Compare(left, right, HCompareType.NotEquals);
                        continue;
                    }

                    return left;
                }
            }

            object _ParseRelational() {
                object left = _ParseUnary();

                while (true) {
                    if (_Match(TokenType.Greater)) {
                        object right = _ParseUnary();
                        left = _Compare(left, right, HCompareType.Greater);
                        continue;
                    }

                    if (_Match(TokenType.Less)) {
                        object right = _ParseUnary();
                        left = _Compare(left, right, HCompareType.Less);
                        continue;
                    }

                    if (_Match(TokenType.GreaterOrEqual)) {
                        object right = _ParseUnary();
                        left = _Compare(left, right, HCompareType.GreaterOrEqual);
                        continue;
                    }

                    if (_Match(TokenType.LessOrEqual)) {
                        object right = _ParseUnary();
                        left = _Compare(left, right, HCompareType.LessOrEqual);
                        continue;
                    }

                    return left;
                }
            }

            object _ParseUnary() {
                if (_Match(TokenType.Not))
                    return !_ToBool(_ParseUnary());

                return _ParsePrimary();
            }

            object _ParsePrimary() {
                Token token = _Peek();

                if (_Match(TokenType.LeftParen)) {
                    object value = _ParseOr();
                    _Expect(TokenType.RightParen);
                    return value;
                }

                if (_Match(TokenType.Boolean))
                    return string.Equals(token.Text, "true", StringComparison.OrdinalIgnoreCase);

                if (_Match(TokenType.Null))
                    return null;

                if (_Match(TokenType.Number)) {
                    if (token.Text.IndexOf('.') >= 0) {
                        if (double.TryParse(token.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
                            return doubleValue;
                    }
                    else {
                        if (long.TryParse(token.Text, out long longValue))
                            return longValue;
                    }

                    throw new Exception($"Invalid number: {token.Text}");
                }

                if (_Match(TokenType.String))
                    return token.Text;

                if (_Match(TokenType.Identifier))
                    return _ResolveIdentifier(token.Text);

                throw new Exception($"Unexpected token: {token.Type} ({token.Text})");
            }

            object _ResolveIdentifier(string identifier) {
                string normalized = identifier;

                if (normalized.StartsWith("this.", StringComparison.Ordinal))
                    normalized = normalized.Substring(5);

                if (HInspectorPropertyUtility.TryGetMemberValue(targetObject, normalized, out object memberValue))
                    return memberValue;

                return new IdentifierLiteral(identifier);
            }

            bool _Compare(object left, object right, HCompareType compareType) {
                object resolvedLeft = _ResolveEnumLiteralIfNeeded(left, right);
                object resolvedRight = _ResolveEnumLiteralIfNeeded(right, left);

                if (resolvedLeft == null || resolvedRight == null) {
                    switch (compareType) {
                    case HCompareType.Equals:
                        return Equals(resolvedLeft, resolvedRight);
                    case HCompareType.NotEquals:
                        return !Equals(resolvedLeft, resolvedRight);
                    default:
                        return false;
                    }
                }

                if (!HInspectorPropertyUtility.TryCompare(resolvedLeft, resolvedRight, out int compareResult))
                    return false;

                switch (compareType) {
                case HCompareType.Equals:
                    return compareResult == 0;
                case HCompareType.NotEquals:
                    return compareResult != 0;
                case HCompareType.Greater:
                    return compareResult > 0;
                case HCompareType.Less:
                    return compareResult < 0;
                case HCompareType.GreaterOrEqual:
                    return compareResult >= 0;
                case HCompareType.LessOrEqual:
                    return compareResult <= 0;
                default:
                    return false;
                }
            }

            object _ResolveEnumLiteralIfNeeded(object candidate, object counterpart) {
                if (!(candidate is IdentifierLiteral literal))
                    return candidate;

                if (counterpart == null)
                    return candidate;

                Type counterpartType = counterpart.GetType();
                if (!counterpartType.IsEnum)
                    return candidate;

                string text = literal.Text;

                if (text.StartsWith("this.", StringComparison.Ordinal))
                    text = text.Substring(5);

                int lastDotIndex = text.LastIndexOf('.');
                if (lastDotIndex >= 0)
                    text = text.Substring(lastDotIndex + 1);

                if (!Enum.IsDefined(counterpartType, text))
                    return candidate;

                return Enum.Parse(counterpartType, text);
            }

            bool _ToBool(object value) {
                if (value == null)
                    return false;

                if (value is bool boolValue)
                    return boolValue;

                if (value is IdentifierLiteral)
                    return false;

                if (value is string stringValue)
                    return !string.IsNullOrEmpty(stringValue);

                if (value is Enum)
                    return Convert.ToInt32(value) != 0;

                if (value is sbyte || value is byte ||
                    value is short || value is ushort ||
                    value is int || value is uint ||
                    value is long || value is ulong ||
                    value is float || value is double ||
                    value is decimal)
                    return Convert.ToDouble(value) != 0d;

                return true;
            }

            bool _Match(TokenType tokenType) {
                if (_Peek().Type != tokenType)
                    return false;

                index++;
                return true;
            }

            void _Expect(TokenType tokenType) {
                Token token = _Peek();
                if (token.Type != tokenType)
                    throw new Exception($"Expected {tokenType}, got {token.Type} ({token.Text})");

                index++;
            }

            Token _Peek() {
                if (index < 0 || index >= tokens.Count)
                    return new Token(TokenType.End, string.Empty);

                return tokens[index];
            }
        }

        #region Public Functions
        public static bool TryEvaluate(object targetObject, string expression, out bool result) {
            result = false;

            if (targetObject == null || string.IsNullOrEmpty(expression))
                return false;

            try {
                string body = expression[0] == '@' ? expression.Substring(1) : expression;
                List<Token> tokens = _Tokenize(body);
                Parser parser = new Parser(targetObject, tokens);
                result = parser.Parse();
                return true;
            }
            catch {
                result = false;
                return false;
            }
        }
        #endregion

        #region Private Functions
        static List<Token> _Tokenize(string expression) {
            List<Token> tokens = new List<Token>();
            int index = 0;

            while (index < expression.Length) {
                char current = expression[index];

                if (char.IsWhiteSpace(current)) {
                    index++;
                    continue;
                }

                if (current == '(') {
                    tokens.Add(new Token(TokenType.LeftParen, "("));
                    index++;
                    continue;
                }

                if (current == ')') {
                    tokens.Add(new Token(TokenType.RightParen, ")"));
                    index++;
                    continue;
                }

                if (current == '!' && _Peek(expression, index + 1) == '=') {
                    tokens.Add(new Token(TokenType.NotEqual, "!="));
                    index += 2;
                    continue;
                }

                if (current == '=' && _Peek(expression, index + 1) == '=') {
                    tokens.Add(new Token(TokenType.Equal, "=="));
                    index += 2;
                    continue;
                }

                if (current == '>' && _Peek(expression, index + 1) == '=') {
                    tokens.Add(new Token(TokenType.GreaterOrEqual, ">="));
                    index += 2;
                    continue;
                }

                if (current == '<' && _Peek(expression, index + 1) == '=') {
                    tokens.Add(new Token(TokenType.LessOrEqual, "<="));
                    index += 2;
                    continue;
                }

                if (current == '&' && _Peek(expression, index + 1) == '&') {
                    tokens.Add(new Token(TokenType.And, "&&"));
                    index += 2;
                    continue;
                }

                if (current == '|' && _Peek(expression, index + 1) == '|') {
                    tokens.Add(new Token(TokenType.Or, "||"));
                    index += 2;
                    continue;
                }

                if (current == '!') {
                    tokens.Add(new Token(TokenType.Not, "!"));
                    index++;
                    continue;
                }

                if (current == '>') {
                    tokens.Add(new Token(TokenType.Greater, ">"));
                    index++;
                    continue;
                }

                if (current == '<') {
                    tokens.Add(new Token(TokenType.Less, "<"));
                    index++;
                    continue;
                }

                if (current == '"' || current == '\'') {
                    char quote = current;
                    index++;

                    int start = index;
                    while (index < expression.Length && expression[index] != quote)
                        index++;

                    string value = expression.Substring(start, index - start);
                    tokens.Add(new Token(TokenType.String, value));

                    if (index < expression.Length)
                        index++;

                    continue;
                }

                if (char.IsDigit(current) || (current == '-' && char.IsDigit(_Peek(expression, index + 1)))) {
                    int start = index;
                    index++;

                    while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
                        index++;

                    tokens.Add(new Token(TokenType.Number, expression.Substring(start, index - start)));
                    continue;
                }

                if (_IsIdentifierStart(current)) {
                    int start = index;
                    index++;

                    while (index < expression.Length && _IsIdentifierPart(expression[index]))
                        index++;

                    string identifier = expression.Substring(start, index - start);

                    if (string.Equals(identifier, "true", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(identifier, "false", StringComparison.OrdinalIgnoreCase)) {
                        tokens.Add(new Token(TokenType.Boolean, identifier));
                        continue;
                    }

                    if (string.Equals(identifier, "null", StringComparison.OrdinalIgnoreCase)) {
                        tokens.Add(new Token(TokenType.Null, identifier));
                        continue;
                    }

                    tokens.Add(new Token(TokenType.Identifier, identifier));
                    continue;
                }

                throw new Exception($"Invalid character in expression: {current}");
            }

            tokens.Add(new Token(TokenType.End, string.Empty));
            return tokens;
        }

        static bool _IsIdentifierStart(char value) {
            return char.IsLetter(value) || value == '_';
        }

        static bool _IsIdentifierPart(char value) {
            return char.IsLetterOrDigit(value) || value == '_' || value == '.';
        }

        static char _Peek(string source, int index) {
            if (index < 0 || index >= source.Length)
                return '\0';

            return source[index];
        }
        #endregion
    }
}
#endif