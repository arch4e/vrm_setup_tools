using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace VST
{
    public class YamlParser
    {
        private readonly string[] _lines;
        private int _currentIndex = 0;

        public YamlParser(string yamlContent)
        {
            _lines = yamlContent.Split('\n')
                                .Select(l => l.TrimEnd())
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToArray();

            _currentIndex = 0;
        }

        public Dictionary<string, object> Parse()
        {
            var result = new Dictionary<string, object>();

            while (_currentIndex < _lines.Length)
            {
                var line = _lines[_currentIndex].TrimStart();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    _currentIndex++;
                    continue;
                }

                var (key, value) = ParseLine(line);
                result[key] = value;
                _currentIndex++;
            }

            return result;
        }

        private (string key, object value) ParseLine(string line)
        {
            var parts = line.Split(new[] {':'}, 2);
            // Debug.Log(parts[1]);
            if (parts.Length != 2) throw new FormatException($"Invalid YAML line format: {line}");

            var key      = parts[0].Trim();
            var valueStr = parts[1].Trim();

            // Check if this is the start of a list
            if (string.IsNullOrEmpty(valueStr)) return (key, ParseList());

            // Parse value
            return (key, ParseValue(valueStr));
        }

        private List<object> ParseList()
        {
            var list = new List<object>();
            _currentIndex++;

            while (_currentIndex < _lines.Length)
            {
                var line = _lines[_currentIndex].TrimStart();
                if (!line.StartsWith("-")) break;

                var value = line.Substring(1).Trim();
                list.Add(ParseValue(value));
                _currentIndex++;
            }

            _currentIndex--;
            return list;
        }

        private object ParseValue(string value)
        {
            // Try parse number
            if (int.TryParse(value, out int intResult))
                return intResult;
            if (double.TryParse(value, out double doubleResult))
                return doubleResult;

            // Parse boolean
            if (value.ToLower() == "true")
                return true;
            if (value.ToLower() == "false")
                return false;

            // Parse null
            if (value.ToLower() == "null")
                return null;

            // Remove quotes if present
            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }
    }
}