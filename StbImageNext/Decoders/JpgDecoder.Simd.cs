#if NET8_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace StbImageNext.Decoders
{
	// Vector versions of the JPEG kernels. They do the same integer math as the scalar ones,
	// several values at a time, so they produce exactly the same output.
	partial class JpgDecoder
	{
		private static void stbi__setup_jpeg_simd(stbi__jpeg j)
		{
			if (!Vector128.IsHardwareAccelerated)
				return;

			j.idct_block_kernel = stbi__idct_simd;
			j.YCbCr_to_RGB_kernel = stbi__YCbCr_to_RGB_simd;
			j.resample_row_hv_2_kernel = stbi__resample_row_hv_2_simd;
		}

		private static void stbi__YCbCr_to_RGB_simd(FakePtr<byte> _out_, FakePtr<byte> y, FakePtr<byte> pcb, FakePtr<byte> pcr, int count, int step)
		{
			var i = 0;
			if (step == 4)
			{
				var output = new Span<byte>(_out_.Data, (int)_out_.Offset, count * 4);
				var ys = new ReadOnlySpan<byte>(y.Data, (int)y.Offset, count);
				var cbs = new ReadOnlySpan<byte>(pcb.Data, (int)pcb.Offset, count);
				var crs = new ReadOnlySpan<byte>(pcr.Data, (int)pcr.Offset, count);
				var alpha = Vector128.Create(unchecked((int)0xff000000));
				for (; i + 16 <= count; i += 16)
				{
					var (y0, y1) = Vector128.Widen(Vector128.Create(ys.Slice(i, 16)));
					var (cb0, cb1) = Vector128.Widen(Vector128.Create(cbs.Slice(i, 16)));
					var (cr0, cr1) = Vector128.Widen(Vector128.Create(crs.Slice(i, 16)));
					stbi__YCbCr_to_RGBA_4(y0, cb0, cr0, alpha, false).AsByte().CopyTo(output.Slice(i * 4, 16));
					stbi__YCbCr_to_RGBA_4(y0, cb0, cr0, alpha, true).AsByte().CopyTo(output.Slice(i * 4 + 16, 16));
					stbi__YCbCr_to_RGBA_4(y1, cb1, cr1, alpha, false).AsByte().CopyTo(output.Slice(i * 4 + 32, 16));
					stbi__YCbCr_to_RGBA_4(y1, cb1, cr1, alpha, true).AsByte().CopyTo(output.Slice(i * 4 + 48, 16));
				}
			}

			if (i < count)
				stbi__YCbCr_to_RGB_row(_out_ + i * step, y + i, pcb + i, pcr + i, count - i, step);
		}

		// Four pixels of stbi__YCbCr_to_RGB_row, as RGBA in the bytes of each int lane
		private static Vector128<int> stbi__YCbCr_to_RGBA_4(Vector128<ushort> y8, Vector128<ushort> cb8, Vector128<ushort> cr8, Vector128<int> alpha, bool upper)
		{
			var y = (upper ? Vector128.WidenUpper(y8) : Vector128.WidenLower(y8)).AsInt32();
			var cb = (upper ? Vector128.WidenUpper(cb8) : Vector128.WidenLower(cb8)).AsInt32() - Vector128.Create(128);
			var cr = (upper ? Vector128.WidenUpper(cr8) : Vector128.WidenLower(cr8)).AsInt32() - Vector128.Create(128);
			var y_fixed = Vector128.ShiftLeft(y, 20) + Vector128.Create(1 << 19);
			var r = y_fixed + cr * ((int)(1.40200f * 4096.0f + 0.5f) << 8);
			var g = y_fixed + cr * -((int)(0.71414f * 4096.0f + 0.5f) << 8) +
				((cb * -((int)(0.34414f * 4096.0f + 0.5f) << 8)) & Vector128.Create(unchecked((int)0xffff0000)));
			var b = y_fixed + cb * ((int)(1.77200f * 4096.0f + 0.5f) << 8);
			var zero = Vector128<int>.Zero;
			var max = Vector128.Create(255);
			r = Vector128.Min(Vector128.Max(Vector128.ShiftRightArithmetic(r, 20), zero), max);
			g = Vector128.Min(Vector128.Max(Vector128.ShiftRightArithmetic(g, 20), zero), max);
			b = Vector128.Min(Vector128.Max(Vector128.ShiftRightArithmetic(b, 20), zero), max);
			return r | Vector128.ShiftLeft(g, 8) | Vector128.ShiftLeft(b, 16) | alpha;
		}

		private static FakePtr<byte> stbi__resample_row_hv_2_simd(FakePtr<byte> _out_, FakePtr<byte> in_near, FakePtr<byte> in_far, int w, int hs)
		{
			if (w < 17)
				return stbi__resample_row_hv_2(_out_, in_near, in_far, w, hs);

			var output = new Span<byte>(_out_.Data, (int)_out_.Offset, w * 2);
			var near = new ReadOnlySpan<byte>(in_near.Data, (int)in_near.Offset, w);
			var far = new ReadOnlySpan<byte>(in_far.Data, (int)in_far.Offset, w);

			output[0] = (byte)((3 * near[0] + far[0] + 2) >> 2);

			// Column sums t at i and at i - 1, sixteen columns at a time; each lane writes out[i * 2 - 1] and out[i * 2]
			var i = 1;
			for (; i + 16 <= w; i += 16)
			{
				var (n0, n1) = Vector128.Widen(Vector128.Create(near.Slice(i, 16)));
				var (f0, f1) = Vector128.Widen(Vector128.Create(far.Slice(i, 16)));
				var (pn0, pn1) = Vector128.Widen(Vector128.Create(near.Slice(i - 1, 16)));
				var (pf0, pf1) = Vector128.Widen(Vector128.Create(far.Slice(i - 1, 16)));
				stbi__resample_hv_2_pairs(n0 * 3 + f0, pn0 * 3 + pf0).AsByte().CopyTo(output.Slice(i * 2 - 1, 16));
				stbi__resample_hv_2_pairs(n1 * 3 + f1, pn1 * 3 + pf1).AsByte().CopyTo(output.Slice(i * 2 + 15, 16));
			}

			for (; i < w; ++i)
			{
				var t0 = 3 * near[i - 1] + far[i - 1];
				var t1 = 3 * near[i] + far[i];
				output[i * 2 - 1] = (byte)((3 * t0 + t1 + 8) >> 4);
				output[i * 2] = (byte)((3 * t1 + t0 + 8) >> 4);
			}

			output[w * 2 - 1] = (byte)((3 * near[w - 1] + far[w - 1] + 2) >> 2);
			return _out_;
		}

		// The two outputs of each column of stbi__resample_row_hv_2, in the low and high byte of each lane
		private static Vector128<ushort> stbi__resample_hv_2_pairs(Vector128<ushort> t, Vector128<ushort> t_prev)
		{
			var eight = Vector128.Create((ushort)8);
			var before = Vector128.ShiftRightLogical(t_prev * 3 + t + eight, 4);
			var after = Vector128.ShiftRightLogical(t * 3 + t_prev + eight, 4);
			return before | Vector128.ShiftLeft(after, 8);
		}

		private static void stbi__idct_simd(FakePtr<byte> _out_, int out_stride, FakePtr<short> data)
		{
			var d = new ReadOnlySpan<short>(data.Data, (int)data.Offset, 64);
			Span<int> val = stackalloc int[64];

			// Columns: a lane per column, the left four columns then the right four
			var r0 = Vector128.Create(d.Slice(0, 8));
			var r1 = Vector128.Create(d.Slice(8, 8));
			var r2 = Vector128.Create(d.Slice(16, 8));
			var r3 = Vector128.Create(d.Slice(24, 8));
			var r4 = Vector128.Create(d.Slice(32, 8));
			var r5 = Vector128.Create(d.Slice(40, 8));
			var r6 = Vector128.Create(d.Slice(48, 8));
			var r7 = Vector128.Create(d.Slice(56, 8));
			for (var half = 0; half < 2; ++half)
			{
				var upper = half == 1;
				var s0 = upper ? Vector128.WidenUpper(r0) : Vector128.WidenLower(r0);
				var s1 = upper ? Vector128.WidenUpper(r1) : Vector128.WidenLower(r1);
				var s2 = upper ? Vector128.WidenUpper(r2) : Vector128.WidenLower(r2);
				var s3 = upper ? Vector128.WidenUpper(r3) : Vector128.WidenLower(r3);
				var s4 = upper ? Vector128.WidenUpper(r4) : Vector128.WidenLower(r4);
				var s5 = upper ? Vector128.WidenUpper(r5) : Vector128.WidenLower(r5);
				var s6 = upper ? Vector128.WidenUpper(r6) : Vector128.WidenLower(r6);
				var s7 = upper ? Vector128.WidenUpper(r7) : Vector128.WidenLower(r7);
				stbi__idct_1d_simd(ref s0, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, 512, 10);
				var column = half * 4;
				s0.CopyTo(val.Slice(column, 4));
				s1.CopyTo(val.Slice(8 + column, 4));
				s2.CopyTo(val.Slice(16 + column, 4));
				s3.CopyTo(val.Slice(24 + column, 4));
				s4.CopyTo(val.Slice(32 + column, 4));
				s5.CopyTo(val.Slice(40 + column, 4));
				s6.CopyTo(val.Slice(48 + column, 4));
				s7.CopyTo(val.Slice(56 + column, 4));
			}

			// Rows: a lane per row, the top four rows then the bottom four
			var output = new Span<byte>(_out_.Data, (int)_out_.Offset, 7 * out_stride + 8);
			var zero = Vector128<int>.Zero;
			var max = Vector128.Create(255);
			for (var top = 0; top < 64; top += 32)
			{
				var s0 = Vector128.Create(val[top], val[top + 8], val[top + 16], val[top + 24]);
				var s1 = Vector128.Create(val[top + 1], val[top + 9], val[top + 17], val[top + 25]);
				var s2 = Vector128.Create(val[top + 2], val[top + 10], val[top + 18], val[top + 26]);
				var s3 = Vector128.Create(val[top + 3], val[top + 11], val[top + 19], val[top + 27]);
				var s4 = Vector128.Create(val[top + 4], val[top + 12], val[top + 20], val[top + 28]);
				var s5 = Vector128.Create(val[top + 5], val[top + 13], val[top + 21], val[top + 29]);
				var s6 = Vector128.Create(val[top + 6], val[top + 14], val[top + 22], val[top + 30]);
				var s7 = Vector128.Create(val[top + 7], val[top + 15], val[top + 23], val[top + 31]);
				stbi__idct_1d_simd(ref s0, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, 65536 + (128 << 17), 17);
				// Back to bytes: each lane holds one value of a row, so the four lanes of s0..s7 make four rows
				var lo = Vector128.Narrow(Vector128.Narrow(Vector128.Min(Vector128.Max(s0, zero), max).AsUInt32(), Vector128.Min(Vector128.Max(s1, zero), max).AsUInt32()),
					Vector128.Narrow(Vector128.Min(Vector128.Max(s2, zero), max).AsUInt32(), Vector128.Min(Vector128.Max(s3, zero), max).AsUInt32()));
				var hi = Vector128.Narrow(Vector128.Narrow(Vector128.Min(Vector128.Max(s4, zero), max).AsUInt32(), Vector128.Min(Vector128.Max(s5, zero), max).AsUInt32()),
					Vector128.Narrow(Vector128.Min(Vector128.Max(s6, zero), max).AsUInt32(), Vector128.Min(Vector128.Max(s7, zero), max).AsUInt32()));
				// lo bytes: s0 rows 0..3, s1 rows 0..3, s2, s3; hi the same for s4..s7
				for (var lane = 0; lane < 4; ++lane)
				{
					var row = output.Slice((top / 8 + lane) * out_stride, 8);
					row[0] = lo.GetElement(lane);
					row[1] = lo.GetElement(lane + 4);
					row[2] = lo.GetElement(lane + 8);
					row[3] = lo.GetElement(lane + 12);
					row[4] = hi.GetElement(lane);
					row[5] = hi.GetElement(lane + 4);
					row[6] = hi.GetElement(lane + 8);
					row[7] = hi.GetElement(lane + 12);
				}
			}
		}

		// The 1D IDCT of stbi__idct_block on each lane: s0..s7 are the inputs, then the outputs
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void stbi__idct_1d_simd(ref Vector128<int> s0, ref Vector128<int> s1, ref Vector128<int> s2, ref Vector128<int> s3,
			ref Vector128<int> s4, ref Vector128<int> s5, ref Vector128<int> s6, ref Vector128<int> s7, int bias, int shift)
		{
			var p2 = s2;
			var p3 = s6;
			var p1 = (p2 + p3) * (int)(0.5411961f * 4096 + 0.5);
			var t2 = p1 + p3 * (int)(-1.847759065f * 4096 + 0.5);
			var t3 = p1 + p2 * (int)(0.765366865f * 4096 + 0.5);
			p2 = s0;
			p3 = s4;
			var t0 = (p2 + p3) * 4096;
			var t1 = (p2 - p3) * 4096;
			var x0 = t0 + t3;
			var x3 = t0 - t3;
			var x1 = t1 + t2;
			var x2 = t1 - t2;
			t0 = s7;
			t1 = s5;
			t2 = s3;
			t3 = s1;
			p3 = t0 + t2;
			var p4 = t1 + t3;
			p1 = t0 + t3;
			p2 = t1 + t2;
			var p5 = (p3 + p4) * (int)(1.175875602f * 4096 + 0.5);
			t0 = t0 * (int)(0.298631336f * 4096 + 0.5);
			t1 = t1 * (int)(2.053119869f * 4096 + 0.5);
			t2 = t2 * (int)(3.072711026f * 4096 + 0.5);
			t3 = t3 * (int)(1.501321110f * 4096 + 0.5);
			p1 = p5 + p1 * (int)(-0.899976223f * 4096 + 0.5);
			p2 = p5 + p2 * (int)(-2.562915447f * 4096 + 0.5);
			p3 = p3 * (int)(-1.961570560f * 4096 + 0.5);
			p4 = p4 * (int)(-0.390180644f * 4096 + 0.5);
			t3 += p1 + p4;
			t2 += p2 + p3;
			t1 += p2 + p4;
			t0 += p1 + p3;
			var b = Vector128.Create(bias);
			x0 += b;
			x1 += b;
			x2 += b;
			x3 += b;
			s0 = Vector128.ShiftRightArithmetic(x0 + t3, shift);
			s7 = Vector128.ShiftRightArithmetic(x0 - t3, shift);
			s1 = Vector128.ShiftRightArithmetic(x1 + t2, shift);
			s6 = Vector128.ShiftRightArithmetic(x1 - t2, shift);
			s2 = Vector128.ShiftRightArithmetic(x2 + t1, shift);
			s5 = Vector128.ShiftRightArithmetic(x2 - t1, shift);
			s3 = Vector128.ShiftRightArithmetic(x3 + t0, shift);
			s4 = Vector128.ShiftRightArithmetic(x3 - t0, shift);
		}
	}
}
#endif
