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
	/// <summary>
	/// This is interface defines a protocol for the reporting of
	/// errors during JavaScript translation or execution.
	/// </summary>
	/// <remarks>
	/// This is interface defines a protocol for the reporting of
	/// errors during JavaScript translation or execution.
	/// </remarks>
	/// <author>Norris Boyd</author>
	public interface ErrorReporter
	{
		// API class
		/// <summary>Report a warning.</summary>
		/// <remarks>
		/// Report a warning.
		/// The implementing class may choose to ignore the warning
		/// if it desires.
		/// </remarks>
		/// <param name="message">a String describing the warning</param>
		/// <param name="sourceName">
		/// a String describing the JavaScript source
		/// where the warning occured; typically a filename or URL
		/// </param>
		/// <param name="line">the line number associated with the warning</param>
		/// <param name="lineSource">the text of the line (may be null)</param>
		/// <param name="lineOffset">the offset into lineSource where problem was detected</param>
		void Warning(string message, string sourceName, int line, string lineSource, int lineOffset);

		/// <summary>Report an error.</summary>
		/// <remarks>
		/// Report an error.
		/// The implementing class is free to throw an exception if
		/// it desires.
		/// If execution has not yet begun, the JavaScript engine is
		/// free to find additional errors rather than terminating
		/// the translation. It will not execute a script that had
		/// errors, however.
		/// </remarks>
		/// <param name="message">a String describing the error</param>
		/// <param name="sourceName">
		/// a String describing the JavaScript source
		/// where the error occured; typically a filename or URL
		/// </param>
		/// <param name="line">the line number associated with the error</param>
		/// <param name="lineSource">the text of the line (may be null)</param>
		/// <param name="lineOffset">the offset into lineSource where problem was detected</param>
		void Error(string message, string sourceName, int line, string lineSource, int lineOffset);

		/// <summary>Creates an EvaluatorException that may be thrown.</summary>
		/// <remarks>
		/// Creates an EvaluatorException that may be thrown.
		/// runtimeErrors, unlike errors, will always terminate the
		/// current script.
		/// </remarks>
		/// <param name="message">a String describing the error</param>
		/// <param name="sourceName">
		/// a String describing the JavaScript source
		/// where the error occured; typically a filename or URL
		/// </param>
		/// <param name="line">the line number associated with the error</param>
		/// <param name="lineSource">the text of the line (may be null)</param>
		/// <param name="lineOffset">the offset into lineSource where problem was detected</param>
		/// <returns>an EvaluatorException that will be thrown.</returns>
		EvaluatorException RuntimeError(string message, string sourceName, int line, string lineSource, int lineOffset);
	}
}
