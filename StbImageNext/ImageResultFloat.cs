using StbImageNext.Decoders;
using StbImageNext.Hebron.Runtime;
using System;
using System.IO;

namespace StbImageNext
{
	public class ImageResultFloat
	{
		private static float stbi__h2l_gamma_i = 1.0f / 2.2f;
		private static float stbi__h2l_scale_i = 1.0f;

		public int Width { get; set; }
		public int Height { get; set; }
		public ColorComponents SourceComp { get; set; }
		public ColorComponents Comp { get; set; }
		public float[] Data { get; set; }

		public ImageResult ToLdr()
		{
			var result = new ImageResult
			{
				Width = Width,
				Height = Height,
				SourceComp = SourceComp,
				Comp = Comp
			};

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
					var z = (float)CRuntime.pow(Data[i * comp + k] * stbi__h2l_scale_i, stbi__h2l_gamma_i) * 255 + 0.5f;
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

			result.Data = output.Data;

			return result;
		}

		public static unsafe ImageResultFloat FromStream(Stream stream, ColorComponents requiredComponents = ColorComponents.Default)
		{
			var context = new StbImage.stbi__context(stream);

			if (HdrImageDecoder.Test(context))
			{
				return HdrImageDecoder.Load(context, requiredComponents);
			}

			throw new NotSupportedException("Image format is not supported");
		}

		public static ImageResultFloat FromMemory(byte[] data, ColorComponents requiredComponents = ColorComponents.Default)
		{
			using (var stream = new MemoryStream(data))
			{
				return FromStream(stream, requiredComponents);
			}
		}
	}
}