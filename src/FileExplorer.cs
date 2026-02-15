using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
class FileExplorer {
	public string cuurentFilePath = "";


	public List<List<char>> readFile(string filePath) {
		string[] lines = File.ReadAllLines(filePath);
		List<List<char>> readFile = new List<List<char>>();
		foreach (string line in lines) {
			readFile.Add([.. line]);
		}
		if (readFile.Count == 0) {
			readFile.Add(new List<char>());
		}
		cuurentFilePath = filePath;
		return readFile;
	}

	public void saveFile(string path, List<List<char>> contentList) {
		string tempPath = path + ".tmp";
		var sb = new StringBuilder();

		for (int i = 0; i < contentList.Count; i++) {
			sb.Append(contentList[i].ToArray());
			if (i < contentList.Count - 1)
				sb.Append(Environment.NewLine);
		}

		string text = sb.ToString();

		File.WriteAllText(tempPath, text);
		File.Replace(tempPath, path, null);
	}


}
