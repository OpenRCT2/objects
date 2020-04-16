#!/usr/bin/env python3
"""
Imports translations into OpenRCT2's JSON objects given a language and an input file
"""

import argparse
import glob
import json
import os
import re
import sys

SUPPORTED_LANGUAGES = ["ar-EG", "ca-ES", "cs-CZ", "da-DK", "de-DE", "en-GB", "en-US", "es-ES",\
                       "fi-FI", "fr-FR", "hu-HU", "it-IT", "ja-JP", "ko-KR", "nb-NO", "nl-NL",\
                       "pl-PL", "pt-BR", "ru-RU", "sv-SE", "tr-TR", "zh-CN", "zh-TW"]

def dir_path(string):
    """ Checks for a valid dir_path """
    if os.path.isdir(string):
        return string
    raise NotADirectoryError(string)

def get_arg_parser():
    """ Command line arguments """
    parser = argparse.ArgumentParser(description=\
                                     'Imports translations into OpenRCT2\'s JSON objects.',
                                     formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument('-o', '--objects', default="objects", help='JSON objects directory')
    parser.add_argument('-f', '--fallback', default="en-GB",\
                        help='Fallback language to check against', choices=SUPPORTED_LANGUAGES)
    input_group = parser.add_mutually_exclusive_group(required=True)
    input_group.add_argument('-i', '--input', help='Translation dump file to import from')
    input_group.add_argument('-d', '--dir', type=dir_path,\
                             help='Directory with translation dump files to import from')
    language_group = parser.add_mutually_exclusive_group(required=True)
    language_group.add_argument('-l', '--language', choices=SUPPORTED_LANGUAGES,\
                                help='Language that is being translated, e.g. ja-JP')
    language_group.add_argument('-a', '--all-languages', action='store_true')
    parser.add_argument('-v', '--verbose', action='store_true', default=False,\
                        help='Maximize information printed on screen')
    return parser

def parse_required_switch_pairs(args):
    """ Make sure only valid switch pairs are used """
    single_language = args.language and args.input
    all_languages = args.all_languages and args.dir
    if not single_language and not all_languages:
        print(f"Invalid switch pair. Use '-l <lang> -i <file>' or '-a -d <target_dir>")
        sys.exit()

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
        print(f"No strings in {data['id']} -- skipping")
    return False

def is_object_translated(verbose, object_id, strings_by_object):
    """ Checks if there are any translations for the given file id """
    if object_id in strings_by_object:
        return True
    if verbose:
        print(f"No translations for {object_id} in dump file -- skipping")
    return False

def is_key_translated(verbose, obj_id, key, object_json):
    """ Checks if the given key of an object is translated in input JSON """
    if key in object_json:
        return True
    if verbose:
        print(f"No translation for {obj_id} string '{key}' in dump file -- skipping")
    return False

def fallback_key_exists(verbose, fallback_language, obj_id, key, object_json):
    """ Checks if there was source language to be translated from """
    if fallback_language in object_json:
        return True
    if verbose:
        print(f"No {fallback_language} reference for {obj_id} string '{key}'"
              f" in dump file -- probably shouldn't exist; skipping")
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
            print(f"Translation for {obj_id} string '{key}' has not changed -- skipping")
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
        if previously_translated:
            translation = data['strings'][string_key][target_lang]
        else:
            translation = data['strings'][string_key][ref_lang]
        target_string = strings_by_object[obj_id][string_key]
        if translation_changed(verbose, obj_id, string_key, translation, target_string):
            print(f"{target_lang}: Updating {obj_id} string '{string_key}'")
            data['strings'][string_key][target_lang] = strings_by_object[obj_id][string_key]
            updated = True

    if updated:
        update_object_translation(filename, data, file)

def load_translation(language, fallback_language, verbose, input_filename, objects):
    """ Load translation from the given file into each object JSON """
    in_file = open(input_filename, encoding="utf8")
    strings_by_object = json.load(in_file)
    in_file.close()

    for filename in glob.iglob(objects + '/**/*.json', recursive=True):
        update_translation(language, fallback_language, verbose, filename, strings_by_object)

def load_translations():
    """ Load translations from the given files into each object JSON """
    parser = get_arg_parser()
    args = parser.parse_args()
    parse_required_switch_pairs(args)
    languages_to_extract = SUPPORTED_LANGUAGES if args.all_languages else [args.language]
    for lang in languages_to_extract:
        read_file_name = f'{args.dir}/{lang}.json' if args.all_languages else args.input
        load_translation(lang, args.fallback, args.verbose, read_file_name, args.objects)

if __name__ == "__main__":
    load_translations()
