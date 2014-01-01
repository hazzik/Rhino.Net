using System;

namespace Rhino.Utils
{
    internal static class NumberUtil
	{
		public static bool IsNumber(this object value)
		{
			return value is SByte ||
				   value is Byte ||
				   value is Int16 ||
				   value is UInt16 ||
				   value is Int32 ||
				   value is UInt32 ||
				   value is Int64 ||
				   value is UInt64 ||
				   value is Single ||
				   value is Double ||
				   value is Decimal;
		}
	}
}