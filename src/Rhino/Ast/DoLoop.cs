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
	/// <summary>Do statement.</summary>
	/// <remarks>
	/// Do statement.  Node type is
	/// <see cref="Rhino.Token.DO">Rhino.Token.DO</see>
	/// .<p>
	/// <pre><i>DoLoop</i>:
	/// <b>do</b> Statement <b>while</b> <b>(</b> Expression <b>)</b> <b>;</b></pre>
	/// </remarks>
	public class DoLoop : Loop
	{
		private AstNode condition;

		private int whilePosition = -1;

		public DoLoop()
		{
			{
				type = Token.DO;
			}
		}

		public DoLoop(int pos) : base(pos)
		{
			{
				type = Token.DO;
			}
		}

		public DoLoop(int pos, int len) : base(pos, len)
		{
			{
				type = Token.DO;
			}
		}

		/// <summary>Returns loop condition</summary>
		public virtual AstNode GetCondition()
		{
			return condition;
		}

		/// <summary>Sets loop condition, and sets its parent to this node.</summary>
		/// <remarks>Sets loop condition, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">if condition is null</exception>
		public virtual void SetCondition(AstNode condition)
		{
			AssertNotNull(condition);
			this.condition = condition;
			condition.SetParent(this);
		}

		/// <summary>Returns source position of "while" keyword</summary>
		public virtual int GetWhilePosition()
		{
			return whilePosition;
		}

		/// <summary>Sets source position of "while" keyword</summary>
		public virtual void SetWhilePosition(int whilePosition)
		{
			this.whilePosition = whilePosition;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("do ");
			sb.Append(body.ToSource(depth).Trim());
			sb.Append(" while (");
			sb.Append(condition.ToSource(0));
			sb.Append(");\n");
			return sb.ToString();
		}

		/// <summary>Visits this node, the body, and then the while-expression.</summary>
		/// <remarks>Visits this node, the body, and then the while-expression.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				body.Visit(v);
				condition.Visit(v);
			}
		}
	}
}
