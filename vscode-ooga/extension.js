// The Ooga extension's only code: the "Run Ooga File" command (the play button on .ooga files).
// Colours, push-in rules and the file icon need no code; they are in package.json.

const vscode = require("vscode");
const fs = require("fs");
const path = require("path");

// Which ooga command to run, best first:
//   1. the "ooga.command" setting, if the person filled it in
//   2. ooga.cmd in the open folder (the ooga repository itself)
//   3. plain "ooga", found through PATH (install.ps1 puts it there)
function findOoga(file, settingValue, workspaceFolders, exists, platform) {
    if (settingValue && settingValue.trim() !== "") return settingValue.trim();
    const launcher = platform === "win32" ? "ooga.cmd" : "ooga.sh";
    for (const folder of workspaceFolders) {
        const candidate = path.join(folder, launcher);
        if (exists(candidate)) return candidate;
    }
    return platform === "win32" ? "ooga" : "ooga.sh";
}

async function runOogaFile(uri) {
    const editor = vscode.window.activeTextEditor;
    const target = uri instanceof vscode.Uri ? uri : editor && editor.document.uri;
    if (!target || !target.fsPath.toLowerCase().endsWith(".ooga")) {
        vscode.window.showWarningMessage("ooga can only run .ooga files.");
        return;
    }

    // Save first, so ooga runs what you see.
    const open = vscode.workspace.textDocuments.find(d => d.uri.fsPath === target.fsPath);
    if (open && open.isDirty) await open.save();

    const file = target.fsPath;
    const folders = (vscode.workspace.workspaceFolders || []).map(f => f.uri.fsPath);
    const setting = vscode.workspace.getConfiguration("ooga").get("command");
    const command = findOoga(file, setting, folders, fs.existsSync, process.platform);

    // Run it as a task: the task system quotes paths with spaces correctly for any terminal,
    // and the terminal stays open so you can answer "ask" questions and read the output.
    const execution = new vscode.ShellExecution(command, [path.basename(file)], { cwd: path.dirname(file) });
    const task = new vscode.Task({ type: "ooga", file }, vscode.TaskScope.Workspace, "run " + path.basename(file), "ooga", execution, []);
    task.presentationOptions = {
        reveal: vscode.TaskRevealKind.Always,
        focus: true,
        panel: vscode.TaskPanelKind.Dedicated,
        clear: true,
        showReuseMessage: false,
    };
    await vscode.tasks.executeTask(task);
}

function activate(context) {
    context.subscriptions.push(vscode.commands.registerCommand("ooga.run", runOogaFile));
}

function deactivate() { }

module.exports = { activate, deactivate, findOoga };
