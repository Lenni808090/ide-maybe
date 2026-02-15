using System;
using System.IO;

class StatusBar {
	Buffer buffer;
	FileExplorer fileExplorer;

	public StatusBar(Buffer buffer, FileExplorer fileExplorer) {
		this.buffer = buffer;
		this.fileExplorer = fileExplorer;
	}

	public (string filePath, FileData fileData, int column, int line) UpdateStatusBar() {
		string filePath = fileExplorer.cuurentFilePath;
		var fileData = GetFileData(filePath);
		int column = buffer.column;
		int line = buffer.line;

		return (filePath, fileData, column, line);
	}

	public FileData GetFileData(string filePath) {
		FileInfo fileInfo = new FileInfo(filePath);

		double sizeInKb = fileInfo.Length / 1024.0;
		string fileSize = $"{sizeInKb:F2} KB";
		string extension = Path.GetExtension(filePath);
		string encoding;

		using (var reader = new StreamReader(filePath, true)) {
			reader.Peek();
			encoding = reader.CurrentEncoding.EncodingName;
		}

		return new FileData(extension, fileSize, encoding);
	}
}

public record FileData(string Extension, string FileSize, string Encoding);
