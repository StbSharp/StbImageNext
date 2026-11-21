using StbImageNext.Tests.Utility;
using System;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class StreamSemanticsTests
{
	private static readonly Assembly _assembly = typeof(StreamSemanticsTests).Assembly;

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
}
