class HelperFunctions {
	public List<int> getOldLineLength(List<List<char>> doubleList) {
		List<int> intList = new List<int>();
		foreach (List<char> list in doubleList) {
			int listLength = list.Count;
			intList.Add(listLength);
		}

		return intList;
	}

}
