using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks.Dataflow;

class Render {
	Buffer buffer;
	SimpleHighlighter simpleHighlighter;
	public int topLine = 0;
	public int bottomLine = 0;

	int currentDistFromEdge;

	private int ContentHeight => Console.WindowHeight - 1;
	private int StatusBarLine => Console.WindowHeight - 1;

	public Render(Buffer buffer) {
		this.buffer = buffer;
		simpleHighlighter = new SimpleHighlighter();
		currentDistFromEdge = 4;
	}


	public ConsoleColor getColor(TokenKind tokenKind) {
		switch (tokenKind) {

			case TokenKind.Identifier:
				return ConsoleColor.White;
			case TokenKind.Keyword:
				return ConsoleColor.Red;
			case TokenKind.Number:
				return ConsoleColor.DarkBlue;
			case TokenKind.String:
				return ConsoleColor.DarkYellow;
			case TokenKind.Whitespace:
				return ConsoleColor.White;
			case TokenKind.Unknown:
				return ConsoleColor.White;
			case TokenKind.Operator:
				return ConsoleColor.Red;
			case TokenKind.Comment:
				return ConsoleColor.Gray;
			default:
				return ConsoleColor.White;

		}
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
		List<char> lineToPrint = buffer.lines[lineInd];
		List<Token> tokensToPrint = simpleHighlighter.HighlightLine(lineToPrint);
		int currentTokenInd = 0;
		Token currentToken = tokensToPrint.Count == 0 ? new Token { Start = 0, Length = 0, tokenKind = TokenKind.Unknown } : tokensToPrint[currentTokenInd];
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
					if (i == selectedArea.endColumn && selectedArea.startLine == selectedArea.endLine) {
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
				Console.ForegroundColor = getColor(currentToken.tokenKind);
				Console.Write(c);
				i++;
				if (i >= currentToken.Start + currentToken.Length) {
					currentTokenInd++;
					if (currentTokenInd < tokensToPrint.Count) {
						currentToken = tokensToPrint[currentTokenInd];
					}
				}

			}
		}
		else {
			Console.BackgroundColor = ConsoleColor.Black;
			foreach (char c in lineToPrint) {
				Console.ForegroundColor = getColor(currentToken.tokenKind);
				Console.Write(c);
				i++;
				if (i >= currentToken.Start + currentToken.Length) {
					currentTokenInd++;
					if (currentTokenInd < tokensToPrint.Count) {
						currentToken = tokensToPrint[currentTokenInd];
					}
				}

			}
		}
		Console.BackgroundColor = ConsoleColor.Black;
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
