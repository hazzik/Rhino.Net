/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>
	/// AST node for a single 'for (foo in bar)' loop construct in a JavaScript 1.7
	/// Array comprehension.
	/// </summary>
	/// <remarks>
	/// AST node for a single 'for (foo in bar)' loop construct in a JavaScript 1.7
	/// Array comprehension.  This node type is almost equivalent to a
	/// <see cref="ForInLoop">ForInLoop</see>
	/// , except that it has no body statement.
	/// Node type is
	/// <see cref="Rhino.Token.FOR">Rhino.Token.FOR</see>
	/// .<p>
	/// </remarks>
	public class ArrayComprehensionLoop : ForInLoop
	{
		public ArrayComprehensionLoop()
		{
		}

		public ArrayComprehensionLoop(int pos) : base(pos)
		{
		}

		public ArrayComprehensionLoop(int pos, int len) : base(pos, len)
		{
		}

		/// <summary>
		/// Returns
		/// <code>null</code>
		/// for loop body
		/// </summary>
		/// <returns>
		/// loop body (always
		/// <code>null</code>
		/// for this node type)
		/// </returns>
		public override AstNode GetBody()
		{
			return null;
		}

		/// <summary>Throws an exception on attempts to set the loop body.</summary>
		/// <remarks>Throws an exception on attempts to set the loop body.</remarks>
		/// <param name="body">loop body</param>
		/// <exception cref="System.NotSupportedException">System.NotSupportedException</exception>
		public override void SetBody(AstNode body)
		{
			throw new NotSupportedException("this node type has no body");
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + " for " + (IsForEach() ? "each " : string.Empty) + "(" + iterator.ToSource(0) + " in " + iteratedObject.ToSource(0) + ")";
		}

		/// <summary>Visits the iterator expression and the iterated object expression.</summary>
		/// <remarks>
		/// Visits the iterator expression and the iterated object expression.
		/// There is no body-expression for this loop type.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				iterator.Visit(v);
				iteratedObject.Visit(v);
			}
		}
	}
}
