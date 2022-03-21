module ImageExporter

open System
open System.IO
open System.Runtime.Serialization
open RCT2ObjectData.Drawing
open RCT2ObjectData.Objects
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Formats.Png
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing.Processors.Quantization

let exportPalette = [|
    0uy; 0uy; 0uy; 0uy;
    1uy; 1uy; 1uy; 255uy;
    2uy; 2uy; 2uy; 255uy;
    3uy; 3uy; 3uy; 255uy;
    4uy; 4uy; 4uy; 255uy;
    5uy; 5uy; 5uy; 255uy;
    6uy; 6uy; 6uy; 255uy;
    7uy; 7uy; 7uy; 255uy;
    8uy; 8uy; 8uy; 255uy;
    9uy; 9uy; 9uy; 255uy;
    35uy; 35uy; 23uy; 255uy;
    51uy; 51uy; 35uy; 255uy;
    67uy; 67uy; 47uy; 255uy;
    83uy; 83uy; 63uy; 255uy;
    99uy; 99uy; 75uy; 255uy;
    115uy; 115uy; 91uy; 255uy;
    131uy; 131uy; 111uy; 255uy;
    151uy; 151uy; 131uy; 255uy;
    175uy; 175uy; 159uy; 255uy;
    195uy; 195uy; 183uy; 255uy;
    219uy; 219uy; 211uy; 255uy;
    243uy; 243uy; 239uy; 255uy;
    0uy; 47uy; 51uy; 255uy;
    0uy; 59uy; 63uy; 255uy;
    11uy; 75uy; 79uy; 255uy;
    19uy; 91uy; 91uy; 255uy;
    31uy; 107uy; 107uy; 255uy;
    47uy; 123uy; 119uy; 255uy;
    59uy; 139uy; 135uy; 255uy;
    79uy; 155uy; 151uy; 255uy;
    95uy; 175uy; 167uy; 255uy;
    115uy; 191uy; 187uy; 255uy;
    139uy; 207uy; 203uy; 255uy;
    163uy; 227uy; 223uy; 255uy;
    7uy; 43uy; 67uy; 255uy;
    11uy; 59uy; 87uy; 255uy;
    23uy; 75uy; 111uy; 255uy;
    31uy; 87uy; 127uy; 255uy;
    39uy; 99uy; 143uy; 255uy;
    51uy; 115uy; 159uy; 255uy;
    67uy; 131uy; 179uy; 255uy;
    87uy; 151uy; 191uy; 255uy;
    111uy; 175uy; 203uy; 255uy;
    135uy; 199uy; 219uy; 255uy;
    163uy; 219uy; 231uy; 255uy;
    195uy; 239uy; 247uy; 255uy;
    0uy; 27uy; 71uy; 255uy;
    0uy; 43uy; 95uy; 255uy;
    0uy; 63uy; 119uy; 255uy;
    7uy; 83uy; 143uy; 255uy;
    7uy; 111uy; 167uy; 255uy;
    15uy; 139uy; 191uy; 255uy;
    19uy; 167uy; 215uy; 255uy;
    27uy; 203uy; 243uy; 255uy;
    47uy; 231uy; 255uy; 255uy;
    95uy; 243uy; 255uy; 255uy;
    143uy; 251uy; 255uy; 255uy;
    195uy; 255uy; 255uy; 255uy;
    0uy; 0uy; 35uy; 255uy;
    0uy; 0uy; 79uy; 255uy;
    7uy; 7uy; 95uy; 255uy;
    15uy; 15uy; 111uy; 255uy;
    27uy; 27uy; 127uy; 255uy;
    39uy; 39uy; 143uy; 255uy;
    59uy; 59uy; 163uy; 255uy;
    79uy; 79uy; 179uy; 255uy;
    103uy; 103uy; 199uy; 255uy;
    127uy; 127uy; 215uy; 255uy;
    159uy; 159uy; 235uy; 255uy;
    191uy; 191uy; 255uy; 255uy;
    19uy; 51uy; 27uy; 255uy;
    23uy; 63uy; 35uy; 255uy;
    31uy; 79uy; 47uy; 255uy;
    39uy; 95uy; 59uy; 255uy;
    43uy; 111uy; 71uy; 255uy;
    51uy; 127uy; 87uy; 255uy;
    59uy; 143uy; 99uy; 255uy;
    67uy; 155uy; 115uy; 255uy;
    75uy; 171uy; 131uy; 255uy;
    83uy; 187uy; 147uy; 255uy;
    95uy; 203uy; 163uy; 255uy;
    103uy; 219uy; 183uy; 255uy;
    27uy; 55uy; 31uy; 255uy;
    35uy; 71uy; 47uy; 255uy;
    43uy; 83uy; 59uy; 255uy;
    55uy; 99uy; 75uy; 255uy;
    67uy; 111uy; 91uy; 255uy;
    79uy; 135uy; 111uy; 255uy;
    95uy; 159uy; 135uy; 255uy;
    111uy; 183uy; 159uy; 255uy;
    127uy; 207uy; 183uy; 255uy;
    147uy; 219uy; 195uy; 255uy;
    167uy; 231uy; 207uy; 255uy;
    191uy; 247uy; 223uy; 255uy;
    0uy; 63uy; 15uy; 255uy;
    0uy; 83uy; 19uy; 255uy;
    0uy; 103uy; 23uy; 255uy;
    0uy; 123uy; 31uy; 255uy;
    7uy; 143uy; 39uy; 255uy;
    23uy; 159uy; 55uy; 255uy;
    39uy; 175uy; 71uy; 255uy;
    63uy; 191uy; 91uy; 255uy;
    87uy; 207uy; 111uy; 255uy;
    115uy; 223uy; 139uy; 255uy;
    143uy; 239uy; 163uy; 255uy;
    179uy; 255uy; 195uy; 255uy;
    19uy; 43uy; 79uy; 255uy;
    27uy; 55uy; 99uy; 255uy;
    43uy; 71uy; 119uy; 255uy;
    59uy; 87uy; 139uy; 255uy;
    67uy; 99uy; 167uy; 255uy;
    83uy; 115uy; 187uy; 255uy;
    99uy; 131uy; 207uy; 255uy;
    115uy; 151uy; 215uy; 255uy;
    131uy; 171uy; 227uy; 255uy;
    151uy; 191uy; 239uy; 255uy;
    171uy; 207uy; 247uy; 255uy;
    195uy; 227uy; 255uy; 255uy;
    55uy; 19uy; 15uy; 255uy;
    87uy; 43uy; 39uy; 255uy;
    103uy; 55uy; 51uy; 255uy;
    119uy; 67uy; 63uy; 255uy;
    139uy; 83uy; 83uy; 255uy;
    155uy; 99uy; 99uy; 255uy;
    175uy; 119uy; 119uy; 255uy;
    191uy; 139uy; 139uy; 255uy;
    207uy; 159uy; 159uy; 255uy;
    223uy; 183uy; 183uy; 255uy;
    239uy; 211uy; 211uy; 255uy;
    255uy; 239uy; 239uy; 255uy;
    111uy; 27uy; 0uy; 255uy;
    151uy; 39uy; 0uy; 255uy;
    167uy; 51uy; 7uy; 255uy;
    187uy; 67uy; 15uy; 255uy;
    203uy; 83uy; 27uy; 255uy;
    223uy; 103uy; 43uy; 255uy;
    227uy; 135uy; 67uy; 255uy;
    231uy; 163uy; 91uy; 255uy;
    239uy; 187uy; 119uy; 255uy;
    243uy; 211uy; 143uy; 255uy;
    251uy; 231uy; 175uy; 255uy;
    255uy; 247uy; 215uy; 255uy;
    15uy; 43uy; 11uy; 255uy;
    23uy; 55uy; 15uy; 255uy;
    31uy; 71uy; 23uy; 255uy;
    43uy; 83uy; 35uy; 255uy;
    59uy; 99uy; 47uy; 255uy;
    75uy; 115uy; 59uy; 255uy;
    95uy; 135uy; 79uy; 255uy;
    119uy; 155uy; 99uy; 255uy;
    139uy; 175uy; 123uy; 255uy;
    167uy; 199uy; 147uy; 255uy;
    195uy; 219uy; 175uy; 255uy;
    223uy; 243uy; 207uy; 255uy;
    95uy; 0uy; 63uy; 255uy;
    115uy; 7uy; 75uy; 255uy;
    127uy; 15uy; 83uy; 255uy;
    143uy; 31uy; 95uy; 255uy;
    155uy; 43uy; 107uy; 255uy;
    171uy; 63uy; 123uy; 255uy;
    187uy; 83uy; 135uy; 255uy;
    199uy; 103uy; 155uy; 255uy;
    215uy; 127uy; 171uy; 255uy;
    231uy; 155uy; 191uy; 255uy;
    243uy; 195uy; 215uy; 255uy;
    255uy; 235uy; 243uy; 255uy;
    0uy; 0uy; 63uy; 255uy;
    0uy; 0uy; 87uy; 255uy;
    0uy; 0uy; 115uy; 255uy;
    0uy; 0uy; 143uy; 255uy;
    0uy; 0uy; 171uy; 255uy;
    0uy; 0uy; 199uy; 255uy;
    0uy; 7uy; 227uy; 255uy;
    0uy; 7uy; 255uy; 255uy;
    67uy; 79uy; 255uy; 255uy;
    115uy; 123uy; 255uy; 255uy;
    163uy; 171uy; 255uy; 255uy;
    215uy; 219uy; 255uy; 255uy;
    0uy; 39uy; 79uy; 255uy;
    0uy; 51uy; 111uy; 255uy;
    0uy; 63uy; 147uy; 255uy;
    0uy; 71uy; 183uy; 255uy;
    0uy; 79uy; 219uy; 255uy;
    0uy; 83uy; 255uy; 255uy;
    23uy; 111uy; 255uy; 255uy;
    51uy; 139uy; 255uy; 255uy;
    79uy; 163uy; 255uy; 255uy;
    107uy; 183uy; 255uy; 255uy;
    135uy; 203uy; 255uy; 255uy;
    163uy; 219uy; 255uy; 255uy;
    47uy; 51uy; 0uy; 255uy;
    55uy; 63uy; 0uy; 255uy;
    67uy; 75uy; 0uy; 255uy;
    79uy; 87uy; 0uy; 255uy;
    99uy; 107uy; 7uy; 255uy;
    119uy; 127uy; 23uy; 255uy;
    143uy; 147uy; 43uy; 255uy;
    163uy; 167uy; 71uy; 255uy;
    187uy; 187uy; 99uy; 255uy;
    207uy; 207uy; 131uy; 255uy;
    231uy; 231uy; 171uy; 255uy;
    255uy; 255uy; 207uy; 255uy;
    27uy; 0uy; 63uy; 255uy;
    51uy; 0uy; 103uy; 255uy;
    63uy; 11uy; 123uy; 255uy;
    79uy; 23uy; 143uy; 255uy;
    95uy; 31uy; 163uy; 255uy;
    111uy; 39uy; 183uy; 255uy;
    143uy; 59uy; 219uy; 255uy;
    171uy; 91uy; 239uy; 255uy;
    187uy; 119uy; 243uy; 255uy;
    203uy; 151uy; 247uy; 255uy;
    223uy; 183uy; 251uy; 255uy;
    239uy; 215uy; 255uy; 255uy;
    0uy; 19uy; 39uy; 255uy;
    7uy; 31uy; 55uy; 255uy;
    15uy; 47uy; 71uy; 255uy;
    31uy; 63uy; 91uy; 255uy;
    51uy; 83uy; 107uy; 255uy;
    75uy; 103uy; 123uy; 255uy;
    107uy; 127uy; 143uy; 255uy;
    127uy; 147uy; 163uy; 255uy;
    147uy; 171uy; 187uy; 255uy;
    171uy; 195uy; 207uy; 255uy;
    195uy; 219uy; 231uy; 255uy;
    223uy; 243uy; 255uy; 255uy;
    75uy; 75uy; 55uy; 255uy;
    0uy; 183uy; 255uy; 255uy;
    0uy; 219uy; 255uy; 255uy;
    0uy; 255uy; 255uy; 255uy;
    99uy; 107uy; 7uy; 255uy;
    99uy; 107uy; 7uy; 255uy;
    135uy; 143uy; 39uy; 255uy;
    123uy; 131uy; 27uy; 255uy;
    99uy; 107uy; 7uy; 255uy;
    151uy; 155uy; 55uy; 255uy;
    151uy; 155uy; 55uy; 255uy;
    227uy; 227uy; 155uy; 255uy;
    203uy; 203uy; 115uy; 255uy;
    151uy; 155uy; 55uy; 255uy;
    91uy; 91uy; 67uy; 255uy;
    107uy; 107uy; 83uy; 255uy;
    123uy; 123uy; 99uy; 255uy;
    47uy; 51uy; 111uy; 255uy;
    47uy; 55uy; 131uy; 255uy;
    51uy; 63uy; 151uy; 255uy;
    51uy; 67uy; 171uy; 255uy;
    47uy; 75uy; 191uy; 255uy;
    43uy; 79uy; 211uy; 255uy;
    35uy; 87uy; 231uy; 255uy;
    31uy; 95uy; 255uy; 255uy;
    39uy; 127uy; 255uy; 255uy;
    51uy; 155uy; 255uy; 255uy;
    63uy; 183uy; 255uy; 255uy;
    75uy; 207uy; 255uy; 255uy;
    255uy; 255uy; 255uy; 255uy |]

