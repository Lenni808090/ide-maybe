using System;
using System.Collections.Generic;
using System.ComponentModel;

class Program {
	static async Task Main() {
		StateManager stateManager = new();
		await stateManager.StartStateManager();
	}

}

