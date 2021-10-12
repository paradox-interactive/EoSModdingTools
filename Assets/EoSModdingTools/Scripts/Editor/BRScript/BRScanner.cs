using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RomeroGames
{
    public class BRScanner
    {
        public enum TokenType
        {
            // Single-char tokens
            LEFT_PAREN,     // (
            RIGHT_PAREN,    // )
            COMMA,          // ,
            DOT,            // .
            PLUS,           // +
            MINUS,          // -
            SLASH,          // /
            STAR,           // *
            LT,             // <
            GT,             // >

            // Keywords and operators
            EQ,             // ==
            LE,             // <=
            GE,             // >=
            NE,             // !=
            AND,
            OR,
            NOT,
            IF,
            ELSEIF,
            ELSE,
            END,
            GOTO,
            CALL,
            CALL_LUA,
            FALSE,
            TRUE,
            TITLE,
            QUOTE,
            FUNCTION,
            TIMEOUT,
            SAY,
            SAY_EMOTE,
            SET,
            NIL,
            DISABLE_OPTION,
            HOVER_OPTION,
            HIDE_OPTION,
            TONE_OPTION,
            SUCCESS_CHANCE_OPTION,
            TOOLTIP_OPTION,
            REWARD_OPTION,
            START_QUOTE,
            LOCAL,
            OPTION,
            STRING,
            NUMBER,
            IDENTIFIER,
            SCRIPT,
            IN,
            TRIGGER,
            TICKS,
            DAYS,
            MONTHS,
            LUA_SECTION,
            VAR,
            RULE,
            MAKE_STRING,
            STRING_COMMAND,
            REM,

            ENTRY,

            // Terminators
            EOL,
            EOF
        }

        public struct TokenLocation
        {
            public int line;
            public int offset; // The number of characters from the start

            public TokenLocation(int line, int offset)
            {
                this.line = line;
                this.offset = offset;
            }
        }

        public struct Token
        {
            public TokenType type;
            public TokenLocation location;
            public string lexeme;
            public object literal;

            public Token(TokenType type, string lexeme, object literal, TokenLocation location)
            {
                this.type = type;
                this.location = location;
                this.lexeme = lexeme;
                this.literal = literal;
            }

            public override string ToString()
            {
                // Todo
                return null;
            }
        }

        public struct ScannerError
        {
            public String message;
            public TokenLocation location;
        }


        private string _source;
        private int _sourceLen;
        private int _start = 0;
        private int _current = 0;
        private int _line = 1;
        private int _lineStart = 0;
        private int _prevLineStart = 0;
        private System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder();
        private System.Text.StringBuilder _subBuilder = new System.Text.StringBuilder();
        private TokenType _lastTokenType = TokenType.EOF;
        private List<Token> _tokens = new List<Token>();
        private List<ScannerError> _errors = new List<ScannerError>();
        private readonly Dictionary<string, TokenType> _keywords = new Dictionary<string, TokenType>()
        {
            {"AND", TokenType.AND},
            {"CALL", TokenType.CALL},
            {"CALL_LUA", TokenType.CALL_LUA},
            {"DAYS", TokenType.DAYS},
            {"ELSE", TokenType.ELSE},
            {"ELSEIF", TokenType.ELSEIF},
            {"END", TokenType.END},
            {"FALSE", TokenType.FALSE},
            {"GOTO", TokenType.GOTO},
            {"IF", TokenType.IF},
            {"IN", TokenType.IN},
            {"LOCAL", TokenType.LOCAL},
            {"MAKE_STRING", TokenType.MAKE_STRING},
            {"MONTHS", TokenType.MONTHS},
            {"NIL", TokenType.NIL},
            {"NOT", TokenType.NOT},
            {"OR", TokenType.OR},
            {"TITLE", TokenType.TITLE},
            {"FUNCTION", TokenType.FUNCTION},
            {"QUOTE", TokenType.QUOTE},
            {"SAY", TokenType.SAY},
            {"SAY_EMOTE", TokenType.SAY_EMOTE},
            {"SCRIPT", TokenType.SCRIPT},
            {"SET", TokenType.SET},
            {"START_QUOTE", TokenType.START_QUOTE},
            {"STRING", TokenType.STRING_COMMAND},
            {"TICKS", TokenType.TICKS},
            {"TIMEOUT", TokenType.TIMEOUT},
            {"TRIGGER", TokenType.TRIGGER},
            {"TRUE", TokenType.TRUE},
            {"VAR", TokenType.VAR},
            {"RULE", TokenType.RULE},
            {"ENTRY", TokenType.ENTRY}
        };

        private TokenLocation CurLocation()
        {
            return new TokenLocation(_line, (_start + 1) - _lineStart);
        }

        private bool IsAtEnd()
        {
            return _current >= _sourceLen;
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return _source[_current];
        }

        private char PeekNext()
        {
            if (_current + 1 >= _sourceLen) return '\0';
            return _source[_current + 1];
        }

        private char Advance()
        {
            char c = _source[_current++];
            if (c == '\n')
            {
                _line++;
                _prevLineStart = _lineStart;
                _lineStart = _current;
            }
            return c;
        }

        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c == '_') || (c == '.');
        }

        private bool IsAlphaNumeric(char c)
        {
            return IsDigit(c) || IsAlpha(c);
        }

        private bool Match(char expected)
        {
            if (IsAtEnd()) return false;
            if (_source[_current] != expected) return false;
            _current++;
            return true;
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, new TokenLocation(_line, (_start + 1) - _lineStart));
        }

        private void AddToken(TokenType type, TokenLocation location, object literal = null) {
            String text = _source.Substring(_start, _current - _start);
            // Debug.Log("Adding token: " + type.ToString() + ": " + text);
            _tokens.Add(new Token(type, text, literal, location));
            _lastTokenType = type;
        }

        private void AddError(string message)
        {
            TokenLocation location = CurLocation();
            Debug.LogError("BR scanner error: " + message + " at (" + location.line.ToString() + "," + location.offset.ToString() + ")");
            _errors.Add(new ScannerError{message = message, location = location});
        }

        private void AddString()
        {
            int lineBegin = _line;
            int lineStartBegin = _lineStart;
            char c = Peek();
            _stringBuilder.Length = 0;
            while (c != '"' && c != '”' && c != '“' && c != '\0')
            {
                if (c == '\r')
                {
                    Advance();
                    c = Peek();
                    continue;
                }
                else if (c == '\\')
                {
                    Advance();
                    c = Peek();
                    if (c == '\\' || c == '\"')
                    {
                        _stringBuilder.Append(c);
                    }
                    else if (c == 't')
                    {
                        _stringBuilder.Append('\t');
                    }
                    else if (c == 'n')
                    {
                        _stringBuilder.Append('\n');
                    }
                    else
                    {
                        AddError("Error parsing string - invalid character after \\ " + _stringBuilder.ToString());
                    }
                }
                else
                {
                    _stringBuilder.Append(c);
                }
                Advance();
                c = Peek();
            }
            if (c == '\0')
            {
                AddError("Unterminated string (started at (" + lineBegin + "," + (_start + 1 - lineStartBegin) + ").");
                return;
            }
            Advance(); // Skip final '"'
            string value = _stringBuilder.ToString();
            AddToken(TokenType.STRING, new TokenLocation(lineBegin, (_start + 1) - lineStartBegin), value);
        }

        private void AddEndOfLine()
        {
            int lineBegin = _line;
            int prevLineStartBegin = _prevLineStart;
            char c = Peek();
            while (c != '\0' && (c == ' ' || c == '\t' || c == '\r' || c == '\n'))
            {
                Advance();
                c = Peek();
            }
            if (_lastTokenType != TokenType.EOL)
            {
                AddToken(TokenType.EOL, new TokenLocation(lineBegin - 1, _start - prevLineStartBegin));
            }
        }

        private void AddNumberOrIdentifier()
        {
            while(IsDigit(Peek()))
            {
                Advance();
            }

            // Look for optional decimal point
            if (Peek() == '.' && IsDigit(PeekNext()))
            {
                Advance();
                while(IsDigit(Peek()))
                {
                    Advance();
                }
            }
            bool isNumber = true;
            while (IsAlpha(Peek()))
            {
                Advance();
                isNumber = false;
            }

            if (isNumber)
            {
                string numberString = _source.Substring(_start, _current - _start);
                try
                {
                    Double n = Double.Parse(numberString, CultureInfo.InvariantCulture);
                    AddToken(TokenType.NUMBER, new TokenLocation(_line, (_start + 1) - _lineStart), n);
                }
                catch (Exception ex)
                {
                    AddError("Error parsing number " + numberString +" : " + ex.Message);
                }
            }
            else
            {
                string text = _source.Substring(_start, _current - _start);
                AddToken(TokenType.IDENTIFIER, new TokenLocation(_line, (_start + 1) - _lineStart), text);
            }
        }

        private void AddIdentifier()
        {
            int lineBegin = _line;
            int lineStartBegin = _lineStart;
            while (IsAlphaNumeric(Peek()))
            {
                Advance();
            }
            string text = _source.Substring(_start, _current - _start);

            TokenType tokenType;
            object literal = null;
            if (!_keywords.TryGetValue(text, out tokenType))
            {
                literal = text;
                tokenType = TokenType.IDENTIFIER;
                int checkStart = 0;
                TokenType matchType = TokenType.EOF;
                if (text.StartsWith("OPTION"))
                {
                    checkStart = "OPTION".Length;
                    matchType = TokenType.OPTION;
                }
                else if (text.StartsWith("DISABLE_OPTION"))
                {
                    checkStart = "DISABLE_OPTION".Length;
                    matchType = TokenType.DISABLE_OPTION;
                }
                else if (text.StartsWith("HOVER_OPTION"))
                {
                    checkStart = "HOVER_OPTION".Length;
                    matchType = TokenType.HOVER_OPTION;
                }
                else if (text.StartsWith("HIDE_OPTION"))
                {
                    checkStart = "HIDE_OPTION".Length;
                    matchType = TokenType.HIDE_OPTION;
                }
                else if (text.StartsWith("TONE_OPTION"))
                {
                    checkStart = "TONE_OPTION".Length;
                    matchType = TokenType.TONE_OPTION;
                }
                else if (text.StartsWith("SUCCESS_CHANCE_OPTION"))
                {
                    checkStart = "SUCCESS_CHANCE_OPTION".Length;
                    matchType = TokenType.SUCCESS_CHANCE_OPTION;
                }
                else if (text.StartsWith("TOOLTIP_OPTION"))
                {
                    checkStart = "TOOLTIP_OPTION".Length;
                    matchType = TokenType.TOOLTIP_OPTION;
                }
                else if (text.StartsWith("REWARD_OPTION"))
                {
                    checkStart = "REWARD_OPTION".Length;
                    matchType = TokenType.REWARD_OPTION;
                }
                else if (text == "REM")
                {
                    tokenType = TokenType.REM;
                    _subBuilder.Length = 0;
                    char c = Peek();
                    while (!IsAtEnd() && c != '\n')
                    {
                        if (c != '\r')
                        {
                            _subBuilder.Append(c);
                        }
                        Advance();
                        c = Peek();
                    }
                    literal = _subBuilder.ToString();
                }
                else if (text == "LUA_START")
                {
                    tokenType = TokenType.LUA_SECTION;
                    // Keep reading until LUA_END is found
                    string luaEnd = "LUA_END";
                    int maxMatchLen = luaEnd.Length;
                    _subBuilder.Length = 0;
                    int m = 0;
                    while (m < maxMatchLen && !IsAtEnd())
                    {
                        char c = Peek();
                        if (c == luaEnd[m])
                        {
                            m++;
                        }
                        else
                        {
                            _subBuilder.Append(luaEnd.Substring(0, m));
                            m = 0;
                            if (c != '\r')
                            {
                                _subBuilder.Append(c);
                            }
                        }
                        Advance();
                    }
                    if (m < maxMatchLen)
                    {
                         AddError("Expected LUA_END to close LUA_START!");
                         return;
                    }
                    literal = _subBuilder.ToString();
                }
                if (checkStart > 0)
                {
                    string n = text.Substring(checkStart);
                    try
                    {
                        literal = Int32.Parse(n, CultureInfo.InvariantCulture);
                        tokenType = matchType;
                    }
                    catch (Exception)
                    {
                        // Not a number - it's just an identier
                    }
                }
            }
            AddToken(tokenType, new TokenLocation(lineBegin, (_start + 1) - lineStartBegin), literal);
        }

        private void ScanToken()
        {
            _start = _current;
            char c = Advance();

            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    // Ignore whitespace
                    break;
                case '\n' :
                    AddEndOfLine();
                    break;
                case '(': AddToken(TokenType.LEFT_PAREN); break;
                case ')': AddToken(TokenType.RIGHT_PAREN); break;
                case ',': AddToken(TokenType.COMMA); break;
                case '.': AddToken(TokenType.DOT); break;
                case '-':
                    if (Match('-'))
                    {
                        char p = Peek();
                        while (p != '\n' && p != '\0')
                        {
                            Advance();
                            p = Peek();
                        }
                    }
                    else
                    {
                        AddToken(TokenType.MINUS);
                    }
                    break;
                case '+': AddToken(TokenType.PLUS); break;
                case '*': AddToken(TokenType.STAR); break;
                case '/' : AddToken(TokenType.SLASH); break;

                case '"':
                case '”':
                case '“':
                    AddString();
                    break;

                case '<':AddToken(Match('=') ? TokenType.LE : TokenType.LT); break;
                case '>':AddToken(Match('=') ? TokenType.GE : TokenType.GT); break;

                case '!':
                    if (Match('='))
                    {
                        AddToken(TokenType.NE);
                    }
                    else
                    {
                        AddError("Unknown operator: !" + Peek());
                    }
                    break;

                case '=':
                    if (Match('='))
                    {
                        AddToken(TokenType.EQ);
                    }
                    else
                    {
                        AddError("Unknown operator: =" + Peek());
                    }
                    break;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    AddNumberOrIdentifier();
                    break;

                default:
                    if (IsAlpha(c))
                    {
                        AddIdentifier();
                    }
                    else
                    {
                        AddError("Unknown character: " + c.ToString());
                    }
                    break;
            }

        }

        public void Clear()
        {
            _source = null;
            _sourceLen = 0;
            _start = 0;
            _current = 0;
            _line = 1;
            _prevLineStart = 0;
            _tokens.Clear();
            _errors.Clear();
            _lastTokenType = TokenType.EOF;
        }

        /// <exception cref="BRException"></exception>
        public bool ScanTokens(string source)
        {
            try
            {
                Clear();
                _source = source;
                _sourceLen = source.Length;
                while (!IsAtEnd())
                {
                    ScanToken();
                }
                _start = _current;
                AddToken(TokenType.EOF);
                return _errors.Count == 0;
            }
            catch (BRScript.BRException ex)
            {
                Debug.LogWarning("Scanner exception occurred: " + ex.ToString());
                throw ex;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Scanner exception occurred: " + ex.ToString());
                throw new BRScript.BRException("Scanner exception occurred: " + ex.ToString(), ex);
            }
        }

        public List<Token> Tokens { get { return _tokens; }}

        public List<ScannerError> Errors { get { return _errors; }}
    }
}
