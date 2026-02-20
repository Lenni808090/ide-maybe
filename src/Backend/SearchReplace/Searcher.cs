using System.Runtime.InteropServices;

class Searcher {
	Buffer buffer;
	public bool isSearching;
	public List<char> searchedChars;
	public int totalFinds = 0;

	public int? currentFindInd = null;
	int indexToBeAssigned;
	public List<List<Findling>> findlings;

	public Searcher(Buffer buffer) {
		this.buffer = buffer;
		findlings = new();
		searchedChars = new List<char>();
	}

	private static int NormalizeIndex(int index, int count) {
		return ((index % count) + count) % count;
	}


	public void ClampCurrentFindInd() {
		if (totalFinds == 0) {
			currentFindInd = null;
			return;
		}

		currentFindInd = NormalizeIndex(currentFindInd ?? 0, totalFinds);
	}
	public (int start, int length, int line)? GetCurrentFindlingData() {
		Findling? currentFindling = GetFindlingByInd(currentFindInd ?? 0);
		if (currentFindling == null) return null;
		int start = currentFindling.Value.Start;
		int length = currentFindling.Value.Length;
		int line = currentFindling.Value.line;

		return (start, length, line);
	}
	public Findling? GetFindlingByInd(int findlingInd) {
		if (totalFinds == 0) {
			return null;
		}

		findlingInd = NormalizeIndex(findlingInd, totalFinds);

		int test = findlingInd;
		Findling? findling = null;
		for (int i = 0; i < findlings.Count; i++) {
			if (test < findlings[i].Count) {
				findling = findlings[i][test];
				break;
			}
			else {
				test -= findlings[i].Count;
			}
		}

		return findling;
	}

	public void CalculateTotalFindlings() {
		totalFinds = 0;
		foreach (var list in findlings) {
			totalFinds += list.Count;
		}
	}

	public void RemoveFindlingByInd(int findlingInd) {

		findlingInd = NormalizeIndex(findlingInd, totalFinds);

		int test = findlingInd;

		for (int i = 0; i < findlings.Count; i++) {
			if (test < findlings[i].Count) {
				findlings[i].RemoveAt(test);
				break;
			}
			else {
				test -= findlings[i].Count;
			}
		}

		CalculateTotalFindlings();
	}
	public void MoveToNextFind() {
		if (totalFinds == 0) {
			return;
		}

		int nextIndex = currentFindInd.HasValue ? currentFindInd.Value + 1 : 0;
		Findling? nextFindling = GetFindlingByInd(nextIndex);

		if (nextFindling != null) {
			currentFindInd = nextIndex;
			ClampCurrentFindInd();
			buffer.column = nextFindling.Value.Start;
			buffer.line = nextFindling.Value.line;
			buffer.ClampCursor();
		}

	}
	public void MoveToPrevFind() {
		if (totalFinds == 0) {
			return;
		}

		int prevIndex = currentFindInd.HasValue ? currentFindInd.Value - 1 : totalFinds - 1;
		Findling? prevFindling = GetFindlingByInd(prevIndex);

		if (prevFindling != null) {
			currentFindInd = prevIndex;
			ClampCurrentFindInd();
			buffer.column = prevFindling.Value.Start;
			buffer.line = prevFindling.Value.line;
			buffer.ClampCursor();
		}
	}


	public void SetSearch(List<char> chars) {
		isSearching = true;
		searchedChars = chars;
		SearchFile();

	}

	public void ClearSearch() {
		isSearching = false;
		searchedChars.Clear();
		totalFinds = 0;
		currentFindInd = null;
		findlings.Clear();
	}
	public void SearchFile() {
		if (searchedChars.Count == 0) {
			totalFinds = 0;
			currentFindInd = null;
			findlings.Clear();
			return;
		}

		findlings.Clear();
		indexToBeAssigned = 0;

		for (int i = 0; i < buffer.lines.Count; i++) {
			var lineFindlings = SearchLine(i);

			if (lineFindlings.Count > 0) {
				findlings.Add(lineFindlings);
			}
			else {
				findlings.Add(new List<Findling>());
			}
		}
		CalculateTotalFindlings();
		ClampCurrentFindInd();

	}
	public List<Findling> SearchLine(int lineInd) {
		if (searchedChars.Count == 0) {
			return new List<Findling>();
		}
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
						line = lineInd,
						Index = indexToBeAssigned,
					});
					indexToBeAssigned++;
				}
			}
		}

		return findlings;
	}
}

