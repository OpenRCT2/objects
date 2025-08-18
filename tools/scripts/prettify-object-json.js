// To use this script do:
// node ./tools/scripts/prettify-object-json.js PATH_TO_OBJECTS_REPO

const fs = require("fs");
const path = require("path");
const utils = require("./modules/utils.js");
const { prettifyJSON } = require("./modules/prettify-json");

const formatOptions = {
    "forceInline": ["images", "noCsgImages"],
    "useTabs": false,
    "tabSize": 4,
};

const args = process.argv
if (args.length < 3) {
    console.error("ERROR: Expected <path_or_file>");
    process.exit(-1);
}

const objectsPathOrFile = args[2];

function formatObjectFile(fullPath) {
    console.info(`Formatting: ${fullPath}`);
    const fileData = fs.readFileSync(fullPath, "utf8");
    const jsonData = JSON.parse(fileData);
    const prettified = prettifyJSON(jsonData, formatOptions);
    return prettified;
}

const fileInfo = fs.lstatSync(objectsPathOrFile);
if (fileInfo.isDirectory()) {
    // Format entire directory.
    const objectFiles = utils.getObjectFiles(objectsPathOrFile);
    objectFiles.forEach(filePath => {
        const fullPath = path.join(objectsPathOrFile, filePath);
        const prettified = formatObjectFile(fullPath);
        fs.writeFileSync(fullPath, prettified, "utf8");
    });
} else if(fileInfo.isFile()) {
    // Format single file.
    const prettified = formatObjectFile(objectsPathOrFile);
    //console.log(prettified);
    fs.writeFileSync(objectsPathOrFile, prettified, "utf8");
} else {
    console.error("ERROR: Invalid argument provided, argument is not a file or directory.");
    process.exit(-1);
}
