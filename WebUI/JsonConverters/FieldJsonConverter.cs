using System.Text.Json;
using System.Text.Json.Serialization;
using WebUI.Endpoints.Resources;

namespace WebUI.JsonConverters
{
    public class FieldJsonConverter<T> : JsonConverter<Field<T>>
    {
        public override Field<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                var value = (T?)JsonSerializer.Deserialize(ref reader, typeof(T), options);
                return new(value, null);
            }
            catch (JsonException ex)
            {
                ex.Data.Add("ActualValue", new string(ex.Message.SkipWhile(c => c != '(').TakeWhile(c => c != ')').ToArray())[1..^1]);
                return new(default, ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ex.Data.Add("ActualValue", ex.ActualValue);
                return new(default, ex);
            }
            catch (Exception ex)
            {
                return new(default, ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, Field<T> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class InputJsonConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Field<>);
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var actualValueType = typeToConvert.GetGenericArguments()[0];
            return (JsonConverter?)Activator.CreateInstance(typeof(FieldJsonConverter<>).MakeGenericType(actualValueType));
        }
    }
}
