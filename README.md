# StbImageNext
[![Nuget](https://img.shields.io/nuget/dt/StbImageNext)](https://www.nuget.org/packages/StbImageNext/)
[![Build & Publish Beta](https://github.com/StbSharp/StbImageNext/actions/workflows/build-and-publish-beta.yml/badge.svg)](https://github.com/StbSharp/StbImageNext/actions/workflows/build-and-publish-beta.yml)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbImageNext is safe fork of [StbImageSharp](https://github.com/StbSharp/StbImageSharp)

In other words, it's a cross-platform .NET library for decoding images.

## Supported Image Formats

| Format | Decode | Notes |
|--------|--------|-------|
| PNG    | Yes    |       |
| JPEG / JPG | Yes |       |
| BMP    | Yes    |       |
| GIF    | Yes    | Including animated frames |
| TGA    | Yes    |       |
| PSD    | Yes    |       |
| HDR    | Yes    | Radiance format, floating-point |


## Installation

https://www.nuget.org/packages/StbImageNext/

## Basic Usage

Add the namespace at the top of your file:

```csharp
using StbImageNext;
using System.IO;
```

### Load an image from a file stream

```csharp
using var stream = File.OpenRead("photo.jpg");
using var image = ImageResult.FromStream(stream);

// image.Width   -> pixel width
// image.Height  -> pixel height
// image.Data    -> decoded pixel data (byte array)
// image.Comp    -> color components of the decoded data
// image.SourceComp -> color components of the source file
```

### Load an image from a byte array

```csharp
byte[] imageData = File.ReadAllBytes("photo.png");
using var image = ImageResult.FromMemory(imageData);
```

### Request a specific pixel format

You can request that the decoded image be converted to a specific component layout:

```csharp
using var stream = File.OpenRead("photo.jpg");
using var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
```

Available `ColorComponents` values:
- `Grey` (1)
- `GreyAlpha` (2)
- `RedGreenBlue` (3)
- `RedGreenBlueAlpha` (4)

If `requiredComponents` is omitted or `null`, the image is decoded in its native format.

### Get image info without decoding the full pixel data

If you only need dimensions and format (e.g. to validate an upload), use `ImageInfo`:

```csharp
using var stream = File.OpenRead("photo.jpg");
var info = ImageInfo.FromStream(stream);

Console.WriteLine($"{info.Width}x{info.Height}, {info.ColorComponents}");
```

## Image Manipulation

### Vertical flip

Flip the decoded image vertically (top-to-bottom) in place:

```csharp
image.VerticalFlip();
```

### Convert LDR (8-bit) to HDR (float)

Convert a standard 8-bit-per-channel image to floating-point HDR:

```csharp
using var stream = File.OpenRead("photo.png");
using var ldr = ImageResult.FromStream(stream);

var hdr = ldr.ToHdr(gamma: 2.2f, scale: 1.0f);
// hdr.Data contains float pixel values
```

### Convert HDR (float) to LDR (8-bit)

Convert a high-dynamic-range floating-point image back to 8-bit:

```csharp
using var stream = File.OpenRead("image.hdr");
using var hdr = ImageResultFloat.FromStream(stream);

var ldr = hdr.ToLdr(gamma_i: 1.0f / 2.2f, scale_i: 1.0f);
// ldr.Data contains 8-bit pixel values
```

## Animated GIFs

Enumerate the frames of an animated GIF:

```csharp
using var stream = File.OpenRead("animation.gif");
foreach (var frame in ImageResult.AnimatedGifFramesFromStream(stream))
{
    Console.WriteLine($"Frame: {frame.Width}x{frame.Height}, delay: {frame.DelayInMs}ms");
    // frame.Data contains the decoded pixels for this frame
}
```

You can also request a specific component format for animated frames:

```csharp
foreach (var frame in ImageResult.AnimatedGifFramesFromStream(stream, ColorComponents.RedGreenBlue))
{
    // ...
}
```

## HDR Images

Decode an HDR (Radiance `.hdr`) image directly as floating-point data:

```csharp
using var stream = File.OpenRead("scene.hdr");
using var hdr = ImageResultFloat.FromStream(stream);
```

## Using with MonoGame

You can load an image with StbImageNext and upload it into a MonoGame `Texture2D` for use in a game.

### Simple approach (recommended): Request RGBA and upload directly

When you request `ColorComponents.RedGreenBlueAlpha`, the decoded pixel data is guaranteed to be 4 bytes per pixel (RGBA), matching MonoGame's `Color` struct exactly. You can upload the byte array directly without any conversion:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StbImageNext;
using System.IO;

// Load the image as RGBA (4 bytes per pixel)
using var stream = File.OpenRead("photo.png");
using var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

// Create the MonoGame texture
var texture = new Texture2D(GraphicsDevice, image.Width, image.Height);

// Upload directly - image.Data is byte[] with RGBA layout matching Color
texture.SetData(image.Data);

// Use the texture in your game
// _spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, Color.White);
```

## License

Public Domain or MIT

## Support
[Discord](https://discord.gg/ZeHxhCY)

## Building From Source Code
1. Clone this repo.
2. Open StbImageNext.sln.

## Sponsor
If this project is useful for you, you can support development:
- Boosty: https://boosty.to/rds1983
- Telegram Wallet: https://t.me/rds1983

### Crypto
USDT (TON): `UQCQy6tFInPvqinE44zHY4R0rYS3niaBikkqiSyGmyoAMwyO`

TON: `UQCQy6tFInPvqinE44zHY4R0rYS3niaBikkqiSyGmyoAMwyO`

# Credits
* [stb](https://github.com/nothings/stb)
