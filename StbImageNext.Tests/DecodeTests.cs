using StbImageNext.Tests.Utility;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Xunit;

namespace StbImageNext.Tests;

public class DecodeTests
{
	private static readonly Assembly _assembly = typeof(DecodeTests).Assembly;

	[Theory]
	[InlineData("The Public Domain_ Enclosing the Commons of the Mind.pdf")]
	[InlineData("empty")]
	public void LoadUnknownFormat(string filename)
	{
		Assert.Throws<NotSupportedException>(() =>
		{
			ImageResult result = null;
			using (var stream = _assembly.OpenResourceStream(filename))
			{
				result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
			}
		});
	}

	[Theory]
	[InlineData("panel.bmp", 164, 314, ColorComponents.RedGreenBlueAlpha)]
	[InlineData("AgedPavement_specular.png", 512, 512, ColorComponents.GreyAlpha)]
	[InlineData("Normal.psd", 128, 128, ColorComponents.RedGreenBlueAlpha)]
	[InlineData("MonoGameLogo_64px.bmp", 64, 64, ColorComponents.RedGreenBlue)]
	[InlineData("IDockable.png", 715, 426, ColorComponents.RedGreenBlueAlpha)]
	[InlineData("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
	[InlineData("DockPanes.jpg", 609, 406, ColorComponents.RedGreenBlue)]
	[InlineData("head.tga", 1024, 1024, ColorComponents.RedGreenBlue)]
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

	[Theory]
	[InlineData("panel.bmp", 164, 314, ColorComponents.RedGreenBlueAlpha)]
	[InlineData("AgedPavement_specular.png", 512, 512, ColorComponents.GreyAlpha)]
	[InlineData("Normal.psd", 128, 128, ColorComponents.RedGreenBlueAlpha)]
	[InlineData("MonoGameLogo_64px.bmp", 64, 64, ColorComponents.RedGreenBlue)]
	[InlineData("IDockable.png", 715, 426, ColorComponents.RedGreenBlueAlpha)]
	[InlineData("sample_1280×853.hdr", 1280, 853, ColorComponents.RedGreenBlue)]
	[InlineData("DockPanes.jpg", 609, 406, ColorComponents.RedGreenBlue)]
	[InlineData("head.tga", 1024, 1024, ColorComponents.RedGreenBlue)]
	[InlineData("PngSuite.basn6a16.png", 32, 32, ColorComponents.RedGreenBlueAlpha)]
	[InlineData("PngSuite.tbwn0g16.png", 32, 32, ColorComponents.GreyAlpha)]
	public void Info(string filename, int width, int height, ColorComponents colorComponents)
	{
		ImageInfo info;

		using (var stream = _assembly.OpenResourceStream(filename))
		{
			info = ImageInfo.FromStream(stream);
		}

		Assert.Equal(width, info.Width);
		Assert.Equal(height, info.Height);
		Assert.Equal(colorComponents, info.ColorComponents);
	}

	// Images from PngSuite (http://www.schaik.com/pngsuite/). The expected hashes are the first 16 hex
	// digits of the SHA-256 of the output of stb_image 2.30 and StbImageSharp 2.30.15, which agree.
	[Theory]
	[InlineData("basn0g02.png", null, ColorComponents.Grey, "c94bb4ae8f36ad2e")]
	[InlineData("basn0g02.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.Grey, "166bd68377b119b5")]
	[InlineData("basn0g04.png", null, ColorComponents.Grey, "c263f47ced16e00f")]
	[InlineData("basn0g04.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.Grey, "b05a4bc8e7079c8a")]
	[InlineData("basn3p02.png", null, ColorComponents.RedGreenBlue, "295fe76227f9704c")]
	[InlineData("basn3p02.png", ColorComponents.Grey, ColorComponents.RedGreenBlue, "ff6d4debdb57ac5a")]
	[InlineData("basn3p04.png", null, ColorComponents.RedGreenBlue, "93302575430e4e81")]
	[InlineData("basn3p04.png", ColorComponents.Grey, ColorComponents.RedGreenBlue, "c0430e76999488d1")]
	[InlineData("basn3p04.png", ColorComponents.GreyAlpha, ColorComponents.RedGreenBlue, "4191bcf8182febed")]
	[InlineData("basn3p04.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.RedGreenBlue, "a7abc212cf1a44c8")]
	[InlineData("basi3p04.png", null, ColorComponents.RedGreenBlue, "93302575430e4e81")]
	[InlineData("tbbn0g04.png", null, ColorComponents.GreyAlpha, "4d2c5f34535da31d")]
	[InlineData("tbbn0g04.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.GreyAlpha, "1c36e9d46fe44582")]
	[InlineData("basn0g16.png", null, ColorComponents.Grey, "a9a8592f67f55de0")]
	[InlineData("basn0g16.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.Grey, "5f42df4fd50dbea3")]
	[InlineData("basn2c16.png", null, ColorComponents.RedGreenBlue, "eb8706169d6bc8af")]
	[InlineData("basn2c16.png", ColorComponents.Grey, ColorComponents.RedGreenBlue, "f6b5aa53387a0e05")]
	[InlineData("basn2c16.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.RedGreenBlue, "7c4b73e829f02793")]
	[InlineData("basn4a16.png", null, ColorComponents.GreyAlpha, "5a640312d6eeaff6")]
	[InlineData("basn4a16.png", ColorComponents.Grey, ColorComponents.GreyAlpha, "bcdcfbb1c24e53ec")]
	[InlineData("basn4a16.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.GreyAlpha, "e96f0631f384c454")]
	[InlineData("basn6a16.png", null, ColorComponents.RedGreenBlueAlpha, "f6912d034804dc6b")]
	[InlineData("basn6a16.png", ColorComponents.Grey, ColorComponents.RedGreenBlueAlpha, "b1479f6ee37f4704")]
	[InlineData("basn6a16.png", ColorComponents.RedGreenBlue, ColorComponents.RedGreenBlueAlpha, "7c6a5793b16038fe")]
	[InlineData("basi0g16.png", null, ColorComponents.Grey, "a9a8592f67f55de0")]
	[InlineData("basi6a16.png", null, ColorComponents.RedGreenBlueAlpha, "f6912d034804dc6b")]
	[InlineData("tbbn2c16.png", null, ColorComponents.RedGreenBlueAlpha, "053eb9d28b7ac85c")]
	[InlineData("tbbn2c16.png", ColorComponents.GreyAlpha, ColorComponents.RedGreenBlueAlpha, "a258aa0c1b597464")]
	[InlineData("tbwn0g16.png", null, ColorComponents.GreyAlpha, "32f3b213f33c232b")]
	[InlineData("tbwn0g16.png", ColorComponents.RedGreenBlueAlpha, ColorComponents.GreyAlpha, "9b13bcf30183dec6")]
	[InlineData("oi9n2c16.png", null, ColorComponents.RedGreenBlue, "eb8706169d6bc8af")]
	public void LoadPngSuite(string fileName, ColorComponents? requiredComponents, ColorComponents sourceComponents, string expectedHash)
	{
		ImageResult result;
		using (var stream = _assembly.OpenResourceStream("PngSuite." + fileName))
		{
			result = ImageResult.FromStream(stream, requiredComponents);
		}

		var components = requiredComponents ?? sourceComponents;
		Assert.Equal(32, result.Width);
		Assert.Equal(32, result.Height);
		Assert.Equal(sourceComponents, result.SourceComp);
		Assert.Equal(components, result.Comp);
		Assert.Equal(32 * 32 * (int)components, result.Data.Length);

		var hash = Convert.ToHexString(SHA256.HashData(result.Data)).Substring(0, 16).ToLowerInvariant();
		Assert.Equal(expectedHash, hash);
	}

	[Theory]
	[InlineData("somersault.gif", 384, 480, ColorComponents.RedGreenBlueAlpha, 43)]
	public void AnimatedGifFrames(string fileName, int width, int height, ColorComponents colorComponents, int originalFrameCount)
	{
		using (var stream = _assembly.OpenResourceStream(fileName))
		{
			var frameCount = 0;
			foreach (var frame in ImageResult.AnimatedGifFramesFromStream(stream))
			{
				Assert.Equal(width, frame.Width);
				Assert.Equal(height, frame.Height);
				Assert.Equal(colorComponents, frame.Comp);
				Assert.NotNull(frame.Data);
				Assert.Equal(frame.Width * frame.Height * (int)frame.Comp, frame.Data.Length);

				++frameCount;
			}

			Assert.Equal(frameCount, originalFrameCount);

			stream.Seek(0, SeekOrigin.Begin);
		}
	}
}
