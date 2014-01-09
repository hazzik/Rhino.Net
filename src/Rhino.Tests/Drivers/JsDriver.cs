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
using Rhino.Drivers;
using Rhino.Tools.Shell;
using Sharpen;

namespace Rhino.Drivers
{
	/// <version>$Id: JsDriver.java,v 1.10 2009/05/15 12:30:45 nboyd%atg.com Exp $</version>
	public class JsDriver
	{
		private JsDriver()
		{
		}

		private static string Join(string[] list)
		{
			string rv = string.Empty;
			for (int i = 0; i < list.Length; i++)
			{
				rv += list[i];
				if (i + 1 != list.Length)
				{
					rv += ",";
				}
			}
			return rv;
		}

		private class Tests
		{
			private FilePath testDirectory;

			private string[] list;

			private string[] skip;

			/// <exception cref="System.IO.IOException"></exception>
			internal Tests(FilePath testDirectory, string[] list, string[] skip)
			{
				this.testDirectory = testDirectory;
				this.list = GetTestList(list);
				this.skip = GetTestList(skip);
			}

			/// <exception cref="System.IO.IOException"></exception>
			private string[] GetTestList(string[] tests)
			{
				List<string> list = new List<string>();
				for (int i = 0; i < tests.Length; i++)
				{
					if (tests[i].StartsWith("@"))
					{
						TestUtils.AddTestsFromFile(Sharpen.Runtime.Substring(tests[i], 1), list);
					}
					else
					{
						list.Add(tests[i]);
					}
				}
				return Sharpen.Collections.ToArray(list, new string[0]);
			}

			private bool Matches(string path)
			{
				if (list.Length == 0)
				{
					return true;
				}
				return TestUtils.Matches(list, path);
			}

			private bool Excluded(string path)
			{
				if (skip.Length == 0)
				{
					return false;
				}
				return TestUtils.Matches(skip, path);
			}

			private void AddFiles(IList<JsDriver.Tests.Script> rv, string prefix, FilePath directory)
			{
				FilePath[] files = directory.ListFiles();
				if (files == null)
				{
					throw new Exception("files null for " + directory);
				}
				for (int i = 0; i < files.Length; i++)
				{
					string path = prefix + files[i].GetName();
					if (ShellTest.DIRECTORY_FILTER.Accept(files[i]))
					{
						AddFiles(rv, path + "/", files[i]);
					}
					else
					{
						bool isTopLevel = prefix.Length == 0;
						if (ShellTest.TEST_FILTER.Accept(files[i]) && Matches(path) && !Excluded(path) && !isTopLevel)
						{
							rv.Add(new JsDriver.Tests.Script(path, files[i]));
						}
					}
				}
			}

			internal class Script
			{
				private string path;

				private FilePath file;

				internal Script(string path, FilePath file)
				{
					this.path = path;
					this.file = file;
				}

				internal virtual string GetPath()
				{
					return path;
				}

				internal virtual FilePath GetFile()
				{
					return file;
				}
			}

			internal virtual JsDriver.Tests.Script[] GetFiles()
			{
				List<JsDriver.Tests.Script> rv = new List<JsDriver.Tests.Script>();
				AddFiles(rv, string.Empty, testDirectory);
				return Sharpen.Collections.ToArray(rv, new JsDriver.Tests.Script[0]);
			}
		}

		private class ConsoleStatus : ShellTest.Status
		{
			private FilePath jsFile;

			private JsDriver.Arguments.Console console;

			private bool trace;

			private bool failed;

			internal ConsoleStatus(JsDriver.Arguments.Console console, bool trace)
			{
				this.console = console;
				this.trace = trace;
			}

			public override void Running(FilePath jsFile)
			{
				try
				{
					console.Println("Running: " + jsFile.GetCanonicalPath());
					this.jsFile = jsFile;
				}
				catch (IOException e)
				{
					throw new Exception(e);
				}
			}

			public override void Failed(string s)
			{
				console.Println("Failed: " + jsFile + ": " + s);
				failed = true;
			}

