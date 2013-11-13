/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>Represents a scope in the lexical scope chain.</summary>
	/// <remarks>
	/// Represents a scope in the lexical scope chain.  Base type for
	/// all
	/// <see cref="AstNode">AstNode</see>
	/// implementations that can introduce a new scope.
	/// </remarks>
	public class Scope : Jump
	{
		protected internal IDictionary<string, Symbol> symbolTable;

		protected internal Rhino.Ast.Scope parentScope;

		protected internal ScriptNode top;

		private IList<Rhino.Ast.Scope> childScopes;

		public Scope()
		{
			{
				// Use LinkedHashMap so that the iteration order is the insertion order
				// current script or function scope
				this.type = Token.BLOCK;
			}
		}

		public Scope(int pos)
		{
			{
				this.type = Token.BLOCK;
			}
			this.position = pos;
		}

		public Scope(int pos, int len) : this(pos)
		{
			this.length = len;
		}

		public virtual Rhino.Ast.Scope GetParentScope()
		{
			return parentScope;
		}

		/// <summary>Sets parent scope</summary>
		public virtual void SetParentScope(Rhino.Ast.Scope parentScope)
		{
			this.parentScope = parentScope;
			this.top = parentScope == null ? (ScriptNode)this : parentScope.top;
		}

		/// <summary>Used only for code generation.</summary>
		/// <remarks>Used only for code generation.</remarks>
		public virtual void ClearParentScope()
		{
			this.parentScope = null;
		}

		/// <summary>Return a list of the scopes whose parent is this scope.</summary>
		/// <remarks>Return a list of the scopes whose parent is this scope.</remarks>
		/// <returns>
		/// the list of scopes we enclose, or
		/// <code>null</code>
		/// if none
		/// </returns>
		public virtual IList<Rhino.Ast.Scope> GetChildScopes()
		{
			return childScopes;
		}

		/// <summary>Add a scope to our list of child scopes.</summary>
		/// <remarks>
		/// Add a scope to our list of child scopes.
		/// Sets the child's parent scope to this scope.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// if the child's parent scope is
		/// non-
		/// <code>null</code>
		/// </exception>
		public virtual void AddChildScope(Rhino.Ast.Scope child)
		{
			if (childScopes == null)
			{
				childScopes = new AList<Rhino.Ast.Scope>();
			}
			childScopes.AddItem(child);
			child.SetParentScope(this);
		}

		/// <summary>Used by the parser; not intended for typical use.</summary>
		/// <remarks>
		/// Used by the parser; not intended for typical use.
		/// Changes the parent-scope links for this scope's child scopes
		/// to the specified new scope.  Copies symbols from this scope
		/// into new scope.
		/// </remarks>
		/// <param name="newScope">
		/// the scope that will replace this one on the
		/// scope stack.
		/// </param>
		public virtual void ReplaceWith(Rhino.Ast.Scope newScope)
		{
			if (childScopes != null)
			{
				foreach (Rhino.Ast.Scope kid in childScopes)
				{
					newScope.AddChildScope(kid);
				}
				// sets kid's parent
				childScopes.Clear();
				childScopes = null;
			}
			if (symbolTable != null && !symbolTable.IsEmpty())
			{
				JoinScopes(this, newScope);
			}
		}

		/// <summary>Returns current script or function scope</summary>
		public virtual ScriptNode GetTop()
		{
			return top;
		}

		/// <summary>Sets top current script or function scope</summary>
		public virtual void SetTop(ScriptNode top)
		{
			this.top = top;
		}

		/// <summary>
		/// Creates a new scope node, moving symbol table information
		/// from "scope" to the new node, and making "scope" a nested
		/// scope contained by the new node.
		/// </summary>
		/// <remarks>
		/// Creates a new scope node, moving symbol table information
		/// from "scope" to the new node, and making "scope" a nested
		/// scope contained by the new node.
		/// Useful for injecting a new scope in a scope chain.
		/// </remarks>
		public static Rhino.Ast.Scope SplitScope(Rhino.Ast.Scope scope)
		{
			Rhino.Ast.Scope result = new Rhino.Ast.Scope(scope.GetType());
			result.symbolTable = scope.symbolTable;
			scope.symbolTable = null;
			result.parent = scope.parent;
			result.SetParentScope(scope.GetParentScope());
			result.SetParentScope(result);
			scope.parent = result;
			result.top = scope.top;
			return result;
		}

		/// <summary>Copies all symbols from source scope to dest scope.</summary>
		/// <remarks>Copies all symbols from source scope to dest scope.</remarks>
		public static void JoinScopes(Rhino.Ast.Scope source, Rhino.Ast.Scope dest)
		{
			IDictionary<string, Symbol> src = source.EnsureSymbolTable();
			IDictionary<string, Symbol> dst = dest.EnsureSymbolTable();
			if (!Sharpen.Collections.Disjoint(src.Keys, dst.Keys))
			{
				CodeBug();
			}
			foreach (KeyValuePair<string, Symbol> entry in src.EntrySet())
			{
				Symbol sym = entry.Value;
				sym.SetContainingTable(dest);
				dst.Put(entry.Key, sym);
			}
		}

		/// <summary>Returns the scope in which this name is defined</summary>
		/// <param name="name">the symbol to look up</param>
		/// <returns>
		/// this
		/// <see cref="Scope">Scope</see>
		/// , one of its parent scopes, or
		/// <code>null</code>
		/// if
		/// the name is not defined any this scope chain
		/// </returns>
		public virtual Rhino.Ast.Scope GetDefiningScope(string name)
		{
			for (Rhino.Ast.Scope s = this; s != null; s = s.parentScope)
			{
				IDictionary<string, Symbol> symbolTable = s.GetSymbolTable();
				if (symbolTable != null && symbolTable.ContainsKey(name))
				{
					return s;
				}
			}
			return null;
		}

		/// <summary>Looks up a symbol in this scope.</summary>
		/// <remarks>Looks up a symbol in this scope.</remarks>
		/// <param name="name">the symbol name</param>
		/// <returns>
		/// the Symbol, or
		/// <code>null</code>
		/// if not found
		/// </returns>
		public virtual Symbol GetSymbol(string name)
		{
			return symbolTable == null ? null : symbolTable.Get(name);
		}

		/// <summary>Enters a symbol into this scope.</summary>
		/// <remarks>Enters a symbol into this scope.</remarks>
		public virtual void PutSymbol(Symbol symbol)
		{
			if (symbol.GetName() == null)
			{
				throw new ArgumentException("null symbol name");
			}
			EnsureSymbolTable();
			symbolTable.Put(symbol.GetName(), symbol);
			symbol.SetContainingTable(this);
			top.AddSymbol(symbol);
		}

		/// <summary>Returns the symbol table for this scope.</summary>
		/// <remarks>Returns the symbol table for this scope.</remarks>
		/// <returns>
		/// the symbol table.  May be
		/// <code>null</code>
		/// .
		/// </returns>
		public virtual IDictionary<string, Symbol> GetSymbolTable()
		{
			return symbolTable;
		}

		/// <summary>Sets the symbol table for this scope.</summary>
		/// <remarks>
		/// Sets the symbol table for this scope.  May be
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual void SetSymbolTable(IDictionary<string, Symbol> table)
		{
			symbolTable = table;
		}

		private IDictionary<string, Symbol> EnsureSymbolTable()
		{
			if (symbolTable == null)
			{
				symbolTable = new LinkedHashMap<string, Symbol>(5);
			}
			return symbolTable;
		}

		/// <summary>
		/// Returns a copy of the child list, with each child cast to an
		/// <see cref="AstNode">AstNode</see>
		/// .
		/// </summary>
		/// <exception cref="System.InvalidCastException">
		/// if any non-
		/// <code>AstNode</code>
		/// objects are
		/// in the child list, e.g. if this method is called after the code
		/// generator begins the tree transformation.
		/// </exception>
		public virtual IList<AstNode> GetStatements()
		{
			IList<AstNode> stmts = new AList<AstNode>();
			Node n = GetFirstChild();
			while (n != null)
			{
				stmts.AddItem((AstNode)n);
				n = n.GetNext();
			}
			return stmts;
		}

		public override string ToSource(int depth)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(MakeIndent(depth));
			sb.Append("{\n");
			foreach (Node kid in this)
			{
				sb.Append(((AstNode)kid).ToSource(depth + 1));
			}
			sb.Append(MakeIndent(depth));
			sb.Append("}\n");
			return sb.ToString();
		}

		public override void Visit(NodeVisitor v)
		{
			if (v.Visit(this))
			{
				foreach (Node kid in this)
				{
					((AstNode)kid).Visit(v);
				}
			}
		}
	}
}
