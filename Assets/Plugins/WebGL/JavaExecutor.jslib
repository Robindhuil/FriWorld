mergeInto(LibraryManager.library, {
    SetupJavaExecutor: function() {
        // Inicializácia Java prostredia v prehliadači
        if (typeof BrowserJava === 'undefined') {
            BrowserJava = {
                worker: null,
                callbacks: {}
            };
        }
    },

    ExecuteJavaInBrowser: function(code, callbackObject, callbackMethod) {
        var jsCode = UTF8ToString(code);
        var jsCallbackObj = UTF8ToString(callbackObject);
        var jsCallbackMethod = UTF8ToString(callbackMethod);

        // Vytvorenie Web Worker pre vykonanie kódu
        var workerCode = `
            self.onmessage = function(e) {
                try {
                    // Simulácia Java prostredia
                    var stdout = "";
                    var stderr = "";
                    
                    // Prepísanie System.out.println
                    var System = {
                        out: {
                            println: function(msg) { stdout += msg + "\\n"; }
                        }
                    };
                    
                    // Vykonanie používateľského kódu
                    try {
                        eval(e.data.code);
                        postMessage({
                            stdOut: stdout,
                            stdErr: stderr,
                            success: true
                        });
                    } catch (err) {
                        postMessage({
                            stdOut: stdout,
                            stdErr: err.toString(),
                            success: false
                        });
                    }
                } catch (err) {
                    postMessage({
                        stdOut: "",
                        stdErr: "Execution error: " + err.toString(),
                        success: false
                    });
                }
            };
        `;

        var blob = new Blob([workerCode], { type: 'application/javascript' });
        var worker = new Worker(URL.createObjectURL(blob));
        
        worker.onmessage = function(e) {
            var result = e.data;
            unityInstance.SendMessage(jsCallbackObj, jsCallbackMethod, JSON.stringify(result));
            worker.terminate();
        };
        
        worker.postMessage({ code: jsCode });
    }
});