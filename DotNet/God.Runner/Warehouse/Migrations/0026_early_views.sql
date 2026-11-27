-- Warehouse schema v26 (Q1, LEAD_RULINGS R18): the early views and the Tier 1 KPI rows over the tables that exist at 0001-0007.
-- References only 0001-0007; never edited after it merges (the org, service and religion views and every KPI whose source
-- package has not merged land in 0030 and later, which may DROP VIEW and CREATE it again). Conventions of a KPI view v_kpi_<id>:
-- one long-format row per (kpi_code, faction, period): run_key, kpi_code, faction_key (NULL = all), period_from_day and
-- period_to_day (half open, sim days), actual_value (integer, the unit of dim_kpi.unit), sample_n (the denominator or count the
-- value rests on) and missing_reason (NULL, or 'min_cell': actual_value is then NULL, never 0). The KPI engine
-- (God.Runner/Kpi) copies these rows into fact_kpi at run end; a KPI whose view does not exist yet is Missing, never 0.
-- Medians and percentiles follow KPI_CATALOGUE 6.2 (ties by person_key). Analytics only: nothing in the sim reads these views.

-- ---------------------------------------------------------------- parameters and codes
CREATE TABLE meta_kpi_param (name TEXT PRIMARY KEY, value INTEGER NOT NULL, note TEXT) WITHOUT ROWID;
INSERT INTO meta_kpi_param (name, value, note) VALUES
  ('min_cell', 5, 'KPI_CATALOGUE 3.3: a cell with fewer rows is Missing'),
  ('pop12_min_person_years_pm1000', 50000, 'POP-12: Missing under 50 person-years'),
  ('pop12_pool_years', 3, 'POP-12: cohort periods of this many sim years'),
  ('hlt10_low_health', 300, 'HLT-10: the share below this health (permille)'),
  ('liv07_cover_days', 3, 'LIV-07: a household is short below this many days of food'),
  ('liv07_food_units_per_member_day', 1, 'LIV-07: measurement convention, one food unit (a meal) per member per day; revisit with F2/C1');

INSERT INTO dim_code (code_set, code, name, description) VALUES
  ('kpi_status', 0, 'ok', 'a value computed from the view'),
  ('kpi_status', 1, 'missing_source_absent', 'the view or its source package does not exist in this schema'),
  ('kpi_status', 2, 'missing_empty_source', 'the view exists but returned no row (no eligible data in the run)'),
  ('kpi_status', 3, 'missing_min_cell', 'below the minimum cell or exposure: Missing, never 0');