let private getPaletteColour (index: byte): Rgba32 =
    let offset = int index * 4
    let r = uint exportPalette.[offset]
    let g = uint exportPalette.[offset + 1]
    let b = uint exportPalette.[offset + 2]
    let a = uint exportPalette.[offset + 3]
    Rgba32 ((a <<< 24) ||| (r <<< 16) ||| (g <<< 8) ||| (b <<< 0))

let private drawSprite (sprite: PaletteImage) (sx: int) (sy: int) (image: Image<Rgba32>) =
    let src = sprite.Pixels
    for y in 0..sprite.Height - 1 do
        let dst = image.GetPixelRowSpan(sy + y)
        for x in 0..sprite.Width - 1 do
            let paletteIndex = src.[x, y]
            dst.[sx + x] <- getPaletteColour paletteIndex
    image

let private palette: ReadOnlyMemory<Color> =
    let colors =
        [|0uy..255uy|]
        |> Array.map (getPaletteColour >> Color)
    new ReadOnlyMemory<Color>(colors)

let private saveImage (path: string) (image: Image<Rgba32>) =
    use fs = new FileStream(path, FileMode.Create)
    let quantizer = new PaletteQuantizer(palette)
    let encoder = new PngEncoder(Quantizer = quantizer, ColorType = PngColorType.Palette)
    image.Save(fs, encoder)

