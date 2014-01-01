/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if XML

using System;
using Rhino;
using Rhino.Xml;
using Rhino.XmlImpl;
using Sharpen;

namespace Rhino.XmlImpl
{
	/// <summary>
	/// This abstract class describes what all XML objects (XML, XMLList) should
	/// have in common.
	/// </summary>
	/// <remarks>
	/// This abstract class describes what all XML objects (XML, XMLList) should
	/// have in common.
	/// </remarks>
	/// <seealso cref="XML">XML</seealso>
	[System.Serializable]
	internal abstract class XMLObjectImpl : XMLObject
	{
		private static readonly object XMLOBJECT_TAG = "XMLObject";

		private XMLLibImpl lib;

		private bool prototypeFlag;

		protected internal XMLObjectImpl(XMLLibImpl lib, Scriptable scope, XMLObject prototype)
		{
			Initialize(lib, scope, prototype);
		}

		internal void Initialize(XMLLibImpl lib, Scriptable scope, XMLObject prototype)
		{
			SetParentScope(scope);
			SetPrototype(prototype);
			prototypeFlag = (prototype == null);
			this.lib = lib;
		}

		internal bool IsPrototype()
		{
			return prototypeFlag;
		}

		internal virtual XMLLibImpl GetLib()
		{
			return lib;
		}

		internal XML NewXML(XmlNode node)
		{
			return lib.NewXML(node);
		}

		internal virtual XML XmlFromNode(XmlNode node)
		{
			if (node.GetXml() == null)
			{
				node.SetXml(NewXML(node));
			}
			return node.GetXml();
		}

		internal XMLList NewXMLList()
		{
			return lib.NewXMLList();
		}

		internal XMLList NewXMLListFrom(object o)
		{
			return lib.NewXMLListFrom(o);
		}

		internal XmlProcessor GetProcessor()
		{
			return lib.GetProcessor();
		}

		internal QName NewQName(string uri, string localName, string prefix)
		{
			return lib.NewQName(uri, localName, prefix);
		}

		internal QName NewQName(XmlNode.QName name)
		{
			return lib.NewQName(name);
		}

		internal Namespace CreateNamespace(XmlNode.Namespace declaration)
		{
			if (declaration == null)
			{
				return null;
			}
			return lib.CreateNamespaces(new XmlNode.Namespace[] { declaration })[0];
		}

		internal Namespace[] CreateNamespaces(XmlNode.Namespace[] declarations)
		{
			return lib.CreateNamespaces(declarations);
		}

		public sealed override Scriptable GetPrototype()
		{
			return base.GetPrototype();
		}

		public sealed override void SetPrototype(Scriptable prototype)
		{
			base.SetPrototype(prototype);
		}

		public sealed override Scriptable GetParentScope()
		{
			return base.GetParentScope();
		}

		public sealed override void SetParentScope(Scriptable parent)
		{
			base.SetParentScope(parent);
		}

		public sealed override object GetDefaultValue(Type hint)
		{
			return this.ToString();
		}

		public sealed override bool HasInstance(Scriptable scriptable)
		{
			return base.HasInstance(scriptable);
		}

		/// <summary>
		/// ecmaHas(cx, id) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </summary>
		/// <remarks>
		/// ecmaHas(cx, id) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </remarks>
		internal abstract bool HasXMLProperty(XMLName name);

		/// <summary>
		/// ecmaGet(cx, id) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </summary>
		/// <remarks>
		/// ecmaGet(cx, id) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </remarks>
		internal abstract object GetXMLProperty(XMLName name);

		/// <summary>
		/// ecmaPut(cx, id, value) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </summary>
		/// <remarks>
		/// ecmaPut(cx, id, value) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </remarks>
		internal abstract void PutXMLProperty(XMLName name, object value);

		/// <summary>
		/// ecmaDelete(cx, id) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </summary>
		/// <remarks>
		/// ecmaDelete(cx, id) calls this after resolving when id to XMLName
		/// and checking it is not Uint32 index.
		/// </remarks>
		internal abstract void DeleteXMLProperty(XMLName name);

		/// <summary>Test XML equality with target the target.</summary>
		/// <remarks>Test XML equality with target the target.</remarks>
		internal abstract bool EquivalentXml(object target);

