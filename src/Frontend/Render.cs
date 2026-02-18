using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks.Dataflow;

class Render {
	Buffer buffer;
	SimpleHighlighter simpleHighlighter;
	Searcher searcher;
	Converter converter;
	public int topLine = 0;
	public int bottomLine = 0;

	int currentDistFromEdge;

	private int ContentHeight => Console.WindowHeight - 1;
	private int StatusBarLine => Console.WindowHeight - 1;

	public Render(Buffer buffer, Searcher searcher) {
		this.buffer = buffer;
		this.searcher = searcher;
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

	private static string FitStatusSegment(string text, int maxLength) {
		if (maxLength <= 0) return "";
		if (text.Length <= maxLength) return text;
		return text.Substring(0, maxLength);
	}

	public void drawStatusBar((string filePath, FileData fileData, int column, int line) statusBar) {
		Console.CursorVisible = false;
		int width = Console.WindowWidth;
		if (width <= 0) {
			Console.CursorVisible = true;
			return;
		}

		Console.SetCursorPosition(0, StatusBarLine);
		Console.BackgroundColor = ConsoleColor.DarkBlue;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("\x1b[K");

		string leftRaw = $" {statusBar.filePath} ";
		string middleRaw = $" {statusBar.fileData.Extension}  {statusBar.fileData.Encoding}  {statusBar.fileData.FileSize} ";
		string rightRaw = $" Ln {statusBar.line}/ Col {statusBar.column} ";

		string rightText = FitStatusSegment(rightRaw, width);
		int rightStart = width - rightText.Length;

		string middleCandidate = FitStatusSegment(middleRaw, rightStart);
		int middleStart = Math.Max(0, rightStart - middleCandidate.Length);
		int middleMaxLength = Math.Max(0, rightStart - middleStart);
		string middleText = FitStatusSegment(middleCandidate, middleMaxLength);

		string leftText = FitStatusSegment(leftRaw, middleStart);

		if (leftText.Length > 0) {
			Console.SetCursorPosition(0, StatusBarLine);
			Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(leftText);
		}

		if (middleText.Length > 0) {
			Console.SetCursorPosition(middleStart, StatusBarLine);
			Console.BackgroundColor = ConsoleColor.Blue;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(middleText);
		}

		if (rightText.Length > 0) {
			Console.SetCursorPosition(rightStart, StatusBarLine);
			Console.BackgroundColor = ConsoleColor.DarkCyan;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(rightText);
		}

		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
		setCursor(buffer.line);
		Console.CursorVisible = true;
	}


}
