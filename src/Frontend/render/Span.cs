struct Span {
	public int Lenght;
	public int Start;
	public ConsoleColor ForegroundColor;
	public ConsoleColor? BackgroundColor;
	public int Priority;
}
class Converter {


	public List<Span> ConvertFindlingsToSpans(List<Findling> findlings) {
		List<Span> spans = new();
		foreach (Findling findling in findlings) {
			spans.Add(FindlingToSpan(findling));
		}
		return spans;
	}

	public List<Span> ConvertTokensToSpans(List<Token> tokens) {
		List<Span> spans = new();
		foreach (Token token in tokens) {
			spans.Add(TokenToSpan(token));
		}
		return spans;
	}


	public Span ConvertSelectionToSpan(int start, int length) {
		return SelectionToSpan(start, length);
	}



	Span TokenToSpan(Token t) {
		return new Span {
			Lenght = t.Length,
			Start = t.Start,
			ForegroundColor = GetColor(t.tokenKind),
			BackgroundColor = null,
			Priority = 1,
		};
	}

	Span FindlingToSpan(Findling f) {
		return new Span {
			Lenght = f.Length,
			Start = f.Start,
			ForegroundColor = ConsoleColor.Black,
			BackgroundColor = ConsoleColor.Yellow,
			Priority = 5,
		};
	}

	Span SelectionToSpan(int start, int length) {
		return new Span {
			Start = start,
			Lenght = length,
			ForegroundColor = ConsoleColor.Black,
			BackgroundColor = ConsoleColor.Cyan,
			Priority = 10
		};
	}

	public ConsoleColor GetColor(TokenKind tokenKind) {
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
}


