/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if XML

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Sharpen;

namespace Rhino.XmlImpl
{
	[System.Serializable]
	internal class XmlProcessor
	{
		private bool ignoreComments;

		private bool ignoreProcessingInstructions;

		private bool ignoreWhitespace;

		private bool prettyPrint;

		private int prettyIndent;

		[System.NonSerialized]
		private DocumentBuilderFactory dom;

		[System.NonSerialized]
		private TransformerFactory xform;

		//    Disambiguate from Rhino.Net.Node
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream stream)
		{
			stream.DefaultReadObject();
			this.dom = DocumentBuilderFactory.NewInstance();
			this.dom.SetNamespaceAware(true);
			this.dom.SetIgnoringComments(false);
			this.xform = TransformerFactory.NewInstance();
		}

		internal XmlProcessor()
		{
			SetDefault();
			this.dom = DocumentBuilderFactory.NewInstance();
			this.dom.SetNamespaceAware(true);
			this.dom.SetIgnoringComments(false);
			this.xform = TransformerFactory.NewInstance();
		}

		internal void SetDefault()
		{
			this.SetIgnoreComments(true);
			this.SetIgnoreProcessingInstructions(true);
			this.SetIgnoreWhitespace(true);
			this.SetPrettyPrinting(true);
			this.SetPrettyIndent(2);
		}

		internal void SetIgnoreComments(bool b)
		{
			this.ignoreComments = b;
		}

		internal void SetIgnoreWhitespace(bool b)
		{
			this.ignoreWhitespace = b;
		}

		internal void SetIgnoreProcessingInstructions(bool b)
		{
			this.ignoreProcessingInstructions = b;
		}

		internal void SetPrettyPrinting(bool b)
		{
			this.prettyPrint = b;
		}

		internal void SetPrettyIndent(int i)
		{
			this.prettyIndent = i;
		}

		internal bool IsIgnoreComments()
		{
			return ignoreComments;
		}

		internal bool IsIgnoreProcessingInstructions()
		{
			return ignoreProcessingInstructions;
		}

		internal bool IsIgnoreWhitespace()
		{
			return ignoreWhitespace;
		}

		internal bool IsPrettyPrinting()
		{
			return prettyPrint;
		}

		internal int GetPrettyIndent()
		{
			return prettyIndent;
		}

		private string ToXmlNewlines(string rv)
		{
			StringBuilder nl = new StringBuilder();
			for (int i = 0; i < rv.Length; i++)
			{
				if (rv[i] == '\r')
				{
					if (rv[i + 1] == '\n')
					{
					}
					else
					{
						//    DOS, do nothing and skip the \r
						//    Macintosh, substitute \n
						nl.Append('\n');
					}
				}
				else
				{
					nl.Append(rv[i]);
				}
			}
			return nl.ToString();
		}

		private DocumentBuilderFactory GetDomFactory()
		{
			return dom;
		}

		// document builders that don't support reset() can't be pooled
		private void AddProcessingInstructionsTo(IList<System.Xml.XmlNode> list, System.Xml.XmlNode node)
		{
			if (node is XmlProcessingInstruction)
			{
				list.Add(node);
			}
			if (node.ChildNodes != null)
			{
				for (int i = 0; i < node.ChildNodes.Count; i++)
				{
					AddProcessingInstructionsTo(list, node.ChildNodes.Item(i));
				}
			}
		}

		private void AddCommentsTo(IList<System.Xml.XmlNode> list, System.Xml.XmlNode node)
		{
			if (node is XmlComment)
			{
				list.Add(node);
			}
			if (node.ChildNodes != null)
			{
				for (int i = 0; i < node.ChildNodes.Count; i++)
				{
					AddProcessingInstructionsTo(list, node.ChildNodes.Item(i));
				}
			}
		}

		private void AddTextNodesToRemoveAndTrim(IList<System.Xml.XmlNode> toRemove, System.Xml.XmlNode node)
		{
			if (node is XmlText)
			{
				XmlText text = (XmlText)node;
				bool BUG_369394_IS_VALID = false;
				if (!BUG_369394_IS_VALID)
				{
					text.Data = text.Data.Trim();
				}
				else
				{
					if (text.Data.Trim().Length == 0)
					{
						text.Data = string.Empty;
					}
				}
				if (text.Data.Length == 0)
				{
					toRemove.Add(node);
				}
			}
			if (node.ChildNodes != null)
			{
				for (int i = 0; i < node.ChildNodes.Count; i++)
				{
					AddTextNodesToRemoveAndTrim(toRemove, node.ChildNodes.Item(i));
				}
			}
		}