		internal abstract void AddMatches(XMLList rv, XMLName name);

		private XMLList GetMatches(XMLName name)
		{
			XMLList rv = NewXMLList();
			AddMatches(rv, name);
			return rv;
		}

		internal abstract XML GetXML();

		// Methods from section 12.4.4 in the spec
		internal abstract XMLList Child(int index);

		internal abstract XMLList Child(XMLName xmlName);

		internal abstract XMLList Children();

		internal abstract XMLList Comments();

		internal abstract bool Contains(object xml);

		internal abstract XMLObjectImpl Copy();

		internal abstract XMLList Elements(XMLName xmlName);

		internal abstract bool HasOwnProperty(XMLName xmlName);

		internal abstract bool HasComplexContent();

		internal abstract bool HasSimpleContent();

		internal abstract int Length();

		internal abstract void Normalize();

		internal abstract object Parent();

		internal abstract XMLList ProcessingInstructions(XMLName xmlName);

		internal abstract bool PropertyIsEnumerable(object member);

		internal abstract XMLList Text();

		public abstract override string ToString();

		internal abstract string ToSource(int indent);

		internal abstract string ToXMLString();

		internal abstract object ValueOf();

		protected internal abstract object JsConstructor(Context cx, bool inNewExpr, object[] args);

		//
		//
		// Methods overriding ScriptableObject
		//
		//
		/// <summary>
		/// XMLObject always compare with any value and equivalentValues
		/// never returns
		/// <see cref="Rhino.ScriptableConstants.NOT_FOUND">Rhino.ScriptableConstants.NOT_FOUND</see>
		/// for them but rather
		/// calls equivalentXml(value) and wrap the result as Boolean.
		/// </summary>
		protected internal sealed override object EquivalentValues(object value)
		{
			bool result = EquivalentXml(value);
			return result ? true : false;
		}

		//
		//
		// Methods overriding XMLObject
		//
		//
		/// <summary>Implementation of ECMAScript [[Has]]</summary>
		public sealed override bool Has(Context cx, object id)
		{
			if (cx == null)
			{
				cx = Context.GetCurrentContext();
			}
			XMLName xmlName = lib.ToXMLNameOrIndex(cx, id);
			if (xmlName == null)
			{
				long index = ScriptRuntime.LastUint32Result(cx);
				// XXX Fix this cast
				return Has((int)index, this);
			}
			return HasXMLProperty(xmlName);
		}

		public override bool Has(string name, Scriptable start)
		{
			Context cx = Context.GetCurrentContext();
			return HasXMLProperty(lib.ToXMLNameFromString(cx, name));
		}

		/// <summary>Implementation of ECMAScript [[Get]]</summary>
		public sealed override object Get(Context cx, object id)
		{
			if (cx == null)
			{
				cx = Context.GetCurrentContext();
			}
			XMLName xmlName = lib.ToXMLNameOrIndex(cx, id);
			if (xmlName == null)
			{
				long index = ScriptRuntime.LastUint32Result(cx);
				// XXX Fix this cast
				object result = Get((int)index, this);
				if (result == ScriptableConstants.NOT_FOUND)
				{
					result = Undefined.instance;
				}
				return result;
			}
			return GetXMLProperty(xmlName);
		}

		public override object Get(string name, Scriptable start)
		{
			Context cx = Context.GetCurrentContext();
			return GetXMLProperty(lib.ToXMLNameFromString(cx, name));
		}

		/// <summary>Implementation of ECMAScript [[Put]]</summary>
		public sealed override void Put(Context cx, object id, object value)
		{
			if (cx == null)
			{
				cx = Context.GetCurrentContext();
			}
			XMLName xmlName = lib.ToXMLNameOrIndex(cx, id);
			if (xmlName == null)
			{
				long index = ScriptRuntime.LastUint32Result(cx);
				// XXX Fix this cast
				Put((int)index, this, value);
				return;
			}
			PutXMLProperty(xmlName, value);
		}

		public override void Put(string name, Scriptable start, object value)
		{
			Context cx = Context.GetCurrentContext();
			PutXMLProperty(lib.ToXMLNameFromString(cx, name), value);
		}

