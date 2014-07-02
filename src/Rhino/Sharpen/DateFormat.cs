using System;

namespace Sharpen
{
	public abstract class DateFormat
	{
	    TimeZoneInfo timeZone;

	    public TimeZoneInfo GetTimeZone ()
		{
			return timeZone;
		}
		
		public void SetTimeZone (TimeZoneInfo timeZone)
		{
			this.timeZone = timeZone;
		}
	
		public abstract string Format (DateTime time);
	}
}

