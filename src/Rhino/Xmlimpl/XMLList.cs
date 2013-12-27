/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Text;
using Rhino;
using Rhino.Xml;
using Rhino.XmlImpl;
using Sharpen;

namespace Rhino.XmlImpl
{
	[System.Serializable]
	internal class XMLList : XMLObjectImpl, Function
	{
		internal const long serialVersionUID = -4543618751670781135L;

		private XmlNode.InternalList _annos;

		private XMLObjectImpl targetObject = null;

		private XmlNode.QName targetProperty = null;

		internal XMLList(XMLLibImpl lib, Scriptable scope, XMLObject prototype) : base(lib, scope, prototype)
		{
			_annos = new XmlNode.InternalList();
		}

		internal virtual XmlNode.InternalList GetNodeList()
		{
			return _annos;
		}

		//    TODO    Should be XMLObjectImpl, XMLName?
		internal virtual void SetTargets(XMLObjectImpl @object, XmlNode.QName property)
		{
			targetObject = @object;
			targetProperty = property;
		}

		private XML GetXmlFromAnnotation(int index)
		{
			return GetXML(_annos, index);
		}

		internal override XML GetXML()
		{
			if (Length() == 1)
			{
				return GetXmlFromAnnotation(0);
			}
			return null;
		}

		private void InternalRemoveFromList(int index)
		{
			_annos.Remove(index);
		}

		internal virtual void Replace(int index, XML xml)
		{
			if (index < Length())
			{
				XmlNode.InternalList newAnnoList = new XmlNode.InternalList();
				newAnnoList.Add(_annos, 0, index);
				newAnnoList.Add(xml);
				newAnnoList.Add(_annos, index + 1, Length());
				_annos = newAnnoList;
			}
		}

		private void Insert(int index, XML xml)
		{
			if (index < Length())
			{
				XmlNode.InternalList newAnnoList = new XmlNode.InternalList();
				newAnnoList.Add(_annos, 0, index);
				newAnnoList.Add(xml);
				newAnnoList.Add(_annos, index, Length());
				_annos = newAnnoList;
			}
		}

		//
		//
		//  methods overriding ScriptableObject
		//
		//
		public override string GetClassName()
		{
			return "XMLList";
		}

		//
		//
		//  methods overriding IdScriptableObject
		//
		//
		public override object Get(int index, Scriptable start)
		{
			//Log("get index: " + index);
			if (index >= 0 && index < Length())
			{
				return GetXmlFromAnnotation(index);
			}
			else
			{
				return ScriptableConstants.NOT_FOUND;
			}
		}

		internal override bool HasXMLProperty(XMLName xmlName)
		{
			// Has should return true if the property would have results > 0
			return GetPropertyList(xmlName).Length() > 0;
		}

		public override bool Has(int index, Scriptable start)
		{
			return 0 <= index && index < Length();
		}

		internal override void PutXMLProperty(XMLName xmlName, object value)
		{
			//Log("put property: " + name);
			// Special-case checks for undefined and null
			if (value == null)
			{
				value = "null";
			}
			else
			{
				if (value is Undefined)
				{
					value = "undefined";
				}
			}
			if (Length() > 1)
			{
				throw ScriptRuntime.TypeError("Assignment to lists with more than one item is not supported");
			}
			else
			{
				if (Length() == 0)
				{
					// Secret sauce for super-expandos.
					// We set an element here, and then add ourselves to our target.
					if (targetObject != null && targetProperty != null && targetProperty.GetLocalName() != null && targetProperty.GetLocalName().Length > 0)
					{
						// Add an empty element with our targetProperty name and
						// then set it.
						XML xmlValue = NewTextElementXML(null, targetProperty, null);
						AddToList(xmlValue);
						if (xmlName.IsAttributeName())
						{
							SetAttribute(xmlName, value);
						}
						else
						{
							XML xml = Item(0);
							xml.PutXMLProperty(xmlName, value);
							// Update the list with the new item at location 0.
							Replace(0, Item(0));
						}
						// Now add us to our parent
						XMLName name2 = XMLName.FormProperty(targetProperty.GetNamespace().GetUri(), targetProperty.GetLocalName());
						targetObject.PutXMLProperty(name2, this);
						Replace(0, targetObject.GetXML().GetLastXmlChild());
					}
					else
					{
						throw ScriptRuntime.TypeError("Assignment to empty XMLList without targets not supported");
					}
				}
				else
				{
					if (xmlName.IsAttributeName())
					{
						SetAttribute(xmlName, value);
					}
					else
					{
						XML xml = Item(0);
						xml.PutXMLProperty(xmlName, value);
						// Update the list with the new item at location 0.
						Replace(0, Item(0));
					}
				}
			}
		}

