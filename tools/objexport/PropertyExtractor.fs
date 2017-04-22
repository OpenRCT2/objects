// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

module PropertyExtractor =

    open System
    open JsonTypes
    open RCT2ObjectData.DataObjects
    open RCT2ObjectData.DataObjects.Types

    let getCursor = function
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

    ///////////////////////////////////////////////////////////////////////////
    // Wall
    ///////////////////////////////////////////////////////////////////////////
    let getWall (wall: Wall) =
        { isDoor = wall.Header.Flags.HasFlag(WallFlags.Door)
          isBanner = wall.Header.Flags.HasFlag(WallFlags.TwoSides)
          hasPrimaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap1)
          hasSecondaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap2)
          hasTenaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap3)
          hasGlass = wall.Header.Flags.HasFlag(WallFlags.Glass)
          isAllowedOnSlope = not (wall.Header.Flags.HasFlag(WallFlags.Flat))
          doorSound =
              match (int wall.Header.Effects <<< 1) &&& 3 with
              | _ -> null
          height = int wall.Header.Clearance
          price = int wall.Header.BuildCost
          cursor = getCursor (int wall.Header.Cursor)
          scrollingMode =
              match int wall.Header.Scrolling with
              | 255 -> 0
              | x -> x }

    ///////////////////////////////////////////////////////////////////////////
    // Footpath
    ///////////////////////////////////////////////////////////////////////////
    let getFootpath (footpath: Pathing) =
        let getSupportType (flags: PathingFlags) =
            if flags.HasFlag(PathingFlags.PoleSupports) then "pole" else "box"

        { hasSupportImages = footpath.Header.Flags.HasFlag(PathingFlags.PoleBase)
          hasElevatedPathImages = footpath.Header.Flags.HasFlag(PathingFlags.OverlayPath)
          editorOnly = footpath.Header.Flags.HasFlag(PathingFlags.Hidden)
          supportType = getSupportType footpath.Header.Flags
          scrollingMode = int footpath.Header.Reserved1 }

    ///////////////////////////////////////////////////////////////////////////
    // Footpath item
    ///////////////////////////////////////////////////////////////////////////
    let getFootpathItem (pa: PathAddition) =
        let getRenderAs = function
            | PathAdditionSubtypes.LitterBin -> "bin"
            | PathAdditionSubtypes.Bench -> "bench"
            | PathAdditionSubtypes.Lamp -> "lamp"
            | PathAdditionSubtypes.JumpFountain -> "fountain"
            | _ -> "other"

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

        let getEntertainer = function
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
    // Park entrance
    ///////////////////////////////////////////////////////////////////////////
    let getParkEntrance (pe: ParkEntrance) =
        { scrollingMode = int pe.Header.SignX
          textHeight = int pe.Header.SignY }

    ///////////////////////////////////////////////////////////////////////////
    // Water
    ///////////////////////////////////////////////////////////////////////////
    let getWater (water: Water) =
        let flags = int (BitConverter.ToInt16(water.Header.Reserved0, 14))
        { allowDucks = ((flags &&& 1) <> 0) }

    ///////////////////////////////////////////////////////////////////////////
    // Catch all
    ///////////////////////////////////////////////////////////////////////////

    let getProperties (obj: ObjectData) =
        match obj.Type with
        | ObjectTypes.Wall -> getWall (obj :?> Wall) :> obj
        | ObjectTypes.Path -> getFootpath (obj :?> Pathing) :> obj
        | ObjectTypes.PathAddition -> getFootpathItem (obj :?> PathAddition) :> obj
        | ObjectTypes.SceneryGroup -> getSceneryGroup (obj :?> SceneryGroup) :> obj
        | ObjectTypes.ParkEntrance -> getParkEntrance (obj :?> ParkEntrance) :> obj
        | ObjectTypes.Water -> getWater (obj :?> Water) :> obj
        | _ -> new Object()

