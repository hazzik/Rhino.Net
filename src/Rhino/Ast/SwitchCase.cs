/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>Switch-case AST node type.</summary>
	/// <remarks>
	/// Switch-case AST node type.  The switch case is always part of a
	/// switch statement.
	/// Node type is
	/// <see cref="Rhino.Token.CASE">Rhino.Token.CASE</see>
	/// .<p>
	/// <pre><i>CaseBlock</i> :
	/// { [CaseClauses] }
	/// { [CaseClauses] DefaultClause [CaseClauses] }
	/// <i>CaseClauses</i> :
	/// CaseClause
	/// CaseClauses CaseClause
	/// <i>CaseClause</i> :
	/// <b>case</b> Expression : [StatementList]
	/// <i>DefaultClause</i> :
	/// <b>default</b> : [StatementList]</pre>
	/// </remarks>
	public class SwitchCase : AstNode
	{
		private AstNode expression;

		private IList<AstNode> statements;

		public SwitchCase()
		{
			{
				type = Token.CASE;
			}
		}

		public SwitchCase(int pos) : base(pos)
		{
			{
				type = Token.CASE;
			}
		}

		public SwitchCase(int pos, int len) : base(pos, len)
		{
			{
				type = Token.CASE;
			}
		}

		/// <summary>
		/// Returns the case expression,
		/// <code>null</code>
		/// for default case
		/// </summary>
		public virtual AstNode GetExpression()
		{
			return expression;
		}

		/// <summary>
		/// Sets the case expression,
		/// <code>null</code>
		/// for default case.
		/// Note that for empty fall-through cases, they still have
		/// a case expression.  In
		/// <code>case 0: case 1: break;</code>
		/// the
		/// first case has an
		/// <code>expression</code>
		/// that is a
		/// <see cref="NumberLiteral">NumberLiteral</see>
		/// with value
		/// <code>0</code>
		/// .
		/// </summary>
		public virtual void SetExpression(AstNode expression)
		{
			this.expression = expression;
			if (expression != null)
			{
				expression.SetParent(this);
			}
		}

		/// <summary>Return true if this is a default case.</summary>
		/// <remarks>Return true if this is a default case.</remarks>
		/// <returns>
		/// true if
		/// <see cref="GetExpression()">GetExpression()</see>
		/// would return
		/// <code>null</code>
		/// </returns>
		public virtual bool IsDefault()
		{
			return expression == null;
		}

		/// <summary>
		/// Returns statement list, which may be
		/// <code>null</code>
		/// .
		/// </summary>
		public virtual IList<AstNode> GetStatements()
		{
			return statements;
		}

		/// <summary>Sets statement list.</summary>
		/// <remarks>
		/// Sets statement list.  May be
		/// <code>null</code>
		/// .  Replaces any existing
		/// statements.  Each element in the list has its parent set to this node.
		/// </remarks>
		public virtual void SetStatements(IList<AstNode> statements)
		{
			if (this.statements != null)
			{
				this.statements.Clear();
			}
			foreach (AstNode s in statements)
			{
				AddStatement(s);
			}
		}

		/// <summary>Adds a statement to the end of the statement list.</summary>
		/// <remarks>
		/// Adds a statement to the end of the statement list.
		/// Sets the parent of the new statement to this node, updates
		/// its start offset to be relative to this node, and sets the
		/// length of this node to include the new child.
		/// </remarks>
		/// <param name="statement">a child statement</param>
		/// <exception cref="System.ArgumentException">
		/// } if statement is
		/// <code>null</code>
		/// </exception>
		public virtual void AddStatement(AstNode statement)
		{
			AssertNotNull(statement);
			if (statements == null)
			{
				statements = new List<AstNode>();
			}
			int end = statement.GetPosition() + statement.GetLength();
			this.SetLength(end - this.GetPosition());
			statements.AddItem(statement);
			statement.SetParent(this);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			if (expression == null)
			{
				sb.Append("default:\n");
			}
			else
			{
				sb.Append("case ");
				sb.Append(expression.ToSource(0));
				sb.Append(":\n");
			}
			if (statements != null)
			{
				foreach (AstNode s in statements)
				{
					sb.Append(s.ToSource(depth + 1));
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then the case expression if present, then
		/// each statement (if any are specified).
		/// </summary>
		/// <remarks>
		/// Visits this node, then the case expression if present, then
		/// each statement (if any are specified).
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				if (expression != null)
				{
					expression.Visit(v);
				}
				if (statements != null)
				{
					foreach (AstNode s in statements)
					{
						s.Visit(v);
					}
				}
			}
		}
	}
}
