using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

class Buffer {
	public List<List<char>> lines = new List<List<char>>();
	public (int startLine, int startColumn) startSelection;
	public (int endLine, int endColumn) endSelection;

	public List<List<char>> copiedLines = new List<List<char>>();
	public bool isSelecting;
	public int line = 0;
	public int coloumn = 0;
	public int prefColoum = 0;

	public Buffer() {
		lines.Add(new List<char>());
	}

	public void startSelecting() {
		isSelecting = true;
		startSelection = (line, coloumn);
	}

	public void updateSelection() {
		if (isSelecting) {
			endSelection = (line, coloumn);
		}
	}

	public void stopSelecting() {
		isSelecting = false;
	}

	public void copyLines() {
		if (isSelecting) {
			copiedLines.Clear();

			int startLineCopy = Math.Min(startSelection.startLine, endSelection.endLine);
			int endLineCopy = Math.Max(startSelection.startLine, endSelection.endLine);
			int startColumnCopy;
			int endColumnCopy;
			if (startSelection.startLine == endSelection.endLine) {
				startColumnCopy = startSelection.startColumn;
				endColumnCopy = endSelection.endColumn;
			}
			else if (startLineCopy == startSelection.startLine) {
				startColumnCopy = Math.Min(startSelection.startColumn, endSelection.endColumn);
				endColumnCopy = Math.Max(startSelection.startColumn, endSelection.endColumn);
			}
			else {
				startColumnCopy = endSelection.endColumn;
				endColumnCopy = startSelection.startColumn;
			}

			for (int i = startLineCopy; i <= endLineCopy; i++) {
				if (i == startLineCopy) {
					List<char> firstCopiedLine = lines[startLineCopy].GetRange(startColumnCopy, lines[startLineCopy].Count - startColumnCopy);
					copiedLines.Add(firstCopiedLine);
				}
				else if (i == endLineCopy) {
					List<char> lastLineCopied = lines[endLineCopy].GetRange(0, endColumnCopy);
					copiedLines.Add(lastLineCopied);
				}
				else {
					copiedLines.Add(new List<char>(lines[i]));
				}
			}
		}
	}


	public void pasteCopiedLines() {
		if (copiedLines.Count == 0) return;
		lines[line].InsertRange(coloumn, copiedLines[0]);


		if (copiedLines.Count > 1) {
			var linesToInsert = copiedLines.Skip(1)
										   .Select(l => new List<char>(l))
										   .ToList();
			lines.InsertRange(line + 1, linesToInsert);
			line += copiedLines.Count - 1;
		}

		coloumn = copiedLines.Last().Count;
		prefColoum = coloumn;
	}


	public int getPrevWhiteSpaces() {
		int whiteSpaceCount = 0;
		List<char> lastLine = lines[line];
		foreach (char c in lastLine) {
			if (char.IsWhiteSpace(c)) {
				whiteSpaceCount++;
			}
			else {
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
			}
			else {
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
		}
		else if (prefColoum > lines[line].Count) {
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
		}
		else if (prefColoum > lines[line].Count) {
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
		}
		else {
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
		}
		else {
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
			}
			else {
				lines[line].RemoveAt(Math.Max(coloumn - 1, 0));
				prefColoum--;
				coloumn = prefColoum;
			}

			return false;
		}
		else {
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

