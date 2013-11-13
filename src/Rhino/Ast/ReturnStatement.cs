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
	/// <summary>Return statement.</summary>
	/// <remarks>
	/// Return statement.  Node type is
	/// <see cref="Rhino.Token.RETURN">Rhino.Token.RETURN</see>
	/// .<p>
	/// <pre><i>ReturnStatement</i> :
	/// <b>return</b> [<i>no LineTerminator here</i>] [Expression] ;</pre>
	/// </remarks>
	public class ReturnStatement : AstNode
	{
		private AstNode returnValue;

		public ReturnStatement()
		{
			{
				type = Token.RETURN;
			}
		}

		public ReturnStatement(int pos) : base(pos)
		{
			{
				type = Token.RETURN;
			}
		}

		public ReturnStatement(int pos, int len) : base(pos, len)
		{
			{
				type = Token.RETURN;
			}
		}

		public ReturnStatement(int pos, int len, AstNode returnValue) : base(pos, len)
		{
			{
				type = Token.RETURN;
			}
			SetReturnValue(returnValue);
		}

		/// <summary>
		/// Returns return value,
		/// <code>null</code>
		/// if return value is void
		/// </summary>
		public virtual AstNode GetReturnValue()
		{
			return returnValue;
		}

		/// <summary>Sets return value expression, and sets its parent to this node.</summary>
		/// <remarks>
		/// Sets return value expression, and sets its parent to this node.
		/// Can be
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual void SetReturnValue(AstNode returnValue)
		{
			this.returnValue = returnValue;
			if (returnValue != null)
			{
				returnValue.SetParent(this);
			}
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("return");
			if (returnValue != null)
			{
				sb.Append(" ");
				sb.Append(returnValue.ToSource(0));
			}
			sb.Append(";\n");
			return sb.ToString();
		}

		/// <summary>Visits this node, then the return value if specified.</summary>
		/// <remarks>Visits this node, then the return value if specified.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this) && returnValue != null)
			{
				returnValue.Visit(v);
			}
		}
	}
}
