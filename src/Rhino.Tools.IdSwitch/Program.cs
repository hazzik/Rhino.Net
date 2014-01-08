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
using System.Text;
using Rhino;
using Rhino.Tools;
using Rhino.Tools.Idswitch;
using Sharpen;

namespace Rhino.Tools.Idswitch
{
	public class Program
	{
		private const string SWITCH_TAG_STR = "string_id_map";

		private const string GENERATED_TAG_STR = "generated";

		private const string STRING_TAG_STR = "string";

		private const int NORMAL_LINE = 0;

		private const int SWITCH_TAG = 1;

		private const int GENERATED_TAG = 2;

		private const int STRING_TAG = 3;

		private readonly IList<IdValuePair> all_pairs = new List<IdValuePair>();

		private ToolErrorReporter R;

		private CodePrinter P;

		private FileBody body;

		private string source_file;

		private int tag_definition_end;

		private int tag_value_start;

		private int tag_value_end;

		private static bool Is_value_type(int id)
		{
			if (id == STRING_TAG)
			{
				return true;
			}
			return false;
		}

		private static string Tag_name(int id)
		{
			switch (id)
			{
				case SWITCH_TAG:
				{
					return SWITCH_TAG_STR;
				}

				case -SWITCH_TAG:
				{
					return "/" + SWITCH_TAG_STR;
				}

				case GENERATED_TAG:
				{
					return GENERATED_TAG_STR;
				}

				case -GENERATED_TAG:
				{
					return "/" + GENERATED_TAG_STR;
				}
			}
			return string.Empty;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Process_file(string file_path)
		{
			source_file = file_path;
			body = new FileBody();
			using (Stream @is = file_path == "-" ? Console.OpenStandardInput() : File.OpenRead(file_path))
			using (TextReader r = new StreamReader(@is, Encoding.ASCII))
			{
				body.ReadData(r);
			}
			Process_file();
			if (body.WasModified())
			{
				using (Stream os = file_path == "-" ? Console.OpenStandardOutput() : File.OpenWrite(file_path))
				using (TextWriter w = new StreamWriter(os))
				{
					body.WriteData(w);
				}
			}
		}

		private void Process_file()
		{
			int cur_state = 0;
			char[] buffer = body.GetBuffer();
			int generated_begin = -1;
			int generated_end = -1;
			int time_stamp_begin = -1;
			int time_stamp_end = -1;
			body.StartLineLoop();
			while (body.NextLine())
			{
				int begin = body.GetLineBegin();
				int end = body.GetLineEnd();
				int tag_id = Extract_line_tag_id(buffer, begin, end);
				bool bad_tag = false;
				switch (cur_state)
				{
					case NORMAL_LINE:
					{
						if (tag_id == SWITCH_TAG)
						{
							cur_state = SWITCH_TAG;
							all_pairs.Clear();
							generated_begin = -1;
						}
						else
						{
							if (tag_id == -SWITCH_TAG)
							{
								bad_tag = true;
							}
						}
						break;
					}

					case SWITCH_TAG:
					{
						if (tag_id == 0)
						{
							Look_for_id_definitions(buffer, begin, end, false);
						}
						else
						{
							if (tag_id == STRING_TAG)
							{
								Look_for_id_definitions(buffer, begin, end, true);
							}
							else
							{
								if (tag_id == GENERATED_TAG)
								{
									if (generated_begin >= 0)
									{
										bad_tag = true;
									}
									else
									{
										cur_state = GENERATED_TAG;
										time_stamp_begin = tag_definition_end;
										time_stamp_end = end;
									}
								}
								else
								{
									if (tag_id == -SWITCH_TAG)
									{
										cur_state = 0;
										if (generated_begin >= 0 && !all_pairs.IsEmpty())
										{
											Generate_java_code();
											string code = P.ToString();
											bool different = body.SetReplacement(generated_begin, generated_end, code);
											if (different)
											{
												string stamp = Get_time_stamp();
												body.SetReplacement(time_stamp_begin, time_stamp_end, stamp);
											}
										}
										break;
									}
									else
									{
										bad_tag = true;
									}
								}
							}
						}
						break;
					}

					case GENERATED_TAG:
					{
						if (tag_id == 0)
						{
							if (generated_begin < 0)
							{
								generated_begin = begin;
							}
						}
						else
						{
							if (tag_id == -GENERATED_TAG)
							{
								if (generated_begin < 0)
								{
									generated_begin = begin;
								}
								cur_state = SWITCH_TAG;
								generated_end = begin;
							}
							else
							{
								bad_tag = true;
							}
						}
						break;
					}
				}
				if (bad_tag)
				{
					string text = ToolErrorReporter.GetMessage("msg.idswitch.bad_tag_order", Tag_name(tag_id));
					throw R.RuntimeError(text, source_file, body.GetLineNumber(), null, 0);
				}
			}
			if (cur_state != 0)
			{
				string text = ToolErrorReporter.GetMessage("msg.idswitch.file_end_in_switch", Tag_name(cur_state));
				throw R.RuntimeError(text, source_file, body.GetLineNumber(), null, 0);
			}
		}

		private string Get_time_stamp()
		{
			SimpleDateFormat f = new SimpleDateFormat(" 'Last update:' yyyy-MM-dd HH:mm:ss z");
			return f.Format(new DateTime());
		}

		private void Generate_java_code()
		{
			P.Clear();
			IdValuePair[] pairs = all_pairs.ToArray();
			SwitchGenerator g = new SwitchGenerator();
			g.char_tail_test_threshold = 2;
			g.SetReporter(R);
			g.SetCodePrinter(P);
			g.GenerateSwitch(pairs, "0");
		}

		private int Extract_line_tag_id(char[] array, int cursor, int end)
		{
			int id = 0;
			cursor = Skip_white_space(array, cursor, end);
			int after_leading_white_space = cursor;
			cursor = Look_for_slash_slash(array, cursor, end);
			if (cursor != end)
			{
				bool at_line_start = (after_leading_white_space + 2 == cursor);
				cursor = Skip_white_space(array, cursor, end);
				if (cursor != end && array[cursor] == '#')
				{
					++cursor;
					bool end_tag = false;
					if (cursor != end && array[cursor] == '/')
					{
						++cursor;
						end_tag = true;
					}
					int tag_start = cursor;
					for (; cursor != end; ++cursor)
					{
						int c = array[cursor];
						if (c == '#' || c == '=' || Is_white_space(c))
						{
							break;
						}
					}
					if (cursor != end)
					{
						int tag_end = cursor;
						cursor = Skip_white_space(array, cursor, end);
						if (cursor != end)
						{
							int c = array[cursor];
							if (c == '=' || c == '#')
							{
								id = Get_tag_id(array, tag_start, tag_end, at_line_start);
								if (id != 0)
								{
									string bad = null;
									if (c == '#')
									{
										if (end_tag)
										{
											id = -id;
											if (Is_value_type(id))
											{
												bad = "msg.idswitch.no_end_usage";
											}
										}
										tag_definition_end = cursor + 1;
									}
									else
									{
										if (end_tag)
										{
											bad = "msg.idswitch.no_end_with_value";
										}
										else
										{
											if (!Is_value_type(id))
											{
												bad = "msg.idswitch.no_value_allowed";
											}
										}
										id = Extract_tag_value(array, cursor + 1, end, id);
									}
									if (bad != null)
									{
										string s = ToolErrorReporter.GetMessage(bad, Tag_name(id));
										throw R.RuntimeError(s, source_file, body.GetLineNumber(), null, 0);
									}
								}
							}
						}
					}
				}
			}
			return id;
		}

		// Return position after first of // or end if not found
		private int Look_for_slash_slash(char[] array, int cursor, int end)
		{
			while (cursor + 2 <= end)
			{
				int c = array[cursor++];
				if (c == '/')
				{
					c = array[cursor++];
					if (c == '/')
					{
						return cursor;
					}
				}
			}
			return end;
		}

		private int Extract_tag_value(char[] array, int cursor, int end, int id)
		{
			// cursor points after #[^#=]+=
			// ALERT: implement support for quoted strings
			bool found = false;
			cursor = Skip_white_space(array, cursor, end);
			if (cursor != end)
			{
				int value_start = cursor;
				int value_end = cursor;
				while (cursor != end)
				{
					int c = array[cursor];
					if (Is_white_space(c))
					{
						int after_space = Skip_white_space(array, cursor + 1, end);
						if (after_space != end && array[after_space] == '#')
						{
							value_end = cursor;
							cursor = after_space;
							break;
						}
						cursor = after_space + 1;
					}
					else
					{
						if (c == '#')
						{
							value_end = cursor;
							break;
						}
						else
						{
							++cursor;
						}
					}
				}
				if (cursor != end)
				{
					// array[cursor] is '#' here
					found = true;
					tag_value_start = value_start;
					tag_value_end = value_end;
					tag_definition_end = cursor + 1;
				}
			}
			return (found) ? id : 0;
		}

		private int Get_tag_id(char[] array, int begin, int end, bool at_line_start)
		{
			if (at_line_start)
			{
				if (Equals(SWITCH_TAG_STR, array, begin, end))
				{
					return SWITCH_TAG;
				}
				if (Equals(GENERATED_TAG_STR, array, begin, end))
				{
					return GENERATED_TAG;
				}
			}
			if (Equals(STRING_TAG_STR, array, begin, end))
			{
				return STRING_TAG;
			}
			return 0;
		}

		private void Look_for_id_definitions(char[] array, int begin, int end, bool use_tag_value_as_string)
		{
			// Look for the pattern
			// '^[ \t]+Id_([a-zA-Z0-9_]+)[ \t]*=.*$'
			// where \1 gives field or method name
			int cursor = begin;
			// Skip tab and spaces at the beginning
			cursor = Skip_white_space(array, cursor, end);
			int id_start = cursor;
			int name_start = Skip_matched_prefix("Id_", array, cursor, end);
			if (name_start >= 0)
			{
				// Found Id_ prefix
				cursor = name_start;
				cursor = Skip_name_char(array, cursor, end);
				int name_end = cursor;
				if (name_start != name_end)
				{
					cursor = Skip_white_space(array, cursor, end);
					if (cursor != end)
					{
						if (array[cursor] == '=')
						{
							int id_end = name_end;
							if (use_tag_value_as_string)
							{
								name_start = tag_value_start;
								name_end = tag_value_end;
							}
							// Got the match
							Add_id(array, id_start, id_end, name_start, name_end);
						}
					}
				}
			}
		}

		private void Add_id(char[] array, int id_start, int id_end, int name_start, int name_end)
		{
			string name = new string(array, name_start, name_end - name_start);
			string value = new string(array, id_start, id_end - id_start);
			IdValuePair pair = new IdValuePair(name, value);
			pair.SetLineNumber(body.GetLineNumber());
			all_pairs.Add(pair);
		}

		private static bool Is_white_space(int c)
		{
			return c == ' ' || c == '\t';
		}

		private static int Skip_white_space(char[] array, int begin, int end)
		{
			int cursor = begin;
			for (; cursor != end; ++cursor)
			{
				int c = array[cursor];
				if (!Is_white_space(c))
				{
					break;
				}
			}
			return cursor;
		}

		private static int Skip_matched_prefix(string prefix, char[] array, int begin, int end)
		{
			int cursor = -1;
			int prefix_length = prefix.Length;
			if (prefix_length <= end - begin)
			{
				cursor = begin;
				for (int i = 0; i != prefix_length; ++i, ++cursor)
				{
					if (prefix[i] != array[cursor])
					{
						cursor = -1;
						break;
					}
				}
			}
			return cursor;
		}

		private static bool Equals(string str, char[] array, int begin, int end)
		{
			if (str.Length == end - begin)
			{
				for (int i = begin, j = 0; i != end; ++i, ++j)
				{
					if (array[i] != str[j])
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}

		private static int Skip_name_char(char[] array, int begin, int end)
		{
			int cursor = begin;
			for (; cursor != end; ++cursor)
			{
				int c = array[cursor];
				if (!('a' <= c && c <= 'z') && !('A' <= c && c <= 'Z'))
				{
					if (!('0' <= c && c <= '9'))
					{
						if (c != '_')
						{
							break;
						}
					}
				}
			}
			return cursor;
		}

		public static void Main(string[] args)
		{
			Program self = new Program();
			int status = self.Exec(args);
			System.Environment.Exit(status);
		}

		private int Exec(string[] args)
		{
			R = new ToolErrorReporter(true, System.Console.Error);
			int arg_count = Process_options(args);
			if (arg_count == 0)
			{
				Option_error(ToolErrorReporter.GetMessage("msg.idswitch.no_file_argument"));
				return -1;
			}
			if (arg_count > 1)
			{
				Option_error(ToolErrorReporter.GetMessage("msg.idswitch.too_many_arguments"));
				return -1;
			}
			P = new CodePrinter();
			P.SetIndentStep(4);
			P.SetIndentTabSize(0);
			try
			{
				Process_file(args[0]);
			}
			catch (IOException ex)
			{
				Print_error(ToolErrorReporter.GetMessage("msg.idswitch.io_error", ex.ToString()));
				return -1;
			}
			catch (EvaluatorException)
			{
				return -1;
			}
			return 0;
		}

		private int Process_options(string[] args)
		{
			int status = 1;
			bool show_usage = false;
			bool show_version = false;
			int N = args.Length;
			for (int i = 0; i != N; ++i)
			{
				string arg = args[i];
				int arg_length = arg.Length;
				if (arg_length >= 2)
				{
					if (arg[0] == '-')
					{
						if (arg[1] == '-')
						{
							if (arg_length == 2)
							{
								args[i] = null;
								break;
							}
							if (arg.Equals("--help"))
							{
								show_usage = true;
							}
							else
							{
								if (arg.Equals("--version"))
								{
									show_version = true;
								}
								else
								{
									Option_error(ToolErrorReporter.GetMessage("msg.idswitch.bad_option", arg));
									status = -1;
									goto L_break;
								}
							}
						}
						else
						{
							for (int j = 1; j != arg_length; ++j)
							{
								char c = arg[j];
								switch (c)
								{
									case 'h':
									{
										show_usage = true;
										break;
									}

									default:
									{
										Option_error(ToolErrorReporter.GetMessage("msg.idswitch.bad_option_char", c.ToString()));
										status = -1;
										goto L_break;
									}
								}
							}
						}
						args[i] = null;
					}
				}
L_continue: ;
			}
L_break: ;
			if (status == 1)
			{
				if (show_usage)
				{
					Show_usage();
					status = 0;
				}
				if (show_version)
				{
					Show_version();
					status = 0;
				}
			}
			if (status != 1)
			{
				System.Environment.Exit(status);
			}
			return Remove_nulls(args);
		}

		private void Show_usage()
		{
			System.Console.Out.WriteLine(ToolErrorReporter.GetMessage("msg.idswitch.usage"));
			System.Console.Out.WriteLine();
		}

		private void Show_version()
		{
			System.Console.Out.WriteLine(ToolErrorReporter.GetMessage("msg.idswitch.version"));
		}

		private void Option_error(string str)
		{
			Print_error(ToolErrorReporter.GetMessage("msg.idswitch.bad_invocation", str));
		}

		private void Print_error(string text)
		{
			System.Console.Error.WriteLine(text);
		}

		private int Remove_nulls(string[] array)
		{
			int N = array.Length;
			int cursor = 0;
			for (; cursor != N; ++cursor)
			{
				if (array[cursor] == null)
				{
					break;
				}
			}
			int destination = cursor;
			if (cursor != N)
			{
				++cursor;
				for (; cursor != N; ++cursor)
				{
					string elem = array[cursor];
					if (elem != null)
					{
						array[destination] = elem;
						++destination;
					}
				}
			}
			return destination;
		}
	}
}
