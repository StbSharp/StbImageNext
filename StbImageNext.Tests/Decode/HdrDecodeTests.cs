using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class HdrDecodeTests
{
	private static readonly Assembly _assembly = typeof(HdrDecodeTests).Assembly;

	[Theory]
	[InlineData("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
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

	[Theory]
	[InlineData("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
	public void LoadHdr(string filename, int width, int height, ColorComponents colorComponents)
	{
		ImageResultFloat result = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result = ImageResultFloat.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
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
