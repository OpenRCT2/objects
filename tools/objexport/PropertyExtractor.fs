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

type FootpathSplitChild = FootpathSurface | FootpathQueue | FootpathRailings

module PropertyExtractor =

    open System
    open System.IO
    open JsonTypes
    open RCT2ObjectData.Drawing
    open RCT2ObjectData.Objects
    open RCT2ObjectData.Objects.Types
    open RCT2ObjectData.Objects.Types.AttractionInfo

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

    let getCar (ride: Attraction) (car: CarHeader) (loadingPositionsRaw: byte[] option) =
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
        let (numSeats, seatsInPairs) =
            let b = int (br.ReadByte())
            (b &&& 0x7F, (b &&& 0x80) <> 0)
        let spriteFlags = br.ReadUInt16()
        let spriteWidth = br.ReadByte()
        let spriteHeightNegative = br.ReadByte()
        let spriteHeightPositive = br.ReadByte()
        let animation = br.ReadByte()
        let flags = br.ReadUInt32()
        let baseNumFrames = br.ReadUInt16()
        ignore (br.ReadBytes(15 * 4))
        let numSeatRows = br.ReadByte()
        let spinningInertia = br.ReadByte()
        let spinningFriction = br.ReadByte()
        let frictionSoundId = br.ReadByte()
        let logFlumeReverserVehicleType = br.ReadByte()
        let soundRange = br.ReadByte()
        let doubleSoundFrequency = br.ReadByte()
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
              curvedLiftHill = hasSpriteFlag 14
              VEHICLE_SPRITE_FLAG_15 = hasSpriteFlag 15 }

        let hasFlag i = (int flags &&& (1 <<< i)) <> 0

        let loadingPositions =
            match loadingPositionsRaw with
            | Some loadingPositions when not (Array.isEmpty loadingPositions) && not (hasFlag 26) -> Some loadingPositions
            | _ -> None
        let loadingWaypoints =
            match loadingPositionsRaw with
            | Some loadingWaypoints when not (Array.isEmpty loadingWaypoints) && hasFlag 26 -> Some loadingWaypoints
            | _ -> None

        { rotationFrameMask = int rotationFrameMask
          spacing = int spacing
          mass = int mass
          tabOffset = int tabOffset
          numSeats = numSeats
          seatsInPairs =
              if seatsInPairs then
                  None
              elif numSeats <= 1 then
                  None
              else
                  Some false
          spriteWidth = int spriteWidth
          spriteHeightNegative = int spriteHeightNegative
          spriteHeightPositive = int spriteHeightPositive
          animation = int animation
          baseNumFrames = int baseNumFrames
          numSeatRows = int numSeatRows
          spinningInertia = int spinningInertia
          spinningFriction = int spinningFriction
          frictionSoundId =
              match int frictionSoundId with
              | 255 -> None
              | i -> Some i
          logFlumeReverserVehicleType = int logFlumeReverserVehicleType
          soundRange =
              match int soundRange with
              | 255 -> None
              | i -> Some i
          doubleSoundFrequency = int doubleSoundFrequency
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
          isPoweredRideWithUnrestrictedGravity = hasFlag 0
          hasNoUpstopWheels = hasFlag 1
          hasNoUpstopWheelsBobsleigh = hasFlag 2
          isMiniGolf = hasFlag 3
          isReverserBogie = hasFlag 4
          isReverserPassengerCar = hasFlag 5
          hasInvertedSpriteSet = hasFlag 6
          hasDodgemInUseLights = hasFlag 7
          hasAdditionalColour2 = hasFlag 9
          recalculateSpriteBounds = hasFlag 10
          VEHICLE_ENTRY_FLAG_11 = hasFlag 11
          overrideNumberOfVerticalFrames = hasFlag 12
          spriteBoundsIncludeInvertedSet = hasFlag 13
          hasAdditionalSpinningFrames = hasFlag 14
          isLift = hasFlag 15
          hasAdditionalColour1 = hasFlag 16
          hasSwinging = hasFlag 17
          hasSpinning = hasFlag 18
          isPowered = hasFlag 19
          hasScreamingRiders = hasFlag 20
          useSuspendedSwing = hasFlag 21
          useBoatHireCollisionDetection = hasFlag 22
          hasVehicleAnimation = hasFlag 23
          hasRiderAnimation = hasFlag 24
          useWoodenWildMouseSwing = hasFlag 25
          useSlideSwing = hasFlag 27
          isChairlift = hasFlag 28
          isWaterRide = hasFlag 29
          isGoKart = hasFlag 30
          useDodgemCarPlacement = hasFlag 31
          numSegments =
              match loadingWaypoints with
              | Some loadingWaypoints ->
                  if Array.contains TrackTypes.Enterprise ride.Header.TrackTypeList then
                      Some 8
                  else
                      if int (Array.head loadingWaypoints) = 0 then
                          Some 0
                      else
                          Some 4
              | None -> None
          loadingPositions =
              match loadingPositions with
              | Some loadingPositions ->
                  loadingPositions
                  |> Array.map (sbyte >> int)
              | None -> null
          loadingWaypoints =
              match loadingWaypoints with
              | Some loadingWaypoints ->
                  loadingWaypoints
                  |> Array.skip 1
                  |> Array.map (sbyte >> int)
                  |> Array.chunkBySize 8
                  |> Array.map (Array.chunkBySize 2 >> Array.take 3)
              | None -> null }

    let getRide (ride: Attraction) =
        let getRideType = function
            | TrackTypes.AirPoweredVerticalCoaster -> "air_powered_vertical_rc"
            | TrackTypes.BoatHire -> "boat_hire"
            | TrackTypes.BobsledCoaster -> "bobsleigh_rc"
            | TrackTypes.Dodgems -> "dodgems"
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
            | TrackTypes.GigaCoaster -> "giga_rc"
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
            | TrackTypes.SwingingInvertedShip -> "swinging_inverter_ship"
            | TrackTypes.SwingingShip -> "swinging_ship"
            | TrackTypes.TopSpin -> "top_spin"
            | TrackTypes.Twist -> "twist"
            | TrackTypes.TwisterRollerCoaster -> "twister_rc"
            | TrackTypes.VerticalDropRollerCoaster -> "vertical_drop_rc"
            | TrackTypes.VirginiaReel -> "virginia_reel"
            | TrackTypes.WaterCoaster -> "water_coaster"
            | TrackTypes.DinghySlide -> "dinghy_slide"
            | TrackTypes.WildMouse -> "steel_wild_mouse"
            | TrackTypes.WoodenRollerCoaster -> "wooden_rc"
            | TrackTypes.WoodenWildRide -> "wooden_wild_mouse"
            | x -> x.ToString().ToLower()

        let getShopItem = function
        | ItemTypes.Burger -> "burger"
        | ItemTypes.Fries -> "chips"
        | ItemTypes.IceCream -> "ice_cream"
        | ItemTypes.CottonCandy -> "candyfloss"
        | ItemTypes.Pizza -> "pizza"
        | ItemTypes.Popcorn -> "popcorn"
        | ItemTypes.Hotdog -> "hot_dog"
        | ItemTypes.Seafood -> "tentacle"
        | ItemTypes.CandyApple -> "toffee_apple"
        | ItemTypes.Donut -> "doughnut"
        | ItemTypes.Chicken -> "chicken"
        | ItemTypes.Pretzel -> "pretzel"
        | ItemTypes.FunnelCake -> "funnel_cake"
        | ItemTypes.BeefNoodles -> "beef_noodles"
        | ItemTypes.FriedNoodles -> "fried_rice_noodles"
        | ItemTypes.WontonSoup -> "wonton_soup"
        | ItemTypes.MeatballSoup -> "meatball_soup"
        | ItemTypes.SubSandwich -> "sub_sandwich"
        | ItemTypes.Cookies -> "cookie"
        | ItemTypes.RoastSausage -> "roast_sausage"
        | ItemTypes.Cola -> "drink"
        | ItemTypes.Coffee -> "coffee"
        | ItemTypes.Lemonade -> "lemonade"
        | ItemTypes.HotChocolate -> "chocolate"
        | ItemTypes.IcedTea -> "iced_tea"
        | ItemTypes.FruitJuice -> "fruit_juice"
        | ItemTypes.SoybeanMilk -> "soybean_milk"
        | ItemTypes.Sujongkwa -> "sujeonggwa"
        | ItemTypes.Balloon -> "balloon"
        | ItemTypes.PlushToy -> "toy"
        | ItemTypes.Map -> "map"
        | ItemTypes.OnRidePhoto -> "photo"
        | ItemTypes.Umbrella -> "umbrella"
        | ItemTypes.Voucher -> "voucher"
        | ItemTypes.Hat -> "hat"
        | ItemTypes.TShirt -> "tshirt"
        | ItemTypes.Sunglasses -> "sunglasses"
        | _ -> "unknown"

        let rideTypes =
            let numTypes =
                let lastTypeIndex =
                    ride.Header.TrackTypeList
                    |> Seq.findIndexBack (fun x -> x <> TrackTypes.None)
                match lastTypeIndex with
                | -1 -> 0
                | x -> x + 1
            ride.Header.TrackTypeList
            |> Seq.take numTypes
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
                |> Seq.mapi (fun i c -> getCar ride c (Seq.tryItem i ride.RiderPositions))
                |> Seq.takeWhile (fun c -> c.effectVisual <> Some 0)
                |> Seq.toList

        let ratingMultiplier =
            match (int ride.Header.Excitement, int ride.Header.Intensity, int ride.Header.Nausea) with
            | (0, 0, 0) -> None
            | (e, i, n) -> Some { excitement = e; intensity = i; nausea = n }

        { ``type`` = rideTypes
          category =
              [| ride.Header.RideCategory; ride.Header.RideCategoryAlternate |]
              |> Seq.filterOut RideCategories.None
              |> Seq.map (fun x -> x.ToString().ToLower())
              |> Seq.toList
          sells =
              [| ride.Header.SoldItem1; ride.Header.SoldItem2 |]
              |> Seq.filterOut ItemTypes.None
              |> Seq.map getShopItem
              |> Seq.toList
          tabScale = if ride.Header.Flags.HasFlag(AttractionFlags.VehicleTabHalfScale) then 0.5f else 0.0f
          operatingModes = []
          hasShelter = ride.Header.Flags.HasFlag(AttractionFlags.Covered)
          disableBreakdown = ride.Header.Flags.HasFlag(AttractionFlags.CannotBreakDown)
          disablePainting = ride.Header.Flags.HasFlag(AttractionFlags.DisableColorTab)
          noInversions = ride.Header.Flags.HasFlag(AttractionFlags.NoInversions)
          noBanking = ride.Header.Flags.HasFlag(AttractionFlags.NoBankedTrack)
          limitAirTimeBonus = ride.Header.Flags.HasFlag(AttractionFlags.LimitAirtimeRatingBonus)
          playDepartSound = ride.Header.Flags.HasFlag(AttractionFlags.PlayTrainDepartSound)
          playSplashSound = ride.Header.Flags.HasFlag(AttractionFlags.PlaySplashSound)
          playSplashSoundSlide = ride.Header.Flags.HasFlag(AttractionFlags.PlaySplashSoundSlowInWater)
          swingMode =
              if ride.Header.Flags.HasFlag(AttractionFlags.AlternativeSwingMode1) then
                  if ride.Header.Flags.HasFlag(AttractionFlags.AlternativeSwingMode2) then 2
                  else 1
              else 0
          rotationMode =
              if ride.Header.Flags.HasFlag(AttractionFlags.AlternativeRotationMode1) then 1
              elif ride.Header.Flags.HasFlag(AttractionFlags.AlternativeRotationMode2) then 2
              else 0
          disallowWandering = ride.Header.Flags.HasFlag(AttractionFlags.UnknownBoatHireFlag)
          noDoorsOverTrack = ride.Header.Flags.HasFlag(AttractionFlags.DisableDoors)
          noCollisionCrashes = ride.Header.Flags.HasFlag(AttractionFlags.UnknownVehicleTrackMotionFlag)
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
          cars = cars }

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
          hasPrimaryColour = largeScenery.Header.Flags.HasFlag(LargeSceneryFlags.Remap1)
          hasSecondaryColour = largeScenery.Header.Flags.HasFlag(LargeSceneryFlags.Remap2)
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
          // Remap1 is always set if there is *any* remap colour. To suppress remap colour 1, the lowest bit of "Effects" can be set to 1.
          hasPrimaryColour = (wall.Header.Flags.HasFlag(WallFlags.Remap1) && ((int wall.Header.Effects) &&& (1 <<< 0)) = 0)
          hasSecondaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap2)
          hasTernaryColour = wall.Header.Flags.HasFlag(WallFlags.Remap3)
          hasGlass = wall.Header.Flags.HasFlag(WallFlags.Glass)
          isOpaque = ((int wall.Header.Effects) &&& (1 <<< 3)) <> 0
          isAllowedOnSlope = not (wall.Header.Flags.HasFlag(WallFlags.Flat))
          doorSound = (int wall.Header.Effects >>> 1) &&& 3
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
    let getSupportType (flags: FootpathFlags) =
        if flags.HasFlag(FootpathFlags.PoleSupports) then "pole" else "box"

    let getFootpath (footpath: Footpath) =
        { hasSupportImages = footpath.Header.Flags.HasFlag(FootpathFlags.PoleBase)
          hasElevatedPathImages = footpath.Header.Flags.HasFlag(FootpathFlags.OverlayPath)
          editorOnly = footpath.Header.Flags.HasFlag(FootpathFlags.Hidden)
          supportType = getSupportType footpath.Header.Flags
          scrollingMode = int footpath.Header.Reserved1 }

    let getFootpathSplit (splitChild: FootpathSplitChild) (footpath: Footpath) =
        match splitChild with
        | FootpathSurface ->
            { editorOnly = footpath.Header.Flags.HasFlag(FootpathFlags.Hidden)
              isQueue = false
              noSlopeRailings = false }
            :> obj
        | FootpathQueue ->
            { editorOnly = footpath.Header.Flags.HasFlag(FootpathFlags.Hidden)
              isQueue = true
              noSlopeRailings = false }
            :> obj
        | FootpathRailings ->
            { hasSupportImages = footpath.Header.Flags.HasFlag(FootpathFlags.PoleBase)
              hasElevatedPathImages = footpath.Header.Flags.HasFlag(FootpathFlags.OverlayPath)
              supportType = getSupportType footpath.Header.Flags
              scrollingMode = int footpath.Header.Reserved1
              colour = null }
            :> obj

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
          hasPrimaryColour = pb.Header.Flags.HasFlag(PathBannerFlags.Remap1)
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
        | ObjectTypes.Footpath -> getFootpath (obj :?> Footpath) :> obj
        | ObjectTypes.PathAddition -> getFootpathItem (obj :?> PathAddition) :> obj
        | ObjectTypes.PathBanner -> getFootpathBanner (obj :?> PathBanner) :> obj
        | ObjectTypes.SceneryGroup -> getSceneryGroup (obj :?> SceneryGroup) :> obj
        | ObjectTypes.ParkEntrance -> getParkEntrance (obj :?> ParkEntrance) :> obj
        | ObjectTypes.Water -> getWater (obj :?> Water) :> obj
        | _ -> new Object()
