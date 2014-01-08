/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Rhino;
using Rhino.Json;
using Rhino.Tests.Json;
using Sharpen;

namespace Rhino.Tests.Json
{
	[NUnit.Framework.TestFixture]
	public class JsonParserTest
	{
		private JsonParser parser;

		private Context cx;

		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			cx = Context.Enter();
			parser = new JsonParser(cx, cx.InitStandardObjects());
		}

		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			Context.Exit();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseIllegalWhitespaceChars()
		{
			parser.ParseValue(" \u000b 1");
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseJsonNull()
		{
			NUnit.Framework.Assert.AreEqual(null, parser.ParseValue("null"));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseJavaNull()
		{
			parser.ParseValue(null);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseJsonBoolean()
		{
			NUnit.Framework.Assert.AreEqual(true, parser.ParseValue("true"));
			NUnit.Framework.Assert.AreEqual(false, parser.ParseValue("false"));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseJsonNumbers()
		{
			NUnit.Framework.Assert.AreEqual(1, parser.ParseValue("1"));
			NUnit.Framework.Assert.AreEqual(-1, parser.ParseValue("-1"));
			NUnit.Framework.Assert.AreEqual(1.5, parser.ParseValue("1.5"));
			NUnit.Framework.Assert.AreEqual(1.5e13, parser.ParseValue("1.5e13"));
			NUnit.Framework.Assert.AreEqual(1.0e16, parser.ParseValue("9999999999999999"));
			NUnit.Framework.Assert.AreEqual(double.PositiveInfinity, parser.ParseValue("1.5e99999999"));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseDoubleNegativeNumbers()
		{
			parser.ParseValue("--5");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseNumbersWithDecimalExponent()
		{
			parser.ParseValue("5e5.5");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseNumbersBeginningWithZero()
		{
			parser.ParseValue("05");
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseJsonString()
		{
			NUnit.Framework.Assert.AreEqual("hello", parser.ParseValue("\"hello\""));
			NUnit.Framework.Assert.AreEqual("Sch\u00f6ne Gr\u00fc\u00dfe", parser.ParseValue("\"Sch\\u00f6ne Gr\\u00fc\\u00dfe\""));
			NUnit.Framework.Assert.AreEqual(string.Empty, parser.ParseValue(Str('"', '"')));
			NUnit.Framework.Assert.AreEqual(" ", parser.ParseValue(Str('"', ' ', '"')));
			NUnit.Framework.Assert.AreEqual("\r", parser.ParseValue(Str('"', '\\', 'r', '"')));
			NUnit.Framework.Assert.AreEqual("\n", parser.ParseValue(Str('"', '\\', 'n', '"')));
			NUnit.Framework.Assert.AreEqual("\t", parser.ParseValue(Str('"', '\\', 't', '"')));
			NUnit.Framework.Assert.AreEqual("\\", parser.ParseValue(Str('"', '\\', '\\', '"')));
			NUnit.Framework.Assert.AreEqual("/", parser.ParseValue(Str('"', '/', '"')));
			NUnit.Framework.Assert.AreEqual("/", parser.ParseValue(Str('"', '\\', '/', '"')));
			NUnit.Framework.Assert.AreEqual("\"", parser.ParseValue(Str('"', '\\', '"', '"')));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseEmptyJavaString()
		{
			parser.ParseValue(string.Empty);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseSingleDoubleQuote()
		{
			parser.ParseValue(Str('"'));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseStringContainingSingleBackslash()
		{
			parser.ParseValue(Str('"', '\\', '"'));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseStringIllegalStringChars()
		{
			parser.ParseValue(Str('"', '\n', '"'));
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseEmptyJsonArray()
		{
			NUnit.Framework.Assert.AreEqual(0, ((NativeArray)parser.ParseValue("[]")).GetLength());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseHeterogeneousJsonArray()
		{
			NativeArray actual = (NativeArray)parser.ParseValue("[ \"hello\" , 3, null, [false] ]");
			NUnit.Framework.Assert.AreEqual("hello", actual.Get(0, actual));
			NUnit.Framework.Assert.AreEqual(3, actual.Get(1, actual));
			NUnit.Framework.Assert.AreEqual(null, actual.Get(2, actual));
			NativeArray innerArr = (NativeArray)actual.Get(3, actual);
			NUnit.Framework.Assert.AreEqual(false, innerArr.Get(0, innerArr));
			NUnit.Framework.Assert.AreEqual(4, actual.GetLength());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseArrayWithInvalidElements()
		{
			parser.ParseValue("[wtf]");
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseJsonObject()
		{
			string json = "{" + "\"bool\" : false, " + "\"str\"  : \"xyz\", " + "\"obj\"  : {\"a\":1} " + "}";
			NativeObject actual = (NativeObject)parser.ParseValue(json);
			NUnit.Framework.Assert.AreEqual(false, actual.Get("bool", actual));
			NUnit.Framework.Assert.AreEqual("xyz", actual.Get("str", actual));
			NativeObject innerObj = (NativeObject)actual.Get("obj", actual);
			NUnit.Framework.Assert.AreEqual(1, innerObj.Get("a", innerObj));
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseJsonObjectsWithInvalidFormat()
		{
			parser.ParseValue("{\"only\", \"keys\"}");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseMoreThanOneToplevelValue()
		{
			parser.ParseValue("1 2");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseStringTruncatedUnicode()
		{
			parser.ParseValue("\"\\u00f\"");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseStringControlChars1()
		{
			parser.ParseValue("\"\u0000\"");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseStringControlChars2()
		{
			parser.ParseValue("\"\u001f\"");
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldAllowTrailingWhitespace()
		{
			parser.ParseValue("1 ");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldThrowParseExceptionWhenIncompleteObject()
		{
			parser.ParseValue("{\"a\" ");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldThrowParseExceptionWhenIncompleteArray()
		{
			parser.ParseValue("[1 ");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseIllegalUnicodeEscapeSeq()
		{
			parser.ParseValue("\"\\u-123\"");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseIllegalUnicodeEscapeSeq2()
		{
			parser.ParseValue("\"\\u006\u0661\"");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseIllegalUnicodeEscapeSeq3()
		{
			parser.ParseValue("\"\\u006Ù¡\"");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseTrailingCommaInObject1()
		{
			parser.ParseValue("{\"a\": 1,}");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseTrailingCommaInObject2()
		{
			parser.ParseValue("{,\"a\": 1}");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseTrailingCommaInObject3()
		{
			parser.ParseValue("{,}");
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseEmptyObject()
		{
			parser.ParseValue("{}");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseTrailingCommaInArray1()
		{
			parser.ParseValue("[1,]");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseTrailingCommaInArray2()
		{
			parser.ParseValue("[,1]");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseTrailingCommaInArray3()
		{
			parser.ParseValue("[,]");
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void ShouldParseEmptyArray()
		{
			parser.ParseValue("[]");
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void ShouldFailToParseIllegalNumber()
		{
			parser.ParseValue("1.");
		}

		private string Str(params char[] chars)
		{
			return new string(chars);
		}
	}
}
