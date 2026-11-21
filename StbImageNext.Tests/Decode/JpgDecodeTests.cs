using StbImageNext.Tests.Utility;
using System;
using System.Reflection;
using System.Security.Cryptography;
using Xunit;

namespace StbImageNext.Tests;

public class JpgDecodeTests
{
	private static readonly Assembly _assembly = typeof(JpgDecodeTests).Assembly;

	[Theory]
	[InlineData("DockPanes.jpg", 609, 406, ColorComponents.RedGreenBlue)]
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
	[InlineData(null)]
	[InlineData(ColorComponents.Grey)]
	[InlineData(ColorComponents.GreyAlpha)]
	[InlineData(ColorComponents.RedGreenBlue)]
	[InlineData(ColorComponents.RedGreenBlueAlpha)]
	public void LoadJpgHasExactSize(ColorComponents? requiredComponents)
	{
		ImageResult result;
		using (var stream = _assembly.OpenResourceStream("DockPanes.jpg"))
		{
			result = ImageResult.FromStream(stream, requiredComponents);
		}

		Assert.Equal(result.Width * result.Height * (int)result.Comp, result.Data.Length);
	}

	// The expected hashes are the first 16 hex digits of the SHA-256 of the output of stb_image 2.30
	// and StbImageSharp 2.30.15, which agree. DockPanes.jpg is 4:4:4, synthetic_420.jpg 4:2:0.
	[Theory]
	[InlineData("DockPanes.jpg", null, "9532daac86b63f3c")]
	[InlineData("DockPanes.jpg", ColorComponents.Grey, "46759c9f708843a7")]
	[InlineData("DockPanes.jpg", ColorComponents.RedGreenBlueAlpha, "89d644643f2fb3ce")]
	[InlineData("synthetic_420.jpg", null, "e9a5404aa03c598e")]
	[InlineData("synthetic_420.jpg", ColorComponents.Grey, "dde8e7012031107c")]
	[InlineData("synthetic_420.jpg", ColorComponents.GreyAlpha, "2987a5034ed88eeb")]
	[InlineData("synthetic_420.jpg", ColorComponents.RedGreenBlueAlpha, "02e761e5a3a61e37")]
	public void LoadJpgMatchesStbImage(string fileName, ColorComponents? requiredComponents, string expectedHash)
	{
		ImageResult result;
		using (var stream = _assembly.OpenResourceStream(fileName))
		{
			result = ImageResult.FromStream(stream, requiredComponents);
		}

		var hash = Convert.ToHexString(SHA256.HashData(result.Data)).Substring(0, 16).ToLowerInvariant();
		Assert.Equal(expectedHash, hash);
	}

	// Progressive JPEGs that stb_image 2.30 and 2.32 decode differently (2.31 stopped decoding blocks
	// once the data runs out). The expected hashes are the first 16 hex digits of the SHA-256 of the
	// RGBA output of stb_image 2.32.
	[Theory]
	[InlineData("synth_17x9_prog_420.jpg", "21ad62d5e85615b6")]
	[InlineData("synth_33x17_prog_gray.jpg", "8b1828bdffc81306")]
	public void LoadProgressiveJpg(string fileName, string expectedHash)
	{
		ImageResult result;
		using (var stream = _assembly.OpenResourceStream(fileName))
		{
			result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
		}

		var hash = Convert.ToHexString(SHA256.HashData(result.Data)).Substring(0, 16).ToLowerInvariant();
		Assert.Equal(expectedHash, hash);
	}
}
