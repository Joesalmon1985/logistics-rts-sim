-- Example queries over a run's warehouse.sqlite (M13a core: today's events). Each statement is separated by a semicolon and is
-- run by the test WarehouseTests.ExampleQueries_Run, so they stay valid when the schema grows. Open the file with any SQLite
-- client, for example:  sqlite3 out/adhoc/watch6/1/<run id>/warehouse.sqlite < ExampleQueries.sql
-- Day numbers are sim days (day 0 first); "units" are ware units; integers everywhere.

-- Q1. Goods produced per faction per day. fact_production_day is aggregated before sampling, so it is exact at Summary and Full.
SELECT d.date_key AS day, f.name AS faction, w.name AS ware, SUM(p.quantity) AS produced
FROM fact_production_day p
JOIN dim_date d ON d.date_key = p.date_key
JOIN dim_faction f ON f.faction_key = p.faction_key
JOIN dim_ware w ON w.ware_key = p.ware_key
GROUP BY d.date_key, f.name, w.name
ORDER BY d.date_key, f.name, w.name;

-- Q2. Total goods per ware over the run (equals the Metrics counter wares_produced when summed over all wares).
SELECT w.name AS ware, SUM(p.quantity) AS produced, SUM(p.cycles) AS cycles
FROM fact_production_day p JOIN dim_ware w ON w.ware_key = p.ware_key
GROUP BY w.name ORDER BY produced DESC;

-- Q3. Soldiers lost per faction: killed in combat, and disbanded because their faction fell (combat_kind 2 = unit_died).
SELECT f.name AS faction,
       SUM(CASE WHEN c.outcome_code = 0 THEN c.events ELSE 0 END) AS killed_in_combat,
       SUM(CASE WHEN c.outcome_code = 1 THEN c.events ELSE 0 END) AS disbanded
FROM fact_combat_day c JOIN dim_faction f ON f.faction_key = c.faction_key
WHERE c.kind_code = 2
GROUP BY f.name ORDER BY f.name;

-- Q4. Attack outcomes (combat_kind 1 = attack): hits, misses, blocks, crits, building strikes, with the damage dealt.
SELECT o.name AS outcome, SUM(c.events) AS attacks, SUM(c.damage) AS damage
FROM fact_combat_day c JOIN dim_code o ON o.code_set = 'attack_outcome' AND o.code = c.outcome_code
WHERE c.kind_code = 1
GROUP BY o.name ORDER BY attacks DESC;

-- Q5. Buildings completed per faction (the faction that owns the building now) and how long they took from placement.
SELECT f.name AS faction, COUNT(*) AS completed,
       AVG(b.completed_tick - b.placed_tick) AS mean_build_ticks
FROM dim_building b JOIN dim_faction f ON f.faction_key = b.faction_key
WHERE b.completed_tick IS NOT NULL AND b.placed_tick IS NOT NULL
GROUP BY f.name ORDER BY f.name;

-- Q6. Daily standings: victory points, headquarters and soldiers per faction on the last day of each sim year.
WITH last_day AS (SELECT sim_year, MAX(date_key) AS d FROM dim_date GROUP BY sim_year)
SELECT l.sim_year, f.name AS faction, x.victory_points, x.hqs, x.units, x.stock_total
FROM last_day l
JOIN fact_faction_day x ON x.date_key = l.d
JOIN dim_faction f ON f.faction_key = x.faction_key
ORDER BY l.sim_year, x.victory_points DESC, f.name;

-- Q7. One ware's stock in the HQs over time (grain), per faction.
SELECT s.date_key AS day, f.name AS faction, s.quantity
FROM fact_faction_day_stock s
JOIN dim_faction f ON f.faction_key = s.faction_key
WHERE s.ware_key = (SELECT ware_key FROM dim_ware WHERE name = 'grain')
ORDER BY s.date_key, f.name;

-- Q8. Event counts by type for the run, exact (fact_event_day is aggregated before sampling).
SELECT t.name AS event, t.domain, SUM(e.events) AS events
FROM fact_event_day e JOIN meta_event_type t ON t.code = e.event_code
GROUP BY t.name, t.domain ORDER BY events DESC;

-- Q9. Estimated count of a high-volume event from the sample (sample_weight = the sampling interval) next to the exact count.
SELECT (SELECT SUM(sample_weight) FROM fact_combat WHERE kind_code = 1) AS estimated_attacks,
       (SELECT SUM(events) FROM fact_combat_day WHERE kind_code = 1) AS exact_attacks;

-- Q10. Commands applied or rejected, by type and faction.
SELECT c.command_type, f.name AS faction, SUM(c.applied) AS applied, SUM(1 - c.applied) AS rejected
FROM fact_command c LEFT JOIN dim_faction f ON f.faction_key = c.faction_key
GROUP BY c.command_type, f.name ORDER BY c.command_type, f.name;

-- Q11. A faction's history (type 2 dimension): half-open spells [valid_from_tick, valid_to_tick).
SELECT f.name AS faction, h.valid_from_tick, h.valid_to_tick, h.controller, h.eliminated, h.change_reason
FROM dim_faction_history h JOIN dim_faction f ON f.faction_key = h.faction_key
ORDER BY f.name, h.valid_from_tick;

-- Q12. Which table each event type lands in (meta_event_map), and the mapper that handles it.
SELECT t.code, t.name, t.domain, m.target_table, m.mapper
FROM meta_event_type t JOIN meta_event_map m ON m.code = t.code AND m.version = t.version
ORDER BY t.code;

-- Q13. Run facts and the metrics at the end of the run.
SELECT r.run_id, r.scenario_name, r.seed, r.recorder_level, r.final_tick, r.final_hash, r.stop_reason, r.exit_code
FROM dim_run r;
