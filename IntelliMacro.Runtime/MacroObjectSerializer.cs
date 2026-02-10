using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// A modern, secure replacement for BinaryFormatter for serializing MacroObjects and wrapped objects.
    /// </summary>
    public static class MacroObjectSerializer
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IncludeFields = true,
            Converters = { new MacroObjectJsonConverter() }
        };

        /// <summary>
        /// Serialize a MacroObject to a stream.
        /// </summary>
        public static void Serialize(Stream stream, object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var wrapper = new SerializationWrapper
            {
                TypeName = obj.GetType().AssemblyQualifiedName,
                Data = obj
            };

            JsonSerializer.Serialize(stream, wrapper, _options);
        }

        /// <summary>
        /// Deserialize a MacroObject from a stream.
        /// </summary>
        public static object Deserialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var wrapper = JsonSerializer.Deserialize<SerializationWrapper>(stream, _options);
            
            if (wrapper == null)
                throw new InvalidOperationException("Failed to deserialize object");

            return wrapper.Data;
        }

        /// <summary>
        /// Serialize a MacroObject to a byte array.
        /// </summary>
        public static byte[] SerializeToBytes(object obj)
        {
            using (var ms = new MemoryStream())
            {
                Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserialize a MacroObject from a byte array.
        /// </summary>
        public static object DeserializeFromBytes(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                return Deserialize(ms);
            }
        }

        private class SerializationWrapper
        {
            public string TypeName { get; set; }
            public object Data { get; set; }
        }

        private class MacroObjectJsonConverter : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;

                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException("Expected start of object");

                string typeName = null;
                JsonElement dataElement = default;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString();
                        reader.Read();

                        if (propertyName == "TypeName")
                        {
                            typeName = reader.GetString();
                        }
                        else if (propertyName == "Data")
                        {
                            dataElement = JsonDocument.ParseValue(ref reader).RootElement;
                        }
                    }
                }

                if (string.IsNullOrEmpty(typeName))
                    throw new JsonException("TypeName not found");

                Type type = Type.GetType(typeName);
                if (type == null)
                    throw new JsonException($"Type not found: {typeName}");

                return JsonSerializer.Deserialize(dataElement.GetRawText(), type, options);
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                // This is handled by the SerializationWrapper, so this method won't be called
                throw new NotImplementedException();
            }
        }
    }
}
