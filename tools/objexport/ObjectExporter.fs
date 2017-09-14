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
        | 9 -> "ko-KR"
        | 10 -> "zh-CN"
        | 11 -> "zh-TW"
        | 13 -> "pt-BR"
        | i -> i.ToString()

    let exportObject outputPath ourStrings (obj: ObjectData) =
        let objName = obj.ObjectHeader.FileName.ToUpper()
        let objId = getObjId obj
        let outputJsonPath = getOutputJsonPath outputPath obj objId

        printfn "Exporting %s to %s" objName (Path.GetFullPath(outputJsonPath))

        let numImages = obj.ImageDirectory.NumEntries
        let images = [ sprintf "$RCT2:OBJDATA/%s.DAT[%d..%d]" objName 0 numImages ]

        let decodeString language s =
            let decodedBytes =
                let result = new ResizeArray<byte>()
                let sr = new StringReader(s)
                while sr.Peek() <> -1 do
                    match sr.Read() with
                    | 255 ->
                        let a = sr.Read()
                        let b = sr.Read()
                        result.Add (byte a)
                        result.Add (byte b)
                    | c ->
                        result.Add (byte c)
                result.ToArray()

            let codepage =
                match language with
                | "ko-KR" -> 949
                | "zh-CN" -> 936
                | "zh-TW" -> 950
                | _ -> 1252

            Encoding.GetEncoding(codepage)
                    .GetString(decodedBytes)

        let getStrings index =
            let stringEntries = obj.StringTable.Entries
            match ResizeArray.tryItem index stringEntries with
            | None -> dict []
            | Some stringEntry ->
                let strings =
                    stringEntry.Languages
                    |> Seq.mapi(fun i str ->
                        let lang = getLanguageName i
                        (lang, decodeString lang str))
                    |> Seq.filter(fun (_, y) -> not (String.IsNullOrWhiteSpace(y)))
                    |> Seq.toArray

                strings
                |> Seq.filter(fun (x, y) -> x = getLanguageName 0 || y <> snd strings.[0])
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

            // TODO overlay ourStrings on top of entries

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
        File.WriteAllText(outputJsonPath, json, Encoding.UTF8)

    let getObjectStringsFromLanguageFile path =
        let (|Object|_|) (s : string) =
            let st = s.Trim()
            if (st.StartsWith("[") && st.EndsWith("]")) then
                Some (st.Substring(1, st.Length - 2))
            else
                None

        let (|Property|_|) (name: string) (s : string) =
            let st = s.Trim()
            match st.IndexOf(':') with
            | -1 -> None
            | i ->
                let left = st.Remove(i).Trim()
                let right = st.Substring(i + 1)
                if left = name then Some right
                else None

        let mutable curObject = None
        let mutable curName = None
        let mutable curDesc = None
        let mutable curCpty = None
        let items = new ResizeArray<string * (string option * string option * string option)>()
        let lines = File.ReadAllLines path
        for line in lines do
            match line with
            | Object s ->
                match curObject with
                | None -> ()
                | Some obj ->
                    items.Add (obj, (curName, curDesc, curCpty))
                curObject <- Some s
                curName <- None
                curDesc <- None
                curCpty <- None
            | Property "STR_NAME" s -> curName <- Some s
            | Property "STR_DESC" s -> curDesc <- Some s
            | Property "STR_CPTY" s -> curCpty <- Some s
            | _ -> ()
        items

    let exportObjects path outputPath options =
        let objectStrings =
            match options.languageDirectory with
            | None -> dict []
            | Some dir ->
                dir
                |> Directory.GetFiles
                |> Seq.map (fun f ->
                    let lang = Path.GetFileNameWithoutExtension f
                    getObjectStringsFromLanguageFile f
                    |> Seq.map (fun (objName, strings) -> (objName, lang, strings))
                    |> Seq.toList)
                |> Seq.collect id
                |> Seq.groupBy (fun (objName, _, _) -> objName)
                |> Seq.map (fun (k, v) -> (k, v |> Seq.map (fun (_, b, c) -> (b, c)) |> Seq.toArray))
                |> dict

        printfn "Exporting objects from '%s' to '%s'" path outputPath
        if not (Directory.Exists(path)) then
            printfn "'%s' does not exist" path
            1
        elif not (Directory.Exists(outputPath)) then
            printfn "'%s' does not exist" outputPath
            1
        else
            Directory.GetFiles(path)
            |> Seq.map ObjectData.FromFile
            |> Seq.filter (fun x -> x <> null)
            |> Seq.iter (fun x ->
                let strings =
                    match objectStrings.TryGetValue x.ObjectHeader.FileName with
                    | true, value -> Some value
                    | _ -> None
                exportObject outputPath strings x)
            0
