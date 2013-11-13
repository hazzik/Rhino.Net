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
	/// <summary>While statement.</summary>
	/// <remarks>
	/// While statement.  Node type is
	/// <see cref="Rhino.Token.WHILE">Rhino.Token.WHILE</see>
	/// .<p>
	/// <pre><i>WhileStatement</i>:
	/// <b>while</b> <b>(</b> Expression <b>)</b> Statement</pre>
	/// </remarks>
	public class WhileLoop : Loop
	{
		private AstNode condition;

		public WhileLoop()
		{
			{
				type = Token.WHILE;
			}
		}

		public WhileLoop(int pos) : base(pos)
		{
			{
				type = Token.WHILE;
			}
		}

		public WhileLoop(int pos, int len) : base(pos, len)
		{
			{
				type = Token.WHILE;
			}
		}

		/// <summary>Returns loop condition</summary>
		public virtual AstNode GetCondition()
		{
			return condition;
		}

		/// <summary>Sets loop condition</summary>
		/// <exception cref="System.ArgumentException">
		/// } if condition is
		/// <code>null</code>
		/// </exception>
		public virtual void SetCondition(AstNode condition)
		{
			AssertNotNull(condition);
			this.condition = condition;
			condition.SetParent(this);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("while (");
			sb.Append(condition.ToSource(0));
			sb.Append(") ");
			if (body.GetType() == Token.BLOCK)
			{
				sb.Append(body.ToSource(depth).Trim());
				sb.Append("\n");
			}
			else
			{
				sb.Append("\n").Append(body.ToSource(depth + 1));
			}
			return sb.ToString();
		}

		/// <summary>Visits this node, the condition, then the body.</summary>
		/// <remarks>Visits this node, the condition, then the body.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				condition.Visit(v);
				body.Visit(v);
			}
		}
	}
}
