using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoApp.API.DTOs;

/// <summary>
/// Factory that produces a <see cref="JsonConverter"/> for every closed
/// <c>Optional&lt;T&gt;</c> type encountered during serialisation/deserialisation.
///
/// Behaviour:
///   - Field absent from JSON  → <c>Optional&lt;T&gt;.IsPresent == false</c>  (struct default)
///   - Field present as null   → <c>Optional&lt;T&gt;.IsPresent == true</c>, Value == null
///   - Field present with value→ <c>Optional&lt;T&gt;.IsPresent == true</c>, Value == &lt;value&gt;
///
/// Registration: call <c>options.Converters.Add(new OptionalJsonConverterFactory())</c>
/// inside <c>AddControllers(o =&gt; ...)</c> or <c>AddJsonOptions</c>.
/// </summary>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // The factory is only called when the token is actually present in the JSON,
        // so we know the field was included — IsPresent should be true.
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return new Optional<T>(value!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Optional<T> value,
        JsonSerializerOptions options)
    {
        // When serialising responses we write the inner value (or null).
        if (value.IsPresent)
            JsonSerializer.Serialize(writer, value.Value, options);
        else
            writer.WriteNullValue();
    }
}
