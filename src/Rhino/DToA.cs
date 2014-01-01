/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Numerics;
using System.Text;
using Rhino;
using Sharpen;

namespace Rhino
{
	internal class DToA
	{
		private static char BASEDIGIT(int digit)
		{
			return (char)((digit >= 10) ? 'a' - 10 + digit : '0' + digit);
		}

		internal const int DTOSTR_STANDARD = 0;

		internal const int DTOSTR_STANDARD_EXPONENTIAL = 1;

		internal const int DTOSTR_FIXED = 2;

		internal const int DTOSTR_EXPONENTIAL = 3;

		internal const int DTOSTR_PRECISION = 4;

		private const int Frac_mask = unchecked((int)(0xfffff));

		private const int Exp_shift = 20;

		private const int Exp_msk1 = unchecked((int)(0x100000));

		private const long Frac_maskL = unchecked((long)(0xfffffffffffffL));

		private const int Exp_shiftL = 52;

		private const long Exp_msk1L = unchecked((long)(0x10000000000000L));

		private const int Bias = 1023;

		private const int P = 53;

		private const int Exp_shift1 = 20;

		private const int Exp_mask = unchecked((int)(0x7ff00000));

		private const int Exp_mask_shifted = unchecked((int)(0x7ff));

		private const int Bndry_mask = unchecked((int)(0xfffff));

		private const int Log2P = 1;

		private const int Sign_bit = unchecked((int)(0x80000000));

		private const int Exp_11 = unchecked((int)(0x3ff00000));

		private const int Ten_pmax = 22;

		private const int Quick_max = 14;

		private const int Bletch = unchecked((int)(0x10));

		private const int Frac_mask1 = unchecked((int)(0xfffff));

		private const int Int_max = 14;

		private const int n_bigtens = 5;

		private static readonly double[] tens = new double[] { 1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22 };

		private static readonly double[] bigtens = new double[] { 1e16, 1e32, 1e64, 1e128, 1e256 };

		private static int Lo0bits(int y)
		{
			int k;
			int x = y;
			if ((x & 7) != 0)
			{
				if ((x & 1) != 0)
				{
					return 0;
				}
				if ((x & 2) != 0)
				{
					return 1;
				}
				return 2;
			}
			k = 0;
			if ((x & unchecked((int)(0xffff))) == 0)
			{
				k = 16;
				x = (int)(((uint)x) >> 16);
			}
			if ((x & unchecked((int)(0xff))) == 0)
			{
				k += 8;
				x = (int)(((uint)x) >> 8);
			}
			if ((x & unchecked((int)(0xf))) == 0)
			{
				k += 4;
				x = (int)(((uint)x) >> 4);
			}
			if ((x & unchecked((int)(0x3))) == 0)
			{
				k += 2;
				x = (int)(((uint)x) >> 2);
			}
			if ((x & 1) == 0)
			{
				k++;
				x = (int)(((uint)x) >> 1);
				if ((x & 1) == 0)
				{
					return 32;
				}
			}
			return k;
		}

		private static int Hi0bits(int x)
		{
			int k = 0;
			if ((x & unchecked((int)(0xffff0000))) == 0)
			{
				k = 16;
				x <<= 16;
			}
			if ((x & unchecked((int)(0xff000000))) == 0)
			{
				k += 8;
				x <<= 8;
			}
			if ((x & unchecked((int)(0xf0000000))) == 0)
			{
				k += 4;
				x <<= 4;
			}
			if ((x & unchecked((int)(0xc0000000))) == 0)
			{
				k += 2;
				x <<= 2;
			}
			if ((x & unchecked((int)(0x80000000))) == 0)
			{
				k++;
				if ((x & unchecked((int)(0x40000000))) == 0)
				{
					return 32;
				}
			}
			return k;
		}

		private static void StuffBits(byte[] bits, int offset, int val)
		{
			bits[offset] = unchecked((byte)(val >> 24));
			bits[offset + 1] = unchecked((byte)(val >> 16));
			bits[offset + 2] = unchecked((byte)(val >> 8));
			bits[offset + 3] = unchecked((byte)(val));
		}

