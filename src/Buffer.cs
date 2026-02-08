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

	public void newLine() {
		List<char> newLine = lines[line].Slice(coloumn, lines[line].Count - coloumn);
		lines[line].RemoveRange(coloumn, lines[line].Count - coloumn);
		line++;
		coloumn = 0;
		prefColoum = 0;
		lines.Insert(line, newLine);
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
			prefColoum = 0;
			coloumn = prefColoum;
		} else {
			prefColoum++;
			coloumn = prefColoum;
		}
	}

	public void moveLeft() {
		if (coloumn == 0) {
			if (line == 0) {
				return;
			}
			line--;
			prefColoum = lines[line].Count;
			coloumn = prefColoum;
		} else {
			prefColoum--;
			coloumn = prefColoum;
		}
	}

	public bool backspace() {
		if (coloumn > 0) {
			lines[line].RemoveAt(coloumn - 1);
			prefColoum--;
			coloumn = prefColoum;
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

	public void insertChar(char c) {
		lines[line].Insert(coloumn, c);
		prefColoum++;
		coloumn = prefColoum;
	}

}

