---
name: transcribe-pdf-resource
description: Transcribe a 5coy-archive PDF (scanned or text) into the markdown shown on its article page, OCR-ing locally when there's no text layer, then find and fix transcription errors. Also re-verifies existing hand-made markdown against its PDF. Use when an article has a PdfUrl but no ContentUrl, when asked to transcribe/OCR a PDF, or to check/verify/proof-read existing transcriptions.
---

# Transcribe and verify PDF resources

Many archive articles started life as scanned typewritten or photocopied pages. The web page shows the
markdown at `ContentUrl`; the original is the PDF at `PdfUrl`. This skill makes the markdown when it's
missing, and checks the markdown against the PDF when it already exists.

**Never commit.** Leave every change in the working tree for Joel to review (see CLAUDE.md).

Run everything from the repo root (in device_bash: `cd $HOME/mnt/5coy-archive`). Scripts live in
`.claude/skills/transcribe-pdf-resource/scripts/` (`$S` below). They need `python3`, `pdfinfo`,
`pdftotext`, `pdftoppm`, `pdfimages` (poppler-utils), `tesseract` and Pillow. `pyspellchecker` is optional but makes
the error checks much better: `pip3 install --user pyspellchecker`.

## 0. Getting the PDF

PDFs live in S3 under `/media/...`, not in the repo. `extract_pdf.py` looks for a local copy first, at
`.transcribe/media/<path after /media/>` (raw or URL-decoded name), or under `$FIVECOY_MEDIA_DIR` if Joel
has a local mirror. Failing that it tries to download from `https://www.5coy-cmc.org.uk/media/...` and
the CloudFront origin. Downloads are often blocked from the sandbox: if so, the script prints the exact
path to drop the file at. Ask Joel for the PDF (or a mirror folder) rather than guessing. You can also
pass a PDF path directly.