		private static BigInteger D2b(double d, int[] e, int[] bits)
		{
			byte[] dbl_bits;
			int i;
			int k;
			int y;
			int z;
			int de;
			long dBits = System.BitConverter.DoubleToInt64Bits(d);
			int d0 = (int)((long)(((ulong)dBits) >> 32));
			int d1 = (int)(dBits);
			z = d0 & Frac_mask;
			d0 &= unchecked((int)(0x7fffffff));
			if ((de = ((int)(((uint)d0) >> Exp_shift))) != 0)
			{
				z |= Exp_msk1;
			}
			if ((y = d1) != 0)
			{
				dbl_bits = new byte[8];
				k = Lo0bits(y);
				y = (int)(((uint)y) >> k);
				if (k != 0)
				{
					StuffBits(dbl_bits, 4, y | z << (32 - k));
					z >>= k;
				}
				else
				{
					StuffBits(dbl_bits, 4, y);
				}
				StuffBits(dbl_bits, 0, z);
				i = (z != 0) ? 2 : 1;
			}
			else
			{
				//        JS_ASSERT(z);
				dbl_bits = new byte[4];
				k = Lo0bits(z);
				z = (int)(((uint)z) >> k);
				StuffBits(dbl_bits, 0, z);
				k += 32;
				i = 1;
			}
			if (de != 0)
			{
				e[0] = de - Bias - (P - 1) + k;
				bits[0] = P - k;
			}
			else
			{
				e[0] = de - Bias - (P - 1) + 1 + k;
				bits[0] = 32 * i - Hi0bits(z);
			}
			return new BigInteger(dbl_bits);
		}

