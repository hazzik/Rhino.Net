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
	/// <summary>AST node for a parenthesized expression.</summary>
	/// <remarks>
	/// AST node for a parenthesized expression.
	/// Node type is
	/// <see cref="Rhino.Token.LP">Rhino.Token.LP</see>
	/// .<p>
	/// </remarks>
	public class ParenthesizedExpression : AstNode
	{
		private AstNode expression;

		public ParenthesizedExpression()
		{
			{
				type = Token.LP;
			}
		}

		public ParenthesizedExpression(int pos) : base(pos)
		{
			{
				type = Token.LP;
			}
		}

		public ParenthesizedExpression(int pos, int len) : base(pos, len)
		{
			{
				type = Token.LP;
			}
		}

		public ParenthesizedExpression(AstNode expr) : this(expr != null ? expr.GetPosition() : 0, expr != null ? expr.GetLength() : 1, expr)
		{
		}

		public ParenthesizedExpression(int pos, int len, AstNode expr) : base(pos, len)
		{
			{
				type = Token.LP;
			}
			SetExpression(expr);
		}

		/// <summary>Returns the expression between the parens</summary>
		public virtual AstNode GetExpression()
		{
			return expression;
		}

		/// <summary>
		/// Sets the expression between the parens, and sets the parent
		/// to this node.
		/// </summary>
		/// <remarks>
		/// Sets the expression between the parens, and sets the parent
		/// to this node.
		/// </remarks>
		/// <param name="expression">the expression between the parens</param>
		/// <exception cref="System.ArgumentException">
		/// } if expression is
		/// <code>null</code>
		/// </exception>
		public virtual void SetExpression(AstNode expression)
		{
			AssertNotNull(expression);
			this.expression = expression;
			expression.SetParent(this);
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + "(" + expression.ToSource(0) + ")";
		}

		/// <summary>Visits this node, then the child expression.</summary>
		/// <remarks>Visits this node, then the child expression.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				expression.Visit(v);
			}
		}
	}
}
