class Searcher {
	Buffer buffer;
	public bool isSearching;
	public List<char> searchedChars;

	public Searcher(Buffer buffer) {
		this.buffer = buffer;
		searchedChars = new List<char>();
	}

	public void setSearch(List<char> chars) {
		isSearching = true;
		searchedChars = chars;
	}

	public void clearSearch() {
		isSearching = false;
		searchedChars.Clear();
	}

	public List<Findling> searchLine(int lineInd) {
		if (buffer?.lines == null || searchedChars == null || searchedChars.Count == 0)
			return new List<Findling>();

		List<Findling> findlings = new List<Findling>();

		List<char> line = buffer.lines[lineInd];
		for (int i = 0; i <= line.Count - searchedChars.Count; i++) {

			if (line[i] == searchedChars[0]) {
				int y = 1;
				int length = 1;
				bool match = true;

				while (y < searchedChars.Count) {
					if (line[i + y] == searchedChars[y]) {
						length++;
						y++;
					}
					else {
						match = false;
						break;
					}
				}

				if (match) {
					findlings.Add(new Findling {
						Start = i,
						Length = length,
					});
				}
			}
		}

		return findlings;
	}
}
