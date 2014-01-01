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
	/// <summary>Switch statement AST node type.</summary>
	/// <remarks>
	/// Switch statement AST node type.
	/// Node type is
	/// <see cref="Rhino.Token.SWITCH">Rhino.Token.SWITCH</see>
	/// .<p>
	/// <pre><i>SwitchStatement</i> :
	/// <b>switch</b> ( Expression ) CaseBlock
	/// <i>CaseBlock</i> :
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
	public class SwitchStatement : Jump
	{
		private static readonly IList<SwitchCase> NO_CASES = new List<SwitchCase>().AsReadOnly();

		private AstNode expression;

		private IList<SwitchCase> cases;

		private int lp = -1;

		private int rp = -1;

		public SwitchStatement()
		{
			{
				type = Token.SWITCH;
			}
		}

		public SwitchStatement(int pos)
		{
			{
				type = Token.SWITCH;
			}
			// can't call super (Jump) for historical reasons
			position = pos;
		}

		public SwitchStatement(int pos, int len)
		{
			{
				type = Token.SWITCH;
			}
			position = pos;
			length = len;
		}

		/// <summary>Returns the switch discriminant expression</summary>
		public virtual AstNode GetExpression()
		{
			return expression;
		}

		/// <summary>
		/// Sets the switch discriminant expression, and sets its parent
		/// to this node.
		/// </summary>
		/// <remarks>
		/// Sets the switch discriminant expression, and sets its parent
		/// to this node.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// } if expression is
		/// <code>null</code>
		/// </exception>
		public virtual void SetExpression(AstNode expression)
		{
			AssertNotNull(expression);
			this.expression = expression;
			expression.SetParent(this);
		}

		/// <summary>Returns case statement list.</summary>
		/// <remarks>
		/// Returns case statement list.  If there are no cases,
		/// returns an immutable empty list.
		/// </remarks>
		public virtual IList<SwitchCase> GetCases()
		{
			return cases != null ? cases : NO_CASES;
		}

		/// <summary>
		/// Sets case statement list, and sets the parent of each child
		/// case to this node.
		/// </summary>
		/// <remarks>
		/// Sets case statement list, and sets the parent of each child
		/// case to this node.
		/// </remarks>
		/// <param name="cases">
		/// list, which may be
		/// <code>null</code>
		/// to remove all the cases
		/// </param>
		public virtual void SetCases(IList<SwitchCase> cases)
		{
			if (cases == null)
			{
				this.cases = null;
			}
			else
			{
				if (this.cases != null)
				{
					this.cases.Clear();
				}
				foreach (SwitchCase sc in cases)
				{
					AddCase(sc);
				}
			}
		}

		/// <summary>Adds a switch case statement to the end of the list.</summary>
		/// <remarks>Adds a switch case statement to the end of the list.</remarks>
		/// <exception cref="System.ArgumentException">
		/// } if switchCase is
		/// <code>null</code>
		/// </exception>
		public virtual void AddCase(SwitchCase switchCase)
		{
			AssertNotNull(switchCase);
			if (cases == null)
			{
				cases = new List<SwitchCase>();
			}
			cases.Add(switchCase);
			switchCase.SetParent(this);
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
			sb.Append("switch (");
			sb.Append(expression.ToSource(0));
			sb.Append(") {\n");
			foreach (SwitchCase sc in cases)
			{
				sb.Append(sc.ToSource(depth + 1));
			}
			sb.Append(pad);
			sb.Append("}\n");
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then the switch-expression, then the cases
		/// in lexical order.
		/// </summary>
		/// <remarks>
		/// Visits this node, then the switch-expression, then the cases
		/// in lexical order.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				expression.Visit(v);
				foreach (SwitchCase sc in GetCases())
				{
					sc.Visit(v);
				}
			}
		}
	}
}
