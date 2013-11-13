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
	/// Objects that can wrap other values for reflection in the JS environment
	/// will implement Wrapper.
	/// </summary>
	/// <remarks>
	/// Objects that can wrap other values for reflection in the JS environment
	/// will implement Wrapper.
	/// Wrapper defines a single method that can be called to unwrap the object.
	/// </remarks>
	public interface Wrapper
	{
		// API class
		/// <summary>Unwrap the object by returning the wrapped value.</summary>
		/// <remarks>Unwrap the object by returning the wrapped value.</remarks>
		/// <returns>a wrapped value</returns>
		object Unwrap();
	}
}
