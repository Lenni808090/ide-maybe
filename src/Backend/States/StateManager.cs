using System.Runtime.CompilerServices;

class StateManager {
	ProgrammState prevProgrammState;
	ProgrammState programmState = ProgrammState.FileExplorer;
	State currentState;
	EditorState editorState;
	FileExplorerState fileExplorerState;
	Buffer buffer;
	FileManager fileManager;
	string? currentFilePath;
	bool programmStateChanged;

	public StateManager(string baseDir) {
		buffer = new();
		fileExplorerState = new(this, baseDir);
		editorState = new(this, buffer);
		fileManager = new();
		currentState = fileExplorerState;
		Console.TreatControlCAsInput = true;
	}
	public async Task StartStateManager() {
		Console.Clear();
		Console.Write("\x1b[?2004h");

		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

			await currentState.handleInput(keyInfo);

			if (programmState != prevProgrammState) {
				programmStateChanged = true;
				prevProgrammState = programmState;
			}

			if (programmStateChanged) {
				Console.Clear();
				if (programmState == ProgrammState.Editor) {
					currentState = editorState;
					currentState.Render();
				}
				else if (programmState == ProgrammState.FileExplorer) {
					currentState = fileExplorerState;
					currentState.Render();
				}
				else if (programmState == ProgrammState.CLosed) {
					Console.Clear();
					Console.Write("\x1b[?2004l");
					Console.CursorVisible = true;
					Console.WriteLine("till next time");
					break;
				}
			}

			if (programmStateChanged) {
				programmStateChanged = false;
			}
		}
	}


	public void SwitchState(ProgrammState programmState) {
		this.programmState = programmState;
	}

	public void OpenFileInEditor(string filePath) {
		List<List<char>> newBuffer = fileManager.ReadFile(filePath);
		buffer.lines = new(newBuffer);
		buffer.column = 0;
		buffer.line = 0;
		currentFilePath = filePath;
		SwitchState(ProgrammState.Editor);
	}

	public string GetCurrentFilePath() {
		return currentFilePath ?? "";
	}

	public void SaveCurrentFile(List<List<char>> lines) {
		if (string.IsNullOrEmpty(currentFilePath)) return;
		fileManager.SaveFile(currentFilePath, lines);
	}

	private void StartResizeWatcher() {
		Task.Run(async () => {
			int prevHeight = Console.WindowHeight;
			int prevWidth = Console.WindowWidth;
			while (true) {
				if (Console.WindowHeight != prevHeight || Console.WindowWidth != prevWidth) {
					Console.Clear();
					prevHeight = Console.WindowHeight;
					prevWidth = Console.WindowWidth;
					currentState.Render();
				}

				await Task.Delay(50);
			}
		});
	}
}

enum ProgrammState {
	Editor,
	FileExplorer,

	CLosed,
}
