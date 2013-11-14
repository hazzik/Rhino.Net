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
	/// <summary>This interface exposes debugging information from objects.</summary>
	/// <remarks>This interface exposes debugging information from objects.</remarks>
	public interface DebuggableObject
	{
		// API class
		/// <summary>Returns an array of ids for the properties of the object.</summary>
		/// <remarks>
		/// Returns an array of ids for the properties of the object.
		/// <p>All properties, even those with attribute {DontEnum}, are listed.
		/// This allows the debugger to display all properties of the object.<p>
		/// </remarks>
		/// <returns>
		/// an array of java.lang.Objects with an entry for every
		/// listed property. Properties accessed via an integer index will
		/// have a corresponding
		/// Integer entry in the returned array. Properties accessed by
		/// a String will have a String entry in the returned array.
		/// </returns>
		object[] GetAllIds();
	}
}
