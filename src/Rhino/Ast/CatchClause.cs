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
	/// <summary>Node representing a catch-clause of a try-statement.</summary>
	/// <remarks>
	/// Node representing a catch-clause of a try-statement.
	/// Node type is
	/// <see cref="Rhino.Token.CATCH">Rhino.Token.CATCH</see>
	/// .
	/// <pre><i>CatchClause</i> :
	/// <b>catch</b> ( <i><b>Identifier</b></i> [<b>if</b> Expression] ) Block</pre>
	/// </remarks>
	public class CatchClause : AstNode
	{
		private Name varName;

		private AstNode catchCondition;

		private Block body;

		private int ifPosition = -1;

		private int lp = -1;

		private int rp = -1;

		public CatchClause()
		{
			{
				type = Token.CATCH;
			}
		}

		public CatchClause(int pos) : base(pos)
		{
			{
				type = Token.CATCH;
			}
		}

		public CatchClause(int pos, int len) : base(pos, len)
		{
			{
				type = Token.CATCH;
			}
		}

		/// <summary>Returns catch variable node</summary>
		/// <returns>catch variable</returns>
		public virtual Name GetVarName()
		{
			return varName;
		}

		/// <summary>Sets catch variable node, and sets its parent to this node.</summary>
		/// <remarks>Sets catch variable node, and sets its parent to this node.</remarks>
		/// <param name="varName">catch variable</param>
		/// <exception cref="System.ArgumentException">
		/// if varName is
		/// <code>null</code>
		/// </exception>
		public virtual void SetVarName(Name varName)
		{
			AssertNotNull(varName);
			this.varName = varName;
			varName.SetParent(this);
		}

		/// <summary>Returns catch condition node, if present</summary>
		/// <returns>
		/// catch condition node,
		/// <code>null</code>
		/// if not present
		/// </returns>
		public virtual AstNode GetCatchCondition()
		{
			return catchCondition;
		}

		/// <summary>Sets catch condition node, and sets its parent to this node.</summary>
		/// <remarks>Sets catch condition node, and sets its parent to this node.</remarks>
		/// <param name="catchCondition">
		/// catch condition node.  Can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetCatchCondition(AstNode catchCondition)
		{
			this.catchCondition = catchCondition;
			if (catchCondition != null)
			{
				catchCondition.SetParent(this);
			}
		}

		/// <summary>Returns catch body</summary>
		public virtual Block GetBody()
		{
			return body;
		}

		/// <summary>Sets catch body, and sets its parent to this node.</summary>
		/// <remarks>Sets catch body, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if body is
		/// <code>null</code>
		/// </exception>
		public virtual void SetBody(Block body)
		{
			AssertNotNull(body);
			this.body = body;
			body.SetParent(this);
		}

		/// <summary>Returns left paren position</summary>
		public virtual int GetLp()
		{
			return lp;
		}

		/// <summary>Sets left paren position</summary>
		public virtual void SetLp(int lp)
		{
			this.lp = lp;
		}

		/// <summary>Returns right paren position</summary>
		public virtual int GetRp()
		{
			return rp;
		}

		/// <summary>Sets right paren position</summary>
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

		/// <summary>Returns position of "if" keyword</summary>
		/// <returns>position of "if" keyword, if present, or -1</returns>
		public virtual int GetIfPosition()
		{
			return ifPosition;
		}

		/// <summary>Sets position of "if" keyword</summary>
		/// <param name="ifPosition">position of "if" keyword, if present, or -1</param>
		public virtual void SetIfPosition(int ifPosition)
		{
			this.ifPosition = ifPosition;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("catch (");
			sb.Append(varName.ToSource(0));
			if (catchCondition != null)
			{
				sb.Append(" if ");
				sb.Append(catchCondition.ToSource(0));
			}
			sb.Append(") ");
			sb.Append(body.ToSource(0));
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, the catch var name node, the condition if
		/// non-
		/// <code>null</code>
		/// , and the catch body.
		/// </summary>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				varName.Visit(v);
				if (catchCondition != null)
				{
					catchCondition.Visit(v);
				}
				body.Visit(v);
			}
		}
	}
}
