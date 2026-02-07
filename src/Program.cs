using System;
using System.Collections.Generic;
using System.ComponentModel;

class Program {
	static void Main() {
		Console.Title = "My Editor";
		Console.TreatControlCAsInput = true;
		Console.CursorVisible = true;

		HelperFunctions helper = new HelperFunctions();


		List<List<char>> lines = new List<List<char>>();
		int line = 0;
		int coloumn = 0;
		int prefColoum = 0;
		lines.Add(new List<char>());
		Console.Clear();

		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);


			if (keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control)) {
				Console.Clear();
				Console.CursorVisible = true;
				Console.WriteLine("till next time");
				break;
			}


			if (keyInfo.Key == ConsoleKey.Enter) {
				List<char> newLine = lines[line].Slice(coloumn, lines[line].Count - coloumn);
				for (int i = 0; i < (lines[line].Count - coloumn); i++) {
					Console.Write(" ");
				}
				lines[line].RemoveRange(coloumn, lines[line].Count - coloumn);
				line++;
				coloumn = 0;
				prefColoum = 0;
				Console.SetCursorPosition(coloumn, line);
				lines.Insert(line, newLine);

				foreach (char c in lines[line]) {
					Console.Write(c);
				}
				Console.SetCursorPosition(coloumn, line);
			} else if (keyInfo.Key == ConsoleKey.Backspace) {

				if (coloumn > 0) {
					lines[line].RemoveAt(coloumn - 1);
					Console.SetCursorPosition(coloumn - 1, line);
					Console.Write(" ");
					Console.SetCursorPosition(coloumn - 1, line);
					coloumn--;
					prefColoum--;

				} else if (coloumn == 0) {
					int lineBefore = line - 1;

					if (lineBefore < 0) {
						continue;
					}

					if (lineBefore >= 0 && lineBefore < lines.Count) {
						int oldLineCount = lines[line].Count;
						List<char> toAddLine = lines[line].Slice(0, oldLineCount);
						lines.RemoveAt(line);
						line--;
						coloumn = lines[line].Count;
						prefColoum = lines[line].Count;
						Console.SetCursorPosition(coloumn, line);
						foreach (char c in toAddLine) {
							lines[line].Add(c);
							Console.Write(c);
						}
						Console.SetCursorPosition(coloumn, line);
					}

				}

			} else if (keyInfo.Key == ConsoleKey.LeftArrow) {

				if (coloumn == 0) {
					if (line == 0) {
						continue;
					}
					line--;
					coloumn = lines[line].Count;
					prefColoum = lines[line].Count;
					Console.SetCursorPosition(coloumn, line);
				} else {
					coloumn--;
					prefColoum--;
					Console.SetCursorPosition(coloumn, line);
				}

			} else if (keyInfo.Key == ConsoleKey.RightArrow) {
				if (coloumn == lines[line].Count) {
					if (lines.Count == (line + 1)) {
						continue;
					}
					line++;
					coloumn = 0;
					prefColoum = 0;
					Console.SetCursorPosition(coloumn, line);
				} else {
					coloumn++;
					prefColoum++;
					Console.SetCursorPosition(coloumn, line);
				}
			} else if (keyInfo.Key == ConsoleKey.DownArrow) {

				if (lines.Count == (line + 1)) {
					continue;
				}
				line++;
				if (prefColoum <= lines[line].Count) {
					Console.SetCursorPosition(prefColoum, line);
					coloumn = prefColoum;
				} else if (prefColoum > lines[line].Count) {
					coloumn = lines[line].Count;
					Console.SetCursorPosition(coloumn, line);
				}

			} else if (keyInfo.Key == ConsoleKey.UpArrow) {

				if (line == 0) {
					continue;
				}
				line--;
				if (prefColoum <= lines[line].Count) {
					Console.SetCursorPosition(prefColoum, line);
					coloumn = prefColoum;
				} else if (prefColoum > lines[line].Count) {
					coloumn = lines[line].Count;
					Console.SetCursorPosition(coloumn, line);
				}

			} else if (!char.IsControl(keyInfo.KeyChar)) {
				lines[line].Insert(coloumn, keyInfo.KeyChar);
				Console.Write(keyInfo.KeyChar);
				coloumn++;
				prefColoum++;
			}


		}
	}

	// old Lines
	//123456
	//123

	//update lines  1          0
	//123456	    2          1
	//12            3          2


	//lines.Count < (line + 1))
}           //      3      <     2 +1 = 3
			//	false also continue also kein out of bounds