		/// <exception cref="Org.Xml.Sax.SAXException"></exception>
		internal System.Xml.XmlNode ToXml(string defaultNamespaceUri, string xml)
		{
			//    See ECMA357 10.3.1
			try
			{
				string syntheticXml = string.Format("<parent xmlns=\"{0}\">{1}</parent>", defaultNamespaceUri, xml);
				var document = new XmlDocument();
				document.LoadXml(syntheticXml);
				if (ignoreProcessingInstructions)
				{
					IList<System.Xml.XmlNode> list = new List<System.Xml.XmlNode>();
					AddProcessingInstructionsTo(list, document);
					foreach (System.Xml.XmlNode node in list)
					{
						node.ParentNode.RemoveChild(node);
					}
				}
				if (ignoreComments)
				{
					IList<System.Xml.XmlNode> list = new List<System.Xml.XmlNode>();
					AddCommentsTo(list, document);
					foreach (System.Xml.XmlNode node in list)
					{
						node.ParentNode.RemoveChild(node);
					}
				}
				if (ignoreWhitespace)
				{
					//    Apparently JAXP setIgnoringElementContentWhitespace() has a different meaning, it appears from the Javadoc
					//    Refers to element-only content models, which means we would need to have a validating parser and DTD or schema
					//    so that it would know which whitespace to ignore.
					//    Instead we will try to delete it ourselves.
					IList<System.Xml.XmlNode> list = new List<System.Xml.XmlNode>();
					AddTextNodesToRemoveAndTrim(list, document);
					foreach (System.Xml.XmlNode node in list)
					{
						node.ParentNode.RemoveChild(node);
					}
				}
				XmlNodeList rv = document.DocumentElement.ChildNodes;
				if (rv.Count > 1)
				{
					throw ScriptRuntime.ConstructError("SyntaxError", "XML objects may contain at most one node.");
				}
				else
				{
					if (rv.Count == 0)
					{
						System.Xml.XmlNode node = document.CreateTextNode(string.Empty);
						return node;
					}
					else
					{
						System.Xml.XmlNode node = rv.Item(0);
						document.DocumentElement.RemoveChild(node);
						return node;
					}
				}
			}
			catch (IOException)
			{
				throw new Exception("Unreachable.");
			}
			catch (ParserConfigurationException e)
			{
				throw new Exception(e);
			}
		}

		internal virtual XmlDocument NewDocument()
		{
			return new XmlDocument();
		}

		//    TODO    Cannot remember what this is for, so whether it should use settings or not
		private string ToString(System.Xml.XmlNode node)
		{
			DOMSource source = new DOMSource(node);
			StringWriter writer = new StringWriter();
			StreamResult result = new StreamResult(writer);
			try
			{
				Transformer transformer = xform.NewTransformer();
				transformer.SetOutputProperty(OutputKeys.OMIT_XML_DECLARATION, "yes");
				transformer.SetOutputProperty(OutputKeys.INDENT, "no");
				transformer.SetOutputProperty(OutputKeys.METHOD, "xml");
				transformer.Transform(source, result);
			}
			catch (TransformerConfigurationException ex)
			{
				//    TODO    How to handle these runtime errors?
				throw new Exception(ex);
			}
			catch (TransformerException ex)
			{
				//    TODO    How to handle these runtime errors?
				throw new Exception(ex);
			}
			return ToXmlNewlines(writer.ToString());
		}

		internal virtual string EscapeAttributeValue(object value)
		{
			string text = ScriptRuntime.ToString(value);
			if (text.Length == 0)
			{
				return string.Empty;
			}
			XmlDocument dom = NewDocument();
			XmlElement e = dom.CreateElement("a");
			e.SetAttribute("b", text);
			string elementText = ToString(e);
			int begin = elementText.IndexOf('"');
			int end = elementText.LastIndexOf('"');
			int index = begin + 1;
			return elementText.Substring(index, end - index);
		}