		internal override object GetXMLProperty(XMLName name)
		{
			return GetPropertyList(name);
		}

		private void ReplaceNode(XML xml, XML with)
		{
			xml.ReplaceWith(with);
		}

		public override void Put(int index, Scriptable start, object value)
		{
			object parent = Undefined.instance;
			// Convert text into XML if needed.
			XMLObject xmlValue;
			// Special-case checks for undefined and null
			if (value == null)
			{
				value = "null";
			}
			else
			{
				if (value is Undefined)
				{
					value = "undefined";
				}
			}
			if (value is XMLObject)
			{
				xmlValue = (XMLObject)value;
			}
			else
			{
				if (targetProperty == null)
				{
					xmlValue = NewXMLFromJs(value.ToString());
				}
				else
				{
					//    Note that later in the code, we will use this as an argument to replace(int,value)
					//    So we will be "replacing" this element with itself
					//    There may well be a better way to do this
					//    TODO    Find a way to refactor this whole method and simplify it
					xmlValue = Item(index);
					if (xmlValue == null)
					{
						XML x = Item(0);
						xmlValue = x == null ? NewTextElementXML(null, targetProperty, null) : x.Copy();
					}
					((XML)xmlValue).SetChildren(value);
				}
			}
			// Find the parent
			if (index < Length())
			{
				parent = Item(index).Parent();
			}
			else
			{
				if (Length() == 0)
				{
					parent = targetObject != null ? targetObject.GetXML() : Parent();
				}
				else
				{
					// Appending
					parent = Parent();
				}
			}
			if (parent is XML)
			{
				// found parent, alter doc
				XML xmlParent = (XML)parent;
				if (index < Length())
				{
					// We're replacing the the node.
					XML xmlNode = GetXmlFromAnnotation(index);
					if (xmlValue is XML)
					{
						ReplaceNode(xmlNode, (XML)xmlValue);
						Replace(index, xmlNode);
					}
					else
					{
						if (xmlValue is XMLList)
						{
							// Replace the first one, and add the rest on the list.
							XMLList list = (XMLList)xmlValue;
							if (list.Length() > 0)
							{
								int lastIndexAdded = xmlNode.ChildIndex();
								ReplaceNode(xmlNode, list.Item(0));
								Replace(index, list.Item(0));
								for (int i = 1; i < list.Length(); i++)
								{
									xmlParent.InsertChildAfter(xmlParent.GetXmlChild(lastIndexAdded), list.Item(i));
									lastIndexAdded++;
									Insert(index + i, list.Item(i));
								}
							}
						}
					}
				}
				else
				{
					// Appending
					xmlParent.AppendChild(xmlValue);
					AddToList(xmlParent.GetLastXmlChild());
				}
			}
			else
			{
				// Don't all have same parent, no underlying doc to alter
				if (index < Length())
				{
					XML xmlNode = GetXML(_annos, index);
					if (xmlValue is XML)
					{
						ReplaceNode(xmlNode, (XML)xmlValue);
						Replace(index, xmlNode);
					}
					else
					{
						if (xmlValue is XMLList)
						{
							// Replace the first one, and add the rest on the list.
							XMLList list = (XMLList)xmlValue;
							if (list.Length() > 0)
							{
								ReplaceNode(xmlNode, list.Item(0));
								Replace(index, list.Item(0));
								for (int i = 1; i < list.Length(); i++)
								{
									Insert(index + i, list.Item(i));
								}
							}
						}
					}
				}
				else
				{
					AddToList(xmlValue);
				}
			}
		}

		private XML GetXML(XmlNode.InternalList _annos, int index)
		{
			if (index >= 0 && index < Length())
			{
				return XmlFromNode(_annos.Item(index));
			}
			else
			{
				return null;
			}
		}

