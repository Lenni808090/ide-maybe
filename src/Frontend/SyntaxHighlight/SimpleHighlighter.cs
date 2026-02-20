using System.Runtime.InteropServices;

class SimpleHighlighter : IHighlighter {
	public List<Token> HighlightLine(List<char> line) {
		List<Token> tokens = new List<Token>();
		int i = 0;
		ReadOnlySpan<char> lineSpan = CollectionsMarshal.AsSpan(line);

		while (i < line.Count) {
			char c = line[i];

			if (char.IsWhiteSpace(c)) {
				int start = i;
				while (i < line.Count && char.IsWhiteSpace(line[i])) i++;
				tokens.Add(new Token { Start = start, Length = i - start, tokenKind = TokenKind.Whitespace });
				continue;
			}

			if (c == '/' && i + 1 < line.Count && line[i + 1] == '/') {
				int start = i;
				i = line.Count;
				tokens.Add(new Token { Start = start, Length = i - start, tokenKind = TokenKind.Comment });
				break;
			}

			if (c == '"') {
				int start = i;
				i++;
				while (i < line.Count && line[i] != '"') i++;
				if (i < line.Count) i++;
				tokens.Add(new Token { Start = start, Length = i - start, tokenKind = TokenKind.String });
				continue;
			}

			if (char.IsDigit(c)) {
				int start = i;
				while (i < line.Count && char.IsDigit(line[i])) i++;
				tokens.Add(new Token { Start = start, Length = i - start, tokenKind = TokenKind.Number });
				continue;
			}

			if (char.IsLetter(c) || c == '_') {
				int start = i;
				while (i < line.Count && (char.IsLetterOrDigit(line[i]) || line[i] == '_')) i++;
				ReadOnlySpan<char> word = lineSpan.Slice(start, i - start);
				TokenKind kind = IsKeyword(word) ? TokenKind.Keyword : TokenKind.Identifier;
				tokens.Add(new Token { Start = start, Length = i - start, tokenKind = kind });
				continue;
			}

			tokens.Add(new Token { Start = i, Length = 1, tokenKind = TokenKind.Operator });
			i++;
		}

		return tokens;
	}

	private static bool IsKeyword(ReadOnlySpan<char> word) {
		return word.SequenceEqual("if")
			|| word.SequenceEqual("for")
			|| word.SequenceEqual("while")
			|| word.SequenceEqual("return")
			|| word.SequenceEqual("public")
			|| word.SequenceEqual("private")
			|| word.SequenceEqual("int");
	}
}




