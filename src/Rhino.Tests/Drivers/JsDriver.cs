/*
 * This code is derived from rhino (http://github.com/mozilla/rhino)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#if JS_DRIVER

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Rhino.Tools.Shell;
using Sharpen;
using Environment = System.Environment;

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
			private readonly DirectoryInfo testDirectory;

			private readonly string[] list;

			private readonly string[] skip;

			/// <exception cref="System.IO.IOException"></exception>
			internal Tests(DirectoryInfo testDirectory, string[] list, string[] skip)
			{
				this.testDirectory = testDirectory;
				this.list = GetTestList(list);
				this.skip = GetTestList(skip);
			}

			/// <exception cref="System.IO.IOException"></exception>
			private static string[] GetTestList(string[] tests)
			{
				var result = new List<string>();
				foreach (string t in tests)
				{
					if (t.StartsWith("@"))
					{
						TestUtils.AddTestsFromFile(t.Substring(1), result);
					}
					else
					{
						result.Add(t);
					}
				}
				return result.ToArray();
			}

			private bool Matches(string path)
			{
				return list.Length == 0 || TestUtils.Matches(list, path);
			}

			private bool Excluded(string path)
			{
				return skip.Length != 0 && TestUtils.Matches(skip, path);
			}

			private void AddFiles(ICollection<Script> rv, string prefix, DirectoryInfo directory)
			{
				FileSystemInfo[] files = directory.GetFileSystemInfos();
				foreach (FileSystemInfo file in files)
				{
					string path = prefix + file.Name;
					var info = file as DirectoryInfo;
					if (info != null  )
					{
						if (!file.Name.Equals("CVS"))
							AddFiles(rv, path + "/", info);
					}
					else
					{
						var name = file.Name;
						if (name.EndsWith(".js") &&
							!name.Equals("shell.js") &&
							!name.Equals("browser.js") &&
							!name.Equals("template.js") &&
							Matches(path) &&
							!Excluded(path) &&
							prefix.Length > 0)
						{
							rv.Add(new Script(path, (FileInfo) file));
						}
					}
				}
			}

			internal class Script
			{
				internal Script(string path, FileInfo file)
				{
					Path = path;
					File = file;
				}

				internal string Path { get; private set; }

				internal FileInfo File { get; private set; }
			}

			internal virtual Script[] GetFiles()
			{
				List<Script> rv = new List<Script>();
				AddFiles(rv, string.Empty, testDirectory);
				return rv.ToArray();
			}
		}

		private class ConsoleStatus : ShellTest.Status
		{
			private FileInfo jsFile;

			private readonly Arguments.Console console;

			private readonly bool trace;

			private bool failed;

			internal ConsoleStatus(Arguments.Console console, bool trace)
			{
				this.console = console;
				this.trace = trace;
			}

			public override void Running(FileInfo jsFile)
			{
				console.Println("Running: " + jsFile.FullName);
				this.jsFile = jsFile;
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
			return false;
		}

		private static XmlElement GetElementById(XmlElement node, string id)
		{
			if (node.GetAttribute("id").Equals(id))
			{
				return node;
			}
			
			XmlNodeList children = node.ChildNodes;
			for (int i = 0; i < children.Count; i++)
			{
				var element = children.Item(i) as XmlElement;
				if (element != null)
				{
					XmlElement rv = GetElementById(element, id);
					if (rv != null)
					{
						return rv;
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
			private readonly string testPath;

			private readonly string bugUrl;

			private readonly string lxrUrl;

			private readonly XmlDocument html;

			private readonly XmlElement failureHtml;

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

			public override void Running(FileInfo file)
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
				output = s;
			}

			private string GetLinesStartingWith(string prefix)
			{
				StringReader r = new StringReader(output);
				string rv = string.Empty;
				try
				{
					string line;
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

			internal bool Failed()
			{
				return failed;
			}

			internal void Finish()
			{
				if (failed)
				{
					GetElementById(failureHtml, "failureDetails.status").InnerText = GetLinesStartingWith("STATUS:");
					string bn = GetLinesStartingWith("BUGNUMBER:");
					XmlElement bnlink = GetElementById(failureHtml, "failureDetails.bug.href");
					if (bn.Length > 0)
					{
						string number = bn.Substring("BUGNUMBER: ".Length);
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
			private readonly XmlElement target;

			private DateTime start;

			internal XmlStatus(string path, XmlElement root)
			{
				target = root.OwnerDocument.CreateElement("test");
				target.SetAttribute("path", path);
				root.AppendChild(target);
			}

			public override void Running(FileInfo file)
			{
				start = new DateTime();
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
				target.SetAttribute("elapsed", elapsed.ToString());
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
			private readonly ShellContextFactory factory;

			private readonly Arguments arguments;

			private readonly FileInfo output;

			private readonly bool trace;

			private XmlDocument html;

			private XmlElement failureHtml;

			private XmlDocument xml;

			private DateTime start;

			private int tests;

			private int failures;

			internal Results(ShellContextFactory factory, Arguments arguments, bool trace)
			{
				this.factory = factory;
				this.arguments = arguments;
				FileInfo output = arguments.GetOutputFile() ??
								  new FileInfo(string.Format("rhino-test-results.{0:yyyy.MM.dd.HH.mm.ss}.html", DateTime.Now));
				this.output = output;
				this.trace = trace;
			}

			private XmlDocument Parse(Stream @in)
			{
				var document = new XmlDocument();
				document.Load(@in);
				return document;
			}

			private XmlDocument GetTemplate()
			{
				return Parse(GetType().GetResourceAsStream("results.html"));
			}

			private void Write(XmlDocument template, bool xml)
			{
				try
				{
					FileInfo output = this.output;
					TransformerFactory factory = TransformerFactory.NewInstance();
					Transformer xform = factory.NewTransformer();
					if (xml)
					{
						xform.SetOutputProperty(OutputKeys.METHOD, "xml");
						xform.SetOutputProperty(OutputKeys.OMIT_XML_DECLARATION, "yes");
						output = new FileInfo(output.FullName + ".xml");
					}
					xform.Transform(new DOMSource(template), new StreamResult(new FileOutputStream(new FilePath(output.FullName))));
				}
				catch (IOException e)
				{
					arguments.GetConsole().Println("Could not write results file to " + output + ": ");
					Console.Error.WriteLine (e);
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
				html = GetTemplate();
				failureHtml = GetElementById(html.DocumentElement, "failureDetails.prototype");
				if (failureHtml == null)
				{
					try
					{
						TransformerFactory.NewInstance().NewTransformer().Transform(new DOMSource(html), new StreamResult(Console.Error));
					}
					catch (Exception t)
					{
						throw new Exception(t);
					}
					throw new Exception("No");
				}
				failureHtml.ParentNode.RemoveChild(failureHtml);
				try
				{
					xml = DocumentBuilderFactory.NewInstance().NewDocumentBuilder().GetDOMImplementation().CreateDocument(null, "results", null);
					xml.DocumentElement.SetAttribute("timestamp", new DateTime().GetTime().ToString());
					xml.DocumentElement.SetAttribute("optimization", arguments.GetOptimizationLevel().ToString());
					xml.DocumentElement.SetAttribute("strict", arguments.IsStrict().ToString());
					xml.DocumentElement.SetAttribute("timeout", arguments.GetTimeout().ToString());
				}
				catch (ParserConfigurationException e)
				{
					throw new Exception(e);
				}
				start = new DateTime();
			}

			internal virtual void Run(Tests.Script script, ShellTest.Parameters parameters)
			{
				string path = script.Path;
				FileInfo test = script.File;
				ConsoleStatus cStatus = new ConsoleStatus(arguments.GetConsole(), trace);
				HtmlStatus hStatus = new HtmlStatus(arguments.GetLxrUrl(), arguments.GetBugUrl(), path, html, (XmlElement)failureHtml.CloneNode(true));
				XmlStatus xStatus = new XmlStatus(path, xml.DocumentElement);
				ShellTest.Status status = ShellTest.Status.Compose(new ShellTest.Status[] { cStatus, hStatus, xStatus });
				ShellTest.Run(factory, test, parameters, status);
				tests++;
				if (hStatus.Failed())
				{
					failures++;
				}
				hStatus.Finish();
			}

			private static void Set(XmlDocument document, string id, string value)
			{
				GetElementById(document.DocumentElement, id).InnerText = value;
			}

			internal virtual void Finish()
			{
				DateTime end = new DateTime();
				long elapsedMs = end.GetTime() - start.GetTime();
				Set(html, "results.testlist", Join(arguments.GetTestList()));
				Set(html, "results.skiplist", Join(arguments.GetSkipList()));
				string pct = string.Format("{0:##0.00}", (double)failures / tests * 100.0);
				Set(html, "results.results", "Tests attempted: " + tests + " Failures: " + failures + " (" + pct + "%)");
				Set(html, "results.platform", "java.home=" + Runtime.GetProperty("java.home") + "\n" + "java.version=" + Runtime.GetProperty("java.version") + "\n" + "os.name=" + Runtime.GetProperty("os.name"));
				Set(html, "results.classpath", Runtime.GetProperty("java.class.path").Replace(Path.PathSeparator, ' '));
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
			private readonly int timeout;

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
		internal virtual void Run(Arguments arguments)
		{
			if (arguments.Help())
			{
				Console.Out.WriteLine("See mozilla/js/tests/README-jsDriver.html; note that some options are not supported.");
				Console.Out.WriteLine("Consult the Java source code at testsrc/org/mozilla/javascript/JsDriver.java for details.");
				Environment.Exit(0);
			}
			ShellContextFactory factory = new ShellContextFactory();
			factory.SetOptimizationLevel(arguments.GetOptimizationLevel());
			factory.SetStrictMode(arguments.IsStrict());
			DirectoryInfo path = arguments.GetTestsPath() ??
								 new DirectoryInfo("../tests");
			if (!path.Exists)
			{
				throw new Exception("JavaScript tests not found at " + path.FullName);
			}
			Tests tests = new Tests(path, arguments.GetTestList(), arguments.GetSkipList());
			Tests.Script[] all = tests.GetFiles();
			arguments.GetConsole().Println("Running " + all.Length + " tests.");
			Results results = new Results(factory, arguments, arguments.Trace());
			results.Start();
			foreach (var t in all)
			{
				results.Run(t, new ShellTestParameters(arguments.GetTimeout()));
			}
			results.Finish();
		}

		/// <exception cref="System.Exception"></exception>
		public static void Main(Arguments arguments)
		{
			JsDriver driver = new JsDriver();
			driver.Run(arguments);
		}

		public class Arguments
		{
			private readonly List<Option> options = new List<Option>();

			private readonly Option bugUrl;

			private readonly Option optimizationLevel;

			private readonly Option strict;

			private readonly Option outputFile;

			private readonly Option help;

			private readonly Option logFailuresToConsole;

			private readonly Option testList;

			private readonly Option skipList;

			private readonly Option testsPath;

			private readonly Option trace;

			private readonly Option lxrUrl;

			private readonly Option timeout;

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

			private readonly Console console = new Console();

			private sealed class Option
			{
				private readonly string letterOption;

				private readonly string wordOption;

				private readonly bool array;

				private readonly bool flag;

				private bool ignored;

				private readonly List<string> values = new List<string>();

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
						values.Add(unspecified);
					}
					this._enclosing.options.Add(this);
				}

				internal Option Ignored()
				{
					ignored = true;
					return this;
				}

				internal int GetInt()
				{
					return Convert.ToInt32(GetValue());
				}

				internal string GetValue()
				{
					return values[0];
				}

				internal bool GetSwitch()
				{
					return values.Count > 0;
				}

				internal FileInfo GetFile()
				{
					if (GetValue() == null)
					{
						return null;
					}
					return new FileInfo(GetValue());
				}

				internal string[] GetValues()
				{
					return values.ToArray();
				}

				internal void Process(IList<string> arguments)
				{
					string option = arguments[0];
					string dashLetter = (letterOption == null) ? (string)null : "-" + letterOption;
					if (option.Equals(dashLetter) || option.Equals("--" + wordOption))
					{
						arguments.Remove(0);
						if (flag)
						{
							values.Insert(0, null);
						}
						else
						{
							if (array)
							{
								while (arguments.Count > 0 && !arguments[0].StartsWith("-"))
								{
									values.Add(arguments.Remove(0));
								}
							}
							else
							{
								values.Set(0, arguments.Remove(0));
							}
						}
						if (ignored)
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
			public virtual FileInfo GetOutputFile()
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
			public virtual DirectoryInfo GetTestsPath()
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

			public virtual Console GetConsole()
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
						var indexOfEq = option.IndexOf("=", StringComparison.Ordinal);
						if (indexOfEq != -1)
						{
							arguments.Set(0, option.Substring(indexOfEq));
							arguments.Insert(1, option.Substring(indexOfEq + 1));
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
									arguments.Insert(1, "-" + option.Substring(i, i + 1 - i));
								}
								arguments.Set(0, option.Substring(0, 2 - 0));
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
				bugUrl = new Option(this, "b", "bugurl", false, false, "http://bugzilla.mozilla.org/show_bug.cgi?id=");
				optimizationLevel = new Option(this, "o", "optimization", false, false, "-1");
				strict = new Option(this, null, "strict", false, true, null);
				outputFile = new Option(this, "f", "file", false, false, null);
				help = new Option(this, "h", "help", false, true, null);
				logFailuresToConsole = new Option(this, "k", "confail", false, true, null);
				testList = new Option(this, "l", "list", true, false, null);
				skipList = new Option(this, "L", "neglist", true, false, null);
				testsPath = new Option(this, "p", "testpath", false, false, null);
				trace = new Option(this, "t", "trace", false, true, null);
				lxrUrl = new Option(this, "u", "lxrurl", false, false, "http://lxr.mozilla.org/mozilla/source/js/tests/");
				timeout = new Option(this, null, "timeout", false, false, "60000");
			}
		}

		/// <exception cref="System.Exception"></exception>
		public static void Main(string[] args)
		{
			List<string> arguments = new List<string>();
			arguments.AddRange(args);
			Arguments clArguments = new Arguments();
			clArguments.Process(arguments);
			Main(clArguments);
		}
	}
}

#endif