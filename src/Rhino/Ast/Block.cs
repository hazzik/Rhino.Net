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
	/// <summary>A block statement delimited by curly braces.</summary>
	/// <remarks>
	/// A block statement delimited by curly braces.  The node position is the
	/// position of the open-curly, and the length extends to the position of
	/// the close-curly.  Node type is
	/// <see cref="Rhino.Token.BLOCK">Rhino.Token.BLOCK</see>
	/// .
	/// <pre><i>Block</i> :
	/// <b>{</b> Statement* <b>}</b></pre>
	/// </remarks>
	public class Block : AstNode
	{
		public Block()
		{
			{
				this.type = Token.BLOCK;
			}
		}

		public Block(int pos) : base(pos)
		{
			{
				this.type = Token.BLOCK;
			}
		}

		public Block(int pos, int len) : base(pos, len)
		{
			{
				this.type = Token.BLOCK;
			}
		}

		/// <summary>
		/// Alias for
		/// <see cref="AstNode.AddChild(AstNode)">AstNode.AddChild(AstNode)</see>
		/// .
		/// </summary>
		public virtual void AddStatement(AstNode statement)
		{
			AddChild(statement);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("{\n");
			foreach (Node kid in this)
			{
				sb.Append(((AstNode)kid).ToSource(depth + 1));
			}
			sb.Append(MakeIndent(depth));
			sb.Append("}\n");
			return sb.ToString();
		}

		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				foreach (Node kid in this)
				{
					((AstNode)kid).Visit(v);
				}
			}
		}
	}
}
