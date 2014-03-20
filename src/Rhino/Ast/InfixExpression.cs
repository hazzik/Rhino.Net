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
	/// <summary>AST node representing an infix (binary operator) expression.</summary>
	/// <remarks>
	/// AST node representing an infix (binary operator) expression.
	/// The operator is the node's
	/// <see cref="Rhino.Token">Rhino.Token</see>
	/// type.
	/// </remarks>
	public class InfixExpression : AstNode
	{
		protected internal AstNode left;

		protected internal AstNode right;

		protected internal int operatorPosition = -1;

		public InfixExpression()
		{
		}

		public InfixExpression(int pos) : base(pos)
		{
		}

		public InfixExpression(int pos, int len) : base(pos, len)
		{
		}

		public InfixExpression(int pos, int len, AstNode left, AstNode right) : base(pos, len)
		{
			SetLeft(left);
			SetRight(right);
		}

		/// <summary>
		/// Constructs a new
		/// <code>InfixExpression</code>
		/// .  Updates bounds to include
		/// left and right nodes.
		/// </summary>
		public InfixExpression(AstNode left, AstNode right)
		{
			SetLeftAndRight(left, right);
		}

		/// <summary>
		/// Constructs a new
		/// <code>InfixExpression</code>
		/// .
		/// </summary>
		/// <param name="operatorPos">the <em>absolute</em> position of the operator</param>
		public InfixExpression(int @operator, AstNode left, AstNode right, int operatorPos)
		{
			SetType(@operator);
			SetOperatorPosition(operatorPos - left.Position);
			SetLeftAndRight(left, right);
		}

		public virtual void SetLeftAndRight(AstNode left, AstNode right)
		{
			AssertNotNull(left);
			AssertNotNull(right);
			// compute our bounds while children have absolute positions
			int beg = left.Position;
			int end = right.Position + right.GetLength();
			SetBounds(beg, end);
			// this updates their positions to be parent-relative
			SetLeft(left);
			SetRight(right);
		}

		/// <summary>
		/// Returns operator token &ndash; alias for
		/// <see cref="Rhino.Node.GetType()">Rhino.Node.GetType()</see>
		/// </summary>
		public virtual int GetOperator()
		{
			return GetType();
		}

		/// <summary>
		/// Sets operator token &ndash; like
		/// <see cref="Rhino.Node.SetType(int)">Rhino.Node.SetType(int)</see>
		/// , but throws
		/// an exception if the operator is invalid.
		/// </summary>
		/// <exception cref="System.ArgumentException">
		/// if operator is not a valid token
		/// code
		/// </exception>
		public virtual void SetOperator(int @operator)
		{
			if (!Token.IsValidToken(@operator))
			{
				throw new ArgumentException("Invalid token: " + @operator);
			}
			SetType(@operator);
		}

		/// <summary>Returns the left-hand side of the expression</summary>
		public virtual AstNode GetLeft()
		{
			return left;
		}

		/// <summary>
		/// Sets the left-hand side of the expression, and sets its
		/// parent to this node.
		/// </summary>
		/// <remarks>
		/// Sets the left-hand side of the expression, and sets its
		/// parent to this node.
		/// </remarks>
		/// <param name="left">the left-hand side of the expression</param>
		/// <exception cref="System.ArgumentException">
		/// } if left is
		/// <code>null</code>
		/// </exception>
		public virtual void SetLeft(AstNode left)
		{
			AssertNotNull(left);
			this.left = left;
			// line number should agree with source position
			SetLineno(left.GetLineno());
			left.SetParent(this);
		}

		/// <summary>Returns the right-hand side of the expression</summary>
		/// <returns>
		/// the right-hand side.  It's usually an
		/// <see cref="AstNode">AstNode</see>
		/// node, but can also be a
		/// <see cref="FunctionNode">FunctionNode</see>
		/// representing Function expressions.
		/// </returns>
		public virtual AstNode GetRight()
		{
			return right;
		}

		/// <summary>
		/// Sets the right-hand side of the expression, and sets its parent to this
		/// node.
		/// </summary>
		/// <remarks>
		/// Sets the right-hand side of the expression, and sets its parent to this
		/// node.
		/// </remarks>
		/// <exception cref="System.ArgumentException">
		/// } if right is
		/// <code>null</code>
		/// </exception>
		public virtual void SetRight(AstNode right)
		{
			AssertNotNull(right);
			this.right = right;
			right.SetParent(this);
		}

		/// <summary>Returns relative offset of operator token</summary>
		public virtual int GetOperatorPosition()
		{
			return operatorPosition;
		}

		/// <summary>Sets operator token's relative offset</summary>
		/// <param name="operatorPosition">offset in parent of operator token</param>
		public virtual void SetOperatorPosition(int operatorPosition)
		{
			this.operatorPosition = operatorPosition;
		}

		public override bool HasSideEffects()
		{
			switch (GetType())
			{
				case Token.COMMA:
				{
					// the null-checks are for malformed expressions in IDE-mode
					return right != null && right.HasSideEffects();
				}

				case Token.AND:
				case Token.OR:
				{
					return left != null && left.HasSideEffects() || (right != null && right.HasSideEffects());
				}

				default:
				{
					return base.HasSideEffects();
				}
			}
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append(left.ToSource());
			sb.Append(" ");
			sb.Append(OperatorToString(GetType()));
			sb.Append(" ");
			sb.Append(right.ToSource());
			return sb.ToString();
		}

		/// <summary>Visits this node, the left operand, and the right operand.</summary>
		/// <remarks>Visits this node, the left operand, and the right operand.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				left.Visit(v);
				right.Visit(v);
			}
		}
	}
}
