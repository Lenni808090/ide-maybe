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
		} else if (buffer.line > topLine + h - margin) {
			topLine = buffer.line - (h - margin);
		}

		topLine = Math.Max(0, topLine);
		topLine = Math.Min(topLine, buffer.lines.Count - h);

		bottomLine = topLine + h;
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
		Console.ForegroundColor = ConsoleColor.DarkMagenta;
	}

	public int getCursorXPos(int lineInd) {
		return lineInd.ToString().Length;
	}

	public void printLine(int lineInd) {
		printLineNumber(lineInd);
		List<char> lineToPrint = buffer.lines[lineInd];
		foreach (char c in lineToPrint) {
			Console.Write(c);
		}
		if (screenCharCount.Count - 1 < lineInd) {
			screenCharCount.Add(lineToPrint.Count);
		} else if (screenCharCount[lineInd] > lineToPrint.Count) {
			for (int i = lineToPrint.Count; i < screenCharCount[lineInd]; i++) {
				Console.Write(" ");
			}
		}
		setCursor(lineInd);
	}



}
