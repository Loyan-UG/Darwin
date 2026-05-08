/* eslint-disable @typescript-eslint/no-require-imports */
const Module = require("module");

const originalLoad = Module._load;

Module._load = function loadWithServerOnlyShim(request, parent, isMain) {
  if (request === "server-only") {
    return {};
  }

  return originalLoad.call(this, request, parent, isMain);
};
