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
using System.Reflection;
using System.Reflection.Emit;
using Rhino.Ast;
using Rhino.Optimizer;
using Sharpen;

namespace Rhino.Tools.Jsc
{
	/// <author>Norris Boyd</author>
	public sealed class Program
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
					Console.Out.WriteLine(ToolErrorReporter.GetMessage("msg.jsc.usage", typeof(Program).FullName));
					Environment.Exit(0);
				}
				Environment.Exit(1);
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
			compilerEnv.ErrorReporter = reporter;
			mainMethodClass = Codegen.DEFAULT_MAIN_METHOD_CLASS.FullName;
			targetImplements = new Type[0];
		}

		/// <summary>Parse arguments.</summary>
		/// <remarks>Parse arguments.</remarks>
		public string[] ProcessOptions(string[] args)
		{
			targetPackage = string.Empty;
			// default to no package
			compilerEnv.GenerateDebugInfo = false;
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
				if (arg == "-help" || arg == "-h" || arg == "--help")
				{
					printHelp = true;
					return null;
				}
				try
				{
					if (arg == "-version" && ++i < args.Length)
					{
						var version = (LanguageVersion) Convert.ToInt32(args[i]);
						compilerEnv.LanguageVersion = version;
						continue;
					}
					if ((arg == "-opt" || arg == "-O") && ++i < args.Length)
					{
						int optLevel = Convert.ToInt32(args[i]);
						compilerEnv.OptimizationLevel = optLevel;
						continue;
					}
				}
				catch (FormatException)
				{
					BadUsage(args[i]);
					return null;
				}
				if (arg == "-nosource")
				{
					compilerEnv.GeneratingSource = false;
					continue;
				}
				if (arg == "-debug" || arg == "-g")
				{
					compilerEnv.GenerateDebugInfo = true;
					continue;
				}
				if (arg == "-main-method-class" && ++i < args.Length)
				{
					mainMethodClass = args[i];
					continue;
				}
				if (arg == "-encoding" && ++i < args.Length)
				{
					characterEncoding = args[i];
					continue;
				}
				if (arg == "-o" && ++i < args.Length)
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
				if (arg == "-observe-instruction-count")
				{
					compilerEnv.GenerateObserverCount = true;
				}
				if (arg == "-package" && ++i < args.Length)
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
				if (arg == "-extends" && ++i < args.Length)
				{
					string targetExtends = args[i];
					Type superClass;
					try
					{
						superClass = Runtime.GetType(targetExtends);
					}
					catch (TypeLoadException e)
					{
						// TODO: better error
						throw new Exception(e.ToString());
					}
					this.targetExtends = superClass;
					continue;
				}
				if (arg == "-implements" && ++i < args.Length)
				{
					// TODO: allow for multiple comma-separated interfaces.
					string targetImplements = args[i];
					List<Type> list = new List<Type>();
					foreach (var className in targetImplements.Split(','))
					{
						try
						{
							list.Add (Runtime.GetType(className));
						}
						catch (TypeLoadException e)
						{
							throw new Exception(e.ToString());
						}
					}
					// TODO: better error
					this.targetImplements = list.ToArray();
					continue;
				}
				if (arg == "-d" && ++i < args.Length)
				{
					destinationDir = args[i];
					continue;
				}
				BadUsage(arg);
				return null;
			}
			// no file name
			Console.Out.WriteLine(ToolErrorReporter.GetMessage("msg.no.file"));
			return null;
		}

		/// <summary>Print a usage message.</summary>
		/// <remarks>Print a usage message.</remarks>
		private static void BadUsage(string s)
		{
			Console.Error.WriteLine(ToolErrorReporter.GetMessage("msg.jsc.bad.usage", typeof(Program).FullName, s));
		}

		/// <summary>Compile JavaScript source.</summary>
		public void ProcessSource(IEnumerable<string> filenames)
		{
			Codegen codegen = new Codegen();
			codegen.SetMainMethodClass(mainMethodClass);
			foreach (string filename in filenames)
			{
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
					mainClassName = FileNameToClassName(nojs);
				}
				if (targetPackage.Length != 0)
				{
					mainClassName = targetPackage + "." + mainClassName;
				}
				Tuple<string, Type>[] compiled = CompileToClassFiles(codegen, source, filename, mainClassName, compilerEnv);
				if (compiled == null || compiled.Length == 0)
				{
					return;
				}
				DirectoryInfo targetTopDir = destinationDir != null ? new DirectoryInfo(destinationDir) : f.Directory;
/*
				foreach (var tuple in compiled)
				{
					string className = tuple.Item1;
					Type bytes = tuple.Item2;
					FilePath outfile = GetOutputFile(targetTopDir, className);
					try
					{
						FileOutputStream os = new FileOutputStream(outfile);
						try
						{
							//os.Write(bytes);
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
*/
			}
			try
			{
				codegen.Save();
			}
			catch (Exception e)
			{
				AddFormatedError(e.ToString());
			}
		}

		private Tuple<string, Type>[] CompileToClassFiles(Codegen codegen, string source, string filename, string mainClassName, CompilerEnvirons compilerEnvirons)
		{
			Parser p = new Parser(compilerEnvirons);
			AstRoot ast = p.Parse(source, filename, 1);
			IRFactory irf = new IRFactory(compilerEnvirons);
			ScriptNode tree = irf.TransformTree(ast);
			// release reference to original parse tree & parser
			Type superClass = targetExtends;
			Type[] interfaces = targetImplements;
			bool isPrimary = (interfaces == null && superClass == null);
			string scriptClassName = isPrimary
				? mainClassName
				: mainClassName + "1";
			Type scriptClassBytes = codegen.CompileToClassFile(compilerEnvirons, scriptClassName, tree, tree.GetEncodedSource(), false);
			if (isPrimary)
			{
				return new[] { Tuple.Create(scriptClassName, scriptClassBytes) };
			}
			int functionCount = tree.GetFunctionCount();
			var functionNames = new Dictionary<string, int>(functionCount);
			for (int i = 0; i < functionCount; i++)
			{
				FunctionNode ofn = tree.GetFunctionNode(i);
				string name = ofn.GetName();
				if (!string.IsNullOrEmpty(name))
				{
					functionNames[name] = ofn.GetParamCount();
				}
			}
			if (superClass == null)
			{
				superClass = ScriptRuntime.ObjectClass;
			}
			Type mainClassBytes = JavaAdapter.CreateAdapterCode(functionNames, mainClassName, superClass, interfaces, scriptClassBytes, codegen.ModuleBuilder);
			return new[] { Tuple.Create(mainClassName, mainClassBytes), Tuple.Create(scriptClassName, scriptClassBytes) };
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
			string path = className.Replace('.', Path.DirectorySeparatorChar);
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
		internal static string FileNameToClassName(string name)
		{
			char[] s = new char[name.Length];
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

		private readonly ToolErrorReporter reporter;

		private readonly CompilerEnvirons compilerEnv;

		private string targetName;

		private string targetPackage;

		private string destinationDir;

		private string characterEncoding;
		private string mainMethodClass;
		private Type targetExtends;
		private Type[] targetImplements;
#endif
	}
}
