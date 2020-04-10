#!/usr/bin/env python3
"""
Dump translations for OpenRCT2's JSON objects given a language and a file
"""

import argparse
import glob
import json
import os
# from pprint import pprint

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
    parser = argparse.ArgumentParser(description='Dump translations for OpenRCT2\'s JSON objects.',\
                                     formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument('-o', '--objects', default="objects", help='JSON objects directory')
    parser.add_argument('-f', '--fallback', default="en-GB",\
                        help='Fallback language to check against', choices=SUPPORTED_LANGUAGES)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument('-t', '--target_dir', help='Directory to dump translation files',\
                       type=dir_path)
    group.add_argument('-d', '--dumpfile', help='Translation file to export to')
    parser.add_argument('-l', '--language', help='Language that should be extracted, e.g. ja-JP',\
                        required=True, choices=SUPPORTED_LANGUAGES)
    parser.add_argument('-v', '--verbose', action='store_true', default=False,\
                        help='Maximize information printed on screen')
    return parser

def extract_translations(language_to_extract, fallback_language, verbose, filename,\
                         strings_by_object):
    """ Read JSON file and extracts translations for a given language """

    reference_str_count = 0
    translated_str_count = 0
    with open(filename, encoding="utf8") as file:
        data = json.load(file)

        if not 'strings' in data:
            print(f"No strings in {data['id']}, skipping")
            return (translated_str_count, reference_str_count)

        for string_key in data['strings']:
            if not data['id'] in strings_by_object:
                strings_by_object[data['id']] = {}

            if language_to_extract in data['strings'][string_key]:
                if verbose:
                    print(f"Found existing translation for {data['id']}")
                current_translation = data['strings'][string_key][language_to_extract]
                strings_by_object[data['id']][string_key] = current_translation
                reference_str_count += 1
                translated_str_count += 1
            elif fallback_language in data['strings'][string_key]:
                print(f"No existing translation for {data['id']} yet, using {fallback_language}")
                fallback_translation = data['strings'][string_key][fallback_language]
                strings_by_object[data['id']][string_key] = fallback_translation
                reference_str_count += 1
            else:
                if verbose:
                    print(f"No existing translation for {data['id']} yet,"
                          f" but no {fallback_language} string either -- skipping")
    return (translated_str_count, reference_str_count)

def dump_translations():
    """ Dump translations for OpenRCT2's JSON objects """
    parser = get_arg_parser()
    args = parser.parse_args()
    language_to_extract = args.language
    fallback_language = args.fallback
    verbose = args.verbose
    strings_by_object = {}
    reference_str_count = 0
    translated_str_count = 0

    for filename in glob.iglob(args.objects + '/**/*.json', recursive=True):
        obj_translations, ref_obj_translation =\
            extract_translations(language_to_extract, fallback_language, verbose,\
                                 filename, strings_by_object)
        reference_str_count += ref_obj_translation
        translated_str_count += obj_translations

    translation_progress = round(100 * translated_str_count / reference_str_count, 2)
    print(f'{language_to_extract}: {translation_progress}% completeness')

    dump_file_name = args.dumpfile
    out = open(dump_file_name, "w", encoding="utf8")
    json.dump(strings_by_object, out, indent=4, ensure_ascii=False, separators=(',', ': '))
    out.write("\n")
    out.close()

if __name__ == "__main__":
    dump_translations()
