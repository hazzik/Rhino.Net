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
	/// <summary>AST node for an empty expression.</summary>
	/// <remarks>
	/// AST node for an empty expression.  Node type is
	/// <see cref="Rhino.Token.EMPTY">Rhino.Token.EMPTY</see>
	/// .<p>
	/// To create an empty statement, wrap it with an
	/// <see cref="ExpressionStatement">ExpressionStatement</see>
	/// .
	/// </remarks>
	public class EmptyExpression : AstNode
	{
		public EmptyExpression()
		{
			{
				type = Token.EMPTY;
			}
		}

		public EmptyExpression(int pos) : base(pos)
		{
			{
				type = Token.EMPTY;
			}
		}

		public EmptyExpression(int pos, int len) : base(pos, len)
		{
			{
				type = Token.EMPTY;
			}
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth);
		}

		/// <summary>Visits this node.</summary>
		/// <remarks>Visits this node.  There are no children.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
