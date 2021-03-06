/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino.V8dtoa;
using Sharpen;

namespace Rhino.V8dtoa
{
	public class FastDtoaBuilder
	{
		internal readonly char[] chars = new char[FastDtoa.kFastDtoaMaximalLength + 8];

		internal int end = 0;

		internal int point;

		internal bool formatted = false;

		// allocate buffer for generated digits + extra notation + padding zeroes
		internal virtual void Append(char c)
		{
			chars[end++] = c;
		}

		internal virtual void DecreaseLast()
		{
			chars[end - 1]--;
		}

		public virtual void Reset()
		{
			end = 0;
			formatted = false;
		}

		public override string ToString()
		{
			return "[chars:" + new string(chars, 0, end) + ", point:" + point + "]";
		}

		public virtual string Format()
		{
			if (!formatted)
			{
				// check for minus sign
				int firstDigit = chars[0] == '-' ? 1 : 0;
				int decPoint = point - firstDigit;
				if (decPoint < -5 || decPoint > 21)
				{
					ToExponentialFormat(firstDigit, decPoint);
				}
				else
				{
					ToFixedFormat(firstDigit, decPoint);
				}
				formatted = true;
			}
			return new string(chars, 0, end);
		}

		private void ToFixedFormat(int firstDigit, int decPoint)
		{
			if (point < end)
			{
				// insert decimal point
				if (decPoint > 0)
				{
					// >= 1, split decimals and insert point
					System.Array.Copy(chars, point, chars, point + 1, end - point);
					chars[point] = '.';
					end++;
				}
				else
				{
					// < 1,
					int target = firstDigit + 2 - decPoint;
					System.Array.Copy(chars, firstDigit, chars, target, end - firstDigit);
					chars[firstDigit] = '0';
					chars[firstDigit + 1] = '.';
					if (decPoint < 0)
					{
						for (int i = firstDigit + 2; i < target; i++)
						{
							chars[i] = '0';
						}
					}
					end += 2 - decPoint;
				}
			}
			else
			{
				if (point > end)
				{
					// large integer, add trailing zeroes
					for (int i = end; i < point; i++)
					{
						chars[i] = '0';
					}
					end += point - end;
				}
			}
		}

		private void ToExponentialFormat(int firstDigit, int decPoint)
		{
			if (end - firstDigit > 1)
			{
				// insert decimal point if more than one digit was produced
				int dot = firstDigit + 1;
				System.Array.Copy(chars, dot, chars, dot + 1, end - dot);
				chars[dot] = '.';
				end++;
			}
			chars[end++] = 'e';
			char sign = '+';
			int exp = decPoint - 1;
			if (exp < 0)
			{
				sign = '-';
				exp = -exp;
			}
			chars[end++] = sign;
			int charPos = exp > 99 ? end + 2 : exp > 9 ? end + 1 : end;
			end = charPos + 1;
			// code below is needed because Integer.getChars() is not public
			for (; ; )
			{
				int r = exp % 10;
				chars[charPos--] = digits[r];
				exp = exp / 10;
				if (exp == 0)
				{
					break;
				}
			}
		}

		internal static readonly char[] digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
	}
}
