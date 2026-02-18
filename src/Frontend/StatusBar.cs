using System;
using System.IO;

class StatusBar {
	Buffer buffer;
	FileExplorer fileExplorer;
	Searcher searcher;

	public StatusBarMode statusBarMode;

	public string? search = "";
	public int line;
	public int column;
	public FileData fileData;
	public string filePath;

	public StatusBar(Buffer buffer, FileExplorer fileExplorer, Searcher searcher) {
		this.buffer = buffer;
		this.searcher = searcher;
		this.fileExplorer = fileExplorer;
		this.filePath = fileExplorer.cuurentFilePath;
		this.fileData = new FileData("", "0 KB", "Unknown");
	}

	public (string filePath, FileData fileData, int column, int line, StatusBarMode statusBarMode, string? searchedChars) getData() {
		UpdateStatusBar();

		return (filePath, fileData, column, line, statusBarMode, search);
	}

	public void UpdateStatusBar() {
		filePath = fileExplorer.cuurentFilePath;
		GetFileData();
		column = buffer.column;
		line = buffer.line;

		if (statusBarMode == StatusBarMode.Normal) {
			search = null;
		}
		else {
			search = new string(searcher.searchedChars.ToArray());
		}
	}

	public void GetFileData() {
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
			fileData = new FileData("", "0 KB", "Unknown");
			return;
		}

		FileInfo fileInfo = new FileInfo(filePath);
		double sizeInKb = fileInfo.Length / 1024.0;
		string fileSize = $"{sizeInKb:F2} KB";
		string extension = Path.GetExtension(filePath);
		string encoding;

		using (var reader = new StreamReader(filePath, true)) {
			reader.Peek();
			encoding = reader.CurrentEncoding.EncodingName;
		}

		fileData = new FileData(extension, fileSize, encoding);
	}
}

public record FileData(string Extension, string FileSize, string Encoding);

enum StatusBarMode {
	Normal,
	Search,
}
