const parserJS = require("./lang-js-parse")
const parserTxt = require("./lang-txt-parse")
const utils = require("./utils");
const path = require("node:path")
const fs = require("fs");

const SupportedLanguages = [
    "ar-EG", "ca-ES", "cs-CZ", "da-DK", "de-DE", "en-GB", "en-US", "eo-ZZ",
    "es-ES", "fi-FI", "fr-CA", "fr-FR", "gl-ES", "hu-HU", "it-IT", "ja-JP",
    "ko-KR", "nb-NO", "nl-NL", "pl-PL", "pt-BR", "ru-RU", "sv-SE", "tr-TR",
    "uk-UA", "vi-VN", "zh-CN", "zh-TW"
];

const Language = {
    ArEG: "ar-EG", 
    CaES: "ca-ES", 
    CsCZ: "cs-CZ", 
    DaDK: "da-DK", 
    DeDE: "de-DE", 
    EnGB: "en-GB", 
    EnUS: "en-US", 
    EoZZ: "eo-ZZ",
    EsES: "es-ES", 
    FiFI: "fi-FI", 
    FrCA: "fr-CA", 
    FrFR: "fr-FR", 
	GlES: "gl-ES",
    HuHU: "hu-HU", 
    ItIT: "it-IT", 
    JaJP: "ja-JP", 
    KoKR: "ko-KR", 
    NbNO: "nb-NO",
    NlNL: "nl-NL", 
    PlPL: "pl-PL", 
    PtBR: "pt-BR", 
    RuRU: "ru-RU", 
    SvSE: "sv-SE", 
    TrTR: "tr-TR", 
    UkUA: "uk-UA", 
    ViVN: "vi-VN", 
    ZhCN: "zh-CN", 
    ZhTW: "zh-TW"
};

function parseLanguage(file) {
    var ext = path.extname(file);
    var data = null;
    if(ext == ".json") {
        data = parserJS.parse(file);
    }
    else if(ext == ".txt") {
        data = parserTxt.parse(file);
    }
    if(data == null) {
        throw "Unable to parse file";
    }
    return data;
}

function parseObject(file) {
    var res = JSON.parse(fs.readFileSync(file, "utf8"));
    return res;
}

module.exports = { 
    Language,
    utils,
    parseLanguage,
    parseObject
}
