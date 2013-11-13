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
	/// <summary>Simple visitor interface for traversing the AST.</summary>
	/// <remarks>
	/// Simple visitor interface for traversing the AST.  The nodes are visited in
	/// an arbitrary order.  The visitor must cast nodes to the appropriate
	/// type based on their token-type.
	/// </remarks>
	public interface NodeVisitor
	{
		/// <summary>Visits an AST node.</summary>
		/// <remarks>Visits an AST node.</remarks>
		/// <param name="node">
		/// the AST node.  Will never visit an
		/// <see cref="AstRoot">AstRoot</see>
		/// node,
		/// since the
		/// <code>AstRoot</code>
		/// is where the visiting begins.
		/// </param>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if the children should be visited.
		/// If
		/// <code>false</code>
		/// , the subtree rooted at this node is skipped.
		/// The
		/// <code>node</code>
		/// argument should <em>never</em> be
		/// <code>null</code>
		/// --
		/// the individual
		/// <see cref="AstNode">AstNode</see>
		/// classes should skip any children
		/// that are not present in the source when they invoke this method.
		/// </returns>
		bool Visit(AstNode node);
	}
}
