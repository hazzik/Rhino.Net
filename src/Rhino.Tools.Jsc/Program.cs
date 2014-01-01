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
using Rhino;
using Rhino.Optimizer;
using Rhino.Tools;
using Rhino.Tools.Jsc;
using Sharpen;

namespace Rhino.Tools.Jsc
{
	/// <author>Norris Boyd</author>
	public class Program
	{
		/// <summary>Main entry point.</summary>
		/// <remarks>
		/// Main entry point.
		/// Process arguments as would a normal Java program.
		/// Then set up the execution environment and begin to
		/// compile scripts.
		/// </remarks>
		public static void Main(string[] args)
		{
#if COMPILATION
			Program main = new Program();
			args = main.ProcessOptions(args);
			if (args == null)
			{
				if (main.printHelp)
				{
					System.Console.Out.WriteLine(ToolErrorReporter.GetMessage("msg.jsc.usage", typeof(Program).FullName));
					System.Environment.Exit(0);
				}
				System.Environment.Exit(1);
			}
			if (!main.reporter.HasReportedError())
			{
				main.ProcessSource(args);
			}
#endif
		}

#if COMPILATION

		public Program()
		{
			reporter = new ToolErrorReporter(true);
			compilerEnv = new CompilerEnvirons();
			compilerEnv.SetErrorReporter(reporter);
			compiler = new ClassCompiler(compilerEnv);
		}

