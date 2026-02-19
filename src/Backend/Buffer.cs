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

	public Dictionary<char, char> pairs = new() {
		{ '(', ')' },
		{ '[', ']' },
		{ '{', '}' },
		{ '"', '"' },
	};
	public void setSelectedArea(int startLine, int endLine, int startColumn, int endColumn) {
		startSelection.startLine = startLine;
		startSelection.startColumn = startColumn;
		endSelection.endLine = endLine;
		endSelection.endColumn = endColumn;
		isSelecting = true;
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
			insertAtSelectedArea(pasteDataLines);
			stopSelecting();
		}
		else {
			insertLinesAtPos(line, column, pasteDataLines);
		}
	}

	public void removeSelectedArea() {
		var (startLineSelect, endLineSelect, startColumnSelect, endColumnSelect) = getSelectedArea();
		removeArea(startLineSelect, endLineSelect, startColumnSelect, endColumnSelect);
		line = startLineSelect;
		column = startColumnSelect;
		prefColumn = column;
		clampCursor();
	}

	public void removeArea(int startLine, int endLine, int startColumn, int endColumn) {
		int firstLineRemovalCount;
		if (startLine == endLine) {
			firstLineRemovalCount = endColumn - startColumn;
		}
		else {
			firstLineRemovalCount = lines[startLine].Count - startColumn;
		}
		lines[startLine].RemoveRange(startColumn, firstLineRemovalCount);

		if (startLine == endLine) {
			return;
		}

		var toBeMerged = lines[endLine].GetRange(endColumn, lines[endLine].Count - endColumn);
		//beggining from behind because of shifting indeces
		for (int i = endLine; i > startLine; i--) {
			lines.RemoveAt(i);
		}
		lines[startLine].AddRange(toBeMerged);
	}

	public void insertAtSelectedArea(List<List<char>> linesToInsertWithTab) {
		removeSelectedArea();
		insertLinesAtPos(line, column, linesToInsertWithTab);
	}

	public void insertLinesAtPos(int linePos, int columnPos, List<List<char>> linesToInsertWithTab) {
		if (linesToInsertWithTab.Count == 0) return;
		var linesToInsert = convertTabsToSpace(linesToInsertWithTab);
		int originalColumn = columnPos;

		List<char> remainingLine = lines[linePos].Slice(columnPos, lines[linePos].Count - columnPos);
		lines[linePos].RemoveRange(columnPos, lines[linePos].Count - columnPos);

		lines[linePos].InsertRange(columnPos, linesToInsert[0]);
		int firstLineLength = linesToInsert[0].Count;

		int insertCount = linesToInsert.Count - 1;
		int currentLine = linePos;

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

	public (int startLine, int endLine, int startColumn, int endColumn) getSelectedArea() {
		int startLine = Math.Min(startSelection.startLine, endSelection.endLine);
		int endLine = Math.Max(startSelection.startLine, endSelection.endLine);

		int startColumn;
		int endColumn;

		if (startSelection.startLine == endSelection.endLine) {
			startColumn = Math.Min(startSelection.startColumn, endSelection.endColumn);
			endColumn = Math.Max(startSelection.startColumn, endSelection.endColumn);
		}
		else if (startLine == startSelection.startLine) {
			startColumn = startSelection.startColumn;
			endColumn = endSelection.endColumn;
		}
		else {
			startColumn = endSelection.endColumn;
			endColumn = startSelection.startColumn;
		}

		return (startLine, endLine, startColumn, endColumn);
	}

	public async Task copyLines() {
		if (isSelecting) {
			List<List<char>> copiedLines = new List<List<char>>();

			var (startLineCopy, endLineCopy, startColumnCopy, endColumnCopy) = getSelectedArea();

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

	public async Task cutLines() {
		await copyLines();
		removeSelectedArea();
		stopSelecting();
	}


	public int getPrevWhiteSpaces(int linePos) {
		int whiteSpaceCount = 0;
		List<char> lastLine = lines[linePos];
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
		if (isSelecting) {
			var selectedArea = getSelectedArea();
			List<char> newLine = lines[selectedArea.endLine].Slice(selectedArea.endColumn, lines[selectedArea.endLine].Count - selectedArea.endColumn);
			lines[selectedArea.endLine].RemoveRange(selectedArea.endColumn, lines[selectedArea.endLine].Count - selectedArea.endColumn);
			int leadingWhiteSpaces = getPrevWhiteSpaces(selectedArea.startLine);
			removeSelectedArea();
			stopSelecting();
			line = selectedArea.startLine + 1;
			column = leadingWhiteSpaces;
			prefColumn = column;
			lines.Insert(line, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
			lines[line].AddRange(newLine);
		}
		else {
			List<char> newLine = lines[line].Slice(column, lines[line].Count - column);
			lines[line].RemoveRange(column, lines[line].Count - column);
			int leadingWhiteSpaces = getPrevWhiteSpaces(line);
			line++;
			column = leadingWhiteSpaces;
			prefColumn = column;
			lines.Insert(line, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
			lines[line].AddRange(newLine);
		}
	}

	public void newLineAtPos(int linePos, int columnPos) {
		List<char> newLine = lines[linePos].Slice(columnPos, lines[linePos].Count - columnPos);
		lines[linePos].RemoveRange(columnPos, lines[linePos].Count - columnPos);
		int leadingWhiteSpaces = getPrevWhiteSpaces(linePos);
		line = linePos + 1;
		column = leadingWhiteSpaces;
		prefColumn = leadingWhiteSpaces;
		lines.Insert(linePos + 1, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
		lines[linePos + 1].AddRange(newLine);
	}

	public void newLineAtPosRaw(int linePos, int columnPos) {
		List<char> newLine = lines[linePos].Slice(columnPos, lines[linePos].Count - columnPos);
		lines[linePos].RemoveRange(columnPos, lines[linePos].Count - columnPos);
		lines.Insert(linePos + 1, new List<char>(newLine));
		line = linePos + 1;
		column = 0;
		prefColumn = 0;
	}

	public void mergeLinesAtPos(int linePos, int removeCharsFromNextLineStart = 0) {
		if (linePos < 0) {
			return;
		}

		int lineToBeMerged = linePos + 1;
		if (lineToBeMerged >= lines.Count) {
			return;
		}

		int removeCount = Math.Max(0, Math.Min(removeCharsFromNextLineStart, lines[lineToBeMerged].Count));
		int oldLineCount = lines[lineToBeMerged].Count;
		List<char> toAddLine = lines[lineToBeMerged].Slice(removeCount, oldLineCount - removeCount);
		lines.RemoveAt(lineToBeMerged);
		line = linePos;
		column = lines[linePos].Count;
		prefColumn = column;
		lines[linePos].AddRange(toAddLine);
	}

	public void moveUp() {
		if (line == 0) {
			if (column != 0) {
				column = 0;
				prefColumn = column;
			}
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
			column = lines[line].Count;
			prefColumn = column;
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


	public void moveRightWhileSelecting() {
		if (isSelecting) {
			var selectedArea = getSelectedArea();
			line = selectedArea.endLine;
			column = selectedArea.endColumn;
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

	public void moveLeftWhileSelecting() {
		if (isSelecting) {
			var selectedArea = getSelectedArea();
			line = selectedArea.startLine;
			column = selectedArea.startColumn;
			prefColumn = column;
		}
	}

	public bool backspace() {
		if (isSelecting) {
			removeSelectedArea();
			stopSelecting();
			return true;
		}
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
		if (isSelecting) {
			removeSelectedArea();
			int charPos = getSelectedArea().startColumn;
			int linePos = getSelectedArea().startLine;
			stopSelecting();
			lines[linePos].Insert(charPos, c);

			column = charPos + 1;
			prefColumn = column;
		}
		else {
			lines[line].Insert(column, c);

			column++;
			prefColumn = column;
		}
		clampCursor();
	}


	public void insertCharPair(char c) {
		if (isSelecting) {
			var area = getSelectedArea();

			insertCharPairArroundSelection(
				c,
				area.startColumn,
				area.startLine,
				area.endLine,
				area.endColumn
			);
		}
		else {
			insertCharPairAtPos(c, column, line);
		}

		clampCursor();
	}


	public void insertCharPairArroundSelection(char c, int firstCharColumnPos, int firstCharlinePos, int secoundCharlinePos, int secoundCharColumnPos) {
		char secoundC = pairs[c];

		insertCharAtPos(c, firstCharColumnPos, firstCharlinePos);
		int endColumnPos = firstCharlinePos == secoundCharlinePos ? secoundCharColumnPos + 1 : secoundCharColumnPos;
		insertCharAtPos(secoundC, endColumnPos, secoundCharlinePos);
		setSelectedArea(firstCharlinePos, secoundCharlinePos, firstCharColumnPos + 1, endColumnPos);
		line = firstCharlinePos;
		column = firstCharColumnPos + 1;
		prefColumn = column;
	}
	public void insertCharPairAtPos(char c, int columPos, int linePos) {
		char secoundC = pairs[c];

		lines[linePos].Insert(columPos, c);
		lines[linePos].Insert(columPos + 1, secoundC);
		column = columPos + 1;
		prefColumn = column;
		clampCursor();
	}

	public void insertCharAtPos(char c, int columPos, int linePos) {
		lines[linePos].Insert(Math.Min(columPos, lines[linePos].Count), c);
		column = columPos + 1;
		prefColumn = column;
		clampCursor();
	}

	public void removeCharAtPos(int columPos, int linePos) {
		lines[linePos].RemoveAt(Math.Min(columPos, lines[linePos].Count - 1));
		column = columPos;
		prefColumn = column;
		clampCursor();
	}

	public void removeTabAtPos(int columPos, int linePos) {
		lines[linePos].RemoveRange(columPos - 4, 4);
		column = columPos - 4;
		prefColumn = column;
		clampCursor();
	}
	public void insertTabAtPos(int columPos, int linePos, int count) {
		clampCursor();
		lines[linePos].InsertRange(columPos, new string(' ', count));
		column = columPos + count;
		prefColumn = column;
		clampCursor();
	}
	public void insertTab(int count) {
		clampCursor();
		lines[line].InsertRange(column, new string(' ', count));
		column += count;
		prefColumn = column;
		clampCursor();
	}
}
