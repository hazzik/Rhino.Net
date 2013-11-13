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
	/// <summary>AST node for let statements and expressions.</summary>
	/// <remarks>
	/// AST node for let statements and expressions.
	/// Node type is
	/// <see cref="Rhino.Token.LET">Rhino.Token.LET</see>
	/// or
	/// <see cref="Rhino.Token.LETEXPR">Rhino.Token.LETEXPR</see>
	/// .<p>
	/// <pre> <i>LetStatement</i>:
	/// <b>let</b> ( VariableDeclarationList ) Block
	/// <i>LetExpression</i>:
	/// <b>let</b> ( VariableDeclarationList ) Expression</pre>
	/// Note that standalone let-statements with no parens or body block,
	/// such as
	/// <code>let x=6, y=7;</code>
	/// , are represented as a
	/// <see cref="VariableDeclaration">VariableDeclaration</see>
	/// node of type
	/// <code>Token.LET</code>
	/// ,
	/// wrapped with an
	/// <see cref="ExpressionStatement">ExpressionStatement</see>
	/// .<p>
	/// </remarks>
	public class LetNode : Scope
	{
		private VariableDeclaration variables;

		private AstNode body;

		private int lp = -1;

		private int rp = -1;

		public LetNode()
		{
			{
				type = Token.LETEXPR;
			}
		}

		public LetNode(int pos) : base(pos)
		{
			{
				type = Token.LETEXPR;
			}
		}

		public LetNode(int pos, int len) : base(pos, len)
		{
			{
				type = Token.LETEXPR;
			}
		}

		/// <summary>Returns variable list</summary>
		public virtual VariableDeclaration GetVariables()
		{
			return variables;
		}

		/// <summary>Sets variable list.</summary>
		/// <remarks>Sets variable list.  Sets list parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if variables is
		/// <code>null</code>
		/// </exception>
		public virtual void SetVariables(VariableDeclaration variables)
		{
			AssertNotNull(variables);
			this.variables = variables;
			variables.SetParent(this);
		}

		/// <summary>Returns body statement or expression.</summary>
		/// <remarks>
		/// Returns body statement or expression.  Body is
		/// <code>null</code>
		/// if the
		/// form of the let statement is similar to a VariableDeclaration, with no
		/// curly-brace.  (This form is used to define let-bound variables in the
		/// scope of the current block.)<p>
		/// </remarks>
		/// <returns>the body form</returns>
		public virtual AstNode GetBody()
		{
			return body;
		}

		/// <summary>Sets body statement or expression.</summary>
		/// <remarks>
		/// Sets body statement or expression.  Also sets the body parent to this
		/// node.
		/// </remarks>
		/// <param name="body">
		/// the body statement or expression.  May be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetBody(AstNode body)
		{
			this.body = body;
			if (body != null)
			{
				body.SetParent(this);
			}
		}

		/// <summary>Returns left paren position, -1 if missing</summary>
		public virtual int GetLp()
		{
			return lp;
		}

		/// <summary>Sets left paren position</summary>
		public virtual void SetLp(int lp)
		{
			this.lp = lp;
		}

		/// <summary>Returns right paren position, -1 if missing</summary>
		public virtual int GetRp()
		{
			return rp;
		}

		/// <summary>Sets right paren position</summary>
		public virtual void SetRp(int rp)
		{
			this.rp = rp;
		}

		/// <summary>Sets both paren positions</summary>
		public virtual void SetParens(int lp, int rp)
		{
			this.lp = lp;
			this.rp = rp;
		}

		public override string ToSource(int depth)
		{
			string pad = MakeIndent(depth);
			StringBuilder sb = new StringBuilder();
			sb.Append(pad);
			sb.Append("let (");
			PrintList(variables.GetVariables(), sb);
			sb.Append(") ");
			if (body != null)
			{
				sb.Append(body.ToSource(depth));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, the variable list, and if present, the body
		/// expression or statement.
		/// </summary>
		/// <remarks>
		/// Visits this node, the variable list, and if present, the body
		/// expression or statement.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				variables.Visit(v);
				if (body != null)
				{
					body.Visit(v);
				}
			}
		}
	}
}
