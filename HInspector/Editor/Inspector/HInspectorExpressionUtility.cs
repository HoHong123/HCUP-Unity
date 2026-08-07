#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;

namespace HInspector.Editor {
    internal static class HInspectorExpressionUtility {
        // 파싱 실패 경고는 표현식당 1회 — OnGUI 에서 프레임당 2회 파싱되므로 스팸 방지가 필수.
        static readonly HashSet<string> warnedExpressions = new HashSet<string>();

        // 토큰화는 표현식 문자열만의 함수다 (대상 오브젝트와 무관). GetPropertyHeight + OnGUI 가
        // 프레임당 각각 도는 경로라, 캐시가 없으면 같은 문자열을 리페인트마다 두 번 재스캔하며
        // List<Token> 을 새로 할당한다. 토큰 리스트는 Parser 가 읽기만 하므로 공유해도 안전하다.
        // 실패한 문자열은 null 을 캐시해 재시도 자체를 막는다 (문법 오류는 대상과 무관하게 항상 실패).
        // 파싱(멤버 해석)은 대상 오브젝트에 따라 결과가 달라지므로 캐시하지 않는다.
        static readonly Dictionary<string, List<Token>> tokenCache = new Dictionary<string, List<Token>>();

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
                List<Token> tokens = _GetTokens(expression);
                if (tokens == null) {
                    // 토큰화 단계에서 이미 실패로 판정된 문자열 — 경고는 최초 1회에 남았다.
                    return false;
                }

                Parser parser = new Parser(targetObject, tokens);
                result = parser.Parse();
                return true;
            }
            catch (Exception e) {
                // 무음 삼킴은 멤버 오타·문법 오류 시 인스펙터 필드가 이유 없이 사라지게 만든다.
                if (warnedExpressions.Add(expression)) {
                    UnityEngine.Debug.LogWarning(
                        $"[HInspector] Expression evaluation failed — field will be hidden. expression='{expression}' :: {e.Message}");
                }
                result = false;
                return false;
            }
        }
        #endregion

        #region Private Functions
        // 성공하면 토큰 리스트, 문법 오류면 null 을 캐시한다 (둘 다 표현식 문자열만의 함수).
        static List<Token> _GetTokens(string expression) {
            if (tokenCache.TryGetValue(expression, out var cached)) return cached;

            List<Token> tokens = null;
            try {
                string body = expression[0] == '@' ? expression.Substring(1) : expression;
                tokens = _Tokenize(body);
            }
            catch (Exception e) {
                if (warnedExpressions.Add(expression)) {
                    UnityEngine.Debug.LogWarning(
                        $"[HInspector] Expression tokenization failed — field will be hidden. expression='{expression}' :: {e.Message}");
                }
            }

            tokenCache[expression] = tokens;
            return tokens;
        }

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