---
name: update-change-history
description: Add entries to the 5coy-archive Change History page (web/wwwroot/change-history.md), which also drives the "What's New" section on the home page. Use at the end of any task that changes what readers see on the site (new or transcribed content, new pages or features, mission summaries, fixes readers would notice), when asked to update the change history or What's New, or to check that file.
---

# Update the Change History

`web/wwwroot/change-history.md` is the site's change history, served at `/change-history`.
The home page's **What's New** reads the same file (`Services/ChangeHistoryService.cs`) and shows
the newest three entries marked `.whats-new`, each linking to its entry on the history page.
There is no second copy to keep in step: edit the markdown only.

**Never commit** - leave the change in the working tree for Joel to review (see CLAUDE.md).

## When to use it

At the end of a task, if the work changed anything a reader of the site would notice, **offer**
to add a change-history entry (or add to today's) (one line: the proposed title, and whether it should be a What's
New highlight). Only write it once Joel says yes, or if he asked for it as part of the task.
Don't offer for purely internal work: refactors, tests, tooling, skills, dependency bumps or CI,
unless it changes the site for readers (e.g. a .NET upgrade that's worth a one-line note).

## Entry format

**One entry per day.** Newest first, straight after the format comment at the top of the file:

```markdown
## 25 November 2026 - Videos, Personnel Jackets and Other Publications {#2026-11-25-videos-personnel-publications .whats-new}

A new [Videos](/videos) page, two new in-character sections ... and a batch of newly
transcribed out-of-character documents.

- [Videos](/videos) brings together 17 videos from the campaign, ...
- [Personnel Jackets](/section/personnel-jackets) has 20 new jackets, ...
```

- Heading: `## {d MMMM yyyy} - {Title} {#{yyyy-mm-dd}-{slug}}`. Use today's real-world date
  (the date the change is made, UK English, no leading zero). Old entries reconstructed from git
  history may use `MMMM yyyy` and an id of `{yyyy-mm}-{slug}`.
- **If today already has an entry, merge into it** rather than adding a second one: add a bullet
  (turning the single paragraph into summary + bullets if it hasn't been already), rewrite the
  summary paragraph and title so they cover the whole day, and add `.whats-new` if the new change
  is important. The checker rejects a second entry with the same date.
- The id is the anchor What's New links to, so don't change it once the day is over (entries are
  only published once committed). While merging into today's entry, keep today's id unless the
  slug is now misleading.
- Add ` .whats-new` inside the braces only for an **important** day: a new section, page or
  feature, or a substantial batch of new content (e.g. a run of newly transcribed issues). Days of
  only small fixes, tidy-ups and technical upgrades stay unmarked. Leave older `.whats-new` marks
  in place; the home page simply shows the newest three.
- Summary: the **first paragraph** is what What's New shows, so it must stand on its own: one
  short paragraph (2-3 sentences) for readers, not developers, saying what they can now find or
  do, with links. When the day has more than one change, follow it with a bullet list giving the
  detail of each (counts help); the bullets appear only on the Change History page.

Writing rules carried over from CLAUDE.md:

- Internal links only, using the site's routes: `/section/{ref}`, `/article/{sectionRef}/{ref}`,
  `/mission/{ref}`, `/video/{ref}`, `/missions`, `/videos`. A mission link's text follows the
  mission-link rule (`Mission '{name}' - {month} {year} ({planet})`, built from `missions.json`).
- In-character dates are in-game years (24xx/25xx); the entry's own date is the real-world date.
- No real-world contact details. External links follow the web-link rule (code + dated marker),
  though entries rarely need one.

## Steps

1. Work out what changed for readers: the task itself, plus `git status` / `git diff --stat`
   (and `git log` for anything committed since the newest entry that isn't listed yet).
2. Draft the entry - or, if today already has one, the merged version of it - and decide on
   `.whats-new`. Offer it to Joel as above.
3. Insert it at the top of the entry list in `web/wwwroot/change-history.md` (or replace today's entry).
4. Check it:
   `python3 .claude/skills/update-change-history/scripts/check_change_history.py web/wwwroot`
   It checks heading format, ids (unique, date-prefixed), summaries, newest-first order, at
   least three highlights, and that internal links resolve. Its last line shows the three entries
   the home page will now show - include that in your report.
5. If .NET is available, `make test` also runs `ArchiveDataTests` (which parses the real file).