		/// <summary>Parse arguments.</summary>
		/// <remarks>Parse arguments.</remarks>
		public virtual string[] ProcessOptions(string[] args)
		{
			targetPackage = string.Empty;
			// default to no package
			compilerEnv.SetGenerateDebugInfo(false);
			// default to no symbols
			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];
				if (!arg.StartsWith("-"))
				{
					int tail = args.Length - i;
					if (targetName != null && tail > 1)
					{
						AddError("msg.multiple.js.to.file", targetName);
						return null;
					}
					string[] result = new string[tail];
					for (int j = 0; j != tail; ++j)
					{
						result[j] = args[i + j];
					}
					return result;
				}
				if (arg.Equals("-help") || arg.Equals("-h") || arg.Equals("--help"))
				{
					printHelp = true;
					return null;
				}
				try
				{
					if (arg.Equals("-version") && ++i < args.Length)
					{
						int version = System.Convert.ToInt32(args[i]);
						compilerEnv.SetLanguageVersion(version);
						continue;
					}
					if ((arg.Equals("-opt") || arg.Equals("-O")) && ++i < args.Length)
					{
						int optLevel = System.Convert.ToInt32(args[i]);
						compilerEnv.SetOptimizationLevel(optLevel);
						continue;
					}
				}
				catch (FormatException)
				{
					BadUsage(args[i]);
					return null;
				}
				if (arg.Equals("-nosource"))
				{
					compilerEnv.SetGeneratingSource(false);
					continue;
				}
				if (arg.Equals("-debug") || arg.Equals("-g"))
				{
					compilerEnv.SetGenerateDebugInfo(true);
					continue;
				}
				if (arg.Equals("-main-method-class") && ++i < args.Length)
				{
					compiler.SetMainMethodClass(args[i]);
					continue;
				}
				if (arg.Equals("-encoding") && ++i < args.Length)
				{
					characterEncoding = args[i];
					continue;
				}
				if (arg.Equals("-o") && ++i < args.Length)
				{
					string name = args[i];
					int end = name.Length;
					if (end == 0 || !CharEx.IsJavaIdentifierStart(name[0]))
					{
						AddError("msg.invalid.classfile.name", name);
						continue;
					}
					for (int j = 1; j < end; j++)
					{
						char c = name[j];
						if (!CharEx.IsJavaIdentifierPart(c))
						{
							if (c == '.')
							{
								// check if it is the dot in .class
								if (j == end - 6 && name.EndsWith(".class"))
								{
									name = name.Substring(0, j);
									break;
								}
							}
							AddError("msg.invalid.classfile.name", name);
							break;
						}
					}
					targetName = name;
					continue;
				}
				if (arg.Equals("-observe-instruction-count"))
				{
					compilerEnv.SetGenerateObserverCount(true);
				}
				if (arg.Equals("-package") && ++i < args.Length)
				{
					string pkg = args[i];
					int end = pkg.Length;
					for (int j = 0; j != end; ++j)
					{
						char c = pkg[j];
						if (CharEx.IsJavaIdentifierStart(c))
						{
							for (++j; j != end; ++j)
							{
								c = pkg[j];
								if (!CharEx.IsJavaIdentifierPart(c))
								{
									break;
								}
							}
							if (j == end)
							{
								break;
							}
							if (c == '.' && j != end - 1)
							{
								continue;
							}
						}
						AddError("msg.package.name", targetPackage);
						return null;
					}
					targetPackage = pkg;
					continue;
				}
				if (arg.Equals("-extends") && ++i < args.Length)
				{
					string targetExtends = args[i];
					Type superClass;
					try
					{
						superClass = Sharpen.Runtime.GetType(targetExtends);
					}
					catch (TypeLoadException e)
					{
						throw new Exception(e.ToString());
					}
					// TODO: better error
					compiler.SetTargetExtends(superClass);
					continue;
				}
				if (arg.Equals("-implements") && ++i < args.Length)
				{
					// TODO: allow for multiple comma-separated interfaces.
					string targetImplements = args[i];
					List<Type> list = new List<Type>();
					foreach (var className in targetImplements.Split(","))
					{
						try
						{
							list.Add (Sharpen.Runtime.GetType(className));
						}
						catch (TypeLoadException e)
						{
							throw new Exception(e.ToString());
						}
					}
					// TODO: better error
					Type[] implementsClasses = list.ToArray();
					compiler.SetTargetImplements(implementsClasses);
					continue;
				}
				if (arg.Equals("-d") && ++i < args.Length)
				{
					destinationDir = args[i];
					continue;
				}
				BadUsage(arg);
				return null;
			}
			// no file name
			P(ToolErrorReporter.GetMessage("msg.no.file"));
			return null;
		}

		/// <summary>Print a usage message.</summary>
		/// <remarks>Print a usage message.</remarks>
		private static void BadUsage(string s)
		{
			System.Console.Error.WriteLine(ToolErrorReporter.GetMessage("msg.jsc.bad.usage", typeof(Program).FullName, s));
		}

		/// <summary>Compile JavaScript source.</summary>
		/// <remarks>Compile JavaScript source.</remarks>
		public virtual void ProcessSource(string[] filenames)
		{
			for (int i = 0; i != filenames.Length; ++i)
			{
				string filename = filenames[i];
				if (!filename.EndsWith(".js"))
				{
					AddError("msg.extension.not.js", filename);
					return;
				}
				FileInfo f = new FileInfo(filename);
				string source = ReadSource(f);
				if (source == null)
				{
					return;
				}
				string mainClassName = targetName;
				if (mainClassName == null)
				{
					string name = f.Name;
					string nojs = name.Substring(0, name.Length - 3);
					mainClassName = GetClassName(nojs);
				}
				if (targetPackage.Length != 0)
				{
					mainClassName = targetPackage + "." + mainClassName;
				}
				Tuple<string, byte[]>[] compiled = compiler.CompileToClassFiles(source, filename, 1, mainClassName);
				if (compiled == null || compiled.Length == 0)
				{
					return;
				}
				DirectoryInfo targetTopDir = destinationDir != null ? new DirectoryInfo(destinationDir) : f.Directory;
				foreach (var tuple in compiled)
				{
					string className = tuple.Item1;
					byte[] bytes = tuple.Item2;
					FilePath outfile = GetOutputFile(targetTopDir, className);
					try
					{
						FileOutputStream os = new FileOutputStream(outfile);
						try
						{
							os.Write(bytes);
						}
						finally
						{
							os.Close();
						}
					}
					catch (IOException ioe)
					{
						AddFormatedError(ioe.ToString());
					}
				}
			}
		}

		private string ReadSource(FileSystemInfo f)
		{
			if (!f.Exists)
			{
				AddError("msg.jsfile.not.found", f.FullName);
				return null;
			}
			try
			{
				return SourceReader.ReadFileOrUrlAsString(f.FullName, characterEncoding);
			}
			catch (FileNotFoundException)
			{
				AddError("msg.couldnt.open", f.FullName);
			}
			catch (IOException ioe)
			{
				AddFormatedError(ioe.ToString());
			}
			return null;
		}

		private static FilePath GetOutputFile(DirectoryInfo parentDir, string className)
		{
			string path = className.Replace('.', FilePath.separatorChar);
			path = String.Concat(path, ".class");
			FilePath f = new FilePath(parentDir + "/" + path);
			string dirPath = f.GetParent();
			if (dirPath != null)
			{
				FilePath dir = new FilePath(dirPath);
				if (!dir.Exists())
				{
					dir.Mkdirs();
				}
			}
			return f;
		}

		/// <summary>Verify that class file names are legal Java identifiers.</summary>
		/// <remarks>
		/// Verify that class file names are legal Java identifiers.  Substitute
		/// illegal characters with underscores, and prepend the name with an
		/// underscore if the file name does not begin with a JavaLetter.
		/// </remarks>
		internal virtual string GetClassName(string name)
		{
			char[] s = new char[name.Length + 1];
			char c;
			int j = 0;
			if (!CharEx.IsJavaIdentifierStart(name[0]))
			{
				s[j++] = '_';
			}
			for (int i = 0; i < name.Length; i++, j++)
			{
				c = name[i];
				if (CharEx.IsJavaIdentifierPart(c))
				{
					s[j] = c;
				}
				else
				{
					s[j] = '_';
				}
			}
			return (new string(s)).Trim();
		}

		private static void P(string s)
		{
			System.Console.Out.WriteLine(s);
		}

		private void AddError(string messageId, string arg)
		{
			string msg;
			if (arg == null)
			{
				msg = ToolErrorReporter.GetMessage(messageId);
			}
			else
			{
				msg = ToolErrorReporter.GetMessage(messageId, arg);
			}
			AddFormatedError(msg);
		}

		private void AddFormatedError(string message)
		{
			reporter.Error(message, null, -1, null, -1);
		}

		private bool printHelp;

		private ToolErrorReporter reporter;

		private CompilerEnvirons compilerEnv;

		private ClassCompiler compiler;

		private string targetName;

		private string targetPackage;

		private string destinationDir;

		private string characterEncoding;
#endif
	}
}
