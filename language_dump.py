#!/usr/bin/env python3
"""
Dump translations for OpenRCT2's JSON objects given a language and a file
"""

import argparse
import glob
import json
import os
import sys
# from pprint import pprint

from languages import SUPPORTED_LANGUAGES

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
    target_group = parser.add_mutually_exclusive_group(required=True)
    target_group.add_argument('-t', '--target_dir', help='Directory to dump translation files',\
                              type=dir_path)
    target_group.add_argument('-d', '--dumpfile', help='Translation file to export to')
    language_group = parser.add_mutually_exclusive_group(required=True)
    language_group.add_argument('-l', '--language', choices=SUPPORTED_LANGUAGES,\
                                help='Language that should be extracted, e.g. ja-JP')
    language_group.add_argument('-a', '--all-languages', action='store_true')
    parser.add_argument('-v', '--verbose', action='store_true', default=False,\
                        help='Maximize information printed on screen')
    parser.add_argument('-m', '--missing', action='store_true', default=False,\
                        help='Only dump keys with missing translation')
    return parser

def parse_required_switch_pairs(args):
    """ Make sure only valid switch pairs are used """
    single_language = args.language and args.dumpfile
    all_languages = args.all_languages and args.target_dir
    if not single_language and not all_languages:
        print(f"Invalid switch pair. Use '-l <lang> -d <file>' or '-a -t <target_dir>")
        sys.exit()

def add_key_value(strings_by_object, obj_id, key, value):
    """ Add key-value pair to id entry in dict, initializing id if not valid """
    if not obj_id in strings_by_object:
        strings_by_object[obj_id] = {}
    strings_by_object[obj_id][key] = value

def extract_translations(language_to_extract, args, filename, strings_by_object):
    """ Read JSON file and extracts translations for a given language """

    fallback_language = args.fallback
    verbose = args.verbose
    reference_str_count = 0
    translated_str_count = 0

    with open(filename, encoding="utf8") as file:
        data = json.load(file)

        if not 'strings' in data:
            print(f"No strings in {data['id']}, skipping")
            return (translated_str_count, reference_str_count)

        for string_key in data['strings']:
            if fallback_language in data['strings'][string_key]:
                fallback_translation = data['strings'][string_key][fallback_language]
            else:
                fallback_translation = ""

            if language_to_extract in data['strings'][string_key]:
                if verbose:
                    print(f"Found existing translation for {data['id']}")
                current_translation = data['strings'][string_key][language_to_extract]
                ref_key = f'reference-{string_key}'
                if not args.missing:
                    add_key_value(strings_by_object, data['id'], ref_key, fallback_translation)
                    add_key_value(strings_by_object, data['id'], string_key, current_translation)
                reference_str_count += 1
                translated_str_count += 1
            elif fallback_language in data['strings'][string_key]:
                if verbose:
                    print(f"No existing translation for {data['id']} yet,"
                          f" using {fallback_language}")
                ref_key = f'reference-{string_key}'
                add_key_value(strings_by_object, data['id'], ref_key, fallback_translation)
                add_key_value(strings_by_object, data['id'], string_key, fallback_translation)
                reference_str_count += 1
            else:
                if verbose:
                    print(f"No existing translation for {data['id']} yet,"
                          f" but no {fallback_language} string either -- skipping")
    return (translated_str_count, reference_str_count)

def dump_translation(language_to_extract, args, dump_file_name):
    """ Dump a language translation for OpenRCT2's JSON objects """
    strings_by_object = {}
    reference_str_count = 0
    translated_str_count = 0

    for filename in glob.iglob(args.objects + '/**/*.json', recursive=True):
        obj_translations, ref_obj_translation =\
            extract_translations(language_to_extract, args, filename, strings_by_object)
        reference_str_count += ref_obj_translation
        translated_str_count += obj_translations

    translation_progress = round(100 * translated_str_count / reference_str_count, 2)
    print(f'{language_to_extract}: {translation_progress}% completeness')

    # Sort output by object ids (dict keys)
    strings_by_object = dict(sorted(strings_by_object.items()))

    out = open(dump_file_name, "w", encoding="utf8")
    json.dump(strings_by_object, out, indent=4, ensure_ascii=False, separators=(',', ': '))
    out.write("\n")
    out.close()

def dump_translations():
    """ Dump translations for OpenRCT2's JSON objects """
    parser = get_arg_parser()
    args = parser.parse_args()
    parse_required_switch_pairs(args)
    languages_to_extract = SUPPORTED_LANGUAGES if args.all_languages else [args.language]
    for lang in languages_to_extract:
        dump_file_name = f'{args.target_dir}/{lang}.json' if args.all_languages else args.dumpfile
        dump_translation(lang, args, dump_file_name)

if __name__ == "__main__":
    dump_translations()