[<DataContract>]
type AtlasPlacement =
    { index: int
      [<DataMember>]
      path: string
      [<DataMember>]
      srcX: int
      [<DataMember>]
      srcY: int
      [<DataMember>]
      srcWidth: int
      [<DataMember>]
      srcHeight: int
      [<DataMember>]
      x: int
      [<DataMember>]
      y: int }

module AtlasPlacement =
    let getRight p = p.srcX + p.srcWidth
    let getBottom p = p.srcY + p.srcHeight

let getImagePlacements path ids (getSprite: int -> PaletteImage option) =
    let rec getImagePlacements ids x y lineHeight maxWidth placements =
        match ids with
        | id :: ids ->
            match getSprite id with
            | Some sprite ->
                let (x, y, lineHeight) =
                    if x + sprite.Width >= 1024 then
                        (0, y + lineHeight, 0)
                    else
                        (x, y, lineHeight)

                let p =
                    { index = id
                      path = path
                      srcX = x
                      srcY = y
                      srcWidth = sprite.Width
                      srcHeight = sprite.Height
                      x = sprite.XOffset
                      y = sprite.YOffset }
                let x = x + sprite.Width
                let lineHeight = max lineHeight sprite.Height
                let maxWidth = max maxWidth x
                let placements = p :: placements
                getImagePlacements ids x y lineHeight maxWidth placements
            | None ->
                let p =
                    { index = id
                      path = path
                      srcX = x
                      srcY = y
                      srcWidth = 0
                      srcHeight = 0
                      x = 0
                      y = 0 }
                let placements = p :: placements
                getImagePlacements ids x y lineHeight maxWidth placements
        | [] ->
            placements

    getImagePlacements ids 0 0 0 0 []
    |> List.rev

let exportImages ids basePath println (obj: ObjectData) =
    let getSprite i =
        if i >= 0 && i < obj.GraphicsData.NumImages then
            Some (obj.GraphicsData.GetPaletteImage(i))
        else
            None
    let filename = "images.png"
    let path = Path.Combine(basePath, filename)
    let placements = getImagePlacements filename ids getSprite

    sprintf "Exporting %s..." "images.png"
    |> println

    let imageWidth =
        placements
        |> List.maxBy AtlasPlacement.getRight
        |> AtlasPlacement.getRight
    let imageHeight =
        placements
        |> List.maxBy AtlasPlacement.getBottom
        |> AtlasPlacement.getBottom
    let image = new Image<Rgba32>(imageWidth, imageHeight)
    for p in placements do
        match getSprite p.index with
        | Some sprite ->
            image |> drawSprite sprite p.srcX p.srcY |> ignore
        | None ->
            ()
    image |> saveImage path

    placements