-- ---------------------------------------------------------------- the KPI rows of the Tier 1 subset (R30)
-- source_table lists the tables the KPI reads; definition names the package that supplies a missing source.
INSERT INTO dim_kpi (code, version, name, unit, source_table, definition) VALUES
  ('POP-12', 1, 'Child mortality, ages 1 to 4', 'pm1000 per person-year', 'fact_death,fact_person_day', 'deaths at age >= 1 and < 5 years per person-year lived in the band, 3-year pooled; Missing under 50 person-years'),
  ('POP-13', 1, 'Population accounting identity', 'n', 'fact_person_day,fact_birth,fact_migration,fact_death', 'alive at end - alive at start - (births + immigrants - deaths - emigrants); resurrections join with D7; must be 0 (actual-only)'),
  ('POP-14', 1, 'Deaths by life number', 'permille', 'fact_death.life_no,fact_resurrection', 'needs F2 (life_no) and D7'),
  ('HLT-10.median', 1, 'Health distribution: median', 'permille', 'fact_person_day', 'median health of alive persons that day'),
  ('HLT-10.p10', 1, 'Health distribution: 10th percentile', 'permille', 'fact_person_day', '10th percentile health of alive persons that day'),
  ('HLT-10.below', 1, 'Health distribution: share below the low-health line', 'permille', 'fact_person_day', 'alive persons below meta_kpi_param hlt10_low_health, per 1000'),
  ('LIV-07.short', 1, 'Household food cover: households short', 'permille', 'fact_household_day', 'households whose food stock covers fewer than liv07_cover_days days, per 1000 households'),
  ('LIV-07.median_days', 1, 'Household food cover: median days', 'pm1000 days', 'fact_household_day', 'median days of food cover over households'),
  ('TIM-04', 1, 'Time by control source', 'permille', 'fact_compulsion,fact_possession', 'needs D2 (compulsion) and D4'),
  ('PRD-01', 1, 'Physical output per labour tick', 'pm1000 units per tick', 'fact_production,fact_person_day', 'needs C1 (ware-attributed work ticks)'),
  ('OWN-01', 1, 'Productive-asset concentration', 'pm1000', 'fact_ownership_spell', 'needs C1'),
  ('DIV-01', 1, 'Interventions', 'n', 'fact_divine_action', 'needs D1'),
  ('DIV-02', 1, 'Mythic expenditure', 'pm1000', 'fact_mythic_ledger', 'needs D1'),
  ('DIV-03', 1, 'Mythic balance use', 'permille', 'fact_mythic_balance_day', 'needs D1'),
  ('DIV-04', 1, 'Response to instructions', 'permille', 'fact_command_perception,fact_command_response', 'needs D2'),
  ('DIV-05', 1, 'Perception rate', 'permille', 'fact_divine_command,fact_command_perception', 'needs D2'),
  ('DIV-06', 1, 'Compulsion', 'n', 'fact_compulsion', 'needs D2'),
  ('DIV-09', 1, 'Miracle effects', 'n', 'fact_miracle,fact_miracle_effect', 'needs D3'),
  ('DIV-10', 1, 'Resource creation and destruction', 'units', 'fact_resource_ledger,fact_production', 'needs D3 events and a ware breakdown column in fact_kpi (not in the schema); table exists, empty'),
  ('DIV-13', 1, 'Divine control share', 'permille', 'fact_person_day,fact_compulsion', 'needs D2 and D4'),
  ('CMD-01', 1, 'Commandments', 'n', 'fact_commandment', 'needs D6'),
  ('CMD-05', 1, 'Commandment actions', 'n', 'fact_commandment_action', 'needs D6');

-- ---------------------------------------------------------------- analysis views
-- v_population_day: alive, births, deaths and migration per faction per day. Alive counts the persons with a fact_person_day
-- row (alive at the end of the day); the flows are the events dated that day.
CREATE VIEW v_population_day AS
SELECT d.run_key, d.date_key AS day, f.faction_key,
       COALESCE(a.n, 0) AS alive, COALESCE(b.n, 0) AS births, COALESCE(x.n, 0) AS deaths,
       COALESCE(i.n, 0) AS immigrants, COALESCE(e.n, 0) AS emigrants
FROM dim_date d
JOIN dim_faction f ON f.run_key = d.run_key
LEFT JOIN (SELECT date_key, faction_key, COUNT(*) AS n FROM fact_person_day GROUP BY date_key, faction_key) a ON a.date_key = d.date_key AND a.faction_key = f.faction_key
LEFT JOIN (SELECT date_key, faction_key, COUNT(*) AS n FROM fact_birth GROUP BY date_key, faction_key) b ON b.date_key = d.date_key AND b.faction_key = f.faction_key
LEFT JOIN (SELECT date_key, faction_key, COUNT(*) AS n FROM fact_death GROUP BY date_key, faction_key) x ON x.date_key = d.date_key AND x.faction_key = f.faction_key
LEFT JOIN (SELECT date_key, faction_key, COUNT(*) AS n FROM fact_migration WHERE direction = 1 GROUP BY date_key, faction_key) i ON i.date_key = d.date_key AND i.faction_key = f.faction_key
LEFT JOIN (SELECT date_key, faction_key, COUNT(*) AS n FROM fact_migration WHERE direction = -1 GROUP BY date_key, faction_key) e ON e.date_key = d.date_key AND e.faction_key = f.faction_key;

