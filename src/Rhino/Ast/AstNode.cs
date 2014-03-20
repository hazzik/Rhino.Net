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
	/// <summary>Base class for AST node types.</summary>
	/// <remarks>
	/// Base class for AST node types.  The goal of the AST is to represent the
	/// physical source code, to make it useful for code-processing tools such
	/// as IDEs or pretty-printers.  The parser must not rewrite the parse tree
	/// when producing this representation. <p>
	/// The
	/// <code>AstNode</code>
	/// hierarchy sits atop the older
	/// <see cref="Rhino.Node">Rhino.Node</see>
	/// class,
	/// which was designed for code generation.  The
	/// <code>Node</code>
	/// class is a
	/// flexible, weakly-typed class suitable for creating and rewriting code
	/// trees, but using it requires you to remember the exact ordering of the
	/// child nodes, which are kept in a linked list.  The
	/// <code>AstNode</code>
	/// hierarchy is a strongly-typed facade with named accessors for children
	/// and common properties, but under the hood it's still using a linked list
	/// of child nodes.  It isn't a very good idea to use the child list directly
	/// unless you know exactly what you're doing.</p>
	/// Note that
	/// <code>AstNode</code>
	/// records additional information, including
	/// the node's position, length, and parent node.  Also, some
	/// <code>AstNode</code>
	/// subclasses record some of their child nodes in instance members, since
	/// they are not needed for code generation.  In a nutshell, only the code
	/// generator should be mixing and matching
	/// <code>AstNode</code>
	/// and
	/// <code>Node</code>
	/// objects.<p>
	/// All offset fields in all subclasses of AstNode are relative to their
	/// parent.  For things like paren, bracket and keyword positions, the
	/// position is relative to the current node.  The node start position is
	/// relative to the parent node. <p>
	/// During the actual parsing, node positions are absolute; adding the node to
	/// its parent fixes up the offsets to be relative.  By the time you see the AST
	/// (e.g. using the
	/// <code>Visitor</code>
	/// interface), the offsets are relative. <p>
	/// <code>AstNode</code>
	/// objects have property lists accessible via the
	/// <see cref="Rhino.Node.GetProp(int)">Rhino.Node.GetProp(int)</see>
	/// and
	/// <see cref="Rhino.Node.PutProp(int, object)">Rhino.Node.PutProp(int, object)</see>
	/// methods.  The property lists are
	/// integer-keyed with arbitrary
	/// <code>Object</code>
	/// values.  For the most part the
	/// parser generating the AST avoids using properties, preferring fields for
	/// elements that are always set.  Property lists are intended for user-defined
	/// annotations to the tree.  The Rhino code generator acts as a client and
	/// uses node properties extensively.  You are welcome to use the property-list
	/// API for anything your client needs.<p>
	/// This hierarchy does not have separate branches for expressions and
	/// statements, as the distinction in JavaScript is not as clear-cut as in
	/// Java or C++. <p>
	/// </remarks>
	public abstract class AstNode : Node, IComparable<Rhino.Ast.AstNode>
	{
		protected internal int position = -1;

		protected internal int length = 1;

		protected internal Rhino.Ast.AstNode parent;

		private static readonly IDictionary<int, string> operatorNames = new Dictionary<int, string>();

		static AstNode()
		{
			operatorNames[Token.IN] = "in";
			operatorNames[Token.TYPEOF] = "typeof";
			operatorNames[Token.INSTANCEOF] = "instanceof";
			operatorNames[Token.DELPROP] = "delete";
			operatorNames[Token.COMMA] = ",";
			operatorNames[Token.COLON] = ":";
			operatorNames[Token.OR] = "||";
			operatorNames[Token.AND] = "&&";
			operatorNames[Token.INC] = "++";
			operatorNames[Token.DEC] = "--";
			operatorNames[Token.BITOR] = "|";
			operatorNames[Token.BITXOR] = "^";
			operatorNames[Token.BITAND] = "&";
			operatorNames[Token.EQ] = "==";
			operatorNames[Token.NE] = "!=";
			operatorNames[Token.LT] = "<";
			operatorNames[Token.GT] = ">";
			operatorNames[Token.LE] = "<=";
			operatorNames[Token.GE] = ">=";
			operatorNames[Token.LSH] = "<<";
			operatorNames[Token.RSH] = ">>";
			operatorNames[Token.URSH] = ">>>";
			operatorNames[Token.ADD] = "+";
			operatorNames[Token.SUB] = "-";
			operatorNames[Token.MUL] = "*";
			operatorNames[Token.DIV] = "/";
			operatorNames[Token.MOD] = "%";
			operatorNames[Token.NOT] = "!";
			operatorNames[Token.BITNOT] = "~";
			operatorNames[Token.POS] = "+";
			operatorNames[Token.NEG] = "-";
			operatorNames[Token.SHEQ] = "===";
			operatorNames[Token.SHNE] = "!==";
			operatorNames[Token.ASSIGN] = "=";
			operatorNames[Token.ASSIGN_BITOR] = "|=";
			operatorNames[Token.ASSIGN_BITAND] = "&=";
			operatorNames[Token.ASSIGN_LSH] = "<<=";
			operatorNames[Token.ASSIGN_RSH] = ">>=";
			operatorNames[Token.ASSIGN_URSH] = ">>>=";
			operatorNames[Token.ASSIGN_ADD] = "+=";
			operatorNames[Token.ASSIGN_SUB] = "-=";
			operatorNames[Token.ASSIGN_MUL] = "*=";
			operatorNames[Token.ASSIGN_DIV] = "/=";
			operatorNames[Token.ASSIGN_MOD] = "%=";
			operatorNames[Token.ASSIGN_BITXOR] = "^=";
			operatorNames[Token.VOID] = "void";
		}

		[System.Serializable]
		public class PositionComparator : IComparer<AstNode>
		{
			/// <summary>Sorts nodes by (relative) start position.</summary>
			/// <remarks>
			/// Sorts nodes by (relative) start position.  The start positions are
			/// relative to their parent, so this comparator is only meaningful for
			/// comparing siblings.
			/// </remarks>
			public virtual int Compare(AstNode n1, AstNode n2)
			{
				return n1.position - n2.position;
			}
		}

		public AstNode() : base(Token.ERROR)
		{
		}

		/// <summary>Constructs a new AstNode</summary>
		/// <param name="pos">the start position</param>
		public AstNode(int pos) : this()
		{
			position = pos;
		}

		/// <summary>Constructs a new AstNode</summary>
		/// <param name="pos">the start position</param>
		/// <param name="len">
		/// the number of characters spanned by the node in the source
		/// text
		/// </param>
		public AstNode(int pos, int len) : this()
		{
			position = pos;
			length = len;
		}

		/// <summary>Returns relative position in parent</summary>
		public virtual int GetPosition()
		{
			return position;
		}

		/// <summary>Sets relative position in parent</summary>
		public virtual void SetPosition(int position)
		{
			this.position = position;
		}

		/// <summary>Returns the absolute document position of the node.</summary>
		/// <remarks>
		/// Returns the absolute document position of the node.
		/// Computes it by adding the node's relative position
		/// to the relative positions of all its parents.
		/// </remarks>
		public virtual int GetAbsolutePosition()
		{
			int pos = position;
			AstNode parent = this.parent;
			while (parent != null)
			{
				pos += parent.GetPosition();
				parent = parent.GetParent();
			}
			return pos;
		}

		/// <summary>Returns node length</summary>
		public virtual int GetLength()
		{
			return length;
		}

		/// <summary>Sets node length</summary>
		public virtual void SetLength(int length)
		{
			this.length = length;
		}

		/// <summary>Sets the node start and end positions.</summary>
		/// <remarks>
		/// Sets the node start and end positions.
		/// Computes the length as (
		/// <code>end</code>
		/// -
		/// <code>position</code>
		/// ).
		/// </remarks>
		public virtual void SetBounds(int position, int end)
		{
			SetPosition(position);
			SetLength(end - position);
		}

		/// <summary>Make this node's position relative to a parent.</summary>
		/// <remarks>
		/// Make this node's position relative to a parent.
		/// Typically only used by the parser when constructing the node.
		/// </remarks>
		/// <param name="parentPosition">
		/// the absolute parent position; the
		/// current node position is assumed to be absolute and is
		/// decremented by parentPosition.
		/// </param>
		public virtual void SetRelative(int parentPosition)
		{
			this.position -= parentPosition;
		}

		/// <summary>
		/// Returns the node parent, or
		/// <code>null</code>
		/// if it has none
		/// </summary>
		public virtual AstNode GetParent()
		{
			return parent;
		}

		/// <summary>Sets the node parent.</summary>
		/// <remarks>
		/// Sets the node parent.  This method automatically adjusts the
		/// current node's start position to be relative to the new parent.
		/// </remarks>
		/// <param name="parent">
		/// the new parent. Can be
		/// <code>null</code>
		/// .
		/// </param>
		public virtual void SetParent(AstNode parent)
		{
			if (parent == this.parent)
			{
				return;
			}
			// Convert position back to absolute.
			if (this.parent != null)
			{
				SetRelative(-this.parent.GetPosition());
			}
			this.parent = parent;
			if (parent != null)
			{
				SetRelative(parent.GetPosition());
			}
		}

		/// <summary>Adds a child or function to the end of the block.</summary>
		/// <remarks>
		/// Adds a child or function to the end of the block.
		/// Sets the parent of the child to this node, and fixes up
		/// the start position of the child to be relative to this node.
		/// Sets the length of this node to include the new child.
		/// </remarks>
		/// <param name="kid">the child</param>
		/// <exception cref="System.ArgumentException">
		/// if kid is
		/// <code>null</code>
		/// </exception>
		public virtual void AddChild(AstNode kid)
		{
			AssertNotNull(kid);
			int end = kid.GetPosition() + kid.GetLength();
			SetLength(end - this.GetPosition());
			AddChildToBack(kid);
			kid.SetParent(this);
		}

		/// <summary>Returns the root of the tree containing this node.</summary>
		/// <remarks>Returns the root of the tree containing this node.</remarks>
		/// <returns>
		/// the
		/// <see cref="AstRoot">AstRoot</see>
		/// at the root of this node's parent
		/// chain, or
		/// <code>null</code>
		/// if the topmost parent is not an
		/// <code>AstRoot</code>
		/// .
		/// </returns>
		public virtual AstRoot GetAstRoot()
		{
			AstNode parent = this;
			// this node could be the AstRoot
			while (parent != null && !(parent is AstRoot))
			{
				parent = parent.GetParent();
			}
			return (AstRoot)parent;
		}

		/// <summary>Emits source code for this node.</summary>
		/// <remarks>
		/// Emits source code for this node.  Callee is responsible for calling this
		/// function recursively on children, incrementing indent as appropriate.<p>
		/// Note: if the parser was in error-recovery mode, some AST nodes may have
		/// <code>null</code>
		/// children that are expected to be non-
		/// <code>null</code>
		/// when no errors are present.  In this situation, the behavior of the
		/// <code>toSource</code>
		/// method is undefined:
		/// <code>toSource</code>
		/// implementations may assume that the AST node is error-free, since it is
		/// intended to be invoked only at runtime after a successful parse.<p>
		/// </remarks>
		/// <param name="depth">
		/// the current recursion depth, typically beginning at 0
		/// when called on the root node.
		/// </param>
		public abstract string ToSource(int depth);

		/// <summary>Prints the source indented to depth 0.</summary>
		/// <remarks>Prints the source indented to depth 0.</remarks>
		public virtual string ToSource()
		{
			return this.ToSource(0);
		}

		/// <summary>Constructs an indentation string.</summary>
		/// <remarks>Constructs an indentation string.</remarks>
		/// <param name="indent">the number of indentation steps</param>
		public virtual string MakeIndent(int indent)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < indent; i++)
			{
				sb.Append("  ");
			}
			return sb.ToString();
		}

		/// <summary>
		/// Returns a short, descriptive name for the node, such as
		/// "ArrayComprehension".
		/// </summary>
		/// <remarks>
		/// Returns a short, descriptive name for the node, such as
		/// "ArrayComprehension".
		/// </remarks>
		public virtual string ShortName()
		{
			string classname = ((object) this).GetType().FullName;
			int last = classname.LastIndexOf(".");
			return classname.Substring(last + 1);
		}

		/// <summary>Returns the string name for this operator.</summary>
		/// <remarks>Returns the string name for this operator.</remarks>
		/// <param name="op">
		/// the token type, e.g.
		/// <see cref="Rhino.Token.ADD">Rhino.Token.ADD</see>
		/// or
		/// <see cref="Rhino.Token.TYPEOF">Rhino.Token.TYPEOF</see>
		/// </param>
		/// <returns>the source operator string, such as "+" or "typeof"</returns>
		public static string OperatorToString(int op)
		{
			string result;
			if (!operatorNames.TryGetValue(op, out result))
			{
				throw new ArgumentException("Invalid operator: " + op);
			}
			return result;
		}

		/// <summary>Visits this node and its children in an arbitrary order.</summary>
		/// <remarks>
		/// Visits this node and its children in an arbitrary order. <p>
		/// It's up to each node subclass to decide the order for processing
		/// its children.  The subclass also decides (and should document)
		/// which child nodes are not passed to the
		/// <code>NodeVisitor</code>
		/// .
		/// For instance, nodes representing keywords like
		/// <code>each</code>
		/// or
		/// <code>in</code>
		/// may not be passed to the visitor object.  The visitor
		/// can simply query the current node for these children if desired.<p>
		/// Generally speaking, the order will be deterministic; the order is
		/// whatever order is decided by each child node.  Normally child nodes
		/// will try to visit their children in lexical order, but there may
		/// be exceptions to this rule.<p>
		/// </remarks>
		/// <param name="visitor">the object to call with this node and its children</param>
		public abstract void Visit(NodeVisitor visitor);

		// subclasses with potential side effects should override this
		public override bool HasSideEffects()
		{
			switch (GetType())
			{
				case Token.ASSIGN:
				case Token.ASSIGN_ADD:
				case Token.ASSIGN_BITAND:
				case Token.ASSIGN_BITOR:
				case Token.ASSIGN_BITXOR:
				case Token.ASSIGN_DIV:
				case Token.ASSIGN_LSH:
				case Token.ASSIGN_MOD:
				case Token.ASSIGN_MUL:
				case Token.ASSIGN_RSH:
				case Token.ASSIGN_SUB:
				case Token.ASSIGN_URSH:
				case Token.BLOCK:
				case Token.BREAK:
				case Token.CALL:
				case Token.CATCH:
				case Token.CATCH_SCOPE:
				case Token.CONST:
				case Token.CONTINUE:
				case Token.DEC:
				case Token.DELPROP:
				case Token.DEL_REF:
				case Token.DO:
				case Token.ELSE:
				case Token.ENTERWITH:
				case Token.ERROR:
				case Token.EXPORT:
				case Token.EXPR_RESULT:
				case Token.FINALLY:
				case Token.FUNCTION:
				case Token.FOR:
				case Token.GOTO:
				case Token.IF:
				case Token.IFEQ:
				case Token.IFNE:
				case Token.IMPORT:
				case Token.INC:
				case Token.JSR:
				case Token.LABEL:
				case Token.LEAVEWITH:
				case Token.LET:
				case Token.LETEXPR:
				case Token.LOCAL_BLOCK:
				case Token.LOOP:
				case Token.NEW:
				case Token.REF_CALL:
				case Token.RETHROW:
				case Token.RETURN:
				case Token.RETURN_RESULT:
				case Token.SEMI:
				case Token.SETELEM:
				case Token.SETELEM_OP:
				case Token.SETNAME:
				case Token.SETPROP:
				case Token.SETPROP_OP:
				case Token.SETVAR:
				case Token.SET_REF:
				case Token.SET_REF_OP:
				case Token.SWITCH:
				case Token.TARGET:
				case Token.THROW:
				case Token.TRY:
				case Token.VAR:
				case Token.WHILE:
				case Token.WITH:
				case Token.WITHEXPR:
				case Token.YIELD:
				{
					// Avoid cascaded error messages
					return true;
				}

				default:
				{
					return false;
				}
			}
		}

		/// <summary>
		/// Bounces an IllegalArgumentException up if arg is
		/// <code>null</code>
		/// .
		/// </summary>
		/// <param name="arg">any method argument</param>
		/// <exception cref="System.ArgumentException">
		/// if the argument is
		/// <code>null</code>
		/// </exception>
		protected internal virtual void AssertNotNull(object arg)
		{
			if (arg == null)
			{
				throw new ArgumentException("arg cannot be null");
			}
		}

		/// <summary>
		/// Prints a comma-separated item list into a
		/// <see cref="System.Text.StringBuilder">System.Text.StringBuilder</see>
		/// .
		/// </summary>
		/// <param name="items">a list to print</param>
		/// <param name="sb">
		/// a
		/// <see cref="System.Text.StringBuilder">System.Text.StringBuilder</see>
		/// into which to print
		/// </param>
		protected internal virtual void PrintList<T>(IList<T> items, StringBuilder sb) where T:AstNode
		{
			int max = items.Count;
			int count = 0;
			foreach (T item in items)
			{
				sb.Append(item.ToSource(0));
				if (count++ < max - 1)
				{
					sb.Append(", ");
				}
				else
				{
					if (item is EmptyExpression)
					{
						sb.Append(",");
					}
				}
			}
		}

		/// <seealso cref="Rhino.Kit.CodeBug()">Rhino.Kit.CodeBug()</seealso>
		/// <exception cref="System.Exception"></exception>
		public static Exception CodeBug()
		{
			throw Kit.CodeBug();
		}

		// TODO(stevey):  think of a way to have polymorphic toString
		// methods while keeping the ability to use Node.toString for
		// dumping the IR with Token.printTrees.  Most likely:  change
		// Node.toString to be Node.dumpTree and change callers to use that.
		// For now, need original toString, to compare output to old Rhino's.
		//     @Override
		//     public String toString() {
		//         return this.getClass().getName() + ": " +
		//             Token.typeToName(getType());
		//     }
		/// <summary>
		/// Returns the innermost enclosing function, or
		/// <code>null</code>
		/// if not in a
		/// function.  Begins the search with this node's parent.
		/// </summary>
		/// <returns>
		/// the
		/// <see cref="FunctionNode">FunctionNode</see>
		/// enclosing this node, else
		/// <code>null</code>
		/// </returns>
		public virtual FunctionNode GetEnclosingFunction()
		{
			AstNode parent = this.GetParent();
			while (parent != null && !(parent is FunctionNode))
			{
				parent = parent.GetParent();
			}
			return (FunctionNode)parent;
		}

		/// <summary>
		/// Returns the innermost enclosing
		/// <see cref="Scope">Scope</see>
		/// node, or
		/// <code>null</code>
		/// if we're not nested in a scope.  Begins the search with this node's parent.
		/// Note that this is not the same as the defining scope for a
		/// <see cref="Name">Name</see>
		/// .
		/// </summary>
		/// <returns>
		/// the
		/// <see cref="Scope">Scope</see>
		/// enclosing this node, else
		/// <code>null</code>
		/// </returns>
		public virtual Scope GetEnclosingScope()
		{
			AstNode parent = this.GetParent();
			while (parent != null && !(parent is Scope))
			{
				parent = parent.GetParent();
			}
			return (Scope)parent;
		}

		/// <summary>Permits AST nodes to be sorted based on start position and length.</summary>
		/// <remarks>
		/// Permits AST nodes to be sorted based on start position and length.
		/// This makes it easy to sort Comment and Error nodes into a set of
		/// other AST nodes:  just put them all into a
		/// <see cref="SortedSet{T}">Sharpen.SortedSet&lt;E&gt;</see>
		/// ,
		/// for instance.
		/// </remarks>
		/// <param name="other">another node</param>
		/// <returns>
		/// -1 if this node's start position is less than
		/// <code>other</code>
		/// 's
		/// start position.  If tied, -1 if this node's length is less than
		/// <code>other</code>
		/// 's length.  If the lengths are equal, sorts abitrarily
		/// on hashcode unless the nodes are the same per
		/// <see cref="object.Equals(object)">object.Equals(object)</see>
		/// .
		/// </returns>
		public virtual int CompareTo(AstNode other)
		{
			if (this.Equals(other))
			{
				return 0;
			}
			int abs1 = this.GetAbsolutePosition();
			int abs2 = other.GetAbsolutePosition();
			if (abs1 < abs2)
			{
				return -1;
			}
			if (abs2 < abs1)
			{
				return 1;
			}
			int len1 = this.GetLength();
			int len2 = other.GetLength();
			if (len1 < len2)
			{
				return -1;
			}
			if (len2 < len1)
			{
				return 1;
			}
			return this.GetHashCode() - other.GetHashCode();
		}

		/// <summary>Returns the depth of this node.</summary>
		/// <remarks>
		/// Returns the depth of this node.  The root is depth 0, its
		/// children are depth 1, and so on.
		/// </remarks>
		/// <returns>the node depth in the tree</returns>
		public virtual int Depth()
		{
			return parent == null ? 0 : 1 + parent.Depth();
		}

		protected internal class DebugPrintVisitor : NodeVisitor
		{
			private StringBuilder buffer;

			private const int DEBUG_INDENT = 2;

			public DebugPrintVisitor(StringBuilder buf)
			{
				buffer = buf;
			}

			public override string ToString()
			{
				return buffer.ToString();
			}

			private string MakeIndent(int depth)
			{
				StringBuilder sb = new StringBuilder(DEBUG_INDENT * depth);
				for (int i = 0; i < (DEBUG_INDENT * depth); i++)
				{
					sb.Append(" ");
				}
				return sb.ToString();
			}

			public virtual bool Visit(AstNode node)
			{
				int tt = node.GetType();
				string name = Token.TypeToName(tt);
				buffer.Append(node.GetAbsolutePosition()).Append("\t");
				buffer.Append(MakeIndent(node.Depth()));
				buffer.Append(name).Append(" ");
				buffer.Append(node.GetPosition()).Append(" ");
				buffer.Append(node.GetLength());
				if (tt == Token.NAME)
				{
					buffer.Append(" ").Append(((Name)node).GetIdentifier());
				}
				buffer.Append("\n");
				return true;
			}
			// process kids
		}

		/// <summary>Return the line number recorded for this node.</summary>
		/// <remarks>
		/// Return the line number recorded for this node.
		/// If no line number was recorded, searches the parent chain.
		/// </remarks>
		/// <returns>the nearest line number, or -1 if none was found</returns>
		public override int GetLineno()
		{
			if (lineno != -1)
			{
				return lineno;
			}
			if (parent != null)
			{
				return parent.GetLineno();
			}
			return -1;
		}

		/// <summary>
		/// Returns a debugging representation of the parse tree
		/// starting at this node.
		/// </summary>
		/// <remarks>
		/// Returns a debugging representation of the parse tree
		/// starting at this node.
		/// </remarks>
		/// <returns>
		/// a very verbose indented printout of the tree.
		/// The format of each line is:  abs-pos  name position length [identifier]
		/// </returns>
		public virtual string DebugPrint()
		{
			AstNode.DebugPrintVisitor dpv = new AstNode.DebugPrintVisitor(new StringBuilder(1000));
			Visit(dpv);
			return dpv.ToString();
		}
	}
}
