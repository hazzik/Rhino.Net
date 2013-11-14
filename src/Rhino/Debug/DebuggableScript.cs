/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Debug;
using Sharpen;

namespace Rhino.Debug
{
	/// <summary>
	/// This interface exposes debugging information from executable
	/// code (either functions or top-level scripts).
	/// </summary>
	/// <remarks>
	/// This interface exposes debugging information from executable
	/// code (either functions or top-level scripts).
	/// </remarks>
	public interface DebuggableScript
	{
		// API class
		bool IsTopLevel();

		/// <summary>Returns true if this is a function, false if it is a script.</summary>
		/// <remarks>Returns true if this is a function, false if it is a script.</remarks>
		bool IsFunction();

		/// <summary>Get name of the function described by this script.</summary>
		/// <remarks>
		/// Get name of the function described by this script.
		/// Return null or an empty string if this script is not a function.
		/// </remarks>
		string GetFunctionName();

		/// <summary>Get number of declared parameters in the function.</summary>
		/// <remarks>
		/// Get number of declared parameters in the function.
		/// Return 0 if this script is not a function.
		/// </remarks>
		/// <seealso cref="GetParamAndVarCount()">GetParamAndVarCount()</seealso>
		/// <seealso cref="GetParamOrVarName(int)">GetParamOrVarName(int)</seealso>
		int GetParamCount();

		/// <summary>Get number of declared parameters and local variables.</summary>
		/// <remarks>
		/// Get number of declared parameters and local variables.
		/// Return number of declared global variables if this script is not a
		/// function.
		/// </remarks>
		/// <seealso cref="GetParamCount()">GetParamCount()</seealso>
		/// <seealso cref="GetParamOrVarName(int)">GetParamOrVarName(int)</seealso>
		int GetParamAndVarCount();

		/// <summary>Get name of a declared parameter or local variable.</summary>
		/// <remarks>
		/// Get name of a declared parameter or local variable.
		/// <tt>index</tt> should be less then
		/// <see cref="GetParamAndVarCount()">GetParamAndVarCount()</see>
		/// .
		/// If <tt>index&nbsp;&lt;&nbsp;
		/// <see cref="GetParamCount()">GetParamCount()</see>
		/// </tt>, return
		/// the name of the corresponding parameter, otherwise return the name
		/// of variable.
		/// If this script is not function, return the name of the declared
		/// global variable.
		/// </remarks>
		string GetParamOrVarName(int index);

		/// <summary>
		/// Get the name of the source (usually filename or URL)
		/// of the script.
		/// </summary>
		/// <remarks>
		/// Get the name of the source (usually filename or URL)
		/// of the script.
		/// </remarks>
		string GetSourceName();

		/// <summary>
		/// Returns true if this script or function were runtime-generated
		/// from JavaScript using <tt>eval</tt> function or <tt>Function</tt>
		/// or <tt>Script</tt> constructors.
		/// </summary>
		/// <remarks>
		/// Returns true if this script or function were runtime-generated
		/// from JavaScript using <tt>eval</tt> function or <tt>Function</tt>
		/// or <tt>Script</tt> constructors.
		/// </remarks>
		bool IsGeneratedScript();

		/// <summary>
		/// Get array containing the line numbers that
		/// that can be passed to <code>DebugFrame.onLineChange()<code>.
		/// </summary>
		/// <remarks>
		/// Get array containing the line numbers that
		/// that can be passed to <code>DebugFrame.onLineChange()<code>.
		/// Note that line order in the resulting array is arbitrary
		/// </remarks>
		int[] GetLineNumbers();

		int GetFunctionCount();

		DebuggableScript GetFunction(int index);

		DebuggableScript GetParent();
	}
}