		/// <summary>Implementation of ECMAScript [[Delete]].</summary>
		/// <remarks>Implementation of ECMAScript [[Delete]].</remarks>
		public sealed override bool Delete(Context cx, object id)
		{
			if (cx == null)
			{
				cx = Context.GetCurrentContext();
			}
			XMLName xmlName = lib.ToXMLNameOrIndex(cx, id);
			if (xmlName == null)
			{
				long index = ScriptRuntime.LastUint32Result(cx);
				// XXX Fix this
				Delete((int)index);
				return true;
			}
			DeleteXMLProperty(xmlName);
			return true;
		}

		public override void Delete(string name)
		{
			Context cx = Context.GetCurrentContext();
			DeleteXMLProperty(lib.ToXMLNameFromString(cx, name));
		}

		public override object GetFunctionProperty(Context cx, int id)
		{
			if (IsPrototype())
			{
				return base.Get(id, this);
			}
			else
			{
				Scriptable proto = GetPrototype();
				if (proto is XMLObject)
				{
					return ((XMLObject)proto).GetFunctionProperty(cx, id);
				}
			}
			return ScriptableConstants.NOT_FOUND;
		}

		public override object GetFunctionProperty(Context cx, string name)
		{
			if (IsPrototype())
			{
				return base.Get(name, this);
			}
			else
			{
				Scriptable proto = GetPrototype();
				if (proto is XMLObject)
				{
					return ((XMLObject)proto).GetFunctionProperty(cx, name);
				}
			}
			return ScriptableConstants.NOT_FOUND;
		}

		//    TODO    Can this be made more strongly typed?
		public override Ref MemberRef(Context cx, object elem, int memberTypeFlags)
		{
			bool attribute = (memberTypeFlags & Node.ATTRIBUTE_FLAG) != 0;
			bool descendants = (memberTypeFlags & Node.DESCENDANTS_FLAG) != 0;
			if (!attribute && !descendants)
			{
				// Code generation would use ecma(Get|Has|Delete|Set) for
				// normal name identifiers so one ATTRIBUTE_FLAG
				// or DESCENDANTS_FLAG has to be set
				throw Kit.CodeBug();
			}
			XmlNode.QName qname = lib.ToNodeQName(cx, elem, attribute);
			XMLName rv = XMLName.Create(qname, attribute, descendants);
			rv.InitXMLObject(this);
			return rv;
		}

		/// <summary>Generic reference to implement x::ns, x.@ns::y, x..@ns::y etc.</summary>
		/// <remarks>Generic reference to implement x::ns, x.@ns::y, x..@ns::y etc.</remarks>
		public override Ref MemberRef(Context cx, object @namespace, object elem, int memberTypeFlags)
		{
			bool attribute = (memberTypeFlags & Node.ATTRIBUTE_FLAG) != 0;
			bool descendants = (memberTypeFlags & Node.DESCENDANTS_FLAG) != 0;
			XMLName rv = XMLName.Create(lib.ToNodeQName(cx, @namespace, elem), attribute, descendants);
			rv.InitXMLObject(this);
			return rv;
		}

		public override NativeWith EnterWith(Scriptable scope)
		{
			return new XMLWithScope(lib, scope, this);
		}

		public override NativeWith EnterDotQuery(Scriptable scope)
		{
			XMLWithScope xws = new XMLWithScope(lib, scope, this);
			xws.InitAsDotQuery();
			return xws;
		}

		public sealed override object AddValues(Context cx, bool thisIsLeft, object value)
		{
			if (value is XMLObject)
			{
				XMLObject v1;
				XMLObject v2;
				if (thisIsLeft)
				{
					v1 = this;
					v2 = (XMLObject)value;
				}
				else
				{
					v1 = (XMLObject)value;
					v2 = this;
				}
				return lib.AddXMLObjects(cx, v1, v2);
			}
			if (value == Undefined.instance)
			{
				// both "xml + undefined" and "undefined + xml" gives String(xml)
				return ScriptRuntime.ToString(this);
			}
			return base.AddValues(cx, thisIsLeft, value);
		}

		//
		//
		// IdScriptableObject machinery
		//
		//
		internal void ExportAsJSClass(bool @sealed)
		{
			prototypeFlag = true;
			ExportAsJSClass(MAX_PROTOTYPE_ID, GetParentScope(), @sealed);
		}

