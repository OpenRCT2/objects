// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

module Seq =
    let filterOut needle =
        Seq.filter (fun x -> x <> needle)

    let singleOrMany items =
        let len = Seq.length items
        if len = 0 then
            null
        elif len = 1 then
            (Seq.head items) :> obj
        else
            (items |> Seq.toList) :> obj

module PropertyExtractor =

    open System
    open System.IO
    open JsonTypes
    open RCT2ObjectData.DataObjects
    open RCT2ObjectData.DataObjects.Types
    open RCT2ObjectData.DataObjects.Types.AttractionInfo

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

    let getColour (c: RemapColors) =
        match int c with
        | 0 -> "black"
        | 1 -> "grey"
        | 2 -> "white"
        | 3 -> "dark_purple"
        | 4 -> "light_purple"
        | 5 -> "bright_purple"
        | 6 -> "dark_blue"
        | 7 -> "light_blue"
        | 8 -> "icy_blue"
        | 9 -> "teal"
        | 10 -> "aquamarine"
        | 11 -> "saturated_green"
        | 12 -> "dark_green"
        | 13 -> "moss_green"
        | 14 -> "bright_green"
        | 15 -> "olive_green"
        | 16 -> "dark_olive_green"
        | 17 -> "bright_yellow"
        | 18 -> "yellow"
        | 19 -> "dark_yellow"
        | 20 -> "light_orange"
        | 21 -> "dark_orange"
        | 22 -> "light_brown"
        | 23 -> "saturated_brown"
        | 24 -> "dark_brown"
        | 25 -> "salmon_pink"
        | 26 -> "bordeaux_red"
        | 27 -> "saturated_red"
        | 28 -> "bright_red"
        | 29 -> "dark_pink"
        | 30 -> "bright_pink"
        | 31 -> "light_pink"
        | _ -> "unknown"

    let getBits32 (x: int) =
        seq { 0..31 }
        |> Seq.filter(fun i -> (x &&& (1 <<< i)) <> 0)

    let getBits64 (x: int64) =
        seq { 0..63 }
        |> Seq.filter(fun i -> (x &&& (1L <<< i)) <> 0L)

    let getSceneryGroupHeader (obj: ObjectData) =
        match obj.GroupInfo with
        | hdr when not (isNull hdr) && not (String.IsNullOrEmpty hdr.FileName) -> hdr.FileName
        | _ -> null

    ///////////////////////////////////////////////////////////////////////////
    // Ride
    ///////////////////////////////////////////////////////////////////////////
    let isTrackTypeShop = function
        | TrackTypes.DrinksStall
        | TrackTypes.FoodStall
        | TrackTypes.Restroom
        | TrackTypes.SouvenirStall -> true
        | _ -> false

    let getCar(car: CarHeader) =
        // CarHeader is just too wrong with variable alignment and too many unknowns
        // Read it ourselves from a buffer
        use ms = new MemoryStream()
        let bw = new BinaryWriter(ms)
        car.Write(bw)

        ms.Position <- 0L
        let br = new BinaryReader(ms)

        let rotationFrameMask = br.ReadUInt16()
        ignore (br.ReadByte())
        ignore (br.ReadByte())
        let spacing = br.ReadUInt32()
        let mass = br.ReadUInt16()
        let tabOffset = br.ReadSByte()
        let numSeats = br.ReadByte()
        let spriteFlags = br.ReadUInt16()
        let spriteWidth = br.ReadByte()
        let spriteHeightNegative = br.ReadByte()
        let spriteHeightPositive = br.ReadByte()
        let var11 = br.ReadByte()
        let flags = br.ReadUInt32()
        let baseNumFrames = br.ReadUInt16()
        ignore (br.ReadBytes(15 * 4))
        let numSeatRows = br.ReadByte()
        let spinningInertia = br.ReadByte()
        let spinningFriction = br.ReadByte()
        let frictionSoundId = br.ReadByte()
        let var58 = br.ReadByte()
        let soundRange = br.ReadByte()
        let var5A = br.ReadByte()
        let poweredAcceleration = br.ReadByte()
        let poweredMaxSpeed = br.ReadByte()
        let carVisual = br.ReadByte()
        let effectVisual = br.ReadByte()
        let drawOrder = br.ReadByte()
        let numVerticalFramesOverride = br.ReadByte()

        let frames =
            let hasSpriteFlag i = (int spriteFlags &&& (1 <<< i)) <> 0
            { flat = hasSpriteFlag 0
              gentleSlopes = hasSpriteFlag 1
              steepSlopes = hasSpriteFlag 2
              verticalSlopes = hasSpriteFlag 3
              diagonalSlopes = hasSpriteFlag 4
              flatBanked = hasSpriteFlag 5
              inlineTwists = hasSpriteFlag 6
              flatToGentleSlopeBankedTransitions = hasSpriteFlag 7
              diagonalGentleSlopeBankedTransitions = hasSpriteFlag 8
              gentleSlopeBankedTransitions = hasSpriteFlag 9
              gentleSlopeBankedTurns = hasSpriteFlag 10
              flatToGentleSlopeWhileBankedTransitions = hasSpriteFlag 11
              corkscrews = hasSpriteFlag 12
              restraintAnimation = hasSpriteFlag 13
              VEHICLE_SPRITE_FLAG_14 = hasSpriteFlag 14
              VEHICLE_SPRITE_FLAG_15 = hasSpriteFlag 15 }

        let hasFlag i = (int flags &&& (1 <<< i)) <> 0

        { rotationFrameMask = int rotationFrameMask
          spacing = int spacing
          mass = int mass
          tabOffset = int tabOffset
          numSeats = int numSeats
          spriteWidth = int spriteWidth
          spriteHeightNegative = int spriteHeightNegative
          spriteHeightPositive = int spriteHeightPositive
          var11 = int var11
          baseNumFrames = int baseNumFrames
          numSeatRows = int numSeatRows
          spinningInertia = int spinningInertia
          spinningFriction = int spinningFriction
          frictionSoundId =
              match int frictionSoundId with
              | 255 -> None
              | i -> Some i
          var58 = int var58
          soundRange =
              match int soundRange with
              | 255 -> None
              | i -> Some i
          var5A = int var5A
          poweredAcceleration = int poweredAcceleration
          poweredMaxSpeed = int poweredMaxSpeed
          carVisual = int carVisual
          effectVisual =
              match int effectVisual with
              | 1 -> None
              | i -> Some i
          drawOrder = int drawOrder
          numVerticalFramesOverride = int numVerticalFramesOverride
          frames = frames
          VEHICLE_ENTRY_FLAG_0 = hasFlag 0
          VEHICLE_ENTRY_FLAG_NO_UPSTOP_WHEELS = hasFlag 1
          VEHICLE_ENTRY_FLAG_NO_UPSTOP_BOBSLEIGH = hasFlag 2
          VEHICLE_ENTRY_FLAG_MINI_GOLF = hasFlag 3
          VEHICLE_ENTRY_FLAG_4 = hasFlag 4
          VEHICLE_ENTRY_FLAG_5 = hasFlag 5
          VEHICLE_ENTRY_FLAG_HAS_INVERTED_SPRITE_SET = hasFlag 6
          VEHICLE_ENTRY_FLAG_7 = hasFlag 7
          VEHICLE_ENTRY_FLAG_ALLOW_DOORS_DEPRECATED = hasFlag 8
          VEHICLE_ENTRY_FLAG_ENABLE_ADDITIONAL_COLOUR_2 = hasFlag 9
          VEHICLE_ENTRY_FLAG_10 = hasFlag 10
          VEHICLE_ENTRY_FLAG_11 = hasFlag 11
          VEHICLE_ENTRY_FLAG_OVERRIDE_NUM_VERTICAL_FRAMES = hasFlag 12
          VEHICLE_ENTRY_FLAG_13 = hasFlag 13
          VEHICLE_ENTRY_FLAG_14 = hasFlag 14
          VEHICLE_ENTRY_FLAG_15 = hasFlag 15
          VEHICLE_ENTRY_FLAG_ENABLE_ADDITIONAL_COLOUR_1 = hasFlag 16
          VEHICLE_ENTRY_FLAG_SWINGING = hasFlag 17
          VEHICLE_ENTRY_FLAG_SPINNING = hasFlag 18
          VEHICLE_ENTRY_FLAG_POWERED = hasFlag 19
          VEHICLE_ENTRY_FLAG_RIDERS_SCREAM = hasFlag 20
          VEHICLE_ENTRY_FLAG_21 = hasFlag 21
          VEHICLE_ENTRY_FLAG_22 = hasFlag 22
          VEHICLE_ENTRY_FLAG_23 = hasFlag 23
          VEHICLE_ENTRY_FLAG_24 = hasFlag 24
          VEHICLE_ENTRY_FLAG_25 = hasFlag 25
          VEHICLE_ENTRY_FLAG_26 = hasFlag 26
          VEHICLE_ENTRY_FLAG_27 = hasFlag 27
          VEHICLE_ENTRY_FLAG_28 = hasFlag 28
          VEHICLE_ENTRY_FLAG_29 = hasFlag 29
          VEHICLE_ENTRY_FLAG_30 = hasFlag 30
          VEHICLE_ENTRY_FLAG_31 = hasFlag 31 }

    let getRide (ride: Attraction) =
        // TODO populate this fully
        let getRideType = function
            | TrackTypes.AirPoweredVerticalCoaster -> "air_powered_vertical_rc"
            | TrackTypes.BoatHire -> "boat_hire"
            | TrackTypes.BobsledCoaster -> "bobsleigh_rc"
            | TrackTypes.BumperCars -> "dodgems"
            | TrackTypes.CarRide -> "car_ride"
            | TrackTypes.CashMachine -> "cash_machine"
            | TrackTypes.ChairLift -> "chairlift"
            | TrackTypes.Cinema3D -> "3d_cinema"
            | TrackTypes.Circus -> "circus"
            | TrackTypes.CorkscrewRollerCoaster -> "corkscrew_rc"
            | TrackTypes.CrookedHouse -> "crooked_house"
            | TrackTypes.DrinksStall -> "drink_stall"
            | TrackTypes.Elevator -> "lift"
            | TrackTypes.Enterprise -> "enterprise"
            | TrackTypes.FerrisWheel -> "ferris_wheel"
            | TrackTypes.FirstAid -> "first_aid"
            | TrackTypes.FlyingRollerCoaster -> "flying_rc"
            | TrackTypes.FlyingSaucers -> "flying_saucers"
            | TrackTypes.FoodStall -> "food_stall"
            | TrackTypes.GigaCoaster -> "giga_coaster"
            | TrackTypes.GoKarts -> "go_karts"
            | TrackTypes.HauntedHouse -> "haunted_house"
            | TrackTypes.HauntedRide -> "ghost_train"
            | TrackTypes.HeartlineTwisterCoaster -> "heartline_twister_rc"
            | TrackTypes.HedgeMaze -> "maze"
            | TrackTypes.InfoKiosk -> "information_kiosk"
            | TrackTypes.InvertedHairpinCoaster -> "inverted_hairpin_rc"
            | TrackTypes.InvertedImpulseCoaster -> "inverted_impulse_rc"
            | TrackTypes.InvertedRollerCoaster -> "inverted_rc"
            | TrackTypes.InvertedShuttleCoaster -> "compact_inverted_rc"
            | TrackTypes.JuniorRollerCoaster -> "junior_rc"
            | TrackTypes.LaunchedFreefall -> "launched_freefall"
            | TrackTypes.LaydownRollerCoaster -> "lay_down_rc"
            | TrackTypes.LIMLaunchedRollerCoaster -> "lim_launched_rc"
            | TrackTypes.LogFlume -> "log_flume"
            | TrackTypes.LoopingRollerCoaster -> "looping_rc"
            | TrackTypes.MagicCarpet -> "magic_carpet"
            | TrackTypes.MerryGoRound -> "merry_go_round"
            | TrackTypes.MineRide -> "mine_ride"
            | TrackTypes.MineTrainCoaster -> "mine_train_rc"
            | TrackTypes.MiniGolf -> "mini_golf"
            | TrackTypes.MiniHelicopters -> "mini_helicopters"
            | TrackTypes.MiniRollerCoaster -> "mini_rc"
            | TrackTypes.MiniSuspendedCoaster -> "mini_suspended_rc"
            | TrackTypes.Monorail -> "monorail"
            | TrackTypes.MonorailCycles -> "monorail_cycles"
            | TrackTypes.MotionSimulator -> "motion_simulator"
            | TrackTypes.MultiDimensionRollerCoaster -> "multi_dimension_rc"
            | TrackTypes.ObservationTower -> "observation_tower"
            | TrackTypes.Railroad -> "miniature_railway"
            | TrackTypes.Restroom -> "toilets"
            | TrackTypes.ReverseFreefallCoaster -> "reverse_freefall_rc"
            | TrackTypes.ReverserRollerCoaster -> "reverser_rc"
            | TrackTypes.RiverRafts -> "river_rafts"
            | TrackTypes.RiverRapids -> "river_rapids"
            | TrackTypes.RotoDrop -> "roto_drop"
            | TrackTypes.SideFrictionRollerCoaster -> "side_friction_rc"
            | TrackTypes.SingleRailCoaster -> "steeplechase"
            | TrackTypes.SouvenirStall -> "shop"
            | TrackTypes.SpaceRings -> "space_rings"
            | TrackTypes.SpiralRollerCoaster -> "spiral_rc"
            | TrackTypes.SpiralSlide -> "spiral_slide"
            | TrackTypes.SplashBoats -> "splash_boats"
            | TrackTypes.StandUpRollerCoaster -> "stand_up_rc"
            | TrackTypes.SubmarineRide -> "submarine_ride"
            | TrackTypes.SuspendedMonorail -> "suspended_monorail"
            | TrackTypes.SuspendedSwingingCoaster -> "suspended_swinging_rc"
            | TrackTypes.SwingingInvertedShip -> "swinging_inverted_ship"
            | TrackTypes.SwingingShip -> "swinging_ship"
            | TrackTypes.TopSpin -> "top_spin"
            | TrackTypes.Twist -> "twist"
            | TrackTypes.TwisterRollerCoaster -> "twister_rc"
            | TrackTypes.VerticalDropRollerCoaster -> "vertical_drop_rc"
            | TrackTypes.VirginiaReel -> "virginia_reel"
            | TrackTypes.WaterCoaster -> "water_coaster"
            | TrackTypes.WaterSlide -> "dinghy_slide"
            | TrackTypes.WildMouse -> "steel_wild_mouse"
            | TrackTypes.WoodenRollerCoaster -> "wooden_rc"
            | TrackTypes.WoodenWildRide -> "wooden_wild_mouse"
            | x -> x.ToString().ToLower()

        let rideTypes =
            ride.Header.TrackTypeList
            |> Seq.filterOut TrackTypes.None
            |> Seq.map getRideType
            |> Seq.toList

        let isShop = Seq.exists isTrackTypeShop ride.Header.TrackTypeList

        let headCars =
            [| ride.Header.FrontCarType
               ride.Header.SecondCarType
               ride.Header.ThirdCarType |]
            |> Seq.filterOut CarTypes.None
            |> Seq.map int
            |> Seq.toList

        let tailCars =
            [| ride.Header.RearCarType |]
            |> Seq.filterOut CarTypes.None
            |> Seq.map int
            |> Seq.toList

        let cars =
            if isShop then
                []
            else
                ride.Header.CarTypeList
                |> Seq.map getCar
                |> Seq.takeWhile (fun c -> c.effectVisual <> Some 0)
                |> Seq.toList

        let ratingMultiplier =
            match (int ride.Header.Excitement, int ride.Header.Intensity, int ride.Header.Nausea) with
            | (0, 0, 0) -> None
            | (e, i, n) -> Some { excitement = e; intensity = i; nausea = n }

        let availableTrackPieces =
            match ride.Header.AvailableTrackSections with
            | TrackSections.All -> None
            | sections ->
                Some (getBits64 (int64 sections)
                |> Seq.map (fun x -> 1UL <<< x)
                |> Seq.map (fun x -> LanguagePrimitives.EnumOfValue<uint64, TrackSections> x)
                |> Seq.map (fun x -> x.ToString().ToLower())
                |> Seq.toList)

        { ``type`` = rideTypes
          category =
              [| ride.Header.RideType; ride.Header.RideTypeAlternate |]
              |> Seq.filterOut RideTypes.None
              |> Seq.map (fun x -> x.ToString().ToLower())
              |> Seq.toList
          sells =
              [| ride.Header.SoldItem1; ride.Header.SoldItem2 |]
              |> Seq.filterOut ItemTypes.None
              |> Seq.map (fun x -> x.ToString().ToLower())
              |> Seq.toList
          tabScale = if ride.Header.Flags.HasFlag(AttractionFlags.Unknown1_0) then 0.5f else 0.0f
          separateRide = ride.Header.Flags.HasFlag(AttractionFlags.SeparateRide)
          operatingModes = []
          hasShelter = ride.Header.Flags.HasFlag(AttractionFlags.Covered)
          disableBreakdown = ride.Header.Flags.HasFlag(AttractionFlags.RowingBoatsCanoesElevator)
          disablePainting = ride.Header.Flags.HasFlag(AttractionFlags.SunglassesStall)
          noInversions = ride.Header.Flags.HasFlag(AttractionFlags.Unknown2_0)
          noBanking = ride.Header.Flags.HasFlag(AttractionFlags.Unused4_0)
          limitAirTimeBonus = ride.Header.Flags.HasFlag(AttractionFlags.Unknown8_2)
          playDepartSound = ride.Header.Flags.HasFlag(AttractionFlags.NoTrackRemap)
          playSplashSound = ride.Header.Flags.HasFlag(AttractionFlags.RidersGetWet)
          playSplashSoundSlide = ride.Header.Flags.HasFlag(AttractionFlags.SlowInWater)
          swingMode =
              if ride.Header.Flags.HasFlag(AttractionFlags.MagicCarpetInvertedShip) then
                  if ride.Header.Flags.HasFlag(AttractionFlags.MagicCarpet) then 2
                  else 1
              else 0
          rotationMode =
              if ride.Header.Flags.HasFlag(AttractionFlags.TwistSnowCups) then 1
              elif ride.Header.Flags.HasFlag(AttractionFlags.Enterprise) then 2
              else 0
          RIDE_ENTRY_FLAG_7 = ride.Header.Flags.HasFlag(AttractionFlags.Unused8_1)
          RIDE_ENTRY_FLAG_16 = ride.Header.Flags.HasFlag(AttractionFlags.SpinningWildMouse)
          RIDE_ENTRY_FLAG_18 = ride.Header.Flags.HasFlag(AttractionFlags.Unknown4_4)
          minCarsPerTrain = int ride.Header.MinCarsPerTrain
          maxCarsPerTrain = int ride.Header.MaxCarsPerTrain
          carsPerFlatRide = int ride.Header.CarsPerFlatRide
          numEmptyCars = int ride.Header.ZeroCars
          tabCar = int ride.Header.CarTabIndex
          defaultCar = int ride.Header.DefaultCarType
          headCars = headCars
          tailCars = tailCars
          ratingMultipler = ratingMultiplier
          maxHeight = int ride.Header.MaxHeight
          availableTrackPieces = availableTrackPieces
          carColours =
              // ObjectData library doesn't keep whether colours are per car or not
              // Assume if there are 32 then it is per car
              if ride.CarColors.Count = 32 then
                  [| ride.CarColors
                      |> Seq.map (Array.map getColour)
                      |> Seq.toArray |]
              else
                  ride.CarColors
                      |> Seq.map (Array.map getColour)
                      |> Seq.map (fun x -> [| x |])
                      |> Seq.toArray
          cars = cars
          loadingPositions =
              let count =
                  ride.RiderPositions
                  |> Seq.tryFindIndexBack (Array.isEmpty >> not)
              match count with
              | Some count ->
                  ride.RiderPositions
                  |> Seq.take (count + 1)
                  |> Seq.map (Array.map (sbyte >> int))
                  |> Seq.toArray
              | None -> null }

    ///////////////////////////////////////////////////////////////////////////
    // Small scenery
    ///////////////////////////////////////////////////////////////////////////
    let getSmallScenery (smallScenery: SmallScenery) =
        let hasFlag flag = ((int smallScenery.Header.Flags) &&& (1 <<< flag)) <> 0

        { price = int smallScenery.Header.BuildCost
          removalPrice = int smallScenery.Header.RemoveCost
          cursor = getCursor (int smallScenery.Header.Cursor)
          height = int smallScenery.Header.Height
          animationDelay = int smallScenery.Header.Animation1
          animationMask = int smallScenery.Header.Animation2
          numFrames = int smallScenery.Header.Animation3
          sceneryGroup = getSceneryGroupHeader smallScenery
          frameOffsets =
              match Seq.toArray smallScenery.AnimationSequence with
              | [||] -> null
              | offsets -> offsets |> Array.map int
          shape =
              let isFullTile = hasFlag 0
              let isDiag = hasFlag 8
              let is2q = hasFlag 24
              let is3q = hasFlag 25
              let part0 =
                  if isFullTile then
                      if is2q then "2/4"
                      elif is3q then "3/4"
                      else "4/4"
                  else
                      // TT:ARTDEC29 is only known occurrence of a 2/4 or 3/4 without isFullTile
                      if is2q then "2/4"
                      elif is3q then "3/4"
                      else "1/4"
              if isDiag then part0 + "+D"
              else part0

          SMALL_SCENERY_FLAG_VOFFSET_CENTRE = hasFlag 1
          requiresFlatSurface = hasFlag 2
          isRotatable = hasFlag 3
          isAnimated = hasFlag 4
          canWither = hasFlag 5
          canBeWatered = hasFlag 6
          hasOverlayImage = hasFlag 7
          hasGlass = hasFlag 9
          hasPrimaryColour = hasFlag 10
          SMALL_SCENERY_FLAG_FOUNTAIN_SPRAY_1 = hasFlag 11
          SMALL_SCENERY_FLAG_FOUNTAIN_SPRAY_4 = hasFlag 12
          isClock = hasFlag 13
          SMALL_SCENERY_FLAG_SWAMP_GOO = hasFlag 14
          SMALL_SCENERY_FLAG17 = hasFlag 16
          isStackable = hasFlag 17
          prohibitWalls = hasFlag 18
          hasSecondaryColour = hasFlag 19
          hasNoSupports = hasFlag 20
          SMALL_SCENERY_FLAG_VISIBLE_WHEN_ZOOMED = hasFlag 21
          SMALL_SCENERY_FLAG_COG = hasFlag 22
          allowSupportsAbove = hasFlag 23
          supportsHavePrimaryColour = hasFlag 26
          SMALL_SCENERY_FLAG27 = hasFlag 27 }

    ///////////////////////////////////////////////////////////////////////////
    // Large scenery
    ///////////////////////////////////////////////////////////////////////////
    let getLargeScenery (largeScenery: LargeScenery) =
        let getTile (tile: LargeSceneryTileHeader) =
            let flags0 = int tile.Unknown1
            let flags1 = int tile.Flags
            { x = int tile.Row
              y = int tile.Column
              z = int tile.BaseHeight
              clearance = int tile.Clearance
              hasSupports = (flags0 &&& (1 <<< 5)) = 0
              allowSupportsAbove = (flags0 &&& (1 <<< 6)) <> 0
              walls = (flags1 &&& 0x0F)
              corners =
                  // 15 (all corners occupied) is the default, so don't emit that
                  match (flags1 >>> 4) with
                  | 15 -> None
                  | i -> Some i }

        let get3dFont =
            if largeScenery.Text3D.Count = 0 then
                None
            else
                use ms = new System.IO.MemoryStream(Seq.toArray largeScenery.Text3D)
                let br = new System.IO.BinaryReader(ms)

                let readOffset() =
                    let x = int (br.ReadInt16())
                    let y = int (br.ReadInt16())
                    { x = x; y = y }

                let offset0 = readOffset()
                let offset1 = readOffset()
                let maxWidth = int (br.ReadInt16())
                ignore (br.ReadInt16())
                let flags = int (br.ReadByte())
                let numImages = int (br.ReadByte())
                let glyphs =
                    [| for i in 0..255 do
                           let image = int (br.ReadByte())
                           let width = int (br.ReadByte())
                           let height = int (br.ReadByte())
                           ignore (br.ReadByte())
                           yield { image = image; width = width; height = height } |]

                Some { offsets = [| offset0; offset1 |]
                       maxWidth = maxWidth
                       numImages = numImages
                       isVertical = (flags &&& 1) <> 0
                       isTwoLine = (flags &&& 2) <> 0
                       glyphs = glyphs }

        { price = int largeScenery.Header.BuildCost
          removalPrice = int largeScenery.Header.RemoveCost
          cursor = getCursor (int largeScenery.Header.Cursor)
          hasPrimaryColour = largeScenery.Header.Flags.HasFlag(LargeSceneryFlags.Color1)
          hasSecondaryColour = largeScenery.Header.Flags.HasFlag(LargeSceneryFlags.Color2)
          isAnimated = largeScenery.Header.Flags.HasFlag(LargeSceneryFlags.TextScrolling)
          isPhotogenic = largeScenery.Header.Flags.HasFlag(LargeSceneryFlags.Photogenic)
          scrollingMode =
              match int largeScenery.Header.Scrolling with
              | 255 -> None
              | i -> Some i
          sceneryGroup = getSceneryGroupHeader largeScenery
          tiles =
              largeScenery.Tiles
              |> Seq.map getTile
              |> Seq.toArray
          ``3dFont`` = get3dFont }

    ///////////////////////////////////////////////////////////////////////////
    // Wall
    ///////////////////////////////////////////////////////////////////////////
    let getWall (wall: Wall) =
        { isAnimated = ((int wall.Header.Effects) &&& (1 <<< 4)) <> 0
          isLongDoorAnimation = ((int wall.Header.Flags) &&& 32) <> 0
          isDoor = wall.Header.Flags.HasFlag(WallFlags.Door)
          isBanner = wall.Header.Flags.HasFlag(WallFlags.TwoSides)
          hasPrimaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap1)
          hasSecondaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap2)
          hasTernaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap3)
          hasGlass = wall.Header.Flags.HasFlag(WallFlags.Glass)
          isOpaque = ((int wall.Header.Effects) &&& (1 <<< 3)) <> 0
          isAllowedOnSlope = not (wall.Header.Flags.HasFlag(WallFlags.Flat))
          doorSound = (int wall.Header.Effects <<< 1) &&& 3
          height = int wall.Header.Clearance
          price = int wall.Header.BuildCost
          cursor =
              match int wall.Header.Cursor with
              | 15 -> null // cursor wall - most common cursor for walls
              | c -> getCursor c
          scrollingMode =
              match int wall.Header.Scrolling with
              | 255 -> None
              | i -> Some i
          sceneryGroup = getSceneryGroupHeader wall }

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
          price = int pa.Header.BuildCost
          sceneryGroup = getSceneryGroupHeader pa }

    ///////////////////////////////////////////////////////////////////////////
    // Footpath banner
    ///////////////////////////////////////////////////////////////////////////
    let getFootpathBanner (pb: PathBanner) =
        { scrollingMode = int pb.Header.Scrolling
          price = int pb.Header.BuildCost
          hasPrimaryColour = pb.Header.Flags.HasFlag(PathBannerFlags.Color1)
          sceneryGroup = getSceneryGroupHeader pb }

    ///////////////////////////////////////////////////////////////////////////
    // Scenery group
    ///////////////////////////////////////////////////////////////////////////
    let getSceneryGroup (scg: SceneryGroup) =
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
            getBits32 bits
            |> Seq.map(getEntertainer)
            |> Seq.toList

        let getEntries =
            scg.Items
            |> Seq.map(fun x -> x.FileName)
            |> Seq.toList

        { entries = getEntries
          priority = int scg.Header.Unknown0x108
          entertainerCostumes =
            let bits = (int scg.Header.Unknown0x10A) ||| (int (scg.Header.Unknown0x10B) <<< 8)
            getEntertainers bits }

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
        let toHex (c: Drawing.Color) = String.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B)
        let paletteNames =
            [| "general"
               "waves-0"; "waves-1"; "waves-2"
               "sparkles-0"; "sparkles-1"; "sparkles-2" |]
        let numPalettes = min (Array.length paletteNames) water.GraphicsData.NumPalettes
        Array.Resize(ref paletteNames, numPalettes)
        let palettes =
            paletteNames
            |> Array.mapi (fun i name -> (name, water.GraphicsData.GetPalette(i)))
            |> Array.map (fun (name, palette) ->
                let palette =
                    { index = palette.Offset
                      colours =
                          palette.Colors
                          |> Array.map toHex }
                (name, palette))
            |> dict
        let flags = int (BitConverter.ToInt16(water.Header.Reserved0, 14))
        { palettes = palettes
          allowDucks = ((flags &&& 1) <> 0) }

    ///////////////////////////////////////////////////////////////////////////
    // Catch all
    ///////////////////////////////////////////////////////////////////////////

    let getProperties (obj: ObjectData) =
        match obj.Type with
        | ObjectTypes.Attraction -> getRide (obj :?> Attraction) :> obj
        | ObjectTypes.SmallScenery -> getSmallScenery (obj :?> SmallScenery) :> obj
        | ObjectTypes.LargeScenery -> getLargeScenery (obj :?> LargeScenery) :> obj
        | ObjectTypes.Wall -> getWall (obj :?> Wall) :> obj
        | ObjectTypes.Path -> getFootpath (obj :?> Pathing) :> obj
        | ObjectTypes.PathAddition -> getFootpathItem (obj :?> PathAddition) :> obj
        | ObjectTypes.PathBanner -> getFootpathBanner (obj :?> PathBanner) :> obj
        | ObjectTypes.SceneryGroup -> getSceneryGroup (obj :?> SceneryGroup) :> obj
        | ObjectTypes.ParkEntrance -> getParkEntrance (obj :?> ParkEntrance) :> obj
        | ObjectTypes.Water -> getWater (obj :?> Water) :> obj
        | _ -> new Object()

