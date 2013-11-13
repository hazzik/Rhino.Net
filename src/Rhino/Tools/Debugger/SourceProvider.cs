/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Debug;
using Rhino.Tools.Debugger;
using Sharpen;

namespace Rhino.Tools.Debugger
{
	/// <summary>Interface to provide a source of scripts to the debugger.</summary>
	/// <remarks>Interface to provide a source of scripts to the debugger.</remarks>
	/// <version>$Id: SourceProvider.java,v 1.1 2009/10/23 12:49:58 szegedia%freemail.hu Exp $</version>
	public interface SourceProvider
	{
		/// <summary>Returns the source of the script.</summary>
		/// <remarks>Returns the source of the script.</remarks>
		/// <param name="script">the script object</param>
		/// <returns>
		/// the source code of the script, or null if it can not be provided
		/// (the provider is not expected to decompile the script, so if it doesn't
		/// have a readily available source text, it is free to return null).
		/// </returns>
		string GetSource(DebuggableScript script);
	}
}
