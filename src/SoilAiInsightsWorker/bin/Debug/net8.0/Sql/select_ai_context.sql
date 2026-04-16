-- =============================================================================
-- Select rows from analytics.v_device_ai_context (one row per device).
-- Placeholders (SqlTemplateProvider): {{PROJECT}}, {{ANALYTICS_DATASET}}, {{DEVICE_FILTER}}
--
-- Optional single-device runs: set {{DEVICE_FILTER}} to:
--   AND device_id = @device_id
-- Batch runs: use empty string for {{DEVICE_FILTER}}
-- =============================================================================

SELECT
  c.*
FROM `{{PROJECT}}.{{ANALYTICS_DATASET}}.v_device_ai_context` AS c
WHERE 1 = 1
{{DEVICE_FILTER}}
ORDER BY c.device_id;
