// JSON converters for F# types
// https://github.com/eulerfx/JsonNet.FSharp

namespace Newtonsoft.Json.FSharp

open System
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Converters

/// Converts F# lists to/from JSON arrays
type ListConverter() =
    inherit JsonConverter()

    override x.CanConvert(t:Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<list<_>>

    override x.WriteJson(writer, value, serializer) =
        let list = value :?> System.Collections.IEnumerable |> Seq.cast
        serializer.Serialize(writer, list)

    override x.ReadJson(reader, t, _, serializer) =
        let itemType = t.GetGenericArguments().[0]
        let collectionType = typedefof<IEnumerable<_>>.MakeGenericType(itemType)
        let collection = serializer.Deserialize(reader, collectionType) :?> System.Collections.IEnumerable |> Seq.cast
        let listType = typedefof<list<_>>.MakeGenericType(itemType)
        let cases = FSharpType.GetUnionCases(listType)
        let rec make = function
            | [] -> FSharpValue.MakeUnion(cases.[0], [||])
            | head::tail -> FSharpValue.MakeUnion(cases.[1], [| head; (make tail); |])
        make (collection |> Seq.toList)

/// Converts F# Option values to JSON
type OptionConverter() =
    inherit JsonConverter()

    override x.CanConvert(t) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

    override x.WriteJson(writer, value, serializer) =
        let value =
            if value = null then null
            else
                let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
                fields.[0]
        serializer.Serialize(writer, value)

    override x.ReadJson(reader, t, existingValue, serializer) =
        let innerType = t.GetGenericArguments().[0]
        let innerType =
            if innerType.IsValueType then typedefof<Nullable<_>>.MakeGenericType([|innerType|])
            else innerType
        let value = serializer.Deserialize(reader, innerType)
        let cases = FSharpType.GetUnionCases(t)
        if value = null then FSharpValue.MakeUnion(cases.[0], [||])
        else FSharpValue.MakeUnion(cases.[1], [|value|])

// Convert single case unions like "type Email = Email of string" to/from json.
type SingleCaseUnionConverter () =
    inherit JsonConverter ()

    override this.CanConvert(t) =
        FSharpType.IsUnion(t) && FSharpType.GetUnionCases(t).Length = 1

    override this.WriteJson(writer, value, serializer) =
        let value =
            if value = null then null
            else
                let _,fields = FSharpValue.GetUnionFields(value, value.GetType())
                fields.[0]
        serializer.Serialize(writer, value)

    override this.ReadJson(reader, t, existingValue, serializer) =
        let value = serializer.Deserialize(reader)
        if value <> null then FSharpValue.MakeUnion(FSharpType.GetUnionCases(t).[0],[|value|]) else null

/// Converts F# tuples to/from JSON arrays. (a,b) = [a,b]
type TupleArrayConverter() =
    inherit JsonConverter()

    override x.CanConvert(t:Type) =
        FSharpType.IsTuple(t)

    override x.WriteJson(writer, value, serializer) =
        let values = FSharpValue.GetTupleFields(value)
        serializer.Serialize(writer, values)

    override x.ReadJson(reader, t, _, serializer) =
        let advance = reader.Read >> ignore
        let deserialize t = serializer.Deserialize(reader, t)
        let itemTypes = FSharpType.GetTupleElements(t)

        let readElements() =
            let rec read index acc =
                match reader.TokenType with
                | JsonToken.EndArray -> acc
                | _ ->
                    let value = deserialize(itemTypes.[index])
                    advance()
                    read (index + 1) (acc @ [value])
            advance()
            read 0 List.empty

        match reader.TokenType with
        | JsonToken.StartArray ->
            let values = readElements()
            FSharpValue.MakeTuple(values |> List.toArray, t)
        | _ -> failwith "invalid token"

// Convert unions of form "type Union = A | B | C" to/from json strings
type UnionEnumConverter () =
    inherit JsonConverter ()

    override this.CanConvert(t) =
        FSharpType.IsUnion(t) &&
        not (FSharpType.GetUnionCases(t) |> Array.exists (fun case -> case.GetFields().Length > 0))

    override this.WriteJson(writer, value, serializer) =
        let name =
            if value = null then null
            else
                match FSharpValue.GetUnionFields(value, value.GetType()) with
                | case, _ -> case.Name
        serializer.Serialize(writer,name)

    override this.ReadJson(reader, t, existingValue, serializer) =
        let value = serializer.Deserialize(reader,typeof<string>) :?> string

        let case = FSharpType.GetUnionCases(t) |> Array.pick (fun case ->
            // Note: Case insensitive match!
            if case.Name.ToUpper() = value.ToUpper() then Some case else None
        )

        FSharpValue.MakeUnion(case,[||])

module JsonFsharp =
    let converters: JsonConverter list =
        [ new ListConverter()
          new OptionConverter()
          new TupleArrayConverter()
          new SingleCaseUnionConverter()
          new UnionEnumConverter() ]
