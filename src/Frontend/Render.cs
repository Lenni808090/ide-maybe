using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks.Dataflow;

class Render {
	Buffer buffer;

	StatusBar statusBar;
	SimpleHighlighter simpleHighlighter;
	Searcher searcher;
	Converter converter;
	public int topLine = 0;
	public int bottomLine = 0;

	int currentDistFromEdge;

	private int ContentHeight => Console.WindowHeight - 1;
	private int StatusBarLine => Console.WindowHeight - 1;

	public Render(Buffer buffer, Searcher searcher, StatusBar statusBar) {
		this.buffer = buffer;
		this.searcher = searcher;
		this.statusBar = statusBar;
		simpleHighlighter = new SimpleHighlighter();
		converter = new();
		currentDistFromEdge = 4;
	}




	public void resetView() {
		int h = ContentHeight;

		int margin = Math.Min(5, h / 3);

		if (buffer.line < topLine + margin) {
			topLine = buffer.line - margin;
		}
		else if (buffer.line >= topLine + h - margin) {
			topLine = buffer.line - (h - margin - 1);
		}

		if (topLine < 0) {
			topLine = 0;
		}

		int maxTop = Math.Max(0, buffer.lines.Count - h);
		if (topLine > maxTop) {
			topLine = maxTop;
		}

		bottomLine = Math.Min(topLine + h, buffer.lines.Count);
	}


	public void setCursor(int lineInd) {
		Console.SetCursorPosition(getCursorXPos(lineInd), getScreenLine(lineInd));
	}

	public int getScreenLine(int lineInd) {
		return lineInd - topLine;
	}

	public int getSpacesNeeded(int lineInd) {
		if (lineInd.ToString().Length >= currentDistFromEdge) {
			currentDistFromEdge = lineInd.ToString().Length + 2;
		}
		return currentDistFromEdge - lineInd.ToString().Length;
	}

	public void printLineNumber(int lineInd) {
		Console.SetCursorPosition(0, getScreenLine(lineInd));
		Console.ForegroundColor = ConsoleColor.DarkMagenta;
		Console.Write(lineInd + new string(' ', getSpacesNeeded(lineInd)));
		Console.ForegroundColor = ConsoleColor.White;
	}

	public int getCursorXPos(int lineInd) {
		return lineInd.ToString().Length + buffer.column + getSpacesNeeded(lineInd);
	}

	public (int startLine, int endLine, int startColumn, int endColumn) getSelectedArea() {
		int startLine = Math.Min(buffer.startSelection.startLine, buffer.endSelection.endLine);
		int endLine = Math.Max(buffer.startSelection.startLine, buffer.endSelection.endLine);

		int startColumn;
		int endColumn;

		if (buffer.startSelection.startLine == buffer.endSelection.endLine) {
			startColumn = Math.Min(buffer.startSelection.startColumn, buffer.endSelection.endColumn);
			endColumn = Math.Max(buffer.startSelection.startColumn, buffer.endSelection.endColumn);
		}
		else if (startLine == buffer.startSelection.startLine) {
			startColumn = buffer.startSelection.startColumn;
			endColumn = buffer.endSelection.endColumn;
		}
		else {
			startColumn = buffer.endSelection.endColumn;
			endColumn = buffer.startSelection.startColumn;
		}

		return (startLine, endLine, startColumn, endColumn);
	}


