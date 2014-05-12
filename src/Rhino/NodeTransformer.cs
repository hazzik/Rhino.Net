/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhino;
using Rhino.Ast;
using Sharpen;

namespace Rhino
{
	/// <summary>This class transforms a tree to a lower-level representation for codegen.</summary>
	/// <remarks>This class transforms a tree to a lower-level representation for codegen.</remarks>
	/// <seealso cref="Node">Node</seealso>
	/// <author>Norris Boyd</author>
	public class NodeTransformer
	{
		public NodeTransformer()
		{
		}

		public void Transform(ScriptNode tree)
		{
			TransformCompilationUnit(tree);
			for (int i = 0; i != tree.GetFunctionCount(); ++i)
			{
				FunctionNode fn = tree.GetFunctionNode(i);
				Transform(fn);
			}
		}

		private void TransformCompilationUnit(ScriptNode tree)
		{
			loops = new Stack<Node>();
            loopEnds = new Stack<Node>();
			// to save against upchecks if no finally blocks are used.
			hasFinally = false;
			// Flatten all only if we are not using scope objects for block scope
			bool createScopeObjects = tree.GetType() != Token.FUNCTION || ((FunctionNode)tree).RequiresActivation();
			tree.FlattenSymbolTable(!createScopeObjects);
			//uncomment to print tree before transformation
			bool inStrictMode = tree is AstRoot && ((AstRoot)tree).IsInStrictMode();
			TransformCompilationUnit_r(tree, tree, tree, createScopeObjects, inStrictMode);
		}

