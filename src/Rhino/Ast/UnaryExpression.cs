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
	/// AST node representing unary operators such as
	/// <code>++</code>
	/// ,
	/// <code>~</code>
	/// ,
	/// <code>typeof</code>
	/// and
	/// <code>delete</code>
	/// .  The type field
	/// is set to the appropriate Token type for the operator.  The node length spans
	/// from the operator to the end of the operand (for prefix operators) or from
	/// the start of the operand to the operator (for postfix).<p>
	/// The
	/// <code>default xml namespace = &lt;expr&gt;</code>
	/// statement in E4X
	/// (JavaScript 1.6) is represented as a
	/// <code>UnaryExpression</code>
	/// of node
	/// type
	/// <see cref="Rhino.Token.DEFAULTNAMESPACE">Rhino.Token.DEFAULTNAMESPACE</see>
	/// , wrapped with an
	/// <see cref="ExpressionStatement">ExpressionStatement</see>
	/// .
	/// </summary>
	public class UnaryExpression : AstNode
	{
		private AstNode operand;

		private bool isPostfix;

		public UnaryExpression()
		{
		}

		public UnaryExpression(int pos) : base(pos)
		{
		}

		/// <summary>Constructs a new postfix UnaryExpression</summary>
		public UnaryExpression(int pos, int len) : base(pos, len)
		{
		}

		/// <summary>Constructs a new prefix UnaryExpression.</summary>
		/// <remarks>Constructs a new prefix UnaryExpression.</remarks>
		public UnaryExpression(int @operator, int operatorPosition, AstNode operand) : this(@operator, operatorPosition, operand, false)
		{
		}

		/// <summary>
		/// Constructs a new UnaryExpression with the specified operator
		/// and operand.
		/// </summary>
		/// <remarks>
		/// Constructs a new UnaryExpression with the specified operator
		/// and operand.  It sets the parent of the operand, and sets its own bounds
		/// to encompass the operator and operand.
		/// </remarks>
		/// <param name="operator">the node type</param>
		/// <param name="operatorPosition">the absolute position of the operator.</param>
		/// <param name="operand">the operand expression</param>
		/// <param name="postFix">true if the operator follows the operand.  Int</param>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>operand</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public UnaryExpression(int @operator, int operatorPosition, AstNode operand, bool postFix)
		{
			AssertNotNull(operand);
			int beg = postFix ? operand.GetPosition() : operatorPosition;
			// JavaScript only has ++ and -- postfix operators, so length is 2
			int end = postFix ? operatorPosition + 2 : operand.GetPosition() + operand.GetLength();
			SetBounds(beg, end);
			SetOperator(@operator);
			SetOperand(operand);
			isPostfix = postFix;
		}

		/// <summary>
		/// Returns operator token &ndash; alias for
		/// <see cref="Rhino.Node.GetType()">Rhino.Node.GetType()</see>
		/// </summary>
		public virtual int GetOperator()
		{
			return type;
		}

		/// <summary>
		/// Sets operator &ndash; same as
		/// <see cref="Rhino.Node.SetType(int)">Rhino.Node.SetType(int)</see>
		/// , but throws an
		/// exception if the operator is invalid
		/// </summary>
		/// <exception cref="System.ArgumentException">
		/// if operator is not a valid
		/// Token code
		/// </exception>
		public virtual void SetOperator(int @operator)
		{
			if (!Token.IsValidToken(@operator))
			{
				throw new ArgumentException("Invalid token: " + @operator);
			}
			SetType(@operator);
		}

		public virtual AstNode GetOperand()
		{
			return operand;
		}

		/// <summary>Sets the operand, and sets its parent to be this node.</summary>
		/// <remarks>Sets the operand, and sets its parent to be this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// } if
		/// <code>operand</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetOperand(AstNode operand)
		{
			AssertNotNull(operand);
			this.operand = operand;
			operand.SetParent(this);
		}

		/// <summary>Returns whether the operator is postfix</summary>
		public virtual bool IsPostfix()
		{
			return isPostfix;
		}

		/// <summary>Returns whether the operator is prefix</summary>
		public virtual bool IsPrefix()
		{
			return !isPostfix;
		}

		/// <summary>Sets whether the operator is postfix</summary>
		public virtual void SetIsPostfix(bool isPostfix)
		{
			this.isPostfix = isPostfix;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			int type = GetType();
			if (!isPostfix)
			{
				sb.Append(OperatorToString(type));
				if (type == Token.TYPEOF || type == Token.DELPROP || type == Token.VOID)
				{
					sb.Append(" ");
				}
			}
			sb.Append(operand.ToSource());
			if (isPostfix)
			{
				sb.Append(OperatorToString(type));
			}
			return sb.ToString();
		}

		/// <summary>Visits this node, then the operand.</summary>
		/// <remarks>Visits this node, then the operand.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				operand.Visit(v);
			}
		}
	}
}
