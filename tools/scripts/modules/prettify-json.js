function init(state, opts) {
    var spacer = "";
    if (opts.useTabs) {
        spacer = "\t";
    } else {
        for (var i = 0; i < opts.tabSize; ++i) {
            spacer += " ";
        }
    }
    for (var i = 0; i < 20; ++i) {
        var spacing = "";
        for(var n = 0; n < i; ++n) {
            spacing += spacer;
        }
        state.spacing[i] = spacing;
    }
}

function getSpacing(state, opts) {
    return state.spacing[state.indent];
}

function beginScope(state, opts, str, inline) {
    var res = str + (inline ? "" : "\n");
    state.indent++;
    return res;
}

function endScope(state, opts, str, inline) {
    state.indent--;
    return (inline ? "" : getSpacing(state, opts)) + str;
}

function processValue(state, opts, value, inline) {
    var res = "";
    if (value instanceof Array) {
        res += beginScope(state, opts, "[", false);
        var values = [];
        for (var i = 0; i < value.length; ++i) {
            const innerBody = getSpacing(state, opts) + processValue(state, opts, value[i], inline);
            values.push(innerBody);
        }
        if (values.length > 0) {
            res += values.join(",\n") + "\n";
        }
        res += endScope(state, opts, "]", false);
    }
    else if (value instanceof Object) {
        res += beginScope(state, opts, inline ? "{ " : "{", inline);

        var values = [];
        for (const key of Object.keys(value)) {
            const inlineChildren = opts.forceInline.includes(key);
            const innerBody = (inline ? "" : getSpacing(state, opts)) + JSON.stringify(key) + ": " + processValue(state, opts, value[key], inlineChildren);
            values.push(innerBody);
        }
        if (values.length > 0) {
            res += values.join(inline ? ", " : ",\n") + (inline ? "" : "\n");
        }
        res += endScope(state, opts, inline ? " }" : "}", inline);
    }
    else {
        res += JSON.stringify(value);
    }
    return res;
}

function prettifyJSON(value, opts) {
    var state = {
        indent: 0,
        spacing: [],
    }
    if (opts == null || opts == undefined) {
        opts = {};
    }
    if (!('forceInline' in opts)) {
        opts.forceInline = [];
    }
    if (!('useTabs' in opts)) {
        opts.useTabs = false;
    }
    if (!('tabSize' in opts)) {
        opts.tabSize = 4;
    }
    init(state, opts);
    return processValue(state, opts, value, false) +
        "\n" /* Some editors like adding newlines so lets just keep it that way */;
}

module.exports = { prettifyJSON };