		private void TransformCompilationUnit_r(ScriptNode tree, Node parent, Scope scope, bool createScopeObjects, bool inStrictMode)
		{
			Node node = null;
			for (; ; )
			{
				Node previous = null;
				if (node == null)
				{
					node = parent.GetFirstChild();
				}
				else
				{
					previous = node;
					node = node.GetNext();
				}
				if (node == null)
				{
					break;
				}
				int type = node.GetType();
				if (createScopeObjects && (type == Token.BLOCK || type == Token.LOOP || type == Token.ARRAYCOMP) && (node is Scope))
				{
					Scope newScope = (Scope)node;
					if (newScope.GetSymbolTable() != null)
					{
						// transform to let statement so we get a with statement
						// created to contain scoped let variables
						Node let = new Node(type == Token.ARRAYCOMP ? Token.LETEXPR : Token.LET);
						Node innerLet = new Node(Token.LET);
						let.AddChildToBack(innerLet);
						foreach (string name in newScope.GetSymbolTable().Keys)
						{
							innerLet.AddChildToBack(Node.NewString(Token.NAME, name));
						}
						newScope.SetSymbolTable(null);
						// so we don't transform again
						Node oldNode = node;
						node = ReplaceCurrent(parent, previous, node, let);
						type = node.GetType();
						let.AddChildToBack(oldNode);
					}
				}
				switch (type)
				{
					case Token.LABEL:
					case Token.SWITCH:
					case Token.LOOP:
					{
						loops.Push(node);
						loopEnds.Push(((Jump)node).target);
						break;
					}

					case Token.WITH:
					{
						loops.Push(node);
						Node leave = node.GetNext();
						if (leave.GetType() != Token.LEAVEWITH)
						{
							Kit.CodeBug();
						}
						loopEnds.Push(leave);
						break;
					}

					case Token.TRY:
					{
						Jump jump = (Jump)node;
						Node finallytarget = jump.GetFinally();
						if (finallytarget != null)
						{
							hasFinally = true;
							loops.Push(node);
							loopEnds.Push(finallytarget);
						}
						break;
					}

					case Token.TARGET:
					case Token.LEAVEWITH:
					{
						if (loopEnds.Count > 0 && loopEnds.Peek() == node)
						{
							loopEnds.Pop();
							loops.Pop();
						}
						break;
					}

					case Token.YIELD:
					{
						((FunctionNode)tree).AddResumptionPoint(node);
						break;
					}

					case Token.RETURN:
					{
						bool isGenerator = tree.GetType() == Token.FUNCTION && ((FunctionNode)tree).IsGenerator();
						if (isGenerator)
						{
							node.PutIntProp(Node.GENERATOR_END_PROP, 1);
						}
						if (!hasFinally)
						{
							break;
						}
						// skip the whole mess.
						Node unwindBlock = null;
						for (int i = loops.Count - 1; i >= 0; i--)
						{
                            //TODO: optimize that
						    Node n = loops.ElementAt(i);
							int elemtype = n.GetType();
							if (elemtype == Token.TRY || elemtype == Token.WITH)
							{
								Node unwind;
								if (elemtype == Token.TRY)
								{
									Jump jsrnode = new Jump(Token.JSR);
									Node jsrtarget = ((Jump)n).GetFinally();
									jsrnode.target = jsrtarget;
									unwind = jsrnode;
								}
								else
								{
									unwind = new Node(Token.LEAVEWITH);
								}
								if (unwindBlock == null)
								{
									unwindBlock = new Node(Token.BLOCK, node.GetLineno());
								}
								unwindBlock.AddChildToBack(unwind);
							}
						}
						if (unwindBlock != null)
						{
							Node returnNode = node;
							Node returnExpr = returnNode.GetFirstChild();
							node = ReplaceCurrent(parent, previous, node, unwindBlock);
							if (returnExpr == null || isGenerator)
							{
								unwindBlock.AddChildToBack(returnNode);
							}
							else
							{
								Node store = new Node(Token.EXPR_RESULT, returnExpr);
								unwindBlock.AddChildToFront(store);
								returnNode = new Node(Token.RETURN_RESULT);
								unwindBlock.AddChildToBack(returnNode);
								// transform return expression
								TransformCompilationUnit_r(tree, store, scope, createScopeObjects, inStrictMode);
							}
							// skip transformCompilationUnit_r to avoid infinite loop
							goto siblingLoop_continue;
						}
						break;
					}

					case Token.BREAK:
					case Token.CONTINUE:
					{
						Jump jump = (Jump)node;
						Jump jumpStatement = jump.GetJumpStatement();
						if (jumpStatement == null)
						{
							Kit.CodeBug();
						}
						for (int i = loops.Count; ; )
						{
							if (i == 0)
							{
								// Parser/IRFactory ensure that break/continue
								// always has a jump statement associated with it
								// which should be found
								throw Kit.CodeBug();
							}
							--i;
							Node n = loops.ElementAt(i);
							if (n == jumpStatement)
							{
								break;
							}
							int elemtype = n.GetType();
							if (elemtype == Token.WITH)
							{
								Node leave = new Node(Token.LEAVEWITH);
								previous = AddBeforeCurrent(parent, previous, node, leave);
							}
							else
							{
								if (elemtype == Token.TRY)
								{
									Jump tryNode = (Jump)n;
									Jump jsrFinally = new Jump(Token.JSR);
									jsrFinally.target = tryNode.GetFinally();
									previous = AddBeforeCurrent(parent, previous, node, jsrFinally);
								}
							}
						}
						if (type == Token.BREAK)
						{
							jump.target = jumpStatement.target;
						}
						else
						{
							jump.target = jumpStatement.GetContinue();
						}
						jump.SetType(Token.GOTO);
						break;
					}

					case Token.CALL:
					{
						VisitCall(node, tree);
						break;
					}

					case Token.NEW:
					{
						VisitNew(node, tree);
						break;
					}

					case Token.LETEXPR:
					case Token.LET:
					{
						Node child = node.GetFirstChild();
						if (child.GetType() == Token.LET)
						{
							// We have a let statement or expression rather than a
							// let declaration
							bool createWith = tree.GetType() != Token.FUNCTION || ((FunctionNode)tree).RequiresActivation();
							node = VisitLet(createWith, parent, previous, node);
							break;
						}
						goto case Token.CONST;
					}

					case Token.CONST:
					case Token.VAR:
					{
						// fall through to process let declaration...
						Node result = new Node(Token.BLOCK);
						for (Node cursor = node.GetFirstChild(); cursor != null; )
						{
							// Move cursor to next before createAssignment gets chance
							// to change n.next
							Node n = cursor;
							cursor = cursor.GetNext();
							if (n.GetType() == Token.NAME)
							{
								if (!n.HasChildren())
								{
									continue;
								}
								Node init = n.GetFirstChild();
								n.RemoveChild(init);
								n.SetType(Token.BINDNAME);
								n = new Node(type == Token.CONST ? Token.SETCONST : Token.SETNAME, n, init);
							}
							else
							{
								// May be a destructuring assignment already transformed
								// to a LETEXPR
								if (n.GetType() != Token.LETEXPR)
								{
									throw Kit.CodeBug();
								}
							}
							Node pop = new Node(Token.EXPR_VOID, n, node.GetLineno());
							result.AddChildToBack(pop);
						}
						node = ReplaceCurrent(parent, previous, node, result);
						break;
					}

					case Token.TYPEOFNAME:
					{
						Scope defining = scope.GetDefiningScope(node.GetString());
						if (defining != null)
						{
							node.SetScope(defining);
						}
						break;
					}

					case Token.TYPEOF:
					case Token.IFNE:
					{
						Node child = node.GetFirstChild();
						if (type == Token.IFNE)
						{
							while (child.GetType() == Token.NOT)
							{
								child = child.GetFirstChild();
							}
							if (child.GetType() == Token.EQ || child.GetType() == Token.NE)
							{
								Node first = child.GetFirstChild();
								Node last = child.GetLastChild();
								if (first.GetType() == Token.NAME && first.GetString().Equals("undefined"))
								{
									child = last;
								}
								else
								{
									if (last.GetType() == Token.NAME && last.GetString().Equals("undefined"))
									{
										child = first;
									}
								}
							}
						}
						if (child.GetType() == Token.GETPROP)
						{
							child.SetType(Token.GETPROPNOWARN);
						}
						break;
					}

					case Token.SETNAME:
					{
						if (inStrictMode)
						{
							node.SetType(Token.STRICT_SETNAME);
						}
						goto case Token.NAME;
					}

					case Token.NAME:
					case Token.SETCONST:
					case Token.DELPROP:
					{
						// Turn name to var for faster access if possible
						if (createScopeObjects)
						{
							break;
						}
						Node nameSource;
						if (type == Token.NAME)
						{
							nameSource = node;
						}
						else
						{
							nameSource = node.GetFirstChild();
							if (nameSource.GetType() != Token.BINDNAME)
							{
								if (type == Token.DELPROP)
								{
									break;
								}
								throw Kit.CodeBug();
							}
						}
						if (nameSource.GetScope() != null)
						{
							break;
						}
						// already have a scope set
						string name = nameSource.GetString();
						Scope defining = scope.GetDefiningScope(name);
						if (defining != null)
						{
							nameSource.SetScope(defining);
							if (type == Token.NAME)
							{
								node.SetType(Token.GETVAR);
							}
							else
							{
								if (type == Token.SETNAME || type == Token.STRICT_SETNAME)
								{
									node.SetType(Token.SETVAR);
									nameSource.SetType(Token.STRING);
								}
								else
								{
									if (type == Token.SETCONST)
									{
										node.SetType(Token.SETCONSTVAR);
										nameSource.SetType(Token.STRING);
									}
									else
									{
										if (type == Token.DELPROP)
										{
											// Local variables are by definition permanent
											Node n = new Node(Token.FALSE);
											node = ReplaceCurrent(parent, previous, node, n);
										}
										else
										{
											throw Kit.CodeBug();
										}
									}
								}
							}
						}
						break;
					}
				}
				TransformCompilationUnit_r(tree, node, node is Scope ? (Scope)node : scope, createScopeObjects, inStrictMode);
siblingLoop_continue: ;
			}
siblingLoop_break: ;
		}

