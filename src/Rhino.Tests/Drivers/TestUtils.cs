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
using Rhino.Tests;
using Rhino.Tests.Drivers;
using Sharpen;

namespace Rhino.Tests.Drivers
{
	public class TestUtils
	{
		private static ContextFactory.GlobalSetter globalSetter;

		public static void GrabContextFactoryGlobalSetter()
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

		public static FilePath[] RecursiveListFiles(FilePath dir, FileFilter filter)
		{
			if (!dir.IsDirectory())
			{
				throw new ArgumentException(dir + " is not a directory");
			}
			IList<FilePath> fileList = new AList<FilePath>();
			RecursiveListFilesHelper(dir, filter, fileList);
			return Sharpen.Collections.ToArray(fileList, new FilePath[fileList.Count]);
		}

		public static void RecursiveListFilesHelper(FilePath dir, FileFilter filter, IList<FilePath> fileList)
		{
			foreach (FilePath f in dir.ListFiles())
			{
				if (f.IsDirectory())
				{
					RecursiveListFilesHelper(f, filter, fileList);
				}
				else
				{
					if (filter.Accept(f))
					{
						fileList.AddItem(f);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void AddTestsFromFile(string filename, IList<string> list)
		{
			AddTestsFromStream(new FileInputStream(new FilePath(filename)), list);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static void AddTestsFromStream(InputStream @in, IList<string> list)
		{
			Properties props = new Properties();
			props.Load(@in);
			foreach (object obj in props.Keys)
			{
				list.AddItem(obj.ToString());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static string[] LoadTestsFromResource(string resource, string[] inherited)
		{
			IList<string> list = inherited == null ? new AList<string>() : new AList<string>(Arrays.AsList(inherited));
			InputStream @in = typeof(StandardTests).GetResourceAsStream(resource);
			if (@in != null)
			{
				AddTestsFromStream(@in, list);
			}
			return Sharpen.Collections.ToArray(list, new string[0]);
		}

		public static bool Matches(string[] patterns, string path)
		{
			for (int i = 0; i < patterns.Length; i++)
			{
				if (path.StartsWith(patterns[i]))
				{
					return true;
				}
			}
			return false;
		}
	}
}
