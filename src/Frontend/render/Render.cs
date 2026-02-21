using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
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

	Dictionary<int, LineData> cachedLines;
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
		cachedLines = new();
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
		int maxX = Math.Max(0, Console.WindowWidth - 1);
		int maxY = Math.Max(0, ContentHeight - 1);
		int x = GetCursorXPos(lineInd);
		int y = GetScreenLine(lineInd);
		x = Math.Clamp(x, 0, maxX);
		y = Math.Clamp(y, 0, maxY);
		Console.SetCursorPosition(x, y);
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
		if (!completeRedraw) {
			Console.CursorVisible = false;
		}
		PrintLineNumber(lineInd);
		List<char> line = buffer.lines[lineInd];
		ReadOnlySpan<char> spanLine = CollectionsMarshal.AsSpan(line);
		List<Span> spans = new();

		Span?[] activeSpans;

		(int startLine, int endLine, int startColumn, int endColumn)? selectedArea =
			buffer.isSelecting ? GetSelectedArea() : null;
		(int selectStart, int selectLength) = GetSelectionForLine(lineInd, line);




		bool isSearching = searcher.isSearching;
		bool hasCache = cachedLines.TryGetValue(lineInd, out LineData? cachedLine);
		int hashedLine = HashLine(line);
		List<Findling> findlings = lineInd < searcher.findlings.Count
			? searcher.findlings[lineInd]
			: new List<Findling>();
		int hashedFindlings = HashFindlings(findlings);


		bool canReuse = hasCache && cachedLine is not null && cachedLine.isSearching == isSearching && cachedLine.lineHash == hashedLine && cachedLine.findlingHash == hashedFindlings;

		if (canReuse && cachedLine is not null) {
			activeSpans = cachedLine.spans;
		}
		else {
			spans.AddRange(converter.ConvertTokensToSpans(simpleHighlighter.HighlightLine(line)));
			if (isSearching) {
				spans.AddRange(converter.ConvertFindlingsToSpans(findlings, searcher.currentFindInd ?? 0));
			}
			activeSpans = GetActiveSpans(spans, line.Count);
			cachedLines[lineInd] = new LineData(hashedLine, hashedFindlings, isSearching, lineInd, activeSpans);
		}


		bool isEmptySelected = buffer.isSelecting && line.Count == 0 && selectedArea.HasValue && lineInd >= selectedArea.Value.startLine && lineInd <= selectedArea.Value.endLine;
		if (isEmptySelected) {
			Console.BackgroundColor = ConsoleColor.Cyan;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Write(" ");
			Console.BackgroundColor = ConsoleColor.Black;
			Console.Write("\x1b[K");
			if (!completeRedraw) {
				SetCursor(lineInd);
			}
			if (!completeRedraw) Console.CursorVisible = true;
			return;
		}
		else if (line.Count == 0) {
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write("\x1b[K");
			if (!completeRedraw) SetCursor(lineInd);
			if (!completeRedraw) Console.CursorVisible = true;
			return;
		}

		int flushStart = 0;
		var current = activeSpans[0];
		var runStyle = GetFinalStyle(current, isSelected(0, selectStart, selectLength));
		for (int i = 0; i < line.Count; i++) {


			if (i + 1 < line.Count) {
				var next = activeSpans[i + 1];
				var nextStyle = GetFinalStyle(next, isSelected(i + 1, selectStart, selectLength));

				if (!StylesEqual(runStyle, nextStyle)) {
					Console.ForegroundColor = runStyle.fc;
					Console.BackgroundColor = runStyle.bc;
					Console.Write(spanLine.Slice(flushStart, i - flushStart + 1));
					runStyle = nextStyle;
					flushStart = i + 1;
				}
			}
		}
		Console.ForegroundColor = runStyle.fc;
		Console.BackgroundColor = runStyle.bc;
		Console.Write(spanLine.Slice(flushStart, line.Count - flushStart));

		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("\x1b[K");
		if (!completeRedraw) {
			SetCursor(lineInd);
		}
		if (!completeRedraw) {
			Console.CursorVisible = true;
		}
	}


	public bool isSelected(int i, int selectStart, int selectLength) {
		if (selectStart == 0 && selectLength == 0) return false;
		return i >= selectStart && i < selectStart + selectLength && buffer.isSelecting;
	}
	static public (ConsoleColor fc, ConsoleColor bc) GetFinalStyle(Span? span, bool selected) {
		if (selected) {
			return (ConsoleColor.Black, ConsoleColor.Cyan);
		}
		else {
			if (span.HasValue) {
				return (span.Value.ForegroundColor, span.Value.BackgroundColor ?? ConsoleColor.Black);

			}
			else {
				return (ConsoleColor.White, ConsoleColor.Black);
			}
		}
	}

	public void ScrollViewportIfPoss(int prevTopLine) {
		// Search overlays can change styling across many lines, so keep full redraw there.
		if (searcher.isSearching) {
			PrintScreen();
			return;
		}

		int delta = topLine - prevTopLine;
		if (Math.Abs(delta) != 1) {
			PrintScreen();
		}
		else {
			if (delta == -1) {
				Console.CursorVisible = false;
				Console.Write($"\x1b[1;{ContentHeight}r");
				Console.SetCursorPosition(0, 0);
				Console.Write("\x1b[1T");
				PrintLine(topLine, true);
				Console.Write("\x1b[r");
				SetCursor(buffer.line);
				Console.CursorVisible = true;
			}
			else if (delta == 1) {
				Console.CursorVisible = false;
				Console.Write($"\x1b[1;{ContentHeight}r");
				Console.SetCursorPosition(0, 0);
				Console.Write("\x1b[1S");
				PrintLine(bottomLine - 1, true);
				Console.Write("\x1b[r");
				SetCursor(buffer.line);
				Console.CursorVisible = true;
			}
		}
	}

	static public bool StylesEqual((ConsoleColor fc, ConsoleColor bc) firstStyle, (ConsoleColor fc, ConsoleColor bc) secoundStyle) {
		return firstStyle.fc == secoundStyle.fc && firstStyle.bc == secoundStyle.bc;
	}

	private (int selectStart, int selectLength) GetSelectionForLine(int lineInd, List<char> line) {
		if (!buffer.isSelecting) return (0, 0);

		var (startLine, endLine, startColumn, endColumn) = GetSelectedArea();

		int selectStart = 0;
		int selectLength = 0;

		if (lineInd < startLine || lineInd > endLine) {
			selectStart = 0;
			selectLength = 0;
		}
		else if (startLine == endLine) {
			selectStart = startColumn;
			selectLength = endColumn - startColumn;
		}
		else if (lineInd == startLine) {
			selectStart = startColumn;
			selectLength = line.Count - startColumn;
		}
		else if (lineInd == endLine) {
			selectStart = 0;
			selectLength = endColumn;
		}
		else {
			selectStart = 0;
			selectLength = line.Count;
		}

		return (selectStart, selectLength);
	}

	public void PrintSection(int startLineInd) {
		Console.CursorVisible = false;
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

	private static Span?[] GetActiveSpans(List<Span> spans, int lineLength) {
		Span?[] active = new Span?[lineLength];
		foreach (var span in spans) {
			int start = Math.Max(0, span.Start);
			int end = Math.Min(lineLength, span.Start + span.Lenght);
			for (int i = start; i < end; i++) {
				Span? current = active[i];
				if (!current.HasValue || span.Priority > current.Value.Priority) {
					active[i] = span;
				}
			}
		}
		return active;
	}

	public void PrintScreen() {
		PrintSection(topLine);
	}

	public void RedrawVisibleRanges(List<(int startLine, int endLine)> ranges) {
		if (ranges.Count == 0) {
			SetCursor(buffer.line);
			Console.CursorVisible = true;
			return;
		}

		Console.CursorVisible = false;
		SortedSet<int> linesToRedraw = new();
		foreach (var (startLine, endLine) in ranges) {
			int visibleStart = Math.Max(startLine, topLine);
			int visibleEnd = Math.Min(endLine, bottomLine - 1);
			if (visibleStart > visibleEnd) continue;

			for (int line = visibleStart; line <= visibleEnd; line++) {
				linesToRedraw.Add(line);
			}
		}
		foreach (int line in linesToRedraw) {
			PrintLine(line, true);
		}
		SetCursor(buffer.line);
		Console.CursorVisible = true;
	}


	public void DrawStatusBar(SearchInputMode searchInputMode) {
		var (filePath, fileData, column, line, statusBarMode, searchedChars, replaceChars, showReplace, isDirty, warningMessage) = statusBar.GetData();
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
		else if (statusBarMode == StatusBarMode.Warning) {
			leftSegments.Add(new StatusBarSegment($" [WARN] {warningMessage} ", ConsoleColor.Red));
			rightSegment.BackgroundColor = ConsoleColor.DarkRed;
			statusBarRenderer.Draw(
				StatusBarLine,
				width,
				ConsoleColor.DarkRed,
				leftSegments,
				rightSegment
			);
		}
		else {
			leftSegments.Add(new StatusBarSegment($" {filePath} ", ConsoleColor.DarkBlue));
			if (isDirty) {
				leftSegments.Add(new StatusBarSegment(" [+] ", ConsoleColor.DarkYellow));
			}
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

	int HashLine(List<char> line) {
		unchecked {
			int hash = 17;
			foreach (char c in line) {
				hash = (hash * 31) + c;
			}
			return hash;
		}
	}

	int HashFindlings(List<Findling> findlings) {
		unchecked {
			int hash = 17;
			foreach (Findling findling in findlings) {
				hash = (hash * 31) + findling.Start;
				hash = (hash * 31) + findling.Length;
				bool isCurrent = searcher.currentFindInd is int current && findling.Index == current;
				hash = (hash * 31) ^ (isCurrent ? 234798 : 92487);

			}
			hash = (hash * 31) + findlings.Count;
			return hash;
		}
	}

}

class LineData {
	public int lineHash;
	public int findlingHash;

	public bool isSearching;
	public int lineInd;
	public Span?[] spans = [];
	public LineData(int lineHash, int findlingHash, bool isSearching, int lineInd, Span?[] spans) {
		this.lineHash = lineHash;
		this.findlingHash = findlingHash;
		this.isSearching = isSearching;
		this.lineInd = lineInd;
		this.spans = spans;
	}
}
