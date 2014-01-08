using System;
using System.IO;
using System.Numerics;
using System.Xml;
using Sharpen;

namespace Rhino
{
	public static class BigIntegerEx
	{
		public static BigInteger ShiftLeft(this BigInteger integer, int shift)
		{
			return integer << shift;
		}

		public static BigInteger ShiftRight(this BigInteger integer, int shift)
		{
			return integer >> shift;
		}

		public static BigInteger[] DivideAndRemainder(this BigInteger dividend,BigInteger divisor)
		{
			BigInteger remainder;
			var result = BigInteger.DivRem(dividend, divisor, out remainder);
			return new[] {result, remainder};
		}

		private const string Numbers = "0123456789abcdefghijklmnopqrstuvwxyz";

		public static string ToString(this BigInteger integer, int radix)
		{
			if (radix < 2 || radix > 36)
				throw new ArgumentException("ToString() radix argument must be between 2 and 36", "radix");
			var result = "";
			var sign = integer < 0 ? "-" : "";
			integer = BigInteger.Abs(integer);
			while (integer != 0)
			{
				int x = (int)(integer % radix);
				integer = integer / radix;
				result = Numbers[x] + result;
			}
			return sign + result;
		}
	}

	public class TransformerException : Exception
	{
	}

	public enum OutputKeys
	{
		METHOD,
		OMIT_XML_DECLARATION
	}

	public class StreamResult
	{
		public StreamResult(Stream outputStream)
		{
			throw new NotImplementedException();
		}
	}

	public class DOMSource
	{
		public DOMSource(XmlDocument template)
		{
		}
	}
}