	public void printLine(int lineInd, bool completeRedraw) {
		Console.CursorVisible = false;
		printLineNumber(lineInd);
		List<char> line = buffer.lines[lineInd];
		List<Span> spans = new();
		spans.AddRange(converter.convertTokensToSpans(simpleHighlighter.HighlightLine(line)));
		if (searcher.isSearching) {
			spans.AddRange(converter.convertFindlingsToSpans(searcher.searchLine(lineInd)));
		}
		if (buffer.isSelecting) {
			var sel = getSelectedArea();

			int startLine = sel.startLine;
			int endLine = sel.endLine;
			int startCol = sel.startColumn;
			int endCol = sel.endColumn;

			int selectStart = 0;
			int selectLength = 0;

			if (lineInd < startLine || lineInd > endLine) {
			}
			else if (startLine == endLine) {
				selectStart = startCol;
				selectLength = endCol - startCol;
			}
			else if (lineInd == startLine) {
				selectStart = startCol;
				selectLength = line.Count - startCol;
			}
			else if (lineInd == endLine) {
				selectStart = 0;
				selectLength = endCol;
			}
			else {
				selectStart = 0;
				selectLength = line.Count;
			}
			spans.Add(converter.convertSelectionToSpan(selectStart, selectLength));

		}

		for (int i = 0; i < line.Count; i++) {
			Span? active = null;

			foreach (var s in spans) {
				if (i >= s.Start && i < s.Start + s.Lenght) {
					if (active == null || s.Priority > active.Value.Priority) {
						active = s;
					}
				}
			}

			if (active.HasValue) {
				Console.ForegroundColor = active.Value.ForegroundColor;
				Console.BackgroundColor = active.Value.BackgroundColor ?? ConsoleColor.Black;
			}
			else {
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.BackgroundColor = ConsoleColor.Black;
			}

			Console.Write(line[i]);
		}
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("\x1b[K");
		setCursor(lineInd);
		if (!completeRedraw) {
			Console.CursorVisible = true;
		}
	}

	public void printSection(int startLineInd) {
		for (int i = startLineInd; i < bottomLine; i++) {
			printLine(i, true);
		}
		if (bottomLine < ContentHeight) {
			for (int i = bottomLine; i < ContentHeight; i++) {
				Console.SetCursorPosition(0, getScreenLine(i));
				Console.Write("\x1b[K");
			}
		}
		setCursor(buffer.line);
		Console.CursorVisible = true;
	}

	public void printScreen() {
		printSection(topLine);
	}

	private static string fitStatusSegment(string text, int maxLength) {
		if (maxLength <= 0) return "";
		if (text.Length <= maxLength) return text;
		return text.Substring(0, maxLength);
	}

	private void drawStatusBarSegments(
		string leftText,
		string middleText,
		string rightText,
		int statusBarLine
	) {
		int width = Console.WindowWidth;
		if (width <= 0) return;

		Console.SetCursorPosition(0, statusBarLine);
		Console.BackgroundColor = ConsoleColor.DarkBlue;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("\x1b[K");

		if (!string.IsNullOrEmpty(leftText)) {
			Console.SetCursorPosition(0, statusBarLine);
			Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(leftText);
		}

		if (!string.IsNullOrEmpty(middleText)) {
			int middleStart = Math.Max(0, (width - rightText.Length - middleText.Length));
			Console.SetCursorPosition(middleStart, statusBarLine);
			Console.BackgroundColor = ConsoleColor.Blue;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(middleText);
		}

		if (!string.IsNullOrEmpty(rightText)) {
			int rightStart = width - rightText.Length;
			Console.SetCursorPosition(rightStart, statusBarLine);
			Console.BackgroundColor = ConsoleColor.DarkCyan;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(rightText);
		}

		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
	}

	public void drawStatusBar() {
		var (filePath, fileData, column, line, statusBarMode, searchedChars) = statusBar.getData();
		int width = Console.WindowWidth;
		if (width <= 0) return;

		Console.SetCursorPosition(0, StatusBarLine);
		Console.BackgroundColor = ConsoleColor.DarkBlue;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("\x1b[K");

		string leftRaw = statusBarMode == StatusBarMode.Search && !string.IsNullOrEmpty(searchedChars)
			? $" {filePath} SEARCH: {searchedChars} "
			: $" {filePath} ";

		string middleRaw = $" {fileData.Extension}  {fileData.Encoding}  {fileData.FileSize} ";

		string rightRaw = $" Ln {line + 1}/ Col {column + 1} ";

		string leftText = fitStatusSegment(leftRaw, width);
		string middleText = fitStatusSegment(middleRaw, width);
		string rightText = fitStatusSegment(rightRaw, width);

		drawStatusBarSegments(leftText, middleText, rightText, StatusBarLine);

		setCursor(buffer.line);
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
		Console.CursorVisible = true;
	}

}
