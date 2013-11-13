/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for an embedded JavaScript expression within an E4X XML literal.</summary>
	/// <remarks>
	/// AST node for an embedded JavaScript expression within an E4X XML literal.
	/// Node type, like
	/// <see cref="XmlLiteral">XmlLiteral</see>
	/// , is
	/// <see cref="Rhino.Token.XML">Rhino.Token.XML</see>
	/// .  The node length
	/// includes the curly braces.
	/// </remarks>
	public class XmlExpression : XmlFragment
	{
		private AstNode expression;

		private bool isXmlAttribute;

		public XmlExpression()
		{
		}

		public XmlExpression(int pos) : base(pos)
		{
		}

		public XmlExpression(int pos, int len) : base(pos, len)
		{
		}

		public XmlExpression(int pos, AstNode expr) : base(pos)
		{
			SetExpression(expr);
		}

		/// <summary>Returns the expression embedded in {}</summary>
		public virtual AstNode GetExpression()
		{
			return expression;
		}

		/// <summary>Sets the expression embedded in {}, and sets its parent to this node.</summary>
		/// <remarks>Sets the expression embedded in {}, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>expression</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetExpression(AstNode expression)
		{
			AssertNotNull(expression);
			this.expression = expression;
			expression.SetParent(this);
		}

		/// <summary>Returns whether this is part of an xml attribute value</summary>
		public virtual bool IsXmlAttribute()
		{
			return isXmlAttribute;
		}

		/// <summary>Sets whether this is part of an xml attribute value</summary>
		public virtual void SetIsXmlAttribute(bool isXmlAttribute)
		{
			this.isXmlAttribute = isXmlAttribute;
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + "{" + expression.ToSource(depth) + "}";
		}

		/// <summary>Visits this node, then the child expression.</summary>
		/// <remarks>Visits this node, then the child expression.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				expression.Visit(v);
			}
		}
	}
}
