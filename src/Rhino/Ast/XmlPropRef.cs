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
	/// <summary>
	/// AST node for an E4X XML
	/// <code>[expr]</code>
	/// property-ref expression.
	/// The node type is
	/// <see cref="Rhino.Token.REF_NAME">Rhino.Token.REF_NAME</see>
	/// .<p>
	/// Syntax:<p>
	/// <pre> @<i><sub>opt</sub></i> ns:: <i><sub>opt</sub></i> name</pre>
	/// Examples include
	/// <code>name</code>
	/// ,
	/// <code>ns::name</code>
	/// ,
	/// <code>ns::*</code>
	/// ,
	/// <code>*::name</code>
	/// ,
	/// <code>*::*</code>
	/// ,
	/// <code>@attr</code>
	/// ,
	/// <code>@ns::attr</code>
	/// ,
	/// <code>@ns::*</code>
	/// ,
	/// <code>@*::attr</code>
	/// ,
	/// <code>@*::*</code>
	/// and
	/// <code>@*</code>
	/// .<p>
	/// The node starts at the
	/// <code>@</code>
	/// token, if present.  Otherwise it starts
	/// at the namespace name.  The node bounds extend through the closing
	/// right-bracket, or if it is missing due to a syntax error, through the
	/// end of the index expression.<p>
	/// </summary>
	public class XmlPropRef : XmlRef
	{
		private Name propName;

		public XmlPropRef()
		{
			{
				type = Token.REF_NAME;
			}
		}

		public XmlPropRef(int pos) : base(pos)
		{
			{
				type = Token.REF_NAME;
			}
		}

		public XmlPropRef(int pos, int len) : base(pos, len)
		{
			{
				type = Token.REF_NAME;
			}
		}

		/// <summary>Returns property name.</summary>
		/// <remarks>Returns property name.</remarks>
		public virtual Name GetPropName()
		{
			return propName;
		}

		/// <summary>Sets property name, and sets its parent to this node.</summary>
		/// <remarks>Sets property name, and sets its parent to this node.</remarks>
		/// <exception cref="System.ArgumentException">
		/// if
		/// <code>propName</code>
		/// is
		/// <code>null</code>
		/// </exception>
		public virtual void SetPropName(Name propName)
		{
			AssertNotNull(propName);
			this.propName = propName;
			propName.SetParent(this);
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			if (IsAttributeAccess())
			{
				sb.Append("@");
			}
			if (@namespace != null)
			{
				sb.Append(@namespace.ToSource(0));
				sb.Append("::");
			}
			sb.Append(propName.ToSource(0));
			return sb.ToString();
		}

		/// <summary>Visits this node, then the namespace if present, then the property name.</summary>
		/// <remarks>Visits this node, then the namespace if present, then the property name.</remarks>
		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				if (@namespace != null)
				{
					@namespace.Visit(v);
				}
				propName.Visit(v);
			}
		}
	}
}
