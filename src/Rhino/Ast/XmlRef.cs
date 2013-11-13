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
	/// <summary>Base class for E4X XML attribute-access or property-get expressions.</summary>
	/// <remarks>
	/// Base class for E4X XML attribute-access or property-get expressions.
	/// Such expressions can take a variety of forms. The general syntax has
	/// three parts:<p>
	/// <ol>
	/// <li>optional: an
	/// <code>@</code>
	/// </li>  (specifying an attribute access)</li>
	/// <li>optional: a namespace (a
	/// <code>Name</code>
	/// ) and double-colon</li>
	/// <li>required:  either a
	/// <code>Name</code>
	/// or a bracketed [expression]</li>
	/// </ol>
	/// The property-name expressions (examples:
	/// <code>ns::name</code>
	/// ,
	/// <code>@name</code>
	/// )
	/// are represented as
	/// <see cref="XmlPropRef">XmlPropRef</see>
	/// nodes.  The bracketed-expression
	/// versions (examples:
	/// <code>ns::[name]</code>
	/// ,
	/// <code>@[name]</code>
	/// ) become
	/// <see cref="XmlElemRef">XmlElemRef</see>
	/// nodes.<p>
	/// This node type (or more specifically, its subclasses) will
	/// sometimes be the right-hand child of a
	/// <see cref="PropertyGet">PropertyGet</see>
	/// node or
	/// an
	/// <see cref="XmlMemberGet">XmlMemberGet</see>
	/// node.  The
	/// <code>XmlRef</code>
	/// node may also
	/// be a standalone primary expression with no explicit target, which
	/// is valid in certain expression contexts such as
	/// <code>company..employee.(@id &lt; 100)</code>
	/// - in this case, the
	/// <code>@id</code>
	/// is an
	/// <code>XmlRef</code>
	/// that is part of an infix '&lt;' expression
	/// whose parent is an
	/// <code>XmlDotQuery</code>
	/// node.<p>
	/// </remarks>
	public abstract class XmlRef : AstNode
	{
		protected internal Name @namespace;

		protected internal int atPos = -1;

		protected internal int colonPos = -1;

		public XmlRef()
		{
		}

		public XmlRef(int pos) : base(pos)
		{
		}

		public XmlRef(int pos, int len) : base(pos, len)
		{
		}

		/// <summary>Return the namespace.</summary>
		/// <remarks>
		/// Return the namespace.  May be
		/// <code>@null</code>
		/// .
		/// </remarks>
		public virtual Name GetNamespace()
		{
			return @namespace;
		}

		/// <summary>Sets namespace, and sets its parent to this node.</summary>
		/// <remarks>
		/// Sets namespace, and sets its parent to this node.
		/// Can be
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual void SetNamespace(Name @namespace)
		{
			this.@namespace = @namespace;
			if (@namespace != null)
			{
				@namespace.SetParent(this);
			}
		}

		/// <summary>
		/// Returns
		/// <code>true</code>
		/// if this expression began with an
		/// <code>@</code>
		/// -token.
		/// </summary>
		public virtual bool IsAttributeAccess()
		{
			return atPos >= 0;
		}

		/// <summary>
		/// Returns position of
		/// <code>@</code>
		/// -token, or -1 if this is not
		/// an attribute-access expression.
		/// </summary>
		public virtual int GetAtPos()
		{
			return atPos;
		}

		/// <summary>
		/// Sets position of
		/// <code>@</code>
		/// -token, or -1
		/// </summary>
		public virtual void SetAtPos(int atPos)
		{
			this.atPos = atPos;
		}

		/// <summary>
		/// Returns position of
		/// <code>::</code>
		/// token, or -1 if not present.
		/// It will only be present if the namespace node is non-
		/// <code>null</code>
		/// .
		/// </summary>
		public virtual int GetColonPos()
		{
			return colonPos;
		}

		/// <summary>
		/// Sets position of
		/// <code>::</code>
		/// token, or -1 if not present
		/// </summary>
		public virtual void SetColonPos(int colonPos)
		{
			this.colonPos = colonPos;
		}
	}
}
