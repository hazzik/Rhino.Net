/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>
	/// Common interface for
	/// <see cref="ArrayLiteral">ArrayLiteral</see>
	/// and
	/// <see cref="ObjectLiteral">ObjectLiteral</see>
	/// node types, both of which may appear in "destructuring" expressions or
	/// contexts.
	/// </summary>
	public interface DestructuringForm
	{
		/// <summary>
		/// Marks this node as being a destructuring form - that is, appearing
		/// in a context such as
		/// <code>for ([a, b] in ...)</code>
		/// where it's the
		/// target of a destructuring assignment.
		/// </summary>
		void SetIsDestructuring(bool destructuring);

		/// <summary>
		/// Returns true if this node is in a destructuring position:
		/// a function parameter, the target of a variable initializer, the
		/// iterator of a for..in loop, etc.
		/// </summary>
		/// <remarks>
		/// Returns true if this node is in a destructuring position:
		/// a function parameter, the target of a variable initializer, the
		/// iterator of a for..in loop, etc.
		/// </remarks>
		bool IsDestructuring();
	}
}
