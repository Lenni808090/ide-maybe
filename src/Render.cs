using System.Diagnostics;

class Render {
	List<int> screenCharCount;
	Buffer buffer;

	public int topLine = 0;
	public int bottomLine = 0;

	public Render(Buffer buffer) {
		this.buffer = buffer;
		screenCharCount = new List<int>();
	}

	public int getCursorPos(int indLine) {
		return buffer.coloumn + indLine.ToString().Length + 1;
	}

	public void RedrawScreen() {
		topLine = Math.Max(buffer.line - Console.WindowHeight, 0);
		bottomLine = Math.Min(topLine + Console.WindowHeight, buffer.lines.Count - 1);
		RedrawSection(topLine);
	}

	public void RedrawLine(int indLineToRedraw) {
		Console.CursorVisible = false;
		List<char> lineToRedraw = buffer.lines[indLineToRedraw];
		int start = 0;
		Console.SetCursorPosition(start, indLineToRedraw);

		Console.Write(indLineToRedraw + " ");

		for (int i = start; i < lineToRedraw.Count; i++) {
			Console.Write(lineToRedraw[i]);
		}

		int lineCount = lineToRedraw.Count;

		int screenCount = 0;
		if (screenCharCount.Count > indLineToRedraw) {
			screenCount = screenCharCount[indLineToRedraw];
		}

		int remainingChars = (screenCount > lineCount) ? (screenCount - lineCount) : 0;

		Console.Write(new string(' ', remainingChars));

		if (screenCharCount.Count <= indLineToRedraw) {
			screenCharCount.Add(lineCount);
		} else {
			screenCharCount[indLineToRedraw] = lineCount;
		}
		Console.SetCursorPosition(getCursorPos(indLineToRedraw), indLineToRedraw);

		Console.CursorVisible = true;
	}

	public void RedrawSection(int startLine) {
		for (int i = startLine; i < buffer.lines.Count; i++) {
			RedrawLine(i);
		}

		if (screenCharCount.Count > bottomLine) {
			for (int i = bottomLine; i < screenCharCount.Count; i++) {
				Console.SetCursorPosition(0, i);
				clearLine();
			}

			screenCharCount.RemoveRange(buffer.lines.Count, screenCharCount.Count - buffer.lines.Count);
		}
	}

	public void setCursor() {
		int xPos;
		if (buffer.line > Console.WindowHeight) {
			xPos = buffer.line - topLine;
		} else {
			xPos = buffer.line;
		}
		Console.SetCursorPosition(getCursorPos(xPos), buffer.line);
	}

	public void clearLine() {
		Console.Write(new string(' ', Console.WindowWidth));
	}
}