		internal static string JS_dtobasestr(int @base, double d)
		{
			if (!(2 <= @base && @base <= 36))
			{
				throw new ArgumentException("Bad base: " + @base);
			}
			if (double.IsNaN(d))
			{
				return "NaN";
			}
			else
			{
				if (System.Double.IsInfinity(d))
				{
					return (d > 0.0) ? "Infinity" : "-Infinity";
				}
				else
				{
					if (d == 0)
					{
						// ALERT: should it distinguish -0.0 from +0.0 ?
						return "0";
					}
				}
			}
			bool negative;
			if (d >= 0.0)
			{
				negative = false;
			}
			else
			{
				negative = true;
				d = -d;
			}
			string intDigits;
			double dfloor = Math.Floor(d);
			long lfloor = (long)dfloor;
			if (lfloor == dfloor)
			{
				// int part fits long
				intDigits = BigIntegerEx.ToString((negative) ? -lfloor : lfloor, @base);
			}
			else
			{
				// BigInteger should be used
				long floorBits = System.BitConverter.DoubleToInt64Bits(dfloor);
				int exp = (int)(floorBits >> Exp_shiftL) & Exp_mask_shifted;
				long mantissa;
				if (exp == 0)
				{
					mantissa = (floorBits & Frac_maskL) << 1;
				}
				else
				{
					mantissa = (floorBits & Frac_maskL) | Exp_msk1L;
				}
				if (negative)
				{
					mantissa = -mantissa;
				}
				exp -= 1075;
				BigInteger x = mantissa;
				if (exp > 0)
				{
					x = x.ShiftLeft(exp);
				}
				else
				{
					if (exp < 0)
					{
						x = x.ShiftRight(-exp);
					}
				}
				intDigits = x.ToString(@base);
			}
			if (d == dfloor)
			{
				// No fraction part
				return intDigits;
			}
			else
			{
				StringBuilder buffer;
				int digit;
				double df;
				BigInteger b;
				buffer = new StringBuilder();
				buffer.Append(intDigits).Append('.');
				df = d - dfloor;
				long dBits = System.BitConverter.DoubleToInt64Bits(d);
				int word0 = (int)(dBits >> 32);
				int word1 = (int)(dBits);
				int[] e = new int[1];
				int[] bbits = new int[1];
				b = D2b(df, e, bbits);
				//            JS_ASSERT(e < 0);
				int s2 = -((int)(((uint)word0) >> Exp_shift1) & Exp_mask >> Exp_shift1);
				if (s2 == 0)
				{
					s2 = -1;
				}
				s2 += Bias + P;
				//            JS_ASSERT(-s2 < e);
				BigInteger mlo = 1;
				BigInteger mhi = mlo;
				if ((word1 == 0) && ((word0 & Bndry_mask) == 0) && ((word0 & (Exp_mask & Exp_mask << 1)) != 0))
				{
					s2 += Log2P;
					mhi = 1 << Log2P;
				}
				b = b.ShiftLeft(e[0] + s2);
				BigInteger s = 1;
				s = s.ShiftLeft(s2);
				BigInteger bigBase = @base;
				bool done = false;
				do
				{
					b = System.Numerics.BigInteger.Multiply(b, bigBase);
					BigInteger[] divResult = b.DivideAndRemainder(s);
					b = divResult[1];
					digit = (char)(System.Convert.ToInt32(divResult[0]));
					if (mlo == mhi)
					{
						mlo = mhi = System.Numerics.BigInteger.Multiply(mlo, bigBase);
					}
					else
					{
						mlo = System.Numerics.BigInteger.Multiply(mlo, bigBase);
						mhi = System.Numerics.BigInteger.Multiply(mhi, bigBase);
					}
					int j = b.CompareTo(mlo);
					BigInteger delta = System.Numerics.BigInteger.Subtract(s, mhi);
					int j1 = (delta.Sign <= 0) ? 1 : b.CompareTo(delta);
					if (j1 == 0 && ((word1 & 1) == 0))
					{
						if (j > 0)
						{
							digit++;
						}
						done = true;
					}
					else
					{
						if (j < 0 || (j == 0 && ((word1 & 1) == 0)))
						{
							if (j1 > 0)
							{
								b = b.ShiftLeft(1);
								j1 = b.CompareTo(s);
								if (j1 > 0)
								{
									digit++;
								}
							}
							done = true;
						}
						else
						{
							if (j1 > 0)
							{
								digit++;
								done = true;
							}
						}
					}
					//                JS_ASSERT(digit < (uint32)base);
					buffer.Append(BASEDIGIT(digit));
				}
				while (!done);
				return buffer.ToString();
			}
		}

		internal static int Word0(double d)
		{
			long dBits = System.BitConverter.DoubleToInt64Bits(d);
			return (int)(dBits >> 32);
		}

		internal static double SetWord0(double d, int i)
		{
			long dBits = System.BitConverter.DoubleToInt64Bits(d);
			dBits = ((long)i << 32) | (dBits & unchecked((long)(0x0FFFFFFFFL)));
			return System.BitConverter.Int64BitsToDouble(dBits);
		}

		internal static int Word1(double d)
		{
			long dBits = System.BitConverter.DoubleToInt64Bits(d);
			return (int)(dBits);
		}

		// XXXX the C version built a cache of these
		internal static BigInteger Pow5mult(BigInteger b, int k)
		{
			return System.Numerics.BigInteger.Multiply(b, System.Numerics.BigInteger.Pow(5, k));
		}

		internal static bool RoundOff(StringBuilder buf)
		{
			int i = buf.Length;
			while (i != 0)
			{
				--i;
				char c = buf[i];
				if (c != '9')
				{
					Sharpen.Runtime.SetCharAt(buf, i, (char)(c + 1));
					buf.Length = i + 1;
					return false;
				}
			}
			buf.Length = 0;
			return true;
		}

