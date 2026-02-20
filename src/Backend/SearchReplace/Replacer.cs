class Replacer {

	Buffer buffer;
	Searcher searcher;

	public bool isReplacing;
	public List<char> charsUsedToReplace = new List<char>();

	public Replacer(Buffer buffer, Searcher searcher) {
		this.buffer = buffer;
		this.searcher = searcher;
	}

	public void ClearReplace() {
		isReplacing = false;
		charsUsedToReplace.Clear();
	}

	public void ReplaceCurrentFindling() {
		if (searcher.currentFindInd == null) return;
		var currentFindlingData = searcher.GetCurrentFindlingData();
		if (currentFindlingData == null) return;
		int start = currentFindlingData.Value.start;
		int length = currentFindlingData.Value.length;
		int line = currentFindlingData.Value.line;

		searcher.RemoveFindlingByInd(searcher.currentFindInd.Value);
		buffer.RemoveArea(line, line, start, start + length);
		buffer.InsertCharsAtPos(line, start, charsUsedToReplace);
		searcher.SearchFile();
		buffer.line = line;
		buffer.column = start;
		buffer.ClampCursor();
	}

	public void ReplaceAllFindlings() {
		Findling? currentFindling;
		for (int i = searcher.totalFinds - 1; i >= 0; i--) {
			currentFindling = searcher.GetFindlingByInd(i);
			if (currentFindling == null) return;
			searcher.RemoveFindlingByInd(i);
			buffer.RemoveArea(currentFindling.Value.line, currentFindling.Value.line, currentFindling.Value.Start, currentFindling.Value.Start + currentFindling.Value.Length);
			buffer.InsertCharsAtPos(currentFindling.Value.line, currentFindling.Value.Start, charsUsedToReplace);
		}
		buffer.ClampCursor();
	}

	public void SetCharsUsedToReplace(List<char> chars) {
		charsUsedToReplace = new List<char>(chars);
	}
}

