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
	/// <summary>
	/// AST node representing an E4X
	/// <code>foo.(bar)</code>
	/// query expression.
	/// The node type (operator) is
	/// <see cref="Rhino.Token.DOTQUERY">Rhino.Token.DOTQUERY</see>
	/// .
	/// Its
	/// <code>getLeft</code>
	/// node is the target ("foo" in the example),
	/// and the
	/// <code>getRight</code>
	/// node is the filter expression node.<p>
	/// This class exists separately from
	/// <see cref="InfixExpression">InfixExpression</see>
	/// largely because it
	/// has different printing needs.  The position of the left paren is just after
	/// the dot (operator) position, and the right paren is the final position in the
	/// bounds of the node.  If the right paren is missing, the node ends at the end
	/// of the filter expression.
	/// </summary>
	public class XmlDotQuery : InfixExpression
	{
		private int rp = -1;

		public XmlDotQuery()
		{
			{
				type = Token.DOTQUERY;
			}
		}

		public XmlDotQuery(int pos) : base(pos)
		{
			{
				type = Token.DOTQUERY;
			}
		}

		public XmlDotQuery(int pos, int len) : base(pos, len)
		{
			{
				type = Token.DOTQUERY;
			}
		}

		/// <summary>
		/// Returns right-paren position, -1 if missing.<p>
		/// Note that the left-paren is automatically the character
		/// immediately after the "." in the operator - no whitespace is
		/// permitted between the dot and lp by the scanner.
		/// </summary>
		/// <remarks>
		/// Returns right-paren position, -1 if missing.<p>
		/// Note that the left-paren is automatically the character
		/// immediately after the "." in the operator - no whitespace is
		/// permitted between the dot and lp by the scanner.
		/// </remarks>
		public virtual int GetRp()
		{
			return rp;
		}

		/// <summary>Sets right-paren position</summary>
		public virtual void SetRp(int rp)
		{
			this.rp = rp;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(GetLeft().ToSource(0));
			sb.Append(".(");
			sb.Append(GetRight().ToSource(0));
			sb.Append(")");
			return sb.ToString();
		}
	}
}
