using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

class Buffer {
	public List<List<char>> lines = new List<List<char>>();
	public int line = 0;
	public int coloumn = 0;
	public int prefColoum = 0;

	public Buffer() {
		lines.Add(new List<char>());
	}


	public int getPrevWhiteSpaces() {
		int whiteSpaceCount = 0;
		List<char> lastLine = lines[line];
		foreach (char c in lastLine) {
			if (char.IsWhiteSpace(c)) {
				whiteSpaceCount++;
			} else {
				break;
			}
		}
		return whiteSpaceCount;
	}

	public bool isItTab() {
		int toDeleteSpaces = 0;

		for (int i = coloumn - 1; i >= 0; i--) {
			if (char.IsWhiteSpace(lines[line][i])) {
				toDeleteSpaces++;
				if (toDeleteSpaces == 4) {
					return true;
				}
			} else {
				return false;
			}
		}

		return false;

	}


	public void newLine() {
		List<char> newLine = lines[line].Slice(coloumn, lines[line].Count - coloumn);
		lines[line].RemoveRange(coloumn, lines[line].Count - coloumn);
		int leadingWhiteSpaces = getPrevWhiteSpaces();
		line++;
		coloumn = leadingWhiteSpaces;
		prefColoum = leadingWhiteSpaces;
		lines.Insert(line, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
		lines[line].AddRange(newLine);
	}

	public void moveUp() {
		if (line == 0) {
			return;
		}
		line--;
		if (prefColoum <= lines[line].Count) {
			coloumn = prefColoum;
		} else if (prefColoum > lines[line].Count) {
			coloumn = lines[line].Count;
		}
	}

	public void moveDown() {
		if (lines.Count == (line + 1)) {
			return;
		}
		line++;
		if (prefColoum <= lines[line].Count) {
			coloumn = prefColoum;
		} else if (prefColoum > lines[line].Count) {
			coloumn = lines[line].Count;
		}
	}

	public void moveRight() {
		if (coloumn == lines[line].Count) {
			if (lines.Count == (line + 1)) {
				return;
			}
			line++;
			coloumn = 0;
			prefColoum = coloumn;
		} else {
			coloumn++;
			prefColoum = coloumn;
		}
	}

	public void moveLeft() {
		if (coloumn == 0) {
			if (line == 0) {
				return;
			}
			line--;
			coloumn = lines[line].Count;
			prefColoum = coloumn;
		} else {
			coloumn--;
			prefColoum = coloumn;
		}
	}

	public bool backspace() {
		if (coloumn > 0) {
			if (isItTab()) {
				lines[line].RemoveAt(Math.Max(coloumn - 4, 0));
				prefColoum -= 4;
				coloumn = prefColoum;
			} else {
				lines[line].RemoveAt(Math.Max(coloumn - 1, 0));
				prefColoum--;
				coloumn = prefColoum;
			}
			return false;
		} else {
			int lineBefore = line - 1;

			if (lineBefore < 0) {
				return false;
			}

			if (lineBefore >= 0 && lineBefore < lines.Count) {
				int oldLineCount = lines[line].Count;
				List<char> toAddLine = lines[line].Slice(0, oldLineCount);
				lines.RemoveAt(line);
				line--;
				prefColoum = lines[line].Count;
				coloumn = prefColoum;
				foreach (char c in toAddLine) {
					lines[line].Add(c);
				}
				return true;
			}
			return false;
		}
	}

	public void clampCursor() {
		if (coloumn < 0) {
			coloumn = 0;
		}
		if (coloumn > lines[line].Count) {
			coloumn = lines[line].Count;
		}
		prefColoum = coloumn;
	}

	public void insertChar(char c) {
		clampCursor();
		lines[line].Insert(coloumn, c);
		prefColoum++;
		coloumn = prefColoum;
	}

	public void insertTab(int count) {
		clampCursor();
		lines[line].InsertRange(coloumn, new string(' ', count));
		prefColoum += count;
		coloumn = prefColoum;
	}

}

