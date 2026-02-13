using System.Diagnostics;
using System.Security.AccessControl;

class Render {
	List<int> screenCharCount;
	Buffer buffer;

	public int topLine = 0;
	public int bottomLine = 0;


	public Render(Buffer buffer) {
		this.buffer = buffer;
		screenCharCount = new List<int>();
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

	public void printLineNumber(int lineInd) {
		Console.SetCursorPosition(0, getScreenLine(lineInd));
		Console.ForegroundColor = ConsoleColor.DarkMagenta;
		Console.Write(lineInd + "  ");
		Console.ForegroundColor = ConsoleColor.White;
	}

	public int getCursorXPos(int lineInd) {
		return lineInd.ToString().Length + buffer.coloumn + 2;
	}

	public void printLine(int lineInd) {
		Console.CursorVisible = false;
		printLineNumber(lineInd);
		List<char> lineToPrint = buffer.lines[lineInd];
		foreach (char c in lineToPrint) {
			Console.Write(c);
		}
		Console.Write("\x1b[K");
		if (screenCharCount.Count - 1 < lineInd) {
			screenCharCount.Add(lineToPrint.Count);
		}
		else if (screenCharCount[lineInd] > lineToPrint.Count) {
			for (int i = lineToPrint.Count; i < screenCharCount[lineInd]; i++) {
				Console.Write(" ");
			}
		}
		else {
			screenCharCount[lineInd] = lineToPrint.Count;
		}
		setCursor(lineInd);
		Console.CursorVisible = true;
	}

	public void printSection(int startLineInd) {
		for (int i = startLineInd; i < bottomLine; i++) {
			printLine(i);
		}

		if (screenCharCount.Count > buffer.lines.Count) {
			if (bottomLine + Console.WindowHeight - 1 > screenCharCount.Count) {
				Console.SetCursorPosition(0, bottomLine);
				for (int i = buffer.lines.Count; i < screenCharCount.Count; i++) {
					Console.WriteLine(new string(' ', Console.WindowWidth));
				}
			}
			screenCharCount.RemoveRange(buffer.lines.Count - 1, screenCharCount.Count - buffer.lines.Count);
		}
		setCursor(buffer.line);
	}

	public void printScreen() {
		printSection(topLine);
	}


}
