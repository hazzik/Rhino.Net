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
using System.Linq;
using System.Reflection;
using Sharpen;

namespace Rhino.Drivers
{
	public static class TestUtils
	{
		private static ContextFactory.GlobalSetter globalSetter;

		private static void GrabContextFactoryGlobalSetter()
		{
			if (globalSetter == null)
			{
				globalSetter = ContextFactory.GetGlobalSetter();
			}
		}

		public static void SetGlobalContextFactory(ContextFactory factory)
		{
			GrabContextFactoryGlobalSetter();
			globalSetter.SetContextFactoryGlobal(factory);
		}

		public static FileInfo[] RecursiveListFiles(DirectoryInfo directory, Predicate<FileInfo> filter)
		{
			return directory.EnumerateFiles("*", SearchOption.AllDirectories)
				.Where(file => filter(file))
				.ToArray();
		}

		public static IEnumerable<FileInfo> RecursiveListFiles(DirectoryInfo directory, string filter)
		{
			return directory.GetFiles(filter, SearchOption.AllDirectories);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void AddTestsFromFile(string filename, IList<string> list)
		{
			using (var reader = File.OpenText(filename))
			{
				AddTestsFromStream(reader, list);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static void AddTestsFromStream(Stream @in, ICollection<string> list)
		{
			using (var reader = new StreamReader(@in))
			{
				AddTestsFromStream(reader, list);
			}
		}
	   
		private static void AddTestsFromStream(TextReader reader, ICollection<string> list)
		{
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				list.Add(line);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static string[] LoadTestsFromResource(string resource)
		{
			var list = new List<string>();
			Stream @in = typeof (TestUtils).Assembly.GetManifestResourceStream("Rhino" + resource.Replace("/", "."));//.GetResourceAsStream(resource);
			if (@in != null)
			{
				AddTestsFromStream(@in, list);
			}
			return list.ToArray();
		}

		public static bool Matches(string[] patterns, string path)
		{
			return patterns.Any(path.StartsWith);
		}
	}
}
