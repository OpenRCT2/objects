// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

// Must be public so that the records can be serialised to JSON
module JsonTypes =

    open System.Collections.Generic
    open System.ComponentModel
    open System.Runtime.Serialization
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp

    type JObject =
        { id: string
          authors: string list
          version: string
          objectType: string
          properties: obj
          images: string list
          strings: IDictionary<string, IDictionary<string, string>> }

    [<DataContract>]
    type JCar =
        { [<DataMember(EmitDefaultValue = false)>]
          numSeats: int
          [<DataMember(EmitDefaultValue = false)>]
          numSeatRows: int
          [<DataMember(EmitDefaultValue = false)>]
          friction: int
          [<DataMember(EmitDefaultValue = false)>]
          spacing: int
          [<DataMember(EmitDefaultValue = false)>]
          tabOffset: int
          [<DataMember(EmitDefaultValue = false)>]
          spinningInertia: int
          [<DataMember(EmitDefaultValue = false)>]
          spinningFriction: int
          [<DataMember(EmitDefaultValue = false)>]
          poweredAcceleration: int
          [<DataMember(EmitDefaultValue = false)>]
          poweredMaxSpeed: int
          [<DataMember(EmitDefaultValue = false)>]
          carVisual: int
          [<DataMember(EmitDefaultValue = false)>]
          effectVisual: int
          [<DataMember(EmitDefaultValue = false)>]
          drawOrder: int
          [<DataMember(EmitDefaultValue = false)>]
          specialFrames: int
          [<DataMember(EmitDefaultValue = false)>]
          rotationFrameMask: int
          }

    [<DataContract>]
    type JRating =
        { [<DataMember(EmitDefaultValue = false)>]
          excitement: int
          [<DataMember(EmitDefaultValue = false)>]
          intensity: int
          [<DataMember(EmitDefaultValue = false)>]
          nausea: int }

    [<DataContract>]
    type JRide =
        { [<DataMember>]
          ``type``: string list
          [<DataMember(EmitDefaultValue = false)>]
          category: string list
          [<DataMember(EmitDefaultValue = false)>]
          sells: string list
          [<DataMember(EmitDefaultValue = false)>]
          tabScale: int
          [<DataMember(EmitDefaultValue = false)>]
          separateRide: bool
          [<DataMember(EmitDefaultValue = false)>]
          operatingModes: string list
          [<DataMember(EmitDefaultValue = false)>]
          hasShelter: bool
          [<DataMember(EmitDefaultValue = false)>]
          disableBreakdown: bool
          [<DataMember(EmitDefaultValue = false)>]
          disablePainting: bool
          [<DataMember(EmitDefaultValue = false)>]
          noInversions: bool
          [<DataMember(EmitDefaultValue = false)>]
          noBanking: bool
          [<DataMember(EmitDefaultValue = false)>]
          limitAirTimeBonus: bool
          [<DataMember(EmitDefaultValue = false)>]
          playDepartSound: bool
          [<DataMember(EmitDefaultValue = false)>]
          playSplashSound: bool
          [<DataMember(EmitDefaultValue = false)>]
          playSplashSoundSlide: bool
          [<DataMember(EmitDefaultValue = false)>]
          swingMode: int
          [<DataMember(EmitDefaultValue = false)>]
          rotationMode: int
          [<DataMember(EmitDefaultValue = false)>]
          [<DefaultValue(1)>]
          minCarsPerTrain: int
          [<DataMember(EmitDefaultValue = false)>]
          [<DefaultValue(1)>]
          maxCarsPerTrain: int
          [<DataMember(EmitDefaultValue = false)>]
          [<DefaultValue(255)>]
          carsPerFlatRide: int
          [<DataMember(EmitDefaultValue = false)>]
          numEmptyCars: int
          [<DataMember(EmitDefaultValue = false)>]
          tabCar: int
          [<DataMember(EmitDefaultValue = false)>]
          defaultCar: int
          [<DataMember(EmitDefaultValue = false)>]
          headCars: int list
          [<DataMember(EmitDefaultValue = false)>]
          tailCars: int list
          [<DataMember(EmitDefaultValue = false)>]
          ratingMultipler: JRating option
          [<DataMember(EmitDefaultValue = false)>]
          maxHeight: int
          [<DataMember(EmitDefaultValue = false)>]
          cars: JCar list
          [<DataMember(EmitDefaultValue = false)>]
          availableTrackPieces: string list option }

    [<DataContract>]
    type JWall =
        { [<DataMember(EmitDefaultValue = false)>]
          isDoor: bool
          [<DataMember(EmitDefaultValue = false)>]
          isBanner: bool
          [<DataMember(EmitDefaultValue = false)>]
          hasPrimaryColour: bool
          [<DataMember(EmitDefaultValue = false)>]
          hasSecondaryColour: bool
          [<DataMember(EmitDefaultValue = false)>]
          hasTenaryColour: bool
          [<DataMember(EmitDefaultValue = false)>]
          hasGlass: bool
          [<DataMember(EmitDefaultValue = false)>]
          isAllowedOnSlope: bool
          [<DataMember(EmitDefaultValue = false)>]
          doorSound: string
          [<DataMember>]
          height: int
          [<DataMember>]
          price: int
          [<DataMember>]
          cursor: string
          [<DataMember(EmitDefaultValue = false)>]
          scrollingMode: int }

    [<DataContract>]
    type JFootpathProperties =
        { [<DataMember(EmitDefaultValue = false)>]
          hasSupportImages: bool
          [<DataMember(EmitDefaultValue = false)>]
          hasElevatedPathImages: bool
          [<DataMember(EmitDefaultValue = false)>]
          editorOnly: bool
          [<DataMember>]
          supportType: string
          [<DataMember(EmitDefaultValue = false)>]
          scrollingMode: int }

    [<DataContract>]
    type JFootpathItemProperties =
        { [<DataMember(EmitDefaultValue = false)>]
          isBin: bool
          [<DataMember(EmitDefaultValue = false)>]
          isBench: bool
          [<DataMember(EmitDefaultValue = false)>]
          isLamp: bool
          [<DataMember(EmitDefaultValue = false)>]
          isTelevision: bool
          [<DataMember(EmitDefaultValue = false)>]
          isBreakable: bool
          [<DataMember(EmitDefaultValue = false)>]
          isJumpingFountainWater: bool
          [<DataMember(EmitDefaultValue = false)>]
          isJumpingFountainSnow: bool
          [<DataMember(EmitDefaultValue = false)>]
          isAllowedOnQueue: bool
          [<DataMember(EmitDefaultValue = false)>]
          isAllowedOnSlope: bool
          [<DataMember>]
          renderAs: string
          [<DataMember>]
          cursor: string
          [<DataMember>]
          price: int }

    type JSceneryGroupProperties =
        { entries: string list
          order: int
          entertainerCostumes: string list }

    type JParkEntrance =
        { scrollingMode: int
          textHeight: int }

    [<DataContract>]
    type JWater =
        { [<DataMember(EmitDefaultValue = false)>]
          allowDucks: bool }
