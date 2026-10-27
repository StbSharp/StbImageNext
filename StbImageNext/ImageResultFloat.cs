using System;
using System.IO;
using System.Runtime.InteropServices;
using StbImageNext.Hebron.Runtime;

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
			var output = new OutputBuffer<byte>(Width * Height * comp);

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

		internal static unsafe ImageResultFloat FromResult(float* result, int width, int height, ColorComponents comp,
			ColorComponents req_comp)
		{
			if (result == null)
				throw new InvalidOperationException(StbImage.stbi__g_failure_reason);

			var image = new ImageResultFloat
			{
				Width = width,
				Height = height,
				SourceComp = comp,
				Comp = req_comp == ColorComponents.Default ? comp : req_comp
			};

			// Convert to array
			image.Data = new float[width * height * (int)image.Comp];
			Marshal.Copy(new IntPtr(result), image.Data, 0, image.Data.Length);

			return image;
		}

		public static unsafe ImageResultFloat FromStream(Stream stream,
			ColorComponents requiredComponents = ColorComponents.Default)
		{
			float* result = null;

			try
			{
				int x, y, comp;

				var context = new StbImage.stbi__context(stream);

				result = StbImage.stbi__loadf_main(context, &x, &y, &comp, (int)requiredComponents);

				return FromResult(result, x, y, (ColorComponents)comp, requiredComponents);
			}
			finally
			{
				if (result != null)
					CRuntime.free(result);
			}
		}

		public static ImageResultFloat FromMemory(byte[] data,
			ColorComponents requiredComponents = ColorComponents.Default)
		{
			using (var stream = new MemoryStream(data))
			{
				return FromStream(stream, requiredComponents);
			}
		}
	}
}