using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks.Dataflow;

class Render {
	Buffer buffer;

	public int topLine = 0;
	public int bottomLine = 0;

	int currentDistFromEdge;

	private int ContentHeight => Console.WindowHeight - 1;
	private int StatusBarLine => Console.WindowHeight - 1;

	public Render(Buffer buffer) {
		this.buffer = buffer;
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


	public void printLine(int lineInd) {
		Console.CursorVisible = false;
		printLineNumber(lineInd);
		List<char> lineToPrint = buffer.lines[lineInd];

		int i = 0;


		if (buffer.isSelecting) {
			var selectedArea = getSelectedArea();
			if (selectedArea.startLine < lineInd && selectedArea.endLine > lineInd) {
				Console.BackgroundColor = ConsoleColor.Cyan;
			}

			if (lineToPrint.Count == 0 && Console.BackgroundColor == ConsoleColor.Cyan) {
				Console.Write(" ");
			}

			foreach (char c in lineToPrint) {
				if (selectedArea.startLine == lineInd && selectedArea.startColumn == i) {
					if (i == selectedArea.endColumn) {
						Console.BackgroundColor = ConsoleColor.Black;
					}
					else {
						Console.BackgroundColor = ConsoleColor.Cyan;
					}
				}
				else if (selectedArea.endLine == lineInd) {
					if (i < selectedArea.endColumn && selectedArea.endLine != selectedArea.startLine) {
						Console.BackgroundColor = ConsoleColor.Cyan;
					}
					else if (i == selectedArea.endColumn) {
						Console.BackgroundColor = ConsoleColor.Black;
					}
				}

				Console.Write(c);
				i++;
			}
		}
		else {
			Console.BackgroundColor = ConsoleColor.Black;
			foreach (char c in lineToPrint) {
				Console.Write(c);
			}
		}
		Console.BackgroundColor = ConsoleColor.Black;
		Console.Write("\x1b[K");
		setCursor(lineInd);
		Console.CursorVisible = true;
	}

	public void printSection(int startLineInd) {
		for (int i = startLineInd; i < bottomLine; i++) {
			printLine(i);
		}
		if (bottomLine < ContentHeight) {
			for (int i = bottomLine; i < ContentHeight; i++) {
				Console.SetCursorPosition(0, getScreenLine(i));
				Console.Write("\x1b[K");
			}
		}
		setCursor(buffer.line);
	}

	public void printScreen() {
		printSection(topLine);
	}


	public void drawStatusBar((string filePath, FileData fileData, int column, int line) statusBar) {
		Console.CursorVisible = false;

		Console.SetCursorPosition(0, StatusBarLine);
		Console.BackgroundColor = ConsoleColor.DarkGray;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write(new string(' ', Console.WindowWidth));

		Console.SetCursorPosition(0, StatusBarLine);
		Console.BackgroundColor = ConsoleColor.DarkBlue;
		Console.ForegroundColor = ConsoleColor.White;
		string filePath = $" {statusBar.filePath} ";
		Console.Write(filePath);

		Console.BackgroundColor = ConsoleColor.DarkGreen;
		Console.ForegroundColor = ConsoleColor.White;
		int middleStart = Console.WindowWidth / 2 - 15;
		Console.SetCursorPosition(middleStart, StatusBarLine);
		string fileInfo = $" {statusBar.fileData.Extension}  {statusBar.fileData.Encoding}  {statusBar.fileData.FileSize} ";
		Console.Write(fileInfo);

		Console.BackgroundColor = ConsoleColor.DarkMagenta;
		Console.ForegroundColor = ConsoleColor.White;
		string lineColumn = $" Ln {statusBar.line}, Col {statusBar.column} ";
		Console.SetCursorPosition(Console.WindowWidth - lineColumn.Length, StatusBarLine);
		Console.Write(lineColumn);

		Console.BackgroundColor = ConsoleColor.Black;
		Console.ForegroundColor = ConsoleColor.White;
		setCursor(buffer.line);
		Console.CursorVisible = true;
	}


}