-- v_person_life: what happened to this person, one row per person ever alive (no joins to write).
CREATE VIEW v_person_life AS
SELECT p.run_key, p.person_key, p.npc_id, p.faction_key, p.is_founder, p.generation,
       p.spawn_tick, p.death_tick, p.death_cause_code, p.age_ticks_at_death,
       COALESCE(pd.days_lived, 0) AS days_lived, pd.min_health, pd.last_day,
       COALESCE(bm.n, 0) AS births_as_mother, COALESCE(mi.n, 0) AS arrivals, COALESCE(mo.n, 0) AS departures,
       b.mother_person_key, b.father_person_key
FROM dim_person p
LEFT JOIN (SELECT person_key, COUNT(*) AS days_lived, MIN(health) AS min_health, MAX(date_key) AS last_day FROM fact_person_day GROUP BY person_key) pd ON pd.person_key = p.person_key
LEFT JOIN fact_birth b ON b.person_key = p.person_key
LEFT JOIN (SELECT mother_person_key AS person_key, COUNT(*) AS n FROM fact_birth GROUP BY mother_person_key) bm ON bm.person_key = p.person_key
LEFT JOIN (SELECT person_key, COUNT(*) AS n FROM fact_migration WHERE direction = 1 GROUP BY person_key) mi ON mi.person_key = p.person_key
LEFT JOIN (SELECT person_key, COUNT(*) AS n FROM fact_migration WHERE direction = -1 GROUP BY person_key) mo ON mo.person_key = p.person_key;

-- v_god_day: one row per god per day with a resource-ledger row (miracle creation or destruction). Later packages add their
-- columns by replacing this view in 0030. Empty while Divinity is off.
CREATE VIEW v_god_day AS
SELECT g.run_key, g.god_key, g.god_id, g.name AS god_name, l.date_key AS day,
       SUM(CASE WHEN l.direction = 1 THEN 1 ELSE 0 END) AS ledger_created_rows,
       SUM(CASE WHEN l.direction = 1 THEN l.quantity ELSE 0 END) AS units_created,
       SUM(CASE WHEN l.direction = -1 THEN 1 ELSE 0 END) AS ledger_destroyed_rows,
       SUM(CASE WHEN l.direction = -1 THEN l.quantity ELSE 0 END) AS units_destroyed,
       COUNT(DISTINCT l.intervention_id) AS interventions
FROM fact_resource_ledger l JOIN dim_god g ON g.god_key = l.god_key
GROUP BY g.run_key, g.god_key, g.god_id, g.name, l.date_key;

-- v_intervention_chain: what each divine intervention led to: the rows of every person-action table (and the resource
-- ledger) that carry its intervention_id, by table and control source. Empty while Divinity is off.
CREATE VIEW v_intervention_chain AS
SELECT intervention_id, source_table, control_code, COUNT(*) AS n_rows, MIN(tick) AS first_tick, MAX(tick) AS last_tick FROM (
  SELECT intervention_id, 'fact_decision' AS source_table, control_code, tick FROM fact_decision WHERE intervention_id IS NOT NULL
  UNION ALL SELECT intervention_id, 'fact_production', control_code, tick FROM fact_production WHERE intervention_id IS NOT NULL
  UNION ALL SELECT intervention_id, 'fact_ownership', control_code, tick FROM fact_ownership WHERE intervention_id IS NOT NULL
  UNION ALL SELECT intervention_id, 'fact_care', control_code, tick FROM fact_care WHERE intervention_id IS NOT NULL
  UNION ALL SELECT intervention_id, 'fact_social_interaction', control_code, tick FROM fact_social_interaction WHERE intervention_id IS NOT NULL
  UNION ALL SELECT intervention_id, 'fact_transaction_line', control_code, tick FROM fact_transaction_line WHERE intervention_id IS NOT NULL
  UNION ALL SELECT intervention_id, 'fact_resource_ledger', NULL, tick FROM fact_resource_ledger WHERE intervention_id IS NOT NULL)
GROUP BY intervention_id, source_table, control_code;

