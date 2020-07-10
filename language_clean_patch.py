#!/usr/bin/env python3

from unidiff import PatchSet
from unidiff.constants import LINE_TYPE_EMPTY

class PatchCleaner:
    """
    Cleans a given unified diff such that only lines matching the language parameter are included,
    as well as any preceding lines that may be giving way to them (e.g. by adding a trailing comma).
    """

    def __init__(self, filename, language):
        self.patch = PatchSet.from_filename(filename)
        self.language = language
        self.clean_patch()

    def __str__(self):
        return str(self.patch)

    def clean_patch(self):
        """ Cleans the patch, removing irrelevant files. """

        files_to_be_removed = []
        for i, file in enumerate(self.patch):
            self.clean_file(file)

            if not file.added and not file.removed:
                files_to_be_removed.append(i)

        # Any files to be removed?
        # (In reverse order, as we'll be reindexing.)
        if len(files_to_be_removed):
            for i in reversed(files_to_be_removed):
                del self.patch[i]

    def clean_file(self, file):
        """ Cleans one particular file in the patch set, removing empty hunks. """

        hunks_to_be_removed = []
        for j, hunk in enumerate(file):
            self.clean_hunk(hunk)

            # Is this hunk still modifying anything?
            # If not, we'll drop it after iterating everything.
            if not hunk.added and not hunk.removed:
                hunks_to_be_removed.append(j)

        # Any hunks to be removed?
        # (In reverse order, as we'll be reindexing.)
        if len(hunks_to_be_removed):
            for j in reversed(hunks_to_be_removed):
                del file[j]

    def clean_hunk(self, hunk):
        """ Cleans one particular hunk in the patch set, removing irrelevant lines. """

        prev_line = None
        for k, line in enumerate(hunk):
            # Is this line modifying anything?
            if line.is_added or line.is_removed:
                # We're definitely keeping lines that have something to do
                # with the target language.
                if self.language in line.value:
                    continue

                # Is this line a removal followed by an addition of the same language,
                # in turn followed by the target language?
                if line.is_removed and hunk[k + 1].is_added and \
                    self.language in hunk[k + 2].value:
                    # TODO: Check languages match -- good enough a heuristic for now.
                    continue

                # Indeed, is the previous line a removal and the next line an addition
                # of the target language?
                elif hunk[k - 1].is_removed and line.is_added and \
                    self.language in hunk[k + 1].value:
                    # TODO: Check languages match -- good enough a heuristic for now.
                    continue

                # Otherwise, exclude this change from the patch.
                else:
                    line.line_type = LINE_TYPE_EMPTY

            prev_line = k


if __name__ == "__main__":
    patch = PatchCleaner("patch.diff", "nl-NL")
    print(patch)
