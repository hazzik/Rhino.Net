/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Javax.Xml.Parsers;
using Javax.Xml.Transform;
using Javax.Xml.Transform.Dom;
using Javax.Xml.Transform.Stream;
using Org.W3c.Dom;
using Org.Xml.Sax;
using Rhino;
using Rhino.Xmlimpl;
using Sharpen;

namespace Rhino.Xmlimpl
{
	[System.Serializable]
	internal class XmlProcessor
	{
		private const long serialVersionUID = 6903514433204808713L;

		private bool ignoreComments;

		private bool ignoreProcessingInstructions;

		private bool ignoreWhitespace;

		private bool prettyPrint;

		private int prettyIndent;

		[System.NonSerialized]
		private DocumentBuilderFactory dom;

		[System.NonSerialized]
		private TransformerFactory xform;

		[System.NonSerialized]
		private LinkedBlockingDeque<DocumentBuilder> documentBuilderPool;

		private XmlProcessor.RhinoSAXErrorHandler errorHandler = new XmlProcessor.RhinoSAXErrorHandler();

		//    Disambiguate from org.mozilla.javascript.Node
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		private void ReadObject(ObjectInputStream stream)
		{
			stream.DefaultReadObject();
			this.dom = DocumentBuilderFactory.NewInstance();
			this.dom.SetNamespaceAware(true);
			this.dom.SetIgnoringComments(false);
			this.xform = TransformerFactory.NewInstance();
			int poolSize = Runtime.GetRuntime().AvailableProcessors() * 2;
			this.documentBuilderPool = new LinkedBlockingDeque<DocumentBuilder>(poolSize);
		}

		[System.Serializable]
		private class RhinoSAXErrorHandler : ErrorHandler
		{
			private const long serialVersionUID = 6918417235413084055L;

			private void ThrowError(SAXParseException e)
			{
				throw ScriptRuntime.ConstructError("TypeError", e.Message, e.GetLineNumber() - 1);
			}

			public virtual void Error(SAXParseException e)
			{
				ThrowError(e);
			}

			public virtual void FatalError(SAXParseException e)
			{
				ThrowError(e);
			}

			public virtual void Warning(SAXParseException e)
			{
				Context.ReportWarning(e.Message);
			}
		}

		internal XmlProcessor()
		{
			SetDefault();
			this.dom = DocumentBuilderFactory.NewInstance();
			this.dom.SetNamespaceAware(true);
			this.dom.SetIgnoringComments(false);
			this.xform = TransformerFactory.NewInstance();
			int poolSize = Runtime.GetRuntime().AvailableProcessors() * 2;
			this.documentBuilderPool = new LinkedBlockingDeque<DocumentBuilder>(poolSize);
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

		// Get from pool, or create one without locking, if needed.
		/// <exception cref="Javax.Xml.Parsers.ParserConfigurationException"></exception>
		private DocumentBuilder GetDocumentBuilderFromPool()
		{
			DocumentBuilder builder = documentBuilderPool.PollFirst();
			if (builder == null)
			{
				builder = GetDomFactory().NewDocumentBuilder();
			}
			builder.SetErrorHandler(errorHandler);
			return builder;
		}

		// Insert into pool, if resettable. Pool capacity is limited to
		// number of processors * 2.
		private void ReturnDocumentBuilderToPool(DocumentBuilder db)
		{
			try
			{
				db.Reset();
				documentBuilderPool.OfferFirst(db);
			}
			catch (NotSupportedException)
			{
			}
		}

		// document builders that don't support reset() can't be pooled
		private void AddProcessingInstructionsTo(IList<System.Xml.XmlNode> list, System.Xml.XmlNode node)
		{
			if (node is ProcessingInstruction)
			{
				list.AddItem(node);
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
				list.AddItem(node);
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
					text.SetData(text.GetData().Trim());
				}
				else
				{
					if (text.GetData().Trim().Length == 0)
					{
						text.SetData(string.Empty);
					}
				}
				if (text.GetData().Length == 0)
				{
					toRemove.AddItem(node);
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
			DocumentBuilder builder = null;
			try
			{
				string syntheticXml = "<parent xmlns=\"" + defaultNamespaceUri + "\">" + xml + "</parent>";
				builder = GetDocumentBuilderFromPool();
				XmlDocument document = builder.Parse(new InputSource(new StringReader(syntheticXml)));
				if (ignoreProcessingInstructions)
				{
					IList<System.Xml.XmlNode> list = new AList<System.Xml.XmlNode>();
					AddProcessingInstructionsTo(list, document);
					foreach (System.Xml.XmlNode node in list)
					{
						node.ParentNode.RemoveChild(node);
					}
				}
				if (ignoreComments)
				{
					IList<System.Xml.XmlNode> list = new AList<System.Xml.XmlNode>();
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
					IList<System.Xml.XmlNode> list = new AList<System.Xml.XmlNode>();
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
			finally
			{
				if (builder != null)
				{
					ReturnDocumentBuilderToPool(builder);
				}
			}
		}

		internal virtual XmlDocument NewDocument()
		{
			DocumentBuilder builder = null;
			try
			{
				//    TODO    Should this use XML settings?
				builder = GetDocumentBuilderFromPool();
				return builder.NewDocument();
			}
			catch (ParserConfigurationException ex)
			{
				//    TODO    How to handle these runtime errors?
				throw new Exception(ex);
			}
			finally
			{
				if (builder != null)
				{
					ReturnDocumentBuilderToPool(builder);
				}
			}
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
			return Sharpen.Runtime.Substring(elementText, begin + 1, end);
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
			return (begin < end) ? Sharpen.Runtime.Substring(elementText, begin, end) : string.Empty;
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
				string data = ((XmlText)node).GetData();
				//    TODO Does Java trim() work same as XMLWhitespace?
				string v = (prettyPrint) ? data.Trim() : data;
				s.Append(EscapeElementValue(v));
				return s.ToString();
			}
			if (node is Attr)
			{
				string value = ((Attr)node).GetValue();
				s.Append(EscapeAttributeValue(value));
				return s.ToString();
			}
			if (node is XmlComment)
			{
				s.Append("<!--" + ((XmlComment)node).GetNodeValue() + "-->");
				return s.ToString();
			}
			if (node is ProcessingInstruction)
			{
				ProcessingInstruction pi = (ProcessingInstruction)node;
				s.Append("<?" + pi.GetTarget() + " " + pi.GetData() + "?>");
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
			AList<System.Xml.XmlNode> toIndent = new AList<System.Xml.XmlNode>();
			bool indentChildren = false;
			for (int i_2 = 0; i_2 < e.ChildNodes.Count; i_2++)
			{
				if (i_2 == 1)
				{
					indentChildren = true;
				}
				if (e.ChildNodes.Item(i_2) is XmlText)
				{
					toIndent.AddItem(e.ChildNodes.Item(i_2));
				}
				else
				{
					indentChildren = true;
					toIndent.AddItem(e.ChildNodes.Item(i_2));
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
			AList<XmlElement> list = new AList<XmlElement>();
			for (int i_4 = 0; i_4 < nodes.Count; i_4++)
			{
				if (nodes.Item(i_4) is XmlElement)
				{
					list.AddItem((XmlElement)nodes.Item(i_4));
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
