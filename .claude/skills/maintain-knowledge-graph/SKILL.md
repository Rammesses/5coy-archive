---
name: maintain-knowledge-graph
description: Keep the 5coy-archive knowledge graph (web/wwwroot/data/kg - characters and organisations with sourced facts) in step with the archive, and use it to answer questions about the content. Use when new or changed articles, transcriptions or mission summaries land, when asked to add/fix/check people or organisations, or when asked who/what/when questions about the archive's in-game world.
---

# Maintain the knowledge graph

The graph lives in `web/wwwroot/data/kg/`:

| File | What | Edited by |
| --- | --- | --- |
| `characters.json` | in-game characters | hand / Claude (curated) |
| `organisations.json` | corporations, CMC units, governments, groups, press | hand / Claude (curated) |
| `generated/mentions.json` | which entities each article/mission page mentions | `build_generated.py` only |
| `generated/graph.jsonl` | flattened nodes + edges for graph DB / RAG import | `build_generated.py` only |

Scripts (run from the repo root):

```bash
S=.claude/skills/maintain-knowledge-graph/scripts
python3 $S/check_kg.py web/wwwroot          # validate - must report 0 errors
python3 $S/build_generated.py web/wwwroot   # regenerate generated/
python3 $S/query_kg.py web/wwwroot "Calvin" # entity card (also --mission <ref>, --search <text>)
```

## Data model

Every entity: `Reference` (kebab-case, unique within its file), `Name`, `Aliases` (every spelling the sources use,
including OCR variants and rank+surname forms), optional `Type` (organisations: corporation, military, government,
political, religious, terrorist, mercenary, press, police, clan, family, other), `Species`, `ServiceNumber`,
a one-sentence `Description` in your own words, and `Facts`.

Graph IDs are type-prefixed because References collide across types (`article/2495-11_mission-nova` and
`mission/2495-11_mission-nova` both exist): `character/…`, `organisation/…`, `mission/…`, `article/…`.

A fact: `{"P": predicate, "O": "<id>" and/or "V": "<text>", qualifiers…, "Sources": ["article/…" | "mission/…"]}`.

- **Character predicates:** memberOf, rank, role, participatedIn, commanded, authored, employedBy, founded,
  relative, relatedTo, status, award, born, enlisted, note, appearsIn
- **Organisation predicates:** partOf, participatedIn, owns, basedAt, status, supplies, opposes, successorOf,
  relatedTo, note, appearsIn
- **Qualifiers:** `Role` (what they did / which side), `At` (the mission it applies to), `Date`, `From`, `To`
  (in-game `yyyy[-mm[-dd]]`), `Note` (caveats, disagreements between sources).
- Ranks, roles and memberships change over time: always record them per mission (`At`) or with a `Date`,
  never as a single fixed property.
- Places, vessels and events are not entities yet: put them in `V` text (e.g. `"owns", V: "Golgotha"`). They will
  become `place/…`, `vessel/…` and `event/…` entities later, so name them consistently.

## Rules

1. **Every fact cites at least one source** that resolves to an article or mission reference. Mission summaries
   (`mission/<ref>`) can be cited, but prefer the primary article.
2. **Record what the source says, not what you infer.** If you're connecting two things, put the reasoning in
   `Note` (e.g. "probably the same person"), and add "needs review" to the Description when identity is uncertain.
   Don't merge entities on a hunch: Gillian/Gillham, Nim/Nix and the two Bryants are open questions.
3. **When sources disagree, keep both facts and say so in `Note`.** Mission reports are canon for which weekend.
4. **Characters only, never players.** Several OOC documents (the honours list, the newsletter) give players'
   real names in brackets. Never put those in the graph.
5. **Scenario notes are OOC design documents.** Facts from them should say so in the Description or `Note`.
6. Aliases must not be shared by two entities (the checker warns). Drop the ambiguous alias rather than
   guessing; a bare surname belongs to the entity people overwhelmingly mean by it.

## Workflow when content changes

1. Read the new or changed markdown (and check `articles.json`/`missions.json` for its Reference and `MissionRef`).
2. For every named person and organisation: find the existing entity (`query_kg.py --search`) or create one;
   add aliases for new spellings; add facts with this article as the source. Edit the JSON with a short Python
   read-modify-write, never by retyping the file.
3. Run `check_kg.py` (0 errors) and `build_generated.py`.
4. Summarise the diff for Joel: entities added, facts added, anything flagged for review.
   **Do not commit** (see CLAUDE.md). Wait for Joel's review.

The transcribe-pdf-resource and write-mission-summary skills should finish by running this workflow on what
they produced.

## Answering questions

Always give in-character dates in in-game years (the game is set from 2488 onwards: Nightside is January 2495, not 1995 or 1495). Real-world dates are only for out-of-character events.

Use `query_kg.py` for the facts, then open the cited sources for detail and quote from them. When an answer
depends on a `Note` or a "needs review" flag, say so. `generated/mentions.json` lists every page an entity
appears on, which is useful for "where else is X mentioned?".
