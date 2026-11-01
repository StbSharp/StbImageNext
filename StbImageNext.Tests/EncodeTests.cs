using StbImageNext.Encoders;
using StbImageNext.Tests.Utility;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class EncodeTests
{
	private static readonly Assembly _assembly = typeof(EncodeTests).Assembly;


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

		var encoded = PngEncoder.Encode(result1.Data, result1.Width, result1.Height, result1.Comp);
		Assert.NotNull(encoded);

		var result2 = ImageResult.FromMemory(encoded);

		Assert.NotNull(result2);
		Assert.Equal(result1.Width, result2.Width);
		Assert.Equal(result1.Height, result2.Height);
		Assert.Equal(result1.Comp, result2.Comp);
		Assert.NotNull(result2.Data);
		Assert.Equal(result1.Data.Length, result2.Data.Length);
		for(var i = 0; i < result1.Data.Length; ++i)
		{
			Assert.Equal(result1.Data[i], result2.Data[i]);
		}
	}
}
