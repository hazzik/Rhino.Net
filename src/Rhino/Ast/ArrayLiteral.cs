/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for an Array literal.</summary>
	/// <remarks>
	/// AST node for an Array literal.  The elements list will always be
	/// non-
	/// <code>null</code>
	/// , although the list will have no elements if the Array literal
	/// is empty.<p>
	/// Node type is
	/// <see cref="Rhino.Token.ARRAYLIT">Rhino.Token.ARRAYLIT</see>
	/// .<p>
	/// <pre><i>ArrayLiteral</i> :
	/// <b>[</b> Elisionopt <b>]</b>
	/// <b>[</b> ElementList <b>]</b>
	/// <b>[</b> ElementList , Elisionopt <b>]</b>
	/// <i>ElementList</i> :
	/// Elisionopt AssignmentExpression
	/// ElementList , Elisionopt AssignmentExpression
	/// <i>Elision</i> :
	/// <b>,</b>
	/// Elision <b>,</b></pre>
	/// </remarks>
	public class ArrayLiteral : AstNode, DestructuringForm
	{
		private static readonly IList<AstNode> NO_ELEMS = new List<AstNode>().AsReadOnly();

		private IList<AstNode> elements;

		private int destructuringLength;

		private int skipCount;

		private bool isDestructuring;

		public ArrayLiteral()
		{
			{
				type = Token.ARRAYLIT;
			}
		}

		public ArrayLiteral(int pos) : base(pos)
		{
			{
				type = Token.ARRAYLIT;
			}
		}

		public ArrayLiteral(int pos, int len) : base(pos, len)
		{
			{
				type = Token.ARRAYLIT;
			}
		}

		/// <summary>Returns the element list</summary>
		/// <returns>
		/// the element list.  If there are no elements, returns an immutable
		/// empty list.  Elisions are represented as
		/// <see cref="EmptyExpression">EmptyExpression</see>
		/// nodes.
		/// </returns>
		public virtual IList<AstNode> GetElements()
		{
			return elements != null ? elements : NO_ELEMS;
		}

		/// <summary>Sets the element list, and sets each element's parent to this node.</summary>
		/// <remarks>Sets the element list, and sets each element's parent to this node.</remarks>
		/// <param name="elements">
		/// the element list.  Can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetElements(IList<AstNode> elements)
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
				foreach (AstNode e in elements)
				{
					AddElement(e);
				}
			}
		}

		/// <summary>Adds an element to the list, and sets its parent to this node.</summary>
		/// <remarks>Adds an element to the list, and sets its parent to this node.</remarks>
		/// <param name="element">the element to add</param>
		/// <exception cref="System.ArgumentException">
		/// if element is
		/// <code>null</code>
		/// .  To indicate
		/// an empty element, use an
		/// <see cref="EmptyExpression">EmptyExpression</see>
		/// node.
		/// </exception>
		public virtual void AddElement(AstNode element)
		{
			AssertNotNull(element);
			if (elements == null)
			{
				elements = new List<AstNode>();
			}
			elements.Add(element);
			element.SetParent(this);
		}

		/// <summary>
		/// Returns the number of elements in this
		/// <code>Array</code>
		/// literal,
		/// including empty elements.
		/// </summary>
		public virtual int GetSize()
		{
			return elements == null ? 0 : elements.Count;
		}

		/// <summary>Returns element at specified index.</summary>
		/// <remarks>Returns element at specified index.</remarks>
		/// <param name="index">the index of the element to retrieve</param>
		/// <returns>the element</returns>
		/// <exception cref="System.IndexOutOfRangeException">if the index is invalid</exception>
		public virtual AstNode GetElement(int index)
		{
			if (elements == null)
			{
				throw new IndexOutOfRangeException("no elements");
			}
			return elements[index];
		}

		/// <summary>Returns destructuring length</summary>
		public virtual int GetDestructuringLength()
		{
			return destructuringLength;
		}

		/// <summary>Sets destructuring length.</summary>
		/// <remarks>
		/// Sets destructuring length.  This is set by the parser and used
		/// by the code generator.
		/// <code>for ([a,] in obj)</code>
		/// is legal,
		/// but
		/// <code>for ([a] in obj)</code>
		/// is not since we have both key and
		/// value supplied.  The difference is only meaningful in array literals
		/// used in destructuring-assignment contexts.
		/// </remarks>
		public virtual void SetDestructuringLength(int destructuringLength)
		{
			this.destructuringLength = destructuringLength;
		}

		/// <summary>Used by code generator.</summary>
		/// <remarks>Used by code generator.</remarks>
		/// <returns>the number of empty elements</returns>
		public virtual int GetSkipCount()
		{
			return skipCount;
		}

		/// <summary>Used by code generator.</summary>
		/// <remarks>Used by code generator.</remarks>
		/// <param name="count">the count of empty elements</param>
		public virtual void SetSkipCount(int count)
		{
			skipCount = count;
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
			sb.Append("[");
			if (elements != null)
			{
				PrintList(elements, sb);
			}
			sb.Append("]");
			return sb.ToString();
		}

		/// <summary>Visits this node, then visits its element expressions in order.</summary>
		/// <remarks>
		/// Visits this node, then visits its element expressions in order.
		/// Any empty elements are represented by
		/// <see cref="EmptyExpression">EmptyExpression</see>
		/// objects, so the callback will never be passed
		/// <code>null</code>
		/// .
		/// </remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				foreach (AstNode e in GetElements())
				{
					e.Visit(v);
				}
			}
		}
	}
}
