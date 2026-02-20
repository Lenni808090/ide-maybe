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
	public void SetSelectedArea(int startLine, int endLine, int startColumn, int endColumn) {
		startSelection.startLine = startLine;
		startSelection.startColumn = startColumn;
		endSelection.endLine = endLine;
		endSelection.endColumn = endColumn;
		isSelecting = true;
	}
	public void StartSelecting() {
		isSelecting = true;
		startSelection = (line, column);
	}

	public void UpdateSelection() {
		if (isSelecting) {
			endSelection = (line, column);
		}
	}

	public void StopSelecting() {
		isSelecting = false;
		startSelection = (0, 0);
		endSelection = (0, 0);
	}

	public List<List<char>> ConvertTabsToSpace(List<List<char>> linesWithTab) {
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
	public void PasteData(List<List<char>> pasteDataLines) {
		if (isSelecting) {
			InsertAtSelectedArea(pasteDataLines);
			StopSelecting();
		}
		else {
			InsertLinesAtPos(line, column, pasteDataLines);
		}
	}

	public void RemoveSelectedArea() {
		var (startLineSelect, endLineSelect, startColumnSelect, endColumnSelect) = GetSelectedArea();
		RemoveArea(startLineSelect, endLineSelect, startColumnSelect, endColumnSelect);
		line = startLineSelect;
		column = startColumnSelect;
		prefColumn = column;
		ClampCursor();
	}

	public void RemoveArea(int startLine, int endLine, int startColumn, int endColumn) {
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

	public void InsertAtSelectedArea(List<List<char>> linesToInsertWithTab) {
		RemoveSelectedArea();
		InsertLinesAtPos(line, column, linesToInsertWithTab);
	}


	public void InsertCharsAtPos(int linePos, int columnPos, List<char> charsToInsert) {
		if (charsToInsert.Count == 0) return;
		lines[linePos].InsertRange(columnPos, charsToInsert);
	}

	public void InsertLinesAtPos(int linePos, int columnPos, List<List<char>> linesToInsertWithTab) {
		if (linesToInsertWithTab.Count == 0) return;
		var linesToInsert = ConvertTabsToSpace(linesToInsertWithTab);
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

	public (int startLine, int endLine, int startColumn, int endColumn) GetSelectedArea() {
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

	public List<List<char>> GetAreaData(int startLine, int endLine, int startColumn, int endColumn) {
		List<List<char>> areaData = new List<List<char>>();

		for (int i = startLine; i <= endLine; i++) {
			if (i == startLine) {
				List<char> firstLineData = new List<char>();
				if (startLine == endLine) {
					firstLineData = lines[startLine].GetRange(startColumn, endColumn - startColumn);
				}
				else {
					firstLineData = lines[startLine].GetRange(startColumn, lines[startLine].Count - startColumn);
				}
				areaData.Add(firstLineData);
			}
			else if (i == endLine) {
				List<char> lastLineData = lines[endLine].GetRange(0, endColumn);
				areaData.Add(lastLineData);
			}
			else {
				areaData.Add(new List<char>(lines[i]));
			}
		}

		return areaData;
	}

	public async Task CopyLines() {
		if (isSelecting) {
			var (startLineCopy, endLineCopy, startColumnCopy, endColumnCopy) = GetSelectedArea();
			List<List<char>> copiedLines = GetAreaData(startLineCopy, endLineCopy, startColumnCopy, endColumnCopy);

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

	public async Task CutLines() {
		await CopyLines();
		RemoveSelectedArea();
		StopSelecting();
	}


	public int GetPrevWhiteSpaces(int linePos) {
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



	public bool IsItTab() {
		if (column < 4) return false;

		for (int i = column - 1; i >= column - 4; i--) {
			if (i < 0 || !char.IsWhiteSpace(lines[line][i])) {
				return false;
			}
		}
		return true;
	}


	public void NewLine() {
		if (isSelecting) {
			var selectedArea = GetSelectedArea();
			List<char> newLine = lines[selectedArea.endLine].Slice(selectedArea.endColumn, lines[selectedArea.endLine].Count - selectedArea.endColumn);
			lines[selectedArea.endLine].RemoveRange(selectedArea.endColumn, lines[selectedArea.endLine].Count - selectedArea.endColumn);
			int leadingWhiteSpaces = GetPrevWhiteSpaces(selectedArea.startLine);
			RemoveSelectedArea();
			StopSelecting();
			line = selectedArea.startLine + 1;
			column = leadingWhiteSpaces;
			prefColumn = column;
			lines.Insert(line, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
			lines[line].AddRange(newLine);
		}
		else {
			List<char> newLine = lines[line].Slice(column, lines[line].Count - column);
			lines[line].RemoveRange(column, lines[line].Count - column);
			int leadingWhiteSpaces = GetPrevWhiteSpaces(line);
			line++;
			column = leadingWhiteSpaces;
			prefColumn = column;
			lines.Insert(line, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
			lines[line].AddRange(newLine);
		}
	}

	public void NewLineAtPos(int linePos, int columnPos) {
		List<char> newLine = lines[linePos].Slice(columnPos, lines[linePos].Count - columnPos);
		lines[linePos].RemoveRange(columnPos, lines[linePos].Count - columnPos);
		int leadingWhiteSpaces = GetPrevWhiteSpaces(linePos);
		line = linePos + 1;
		column = leadingWhiteSpaces;
		prefColumn = leadingWhiteSpaces;
		lines.Insert(linePos + 1, [.. new string(' ', leadingWhiteSpaces).ToCharArray()]);
		lines[linePos + 1].AddRange(newLine);
	}

	public void NewLineAtPosRaw(int linePos, int columnPos) {
		List<char> newLine = lines[linePos].Slice(columnPos, lines[linePos].Count - columnPos);
		lines[linePos].RemoveRange(columnPos, lines[linePos].Count - columnPos);
		lines.Insert(linePos + 1, new List<char>(newLine));
		line = linePos + 1;
		column = 0;
		prefColumn = 0;
	}

	public void MergeLinesAtPos(int linePos, int removeCharsFromNextLineStart = 0) {
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

	public void MoveUp() {
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

	public void MoveDown() {
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

	public void MoveRight() {
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


	public void MoveRightWhileSelecting() {
		if (isSelecting) {
			var selectedArea = GetSelectedArea();
			line = selectedArea.endLine;
			column = selectedArea.endColumn;
			prefColumn = column;
		}
	}

	public void MoveLeft() {
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

	public void MoveLeftWhileSelecting() {
		if (isSelecting) {
			var selectedArea = GetSelectedArea();
			line = selectedArea.startLine;
			column = selectedArea.startColumn;
			prefColumn = column;
		}
	}

	public bool Backspace() {
		if (isSelecting) {
			RemoveSelectedArea();
			StopSelecting();
			return true;
		}
		if (column > 0) {
			if (IsItTab()) {
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

	public void ClampCursor() {
		if (column < 0) {
			column = 0;
		}
		if (column > lines[line].Count) {
			column = lines[line].Count;
		}
		prefColumn = column;
	}

	public void InsertChar(char c) {
		if (isSelecting) {
			RemoveSelectedArea();
			int charPos = GetSelectedArea().startColumn;
			int linePos = GetSelectedArea().startLine;
			StopSelecting();
			lines[linePos].Insert(charPos, c);

			column = charPos + 1;
			prefColumn = column;
		}
		else {
			lines[line].Insert(column, c);

			column++;
			prefColumn = column;
		}
		ClampCursor();
	}


	public void InsertCharPair(char c) {
		if (isSelecting) {
			var area = GetSelectedArea();

			InsertCharPairAroundSelection(
				c,
				area.startColumn,
				area.startLine,
				area.endLine,
				area.endColumn
			);
		}
		else {
			InsertCharPairAtPos(c, column, line);
		}

		ClampCursor();
	}


	public void InsertCharPairAroundSelection(char c, int firstCharColumnPos, int firstCharlinePos, int secoundCharlinePos, int secoundCharColumnPos) {
		char secoundC = pairs[c];

		InsertCharAtPos(c, firstCharColumnPos, firstCharlinePos);
		int endColumnPos = firstCharlinePos == secoundCharlinePos ? secoundCharColumnPos + 1 : secoundCharColumnPos;
		InsertCharAtPos(secoundC, endColumnPos, secoundCharlinePos);
		SetSelectedArea(firstCharlinePos, secoundCharlinePos, firstCharColumnPos + 1, endColumnPos);
		line = firstCharlinePos;
		column = firstCharColumnPos + 1;
		prefColumn = column;
	}
	public void InsertCharPairAtPos(char c, int columPos, int linePos) {
		char secoundC = pairs[c];

		lines[linePos].Insert(columPos, c);
		lines[linePos].Insert(columPos + 1, secoundC);
		column = columPos + 1;
		prefColumn = column;
		ClampCursor();
	}

	public void InsertCharAtPos(char c, int columPos, int linePos) {
		lines[linePos].Insert(Math.Min(columPos, lines[linePos].Count), c);
		column = columPos + 1;
		prefColumn = column;
		ClampCursor();
	}

	public void RemoveCharAtPos(int columPos, int linePos) {
		lines[linePos].RemoveAt(Math.Min(columPos, lines[linePos].Count - 1));
		column = columPos;
		prefColumn = column;
		ClampCursor();
	}

	public void RemoveTabAtPos(int columPos, int linePos) {
		lines[linePos].RemoveRange(columPos - 4, 4);
		column = columPos - 4;
		prefColumn = column;
		ClampCursor();
	}
	public void InsertTabAtPos(int columPos, int linePos, int count) {
		ClampCursor();
		lines[linePos].InsertRange(columPos, new string(' ', count));
		column = columPos + count;
		prefColumn = column;
		ClampCursor();
	}
	public void InsertTab(int count) {
		ClampCursor();
		lines[line].InsertRange(column, new string(' ', count));
		column += count;
		prefColumn = column;
		ClampCursor();
	}

	public void LoadNewBuffer(List<List<char>> newBuffer) {
		var cleanedBuffer = ConvertTabsToSpace(newBuffer);
		lines = new(cleanedBuffer);
		line = 0;
		column = 0;
	}



}

