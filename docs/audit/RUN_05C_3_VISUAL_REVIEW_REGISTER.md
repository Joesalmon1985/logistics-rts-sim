# RUN_05C.3 Visual Review Register

**Run:** RUN_05C.3 — Economy Spectator Visual Review, UX Polish, and Showcase Pass  
**Branch:** `feature/run-05c-economy-spectator-visual-identity`  
**Artefacts:** `training_runs/visual_review/run_05c_economy_spectator/` (tick0/10/30/60 PNGs, `contact_sheet.html`, `capture_manifest.json`)  
**Baseline commit:** `2a7b3fc` (post RUN_05C.2)

Review performed from generated screenshots and manifest before polish edits. Grouped observations for Phase 1–4 targeting.

---

## Immediately clear

1. **Production-chain layout** — HQ (yellow), woodcutter (WOOD), sawmill (SAW), and forest source hex (D:80→D:35) are identifiable on the hex grid without reading code.
2. **Road network** — Blue road segments connect HQ to woodcutter and sawmill; readable at all milestone ticks.
3. **Carrier presence** — Orange/blue carrier dots on routes are visible by tick 10; carrying state shows ware abbreviations on the map.
4. **Store/production labels** — Building hex labels (`F:… S:…`, `P:80`) communicate forest, stock, and production progress.
5. **Event log** — Tick 30 shows grounded sim events (`ware_delivered`, `ware_picked_up`, `production_started`) matching visible chain activity.
6. **Sprite families** — Manifest reports logistics + building + storage families active at every milestone (sprites 7→10).
7. **Recent-changes buffer** — Tick 30 panel lists forest D, carrier movement, and ware-route deltas (grounded, not invented).

---

## Unclear / confusing

8. **Run identity** — Title reads “RUN_05C.2”; does not state this is a visual-only spectator pass.
9. **Bot-proof disclaimer missing** — No top-level banner that production-chain demo is **not** bot-vs-bot expansion proof.
10. **Duplicate status lines** — Tick, demo name, and transport mode appear in Status, Summary, and Bot status with overlapping wording.
11. **Map title `routes:0`** — Title bar reports zero ware routes while dashed route overlays and jobs are drawn (counter vs overlay mismatch confuses trace reading).
12. **Dual-bot control placement** — “Dual-Bot View” sits beside primary demo controls without enough visual separation from the automated production-chain path.

---

## Visually noisy

13. **Right-panel density** — Debug counts, summary, bot status, activity hint, buttons, event log, and legend compete for attention; map area is comparatively calm.
14. **Hex label stacking** — Multiple small strings per building hex (type, stores, production) overlap and crowd the map centre.
15. **Legend length** — Full animation-family list consumes vertical space below the event log on every capture frame.
16. **Debug line redundancy** — `roads 5 | sites 0 | carriers 3 | sprites N | events N` repeats information already in summary and status.

---

## Too static

17. **Tick 0 idle feel** — Carriers present but little motion emphasis until stepping; road-flow animation not obvious against static road lines.
18. **Tick 60 plateau** — Pixel delta tick30→tick60 is small; visual story stalls after mid-chain activity (acceptable for prod-chain plateau, but contact sheet does not explain it).

---

## Hard to trace source → transport → processing / storage / use

19. **No chain trace panel** — Viewer must infer chain from scattered hints; no single “Forest → carrier → HQ/store → sawmill” readout derived from render data.
20. **Forest source not highlighted** — Forest density tint is subtle; source hex does not stand out from grass neighbours.
21. **Route direction weak** — Small chevrons on dashed ware routes are easy to miss at 1920×1080 contact-sheet scale.
22. **Map markers vs sprites** — Static carrier dots and animated sprites coexist without a clear “moving vs idle” hierarchy.

---

## Missing or weak feedback

23. **Delivery destination pulse** — `ware_delivered` events exist in log but no persistent destination highlight on the receiving building hex.
24. **Activity score absent** — No single numeric hint summarising how much visual/economic activity is happening this tick.
25. **Construction family dormant** — Production-chain demo has no construction sites; legend still lists construction animations (expected, but smoke/contact sheet should note “not exposable”).

---

## UI / panel readability issues

26. **Recent changes buried** — “Recent changes” sits inside the activity hint label below other lines instead of a dedicated prominent panel.
27. **Speed vs tick split** — Speed label is on button row 2b while tick/play state is in StatusLabel (inconsistent transport bar).
28. **Capture path not shown** — Screenshot button gives per-tick export path but not the automated `training_runs/visual_review/…` review package location.

---

## Screenshot / contact-sheet issues

29. **Minimal contact sheet** — HTML table shows tick + image + sprite count only; no visual families, meaningful_count, or pixel deltas.
30. **Manifest gaps** — `capture_manifest.json` lacks per-tick activity score, meaningful_count, pixel delta from tick0, or link to review notes.
31. **No REVIEW_NOTES.md** — Automated capture does not emit a markdown summary for offline review.

---

## Polish priorities (RUN_05C.3)

| Priority | Target observation IDs | Phase |
|----------|-------------------------|-------|
| P0 | 8, 9, 10, 26, 27 | Phase 1 — hierarchy |
| P1 | 19, 20, 21, 22, 11 | Phase 2 — cause/effect trace |
| P2 | 17, 23, 24 | Phase 3 — rhythm / feedback |
| P3 | 29, 30, 31, 18 | Phase 4 — capture package |

**Out of scope:** ECON_BOT_002, dual-bot gates, authoritative economy rules, final art.
