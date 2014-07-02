using System.Linq;

namespace Sharpen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
	using System.Text.RegularExpressions;

	public static class Extensions
	{
		private static readonly long EPOCH_TICKS;

		static Extensions ()
		{
			DateTime time = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			EPOCH_TICKS = time.Ticks;
		}

	    private static UTF8Encoding UTF8Encoder = new UTF8Encoding(false, true);

        public static Encoding GetEncoding(string name)
        {
            //			Encoding e = Encoding.GetEncoding (name, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            try
            {
                Encoding e = Encoding.GetEncoding(name.Replace('_', '-'));
                if (e is UTF8Encoding)
                    return UTF8Encoder;
                return e;
            }
            catch (ArgumentException)
            {
                throw new UnsupportedCharsetException(name);
            }
        }

	    public static T GetLast<T>(this IList<T> list)
        {
            return ((list.Count == 0) ? default(T) : list[list.Count - 1]);
        }

        public static InputStream GetResourceAsStream(this Type type, string name)
        {
            string str2 = type.Assembly.GetName().Name + ".resources";
            string[] textArray1 = new string[] { str2, ".", type.Namespace, ".", name };
            string str = string.Concat(textArray1);
            Stream manifestResourceStream = type.Assembly.GetManifestResourceStream(str);
            if (manifestResourceStream == null)
            {
                return null;
            }
            return InputStream.Wrap(manifestResourceStream);
        }

        public static long GetTime(this DateTime dateTime)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), TimeSpan.Zero).ToMillisecondsSinceEpoch();
        }

	    public static ListIterator<T> ListIterator<T>(this IList<T> col, int n)
        {
            return new ListIterator<T>(col, n);
        }

        public static DateTime CreateDate(long milliSecondsSinceEpoch)
        {
            long num = EPOCH_TICKS + (milliSecondsSinceEpoch*10000);
            return new DateTime(num);
        }

        public static DateTimeOffset MillisToDateTimeOffset(long milliSecondsSinceEpoch, long offsetMinutes)
        {
            TimeSpan offset = TimeSpan.FromMinutes(offsetMinutes);
            long num = EPOCH_TICKS + (milliSecondsSinceEpoch*10000);
            return new DateTimeOffset(num + offset.Ticks, offset);
        }

	    public static string ReplaceAll(this string str, string regex, string replacement)
        {
            Regex rgx = new Regex(regex);

            if (replacement.IndexOfAny(new char[] { '\\', '$' }) != -1)
            {
                // Back references not yet supported
                StringBuilder sb = new StringBuilder();
                for (int n = 0; n < replacement.Length; n++)
                {
                    char c = replacement[n];
                    if (c == '$')
                        throw new NotSupportedException("Back references not supported");
                    if (c == '\\')
                        c = replacement[++n];
                    sb.Append(c);
                }
                replacement = sb.ToString();
            }

            return rgx.Replace(str, replacement);
        }

        public static bool RegionMatches(this string str, int toOffset, string other, int ooffset, int len)
        {
            return RegionMatches(str, false, toOffset, other, ooffset, len);
        }

        public static bool RegionMatches(this string str, bool ignoreCase, int toOffset, string other, int ooffset, int len)
        {
            return toOffset >= 0 && ooffset >= 0 && toOffset + len <= str.Length && ooffset + len <= other.Length && string.Compare(str, toOffset, other, ooffset, len, ignoreCase) == 0;
        }

	    public static T Set<T>(this IList<T> list, int index, T item)
        {
            T old = list[index];
            list[index] = item;
            return old;
        }

        public static void RemoveAll<T, U>(this ICollection<T> col, ICollection<U> items) where U : T
        {
            foreach (var u in items)
                col.Remove(u);
        }

        public static bool ContainsAll<T, U>(this ICollection<T> col, ICollection<U> items) where U : T
        {
            foreach (var u in items)
                if (!col.Any(n => (object.ReferenceEquals(n, u)) || n.Equals(u)))
                    return false;
            return true;
        }

        public static bool Contains<T>(this ICollection<T> col, T item)
        {
            return col.Any(n => (object.ReferenceEquals(n, item)) || n.Equals(item));
        }

	    public static long ToMillisecondsSinceEpoch(this DateTime dateTime)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("dateTime is expected to be expressed as a UTC DateTime", "dateTime");
            }
            return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc), TimeSpan.Zero).ToMillisecondsSinceEpoch();
        }

        public static long ToMillisecondsSinceEpoch(this DateTimeOffset dateTimeOffset)
        {
            return (((dateTimeOffset.Ticks - dateTimeOffset.Offset.Ticks) - EPOCH_TICKS)/TimeSpan.TicksPerMillisecond);
        }

        public static string ToHexString(int val)
        {
            return Convert.ToString(val, 16);
        }

	    public static HttpURLConnection OpenConnection(this Uri uri)
        {
            return new HttpsURLConnection(uri);
        }


        public static Uri Resolve(this Uri uri, string str)
        {
            //TODO: Check implementation
            return new Uri(uri, str);
        }
    }
}
