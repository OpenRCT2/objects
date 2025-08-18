const path = require("path")
const fs = require("fs");

function getFiles(path) {
    const files = []
    for (const file of fs.readdirSync(path)) {
        const fullPath = path + '/' + file
        if(fs.lstatSync(fullPath).isDirectory())
            getFiles(fullPath).forEach(x => files.push(file + '/' + x))
        else files.push(file)
    }
    return files
}

function getObjectFiles(filePath) {
    var files = getFiles(filePath);
    return files.filter(file => {
        return path.extname(file) === ".json";
    });
}

module.exports = { getObjectFiles };
