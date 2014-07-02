namespace Sharpen
{
	using System;

	public class AccessController
	{
		public static T DoPrivileged<T> (PrivilegedAction<T> action)
		{
			return action.Run ();
		}
		public static T DoPrivileged<T> (PrivilegedExceptionAction<T> action)
		{
			return action.Run ();
		}
	}
}
