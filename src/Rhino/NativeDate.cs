/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This class implements the Date native object.</summary>
	/// <remarks>
	/// This class implements the Date native object.
	/// See ECMA 15.9.
	/// </remarks>
	/// <author>Mike McCabe</author>
	[System.Serializable]
	internal sealed class NativeDate : IdScriptableObject
	{
		internal const long serialVersionUID = -8307438915861678966L;

		private static readonly object DATE_TAG = "Date";

		private const string js_NaN_date_str = "Invalid Date";

		private static readonly DateFormat isoFormat;

		static NativeDate()
		{
			isoFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'");
			isoFormat.SetTimeZone(new SimpleTimeZone(0, "UTC"));
			isoFormat.SetLenient(false);
		}

		internal static void Init(Scriptable scope, bool @sealed)
		{
			Rhino.NativeDate obj = new Rhino.NativeDate();
			// Set the value of the prototype Date to NaN ('invalid date');
			obj.date = ScriptRuntime.NaN;
			obj.ExportAsJSClass(MAX_PROTOTYPE_ID, scope, @sealed);
		}

		private NativeDate()
		{
			if (thisTimeZone == null)
			{
				// j.u.TimeZone is synchronized, so setting class statics from it
				// should be OK.
				thisTimeZone = System.TimeZoneInfo.Local;
				LocalTZA = thisTimeZone.GetRawOffset();
			}
		}

		public override string GetClassName()
		{
			return "Date";
		}

		public override object GetDefaultValue(Type typeHint)
		{
			if (typeHint == null)
			{
				typeHint = ScriptRuntime.StringClass;
			}
			return base.GetDefaultValue(typeHint);
		}

		internal double GetJSTimeValue()
		{
			return date;
		}

		protected internal override void FillConstructorProperties(IdFunctionObject ctor)
		{
			AddIdFunctionProperty(ctor, DATE_TAG, ConstructorId_now, "now", 0);
			AddIdFunctionProperty(ctor, DATE_TAG, ConstructorId_parse, "parse", 1);
			AddIdFunctionProperty(ctor, DATE_TAG, ConstructorId_UTC, "UTC", 1);
			base.FillConstructorProperties(ctor);
		}

		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					arity = 1;
					s = "constructor";
					break;
				}

				case Id_toString:
				{
					arity = 0;
					s = "toString";
					break;
				}

				case Id_toTimeString:
				{
					arity = 0;
					s = "toTimeString";
					break;
				}

				case Id_toDateString:
				{
					arity = 0;
					s = "toDateString";
					break;
				}

				case Id_toLocaleString:
				{
					arity = 0;
					s = "toLocaleString";
					break;
				}

				case Id_toLocaleTimeString:
				{
					arity = 0;
					s = "toLocaleTimeString";
					break;
				}

				case Id_toLocaleDateString:
				{
					arity = 0;
					s = "toLocaleDateString";
					break;
				}

				case Id_toUTCString:
				{
					arity = 0;
					s = "toUTCString";
					break;
				}

				case Id_toSource:
				{
					arity = 0;
					s = "toSource";
					break;
				}

				case Id_valueOf:
				{
					arity = 0;
					s = "valueOf";
					break;
				}

				case Id_getTime:
				{
					arity = 0;
					s = "getTime";
					break;
				}

				case Id_getYear:
				{
					arity = 0;
					s = "getYear";
					break;
				}

				case Id_getFullYear:
				{
					arity = 0;
					s = "getFullYear";
					break;
				}

				case Id_getUTCFullYear:
				{
					arity = 0;
					s = "getUTCFullYear";
					break;
				}

				case Id_getMonth:
				{
					arity = 0;
					s = "getMonth";
					break;
				}

				case Id_getUTCMonth:
				{
					arity = 0;
					s = "getUTCMonth";
					break;
				}

				case Id_getDate:
				{
					arity = 0;
					s = "getDate";
					break;
				}

				case Id_getUTCDate:
				{
					arity = 0;
					s = "getUTCDate";
					break;
				}

				case Id_getDay:
				{
					arity = 0;
					s = "getDay";
					break;
				}

				case Id_getUTCDay:
				{
					arity = 0;
					s = "getUTCDay";
					break;
				}

				case Id_getHours:
				{
					arity = 0;
					s = "getHours";
					break;
				}

				case Id_getUTCHours:
				{
					arity = 0;
					s = "getUTCHours";
					break;
				}

				case Id_getMinutes:
				{
					arity = 0;
					s = "getMinutes";
					break;
				}

				case Id_getUTCMinutes:
				{
					arity = 0;
					s = "getUTCMinutes";
					break;
				}

				case Id_getSeconds:
				{
					arity = 0;
					s = "getSeconds";
					break;
				}

				case Id_getUTCSeconds:
				{
					arity = 0;
					s = "getUTCSeconds";
					break;
				}

				case Id_getMilliseconds:
				{
					arity = 0;
					s = "getMilliseconds";
					break;
				}

				case Id_getUTCMilliseconds:
				{
					arity = 0;
					s = "getUTCMilliseconds";
					break;
				}

				case Id_getTimezoneOffset:
				{
					arity = 0;
					s = "getTimezoneOffset";
					break;
				}

				case Id_setTime:
				{
					arity = 1;
					s = "setTime";
					break;
				}

				case Id_setMilliseconds:
				{
					arity = 1;
					s = "setMilliseconds";
					break;
				}

				case Id_setUTCMilliseconds:
				{
					arity = 1;
					s = "setUTCMilliseconds";
					break;
				}

				case Id_setSeconds:
				{
					arity = 2;
					s = "setSeconds";
					break;
				}

				case Id_setUTCSeconds:
				{
					arity = 2;
					s = "setUTCSeconds";
					break;
				}

				case Id_setMinutes:
				{
					arity = 3;
					s = "setMinutes";
					break;
				}

				case Id_setUTCMinutes:
				{
					arity = 3;
					s = "setUTCMinutes";
					break;
				}

				case Id_setHours:
				{
					arity = 4;
					s = "setHours";
					break;
				}

				case Id_setUTCHours:
				{
					arity = 4;
					s = "setUTCHours";
					break;
				}

				case Id_setDate:
				{
					arity = 1;
					s = "setDate";
					break;
				}

				case Id_setUTCDate:
				{
					arity = 1;
					s = "setUTCDate";
					break;
				}

				case Id_setMonth:
				{
					arity = 2;
					s = "setMonth";
					break;
				}

				case Id_setUTCMonth:
				{
					arity = 2;
					s = "setUTCMonth";
					break;
				}

				case Id_setFullYear:
				{
					arity = 3;
					s = "setFullYear";
					break;
				}

				case Id_setUTCFullYear:
				{
					arity = 3;
					s = "setUTCFullYear";
					break;
				}

				case Id_setYear:
				{
					arity = 1;
					s = "setYear";
					break;
				}

				case Id_toISOString:
				{
					arity = 0;
					s = "toISOString";
					break;
				}

				case Id_toJSON:
				{
					arity = 1;
					s = "toJSON";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(DATE_TAG, id, s, arity);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(DATE_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			switch (id)
			{
				case ConstructorId_now:
				{
					return ScriptRuntime.WrapNumber(Now());
				}

				case ConstructorId_parse:
				{
					string dataStr = ScriptRuntime.ToString(args, 0);
					return ScriptRuntime.WrapNumber(Date_parseString(dataStr));
				}

				case ConstructorId_UTC:
				{
					return ScriptRuntime.WrapNumber(JsStaticFunction_UTC(args));
				}

				case Id_constructor:
				{
					// if called as a function, just return a string
					// representing the current time.
					if (thisObj != null)
					{
						return Date_format(Now(), Id_toString);
					}
					return JsConstructor(args);
				}

				case Id_toJSON:
				{
					if (thisObj is Rhino.NativeDate)
					{
						return ((Rhino.NativeDate)thisObj).ToISOString();
					}
					string toISOString = "toISOString";
					Scriptable o = ScriptRuntime.ToObject(cx, scope, thisObj);
					object tv = ScriptRuntime.ToPrimitive(o, ScriptRuntime.NumberClass);
					if (tv.IsNumber())
					{
						double d = System.Convert.ToDouble(tv);
						if (d != d || System.Double.IsInfinity(d))
						{
							return null;
						}
					}
					object toISO = o.Get(toISOString, o);
					if (toISO == ScriptableConstants.NOT_FOUND)
					{
						throw ScriptRuntime.TypeError2("msg.function.not.found.in", toISOString, ScriptRuntime.ToString(o));
					}
					if (!(toISO is Callable))
					{
						throw ScriptRuntime.TypeError3("msg.isnt.function.in", toISOString, ScriptRuntime.ToString(o), ScriptRuntime.ToString(toISO));
					}
					object result = ((Callable)toISO).Call(cx, scope, o, ScriptRuntime.emptyArgs);
					if (!ScriptRuntime.IsPrimitive(result))
					{
						throw ScriptRuntime.TypeError1("msg.toisostring.must.return.primitive", ScriptRuntime.ToString(result));
					}
					return result;
				}
			}
			// The rest of Date.prototype methods require thisObj to be Date
			if (!(thisObj is Rhino.NativeDate))
			{
				throw IncompatibleCallError(f);
			}
			Rhino.NativeDate realThis = (Rhino.NativeDate)thisObj;
			double t = realThis.date;
			switch (id)
			{
				case Id_toString:
				case Id_toTimeString:
				case Id_toDateString:
				{
					if (t == t)
					{
						return Date_format(t, id);
					}
					return js_NaN_date_str;
				}

				case Id_toLocaleString:
				case Id_toLocaleTimeString:
				case Id_toLocaleDateString:
				{
					if (t == t)
					{
						return ToLocale_helper(t, id);
					}
					return js_NaN_date_str;
				}

				case Id_toUTCString:
				{
					if (t == t)
					{
						return Js_toUTCString(t);
					}
					return js_NaN_date_str;
				}

				case Id_toSource:
				{
					return "(new Date(" + ScriptRuntime.ToString(t) + "))";
				}

				case Id_valueOf:
				case Id_getTime:
				{
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getYear:
				case Id_getFullYear:
				case Id_getUTCFullYear:
				{
					if (t == t)
					{
						if (id != Id_getUTCFullYear)
						{
							t = LocalTime(t);
						}
						t = YearFromTime(t);
						if (id == Id_getYear)
						{
							if (cx.HasFeature(Context.FEATURE_NON_ECMA_GET_YEAR))
							{
								if (1900 <= t && t < 2000)
								{
									t -= 1900;
								}
							}
							else
							{
								t -= 1900;
							}
						}
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getMonth:
				case Id_getUTCMonth:
				{
					if (t == t)
					{
						if (id == Id_getMonth)
						{
							t = LocalTime(t);
						}
						t = MonthFromTime(t);
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getDate:
				case Id_getUTCDate:
				{
					if (t == t)
					{
						if (id == Id_getDate)
						{
							t = LocalTime(t);
						}
						t = DateFromTime(t);
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getDay:
				case Id_getUTCDay:
				{
					if (t == t)
					{
						if (id == Id_getDay)
						{
							t = LocalTime(t);
						}
						t = WeekDay(t);
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getHours:
				case Id_getUTCHours:
				{
					if (t == t)
					{
						if (id == Id_getHours)
						{
							t = LocalTime(t);
						}
						t = HourFromTime(t);
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getMinutes:
				case Id_getUTCMinutes:
				{
					if (t == t)
					{
						if (id == Id_getMinutes)
						{
							t = LocalTime(t);
						}
						t = MinFromTime(t);
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getSeconds:
				case Id_getUTCSeconds:
				{
					if (t == t)
					{
						if (id == Id_getSeconds)
						{
							t = LocalTime(t);
						}
						t = SecFromTime(t);
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getMilliseconds:
				case Id_getUTCMilliseconds:
				{
					if (t == t)
					{
						if (id == Id_getMilliseconds)
						{
							t = LocalTime(t);
						}
						t = MsFromTime(t);
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_getTimezoneOffset:
				{
					if (t == t)
					{
						t = (t - LocalTime(t)) / msPerMinute;
					}
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_setTime:
				{
					t = TimeClip(ScriptRuntime.ToNumber(args, 0));
					realThis.date = t;
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_setMilliseconds:
				case Id_setUTCMilliseconds:
				case Id_setSeconds:
				case Id_setUTCSeconds:
				case Id_setMinutes:
				case Id_setUTCMinutes:
				case Id_setHours:
				case Id_setUTCHours:
				{
					t = MakeTime(t, args, id);
					realThis.date = t;
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_setDate:
				case Id_setUTCDate:
				case Id_setMonth:
				case Id_setUTCMonth:
				case Id_setFullYear:
				case Id_setUTCFullYear:
				{
					t = MakeDate(t, args, id);
					realThis.date = t;
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_setYear:
				{
					double year = ScriptRuntime.ToNumber(args, 0);
					if (year != year || System.Double.IsInfinity(year))
					{
						t = ScriptRuntime.NaN;
					}
					else
					{
						if (t != t)
						{
							t = 0;
						}
						else
						{
							t = LocalTime(t);
						}
						if (year >= 0 && year <= 99)
						{
							year += 1900;
						}
						double day = MakeDay(year, MonthFromTime(t), DateFromTime(t));
						t = MakeDate(day, TimeWithinDay(t));
						t = InternalUTC(t);
						t = TimeClip(t);
					}
					realThis.date = t;
					return ScriptRuntime.WrapNumber(t);
				}

				case Id_toISOString:
				{
					return realThis.ToISOString();
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
		}

		private string ToISOString()
		{
			if (date == date)
			{
				lock (isoFormat)
				{
					return isoFormat.Format(Sharpen.Extensions.CreateDate((long)date));
				}
			}
			string msg = ScriptRuntime.GetMessage0("msg.invalid.date");
			throw ScriptRuntime.ConstructError("RangeError", msg);
		}

		private const double HalfTimeDomain = 8.64e15;

		private const double HoursPerDay = 24.0;

		private const double MinutesPerHour = 60.0;

		private const double SecondsPerMinute = 60.0;

		private const double msPerSecond = 1000.0;

		private const double MinutesPerDay = (HoursPerDay * MinutesPerHour);

		private const double SecondsPerDay = (MinutesPerDay * SecondsPerMinute);

		private const double SecondsPerHour = (MinutesPerHour * SecondsPerMinute);

		private const double msPerDay = (SecondsPerDay * msPerSecond);

		private const double msPerHour = (SecondsPerHour * msPerSecond);

		private const double msPerMinute = (SecondsPerMinute * msPerSecond);

		private static double Day(double t)
		{
			return Math.Floor(t / msPerDay);
		}

		private static double TimeWithinDay(double t)
		{
			double result;
			result = t % msPerDay;
			if (result < 0)
			{
				result += msPerDay;
			}
			return result;
		}

		private static bool IsLeapYear(int year)
		{
			return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
		}

		private static double DayFromYear(double y)
		{
			return ((365 * ((y) - 1970) + Math.Floor(((y) - 1969) / 4.0) - Math.Floor(((y) - 1901) / 100.0) + Math.Floor(((y) - 1601) / 400.0)));
		}

		private static double TimeFromYear(double y)
		{
			return DayFromYear(y) * msPerDay;
		}

		private static int YearFromTime(double t)
		{
			int lo = (int)Math.Floor((t / msPerDay) / 366) + 1970;
			int hi = (int)Math.Floor((t / msPerDay) / 365) + 1970;
			int mid;
			if (hi < lo)
			{
				int temp = lo;
				lo = hi;
				hi = temp;
			}
			while (hi > lo)
			{
				mid = (hi + lo) / 2;
				if (TimeFromYear(mid) > t)
				{
					hi = mid - 1;
				}
				else
				{
					lo = mid + 1;
					if (TimeFromYear(lo) > t)
					{
						return mid;
					}
				}
			}
			return lo;
		}

		private static double DayFromMonth(int m, int year)
		{
			int day = m * 30;
			if (m >= 7)
			{
				day += m / 2 - 1;
			}
			else
			{
				if (m >= 2)
				{
					day += (m - 1) / 2 - 1;
				}
				else
				{
					day += m;
				}
			}
			if (m >= 2 && IsLeapYear(year))
			{
				++day;
			}
			return day;
		}

		private static int MonthFromTime(double t)
		{
			int year = YearFromTime(t);
			int d = (int)(Day(t) - DayFromYear(year));
			d -= 31 + 28;
			if (d < 0)
			{
				return (d < -28) ? 0 : 1;
			}
			if (IsLeapYear(year))
			{
				if (d == 0)
				{
					return 1;
				}
				// 29 February
				--d;
			}
			// d: date count from 1 March
			int estimate = d / 30;
			// approx number of month since March
			int mstart;
			switch (estimate)
			{
				case 0:
				{
					return 2;
				}

				case 1:
				{
					mstart = 31;
					break;
				}

				case 2:
				{
					mstart = 31 + 30;
					break;
				}

				case 3:
				{
					mstart = 31 + 30 + 31;
					break;
				}

				case 4:
				{
					mstart = 31 + 30 + 31 + 30;
					break;
				}

				case 5:
				{
					mstart = 31 + 30 + 31 + 30 + 31;
					break;
				}

				case 6:
				{
					mstart = 31 + 30 + 31 + 30 + 31 + 31;
					break;
				}

				case 7:
				{
					mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30;
					break;
				}

				case 8:
				{
					mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31;
					break;
				}

				case 9:
				{
					mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31 + 30;
					break;
				}

				case 10:
				{
					return 11;
				}

				default:
				{
					//Late december
					throw Kit.CodeBug();
				}
			}
			// if d < mstart then real month since March == estimate - 1
			return (d >= mstart) ? estimate + 2 : estimate + 1;
		}

		private static int DateFromTime(double t)
		{
			int year = YearFromTime(t);
			int d = (int)(Day(t) - DayFromYear(year));
			d -= 31 + 28;
			if (d < 0)
			{
				return (d < -28) ? d + 31 + 28 + 1 : d + 28 + 1;
			}
			if (IsLeapYear(year))
			{
				if (d == 0)
				{
					return 29;
				}
				// 29 February
				--d;
			}
			// d: date count from 1 March
			int mdays;
			int mstart;
			switch (d / 30)
			{
				case 0:
				{
					// approx number of month since March
					return d + 1;
				}

				case 1:
				{
					mdays = 31;
					mstart = 31;
					break;
				}

				case 2:
				{
					mdays = 30;
					mstart = 31 + 30;
					break;
				}

				case 3:
				{
					mdays = 31;
					mstart = 31 + 30 + 31;
					break;
				}

				case 4:
				{
					mdays = 30;
					mstart = 31 + 30 + 31 + 30;
					break;
				}

				case 5:
				{
					mdays = 31;
					mstart = 31 + 30 + 31 + 30 + 31;
					break;
				}

				case 6:
				{
					mdays = 31;
					mstart = 31 + 30 + 31 + 30 + 31 + 31;
					break;
				}

				case 7:
				{
					mdays = 30;
					mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30;
					break;
				}

				case 8:
				{
					mdays = 31;
					mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31;
					break;
				}

				case 9:
				{
					mdays = 30;
					mstart = 31 + 30 + 31 + 30 + 31 + 31 + 30 + 31 + 30;
					break;
				}

				case 10:
				{
					return d - (31 + 30 + 31 + 30 + 31 + 31 + 30 + 31 + 30) + 1;
				}

				default:
				{
					//Late december
					throw Kit.CodeBug();
				}
			}
			d -= mstart;
			if (d < 0)
			{
				// wrong estimate: sfhift to previous month
				d += mdays;
			}
			return d + 1;
		}

		private static int WeekDay(double t)
		{
			double result;
			result = Day(t) + 4;
			result = result % 7;
			if (result < 0)
			{
				result += 7;
			}
			return (int)result;
		}

		private static double Now()
		{
			return Runtime.CurrentTimeMillis();
		}

		private static double DaylightSavingTA(double t)
		{
			// Another workaround!  The JRE doesn't seem to know about DST
			// before year 1 AD, so we map to equivalent dates for the
			// purposes of finding DST. To be safe, we do this for years
			// before 1970.
			if (t < 0.0)
			{
				int year = EquivalentYear(YearFromTime(t));
				double day = MakeDay(year, MonthFromTime(t), DateFromTime(t));
				t = MakeDate(day, TimeWithinDay(t));
			}
			DateTime date = Sharpen.Extensions.CreateDate((long)t);
			if (thisTimeZone.InDaylightTime(date))
			{
				return msPerHour;
			}
			else
			{
				return 0;
			}
		}

		private static int EquivalentYear(int year)
		{
			int day = (int)DayFromYear(year) + 4;
			day = day % 7;
			if (day < 0)
			{
				day += 7;
			}
			// Years and leap years on which Jan 1 is a Sunday, Monday, etc.
			if (IsLeapYear(year))
			{
				switch (day)
				{
					case 0:
					{
						return 1984;
					}

					case 1:
					{
						return 1996;
					}

					case 2:
					{
						return 1980;
					}

					case 3:
					{
						return 1992;
					}

					case 4:
					{
						return 1976;
					}

					case 5:
					{
						return 1988;
					}

					case 6:
					{
						return 1972;
					}
				}
			}
			else
			{
				switch (day)
				{
					case 0:
					{
						return 1978;
					}

					case 1:
					{
						return 1973;
					}

					case 2:
					{
						return 1985;
					}

					case 3:
					{
						return 1986;
					}

					case 4:
					{
						return 1981;
					}

					case 5:
					{
						return 1971;
					}

					case 6:
					{
						return 1977;
					}
				}
			}
			// Unreachable
			throw Kit.CodeBug();
		}

		private static double LocalTime(double t)
		{
			return t + LocalTZA + DaylightSavingTA(t);
		}

		private static double InternalUTC(double t)
		{
			return t - LocalTZA - DaylightSavingTA(t - LocalTZA);
		}

		private static int HourFromTime(double t)
		{
			double result;
			result = Math.Floor(t / msPerHour) % HoursPerDay;
			if (result < 0)
			{
				result += HoursPerDay;
			}
			return (int)result;
		}

		private static int MinFromTime(double t)
		{
			double result;
			result = Math.Floor(t / msPerMinute) % MinutesPerHour;
			if (result < 0)
			{
				result += MinutesPerHour;
			}
			return (int)result;
		}

		private static int SecFromTime(double t)
		{
			double result;
			result = Math.Floor(t / msPerSecond) % SecondsPerMinute;
			if (result < 0)
			{
				result += SecondsPerMinute;
			}
			return (int)result;
		}

		private static int MsFromTime(double t)
		{
			double result;
			result = t % msPerSecond;
			if (result < 0)
			{
				result += msPerSecond;
			}
			return (int)result;
		}

		private static double MakeTime(double hour, double min, double sec, double ms)
		{
			return ((hour * MinutesPerHour + min) * SecondsPerMinute + sec) * msPerSecond + ms;
		}

		private static double MakeDay(double year, double month, double date)
		{
			year += Math.Floor(month / 12);
			month = month % 12;
			if (month < 0)
			{
				month += 12;
			}
			double yearday = Math.Floor(TimeFromYear(year) / msPerDay);
			double monthday = DayFromMonth((int)month, (int)year);
			return yearday + monthday + date - 1;
		}

		private static double MakeDate(double day, double time)
		{
			return day * msPerDay + time;
		}

		private static double TimeClip(double d)
		{
			if (d != d || d == double.PositiveInfinity || d == double.NegativeInfinity || Math.Abs(d) > HalfTimeDomain)
			{
				return ScriptRuntime.NaN;
			}
			if (d > 0.0)
			{
				return Math.Floor(d + 0.);
			}
			else
			{
				return System.Math.Ceiling(d + 0.);
			}
		}

		private static double Date_msecFromDate(double year, double mon, double mday, double hour, double min, double sec, double msec)
		{
			double day;
			double time;
			double result;
			day = MakeDay(year, mon, mday);
			time = MakeTime(hour, min, sec, msec);
			result = MakeDate(day, time);
			return result;
		}

		private const int MAXARGS = 7;

		private static double Date_msecFromArgs(object[] args)
		{
			double[] array = new double[MAXARGS];
			int loop;
			double d;
			for (loop = 0; loop < MAXARGS; loop++)
			{
				if (loop < args.Length)
				{
					d = ScriptRuntime.ToNumber(args[loop]);
					if (d != d || System.Double.IsInfinity(d))
					{
						return ScriptRuntime.NaN;
					}
					array[loop] = ScriptRuntime.ToInteger(args[loop]);
				}
				else
				{
					if (loop == 2)
					{
						array[loop] = 1;
					}
					else
					{
						array[loop] = 0;
					}
				}
			}
			if (array[0] >= 0 && array[0] <= 99)
			{
				array[0] += 1900;
			}
			return Date_msecFromDate(array[0], array[1], array[2], array[3], array[4], array[5], array[6]);
		}

		private static double JsStaticFunction_UTC(object[] args)
		{
			return TimeClip(Date_msecFromArgs(args));
		}

		private static double Date_parseString(string s)
		{
			try
			{
				if (s.Length == 24)
				{
					DateTime d;
					lock (isoFormat)
					{
						d = isoFormat.Parse(s);
					}
					return d.GetTime();
				}
			}
			catch (ParseException)
			{
			}
			int year = -1;
			int mon = -1;
			int mday = -1;
			int hour = -1;
			int min = -1;
			int sec = -1;
			char c = 0;
			char si = 0;
			int i = 0;
			int n = -1;
			double tzoffset = -1;
			char prevc = 0;
			int limit = 0;
			bool seenplusminus = false;
			limit = s.Length;
			while (i < limit)
			{
				c = s[i];
				i++;
				if (c <= ' ' || c == ',' || c == '-')
				{
					if (i < limit)
					{
						si = s[i];
						if (c == '-' && '0' <= si && si <= '9')
						{
							prevc = c;
						}
					}
					continue;
				}
				if (c == '(')
				{
					int depth = 1;
					while (i < limit)
					{
						c = s[i];
						i++;
						if (c == '(')
						{
							depth++;
						}
						else
						{
							if (c == ')')
							{
								if (--depth <= 0)
								{
									break;
								}
							}
						}
					}
					continue;
				}
				if ('0' <= c && c <= '9')
				{
					n = c - '0';
					while (i < limit && '0' <= (c = s[i]) && c <= '9')
					{
						n = n * 10 + c - '0';
						i++;
					}
					if ((prevc == '+' || prevc == '-'))
					{
						seenplusminus = true;
						if (n < 24)
						{
							n = n * 60;
						}
						else
						{
							n = n % 100 + n / 100 * 60;
						}
						if (prevc == '+')
						{
							n = -n;
						}
						if (tzoffset != 0 && tzoffset != -1)
						{
							return ScriptRuntime.NaN;
						}
						tzoffset = n;
					}
					else
					{
						if (n >= 70 || (prevc == '/' && mon >= 0 && mday >= 0 && year < 0))
						{
							if (year >= 0)
							{
								return ScriptRuntime.NaN;
							}
							else
							{
								if (c <= ' ' || c == ',' || c == '/' || i >= limit)
								{
									year = n < 100 ? n + 1900 : n;
								}
								else
								{
									return ScriptRuntime.NaN;
								}
							}
						}
						else
						{
							if (c == ':')
							{
								if (hour < 0)
								{
									hour = n;
								}
								else
								{
									if (min < 0)
									{
										min = n;
									}
									else
									{
										return ScriptRuntime.NaN;
									}
								}
							}
							else
							{
								if (c == '/')
								{
									if (mon < 0)
									{
										mon = n - 1;
									}
									else
									{
										if (mday < 0)
										{
											mday = n;
										}
										else
										{
											return ScriptRuntime.NaN;
										}
									}
								}
								else
								{
									if (i < limit && c != ',' && c > ' ' && c != '-')
									{
										return ScriptRuntime.NaN;
									}
									else
									{
										if (seenplusminus && n < 60)
										{
											if (tzoffset < 0)
											{
												tzoffset -= n;
											}
											else
											{
												tzoffset += n;
											}
										}
										else
										{
											if (hour >= 0 && min < 0)
											{
												min = n;
											}
											else
											{
												if (min >= 0 && sec < 0)
												{
													sec = n;
												}
												else
												{
													if (mday < 0)
													{
														mday = n;
													}
													else
													{
														return ScriptRuntime.NaN;
													}
												}
											}
										}
									}
								}
							}
						}
					}
					prevc = (char)0;
				}
				else
				{
					if (c == '/' || c == ':' || c == '+' || c == '-')
					{
						prevc = c;
					}
					else
					{
						int st = i - 1;
						while (i < limit)
						{
							c = s[i];
							if (!(('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z')))
							{
								break;
							}
							i++;
						}
						int letterCount = i - st;
						if (letterCount < 2)
						{
							return ScriptRuntime.NaN;
						}
						string wtb = "am;pm;" + "monday;tuesday;wednesday;thursday;friday;" + "saturday;sunday;" + "january;february;march;april;may;june;" + "july;august;september;october;november;december;" + "gmt;ut;utc;est;edt;cst;cdt;mst;mdt;pst;pdt;";
						int index = 0;
						for (int wtbOffset = 0; ; )
						{
							int wtbNext = wtb.IndexOf(';', wtbOffset);
							if (wtbNext < 0)
							{
								return ScriptRuntime.NaN;
							}
							if (wtb.RegionMatches(true, wtbOffset, s, st, letterCount))
							{
								break;
							}
							wtbOffset = wtbNext + 1;
							++index;
						}
						if (index < 2)
						{
							if (hour > 12 || hour < 0)
							{
								return ScriptRuntime.NaN;
							}
							else
							{
								if (index == 0)
								{
									// AM
									if (hour == 12)
									{
										hour = 0;
									}
								}
								else
								{
									// PM
									if (hour != 12)
									{
										hour += 12;
									}
								}
							}
						}
						else
						{
							if ((index -= 2) < 7)
							{
							}
							else
							{
								// ignore week days
								if ((index -= 7) < 12)
								{
									// month
									if (mon < 0)
									{
										mon = index;
									}
									else
									{
										return ScriptRuntime.NaN;
									}
								}
								else
								{
									index -= 12;
									switch (index)
									{
										case 0:
										{
											// timezones
											tzoffset = 0;
											break;
										}

										case 1:
										{
											tzoffset = 0;
											break;
										}

										case 2:
										{
											tzoffset = 0;
											break;
										}

										case 3:
										{
											tzoffset = 5 * 60;
											break;
										}

										case 4:
										{
											tzoffset = 4 * 60;
											break;
										}

										case 5:
										{
											tzoffset = 6 * 60;
											break;
										}

										case 6:
										{
											tzoffset = 5 * 60;
											break;
										}

										case 7:
										{
											tzoffset = 7 * 60;
											break;
										}

										case 8:
										{
											tzoffset = 6 * 60;
											break;
										}

										case 9:
										{
											tzoffset = 8 * 60;
											break;
										}

										case 10:
										{
											tzoffset = 7 * 60;
											break;
										}

										default:
										{
											Kit.CodeBug();
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			if (year < 0 || mon < 0 || mday < 0)
			{
				return ScriptRuntime.NaN;
			}
			if (sec < 0)
			{
				sec = 0;
			}
			if (min < 0)
			{
				min = 0;
			}
			if (hour < 0)
			{
				hour = 0;
			}
			double msec = Date_msecFromDate(year, mon, mday, hour, min, sec, 0);
			if (tzoffset == -1)
			{
				return InternalUTC(msec);
			}
			else
			{
				return msec + tzoffset * msPerMinute;
			}
		}

		private static string Date_format(double t, int methodId)
		{
			StringBuilder result = new StringBuilder(60);
			double local = LocalTime(t);
			if (methodId != Id_toTimeString)
			{
				AppendWeekDayName(result, WeekDay(local));
				result.Append(' ');
				AppendMonthName(result, MonthFromTime(local));
				result.Append(' ');
				Append0PaddedUint(result, DateFromTime(local), 2);
				result.Append(' ');
				int year = YearFromTime(local);
				if (year < 0)
				{
					result.Append('-');
					year = -year;
				}
				Append0PaddedUint(result, year, 4);
				if (methodId != Id_toDateString)
				{
					result.Append(' ');
				}
			}
			if (methodId != Id_toDateString)
			{
				Append0PaddedUint(result, HourFromTime(local), 2);
				result.Append(':');
				Append0PaddedUint(result, MinFromTime(local), 2);
				result.Append(':');
				Append0PaddedUint(result, SecFromTime(local), 2);
				// offset from GMT in minutes.  The offset includes daylight
				// savings, if it applies.
				int minutes = (int)Math.Floor((LocalTZA + DaylightSavingTA(t)) / msPerMinute);
				// map 510 minutes to 0830 hours
				int offset = (minutes / 60) * 100 + minutes % 60;
				if (offset > 0)
				{
					result.Append(" GMT+");
				}
				else
				{
					result.Append(" GMT-");
					offset = -offset;
				}
				Append0PaddedUint(result, offset, 4);
				if (timeZoneFormatter == null)
				{
					timeZoneFormatter = new SimpleDateFormat("zzz");
				}
				// Find an equivalent year before getting the timezone
				// comment.  See DaylightSavingTA.
				if (t < 0.0)
				{
					int equiv = EquivalentYear(YearFromTime(local));
					double day = MakeDay(equiv, MonthFromTime(t), DateFromTime(t));
					t = MakeDate(day, TimeWithinDay(t));
				}
				result.Append(" (");
				DateTime date = Sharpen.Extensions.CreateDate((long)t);
				lock (timeZoneFormatter)
				{
					result.Append(timeZoneFormatter.Format(date));
				}
				result.Append(')');
			}
			return result.ToString();
		}

		private static object JsConstructor(object[] args)
		{
			Rhino.NativeDate obj = new Rhino.NativeDate();
			// if called as a constructor with no args,
			// return a new Date with the current time.
			if (args.Length == 0)
			{
				obj.date = Now();
				return obj;
			}
			// if called with just one arg -
			if (args.Length == 1)
			{
				object arg0 = args[0];
				if (arg0 is Scriptable)
				{
					arg0 = ((Scriptable)arg0).GetDefaultValue(null);
				}
				double date;
				if (arg0 is string)
				{
					// it's a string; parse it.
					date = Date_parseString(arg0.ToString());
				}
				else
				{
					// if it's not a string, use it as a millisecond date
					date = ScriptRuntime.ToNumber(arg0);
				}
				obj.date = TimeClip(date);
				return obj;
			}
			double time = Date_msecFromArgs(args);
			if (!double.IsNaN(time) && !System.Double.IsInfinity(time))
			{
				time = TimeClip(InternalUTC(time));
			}
			obj.date = time;
			return obj;
		}

		private static string ToLocale_helper(double t, int methodId)
		{
			DateFormat formatter;
			switch (methodId)
			{
				case Id_toLocaleString:
				{
					if (localeDateTimeFormatter == null)
					{
						localeDateTimeFormatter = DateFormat.GetDateTimeInstance(DateFormat.LONG, DateFormat.LONG);
					}
					formatter = localeDateTimeFormatter;
					break;
				}

				case Id_toLocaleTimeString:
				{
					if (localeTimeFormatter == null)
					{
						localeTimeFormatter = DateFormat.GetTimeInstance(DateFormat.LONG);
					}
					formatter = localeTimeFormatter;
					break;
				}

				case Id_toLocaleDateString:
				{
					if (localeDateFormatter == null)
					{
						localeDateFormatter = DateFormat.GetDateInstance(DateFormat.LONG);
					}
					formatter = localeDateFormatter;
					break;
				}

				default:
				{
					throw new Exception();
				}
			}
			// unreachable
			lock (formatter)
			{
				return formatter.Format(Sharpen.Extensions.CreateDate((long)t));
			}
		}

		private static string Js_toUTCString(double date)
		{
			StringBuilder result = new StringBuilder(60);
			AppendWeekDayName(result, WeekDay(date));
			result.Append(", ");
			Append0PaddedUint(result, DateFromTime(date), 2);
			result.Append(' ');
			AppendMonthName(result, MonthFromTime(date));
			result.Append(' ');
			int year = YearFromTime(date);
			if (year < 0)
			{
				result.Append('-');
				year = -year;
			}
			Append0PaddedUint(result, year, 4);
			result.Append(' ');
			Append0PaddedUint(result, HourFromTime(date), 2);
			result.Append(':');
			Append0PaddedUint(result, MinFromTime(date), 2);
			result.Append(':');
			Append0PaddedUint(result, SecFromTime(date), 2);
			result.Append(" GMT");
			return result.ToString();
		}

		private static void Append0PaddedUint(StringBuilder sb, int i, int minWidth)
		{
			if (i < 0)
			{
				Kit.CodeBug();
			}
			int scale = 1;
			--minWidth;
			if (i >= 10)
			{
				if (i < 1000 * 1000 * 1000)
				{
					for (; ; )
					{
						int newScale = scale * 10;
						if (i < newScale)
						{
							break;
						}
						--minWidth;
						scale = newScale;
					}
				}
				else
				{
					// Separated case not to check against 10 * 10^9 overflow
					minWidth -= 9;
					scale = 1000 * 1000 * 1000;
				}
			}
			while (minWidth > 0)
			{
				sb.Append('0');
				--minWidth;
			}
			while (scale != 1)
			{
				sb.Append((char)('0' + (i / scale)));
				i %= scale;
				scale /= 10;
			}
			sb.Append((char)('0' + i));
		}

		private static void AppendMonthName(StringBuilder sb, int index)
		{
			// Take advantage of the fact that all month abbreviations
			// have the same length to minimize amount of strings runtime has
			// to keep in memory
			string months = "Jan" + "Feb" + "Mar" + "Apr" + "May" + "Jun" + "Jul" + "Aug" + "Sep" + "Oct" + "Nov" + "Dec";
			index *= 3;
			for (int i = 0; i != 3; ++i)
			{
				sb.Append(months[index + i]);
			}
		}

		private static void AppendWeekDayName(StringBuilder sb, int index)
		{
			string days = "Sun" + "Mon" + "Tue" + "Wed" + "Thu" + "Fri" + "Sat";
			index *= 3;
			for (int i = 0; i != 3; ++i)
			{
				sb.Append(days[index + i]);
			}
		}

		private static double MakeTime(double date, object[] args, int methodId)
		{
			int maxargs;
			bool local = true;
			switch (methodId)
			{
				case Id_setUTCMilliseconds:
				{
					local = false;
					goto case Id_setMilliseconds;
				}

				case Id_setMilliseconds:
				{
					// fallthrough
					maxargs = 1;
					break;
				}

				case Id_setUTCSeconds:
				{
					local = false;
					goto case Id_setSeconds;
				}

				case Id_setSeconds:
				{
					// fallthrough
					maxargs = 2;
					break;
				}

				case Id_setUTCMinutes:
				{
					local = false;
					goto case Id_setMinutes;
				}

				case Id_setMinutes:
				{
					// fallthrough
					maxargs = 3;
					break;
				}

				case Id_setUTCHours:
				{
					local = false;
					goto case Id_setHours;
				}

				case Id_setHours:
				{
					// fallthrough
					maxargs = 4;
					break;
				}

				default:
				{
					Kit.CodeBug();
					maxargs = 0;
					break;
				}
			}
			int i;
			double[] conv = new double[4];
			double hour;
			double min;
			double sec;
			double msec;
			double lorutime;
			double time;
			double result;
			if (date != date)
			{
				return date;
			}
			if (args.Length == 0)
			{
				args = ScriptRuntime.PadArguments(args, 1);
			}
			for (i = 0; i < args.Length && i < maxargs; i++)
			{
				conv[i] = ScriptRuntime.ToNumber(args[i]);
				// limit checks that happen in MakeTime in ECMA.
				if (conv[i] != conv[i] || System.Double.IsInfinity(conv[i]))
				{
					return ScriptRuntime.NaN;
				}
				conv[i] = ScriptRuntime.ToInteger(conv[i]);
			}
			if (local)
			{
				lorutime = LocalTime(date);
			}
			else
			{
				lorutime = date;
			}
			i = 0;
			int stop = args.Length;
			if (maxargs >= 4 && i < stop)
			{
				hour = conv[i++];
			}
			else
			{
				hour = HourFromTime(lorutime);
			}
			if (maxargs >= 3 && i < stop)
			{
				min = conv[i++];
			}
			else
			{
				min = MinFromTime(lorutime);
			}
			if (maxargs >= 2 && i < stop)
			{
				sec = conv[i++];
			}
			else
			{
				sec = SecFromTime(lorutime);
			}
			if (maxargs >= 1 && i < stop)
			{
				msec = conv[i++];
			}
			else
			{
				msec = MsFromTime(lorutime);
			}
			time = MakeTime(hour, min, sec, msec);
			result = MakeDate(Day(lorutime), time);
			if (local)
			{
				result = InternalUTC(result);
			}
			date = TimeClip(result);
			return date;
		}

		private static double MakeDate(double date, object[] args, int methodId)
		{
			int maxargs;
			bool local = true;
			switch (methodId)
			{
				case Id_setUTCDate:
				{
					local = false;
					goto case Id_setDate;
				}

				case Id_setDate:
				{
					// fallthrough
					maxargs = 1;
					break;
				}

				case Id_setUTCMonth:
				{
					local = false;
					goto case Id_setMonth;
				}

				case Id_setMonth:
				{
					// fallthrough
					maxargs = 2;
					break;
				}

				case Id_setUTCFullYear:
				{
					local = false;
					goto case Id_setFullYear;
				}

				case Id_setFullYear:
				{
					// fallthrough
					maxargs = 3;
					break;
				}

				default:
				{
					Kit.CodeBug();
					maxargs = 0;
					break;
				}
			}
			int i;
			double[] conv = new double[3];
			double year;
			double month;
			double day;
			double lorutime;
			double result;
			if (args.Length == 0)
			{
				args = ScriptRuntime.PadArguments(args, 1);
			}
			for (i = 0; i < args.Length && i < maxargs; i++)
			{
				conv[i] = ScriptRuntime.ToNumber(args[i]);
				// limit checks that happen in MakeDate in ECMA.
				if (conv[i] != conv[i] || System.Double.IsInfinity(conv[i]))
				{
					return ScriptRuntime.NaN;
				}
				conv[i] = ScriptRuntime.ToInteger(conv[i]);
			}
			if (date != date)
			{
				if (args.Length < 3)
				{
					return ScriptRuntime.NaN;
				}
				else
				{
					lorutime = 0;
				}
			}
			else
			{
				if (local)
				{
					lorutime = LocalTime(date);
				}
				else
				{
					lorutime = date;
				}
			}
			i = 0;
			int stop = args.Length;
			if (maxargs >= 3 && i < stop)
			{
				year = conv[i++];
			}
			else
			{
				year = YearFromTime(lorutime);
			}
			if (maxargs >= 2 && i < stop)
			{
				month = conv[i++];
			}
			else
			{
				month = MonthFromTime(lorutime);
			}
			if (maxargs >= 1 && i < stop)
			{
				day = conv[i++];
			}
			else
			{
				day = DateFromTime(lorutime);
			}
			day = MakeDay(year, month, day);
			result = MakeDate(day, TimeWithinDay(lorutime));
			if (local)
			{
				result = InternalUTC(result);
			}
			date = TimeClip(result);
			return date;
		}

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2009-07-22 05:44:02 EST
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 6:
				{
					c = s[0];
					if (c == 'g')
					{
						X = "getDay";
						id = Id_getDay;
					}
					else
					{
						if (c == 't')
						{
							X = "toJSON";
							id = Id_toJSON;
						}
					}
					goto L_break;
				}

				case 7:
				{
					switch (s[3])
					{
						case 'D':
						{
							c = s[0];
							if (c == 'g')
							{
								X = "getDate";
								id = Id_getDate;
							}
							else
							{
								if (c == 's')
								{
									X = "setDate";
									id = Id_setDate;
								}
							}
							goto L_break;
						}

						case 'T':
						{
							c = s[0];
							if (c == 'g')
							{
								X = "getTime";
								id = Id_getTime;
							}
							else
							{
								if (c == 's')
								{
									X = "setTime";
									id = Id_setTime;
								}
							}
							goto L_break;
						}

						case 'Y':
						{
							c = s[0];
							if (c == 'g')
							{
								X = "getYear";
								id = Id_getYear;
							}
							else
							{
								if (c == 's')
								{
									X = "setYear";
									id = Id_setYear;
								}
							}
							goto L_break;
						}

						case 'u':
						{
							X = "valueOf";
							id = Id_valueOf;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 8:
				{
					switch (s[3])
					{
						case 'H':
						{
							c = s[0];
							if (c == 'g')
							{
								X = "getHours";
								id = Id_getHours;
							}
							else
							{
								if (c == 's')
								{
									X = "setHours";
									id = Id_setHours;
								}
							}
							goto L_break;
						}

						case 'M':
						{
							c = s[0];
							if (c == 'g')
							{
								X = "getMonth";
								id = Id_getMonth;
							}
							else
							{
								if (c == 's')
								{
									X = "setMonth";
									id = Id_setMonth;
								}
							}
							goto L_break;
						}

						case 'o':
						{
							X = "toSource";
							id = Id_toSource;
							goto L_break;
						}

						case 't':
						{
							X = "toString";
							id = Id_toString;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 9:
				{
					X = "getUTCDay";
					id = Id_getUTCDay;
					goto L_break;
				}

				case 10:
				{
					c = s[3];
					if (c == 'M')
					{
						c = s[0];
						if (c == 'g')
						{
							X = "getMinutes";
							id = Id_getMinutes;
						}
						else
						{
							if (c == 's')
							{
								X = "setMinutes";
								id = Id_setMinutes;
							}
						}
					}
					else
					{
						if (c == 'S')
						{
							c = s[0];
							if (c == 'g')
							{
								X = "getSeconds";
								id = Id_getSeconds;
							}
							else
							{
								if (c == 's')
								{
									X = "setSeconds";
									id = Id_setSeconds;
								}
							}
						}
						else
						{
							if (c == 'U')
							{
								c = s[0];
								if (c == 'g')
								{
									X = "getUTCDate";
									id = Id_getUTCDate;
								}
								else
								{
									if (c == 's')
									{
										X = "setUTCDate";
										id = Id_setUTCDate;
									}
								}
							}
						}
					}
					goto L_break;
				}

				case 11:
				{
					switch (s[3])
					{
						case 'F':
						{
							c = s[0];
							if (c == 'g')
							{
								X = "getFullYear";
								id = Id_getFullYear;
							}
							else
							{
								if (c == 's')
								{
									X = "setFullYear";
									id = Id_setFullYear;
								}
							}
							goto L_break;
						}

						case 'M':
						{
							X = "toGMTString";
							id = Id_toGMTString;
							goto L_break;
						}

						case 'S':
						{
							X = "toISOString";
							id = Id_toISOString;
							goto L_break;
						}

						case 'T':
						{
							X = "toUTCString";
							id = Id_toUTCString;
							goto L_break;
						}

						case 'U':
						{
							c = s[0];
							if (c == 'g')
							{
								c = s[9];
								if (c == 'r')
								{
									X = "getUTCHours";
									id = Id_getUTCHours;
								}
								else
								{
									if (c == 't')
									{
										X = "getUTCMonth";
										id = Id_getUTCMonth;
									}
								}
							}
							else
							{
								if (c == 's')
								{
									c = s[9];
									if (c == 'r')
									{
										X = "setUTCHours";
										id = Id_setUTCHours;
									}
									else
									{
										if (c == 't')
										{
											X = "setUTCMonth";
											id = Id_setUTCMonth;
										}
									}
								}
							}
							goto L_break;
						}

						case 's':
						{
							X = "constructor";
							id = Id_constructor;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 12:
				{
					c = s[2];
					if (c == 'D')
					{
						X = "toDateString";
						id = Id_toDateString;
					}
					else
					{
						if (c == 'T')
						{
							X = "toTimeString";
							id = Id_toTimeString;
						}
					}
					goto L_break;
				}

				case 13:
				{
					c = s[0];
					if (c == 'g')
					{
						c = s[6];
						if (c == 'M')
						{
							X = "getUTCMinutes";
							id = Id_getUTCMinutes;
						}
						else
						{
							if (c == 'S')
							{
								X = "getUTCSeconds";
								id = Id_getUTCSeconds;
							}
						}
					}
					else
					{
						if (c == 's')
						{
							c = s[6];
							if (c == 'M')
							{
								X = "setUTCMinutes";
								id = Id_setUTCMinutes;
							}
							else
							{
								if (c == 'S')
								{
									X = "setUTCSeconds";
									id = Id_setUTCSeconds;
								}
							}
						}
					}
					goto L_break;
				}

				case 14:
				{
					c = s[0];
					if (c == 'g')
					{
						X = "getUTCFullYear";
						id = Id_getUTCFullYear;
					}
					else
					{
						if (c == 's')
						{
							X = "setUTCFullYear";
							id = Id_setUTCFullYear;
						}
						else
						{
							if (c == 't')
							{
								X = "toLocaleString";
								id = Id_toLocaleString;
							}
						}
					}
					goto L_break;
				}

				case 15:
				{
					c = s[0];
					if (c == 'g')
					{
						X = "getMilliseconds";
						id = Id_getMilliseconds;
					}
					else
					{
						if (c == 's')
						{
							X = "setMilliseconds";
							id = Id_setMilliseconds;
						}
					}
					goto L_break;
				}

				case 17:
				{
					X = "getTimezoneOffset";
					id = Id_getTimezoneOffset;
					goto L_break;
				}

				case 18:
				{
					c = s[0];
					if (c == 'g')
					{
						X = "getUTCMilliseconds";
						id = Id_getUTCMilliseconds;
					}
					else
					{
						if (c == 's')
						{
							X = "setUTCMilliseconds";
							id = Id_setUTCMilliseconds;
						}
						else
						{
							if (c == 't')
							{
								c = s[8];
								if (c == 'D')
								{
									X = "toLocaleDateString";
									id = Id_toLocaleDateString;
								}
								else
								{
									if (c == 'T')
									{
										X = "toLocaleTimeString";
										id = Id_toLocaleTimeString;
									}
								}
							}
						}
					}
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		private const int ConstructorId_now = -3;

		private const int ConstructorId_parse = -2;

		private const int ConstructorId_UTC = -1;

		private const int Id_constructor = 1;

		private const int Id_toString = 2;

		private const int Id_toTimeString = 3;

		private const int Id_toDateString = 4;

		private const int Id_toLocaleString = 5;

		private const int Id_toLocaleTimeString = 6;

		private const int Id_toLocaleDateString = 7;

		private const int Id_toUTCString = 8;

		private const int Id_toSource = 9;

		private const int Id_valueOf = 10;

		private const int Id_getTime = 11;

		private const int Id_getYear = 12;

		private const int Id_getFullYear = 13;

		private const int Id_getUTCFullYear = 14;

		private const int Id_getMonth = 15;

		private const int Id_getUTCMonth = 16;

		private const int Id_getDate = 17;

		private const int Id_getUTCDate = 18;

		private const int Id_getDay = 19;

		private const int Id_getUTCDay = 20;

		private const int Id_getHours = 21;

		private const int Id_getUTCHours = 22;

		private const int Id_getMinutes = 23;

		private const int Id_getUTCMinutes = 24;

		private const int Id_getSeconds = 25;

		private const int Id_getUTCSeconds = 26;

		private const int Id_getMilliseconds = 27;

		private const int Id_getUTCMilliseconds = 28;

		private const int Id_getTimezoneOffset = 29;

		private const int Id_setTime = 30;

		private const int Id_setMilliseconds = 31;

		private const int Id_setUTCMilliseconds = 32;

		private const int Id_setSeconds = 33;

		private const int Id_setUTCSeconds = 34;

		private const int Id_setMinutes = 35;

		private const int Id_setUTCMinutes = 36;

		private const int Id_setHours = 37;

		private const int Id_setUTCHours = 38;

		private const int Id_setDate = 39;

		private const int Id_setUTCDate = 40;

		private const int Id_setMonth = 41;

		private const int Id_setUTCMonth = 42;

		private const int Id_setFullYear = 43;

		private const int Id_setUTCFullYear = 44;

		private const int Id_setYear = 45;

		private const int Id_toISOString = 46;

		private const int Id_toJSON = 47;

		private const int MAX_PROTOTYPE_ID = Id_toJSON;

		private const int Id_toGMTString = Id_toUTCString;

		private static TimeZoneInfo thisTimeZone;

		private static double LocalTZA;

		private static DateFormat timeZoneFormatter;

		private static DateFormat localeDateTimeFormatter;

		private static DateFormat localeDateFormatter;

		private static DateFormat localeTimeFormatter;

		private double date;
		// Alias, see Ecma B.2.6
		// #/string_id_map#
	}
}
