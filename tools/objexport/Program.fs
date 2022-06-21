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

    let getOption name argv =
        let getOptionValues =
            let rec getOptionValues acc (argv: string list) =
                match argv with
                | head :: tail ->
                    if head.StartsWith("-") then
                        (acc |> List.rev, argv)
                    else
                        getOptionValues (head :: acc) tail
                | [] ->
                    (acc |> List.rev, [])
            getOptionValues []

        let rec findOption acc argv =
            match argv with
            | head :: tail ->
                if head = name then
                    let (values, tail) = getOptionValues tail
                    findOption (values :: acc) tail
                else
                    findOption (acc |> List.rev) tail
            | [] -> (acc |> List.rev)
        findOption [] argv

    let getOptionSingle name argv =
        match getOption name argv with
        | head :: _ ->
            match head with
            | head :: _ -> Some head
            | [] -> None
        | [] -> None

    let getOptionMany name argv =
        getOption name argv
        |> List.map List.tryHead
        |> List.choose id

    let parseOptions argv =
        { id = argv |> getOptionSingle "--id"
          authors = argv |> getOptionMany "--author"
          languageDirectory = argv |> getOptionSingle "--language"
          objectType = argv |> getOptionSingle "--type"
          multithreaded = argv |> hasFlag "-j"
          splitFootpaths = argv |> hasFlag "--split"
          storePng = argv |> hasFlag "--png"
          outputParkobj = argv |> hasFlag "-z" }

    let printHelp () =
        printfn "Usage: objexport <objects path> <output path> [options]"
        printfn "                 <object path> <output path> [options]"
        printfn "Options:"
        printfn "  --author <author>       Specify an author (multiple use)"
        printfn "  --id                    Specify the id of the target object"
        printfn "  --language <dir>        Specify directory for language files"
        printfn "  --type <type>           Specify type of object to export"
        printfn "  --split                 Split footpath into surface and railing objects"
        printfn "  --png                   Store images as a .png instead of gx file"
        printfn "  -j                      Multithreaded"
        printfn "  -z                      Create .parkobj files"
        1

    [<EntryPoint>]
    let main argv =
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)
        printfn "RCT2 object to .json / .parkobj exporter"
        match Array.toList argv with
        | path :: outputPath :: options ->
            exportObjects path outputPath (parseOptions options)
        | _ ->
            printHelp ()