			public override void Threw(Exception t)
			{
				console.Println("Failed: " + jsFile + " with exception.");
				console.Println(ShellTest.GetStackTrace(t));
				failed = true;
			}

			public override void TimedOut()
			{
				console.Println("Failed: " + jsFile + ": timed out.");
				failed = true;
			}

			public override void ExitCodesWere(int expected, int actual)
			{
				if (expected != actual)
				{
					console.Println("Failed: " + jsFile + " expected " + expected + " actual " + actual);
					failed = true;
				}
			}

			public override void OutputWas(string s)
			{
				if (!failed)
				{
					console.Println("Passed: " + jsFile);
					if (trace)
					{
						console.Println(s);
					}
				}
			}
		}

		//    returns true if node was found, false otherwise
		private static bool SetContent(XmlElement node, string id, string content)
		{
			if (node.GetAttribute("id").Equals(id))
			{
				node.InnerText = node.InnerText + "\n" + content;
				return true;
			}
			else
			{
				XmlNodeList children = node.ChildNodes;
				for (int i = 0; i < children.Count; i++)
				{
					if (children.Item(i) is XmlElement)
					{
						XmlElement e = (XmlElement)children.Item(i);
						bool rv = SetContent(e, id, content);
						if (rv)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private static XmlElement GetElementById(XmlElement node, string id)
		{
			if (node.GetAttribute("id").Equals(id))
			{
				return node;
			}
			else
			{
				XmlNodeList children = node.ChildNodes;
				for (int i = 0; i < children.Count; i++)
				{
					if (children.Item(i) is XmlElement)
					{
						XmlElement rv = GetElementById((XmlElement)children.Item(i), id);
						if (rv != null)
						{
							return rv;
						}
					}
				}
			}
			return null;
		}

		private static string NewlineLineEndings(string s)
		{
			StringBuilder rv = new StringBuilder();
			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] == '\r')
				{
					if (i + 1 < s.Length && s[i + 1] == '\n')
					{
					}
					else
					{
						//    just skip \r
						//    Macintosh, substitute \n
						rv.Append('\n');
					}
				}
				else
				{
					rv.Append(s[i]);
				}
			}
			return rv.ToString();
		}

		private class HtmlStatus : ShellTest.Status
		{
			private string testPath;

			private string bugUrl;

			private string lxrUrl;

			private XmlDocument html;

			private XmlElement failureHtml;

			private bool failed;

			private string output;

			internal HtmlStatus(string lxrUrl, string bugUrl, string testPath, XmlDocument html, XmlElement failureHtml)
			{
				this.testPath = testPath;
				this.bugUrl = bugUrl;
				this.lxrUrl = lxrUrl;
				this.html = html;
				this.failureHtml = failureHtml;
			}

			public override void Running(FilePath file)
			{
			}

			public override void Failed(string s)
			{
				failed = true;
				SetContent(failureHtml, "failureDetails.reason", "Failure reason: \n" + s);
			}

			public override void ExitCodesWere(int expected, int actual)
			{
				if (expected != actual)
				{
					failed = true;
					SetContent(failureHtml, "failureDetails.reason", "expected exit code " + expected + " but got " + actual);
				}
			}

			public override void Threw(Exception e)
			{
				failed = true;
				SetContent(failureHtml, "failureDetails.reason", "Threw Java exception:\n" + NewlineLineEndings(ShellTest.GetStackTrace(e)));
			}

			public override void TimedOut()
			{
				failed = true;
				SetContent(failureHtml, "failureDetails.reason", "Timed out.");
			}

			public override void OutputWas(string s)
			{
				this.output = s;
			}

			private string GetLinesStartingWith(string prefix)
			{
				BufferedReader r = new BufferedReader(new StringReader(output));
				string line = null;
				string rv = string.Empty;
				try
				{
					while ((line = r.ReadLine()) != null)
					{
						if (line.StartsWith(prefix))
						{
							if (rv.Length > 0)
							{
								rv += "\n";
							}
							rv += line;
						}
					}
					return rv;
				}
				catch (IOException)
				{
					throw new Exception("Can't happen.");
				}
			}

			internal virtual bool Failed()
			{
				return failed;
			}

			internal virtual void Finish()
			{
				if (failed)
				{
					GetElementById(failureHtml, "failureDetails.status").InnerText = GetLinesStartingWith("STATUS:");
					string bn = GetLinesStartingWith("BUGNUMBER:");
					XmlElement bnlink = GetElementById(failureHtml, "failureDetails.bug.href");
					if (bn.Length > 0)
					{
						string number = Sharpen.Runtime.Substring(bn, "BUGNUMBER: ".Length);
						if (!number.Equals("none"))
						{
							bnlink.SetAttribute("href", bugUrl + number);
							GetElementById(bnlink, "failureDetails.bug.number").InnerText = number;
						}
						else
						{
							bnlink.ParentNode.RemoveChild(bnlink);
						}
					}
					else
					{
						bnlink.ParentNode.RemoveChild(bnlink);
					}
					GetElementById(failureHtml, "failureDetails.lxr").SetAttribute("href", lxrUrl + testPath);
					GetElementById(failureHtml, "failureDetails.lxr.text").InnerText = testPath;
					GetElementById(html.DocumentElement, "retestList.text").InnerText = GetElementById(html.DocumentElement, "retestList.text").InnerText + testPath + "\n";
					GetElementById(html.DocumentElement, "failureDetails").AppendChild(failureHtml);
				}
			}
		}

		private class XmlStatus : ShellTest.Status
		{
			private XmlElement target;

			private DateTime start;

			internal XmlStatus(string path, XmlElement root)
			{
				this.target = root.OwnerDocument.CreateElement("test");
				this.target.SetAttribute("path", path);
				root.AppendChild(target);
			}

			public override void Running(FilePath file)
			{
				this.start = new DateTime();
			}

			private XmlElement CreateElement(XmlElement parent, string name)
			{
				XmlElement rv = parent.OwnerDocument.CreateElement(name);
				parent.AppendChild(rv);
				return rv;
			}

			private void Finish()
			{
				DateTime end = new DateTime();
				long elapsed = end.GetTime() - start.GetTime();
				this.target.SetAttribute("elapsed", elapsed.ToString());
			}

			private void SetTextContent(XmlElement e, string content)
			{
				e.InnerText = NewlineLineEndings(content);
			}

			public override void ExitCodesWere(int expected, int actual)
			{
				Finish();
				XmlElement exit = CreateElement(target, "exit");
				exit.SetAttribute("expected", expected.ToString());
				exit.SetAttribute("actual", actual.ToString());
			}

			public override void TimedOut()
			{
				Finish();
				CreateElement(target, "timedOut");
			}

			public override void Failed(string s)
			{
				Finish();
				XmlElement failed = CreateElement(target, "failed");
				SetTextContent(failed, s);
			}

			public override void OutputWas(string message)
			{
				Finish();
				XmlElement output = CreateElement(target, "output");
				SetTextContent(output, message);
			}

			public override void Threw(Exception t)
			{
				Finish();
				XmlElement threw = CreateElement(target, "threw");
				SetTextContent(threw, ShellTest.GetStackTrace(t));
			}
		}

		private class Results
		{
			private ShellContextFactory factory;

			private JsDriver.Arguments arguments;

			private FilePath output;

			private bool trace;

			private XmlDocument html;

			private XmlElement failureHtml;

			private XmlDocument xml;

			private DateTime start;

			private int tests;

			private int failures;

			internal Results(ShellContextFactory factory, JsDriver.Arguments arguments, bool trace)
			{
				this.factory = factory;
				this.arguments = arguments;
				FilePath output = arguments.GetOutputFile();
				if (output == null)
				{
					output = new FilePath("rhino-test-results." + new SimpleDateFormat("yyyy.MM.dd.HH.mm.ss").Format(new DateTime()) + ".html");
				}
				this.output = output;
				this.trace = trace;
			}

			private XmlDocument Parse(Stream @in)
			{
				try
				{
					DocumentBuilderFactory factory = DocumentBuilderFactory.NewInstance();
					factory.SetValidating(false);
					DocumentBuilder dom = factory.NewDocumentBuilder();
					return dom.Parse(@in);
				}
				catch (Exception t)
				{
					throw new Exception("Parser failure", t);
				}
			}

			private XmlDocument GetTemplate()
			{
				return Parse(GetType().GetResourceAsStream("results.html"));
			}

			private void Write(XmlDocument template, bool xml)
			{
				try
				{
					FilePath output = this.output;
					TransformerFactory factory = TransformerFactory.NewInstance();
					Transformer xform = factory.NewTransformer();
					if (xml)
					{
						xform.SetOutputProperty(OutputKeys.METHOD, "xml");
						xform.SetOutputProperty(OutputKeys.OMIT_XML_DECLARATION, "yes");
						output = new FilePath(output.GetCanonicalPath() + ".xml");
					}
					xform.Transform(new DOMSource(template), new StreamResult(new FileOutputStream(output)));
				}
				catch (IOException e)
				{
					arguments.GetConsole().Println("Could not write results file to " + output + ": ");
					Sharpen.Runtime.PrintStackTrace(e, System.Console.Error);
				}
				catch (TransformerConfigurationException e)
				{
					throw new Exception("Parser failure", e);
				}
				catch (TransformerException e)
				{
					throw new Exception("Parser failure", e);
				}
			}

			internal virtual void Start()
			{
				this.html = GetTemplate();
				this.failureHtml = GetElementById(html.DocumentElement, "failureDetails.prototype");
				if (this.failureHtml == null)
				{
					try
					{
						TransformerFactory.NewInstance().NewTransformer().Transform(new DOMSource(html), new StreamResult(System.Console.Error));
					}
					catch (Exception t)
					{
						throw new Exception(t);
					}
					throw new Exception("No");
				}
				this.failureHtml.ParentNode.RemoveChild(this.failureHtml);
				try
				{
					this.xml = DocumentBuilderFactory.NewInstance().NewDocumentBuilder().GetDOMImplementation().CreateDocument(null, "results", null);
					xml.DocumentElement.SetAttribute("timestamp", new DateTime().GetTime().ToString());
					xml.DocumentElement.SetAttribute("optimization", arguments.GetOptimizationLevel().ToString());
					xml.DocumentElement.SetAttribute("strict", arguments.IsStrict().ToString());
					xml.DocumentElement.SetAttribute("timeout", arguments.GetTimeout().ToString());
				}
				catch (ParserConfigurationException e)
				{
					throw new Exception(e);
				}
				this.start = new DateTime();
			}

			internal virtual void Run(JsDriver.Tests.Script script, ShellTest.Parameters parameters)
			{
				string path = script.GetPath();
				FilePath test = script.GetFile();
				JsDriver.ConsoleStatus cStatus = new JsDriver.ConsoleStatus(arguments.GetConsole(), trace);
				JsDriver.HtmlStatus hStatus = new JsDriver.HtmlStatus(arguments.GetLxrUrl(), arguments.GetBugUrl(), path, html, (XmlElement)failureHtml.CloneNode(true));
				JsDriver.XmlStatus xStatus = new JsDriver.XmlStatus(path, this.xml.DocumentElement);
				ShellTest.Status status = ShellTest.Status.Compose(new ShellTest.Status[] { cStatus, hStatus, xStatus });
				try
				{
					ShellTest.Run(factory, test, parameters, status);
				}
				catch (Exception e)
				{
					throw new Exception(e);
				}
				tests++;
				if (hStatus.Failed())
				{
					failures++;
				}
				hStatus.Finish();
			}

			private void Set(XmlDocument document, string id, string value)
			{
				GetElementById(document.DocumentElement, id).InnerText = value;
			}

			internal virtual void Finish()
			{
				DateTime end = new DateTime();
				long elapsedMs = end.GetTime() - start.GetTime();
				Set(html, "results.testlist", Join(arguments.GetTestList()));
				Set(html, "results.skiplist", Join(arguments.GetSkipList()));
				string pct = new DecimalFormat("##0.00").Format((double)failures / (double)tests * 100.0);
				Set(html, "results.results", "Tests attempted: " + tests + " Failures: " + failures + " (" + pct + "%)");
				Set(html, "results.platform", "java.home=" + Runtime.GetProperty("java.home") + "\n" + "java.version=" + Runtime.GetProperty("java.version") + "\n" + "os.name=" + Runtime.GetProperty("os.name"));
				Set(html, "results.classpath", Runtime.GetProperty("java.class.path").Replace(FilePath.pathSeparatorChar, ' '));
				int elapsedSeconds = (int)(elapsedMs / 1000);
				int elapsedMinutes = elapsedSeconds / 60;
				elapsedSeconds = elapsedSeconds % 60;
				string elapsed = string.Empty + elapsedMinutes + " minutes, " + elapsedSeconds + " seconds";
				Set(html, "results.elapsed", elapsed);
				Set(html, "results.time", new SimpleDateFormat("MMMM d yyyy h:mm:ss aa").Format(new DateTime()));
				Write(html, false);
				Write(xml, true);
			}
		}

		private class ShellTestParameters : ShellTest.Parameters
		{
			private int timeout;

			internal ShellTestParameters(int timeout)
			{
				this.timeout = timeout;
			}

			public override int GetTimeoutMilliseconds()
			{
				return timeout;
			}
		}

		/// <exception cref="System.Exception"></exception>
		internal virtual void Run(JsDriver.Arguments arguments)
		{
			if (arguments.Help())
			{
				System.Console.Out.WriteLine("See mozilla/js/tests/README-jsDriver.html; note that some options are not supported.");
				System.Console.Out.WriteLine("Consult the Java source code at testsrc/org/mozilla/javascript/JsDriver.java for details.");
				System.Environment.Exit(0);
			}
			ShellContextFactory factory = new ShellContextFactory();
			factory.SetOptimizationLevel(arguments.GetOptimizationLevel());
			factory.SetStrictMode(arguments.IsStrict());
			FilePath path = arguments.GetTestsPath();
			if (path == null)
			{
				path = new FilePath("../tests");
			}
			if (!path.Exists())
			{
				throw new Exception("JavaScript tests not found at " + path.GetCanonicalPath());
			}
			JsDriver.Tests tests = new JsDriver.Tests(path, arguments.GetTestList(), arguments.GetSkipList());
			JsDriver.Tests.Script[] all = tests.GetFiles();
			arguments.GetConsole().Println("Running " + all.Length + " tests.");
			JsDriver.Results results = new JsDriver.Results(factory, arguments, arguments.Trace());
			results.Start();
			for (int i = 0; i < all.Length; i++)
			{
				results.Run(all[i], new JsDriver.ShellTestParameters(arguments.GetTimeout()));
			}
			results.Finish();
		}

		/// <exception cref="System.Exception"></exception>
		public static void Main(JsDriver.Arguments arguments)
		{
			JsDriver driver = new JsDriver();
			driver.Run(arguments);
		}

		private class Arguments
		{
			private List<JsDriver.Arguments.Option> options = new List<JsDriver.Arguments.Option>();

			private JsDriver.Arguments.Option bugUrl;

			private JsDriver.Arguments.Option optimizationLevel;

			private JsDriver.Arguments.Option strict;

			private JsDriver.Arguments.Option outputFile;

			private JsDriver.Arguments.Option help;

			private JsDriver.Arguments.Option logFailuresToConsole;

			private JsDriver.Arguments.Option testList;

			private JsDriver.Arguments.Option skipList;

			private JsDriver.Arguments.Option testsPath;

			private JsDriver.Arguments.Option trace;

			private JsDriver.Arguments.Option lxrUrl;

			private JsDriver.Arguments.Option timeout;

			public class Console
			{
				public virtual void Print(string message)
				{
					System.Console.Out.Write(message);
				}

				public virtual void Println(string message)
				{
					System.Console.Out.WriteLine(message);
				}
			}

			private JsDriver.Arguments.Console console = new JsDriver.Arguments.Console();

			private class Option
			{
				private string letterOption;

				private string wordOption;

				private bool array;

				private bool flag;

				private bool ignored;

				private List<string> values = new List<string>();

				internal Option(Arguments _enclosing, string letterOption, string wordOption, bool array, bool flag, string unspecified)
				{
					this._enclosing = _enclosing;
					//    array: can this option have multiple values?
					//    flag: is this option a simple true/false switch?
					this.letterOption = letterOption;
					this.wordOption = wordOption;
					this.flag = flag;
					this.array = array;
					if (!flag && !array)
					{
						this.values.Add(unspecified);
					}
					this._enclosing.options.Add(this);
				}

				internal virtual JsDriver.Arguments.Option Ignored()
				{
					this.ignored = true;
					return this;
				}

				internal virtual int GetInt()
				{
					return System.Convert.ToInt32(this.GetValue());
				}

				internal virtual string GetValue()
				{
					return this.values[0];
				}

				internal virtual bool GetSwitch()
				{
					return this.values.Count > 0;
				}

				internal virtual FilePath GetFile()
				{
					if (this.GetValue() == null)
					{
						return null;
					}
					return new FilePath(this.GetValue());
				}

				internal virtual string[] GetValues()
				{
					return Sharpen.Collections.ToArray(this.values, new string[0]);
				}

				internal virtual void Process(IList<string> arguments)
				{
					string option = arguments[0];
					string dashLetter = (this.letterOption == null) ? (string)null : "-" + this.letterOption;
					if (option.Equals(dashLetter) || option.Equals("--" + this.wordOption))
					{
						arguments.Remove(0);
						if (this.flag)
						{
							this.values.Add(0, (string)null);
						}
						else
						{
							if (this.array)
							{
								while (arguments.Count > 0 && !arguments[0].StartsWith("-"))
								{
									this.values.Add(arguments.Remove(0));
								}
							}
							else
							{
								this.values.Set(0, arguments.Remove(0));
							}
						}
						if (this.ignored)
						{
							System.Console.Error.WriteLine("WARNING: " + option + " is ignored in the Java version of the test driver.");
						}
					}
				}

				private readonly Arguments _enclosing;
			}

			//    -b URL, --bugurl=URL
			public virtual string GetBugUrl()
			{
				return bugUrl.GetValue();
			}

			//    -c PATH, --classpath=PATH
			//    Does not apply; we will use the VM's classpath
			//    -e TYPE ..., --engine=TYPE ...
			//    Does not apply; was used to select between SpiderMonkey and Rhino
			//    Not in jsDriver.pl
			public virtual int GetOptimizationLevel()
			{
				return optimizationLevel.GetInt();
			}

			//    --strict
			public virtual bool IsStrict()
			{
				return strict.GetSwitch();
			}

			//    -f FILE, --file=FILE
			public virtual FilePath GetOutputFile()
			{
				return outputFile.GetFile();
			}

			//    -h, --help
			public virtual bool Help()
			{
				return help.GetSwitch();
			}

			//    -j PATH, --javapath=PATH
			//    Does not apply; we will use this JVM
			//    -k, --confail
			//    TODO    Currently this is ignored; not clear precisely what it means (perhaps we should not be logging ordinary
			//            pass/fail to the console currently?)
			public virtual bool LogFailuresToConsole()
			{
				return logFailuresToConsole.GetSwitch();
			}

			//    -l FILE,... or --list=FILE,...
			public virtual string[] GetTestList()
			{
				return testList.GetValues();
			}

			//    -L FILE,... or --neglist=FILE,...
			public virtual string[] GetSkipList()
			{
				return skipList.GetValues();
			}

			//    -p PATH, --testpath=PATH
			public virtual FilePath GetTestsPath()
			{
				return testsPath.GetFile();
			}

			//    -s PATH, --shellpath=PATH
			//    Does not apply; we will use the Rhino shell with any classes given on the classpath
			//    -t, --trace
			public virtual bool Trace()
			{
				return trace.GetSwitch();
			}

			//    -u URL, --lxrurl=URL
			public virtual string GetLxrUrl()
			{
				return lxrUrl.GetValue();
			}

			//
			//    New arguments
			//
			//    --timeout
			//    Milliseconds to wait for each test
			public virtual int GetTimeout()
			{
				return timeout.GetInt();
			}

			public virtual JsDriver.Arguments.Console GetConsole()
			{
				return console;
			}

			internal virtual void Process(IList<string> arguments)
			{
				while (arguments.Count > 0)
				{
					string option = arguments[0];
					if (option.StartsWith("--"))
					{
						//    preprocess --name=value options into --name value
						if (option.IndexOf("=") != -1)
						{
							arguments.Set(0, Sharpen.Runtime.Substring(option, option.IndexOf("=")));
							arguments.Add(1, Sharpen.Runtime.Substring(option, option.IndexOf("=") + 1));
						}
					}
					else
					{
						if (option.StartsWith("-"))
						{
							//    could be multiple single-letter options, e.g. -kht, so preprocess them into -k -h -t
							if (option.Length > 2)
							{
								for (int i = 2; i < option.Length; i++)
								{
									arguments.Add(1, "-" + Sharpen.Runtime.Substring(option, i, i + 1));
								}
								arguments.Set(0, Sharpen.Runtime.Substring(option, 0, 2));
							}
						}
					}
					int lengthBefore = arguments.Count;
					for (int i_1 = 0; i_1 < options.Count; i_1++)
					{
						if (arguments.Count > 0)
						{
							options[i_1].Process(arguments);
						}
					}
					if (arguments.Count == lengthBefore)
					{
						System.Console.Error.WriteLine("WARNING: ignoring unrecognized option " + arguments.Remove(0));
					}
				}
			}

			public Arguments()
			{
				bugUrl = new JsDriver.Arguments.Option(this, "b", "bugurl", false, false, "http://bugzilla.mozilla.org/show_bug.cgi?id=");
				optimizationLevel = new JsDriver.Arguments.Option(this, "o", "optimization", false, false, "-1");
				strict = new JsDriver.Arguments.Option(this, null, "strict", false, true, null);
				outputFile = new JsDriver.Arguments.Option(this, "f", "file", false, false, null);
				help = new JsDriver.Arguments.Option(this, "h", "help", false, true, null);
				logFailuresToConsole = new JsDriver.Arguments.Option(this, "k", "confail", false, true, null);
				testList = new JsDriver.Arguments.Option(this, "l", "list", true, false, null);
				skipList = new JsDriver.Arguments.Option(this, "L", "neglist", true, false, null);
				testsPath = new JsDriver.Arguments.Option(this, "p", "testpath", false, false, null);
				trace = new JsDriver.Arguments.Option(this, "t", "trace", false, true, null);
				lxrUrl = new JsDriver.Arguments.Option(this, "u", "lxrurl", false, false, "http://lxr.mozilla.org/mozilla/source/js/tests/");
				timeout = new JsDriver.Arguments.Option(this, null, "timeout", false, false, "60000");
			}
		}

		/// <exception cref="System.Exception"></exception>
		public static void Main(string[] args)
		{
			List<string> arguments = new List<string>();
			Sharpen.Collections.AddAll(arguments, Arrays.AsList(args));
			JsDriver.Arguments clArguments = new JsDriver.Arguments();
			clArguments.Process(arguments);
			Main(clArguments);
		}
	}
}
