// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

module internal Program =

    open ObjectExporter

    let rec hasFlag name argv =
        match argv with
        | head :: tail ->
            if head = name then
                true
            else
                hasFlag name tail
        | [] -> false

    let getOption name =
        let rec findOption argv =
            match argv with
            | head :: tail ->
                if head = name then
                    List.takeWhile (fun (x: string) -> not (x.StartsWith("-"))) tail
                else
                    findOption tail
            | [] -> []
        findOption

    let getOptionSingle name argv =
        match getOption name argv with
        | [] -> None
        | head :: _ -> Some head

    let parseOptions argv =
        { languageDirectory = getOptionSingle "--language" argv
          objectType = getOptionSingle "--type" argv
          multithreaded = hasFlag "-j" argv }

    [<EntryPoint>]
    let main argv =
        printfn "RCT2 object to json exporter"
        match Array.toList argv with
        | path :: outputPath :: options ->
            exportObjects path outputPath (parseOptions options)
        | _ ->
            printfn "Usage: objexport <objects path> <output path> [options]"
            printfn "Options:"
            printfn "  --language <dir>        Specify directory for language files"
            printfn "  --type <type>           Specify type of object to export"
            printfn "  -j                      Multithreaded"
            1
