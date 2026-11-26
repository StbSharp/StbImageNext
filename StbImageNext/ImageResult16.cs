using StbImageNext.Decoders;
using System.IO;

namespace StbImageNext
{
	/// <summary>
	/// Represents a decoded image with 16 bits per channel, together with its source and converted pixel formats.
	/// </summary>
	public class ImageResult16 : BaseImageResult<ushort>
	{
		/// <summary>
		/// Decodes an image from the specified stream, with 16 bits per channel.
		/// </summary>
		/// <remarks>
		/// Like stbi_load_16 of stb_image: a PNG image with 16 bits per channel keeps them, and any other image is
		/// decoded at 8 bits, then each sample is widened to 16 bits (0 to 0, 255 to 65535).
		/// <see cref="ImageInfo.Is16Bit"/> tells beforehand whether the image has 16 bits per channel.
		/// </remarks>
		/// <param name="stream">Stream containing the image data.</param>
		/// <param name="requiredComponents">Required pixel components of the image.</param>
		/// <returns>The decoded image result.</returns>
		public static ImageResult16 FromStream(Stream stream, ColorComponents? requiredComponents = null)
		{
			if (PngDecoder.Test(stream))
			{
				return PngDecoder.Decode16(stream, requiredComponents);
			}

			var result = ImageResult.FromStream(stream, requiredComponents);
			return new ImageResult16
			{
				Width = result.Width,
				Height = result.Height,
				SourceComp = result.SourceComp,
				Comp = result.Comp,
				Data = Utility.stbi__convert_8_to_16(result.Data)
			};
		}

		/// <summary>
		/// Decodes an image from the specified byte array, with 16 bits per channel.
		/// </summary>
		/// <remarks>See <see cref="FromStream"/>.</remarks>
		/// <param name="data">Byte array containing the image data.</param>
		/// <param name="requiredComponents">Required pixel components of the image.</param>
		/// <returns>The decoded image result.</returns>
		public static ImageResult16 FromMemory(byte[] data, ColorComponents? requiredComponents = null)
		{
			using (var stream = new MemoryStream(data))
			{
				return FromStream(stream, requiredComponents);
			}
		}
	}
}
