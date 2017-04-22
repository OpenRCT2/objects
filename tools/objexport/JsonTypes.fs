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

    type JCar =
        { [<DataMember(EmitDefaultValue = false)>]
          numSeats: int
          [<DataMember(EmitDefaultValue = false)>]
          friction: int }

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
          availableTrackPieces: string list option
          [<DataMember(EmitDefaultValue = false)>]
          cars: JCar list }

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