-- ---------------------------------------------------------------- KPI views
-- POP-13 (D): alive at end of the day - alive at end of the previous day - (births + immigrants - deaths - emigrants), all
-- factions together. Resurrections join the identity when D7's table exists. The first recorded day has no previous end of
-- day, so it starts from the persons present at the first day's start (founders, births and arrivals that day are flows).
CREATE VIEW v_kpi_pop13 AS
SELECT run_key, 'POP-13' AS kpi_code, NULL AS faction_key, day AS period_from_day, day + 1 AS period_to_day,
       alive - prev_alive - (births + immigrants - deaths - emigrants) AS actual_value, alive AS sample_n, NULL AS missing_reason
FROM (SELECT run_key, day, SUM(alive) AS alive, SUM(births) AS births, SUM(deaths) AS deaths, SUM(immigrants) AS immigrants, SUM(emigrants) AS emigrants,
             COALESCE(LAG(SUM(alive)) OVER (PARTITION BY run_key ORDER BY day), (SELECT COUNT(*) FROM dim_person q WHERE q.run_key = v_population_day.run_key AND q.is_founder = 1)) AS prev_alive
      FROM v_population_day GROUP BY run_key, day);

-- HLT-10 (D): median, 10th percentile and the share below the low-health line of alive persons.
CREATE VIEW v_kpi_hlt10 AS
WITH ranked AS (
  SELECT run_key, date_key, person_key, health,
         ROW_NUMBER() OVER (PARTITION BY run_key, date_key ORDER BY health, person_key) AS rn,
         COUNT(*) OVER (PARTITION BY run_key, date_key) AS n
  FROM fact_person_day),
stats AS (
  SELECT run_key, date_key AS day, MAX(n) AS n,
         MIN(CASE WHEN rn IN ((n + 1) / 2, (n + 2) / 2) THEN health END) AS med_lo,
         MAX(CASE WHEN rn IN ((n + 1) / 2, (n + 2) / 2) THEN health END) AS med_hi,
         MAX(CASE WHEN rn = (100 * (n - 1) + 500) / 1000 + 1 THEN health END) AS p10,
         SUM(CASE WHEN health < (SELECT value FROM meta_kpi_param WHERE name = 'hlt10_low_health') THEN 1 ELSE 0 END) AS below
  FROM ranked GROUP BY run_key, date_key)
SELECT run_key, 'HLT-10.median' AS kpi_code, NULL AS faction_key, day AS period_from_day, day + 1 AS period_to_day,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN NULL ELSE (med_lo + med_hi + 1) / 2 END AS actual_value, n AS sample_n,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN 'min_cell' END AS missing_reason FROM stats
UNION ALL
SELECT run_key, 'HLT-10.p10', NULL, day, day + 1,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN NULL ELSE p10 END, n,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN 'min_cell' END FROM stats
UNION ALL
SELECT run_key, 'HLT-10.below', NULL, day, day + 1,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN NULL ELSE (2000 * below + n) / (2 * n) END, n,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN 'min_cell' END FROM stats;

-- LIV-07 (D): households (with at least one member) short of food, and the median days of cover. Days of cover =
-- store_food / (members x food units per member-day), in pm1000 days.
CREATE VIEW v_kpi_liv07 AS
WITH cover AS (
  SELECT run_key, date_key, household_id, members, store_food,
         (1000 * store_food) / (members * (SELECT value FROM meta_kpi_param WHERE name = 'liv07_food_units_per_member_day')) AS cover_pm1000
  FROM fact_household_day WHERE members > 0),
ranked AS (
  SELECT *, ROW_NUMBER() OVER (PARTITION BY run_key, date_key ORDER BY cover_pm1000, household_id) AS rn,
         COUNT(*) OVER (PARTITION BY run_key, date_key) AS n FROM cover),
