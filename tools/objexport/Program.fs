// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

module internal Program =

    open ObjectExporter

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