		private const int Id_constructor = 1;

		private const int Id_addNamespace = 2;

		private const int Id_appendChild = 3;

		private const int Id_attribute = 4;

		private const int Id_attributes = 5;

		private const int Id_child = 6;

		private const int Id_childIndex = 7;

		private const int Id_children = 8;

		private const int Id_comments = 9;

		private const int Id_contains = 10;

		private const int Id_copy = 11;

		private const int Id_descendants = 12;

		private const int Id_elements = 13;

		private const int Id_inScopeNamespaces = 14;

		private const int Id_insertChildAfter = 15;

		private const int Id_insertChildBefore = 16;

		private const int Id_hasOwnProperty = 17;

		private const int Id_hasComplexContent = 18;

		private const int Id_hasSimpleContent = 19;

		private const int Id_length = 20;

		private const int Id_localName = 21;

		private const int Id_name = 22;

		private const int Id_namespace = 23;

		private const int Id_namespaceDeclarations = 24;

		private const int Id_nodeKind = 25;

		private const int Id_normalize = 26;

		private const int Id_parent = 27;

		private const int Id_prependChild = 28;

		private const int Id_processingInstructions = 29;

		private const int Id_propertyIsEnumerable = 30;

		private const int Id_removeNamespace = 31;

		private const int Id_replace = 32;

		private const int Id_setChildren = 33;

		private const int Id_setLocalName = 34;

		private const int Id_setName = 35;

		private const int Id_setNamespace = 36;

		private const int Id_text = 37;

		private const int Id_toString = 38;

		private const int Id_toSource = 39;

		private const int Id_toXMLString = 40;

		private const int Id_valueOf = 41;

		private const int MAX_PROTOTYPE_ID = 41;