		internal virtual string EscapeTextValue(object value)
		{
			if (value is XMLObjectImpl)
			{
				return ((XMLObjectImpl)value).ToXMLString();
			}
			string text = ScriptRuntime.ToString(value);
			if (text.Length == 0)
			{
				return text;
			}
			XmlDocument dom = NewDocument();
			XmlElement e = dom.CreateElement("a");
			e.InnerText = text;
			string elementText = ToString(e);
			int begin = elementText.IndexOf('>') + 1;
			int end = elementText.LastIndexOf('<');
			return (begin < end) ? elementText.Substring(begin, end - begin) : string.Empty;
		}

		private string EscapeElementValue(string s)
		{
			//    TODO    Check this
			return EscapeTextValue(s);
		}

		private string ElementToXmlString(XmlElement element)
		{
			//    TODO    My goodness ECMA is complicated (see 10.2.1).  We'll try this first.
			XmlElement copy = (XmlElement)element.CloneNode(true);
			if (prettyPrint)
			{
				BeautifyElement(copy, 0);
			}
			return ToString(copy);
		}

		internal string EcmaToXmlString(System.Xml.XmlNode node)
		{
			//    See ECMA 357 Section 10.2.1
			StringBuilder s = new StringBuilder();
			int indentLevel = 0;
			if (prettyPrint)
			{
				for (int i = 0; i < indentLevel; i++)
				{
					s.Append(' ');
				}
			}
			if (node is XmlText)
			{
				string data = ((XmlText)node).Data;
				//    TODO Does Java trim() work same as XMLWhitespace?
				string v = (prettyPrint) ? data.Trim() : data;
				s.Append(EscapeElementValue(v));
				return s.ToString();
			}
			if (node is XmlAttribute)
			{
				string value = ((XmlAttribute)node).Value;
				s.Append(EscapeAttributeValue(value));
				return s.ToString();
			}
			if (node is XmlComment)
			{
				s.Append("<!--" + ((XmlComment)node).GetNodeValue() + "-->");
				return s.ToString();
			}
			if (node is XmlProcessingInstruction)
			{
				XmlProcessingInstruction pi = (XmlProcessingInstruction)node;
				s.Append("<?" + pi.Target + " " + pi.GetData() + "?>");
				return s.ToString();
			}
			s.Append(ElementToXmlString((XmlElement)node));
			return s.ToString();
		}

		private void BeautifyElement(XmlElement e, int indent)
		{
			StringBuilder s = new StringBuilder();
			s.Append('\n');
			for (int i = 0; i < indent; i++)
			{
				s.Append(' ');
			}
			string afterContent = s.ToString();
			for (int i_1 = 0; i_1 < prettyIndent; i_1++)
			{
				s.Append(' ');
			}
			string beforeContent = s.ToString();
			//    We "mark" all the nodes first; if we tried to do this loop otherwise, it would behave unexpectedly (the inserted nodes
			//    would contribute to the length and it might never terminate).
			List<System.Xml.XmlNode> toIndent = new List<System.Xml.XmlNode>();
			bool indentChildren = false;
			for (int i_2 = 0; i_2 < e.ChildNodes.Count; i_2++)
			{
				if (i_2 == 1)
				{
					indentChildren = true;
				}
				if (e.ChildNodes.Item(i_2) is XmlText)
				{
					toIndent.Add(e.ChildNodes.Item(i_2));
				}
				else
				{
					indentChildren = true;
					toIndent.Add(e.ChildNodes.Item(i_2));
				}
			}
			if (indentChildren)
			{
				for (int i_3 = 0; i_3 < toIndent.Count; i_3++)
				{
					e.InsertBefore(e.OwnerDocument.CreateTextNode(beforeContent), toIndent[i_3]);
				}
			}
			XmlNodeList nodes = e.ChildNodes;
			List<XmlElement> list = new List<XmlElement>();
			for (int i_4 = 0; i_4 < nodes.Count; i_4++)
			{
				if (nodes.Item(i_4) is XmlElement)
				{
					list.Add((XmlElement)nodes.Item(i_4));
				}
			}
			foreach (XmlElement elem in list)
			{
				BeautifyElement(elem, indent + prettyIndent);
			}
			if (indentChildren)
			{
				e.AppendChild(e.OwnerDocument.CreateTextNode(afterContent));
			}
		}
	}
}
#endif