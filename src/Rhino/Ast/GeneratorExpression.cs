/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	public class GeneratorExpression : Scope
	{
		private AstNode result;

		private IList<GeneratorExpressionLoop> loops = new AList<GeneratorExpressionLoop>();

		private AstNode filter;

		private int ifPosition = -1;

		private int lp = -1;

		private int rp = -1;

		public GeneratorExpression()
		{
			{
				type = Token.GENEXPR;
			}
		}

		public GeneratorExpression(int pos) : base(pos)
		{
			{
				type = Token.GENEXPR;
			}
		}

		public GeneratorExpression(int pos, int len) : base(pos, len)
		{
			{
				type = Token.GENEXPR;
			}
		}

		/// <summary>Returns result expression node (just after opening bracket)</summary>
		public virtual AstNode GetResult()
		{
			return result;
		}

		/// <summary>Sets result expression, and sets its parent to this node.</summary>
		/// <remarks>Sets result expression, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if result is
		/// <code>null</code>
		/// </exception>
		public virtual void SetResult(AstNode result)
		{
			AssertNotNull(result);
			this.result = result;
			result.SetParent(this);
		}

		/// <summary>Returns loop list</summary>
		public virtual IList<GeneratorExpressionLoop> GetLoops()
		{
			return loops;
		}

		/// <summary>Sets loop list</summary>
		/// <exception cref="System.ArgumentException">
		/// if loops is
		/// <code>null</code>
		/// </exception>
		public virtual void SetLoops(IList<GeneratorExpressionLoop> loops)
		{
			AssertNotNull(loops);
			this.loops.Clear();
			foreach (GeneratorExpressionLoop acl in loops)
			{
				AddLoop(acl);
			}
		}

		/// <summary>Adds a child loop node, and sets its parent to this node.</summary>
		/// <remarks>Adds a child loop node, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if acl is
		/// <code>null</code>
		/// </exception>
		public virtual void AddLoop(GeneratorExpressionLoop acl)
		{
			AssertNotNull(acl);
			loops.AddItem(acl);
			acl.SetParent(this);
		}

		/// <summary>
		/// Returns filter expression, or
		/// <code>null</code>
		/// if not present
		/// </summary>
		public virtual AstNode GetFilter()
		{
			return filter;
		}

		/// <summary>Sets filter expression, and sets its parent to this node.</summary>
		/// <remarks>
		/// Sets filter expression, and sets its parent to this node.
		/// Can be
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual void SetFilter(AstNode filter)
		{
			this.filter = filter;
			if (filter != null)
			{
				filter.SetParent(this);
			}
		}

		/// <summary>Returns position of 'if' keyword, -1 if not present</summary>
		public virtual int GetIfPosition()
		{
			return ifPosition;
		}

		/// <summary>Sets position of 'if' keyword</summary>
		public virtual void SetIfPosition(int ifPosition)
		{
			this.ifPosition = ifPosition;
		}

		/// <summary>Returns filter left paren position, or -1 if no filter</summary>
		public virtual int GetFilterLp()
		{
			return lp;
		}

		/// <summary>Sets filter left paren position, or -1 if no filter</summary>
		public virtual void SetFilterLp(int lp)
		{
			this.lp = lp;
		}

		/// <summary>Returns filter right paren position, or -1 if no filter</summary>
		public virtual int GetFilterRp()
		{
			return rp;
		}

		/// <summary>Sets filter right paren position, or -1 if no filter</summary>
		public virtual void SetFilterRp(int rp)
		{
			this.rp = rp;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder(250);
			sb.Append("(");
			sb.Append(result.ToSource(0));
			foreach (GeneratorExpressionLoop loop in loops)
			{
				sb.Append(loop.ToSource(0));
			}
			if (filter != null)
			{
				sb.Append(" if (");
				sb.Append(filter.ToSource(0));
				sb.Append(")");
			}
			sb.Append(")");
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, the result expression, the loops, and the optional
		/// filter.
		/// </summary>
		/// <remarks>
		/// Visits this node, the result expression, the loops, and the optional
		/// filter.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (!v.Visit(this))
			{
				return;
			}
			result.Visit(v);
			foreach (GeneratorExpressionLoop loop in loops)
			{
				loop.Visit(v);
			}
			if (filter != null)
			{
				filter.Visit(v);
			}
		}
	}
}
