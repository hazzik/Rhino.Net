/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino.Ast
{
	/// <summary>
	/// Base type for
	/// <see cref="AstRoot">AstRoot</see>
	/// and
	/// <see cref="FunctionNode">FunctionNode</see>
	/// nodes, which need to
	/// collect much of the same information.
	/// </summary>
	public class ScriptNode : Scope
	{
		private int encodedSourceStart = -1;

		private int encodedSourceEnd = -1;

		private string sourceName;

		private string encodedSource;

		private int endLineno = -1;

		private IList<FunctionNode> functions;

		private IList<RegExpLiteral> regexps;

		private IList<FunctionNode> EMPTY_LIST = Sharpen.Collections.EmptyList();

		private IList<Symbol> symbols = new List<Symbol>(4);

		private int paramCount = 0;

		private string[] variableNames;

		private bool[] isConsts;

		private object compilerData;

		private int tempNumber = 0;

		public ScriptNode()
		{
			{
				// during parsing, a ScriptNode or FunctionNode's top scope is itself
				this.top = this;
				this.type = Token.SCRIPT;
			}
		}

		public ScriptNode(int pos) : base(pos)
		{
			{
				this.top = this;
				this.type = Token.SCRIPT;
			}
		}

		/// <summary>
		/// Returns the URI, path or descriptive text indicating the origin
		/// of this script's source code.
		/// </summary>
		/// <remarks>
		/// Returns the URI, path or descriptive text indicating the origin
		/// of this script's source code.
		/// </remarks>
		public virtual string GetSourceName()
		{
			return sourceName;
		}

		/// <summary>
		/// Sets the URI, path or descriptive text indicating the origin
		/// of this script's source code.
		/// </summary>
		/// <remarks>
		/// Sets the URI, path or descriptive text indicating the origin
		/// of this script's source code.
		/// </remarks>
		public virtual void SetSourceName(string sourceName)
		{
			this.sourceName = sourceName;
		}

		/// <summary>Returns the start offset of the encoded source.</summary>
		/// <remarks>
		/// Returns the start offset of the encoded source.
		/// Only valid if
		/// <see cref="GetEncodedSource()">GetEncodedSource()</see>
		/// returns non-
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual int GetEncodedSourceStart()
		{
			return encodedSourceStart;
		}

		/// <summary>Used by code generator.</summary>
		/// <remarks>Used by code generator.</remarks>
		/// <seealso cref="GetEncodedSource()">GetEncodedSource()</seealso>
		public virtual void SetEncodedSourceStart(int start)
		{
			this.encodedSourceStart = start;
		}

		/// <summary>Returns the end offset of the encoded source.</summary>
		/// <remarks>
		/// Returns the end offset of the encoded source.
		/// Only valid if
		/// <see cref="GetEncodedSource()">GetEncodedSource()</see>
		/// returns non-
		/// <code>null</code>
		/// .
		/// </remarks>
		public virtual int GetEncodedSourceEnd()
		{
			return encodedSourceEnd;
		}

		/// <summary>Used by code generator.</summary>
		/// <remarks>Used by code generator.</remarks>
		/// <seealso cref="GetEncodedSource()">GetEncodedSource()</seealso>
		public virtual void SetEncodedSourceEnd(int end)
		{
			this.encodedSourceEnd = end;
		}

		/// <summary>Used by code generator.</summary>
		/// <remarks>Used by code generator.</remarks>
		/// <seealso cref="GetEncodedSource()">GetEncodedSource()</seealso>
		public virtual void SetEncodedSourceBounds(int start, int end)
		{
			this.encodedSourceStart = start;
			this.encodedSourceEnd = end;
		}

		/// <summary>Used by the code generator.</summary>
		/// <remarks>Used by the code generator.</remarks>
		/// <seealso cref="GetEncodedSource()">GetEncodedSource()</seealso>
		public virtual void SetEncodedSource(string encodedSource)
		{
			this.encodedSource = encodedSource;
		}

		/// <summary>
		/// Returns a canonical version of the source for this script or function,
		/// for use in implementing the
		/// <code>Object.toSource</code>
		/// method of
		/// JavaScript objects.  This source encoding is only recorded during code
		/// generation.  It must be passed back to
		/// <see cref="Rhino.Decompiler.Decompile(string, int, Rhino.UintMap)">Rhino.Decompiler.Decompile(string, int, Rhino.UintMap)</see>
		/// to construct the
		/// human-readable source string.<p>
		/// Given a parsed AST, you can always convert it to source code using the
		/// <see cref="AstNode.ToSource()">AstNode.ToSource()</see>
		/// method, although it's not guaranteed to produce
		/// exactly the same results as
		/// <code>Object.toSource</code>
		/// with respect to
		/// formatting, parenthesization and other details.
		/// </summary>
		/// <returns>
		/// the encoded source, or
		/// <code>null</code>
		/// if it was not recorded.
		/// </returns>
		public virtual string GetEncodedSource()
		{
			return encodedSource;
		}

		public virtual int GetBaseLineno()
		{
			return lineno;
		}

		/// <summary>Sets base (starting) line number for this script or function.</summary>
		/// <remarks>
		/// Sets base (starting) line number for this script or function.
		/// This is a one-time operation, and throws an exception if the
		/// line number has already been set.
		/// </remarks>
		public virtual void SetBaseLineno(int lineno)
		{
			if (lineno < 0 || this.lineno >= 0)
			{
				CodeBug();
			}
			this.lineno = lineno;
		}

		public virtual int GetEndLineno()
		{
			return endLineno;
		}

		public virtual void SetEndLineno(int lineno)
		{
			// One time action
			if (lineno < 0 || endLineno >= 0)
			{
				CodeBug();
			}
			endLineno = lineno;
		}

		public virtual int GetFunctionCount()
		{
			return functions == null ? 0 : functions.Count;
		}

		public virtual FunctionNode GetFunctionNode(int i)
		{
			return functions[i];
		}

		public virtual IList<FunctionNode> GetFunctions()
		{
			return functions == null ? EMPTY_LIST : functions;
		}

		/// <summary>
		/// Adds a
		/// <see cref="FunctionNode">FunctionNode</see>
		/// to the functions table for codegen.
		/// Does not set the parent of the node.
		/// </summary>
		/// <returns>the index of the function within its parent</returns>
		public virtual int AddFunction(FunctionNode fnNode)
		{
			if (fnNode == null)
			{
				CodeBug();
			}
			if (functions == null)
			{
				functions = new List<FunctionNode>();
			}
			functions.AddItem(fnNode);
			return functions.Count - 1;
		}

		public virtual int GetRegexpCount()
		{
			return regexps == null ? 0 : regexps.Count;
		}

		public virtual string GetRegexpString(int index)
		{
			return regexps[index].GetValue();
		}

		public virtual string GetRegexpFlags(int index)
		{
			return regexps[index].GetFlags();
		}

		/// <summary>Called by IRFactory to add a RegExp to the regexp table.</summary>
		/// <remarks>Called by IRFactory to add a RegExp to the regexp table.</remarks>
		public virtual void AddRegExp(RegExpLiteral re)
		{
			if (re == null)
			{
				CodeBug();
			}
			if (regexps == null)
			{
				regexps = new List<RegExpLiteral>();
			}
			regexps.AddItem(re);
			re.PutIntProp(REGEXP_PROP, regexps.Count - 1);
		}

		public virtual int GetIndexForNameNode(Node nameNode)
		{
			if (variableNames == null)
			{
				CodeBug();
			}
			Scope node = nameNode.GetScope();
			Symbol symbol = node == null ? null : node.GetSymbol(((Name)nameNode).GetIdentifier());
			return (symbol == null) ? -1 : symbol.GetIndex();
		}

		public virtual string GetParamOrVarName(int index)
		{
			if (variableNames == null)
			{
				CodeBug();
			}
			return variableNames[index];
		}

		public virtual int GetParamCount()
		{
			return paramCount;
		}

		public virtual int GetParamAndVarCount()
		{
			if (variableNames == null)
			{
				CodeBug();
			}
			return symbols.Count;
		}

		public virtual string[] GetParamAndVarNames()
		{
			if (variableNames == null)
			{
				CodeBug();
			}
			return variableNames;
		}

		public virtual bool[] GetParamAndVarConst()
		{
			if (variableNames == null)
			{
				CodeBug();
			}
			return isConsts;
		}

		internal virtual void AddSymbol(Symbol symbol)
		{
			if (variableNames != null)
			{
				CodeBug();
			}
			if (symbol.GetDeclType() == Token.LP)
			{
				paramCount++;
			}
			symbols.AddItem(symbol);
		}

		public virtual IList<Symbol> GetSymbols()
		{
			return symbols;
		}

		public virtual void SetSymbols(IList<Symbol> symbols)
		{
			this.symbols = symbols;
		}

		/// <summary>Assign every symbol a unique integer index.</summary>
		/// <remarks>
		/// Assign every symbol a unique integer index. Generate arrays of variable
		/// names and constness that can be indexed by those indices.
		/// </remarks>
		/// <param name="flattenAllTables">
		/// if true, flatten all symbol tables,
		/// included nested block scope symbol tables. If false, just flatten the
		/// script's or function's symbol table.
		/// </param>
		public virtual void FlattenSymbolTable(bool flattenAllTables)
		{
			if (!flattenAllTables)
			{
				IList<Symbol> newSymbols = new List<Symbol>();
				if (this.symbolTable != null)
				{
					// Just replace "symbols" with the symbols in this object's
					// symbol table. Can't just work from symbolTable map since
					// we need to retain duplicate parameters.
					for (int i = 0; i < symbols.Count; i++)
					{
						Symbol symbol = symbols[i];
						if (symbol.GetContainingTable() == this)
						{
							newSymbols.AddItem(symbol);
						}
					}
				}
				symbols = newSymbols;
			}
			variableNames = new string[symbols.Count];
			isConsts = new bool[symbols.Count];
			for (int i_1 = 0; i_1 < symbols.Count; i_1++)
			{
				Symbol symbol = symbols[i_1];
				variableNames[i_1] = symbol.GetName();
				isConsts[i_1] = symbol.GetDeclType() == Token.CONST;
				symbol.SetIndex(i_1);
			}
		}

		public virtual object GetCompilerData()
		{
			return compilerData;
		}

		public virtual void SetCompilerData(object data)
		{
			AssertNotNull(data);
			// Can only call once
			if (compilerData != null)
			{
				throw new InvalidOperationException();
			}
			compilerData = data;
		}

		public virtual string GetNextTempName()
		{
			return "$" + tempNumber++;
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
