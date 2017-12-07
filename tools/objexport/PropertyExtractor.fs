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

    let getBits32 (x: int) =
        seq { 0..31 }
        |> Seq.filter(fun i -> (x &&& (1 <<< i)) <> 0)

    let getBits64 (x: int64) =
        seq { 0..63 }
        |> Seq.filter(fun i -> (x &&& (1L <<< i)) <> 0L)

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
        { numSeats = int car.RiderSettings
          numSeatRows = int car.RiderSprites
          friction = int car.CarFriction
          spacing = int car.CarSpacing
          tabOffset = int car.CarTabHeight
          spinningInertia = int car.SpinningInertia
          spinningFriction = int car.SpinningFriction
          poweredAcceleration = int car.PoweredAcceleration
          poweredMaxSpeed = int car.PoweredMaxSpeed
          carVisual = int car.CarVisual
          effectVisual = int car.UnknownSetting
          drawOrder = int car.DrawOrder
          specialFrames = int car.SpecialFrames
          rotationFrameMask= int car.LastRotationFrame }

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
                let indexedCars = headCars @ tailCars @ [int ride.Header.CarTabIndex; int ride.Header.DefaultCarType]
                ride.Header.CarTypeList
                |> Seq.indexed
                |> Seq.filter (fun (i, v) -> Seq.contains i indexedCars)
                |> Seq.map (fun (i, v) -> getCar v)
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
          tabScale = if ride.Header.Flags.HasFlag(AttractionFlags.Unknown1_0) then 2 else 0
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
          swingMode = 0
          rotationMode = 0
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
          cars = cars }

    ///////////////////////////////////////////////////////////////////////////
    // Small scenery
    ///////////////////////////////////////////////////////////////////////////
    let getSmallScenery (smallScenery: SmallScenery) =
        { price = int smallScenery.Header.BuildCost
          removalPrice = int smallScenery.Header.RemoveCost
          sceneryGroup = string "" //smallScenery.Header.Fill2
          cursor = getCursor (int smallScenery.Header.Cursor)
          height = int smallScenery.Header.Height
          frameOffsets = int smallScenery.Header.GraphicsStart }

    ///////////////////////////////////////////////////////////////////////////
    // Large scenery
    ///////////////////////////////////////////////////////////////////////////
    let getLargeScenery (largeScenery: LargeScenery) =
        { price = int largeScenery.Header.BuildCost
          removalPrice = int largeScenery.Header.RemoveCost
          sceneryGroup = string "" //smallScenery.Header.Fill2
          cursor = getCursor (int largeScenery.Header.Cursor) }

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
        | ObjectTypes.Attraction -> getRide (obj :?> Attraction) :> obj
        | ObjectTypes.SmallScenery -> getSmallScenery (obj :?> SmallScenery) :> obj
        | ObjectTypes.LargeScenery -> getLargeScenery (obj :?> LargeScenery) :> obj
        | ObjectTypes.Wall -> getWall (obj :?> Wall) :> obj
        | ObjectTypes.Path -> getFootpath (obj :?> Pathing) :> obj
        | ObjectTypes.PathAddition -> getFootpathItem (obj :?> PathAddition) :> obj
        | ObjectTypes.SceneryGroup -> getSceneryGroup (obj :?> SceneryGroup) :> obj
        | ObjectTypes.ParkEntrance -> getParkEntrance (obj :?> ParkEntrance) :> obj
        | ObjectTypes.Water -> getWater (obj :?> Water) :> obj
        | _ -> new Object()