stats AS (
  SELECT run_key, date_key AS day, MAX(n) AS n,
         MIN(CASE WHEN rn IN ((n + 1) / 2, (n + 2) / 2) THEN cover_pm1000 END) AS med_lo,
         MAX(CASE WHEN rn IN ((n + 1) / 2, (n + 2) / 2) THEN cover_pm1000 END) AS med_hi,
         SUM(CASE WHEN cover_pm1000 < 1000 * (SELECT value FROM meta_kpi_param WHERE name = 'liv07_cover_days') THEN 1 ELSE 0 END) AS short
  FROM ranked GROUP BY run_key, date_key)
SELECT run_key, 'LIV-07.short' AS kpi_code, NULL AS faction_key, day AS period_from_day, day + 1 AS period_to_day,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN NULL ELSE (2000 * short + n) / (2 * n) END AS actual_value, n AS sample_n,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN 'min_cell' END AS missing_reason FROM stats
UNION ALL
SELECT run_key, 'LIV-07.median_days', NULL, day, day + 1,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN NULL ELSE (med_lo + med_hi + 1) / 2 END, n,
       CASE WHEN n < (SELECT value FROM meta_kpi_param WHERE name = 'min_cell') THEN 'min_cell' END FROM stats;

-- POP-12 (C, pooled over pop12_pool_years sim years): deaths at age >= 1 and < 5 years per person-year lived in that band,
-- pm1000 (permille per person-year x 1000). Exposure = person-days with age_days in [DaysPerYear, 5 x DaysPerYear); deaths by
-- age_ticks in [TicksPerYear, 5 x TicksPerYear). Missing under pop12_min_person_years_pm1000.
CREATE VIEW v_kpi_pop12 AS
WITH params AS (
  SELECT r.run_key, r.days_per_year AS dpy, r.ticks_per_day * r.days_per_year AS tpy, (SELECT value FROM meta_kpi_param WHERE name = 'pop12_pool_years') AS pool
  FROM dim_run r),
expo AS (
  SELECT d.run_key, d.sim_year / p.pool AS block, COUNT(*) AS person_days
  FROM fact_person_day f JOIN dim_date d ON d.date_key = f.date_key JOIN params p ON p.run_key = d.run_key
  WHERE f.age_days >= p.dpy AND f.age_days < 5 * p.dpy GROUP BY d.run_key, d.sim_year / p.pool),
dead AS (
  SELECT d.run_key, d.sim_year / p.pool AS block, COUNT(*) AS deaths
  FROM fact_death x JOIN dim_date d ON d.date_key = x.date_key JOIN params p ON p.run_key = d.run_key
  WHERE x.age_ticks >= p.tpy AND x.age_ticks < 5 * p.tpy GROUP BY d.run_key, d.sim_year / p.pool),
blocks AS (
  SELECT d.run_key, d.sim_year / p.pool AS block, MIN(d.date_key) AS first_day, MAX(d.date_key) + 1 AS end_day
  FROM dim_date d JOIN params p ON p.run_key = d.run_key GROUP BY d.run_key, d.sim_year / p.pool)
SELECT b.run_key, 'POP-12' AS kpi_code, NULL AS faction_key, b.first_day AS period_from_day, b.end_day AS period_to_day,
       CASE WHEN (1000 * COALESCE(e.person_days, 0)) / p.dpy < (SELECT value FROM meta_kpi_param WHERE name = 'pop12_min_person_years_pm1000') THEN NULL
            ELSE (2 * COALESCE(x.deaths, 0) * 1000000 * p.dpy + e.person_days) / (2 * e.person_days) END AS actual_value,
       COALESCE(e.person_days, 0) AS sample_n,
       CASE WHEN (1000 * COALESCE(e.person_days, 0)) / p.dpy < (SELECT value FROM meta_kpi_param WHERE name = 'pop12_min_person_years_pm1000') THEN 'min_cell' END AS missing_reason
FROM blocks b JOIN params p ON p.run_key = b.run_key
LEFT JOIN expo e ON e.run_key = b.run_key AND e.block = b.block
LEFT JOIN dead x ON x.run_key = b.run_key AND x.block = b.block;
