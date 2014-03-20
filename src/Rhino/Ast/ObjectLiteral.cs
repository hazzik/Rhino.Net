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
	/// <summary>
	/// AST node for an Object literal (also called an Object initialiser in
	/// Ecma-262).
	/// </summary>
	/// <remarks>
	/// AST node for an Object literal (also called an Object initialiser in
	/// Ecma-262).  The elements list will always be non-
	/// <code>null</code>
	/// , although
	/// the list will have no elements if the Object literal is empty.<p>
	/// Node type is
	/// <see cref="Rhino.Token.OBJECTLIT">Rhino.Token.OBJECTLIT</see>
	/// .<p>
	/// <pre><i>ObjectLiteral</i> :
	/// <b>{}</b>
	/// <b>{</b> PropertyNameAndValueList <b>}</b>
	/// <i>PropertyNameAndValueList</i> :
	/// PropertyName <b>:</b> AssignmentExpression
	/// PropertyNameAndValueList , PropertyName <b>:</b> AssignmentExpression
	/// <i>PropertyName</i> :
	/// Identifier
	/// StringLiteral
	/// NumericLiteral</pre>
	/// </remarks>
	public class ObjectLiteral : AstNode, DestructuringForm
	{
		private static readonly IList<ObjectProperty> NO_ELEMS = new List<ObjectProperty>().AsReadOnly();

		private IList<ObjectProperty> elements;

		internal bool isDestructuring;

		public ObjectLiteral()
		{
			{
				type = Token.OBJECTLIT;
			}
		}

		public ObjectLiteral(int pos) : base(pos)
		{
			{
				type = Token.OBJECTLIT;
			}
		}

		public ObjectLiteral(int pos, int len) : base(pos, len)
		{
			{
				type = Token.OBJECTLIT;
			}
		}

		/// <summary>Returns the element list.</summary>
		/// <remarks>
		/// Returns the element list.  Returns an immutable empty list if there are
		/// no elements.
		/// </remarks>
		public virtual IList<ObjectProperty> GetElements()
		{
			return elements ?? NO_ELEMS;
		}

		/// <summary>Sets the element list, and updates the parent of each element.</summary>
		/// <remarks>
		/// Sets the element list, and updates the parent of each element.
		/// Replaces any existing elements.
		/// </remarks>
		/// <param name="elements">
		/// the element list.  Can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetElements(IList<ObjectProperty> elements)
		{
			if (elements == null)
			{
				this.elements = null;
			}
			else
			{
				if (this.elements != null)
				{
					this.elements.Clear();
				}
				foreach (ObjectProperty o in elements)
				{
					AddElement(o);
				}
			}
		}

		/// <summary>Adds an element to the list, and sets its parent to this node.</summary>
		/// <remarks>Adds an element to the list, and sets its parent to this node.</remarks>
		/// <param name="element">the property node to append to the end of the list</param>
		/// <exception cref="System.ArgumentException">
		/// } if element is
		/// <code>null</code>
		/// </exception>
		public virtual void AddElement(ObjectProperty element)
		{
			AssertNotNull(element);
			if (elements == null)
			{
				elements = new List<ObjectProperty>();
			}
			elements.Add(element);
			element.SetParent(this);
		}

		/// <summary>
		/// Marks this node as being a destructuring form - that is, appearing
		/// in a context such as
		/// <code>for ([a, b] in ...)</code>
		/// where it's the
		/// target of a destructuring assignment.
		/// </summary>
		public virtual void SetIsDestructuring(bool destructuring)
		{
			isDestructuring = destructuring;
		}

		/// <summary>
		/// Returns true if this node is in a destructuring position:
		/// a function parameter, the target of a variable initializer, the
		/// iterator of a for..in loop, etc.
		/// </summary>
		/// <remarks>
		/// Returns true if this node is in a destructuring position:
		/// a function parameter, the target of a variable initializer, the
		/// iterator of a for..in loop, etc.
		/// </remarks>
		public virtual bool IsDestructuring()
		{
			return isDestructuring;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("{");
			if (elements != null)
			{
				PrintList(elements, sb);
			}
			sb.Append("}");
			return sb.ToString();
		}

		/// <summary>
		/// Visits this node, then visits each child property node, in lexical
		/// (source) order.
		/// </summary>
		/// <remarks>
		/// Visits this node, then visits each child property node, in lexical
		/// (source) order.
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				foreach (ObjectProperty prop in GetElements())
				{
					prop.Visit(v);
				}
			}
		}
	}
}
