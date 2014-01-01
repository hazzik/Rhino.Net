/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Ast;
using Label = System.Reflection.Emit.Label;

namespace Rhino
{
	/// <summary>This class implements the root of the intermediate representation.</summary>
	/// <remarks>This class implements the root of the intermediate representation.</remarks>
	/// <author>Norris Boyd</author>
	/// <author>Mike McCabe</author>
	public class Node : IEnumerable<Rhino.Node>
	{
		public const int FUNCTION_PROP = 1;

		public const int LOCAL_PROP = 2;

		public const int LOCAL_BLOCK_PROP = 3;

		public const int REGEXP_PROP = 4;

		public const int CASEARRAY_PROP = 5;

		public const int TARGETBLOCK_PROP = 6;

		public const int VARIABLE_PROP = 7;

		public const int ISNUMBER_PROP = 8;

		public const int DIRECTCALL_PROP = 9;

		public const int SPECIALCALL_PROP = 10;

		public const int SKIP_INDEXES_PROP = 11;

		public const int OBJECT_IDS_PROP = 12;

		public const int INCRDECR_PROP = 13;

		public const int CATCH_SCOPE_PROP = 14;

		public const int LABEL_ID_PROP = 15;

		public const int MEMBER_TYPE_PROP = 16;

		public const int NAME_PROP = 17;

		public const int CONTROL_BLOCK_PROP = 18;

		public const int PARENTHESIZED_PROP = 19;

		public const int GENERATOR_END_PROP = 20;

		public const int DESTRUCTURING_ARRAY_LENGTH = 21;

		public const int DESTRUCTURING_NAMES = 22;

		public const int DESTRUCTURING_PARAMS = 23;

		public const int JSDOC_PROP = 24;

		public const int EXPRESSION_CLOSURE_PROP = 25;

		public const int DESTRUCTURING_SHORTHAND = 26;

		public const int LAST_PROP = 26;

		public const int BOTH = 0;

		public const int LEFT = 1;

		public const int RIGHT = 2;

		public const int NON_SPECIALCALL = 0;

		public const int SPECIALCALL_EVAL = 1;

		public const int SPECIALCALL_WITH = 2;

		public const int DECR_FLAG = unchecked((int)(0x1));

		public const int POST_FLAG = unchecked((int)(0x2));

		public const int PROPERTY_FLAG = unchecked((int)(0x1));

		public const int ATTRIBUTE_FLAG = unchecked((int)(0x2));

		public const int DESCENDANTS_FLAG = unchecked((int)(0x4));

		public class PropListItem
		{
			internal Node.PropListItem next;

			internal int type;

			internal int intValue;

			internal object objectValue;
			//  the following properties are defined and manipulated by the
			//  optimizer -
			//  TARGETBLOCK_PROP - the block referenced by a branch node
			//  VARIABLE_PROP - the variable referenced by a BIND or NAME node
			//  ISNUMBER_PROP - this node generates code on Number children and
			//                  delivers a Number result (as opposed to Objects)
			//  DIRECTCALL_PROP - this call node should emit code to test the function
			//                    object against the known class and call direct if it
			//                    matches.
			// array of skipped indexes of array literal
			// array of properties for object literal
			// pre or post type of increment/decrement
			// index of catch scope block in catch
			// label id: code generation uses it
			// type of element access operation
			// property name
			// flags a control block that can drop off
			// expression is parenthesized
			// JS 1.8 expression closure pseudo-return
			// JS 1.8 destructuring shorthand
			// values of ISNUMBER_PROP to specify
			// which of the children are Number types
			// values for SPECIALCALL_PROP
			// flags for INCRDECR_PROP
			// flags for MEMBER_TYPE_PROP
			// property access: element is valid name
			// x.@y or x..@y
			// x..y or x..@i
		}

		public Node(int nodeType)
		{
			type = nodeType;
		}

		public Node(int nodeType, Node child)
		{
			type = nodeType;
			first = last = child;
			child.next = null;
		}

		public Node(int nodeType, Node left, Node right)
		{
			type = nodeType;
			first = left;
			last = right;
			left.next = right;
			right.next = null;
		}

		public Node(int nodeType, Node left, Node mid, Node right)
		{
			type = nodeType;
			first = left;
			last = right;
			left.next = mid;
			mid.next = right;
			right.next = null;
		}

