using StbImageNext.Decoders;
using System;
using System.IO;

namespace StbImageNext
{
	/// <summary>
	/// Represents a decoded HDR image together with its source and converted pixel formats.
	/// </summary>
	public class ImageResultFloat: BaseImageResult<float>
	{
		/// <summary>
		/// Decodes an HDR image from the specified stream.
		/// </summary>
		/// <param name="stream">Stream containing the HDR image data.</param>
		/// <param name="requiredComponents">Required pixel components of the image.</param>
		/// <returns>The decoded HDR image result.</returns>
		public static ImageResultFloat FromStream(Stream stream, ColorComponents? requiredComponents = null)
		{
			if (HdrImageDecoder.Test(stream))
			{
				return HdrImageDecoder.Load(stream, requiredComponents);
			}

			throw new NotSupportedException("Image format is not supported");
		}

		/// <summary>
		/// Decodes an HDR image from the specified byte array.
		/// </summary>
		/// <param name="data">Byte array containing the HDR image data.</param>
		/// <param name="requiredComponents">Required pixel components of the image.</param>
		/// <returns>The decoded HDR image result.</returns>
		public static ImageResultFloat FromMemory(byte[] data, ColorComponents? requiredComponents = null)
		{
			using (var stream = new MemoryStream(data))
			{
				return FromStream(stream, requiredComponents);
			}
		}

		public ImageResult ToLdr(float stbi__h2l_gamma_i = 1.0f / 2.2f, float stbi__h2l_scale_i = 1.0f)
		{
			var comp = (int)Comp;
			var output = new DataBuffer<byte>(Width * Height * comp);

			int n;
			if ((comp & 1) != 0)
			{
				n = comp;
			}
			else
			{
				n = comp - 1;
			}

			for (var i = 0; i < Width * Height; ++i)
			{
				var k = 0;
				for (k = 0; k < n; ++k)
				{
					var z = (float)Math.Pow(Data[i * comp + k] * stbi__h2l_scale_i, stbi__h2l_gamma_i) * 255 + 0.5f;
					if (z < 0)
						z = 0;
					if (z > 255)
						z = 255;
					output[i * comp + k] = (byte)(int)z;
				}

				if (k < comp)
				{
					var z = Data[i * comp + k] * 255 + 0.5f;
					if (z < 0)
						z = 0;
					if (z > 255)
						z = 255;
					output[i * comp + k] = (byte)(int)z;
				}
			}

			return new ImageResult
			{
				Width = Width,
				Height = Height,
				SourceComp = SourceComp,
				Comp = Comp,
				Data = output.Data
			};
		}
	}
}