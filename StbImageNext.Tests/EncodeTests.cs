using StbImageNext.Encoders;
using StbImageNext.Tests.Utility;
using System;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class EncodeTests
{
	private static readonly Assembly _assembly = typeof(EncodeTests).Assembly;

	private static void AssertEqual(ImageResult expected, ImageResult actual, int limit = 0)
	{
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Height, actual.Height);
		Assert.Equal(expected.Comp, actual.Comp);
		Assert.NotNull(actual.Data);
		Assert.Equal(expected.Data.Length, actual.Data.Length);
		for (var i = 0; i < expected.Data.Length; ++i)
		{
			var delta = Math.Abs(expected.Data[i] - actual.Data[i]);
			Assert.InRange(delta, 0, limit);
		}
	}

	private static void AssertEqual(ImageResultFloat expected, ImageResultFloat actual, float limit = 1e-6f)
	{
		Assert.Equal(expected.Width, actual.Width);
		Assert.Equal(expected.Height, actual.Height);
		Assert.Equal(expected.Comp, actual.Comp);
		Assert.NotNull(actual.Data);
		Assert.Equal(expected.Data.Length, actual.Data.Length);
		for (var i = 0; i < expected.Data.Length; ++i)
		{
			var delta = Math.Abs(expected.Data[i] - actual.Data[i]);
			Assert.InRange(delta, 0, limit);
		}
	}

	[Theory]
	[InlineData("AgedPavement_specular.png")]
	[InlineData("IDockable.png")]
	public void PngEncode(string filename)
	{
		ImageResult result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResult.FromStream(stream);
		}

		var encoded = PngEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp);
		Assert.NotNull(encoded);

		var result2 = ImageResult.FromMemory(encoded);

		AssertEqual(result1, result2);
	}

	[Theory]
	[InlineData("DockPanes.jpg")]
	public void JpgEncode(string filename)
	{
		ImageResult result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResult.FromStream(stream);
		}

		var encoded = JpgEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp, 95);
		Assert.NotNull(encoded);

		var result2 = ImageResult.FromMemory(encoded);

		AssertEqual(result1, result2, 20);

		// Jpg: 100
		encoded = JpgEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp, 100);
		Assert.NotNull(encoded);

		result2 = ImageResult.FromMemory(encoded);

		AssertEqual(result1, result2, 4);
	}

	[Theory]
	[InlineData("panel.bmp")]
	[InlineData("MonoGameLogo_64px.bmp")]
	public void BmpEncode(string filename)
	{
		ImageResult result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResult.FromStream(stream);
		}

		var encoded = BmpEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp);
		Assert.NotNull(encoded);

		var result2 = ImageResult.FromMemory(encoded);

		AssertEqual(result1, result2);
	}

	[Theory]
	[InlineData("head.tga")]
	public void TgaEncode(string filename)
	{
		ImageResult result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResult.FromStream(stream);
		}

		var encoded = TgaEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp);
		Assert.NotNull(encoded);

		var result2 = ImageResult.FromMemory(encoded);

		AssertEqual(result1, result2);
	}

	[Theory]
	[InlineData("sample_1280×853.hdr")]
	public void HdrEncode(string filename)
	{
		ImageResultFloat result1 = null;
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			result1 = ImageResultFloat.FromStream(stream);
		}

		var encoded = HdrEncoder.EncodeToByteArray(result1.Data, result1.Width, result1.Height, result1.Comp);
		Assert.NotNull(encoded);

		var result2 = ImageResultFloat.FromMemory(encoded);

		AssertEqual(result1, result2);
	}
}