		internal static int JS_dtoa(double d, int mode, bool biasUp, int ndigits, bool[] sign, StringBuilder buf)
		{
			int b2;
			int b5;
			int i;
			int ieps;
			int ilim;
			int ilim0;
			int ilim1;
			int j;
			int j1;
			int k;
			int k0;
			int m2;
			int m5;
			int s2;
			int s5;
			char dig;
			long L;
			long x;
			BigInteger b;
			BigInteger b1;
			BigInteger delta;
			BigInteger mlo;
			BigInteger mhi = 0;
			BigInteger S;
			int[] be = new int[1];
			int[] bbits = new int[1];
			double d2;
			double ds;
			double eps;
			bool spec_case;
			bool denorm;
			bool k_check;
			bool try_quick;
			bool leftright;
			if ((Word0(d) & Sign_bit) != 0)
			{
				sign[0] = true;
				// word0(d) &= ~Sign_bit;  /* clear sign bit */
				d = SetWord0(d, Word0(d) & ~Sign_bit);
			}
			else
			{
				sign[0] = false;
			}
			if ((Word0(d) & Exp_mask) == Exp_mask)
			{
				buf.Append(((Word1(d) == 0) && ((Word0(d) & Frac_mask) == 0)) ? "Infinity" : "NaN");
				return 9999;
			}
			if (d == 0)
			{
				//          no_digits:
				buf.Length = 0;
				buf.Append('0');
				return 1;
			}
			b = D2b(d, be, bbits);
			if ((i = ((int)(((uint)Word0(d)) >> Exp_shift1) & (Exp_mask >> Exp_shift1))) != 0)
			{
				d2 = SetWord0(d, (Word0(d) & Frac_mask1) | Exp_11);
				i -= Bias;
				denorm = false;
			}
			else
			{
				i = bbits[0] + be[0] + (Bias + (P - 1) - 1);
				x = (i > 32) ? ((long)Word0(d)) << (64 - i) | (int)(((uint)Word1(d)) >> (i - 32)) : ((long)Word1(d)) << (32 - i);
				//            d2 = x;
				//            word0(d2) -= 31*Exp_msk1; /* adjust exponent */
				d2 = SetWord0(x, Word0(x) - 31 * Exp_msk1);
				i -= (Bias + (P - 1) - 1) + 1;
				denorm = true;
			}
			ds = (d2 - 1.5) * 0.289529654602168 + 0.1760912590558 + i * 0.301029995663981;
			k = (int)ds;
			if (ds < 0.0 && ds != k)
			{
				k--;
			}
			k_check = true;
			if (k >= 0 && k <= Ten_pmax)
			{
				if (d < tens[k])
				{
					k--;
				}
				k_check = false;
			}
			j = bbits[0] - i - 1;
			if (j >= 0)
			{
				b2 = 0;
				s2 = j;
			}
			else
			{
				b2 = -j;
				s2 = 0;
			}
			if (k >= 0)
			{
				b5 = 0;
				s5 = k;
				s2 += k;
			}
			else
			{
				b2 -= k;
				b5 = -k;
				s5 = 0;
			}
			if (mode < 0 || mode > 9)
			{
				mode = 0;
			}
			try_quick = true;
			if (mode > 5)
			{
				mode -= 4;
				try_quick = false;
			}
			leftright = true;
			ilim = ilim1 = 0;
			switch (mode)
			{
				case 0:
				case 1:
				{
					ilim = ilim1 = -1;
					i = 18;
					ndigits = 0;
					break;
				}

				case 2:
				{
					leftright = false;
					goto case 4;
				}

				case 4:
				{
					if (ndigits <= 0)
					{
						ndigits = 1;
					}
					ilim = ilim1 = i = ndigits;
					break;
				}

				case 3:
				{
					leftright = false;
					goto case 5;
				}

				case 5:
				{
					i = ndigits + k + 1;
					ilim = i;
					ilim1 = i - 1;
					if (i <= 0)
					{
						i = 1;
					}
					break;
				}
			}
			bool fast_failed = false;
			if (ilim >= 0 && ilim <= Quick_max && try_quick)
			{
				i = 0;
				d2 = d;
				k0 = k;
				ilim0 = ilim;
				ieps = 2;
				if (k > 0)
				{
					ds = tens[k & unchecked((int)(0xf))];
					j = k >> 4;
					if ((j & Bletch) != 0)
					{
						j &= Bletch - 1;
						d /= bigtens[n_bigtens - 1];
						ieps++;
					}
					for (; (j != 0); j >>= 1, i++)
					{
						if ((j & 1) != 0)
						{
							ieps++;
							ds *= bigtens[i];
						}
					}
					d /= ds;
				}
				else
				{
					if ((j1 = -k) != 0)
					{
						d *= tens[j1 & unchecked((int)(0xf))];
						for (j = j1 >> 4; (j != 0); j >>= 1, i++)
						{
							if ((j & 1) != 0)
							{
								ieps++;
								d *= bigtens[i];
							}
						}
					}
				}
				if (k_check && d < 1.0 && ilim > 0)
				{
					if (ilim1 <= 0)
					{
						fast_failed = true;
					}
					else
					{
						ilim = ilim1;
						k--;
						d *= 10.0;
						ieps++;
					}
				}
				//            eps = ieps*d + 7.0;
				//            word0(eps) -= (P-1)*Exp_msk1;
				eps = ieps * d + 7.0;
				eps = SetWord0(eps, Word0(eps) - (P - 1) * Exp_msk1);
				if (ilim == 0)
				{
					d -= 5.0;
					if (d > eps)
					{
						buf.Append('1');
						k++;
						return k + 1;
					}
					if (d < -eps)
					{
						buf.Length = 0;
						buf.Append('0');
						return 1;
					}
					fast_failed = true;
				}
				if (!fast_failed)
				{
					fast_failed = true;
					if (leftright)
					{
						eps = 0.5 / tens[ilim - 1] - eps;
						for (i = 0; ; )
						{
							L = (long)d;
							d -= L;
							buf.Append((char)('0' + L));
							if (d < eps)
							{
								return k + 1;
							}
							if (1.0 - d < eps)
							{
								//                            goto bump_up;
								char lastCh;
								while (true)
								{
									lastCh = buf[buf.Length - 1];
									buf.Length = buf.Length - 1;
									if (lastCh != '9')
									{
										break;
									}
									if (buf.Length == 0)
									{
										k++;
										lastCh = '0';
										break;
									}
								}
								buf.Append((char)(lastCh + 1));
								return k + 1;
							}
							if (++i >= ilim)
							{
								break;
							}
							eps *= 10.0;
							d *= 10.0;
						}
					}
					else
					{
						eps *= tens[ilim - 1];
						for (i = 1; ; i++, d *= 10.0)
						{
							L = (long)d;
							d -= L;
							buf.Append((char)('0' + L));
							if (i == ilim)
							{
								if (d > 0.5 + eps)
								{
									//                                goto bump_up;
									char lastCh;
									while (true)
									{
										lastCh = buf[buf.Length - 1];
										buf.Length = buf.Length - 1;
										if (lastCh != '9')
										{
											break;
										}
										if (buf.Length == 0)
										{
											k++;
											lastCh = '0';
											break;
										}
									}
									buf.Append((char)(lastCh + 1));
									return k + 1;
								}
								else
								{
									if (d < 0.5 - eps)
									{
										StripTrailingZeroes(buf);
										//                                    while(*--s == '0') ;
										//                                    s++;
										return k + 1;
									}
								}
								break;
							}
						}
					}
				}
				if (fast_failed)
				{
					buf.Length = 0;
					d = d2;
					k = k0;
					ilim = ilim0;
				}
			}
			if (be[0] >= 0 && k <= Int_max)
			{
				ds = tens[k];
				if (ndigits < 0 && ilim <= 0)
				{
					if (ilim < 0 || d < 5 * ds || (!biasUp && d == 5 * ds))
					{
						buf.Length = 0;
						buf.Append('0');
						return 1;
					}
					buf.Append('1');
					k++;
					return k + 1;
				}
				for (i = 1; ; i++)
				{
					L = (long)(d / ds);
					d -= L * ds;
					buf.Append((char)('0' + L));
					if (i == ilim)
					{
						d += d;
						if ((d > ds) || (d == ds && (((L & 1) != 0) || biasUp)))
						{
							//                    bump_up:
							//                        while(*--s == '9')
							//                            if (s == buf) {
							//                                k++;
							//                                *s = '0';
							//                                break;
							//                            }
							//                        ++*s++;
							char lastCh;
							while (true)
							{
								lastCh = buf[buf.Length - 1];
								buf.Length = buf.Length - 1;
								if (lastCh != '9')
								{
									break;
								}
								if (buf.Length == 0)
								{
									k++;
									lastCh = '0';
									break;
								}
							}
							buf.Append((char)(lastCh + 1));
						}
						break;
					}
					d *= 10.0;
					if (d == 0)
					{
						break;
					}
				}
				return k + 1;
			}
			m2 = b2;
			m5 = b5;
			if (leftright)
			{
				if (mode < 2)
				{
					i = (denorm) ? be[0] + (Bias + (P - 1) - 1 + 1) : 1 + P - bbits[0];
				}
				else
				{
					j = ilim - 1;
					if (m5 >= j)
					{
						m5 -= j;
					}
					else
					{
						s5 += j -= m5;
						b5 += j;
						m5 = 0;
					}
					if ((i = ilim) < 0)
					{
						m2 -= i;
						i = 0;
					}
				}
				b2 += i;
				s2 += i;
				mhi = 1;
			}
			if (m2 > 0 && s2 > 0)
			{
				i = (m2 < s2) ? m2 : s2;
				b2 -= i;
				m2 -= i;
				s2 -= i;
			}
			if (b5 > 0)
			{
				if (leftright)
				{
					if (m5 > 0)
					{
						mhi = Pow5mult(mhi, m5);
						b1 = System.Numerics.BigInteger.Multiply(mhi, b);
						b = b1;
					}
					if ((j = b5 - m5) != 0)
					{
						b = Pow5mult(b, j);
					}
				}
				else
				{
					b = Pow5mult(b, b5);
				}
			}
			S = 1;
			if (s5 > 0)
			{
				S = Pow5mult(S, s5);
			}
			spec_case = false;
			if (mode < 2)
			{
				if ((Word1(d) == 0) && ((Word0(d) & Bndry_mask) == 0) && ((Word0(d) & (Exp_mask & Exp_mask << 1)) != 0))
				{
					b2 += Log2P;
					s2 += Log2P;
					spec_case = true;
				}
			}
			byte[] S_bytes = S.ToByteArray();
			int S_hiWord = 0;
			for (int idx = 0; idx < 4; idx++)
			{
				S_hiWord = (S_hiWord << 8);
				if (idx < S_bytes.Length)
				{
					S_hiWord |= (S_bytes[idx] & unchecked((int)(0xFF)));
				}
			}
			if ((i = (((s5 != 0) ? 32 - Hi0bits(S_hiWord) : 1) + s2) & unchecked((int)(0x1f))) != 0)
			{
				i = 32 - i;
			}
			if (i > 4)
			{
				i -= 4;
				b2 += i;
				m2 += i;
				s2 += i;
			}
			else
			{
				if (i < 4)
				{
					i += 28;
					b2 += i;
					m2 += i;
					s2 += i;
				}
			}
			if (b2 > 0)
			{
				b = b.ShiftLeft(b2);
			}
			if (s2 > 0)
			{
				S = S.ShiftLeft(s2);
			}
			if (k_check)
			{
				if (b.CompareTo(S) < 0)
				{
					k--;
					b = System.Numerics.BigInteger.Multiply(b, 10);
					if (leftright)
					{
						mhi = System.Numerics.BigInteger.Multiply(mhi, 10);
					}
					ilim = ilim1;
				}
			}
			if (ilim <= 0 && mode > 2)
			{
				if ((ilim < 0) || ((i = b.CompareTo(S = System.Numerics.BigInteger.Multiply(S, 5))) < 0) || ((i == 0 && !biasUp)))
				{
					buf.Length = 0;
					buf.Append('0');
					return 1;
				}
				//                goto no_digits;
				//        one_digit:
				buf.Append('1');
				k++;
				return k + 1;
			}
			if (leftright)
			{
				if (m2 > 0)
				{
					mhi = mhi.ShiftLeft(m2);
				}
				mlo = mhi;
				if (spec_case)
				{
					mhi = mlo;
					mhi = mhi.ShiftLeft(Log2P);
				}
				for (i = 1; ; i++)
				{
					BigInteger[] divResult = b.DivideAndRemainder(S);
					b = divResult[1];
					dig = (char)(System.Convert.ToInt32(divResult[0]) + '0');
					j = b.CompareTo(mlo);
					delta = System.Numerics.BigInteger.Subtract(S, mhi);
					j1 = (delta.Sign <= 0) ? 1 : b.CompareTo(delta);
					if ((j1 == 0) && (mode == 0) && ((Word1(d) & 1) == 0))
					{
						if (dig == '9')
						{
							buf.Append('9');
							if (RoundOff(buf))
							{
								k++;
								buf.Append('1');
							}
							return k + 1;
						}
						//                        goto round_9_up;
						if (j > 0)
						{
							dig++;
						}
						buf.Append(dig);
						return k + 1;
					}
					if ((j < 0) || ((j == 0) && (mode == 0) && ((Word1(d) & 1) == 0)))
					{
						if (j1 > 0)
						{
							b = b.ShiftLeft(1);
							j1 = b.CompareTo(S);
							if (((j1 > 0) || (j1 == 0 && (((dig & 1) == 1) || biasUp))) && (dig++ == '9'))
							{
								buf.Append('9');
								if (RoundOff(buf))
								{
									k++;
									buf.Append('1');
								}
								return k + 1;
							}
						}
						//                                goto round_9_up;
						buf.Append(dig);
						return k + 1;
					}
					if (j1 > 0)
					{
						if (dig == '9')
						{
							//                    round_9_up:
							//                        *s++ = '9';
							//                        goto roundoff;
							buf.Append('9');
							if (RoundOff(buf))
							{
								k++;
								buf.Append('1');
							}
							return k + 1;
						}
						buf.Append((char)(dig + 1));
						return k + 1;
					}
					buf.Append(dig);
					if (i == ilim)
					{
						break;
					}
					b = System.Numerics.BigInteger.Multiply(b, 10);
					if (mlo == mhi)
					{
						mlo = mhi = System.Numerics.BigInteger.Multiply(mhi, 10);
					}
					else
					{
						mlo = System.Numerics.BigInteger.Multiply(mlo, 10);
						mhi = System.Numerics.BigInteger.Multiply(mhi, 10);
					}
				}
			}
			else
			{
				for (i = 1; ; i++)
				{
					//                (char)(dig = quorem(b,S) + '0');
					BigInteger[] divResult = b.DivideAndRemainder(S);
					b = divResult[1];
					dig = (char)(System.Convert.ToInt32(divResult[0]) + '0');
					buf.Append(dig);
					if (i >= ilim)
					{
						break;
					}
					b = System.Numerics.BigInteger.Multiply(b, 10);
				}
			}
			b = b.ShiftLeft(1);
			j = b.CompareTo(S);
			if ((j > 0) || (j == 0 && (((dig & 1) == 1) || biasUp)))
			{
				//        roundoff:
				//            while(*--s == '9')
				//                if (s == buf) {
				//                    k++;
				//                    *s++ = '1';
				//                    goto ret;
				//                }
				//            ++*s++;
				if (RoundOff(buf))
				{
					k++;
					buf.Append('1');
					return k + 1;
				}
			}
			else
			{
				StripTrailingZeroes(buf);
			}
			//            while(*--s == '0') ;
			//            s++;
			//      ret:
			//        Bfree(S);
			//        if (mhi) {
			//            if (mlo && mlo != mhi)
			//                Bfree(mlo);
			//            Bfree(mhi);
			//        }
			//      ret1:
			//        Bfree(b);
			//        JS_ASSERT(s < buf + bufsize);
			return k + 1;
		}