		internal override void DeleteXMLProperty(XMLName name)
		{
			for (int i = 0; i < Length(); i++)
			{
				XML xml = GetXmlFromAnnotation(i);
				if (xml.IsElement())
				{
					xml.DeleteXMLProperty(name);
				}
			}
		}

		public override void Delete(int index)
		{
			if (index >= 0 && index < Length())
			{
				XML xml = GetXmlFromAnnotation(index);
				xml.Remove();
				InternalRemoveFromList(index);
			}
		}

		public override object[] GetIds()
		{
			object[] enumObjs;
			if (IsPrototype())
			{
				enumObjs = new object[0];
			}
			else
			{
				enumObjs = new object[Length()];
				for (int i = 0; i < enumObjs.Length; i++)
				{
					enumObjs[i] = Sharpen.Extensions.ValueOf(i);
				}
			}
			return enumObjs;
		}

		public virtual object[] GetIdsForDebug()
		{
			return GetIds();
		}

		// XMLList will remove will delete all items in the list (a set delete) this differs from the XMLList delete operator.
		internal virtual void Remove()
		{
			int nLen = Length();
			for (int i = nLen - 1; i >= 0; i--)
			{
				XML xml = GetXmlFromAnnotation(i);
				if (xml != null)
				{
					xml.Remove();
					InternalRemoveFromList(i);
				}
			}
		}

		internal virtual XML Item(int index)
		{
			return _annos != null ? GetXmlFromAnnotation(index) : CreateEmptyXML();
		}

		private void SetAttribute(XMLName xmlName, object value)
		{
			for (int i = 0; i < Length(); i++)
			{
				XML xml = GetXmlFromAnnotation(i);
				xml.SetAttribute(xmlName, value);
			}
		}

		internal virtual void AddToList(object toAdd)
		{
			_annos.AddToList(toAdd);
		}

		//
		//
		// Methods from section 12.4.4 in the spec
		//
		//
		internal override XMLList Child(int index)
		{
			XMLList result = NewXMLList();
			for (int i = 0; i < Length(); i++)
			{
				result.AddToList(GetXmlFromAnnotation(i).Child(index));
			}
			return result;
		}

		internal override XMLList Child(XMLName xmlName)
		{
			XMLList result = NewXMLList();
			for (int i = 0; i < Length(); i++)
			{
				result.AddToList(GetXmlFromAnnotation(i).Child(xmlName));
			}
			return result;
		}

		internal override void AddMatches(XMLList rv, XMLName name)
		{
			for (int i = 0; i < Length(); i++)
			{
				GetXmlFromAnnotation(i).AddMatches(rv, name);
			}
		}

		internal override XMLList Children()
		{
			List<XML> list = new List<XML>();
			for (int i = 0; i < Length(); i++)
			{
				XML xml = GetXmlFromAnnotation(i);
				if (xml != null)
				{
					XMLList childList = xml.Children();
					int cChildren = childList.Length();
					for (int j = 0; j < cChildren; j++)
					{
						list.Add(childList.Item(j));
					}
				}
			}
			XMLList allChildren = NewXMLList();
			int sz = list.Count;
			for (int i_1 = 0; i_1 < sz; i_1++)
			{
				allChildren.AddToList(list[i_1]);
			}
			return allChildren;
		}

		internal override XMLList Comments()
		{
			XMLList result = NewXMLList();
			for (int i = 0; i < Length(); i++)
			{
				XML xml = GetXmlFromAnnotation(i);
				result.AddToList(xml.Comments());
			}
			return result;
		}

		internal override XMLList Elements(XMLName name)
		{
			XMLList rv = NewXMLList();
			for (int i = 0; i < Length(); i++)
			{
				XML xml = GetXmlFromAnnotation(i);
				rv.AddToList(xml.Elements(name));
			}
			return rv;
		}

		internal override bool Contains(object xml)
		{
			bool result = false;
			for (int i = 0; i < Length(); i++)
			{
				XML member = GetXmlFromAnnotation(i);
				if (member.EquivalentXml(xml))
				{
					result = true;
					break;
				}
			}
			return result;
		}

		internal override XMLObjectImpl Copy()
		{
			XMLList result = NewXMLList();
			for (int i = 0; i < Length(); i++)
			{
				XML xml = GetXmlFromAnnotation(i);
				result.AddToList(xml.Copy());
			}
			return result;
		}

