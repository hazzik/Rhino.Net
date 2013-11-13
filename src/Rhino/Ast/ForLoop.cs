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
	/// <summary>C-style for-loop statement.</summary>
	/// <remarks>
	/// C-style for-loop statement.
	/// Node type is
	/// <see cref="Rhino.Token.FOR">Rhino.Token.FOR</see>
	/// .<p>
	/// <pre><b>for</b> ( ExpressionNoInopt; Expressionopt ; Expressionopt ) Statement</pre>
	/// <pre><b>for</b> ( <b>var</b> VariableDeclarationListNoIn; Expressionopt ; Expressionopt ) Statement</pre>
	/// </remarks>
	public class ForLoop : Loop
	{
		private AstNode initializer;

		private AstNode condition;

		private AstNode increment;

		public ForLoop()
		{
			{
				type = Token.FOR;
			}
		}

		public ForLoop(int pos) : base(pos)
		{
			{
				type = Token.FOR;
			}
		}

		public ForLoop(int pos, int len) : base(pos, len)
		{
			{
				type = Token.FOR;
			}
		}

		/// <summary>Returns loop initializer variable declaration list.</summary>
		/// <remarks>
		/// Returns loop initializer variable declaration list.
		/// This is either a
		/// <see cref="VariableDeclaration">VariableDeclaration</see>
		/// , an
		/// <see cref="Assignment">Assignment</see>
		/// , or an
		/// <see cref="InfixExpression">InfixExpression</see>
		/// of
		/// type COMMA that chains multiple variable assignments.
		/// </remarks>
		public virtual AstNode GetInitializer()
		{
			return initializer;
		}

		/// <summary>
		/// Sets loop initializer expression, and sets its parent
		/// to this node.
		/// </summary>
		/// <remarks>
		/// Sets loop initializer expression, and sets its parent
		/// to this node.  Virtually any expression can be in the initializer,
		/// so no error-checking is done other than a
		/// <code>null</code>
		/// -check.
		/// </remarks>
		/// <param name="initializer">
		/// loop initializer.  Pass an
		/// <see cref="EmptyExpression">EmptyExpression</see>
		/// if the initializer is not specified.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// if condition is
		/// <code>null</code>
		/// </exception>
		public virtual void SetInitializer(AstNode initializer)
		{
			AssertNotNull(initializer);
			this.initializer = initializer;
			initializer.SetParent(this);
		}

		/// <summary>Returns loop condition</summary>
		public virtual AstNode GetCondition()
		{
			return condition;
		}

		/// <summary>Sets loop condition, and sets its parent to this node.</summary>
		/// <remarks>Sets loop condition, and sets its parent to this node.</remarks>
		/// <param name="condition">
		/// loop condition.  Pass an
		/// <see cref="EmptyExpression">EmptyExpression</see>
		/// if the condition is missing.
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// } if condition is
		/// <code>null</code>
		/// </exception>
		public virtual void SetCondition(AstNode condition)
		{
			AssertNotNull(condition);
			this.condition = condition;
			condition.SetParent(this);
		}

		/// <summary>Returns loop increment expression</summary>
		public virtual AstNode GetIncrement()
		{
			return increment;
		}

		/// <summary>
		/// Sets loop increment expression, and sets its parent to
		/// this node.
		/// </summary>
		/// <remarks>
		/// Sets loop increment expression, and sets its parent to
		/// this node.
		/// </remarks>
		/// <param name="increment">
		/// loop increment expression.  Pass an
		/// <see cref="EmptyExpression">EmptyExpression</see>
		/// if increment is
		/// <code>null</code>
		/// .
		/// </param>
		/// <exception cref="System.ArgumentException">
		/// } if increment is
		/// <code>null</code>
		/// </exception>
		public virtual void SetIncrement(AstNode increment)
		{
			AssertNotNull(increment);
			this.increment = increment;
			increment.SetParent(this);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("for (");
			sb.Append(initializer.ToSource(0));
			sb.Append("; ");
			sb.Append(condition.ToSource(0));
			sb.Append("; ");
			sb.Append(increment.ToSource(0));
			sb.Append(") ");
			if (body.GetType() == Token.BLOCK)
			{
				sb.Append(body.ToSource(depth).Trim()).Append("\n");
			}
			else
			{
				sb.Append("\n").Append(body.ToSource(depth + 1));
			}
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, the initializer expression, the loop condition
		/// expression, the increment expression, and then the loop body.
		/// </summary>
		/// <remarks>
		/// Visits this node, the initializer expression, the loop condition
		/// expression, the increment expression, and then the loop body.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				initializer.Visit(v);
				condition.Visit(v);
				increment.Visit(v);
				body.Visit(v);
			}
		}
	}
}
