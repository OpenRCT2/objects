// objexport
// Exports objects from RCT2 to OpenRCT2 json files

open System
open System.IO
open System.Text
open Newtonsoft.Json
open RCT2ObjectData.DataObjects

type JRide = {
    id: string;
    authors: string list;
    version: string;
    objectType: string
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

    let ExportObject(obj: ObjectData) =
        if obj.Source = SourceTypes.RCT2 &&
           obj.Type = ObjectTypes.Attraction then
            let objName = obj.ObjectHeader.FileName
            let objId = "rct2." + objName.ToLower()
            let outputJsonPath = Path.Combine(outputPath, "rides", objId + ".json")
            let outputJsonPathAbsolute = Path.GetFullPath(outputJsonPath)

            printfn "Exporting %s to %s" objName outputJsonPathAbsolute

            let jobj = { id = objId;
                         authors = ["Chris Saywer"; "Simon Foster"];
                         version = "1.0";
                         objectType = "ride" }

            let json = SerializeToJson(jobj)
            Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)) |> ignore
            File.WriteAllText(outputJsonPath, json)

    let files = Directory.GetFiles(path)
    for f in files do
        let dat = ObjectData.FromFile(f)
        if dat <> null then
            ExportObject(dat)

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
