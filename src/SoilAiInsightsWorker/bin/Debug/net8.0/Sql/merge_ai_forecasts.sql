-- =============================================================================
-- Idempotent MERGE: one row per (device_id, forecast_date, forecast_horizon_hours).
-- forecast_id is stable via app-generated deterministic id.
--
-- Placeholders: {{PROJECT}}, {{ANALYTICS_DATASET}}, {{FORECASTS_TABLE}}
-- =============================================================================

MERGE `{{PROJECT}}.{{ANALYTICS_DATASET}}.{{FORECASTS_TABLE}}` AS T
USING (
  SELECT
    @forecast_id AS forecast_id,
    @device_id AS device_id,
    @user_id AS user_id,
    @forecast_date AS forecast_date,
    @forecast_horizon_hours AS forecast_horizon_hours,
    @forecast_for_time AS forecast_for_time,
    @predicted_temperature AS predicted_temperature,
    @predicted_moisture AS predicted_moisture,
    @predicted_conductivity AS predicted_conductivity,
    @predicted_ph_value AS predicted_ph_value,
    @predicted_temperature_band AS predicted_temperature_band,
    @predicted_moisture_band AS predicted_moisture_band,
    @predicted_conductivity_band AS predicted_conductivity_band,
    @predicted_ph_band AS predicted_ph_band,
    @predicted_risk_score AS predicted_risk_score,
    @anomaly_risk AS anomaly_risk,
    @stress_risk AS stress_risk,
    @confidence AS confidence,
    @title AS title,
    @forecast_summary AS forecast_summary,
    @recommended_preventive_action AS recommended_preventive_action,
    PARSE_JSON(@supporting_facts_json) AS supporting_facts,
    PARSE_JSON(@raw_context_json) AS raw_context,
    PARSE_JSON(@model_output_json) AS model_output,
    @model_name AS model_name,
    @prompt_version AS prompt_version,
    @created_at AS created_at,
    @updated_at AS updated_at
) AS S
ON T.device_id = S.device_id
  AND T.forecast_date = S.forecast_date
  AND T.forecast_horizon_hours = S.forecast_horizon_hours
WHEN MATCHED THEN
  UPDATE SET
    forecast_id = S.forecast_id,
    user_id = S.user_id,
    forecast_for_time = S.forecast_for_time,
    predicted_temperature = S.predicted_temperature,
    predicted_moisture = S.predicted_moisture,
    predicted_conductivity = S.predicted_conductivity,
    predicted_ph_value = S.predicted_ph_value,
    predicted_temperature_band = S.predicted_temperature_band,
    predicted_moisture_band = S.predicted_moisture_band,
    predicted_conductivity_band = S.predicted_conductivity_band,
    predicted_ph_band = S.predicted_ph_band,
    predicted_risk_score = S.predicted_risk_score,
    anomaly_risk = S.anomaly_risk,
    stress_risk = S.stress_risk,
    confidence = S.confidence,
    title = S.title,
    forecast_summary = S.forecast_summary,
    recommended_preventive_action = S.recommended_preventive_action,
    supporting_facts = S.supporting_facts,
    raw_context = S.raw_context,
    model_output = S.model_output,
    model_name = S.model_name,
    prompt_version = S.prompt_version,
    updated_at = S.updated_at
WHEN NOT MATCHED THEN
  INSERT (
    forecast_id,
    device_id,
    user_id,
    forecast_date,
    forecast_horizon_hours,
    forecast_for_time,
    predicted_temperature,
    predicted_moisture,
    predicted_conductivity,
    predicted_ph_value,
    predicted_temperature_band,
    predicted_moisture_band,
    predicted_conductivity_band,
    predicted_ph_band,
    predicted_risk_score,
    anomaly_risk,
    stress_risk,
    confidence,
    title,
    forecast_summary,
    recommended_preventive_action,
    supporting_facts,
    raw_context,
    model_output,
    model_name,
    prompt_version,
    created_at,
    updated_at
  )
  VALUES (
    S.forecast_id,
    S.device_id,
    S.user_id,
    S.forecast_date,
    S.forecast_horizon_hours,
    S.forecast_for_time,
    S.predicted_temperature,
    S.predicted_moisture,
    S.predicted_conductivity,
    S.predicted_ph_value,
    S.predicted_temperature_band,
    S.predicted_moisture_band,
    S.predicted_conductivity_band,
    S.predicted_ph_band,
    S.predicted_risk_score,
    S.anomaly_risk,
    S.stress_risk,
    S.confidence,
    S.title,
    S.forecast_summary,
    S.recommended_preventive_action,
    S.supporting_facts,
    S.raw_context,
    S.model_output,
    S.model_name,
    S.prompt_version,
    S.created_at,
    S.updated_at
  );
