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
	/// <summary>A break statement.</summary>
	/// <remarks>
	/// A break statement.  Node type is
	/// <see cref="Rhino.Token.BREAK">Rhino.Token.BREAK</see>
	/// .<p>
	/// <pre><i>BreakStatement</i> :
	/// <b>break</b> [<i>no LineTerminator here</i>] [Identifier] ;</pre>
	/// </remarks>
	public class BreakStatement : Jump
	{
		private Name breakLabel;

		private AstNode target;

		public BreakStatement()
		{
			{
				type = Token.BREAK;
			}
		}

		public BreakStatement(int pos)
		{
			{
				type = Token.BREAK;
			}
			// can't call super (Jump) for historical reasons
			position = pos;
		}

		public BreakStatement(int pos, int len)
		{
			{
				type = Token.BREAK;
			}
			position = pos;
			length = len;
		}

		/// <summary>Returns the intended label of this break statement</summary>
		/// <returns>
		/// the break label.
		/// <code>null</code>
		/// if the source code did
		/// not specify a specific break label via "break &lt;target&gt;".
		/// </returns>
		public virtual Name GetBreakLabel()
		{
			return breakLabel;
		}

		/// <summary>Sets the intended label of this break statement, e.g.</summary>
		/// <remarks>
		/// Sets the intended label of this break statement, e.g.  'foo'
		/// in "break foo". Also sets the parent of the label to this node.
		/// </remarks>
		/// <param name="label">
		/// the break label, or
		/// <code>null</code>
		/// if the statement is
		/// just the "break" keyword by itself.
		/// </param>
		public virtual void SetBreakLabel(Name label)
		{
			breakLabel = label;
			if (label != null)
			{
				label.SetParent(this);
			}
		}

		/// <summary>Returns the statement to break to</summary>
		/// <returns>
		/// the break target.  Only
		/// <code>null</code>
		/// if the source
		/// code has an error in it.
		/// </returns>
		public virtual AstNode GetBreakTarget()
		{
			return target;
		}

		/// <summary>Sets the statement to break to.</summary>
		/// <remarks>Sets the statement to break to.</remarks>
		/// <param name="target">the statement to break to</param>
		/// <exception cref="System.ArgumentException">
		/// if target is
		/// <code>null</code>
		/// </exception>
		public virtual void SetBreakTarget(Jump target)
		{
			AssertNotNull(target);
			this.target = target;
			SetJumpStatement(target);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("break");
			if (breakLabel != null)
			{
				sb.Append(" ");
				sb.Append(breakLabel.ToSource(0));
			}
			sb.Append(";\n");
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then visits the break label if non-
		/// <code>null</code>
		/// .
		/// </summary>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this) && breakLabel != null)
			{
				breakLabel.Visit(v);
			}
		}
	}
}
