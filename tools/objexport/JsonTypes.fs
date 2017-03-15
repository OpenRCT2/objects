// objexport
// Exports objects from RCT2 to OpenRCT2 json files

namespace OpenRCT2.Legacy.ObjectExporter

// Must be public so that the records can be serialised to JSON
module JsonTypes =

    open System.Collections.Generic

    type JObject =
        { id: string
          authors: string list
          version: string
          objectType: string
          properties: obj
          images: string list
          strings: IDictionary<string, IDictionary<string, string>> }

    type JSceneryGroupProperties =
        { entries: string list
          order: int
          entertainerCostumes: string list }
