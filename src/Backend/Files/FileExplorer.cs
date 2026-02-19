using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class FileExplorer {
	public string currentDirectoryPath { get; private set; }
	public int selectedIndex { get; private set; }
	public List<FileExplorerEntry> entries { get; private set; }

	public FileExplorer(string? startDirectoryPath = null) {
		currentDirectoryPath = startDirectoryPath ?? Directory.GetCurrentDirectory();
		selectedIndex = 0;
		entries = new List<FileExplorerEntry>();
		loadDirectory(currentDirectoryPath);
	}

	public void loadDirectory(string directoryPath) {
		if (!Directory.Exists(directoryPath)) {
			return;
		}

		currentDirectoryPath = directoryPath;
		var loadedEntries = new List<FileExplorerEntry>();

		DirectoryInfo currentDirectory = new DirectoryInfo(currentDirectoryPath);
		if (currentDirectory.Parent != null) {
			loadedEntries.Add(new FileExplorerEntry("..", currentDirectory.Parent.FullName, true));
		}

		loadedEntries.AddRange(
			Directory.GetDirectories(currentDirectoryPath)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
				.Select(path => new FileExplorerEntry(Path.GetFileName(path), path, true))
		);

		loadedEntries.AddRange(
			Directory.GetFiles(currentDirectoryPath)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
				.Select(path => new FileExplorerEntry(Path.GetFileName(path), path, false))
		);

		entries = loadedEntries;
		selectedIndex = 0;
	}

	public void moveSelectionUp() {
		if (entries.Count == 0) return;
		selectedIndex = Math.Max(0, selectedIndex - 1);
	}

	public void moveSelectionDown() {
		if (entries.Count == 0) return;
		selectedIndex = Math.Min(entries.Count - 1, selectedIndex + 1);
	}

	public FileExplorerEntry? getSelectedEntry() {
		if (entries.Count == 0) return null;
		return entries[selectedIndex];
	}
}

public record FileExplorerEntry(string Name, string FullPath, bool IsDirectory);
