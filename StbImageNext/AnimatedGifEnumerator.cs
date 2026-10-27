using StbImageNext.Decoders;
using StbImageNext.Hebron.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static StbImageNext.Decoders.GifImageDecoder;

namespace StbImageNext
{
	internal class AnimatedGifEnumerator : IEnumerator<AnimatedFrameResult>
	{
		private readonly StbImage.stbi__context _context;
		private stbi__gif _gif;
		private readonly ColorComponents _colorComponents;

		public AnimatedGifEnumerator(Stream input, ColorComponents colorComponents)
		{
			if (input == null) throw new ArgumentNullException("input");

			_context = new StbImage.stbi__context(input);

			if (!Test(_context))
			{
				throw new Exception("Input stream is not GIF file.");
			}

			_gif = new stbi__gif();
			_colorComponents = colorComponents;
		}

		public ColorComponents ColorComponents
		{
			get
			{
				return _colorComponents;
			}
		}

		public AnimatedFrameResult Current { get; private set; }

		object IEnumerator.Current
		{
			get
			{
				return Current;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public unsafe bool MoveNext()
		{
			// Read next frame
			byte two_back;
			var result = stbi__gif_load_next(_context, _gif, (int)ColorComponents, &two_back);
			if (result == null) return false;

			if (Current == null)
			{
				Current = new AnimatedFrameResult
				{
					Width = _gif.w,
					Height = _gif.h,
					SourceComp = _gif.colorComponents,
					Comp = ColorComponents == ColorComponents.Default ? _gif.colorComponents : ColorComponents
				};

			}

			Current.DelayInMs = _gif.delay;

			Current.Data = result.Data;

			return true;
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}

		~AnimatedGifEnumerator()
		{
			Dispose(false);
		}

		protected unsafe virtual void Dispose(bool disposing)
		{
			if (_gif != null)
			{
				if (_gif.history != null)
				{
					CRuntime.free(_gif.history);
					_gif.history = null;
				}

				if (_gif.background != null)
				{
					CRuntime.free(_gif.background);
					_gif.background = null;
				}

				_gif = null;
			}
		}
	}

	internal class AnimatedGifEnumerable : IEnumerable<AnimatedFrameResult>
	{
		private readonly Stream _input;
		private readonly ColorComponents _colorComponents;

		public AnimatedGifEnumerable(Stream input, ColorComponents colorComponents)
		{
			_input = input;
			_colorComponents = colorComponents;
		}

		public ColorComponents ColorComponents
		{
			get
			{
				return _colorComponents;
			}
		}

		public IEnumerator<AnimatedFrameResult> GetEnumerator()
		{
			return new AnimatedGifEnumerator(_input, ColorComponents);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}