		public Node(int nodeType, int line)
		{
			type = nodeType;
			lineno = line;
		}

		public Node(int nodeType, Node child, int line) : this(nodeType, child)
		{
			lineno = line;
		}

		public Node(int nodeType, Node left, Node right, int line) : this(nodeType, left, right)
		{
			lineno = line;
		}

		public Node(int nodeType, Node left, Node mid, Node right, int line) : this(nodeType, left, mid, right)
		{
			lineno = line;
		}

		public static Node NewNumber(double number)
		{
			NumberLiteral n = new NumberLiteral();
			n.SetNumber(number);
			return n;
		}

		public static Node NewString(string str)
		{
			return NewString(Token.STRING, str);
		}

		public static Node NewString(int type, string str)
		{
			Name name = new Name();
			name.SetIdentifier(str);
			name.SetType(type);
			return name;
		}

		public virtual int GetType()
		{
			return type;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>Sets the node type and returns this node.</summary>
		/// <remarks>Sets the node type and returns this node.</remarks>
		public virtual Node SetType(int type)
		{
			this.type = type;
			return this;
		}

		/// <summary>Gets the JsDoc comment string attached to this node.</summary>
		/// <remarks>Gets the JsDoc comment string attached to this node.</remarks>
		/// <returns>
		/// the comment string or
		/// <code>null</code>
		/// if no JsDoc is attached to
		/// this node
		/// </returns>
		public virtual string GetJsDoc()
		{
			Comment comment = GetJsDocNode();
			if (comment != null)
			{
				return comment.GetValue();
			}
			return null;
		}

		/// <summary>Gets the JsDoc Comment object attached to this node.</summary>
		/// <remarks>Gets the JsDoc Comment object attached to this node.</remarks>
		/// <returns>
		/// the Comment or
		/// <code>null</code>
		/// if no JsDoc is attached to
		/// this node
		/// </returns>
		public virtual Comment GetJsDocNode()
		{
			return (Comment)GetProp(JSDOC_PROP);
		}

		/// <summary>Sets the JsDoc comment string attached to this node.</summary>
		/// <remarks>Sets the JsDoc comment string attached to this node.</remarks>
		public virtual void SetJsDocNode(Comment jsdocNode)
		{
			PutProp(JSDOC_PROP, jsdocNode);
		}

		public virtual bool HasChildren()
		{
			return first != null;
		}

		public virtual Node GetFirstChild()
		{
			return first;
		}

		public virtual Node GetLastChild()
		{
			return last;
		}

		public virtual Node GetNext()
		{
			return next;
		}

		public virtual Node GetChildBefore(Node child)
		{
			if (child == first)
			{
				return null;
			}
			Node n = first;
			while (n.next != child)
			{
				n = n.next;
				if (n == null)
				{
					throw new Exception("node is not a child");
				}
			}
			return n;
		}

		public virtual Node GetLastSibling()
		{
			Node n = this;
			while (n.next != null)
			{
				n = n.next;
			}
			return n;
		}

		public virtual void AddChildToFront(Node child)
		{
			child.next = first;
			first = child;
			if (last == null)
			{
				last = child;
			}
		}

		public virtual void AddChildToBack(Node child)
		{
			child.next = null;
			if (last == null)
			{
				first = last = child;
				return;
			}
			last.next = child;
			last = child;
		}

		public virtual void AddChildrenToFront(Node children)
		{
			Node lastSib = children.GetLastSibling();
			lastSib.next = first;
			first = children;
			if (last == null)
			{
				last = lastSib;
			}
		}

		public virtual void AddChildrenToBack(Node children)
		{
			if (last != null)
			{
				last.next = children;
			}
			last = children.GetLastSibling();
			if (first == null)
			{
				first = children;
			}
		}

		/// <summary>Add 'child' before 'node'.</summary>
		/// <remarks>Add 'child' before 'node'.</remarks>
		public virtual void AddChildBefore(Node newChild, Node node)
		{
			if (newChild.next != null)
			{
				throw new Exception("newChild had siblings in addChildBefore");
			}
			if (first == node)
			{
				newChild.next = first;
				first = newChild;
				return;
			}
			Node prev = GetChildBefore(node);
			AddChildAfter(newChild, prev);
		}

		/// <summary>Add 'child' after 'node'.</summary>
		/// <remarks>Add 'child' after 'node'.</remarks>
		public virtual void AddChildAfter(Node newChild, Node node)
		{
			if (newChild.next != null)
			{
				throw new Exception("newChild had siblings in addChildAfter");
			}
			newChild.next = node.next;
			node.next = newChild;
			if (last == node)
			{
				last = newChild;
			}
		}

		public virtual void RemoveChild(Node child)
		{
			Node prev = GetChildBefore(child);
			if (prev == null)
			{
				first = first.next;
			}
			else
			{
				prev.next = child.next;
			}
			if (child == last)
			{
				last = prev;
			}
			child.next = null;
		}

		public virtual void ReplaceChild(Node child, Node newChild)
		{
			newChild.next = child.next;
			if (child == first)
			{
				first = newChild;
			}
			else
			{
				Node prev = GetChildBefore(child);
				prev.next = newChild;
			}
			if (child == last)
			{
				last = newChild;
			}
			child.next = null;
		}

		public virtual void ReplaceChildAfter(Node prevChild, Node newChild)
		{
			Node child = prevChild.next;
			newChild.next = child.next;
			prevChild.next = newChild;
			if (child == last)
			{
				last = newChild;
			}
			child.next = null;
		}

		public virtual void RemoveChildren()
		{
			first = last = null;
		}

		private static readonly Node NOT_SET = new Node(Token.ERROR);

		/// <summary>
		/// Returns an
		/// <see cref="System.Collections.IEnumerator{E}">System.Collections.IEnumerator&lt;E&gt;</see>
		/// over the node's children.
		/// </summary>
		public virtual IEnumerator<Node> GetEnumerator()
		{
			var cursor = first;
			while (cursor != null)
			{
				var prev = cursor;
				cursor = cursor.next;
				yield return prev;
			}
		}

		private static string PropToString(int propType)
		{
			// If Context.printTrees is false, the compiler
			// can remove all these strings.
			return null;
		}

		private Node.PropListItem LookupProperty(int propType)
		{
			Node.PropListItem x = propListHead;
			while (x != null && propType != x.type)
			{
				x = x.next;
			}
			return x;
		}

		private Node.PropListItem EnsureProperty(int propType)
		{
			Node.PropListItem item = LookupProperty(propType);
			if (item == null)
			{
				item = new Node.PropListItem();
				item.type = propType;
				item.next = propListHead;
				propListHead = item;
			}
			return item;
		}

		public virtual void RemoveProp(int propType)
		{
			Node.PropListItem x = propListHead;
			if (x != null)
			{
				Node.PropListItem prev = null;
				while (x.type != propType)
				{
					prev = x;
					x = x.next;
					if (x == null)
					{
						return;
					}
				}
				if (prev == null)
				{
					propListHead = x.next;
				}
				else
				{
					prev.next = x.next;
				}
			}
		}

		public virtual object GetProp(int propType)
		{
			Node.PropListItem item = LookupProperty(propType);
			if (item == null)
			{
				return null;
			}
			return item.objectValue;
		}

		public virtual int GetIntProp(int propType, int defaultValue)
		{
			Node.PropListItem item = LookupProperty(propType);
			if (item == null)
			{
				return defaultValue;
			}
			return item.intValue;
		}

		public virtual int GetExistingIntProp(int propType)
		{
			Node.PropListItem item = LookupProperty(propType);
			if (item == null)
			{
				Kit.CodeBug();
			}
			return item.intValue;
		}

		public virtual void PutProp(int propType, object prop)
		{
			if (prop == null)
			{
				RemoveProp(propType);
			}
			else
			{
				Node.PropListItem item = EnsureProperty(propType);
				item.objectValue = prop;
			}
		}

		public virtual void PutIntProp(int propType, int prop)
		{
			Node.PropListItem item = EnsureProperty(propType);
			item.intValue = prop;
		}

		/// <summary>Return the line number recorded for this node.</summary>
		/// <remarks>Return the line number recorded for this node.</remarks>
		/// <returns>the line number</returns>
		public virtual int GetLineno()
		{
			return lineno;
		}

		public virtual void SetLineno(int lineno)
		{
			this.lineno = lineno;
		}

		/// <summary>Can only be called when <tt>getType() == Token.NUMBER</tt></summary>
		public double GetDouble()
		{
			return ((NumberLiteral)this).GetNumber();
		}

		public void SetDouble(double number)
		{
			((NumberLiteral)this).SetNumber(number);
		}

		/// <summary>Can only be called when node has String context.</summary>
		/// <remarks>Can only be called when node has String context.</remarks>
		public string GetString()
		{
			return ((Name)this).GetIdentifier();
		}

		/// <summary>Can only be called when node has String context.</summary>
		/// <remarks>Can only be called when node has String context.</remarks>
		public void SetString(string s)
		{
			if (s == null)
			{
				Kit.CodeBug();
			}
			((Name)this).SetIdentifier(s);
		}

		/// <summary>Can only be called when node has String context.</summary>
		/// <remarks>Can only be called when node has String context.</remarks>
		public virtual Scope GetScope()
		{
			return ((Name)this).GetScope();
		}

		/// <summary>Can only be called when node has String context.</summary>
		/// <remarks>Can only be called when node has String context.</remarks>
		public virtual void SetScope(Scope s)
		{
			if (s == null)
			{
				Kit.CodeBug();
			}
			if (!(this is Name))
			{
				throw Kit.CodeBug();
			}
			((Name)this).SetScope(s);
		}

		public static Node NewTarget()
		{
			return new Node(Token.TARGET);
		}

		public int LabelId()
		{
			if (type != Token.TARGET && type != Token.YIELD)
			{
				Kit.CodeBug();
			}
			return GetIntProp(LABEL_ID_PROP, -1);
		}

		public void LabelId(int labelId)
		{
			if (type != Token.TARGET && type != Token.YIELD)
			{
				Kit.CodeBug();
			}
			PutIntProp(LABEL_ID_PROP, labelId);
		}

		/// <summary>
		/// These flags enumerate the possible ways a statement/function can
		/// terminate.
		/// </summary>
		/// <remarks>
		/// These flags enumerate the possible ways a statement/function can
		/// terminate. These flags are used by endCheck() and by the Parser to
		/// detect inconsistent return usage.
		/// END_UNREACHED is reserved for code paths that are assumed to always be
		/// able to execute (example: throw, continue)
		/// END_DROPS_OFF indicates if the statement can transfer control to the
		/// next one. Statement such as return dont. A compound statement may have
		/// some branch that drops off control to the next statement.
		/// END_RETURNS indicates that the statement can return (without arguments)
		/// END_RETURNS_VALUE indicates that the statement can return a value.
		/// A compound statement such as
		/// if (condition) {
		/// return value;
		/// }
		/// Will be detected as (END_DROPS_OFF | END_RETURN_VALUE) by endCheck()
		/// </remarks>
		public const int END_UNREACHED = 0;

		public const int END_DROPS_OFF = 1;

		public const int END_RETURNS = 2;

		public const int END_RETURNS_VALUE = 4;

		public const int END_YIELDS = 8;

		/// <summary>
		/// Checks that every return usage in a function body is consistent with the
		/// requirements of strict-mode.
		/// </summary>
		/// <remarks>
		/// Checks that every return usage in a function body is consistent with the
		/// requirements of strict-mode.
		/// </remarks>
		/// <returns>true if the function satisfies strict mode requirement.</returns>
		public virtual bool HasConsistentReturnUsage()
		{
			int n = EndCheck();
			return (n & END_RETURNS_VALUE) == 0 || (n & (END_DROPS_OFF | END_RETURNS | END_YIELDS)) == 0;
		}

		/// <summary>Returns in the then and else blocks must be consistent with each other.</summary>
		/// <remarks>
		/// Returns in the then and else blocks must be consistent with each other.
		/// If there is no else block, then the return statement can fall through.
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheckIf()
		{
			Node th;
			Node el;
			int rv = END_UNREACHED;
			th = next;
			el = ((Jump)this).target;
			rv = th.EndCheck();
			if (el != null)
			{
				rv |= el.EndCheck();
			}
			else
			{
				rv |= END_DROPS_OFF;
			}
			return rv;
		}

		/// <summary>Consistency of return statements is checked between the case statements.</summary>
		/// <remarks>
		/// Consistency of return statements is checked between the case statements.
		/// If there is no default, then the switch can fall through. If there is a
		/// default,we check to see if all code paths in the default return or if
		/// there is a code path that can fall through.
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheckSwitch()
		{
			int rv = END_UNREACHED;
			// examine the cases
			//         for (n = first.next; n != null; n = n.next)
			//         {
			//             if (n.type == Token.CASE) {
			//                 rv |= ((Jump)n).target.endCheck();
			//             } else
			//                 break;
			//         }
			//         // we don't care how the cases drop into each other
			//         rv &= ~END_DROPS_OFF;
			//         // examine the default
			//         n = ((Jump)this).getDefault();
			//         if (n != null)
			//             rv |= n.endCheck();
			//         else
			//             rv |= END_DROPS_OFF;
			//         // remove the switch block
			//         rv |= getIntProp(CONTROL_BLOCK_PROP, END_UNREACHED);
			return rv;
		}

		/// <summary>
		/// If the block has a finally, return consistency is checked in the
		/// finally block.
		/// </summary>
		/// <remarks>
		/// If the block has a finally, return consistency is checked in the
		/// finally block. If all code paths in the finally returns, then the
		/// returns in the try-catch blocks don't matter. If there is a code path
		/// that does not return or if there is no finally block, the returns
		/// of the try and catch blocks are checked for mismatch.
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheckTry()
		{
			int rv = END_UNREACHED;
			// a TryStatement isn't a jump - needs rewriting
			// check the finally if it exists
			//         n = ((Jump)this).getFinally();
			//         if(n != null) {
			//             rv = n.next.first.endCheck();
			//         } else {
			//             rv = END_DROPS_OFF;
			//         }
			//         // if the finally block always returns, then none of the returns
			//         // in the try or catch blocks matter
			//         if ((rv & END_DROPS_OFF) != 0) {
			//             rv &= ~END_DROPS_OFF;
			//             // examine the try block
			//             rv |= first.endCheck();
			//             // check each catch block
			//             n = ((Jump)this).target;
			//             if (n != null)
			//             {
			//                 // point to the first catch_scope
			//                 for (n = n.next.first; n != null; n = n.next.next)
			//                 {
			//                     // check the block of user code in the catch_scope
			//                     rv |= n.next.first.next.first.endCheck();
			//                 }
			//             }
			//         }
			return rv;
		}

		/// <summary>Return statement in the loop body must be consistent.</summary>
		/// <remarks>
		/// Return statement in the loop body must be consistent. The default
		/// assumption for any kind of a loop is that it will eventually terminate.
		/// The only exception is a loop with a constant true condition. Code that
		/// follows such a loop is examined only if one can statically determine
		/// that there is a break out of the loop.
		/// <pre>
		/// for(&lt;&gt; ; &lt;&gt;; &lt;&gt;) {}
		/// for(&lt;&gt; in &lt;&gt; ) {}
		/// while(&lt;&gt;) { }
		/// do { } while(&lt;&gt;)
		/// </pre>
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheckLoop()
		{
			Node n;
			int rv = END_UNREACHED;
			// To find the loop body, we look at the second to last node of the
			// loop node, which should be the predicate that the loop should
			// satisfy.
			// The target of the predicate is the loop-body for all 4 kinds of
			// loops.
			for (n = first; n.next != last; n = n.next)
			{
			}
			if (n.type != Token.IFEQ)
			{
				return END_DROPS_OFF;
			}
			// The target's next is the loop body block
			rv = ((Jump)n).target.next.EndCheck();
			// check to see if the loop condition is true
			if (n.first.type == Token.TRUE)
			{
				rv &= ~END_DROPS_OFF;
			}
			// look for effect of breaks
			rv |= GetIntProp(CONTROL_BLOCK_PROP, END_UNREACHED);
			return rv;
		}

		/// <summary>A general block of code is examined statement by statement.</summary>
		/// <remarks>
		/// A general block of code is examined statement by statement. If any
		/// statement (even compound ones) returns in all branches, then subsequent
		/// statements are not examined.
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheckBlock()
		{
			Node n;
			int rv = END_DROPS_OFF;
			// check each statment and if the statement can continue onto the next
			// one, then check the next statement
			for (n = first; ((rv & END_DROPS_OFF) != 0) && n != null; n = n.next)
			{
				rv &= ~END_DROPS_OFF;
				rv |= n.EndCheck();
			}
			return rv;
		}

		/// <summary>A labelled statement implies that there maybe a break to the label.</summary>
		/// <remarks>
		/// A labelled statement implies that there maybe a break to the label. The
		/// function processes the labelled statement and then checks the
		/// CONTROL_BLOCK_PROP property to see if there is ever a break to the
		/// particular label.
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheckLabel()
		{
			int rv = END_UNREACHED;
			rv = next.EndCheck();
			rv |= GetIntProp(CONTROL_BLOCK_PROP, END_UNREACHED);
			return rv;
		}

		/// <summary>
		/// When a break is encountered annotate the statement being broken
		/// out of by setting its CONTROL_BLOCK_PROP property.
		/// </summary>
		/// <remarks>
		/// When a break is encountered annotate the statement being broken
		/// out of by setting its CONTROL_BLOCK_PROP property.
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheckBreak()
		{
			Node n = ((Jump)this).GetJumpStatement();
			n.PutIntProp(CONTROL_BLOCK_PROP, END_DROPS_OFF);
			return END_UNREACHED;
		}

		/// <summary>
		/// endCheck() examines the body of a function, doing a basic reachability
		/// analysis and returns a combination of flags END_* flags that indicate
		/// how the function execution can terminate.
		/// </summary>
		/// <remarks>
		/// endCheck() examines the body of a function, doing a basic reachability
		/// analysis and returns a combination of flags END_* flags that indicate
		/// how the function execution can terminate. These constitute only the
		/// pessimistic set of termination conditions. It is possible that at
		/// runtime certain code paths will never be actually taken. Hence this
		/// analysis will flag errors in cases where there may not be errors.
		/// </remarks>
		/// <returns>logical OR of END_* flags</returns>
		private int EndCheck()
		{
			switch (type)
			{
				case Token.BREAK:
				{
					return EndCheckBreak();
				}

				case Token.EXPR_VOID:
				{
					if (this.first != null)
					{
						return first.EndCheck();
					}
					return END_DROPS_OFF;
				}

				case Token.YIELD:
				{
					return END_YIELDS;
				}

				case Token.CONTINUE:
				case Token.THROW:
				{
					return END_UNREACHED;
				}

				case Token.RETURN:
				{
					if (this.first != null)
					{
						return END_RETURNS_VALUE;
					}
					else
					{
						return END_RETURNS;
					}
					goto case Token.TARGET;
				}

				case Token.TARGET:
				{
					if (next != null)
					{
						return next.EndCheck();
					}
					else
					{
						return END_DROPS_OFF;
					}
					goto case Token.LOOP;
				}

				case Token.LOOP:
				{
					return EndCheckLoop();
				}

				case Token.LOCAL_BLOCK:
				case Token.BLOCK:
				{
					// there are several special kinds of blocks
					if (first == null)
					{
						return END_DROPS_OFF;
					}
					switch (first.type)
					{
						case Token.LABEL:
						{
							return first.EndCheckLabel();
						}

						case Token.IFNE:
						{
							return first.EndCheckIf();
						}

						case Token.SWITCH:
						{
							return first.EndCheckSwitch();
						}

						case Token.TRY:
						{
							return first.EndCheckTry();
						}

						default:
						{
							return EndCheckBlock();
						}
					}
					goto default;
				}

				default:
				{
					return END_DROPS_OFF;
				}
			}
		}

		public virtual bool HasSideEffects()
		{
			switch (type)
			{
				case Token.EXPR_VOID:
				case Token.COMMA:
				{
					if (last != null)
					{
						return last.HasSideEffects();
					}
					else
					{
						return true;
					}
					goto case Token.HOOK;
				}

				case Token.HOOK:
				{
					if (first == null || first.next == null || first.next.next == null)
					{
						Kit.CodeBug();
					}
					return first.next.HasSideEffects() && first.next.next.HasSideEffects();
				}

				case Token.AND:
				case Token.OR:
				{
					if (first == null || last == null)
					{
						Kit.CodeBug();
					}
					return first.HasSideEffects() || last.HasSideEffects();
				}

				case Token.ERROR:
				case Token.EXPR_RESULT:
				case Token.ASSIGN:
				case Token.ASSIGN_ADD:
				case Token.ASSIGN_SUB:
				case Token.ASSIGN_MUL:
				case Token.ASSIGN_DIV:
				case Token.ASSIGN_MOD:
				case Token.ASSIGN_BITOR:
				case Token.ASSIGN_BITXOR:
				case Token.ASSIGN_BITAND:
				case Token.ASSIGN_LSH:
				case Token.ASSIGN_RSH:
				case Token.ASSIGN_URSH:
				case Token.ENTERWITH:
				case Token.LEAVEWITH:
				case Token.RETURN:
				case Token.GOTO:
				case Token.IFEQ:
				case Token.IFNE:
				case Token.NEW:
				case Token.DELPROP:
				case Token.SETNAME:
				case Token.SETPROP:
				case Token.SETELEM:
				case Token.CALL:
				case Token.THROW:
				case Token.RETHROW:
				case Token.SETVAR:
				case Token.CATCH_SCOPE:
				case Token.RETURN_RESULT:
				case Token.SET_REF:
				case Token.DEL_REF:
				case Token.REF_CALL:
				case Token.TRY:
				case Token.SEMI:
				case Token.INC:
				case Token.DEC:
				case Token.IF:
				case Token.ELSE:
				case Token.SWITCH:
				case Token.WHILE:
				case Token.DO:
				case Token.FOR:
				case Token.BREAK:
				case Token.CONTINUE:
				case Token.VAR:
				case Token.CONST:
				case Token.LET:
				case Token.LETEXPR:
				case Token.WITH:
				case Token.WITHEXPR:
				case Token.CATCH:
				case Token.FINALLY:
				case Token.BLOCK:
				case Token.LABEL:
				case Token.TARGET:
				case Token.LOOP:
				case Token.JSR:
				case Token.SETPROP_OP:
				case Token.SETELEM_OP:
				case Token.LOCAL_BLOCK:
				case Token.SET_REF_OP:
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

		/// <summary>Recursively unlabel every TARGET or YIELD node in the tree.</summary>
		/// <remarks>
		/// Recursively unlabel every TARGET or YIELD node in the tree.
		/// This is used and should only be used for inlining finally blocks where
		/// jsr instructions used to be. It is somewhat hackish, but implementing
		/// a clone() operation would take much, much more effort.
		/// This solution works for inlining finally blocks because you should never
		/// be writing any given block to the class file simultaneously. Therefore,
		/// an unlabeling will never occur in the middle of a block.
		/// </remarks>
		public virtual void ResetTargets()
		{
			if (type == Token.FINALLY)
			{
				ResetTargets_r();
			}
			else
			{
				Kit.CodeBug();
			}
		}

		private void ResetTargets_r()
		{
			if (type == Token.TARGET || type == Token.YIELD)
			{
				LabelId(-1);
			}
			Node child = first;
			while (child != null)
			{
				child.ResetTargets_r();
				child = child.next;
			}
		}

		public override string ToString()
		{
			return type.ToString();
		}

		private void ToString(ObjToIntMap printIds, StringBuilder sb)
		{
		}

		// can't add this as it recurses
		// can't add this as it is dull
		// NON_SPECIALCALL should not be stored
		public virtual string ToStringTree(ScriptNode treeTop)
		{
			return null;
		}

		private static void ToStringTreeHelper(ScriptNode treeTop, Node n, ObjToIntMap printIds, int level, StringBuilder sb)
		{
		}

		private static void GeneratePrintIds(Node n, ObjToIntMap map)
		{
		}

		private static void AppendPrintId(Node n, ObjToIntMap printIds, StringBuilder sb)
		{
		}

		protected internal int type = Token.ERROR;

		protected internal Node next;

		protected internal Node first;

		protected internal Node last;

		protected internal int lineno = -1;

		/// <summary>Linked list of properties.</summary>
		/// <remarks>
		/// Linked list of properties. Since vast majority of nodes would have
		/// no more then 2 properties, linked list saves memory and provides
		/// fast lookup. If this does not holds, propListHead can be replaced
		/// by UintMap.
		/// </remarks>
		protected internal Node.PropListItem propListHead;
		// type of the node, e.g. Token.NAME
		// next sibling
		// first element of a linked list of children
		// last element of a linked list of children
	}
}