		// #string_id_map#
		protected internal override int FindPrototypeId(string s)
		{
			int id;
			// #generated# Last update: 2008-10-21 12:32:31 MESZ
			id = 0;
			string X = null;
			int c;
			switch (s.Length)
			{
				case 4:
				{
					c = s[0];
					if (c == 'c')
					{
						X = "copy";
						id = Id_copy;
					}
					else
					{
						if (c == 'n')
						{
							X = "name";
							id = Id_name;
						}
						else
						{
							if (c == 't')
							{
								X = "text";
								id = Id_text;
							}
						}
					}
					goto L_break;
				}

				case 5:
				{
					X = "child";
					id = Id_child;
					goto L_break;
				}

				case 6:
				{
					c = s[0];
					if (c == 'l')
					{
						X = "length";
						id = Id_length;
					}
					else
					{
						if (c == 'p')
						{
							X = "parent";
							id = Id_parent;
						}
					}
					goto L_break;
				}

				case 7:
				{
					c = s[0];
					if (c == 'r')
					{
						X = "replace";
						id = Id_replace;
					}
					else
					{
						if (c == 's')
						{
							X = "setName";
							id = Id_setName;
						}
						else
						{
							if (c == 'v')
							{
								X = "valueOf";
								id = Id_valueOf;
							}
						}
					}
					goto L_break;
				}

				case 8:
				{
					switch (s[2])
					{
						case 'S':
						{
							c = s[7];
							if (c == 'e')
							{
								X = "toSource";
								id = Id_toSource;
							}
							else
							{
								if (c == 'g')
								{
									X = "toString";
									id = Id_toString;
								}
							}
							goto L_break;
						}

						case 'd':
						{
							X = "nodeKind";
							id = Id_nodeKind;
							goto L_break;
						}

						case 'e':
						{
							X = "elements";
							id = Id_elements;
							goto L_break;
						}

						case 'i':
						{
							X = "children";
							id = Id_children;
							goto L_break;
						}

						case 'm':
						{
							X = "comments";
							id = Id_comments;
							goto L_break;
						}

						case 'n':
						{
							X = "contains";
							id = Id_contains;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 9:
				{
					switch (s[2])
					{
						case 'c':
						{
							X = "localName";
							id = Id_localName;
							goto L_break;
						}

						case 'm':
						{
							X = "namespace";
							id = Id_namespace;
							goto L_break;
						}

						case 'r':
						{
							X = "normalize";
							id = Id_normalize;
							goto L_break;
						}

						case 't':
						{
							X = "attribute";
							id = Id_attribute;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 10:
				{
					c = s[0];
					if (c == 'a')
					{
						X = "attributes";
						id = Id_attributes;
					}
					else
					{
						if (c == 'c')
						{
							X = "childIndex";
							id = Id_childIndex;
						}
					}
					goto L_break;
				}

				case 11:
				{
					switch (s[0])
					{
						case 'a':
						{
							X = "appendChild";
							id = Id_appendChild;
							goto L_break;
						}

						case 'c':
						{
							X = "constructor";
							id = Id_constructor;
							goto L_break;
						}

						case 'd':
						{
							X = "descendants";
							id = Id_descendants;
							goto L_break;
						}

						case 's':
						{
							X = "setChildren";
							id = Id_setChildren;
							goto L_break;
						}

						case 't':
						{
							X = "toXMLString";
							id = Id_toXMLString;
							goto L_break;
						}
					}
					goto L_break;
				}

				case 12:
				{
					c = s[0];
					if (c == 'a')
					{
						X = "addNamespace";
						id = Id_addNamespace;
					}
					else
					{
						if (c == 'p')
						{
							X = "prependChild";
							id = Id_prependChild;
						}
						else
						{
							if (c == 's')
							{
								c = s[3];
								if (c == 'L')
								{
									X = "setLocalName";
									id = Id_setLocalName;
								}
								else
								{
									if (c == 'N')
									{
										X = "setNamespace";
										id = Id_setNamespace;
									}
								}
							}
						}
					}
					goto L_break;
				}

				case 14:
				{
					X = "hasOwnProperty";
					id = Id_hasOwnProperty;
					goto L_break;
				}

				case 15:
				{
					X = "removeNamespace";
					id = Id_removeNamespace;
					goto L_break;
				}

				case 16:
				{
					c = s[0];
					if (c == 'h')
					{
						X = "hasSimpleContent";
						id = Id_hasSimpleContent;
					}
					else
					{
						if (c == 'i')
						{
							X = "insertChildAfter";
							id = Id_insertChildAfter;
						}
					}
					goto L_break;
				}

				case 17:
				{
					c = s[3];
					if (c == 'C')
					{
						X = "hasComplexContent";
						id = Id_hasComplexContent;
					}
					else
					{
						if (c == 'c')
						{
							X = "inScopeNamespaces";
							id = Id_inScopeNamespaces;
						}
						else
						{
							if (c == 'e')
							{
								X = "insertChildBefore";
								id = Id_insertChildBefore;
							}
						}
					}
					goto L_break;
				}

				case 20:
				{
					X = "propertyIsEnumerable";
					id = Id_propertyIsEnumerable;
					goto L_break;
				}

				case 21:
				{
					X = "namespaceDeclarations";
					id = Id_namespaceDeclarations;
					goto L_break;
				}

				case 22:
				{
					X = "processingInstructions";
					id = Id_processingInstructions;
					goto L_break;
				}
			}
L_break: ;
			if (X != null && X != s && !X.Equals(s))
			{
				id = 0;
			}
			goto L0_break;
L0_break: ;
			// #/generated#
			return id;
		}

		// #/string_id_map#
		protected internal override void InitPrototypeId(int id)
		{
			string s;
			int arity;
			switch (id)
			{
				case Id_constructor:
				{
					IdFunctionObject ctor;
					if (this is XML)
					{
						ctor = new XMLCtor((XML)this, XMLOBJECT_TAG, id, 1);
					}
					else
					{
						ctor = new IdFunctionObject(this, XMLOBJECT_TAG, id, 1);
					}
					InitPrototypeConstructor(ctor);
					return;
				}

				case Id_addNamespace:
				{
					arity = 1;
					s = "addNamespace";
					break;
				}

				case Id_appendChild:
				{
					arity = 1;
					s = "appendChild";
					break;
				}

				case Id_attribute:
				{
					arity = 1;
					s = "attribute";
					break;
				}

				case Id_attributes:
				{
					arity = 0;
					s = "attributes";
					break;
				}

				case Id_child:
				{
					arity = 1;
					s = "child";
					break;
				}

				case Id_childIndex:
				{
					arity = 0;
					s = "childIndex";
					break;
				}

				case Id_children:
				{
					arity = 0;
					s = "children";
					break;
				}

				case Id_comments:
				{
					arity = 0;
					s = "comments";
					break;
				}

				case Id_contains:
				{
					arity = 1;
					s = "contains";
					break;
				}

				case Id_copy:
				{
					arity = 0;
					s = "copy";
					break;
				}

				case Id_descendants:
				{
					arity = 1;
					s = "descendants";
					break;
				}

				case Id_elements:
				{
					arity = 1;
					s = "elements";
					break;
				}

				case Id_hasComplexContent:
				{
					arity = 0;
					s = "hasComplexContent";
					break;
				}

				case Id_hasOwnProperty:
				{
					arity = 1;
					s = "hasOwnProperty";
					break;
				}

				case Id_hasSimpleContent:
				{
					arity = 0;
					s = "hasSimpleContent";
					break;
				}

				case Id_inScopeNamespaces:
				{
					arity = 0;
					s = "inScopeNamespaces";
					break;
				}

				case Id_insertChildAfter:
				{
					arity = 2;
					s = "insertChildAfter";
					break;
				}

				case Id_insertChildBefore:
				{
					arity = 2;
					s = "insertChildBefore";
					break;
				}

				case Id_length:
				{
					arity = 0;
					s = "length";
					break;
				}

				case Id_localName:
				{
					arity = 0;
					s = "localName";
					break;
				}

				case Id_name:
				{
					arity = 0;
					s = "name";
					break;
				}

				case Id_namespace:
				{
					arity = 1;
					s = "namespace";
					break;
				}

				case Id_namespaceDeclarations:
				{
					arity = 0;
					s = "namespaceDeclarations";
					break;
				}

				case Id_nodeKind:
				{
					arity = 0;
					s = "nodeKind";
					break;
				}

				case Id_normalize:
				{
					arity = 0;
					s = "normalize";
					break;
				}

				case Id_parent:
				{
					arity = 0;
					s = "parent";
					break;
				}

				case Id_prependChild:
				{
					arity = 1;
					s = "prependChild";
					break;
				}

				case Id_processingInstructions:
				{
					arity = 1;
					s = "processingInstructions";
					break;
				}

				case Id_propertyIsEnumerable:
				{
					arity = 1;
					s = "propertyIsEnumerable";
					break;
				}

				case Id_removeNamespace:
				{
					arity = 1;
					s = "removeNamespace";
					break;
				}

				case Id_replace:
				{
					arity = 2;
					s = "replace";
					break;
				}

				case Id_setChildren:
				{
					arity = 1;
					s = "setChildren";
					break;
				}

				case Id_setLocalName:
				{
					arity = 1;
					s = "setLocalName";
					break;
				}

				case Id_setName:
				{
					arity = 1;
					s = "setName";
					break;
				}

				case Id_setNamespace:
				{
					arity = 1;
					s = "setNamespace";
					break;
				}

				case Id_text:
				{
					arity = 0;
					s = "text";
					break;
				}

				case Id_toString:
				{
					arity = 0;
					s = "toString";
					break;
				}

				case Id_toSource:
				{
					arity = 1;
					s = "toSource";
					break;
				}

				case Id_toXMLString:
				{
					arity = 1;
					s = "toXMLString";
					break;
				}

				case Id_valueOf:
				{
					arity = 0;
					s = "valueOf";
					break;
				}

				default:
				{
					throw new ArgumentException(id.ToString());
				}
			}
			InitPrototypeMethod(XMLOBJECT_TAG, id, s, arity);
		}

		private object[] ToObjectArray(object[] typed)
		{
			object[] rv = new object[typed.Length];
			for (int i = 0; i < rv.Length; i++)
			{
				rv[i] = typed[i];
			}
			return rv;
		}

		private void XmlMethodNotFound(object @object, string name)
		{
			throw ScriptRuntime.NotFunctionError(@object, name);
		}

		public override object ExecIdCall(IdFunctionObject f, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			if (!f.HasTag(XMLOBJECT_TAG))
			{
				return base.ExecIdCall(f, cx, scope, thisObj, args);
			}
			int id = f.MethodId();
			if (id == Id_constructor)
			{
				return JsConstructor(cx, thisObj == null, args);
			}
			// All (XML|XMLList).prototype methods require thisObj to be XML
			if (!(thisObj is XMLObjectImpl))
			{
				throw IncompatibleCallError(f);
			}
			XMLObjectImpl realThis = (XMLObjectImpl)thisObj;
			XML xml = realThis.GetXML();
			switch (id)
			{
				case Id_appendChild:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "appendChild");
					}
					return xml.AppendChild(Arg(args, 0));
				}

				case Id_addNamespace:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "addNamespace");
					}
					Namespace ns = lib.CastToNamespace(cx, Arg(args, 0));
					return xml.AddNamespace(ns);
				}

				case Id_childIndex:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "childIndex");
					}
					return ScriptRuntime.WrapInt(xml.ChildIndex());
				}