`.transcribe/` is the scratch area: downloaded PDFs, page images, extractions and crops are git-ignored.
Two files in it are committed and updated as you work: `.transcribe/lexicon.txt` (archive words the
dictionary doesn't know) and `.transcribe/verification-log.md` (written by `log_verification.py`).

## 1. See what needs doing

```bash
python3 $S/list_pdfs.py               # everything with a PdfUrl, incl. personnel.json
python3 $S/list_pdfs.py --todo        # NO-MD / MD-GONE: need a transcription
python3 $S/list_pdfs.py --unverified  # MD that has never been checked against its PDF
```

## 2. Extract

```bash
python3 $S/extract_pdf.py <reference>      # or a path/to/file.pdf
```

Per page it uses the embedded text layer if there is one, otherwise renders at 300 dpi and OCRs with
Tesseract. It writes `.transcribe/<reference>/`: `page-NN.png` (150 dpi page image), `page-NN.txt`,
`draft.txt` (all pages with `<!-- page N -->` markers), `extract.json` (method and mean confidence per
page, every low-confidence word) and `crops/` (the line around each low-confidence word).

OCR takes roughly 20 seconds a page, so a long document won't finish inside one shell call. That's fine:
the script saves each page as it goes, and **running the same command again resumes** where it stopped
(it ends with `all N pages extracted` or `NOT YET EXTRACTED: pages …`). Pass `timeout_ms: 180000` to
device_bash and repeat until done, or do it in chunks with `--pages 1-6`. `--redo` re-extracts pages
already done (needed after changing `--psm`, `--force-ocr` or `--min-conf`).

- A page with mean confidence under ~70 is poor: look at it closely, and try `--psm 4` (single column)
  or `--psm 6` (one block) for typewritten pages, `--psm 11` for scattered text such as forms and plans.
- If a text layer exists but reads as gibberish (an old bad OCR layer), re-run with `--force-ocr`.
- **Handwriting, stamps, plans and diagrams**: OCR is useless there. Transcribe from the image yourself,
  and say so in the report.

**Look at the pages.** Bash can't show you an image. Stage the PNGs (`device_stage_files` with the
repo path on Joel's machine, e.g. `/home/joel/Projects/5coy-archive/.transcribe/<ref>/page-01.png`) and
open them with Read. Crops are small, so stage a batch of them at once when checking suspects.

## 3a. New transcription (no markdown yet)

1. **Read every page image** before writing anything. Note the layout: columns, headings, tables, forms,
   lists, signatures, stamps, handwritten annotations.
2. **Copy the house style of the group.** Open one or two sibling files in the same `content/` folder
   and match them:
   - two-column newsletter pages (In the Line of Fire) use `::: column-left` / `::: column-right`
     custom containers (Markdig `UseCustomContainers`), each closed by `:::`;
   - forms such as personnel jackets use bold labels and pipe tables, with `_____` for blank fields;
   - typewritten reports keep their headings as `###`, lists as `-` or `1.`;
   - all-caps originals stay all-caps; original line breaks are kept where the siblings keep them.
   No page markers or HTML comments in the finished file.
3. **Write the draft** from `draft.txt`, fixing the layout to match the image.
4. **Extract the pictures** (step 3b) and reference them where they sit on the page.
5. **Find the errors** (step 4), fix them, and repeat until what's left has been checked against the image.
6. **Completeness check**: `python3 $S/compare_md.py <reference> --md <your file>`. Any `OMITTED` run
   is text the OCR saw that you dropped. `ADDED` is fine for things you read off the image that OCR missed.
7. **Wire it up.** Save as `web/wwwroot/content/<section>/<reference>.md` (kebab-case stem = Reference,
   section folder per the crossref-archive-data skill), set the article's `ContentUrl` with a surgical
   string replacement (not `json.dump`), then run
   `python3 .claude/skills/crossref-archive-data/scripts/check_crossrefs.py web/wwwroot`.
   A personnel.json-only PDF needs an article first (see crossref-archive-data, step 6).
8. `python3 $S/log_verification.py <reference> TRANSCRIBED "<pages, method, anything left uncertain>"`

## 3b. Pictures: extract them, don't describe them

When a page carries a picture that is part of the content (an illustration, photo, plan, diagram, map or
a drawn advert), cut it out of the PDF and show it in the markdown. Don't leave an
`_[Illustration: ...]_` note in its place. Skip purely decorative bits (masthead logos, rules, borders,
clip-art dingbats) and say in the report that you skipped them.

1. Find the picture's box on the 150 dpi page image (`.transcribe/<reference>/page-NN.png`): top-left and
   bottom-right corner in pixels. Keep it inside any printed frame, and leave out captions that are
   already transcribed as text and neighbouring rules or text.
2. Cut it out:
   ```bash
   python3 $S/extract_image.py <reference> <page> <x0,y0,x1,y1> \
       web/wwwroot/content/<section>/<reference>_<what>.png
   ```
   The box is scaled to the source resolution. The script crops from the page's embedded scan
   (`pdfimages`, the original pixels) when the page is one full-page image, otherwise from a 300 dpi
   render. It saves greyscale unless you pass `--colour` (use that only when the original is in colour).
   If the PDF isn't in `.transcribe/media/`, set `FIVECOY_MEDIA_DIR` to Joel's media mirror
   (`$HOME/mnt/5coy-media` when that folder is connected).
3. **Look at the PNG** (stage it and open it with Read) and adjust the box until nothing is cut off and
   nothing extra is in it.
4. Name it `<reference>_<short-kebab-description>.png` (e.g. `briefing-notes_type-23-exosuit_plan.png`,
   `thevoice_2493-05_dag-food.png`) and save it in the **same folder as the markdown**.
5. Reference it where it sits in the layout, with the site-absolute path, alt text describing what it
   shows and a title from the page's own caption or heading:
   ```markdown
   ![A marine in the new CMC A.C.C.D camouflage combat dress](/content/in-character/other-publications/dispatches_2495-unknown_accd.png "CMC A.C.C.D (All Condition Combat Dress)")
   ```
   Inside a boxed advert (blockquote), prefix the line with `> ` like the rest of the box.

## 3c. Verify an existing transcription

1. `python3 $S/extract_pdf.py <reference>` (as above).
2. `python3 $S/compare_md.py <reference>` lists every wording difference, most worrying first:
   `CHECK` (PDF confident, MD differs: likely an MD typo), `OMITTED` (dropped text), `ADDED`, `OCR?`
   (likely an OCR misread, MD probably right), `MOVED` (column order), `NOISE` (scan specks read as
   text). Punctuation and case are ignored
   unless you pass `--case`.
3. `python3 $S/flag_suspects.py <md file> --no-unknown`, then without `--no-unknown`: catches typos in
   the MD that compare_md can't see (e.g. where the OCR made the same mistake).
4. Settle every `CHECK`, `OMITTED` and `ADDED` against the page image. Spot-check a few `OCR?`, `MOVED`
   and `NOISE`. If word agreement is under ~60%, find out why (wrong PDF? partial transcription? handwriting?)
   before going through hunks one by one.
5. Fix the MD in place with exact-string edits so the diff stays reviewable. Don't reformat lines you
   aren't correcting.
6. If the PDF has pictures the markdown doesn't show (or only describes), extract them as in 3b.
7. Log it: `VERIFIED` (no change), `FIXED` (say how many corrections, of what kind) or `PARTIAL`
   (what's left for Joel).

## 4. Finding and fixing transcription errors

```bash
python3 $S/flag_suspects.py <file> --extract .transcribe/<reference>/extract.json
```

Codes, most to least likely to be a real error: `JUNK` (scan noise such as `|`, `~`, `»`, lone letters),
`MIXED` (`C0rporal`, `24O5`), `OCR` (one classic swap away from a real word, with suggestion: rn↔m,
cl↔d, vv↔w, li↔h, 0↔o, 1↔l, 5↔s, c↔e…), `SPLIT` / `JOINED` (broken or run-together words), `HYPHEN`
(line-end hyphen), `DOUBLE` (repeated word), `LOWCONF` (Tesseract unsure, crop attached), `UNKNOWN`.

For each suspect, look at the crop or the page and decide which of these it is:

| It's… | Do |
|---|---|
| A misread (the page says something else) | Fix it to what the page says |
| A word broken by a line wrap on the page (`invest-` / `igation`) | Join it, unless the line breaks are being kept and the hyphen is printed (then keep as printed) |
| The author's own spelling, grammar or capitalisation (COPYS, AROND, SARGEANT, THERE for their) | **Keep it.** The archive is a faithful transcription, not a corrected one |
| A name, rank, place, ship or jargon word that's really on the page | Keep it, and add it to `.transcribe/lexicon.txt` so it stops being flagged |
| Genuinely unreadable | `[illegible]`; if you're fairly sure, `[word?]`. List each one for Joel |

Things the checkers can't catch, so check them yourself against the image: numbers, dates, grid
references and CMC idents (OCR swaps 3/8, 5/6, 1/7 into valid-looking numbers); dropped or doubled lines
at page and column breaks; reading order across columns; deliberately censored text (`P***UP`) that must
stay censored; and punctuation that changes meaning.

Keep going until a rerun shows only suspects you've confirmed on the page.

## 5. Report back

Per resource: what was done (new / verified / fixed), extraction method and confidence, the number and
kind of corrections, every `[illegible]` / `[word?]`, any handwritten or image-only content you
transcribed by eye, every picture extracted (file name and where it's used) and any decorative ones skipped, words added to `.transcribe/lexicon.txt`, and the data/ContentUrl changes. Then `git status
--short` and `git diff --stat`. Don't commit.
