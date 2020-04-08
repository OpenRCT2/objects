#!/usr/bin/env python3

import argparse
import glob
import json
# from pprint import pprint

supported_languages = ["ar-EG", "ca-ES", "cs-CZ", "da-DK", "de-DE", "en-GB", "en-US", "es-ES", "fi-FI",\
                       "fr-FR", "hu-HU", "it-IT", "ja-JP", "ko-KR", "nb-NO", "nl-NL", "pl-PL", "pt-BR",\
                       "ru-RU", "sv-SE", "tr-TR", "zh-CN", "zh-TW"]

# Command line arguments.
parser = argparse.ArgumentParser(description='Dump translations for OpenRCT2\'s JSON objects.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
parser.add_argument('-o', '--objects', default="objects", help='JSON objects directory')
parser.add_argument('-f', '--fallback', default="en-GB", help='Fallback language to check against', choices=supported_languages)
parser.add_argument('-d', '--dumpfile', help='Translation file to export to', required=True)
parser.add_argument('-l', '--language', help='Language that should be extracted, e.g. ja-JP', required=True, choices=supported_languages)
args = parser.parse_args()

language_to_extract = args.language
fallback_language = args.fallback
dump_file_name = args.dumpfile

strings_by_object = {}

for filename in glob.iglob(args.objects + '/**/*.json', recursive=True):
    with open(filename, encoding="utf8") as file:
        data = json.load(file)

        if not 'strings' in data:
            print("No strings in " + data['id'] + ", skipping")
            continue

        for string_key in data['strings']:
            if not data['id'] in strings_by_object:
                strings_by_object[data['id']] = {}

            if language_to_extract in data['strings'][string_key]:
                print("Found existing translation for " + data['id'])
                strings_by_object[data['id']][string_key] = data['strings'][string_key][language_to_extract]
            elif fallback_language in data['strings'][string_key]:
                print("No existing translation for " + data['id'] + " yet, using English")
                strings_by_object[data['id']][string_key] = data['strings'][string_key][fallback_language]
            else:
                print("No existing translation for " + data['id'] + " yet, but no English string either -- skipping")
                # pprint(data)

out = open(dump_file_name, "w", encoding="utf8")
json.dump(strings_by_object, out, indent=4, ensure_ascii=False, separators=(',', ': '))
out.write("\n")
out.close()
