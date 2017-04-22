// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

module ObjectExporter =

    open System
    open System.Collections.Generic
    open System.IO
    open System.Text
    open JsonTypes
    open Microsoft.FSharp.Core
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp
    open PropertyExtractor
    open RCT2ObjectData.DataObjects

    let serializeToJson (value: 'a) =
        let sb = new StringBuilder(capacity = 256)
        let sw = new StringWriter(sb)
        let jsonSerializer = JsonSerializer.CreateDefault()
        use jsonWriter = new JsonTextWriter(sw, Indentation = 4,
                                                Formatting = Formatting.Indented)
        Seq.iter (jsonSerializer.Converters.Add) JsonFsharp.converters
        jsonSerializer.ContractResolver <- new ObjectContractResolver()
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

    let getObjTypeName = function
        | ObjectTypes.Attraction -> "ride"
        | ObjectTypes.SceneryGroup -> "scenery_group"
        | ObjectTypes.SmallScenery -> "scenery_small"
        | ObjectTypes.LargeScenery -> "scenery_large"
        | ObjectTypes.Wall -> "scenery_wall"
        | ObjectTypes.Path -> "footpath"
        | ObjectTypes.PathAddition -> "footpath_item"
        | ObjectTypes.PathBanner -> "footpath_banner"
        | ObjectTypes.ParkEntrance -> "park_entrance"
        | ObjectTypes.Water -> "water"
        | _ -> "other"

    let getSourceDirectoryName = function
        | SourceTypes.RCT2 -> "rct2"
        | SourceTypes.WW -> "rct2ww"
        | SourceTypes.TT -> "rct2tt"
        | _ -> "other"

    let getOutputJsonPath basePath (obj: ObjectData) name =
        Path.Combine(basePath,
                     getSourceDirectoryName obj.Source,
                     getObjTypeName obj.Type,
                     name + ".json")

    let getLanguageName = function
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
        | i -> i.ToString()

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
            |> Seq.filter(fun (_, y) -> not (String.IsNullOrWhiteSpace(y)))
            |> Seq.filter(fun (x, y) -> x = getLanguageName 0 || y <> stringSeq.[0])
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
