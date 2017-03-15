// objexport
// Exports objects from RCT2 to OpenRCT2 json files

open System
open System.Collections.Generic
open System.IO
open System.Text
open Microsoft.FSharp.Core
open Newtonsoft.Json
open RCT2ObjectData.DataObjects
open RCT2ObjectData.DataObjects.Types

type JObject = {
    id: string;
    authors: string list;
    version: string;
    objectType: string;
    properties: obj;
    images: string list;
    strings: IDictionary<string, IDictionary<string, string>>;
}

type SceneryGroupProperties = {
    entries: string list;
    order: int;
    entertainerCostumes: string list;
}

let serializeToJson (value: 'a) =
    let sb = new StringBuilder(capacity = 256)
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

let getObjTypeName objType =
    match objType with
    | ObjectTypes.Attraction -> "ride"
    | ObjectTypes.SceneryGroup -> "scenery_group"
    | ObjectTypes.SmallScenery -> "scenery_small"
    | ObjectTypes.LargeScenery -> "scenery_large"
    | ObjectTypes.Wall -> "scenery_wall"
    | ObjectTypes.Path -> "footpath"
    | ObjectTypes.PathAddition -> "footpath_addition"
    | ObjectTypes.PathBanner -> "footpath_banner"
    | ObjectTypes.ParkEntrance -> "park_entrance"
    | ObjectTypes.Water -> "water"
    | _ -> "other"

let getSourceDirectoryName source =
    match source with
    | SourceTypes.RCT2 -> "rct2"
    | SourceTypes.WW -> "rct2ww"
    | SourceTypes.TT -> "rct2tt"
    | _ -> "other"

let getOutputJsonPath basePath (obj: ObjectData) name =
    Path.Combine(basePath,
                 getSourceDirectoryName obj.Source,
                 getObjTypeName obj.Type,
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

let getBits (x: int) =
    seq { 0..31 }
    |> Seq.filter(fun i -> (x &&& (1 <<< i)) <> 0)

let getEntertainer x =
    match x with
    | 4 -> "panda"
    | 5 -> "tiger"
    | 6 -> "elephant"
    | 7 -> "roman"
    | 8 -> "gorilla"
    | 9 -> "snowman"
    | 10 -> "knight"
    | 11 -> "astronaut"
    | 12 -> "bandit"
    | 13 -> "sheriff"
    | 14 -> "pirate"
    | _ -> "unknown"

let getProperties (obj: ObjectData) =
    match obj.Type with
    | ObjectTypes.SceneryGroup ->
        let scg = obj :?> SceneryGroup
        { entries =
            scg.Items
            |> Seq.map(fun x -> x.FileName)
            |> Seq.toList
          order = int scg.Header.Unknown0x108
          entertainerCostumes =
              getBits (int scg.Header.Unknown0x10A)
              |> Seq.map(getEntertainer)
              |> Seq.toList
          } :> obj
    | _ -> new Object()

let exportObject outputPath (obj: ObjectData) =
    let objName = obj.ObjectHeader.FileName.ToUpper()
    let objId = getObjId obj
    let outputJsonPath = getOutputJsonPath outputPath obj objId

    printfn "Exporting %s to %s" objName (Path.GetFullPath(outputJsonPath))

    let numImages = obj.ImageDirectory.NumEntries
    let images = [ sprintf "$RCT2:OBJDATA/%s.DAT[%d..%d]" objName 0 numImages ]

    let getStrings index =
        let stringEntry = obj.StringTable.Entries.[index]
        let stringSeq = stringEntry.Languages
        let indexSeq = [| 0..(stringSeq.Length - 1) |]
        Seq.map2(fun x y -> (getLanguageName x, y)) indexSeq stringSeq
        |> Seq.filter(fun (x, y) -> not (String.IsNullOrWhiteSpace(y)))
        |> dict

    let stEntries = obj.StringTable.Entries
    let strings =
        let entries =
            if obj.Type = ObjectTypes.Attraction then
                [| ("name", getStrings 0);
                   ("description", getStrings 1);
                   ("capacity", getStrings 2) |]
            else
                [| ("name", getStrings 0) |]

        entries
        |> Array.filter(fun (x, y) -> y.Count > 0)
        |> dict

    let properties = getProperties obj
    let jobj = { id = objId;
                 authors = ["Chris Saywer"; "Simon Foster"];
                 version = "1.0";
                 objectType = getObjTypeName obj.Type;
                 properties = properties;
                 images = images;
                 strings = strings }

    let json = serializeToJson jobj + Environment.NewLine
    Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)) |> ignore
    File.WriteAllText(outputJsonPath, json)

let exportObjects path outputPath =
    printfn "Exporting objects from '%s' to '%s'" path outputPath
    Directory.GetFiles(path)
    |> Seq.map(ObjectData.FromFile)
    |> Seq.filter(fun x -> x <> null)
    |> Seq.iter(exportObject outputPath)

[<EntryPoint>]
let main argv =
    printfn "RCT2 object to json exporter"
    if argv.Length < 2 then
        printfn "Usage: objexport <objects path> <output path> [options]"
        1
    else
        let path = argv.[0]
        let outputPath = argv.[1]
        exportObjects path outputPath
        0
