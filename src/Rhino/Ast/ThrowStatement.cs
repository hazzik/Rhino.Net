/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>Throw statement.</summary>
	/// <remarks>
	/// Throw statement.  Node type is
	/// <see cref="Rhino.Token.THROW">Rhino.Token.THROW</see>
	/// .<p>
	/// <pre><i>ThrowStatement</i> :
	/// <b>throw</b> [<i>no LineTerminator here</i>] Expression ;</pre>
	/// </remarks>
	public class ThrowStatement : AstNode
	{
		private AstNode expression;

		public ThrowStatement()
		{
			{
				type = Token.THROW;
			}
		}

		public ThrowStatement(int pos) : base(pos)
		{
			{
				type = Token.THROW;
			}
		}

		public ThrowStatement(int pos, int len) : base(pos, len)
		{
			{
				type = Token.THROW;
			}
		}

		public ThrowStatement(AstNode expr)
		{
			{
				type = Token.THROW;
			}
			SetExpression(expr);
		}

		public ThrowStatement(int pos, AstNode expr) : base(pos, expr.GetLength())
		{
			{
				type = Token.THROW;
			}
			SetExpression(expr);
		}

		public ThrowStatement(int pos, int len, AstNode expr) : base(pos, len)
		{
			{
				type = Token.THROW;
			}
			SetExpression(expr);
		}

		/// <summary>Returns the expression being thrown</summary>
		public virtual AstNode GetExpression()
		{
			return expression;
		}

		/// <summary>
		/// Sets the expression being thrown, and sets its parent
		/// to this node.
		/// </summary>
		/// <remarks>
		/// Sets the expression being thrown, and sets its parent
		/// to this node.
		/// </remarks>
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
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("throw");
			sb.Append(" ");
			sb.Append(expression.ToSource(0));
			sb.Append(";\n");
			return sb.ToString();
		}

		/// <summary>Visits this node, then the thrown expression.</summary>
		/// <remarks>Visits this node, then the thrown expression.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				expression.Visit(v);
			}
		}
	}
}