		protected internal virtual void VisitNew(Node node, ScriptNode tree)
		{
		}

		protected internal virtual void VisitCall(Node node, ScriptNode tree)
		{
		}

		protected internal virtual Node VisitLet(bool createWith, Node parent, Node previous, Node scopeNode)
		{
			Node vars = scopeNode.GetFirstChild();
			Node body = vars.GetNext();
			scopeNode.RemoveChild(vars);
			scopeNode.RemoveChild(body);
			bool isExpression = scopeNode.GetType() == Token.LETEXPR;
			Node result;
			Node newVars;
			if (createWith)
			{
				result = new Node(isExpression ? Token.WITHEXPR : Token.BLOCK);
				result = ReplaceCurrent(parent, previous, scopeNode, result);
				List<object> list = new List<object>();
				Node objectLiteral = new Node(Token.OBJECTLIT);
				for (Node v = vars.GetFirstChild(); v != null; v = v.GetNext())
				{
					Node current = v;
					if (current.GetType() == Token.LETEXPR)
					{
						// destructuring in let expr, e.g. let ([x, y] = [3, 4]) {}
						IList<object> destructuringNames = (IList<object>)current.GetProp(Node.DESTRUCTURING_NAMES);
						Node c = current.GetFirstChild();
						if (c.GetType() != Token.LET)
						{
							throw Kit.CodeBug();
						}
						// Add initialization code to front of body
						if (isExpression)
						{
							body = new Node(Token.COMMA, c.GetNext(), body);
						}
						else
						{
							body = new Node(Token.BLOCK, new Node(Token.EXPR_VOID, c.GetNext()), body);
						}
						// Update "list" and "objectLiteral" for the variables
						// defined in the destructuring assignment
						if (destructuringNames != null)
						{
							list.AddRange(destructuringNames);
							for (int i = 0; i < destructuringNames.Count; i++)
							{
								objectLiteral.AddChildToBack(new Node(Token.VOID, Node.NewNumber(0.0)));
							}
						}
						current = c.GetFirstChild();
					}
					// should be a NAME, checked below
					if (current.GetType() != Token.NAME)
					{
						throw Kit.CodeBug();
					}
					list.Add(ScriptRuntime.GetIndexObject(current.GetString()));
					Node init = current.GetFirstChild();
					if (init == null)
					{
						init = new Node(Token.VOID, Node.NewNumber(0.0));
					}
					objectLiteral.AddChildToBack(init);
				}
				objectLiteral.PutProp(Node.OBJECT_IDS_PROP, list.ToArray());
				newVars = new Node(Token.ENTERWITH, objectLiteral);
				result.AddChildToBack(newVars);
				result.AddChildToBack(new Node(Token.WITH, body));
				result.AddChildToBack(new Node(Token.LEAVEWITH));
			}
			else
			{
				result = new Node(isExpression ? Token.COMMA : Token.BLOCK);
				result = ReplaceCurrent(parent, previous, scopeNode, result);
				newVars = new Node(Token.COMMA);
				for (Node v = vars.GetFirstChild(); v != null; v = v.GetNext())
				{
					Node current = v;
					if (current.GetType() == Token.LETEXPR)
					{
						// destructuring in let expr, e.g. let ([x, y] = [3, 4]) {}
						Node c = current.GetFirstChild();
						if (c.GetType() != Token.LET)
						{
							throw Kit.CodeBug();
						}
						// Add initialization code to front of body
						if (isExpression)
						{
							body = new Node(Token.COMMA, c.GetNext(), body);
						}
						else
						{
							body = new Node(Token.BLOCK, new Node(Token.EXPR_VOID, c.GetNext()), body);
						}
						// We're removing the LETEXPR, so move the symbols
						Scope.JoinScopes((Scope)current, (Scope)scopeNode);
						current = c.GetFirstChild();
					}
					// should be a NAME, checked below
					if (current.GetType() != Token.NAME)
					{
						throw Kit.CodeBug();
					}
					Node stringNode = Node.NewString(current.GetString());
					stringNode.SetScope((Scope)scopeNode);
					Node init = current.GetFirstChild();
					if (init == null)
					{
						init = new Node(Token.VOID, Node.NewNumber(0.0));
					}
					newVars.AddChildToBack(new Node(Token.SETVAR, stringNode, init));
				}
				if (isExpression)
				{
					result.AddChildToBack(newVars);
					scopeNode.SetType(Token.COMMA);
					result.AddChildToBack(scopeNode);
					scopeNode.AddChildToBack(body);
					var scope = body as Scope;
					if (scope != null)
					{
						Scope scopeParent = scope.GetParentScope();
						scope.SetParentScope((Scope)scopeNode);
						((Scope)scopeNode).SetParentScope(scopeParent);
					}
				}
				else
				{
					result.AddChildToBack(new Node(Token.EXPR_VOID, newVars));
					scopeNode.SetType(Token.BLOCK);
					result.AddChildToBack(scopeNode);
					scopeNode.AddChildrenToBack(body);
					var scope = body as Scope;
					if (scope != null)
					{
						Scope scopeParent = scope.GetParentScope();
						scope.SetParentScope((Scope)scopeNode);
						((Scope)scopeNode).SetParentScope(scopeParent);
					}
				}
			}
			return result;
		}

		private static Node AddBeforeCurrent(Node parent, Node previous, Node current, Node toAdd)
		{
			if (previous == null)
			{
				if (current != parent.GetFirstChild())
				{
					Kit.CodeBug();
				}
				parent.AddChildToFront(toAdd);
			}
			else
			{
				if (!(current == previous.GetNext()))
				{
					Kit.CodeBug();
				}
				parent.AddChildAfter(toAdd, previous);
			}
			return toAdd;
		}

		private static Node ReplaceCurrent(Node parent, Node previous, Node current, Node replacement)
		{
			if (previous == null)
			{
				if (!(current == parent.GetFirstChild()))
				{
					Kit.CodeBug();
				}
				parent.ReplaceChild(current, replacement);
			}
			else
			{
				if (previous.next == current)
				{
					// Check cachedPrev.next == current is necessary due to possible
					// tree mutations
					parent.ReplaceChildAfter(previous, replacement);
				}
				else
				{
					parent.ReplaceChild(current, replacement);
				}
			}
			return replacement;
		}

		private Stack<Node> loops;

		private Stack<Node> loopEnds;

		private bool hasFinally;
	}
}
