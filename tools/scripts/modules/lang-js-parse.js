const fs = require("fs");

function parse(file) {
    var res = JSON.parse(fs.readFileSync(file, "utf8"));
    return res;
}

module.exports = { parse }