		internal override bool HasOwnProperty(XMLName xmlName)
		{
			if (IsPrototype())
			{
				string property = xmlName.LocalName();
				return (FindPrototypeId(property) != 0);
			}
			else
			{
				return (GetPropertyList(xmlName).Length() > 0);
			}
		}

		internal override bool HasComplexContent()
		{
			bool complexContent;
			int length = Length();
			if (length == 0)
			{
				complexContent = false;
			}
			else
			{
				if (length == 1)
				{
					complexContent = GetXmlFromAnnotation(0).HasComplexContent();
				}
				else
				{
					complexContent = false;
					for (int i = 0; i < length; i++)
					{
						XML nextElement = GetXmlFromAnnotation(i);
						if (nextElement.IsElement())
						{
							complexContent = true;
							break;
						}
					}
				}
			}
			return complexContent;
		}

		internal override bool HasSimpleContent()
		{
			if (Length() == 0)
			{
				return true;
			}
			else
			{
				if (Length() == 1)
				{
					return GetXmlFromAnnotation(0).HasSimpleContent();
				}
				else
				{
					for (int i = 0; i < Length(); i++)
					{
						XML nextElement = GetXmlFromAnnotation(i);
						if (nextElement.IsElement())
						{
							return false;
						}
					}
					return true;
				}
			}
		}

		internal override int Length()
		{
			int result = 0;
			if (_annos != null)
			{
				result = _annos.Length();
			}
			return result;
		}

		internal override void Normalize()
		{
			for (int i = 0; i < Length(); i++)
			{
				GetXmlFromAnnotation(i).Normalize();
			}
		}

		/// <summary>
		/// If list is empty, return undefined, if elements have different parents return undefined,
		/// If they all have the same parent, return that parent
		/// </summary>
		internal override object Parent()
		{
			if (Length() == 0)
			{
				return Undefined.instance;
			}
			XML candidateParent = null;
			for (int i = 0; i < Length(); i++)
			{
				object currParent = GetXmlFromAnnotation(i).Parent();
				if (!(currParent is XML))
				{
					return Undefined.instance;
				}
				XML xml = (XML)currParent;
				if (i == 0)
				{
					// Set the first for the rest to compare to.
					candidateParent = xml;
				}
				else
				{
					if (candidateParent.Is(xml))
					{
					}
					else
					{
						//    keep looking
						return Undefined.instance;
					}
				}
			}
			return candidateParent;
		}

		internal override XMLList ProcessingInstructions(XMLName xmlName)
		{
			XMLList result = NewXMLList();
			for (int i = 0; i < Length(); i++)
			{
				XML xml = GetXmlFromAnnotation(i);
				result.AddToList(xml.ProcessingInstructions(xmlName));
			}
			return result;
		}

		internal override bool PropertyIsEnumerable(object name)
		{
			long index;
			if (name is int)
			{
				index = System.Convert.ToInt32(((int)name));
			}
			else
			{
				if (name.IsNumber())
				{
					double x = System.Convert.ToDouble(name);
					index = (long)x;
					if (index != x)
					{
						return false;
					}
					if (index == 0 && 1.0 / x < 0)
					{
						// Negative 0
						return false;
					}
				}
				else
				{
					string s = ScriptRuntime.ToString(name);
					index = ScriptRuntime.TestUint32String(s);
				}
			}
			return (0 <= index && index < Length());
		}

		internal override XMLList Text()
		{
			XMLList result = NewXMLList();
			for (int i = 0; i < Length(); i++)
			{
				result.AddToList(GetXmlFromAnnotation(i).Text());
			}
			return result;
		}

