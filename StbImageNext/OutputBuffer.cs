using System;

namespace StbImageNext
{
	internal struct OutputBuffer<T>
	{
		private T[] _data;
		private long _offset;

		public T[] Data => _data;

		public T this[long index]
		{
			get => _data[index + _offset];
			set => _data[index + _offset] = value;
		}

		public OutputBuffer(long size)
		{
			_data = new T[size];
			_offset = 0;
		}

		private OutputBuffer(T[] data, long offset)
		{
			_data = data;
			_offset = offset;
		}

		public OutputBuffer<T> SubBuffer(long offset)
		{
			return new OutputBuffer<T>(_data, _offset + offset);
		}

		public void Write(T value)
		{
			_data[_offset] = value;
			_offset++;
		}

		public void Set(T value, long size)
		{
			for (long i = 0; i < size; i++)
			{
				_data[_offset + i] = value;
			}
		}

		public void Copy(OutputBuffer<T> source, long size)
		{
			Array.Copy(source._data, source._offset, _data, _offset, size);
		}

		public void Copy(T[] source, long size)
		{
			Array.Copy(source, 0, _data, _offset, size);
		}

		public OutputBuffer<T> Realloc(long newSize)
		{
			var newBuffer = new OutputBuffer<T>(newSize);
			Array.Copy(_data, 0, newBuffer._data, 0, Math.Min(_data.Length, newSize));
			return newBuffer;
		}

		public static OutputBuffer<T> operator +(OutputBuffer<T> buffer, long offset)
		{
			return new OutputBuffer<T>(buffer._data, buffer._offset + offset);
		}
	}
}
