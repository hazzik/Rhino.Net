/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Sharpen;

namespace Rhino
{
	/// <summary>This is the default error reporter for JavaScript.</summary>
	/// <remarks>This is the default error reporter for JavaScript.</remarks>
	/// <author>Norris Boyd</author>
	internal class DefaultErrorReporter : ErrorReporter
	{
		internal static readonly Rhino.DefaultErrorReporter instance = new Rhino.DefaultErrorReporter();

		private bool forEval;

		private ErrorReporter chainedReporter;

		private DefaultErrorReporter()
		{
		}

		internal static ErrorReporter ForEval(ErrorReporter reporter)
		{
			Rhino.DefaultErrorReporter r = new Rhino.DefaultErrorReporter();
			r.forEval = true;
			r.chainedReporter = reporter;
			return r;
		}

		public virtual void Warning(string message, string sourceURI, int line, string lineText, int lineOffset)
		{
			if (chainedReporter != null)
			{
				chainedReporter.Warning(message, sourceURI, line, lineText, lineOffset);
			}
		}

		// Do nothing
		public virtual void Error(string message, string sourceURI, int line, string lineText, int lineOffset)
		{
			if (forEval)
			{
				// Assume error message strings that start with "TypeError: "
				// should become TypeError exceptions. A bit of a hack, but we
				// don't want to change the ErrorReporter interface.
				string error = "SyntaxError";
				string TYPE_ERROR_NAME = "TypeError";
				string DELIMETER = ": ";
				string prefix = TYPE_ERROR_NAME + DELIMETER;
				if (message.StartsWith(prefix))
				{
					error = TYPE_ERROR_NAME;
					message = Sharpen.Runtime.Substring(message, prefix.Length);
				}
				throw ScriptRuntime.ConstructError(error, message, sourceURI, line, lineText, lineOffset);
			}
			if (chainedReporter != null)
			{
				chainedReporter.Error(message, sourceURI, line, lineText, lineOffset);
			}
			else
			{
				throw RuntimeError(message, sourceURI, line, lineText, lineOffset);
			}
		}

		public virtual EvaluatorException RuntimeError(string message, string sourceURI, int line, string lineText, int lineOffset)
		{
			if (chainedReporter != null)
			{
				return chainedReporter.RuntimeError(message, sourceURI, line, lineText, lineOffset);
			}
			else
			{
				return new EvaluatorException(message, sourceURI, line, lineText, lineOffset);
			}
		}
	}
}
