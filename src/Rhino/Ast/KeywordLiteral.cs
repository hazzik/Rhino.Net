/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>
	/// AST node for keyword literals:  currently,
	/// <code>this</code>
	/// ,
	/// <code>null</code>
	/// ,
	/// <code>true</code>
	/// ,
	/// <code>false</code>
	/// , and
	/// <code>debugger</code>
	/// .
	/// Node type is one of
	/// <see cref="Rhino.Token.THIS">Rhino.Token.THIS</see>
	/// ,
	/// <see cref="Rhino.Token.NULL">Rhino.Token.NULL</see>
	/// ,
	/// <see cref="Rhino.Token.TRUE">Rhino.Token.TRUE</see>
	/// ,
	/// <see cref="Rhino.Token.FALSE">Rhino.Token.FALSE</see>
	/// , or
	/// <see cref="Rhino.Token.DEBUGGER">Rhino.Token.DEBUGGER</see>
	/// .
	/// </summary>
	public class KeywordLiteral : AstNode
	{
		public KeywordLiteral()
		{
		}

		public KeywordLiteral(int pos) : base(pos)
		{
		}

		public KeywordLiteral(int pos, int len) : base(pos, len)
		{
		}

		/// <summary>Constructs a new KeywordLiteral</summary>
		/// <param name="nodeType">the token type</param>
		public KeywordLiteral(int pos, int len, int nodeType) : base(pos, len)
		{
			SetType(nodeType);
		}

		/// <summary>Sets node token type</summary>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>nodeType</code>
		/// is unsupported
		/// </exception>
		public override Node SetType(int nodeType)
		{
			if (!(nodeType == Token.THIS || nodeType == Token.NULL || nodeType == Token.TRUE || nodeType == Token.FALSE || nodeType == Token.DEBUGGER))
			{
				throw new ArgumentException("Invalid node type: " + nodeType);
			}
			type = nodeType;
			return this;
		}

		/// <summary>
		/// Returns true if the token type is
		/// <see cref="Rhino.Token.TRUE">Rhino.Token.TRUE</see>
		/// or
		/// <see cref="Rhino.Token.FALSE">Rhino.Token.FALSE</see>
		/// .
		/// </summary>
		public virtual bool IsBooleanLiteral()
		{
			return type == Token.TRUE || type == Token.FALSE;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			switch (GetType())
			{
				case Token.THIS:
				{
					sb.Append("this");
					break;
				}

				case Token.NULL:
				{
					sb.Append("null");
					break;
				}

				case Token.TRUE:
				{
					sb.Append("true");
					break;
				}

				case Token.FALSE:
				{
					sb.Append("false");
					break;
				}

				case Token.DEBUGGER:
				{
					sb.Append("debugger;\n");
					break;
				}

				default:
				{
					throw new InvalidOperationException("Invalid keyword literal type: " + GetType());
				}
			}
			return sb.ToString();
		}

		/// <summary>Visits this node.</summary>
		/// <remarks>Visits this node.  There are no children to visit.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
