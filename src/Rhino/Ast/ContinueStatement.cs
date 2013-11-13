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
	/// <summary>A continue statement.</summary>
	/// <remarks>
	/// A continue statement.
	/// Node type is
	/// <see cref="Rhino.Token.CONTINUE">Rhino.Token.CONTINUE</see>
	/// .<p>
	/// <pre><i>ContinueStatement</i> :
	/// <b>continue</b> [<i>no LineTerminator here</i>] [Identifier] ;</pre>
	/// </remarks>
	public class ContinueStatement : Jump
	{
		private Name label;

		private Loop target;

		public ContinueStatement()
		{
			{
				type = Token.CONTINUE;
			}
		}

		public ContinueStatement(int pos) : this(pos, -1)
		{
		}

		public ContinueStatement(int pos, int len)
		{
			{
				type = Token.CONTINUE;
			}
			// can't call super (Jump) for historical reasons
			position = pos;
			length = len;
		}

		public ContinueStatement(Name label)
		{
			{
				type = Token.CONTINUE;
			}
			SetLabel(label);
		}

		public ContinueStatement(int pos, Name label) : this(pos)
		{
			SetLabel(label);
		}

		public ContinueStatement(int pos, int len, Name label) : this(pos, len)
		{
			SetLabel(label);
		}

		/// <summary>Returns continue target</summary>
		public virtual Loop GetTarget()
		{
			return target;
		}

		/// <summary>Sets continue target.</summary>
		/// <remarks>
		/// Sets continue target.  Does NOT set the parent of the target node:
		/// the target node is an ancestor of this node.
		/// </remarks>
		/// <param name="target">continue target</param>
		/// <exception cref="System.ArgumentException">
		/// if target is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTarget(Loop target)
		{
			AssertNotNull(target);
			this.target = target;
			SetJumpStatement(target);
		}

		/// <summary>Returns the intended label of this continue statement</summary>
		/// <returns>
		/// the continue label.  Will be
		/// <code>null</code>
		/// if the statement
		/// consisted only of the keyword "continue".
		/// </returns>
		public virtual Name GetLabel()
		{
			return label;
		}

		/// <summary>Sets the intended label of this continue statement.</summary>
		/// <remarks>
		/// Sets the intended label of this continue statement.
		/// Only applies if the statement was of the form "continue &lt;label&gt;".
		/// </remarks>
		/// <param name="label">
		/// the continue label, or
		/// <code>null</code>
		/// if not present.
		/// </param>
		public virtual void SetLabel(Name label)
		{
			this.label = label;
			if (label != null)
			{
				label.SetParent(this);
			}
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("continue");
			if (label != null)
			{
				sb.Append(" ");
				sb.Append(label.ToSource(0));
			}
			sb.Append(";\n");
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then visits the label if non-
		/// <code>null</code>
		/// .
		/// </summary>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this) && label != null)
			{
				label.Visit(v);
			}
		}
	}
}
