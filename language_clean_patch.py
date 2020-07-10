#!/usr/bin/env python3

from unidiff import PatchSet
from unidiff.constants import (
    # DEFAULT_ENCODING,
    LINE_TYPE_ADDED,
    LINE_TYPE_CONTEXT,
    LINE_TYPE_EMPTY,
    LINE_TYPE_REMOVED,
    # LINE_TYPE_NO_NEWLINE,
    # LINE_VALUE_NO_NEWLINE,
    # RE_HUNK_BODY_LINE,
    # RE_HUNK_EMPTY_BODY_LINE,
    # RE_HUNK_HEADER,
    # RE_RENAME_SOURCE_FILENAME,
    # RE_RENAME_TARGET_FILENAME,
    # RE_SOURCE_FILENAME,
    # RE_TARGET_FILENAME,
    # RE_NO_NEWLINE_MARKER,
    # RE_BINARY_DIFF,
)

import os
import sys

def clean_patch(filename, language):
    patch = PatchSet.from_filename(filename)
    files_to_be_removed = []
    for i, file in enumerate(patch):
        hunks_to_be_removed = []
        for j, hunk in enumerate(file):
            prev_line = None
            for k, line in enumerate(hunk):
                # Is this line modifying anything?
                if line.is_added or line.is_removed:
                    # We're definitely keeping lines that have something to do
                    # with the target language.
                    if language in line.value:
                        continue

                    # Is this line a removal followed by an addition of the same language,
                    # in turn followed by the target language?
                    if line.is_removed and hunk[k + 1].is_added and \
                        language in hunk[k + 2].value:
                        # Check languages match
                        continue

                    # Indeed, is the previous line a removal and the next line an addition
                    # of the target language?
                    elif hunk[k - 1].is_removed and line.is_added and \
                        language in hunk[k + 1].value:
                        continue

                    # Otherwise, exclude this change from the patch.
                    else:
                        line.line_type = LINE_TYPE_EMPTY


                prev_line = k

            # Is this hunk still modifying anything?
            # If not, we'll drop it after iterating everything.
            if not hunk.added and not hunk.removed:
                hunks_to_be_removed.append(j)

        # Any hunks to be removed?
        # (In reverse order, as we'll be reindexing.)
        if len(hunks_to_be_removed):
            for j in reversed(hunks_to_be_removed):
                del file[j]

        if not file.added and not file.removed:
            files_to_be_removed.append(i)

    # Any files to be removed?
    # (In reverse order, as we'll be reindexing.)
    if len(files_to_be_removed):
        for i in reversed(files_to_be_removed):
            del patch[i]

    print(patch)

    return patch



if __name__ == "__main__":
    clean_patch("patch.diff", "nl-NL")
