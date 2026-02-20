using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualBasic;

class Render {
	Buffer buffer;

	StatusBar statusBar;
	StatusBarRenderer statusBarRenderer;
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
		statusBarRenderer = new StatusBarRenderer();
		simpleHighlighter = new SimpleHighlighter();
		converter = new();
		currentDistFromEdge = 4;
	}




	public void ResetView() {
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


	public void SetCursor(int lineInd) {
		Console.SetCursorPosition(GetCursorXPos(lineInd), GetScreenLine(lineInd));
	}

	public int GetScreenLine(int lineInd) {
		return lineInd - topLine;
	}

	public int GetSpacesNeeded(int lineInd) {
		if (lineInd.ToString().Length >= currentDistFromEdge) {
			currentDistFromEdge = lineInd.ToString().Length + 2;
		}
		return currentDistFromEdge - lineInd.ToString().Length;
	}

	public void PrintLineNumber(int lineInd) {
		Console.SetCursorPosition(0, GetScreenLine(lineInd));
		Console.ForegroundColor = ConsoleColor.DarkMagenta;
		Console.Write(lineInd + new string(' ', GetSpacesNeeded(lineInd)));
		Console.ForegroundColor = ConsoleColor.White;
	}

	public int GetCursorXPos(int lineInd) {
		return lineInd.ToString().Length + buffer.column + GetSpacesNeeded(lineInd);
	}

	public (int startLine, int endLine, int startColumn, int endColumn) GetSelectedArea() {
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


	public void PrintLine(int lineInd, bool completeRedraw) {
		Console.CursorVisible = false;
		PrintLineNumber(lineInd);
		List<char> line = buffer.lines[lineInd];
		List<Span> spans = new();
		spans.AddRange(converter.ConvertTokensToSpans(simpleHighlighter.HighlightLine(line)));
		if (searcher.isSearching) {
			spans.AddRange(converter.ConvertFindlingsToSpans(searcher.findlings[lineInd]));
		}
		if (buffer.isSelecting) {
			var sel = GetSelectedArea();

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
			spans.Add(converter.ConvertSelectionToSpan(selectStart, selectLength));

		}

		bool isEmptySelected = buffer.isSelecting && line.Count == 0 && lineInd >= GetSelectedArea().startLine && lineInd <= GetSelectedArea().endLine;

		if (isEmptySelected) {
			Console.BackgroundColor = ConsoleColor.Cyan;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Write(" ");
			Console.BackgroundColor = ConsoleColor.Black;
			Console.Write("\x1b[K");
			SetCursor(lineInd);
			if (!completeRedraw) Console.CursorVisible = true;
			return;
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
		SetCursor(lineInd);
		if (!completeRedraw) {
			Console.CursorVisible = true;
		}
	}

	public void PrintSection(int startLineInd) {
		for (int i = startLineInd; i < bottomLine; i++) {
			PrintLine(i, true);
		}
		if (bottomLine < ContentHeight) {
			for (int i = bottomLine; i < ContentHeight; i++) {
				Console.SetCursorPosition(0, GetScreenLine(i));
				Console.Write("\x1b[K");
			}
		}
		SetCursor(buffer.line);
		Console.CursorVisible = true;
	}

	public void PrintScreen() {
		PrintSection(topLine);
	}

	public void DrawStatusBar(SearchInputMode searchInputMode) {
		var (filePath, fileData, column, line, statusBarMode, searchedChars, replaceChars, showReplace) = statusBar.GetData();
		int width = Console.WindowWidth;
		if (width <= 0) return;

		List<StatusBarSegment> leftSegments = new();
		StatusBarSegment rightSegment = new(
			$" Ln {line + 1}/ Col {column + 1} ",
			ConsoleColor.DarkCyan
		);

		if (statusBarMode == StatusBarMode.Search) {
			leftSegments.Add(new StatusBarSegment(
				searchInputMode == SearchInputMode.Search ? $" [SEARCH]: {searchedChars} " : $" SEARCH: {searchedChars} ",
				searchInputMode == SearchInputMode.Search ? ConsoleColor.Green : ConsoleColor.DarkGreen
			));

			if (showReplace) {
				leftSegments.Add(new StatusBarSegment(
					searchInputMode == SearchInputMode.Replace ? $" [REPLACE]: {replaceChars} " : $" REPLACE: {replaceChars} ",
					searchInputMode == SearchInputMode.Replace ? ConsoleColor.Blue : ConsoleColor.DarkBlue
				));
			}

			statusBarRenderer.Draw(
				StatusBarLine,
				width,
				ConsoleColor.DarkBlue,
				leftSegments,
				rightSegment
			);
		}
		else {
			leftSegments.Add(new StatusBarSegment($" {filePath} ", ConsoleColor.DarkBlue));
			leftSegments.Add(new StatusBarSegment(
				$" {fileData.Extension}  {fileData.Encoding}  {fileData.FileSize} ",
				ConsoleColor.Blue
			));

			statusBarRenderer.Draw(
				StatusBarLine,
				width,
				ConsoleColor.DarkBlue,
				leftSegments,
				rightSegment
			);
		}

		SetCursor(buffer.line);
		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
		Console.CursorVisible = true;
	}

}

