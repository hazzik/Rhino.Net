/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Sharpen;

namespace Rhino.V8dtoa
{
	internal class DiyFp
	{
		private long f;

		private int e;

		internal const int kSignificandSize = 64;

		internal const long kUint64MSB = unchecked((long)(0x8000000000000000L));

		internal DiyFp()
		{
			// Copyright 2010 the V8 project authors. All rights reserved.
			// Redistribution and use in source and binary forms, with or without
			// modification, are permitted provided that the following conditions are
			// met:
			//
			//     * Redistributions of source code must retain the above copyright
			//       notice, this list of conditions and the following disclaimer.
			//     * Redistributions in binary form must reproduce the above
			//       copyright notice, this list of conditions and the following
			//       disclaimer in the documentation and/or other materials provided
			//       with the distribution.
			//     * Neither the name of Google Inc. nor the names of its
			//       contributors may be used to endorse or promote products derived
			//       from this software without specific prior written permission.
			//
			// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
			// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
			// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
			// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
			// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
			// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
			// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
			// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
			// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
			// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
			// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
			// Ported to Java from Mozilla's version of V8-dtoa by Hannes Wallnoefer.
			// The original revision was 67d1049b0bf9 from the mozilla-central tree.
			// This "Do It Yourself Floating Point" class implements a floating-point number
			// with a uint64 significand and an int exponent. Normalized DiyFp numbers will
			// have the most significant bit of the significand set.
			// Multiplication and Subtraction do not normalize their results.
			// DiyFp are not designed to contain special doubles (NaN and Infinity).
			this.f = 0;
			this.e = 0;
		}

		internal DiyFp(long f, int e)
		{
			this.f = f;
			this.e = e;
		}

		private static bool Uint64_gte(long a, long b)
		{
			// greater-or-equal for unsigned int64 in java-style...
			return (a == b) || ((a > b) ^ (a < 0) ^ (b < 0));
		}

		// this = this - other.
		// The exponents of both numbers must be the same and the significand of this
		// must be bigger than the significand of other.
		// The result will not be normalized.
		internal virtual void Subtract(Rhino.V8dtoa.DiyFp other)
		{
			System.Diagnostics.Debug.Assert((e == other.e));
			System.Diagnostics.Debug.Assert(Uint64_gte(f, other.f));
			f -= other.f;
		}

		// Returns a - b.
		// The exponents of both numbers must be the same and this must be bigger
		// than other. The result will not be normalized.
		internal static Rhino.V8dtoa.DiyFp Minus(Rhino.V8dtoa.DiyFp a, Rhino.V8dtoa.DiyFp b)
		{
			Rhino.V8dtoa.DiyFp result = new Rhino.V8dtoa.DiyFp(a.f, a.e);
			result.Subtract(b);
			return result;
		}

		// this = this * other.
		internal virtual void Multiply(Rhino.V8dtoa.DiyFp other)
		{
			// Simply "emulates" a 128 bit multiplication.
			// However: the resulting number only contains 64 bits. The least
			// significant 64 bits are only used for rounding the most significant 64
			// bits.
			long kM32 = unchecked((long)(0xFFFFFFFFL));
			long a = (long)(((ulong)f) >> 32);
			long b = f & kM32;
			long c = (long)(((ulong)other.f) >> 32);
			long d = other.f & kM32;
			long ac = a * c;
			long bc = b * c;
			long ad = a * d;
			long bd = b * d;
			long tmp = ((long)(((ulong)bd) >> 32)) + (ad & kM32) + (bc & kM32);
			// By adding 1U << 31 to tmp we round the final result.
			// Halfway cases will be round up.
			tmp += 1L << 31;
			long result_f = ac + ((long)(((ulong)ad) >> 32)) + ((long)(((ulong)bc) >> 32)) + ((long)(((ulong)tmp) >> 32));
			e += other.e + 64;
			f = result_f;
		}

		// returns a * b;
		internal static Rhino.V8dtoa.DiyFp Times(Rhino.V8dtoa.DiyFp a, Rhino.V8dtoa.DiyFp b)
		{
			Rhino.V8dtoa.DiyFp result = new Rhino.V8dtoa.DiyFp(a.f, a.e);
			result.Multiply(b);
			return result;
		}

		internal virtual void Normalize()
		{
			System.Diagnostics.Debug.Assert((this.f != 0));
			long f = this.f;
			int e = this.e;
			// This method is mainly called for normalizing boundaries. In general
			// boundaries need to be shifted by 10 bits. We thus optimize for this case.
			long k10MSBits = unchecked((long)(0xFFC00000L)) << 32;
			while ((f & k10MSBits) == 0)
			{
				f <<= 10;
				e -= 10;
			}
			while ((f & kUint64MSB) == 0)
			{
				f <<= 1;
				e--;
			}
			this.f = f;
			this.e = e;
		}

		internal static Rhino.V8dtoa.DiyFp Normalize(Rhino.V8dtoa.DiyFp a)
		{
			Rhino.V8dtoa.DiyFp result = new Rhino.V8dtoa.DiyFp(a.f, a.e);
			result.Normalize();
			return result;
		}

		internal virtual long F()
		{
			return f;
		}

		internal virtual int E()
		{
			return e;
		}

		internal virtual void SetF(long new_value)
		{
			f = new_value;
		}

		internal virtual void SetE(int new_value)
		{
			e = new_value;
		}

		public override string ToString()
		{
			return "[DiyFp f:" + f + ", e:" + e + "]";
		}
	}
}
