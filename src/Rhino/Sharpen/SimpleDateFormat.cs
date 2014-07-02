namespace Sharpen
{
	using System;
	using System.Globalization;

	public class SimpleDateFormat : DateFormat
	{
	    readonly string format;

		CultureInfo Culture {
			get; set;
		}

	    public SimpleDateFormat (string format): this (format, CultureInfo.CurrentCulture)
		{
		}

		public SimpleDateFormat (string format, CultureInfo c)
		{
			Culture = c;
			this.format = format.Replace ("EEE", "ddd");
			this.format = this.format.Replace ("Z", "zzz");
			SetTimeZone (TimeZoneInfo.Local);
		}

	    public override string Format (DateTime date)
		{
			date += GetTimeZone().BaseUtcOffset;
			return date.ToString (format);
		}
	}
}
