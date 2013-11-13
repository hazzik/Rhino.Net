/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>AST node for a simple name.</summary>
	/// <remarks>
	/// AST node for a simple name.  A simple name is an identifier that is
	/// not a keyword. Node type is
	/// <see cref="Rhino.Token.NAME">Rhino.Token.NAME</see>
	/// .<p>
	/// This node type is also used to represent certain non-identifier names that
	/// are part of the language syntax.  It's used for the "get" and "set"
	/// pseudo-keywords for object-initializer getter/setter properties, and it's
	/// also used for the "*" wildcard in E4X XML namespace and name expressions.
	/// </remarks>
	public class Name : AstNode
	{
		private string identifier;

		private Scope scope;

		public Name()
		{
			{
				type = Token.NAME;
			}
		}

		public Name(int pos) : base(pos)
		{
			{
				type = Token.NAME;
			}
		}

		public Name(int pos, int len) : base(pos, len)
		{
			{
				type = Token.NAME;
			}
		}

		/// <summary>
		/// Constructs a new
		/// <see cref="Name">Name</see>
		/// </summary>
		/// <param name="pos">node start position</param>
		/// <param name="len">node length</param>
		/// <param name="name">
		/// the identifier associated with this
		/// <code>Name</code>
		/// node
		/// </param>
		public Name(int pos, int len, string name) : base(pos, len)
		{
			{
				type = Token.NAME;
			}
			SetIdentifier(name);
		}

		public Name(int pos, string name) : base(pos)
		{
			{
				type = Token.NAME;
			}
			SetIdentifier(name);
			SetLength(name.Length);
		}

		/// <summary>Returns the node's identifier</summary>
		public virtual string GetIdentifier()
		{
			return identifier;
		}

		/// <summary>Sets the node's identifier</summary>
		/// <exception cref="System.ArgumentException">if identifier is null</exception>
		public virtual void SetIdentifier(string identifier)
		{
			AssertNotNull(identifier);
			this.identifier = identifier;
			SetLength(identifier.Length);
		}

		/// <summary>
		/// Set the
		/// <see cref="Scope">Scope</see>
		/// associated with this node.  This method does not
		/// set the scope's ast-node field to this node.  The field exists only
		/// for temporary storage by the code generator.  Not every name has an
		/// associated scope - typically only function and variable names (but not
		/// property names) are registered in a scope.
		/// </summary>
		/// <param name="s">
		/// the scope.  Can be null.  Doesn't set any fields in the
		/// scope.
		/// </param>
		public override void SetScope(Scope s)
		{
			scope = s;
		}

		/// <summary>
		/// Return the
		/// <see cref="Scope">Scope</see>
		/// associated with this node.  This is
		/// <em>only</em> used for (and set by) the code generator, so it will always
		/// be null in frontend AST-processing code.  Use
		/// <see cref="GetDefiningScope()">GetDefiningScope()</see>
		/// to find the lexical
		/// <code>Scope</code>
		/// in which this
		/// <code>Name</code>
		/// is defined,
		/// if any.
		/// </summary>
		public override Scope GetScope()
		{
			return scope;
		}

		/// <summary>
		/// Returns the
		/// <see cref="Scope">Scope</see>
		/// in which this
		/// <code>Name</code>
		/// is defined.
		/// </summary>
		/// <returns>
		/// the scope in which this name is defined, or
		/// <code>null</code>
		/// if it's not defined in the current lexical scope chain
		/// </returns>
		public virtual Scope GetDefiningScope()
		{
			Scope enclosing = GetEnclosingScope();
			string name = GetIdentifier();
			return enclosing == null ? null : enclosing.GetDefiningScope(name);
		}

		/// <summary>
		/// Return true if this node is known to be defined as a symbol in a
		/// lexical scope other than the top-level (global) scope.
		/// </summary>
		/// <remarks>
		/// Return true if this node is known to be defined as a symbol in a
		/// lexical scope other than the top-level (global) scope.
		/// </remarks>
		/// <returns>
		/// 
		/// <code>true</code>
		/// if this name appears as local variable, a let-bound
		/// variable not in the global scope, a function parameter, a loop
		/// variable, the property named in a
		/// <see cref="PropertyGet">PropertyGet</see>
		/// , or in any other
		/// context where the node is known not to resolve to the global scope.
		/// Returns
		/// <code>false</code>
		/// if the node is defined in the top-level scope
		/// (i.e., its defining scope is an
		/// <see cref="AstRoot">AstRoot</see>
		/// object), or if its
		/// name is not defined as a symbol in the symbol table, in which case it
		/// may be an external or built-in name (or just an error of some sort.)
		/// </returns>
		public virtual bool IsLocalName()
		{
			Scope scope = GetDefiningScope();
			return scope != null && scope.GetParentScope() != null;
		}

		/// <summary>
		/// Return the length of this node's identifier, to let you pretend
		/// it's a
		/// <see cref="string">string</see>
		/// .  Don't confuse this method with the
		/// <see cref="AstNode.GetLength()">AstNode.GetLength()</see>
		/// method, which returns the range of
		/// characters that this node overlaps in the source input.
		/// </summary>
		public virtual int Length()
		{
			return identifier == null ? 0 : identifier.Length;
		}

		public override string ToSource(int depth)
		{
			return MakeIndent(depth) + (identifier == null ? "<null>" : identifier);
		}

		/// <summary>Visits this node.</summary>
		/// <remarks>Visits this node.  There are no children to visit.</remarks>
		public override void Visit(NodeVisitor v)
		{
			v.Visit(this);
		}
	}
}
