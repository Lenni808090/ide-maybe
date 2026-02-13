using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks.Dataflow;

class Render {
	Buffer buffer;

	public int topLine = 0;
	public int bottomLine = 0;

	int currentDistFromEdge;

	public Render(Buffer buffer) {
		this.buffer = buffer;
		currentDistFromEdge = 4;
	}

	public void resetView() {
		int h = Console.WindowHeight - 1;

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
		return lineInd.ToString().Length + buffer.coloumn + getSpacesNeeded(lineInd);
	}

	public void printLine(int lineInd) {
		Console.CursorVisible = false;
		printLineNumber(lineInd);
		List<char> lineToPrint = buffer.lines[lineInd];
		foreach (char c in lineToPrint) {
			Console.Write(c);
		}
		Console.Write("\x1b[K");
		setCursor(lineInd);
		Console.CursorVisible = true;
	}

	public void printSection(int startLineInd) {
		for (int i = startLineInd; i < bottomLine; i++) {
			printLine(i);
		}
		if (bottomLine < Console.WindowHeight - 1) {
			for (int i = bottomLine; i < Console.WindowHeight - 1; i++) {
				Console.SetCursorPosition(0, getScreenLine(bottomLine));
				Console.Write("\x1b[K");
			}
		}
		setCursor(buffer.line);
	}

	public void printScreen() {
		printSection(topLine);
	}


}
