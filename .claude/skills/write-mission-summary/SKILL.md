---
name: write-mission-summary
description: Write the 3-paragraph Mission Summary shown on a 5coy-archive mission page, drawn from the archive's own articles (mission reports, Mexal's letters, scenarios, In the Line of Fire, honours) and written in Joel's voice in the third person. Use when asked to write, draft or add a summary for a mission, or to fill in summaries for missions that don't have one.
---

# Write a mission summary

A mission page (`web/Pages/MissionDetail.razor`) shows the markdown at the mission's `ContentUrl`
in `data/missions.json` above its Related articles. If there's no `ContentUrl`, or the file is
missing, the page just shows nothing. This skill produces that markdown.

**Never commit.** Leave every change in the working tree for Joel to review (see CLAUDE.md).

All paths below are relative to `web/wwwroot` unless they start with `.claude/` or `web/`.

## 1. Gather the sources

```bash
python3 .claude/skills/write-mission-summary/scripts/gather_sources.py <mission-reference> [-k "variant" ...]
```

This prints the mission's `missions.json` entry, every article and video linked by `MissionRef`
(including children, with word counts), and any other content that mentions the mission by
keyword. Run it without a reference to list all missions.

- Keywords match whole words, so pass every spelling variant with its own `-k`. The archive is
  inconsistent: `-k "Marie Celesta" -k "Marie Celeste"`. Don't use ship names like Garavaglia as keywords, because they
  match almost everything.
- **Read all of the linked articles in full**, then skim the keyword hits for follow-ups. The
  aftermath usually lives outside the `MissionRef` links: later In the Line of Fire issues, the
  honours lists in `content/out-of-character/honours_*.md`, `interim-history_*.md`, intercepted
  corporate signals inside Mexal's letters, and the next missions' reports.
- Videos have no transcript. Mention them only if another source says what's in them.

Different sources are good for different things:

| Source | Use it for |
|---|---|
| Mission report (`content/mission-reports/`) | Dates, who was there, the official sequence of events, casualties |
| Mexal's letters | The texture: what it felt like, the odd characters, the jokes |
| Scenario, monster notes, audio script (`content/scenarios/`) | What the writer intended and the in-fiction truth the players never saw |
| In the Line of Fire, honours, interim history | The aftermath: inquiries, decorations, what happened next |

Where they disagree, trust the contemporaneous mission report for facts and dates. Use the
scenario for intent, and use the scenario and intercepts together for the hidden truth. Say what
the scenario intended *and* how the weekend actually went if they differ. Don't quietly pick one.

## 2. Ask Joel what the archive can't tell you

Before drafting, ask Joel for any first-hand detail he wants included, e.g. what the props or
set-pieces were, or how the weekend actually ran compared with the scenario. **Never assume he
played** a given mission: he wrote and monstered Marie Celesta, for example.

**Character names only.** Summaries never use real-world names: no players, writers, monsters
or crew, and that includes Joel. Out-of-character context is fine as long as it's anonymous
("the scenario was written as part of the Drive Wars sequence", "its props included working
computer programs"). Use characters' in-fiction names and ranks from the sources, and never say
who played whom. (The honours lists and the scenario headers give real names in brackets:
leave those out.)

If he isn't around to answer, draft without the out-of-character credit and say what's missing.

## 3. Draft in Joel's voice, in the third person

Load the `in-joels-voice` skill and follow it, with these overrides for mission summaries:

- **Third person**, narrated from outside the story. Never "I", and no real names (see step 2).
- **3 paragraphs, around 350-450 words.** No heading, because the page already shows the
  mission name as its `<h1>`. No TLDR, no lists, no emoji.
- **Shape:**
  1. Setup: where it sits in the campaign (e.g. Drive Wars) and any earlier missions it follows
     on from, the premise, and how the Marines went in.
  2. The weekend, in one paragraph: the high points only. That means the situation they found,
     the turn it hinged on, how it ended, and the casualties, with names and ranks from the report
     and honours list. Tell it as one story, not a timeline: **no "Day 1" / "Day 2" structure**, no
     blow-by-blow of patrols and firefights, and no more than 2 or 3 of the memorable characters.
     Pick the moments a veteran would retell in the bar, and leave the rest to the Related articles.
  3. The fallout: inquiries, news coverage, what was really going on (from scenario notes or
     intercepts), and how it fed into later missions.
- Don't open with "So…": Joel's blog voice uses it a lot, but for summaries it gets repetitive across the archive. Open straight into the situation instead, and use "So" at most once in a summary, mid-text. Use British spelling. Short punchy sentences are fine
  ("Spot the mistake?"), but keep it to 1 or 2 per summary. Use spaced hyphens " - " rather than
  em dashes, and don't lean on them.
- **In-game dates** (May 2495) for in-fiction events, **real dates** for out-of-character things
  (the August 1995 honours list). Numbers as digits.
- Quote sources only briefly (a citation line, a nickname). Paraphrase everything else.
- Use the spelling in the mission's `missions.json` Name. **Don't comment on spelling
  inconsistencies or other archive problems in the summary**: report those to Joel separately.
- Facts only from the sources or from Joel. If something isn't known, leave it out.

### Links

Link to other pages in the archive where it helps the reader, using site-relative markdown links:

| Target | Link | Where the parts come from |
|---|---|---|
| Another mission | `[Mission 'Ogre'](/mission/2494-04_mission-ogre)` | the mission `Reference` in `missions.json` |
| An article | `[mission report](/article/mission-reports/2495-07_mission-omaha)` | `/article/<group Reference>/<article Reference>`: `gather_sources.py` prints both as `[article:<group>] <ref>` |
| A section | `[Mexal's Letters Home](/section/mexals-letters)` | a `Reference` in `sections.json` |

- Link every other mission the summary names (whether or not it has a summary yet), and the
  source articles the summary leans on: the report, Mexal's letter, the scenario, the In the
  Line of Fire issue or honours list a fact came from.
- Link the first mention only. Keep the link text natural ("[didn't make it out](…)") rather
  than pasting article titles, and don't link the mission's own Related articles more than once.
- When you add a summary, check the summaries that already exist for missions it connects to,
  and add a link back where it fits (e.g. Marie Celesta links forward to Mission 'Nova', where
  CNN were still chasing the story).
- The cross-reference checker validates every `/mission/`, `/article/` and `/section/` link.

**Show Joel the draft in chat first** and revise until he approves it. Only then write the file.

## 4. Write the file and link it

1. Write the approved text verbatim to `content/missions/<in-game yyyy-mm>_<short-kebab-name>.md`
   (e.g. `2495-05_marie-celesta.md`) as UTF-8 with **CRLF** line endings, like the rest of
   `content/`. Use device tools/scripts to write it; never retype it from earlier tool output.
2. Add `"ContentUrl": "/content/missions/<file>.md"` to the mission's entry in `data/missions.json`.
   Edit only that entry and keep the file's existing formatting (LF endings, field layout).
3. Run the cross-reference checker. It treats mission `ContentUrl`s as references and errors if
   the file is missing:
   ```bash
   python3 .claude/skills/crossref-archive-data/scripts/check_crossrefs.py web/wwwroot
   ```
4. Show Joel `git status` and `git diff --stat`.

## 5. Report what you noticed

Separately from the summary, list any data problems you found while reading, and offer to fix
each one rather than fixing it silently. Examples: a mission's dates that don't match its report
(Marie Celesta was stored as 1-3 May but the report says 27/28 May), a `MissionRef` pointing at
an article reference, or sources that should be linked to the mission and aren't. Use the
`crossref-archive-data` skill for the fixes.
