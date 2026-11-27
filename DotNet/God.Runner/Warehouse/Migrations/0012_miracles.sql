-- Warehouse schema v12 (D3, SHARED_ARCHITECTURAL_CONTRACTS 18.2): miracles. References only 0001-0007 (0008-0011 may not be merged).
-- Grains: fact_miracle one row per accepted miracle (MiracleCast, InterventionId); fact_miracle_effect one row per thing a miracle
-- changed (MiracleEffectApplied) with its value before and after, and for a death the divine attribution (death = 1, cause_code the
-- recorded DeathCause: always Violence, there is no divine cause, contracts 20.4). F1's fact_resource_ledger (0007) holds the
-- goods a miracle created or destroyed (filled by D3's mapper). Every table is empty while Divinity is off. Integers only.

CREATE TABLE fact_miracle (                                  -- E: one accepted miracle
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, god_key INTEGER REFERENCES dim_god (god_key), kind_code INTEGER NOT NULL,   -- MiracleKind
  target_kind_code INTEGER, target_id INTEGER, target_node INTEGER, target_radius INTEGER,
  magnitude INTEGER NOT NULL,                               -- the raw command value (CreateResource: ware << 8 | count)
  cost INTEGER NOT NULL);                                   -- nominal milli-Mythic
CREATE UNIQUE INDEX ux_fm_intervention ON fact_miracle (run_key, intervention_id);

CREATE TABLE fact_miracle_effect (                           -- E: one thing a miracle changed
  run_key INTEGER NOT NULL, tick INTEGER NOT NULL, seq INTEGER NOT NULL, date_key INTEGER NOT NULL REFERENCES dim_date (date_key),
  intervention_id INTEGER NOT NULL, thing_kind_code INTEGER NOT NULL,   -- 1 person 2 hut 3 source 4 lot 5 node 6 building 7 witness 8 place
  thing_id INTEGER NOT NULL, field TEXT NOT NULL, before_value INTEGER NOT NULL, after_value INTEGER NOT NULL,
  death INTEGER NOT NULL DEFAULT 0, cause_code INTEGER);
CREATE INDEX ix_fme_intervention ON fact_miracle_effect (intervention_id);

INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('miracle_kind', 1, 'heal_injury', 'HealInjury'), ('miracle_kind', 2, 'heal_disease', 'HealDisease'), ('miracle_kind', 3, 'bless', 'Bless'),
  ('miracle_kind', 4, 'protect', 'Protect'), ('miracle_kind', 5, 'smite_person', 'SmitePerson'), ('miracle_kind', 6, 'smite_building', 'SmiteBuilding'),
  ('miracle_kind', 7, 'smite_source', 'SmiteSource'), ('miracle_kind', 8, 'create_resource', 'CreateResource'), ('miracle_kind', 9, 'destroy_resource', 'DestroyResource'),
  ('miracle_kind', 10, 'reveal_knowledge', 'RevealKnowledge'), ('miracle_kind', 12, 'consecrate', 'Consecrate');
