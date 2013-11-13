/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>Java reflection of JavaScript exceptions.</summary>
	/// <remarks>
	/// Java reflection of JavaScript exceptions.
	/// Instances of this class are thrown by the JavaScript 'throw' keyword.
	/// </remarks>
	/// <author>Mike McCabe</author>
	[System.Serializable]
	public class JavaScriptException : RhinoException
	{
		internal const long serialVersionUID = -7666130513694669293L;

		[System.ObsoleteAttribute(@"Use WrappedException.WrappedException(System.Exception) to report exceptions in Java code.")]
		public JavaScriptException(object value) : this(value, string.Empty, 0)
		{
		}

		/// <summary>Create a JavaScript exception wrapping the given JavaScript value</summary>
		/// <param name="value">the JavaScript value thrown.</param>
		public JavaScriptException(object value, string sourceName, int lineNumber)
		{
			// API class
			RecordErrorOrigin(sourceName, lineNumber, null, 0);
			this.value = value;
			// Fill in fileName and lineNumber automatically when not specified
			// explicitly, see Bugzilla issue #342807
			if (value is NativeError && Context.GetContext().HasFeature(Context.FEATURE_LOCATION_INFORMATION_IN_ERROR))
			{
				NativeError error = (NativeError)value;
				if (!error.Has("fileName", error))
				{
					error.Put("fileName", error, sourceName);
				}
				if (!error.Has("lineNumber", error))
				{
					error.Put("lineNumber", error, Sharpen.Extensions.ValueOf(lineNumber));
				}
				// set stack property, see bug #549604
				error.SetStackProvider(this);
			}
		}

		public override string Details()
		{
			if (value == null)
			{
				return "null";
			}
			else
			{
				if (value is NativeError)
				{
					return value.ToString();
				}
			}
			try
			{
				return ScriptRuntime.ToString(value);
			}
			catch (Exception)
			{
				// ScriptRuntime.toString may throw a RuntimeException
				if (value is Scriptable)
				{
					return ScriptRuntime.DefaultObjectToString((Scriptable)value);
				}
				else
				{
					return value.ToString();
				}
			}
		}

		/// <returns>the value wrapped by this exception</returns>
		public virtual object GetValue()
		{
			return value;
		}

		[System.ObsoleteAttribute(@"Use RhinoException.SourceName() from the super class.")]
		public virtual string GetSourceName()
		{
			return SourceName();
		}

		[System.ObsoleteAttribute(@"Use RhinoException.LineNumber() from the super class.")]
		public virtual int GetLineNumber()
		{
			return LineNumber();
		}

		private object value;
	}
}
