using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class ImageInfoTests
{
	private static readonly Assembly _assembly = typeof(ImageInfoTests).Assembly;

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

	[Theory]
	[InlineData("panel.bmp", false)]
	[InlineData("AgedPavement_specular.png", false)]
	[InlineData("Normal.psd", false)]
	[InlineData("sample_1280×853.hdr", false)]
	[InlineData("DockPanes.jpg", false)]
	[InlineData("head.tga", false)]
	[InlineData("somersault.gif", false)]
	[InlineData("PngSuite.basn0g04.png", false)]
	[InlineData("PngSuite.basn3p04.png", false)]
	[InlineData("PngSuite.basn0g16.png", true)]
	[InlineData("PngSuite.basn6a16.png", true)]
	[InlineData("PngSuite.basi0g16.png", true)]
	[InlineData("PngSuite.tbwn0g16.png", true)]
	public void Is16Bit(string filename, bool expected)
	{
		using (var stream = _assembly.OpenResourceStream(filename))
		{
			Assert.Equal(expected, ImageInfo.Is16Bit(stream));

			// The stream is rewound, so the image can be decoded next
			Assert.Equal(0, stream.Position);
		}
	}
}
