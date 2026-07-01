using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AIWarehouseTwin.Artifact
{
    internal static class ArtifactJson
    {
        public static Dictionary<string, object> ParseObject(string json, string artifactName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException($"{artifactName} JSON cannot be empty.", nameof(json));
            }

            try
            {
                var parser = new Parser(json);
                var value = parser.ParseValue();
                parser.SkipWhitespace();
                if (!parser.IsAtEnd)
                {
                    throw new FormatException("Unexpected trailing characters.");
                }

                if (value is Dictionary<string, object> obj)
                {
                    return obj;
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is IndexOutOfRangeException)
            {
                throw new InvalidOperationException($"{artifactName} JSON structure is invalid: {ex.Message}", ex);
            }

            throw new InvalidOperationException($"{artifactName} JSON root must be an object.");
        }

        public static string GetString(Dictionary<string, object> obj, string name, string defaultValue = "")
        {
            return obj.TryGetValue(name, out var value) && value is string text ? text : defaultValue;
        }

        public static int GetInt(Dictionary<string, object> obj, string name, int defaultValue = 0)
        {
            return (int)GetLong(obj, name, defaultValue);
        }

        public static long GetLong(Dictionary<string, object> obj, string name, long defaultValue = 0)
        {
            return obj.TryGetValue(name, out var value) && TryToDouble(value, out var number)
                ? Convert.ToInt64(number)
                : defaultValue;
        }

        public static double GetDouble(Dictionary<string, object> obj, string name, double defaultValue = 0)
        {
            return obj.TryGetValue(name, out var value) && TryToDouble(value, out var number)
                ? number
                : defaultValue;
        }

        public static double? GetNullableDouble(Dictionary<string, object> obj, string name)
        {
            return obj.TryGetValue(name, out var value) && TryToDouble(value, out var number) ? number : null;
        }

        public static bool GetBool(Dictionary<string, object> obj, string name, bool defaultValue = false)
        {
            return obj.TryGetValue(name, out var value) && value is bool flag ? flag : defaultValue;
        }

        public static Dictionary<string, object> GetObject(Dictionary<string, object> obj, string name)
        {
            return obj.TryGetValue(name, out var value) && value is Dictionary<string, object> child
                ? child
                : null;
        }

        public static List<object> GetArray(Dictionary<string, object> obj, string name)
        {
            return obj.TryGetValue(name, out var value) && value is List<object> array ? array : null;
        }

        public static string[] ToStringArray(List<object> array)
        {
            if (array == null)
            {
                return Array.Empty<string>();
            }

            var result = new string[array.Count];
            for (var i = 0; i < array.Count; i++)
            {
                result[i] = array[i] as string ?? string.Empty;
            }

            return result;
        }

        public static Dictionary<string, double> ToDoubleMap(Dictionary<string, object> obj)
        {
            var result = new Dictionary<string, double>();
            if (obj == null)
            {
                return result;
            }

            foreach (var entry in obj)
            {
                if (TryToDouble(entry.Value, out var number))
                {
                    result[entry.Key] = number;
                }
            }

            return result;
        }

        public static T[] MapArray<T>(List<object> array, Func<Dictionary<string, object>, T> map)
        {
            if (array == null)
            {
                return Array.Empty<T>();
            }

            var items = new List<T>(array.Count);
            foreach (var value in array)
            {
                if (value is Dictionary<string, object> obj)
                {
                    items.Add(map(obj));
                }
            }

            return items.ToArray();
        }

        public static WarehouseGraphDto MapWarehouseGraph(Dictionary<string, object> obj)
        {
            var graph = new WarehouseGraphDto();
            if (obj == null)
            {
                return graph;
            }

            graph.nodes = MapArray(GetArray(obj, "nodes"), node => new WarehouseGraphNodeDto
            {
                node_id = GetString(node, "node_id"),
                node_type = GetString(node, "node_type"),
                x = GetDouble(node, "x"),
                y = GetDouble(node, "y")
            });

            graph.edges = MapArray(GetArray(obj, "edges"), edge => new WarehouseGraphEdgeDto
            {
                edge_id = GetString(edge, "edge_id"),
                from_node_id = GetString(edge, "from_node_id"),
                to_node_id = GetString(edge, "to_node_id"),
                distance_m = GetDouble(edge, "distance_m"),
                travel_time_ms = GetLong(edge, "travel_time_ms"),
                bidirectional = GetBool(edge, "bidirectional")
            });

            return graph;
        }

        private static bool TryToDouble(object value, out double number)
        {
            switch (value)
            {
                case double doubleValue:
                    number = doubleValue;
                    return true;
                case long longValue:
                    number = longValue;
                    return true;
                case int intValue:
                    number = intValue;
                    return true;
                default:
                    number = 0;
                    return false;
            }
        }

        private sealed class Parser
        {
            private readonly string json;
            private int index;

            public Parser(string json)
            {
                this.json = json;
            }

            public bool IsAtEnd => index >= json.Length;

            public void SkipWhitespace()
            {
                while (!IsAtEnd && char.IsWhiteSpace(json[index]))
                {
                    index++;
                }
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (IsAtEnd)
                {
                    throw new FormatException("Unexpected end of JSON.");
                }

                return json[index] switch
                {
                    '{' => ParseObjectValue(),
                    '[' => ParseArrayValue(),
                    '"' => ParseStringValue(),
                    't' => ParseLiteral("true", true),
                    'f' => ParseLiteral("false", false),
                    'n' => ParseLiteral("null", null),
                    '-' => ParseNumberValue(),
                    _ when char.IsDigit(json[index]) => ParseNumberValue(),
                    _ => throw new FormatException($"Unexpected token '{json[index]}'.")
                };
            }

            private Dictionary<string, object> ParseObjectValue()
            {
                Expect('{');
                var obj = new Dictionary<string, object>();
                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return obj;
                }

                while (true)
                {
                    SkipWhitespace();
                    var key = ParseStringValue();
                    SkipWhitespace();
                    Expect(':');
                    obj[key] = ParseValue();
                    SkipWhitespace();
                    if (TryConsume('}'))
                    {
                        return obj;
                    }

                    Expect(',');
                }
            }

            private List<object> ParseArrayValue()
            {
                Expect('[');
                var array = new List<object>();
                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return array;
                }

                while (true)
                {
                    array.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']'))
                    {
                        return array;
                    }

                    Expect(',');
                }
            }

            private string ParseStringValue()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (!IsAtEnd)
                {
                    var c = json[index++];
                    if (c == '"')
                    {
                        return builder.ToString();
                    }

                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (IsAtEnd)
                    {
                        throw new FormatException("Unterminated escape sequence.");
                    }

                    var escaped = json[index++];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            builder.Append(ParseUnicodeEscape());
                            break;
                        default:
                            throw new FormatException($"Unsupported escape sequence '\\{escaped}'.");
                    }
                }

                throw new FormatException("Unterminated string.");
            }

            private char ParseUnicodeEscape()
            {
                if (index + 4 > json.Length)
                {
                    throw new FormatException("Incomplete unicode escape.");
                }

                var hex = json.Substring(index, 4);
                index += 4;
                return (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            private object ParseNumberValue()
            {
                var start = index;
                if (json[index] == '-')
                {
                    index++;
                }

                while (!IsAtEnd && char.IsDigit(json[index]))
                {
                    index++;
                }

                var hasFractionOrExponent = false;
                if (!IsAtEnd && json[index] == '.')
                {
                    hasFractionOrExponent = true;
                    index++;
                    while (!IsAtEnd && char.IsDigit(json[index]))
                    {
                        index++;
                    }
                }

                if (!IsAtEnd && (json[index] == 'e' || json[index] == 'E'))
                {
                    hasFractionOrExponent = true;
                    index++;
                    if (!IsAtEnd && (json[index] == '+' || json[index] == '-'))
                    {
                        index++;
                    }

                    while (!IsAtEnd && char.IsDigit(json[index]))
                    {
                        index++;
                    }
                }

                var text = json.Substring(start, index - start);
                return hasFractionOrExponent
                    ? double.Parse(text, CultureInfo.InvariantCulture)
                    : long.Parse(text, CultureInfo.InvariantCulture);
            }

            private object ParseLiteral(string literal, object value)
            {
                if (index + literal.Length > json.Length ||
                    string.CompareOrdinal(json, index, literal, 0, literal.Length) != 0)
                {
                    throw new FormatException($"Expected literal '{literal}'.");
                }

                index += literal.Length;
                return value;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (IsAtEnd || json[index] != expected)
                {
                    throw new FormatException($"Expected '{expected}'.");
                }

                index++;
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (!IsAtEnd && json[index] == expected)
                {
                    index++;
                    return true;
                }

                return false;
            }
        }
    }
}
