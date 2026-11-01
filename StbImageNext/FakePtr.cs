using System;

namespace StbImageNext
{
	internal struct FakePtr<T> where T : unmanaged
	{
		public static readonly FakePtr<T> Null = new FakePtr<T>();

		private T[] _data;
		private long _offset;

		public bool IsNull => _data == null;

		public T[] Data => _data;

		public long Offset
		{
			get => _offset;
			set => _offset = value;
		}

		public T this[long index]
		{
			get => _data[index + _offset];
			set => _data[index + _offset] = value;
		}

		public T FirstValue
		{
			get => _data[_offset];
			set => _data[_offset] = value;
		}

		public int Length => _data.Length;

		public FakePtr(T[] data, long offset = 0)
		{
			_data = data;
			_offset = offset;
		}

		public FakePtr(long size)
		{
			_data = new T[size];
			_offset = 0;
		}

		public FakePtr<T> SubBuffer(long offset)
		{
			return new FakePtr<T>(_data, _offset + offset);
		}

		public void Write(T value)
		{
			_data[_offset] = value;
			++_offset;
		}

		public void Write(T[] value)
		{
			Array.Copy(value, 0, _data, _offset, value.Length);
			_offset += value.Length;
		}

		public T Read()
		{
			var result = _data[_offset];
			++_offset;

			return result;
		}

		public void Set(T value, long size)
		{
			for (long i = 0; i < size; i++)
			{
				_data[_offset + i] = value;
			}
		}

		public void Set(T value) => Set(value, _data.Length - _offset);

		public void Copy(long offset, FakePtr<T> source, long sourceOffset, long size)
		{
			Array.Copy(source._data, source._offset + sourceOffset, _data, _offset + offset, size);
		}

		public void Copy(FakePtr<T> source, long size) => Copy(0, source, 0, size);

		public void Copy(long offset, T[] source, long sourceOffset, long size)
		{
			Array.Copy(source, sourceOffset, _data, _offset + offset, size);
		}

		public FakePtr<T> Expand(long newSize)
		{
			if (newSize <= _data.Length)
				return this;
			var newBuffer = new FakePtr<T>(newSize);
			Array.Copy(_data, 0, newBuffer._data, 0, _data.Length);
			newBuffer._offset = _offset;
			return newBuffer;
		}

		public static FakePtr<T> operator +(FakePtr<T> buffer, long offset)
		{
			return new FakePtr<T>(buffer._data, buffer._offset + offset);
		}

		public static FakePtr<T> operator -(FakePtr<T> buffer, long offset)
		{
			return new FakePtr<T>(buffer._data, buffer._offset - offset);
		}
	}
}
