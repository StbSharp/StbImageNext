using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class PsdDecodeTests
{
	private static readonly Assembly _assembly = typeof(PsdDecodeTests).Assembly;

	[Theory]
	[InlineData("Normal.psd", 128, 128, ColorComponents.RedGreenBlueAlpha)]
	public void Load(string filename, int width, int height, ColorComponents colorComponents)
	{
		ImageResult result = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
		}

		Assert.NotNull(result);
		Assert.Equal(width, result.Width);
		Assert.Equal(height, result.Height);
		Assert.Equal(ColorComponents.RedGreenBlueAlpha, result.Comp);
		Assert.Equal(colorComponents, result.SourceComp);
		Assert.NotNull(result.Data);
		Assert.Equal(result.Width * result.Height * 4, result.Data.Length);
	}
}
