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
	/// <summary>AST node for a RegExp literal.</summary>
	/// <remarks>
	/// AST node for a RegExp literal.
	/// Node type is
	/// <see cref="Rhino.Token.REGEXP">Rhino.Token.REGEXP</see>
	/// .<p>
	/// </remarks>
	public class RegExpLiteral : AstNode
	{
		private string value;

		private string flags;

		public RegExpLiteral()
		{
			{
				type = Token.REGEXP;
			}
		}

		public RegExpLiteral(int pos) : base(pos)
		{
			{
				type = Token.REGEXP;
			}
		}

		public RegExpLiteral(int pos, int len) : base(pos, len)
		{
			{
				type = Token.REGEXP;
			}
		}

		/// <summary>Returns the regexp string without delimiters</summary>
		public virtual string GetValue()
		{
			return value;
		}

		/// <summary>Sets the regexp string without delimiters</summary>
		/// <exception cref="System.ArgumentException">
		/// } if value is
		/// <code>null</code>
		/// </exception>
		public virtual void SetValue(string value)
		{
			AssertNotNull(value);
			this.value = value;
		}

		/// <summary>
		/// Returns regexp flags,
		/// <code>null</code>
		/// or "" if no flags specified
		/// </summary>
		public virtual string GetFlags()
		{
			return flags;
		}

		/// <summary>Sets regexp flags.</summary>
		/// <remarks>
		/// Sets regexp flags.  Can be
		/// <code>null</code>
		/// or "".
		/// </remarks>
		public virtual void SetFlags(string flags)
		{
			this.flags = flags;
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + "/" + value + "/" + (flags == null ? string.Empty : flags);
		}

		/// <summary>Visits this node.</summary>
		/// <remarks>Visits this node.  There are no children to visit.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
