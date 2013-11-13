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
	/// <summary>If-else statement.</summary>
	/// <remarks>
	/// If-else statement.  Node type is
	/// <see cref="Rhino.Token.IF">Rhino.Token.IF</see>
	/// .<p>
	/// <pre><i>IfStatement</i> :
	/// <b>if</b> ( Expression ) Statement <b>else</b> Statement
	/// <b>if</b> ( Expression ) Statement</pre>
	/// </remarks>
	public class IfStatement : AstNode
	{
		private AstNode condition;

		private AstNode thenPart;

		private int elsePosition = -1;

		private AstNode elsePart;

		private int lp = -1;

		private int rp = -1;

		public IfStatement()
		{
			{
				type = Token.IF;
			}
		}

		public IfStatement(int pos) : base(pos)
		{
			{
				type = Token.IF;
			}
		}

		public IfStatement(int pos, int len) : base(pos, len)
		{
			{
				type = Token.IF;
			}
		}

		/// <summary>Returns if condition</summary>
		public virtual AstNode GetCondition()
		{
			return condition;
		}

		/// <summary>Sets if condition.</summary>
		/// <remarks>Sets if condition.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>condition</code>
		/// is
		/// <code>null</code>
		/// .
		/// </exception>
		public virtual void SetCondition(AstNode condition)
		{
			AssertNotNull(condition);
			this.condition = condition;
			condition.SetParent(this);
		}

		/// <summary>Returns statement to execute if condition is true</summary>
		public virtual AstNode GetThenPart()
		{
			return thenPart;
		}

		/// <summary>Sets statement to execute if condition is true</summary>
		/// <exception cref="System.ArgumentException">
		/// if thenPart is
		/// <code>null</code>
		/// </exception>
		public virtual void SetThenPart(AstNode thenPart)
		{
			AssertNotNull(thenPart);
			this.thenPart = thenPart;
			thenPart.SetParent(this);
		}

		/// <summary>Returns statement to execute if condition is false</summary>
		public virtual AstNode GetElsePart()
		{
			return elsePart;
		}

		/// <summary>Sets statement to execute if condition is false</summary>
		/// <param name="elsePart">
		/// statement to execute if condition is false.
		/// Can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetElsePart(AstNode elsePart)
		{
			this.elsePart = elsePart;
			if (elsePart != null)
			{
				elsePart.SetParent(this);
			}
		}

		/// <summary>Returns position of "else" keyword, or -1</summary>
		public virtual int GetElsePosition()
		{
			return elsePosition;
		}

		/// <summary>Sets position of "else" keyword, -1 if not present</summary>
		public virtual void SetElsePosition(int elsePosition)
		{
			this.elsePosition = elsePosition;
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

		/// <summary>Returns right paren position, -1 if missing</summary>
		public virtual int GetRp()
		{
			return rp;
		}

		/// <summary>Sets right paren position, -1 if missing</summary>
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
			string pad = MakeIndent(depth);
			StringBuilder sb = new StringBuilder(32);
			sb.Append(pad);
			sb.Append("if (");
			sb.Append(condition.ToSource(0));
			sb.Append(") ");
			if (thenPart.GetType() != Token.BLOCK)
			{
				sb.Append("\n").Append(MakeIndent(depth + 1));
			}
			sb.Append(thenPart.ToSource(depth).Trim());
			if (elsePart != null)
			{
				if (thenPart.GetType() != Token.BLOCK)
				{
					sb.Append("\n").Append(pad).Append("else ");
				}
				else
				{
					sb.Append(" else ");
				}
				if (elsePart.GetType() != Token.BLOCK && elsePart.GetType() != Token.IF)
				{
					sb.Append("\n").Append(MakeIndent(depth + 1));
				}
				sb.Append(elsePart.ToSource(depth).Trim());
			}
			sb.Append("\n");
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, the condition, the then-part, and
		/// if supplied, the else-part.
		/// </summary>
		/// <remarks>
		/// Visits this node, the condition, the then-part, and
		/// if supplied, the else-part.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				condition.Visit(v);
				thenPart.Visit(v);
				if (elsePart != null)
				{
					elsePart.Visit(v);
				}
			}
		}
	}
}
