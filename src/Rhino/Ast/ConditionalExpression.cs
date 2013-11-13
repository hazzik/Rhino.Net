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
	/// <summary>AST node representing the ternary operator.</summary>
	/// <remarks>
	/// AST node representing the ternary operator.  Node type is
	/// <see cref="Rhino.Token.HOOK">Rhino.Token.HOOK</see>
	/// .
	/// <pre><i>ConditionalExpression</i> :
	/// LogicalORExpression
	/// LogicalORExpression ? AssignmentExpression
	/// : AssignmentExpression</pre>
	/// <i>ConditionalExpressionNoIn</i> :
	/// LogicalORExpressionNoIn
	/// LogicalORExpressionNoIn ? AssignmentExpression
	/// : AssignmentExpressionNoIn</pre>
	/// </remarks>
	public class ConditionalExpression : AstNode
	{
		private AstNode testExpression;

		private AstNode trueExpression;

		private AstNode falseExpression;

		private int questionMarkPosition = -1;

		private int colonPosition = -1;

		public ConditionalExpression()
		{
			{
				type = Token.HOOK;
			}
		}

		public ConditionalExpression(int pos) : base(pos)
		{
			{
				type = Token.HOOK;
			}
		}

		public ConditionalExpression(int pos, int len) : base(pos, len)
		{
			{
				type = Token.HOOK;
			}
		}

		/// <summary>Returns test expression</summary>
		public virtual AstNode GetTestExpression()
		{
			return testExpression;
		}

		/// <summary>Sets test expression, and sets its parent.</summary>
		/// <remarks>Sets test expression, and sets its parent.</remarks>
		/// <param name="testExpression">test expression</param>
		/// <exception cref="System.ArgumentException">
		/// if testExpression is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTestExpression(AstNode testExpression)
		{
			AssertNotNull(testExpression);
			this.testExpression = testExpression;
			testExpression.SetParent(this);
		}

		/// <summary>Returns expression to evaluate if test is true</summary>
		public virtual AstNode GetTrueExpression()
		{
			return trueExpression;
		}

		/// <summary>
		/// Sets expression to evaluate if test is true, and
		/// sets its parent to this node.
		/// </summary>
		/// <remarks>
		/// Sets expression to evaluate if test is true, and
		/// sets its parent to this node.
		/// </remarks>
		/// <param name="trueExpression">expression to evaluate if test is true</param>
		/// <exception cref="System.ArgumentException">
		/// if expression is
		/// <code>null</code>
		/// </exception>
		public virtual void SetTrueExpression(AstNode trueExpression)
		{
			AssertNotNull(trueExpression);
			this.trueExpression = trueExpression;
			trueExpression.SetParent(this);
		}

		/// <summary>Returns expression to evaluate if test is false</summary>
		public virtual AstNode GetFalseExpression()
		{
			return falseExpression;
		}

		/// <summary>
		/// Sets expression to evaluate if test is false, and sets its
		/// parent to this node.
		/// </summary>
		/// <remarks>
		/// Sets expression to evaluate if test is false, and sets its
		/// parent to this node.
		/// </remarks>
		/// <param name="falseExpression">expression to evaluate if test is false</param>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>falseExpression</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetFalseExpression(AstNode falseExpression)
		{
			AssertNotNull(falseExpression);
			this.falseExpression = falseExpression;
			falseExpression.SetParent(this);
		}

		/// <summary>Returns position of ? token</summary>
		public virtual int GetQuestionMarkPosition()
		{
			return questionMarkPosition;
		}

		/// <summary>Sets position of ? token</summary>
		/// <param name="questionMarkPosition">position of ? token</param>
		public virtual void SetQuestionMarkPosition(int questionMarkPosition)
		{
			this.questionMarkPosition = questionMarkPosition;
		}

		/// <summary>Returns position of : token</summary>
		public virtual int GetColonPosition()
		{
			return colonPosition;
		}

		/// <summary>Sets position of : token</summary>
		/// <param name="colonPosition">position of : token</param>
		public virtual void SetColonPosition(int colonPosition)
		{
			this.colonPosition = colonPosition;
		}

		public override bool HasSideEffects()
		{
			if (testExpression == null || trueExpression == null || falseExpression == null)
			{
				CodeBug();
			}
			return trueExpression.HasSideEffects() && falseExpression.HasSideEffects();
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(testExpression.ToSource(depth));
			sb.Append(" ? ");
			sb.Append(trueExpression.ToSource(0));
			sb.Append(" : ");
			sb.Append(falseExpression.ToSource(0));
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then the test-expression, the true-expression,
		/// and the false-expression.
		/// </summary>
		/// <remarks>
		/// Visits this node, then the test-expression, the true-expression,
		/// and the false-expression.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				testExpression.Visit(v);
				trueExpression.Visit(v);
				falseExpression.Visit(v);
			}
		}
	}
}
