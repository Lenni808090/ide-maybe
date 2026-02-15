using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using TextCopy;

class Buffer {
	public List<List<char>> lines = new List<List<char>>();
	public (int startLine, int startColumn) startSelection;
	public (int endLine, int endColumn) endSelection;

	public bool isSelecting;
	public int line = 0;
	public int column = 0;
	public int prefColumn = 0;

	public Buffer() {
		lines.Add(new List<char>());
	}

	public void startSelecting() {
		isSelecting = true;
		startSelection = (line, column);
	}

	public void updateSelection() {
		if (isSelecting) {
			endSelection = (line, column);
		}
	}

	public void stopSelecting() {
		isSelecting = false;
		startSelection = (0, 0);
		endSelection = (0, 0);
	}

	public List<List<char>> convertTabsToSpace(List<List<char>> linesWithTab) {
		List<List<char>> linesWithSpaces = new List<List<char>>();
		foreach (List<char> lineWithTab in linesWithTab) {
			linesWithSpaces.Add(new List<char>());
			foreach (char c in lineWithTab) {
				if (c == '\t') {
					linesWithSpaces[linesWithSpaces.Count - 1].AddRange("    ".ToCharArray());
				}
				else {
					linesWithSpaces[linesWithSpaces.Count - 1].Add(c);
				}
			}
		}
		return linesWithSpaces;
	}
	public void pasteData(List<List<char>> pasteDataLines) {
		if (isSelecting) {
			// TODO: Implement paste with selection (delete selection first)
		}
		else {
			insertLinesAtCursor(pasteDataLines);
		}
	}

	public void insertLinesAtCursor(List<List<char>> linesToInsertWithTab) {
		if (linesToInsertWithTab.Count == 0) return;
		var linesToInsert = convertTabsToSpace(linesToInsertWithTab);
		int originalColumn = column;

		List<char> remainingLine = lines[line].Slice(column, lines[line].Count - column);
		lines[line].RemoveRange(column, lines[line].Count - column);

		lines[line].InsertRange(column, linesToInsert[0]);
		int firstLineLength = linesToInsert[0].Count;

		int insertCount = linesToInsert.Count - 1;
		int currentLine = line;

		if (insertCount > 0) {
			currentLine++;
			for (int i = 1; i < linesToInsert.Count; i++) {
				lines.Insert(currentLine, new List<char>(linesToInsert[i]));
				currentLine++;
			}
			currentLine--;
		}

		lines[currentLine].AddRange(remainingLine);

		line = currentLine;
		if (insertCount == 0) {
			column = originalColumn + firstLineLength;
		}
		else {
			column = linesToInsert[linesToInsert.Count - 1].Count;
		}
		prefColumn = column;
	}

	public async Task copyLines() {
		if (isSelecting) {
			List<List<char>> copiedLines = new List<List<char>>();
			int startLineCopy = Math.Min(startSelection.startLine, endSelection.endLine);
			int endLineCopy = Math.Max(startSelection.startLine, endSelection.endLine);

			int startColumnCopy;
			int endColumnCopy;

			if (startSelection.startLine == endSelection.endLine) {
				startColumnCopy = Math.Min(startSelection.startColumn, endSelection.endColumn);
				endColumnCopy = Math.Max(startSelection.startColumn, endSelection.endColumn);
			}
			else if (startSelection.startLine == startLineCopy) {
				startColumnCopy = startSelection.startColumn;
				endColumnCopy = endSelection.endColumn;
			}
			else {
				startColumnCopy = endSelection.endColumn;
				endColumnCopy = startSelection.startColumn;
			}

			for (int i = startLineCopy; i <= endLineCopy; i++) {
				if (i == startLineCopy) {
					List<char> firstCopiedLine = new List<char>();
					if (startLineCopy == endLineCopy) {
						firstCopiedLine = lines[startLineCopy].GetRange(startColumnCopy, endColumnCopy - startColumnCopy);
					}
					else {
						firstCopiedLine = lines[startLineCopy].GetRange(startColumnCopy, lines[startLineCopy].Count - startColumnCopy);
					}
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
			string copiedLinesString = "";

			foreach (List<char> line in copiedLines) {
				string stringLine = new string(line.ToArray());
				if (copiedLinesString.Length == 0) {
					copiedLinesString += stringLine;
				}
				else {
					copiedLinesString += "\n" + stringLine;
				}
			}

			await ClipboardService.SetTextAsync(copiedLinesString);
		}
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
		if (column < 4) return false;

		for (int i = column - 1; i >= column - 4; i--) {
			if (i < 0 || !char.IsWhiteSpace(lines[line][i])) {
				return false;
			}
		}
		return true;
	}


	public void newLine() {
		List<char> newLine = lines[line].Slice(column, lines[line].Count - column);
		lines[line].RemoveRange(column, lines[line].Count - column);
		int leadingWhiteSpaces = getPrevWhiteSpaces();
		line++;
		column = leadingWhiteSpaces;
		prefColumn = leadingWhiteSpaces;
		lines.Insert(line, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
		lines[line].AddRange(newLine);
	}

	public void moveUp() {
		if (line == 0) {
			return;
		}
		line--;
		if (prefColumn <= lines[line].Count) {
			column = prefColumn;
		}
		else if (prefColumn > lines[line].Count) {
			column = lines[line].Count;
		}
	}

	public void moveDown() {
		if (lines.Count == (line + 1)) {
			return;
		}
		line++;
		if (prefColumn <= lines[line].Count) {
			column = prefColumn;
		}
		else if (prefColumn > lines[line].Count) {
			column = lines[line].Count;
		}
	}

	public void moveRight() {
		if (column == lines[line].Count) {
			if (lines.Count == (line + 1)) {
				return;
			}
			line++;
			column = 0;
			prefColumn = column;
		}
		else {
			column++;
			prefColumn = column;
		}
	}

	public void moveLeft() {
		if (column == 0) {
			if (line == 0) {
				return;
			}
			line--;
			column = lines[line].Count;
			prefColumn = column;
		}
		else {
			column--;
			prefColumn = column;
		}
	}

	public bool backspace() {
		if (column > 0) {
			if (isItTab()) {
				lines[line].RemoveRange(column - 4, 4);
				column -= 4;
				prefColumn = column;
			}
			else {
				lines[line].RemoveAt(column - 1);
				column--;
				prefColumn = column;
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
				column = lines[line].Count;
				prefColumn = column;
				lines[line].AddRange(toAddLine);
				return true;
			}

			return false;
		}
	}

	public void clampCursor() {
		if (column < 0) {
			column = 0;
		}
		if (column > lines[line].Count) {
			column = lines[line].Count;
		}
		prefColumn = column;
	}

	public void insertChar(char c) {
		clampCursor();
		lines[line].Insert(column, c);
		column++;
		prefColumn = column;
	}

	public void insertTab(int count) {
		clampCursor();
		lines[line].InsertRange(column, new string(' ', count));
		column += count;
		prefColumn = column;
	}
}
