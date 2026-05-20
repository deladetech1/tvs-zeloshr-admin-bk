CREATE TABLE IF NOT EXISTS zeloshr.zhr_audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(128) NOT NULL,
    org_id VARCHAR(128) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    action_title VARCHAR(255) NOT NULL,
    action_description TEXT,
    employee_id UUID,
    employee_display_code VARCHAR(32),
    employee_full_name VARCHAR(200),
    actor_id VARCHAR(128),
    actor_full_name VARCHAR(200) NOT NULL,
    category VARCHAR(50) NOT NULL,
    severity VARCHAR(20) NOT NULL,
    is_flagged BOOLEAN NOT NULL DEFAULT FALSE,
    is_sensitive_read BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_zhr_audit_logs_occurred
    ON zeloshr.zhr_audit_logs (tenant_id, org_id, occurred_at DESC);
