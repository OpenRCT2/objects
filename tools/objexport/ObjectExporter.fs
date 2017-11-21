// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

module ResizeArray =
    let tryItem index (list: ResizeArray<'T>) =
        if index < 0 || index >= list.Count then None
        else Some list.[index]

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

    type ObjectExporterOptions =
        { languageDirectory: string option }

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
        | 8 -> "ja-JP"
        | 9 -> "ko-KR"
        | 10 -> "zh-CN"
        | 11 -> "zh-TW"
        | 13 -> "pt-BR"
        | i -> i.ToString()

    let exportObject outputPath (ourStrings: IDictionary<string, IDictionary<string, string>>) (obj: ObjectData) =
        let objName = obj.ObjectHeader.FileName.ToUpper()
        let objId = getObjId obj
        let outputJsonPath = getOutputJsonPath outputPath obj objId

        printfn "Exporting %s to %s" objName (Path.GetFullPath(outputJsonPath))

        // Get RCT2 images
        let numImages = obj.ImageDirectory.NumEntries
        let images = [ sprintf "$RCT2:OBJDATA/%s.DAT[%d..%d]" objName 0 numImages ]

        // Get RCT2 strings
        let getStrings index =
            let stringEntries = obj.StringTable.Entries
            match ResizeArray.tryItem index stringEntries with
            | None -> dict []
            | Some stringEntry ->
                let strings =
                    stringEntry.Languages
                    |> Seq.mapi(fun i str ->
                        let lang = getLanguageName i
                        let decoded = Localisation.decodeStringFromRCT2 lang str
                        (lang, decoded.Trim()))
                    |> Seq.filter(fun (_, str) ->
                        // Decide whether the string is useful
                        match str with
                        | "" -> false
                        | s when s.StartsWith("#not translated", StringComparison.OrdinalIgnoreCase) -> false
                        | _ -> true)
                    |> Seq.toArray

                strings
                |> Seq.filter(fun (x, y) -> x = getLanguageName 0 || y <> snd strings.[0])
                |> dict

        let theirStrings =
            let entries =
                if obj.Type = ObjectTypes.Attraction then
                    [| ("name", getStrings 0);
                        ("description", getStrings 1);
                        ("capacity", getStrings 2);
                        ("vehicleName", getStrings 0) |]
                else
                    [| ("name", getStrings 0) |]

            entries
            |> Array.filter(fun (_, y) -> y.Count > 0)
            |> dict

        // Overlay our strings on top of the RCT2 strings
        let strings = Localisation.overlayStrings ourStrings theirStrings

        let authors =
            match obj.Source with
            | SourceTypes.RCT2 ->
                ["Chris Saywer"; "Simon Foster"]
            | SourceTypes.WW
            | SourceTypes.TT ->
                ["Frontier Studios"]
            | _ ->
                []

        let properties = getProperties obj
        let jobj = { id = objId;
                     authors = authors;
                     version = "1.0";
                     objectType = getObjTypeName obj.Type;
                     properties = properties;
                     images = images;
                     strings = strings }

        let json = serializeToJson jobj + Environment.NewLine
        Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)) |> ignore
        File.WriteAllText(outputJsonPath, json, Encoding.UTF8)

    let createDirectory path =
        try
            if not (Directory.Exists path) then
                Directory.CreateDirectory(path) |> ignore
            true
        with
        | ex ->
            printfn "Unable to create '%s': %s" path ex.Message
            false

    let exportObjects path outputPath options =
        if not (Directory.Exists(path)) then
            printfn "'%s' does not exist" path
            1
        elif not (createDirectory outputPath) then
            1
        else
            // Load new object strings from OpenRCT2
            let objectStrings =
                match options.languageDirectory with
                | None -> dict []
                | Some dir ->
                    printfn "Reading object strings from '%s'" dir
                    Localisation.getOpenObjectStrings dir

            // Export all objects found in path
            printfn "Exporting objects from '%s' to '%s'" path outputPath
            Directory.GetFiles(path)
            |> Seq.map ObjectData.FromFile
            |> Seq.filter (fun x -> x <> null)
            |> Seq.iter (fun x ->
                let strings =
                    match objectStrings.TryGetValue x.ObjectHeader.FileName with
                    | true, value -> value
                    | _ -> dict []
                exportObject outputPath strings x)
            0