		private static void StripTrailingZeroes(StringBuilder buf)
		{
			//      while(*--s == '0') ;
			//      s++;
			int bl = buf.Length;
			while (bl-- > 0 && buf[bl] == '0')
			{
			}
			// empty
			buf.Length = bl + 1;
		}

		private static readonly int[] dtoaModes = new int[] { 0, 0, 3, 2, 2 };

		internal static void JS_dtostr(StringBuilder buffer, int mode, int precision, double d)
		{
			int decPt;
			bool[] sign = new bool[1];
			int nDigits;
			//        JS_ASSERT(bufferSize >= (size_t)(mode <= DTOSTR_STANDARD_EXPONENTIAL ? DTOSTR_STANDARD_BUFFER_SIZE :
			//                DTOSTR_VARIABLE_BUFFER_SIZE(precision)));
			if (mode == DTOSTR_FIXED && (d >= 1e21 || d <= -1e21))
			{
				mode = DTOSTR_STANDARD;
			}
			decPt = JS_dtoa(d, dtoaModes[mode], mode >= DTOSTR_FIXED, precision, sign, buffer);
			nDigits = buffer.Length;
			if (decPt != 9999)
			{
				bool exponentialNotation = false;
				int minNDigits = 0;
				int p;
				switch (mode)
				{
					case DTOSTR_STANDARD:
					{
						if (decPt < -5 || decPt > 21)
						{
							exponentialNotation = true;
						}
						else
						{
							minNDigits = decPt;
						}
						break;
					}

					case DTOSTR_FIXED:
					{
						if (precision >= 0)
						{
							minNDigits = decPt + precision;
						}
						else
						{
							minNDigits = decPt;
						}
						break;
					}

					case DTOSTR_EXPONENTIAL:
					{
						//                    JS_ASSERT(precision > 0);
						minNDigits = precision;
						goto case DTOSTR_STANDARD_EXPONENTIAL;
					}

					case DTOSTR_STANDARD_EXPONENTIAL:
					{
						exponentialNotation = true;
						break;
					}

					case DTOSTR_PRECISION:
					{
						//                    JS_ASSERT(precision > 0);
						minNDigits = precision;
						if (decPt < -5 || decPt > precision)
						{
							exponentialNotation = true;
						}
						break;
					}
				}
				if (nDigits < minNDigits)
				{
					p = minNDigits;
					nDigits = minNDigits;
					do
					{
						buffer.Append('0');
					}
					while (buffer.Length != p);
				}
				if (exponentialNotation)
				{
					if (nDigits != 1)
					{
						buffer.Insert(1, '.');
					}
					buffer.Append('e');
					if ((decPt - 1) >= 0)
					{
						buffer.Append('+');
					}
					buffer.Append(decPt - 1);
				}
				else
				{
					//                JS_snprintf(numEnd, bufferSize - (numEnd - buffer), "e%+d", decPt-1);
					if (decPt != nDigits)
					{
						//                JS_ASSERT(decPt <= nDigits);
						if (decPt > 0)
						{
							buffer.Insert(decPt, '.');
						}
						else
						{
							for (int i = 0; i < 1 - decPt; i++)
							{
								buffer.Insert(0, '0');
							}
							buffer.Insert(1, '.');
						}
					}
				}
			}
			if (sign[0] && !(Word0(d) == Sign_bit && Word1(d) == 0) && !((Word0(d) & Exp_mask) == Exp_mask && ((Word1(d) != 0) || ((Word0(d) & Frac_mask) != 0))))
			{
				buffer.Insert(0, '-');
			}
		}
	}
}
