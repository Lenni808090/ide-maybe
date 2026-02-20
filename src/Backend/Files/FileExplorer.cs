using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

class FileExplorer {
	string baseDir;
	string currentDir;
	public int currentHoveredFile;
	public FileExplorer(string baseDir) {
		this.baseDir = baseDir;
		currentDir = this.baseDir;
	}

	public void MoveToNextEntry() {
		var dirInfo = GetInfoAboutCurrentDir();
		int totalEntryCount = GetTotalEntriesCount(dirInfo.directoryInfos, dirInfo.fileInfos);
		if (currentHoveredFile == totalEntryCount - 1) {
			return;
		}
		currentHoveredFile++;
	}


	public void MoveToPrevEntry() {
		var dirInfo = GetInfoAboutCurrentDir();
		int totalEntryCount = GetTotalEntriesCount(dirInfo.directoryInfos, dirInfo.fileInfos);
		if (currentHoveredFile == 0) {
			return;
		}
		currentHoveredFile--;
	}
	public (List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) GetInfoAboutDir(string currentDir) {
		var currentDirInfo = new DirectoryInfo(currentDir);
		List<DirectoryInfo> directoryInfos = new();
		List<FileInfo> fileInfos = new();

		foreach (FileSystemInfo entry in currentDirInfo.EnumerateFileSystemInfos()) {
			if (entry is DirectoryInfo dir) {
				directoryInfos.Add(dir);
			}
			else if (entry is FileInfo file) {
				fileInfos.Add(file);
			}
		}

		return (directoryInfos, fileInfos);
	}

	public (bool isFile, FileSystemInfo systemInfo) GetEntryByInd(int entryInd) {
		var currentDirInfo = GetInfoAboutCurrentDir();
		int dirCount = currentDirInfo.directoryInfos.Count;
		int fileCount = currentDirInfo.fileInfos.Count;
		int total = dirCount + fileCount;

		if (entryInd < 0 || entryInd >= total) {
			throw new ArgumentOutOfRangeException(nameof(entryInd));
		}

		if (entryInd < dirCount) {
			return (false, currentDirInfo.directoryInfos[entryInd]);
		}

		return (true, currentDirInfo.fileInfos[entryInd - dirCount]);
	}

	public string? MoveIntoEntry() {
		var entryToMoveIn = GetEntryByInd(currentHoveredFile);
		string entryPath = Path.Combine(currentDir, entryToMoveIn.systemInfo.Name);

		if (entryToMoveIn.isFile) {
			return entryPath;
		}

		currentDir = entryPath;
		currentHoveredFile = 0;
		return null;
	}

	public void MoveOutOfEntry() {
		var entry = new DirectoryInfo(currentDir);

		if (entry.Parent != null && entry.Parent.FullName != baseDir) {
			currentDir = entry.Parent.FullName;
			currentHoveredFile = 0;
		}
	}

	public (List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) GetInfoAboutCurrentDir() {
		return GetInfoAboutDir(currentDir);
	}

	public int GetTotalEntriesCount(List<DirectoryInfo> directoryInfos, List<FileInfo> fileInfos) {
		return directoryInfos.Count + fileInfos.Count;
	}
	public void SetBaseDir(string baseDir) {
		this.baseDir = baseDir;
	}

	public void SetCurrentDir(string currentDir) {
		this.currentDir = currentDir;
	}

}