				case Id_inScopeNamespaces:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "inScopeNamespaces");
					}
					return cx.NewArray(scope, ToObjectArray(xml.InScopeNamespaces()));
				}

				case Id_insertChildAfter:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "insertChildAfter");
					}
					object arg0 = Arg(args, 0);
					if (arg0 == null || arg0 is XML)
					{
						return xml.InsertChildAfter((XML)arg0, Arg(args, 1));
					}
					return Undefined.instance;
				}

				case Id_insertChildBefore:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "insertChildBefore");
					}
					object arg0 = Arg(args, 0);
					if (arg0 == null || arg0 is XML)
					{
						return xml.InsertChildBefore((XML)arg0, Arg(args, 1));
					}
					return Undefined.instance;
				}

				case Id_localName:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "localName");
					}
					return xml.LocalName();
				}

				case Id_name:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "name");
					}
					return xml.Name();
				}

				case Id_namespace:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "namespace");
					}
					string prefix = (args.Length > 0) ? ScriptRuntime.ToString(args[0]) : null;
					Namespace rv = xml.Namespace(prefix);
					if (rv == null)
					{
						return Undefined.instance;
					}
					else
					{
						return rv;
					}
					goto case Id_namespaceDeclarations;
				}

				case Id_namespaceDeclarations:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "namespaceDeclarations");
					}
					Namespace[] array = xml.NamespaceDeclarations();
					return cx.NewArray(scope, ToObjectArray(array));
				}

				case Id_nodeKind:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "nodeKind");
					}
					return xml.NodeKind();
				}

				case Id_prependChild:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "prependChild");
					}
					return xml.PrependChild(Arg(args, 0));
				}

				case Id_removeNamespace:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "removeNamespace");
					}
					Namespace ns = lib.CastToNamespace(cx, Arg(args, 0));
					return xml.RemoveNamespace(ns);
				}

				case Id_replace:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "replace");
					}
					XMLName xmlName = lib.ToXMLNameOrIndex(cx, Arg(args, 0));
					object arg1 = Arg(args, 1);
					if (xmlName == null)
					{
						//    I refuse to believe that this number will exceed 2^31
						int index = (int)ScriptRuntime.LastUint32Result(cx);
						return xml.Replace(index, arg1);
					}
					else
					{
						return xml.Replace(xmlName, arg1);
					}
					goto case Id_setChildren;
				}

				case Id_setChildren:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "setChildren");
					}
					return xml.SetChildren(Arg(args, 0));
				}

				case Id_setLocalName:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "setLocalName");
					}
					string localName;
					object arg = Arg(args, 0);
					if (arg is QName)
					{
						localName = ((QName)arg).LocalName();
					}
					else
					{
						localName = ScriptRuntime.ToString(arg);
					}
					xml.SetLocalName(localName);
					return Undefined.instance;
				}

				case Id_setName:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "setName");
					}
					object arg = (args.Length != 0) ? args[0] : Undefined.instance;
					QName qname = lib.ConstructQName(cx, arg);
					xml.SetName(qname);
					return Undefined.instance;
				}

				case Id_setNamespace:
				{
					if (xml == null)
					{
						XmlMethodNotFound(realThis, "setNamespace");
					}
					Namespace ns = lib.CastToNamespace(cx, Arg(args, 0));
					xml.SetNamespace(ns);
					return Undefined.instance;
				}

				case Id_attribute:
				{
					XMLName xmlName = XMLName.Create(lib.ToNodeQName(cx, Arg(args, 0), true), true, false);
					return realThis.GetMatches(xmlName);
				}

				case Id_attributes:
				{
					return realThis.GetMatches(XMLName.Create(XmlNode.QName.Create(null, null), true, false));
				}

				case Id_child:
				{
					XMLName xmlName = lib.ToXMLNameOrIndex(cx, Arg(args, 0));
					if (xmlName == null)
					{
						//    Two billion or so is a fine upper limit, so we cast to int
						int index = (int)ScriptRuntime.LastUint32Result(cx);
						return realThis.Child(index);
					}
					else
					{
						return realThis.Child(xmlName);
					}
					goto case Id_children;
				}

				case Id_children:
				{
					return realThis.Children();
				}

				case Id_comments:
				{
					return realThis.Comments();
				}

				case Id_contains:
				{
					return ScriptRuntime.WrapBoolean(realThis.Contains(Arg(args, 0)));
				}

				case Id_copy:
				{
					return realThis.Copy();
				}

				case Id_descendants:
				{
					XmlNode.QName qname = (args.Length == 0) ? XmlNode.QName.Create(null, null) : lib.ToNodeQName(cx, args[0], false);
					return realThis.GetMatches(XMLName.Create(qname, false, true));
				}

				case Id_elements:
				{
					XMLName xmlName = (args.Length == 0) ? XMLName.FormStar() : lib.ToXMLName(cx, args[0]);
					return realThis.Elements(xmlName);
				}

				case Id_hasOwnProperty:
				{
					XMLName xmlName = lib.ToXMLName(cx, Arg(args, 0));
					return ScriptRuntime.WrapBoolean(realThis.HasOwnProperty(xmlName));
				}

				case Id_hasComplexContent:
				{
					return ScriptRuntime.WrapBoolean(realThis.HasComplexContent());
				}

				case Id_hasSimpleContent:
				{
					return ScriptRuntime.WrapBoolean(realThis.HasSimpleContent());
				}

				case Id_length:
				{
					return ScriptRuntime.WrapInt(realThis.Length());
				}

				case Id_normalize:
				{
					realThis.Normalize();
					return Undefined.instance;
				}

				case Id_parent:
				{
					return realThis.Parent();
				}

				case Id_processingInstructions:
				{
					XMLName xmlName = (args.Length > 0) ? lib.ToXMLName(cx, args[0]) : XMLName.FormStar();
					return realThis.ProcessingInstructions(xmlName);
				}

				case Id_propertyIsEnumerable:
				{
					return ScriptRuntime.WrapBoolean(realThis.PropertyIsEnumerable(Arg(args, 0)));
				}

				case Id_text:
				{
					return realThis.Text();
				}

				case Id_toString:
				{
					return realThis.ToString();
				}

				case Id_toSource:
				{
					int indent = ScriptRuntime.ToInt32(args, 0);
					return realThis.ToSource(indent);
				}

				case Id_toXMLString:
				{
					return realThis.ToXMLString();
				}

				case Id_valueOf:
				{
					return realThis.ValueOf();
				}
			}
			throw new ArgumentException(id.ToString());
		}

		private static object Arg(object[] args, int i)
		{
			return (i < args.Length) ? args[i] : Undefined.instance;
		}

		internal XML NewTextElementXML(XmlNode reference, XmlNode.QName qname, string value)
		{
			return lib.NewTextElementXML(reference, qname, value);
		}

		internal XML NewXMLFromJs(object inputObject)
		{
			return lib.NewXMLFromJs(inputObject);
		}

		internal XML EcmaToXml(object @object)
		{
			return lib.EcmaToXml(@object);
		}

		internal string EcmaEscapeAttributeValue(string s)
		{
			//    TODO    Check this
			string quoted = lib.EscapeAttributeValue(s);
			return quoted.Substring(1, quoted.Length - 2);
		}

		internal XML CreateEmptyXML()
		{
			return NewXML(XmlNode.CreateEmpty(GetProcessor()));
		}
	}
}
#endif