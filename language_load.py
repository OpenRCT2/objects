#!/usr/bin/env python3

import argparse
import glob
import json
import re

# Command line arguments.
parser = argparse.ArgumentParser(description='Imports translations into OpenRCT2\'s JSON objects.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
parser.add_argument('-o', '--objects', default="objects", help='JSON objects directory')
parser.add_argument('-f', '--fallback', default="en-GB", help='Fallback language to check against')
parser.add_argument('-i', '--input', help='Translation dump file to import from', required=True)
parser.add_argument('-l', '--language', help='Language that is being translated, e.g. ja-JP', required=True)
args = parser.parse_args()

language_to_import = args.language
fallback_language = args.fallback

in_file = open(args.input)
strings_by_object = json.load(in_file)
in_file.close()

class LessVerboseJSONEncoder(json.JSONEncoder):
    def iterencode(self, o, _one_shot=False):
        list_lvl = 0
        for s in super(LessVerboseJSONEncoder, self).iterencode(o, _one_shot=_one_shot):
            if s.startswith('['):
                list_lvl += 1
                s = re.sub(r'\n\s*', '', s).strip()
            elif 0 < list_lvl:
                s = re.sub(r'\n\s*', ' ', s).strip()
                if s and s[-1] == ',':
                    s = s[:-1] + self.item_separator
                elif s and s[-1] == ':':
                    s = s[:-1] + self.key_separator
            if s.endswith(']'):
                list_lvl -= 1
            yield s

for filename in glob.iglob(args.objects + '/**/*.json', recursive=True):
    file = open(filename)
    data = json.load(file)
    file.close()

    if not 'strings' in data:
        print("No strings in " + data['id'] + " -- skipping")
        continue

    if not data['id'] in strings_by_object:
        print("No translations for " + data['id'] + " in dump file -- skipping")
        continue

    updated = False
    for string_key in data['strings']:

        if not string_key in strings_by_object[data['id']]:
            print("No translation for " + data['id'] + " string '" + string_key + "' in dump file -- skipping")
            continue

        if not fallback_language in data['strings'][string_key]:
            print("No en-GB reference for " + data['id'] + " string '" + string_key + "' in dump file -- probably shouldn't exist; skipping")
            continue

        if not language_to_import in data['strings'][string_key]:
            if strings_by_object[data['id']][string_key] == data['strings'][string_key][fallback_language]:
                # TODO: Is this desirable behaviour?
                print("Translation for " + data['id'] + " string '" + string_key + "' is identical to en-GB -- skipping")
                continue

            print("Adding " + data['id'] + " string '" + string_key + "'")
            data['strings'][string_key][language_to_import] = strings_by_object[data['id']][string_key]
            updated = True
        else:
            if strings_by_object[data['id']][string_key] == data['strings'][string_key][language_to_import]:
                print("Translation for " + data['id'] + " string '" + string_key + "' has not changed -- skipping")
                continue

            print("Updating " + data['id'] + " string '" + string_key + "'")
            data['strings'][string_key][language_to_import] = strings_by_object[data['id']][string_key]
            updated = True

    if updated:
        file = open(filename, "w")
        json.dump(data, file, indent=4, separators=(',', ': '), ensure_ascii=False, cls=LessVerboseJSONEncoder)
        file.write("\n")
        file.close()
