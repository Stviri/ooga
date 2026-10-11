// Checks which ooga command the Run button picks. Run:  node vscode-ooga/test/find-ooga.test.js
// (needs only Node; a tiny stand-in replaces the real "vscode" module, which exists only inside VS Code)
const Module = require("module");
const realLoad = Module._load;
Module._load = (request, ...rest) => (request === "vscode" ? {} : realLoad(request, ...rest));

const assert = require("assert");
const path = require("path");
const { findOoga } = require(path.join(__dirname, "..", "extension.js"));
const has = files => p => files.includes(p);
const j = (...parts) => path.join(...parts);

assert.strictEqual(findOoga("a.ooga", "  C:\\tools\\ooga.cmd ", ["w"], has([j("w", "ooga.cmd")]), "win32"), "C:\\tools\\ooga.cmd", "the setting wins");
assert.strictEqual(findOoga("a.ooga", "", ["x", "w"], has([j("w", "ooga.cmd")]), "win32"), j("w", "ooga.cmd"), "ooga.cmd in the open folder");
assert.strictEqual(findOoga("a.ooga", "", ["w"], has([]), "win32"), "ooga", "plain ooga from PATH");
assert.strictEqual(findOoga("a.ooga", undefined, [], has([]), "win32"), "ooga", "no folder open, no setting");
assert.strictEqual(findOoga("a.ooga", "", ["w"], has([j("w", "ooga.sh")]), "linux"), j("w", "ooga.sh"), "ooga.sh on Linux and macOS");
console.log("Run button command lookup: 5 checks ok");
