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
	/// <summary>
	/// AST node for JavaScript 1.7
	/// <code>yield</code>
	/// expression or statement.
	/// Node type is
	/// <see cref="Rhino.Token.YIELD">Rhino.Token.YIELD</see>
	/// .<p>
	/// <pre><i>Yield</i> :
	/// <b>yield</b> [<i>no LineTerminator here</i>] [non-paren Expression] ;</pre>
	/// </summary>
	public class Yield : AstNode
	{
		private AstNode value;

		public Yield()
		{
			{
				type = Token.YIELD;
			}
		}

		public Yield(int pos) : base(pos)
		{
			{
				type = Token.YIELD;
			}
		}

		public Yield(int pos, int len) : base(pos, len)
		{
			{
				type = Token.YIELD;
			}
		}

		public Yield(int pos, int len, AstNode value) : base(pos, len)
		{
			{
				type = Token.YIELD;
			}
			SetValue(value);
		}

		/// <summary>
		/// Returns yielded expression,
		/// <code>null</code>
		/// if none
		/// </summary>
		public virtual AstNode GetValue()
		{
			return value;
		}

		/// <summary>Sets yielded expression, and sets its parent to this node.</summary>
		/// <remarks>Sets yielded expression, and sets its parent to this node.</remarks>
		/// <param name="expr">
		/// the value to yield. Can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetValue(AstNode expr)
		{
			this.value = expr;
			if (expr != null)
			{
				expr.SetParent(this);
			}
		}

		public override string ToSource(int depth)
		{
			return value == null ? "yield" : "yield " + value.ToSource(0);
		}

		/// <summary>Visits this node, and if present, the yielded value.</summary>
		/// <remarks>Visits this node, and if present, the yielded value.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this) && value != null)
			{
				value.Visit(v);
			}
		}
	}
}
