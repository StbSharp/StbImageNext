using StbImageNext.Tests.Utility;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace StbImageNext.Tests;

public class AnimatedGifTests
{
	private static readonly Assembly _assembly = typeof(AnimatedGifTests).Assembly;

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