		public override string ToString()
		{
			//    ECMA357 10.1.2
			if (HasSimpleContent())
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < Length(); i++)
				{
					XML next = GetXmlFromAnnotation(i);
					if (next.IsComment() || next.IsProcessingInstruction())
					{
					}
					else
					{
						//    do nothing
						sb.Append(next.ToString());
					}
				}
				return sb.ToString();
			}
			else
			{
				return ToXMLString();
			}
		}

		internal override string ToSource(int indent)
		{
			return ToXMLString();
		}

		internal override string ToXMLString()
		{
			//    See ECMA 10.2.1
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < Length(); i++)
			{
				if (GetProcessor().IsPrettyPrinting() && i != 0)
				{
					sb.Append('\n');
				}
				sb.Append(GetXmlFromAnnotation(i).ToXMLString());
			}
			return sb.ToString();
		}

		internal override object ValueOf()
		{
			return this;
		}

		//
		// Other public Functions from XMLObject
		//
		internal override bool EquivalentXml(object target)
		{
			bool result = false;
			// Zero length list should equate to undefined
			if (target is Undefined && Length() == 0)
			{
				result = true;
			}
			else
			{
				if (Length() == 1)
				{
					result = GetXmlFromAnnotation(0).EquivalentXml(target);
				}
				else
				{
					if (target is XMLList)
					{
						XMLList otherList = (XMLList)target;
						if (otherList.Length() == Length())
						{
							result = true;
							for (int i = 0; i < Length(); i++)
							{
								if (!GetXmlFromAnnotation(i).EquivalentXml(otherList.GetXmlFromAnnotation(i)))
								{
									result = false;
									break;
								}
							}
						}
					}
				}
			}
			return result;
		}

		private XMLList GetPropertyList(XMLName name)
		{
			XMLList propertyList = NewXMLList();
			XmlNode.QName qname = null;
			if (!name.IsDescendants() && !name.IsAttributeName())
			{
				// Only set the targetProperty if this is a regular child get
				// and not a descendant or attribute get
				qname = name.ToQname();
			}
			propertyList.SetTargets(this, qname);
			for (int i = 0; i < Length(); i++)
			{
				propertyList.AddToList(GetXmlFromAnnotation(i).GetPropertyList(name));
			}
			return propertyList;
		}

		private object ApplyOrCall(bool isApply, Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			string methodName = isApply ? "apply" : "call";
			if (!(thisObj is XMLList) || ((XMLList)thisObj).targetProperty == null)
			{
				throw ScriptRuntime.TypeError1("msg.isnt.function", methodName);
			}
			return ScriptRuntime.ApplyOrCall(isApply, cx, scope, thisObj, args);
		}

		protected internal override object JsConstructor(Context cx, bool inNewExpr, object[] args)
		{
			if (args.Length == 0)
			{
				return NewXMLList();
			}
			else
			{
				object arg0 = args[0];
				if (!inNewExpr && arg0 is XMLList)
				{
					// XMLList(XMLList) returns the same object.
					return arg0;
				}
				return NewXMLListFrom(arg0);
			}
		}

		/// <summary>See ECMA 357, 11_2_2_1, Semantics, 3_e.</summary>
		/// <remarks>See ECMA 357, 11_2_2_1, Semantics, 3_e.</remarks>
		public override Scriptable GetExtraMethodSource(Context cx)
		{
			if (Length() == 1)
			{
				return GetXmlFromAnnotation(0);
			}
			return null;
		}

		public virtual object Call(Context cx, Scriptable scope, Scriptable thisObj, object[] args)
		{
			// This XMLList is being called as a Function.
			// Let's find the real Function object.
			if (targetProperty == null)
			{
				throw ScriptRuntime.NotFunctionError(this);
			}
			string methodName = targetProperty.GetLocalName();
			bool isApply = methodName.Equals("apply");
			if (isApply || methodName.Equals("call"))
			{
				return ApplyOrCall(isApply, cx, scope, thisObj, args);
			}
			if (!(thisObj is XMLObject))
			{
				throw ScriptRuntime.TypeError1("msg.incompat.call", methodName);
			}
			object func = null;
			Scriptable sobj = thisObj;
			while (sobj is XMLObject)
			{
				XMLObject xmlObject = (XMLObject)sobj;
				func = xmlObject.GetFunctionProperty(cx, methodName);
				if (func != ScriptableConstants.NOT_FOUND)
				{
					break;
				}
				sobj = xmlObject.GetExtraMethodSource(cx);
				if (sobj != null)
				{
					thisObj = sobj;
					if (!(sobj is XMLObject))
					{
						func = ScriptableObject.GetProperty(sobj, methodName);
					}
				}
			}
			if (!(func is Callable))
			{
				throw ScriptRuntime.NotFunctionError(thisObj, func, methodName);
			}
			return ((Callable)func).Call(cx, scope, thisObj, args);
		}

		public virtual Scriptable Construct(Context cx, Scriptable scope, object[] args)
		{
			throw ScriptRuntime.TypeError1("msg.not.ctor", "XMLList");
		}
	}
}
