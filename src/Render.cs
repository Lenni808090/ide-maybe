using System.Diagnostics;

class Render {
	List<int> screenCharCount;
	Buffer buffer;

	int curRenderLine = 0;
	public Render(Buffer buffer) {
		this.buffer = buffer;
		screenCharCount = new List<int>();
	}

	public int getCursorPos() {
		return buffer.coloumn + curRenderLine.ToString().Length + 1;
	}

	public void RedrawLine(int indLineToRedraw) {
		Console.CursorVisible = false;
		List<char> lineToRedraw = buffer.lines[indLineToRedraw];
		int start = 0;
		curRenderLine = indLineToRedraw;
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
		Console.SetCursorPosition(getCursorPos(), indLineToRedraw);

		Console.CursorVisible = true;
	}

	public void RedrawSection(int startLine) {
		for (int i = startLine; i < buffer.lines.Count; i++) {
			RedrawLine(i);
		}

		if (screenCharCount.Count > buffer.lines.Count) {
			for (int i = buffer.lines.Count; i < screenCharCount.Count; i++) {
				Console.SetCursorPosition(0, i);
				clearLine();
			}

			screenCharCount.RemoveRange(buffer.lines.Count, screenCharCount.Count - buffer.lines.Count);
		}
	}

	public void setCursor() {
		Console.SetCursorPosition(getCursorPos(), buffer.line);
	}

	public void clearLine() {
		Console.Write(new string(' ', Console.WindowWidth));
	}
}
