// To use this script do:
// node ./tools/scripts/update-localisation.js PATH_TO_LOCALISATION_REPO PATH_TO_OBJECTS_REPO

const fs = require("node:fs");
const path = require("node:path");
const langTools = require("./modules/lang-tools.js");
const utils = require("./modules/utils.js")
const { prettifyJSON } = require("./modules/prettify-json");

const formatOptions = {
    "forceInline": ["images", "noCsgImages"],
    "useTabs": false,
    "tabSize": 4,
};

function parseObjectLanguages(localisationRootPath) {
    var res = [];
    for (let key in langTools.Language) {
        const langId = langTools.Language[key];
        const langPath = path.join(localisationRootPath, "objects", langId + ".json");
        const langData = langTools.parseLanguage(langPath, langId);
        res[langId] = langData;
    }
    return res;
}

function mergeLanguageData(objectData, langData, lang) {
    // Create a copy.
    objectData = Object.assign({}, objectData);
    const objectId = objectData["id"];

    var objectLangData = langData[objectId];
    if(objectLangData === undefined)
    {
        console.warn(`Missing object group '${objectId}' in language '${lang}'`);
        return objectData;
    }

    var langStrings = objectData["strings"];
    for(var key in langStrings) {
        const translation = objectLangData[key];
        if(translation === undefined) {
            console.warn(`Missing key ${key} in ${lang} data`);
            continue;
        }
        const referenceTranslation = objectLangData[`reference-${key}`];
        if(lang != langTools.Language.EnGB && lang != langTools.Language.EnUS && referenceTranslation == translation)
        {
            //console.warn(`Warning: Translation ${key} is same as reference for non-english language: ${lang}, skipping.`);
            continue;
        }
        else {
            const oldEntry = langStrings[key][lang];
            if(oldEntry === undefined) {
                //console.log(`Created key ${lang}:${key} -> '${translation}'`);
                langStrings[key][lang] = translation;
            } 
            else if(oldEntry != translation) {
                console.log(`Updated key ${lang}:${key} from '${oldEntry}' -> '${translation}'`);
                langStrings[key][lang] = translation;
            }
            else if(oldEntry == translation) {
                //console.log(`Translation identical for key ${lang}:${key} -> ${translation}`);
            }
        }
    }

    return objectData;
}

function processObjectFile(objectsLangData, objectFilePath) {
    console.info(`Processing Object ${objectFilePath}`);
    
    var objectData = langTools.parseObject(objectFilePath);
    if (objectData == null) {
        throw `Failed to read object data: ${objectFilePath}`;
    }

    // Update all keys.
    for (let key in langTools.Language) {
        const langId = langTools.Language[key];
        const langData = objectsLangData[langId];
        objectData = mergeLanguageData(objectData, langData, langId);
    }

    // Remove en-US when its same as en-GB
    for(let nameKey in objectData["strings"]) {
        var stringEntries = objectData["strings"][nameKey];
        if(stringEntries[langTools.Language.EnUS] !== undefined && stringEntries[langTools.Language.EnUS] == stringEntries[langTools.Language.EnGB]) {
            delete stringEntries[langTools.Language.EnUS];
        }
    }

    // Save to file.
    var jsonStr = prettifyJSON(objectData, formatOptions);
    fs.writeFileSync(objectFilePath, jsonStr, "utf8");
}

function mergeLocalisationToObjects(localisationRootPath, objectsRootPath) {
    const objectsPath = path.join(objectsRootPath, "objects");

    const objectFiles = utils.getObjectFiles(objectsPath);
    if (objectFiles.length == 0) {
        throw "No object files found";
    }

    const objectsLangData = parseObjectLanguages(localisationRootPath);
    objectFiles.forEach(objectFile => processObjectFile(objectsLangData, path.join(objectsPath, objectFile)));
}

const args = process.argv
if (args.length < 4) {
    console.error("ERROR: Expected arguments: <path_to_localisation> <path_to_objects>");
    process.exit(-1);
}

const localisationPath = args[2];
if (!fs.lstatSync(localisationPath).isDirectory()) {
    console.error("ERROR: Argument provided for localisation is not a directory.");
    process.exit(-1);
}

const objectsPath = args[3];
if (!fs.lstatSync(objectsPath).isDirectory()) {
    console.error("ERROR: Argument provided for objects is not a directory.");
    process.exit(-1);
}

mergeLocalisationToObjects(localisationPath, objectsPath);