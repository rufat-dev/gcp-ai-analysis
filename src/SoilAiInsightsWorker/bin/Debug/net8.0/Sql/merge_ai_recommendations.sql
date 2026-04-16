-- =============================================================================
-- Idempotent MERGE: one row per (device_id, insight_date).
-- Logical key: device_id + insight_date (DATETIME, start of insight day UTC).
-- recommendation_id is stable per run via app-generated deterministic id.
--
-- Placeholders: {{PROJECT}}, {{ANALYTICS_DATASET}}, {{RECOMMENDATIONS_TABLE}}
-- Parameters: named @... (see AiInsightsJobOrchestrator / BigQuery runner).
-- =============================================================================

MERGE `{{PROJECT}}.{{ANALYTICS_DATASET}}.{{RECOMMENDATIONS_TABLE}}` AS T
USING (
  SELECT
    @recommendation_id AS recommendation_id,
    @device_id AS device_id,
    @user_id AS user_id,
    @insight_date AS insight_date,
    @status AS status,
    @priority AS priority,
    @urgency AS urgency,
    @confidence AS confidence,
    @title AS title,
    @summary AS summary,
    @recommendation AS recommendation,
    @probable_cause AS probable_cause,
    @reasoning_scope AS reasoning_scope,
    @risk_score AS risk_score,
    @ai_status AS ai_status,
    @active_anomaly_count AS active_anomaly_count,
    @active_out_of_range_count AS active_out_of_range_count,
    PARSE_JSON(@supporting_facts_json) AS supporting_facts,
    PARSE_JSON(@triggered_by_json) AS triggered_by,
    PARSE_JSON(@raw_context_json) AS raw_context,
    PARSE_JSON(@model_output_json) AS model_output,
    @model_name AS model_name,
    @prompt_version AS prompt_version,
    @created_at AS created_at,
    @updated_at AS updated_at
) AS S
ON T.device_id = S.device_id
  AND T.insight_date = S.insight_date
WHEN MATCHED THEN
  UPDATE SET
    recommendation_id = S.recommendation_id,
    user_id = S.user_id,
    status = S.status,
    priority = S.priority,
    urgency = S.urgency,
    confidence = S.confidence,
    title = S.title,
    summary = S.summary,
    recommendation = S.recommendation,
    probable_cause = S.probable_cause,
    reasoning_scope = S.reasoning_scope,
    risk_score = S.risk_score,
    ai_status = S.ai_status,
    active_anomaly_count = S.active_anomaly_count,
    active_out_of_range_count = S.active_out_of_range_count,
    supporting_facts = S.supporting_facts,
    triggered_by = S.triggered_by,
    raw_context = S.raw_context,
    model_output = S.model_output,
    model_name = S.model_name,
    prompt_version = S.prompt_version,
    updated_at = S.updated_at
WHEN NOT MATCHED THEN
  INSERT (
    recommendation_id,
    device_id,
    user_id,
    insight_date,
    status,
    priority,
    urgency,
    confidence,
    title,
    summary,
    recommendation,
    probable_cause,
    reasoning_scope,
    risk_score,
    ai_status,
    active_anomaly_count,
    active_out_of_range_count,
    supporting_facts,
    triggered_by,
    raw_context,
    model_output,
    model_name,
    prompt_version,
    created_at,
    updated_at
  )
  VALUES (
    S.recommendation_id,
    S.device_id,
    S.user_id,
    S.insight_date,
    S.status,
    S.priority,
    S.urgency,
    S.confidence,
    S.title,
    S.summary,
    S.recommendation,
    S.probable_cause,
    S.reasoning_scope,
    S.risk_score,
    S.ai_status,
    S.active_anomaly_count,
    S.active_out_of_range_count,
    S.supporting_facts,
    S.triggered_by,
    S.raw_context,
    S.model_output,
    S.model_name,
    S.prompt_version,
    S.created_at,
    S.updated_at
  );
