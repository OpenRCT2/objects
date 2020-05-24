namespace OpenRCT2.Legacy.ObjectExporter

open System
open System.Collections.Generic
open System.IO
open System.Text

module Localisation =
    type ObjectStrings = IDictionary<string, IDictionary<string, string>>

    let private toMutableDictionary (d: IDictionary<'TKey, 'TValue>) =
        new Dictionary<'TKey, 'TValue>(d)

    // seq (a, b, c) -> seq { (a, seq { (b, c) }) }
    let private groupByT1of3 (items: ('a * 'b * 'c) seq) =
        items
        |> Seq.groupBy (fun (key, _, _) -> key)
        |> Seq.map (fun (key, value) ->
            let newValue =
                value
                |> Seq.map (fun (_, b, c) -> (b, c))
            (key, newValue))

    let private getObjectStringsFromLanguageFile (path: string) =
        printfn "Reading object strings from %s" (Path.GetFileName path)

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
                if left = name then Some (right.Trim())
                else None

        let mutable curObject = None
        let mutable curStrings = []
        let items = new ResizeArray<string * (string * string) list>()
        let lines = File.ReadAllLines path
        for line in lines do
            let addString key value =
                curStrings <- (key, value) :: curStrings
            match line with
            | Object s ->
                match curObject with
                | None -> ()
                | Some obj ->
                    // Add the strings for the object
                    items.Add (obj, curStrings)
                curObject <- Some s
                curStrings <- []
            | Property "STR_NAME" s -> addString "name" s
            | Property "STR_DESC" s -> addString "description" s
            | Property "STR_CPTY" s -> addString "capacity" s
            | _ -> ()
        items

    let getOpenObjectStrings languageDirectory : IDictionary<string, ObjectStrings> =
        languageDirectory
        |> Directory.GetFiles
        |> Seq.map (fun f ->
            let lang = Path.GetFileNameWithoutExtension f
            getObjectStringsFromLanguageFile f
            |> Seq.map (fun (objName, strings) -> (objName, lang, strings))
            |> Seq.toList)
        |> Seq.collect id
        |> groupByT1of3
        |> Seq.map (fun (objName, entries) ->
            let objectToStrings =
                entries
                |> Seq.map (fun (lang, strings) ->
                    strings
                    |> Seq.map (fun (key, str) -> (key, lang, str)))
                |> Seq.collect id
                |> groupByT1of3
                |> Seq.map (fun (key, value) ->
                    let langToStrings =
                        value
                        |> Seq.map (fun (lang, str) -> (lang, str))
                        |> dict
                    (key, langToStrings))
                |> dict
            (objName, objectToStrings))
        |> dict

    let overlayStrings (overlayStrings: ObjectStrings) (strings: ObjectStrings) : ObjectStrings =
        let newStrings = toMutableDictionary strings
        for kvp in Seq.toArray newStrings do
            newStrings.[kvp.Key] <- toMutableDictionary kvp.Value
        for kvp in overlayStrings do
            // Get language to string dictionary
            let sKey =
                match newStrings.TryGetValue kvp.Key with
                | true, l2s ->
                    let l2s = toMutableDictionary l2s
                    newStrings.[kvp.Key] <- l2s
                    l2s
                | _ ->
                    let l2s = new Dictionary<string, string>()
                    newStrings.[kvp.Key] <- l2s
                    l2s

            // Overlay strings
            for kvp2 in kvp.Value do
                sKey.Item(kvp2.Key) <- kvp2.Value
        newStrings :> ObjectStrings

    let decodeStringFromRCT2 language s =
        let decodedBytes =
            let rec decode s =
                match s with
                | [] -> []
                | 255uy :: a :: b :: tail -> [a; b] @ decode tail
                | c :: tail -> c :: decode tail

            s
            |> Array.toList
            |> decode
            |> List.toArray

        let codepage =
            match language with
            | "ja-JP" -> 932
            | "ko-KR" -> 949
            | "zh-CN" -> 936
            | "zh-TW" -> 950
            | _ -> 1252

        Encoding.GetEncoding(codepage)
                .GetString(decodedBytes)
