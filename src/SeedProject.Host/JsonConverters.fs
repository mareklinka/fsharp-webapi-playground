namespace SeedProject.Host

open System.Text.Json
open System
open System.Text.Json.Serialization

type OptionValueConverter<'T>() =
    inherit JsonConverter<'T option>()

    override __.Read(reader: byref<Utf8JsonReader>, _typ: Type, options: JsonSerializerOptions) =
        match reader.TokenType with
        | JsonTokenType.Null -> None
        | _ ->
            Some
            <| JsonSerializer.Deserialize<'T>(&reader, options)

    override __.Write(writer: Utf8JsonWriter, value: 'T option, options: JsonSerializerOptions) =
        match value with
        | None -> writer.WriteNullValue()
        | Some value -> JsonSerializer.Serialize(writer, value, options)


// Instantiates the correct OptionValueConverter<T>
type OptionConverter() =
    inherit JsonConverterFactory()

    override __.CanConvert(t: Type) : bool =
        t.IsGenericType
        && t.GetGenericTypeDefinition() = typedefof<Option<_>>

    override __.CreateConverter(typeToConvert: Type, _options: JsonSerializerOptions) : JsonConverter =
        let typ =
            typeToConvert.GetGenericArguments() |> Array.head

        let converterType =
            typedefof<OptionValueConverter<_>>.MakeGenericType (typ)

        Activator.CreateInstance(converterType) :?> JsonConverter
