class Replacer {

	Buffer buffer;
	Searcher searcher;

	public bool isReplacing;
	public List<char> charsUsedToReplace = new List<char>();

	public Replacer(Buffer buffer, Searcher searcher) {
		this.buffer = buffer;
		this.searcher = searcher;
	}

	public void clearReplace() {
		isReplacing = false;
		charsUsedToReplace.Clear();
	}

	public void replaceCurrentFindling() {
		if (searcher.currentFindInd == null) return;
		var currentFindlingData = searcher.getCurrentFindlingData();
		if (currentFindlingData == null) return;
		int start = currentFindlingData.Value.start;
		int length = currentFindlingData.Value.length;
		int line = currentFindlingData.Value.line;

		searcher.removeFindlingByInd(searcher.currentFindInd.Value);
		buffer.removeArea(line, line, start, start + length);
		buffer.insertCharsAtPos(line, start, charsUsedToReplace);
		searcher.searchFile();
		buffer.line = line;
		buffer.column = start;
		buffer.clampCursor();
	}

	public void replaceAllFindilngs() {
		Findling? currentFindling;
		for (int i = searcher.totalFinds - 1; i >= 0; i--) {
			currentFindling = searcher.getFindlingByInd(i);
			if (currentFindling == null) return;
			searcher.removeFindlingByInd(i);
			buffer.removeArea(currentFindling.Value.line, currentFindling.Value.line, currentFindling.Value.Start, currentFindling.Value.Start + currentFindling.Value.Length);
			buffer.insertCharsAtPos(currentFindling.Value.line, currentFindling.Value.Start, charsUsedToReplace);
		}
		buffer.clampCursor();
	}

	public void setCharsUsedToReplace(List<char> chars) {
		charsUsedToReplace = new List<char>(chars);
	}
}
