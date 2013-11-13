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
	/// <summary>AST node representing a parse error or a warning.</summary>
	/// <remarks>
	/// AST node representing a parse error or a warning.  Node type is
	/// <see cref="Rhino.Token.ERROR">Rhino.Token.ERROR</see>
	/// .<p>
	/// </remarks>
	public class ErrorNode : AstNode
	{
		private string message;

		public ErrorNode()
		{
			{
				type = Token.ERROR;
			}
		}

		public ErrorNode(int pos) : base(pos)
		{
			{
				type = Token.ERROR;
			}
		}

		public ErrorNode(int pos, int len) : base(pos, len)
		{
			{
				type = Token.ERROR;
			}
		}

		/// <summary>Returns error message key</summary>
		public virtual string GetMessage()
		{
			return message;
		}

		/// <summary>Sets error message key</summary>
		public virtual void SetMessage(string message)
		{
			this.message = message;
		}

		public override string ToSource(int depth)
		{
			return string.Empty;
		}

		/// <summary>
		/// Error nodes are not visited during normal visitor traversals,
		/// but comply with the
		/// <see cref="AstNode.Visit(NodeVisitor)">AstNode.Visit(NodeVisitor)</see>
		/// interface.
		/// </summary>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
