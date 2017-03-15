// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

module PropertyExtractor =

    open System
    open RCT2ObjectData.DataObjects
    open RCT2ObjectData.DataObjects.Types

    open JsonTypes

    ///////////////////////////////////////////////////////////////////////////
    // Footpath item
    ///////////////////////////////////////////////////////////////////////////
    let getFootpathItem (pa: PathAddition) =
        let getRenderAs t =
            match t with
            | PathAdditionSubtypes.LitterBin -> "bin"
            | PathAdditionSubtypes.Bench -> "bench"
            | PathAdditionSubtypes.Lamp -> "lamp"
            | PathAdditionSubtypes.JumpFountain -> "fountain"
            | _ -> "other"

        let getCursor cursor =
            match cursor with
            | 1 -> "CURSOR_BLANK"
            | 2 -> "CURSOR_UP_ARROW"
            | 3 -> "CURSOR_UP_DOWN_ARROW"
            | 4 -> "CURSOR_HAND_POINT"
            | 5 -> "CURSOR_ZZZ"
            | 6 -> "CURSOR_DIAGONAL_ARROWS"
            | 7 -> "CURSOR_PICKER"
            | 8 -> "CURSOR_TREE_DOWN"
            | 9 -> "CURSOR_FOUNTAIN_DOWN"
            | 10 -> "CURSOR_STATUE_DOWN"
            | 11 -> "CURSOR_BENCH_DOWN"
            | 12 -> "CURSOR_CROSS_HAIR"
            | 13 -> "CURSOR_BIN_DOWN"
            | 14 -> "CURSOR_LAMPPOST_DOWN"
            | 15 -> "CURSOR_FENCE_DOWN"
            | 16 -> "CURSOR_FLOWER_DOWN"
            | 17 -> "CURSOR_PATH_DOWN"
            | 18 -> "CURSOR_DIG_DOWN"
            | 19 -> "CURSOR_WATER_DOWN"
            | 20 -> "CURSOR_HOUSE_DOWN"
            | 21 -> "CURSOR_VOLCANO_DOWN"
            | 22 -> "CURSOR_WALK_DOWN"
            | 23 -> "CURSOR_PAINT_DOWN"
            | 24 -> "CURSOR_ENTRANCE_DOWN"
            | 25 -> "CURSOR_HAND_OPEN"
            | 26 -> "CURSOR_HAND_CLOSED"
            | _ -> "CURSOR_ARROW"

        { isBin = pa.Header.Flags.HasFlag(PathAdditionFlags.HoldTrash)
          isBench = pa.Header.Flags.HasFlag(PathAdditionFlags.CanSit)
          isLamp = pa.Header.Flags.HasFlag(PathAdditionFlags.Light)
          isTelevision = pa.Header.Flags.HasFlag(PathAdditionFlags.QueueTV)
          isBreakable = pa.Header.Flags.HasFlag(PathAdditionFlags.CanVandal)
          isJumpingFountainWater = pa.Header.Flags.HasFlag(PathAdditionFlags.JumpFountain)
          isJumpingFountainSnow = pa.Header.Flags.HasFlag(PathAdditionFlags.JumpSnowball)
          isAllowedOnQueue = not (pa.Header.Flags.HasFlag(PathAdditionFlags.Unknown1))
          isAllowedOnSlope = not (pa.Header.Flags.HasFlag(PathAdditionFlags.Unknown2))
          renderAs = getRenderAs pa.Header.Subtype
          cursor = getCursor (int pa.Header.Cursor)
          price = int pa.Header.BuildCost }

    ///////////////////////////////////////////////////////////////////////////
    // Scenery group
    ///////////////////////////////////////////////////////////////////////////
    let getSceneryGroup (scg: SceneryGroup) =
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

        let getEntertainers bits =
            getBits bits
            |> Seq.map(getEntertainer)
            |> Seq.toList

        let getEntries =
            scg.Items
            |> Seq.map(fun x -> x.FileName)
            |> Seq.toList

        { entries = getEntries
          order = int scg.Header.Unknown0x108
          entertainerCostumes = getEntertainers (int scg.Header.Unknown0x10A) }

    ///////////////////////////////////////////////////////////////////////////
    // Catch all
    ///////////////////////////////////////////////////////////////////////////

    let getProperties (obj: ObjectData) =
        match obj.Type with
        | ObjectTypes.PathAddition -> getFootpathItem (obj :?> PathAddition) :> obj
        | ObjectTypes.SceneryGroup -> getSceneryGroup (obj :?> SceneryGroup) :> obj
        | _ -> new Object()

