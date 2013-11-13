/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>
	/// This is interface defines a protocol for the reporting of
	/// errors during JavaScript translation in IDE-mode.
	/// </summary>
	/// <remarks>
	/// This is interface defines a protocol for the reporting of
	/// errors during JavaScript translation in IDE-mode.
	/// If the
	/// <see cref="Rhino.Parser">Rhino.Parser</see>
	/// 's error reporter is
	/// set to an instance of this interface, then this interface's
	/// <see cref="Warning(string, string, int, int)">Warning(string, string, int, int)</see>
	/// and
	/// <see cref="Error(string, string, int, int)">Error(string, string, int, int)</see>
	/// methods are called instead
	/// of the
	/// <see cref="Rhino.ErrorReporter">Rhino.ErrorReporter</see>
	/// versions. <p>
	/// These methods take a source char offset and a length.  The
	/// rationale is that in interactive IDE-type environments, the source
	/// is available and the IDE will want to indicate where the error
	/// occurred and how much code participates in it.  The start and length
	/// are generally chosen to fit within a single line, for readability,
	/// but the client is free to use the AST to determine the affected
	/// node(s) from the start position and change the error or warning's
	/// display bounds.<p>
	/// </remarks>
	/// <author>Steve Yegge</author>
	public interface IdeErrorReporter : ErrorReporter
	{
		// API class
		/// <summary>
		/// Report a warning.<p>
		/// The implementing class may choose to ignore the warning
		/// if it desires.
		/// </summary>
		/// <remarks>
		/// Report a warning.<p>
		/// The implementing class may choose to ignore the warning
		/// if it desires.
		/// </remarks>
		/// <param name="message">
		/// a
		/// <code>String</code>
		/// describing the warning
		/// </param>
		/// <param name="sourceName">
		/// a
		/// <code>String</code>
		/// describing the JavaScript source
		/// where the warning occured; typically a filename or URL
		/// </param>
		/// <param name="offset">the warning's 0-indexed char position in the input stream</param>
		/// <param name="length">the length of the region contributing to the warning</param>
		void Warning(string message, string sourceName, int offset, int length);

		/// <summary>
		/// Report an error.<p>
		/// The implementing class is free to throw an exception if
		/// it desires.<p>
		/// If execution has not yet begun, the JavaScript engine is
		/// free to find additional errors rather than terminating
		/// the translation.
		/// </summary>
		/// <remarks>
		/// Report an error.<p>
		/// The implementing class is free to throw an exception if
		/// it desires.<p>
		/// If execution has not yet begun, the JavaScript engine is
		/// free to find additional errors rather than terminating
		/// the translation. It will not execute a script that had
		/// errors, however.<p>
		/// </remarks>
		/// <param name="message">a String describing the error</param>
		/// <param name="sourceName">
		/// a String describing the JavaScript source
		/// where the error occured; typically a filename or URL
		/// </param>
		/// <param name="offset">0-indexed char position of the error in the input stream</param>
		/// <param name="length">the length of the region contributing to the error</param>
		void Error(string message, string sourceName, int offset, int length);
	}
}
