class StateManager {
	ProgrammState prevProgrammState;
	ProgrammState programmState = ProgrammState.FileExplorer;
	State currentState;
	EditorState editorState;
	FileExplorerState fileExplorerState;
	bool programmStateChanged;

	public StateManager() {
		fileExplorerState = new(this);
		editorState = new(this);
		currentState = fileExplorerState;
		Console.TreatControlCAsInput = true;
	}
	public async Task StartStateManager() {
		Console.Clear();
		Console.Write("\x1b[?2004h");

		while (true) {
			ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
			if (programmState != prevProgrammState) {
				programmStateChanged = true;
			}

			if (programmStateChanged) {
				if (programmState == ProgrammState.Editor) {
					currentState = editorState;
				}
				else if (programmState == ProgrammState.FileExplorer) {
					currentState = fileExplorerState;
				}
				else if (programmState == ProgrammState.CLosed) {
					Console.Clear();
					Console.Write("\x1b[?2004l");
					Console.CursorVisible = true;
					Console.WriteLine("till next time");
					break;
				}
			}

			await currentState.handleInput(keyInfo);

			prevProgrammState = programmState;
			if (programmStateChanged) {
				programmStateChanged = false;
			}
		}
	}


	public void SwitchState(ProgrammState programmState) {
		this.programmState = programmState;
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
