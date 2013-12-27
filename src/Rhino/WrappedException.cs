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
	/// <summary>A wrapper for runtime exceptions.</summary>
	/// <remarks>
	/// A wrapper for runtime exceptions.
	/// Used by the JavaScript runtime to wrap and propagate exceptions that occur
	/// during runtime.
	/// </remarks>
	/// <author>Norris Boyd</author>
	[System.Serializable]
	public class WrappedException : EvaluatorException
	{
		internal const long serialVersionUID = -1551979216966520648L;

		/// <seealso cref="Context.ThrowAsScriptRuntimeEx(System.Exception)">Context.ThrowAsScriptRuntimeEx(System.Exception)</seealso>
		public WrappedException(Exception exception) : base("Wrapped " + exception.ToString())
		{
			this.exception = exception;
			Kit.InitCause(this, exception);
			int[] linep = new int[] { 0 };
			string sourceName = Context.GetSourcePositionFromStack(linep);
			int lineNumber = linep[0];
			if (sourceName != null)
			{
				InitSourceName(sourceName);
			}
			if (lineNumber != 0)
			{
				InitLineNumber(lineNumber);
			}
		}

		/// <summary>Get the wrapped exception.</summary>
		/// <remarks>Get the wrapped exception.</remarks>
		/// <returns>
		/// the exception that was presented as a argument to the
		/// constructor when this object was created
		/// </returns>
		public virtual Exception GetWrappedException()
		{
			return exception;
		}

		private Exception exception;
	}
}
