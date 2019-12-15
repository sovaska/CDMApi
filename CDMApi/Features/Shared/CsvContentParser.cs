using System;
using System.Collections.Generic;
using System.Text;

namespace CDMApi.Features.Shared
{
    public class CsvContentParser
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private bool _escaping;
        private bool _cr;
        private bool _isFirstChar;
        private char _prevChar;
        private bool _isDoubleQuote;
        private int _isObject;

        public List<List<string>> SplitContentToLines(string content, int paramCountPerLine)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Initialize();

            var parameters = new List<string>();
            var lines = new List<List<string>>();

            for (var i = 0; i < content.Length; i++)
            {
                if (ProcessChar(content[i], parameters))
                {
                    if (parameters.Count != paramCountPerLine)
                    {
                        throw new Exception("Parser bug found");
                    }
                    lines.Add(new List<string>(parameters));
                    parameters.Clear();
                }
            }

            return lines;
        }

        private bool ProcessChar(char current, List<string> parameters)
        {
            if (!_escaping)
            {
                if (current == '\"' && _prevChar == '\"')
                {
                    if (_isDoubleQuote)
                    {
                        _isDoubleQuote = false;
                        _prevChar = current;
                        _escaping = false;
                        _sb.Append(current);
                        return false;
                    }
                    else
                    {
                        _isDoubleQuote = true;
                        _prevChar = current;
                        _escaping = true;
                        _sb.Append(current);
                        return false;
                    }
                }
                if (current == '\t' || current == ',')
                {
                    parameters.Add(_sb.ToString());
                    _sb.Clear();
                    _isFirstChar = true;
                    _prevChar = current;
                    return false;
                }
                if (current == '\"' && _isFirstChar)
                {
                    _escaping = true;
                    _prevChar = current;
                    return false;
                }
                if (current == '\r')
                {
                    _cr = true;
                    _prevChar = current;
                    return false;
                }
                if (current == '\n' && _cr)
                {
                    _cr = false;
                    parameters.Add(_sb.ToString());
                    _sb.Clear();
                    _prevChar = current;
                    return true;
                }
            }
            else
            {
                if (current == '{')
                {
                    _isObject++;
                }
                if (current == '}')
                {
                    _isObject--;
                }
                if (current == '\"' && _isObject == 0)
                {
                    _escaping = false;
                    _prevChar = current;
                    return false;
                }
                if (current == '\"' && _isObject > 0 && _prevChar == '\"')
                {
                    _prevChar = current;
                    return false;
                }
            }
            ProcessFirstChar();
            _sb.Append(current);
            _prevChar = current;
            return false;
        }

        private void ProcessFirstChar()
        {
            if (_isFirstChar)
            {
                _isFirstChar = false;
            }
        }

        private void Initialize()
        {
            _sb.Clear();
            _escaping = false;
            _cr = false;
            _isFirstChar = true;
        }
    }
}