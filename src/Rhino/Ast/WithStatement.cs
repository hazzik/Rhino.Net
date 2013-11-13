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
	/// <summary>With statement.</summary>
	/// <remarks>
	/// With statement.  Node type is
	/// <see cref="Rhino.Token.WITH">Rhino.Token.WITH</see>
	/// .<p>
	/// <pre><i>WithStatement</i> :
	/// <b>with</b> ( Expression ) Statement ;</pre>
	/// </remarks>
	public class WithStatement : AstNode
	{
		private AstNode expression;

		private AstNode statement;

		private int lp = -1;

		private int rp = -1;

		public WithStatement()
		{
			{
				type = Token.WITH;
			}
		}

		public WithStatement(int pos) : base(pos)
		{
			{
				type = Token.WITH;
			}
		}

		public WithStatement(int pos, int len) : base(pos, len)
		{
			{
				type = Token.WITH;
			}
		}

		/// <summary>Returns object expression</summary>
		public virtual AstNode GetExpression()
		{
			return expression;
		}

		/// <summary>Sets object expression (and its parent link)</summary>
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

		/// <summary>Returns the statement or block</summary>
		public virtual AstNode GetStatement()
		{
			return statement;
		}

		/// <summary>Sets the statement (and sets its parent link)</summary>
		/// <exception cref="System.ArgumentException">
		/// } if statement is
		/// <code>null</code>
		/// </exception>
		public virtual void SetStatement(AstNode statement)
		{
			AssertNotNull(statement);
			this.statement = statement;
			statement.SetParent(this);
		}

		/// <summary>Returns left paren offset</summary>
		public virtual int GetLp()
		{
			return lp;
		}

		/// <summary>Sets left paren offset</summary>
		public virtual void SetLp(int lp)
		{
			this.lp = lp;
		}

		/// <summary>Returns right paren offset</summary>
		public virtual int GetRp()
		{
			return rp;
		}

		/// <summary>Sets right paren offset</summary>
		public virtual void SetRp(int rp)
		{
			this.rp = rp;
		}

		/// <summary>Sets both paren positions</summary>
		public virtual void SetParens(int lp, int rp)
		{
			this.lp = lp;
			this.rp = rp;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("with (");
			sb.Append(expression.ToSource(0));
			sb.Append(") ");
			if (statement.GetType() == Token.BLOCK)
			{
				sb.Append(statement.ToSource(depth).Trim());
				sb.Append("\n");
			}
			else
			{
				sb.Append("\n").Append(statement.ToSource(depth + 1));
			}
			return sb.ToString();
		}

		/// <summary>Visits this node, then the with-object, then the body statement.</summary>
		/// <remarks>Visits this node, then the with-object, then the body statement.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				expression.Visit(v);
				statement.Visit(v);
			}
		}
	}
}
