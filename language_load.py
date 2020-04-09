#!/usr/bin/env python3
"""
Imports translations into OpenRCT2\'s JSON objects given a language and an input file
"""

import argparse
import glob
import json
import re

SUPPORTED_LANGUAGES = ["ar-EG", "ca-ES", "cs-CZ", "da-DK", "de-DE", "en-GB", "en-US", "es-ES",\
                       "fi-FI", "fr-FR", "hu-HU", "it-IT", "ja-JP", "ko-KR", "nb-NO", "nl-NL",\
                       "pl-PL", "pt-BR", "ru-RU", "sv-SE", "tr-TR", "zh-CN", "zh-TW"]

def get_arg_parser():
    """ Command line arguments """
    parser = argparse.ArgumentParser(description=\
                                     'Imports translations into OpenRCT2\'s JSON objects.',
                                     formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument('-o', '--objects', default="objects", help='JSON objects directory')
    parser.add_argument('-f', '--fallback', default="en-GB",\
                        help='Fallback language to check against', choices=SUPPORTED_LANGUAGES)
    parser.add_argument('-i', '--input', help='Translation dump file to import from', required=True)
    parser.add_argument('-l', '--language', help='Language that is being translated, e.g. ja-JP',\
                        required=True, choices=SUPPORTED_LANGUAGES)
    parser.add_argument('-v', '--verbose', action='store_true', default=False,\
                        help='Maximize information printed on screen')
    return parser

class LessVerboseJSONEncoder(json.JSONEncoder):
    """ Custom JSON Encoder that reduces output verbosity """
    def iterencode(self, o, _one_shot=False):
        list_lvl = 0
        for string in super(LessVerboseJSONEncoder, self).iterencode(o, _one_shot=_one_shot):
            if string.startswith('['):
                list_lvl += 1
                string = re.sub(r'\n\s*', '', string).strip()
            elif list_lvl > 0:
                string = re.sub(r'\n\s*', ' ', string).strip()
                if string and string[-1] == ',':
                    string = string[:-1] + self.item_separator
                elif string and string[-1] == ':':
                    string = string[:-1] + self.key_separator
            if string.endswith(']'):
                list_lvl -= 1
            yield string

def update_object_translation(filename, data, file):
    """ Update target object translation """
    file = open(filename, "w", encoding="utf8")
    json.dump(data, file, indent=4, separators=(',', ': '),
              ensure_ascii=False, cls=LessVerboseJSONEncoder)
    file.write("\n")
    file.close()

def translatable(verbose, data):
    """ Checks if the file has string content """
    if 'strings' in data:
        return True
    if verbose:
        print("No strings in " + data['id'] + " -- skipping")
    return False

def is_object_translated(verbose, object_id, strings_by_object):
    """ Checks if there are any translations for the given file id """
    if object_id in strings_by_object:
        return True
    if verbose:
        print("No translations for " + object_id + " in dump file -- skipping")
    return False

def is_key_translated(verbose, obj_id, key, object_json):
    """ Checks if the given key of an object is translated in input JSON """
    if key in object_json:
        return True
    if verbose:
        print("No translation for " + obj_id + " string '" + key + "' in dump file -- skipping")
    return False

def fallback_key_exists(verbose, fallback_language, obj_id, key, object_json):
    """ Checks if there was source language to be translated from """
    if fallback_language in object_json:
        return True
    if verbose:
        print("No en-GB reference for " + obj_id + " string '" + key + \
              "' in dump file -- probably shouldn't exist; skipping")
    return False

def translation_existed(target_lang, translations):
    """ Checks if the translation for a given key existed """
    if target_lang in translations:
        return True
    return False

def translation_changed(verbose, obj_id, key, translation, target_string):
    """ Checks if the translation for a given key changed """
    if target_string == translation:
        if verbose:
            print("Translation for " + obj_id + " string '" + key + "' has not changed -- skipping")
        return False
    return True

def update_translation(target_lang, ref_lang, verbose, filename, strings_by_object):
    """ Updates an object translation, if applicable """
    file = open(filename, encoding="utf8")
    data = json.load(file)
    file.close()
    obj_id = data['id']

    if not translatable(verbose, data):
        return
    if not is_object_translated(verbose, obj_id, strings_by_object):
        return

    updated = False
    for string_key in data['strings']:

        if not is_key_translated(verbose, obj_id, string_key, strings_by_object[obj_id]):
            continue

        if not fallback_key_exists(verbose, ref_lang, string_key, obj_id,
                                   data['strings'][string_key]):
            continue

        previously_translated = translation_existed(target_lang, data['strings'][string_key])
        translation_needs_update = False
        if previously_translated:
            translation_needs_update = translation_changed(verbose, obj_id, string_key,\
                                          data['strings'][string_key][target_lang],\
                                          strings_by_object[obj_id][string_key])
        if not previously_translated or translation_needs_update:
            print("Updating " + obj_id + " string '" + string_key + "'")
            data['strings'][string_key][target_lang] = strings_by_object[obj_id][string_key]
            updated = True

    if updated:
        update_object_translation(filename, data, file)

def load_translations():
    """ Load translations from the given file into each object JSON """
    parser = get_arg_parser()
    args = parser.parse_args()

    in_file = open(args.input, encoding="utf8")
    strings_by_object = json.load(in_file)
    in_file.close()

    for filename in glob.iglob(args.objects + '/**/*.json', recursive=True):
        update_translation(args.language, args.fallback, args.verbose, filename, strings_by_object)

if __name__ == "__main__":
    load_translations()
