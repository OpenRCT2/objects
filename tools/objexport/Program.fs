// objexport
// Exports objects from RCT2 to OpenRCT2 json files

open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.FSharp.Core
open Newtonsoft.Json
open RCT2ObjectData.DataObjects

type JRide = {
    id: string;
    authors: string list;
    version: string;
    objectType: string;
    images: string list;
    strings: IDictionary<string, IDictionary<string, string>>;
}

let ExportObjects (path, outputPath) =
    printfn "Exporting objects from '%s' to '%s'" path outputPath

    let SerializeToJson(value: 'a) =
        let sb = new StringBuilder(256)
        let sw = new StringWriter(sb)
        let jsonSerializer = JsonSerializer.CreateDefault()
        use jsonWriter = new JsonTextWriter(sw, Indentation = 4,
                                                Formatting = Formatting.Indented)
        jsonSerializer.Serialize(jsonWriter, value, typedefof<'a>)
        sw.ToString()

    let getObjId (obj: ObjectData) =
        let prefix =
            match obj.Source with
            | SourceTypes.RCT2 -> "rct2."
            | SourceTypes.WW -> "rct2.ww."
            | SourceTypes.TT -> "rct2.tt."
            | _ -> "other."
        prefix + obj.ObjectHeader.FileName.ToLower()

    let getSourceDirectoryName source =
        match source with
        | SourceTypes.RCT2 -> "rct2"
        | SourceTypes.WW -> "rct2ww"
        | SourceTypes.TT -> "rct2tt"
        | _ -> "other"

    let getObjTypeDirectoryName objType =
        match objType with
        | ObjectTypes.Attraction -> "rides"
        | _ -> "other"

    let getOutputJsonPath (obj: ObjectData, name) =
        Path.Combine(outputPath,
                     getSourceDirectoryName(obj.Source),
                     getObjTypeDirectoryName(obj.Type),
                     name + ".json")

    let getLanguageName i =
        match i with
        | 0 -> "en-GB"
        | 1 -> "en-US"
        | 2 -> "fr-FR"
        | 3 -> "de-DE"
        | 4 -> "es-ES"
        | 5 -> "it-IT"
        | 6 -> "nl-NL"
        | 7 -> "sv-SE"
        | 9 -> "ko-KR"
        | 10 -> "zh-CN"
        | 11 -> "zh-TW"
        | 13 -> "pt-BR"
        | _ -> i.ToString()

    let ExportObject(obj: ObjectData) =
        if obj.Type = ObjectTypes.Attraction then
            let objName = obj.ObjectHeader.FileName.ToUpper()
            let objId = getObjId(obj)
            let outputJsonPath = getOutputJsonPath(obj, objId)

            printfn "Exporting %s to %s" objName (Path.GetFullPath(outputJsonPath))

            let numImages = obj.ImageDirectory.NumEntries
            let images = [ sprintf "$RCT2:OBJDATA/%s.DAT[%d..%d]" objName 0 numImages ]

            let getStrings(index) =
                let stringEntry = obj.StringTable.Entries.[index]
                let stringSeq = stringEntry.Languages
                let indexSeq = [| 0..(stringSeq.Length - 1) |]
                Seq.map2(fun x y -> (getLanguageName(x), y)) indexSeq stringSeq
                |> Seq.filter(fun (x, y) -> not(String.IsNullOrWhiteSpace(y)))
                |> dict

            let stEntries = obj.StringTable.Entries
            let strings = [| ("name", getStrings(0));
                             ("description", getStrings(1));
                             ("capacity", getStrings(2)) |]
                          |> Array.filter(fun (x, y) -> y.Count > 0)
                          |> dict

            let jobj = { id = objId;
                         authors = ["Chris Saywer"; "Simon Foster"];
                         version = "1.0";
                         objectType = "ride";
                         images = images;
                         strings = strings }

            let json = SerializeToJson(jobj)
            Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)) |> ignore
            File.WriteAllText(outputJsonPath, json)

    Directory.GetFiles(path)
    |> Seq.map(ObjectData.FromFile)
    |> Seq.filter(fun x -> x <> null)
    |> Seq.iter(ExportObject)

[<EntryPoint>]
let main argv =
    printfn "RCT2 object to json exporter"
    if argv.Length < 2 then
        printfn "Usage: objexport <objects path> <output path> [options]"
        1
    else
        let path = argv.[0]
        let outputPath = argv.[1]
        ExportObjects(path, outputPath)
        0
