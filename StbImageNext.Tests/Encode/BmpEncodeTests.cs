using StbImageNext.Encoders;
using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class BmpEncodeTests
{
	private static readonly Assembly _assembly = typeof(BmpEncodeTests).Assembly;

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

		AssertEx.ResultsEqual(result1, result2);
	}
}
