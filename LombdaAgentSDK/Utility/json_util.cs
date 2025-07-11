﻿using LombdaAgentSDK.Agents;
using LombdaAgentSDK.Agents.DataClasses;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LombdaAgentSDK
{
    public static class json_util
    {
        public static ModelOutputFormat CreateJsonSchemaFormatFromType(this Type type, bool jsonSchemaIsStrict = true, string formatDescription = "")
        {
            return new ModelOutputFormat(
                type.Name,
                BinaryData.FromBytes(Encoding.UTF8.GetBytes(JsonSchemaGenerator.GenerateSchema(type))),
                jsonSchemaIsStrict,
                formatDescription
                );
        }

        public static T ParseJson<T>(this RunResult Result)
        {
            string json = Result.Text;

            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("RunResult Text is null or empty");

            return JsonSerializer.Deserialize<T>(json)!;
        }

        public static T ParseJson<T>(this string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON is null or empty");

            return JsonSerializer.Deserialize<T>(json)!;
        }

        public static string MapClrTypeToJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "boolean";
            if (type.IsPrimitive || type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return "number";
            if (type.IsArray) return "array";
            return "object"; // fallback for complex types
        }
    }

    //For both Functions and Structured output
    public class ParameterSchema
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("enum")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Enum { get; set; }
    }

    public static class JsonSchemaGenerator
    {
        internal static string BuildFunctionSchema(Dictionary<string, ParameterSchema> properties, List<string> required)
        {
            var schema = new
            {
                type = "object",
                properties = properties.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object)kvp.Value
                ),
                required = required,
                additionalProperties = false
            };

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
        }

        public static string GenerateSchema(Type type)
        {
            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = GetPropertiesSchema(type),
                ["required"] = GetRequiredProperties(type),
                ["additionalProperties"] = false
            };

            return JsonSerializer.Serialize(schema, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private static Dictionary<string, object> GetPropertiesSchema(Type type)
        {
            var properties = new Dictionary<string, object>();
            foreach (var prop in type.GetProperties())
            {
                properties[prop.Name] = GetPropertySchema(prop.PropertyType);
            }
            return properties;
        }

        private static object GetPropertySchema(Type type)
        {
            if (type == typeof(string)) return new { type = "string" };
            if (type == typeof(bool)) return new { type = "boolean" };
            if (type.IsNumeric()) return new { type = "number" };
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                return new
                {
                    type = "array",
                    items = GetPropertySchema(itemType)
                };
            }

            // Fallback for nested objects
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = GetPropertiesSchema(type),
                ["required"] = GetRequiredProperties(type),
                ["additionalProperties"] = false
            };
        }

        private static List<string> GetRequiredProperties(Type type)
        {
            return type.GetProperties().Select(p => p.Name).ToList();
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float), typeof(UInt16), typeof(UInt32),
            typeof(UInt64), typeof(Single)
        };

        public static bool IsNumeric(this Type myType)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }
    }
}
